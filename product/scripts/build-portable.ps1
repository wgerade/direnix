# Publica o executável portátil do Direnix (DirenixPortable.exe): um único arquivo
# self-contained win-x64 que roda sem instalar serviço, grava em %LOCALAPPDATA% e
# abre o portal no navegador. O modo portátil é ativado pelo próprio nome do exe.
#
# Uso:  pwsh -File product\scripts\build-portable.ps1 [-Configuration Release]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$productRoot = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $productRoot "src\Direnix.Service\Direnix.Service.csproj"
$out  = Join-Path $productRoot "artifacts\portable"

Write-Host "Publicando Direnix portátil (self-contained, single-file, win-x64)..."
dotnet publish $proj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -o $out

$src = Join-Path $out "Direnix.Service.exe"
$dst = Join-Path $out "DirenixPortable.exe"
Copy-Item $src $dst -Force

Write-Host ""
Write-Host "Pronto: $dst"
Write-Host "Distribua apenas o DirenixPortable.exe — clique duplo abre o portal."
