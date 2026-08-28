# MuEldryn Launcher
# =================
# Auto-update de arquivos do client (Data/, etc.) SEM rebuildar o Main.exe.
# IP/porta vêm do Launcher.bmd e são passados ao Main via:
#   Main.exe connect /uIP /pPORTA

## Configuração atual (VPS MuEldryn)
- Update URL: `http://170.80.224.11/update/`
- Connect IP: `170.80.224.11`
- Connect Port: `44406`
- GameServer Port (status): `55901`
- Client exe: `Main.exe`

## Estrutura no servidor web (IIS / nginx / Apache)
```
/update/
  MiniUpdate/
    update.info
    Data/...
    (outros arquivos a atualizar)
  FullUpdate/          (opcional — verificação completa)
    client.info
    ...
```

## Publicar uma atualização (sem rebuild do Main)
1. Altere só os arquivos do client (ex.: `Data\Local\Item.bmd`, UI, etc.).
2. Gere o pacote:
   ```powershell
   cd MUPegasusOldLauncher\tools
   .\BuildMiniUpdate.ps1 -ClientRoot "C:\caminho\do\cliente" -Files @(
     "Data\Local\Item.bmd",
     "Data\Interface\algum.ozj"
   )
   ```
   Ou tudo em Data\:
   ```powershell
   .\BuildMiniUpdate.ps1 -ClientRoot "C:\caminho\do\cliente" -IncludeAllData
   ```
3. Envie a pasta `UpdateServer\MiniUpdate\` para `http://170.80.224.11/update/MiniUpdate/`.
4. O jogador abre o Launcher → Start → baixa só o que mudou (CRC) → abre o jogo.

## Regenerar Launcher.bmd
```powershell
cd MUPegasusOldLauncher\tools
.\csc-tools.ps1   # compila utilitários
.\MakeLauncherBmd.exe --ip 170.80.224.11 --cs 44406 --gs 55901 --url http://170.80.224.11/update/
```

## Compilar o Launcher
Abra `Launcher.sln` no Visual Studio (ou MSBuild) e compile Release.
Copie para a pasta do client:
- `Launcher.exe`
- `MuUpdater.exe`
- `Data\Local\Launcher.bmd`
- Resources embutidos já vão no exe

## Branding
A arte `Resources\background.png` recebe a logo Mu Eldryn no canto (Elev8 coberto).
Backup: `background_elev8_backup.png`.
Para reaplicar:
```powershell
.\csc-tools.ps1
.\BrandBackground.exe
```
