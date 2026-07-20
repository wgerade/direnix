# Direnix

Produto Windows para avaliacao, higiene e governanca de Active Directory.

## Direcao Atual

Direnix nao e mais um pacote de scripts. A base de produto fica em:

```text
product/
```

Componentes ativos:

- `product/src/Direnix.Service`: backend ASP.NET Core hospedado como Windows Service.
- `product/src/Direnix.Service/wwwroot`: portal web local servido pelo proprio service.
- `product/src/Direnix.Core`: contratos de dominio, RBAC, auditoria, storage e coleta.
- `product/src/Direnix.Infrastructure`: SQLCipher, DPAPI e conectividade LDAP/LDAPS.
- `product/installer/Direnix.Msi`: instalador MSI via WiX/MSBuild.

O prototipo PowerShell/HTML foi arquivado como referencia historica em:

```text
legacy/powershell-prototype/
```

Esse legado nao deve ser usado para instalacao, validacao de produto ou entrega em VM.

## Build

Pre-requisitos no host de desenvolvimento:

- .NET SDK 8 x64.
- Fonte NuGet `nuget.org` habilitada.

Build do produto:

```powershell
Set-Location E:\ProjetoAD\product
dotnet restore .\Direnix.Product.sln
dotnet build .\Direnix.Product.sln -c Release /nr:false
dotnet build .\installer\Direnix.Msi\Direnix.Msi.wixproj -c Release /nr:false
```

Artefato principal:

```text
E:\ProjetoAD\product\installer\Direnix.Msi\bin\x64\Release\Direnix.msi
```

## Instalar Em VM

Copie o MSI para a VM e execute em um terminal administrativo:

```powershell
msiexec /i C:\Temp\Direnix.msi /l*v C:\Temp\Direnix-install.log
```

O MSI instala:

- `%ProgramFiles%\Direnix\Direnix.Service.exe`
- `%ProgramFiles%\Direnix\wwwroot\`
- `%ProgramFiles%\Direnix\DirenixPortal.url`
- Windows Service `Direnix.Service`
- `%ProgramData%\Direnix\Product\data`
- `%ProgramData%\Direnix\Product\logs`

## Abrir Portal

Endereco padrao:

```text
http://127.0.0.1:8787/
```

Tambem ha um atalho em:

```text
Start Menu > Direnix > Direnix Portal
```

## Remover Da VM

```powershell
msiexec /x C:\Temp\Direnix.msi /l*v C:\Temp\Direnix-uninstall.log
```

## Limpeza Da Versao Antiga Em VM

Antes de instalar o MSI novo, remova restos do pacote antigo:

```powershell
sc.exe stop DirenixPortal
sc.exe delete DirenixPortal
Unregister-ScheduledTask -TaskName "Direnix Portal" -Confirm:$false -ErrorAction SilentlyContinue
Remove-Item "C:\Program Files\Direnix" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "C:\ProgramData\Direnix" -Recurse -Force -ErrorAction SilentlyContinue
```

## Documentacao Principal

- `docs/TARGET_PRODUCT_ARCHITECTURE.md`
- `docs/PRODUCT_REPLATFORMING_PLAN.md`
- `docs/PRODUCT_STORAGE_SCHEMA.md`
- `docs/INSTALLER_STRATEGY.md`
- `docs/LOCAL_DATA_SECURITY_AND_AUTH.md`
- `docs/COLLECTION_ACCESS_AND_SCOPE.md`
- `docs/DIRENIX_RULES_AND_INDICATORS.md`
- `docs/OPERATIONAL_WORKBENCH_PLAN.md`
- `docs/UX_INFORMATION_ARCHITECTURE.md`
- `docs/DIRENIX_QA_MATRIX.md`

## Estado Atual

Concluido:

- backend .NET compila;
- storage SQLCipher com DPAPI e migration v1;
- `/health/live`, `/health/ready` e `/api/v1/system/about`;
- portal web local em `http://127.0.0.1:8787/`;
- publish self-contained `win-x64`;
- MSI inicial sem PowerShell, com service, portal e atalho.

Pendente antes de release de produto:

- bootstrap do `LocalAdmin`;
- login local;
- RBAC aplicado em endpoints;
- audit log persistente;
- fluxo de primeiro uso autenticado;
- assinatura de codigo;
- testes de instalacao/upgrade/uninstall em VM limpa.
