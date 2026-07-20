# Plano de Implementacao MVP - Direnix

Status: Draft v0.1  
Data: 2026-06-20  
Publico: Produto, Arquitetura, Desenvolvimento, QA

## 1. Decisoes confirmadas para iniciar

- Arquitetura alvo em `docs/TARGET_PRODUCT_ARCHITECTURE.md`.
- Produto instalado como software de mercado, nao como pacote de scripts.
- Windows Service real como backend/API/job engine.
- Frontend web moderno servido pelo backend local.
- Banco local transacional criptografado para historico e timeline.
- Sem agente instalado em Domain Controller.
- Sem banco externo ou servidor de banco obrigatorio na v1 local.
- Autenticacao moderna, login local break-glass e RBAC.
- Export estatico somente saneado ou autorizado.
- Perfil de regras inicial: `MicrosoftDefault`.
- Coleta read-only.
- Remediacao apenas recomendada/WhatIf, com geracao de script revisavel.
- Foco operacional: findings devem virar decisao, export, script, runbook ou aceite formal.
- Coletor principal por biblioteca .NET LDAP/LDAPS, com PowerShell apenas como adaptador opcional.
- Fixtures/mock data antes de AD real.

## 2. Sequencia recomendada

### Fase 1 - Fundacao de produto segura

Objetivo: criar base antes de coletar dados sensiveis.

Entregas:

- Solution .NET do produto.
- Windows Service real.
- API local autenticada.
- Camada de storage por interface.
- Banco local transacional criptografado.
- Schema v1 e migrations.
- Protecao de chave.
- Usuario LocalAdmin bootstrap.
- Login local.
- RBAC minimo.
- Audit log.
- Servidor local bindado em `127.0.0.1`.
- Dashboard shell autenticado com dados mockados.

Gate:

- Servico inicia sem PowerShell.
- Banco nao abre sem chave.
- Dashboard nao abre sem login.
- ExecutiveViewer nao acessa payload tecnico.
- Audit log registra login/logout.

### Fase 2 - Contratos e dados mockados

Objetivo: estabilizar os contratos antes de consultar AD e garantir que cada finding seja acionavel.

Entregas:

- `run-manifest`.
- `preflight`.
- `objects`.
- `findings`.
- `metrics`.
- `evidence`.
- `risk_exceptions`.
- `remediation_plan`.
- `decision_support`.
- `script_artifacts`.
- `export_manifest`.
- Importador de fixtures.
- Comparador de runs.
- Status `New`, `Open`, `Resolved`, `Recurring`, `AcceptedRisk`, `CapabilityMissing`, `NeedsEvidence`, `ReadyForCleanup`, `ScriptGenerated`, `ValidationPending`.

Gate:

- Dois runs mockados geram timeline.
- Dashboard mostra crescimento/reducao/novo/resolvido.
- Score deterministico.
- Finding mockado gera proximo passo operacional.
- Cleanup candidate hibrido sem evidencia cloud fica como `NeedsEvidence`.
- Export CSV mockado tem colunas estaveis e hash no manifesto.

### Fase 3 - Cliente de coleta e preflight

Objetivo: permitir ao engenheiro configurar coleta sem ainda depender de todos os pacotes.

Entregas:

- Tela de alvo AD/DC.
- Validacao LDAPS/TLS/RootDSE.
- Descoberta de naming contexts.
- Browser de OU/CN carregado sob demanda.
- DN manual assistido.
- Config persistida no banco.
- Seletor de tipo de objeto.
- Seletor de pacotes.
- Seletor de profundidade.
- Verificacao de ambiente.
- Registro de capacidades.
- Job queue com progresso/cancelamento.

Gate:

- Falta de permissao vira `capabilityMissing`.
- Credencial AD nao e persistida.
- Escopo selecionado fica registrado no run.
- UI nao trava durante a verificacao.

### Fase 4 - Pacotes MVP de coleta

Objetivo: primeira coleta AD real.

Implementacao:

- usar .NET LDAP/LDAPS como motor principal;
- usar PowerShell apenas para fallback ou ferramentas Microsoft especificas;
- aplicar paginacao, attribute allowlist, timeout, limite de objetos e cancelamento.

Pacotes:

- Inventory.
- Cleanup Hygiene.
- Privileged Access basico.
- SPN/UPN basico.
- Authentication Hardening basico: LDAP signing, MachineAccountQuota, Kerberos flags.
- GPO basico.
- Replication Health quando ferramenta/permissao disponivel.

Gate:

- Coleta em lab AD.
- Runs salvos no banco.
- Findings tecnicos e gerenciais aparecem.
- Dashboard nao vaza dados sensiveis para perfil gerencial.

### Fase 5 - Dashboard operacional

Objetivo: tornar a experiencia util para operacao e gestao.

Entregas:

- Overview.
- Action Center / Operations.
- Identity Risk.
- Cleanup.
- Privileged Access.
- Replication & DCs.
- Group Policy.
- Governance.
- Evidence.
- Painel lateral de finding.
- Painel de decisao com evidencia, runbook, script e validacao.
- Tabelas com ordenacao, filtros, paginacao e export do recorte atual.
- Export CSV tecnico e CSV de owner review.
- Export gerencial saneado.
- Export tecnico autorizado.

Gate:

- SecurityAnalyst investiga finding ate evidencia.
- RiskManager aceita risco com validade.
- Auditor ve historico sem alterar dados.
- ExecutiveViewer ve apenas dashboard saneado.
- CollectorOperator exporta CSV de cleanup candidates.
- AD Engineer gera script PowerShell em modo `WhatIf`/revisao, sem execucao automatica.
- Tabela de findings ordena por severidade, risco, owner, idade e status.

### Fase 6 - Hardening, backup e release

Objetivo: preparar release interna/comercial.

Entregas:

- Backup criptografado.
- Restore validado.
- Rotacao de chave.
- Retencao configuravel.
- Open source notices.
- Release checklist.
- Smoke test completo.

Gate:

- QA matrix P0 completa.
- Nenhum dado sensivel em export gerencial.
- Nenhuma porta externa aberta por padrao.
- Nenhum segredo em texto claro.

## 3. Ordem que nao deve ser invertida

Nao iniciar coleta real sensivel antes de:

1. Banco criptografado.
2. Login local.
3. RBAC minimo.
4. Audit log.
5. Separacao tecnico vs gerencial.

Motivo: depois que dados sensiveis entram no produto, retrofitar seguranca fica caro e arriscado.

## 4. Backlog MVP resumido

| Prioridade | Item | Resultado |
| --- | --- | --- |
| P0 | Storage criptografado | Historico seguro |
| P0 | Auth local | Dashboard protegido |
| P0 | RBAC | Perfis separados |
| P0 | Audit log | Rastreabilidade |
| P0 | Run schema | Contrato de dados |
| P0 | Fixture importer | Desenvolvimento sem AD real |
| P0 | Timeline comparator | Novo/resolvido/recorrente |
| P0 | Preflight | Capacidade e permissao |
| P0 | Scope selector | OU/tipo/pacote/profundidade |
| P0 | Dashboard shell | Experiencia base |
| P0 | Action Center | Findings viram acoes operacionais |
| P0 | Decision support model | Cleanup com confianca e guardrails hibridos |
| P0 | CSV export | Revisao por owner e operacao |
| P0 | Table sorting/pagination | Operacao em grande volume |
| P0 | AD Recycle Bin check | Bloqueio seguro para delete |
| P1 | Inventory collector | Primeiro dado real |
| P1 | Cleanup rules | Primeiros findings |
| P1 | Privileged access basic | Risco de identidade |
| P1 | Script generator WhatIf | Remediacao revisavel |
| P1 | Export gerencial | Entrega para gestao |

## 5. Riscos de implementacao

| Risco | Mitigacao |
| --- | --- |
| SQLCipher/binarios bloqueados por Defender | Validar empacotamento cedo e registrar hash/assinatura |
| Auth local virar so frontend | Autorizacao deve ser no servidor local |
| Export estatico vazar dado sensivel | Gerar pacote gerencial a partir de modelo saneado separado |
| Coleta exigir Domain Admin | Criar niveis de acesso e degradar por capacidade |
| Runs grandes degradarem dashboard | Indexar banco e paginar tabelas |
| Timeline instavel | Usar `stableFindingKey` por regra/objeto/condicao |
| Produto parecer apenas "dedo-duro" | Priorizar Action Center, decisao, script, export e validacao |
| Cleanup local quebrar identidade hibrida | Exigir evidencia hibrida ou owner decision antes de limpeza |
| Delete sem recuperacao adequada | Validar AD Recycle Bin/retencao e preferir disable/quarantine no MVP |

## 6. Definicao de pronto para iniciar desenvolvimento

Pode iniciar quando:

- Decisao de storage esta aceita.
- Modelo de auth/RBAC esta aceito.
- Niveis de acesso de coleta estao aceitos.
- UX aceita dashboard local autenticado.
- QA aceita gates P0.
- Produto aceita `docs/OPERATIONAL_WORKBENCH_PLAN.md` como diretriz para remediacao e decisao.

As decisoes pendentes podem evoluir durante a Fase 1, desde que nao enfraquecam criptografia, RBAC ou separacao de dados.
