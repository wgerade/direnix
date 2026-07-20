# Plataforma Local de Dados, Seguranca e Autenticacao

Status: Draft v0.1  
Data: 2026-06-20  
Publico: Arquitetura, Desenvolvimento, Seguranca, QA, Operacoes

## 1. Objetivo

Definir a arquitetura local de persistencia, criptografia, autenticacao, autorizacao, auditoria e exposicao do dashboard para a solucao Direnix.

Este documento substitui a premissa anterior de que o produto poderia operar apenas com artefatos JSON/HTML estaticos. O produto ainda deve exportar relatorios estaticos saneados, mas a experiencia principal deve usar um banco local criptografado e um servidor local autenticado para preservar historico, timeline, comparativos e dados sensiveis.

## 2. Decisao arquitetural recomendada

### 2.1 Decisao

Usar um banco local, embarcado, em arquivo unico, baseado em SQLite criptografado via SQLCipher Community Edition ou distribuicao compativel licenciada corretamente.

Nome conceitual do arquivo:

```text
data/direnix.adcx
```

Extensoes auxiliares:

```text
data/direnix.adcx-wal
data/direnix.adcx-shm
```

### 2.2 Motivos

- SQLite e maduro, rapido, embarcado, amplamente usado e adequado para grandes volumes locais.
- SQLite e public domain e pode ser usado, vendido e redistribuido comercialmente.
- SQLCipher Community Edition permite uso em software open source e closed source commercial, desde que as obrigacoes de licenca BSD-style e atribuicoes sejam cumpridas.
- O modelo em arquivo unico facilita backup, portabilidade controlada e timeline local.
- SQL relacional facilita comparativos: novo, resolvido, recorrente, cresceu, diminuiu, tendencia, baseline e historico.
- Nao exige servico de banco, instalacao de servidor ou componente central.

### 2.3 Licenciamento e distribuicao

Requisitos de produto:

- Incluir `NOTICE` ou secao de licencas com SQLCipher, SQLite e dependencias criptograficas.
- Nao usar a marca SQLCipher como nome principal do produto.
- Registrar versao e hash dos binarios embarcados.
- Permitir modo sem banco criptografado apenas para desenvolvimento/fixture, nunca como padrao de producao.

### 2.4 Alternativas avaliadas

| Opcao | Licenca/comercial | Criptografia | Decisao |
| --- | --- | --- | --- |
| SQLite puro | Public domain, excelente para comercial | Nao possui criptografia nativa no core | Bom motor base, insuficiente sozinho para dados sensiveis |
| SQLCipher Community | BSD-style, pode ser usado em closed source commercial com atribuicao | Criptografia de banco SQLite | Recomendado |
| DuckDB | MIT, excelente para analytics local | Criptografia recente; documentacao informa que ainda nao atende requisitos oficiais NIST | Futuro opcional para analytics, nao storage sensivel primario no MVP |
| LiteDB | MIT, simples, .NET single DLL | Documentacao cita AES com ECB, o que nao e aceitavel como padrao para dados sensiveis | Nao recomendado como storage primario |
| SQL Server Express/LocalDB | Gratis, familiar Microsoft | Pode suportar controles fortes | Exige instalacao/componente maior, fora da proposta local portavel |
| Arquivos JSONL criptografados | Sem dependencia de banco | Pode usar DPAPI/EFS | Bom fallback/export, fraco para consultas historicas volumosas |

## 3. Modelo de armazenamento

### 3.1 Tipos de dados

| Tipo | Onde fica | Observacao |
| --- | --- | --- |
| Configuracao do produto | Banco + arquivos `config/*.json` | Config versionada |
| Regras e thresholds | Banco + `config/business-rules.example.json` | Copiar snapshot para cada run |
| Runs | Banco | Timeline principal |
| Findings | Banco | Estado atual e historico |
| Evidencias | Banco + arquivos protegidos | Evidencia pesada pode ficar em arquivo com hash |
| Objetos normalizados | Banco | Indexado por SID/DN/GUID |
| Dados brutos | Arquivo protegido ou tabela particionada | Redigir quando aplicavel |
| Relatorios estaticos | `output/reports` | Gerencial saneado por padrao |
| Auditoria local | Banco | Imutavel por append |
| Usuarios locais | Banco | Hash de senha, roles, status |
| Segredos/chave DB | Windows DPAPI + ACL | Nunca texto claro |

### 3.2 Tabelas conceituais

```text
app_settings
local_users
local_roles
local_user_roles
auth_sessions
audit_log
rule_catalog
policy_profiles
runs
run_capabilities
collection_scopes
objects
object_snapshots
findings
finding_history
finding_evidence
metrics
metric_history
risk_exceptions
evidence_files
reports
sync_imports
```

### 3.3 Indices obrigatorios

- `runs.startedAt`
- `runs.runId`
- `objects.objectSid`
- `objects.objectGuid`
- `objects.distinguishedNameHash`
- `findings.ruleId`
- `findings.status`
- `findings.severity`
- `findings.businessRiskScore`
- `findings.firstSeen`
- `findings.lastSeen`
- `metrics.metricKey`
- `metrics.runId`
- `audit_log.timestamp`
- `risk_exceptions.expiryDate`

## 4. Historico e comparativos

O banco deve permitir responder:

- O que e novo neste run?
- O que foi resolvido?
- O que voltou a aparecer?
- O que cresceu?
- O que diminuiu?
- Qual regra piorou?
- Qual area melhorou?
- Qual OU/unidade acumulou mais risco?
- Qual risco aceito venceu?
- Qual controle foi implementado?

### 4.1 Estados historicos do finding

| Estado | Definicao |
| --- | --- |
| `New` | Primeiro run em que aparece |
| `Open` | Continua presente |
| `Resolved` | Ausente no run atual apos ter existido |
| `Recurring` | Resolveu e voltou |
| `AcceptedRisk` | Aceito formalmente |
| `ExpiredRisk` | Aceite venceu |
| `FalsePositive` | Marcado como falso positivo com justificativa |
| `NotMeasured` | Regra nao medida no run atual |
| `CapabilityMissing` | Ferramenta/permissao ausente |

### 4.2 Chave estavel de finding

Cada finding deve ter uma chave estavel para comparacao:

```text
stableFindingKey = hash(ruleId + normalizedDomain + normalizedObjectIdentity + normalizedCondition)
```

Para objetos AD, a preferencia de identidade e:

1. SID.
2. ObjectGUID.
3. DistinguishedName normalizado.
4. Hash de atributos compostos quando objeto nao possui SID/GUID.

## 5. Criptografia e protecao de segredo

### 5.1 Criptografia do banco

Requisitos:

- Banco deve ser criptografado por padrao.
- Usar chave forte aleatoria de pelo menos 256 bits.
- Nao armazenar chave em texto claro.
- Suportar rotacao de chave.
- Suportar backup/export protegido por passphrase.

### 5.2 Protecao da chave

Modos:

| Modo | Uso | Protecao |
| --- | --- | --- |
| `CurrentUser` | Estacao individual | DPAPI CurrentUser |
| `LocalMachineAcl` | Workstation administrativa compartilhada | DPAPI LocalMachine + ACL restrita |
| `PortablePassphrase` | Arquivo transportavel | Passphrase informada pelo usuario, nao persistida |

Modo recomendado para MVP:

```text
LocalMachineAcl
```

ACL minima:

- Administrators: Full Control.
- Direnix Service/Operator local group: Modify.
- Usuarios comuns: sem acesso ao diretorio `data`.

### 5.3 Camadas adicionais

Recomendadas:

- BitLocker no volume.
- EFS opcional para pasta `data`.
- ACL restrita em `data`, `output`, `logs` e `backups`.
- Hash SHA-256 para evidencias exportadas.
- Redacao de dados sensiveis nos relatorios gerenciais.

## 6. Autenticacao local

### 6.1 Por que servidor local e obrigatorio para modo seguro

HTML estatico em `file://` nao consegue proteger dados sensiveis de forma confiavel. Qualquer dado entregue ao navegador pode ser aberto fora da interface.

Portanto:

- Modo seguro: dashboard servido por app local em `http://127.0.0.1:<porta>` com login e sessao.
- Modo estatico: somente relatorio saneado, sem dados sensiveis.

### 6.2 Servidor local

Requisitos:

- Bind padrao apenas em `127.0.0.1`.
- Nao abrir porta de rede por padrao.
- Porta configuravel.
- Token de sessao HttpOnly quando aplicavel.
- Timeout de sessao.
- Logout.
- Bloqueio por tentativas falhas.
- Auditoria de login, logout e exportacao.

### 6.3 Usuarios locais

No MVP, usuarios do app nao precisam integrar com AD.

Fluxo:

1. Primeiro uso cria usuario `LocalAdmin`.
2. Senha criada no bootstrap.
3. Senha armazenada como hash forte com salt.
4. Roles atribuiveis localmente.
5. Recuperacao exige acesso administrativo local ao host e procedimento documentado.

Hash de senha:

- Preferir Argon2id se biblioteca aprovada puder ser embarcada.
- Fallback sem dependencia externa: PBKDF2-HMACSHA256 com salt unico por usuario e iteracoes altas configuraveis.

### 6.4 Politicas de senha e sessao

Defaults:

- Minimo 14 caracteres.
- Bloquear senhas triviais conhecidas por lista local simples.
- 5 tentativas falhas bloqueiam por 15 minutos.
- Sessao expira por inatividade em 30 minutos.
- Sessao absoluta expira em 8 horas.
- Login requerido apos reiniciar app.

## 7. Autorizacao e perfis

### 7.1 Roles propostas

| Role | Objetivo |
| --- | --- |
| `LocalAdmin` | Gerencia app, usuarios, roles, banco, backup, chaves e configuracoes |
| `CollectorOperator` | Configura e executa coletas |
| `SecurityAnalyst` | Ve achados tecnicos sensiveis, evidencia e riscos |
| `RiskManager` | Gerencia aceite de risco, excecoes, SLA e decisoes |
| `Auditor` | Le evidencias, historico e relatorios, sem alterar dados |
| `ExecutiveViewer` | Ve dashboard gerencial saneado |
| `ReadOnlyTechnical` | Ve achados tecnicos sem executar coleta ou alterar risco |

### 7.2 Matriz RBAC

| Acao | LocalAdmin | CollectorOperator | SecurityAnalyst | RiskManager | Auditor | ExecutiveViewer | ReadOnlyTechnical |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Configurar usuarios | Sim | Nao | Nao | Nao | Nao | Nao | Nao |
| Configurar banco/chaves | Sim | Nao | Nao | Nao | Nao | Nao | Nao |
| Configurar escopo de coleta | Sim | Sim | Nao | Nao | Nao | Nao | Nao |
| Executar coleta | Sim | Sim | Nao | Nao | Nao | Nao | Nao |
| Ver dashboard tecnico sensivel | Sim | Sim | Sim | Parcial | Sim | Nao | Sim |
| Ver dashboard gerencial | Sim | Sim | Sim | Sim | Sim | Sim | Sim |
| Ver evidencias brutas | Sim | Parcial | Sim | Nao | Sim | Nao | Parcial |
| Aceitar risco | Sim | Nao | Recomenda | Sim | Nao | Nao | Nao |
| Exportar relatorio tecnico | Sim | Sim | Sim | Nao | Sim | Nao | Sim |
| Exportar relatorio gerencial | Sim | Sim | Sim | Sim | Sim | Sim | Sim |
| Apagar runs | Sim | Nao | Nao | Nao | Nao | Nao | Nao |

### 7.3 Separacao de deveres

- Quem coleta nao deve automaticamente aprovar risco.
- Quem aceita risco precisa justificar e ter papel de RiskManager ou LocalAdmin.
- Auditor nao altera estado.
- ExecutiveViewer nunca acessa dados sensiveis brutos.

## 8. Seguranca de rede

### 8.1 Padrao seguro

- Dashboard bindado somente em loopback.
- Sem listener externo.
- Sem dependencia de internet.
- Sem CDN.
- Sem telemetria externa.
- Sem update automatico externo no MVP.

### 8.2 Modo rede opcional

Modo rede deve ser futuro ou explicitamente habilitado.

Se existir:

- Exigir TLS.
- Exigir certificado configurado.
- Exigir allowlist de IP.
- Exigir regra de firewall documentada.
- Exigir logs de acesso.
- Nunca habilitar por padrao.

### 8.3 Bloqueios esperados

O produto deve funcionar em ambientes com:

- Sem internet.
- Proxy bloqueado.
- Defender/Application Control restritivo.
- Sem permissoes para abrir porta externa.
- Sem RSAT completo.
- Sem modulo ActiveDirectory.

## 9. Auditoria

Registrar:

- Login/logout.
- Falha de login.
- Criacao/alteracao de usuario.
- Alteracao de role.
- Alteracao de configuracao.
- Inicio/fim de coleta.
- Mudanca de escopo.
- Exportacao.
- Visualizacao de evidencia sensivel.
- Criacao/alteracao/expiracao de aceite de risco.
- Backup/restore.
- Rotacao de chave.

Campos minimos:

- `timestamp`
- `actorUserId`
- `actorRole`
- `action`
- `targetType`
- `targetId`
- `sourceIp`
- `hostName`
- `result`
- `detailsHash`

## 10. Backup, restore e retencao

### 10.1 Backup

Requisitos:

- Backup manual pelo LocalAdmin.
- Backup criptografado.
- Manifesto com versao, data, hash e tamanho.
- Opcao de incluir ou excluir evidencias brutas.

### 10.2 Restore

Requisitos:

- Validar hash.
- Validar versao de schema.
- Exigir LocalAdmin.
- Registrar em audit log.
- Preservar banco anterior ate confirmacao.

### 10.3 Retencao

Defaults:

- Runs completos: 24 meses.
- Evidencias brutas: 12 meses.
- Relatorios gerenciais: 36 meses.
- Audit log: 36 meses.
- Excecoes: 36 meses apos expiracao.

Todos os valores devem ser configuraveis.

## 11. Requisitos para desenvolvimento

1. Implementar camada de storage por interface para permitir trocar engine no futuro.
2. Versionar schema do banco.
3. Criar migrations idempotentes.
4. Nunca abrir banco sem criptografia no modo producao.
5. Nunca persistir senha de usuario nem credencial AD.
6. Redigir dados sensiveis antes de gerar export gerencial.
7. Garantir que WAL/SHM/temp tambem estejam protegidos.
8. Criar audit log append-only.
9. Implementar RBAC antes de expor dados sensiveis no dashboard.
10. Bloquear bind de rede externa por padrao.

## 12. Requisitos para QA

QA deve validar:

- Banco criado criptografado por padrao.
- App nao abre banco sem chave valida.
- Chave nao existe em texto claro.
- WAL/SHM/temp nao expoem texto sensivel em modo normal.
- Login obrigatorio para dashboard seguro.
- ExecutiveViewer nao acessa dados tecnicos sensiveis.
- Auditor nao altera dados.
- RiskManager consegue aceitar risco com campos obrigatorios.
- RiskManager nao aceita risco sem validade.
- CollectorOperator nao gerencia usuarios.
- Servidor local escuta somente `127.0.0.1` por padrao.
- Export estatico gerencial nao contem SID, DN completo ou nome de conta privilegiada por padrao.
- Backup e restore preservam historico.

## 13. Decisoes pendentes

| Decisao | Opcoes | Recomendacao |
| --- | --- | --- |
| Engine final | SQLCipher Community, DuckDB encrypted, outro | SQLCipher Community |
| Hash de senha | Argon2id, PBKDF2-HMACSHA256 | PBKDF2 no MVP se evitar dependencia; Argon2id se lib aprovada |
| Modo rede | Nunca, futuro opcional, MVP opcional | Futuro opcional |
| Multiusuario simultaneo | Sim, limitado, nao | Limitado local no MVP |
| Backup portavel | Passphrase, DPAPI machine-bound | Ambos, com passphrase para portavel |

## 14. Referencias

- SQLite copyright/public domain: https://sqlite.org/copyright.html
- SQLCipher Community Edition: https://www.zetetic.net/sqlcipher/community/
- DuckDB encryption: https://duckdb.org/2025/11/19/encryption-in-duckdb
- LiteDB encryption: https://www.litedb.org/docs/encryption/
