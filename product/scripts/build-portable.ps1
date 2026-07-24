# Publishes the Direnix portable executable (DirenixPortable.exe): a single-file,
# self-contained win-x64 build that runs without installing a service, writes to
# %LOCALAPPDATA% and opens the portal in the browser. Portable mode is activated by
# the exe name itself.
#
# ASCII-only on purpose: this file is executed by Windows PowerShell 5.1, which reads
# .ps1 files using the ANSI codepage; accented characters there can corrupt parsing.
#
# Usage:  pwsh -File product\scripts\build-portable.ps1 [-Configuration Release]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$productRoot = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $productRoot "src\Direnix.Service\Direnix.Service.csproj"
$out  = Join-Path $productRoot "artifacts\portable"

Write-Host "Publishing Direnix portable (self-contained, single-file, win-x64)..."

# Args as an array (no backtick line-continuation, which breaks on some hosts).
$publishArgs = @(
    "publish", $proj,
    "-c", $Configuration,
    "-r", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "-p:DebugType=none",
    "-o", $out
)
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)." }

$src = Join-Path $out "Direnix.Service.exe"
$dst = Join-Path $out "DirenixPortable.exe"
Copy-Item $src $dst -Force

Write-Host ""
Write-Host "Done: $dst"
Write-Host "Distribute only DirenixPortable.exe -- double-click opens the portal."
