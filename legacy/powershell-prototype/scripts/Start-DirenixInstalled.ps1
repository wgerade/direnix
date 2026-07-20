param(
    [Parameter(Mandatory = $true)]
    [string]$ConfigPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function New-DirectoryIfMissing {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Force -Path $Path | Out-Null
    }
}

if (-not (Test-Path -LiteralPath $ConfigPath -PathType Leaf)) {
    throw "Portal config not found: $ConfigPath"
}

$config = Get-Content -Raw -LiteralPath $ConfigPath | ConvertFrom-Json
$installRoot = [string]$config.installRoot
$scriptRoot = Join-Path $installRoot 'scripts'
$portalScript = Join-Path $scriptRoot 'Start-DirenixPortal.ps1'

if (-not (Test-Path -LiteralPath $portalScript -PathType Leaf)) {
    throw "Portal script not found: $portalScript"
}

$logRoot = [string]$config.logRoot
if ([string]::IsNullOrWhiteSpace($logRoot)) {
    $logRoot = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Direnix\logs'
}
New-DirectoryIfMissing $logRoot

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$transcript = Join-Path $logRoot "portal-$($config.port)-$stamp.log"
$latest = Join-Path $logRoot 'portal-latest.log'

try {
    Start-Transcript -LiteralPath $transcript -Force | Out-Null
    "Direnix installed portal starting"
    "Config: $ConfigPath"
    "InstallRoot: $installRoot"
    "DataRoot: $($config.dataRoot)"
    "OutputRoot: $($config.outputRoot)"
    "Url: $($config.portalUrl)"

    Copy-Item -LiteralPath $transcript -Destination $latest -Force -ErrorAction SilentlyContinue

    & $portalScript `
        -Port ([int]$config.port) `
        -ListenAddress ([string]$config.listenAddress) `
        -DataRoot ([string]$config.dataRoot) `
        -OutputRoot ([string]$config.outputRoot) `
        -ConfigPath $ConfigPath
}
catch {
    $message = "Direnix installed portal failed: $($_.Exception.Message)"
    $message | Tee-Object -FilePath (Join-Path $logRoot 'portal-error-latest.log') -Append
    throw
}
finally {
    try {
        Stop-Transcript | Out-Null
        Copy-Item -LiteralPath $transcript -Destination $latest -Force -ErrorAction SilentlyContinue
    }
    catch {}
}
