#Requires -Version 5.1
<#
.SYNOPSIS
  Exporta bancos locais antes do deploy na VPS.

.DESCRIPTION
  1) OpenMU (Postgres Docker porta 5433) -> backups/openmu-local.dump
  2) Morpheus (SQL Server SQLEXPRESS)     -> backups/morpheus-muonline.bak

  O jogo usa o Postgres. O site Morpheus usa SQL Server separado;
  o .bak guarda contas web, créditos, loja etc. para não perder nada.
#>
param(
    [string]$BackupDir = (Join-Path (Split-Path $PSScriptRoot -Parent) "backups"),
    [string]$PgContainer = "database",
    [string]$SqlInstance = "localhost\SQLEXPRESS",
    [string]$SqlUser = "morpheus",
    [string]$SqlPassword = "Morph3us@Local!",
    [string]$SqlDatabase = "MuOnline"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

Write-Host "==> OpenMU Postgres (container: $PgContainer)..." -ForegroundColor Cyan
$pgRunning = docker ps --filter "name=$PgContainer" --format "{{.Names}}"
if (-not $pgRunning) {
    throw "Container Postgres '$PgContainer' nao esta rodando. Suba com: docker start database"
}

$openmuDump = Join-Path $BackupDir "openmu-local.dump"
docker exec $PgContainer pg_dump -U postgres -Fc openmu -f /tmp/openmu-local.dump
docker cp "${PgContainer}:/tmp/openmu-local.dump" $openmuDump
docker exec $PgContainer rm -f /tmp/openmu-local.dump
$sizeMb = [math]::Round((Get-Item $openmuDump).Length / 1MB, 2)
Write-Host "    OK: $openmuDump ($sizeMb MB)" -ForegroundColor Green

Write-Host "==> Morpheus SQL Server ($SqlDatabase)..." -ForegroundColor Cyan
$sqlBackupDir = sqlcmd -S $SqlInstance -U $SqlUser -P $SqlPassword -Q "SET NOCOUNT ON; SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(512))" -W -h-1
if (-not $sqlBackupDir) { throw "Nao foi possivel obter pasta de backup do SQL Server." }
$sqlBackupDir = $sqlBackupDir.Trim()
$sqlBakOnServer = Join-Path $sqlBackupDir "morpheus-muonline.bak"
$localBak = Join-Path $BackupDir "morpheus-muonline.bak"

$backupSql = "BACKUP DATABASE [$SqlDatabase] TO DISK = N'$sqlBakOnServer' WITH FORMAT, INIT;"
sqlcmd -S $SqlInstance -U $SqlUser -P $SqlPassword -Q $backupSql -W | Out-Null

try {
    Copy-Item $sqlBakOnServer $localBak -Force
    $sizeMb = [math]::Round((Get-Item $localBak).Length / 1MB, 2)
    Write-Host "    OK: $localBak ($sizeMb MB)" -ForegroundColor Green
}
catch {
    Write-Warning "Backup SQL criado em '$sqlBakOnServer', mas copia falhou (permissao)."
    Write-Warning "Copie manualmente como Administrador ou use o .bak direto da pasta do SQL Server."
}

Write-Host ""
Write-Host "Resumo do Postgres local:" -ForegroundColor Yellow
Get-Content (Join-Path $PSScriptRoot "check-local-db.sql") | docker exec -i $PgContainer psql -U postgres -d openmu -t

Write-Host ""
Write-Host "Proximo passo: enviar backups/ para a VPS e seguir DEPLOY-VPS-MUELDRYN.md" -ForegroundColor Cyan
