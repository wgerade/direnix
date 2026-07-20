# Especificacao BTA - Direnix e Identity Hygiene

Status: Draft v0.1  
Data: 2026-06-20  
Publico: Produto, Arquitetura, Desenvolvimento, UX, QA, Operacoes e Seguranca

## 1. Resumo executivo

A solucao proposta e uma plataforma local, Windows-first, para estabilizar, higienizar, proteger e modernizar ambientes Microsoft Active Directory on-premises e hibridos. O produto deve permitir que um engenheiro de AD configure coletas, execute diagnosticos, gere evidencias, produza recomendacoes de remediacao e entregue uma visao web moderna para equipes tecnicas e de gestao.

Direcao tecnica atual: o produto deve ser software instalado de mercado, nao uma colecao de scripts. A arquitetura alvo fica em `docs/TARGET_PRODUCT_ARCHITECTURE.md` e prevalece sobre decisoes antigas de prototipo.

O MVP de produto deve operar sem agente instalado em Domain Controller e sem banco externo obrigatorio. A solucao deve usar um Windows Service real, API local, frontend web moderno, banco local transacional criptografado, autenticacao moderna, RBAC, auditoria, LDAP/LDAPS por biblioteca controlada e ferramentas Microsoft apenas quando forem capacidade opcional. PowerShell pode existir como adaptador de coleta/fallback, mas nao deve ser a plataforma principal do produto.

Diretriz revisada: a ferramenta deve ser operacional antes de ser fiscalizadora. Cada finding precisa conduzir o usuario para uma decisao ou acao segura: coletar evidencia adicional, exportar para owner, gerar script revisavel, abrir runbook externo, aceitar risco com validade ou validar resolucao em novo run. A especificacao detalhada desse comportamento esta em `docs/OPERATIONAL_WORKBENCH_PLAN.md`.

## 2. Objetivo de produto

Criar uma solucao de assessment continuo de Active Directory com foco em:

- Limpeza e otimizacao de diretorio.
- Higiene de identidade e reducao de risco operacional.
- Hardening de seguranca alinhado a Zero Trust.
- Saude de replicacao, SYSVOL, GPO e controladores de dominio.
- Evidencias tecnicas auditaveis e indicadores gerenciais.
- Roadmap de modernizacao e governanca de identidade.

## 3. Proposta comercial

### 3.1 Problema que o produto resolve

Ambientes AD legados acumulam objetos obsoletos, permissoes excessivas, GPOs conflitantes, contas de servico mal geridas, trusts antigos e configuracoes que aumentam risco de movimento lateral, falhas de replicacao, indisponibilidade e exposicao a ataques de identidade. Muitas organizacoes nao possuem uma visao unica, executavel e recorrente para transformar esses achados em backlog priorizado.

### 3.2 Valor para o cliente

- Reduzir superficie de ataque e privilegio acumulado.
- Aumentar confiabilidade operacional de AD, GPO e replicacao.
- Dar visibilidade executiva para risco, evolucao e pendencias.
- Acelerar auditorias com evidencias rastreaveis.
- Priorizar acoes por risco, esforco, impacto e SLA.
- Criar base para Zero Trust, tiered admin model, PAW, gMSA e governanca.

### 3.3 Personas

- Engenheiro de AD: configura escopo, executa coletas, valida achados, cria plano de remediacao.
- Especialista de seguranca: acompanha riscos, exposicoes privilegiadas, delegacoes e baselines.
- Gestor de infraestrutura: acompanha score, tendencia, backlog, progresso e riscos residuais.
- Auditor/compliance: consome evidencias, historico, controles e status por framework.
- Arquitetura: define padroes, roadmap, restricoes tecnicas e criterios de evolucao.

### 3.4 Diferenciais esperados

- Sem dependencia obrigatoria de agente, banco externo, servico cloud ou runtime externo.
- Execucao read-only por padrao.
- Separacao entre coleta, analise, remediacao sugerida e aprovacao.
- Banco local criptografado para historico, timeline, comparativos e auditoria.
- Dashboard seguro servido localmente com autenticacao; export estatico apenas para pacote saneado.
- Dados sensiveis classificados e exportacao gerencial saneada.
- Capacidade de operar em ambientes restritos, isolados ou altamente regulados.

### 3.5 Empacotamento sugerido

- Community/Internal MVP: coleta local, banco local criptografado, dashboard local autenticado, exportacao JSON/HTML saneada.
- Professional: historico de execucoes, score gerencial, matriz de risco, relatorios executivos.
- Enterprise: multi-dominio, multi-floresta, workflow de aprovacao, importacoes hibridas, integracao SIEM via arquivos, pacotes de evidencias para auditoria.

## 4. Escopo funcional

### 4.1 Cliente e coleta para engenheiro

O produto deve entregar um cliente/portal local para o engenheiro configurar e executar coletas. A implementacao alvo deve ser:

- Windows Service real como backend/API/job engine.
- Portal web moderno como interface principal.
- Coletor AD por biblioteca .NET LDAP/LDAPS, paginado, limitado e cancelavel.
- PowerShell adapter apenas para fallback ou evidencias especificas.
- Modo assistido para configurar alvo, credencial, escopo e pacotes.

Funcionalidades minimas do cliente:

- Selecionar dominio, floresta, OUs, sites, DCs, tipos de objeto, profundidade e pacotes de coleta.
- Validar pre-requisitos locais antes da execucao.
- Executar verificacao de ambiente read-only.
- Exibir progresso, warnings, erros e caminho dos artefatos.
- Permitir coleta com credencial atual ou credencial informada no momento da execucao.
- Nao persistir senha, segredo, token ou credencial sensivel.
- Gerar pacote de resultados completo e pacote gerencial saneado.

### 4.2 Dashboard web

O dashboard deve ser uma interface web moderna baseada em HTML, CSS e JavaScript vanilla, sem bibliotecas externas obrigatorias de frontend. Deve funcionar em dois modos:

- Modo seguro: servidor local obrigatorio em `127.0.0.1` com autenticacao, sessao e RBAC.
- Modo estatico: export saneado para gestao/auditoria, sem dados sensiveis e sem promessa de controle de acesso.

Visoes obrigatorias:

- Visao gerencial: score geral, tendencia, riscos criticos, evolucao, SLA, backlog e progresso.
- Visao tecnica: achados por dominio, objeto, GPO, DC, ACL, replicacao, service account e hybrid identity.
- Visao de remediacao: recomendacoes, prioridade, impacto, esforco, pre-requisitos e rollback.
- Visao operacional: fila de acoes, decisao recomendada, confianca, gatilhos, export CSV, script/runbook e validacao.
- Visao de evidencias: arquivos coletados, timestamps, hash, origem, escopo e responsavel.
- Visao historica: comparacao entre execucoes, novos achados, resolvidos e recorrentes.

### 4.3 Autenticacao, perfis e dados locais

O produto deve incluir:

- Banco local criptografado para historico e comparativos.
- Login local do aplicativo, sem integracao obrigatoria com AD no MVP.
- Perfis `LocalAdmin`, `CollectorOperator`, `SecurityAnalyst`, `RiskManager`, `Auditor`, `ExecutiveViewer` e `ReadOnlyTechnical`.
- Auditoria local de login, coleta, exportacao, aceite de risco e visualizacao de evidencias sensiveis.
- Dashboard bindado somente em `127.0.0.1` por padrao.
- Export gerencial estatico somente com dados saneados.

### 4.4 Pacotes de coleta

O cliente deve permitir habilitar/desabilitar pacotes:

1. Inventario e topologia.
2. Limpeza de objetos.
3. Identidade e contas privilegiadas.
4. ACL e delegacoes.
5. SPN, UPN e service accounts.
6. GPO e baselines.
7. Replicacao, SYSVOL e DC health.
8. Trusts, dominios e florestas legadas.
9. Hybrid identity e Entra ID readiness.
10. SIEM/logging readiness.
11. Documentacao e governanca.

O desenho completo de acesso, niveis de permissao, seletores de OU, tipo de objeto, profundidade e feature flags deve seguir `docs/COLLECTION_ACCESS_AND_SCOPE.md`.

## 5. Requisitos funcionais por dominio

### 5.1 Direnix e otimizacao

O produto deve identificar e classificar:

- Usuarios inativos por `lastLogonTimestamp`, `lastLogon`, `pwdLastSet`, `enabled` e ausencia de atividade.
- Computadores inativos, desabilitados, sem logon recente ou sem atualizacao de senha.
- OUs vazias, OUs sem dono, OUs fora de padrao e OUs com heranca bloqueada.
- Objetos com atributos obrigatorios ausentes, inconsistentes ou fora de naming convention.
- SIDs orfaos em ACLs, membros inexistentes, Foreign Security Principals e referencias quebradas.
- GPOs sem link, sem configuracao, duplicadas, antigas, conflitantes ou com permissoes indevidas.
- Trusts obsoletos, dominios/florestas legadas e DCs antigos/decomissionados incorretamente.
- SPNs duplicados, UPNs conflitantes e contas de servico com configuracao arriscada.

Saidas esperadas:

- Achados com severidade, evidencia, objeto afetado, impacto, recomendacao e acao segura.
- Lista de candidatos a remediacao com status `ReviewRequired` por padrao.
- Relatorio de limpeza por categoria e potencial reducao de superficie.
- Classificacao de confianca para cleanup, evitando decisao baseada em um unico atributo.
- Guardrail para identidade hibrida: sem evidencia cloud/import/manual suficiente, usuario sincronizado ou potencialmente sincronizado deve ir para `NeedsEvidence` ou `NeedsOwnerDecision`, nao para limpeza direta.
- Validacao de AD Recycle Bin, `tombstoneLifetime` e `msDS-deletedObjectLifetime` antes de qualquer plano de exclusao.
- Export CSV de candidatos de cleanup com colunas estaveis para revisao por owner.

### 5.2 Hardening e protecao de identidade

O produto deve avaliar:

- Exposicao de grupos privilegiados e nested membership.
- Direitos equivalentes a Domain Admin, Enterprise Admin, Schema Admin e Administrators.
- Delegacoes perigosas, incluindo unconstrained delegation e delegacoes sensiveis.
- Possivel exposicao a DC Sync por permissoes de replicacao de diretorio.
- Riscos de AdminSDHolder, objetos protegidos e `adminCount`.
- Contas com senha sem expiracao, senha reversivel, DES/RC4 legado e pre-auth Kerberos desabilitado.
- Politicas de NTLM, LDAP signing/channel binding, Kerberos, lockout e senha.
- Aderencia conceitual a tiered admin model, PAW e separacao Tier 0/1/2.
- Contas de servico elegiveis para gMSA ou rotacao controlada.
- Baselines CIS/NIST/Microsoft por mapeamento configuravel, sem fixar versao em codigo.

Saidas esperadas:

- Score de risco de identidade.
- Matriz de privilegio efetivo e caminhos de escalada provaveis.
- Recomendacoes de hardening priorizadas por risco.
- Evidencias tecnicas exportaveis para auditoria.

### 5.3 Replicacao, SYSVOL e DC management

O produto deve coletar e analisar:

- Lista de DCs, FSMO roles, GC, sites, subnets e site links.
- Status de replicacao, falhas, latencia, parceiros e convergencia.
- Saude de DFSR/SYSVOL, backlog e inconsistencias.
- Eventos relevantes de Directory Services, DNS Server, DFS Replication, System e Security.
- Indicadores de USN rollback, lingering objects, tombstone issues e erros persistentes.
- Versao de sistema operacional, patch level disponivel localmente, configuracoes de hardening e estado de servicos criticos.

Ferramentas locais permitidas quando disponiveis:

- ADSI/LDAP via .NET.
- WMI/CIM.
- Windows Event Log.
- `repadmin`, `dcdiag`, `nltest`, `netdom`, `wevtutil`, `gpresult`, `secedit`, `auditpol`, `setspn`, `klist`.

O produto deve detectar a ausencia de qualquer ferramenta e registrar capacidade indisponivel com orientacao clara.

### 5.4 GPO management e cleanup

O produto deve:

- Inventariar GPOs, links, filtros WMI, security filtering, status e owners.
- Identificar GPOs sem link, sem configuracao, duplicadas, conflitantes ou antigas.
- Validar GPOs criticas para DCs, servidores, workstations e contas privilegiadas.
- Mapear bloqueio de heranca, enforced links e ordem de precedencia.
- Identificar drift entre GPO esperada e aplicada quando houver evidencia local.
- Exportar matriz de GPO por OU, escopo, risco e recomendacao.

### 5.5 Hybrid identity e Entra ID

O MVP deve tratar hybrid identity em duas camadas:

Camada on-prem obrigatoria:

- Atributos relevantes para sincronizacao: UPN, proxyAddresses, mail, objectGUID, immutable/source anchor quando presente, enabled, OU scope e duplicidades.
- Contas com UPN invalido, dominio nao roteavel ou conflito de atributo.
- Indicios de Azure AD Connect instalados localmente, servicos, eventos e configuracoes acessiveis.
- Erros de sincronizacao disponiveis em logs locais quando a ferramenta estiver instalada no servidor analisado.

Camada cloud opcional:

- Importacao manual de CSV/JSON exportado pelo administrador do tenant.
- Importacao manual de evidencias de sign-in/activity, incluindo ultimo uso cloud quando disponivel.
- Checklist de Conditional Access, MFA, modern authentication e passwordless.
- Sem dependencia obrigatoria de Microsoft Graph, modulo cloud ou internet no MVP.

O dashboard deve deixar claro quando um indicador hibrido e medido por evidencia local, importacao manual ou checklist declarativo.

Regra de seguranca de produto: ausencia de evidencia cloud nao deve ser interpretada como ausencia de uso cloud. Em cleanup de identidades hibridas, a ausencia deve reduzir confianca e gerar proximo passo de coleta.

### 5.6 Documentacao, governanca e melhoria continua

O produto deve gerar:

- Inventario de topologia, dominios, florestas, DCs, sites, trusts, OUs e GPOs.
- Sumario executivo de riscos e evolucao.
- Backlog priorizado de remediacao.
- Mapa de ownership por OU, GPO, conta de servico e grupo critico quando disponivel.
- Evidencias por controle e por framework.
- Comparativo historico entre execucoes.

## 6. Arquitetura proposta

### 6.1 Componentes

1. `Collector Client`
   - PowerShell CLI e GUI local.
   - Lida com configuracao, preflight, execucao e empacotamento.

2. `Collection Engine`
   - Cmdlets/scripts PowerShell.
   - Usa ADSI/LDAP, .NET, WMI/CIM, Event Logs e ferramentas Microsoft nativas.
   - Coleta dados brutos e normalizados.

3. `Analyzer Engine`
   - Regras deterministicas em PowerShell.
   - Gera achados, score, severidade, recomendacoes e evidencias.
   - Deve suportar regras configuraveis por JSON.

4. `Artifact Store`
   - Banco local criptografado e pasta local protegida por ACL.
   - Armazena runs, findings, metricas, evidencias, auditoria e historico.
   - Mantem artefatos pesados em arquivos protegidos com hash e indice no banco.

5. `Local Dashboard Server`
   - Serve HTML/CSS/JS local em `127.0.0.1`.
   - Aplica login, sessao e RBAC.
   - Consulta o banco local por APIs internas.
   - Nao abre porta de rede por padrao.

6. `Dashboard Exporter`
   - Gera export HTML/JSON/CSV saneado.
   - Exporta pacote gerencial estatico sem dados sensiveis.
   - Exporta pacote tecnico apenas para perfil autorizado.
   - Exporta CSV operacional do recorte atual com ordenacao/filtros aplicados.

7. `Remediation Planner`
   - Gera recomendacoes e scripts opcionais em modo `WhatIf`.
   - Nunca executa remediacao destrutiva por padrao.
   - Exige aprovacao e trilha de auditoria para acao real.
   - Gera runbook externo quando a ferramenta nao conseguir medir determinada evidencia.

### 6.2 Fluxo de dados

1. Engenheiro abre cliente e seleciona escopo.
2. Cliente executa preflight e confirma capacidades disponiveis.
3. Engine coleta dados brutos read-only.
4. Engine normaliza objetos e grava run no banco local criptografado.
5. Analyzer aplica regras, calcula score e atualiza historico.
6. Servidor local entrega dashboard autenticado.
7. Gestao acessa visao gerencial saneada conforme RBAC.
8. Time tecnico acessa visao detalhada e plano de remediacao conforme perfil.
9. Execucoes futuras comparam tendencia, recorrencia, crescimento, reducao e resolucao.

### 6.3 Estrutura sugerida de repositorio

```text
Direnix/
  src/
    Collector/
    Analyzers/
    Storage/
    Auth/
    LocalServer/
    Reporting/
    Client/
    Shared/
  dashboard/
    index.html
    app.js
    styles.css
  config/
    business-rules.example.json
    collection-packs.json
    risk-rules.json
    naming-standards.example.json
    baseline-map.example.json
  docs/
    DIRENIX_BTA_SPEC.md
    DIRENIX_RULES_AND_INDICATORS.md
    LOCAL_DATA_SECURITY_AND_AUTH.md
    COLLECTION_ACCESS_AND_SCOPE.md
    UX_INFORMATION_ARCHITECTURE.md
    MVP_IMPLEMENTATION_PLAN.md
    DIRENIX_QA_MATRIX.md
  tests/
    unit/
    fixtures/
    integration/
  tools/
    Invoke-Direnix.ps1
    Start-DirenixClient.ps1
    Start-DirenixDashboard.ps1
```

### 6.4 Estrutura sugerida de saida

```text
output/
  data/
    direnix.adcx
  runs/
    2026-06-20_083000/
      run-manifest.json
      preflight.json
      raw/
      normalized/
      findings.json
      scorecard.json
      remediation-plan.json
      evidence/
      dashboard-technical/
      dashboard-management/
      logs/
  latest-run.json
  reports/
  backups/
```

A plataforma de dados, autenticacao, RBAC, criptografia, backup e seguranca local deve seguir `docs/LOCAL_DATA_SECURITY_AND_AUTH.md`.

## 7. Contratos de dados

### 7.1 `run-manifest.json`

Campos minimos:

- `runId`
- `startedAt`
- `completedAt`
- `executedBy`
- `hostName`
- `domain`
- `forest`
- `scope`
- `collectionPacks`
- `toolCapabilities`
- `warnings`
- `dataClassification`
- `schemaVersion`

### 7.2 `finding`

Campos minimos:

- `id`
- `ruleId`
- `title`
- `description`
- `category`
- `domain`
- `affectedObject`
- `objectType`
- `objectSid`
- `severity`
- `riskScore`
- `confidence`
- `firstSeen`
- `lastSeen`
- `status`
- `businessImpact`
- `technicalImpact`
- `evidenceRefs`
- `recommendation`
- `remediationType`
- `rollbackNotes`
- `decisionSupport`
- `confidence`
- `operationalState`
- `scriptGenerationStatus`
- `validationPlan`
- `frameworkMappings`
- `managementSafe`
- `sensitiveFields`

### 7.3 Severidade

- `Critical`: exposicao imediata a comprometimento de Tier 0, replicacao quebrada em area critica, DC Sync indevido, trust perigoso, GPO critica insegura.
- `High`: risco relevante de escalada, falha persistente de replicacao, conta privilegiada insegura, delegacao perigosa.
- `Medium`: higiene deficiente com impacto moderado, GPO antiga, objeto stale, inconsistencias de atributo.
- `Low`: melhoria de padronizacao, documentacao, naming, ownership ou limpeza de baixo risco.
- `Info`: evidencia, contexto ou recomendacao sem risco imediato.

### 7.4 Score gerencial

O score deve ser calculado de 0 a 100 por area:

- Cleanup Health.
- Identity Risk.
- Replication Health.
- GPO Hygiene.
- Hybrid Readiness.
- Governance Maturity.

O algoritmo deve ser deterministico, documentado e versionado. Mudancas no algoritmo devem alterar `scoreAlgorithmVersion`.

### 7.5 Catalogo de regras e indicadores

As regras de negocio, indicadores tecnicos, indicadores gerenciais, thresholds, benchmarks e decisoes recomendadas devem seguir o contrato definido em `docs/DIRENIX_RULES_AND_INDICATORS.md`.

O produto deve salvar em cada run:

- `ruleCatalogVersion`
- `policyProfile`
- `scoreAlgorithmVersion`
- thresholds efetivamente aplicados
- benchmarks associados aos findings
- decisao recomendada por regra
- status de evidencia: `measured`, `notMeasured`, `notApplicable` ou `capabilityMissing`
- estado operacional e decisao recomendada
- confianca da recomendacao
- plano de remediacao/validacao quando existir

O arquivo `config/business-rules.example.json` deve servir como exemplo inicial para Desenvolvimento e QA, mas o motor deve aceitar configuracoes customizadas do cliente.

## 8. UX e experiencia

As decisoes de arquitetura da informacao, direcao visual, componentes, tom de interface e anti-padroes devem seguir `docs/UX_INFORMATION_ARCHITECTURE.md`. A referencia visual salva em `docs/assets/ux-reference-human-ops-dashboard.png` deve ser tratada como inspiracao para uma experiencia humana, operacional e proxima de um portal tipo Azure, nao como layout a ser copiado literalmente.

### 8.1 Cliente do engenheiro

Telas minimas:

- Inicio: ultimo run, status, botoes de nova coleta e abrir dashboard.
- Escopo: dominio, floresta, OUs, DCs, sites e filtros.
- Pacotes: checkboxes por pacote de coleta com descricao curta.
- Preflight: capacidades disponiveis, permissoes, ferramentas e riscos de escopo.
- Execucao: progresso por etapa, logs resumidos, erros acionaveis.
- Resultados: resumo, caminho do dashboard, exportacoes e plano de remediacao.
- Operacao instalada: criar, atualizar, iniciar, parar e remover o servico Windows do portal.

### 8.2 Dashboard tecnico

Deve priorizar densidade, filtros e rastreabilidade:

- Filtros por dominio, severidade, categoria, OU, owner, status e pacote.
- Tabela de achados com busca e ordenacao.
- Tabelas com paginacao, ordenacao por coluna e export do recorte atual.
- Drawer/modal de detalhe com evidencia, objeto afetado e recomendacao.
- Drawer operacional com abas de evidencia, apoio a decisao, script/runbook e validacao.
- Comparacao entre runs.
- Exportacao CSV/JSON por visao.
- Link entre finding, evidencia e regra.

### 8.3 Dashboard gerencial

Deve priorizar leitura rapida:

- Score geral e por area.
- Tendencia de risco.
- Achados criticos abertos, novos e resolvidos.
- SLA e aging de remediacao.
- Top riscos por dominio/unidade.
- Evolucao de limpeza.
- Riscos aceitos e excecoes.
- Mapa de maturidade Zero Trust identity.

### 8.4 Requisitos visuais

- Design moderno, responsivo e sobrio.
- Sem dependencias CDN.
- Sem fonte externa obrigatoria.
- Cores com contraste adequado.
- Componentes densos para operacao tecnica.
- Indicadores claros para dado medido, inferido, importado ou nao disponivel.

## 9. Seguranca e privacidade

Requisitos obrigatorios:

- Execucao read-only por padrao.
- Separacao de relatorio tecnico completo e gerencial saneado.
- Nao persistir credenciais.
- Banco local criptografado por padrao.
- Login local obrigatorio para dashboard seguro.
- RBAC para separar operacao, seguranca, risco, auditoria e gestao.
- Servidor do dashboard bindado somente em `127.0.0.1` por padrao.
- Registrar quem executou, quando, onde e com qual escopo.
- Classificar campos sensiveis e permitir redacao.
- Gerar hash SHA-256 dos principais artefatos de evidencia.
- Evitar expor DNs, SIDs, nomes de contas privilegiadas e detalhes de ACL em relatorio gerencial por padrao.
- Permitir armazenamento em pasta protegida por ACL do Windows.
- Assinar scripts em distribuicoes formais quando aplicavel.
- Nao enviar dados para internet no MVP.

## 10. Requisitos nao funcionais

### 10.1 Compatibilidade

- Runtime do produto instalado como baseline minimo.
- PowerShell 5.1/7 pode ser usado como adaptador opcional de coleta, suporte ou diagnostico, mas nao como dependencia principal da plataforma.
- Execucao em workstation administrativa domain-joined ou servidor autorizado.
- Suporte a dominios com localizacao de grupos internos em idiomas diferentes usando SID conhecido sempre que possivel.

### 10.2 Performance

- Usar consultas LDAP paginadas.
- Evitar carregar objetos completos quando atributos especificos bastam.
- Suportar range retrieval para grupos grandes.
- Permitir escopo incremental por OU, dominio, pacote e data.
- Registrar tempo por etapa.

Metas iniciais:

- Ate 25.000 objetos: execucao padrao em ate 15 minutos em ambiente saudavel.
- Ate 100.000 objetos: execucao em ate 60 minutos com escopo completo.
- Dashboard com ate 10.000 achados deve abrir em ate 5 segundos em maquina padrao.

### 10.3 Resiliencia

- Falha de um pacote nao deve interromper todo o run.
- Erros devem ser registrados com contexto e recomendacao.
- Coleta deve salvar progresso parcial.
- Reexecucao deve criar novo run imutavel.

### 10.4 Auditoria

- Cada run deve ter manifesto, log e resumo de capacidades.
- Evidencias devem apontar para origem e timestamp.
- Regras devem ser versionadas.
- Excecoes devem ter owner, justificativa, validade e aprovador.

## 11. Regras de remediacao

O MVP deve recomendar e planejar, mas nao executar automaticamente remediacoes destrutivas.

Estados de uma recomendacao:

- `Detected`
- `NeedsReview`
- `Approved`
- `ScriptGenerated`
- `ExecutedExternally`
- `Resolved`
- `RiskAccepted`
- `FalsePositive`

Cada remediacao deve incluir:

- Descricao da acao.
- Impacto esperado.
- Risco de execucao.
- Pre-requisitos.
- Comando sugerido em modo `WhatIf` quando aplicavel.
- Rollback ou plano de reversao quando possivel.
- Evidencia de antes/depois.
- Nivel de confianca.
- Gatilhos para recaptura de evidencia.
- Estado esperado no proximo run.

Scripts gerados:

- Devem ser PowerShell revisaveis.
- Devem usar `-WhatIf` quando suportado.
- Devem separar lista de objetos em CSV com hash quando houver lote.
- Devem incluir comandos de validacao antes/depois.
- Devem ficar em estado `ScriptGenerated`, sem execucao automatica no MVP.

## 12. Dependencias permitidas e proibidas

Permitido no MVP:

- PowerShell 5.1.
- .NET Framework local.
- Banco local embarcado criptografado, distribuido com o produto sem servico externo.
- ADSI/LDAP via `System.DirectoryServices`.
- WMI/CIM.
- Event Log APIs.
- Windows Service Control Manager.
- Ferramentas Microsoft presentes no host.
- HTML/CSS/JavaScript vanilla.
- Arquivos JSON/CSV/XML/HTML locais.

Proibido como dependencia obrigatoria no MVP:

- Banco de dados externo ou servico de banco instalado.
- Servico cloud.
- Agente instalado.
- NPM, Node.js, Python ou runtime externo.
- Bibliotecas JavaScript por CDN.
- Modulos PowerShell de terceiros.
- Microsoft Graph como requisito obrigatorio.

Opcional com deteccao:

- Modulo ActiveDirectory, quando ja disponivel.
- Ferramentas RSAT, quando ja disponiveis.
- Azure AD Connect/ADSync, quando instalado no servidor avaliado.
- Pester para testes locais, desde que a suite tenha fallback ou documente versao minima.
- DuckDB para analytics futuro, se licenca e criptografia atenderem ao perfil de seguranca.

## 13. Roadmap sugerido

### Fase 0 - Discovery tecnico

- Validar consultas ADSI/LDAP sem modulo ActiveDirectory.
- Validar coleta de eventos, GPO, replication e SYSVOL em lab.
- Prototipar banco local criptografado, login local e dashboard autenticado com dados mockados.
- Definir contrato JSON v1.

### Fase 1 - MVP tecnico

- CLI de coleta.
- Banco local criptografado e schema v1.
- Autenticacao local e RBAC minimo.
- Pacotes: inventario, cleanup basico, privileged groups, SPN/UPN, GPO basico, replication health.
- Dashboard tecnico e gerencial via servidor local autenticado.
- Exportacao de evidencias.
- QA com fixtures e lab domain.

### Fase 2 - Operacionalizacao

- Cliente GUI local.
- Windows Service para hospedar portal e execucao assistida.
- Historico e comparacao entre runs.
- Excecoes e SLA.
- Action Center.
- Export CSV operacional.
- Geracao de scripts revisaveis.
- Validacao de AD Recycle Bin antes de delete.
- Relatorio gerencial saneado.

### Fase 3 - Hardening avancado

- ACL analyzer aprofundado.
- DC Sync, delegation, AdminSDHolder, tier model.
- gMSA readiness.
- Baseline mapper configuravel.
- Remediation planner com WhatIf.

### Fase 4 - Hybrid e governanca

- Importacao de evidencias cloud.
- Checklist CA/MFA/passwordless.
- SIEM readiness.
- Pacotes de auditoria.
- Multi-floresta e consolidacao executiva.

## 14. Criterios de aceite de alto nivel

1. O produto executa coleta read-only em ambiente AD sem instalar componentes de terceiros.
2. O cliente informa claramente pre-requisitos presentes e ausentes.
3. O run gera manifesto, dados normalizados, findings, scorecard, evidencias e dashboard.
4. O dashboard tecnico permite investigar achados ate a evidencia.
5. O dashboard gerencial apresenta score, evolucao, riscos, backlog e SLA sem vazar detalhes sensiveis por padrao.
6. A ausencia de RSAT/modulos opcionais nao quebra o produto inteiro.
7. Todas as recomendacoes destrutivas permanecem em modo revisao/WhatIf por padrao.
8. O QA consegue validar regras com fixtures sem depender de AD real para todos os casos.
9. O QA consegue validar pelo menos um ciclo em lab AD real.
10. Cada release documenta limites conhecidos e capacidades indisponiveis.
11. O catalogo de regras gera decisoes consistentes: limpar, ajustar, implementar, investigar, monitorar, descomissionar ou aceitar risco.
12. Os thresholds aplicados aparecem no run e no detalhe do finding.
13. Riscos aceitos exigem owner, justificativa, escopo, validade e aprovador.
14. Todo finding P0/P1 tem proximo passo operacional: investigar, coletar evidencia, exportar, gerar script, implementar, limpar, aceitar risco ou validar.
15. Cleanup de identidade hibrida sem evidencia cloud suficiente nao recomenda exclusao direta.
16. Exclusao de objeto exige validacao de recuperabilidade; sem AD Recycle Bin, o produto recomenda disable/quarantine no MVP.

## 15. Riscos e decisoes em aberto

| Tema | Risco | Decisao proposta |
| --- | --- | --- |
| RSAT ausente | Nem toda maquina tera `repadmin`, `dcdiag` ou modulo AD | Detectar capacidade e degradar por pacote |
| Hybrid cloud | CA/MFA exigem APIs ou exportacao cloud | MVP usa evidencias on-prem e importacao manual |
| Dados sensiveis | Dashboard pode expor SIDs, DNs e grupos privilegiados | Gerar pacote gerencial saneado |
| Remediacao | Automacao pode causar indisponibilidade | MVP recomenda e gera WhatIf, sem auto-execucao |
| Performance | Dominios grandes podem ter consultas caras | Paging, filtros, escopo incremental e logs por etapa |
| Testes | Pester varia por host | Suite deve detectar versao e ter scripts de validacao basica |

## 16. Definicao de pronto para MVP

O MVP sera considerado pronto quando:

- Banco local criptografado, login local, RBAC e audit log estiverem implementados.
- Coleta e analise funcionarem em lab AD com pelo menos dois DCs.
- Dashboard tecnico e gerencial funcionarem sem internet via servidor local autenticado.
- Export gerencial estatico for gerado com saneamento de dados.
- Pelo menos 30 regras de analise estiverem implementadas e documentadas.
- Contrato de dados v1 estiver versionado.
- QA tiver matriz executada com evidencias.
- Security review confirmar que nao ha persistencia de credenciais.
- Relatorio gerencial nao exponha campos sensiveis por padrao.
- Execucao com ferramenta opcional ausente seja tratada como warning, nao crash.
