# Storage Criptografado e Schema v1

Status: Implemented v0.1  
Data: 2026-06-22  
Publico: Arquitetura, Desenvolvimento, QA e Seguranca

## 1. Decisao

A nova plataforma em `product/` usa SQLite criptografado por SQLCipher como banco local do produto.

Implementacao:

- provider ADO.NET: `Microsoft.Data.Sqlite.Core` 8.0.22;
- bundle nativo: `SQLitePCLRaw.bundle_e_sqlcipher` 2.1.11;
- protecao da chave: DPAPI `LocalMachine`;
- arquivo do banco: `direnix.adcx`;
- arquivo da chave protegida: `direnix.dbkey.dpapi`;
- WAL habilitado apos abertura criptografada.

## 2. Localizacao

Padrao:

```text
%ProgramData%\Direnix\Product\data
```

Para testes, o caminho pode ser sobrescrito:

```powershell
dotnet .\src\Direnix.Service\bin\Debug\net8.0-windows\Direnix.Service.dll `
  --Direnix:Storage:DataRoot="$env:TEMP\direnix-product-storage"
```

## 3. Schema v1

Tabelas criadas pela migration inicial:

| Tabela | Objetivo |
| --- | --- |
| `schema_migrations` | Controle de versao do schema. |
| `app_settings` | Configuracoes versionadas do produto. |
| `local_users` | Usuarios locais do app. |
| `local_roles` | Roles conhecidas do RBAC. |
| `local_user_roles` | Atribuicao de roles a usuarios. |
| `audit_events` | Auditoria append-oriented da aplicacao. |
| `collection_jobs` | Jobs de coleta e estado operacional. |
| `runs` | Execucoes de coleta/importacao. |
| `findings` | Achados normalizados e chave estavel. |

Roles semeadas:

- `LocalAdmin`
- `CollectorOperator`
- `SecurityAnalyst`
- `RiskManager`
- `Auditor`
- `ExecutiveViewer`
- `ReadOnlyTechnical`

## 4. Health

`/health/ready` abre o banco criptografado, aplica migrations idempotentes e retorna:

- `status`;
- `schemaAvailable`;
- `schemaVersion`;
- `protectionMode`;
- `databasePath`.

## 5. Validacao Executada

Comandos:

```powershell
Set-Location E:\ProjetoAD\product
dotnet restore .\Direnix.Product.sln
dotnet build .\Direnix.Product.sln /nr:false
```

Resultado validado:

- build com 0 warnings e 0 erros;
- `/health/ready` retornou `Ready`;
- `schemaVersion` retornou `1`;
- banco e chave foram criados em `DataRoot` temporario;
- os primeiros 16 bytes do banco nao correspondem a `SQLite format 3`, confirmando que nao e SQLite em claro.

## 6. Proximos Itens

- Bootstrap do usuario `LocalAdmin`.
- Hash de senha aprovado.
- Persistencia de sessoes.
- Escrita real em `audit_events`.
- Backup e restore criptografados.
