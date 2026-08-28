$ErrorActionPreference = "Stop"
$tools = $PSScriptRoot
$csc = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { throw "csc.exe not found: $csc" }

Write-Host "BrandBackground..."
& $csc /nologo /target:exe /out:"$tools\BrandBackground.exe" /r:System.Drawing.dll "$tools\BrandBackground.cs"
& "$tools\BrandBackground.exe"

Write-Host "MakeLauncherBmd..."
& $csc /nologo /target:exe /out:"$tools\MakeLauncherBmd.exe" "$tools\MakeLauncherBmd.cs"
& "$tools\MakeLauncherBmd.exe"

Write-Host "MuUpdater..."
& $csc /nologo /target:winexe /out:"$tools\..\Launcher\bin\Release\MuUpdater.exe" /r:System.Windows.Forms.dll "$tools\MuUpdater.cs"
Copy-Item -Force "$tools\..\Launcher\bin\Release\MuUpdater.exe" "$tools\MuUpdater.exe"

Write-Host "MuEldrynLaunch..."
& $csc /nologo /target:exe /out:"$tools\..\Launcher\bin\Release\MuEldrynLaunch.exe" "$tools\MuEldrynLaunch.cs"
Copy-Item -Force "$tools\..\Launcher\bin\Release\MuEldrynLaunch.exe" "$tools\MuEldrynLaunch.exe"

Write-Host "Patch Launcher.exe strings..."
powershell -NoProfile -ExecutionPolicy Bypass -File "$tools\PatchLauncherExe.ps1"

# imagebk2.jpg = background customizado (launcher legado carrega automaticamente)
$bg = Join-Path (Split-Path $tools -Parent) "Launcher\Resources\background.png"
$img2 = Join-Path (Split-Path $tools -Parent) "Launcher\bin\Release\imagebk2.jpg"
if (Test-Path $bg) { Copy-Item -Force $bg $img2; Write-Host "imagebk2.jpg OK" }

Write-Host "OK tools ready."
