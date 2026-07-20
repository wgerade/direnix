# Matriz de QA - Direnix e Identity Hygiene

Status: Draft v0.1  
Data: 2026-06-20  
Escopo: Validacao funcional, tecnica, seguranca, UX e release readiness.

## 1. Objetivo de QA

Garantir que a solucao colete, analise, reporte e apresente dados de Active Directory com confiabilidade, sem exigir componentes adicionais obrigatorios, sem executar mudancas destrutivas por padrao e sem expor dados sensiveis na visao gerencial.

A validacao tambem deve garantir que o banco local criptografado, autenticacao local, RBAC, historico evolutivo, seletores de escopo e dashboard seguro funcionem antes de qualquer release com dados sensiveis.

## 2. Ambientes de teste

### 2.1 Ambiente unitario com fixtures

- Sem AD real.
- Arquivos JSON/CSV/XML simulando objetos, GPOs, ACLs, eventos e saidas de ferramentas Microsoft.
- Usado para validar regras, scoring, parsing, sanitizacao e dashboard.

### 2.2 Lab AD minimo

- Uma floresta.
- Um dominio.
- Dois domain controllers.
- Uma OU de teste.
- Usuarios, grupos, computadores e GPOs sinteticos.
- Pelo menos uma falha/risco controlado por categoria principal.

### 2.3 Lab AD estendido

- Multi-site.
- Trust de teste.
- Grupo grande para validar range retrieval.
- Objetos stale e orfaos.
- GPOs conflitantes.
- Conta de servico com SPN.
- Azure AD Connect apenas se ja existir no ambiente de teste.

## 3. Estrategia de teste

- Unit tests para regras deterministicas.
- Contract tests para JSON gerado.
- Integration tests em lab AD.
- UX checks manuais e automatizaveis no dashboard.
- Security tests para credenciais, sanitizacao e permissoes.
- Storage tests para banco criptografado, schema, migrations, backup e restore.
- Auth/RBAC tests para perfis locais e protecao das paginas.
- Regression tests com fixtures versionadas.
- Smoke test de release em maquina sem RSAT completo.

## 4. Matriz funcional

| ID | Area | Cenario | Resultado esperado | Prioridade |
| --- | --- | --- | --- | --- |
| QA-F-001 | Preflight | Executar em host sem ferramentas opcionais | Produto registra capacidades ausentes e continua pacotes independentes | P0 |
| QA-F-002 | Preflight | Executar com usuario sem privilegio elevado | Produto informa limitacoes e nao tenta acao destrutiva | P0 |
| QA-F-003 | Cliente | Criar configuracao por CLI | Arquivo JSON valido e reutilizavel e gerado | P0 |
| QA-F-004 | Cliente | Criar configuracao por GUI | Configuracao equivalente ao modo CLI e gerada | P1 |
| QA-F-005 | Cliente | Selecionar somente uma OU | Coleta respeita escopo configurado | P0 |
| QA-F-006 | Cliente | Interromper pacote com erro | Run continua demais pacotes e registra erro contextual | P0 |
| QA-F-007 | Inventario | Coletar dominios, floresta, DCs, FSMO e sites | Dados aparecem em `normalized` e dashboard | P0 |
| QA-F-008 | Cleanup | Usuario stale sintetico | Finding de usuario inativo com evidencia e recomendacao | P0 |
| QA-F-009 | Cleanup | Computador stale sintetico | Finding de computador inativo com evidencia e recomendacao | P0 |
| QA-F-010 | Cleanup | OU vazia | Finding de OU candidata a revisao | P1 |
| QA-F-011 | Cleanup | SID orfao em ACL fixture | Finding com objeto, ACL afetada e evidencia | P0 |
| QA-F-012 | SPN/UPN | SPN duplicado | Finding High/Critical conforme regra e objetos afetados | P0 |
| QA-F-013 | SPN/UPN | UPN duplicado ou invalido | Finding com impacto em hybrid identity | P0 |
| QA-F-014 | Service Account | Conta de servico com senha sem expiracao | Finding e recomendacao de gMSA/rotacao | P0 |
| QA-F-015 | Privilege | Membro aninhado em grupo privilegiado | Caminho de membership aparece no finding | P0 |
| QA-F-016 | Privilege | Grupo builtin localizado/renomeado | Resolucao por SID conhecido funciona | P0 |
| QA-F-017 | ACL | Permissao equivalente a DC Sync | Finding critico com evidencia | P0 |
| QA-F-018 | Delegation | Unconstrained delegation | Finding High/Critical conforme escopo | P0 |
| QA-F-019 | GPO | GPO sem link | Finding Medium/Low e evidencia | P1 |
| QA-F-020 | GPO | GPO com security filtering arriscado | Finding com alvo e recomendacao | P1 |
| QA-F-021 | Replication | `repadmin` disponivel com erro simulado | Finding de replication health e evidencia bruta | P0 |
| QA-F-022 | Replication | `repadmin` ausente | Warning de capacidade indisponivel, sem crash | P0 |
| QA-F-023 | SYSVOL | Evento DFSR critico em fixture | Finding com severidade correta | P0 |
| QA-F-024 | Trust | Trust obsoleto em fixture | Finding com recomendacao de revisao | P1 |
| QA-F-025 | Hybrid | Atributo proxy duplicado | Finding de conflito hibrido | P0 |
| QA-F-026 | Hybrid | Sem Azure AD Connect local | Indicador aparece como nao disponivel, nao como aprovado | P0 |
| QA-F-027 | SIEM | Logs locais sem auditoria esperada | Finding de logging readiness | P1 |
| QA-F-028 | Governance | Owner ausente em OU/GPO | Finding de governanca | P2 |
| QA-F-029 | Score | Mesmo input duas vezes | Score identico e deterministico | P0 |
| QA-F-030 | Historico | Run anterior com finding resolvido | Dashboard mostra resolvido e tendencia | P1 |
| QA-F-031 | Regras | Executar perfil MicrosoftDefault | Thresholds default aparecem no run e nos findings | P0 |
| QA-F-032 | Regras | Executar perfil CISStrict | Thresholds mais restritivos sao aplicados sem alterar codigo | P1 |
| QA-F-033 | Regras | Capacidade ausente para uma regra | Resultado fica `capabilityMissing`, nao `compliant` | P0 |
| QA-F-034 | Regras | Finding com benchmark associado | Detalhe tecnico mostra benchmark e threshold usado | P0 |
| QA-F-035 | Decisao | Regra de cleanup stale | Decisao recomendada e `CleanUp` | P0 |
| QA-F-036 | Decisao | Regra LDAP signing ausente | Decisao recomendada e `Implement` | P0 |
| QA-F-037 | Decisao | Regra DCSync indevido | Decisao recomendada e `Adjust` com severidade Critical | P0 |
| QA-F-038 | Indicadores | Multiplicador de Tier 0 | Business risk score aumenta conforme regra documentada | P0 |
| QA-F-039 | Historico | Finding novo no run atual | Dashboard marca como `New` | P0 |
| QA-F-040 | Historico | Finding ausente depois de existir | Dashboard marca como `Resolved` | P0 |
| QA-F-041 | Historico | Finding resolvido volta a aparecer | Dashboard marca como `Recurring` | P1 |
| QA-F-042 | Historico | Metrica aumentou entre runs | Dashboard mostra crescimento | P0 |
| QA-F-043 | Historico | Metrica diminuiu entre runs | Dashboard mostra reducao | P0 |
| QA-F-044 | Coleta | Selecionar tipos de objeto | Somente tipos selecionados sao coletados | P0 |
| QA-F-045 | Coleta | Selecionar pacote de feature | Somente pacotes selecionados executam regras relacionadas | P0 |
| QA-F-046 | Coleta | Profundidade Quick | Coleta usa conjunto reduzido e registra profundidade | P1 |
| QA-F-047 | Operacional | Finding P0/P1 | Proximo passo operacional presente: evidencia, export, script, runbook, aceite ou validacao | P0 |
| QA-F-048 | Cleanup hibrido | Usuario stale local com indicio hibrido e sem evidencia cloud | Estado `NeedsEvidence`, sem recomendacao de delete | P0 |
| QA-F-049 | AD Recycle Bin | Recycle Bin desabilitado | Plano `Remove` bloqueado; recomendacao de disable/quarantine | P0 |
| QA-F-050 | Script generation | Cleanup candidate aprovado | Script PowerShell gerado em modo revisao/WhatIf, sem execucao | P0 |
| QA-F-051 | External evidence | Import CSV/JSON de SIEM/Entra | Evidencia anexada ao run e usada para atualizar confianca | P1 |
| QA-F-047 | Coleta | Profundidade Deep | Coleta inclui ACL/eventos/grupos grandes quando permitido | P1 |

## 5. Matriz de seguranca

| ID | Cenario | Resultado esperado | Prioridade |
| --- | --- | --- | --- |
| QA-S-001 | Informar credencial no cliente | Senha nao aparece em logs, JSON, memoria persistida ou dashboard | P0 |
| QA-S-002 | Gerar pacote gerencial | DNs, SIDs e nomes sensiveis sao saneados por padrao | P0 |
| QA-S-003 | Gerar hash de evidencias | Arquivos principais possuem SHA-256 registrado | P1 |
| QA-S-004 | Executar recomendacao destrutiva | Produto nao executa por padrao e exige aprovacao/WhatIf | P0 |
| QA-S-005 | Abrir dashboard tecnico | Dados sensiveis aparecem apenas no pacote tecnico | P0 |
| QA-S-006 | Pasta de output sem permissao | Produto falha com erro claro e nao perde dados parciais | P1 |
| QA-S-007 | Arquivo de excecao vencida | Finding volta para aberto ou vencido | P1 |
| QA-S-008 | Relatorio com dados importados manualmente | Origem fica marcada como importada/manual | P1 |
| QA-S-009 | Aceite de risco incompleto | Produto rejeita `AcceptRisk` sem owner, justificativa, escopo, validade e aprovador | P0 |
| QA-S-010 | Aceite de risco vencido | Dashboard gerencial mostra risco aceito vencido como pendencia | P0 |
| QA-S-011 | Banco local | Banco e criado criptografado por padrao | P0 |
| QA-S-012 | Banco local | Chave ausente/incorreta | App nao abre dados sensiveis | P0 |
| QA-S-013 | Banco local | Chave armazenada | Nenhuma chave aparece em texto claro em disco | P0 |
| QA-S-014 | Banco local | WAL/SHM/temp | Arquivos auxiliares nao expoem texto sensivel em modo normal | P0 |
| QA-S-015 | Auth | Abrir dashboard seguro sem login | Acesso bloqueado | P0 |
| QA-S-016 | Auth | Login invalido repetido | Conta/sessao bloqueada conforme politica | P1 |
| QA-S-017 | RBAC | ExecutiveViewer tenta abrir evidencia bruta | Acesso negado | P0 |
| QA-S-018 | RBAC | Auditor tenta alterar risco | Acesso negado | P0 |
| QA-S-019 | RBAC | CollectorOperator tenta gerenciar usuarios | Acesso negado | P0 |
| QA-S-020 | Rede | Servidor local inicia | Bind somente em `127.0.0.1` por padrao | P0 |
| QA-S-021 | Export | Export gerencial estatico | Nao contem SID, DN completo ou conta privilegiada por padrao | P0 |
| QA-S-022 | Auditoria | Login/coleta/export/aceite | Eventos aparecem no audit log | P0 |
| QA-S-023 | Script safety | Script gerado | Nao executa automaticamente e inclui cabecalho, escopo, runId e validacao | P0 |
| QA-S-024 | Delete guardrail | Plano de exclusao | Exige AD Recycle Bin/retencao conhecida ou bloqueia `Remove` | P0 |

## 6. Matriz de UX

| ID | Cenario | Resultado esperado | Prioridade |
| --- | --- | --- | --- |
| QA-U-001 | Primeiro uso do cliente | Engenheiro entende proximo passo sem documentacao externa | P1 |
| QA-U-002 | Preflight com warning | Warning e acionavel e indica impacto | P0 |
| QA-U-003 | Dashboard gerencial | Gestor identifica score, riscos criticos e tendencia em menos de 2 minutos | P1 |
| QA-U-004 | Dashboard tecnico | Analista filtra por severidade/categoria e chega na evidencia | P0 |
| QA-U-005 | Tela responsiva | Dashboard funciona em 1366x768 e 1920x1080 | P1 |
| QA-U-006 | Sem internet | Dashboard abre sem CDN, fonte externa ou asset remoto | P0 |
| QA-U-007 | Dados indisponiveis | UI distingue nao medido, nao aplicavel, importado e aprovado | P0 |
| QA-U-008 | Muitos achados | Tabela permanece usavel com 10.000 findings | P1 |
| QA-U-009 | Login | Tela de login local e clara e nao parece erro tecnico | P1 |
| QA-U-010 | Seletor de coleta | Usuario escolhe OU, tipos de objeto, pacote e profundidade sem ambiguidade | P0 |
| QA-U-011 | Perfil gerencial | ExecutiveViewer ve somente indicadores saneados | P0 |
| QA-U-012 | Perfil tecnico | SecurityAnalyst consegue ir do KPI ate evidencia permitida | P0 |
| QA-U-013 | Tabela | Clicar header de coluna | Ordenacao funciona por risco, idade, owner, status e confianca | P0 |
| QA-U-014 | Export | Recorte filtrado | CSV exportado contem somente recorte atual e colunas versionadas | P0 |
| QA-U-015 | Action Center | Perfil tecnico | Tela mostra fila operacional, proximo passo e acoes claras | P0 |
| QA-U-016 | Visual | Light/dark | Ambos temas polidos, com contraste e hierarquia equivalentes | P1 |
| QA-U-017 | Decision drawer | Finding operacional | Drawer tem abas Evidence, Decision Support, Script/Runbook e Validation | P0 |

## 7. Matriz de compatibilidade

| ID | Cenario | Resultado esperado | Prioridade |
| --- | --- | --- | --- |
| QA-C-001 | Windows PowerShell 5.1 | CLI executa fluxo principal | P0 |
| QA-C-002 | PowerShell 7 presente | Fluxo nao quebra, mesmo que baseline seja 5.1 | P2 |
| QA-C-003 | Sem modulo ActiveDirectory | Coleta LDAP/ADSI principal funciona ou registra limitacao especifica | P0 |
| QA-C-004 | Com modulo ActiveDirectory | Produto pode usar recurso opcional sem mudar contrato de dados | P1 |
| QA-C-005 | Host sem RSAT completo | Pacotes independentes executam e faltas viram warnings | P0 |
| QA-C-006 | Grupo com muitos membros | Range retrieval ou fallback evita truncamento | P0 |
| QA-C-007 | Grupos builtin renomeados/localizados | Resolucao por SID conhecido identifica grupos criticos | P0 |
| QA-C-008 | Sem internet | Banco, login e dashboard local funcionam sem chamada externa | P0 |
| QA-C-009 | Defender restritivo | Produto nao exige escrita fora do diretorio configurado | P1 |
| QA-C-010 | Porta ocupada | Servidor local escolhe/solicita outra porta segura | P1 |

## 8. Criterios de aceite por epic

### Epic 1 - Coleta local

- Dado um host domain-joined, quando o engenheiro executa a coleta, entao o produto registra run, preflight, objetos, findings, metricas e logs no banco local criptografado.
- Dado que uma ferramenta opcional esta ausente, quando o pacote e executado, entao o produto registra warning e continua os pacotes independentes.
- Dado um escopo por OU, quando a coleta termina, entao objetos fora do escopo nao aparecem nos achados desse pacote.
- Dado seletores de tipos de objeto, pacotes e profundidade, quando a coleta termina, entao o run registra exatamente o escopo selecionado.

### Epic 2 - Analise e scoring

- Dado um conjunto de fixtures conhecido, quando o analyzer executa, entao os findings esperados sao gerados com severidade correta.
- Dado o mesmo input, quando o analyzer executa duas vezes, entao score e findings sao deterministas.
- Dado um finding sensivel, quando o pacote gerencial e criado, entao campos sensiveis sao removidos ou mascarados.
- Dado um perfil de politica, quando o analyzer executa, entao os thresholds efetivamente aplicados sao registrados no run.
- Dado um finding com regra de negocio, quando o dashboard exibe o detalhe, entao a decisao recomendada e o benchmark aparecem.

### Epic 3 - Dashboard

- Dado um run completo, quando o usuario autenticado abre o dashboard tecnico local, entao consegue ver score, filtros, achados e evidencias conforme seu perfil.
- Dado um perfil gerencial, quando o gestor abre o dashboard, entao visualiza indicadores sem detalhes sensiveis.
- Dado ausencia de internet, quando o dashboard abre, entao todos os assets carregam localmente.
- Dado usuario sem permissao, quando tenta abrir evidencia sensivel, entao recebe acesso negado e o evento e auditado.

### Epic 4 - Remediation planner

- Dado um achado com acao possivel, quando o planner roda, entao gera recomendacao, impacto, pre-requisitos e rollback.
- Dado uma acao destrutiva, quando nao ha aprovacao explicita, entao nenhum comando e executado.
- Dado modo `WhatIf`, quando o script e gerado, entao o comando inclui parametros seguros quando suportados.
- Dado cleanup hibrido sem evidencia cloud suficiente, quando o planner roda, entao gera `NeedsEvidence` e nao gera delete.
- Dado AD Recycle Bin desabilitado, quando o planner avalia remocao, entao bloqueia `Remove` e recomenda disable/quarantine.
- Dado lacuna de SIEM/log, quando o planner roda, entao gera runbook externo de coleta.

### Epic 5 - Historico

- Dado dois runs, quando o dashboard compara resultados, entao novos, resolvidos e recorrentes aparecem corretamente.
- Dado alteracao de algoritmo de score, quando o dashboard compara runs, entao a versao do algoritmo aparece no contexto.
- Dado metricas de runs consecutivos, quando a visao gerencial abre, entao crescimento, reducao e tendencia aparecem corretamente.

### Epic 6 - Storage, auth e RBAC

- Dado primeiro uso, quando LocalAdmin e criado, entao senha e armazenada apenas como hash com salt.
- Dado banco local, quando app inicia em modo producao, entao banco sem criptografia e rejeitado.
- Dado servidor local, quando app inicia, entao listener fica em `127.0.0.1` por padrao.
- Dado roles diferentes, quando acessam as mesmas paginas, entao dados e acoes respeitam RBAC.

## 9. Evidencias obrigatorias por release

Cada release candidata deve conter:

- Log do smoke test em host sem dependencia opcional completa.
- Resultado de testes unitarios/fixtures.
- Resultado de teste em lab AD.
- Screenshot ou evidencia do dashboard tecnico.
- Screenshot ou evidencia do dashboard gerencial saneado.
- Lista de capacidades indisponiveis conhecidas.
- Lista de regras implementadas.
- Versao do catalogo de regras e perfil de politica usado.
- Versao de schema do banco e migrations aplicadas.
- Evidencia de banco criptografado.
- Evidencia de testes de login/RBAC.
- Evidencia de bind local em `127.0.0.1`.
- Evidencia de validacao dos thresholds e decisoes recomendadas.
- Evidencia de Action Center com proximo passo operacional.
- Amostra de CSV operacional exportado.
- Amostra de script gerado em modo revisao/WhatIf.
- Amostra de `run-manifest.json`, `findings.json` e `scorecard.json`.
- Confirmacao de que credenciais nao foram persistidas.

## 10. Gate de release

Release nao pode ser aprovada se houver:

- Persistencia de senha, segredo ou token.
- Banco local sem criptografia em modo producao.
- Chave de banco em texto claro.
- Dashboard seguro acessivel sem login.
- Role gerencial acessando dado tecnico sensivel.
- Servidor local expondo porta de rede externa por padrao.
- Crash total por ausencia de RSAT/modulo opcional.
- Remediacao destrutiva executada por padrao.
- Dashboard gerencial expondo campos sensiveis por padrao.
- Score nao deterministico para o mesmo input.
- Threshold aplicado sem registro no manifesto/finding.
- `AcceptRisk` sem owner, justificativa, escopo, validade e aprovador.
- Capacidade ausente sendo tratada como controle aprovado.
- Cleanup hibrido sem evidencia cloud suficiente recomendando delete.
- Plano de delete sem AD Recycle Bin/retencao conhecida.
- Script de remediacao executado automaticamente.
- Tabela operacional sem ordenacao/export para recorte filtrado.
- Falha em abrir dashboard sem internet.
- Contrato JSON quebrado sem bump de schema version.

## 11. Smoke test minimo

1. Abrir PowerShell 5.1.
2. Inicializar banco local criptografado e usuario LocalAdmin.
3. Executar login local.
4. Executar preflight.
5. Rodar coleta com pacote de inventario e cleanup basico.
6. Confirmar run persistido no banco com manifesto, findings e scorecard.
7. Abrir dashboard tecnico local autenticado.
8. Abrir dashboard gerencial com perfil saneado.
9. Validar que warnings de ferramentas ausentes aparecem no preflight.
10. Confirmar que nenhum comando de remediacao foi executado.
11. Confirmar que pacote gerencial nao contem campos sensiveis proibidos.
12. Confirmar que servidor local escuta somente em `127.0.0.1`.
13. Confirmar que a tabela ordena por risco e owner.
14. Exportar CSV filtrado e validar colunas.
15. Abrir Action Center e confirmar proximo passo operacional.
16. Validar que usuario hibrido sem evidencia cloud fica como `NeedsEvidence`.
