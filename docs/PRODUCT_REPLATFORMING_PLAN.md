# Plano de Replataforma do Produto

Status: Started v0.2  
Data: 2026-06-22  
Publico: Produto, Arquitetura, Desenvolvimento, QA e Seguranca

## 1. Objetivo

Transformar Direnix de prototipo funcional baseado em PowerShell para software instalado de mercado.

O prototipo atual continua util para demonstracao controlada, migracao, regras e validacao de UX. Ele nao deve mais receber investimento como runtime final do produto.

## 2. Estrutura Nova

O novo codigo de produto fica em:

```text
product/
```

Projetos iniciais:

| Projeto | Responsabilidade |
| --- | --- |
| `Direnix.Core` | Contratos de identidade, RBAC, auditoria, storage e coleta. |
| `Direnix.Infrastructure` | Implementacoes Windows-native, DPAPI e LDAP/LDAPS. |
| `Direnix.Service` | Host ASP.NET Core planejado para Windows Service, API local, portal web e health. |

## 3. Decisoes de Implementacao Inicial

- Target inicial: `.NET 8` porque a VM atual possui runtime .NET 8 instalado.
- Caminho futuro: avaliar upgrade para `.NET 10 LTS` quando o SDK e empacotamento final forem escolhidos.
- Backend: ASP.NET Core/Kestrel como processo principal.
- Service hosting: `UseWindowsService`.
- Bind padrao: `127.0.0.1`.
- Coleta AD: `System.DirectoryServices.Protocols`.
- Banco: SQLite criptografado por SQLCipher ou provider equivalente.
- Protecao da chave: DPAPI `LocalMachine` com ACL de pasta na instalacao.
- PowerShell: somente legado, diagnostico, fallback ou adaptador isolado.

## 4. Estado Entregue Nesta Primeira Fatia

- Solution nova em `product/Direnix.Product.sln`.
- Contratos centrais para roles, auditoria, storage e probe AD.
- Infraestrutura inicial para chave de banco protegida por DPAPI.
- Probe RootDSE por LDAP/LDAPS usando API .NET.
- Storage SQLCipher real com migrations v1.
- MSI inicial sem PowerShell em `product/installer/Direnix.Msi`.
- Portal web local servido por `Direnix.Service` em `http://127.0.0.1:8787/`.
- Servico ASP.NET Core com endpoints:
  - `/health/live`;
  - `/health/ready`;
  - `/api/v1/system/about`.

## 5. Gates da Fase 1

Antes de coletar qualquer dado sensivel real pela plataforma nova:

1. SDK .NET instalado e build verde. Concluido.
2. Banco SQLite/SQLCipher real com migrations. Concluido para schema v1.
3. Bootstrap do LocalAdmin.
4. Login local.
5. RBAC aplicado no servidor.
6. Audit log persistente.
7. Portal shell local. Concluido sem auth; bootstrap/login ainda pendentes.
8. Separacao de payload tecnico e gerencial.

## 6. Proximas Entregas

1. Implementar bootstrap de usuario LocalAdmin.
2. Implementar hash PBKDF2-HMACSHA256 ou Argon2id aprovado.
3. Implementar cookie auth local, lockout e sessao.
4. Persistir audit log no banco.
5. Bloquear endpoints sensiveis atras de RBAC.
6. Evoluir portal local para frontend autenticado.
7. Criar job queue persistida para coleta.
8. Expor probe LDAPS via API somente apos auth/RBAC.

## 7. Stop Rule

Qualquer instalador ou servico que apenas chame PowerShell continua classificado como prototipo. Release de produto exige que `Direnix.Service` seja o processo principal, hospede a API, proteja storage, aplique auth/RBAC e controle jobs sem depender de `Start-DirenixDemo.ps1`.
