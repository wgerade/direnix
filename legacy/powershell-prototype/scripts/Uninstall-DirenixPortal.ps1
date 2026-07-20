param(
    [string]$TaskName = 'Direnix Portal',
    [string]$ServiceName = 'DirenixPortal',
    [string]$ConfigPath = '',
    [switch]$RemoveShortcuts,
    [switch]$RemoveConfig,
    [switch]$RemoveData,
    [switch]$RemoveInstallFiles
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptRoot

function Resolve-PortalConfigPath {
    param([string]$ExplicitPath)
    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) { $candidates += $ExplicitPath }
    if (-not [string]::IsNullOrWhiteSpace($env:DIRENIX_CONFIG_PATH)) { $candidates += $env:DIRENIX_CONFIG_PATH }
    $candidates += (Join-Path ${env:ProgramData} 'Direnix\config\portal.local.json')
    $candidates += (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Direnix\config\portal.local.json')
    $candidates += (Join-Path $script:ProjectRoot 'config\portal.local.json')
    foreach ($candidate in $candidates | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { return [IO.Path]::GetFullPath($candidate) }
    }
    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) { return $ExplicitPath }
    Join-Path $script:ProjectRoot 'config\portal.local.json'
}

function Remove-PathSafe {
    param(
        [string]$Path,
        [string]$ExpectedLeaf
    )
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    $full = [IO.Path]::GetFullPath($Path)
    if ((Split-Path -Leaf $full) -ne $ExpectedLeaf) {
        throw "Refusing to remove unexpected path: $full"
    }
    if (Test-Path -LiteralPath $full) {
        Remove-Item -LiteralPath $full -Recurse -Force -ErrorAction Stop
        return $true
    }
    $false
}

$resolvedConfigPath = Resolve-PortalConfigPath $ConfigPath
$config = $null
if (Test-Path -LiteralPath $resolvedConfigPath -PathType Leaf) {
    $config = Get-Content -Raw -LiteralPath $resolvedConfigPath | ConvertFrom-Json
}
if ($config -and $config.PSObject.Properties['taskName']) { $TaskName = [string]$config.taskName }
if ($config -and $config.PSObject.Properties['serviceName']) { $ServiceName = [string]$config.serviceName }

$serviceRemoved = $false
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    if ($service.Status -ne 'Stopped') {
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 1
    }
    & sc.exe delete $ServiceName | Out-Null
    $serviceRemoved = $true
}

$task = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($task) {
    Stop-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
}

$shortcutsRemoved = $false
if ($RemoveShortcuts) {
    $paths = @()
    if ($config) {
        if ($config.PSObject.Properties['desktopShortcut']) { $paths += [string]$config.desktopShortcut }
        if ($config.PSObject.Properties['startMenuShortcut']) { $paths += [string]$config.startMenuShortcut }
    }
    foreach ($programs in @([Environment]::GetFolderPath('Programs'), [Environment]::GetFolderPath('CommonPrograms'))) {
        if (-not [string]::IsNullOrWhiteSpace($programs)) {
            $paths += (Join-Path $programs 'Direnix\Direnix Status.lnk')
            $paths += (Join-Path $programs 'Direnix\Direnix Setup Wizard.lnk')
            $paths += (Join-Path $programs 'Direnix\Uninstall Direnix Portal.lnk')
        }
    }
    foreach ($desktop in @([Environment]::GetFolderPath('Desktop'), [Environment]::GetFolderPath('CommonDesktopDirectory'))) {
        if (-not [string]::IsNullOrWhiteSpace($desktop)) {
            $paths += (Join-Path $desktop 'Direnix Portal.url')
        }
    }
    foreach ($path in $paths | Select-Object -Unique) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
            $shortcutsRemoved = $true
        }
    }
}

$configRemoved = $false
if ($RemoveConfig -and (Test-Path -LiteralPath $resolvedConfigPath)) {
    Remove-Item -LiteralPath $resolvedConfigPath -Force -ErrorAction SilentlyContinue
    $configRemoved = $true
}

$dataRemoved = $false
if ($RemoveData -and $config -and $config.PSObject.Properties['profileRoot']) {
    $dataRemoved = Remove-PathSafe -Path ([string]$config.profileRoot) -ExpectedLeaf 'Direnix'
}

$installRemoved = $false
if ($RemoveInstallFiles -and $config -and $config.PSObject.Properties['installRoot']) {
    $installRemoved = Remove-PathSafe -Path ([string]$config.installRoot) -ExpectedLeaf 'Direnix'
}

[pscustomobject]@{
    ok = $true
    serviceRemoved = $serviceRemoved
    taskRemoved = [bool]$task
    shortcutsRemoved = $shortcutsRemoved
    configRemoved = $configRemoved
    dataRemoved = $dataRemoved
    installFilesRemoved = $installRemoved
    configPath = $resolvedConfigPath
} | ConvertTo-Json -Depth 4
