using Microsoft.Data.Sqlite;
using MUAssetInspector.Core.Domain;

namespace MUAssetInspector.Core.Database;

public sealed class AssetDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly object _gate = new();
    private readonly Dictionary<string, AssetRecord> _pathIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AssetRecord> _nameIndex = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public AssetDatabase(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        using (var pragma = _connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;";
            pragma.ExecuteNonQuery();
        }
        InitializeSchema();
        LoadIndex();
    }

    public IReadOnlyDictionary<string, AssetRecord> PathIndex => _pathIndex;
    public IReadOnlyDictionary<string, AssetRecord> NameIndex => _nameIndex;
    public IReadOnlyList<AssetRecord> AllAssets
    {
        get
        {
            lock (_gate)
            {
                return _pathIndex.Values.ToList();
            }
        }
    }

    private void InitializeSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS assets (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                client_role TEXT NOT NULL,
                relative_path TEXT NOT NULL,
                file_name_lower TEXT NOT NULL,
                extension TEXT NOT NULL,
                format TEXT NOT NULL,
                sha256 TEXT NOT NULL,
                size INTEGER NOT NULL,
                last_write_utc_ticks INTEGER NOT NULL,
                width INTEGER,
                height INTEGER,
                has_alpha INTEGER NOT NULL DEFAULT 0,
                alpha_kind TEXT NOT NULL DEFAULT 'Unknown',
                parse_status TEXT NOT NULL DEFAULT 'NotParsed',
                parsed_json TEXT,
                category TEXT NOT NULL DEFAULT 'Other'
            );
            CREATE UNIQUE INDEX IF NOT EXISTS idx_assets_path ON assets(relative_path COLLATE NOCASE);
            CREATE INDEX IF NOT EXISTS idx_assets_name_lower ON assets(file_name_lower);
            CREATE INDEX IF NOT EXISTS idx_assets_sha256 ON assets(sha256);

            CREATE TABLE IF NOT EXISTS dependencies (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                asset_id INTEGER NOT NULL,
                depends_on_asset_id INTEGER,
                dep_type TEXT NOT NULL,
                ref_name TEXT NOT NULL,
                status TEXT NOT NULL DEFAULT 'Pending',
                FOREIGN KEY(asset_id) REFERENCES assets(id)
            );

            CREATE TABLE IF NOT EXISTS items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                item_group INTEGER NOT NULL,
                item_number INTEGER NOT NULL,
                name TEXT NOT NULL,
                model_asset_id INTEGER,
                source_profile TEXT NOT NULL,
                model_path TEXT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS idx_items_key ON items(item_group, item_number, source_profile);

            CREATE TABLE IF NOT EXISTS comparisons (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                source_asset_id INTEGER NOT NULL,
                dest_asset_id INTEGER,
                status TEXT NOT NULL,
                diff_json TEXT
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private void LoadIndex()
    {
        _pathIndex.Clear();
        _nameIndex.Clear();
        foreach (var asset in QueryAssets())
        {
            _pathIndex[asset.RelativePath] = asset;
            _nameIndex[asset.FileNameLower] = asset;
        }
    }

    public List<AssetRecord> QueryAssetsSnapshot(string? filter = null, AssetCategory? category = null)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            return QueryAssetsInternal(filter, category);
        }
    }

    public IEnumerable<AssetRecord> QueryAssets(string? filter = null, AssetCategory? category = null)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            return QueryAssetsInternal(filter, category).ToList();
        }
    }

    private List<AssetRecord> QueryAssetsInternal(string? filter, AssetCategory? category)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM assets WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(filter))
        {
            cmd.CommandText += " AND (relative_path LIKE @f OR file_name_lower LIKE @f)";
            cmd.Parameters.AddWithValue("@f", $"%{filter}%");
        }
        if (category.HasValue)
        {
            cmd.CommandText += " AND category = @cat";
            cmd.Parameters.AddWithValue("@cat", category.Value.ToString());
        }
        cmd.CommandText += " ORDER BY relative_path COLLATE NOCASE";

        using var reader = cmd.ExecuteReader();
        var results = new List<AssetRecord>();
        while (reader.Read())
            results.Add(ReadAsset(reader));
        return results;
    }

    public AssetRecord? FindByPath(string relativePath)
    {
        lock (_gate)
        {
            _pathIndex.TryGetValue(relativePath, out var asset);
            return asset;
        }
    }

    public AssetRecord? FindByFileName(string fileName)
    {
        var lower = Path.GetFileName(fileName).ToLowerInvariant();
        lock (_gate)
        {
            _nameIndex.TryGetValue(lower, out var asset);
            return asset;
        }
    }

    public long UpsertAsset(AssetRecord asset)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO assets (client_role, relative_path, file_name_lower, extension, format, sha256, size,
                last_write_utc_ticks, width, height, has_alpha, alpha_kind, parse_status, parsed_json, category)
            VALUES (@role, @path, @nameLower, @ext, @format, @sha, @size, @ticks, @w, @h, @alpha, @alphaKind,
                @parse, @json, @cat)
            ON CONFLICT(relative_path) DO UPDATE SET
                sha256=@sha, size=@size, last_write_utc_ticks=@ticks, width=@w, height=@h,
                has_alpha=@alpha, alpha_kind=@alphaKind, parse_status=@parse, parsed_json=@json, category=@cat
            RETURNING id;
            """;
        BindAsset(cmd, asset);
        var id = (long)(cmd.ExecuteScalar() ?? 0L);
        asset.Id = id;
        _pathIndex[asset.RelativePath] = asset;
        _nameIndex[asset.FileNameLower] = asset;
        return id;
        }
    }

    public void SaveDependencies(long assetId, IEnumerable<DependencyRecord> deps)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            using var del = _connection.CreateCommand();
        del.CommandText = "DELETE FROM dependencies WHERE asset_id = @id";
        del.Parameters.AddWithValue("@id", assetId);
        del.ExecuteNonQuery();

        foreach (var dep in deps)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO dependencies (asset_id, depends_on_asset_id, dep_type, ref_name, status)
                VALUES (@aid, @did, @type, @ref, @status);
                """;
            cmd.Parameters.AddWithValue("@aid", assetId);
            cmd.Parameters.AddWithValue("@did", (object?)dep.DependsOnAssetId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@type", dep.DepType.ToString());
            cmd.Parameters.AddWithValue("@ref", dep.RefName);
            cmd.Parameters.AddWithValue("@status", dep.Status);
            cmd.ExecuteNonQuery();
        }
        }
    }

    public void UpsertItem(ItemRecord item)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO items (item_group, item_number, name, model_asset_id, source_profile, model_path)
            VALUES (@g, @n, @name, @mid, @profile, @mpath)
            ON CONFLICT(item_group, item_number, source_profile) DO UPDATE SET
                name=@name, model_asset_id=@mid, model_path=@mpath;
            """;
        cmd.Parameters.AddWithValue("@g", item.Group);
        cmd.Parameters.AddWithValue("@n", item.Number);
        cmd.Parameters.AddWithValue("@name", item.Name);
        cmd.Parameters.AddWithValue("@mid", (object?)item.ModelAssetId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@profile", item.SourceProfile);
        cmd.Parameters.AddWithValue("@mpath", (object?)item.ModelPath ?? DBNull.Value);
        cmd.ExecuteNonQuery();
        }
    }

    public IEnumerable<ItemRecord> QueryItems()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, item_group, item_number, name, model_asset_id, source_profile, model_path FROM items ORDER BY item_group, item_number";
        using var reader = cmd.ExecuteReader();
        var items = new List<ItemRecord>();
        while (reader.Read())
        {
            items.Add(new ItemRecord
            {
                Id = reader.GetInt64(0),
                Group = reader.GetInt32(1),
                Number = reader.GetInt32(2),
                Name = reader.GetString(3),
                ModelAssetId = reader.IsDBNull(4) ? null : reader.GetInt64(4),
                SourceProfile = reader.GetString(5),
                ModelPath = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }
        return items;
        }
    }

    public void SaveComparison(ComparisonRecord comparison)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO comparisons (source_asset_id, dest_asset_id, status, diff_json)
            VALUES (@sid, @did, @status, @json);
            """;
        cmd.Parameters.AddWithValue("@sid", comparison.SourceAssetId);
        cmd.Parameters.AddWithValue("@did", (object?)comparison.DestAssetId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", comparison.Status.ToString());
        cmd.Parameters.AddWithValue("@json", (object?)comparison.DiffJson ?? DBNull.Value);
        cmd.ExecuteNonQuery();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AssetDatabase));
    }

    private static void BindAsset(SqliteCommand cmd, AssetRecord asset)
    {
        cmd.Parameters.AddWithValue("@role", asset.ClientRole.ToString());
        cmd.Parameters.AddWithValue("@path", asset.RelativePath);
        cmd.Parameters.AddWithValue("@nameLower", asset.FileNameLower);
        cmd.Parameters.AddWithValue("@ext", asset.Extension);
        cmd.Parameters.AddWithValue("@format", asset.Format.ToString());
        cmd.Parameters.AddWithValue("@sha", asset.Sha256);
        cmd.Parameters.AddWithValue("@size", asset.Size);
        cmd.Parameters.AddWithValue("@ticks", asset.LastWriteUtcTicks);
        cmd.Parameters.AddWithValue("@w", (object?)asset.Width ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@h", (object?)asset.Height ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@alpha", asset.HasAlpha ? 1 : 0);
        cmd.Parameters.AddWithValue("@alphaKind", asset.AlphaKind.ToString());
        cmd.Parameters.AddWithValue("@parse", asset.ParseStatus.ToString());
        cmd.Parameters.AddWithValue("@json", (object?)asset.ParsedJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cat", asset.Category.ToString());
    }

    private static AssetRecord ReadAsset(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(reader.GetOrdinal("id")),
        ClientRole = Enum.Parse<ClientRole>(reader.GetString(reader.GetOrdinal("client_role"))),
        RelativePath = reader.GetString(reader.GetOrdinal("relative_path")),
        FileNameLower = reader.GetString(reader.GetOrdinal("file_name_lower")),
        Extension = reader.GetString(reader.GetOrdinal("extension")),
        Format = Enum.Parse<AssetFormatKind>(reader.GetString(reader.GetOrdinal("format"))),
        Sha256 = reader.GetString(reader.GetOrdinal("sha256")),
        Size = reader.GetInt64(reader.GetOrdinal("size")),
        LastWriteUtcTicks = reader.GetInt64(reader.GetOrdinal("last_write_utc_ticks")),
        Width = reader.IsDBNull(reader.GetOrdinal("width")) ? null : reader.GetInt32(reader.GetOrdinal("width")),
        Height = reader.IsDBNull(reader.GetOrdinal("height")) ? null : reader.GetInt32(reader.GetOrdinal("height")),
        HasAlpha = reader.GetInt32(reader.GetOrdinal("has_alpha")) != 0,
        AlphaKind = Enum.Parse<AlphaKind>(reader.GetString(reader.GetOrdinal("alpha_kind"))),
        ParseStatus = Enum.Parse<ParseStatus>(reader.GetString(reader.GetOrdinal("parse_status"))),
        ParsedJson = reader.IsDBNull(reader.GetOrdinal("parsed_json")) ? null : reader.GetString(reader.GetOrdinal("parsed_json")),
        Category = Enum.Parse<AssetCategory>(reader.GetString(reader.GetOrdinal("category")))
    };

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            try { _connection.Close(); } catch { /* ignore */ }
            try { _connection.Dispose(); } catch { /* ignore */ }
        }
    }
}
