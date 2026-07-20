# Direnix Installer

The product installer is MSI-based and uses WiX/MSBuild.

It must not call the legacy PowerShell wizard or the transitional `dist\setup` package.

WiX is pinned to `WixToolset.Sdk` 5.0.2. WiX v7 requires EULA/OSMF acceptance and is not used by this project.

## Build

```powershell
Set-Location E:\ProjetoAD\product
dotnet build .\installer\Direnix.Msi\Direnix.Msi.wixproj -c Release
```

The MSI installs:

- `Direnix.Service.exe` into `%ProgramFiles%\Direnix`;
- `appsettings.json` into `%ProgramFiles%\Direnix`;
- `wwwroot` portal assets into `%ProgramFiles%\Direnix\wwwroot`;
- `DirenixPortal.url` into `%ProgramFiles%\Direnix`;
- Start Menu shortcut `Direnix > Direnix Portal`;
- Windows service `Direnix.Service`;
- product data/log folders under `%ProgramData%\Direnix\Product`.

Default portal URL:

```text
http://127.0.0.1:8787/
```

## Install

Run from an elevated prompt:

```powershell
msiexec /i .\installer\Direnix.Msi\bin\x64\Release\Direnix.msi /l*v C:\Temp\Direnix-install.log
```

## Uninstall

```powershell
msiexec /x .\installer\Direnix.Msi\bin\x64\Release\Direnix.msi /l*v C:\Temp\Direnix-uninstall.log
```
