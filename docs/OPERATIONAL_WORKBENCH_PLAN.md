# Plano Operacional - Remediacao, Cleanup e Decisao

Status: Draft v0.1  
Data: 2026-06-20  
Publico: Produto, Arquitetura, Desenvolvimento, UX, QA, Operacoes

## 1. Objetivo

Reposicionar a solucao como uma ferramenta operacional de saneamento e remediacao de Active Directory, nao apenas como um painel de apontamento de problemas.

Cada achado deve responder:

- O que foi detectado.
- Quais evidencias sustentam a conclusao.
- Qual e o risco de negocio e tecnico.
- Qual decisao e recomendada.
- Que coleta complementar ajuda a decidir.
- Como corrigir com menor risco.
- Como validar antes/depois.
- Como exportar, auditar ou aceitar o risco.

## 2. Principio de produto

A ferramenta deve trabalhar como um copiloto operacional deterministico:

- Identifica problemas.
- Explica o criterio.
- Mede confianca da recomendacao.
- Sugere proximo passo.
- Gera plano e script seguro.
- Registra decisao e evidencia.
- Nunca executa acao destrutiva automaticamente no MVP.

O tom de UX deve ser: "vamos resolver com controle", nao "voce esta errado".

## 3. Fluxo operacional de um finding

```text
Detectado
  -> Enriquecer evidencia
  -> Classificar confianca
  -> Recomendar decisao
  -> Selecionar acao
  -> Gerar plano/script
  -> Aprovar ou exportar
  -> Executar fora ou em fase futura
  -> Validar depois
  -> Resolver, reabrir ou aceitar risco
```

Estados obrigatorios:

| Estado | Significado |
| --- | --- |
| `Detected` | Regra encontrou uma condicao relevante |
| `NeedsEvidence` | A ferramenta precisa de mais dados para recomendar acao forte |
| `NeedsOwnerDecision` | A decisao depende do dono do sistema/negocio |
| `ReadyForCleanup` | Evidencia suficiente para iniciar limpeza controlada |
| `ReadyForRemediationPlan` | Evidencia suficiente para gerar script ou runbook |
| `ScriptGenerated` | Script seguro foi gerado em modo revisao/WhatIf |
| `ExportedForOwner` | CSV/HTML/JSON enviado para revisao do owner |
| `RiskAccepted` | Risco aceito com justificativa, validade e aprovador |
| `ExecutedExternally` | Acao executada fora da ferramenta |
| `ValidationPending` | Aguardando nova coleta para confirmar resolucao |
| `Resolved` | Proximo run comprovou correcao |

## 4. Bancada operacional

A UX deve incluir uma area chamada `Operations` ou `Action Center`.

Componentes:

- Fila de findings acionaveis.
- Filtro por decisao: limpar, ajustar, implementar, investigar, aceitar risco.
- Filtro por confianca: alta, media, baixa.
- Filtro por impacto: Tier 0, hibrido, GPO critica, OU sensivel.
- Filtro por owner/SLA.
- Acoes em lote para exportar CSV ou gerar pacote de revisao.
- Drawer de detalhe com plano de acao.
- Aba `Evidence`.
- Aba `Decision Support`.
- Aba `Script / Runbook`.
- Aba `Validation`.

## 5. Modelo de decisao para cleanup

Cleanup nao deve ser baseado em um unico atributo. A ferramenta deve gerar uma classificacao de confianca.

### 5.1 Usuario candidato a limpeza

Evidencias locais:

- `enabled`.
- `lastLogonTimestamp`.
- `lastLogon` por DC quando disponivel.
- `pwdLastSet`.
- `whenCreated`.
- `memberOf`.
- `adminCount`.
- grupos privilegiados diretos/indiretos.
- OU e escopo de sincronizacao.
- owner/businessUnit.
- eventos locais quando coletados.

Evidencias hibridas:

- indicio de sincronizacao por Entra Connect.
- OU dentro/fora do escopo de sync.
- atributo sourceAnchor/immutableId quando disponivel.
- UPN/proxy/mail relevantes para identidade hibrida.
- import manual de `lastSignInDateTime` ou relatorio Entra quando fornecido.
- erro de sync local/CSV quando disponivel.

Decisao recomendada:

| Condicao | Decisao |
| --- | --- |
| Sem uso local e sem evidencia cloud | `ReadyForCleanup` apos revisao do owner |
| Sem uso local, mas sincronizado/hibrido sem evidencia cloud | `NeedsEvidence` |
| Privilegiado ou Tier 0 | `NeedsOwnerDecision` ou `Investigate`, nunca limpeza direta |
| Conta de servico suspeita | `Investigate`, exigir evidencia de SPN/log/evento |
| Desabilitado ha mais que retencao definida | `ReadyForCleanup`, se AD Recycle Bin/rollback estiver validado |
| Owner ausente | `NeedsOwnerDecision` e export para responsavel de OU/BU |

### 5.2 Computador candidato a limpeza

Evidencias:

- `lastLogonTimestamp`.
- `pwdLastSet` da conta de computador.
- `operatingSystem`.
- OU/site.
- membership em grupos de deploy/GPO.
- eventos de autenticacao quando disponiveis.
- relacao com servidor, DC, cluster ou equipamento critico.

Guardrails:

- Nunca recomendar exclusao automatica de DC.
- Servidor deve ir para `NeedsOwnerDecision`.
- Workstation pode ir para `ReadyForCleanup` com alta confianca.
- Objeto com BitLocker/LAPS/MDM relacionado exige revisao tecnica.

## 6. Gatilhos operacionais

O produto deve suportar gatilhos configuraveis:

| Gatilho | Resultado esperado |
| --- | --- |
| Novo finding critico | Criar item em Action Center e destacar no overview |
| Finding recorrente por N runs | Aumentar prioridade e SLA |
| Risco aceito vence em X dias | Alertar RiskManager |
| Cleanup candidate com confianca alta | Liberar export CSV para owner |
| Capacidade ausente | Gerar runbook externo de coleta |
| AD Recycle Bin desabilitado | Bloquear plano de exclusao e recomendar habilitacao planejada |
| Hybrid evidence ausente | Marcar decisao como `NeedsEvidence` |
| Script gerado ha mais de X dias | Exigir refresh de evidencia antes da execucao |

## 7. Geracao de scripts

Scripts gerados devem ser artefatos revisaveis, nao execucao direta.

Regras:

- Gerar PowerShell com cabecalho de run, escopo, autor e data.
- Default sempre `-WhatIf` quando o cmdlet suportar.
- Para acoes sem `-WhatIf`, gerar comandos comentados e checklist manual.
- Separar script por categoria: disable, move, remove, ACL, GPO.
- Incluir validacao antes/depois.
- Incluir rollback quando possivel.
- Incluir lista de objetos afetados embutida ou CSV externo com hash.
- Exigir aprovacao antes de mudar estado para `ScriptGenerated`.

Exemplos de scripts planejados:

- Desabilitar usuarios stale apos revisao.
- Mover objetos para OU de quarentena.
- Remover SPN duplicado apos validacao.
- Ajustar `ms-DS-MachineAccountQuota`.
- Gerar comandos para revisar delegation.
- Exportar GPO permissions para revisao.

## 8. Referencias externas quando a ferramenta nao mede

Quando a ferramenta nao consegue coletar uma evidencia, ela deve entregar instrucao tecnica.

Exemplos:

| Lacuna | Referencia operacional |
| --- | --- |
| Sem acesso SIEM | Informar eventos recomendados para busca e janela sugerida |
| Sem Entra import | Informar export esperado de sign-in/activity/report |
| Sem `repadmin` | Informar comando externo e formato de arquivo aceito |
| Sem permissao Event Log | Informar grupos/permissoes necessarias |
| Sem RSAT | Informar pacote/cmdlet opcional, sem tornar dependencia obrigatoria |

Eventos Windows/SIEM inicialmente suportados como referencia:

- 4624: logon bem-sucedido.
- 4625: falha de logon.
- 4648: logon com credencial explicita.
- 4720/4722/4725/4726: lifecycle de contas.
- 4728/4729/4732/4733/4756/4757: membership em grupos.
- 4768: Kerberos TGT solicitado.
- 4769: Kerberos service ticket solicitado.
- 4771: pre-auth Kerberos falhou.
- 4776: validacao NTLM.
- 2887/2888/2889: LDAP signing/channel binding readiness quando habilitado.

## 9. AD Recycle Bin e retencao

Antes de qualquer plano de exclusao, a ferramenta deve validar:

- Se AD Recycle Bin esta habilitado.
- Nivel funcional de floresta/dominio.
- `tombstoneLifetime`.
- `msDS-deletedObjectLifetime` quando presente.
- Politica de quarentena antes de delete.
- Se o objeto pode ser restaurado com atributos suficientes.

Regra de negocio:

- Se AD Recycle Bin estiver desabilitado, a decisao `Remove` deve ser bloqueada no MVP.
- O produto pode recomendar `Disable` ou `MoveToQuarantine`, com export para owner.
- Habilitar AD Recycle Bin deve aparecer como iniciativa planejada, pois e irreversivel.

## 10. Exportes operacionais

Exportes obrigatorios:

- CSV de findings filtrados.
- CSV de cleanup candidates.
- CSV de owner review.
- JSON tecnico completo para automacao.
- HTML/PDF gerencial saneado.
- Pacote de evidencia com manifesto e hashes.
- Script PowerShell gerado.

Requisitos de CSV:

- Colunas estaveis e versionadas.
- Separador configuravel.
- Encoding UTF-8 com BOM para Excel.
- Campos sensiveis omitidos no export gerencial.
- Hash do arquivo no manifesto.

Colunas minimas para cleanup candidates:

- `candidateId`
- `objectType`
- `displayName`
- `samAccountName`
- `domain`
- `ou`
- `enabled`
- `lastLogonTimestamp`
- `lastLogonMax`
- `pwdLastSet`
- `ageDays`
- `hybridIndicator`
- `cloudEvidenceStatus`
- `privilegedIndicator`
- `owner`
- `businessUnit`
- `confidence`
- `recommendedAction`
- `decisionRequiredFrom`
- `riskNotes`
- `sourceRunId`

## 11. Tabelas e ordenacao

Todas as tabelas operacionais devem suportar:

- Ordenacao por coluna.
- Filtro por texto.
- Filtro por severidade, status, owner, categoria, OU e decisao.
- Colunas configuraveis.
- Paginacao.
- Export do recorte atual.
- Indicacao de total filtrado vs total geral.
- Persistencia local da preferencia de colunas.

## 12. UX visual revisada

A interface atual da demo e apenas shell funcional. A proxima iteracao visual deve ser mais marcante:

- Topbar com identidade visual forte e status de run.
- Cards com micrograficos, deltas e acoes diretas.
- Action Center como area principal.
- Cores mais vivas por categoria, mantendo contraste.
- Icones funcionais para export, script, evidence, owner e SLA.
- Empty states com proximo passo, nao texto generico.
- Drawer rico com abas e timeline.
- Tabela densa, com ordenacao visual clara.
- Modo claro e escuro equivalentes, ambos polidos.

Evitar:

- Visual generico de dashboard AI.
- Cards sem acao.
- Score sem caminho para resolver.
- Finding sem botao de export/script/runbook.

## 13. Referencias primarias

- Microsoft Learn: Active Directory Recycle Bin.
- Microsoft Learn: `lastLogonTimestamp`.
- Microsoft Learn: Microsoft Entra Connect sync errors.
- Microsoft Learn: Windows Security Auditing events.
- NIST SP 800-53 Rev. 5: account management and governance controls.

