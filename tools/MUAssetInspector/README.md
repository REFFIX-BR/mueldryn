# MU Asset Inspector

Desktop tool (C# + Avalonia, .NET 10) to analyze, compare, and diagnose MU Online assets between a **Source** (e.g. Mudream.online) and **Destination** (e.g. OpenMU MuMain) client.

## Projects

| Project | Purpose |
|---------|---------|
| `MUAssetInspector.Core` | Domain, SQLite database, scanner, profiles, workspace |
| `MUAssetInspector.Formats` | OZJ/OZT/BMD/effect parsers |
| `MUAssetInspector.Analysis` | Dependency graph, compatibility diagnostics, reports |
| `MUAssetInspector.Migration` | Batch analysis, repair preview, migration packages (Phase 2) |
| `MUAssetInspector.App` | Avalonia UI |
| `MUAssetInspector.Cli` | `analyze` / `batch` for CI |

## Build

```powershell
cd tools/MUAssetInspector
dotnet build MUAssetInspector.sln
dotnet run --project src/MUAssetInspector.App
```

## CLI

```powershell
dotnet run --project src/MUAssetInspector.Cli -- analyze `
  --source "..\..\Mudream.online\Data" `
  --dest "..\..\MuMain\src\bin\Data" `
  --profile mudream `
  --report Workspace/Reports
```

## Profiles

Edit `profiles/mudream.json` and `profiles/openmu-mumain.json` for OZJ/OZT header skips, effect table paths, and catalog references.

All writes go to `Workspace/` — source/destination folders are never modified automatically.
