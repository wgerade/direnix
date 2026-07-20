# Instalacao Em VM

Status: Produto MSI v0.1.3  
Data: 2026-06-22  
Publico: Operacoes, QA, Desenvolvimento

## Decisao

A instalacao profissional de Direnix usa MSI. O caminho antigo baseado em `Direnix.Setup.exe`, wizard PowerShell, `DirenixPortal` e `dist\setup` foi descontinuado.

## Artefato

```text
product/installer/Direnix.Msi/bin/x64/Release/Direnix.msi
```

Build:

```powershell
Set-Location E:\ProjetoAD\product
dotnet build .\Direnix.Product.sln -c Release /nr:false
dotnet build .\installer\Direnix.Msi\Direnix.Msi.wixproj -c Release /nr:false
```

## Instalar

Em um terminal administrativo na VM:

```powershell
msiexec /i C:\Temp\Direnix.msi /l*v C:\Temp\Direnix-install.log
```

O MSI instala:

| Item | Caminho/Nome |
| --- | --- |
| Servico | `Direnix.Service` |
| Binario | `%ProgramFiles%\Direnix\Direnix.Service.exe` |
| Config | `%ProgramFiles%\Direnix\appsettings.json` |
| Portal | `%ProgramFiles%\Direnix\wwwroot` |
| Launcher | `%ProgramFiles%\Direnix\DirenixPortal.url` |
| Menu Iniciar | `Direnix > Direnix Portal` |
| Dados | `%ProgramData%\Direnix\Product\data` |
| Logs | `%ProgramData%\Direnix\Product\logs` |

## Abrir Portal

URL padrao:

```text
http://127.0.0.1:8787/
```

Atalho:

```text
Start Menu > Direnix > Direnix Portal
```

## Verificar

```powershell
Get-Service Direnix.Service
Invoke-RestMethod http://127.0.0.1:8787/health/live
Invoke-RestMethod http://127.0.0.1:8787/health/ready
Invoke-RestMethod http://127.0.0.1:8787/api/v1/system/about
```

O portal deve responder em:

```powershell
Invoke-WebRequest http://127.0.0.1:8787/
```

## Atualizar VM Com MSI 0.1.0

Se a VM ja registrou o pacote `0.1.0` sem portal, instale o MSI `0.1.3` novo:

```powershell
msiexec /i C:\Temp\Direnix.msi /l*v C:\Temp\Direnix-upgrade.log
```

Se o Windows Installer entrar em manutencao/reparo em vez de atualizar, remova o produto antigo registrado no log e instale novamente:

```powershell
msiexec /x {46A3FEA2-BF7D-4C29-8CBF-872E272ACE0F} /l*v C:\Temp\Direnix-remove-0.1.0.log
msiexec /i C:\Temp\Direnix.msi /l*v C:\Temp\Direnix-install-0.1.3.log
```

## Remover

```powershell
msiexec /x C:\Temp\Direnix.msi /l*v C:\Temp\Direnix-uninstall.log
```

## Limpar Versao Antiga

Antes de instalar o MSI novo, remova vestigios do prototipo:

```powershell
sc.exe stop DirenixPortal
sc.exe delete DirenixPortal
Unregister-ScheduledTask -TaskName "Direnix Portal" -Confirm:$false -ErrorAction SilentlyContinue
Remove-Item "C:\Program Files\Direnix" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "C:\ProgramData\Direnix" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "$env:Public\Desktop\Direnix Portal.url" -Force -ErrorAction SilentlyContinue
```

## Fora Do Produto

Nao usar:

- `dist\setup`;
- `Direnix.Setup.exe`;
- `scripts\Install-DirenixApplication.ps1`;
- `scripts\Start-DirenixSetupWizard.ps1`;
- `DirenixPortal`;
- Scheduled Task `Direnix Portal`.

Esses itens pertencem ao prototipo arquivado e nao validam a arquitetura de produto.
