<#
  Skill UP - one-step local database setup.

  Creates App_Data\SkillUp.mdf on SQL Server LocalDB and runs Database\create-tables.sql
  (all 40 tables) and Database\backend-extensions.sql. Run it once after cloning, from the project folder:

      powershell -ExecutionPolicy Bypass -File Database\setup-db.ps1

  Needs SQL Server LocalDB and sqlcmd, both installed with Visual Studio's
  "ASP.NET and web development" workload. Safe to re-run: it stops if SkillUp.mdf exists.
#>
$ErrorActionPreference = 'Stop'

$root    = Split-Path -Parent $PSScriptRoot
$dataDir = Join-Path $root 'App_Data'
$mdf     = Join-Path $dataDir 'SkillUp.mdf'
$ldf     = Join-Path $dataDir 'SkillUp_log.ldf'
$sqlFile = Join-Path $PSScriptRoot 'create-tables.sql'
$server  = '(localdb)\MSSQLLocalDB'
$tempDb  = 'SkillUp_Setup'

foreach ($tool in 'SqlLocalDB', 'sqlcmd') {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        throw "$tool was not found. Install the 'ASP.NET and web development' workload in Visual Studio Installer."
    }
}

if (Test-Path $mdf) {
    Write-Host "App_Data\SkillUp.mdf already exists. Nothing to do. Delete it (and SkillUp_log.ldf) to start over."
    exit 0
}

function Invoke-Sql([string]$query, [string]$database = 'master') {
    sqlcmd -S $server -d $database -E -b -Q $query
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed (exit code $LASTEXITCODE)." }
}

Write-Host 'Starting LocalDB instance MSSQLLocalDB...'
SqlLocalDB start MSSQLLocalDB | Out-Null

New-Item -ItemType Directory -Force -Path $dataDir | Out-Null

# Clear a leftover registration from an earlier failed run.
Invoke-Sql "IF DB_ID(N'$tempDb') IS NOT NULL BEGIN ALTER DATABASE [$tempDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$tempDb]; END"

Write-Host 'Creating App_Data\SkillUp.mdf...'
Invoke-Sql "CREATE DATABASE [$tempDb] ON (NAME = N'SkillUp', FILENAME = N'$mdf') LOG ON (NAME = N'SkillUp_log', FILENAME = N'$ldf')"

try {
    Write-Host 'Creating tables...'
    sqlcmd -S $server -d $tempDb -E -b -i $sqlFile
    if ($LASTEXITCODE -ne 0) { throw "create-tables.sql failed (exit code $LASTEXITCODE)." }
    # Brings an older database up to date; on a new one it finds nothing to do. The app also runs it on every start.
    sqlcmd -S $server -d $tempDb -E -b -i (Join-Path $PSScriptRoot 'backend-extensions.sql')
    if ($LASTEXITCODE -ne 0) { throw "backend-extensions.sql failed (exit code $LASTEXITCODE)." }
}
finally {
    # Detach so the web app can attach the file itself through AttachDbFilename (see Web.config).
    Invoke-Sql "ALTER DATABASE [$tempDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; EXEC sp_detach_db N'$tempDb';"
}

Write-Host 'Done. App_Data\SkillUp.mdf is ready.'
