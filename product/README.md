# Direnix Product Platform

This folder is the new product-grade platform for Direnix.

The legacy PowerShell/vanilla portal remains in the repository as a prototype, migration aid, and fallback evidence adapter. New product code should be added here.

## Current Baseline

- `Direnix.Service`: ASP.NET Core host intended to run as a Windows Service.
- `Direnix.Service/wwwroot`: local web portal served by the Windows Service.
- `Direnix.Core`: product contracts for identity, audit, storage, and AD collection.
- `Direnix.Infrastructure`: Windows-native infrastructure such as DPAPI key protection and LDAP RootDSE probing.

## Runtime Principles

- Default bind address is `127.0.0.1`.
- No domain controller installation is assumed.
- AD credentials are request-scoped and must not be persisted.
- LDAP collection must use LDAPS by default.
- PowerShell is allowed only as an isolated adapter for specific Microsoft tools or migration flows.

## Build

This workspace uses the .NET 8 SDK and NuGet packages from `nuget.org`.

```powershell
Set-Location E:\ProjetoAD\product
dotnet restore .\Direnix.Product.sln
dotnet build .\Direnix.Product.sln
```

## MSI

The product installer is WiX/MSI based and does not use the legacy PowerShell setup wizard.

```powershell
dotnet build .\installer\Direnix.Msi\Direnix.Msi.wixproj -c Release
```

The installed portal opens at:

```text
http://127.0.0.1:8787/
```

## Storage Smoke Test

The service accepts a temporary data root for validation:

```powershell
dotnet .\src\Direnix.Service\bin\Debug\net8.0-windows\Direnix.Service.dll `
  --Direnix:Port=8789 `
  --Direnix:Storage:DataRoot="$env:TEMP\direnix-product-storage"
```

Calling `/health/ready` creates an encrypted SQLCipher database and applies schema v1.
