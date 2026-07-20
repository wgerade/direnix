# Installer Strategy

Status: Implemented v0.1.3  
Data: 2026-06-22  
Publico: Arquitetura, Desenvolvimento, QA e Operacoes

## Decisao

O instalador profissional do produto deve ser MSI, construido por WiX/MSBuild, instalando o `Direnix.Service` novo como Windows Service.

O pacote transicional em `dist\setup` e o wizard PowerShell antigo nao fazem parte da arquitetura final e nao devem ser usados para validar instalacao de produto.

## Implementacao Inicial

Projeto:

```text
product/installer/Direnix.Msi/Direnix.Msi.wixproj
```

Ferramenta:

- `WixToolset.Sdk` 5.0.2 via NuGet/MSBuild.
- WiX v7 nao foi usado porque exige aceite EULA/OSMF no build.
- Esta decisao deve ser revista antes de uso comercial publico.

Build:

```powershell
Set-Location E:\ProjetoAD\product
dotnet build .\installer\Direnix.Msi\Direnix.Msi.wixproj -c Release
```

Conteudo instalado:

- `%ProgramFiles%\Direnix\Direnix.Service.exe`;
- `%ProgramFiles%\Direnix\wwwroot`;
- `%ProgramFiles%\Direnix\DirenixPortal.url`;
- Start Menu `Direnix > Direnix Portal`;
- Windows Service `Direnix.Service`;
- `%ProgramData%\Direnix\Product\data`;
- `%ProgramData%\Direnix\Product\logs`.

Instalacao em VM:

```powershell
msiexec /i .\installer\Direnix.Msi\bin\x64\Release\Direnix.msi /l*v C:\Temp\Direnix-install.log
```

Portal:

```text
http://127.0.0.1:8787/
```

Remocao:

```powershell
msiexec /x .\installer\Direnix.Msi\bin\x64\Release\Direnix.msi /l*v C:\Temp\Direnix-uninstall.log
```

## Nao Objetivos

- Nao chamar `Direnix.Setup.exe` legado.
- Nao abrir wizard PowerShell.
- Nao criar Scheduled Task.
- Nao instalar a partir de `scripts\Install-DirenixApplication.ps1`.

## Proximos Gaps

- UI final de bootstrap do LocalAdmin.
- Assinatura de codigo.
- Icone e ARP metadata completa.
- Upgrade/migracao de dados antigos.
- ACLs refinadas para `%ProgramData%\Direnix\Product`.
