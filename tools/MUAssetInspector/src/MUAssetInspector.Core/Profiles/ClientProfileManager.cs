using System.Text.Json;
using MUAssetInspector.Core.Domain;

namespace MUAssetInspector.Core.Profiles;

public sealed class ClientProfileManager
{
    private readonly string _profilesDirectory;

    public ClientProfileManager(string profilesDirectory)
    {
        _profilesDirectory = profilesDirectory;
    }

    public IReadOnlyList<string> ListProfiles()
    {
        if (!Directory.Exists(_profilesDirectory))
            return [];

        return Directory.GetFiles(_profilesDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => n is not null)
            .Cast<string>()
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public ClientProfile Load(string profileName)
    {
        var path = Path.Combine(_profilesDirectory, $"{profileName}.json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Profile not found: {profileName}", path);

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ClientProfile>(json, JsonOptions()) ?? new ClientProfile { Name = profileName };
    }

    public void Save(ClientProfile profile)
    {
        Directory.CreateDirectory(_profilesDirectory);
        var fileName = string.IsNullOrWhiteSpace(profile.Name) ? "custom" : profile.Name.Replace(' ', '-').ToLowerInvariant();
        var path = Path.Combine(_profilesDirectory, $"{fileName}.json");
        var json = JsonSerializer.Serialize(profile, JsonOptions());
        File.WriteAllText(path, json);
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
