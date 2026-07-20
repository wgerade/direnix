# Regras de Negocio, Indicadores e Benchmarks - Direnix

Status: Draft v0.1  
Data: 2026-06-20  
Publico: Produto, Arquitetura, Desenvolvimento, UX, QA, Seguranca, Operacoes e Gestao

## 1. Objetivo

Este documento define a estrutura do motor de regras, indicadores tecnicos, indicadores de negocio, thresholds e decisoes esperadas para a solucao Direnix e Identity Hygiene.

O objetivo nao e apenas apontar "problemas tecnicos". O produto deve responder quatro perguntas de gestao:

- O que esta quebrado ou inseguro?
- O que precisa ser limpo?
- O que precisa ser implementado ou ajustado?
- O que pode ser aceito como risco, com dono, justificativa e prazo?

## 2. Principios do motor de decisao

1. Toda regra deve ter evidencia.
2. Toda evidencia deve ter origem, data, escopo e confianca.
3. Toda recomendacao deve ser classificada como limpar, ajustar, implementar, investigar, monitorar, descomissionar ou aceitar risco.
4. Nenhuma acao destrutiva deve ser executada automaticamente no MVP.
5. Thresholds devem ser configuraveis por perfil de risco.
6. Achados tecnicos devem ser traduzidos para impacto de negocio.
7. Risco aceito nao e risco ignorado: deve ter owner, validade e revisao.
8. O dashboard gerencial deve mostrar tendencia e exposicao, nao detalhes sensiveis por padrao.
9. Todo finding deve gerar proximo passo operacional.
10. Cleanup deve usar modelo de confianca, nao um unico atributo.
11. Identidade hibrida sem evidencia cloud suficiente deve ir para `NeedsEvidence`.
12. Remediacao destrutiva exige plano, script revisavel e validacao de recuperabilidade.

## 3. Hierarquia de benchmark

Quando referencias divergirem, o produto deve usar esta prioridade:

1. Configuracao aprovada pelo cliente.
2. Baseline Microsoft aplicavel ao sistema operacional e papel do servidor.
3. Microsoft Defender for Identity / Microsoft Secure Score para postura de identidade.
4. NIST SP 800-53 para governanca e controles de alto nivel.
5. CIS Controls e CIS Benchmarks para controles praticos e hardening.
6. Referencias de comunidade AD reconhecidas, como PingCastle e ADSecurity, para padroes de risco e cobertura.

## 4. Referencias de benchmark usadas

| Fonte | Uso na solucao | URL |
| --- | --- | --- |
| Microsoft Defender for Identity - Accounts security posture assessments | Contas stale, service accounts privilegiadas, contas sensiveis dormentes, delegation, credenciais em texto claro, atributos inseguros | https://learn.microsoft.com/en-us/defender-for-identity/security-posture-assessments/accounts |
| Microsoft Defender for Identity - Identity infrastructure assessments | DCs/ADFS/ADCS/Entra Connect sem monitoramento, LDAP signing, `ms-DS-MachineAccountQuota` | https://learn.microsoft.com/en-us/defender-for-identity/security-posture-assessments/identity-infrastructure |
| Microsoft - Best practices for securing Active Directory | Protecao de grupos privilegiados, minimo privilegio, reducao de superficie | https://learn.microsoft.com/en-us/windows-server/identity/ad-ds/plan/security-best-practices/best-practices-for-securing-active-directory |
| Microsoft Security Baselines | Baselines recomendadas pela Microsoft para configuracao segura | https://learn.microsoft.com/en-us/windows/security/operating-system-security/device-management/windows-security-configuration-framework/windows-security-baselines |
| Microsoft Security Compliance Toolkit | Comparacao e armazenamento de baselines em formato consumivel | https://learn.microsoft.com/en-us/windows/security/operating-system-security/device-management/windows-security-configuration-framework/security-compliance-toolkit-10 |
| Microsoft - LDAP signing | LDAP signing, channel binding, eventos 2886/2887/2888 | https://learn.microsoft.com/en-us/windows-server/identity/ad-ds/ldap-signing |
| Microsoft - NTLM audit policy | Auditoria antes de bloqueio de NTLM | https://learn.microsoft.com/en-us/previous-versions/windows/it-pro/windows-10/security/threat-protection/security-policy-settings/network-security-restrict-ntlm-audit-ntlm-authentication-in-this-domain |
| Microsoft - Group Policy processing/scope/modeling | Evidencia para filtros, precedencia, RSoP, security filtering, WMI filters | https://learn.microsoft.com/en-us/windows-server/identity/ad-ds/manage/group-policy/group-policy-processing |
| Microsoft - AD replication troubleshooting | Replicacao, lingering objects, tombstone, `repadmin` e diagnostico | https://learn.microsoft.com/en-us/troubleshoot/windows-server/active-directory/diagnose-replication-failures |
| Microsoft - SYSVOL/DFSR troubleshooting | SYSVOL/NETLOGON, DFSR state, eventos e checks de DC | https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/troubleshoot-missing-sysvol-and-netlogon-shares |
| NIST SP 800-53 Rev. 5 | Controles AC-2, AC-6, IA-5, AU, CM, RA e governanca de risco | https://csrc.nist.gov/pubs/sp/800/53/r5/upd1/final |
| CIS Controls v8.1 | Inventario, configuracao segura, account management, access control management, logging | https://www.cisecurity.org/controls/cis-controls-list |
| PingCastle AD health check rules | Categorias de risco de comunidade: Stale Objects, Privileged Accounts, Trusts, Anomalies | https://www.pingcastle.com/PingCastleFiles/ad_hc_rules_list.html |

## 5. Perfis de politica

O produto deve suportar perfis de benchmark. O cliente pode escolher um perfil inicial e ajustar excecoes.

| Perfil | Uso | Caracteristica |
| --- | --- | --- |
| `MicrosoftDefault` | Ambientes enterprise que querem alinhar com Microsoft Defender for Identity e Secure Score | Usa thresholds Microsoft quando existirem, como 90 dias para contas stale e 180 dias para contas sensiveis dormentes |
| `CISStrict` | Ambientes regulados ou com alto apetite por hardening | Usa thresholds mais agressivos, como 45 dias para contas dormentes quando suportado |
| `OperationalBalanced` | Ambientes grandes com legado e dependencias complexas | Mantem controles criticos em tolerancia zero, mas amplia prazos de limpeza operacional |
| `Custom` | Cliente possui politica formal propria | Usa thresholds definidos em `business-rules.json` |

## 6. Modelo de decisao

Cada regra deve produzir uma decisao recomendada:

| Decisao | Quando usar | Exemplo |
| --- | --- | --- |
| `CleanUp` | Objeto ou configuracao obsoleta sem dependencia conhecida | Usuario inativo ha 180 dias sem owner |
| `Adjust` | Configuracao existe, mas precisa ser corrigida | Conta privilegiada sem flag "Account is sensitive and cannot be delegated" |
| `Implement` | Controle recomendado nao existe | LDAP signing nao exigido; LAPS ausente |
| `Investigate` | Evidencia indica risco, mas faltam dados para acao | NTLM ainda usado por sistemas desconhecidos |
| `Monitor` | Risco baixo ou dependencia temporaria sob observacao | GPO antiga mas com owner e justificativa |
| `Decommission` | Componente legado deve sair do ambiente | Trust antigo sem uso e sem owner |
| `AcceptRisk` | Negocio decide manter excecao por motivo formal | Conta de servico legado com SPN e senha sem expiracao durante projeto de migracao |
| `NeedsEvidence` | Faltam dados para uma decisao segura | Usuario hibrido sem evidencia de ultimo uso cloud |
| `ReadyForCleanup` | Evidencia suficiente para limpeza controlada | Workstation stale com alta confianca |
| `GenerateScript` | Pode gerar script revisavel/WhatIf | Disable ou move para quarentena |

### 6.1 Estados operacionais

| Estado | Uso |
| --- | --- |
| `Detected` | Condicao encontrada |
| `NeedsEvidence` | Coleta adicional necessaria |
| `NeedsOwnerDecision` | Owner/negocio precisa confirmar impacto |
| `ReadyForCleanup` | Candidato revisavel para limpeza |
| `ReadyForRemediationPlan` | Pode gerar plano/script |
| `ScriptGenerated` | Script criado, ainda nao executado |
| `ExportedForOwner` | Enviado para revisao por CSV/relatorio |
| `RiskAccepted` | Risco aceito com validade |
| `ValidationPending` | Espera novo run para confirmar |
| `Resolved` | Corrigido por evidencia posterior |

## 7. Modelo de severidade

### 7.1 Severidade tecnica

| Severidade | Definicao |
| --- | --- |
| `Critical` | Pode comprometer Tier 0, dominio, credenciais, replicacao, autenticacao ou trust de forma ampla |
| `High` | Aumenta risco relevante de escalada, movimento lateral, indisponibilidade ou falha de compliance |
| `Medium` | Exposicao real, mas com impacto limitado ou dependente de condicao adicional |
| `Low` | Higiene, padronizacao, governanca ou risco operacional menor |
| `Info` | Contexto, inventario ou recomendacao sem risco imediato |

### 7.2 Multiplicadores de negocio

O risco final deve considerar a criticidade do ativo ou processo:

| Fator | Multiplicador |
| --- | --- |
| Afeta Tier 0, DC, Entra Connect, ADCS, ADFS ou PKI | +30 |
| Afeta ambiente regulado, SOX, PCI, LGPD, HIPAA ou similar | +20 |
| Afeta sistema de receita, producao ou atendimento critico | +20 |
| Sem owner ou sem janela de manutencao conhecida | +10 |
| Achado recorrente em mais de 3 runs | +10 |
| Possui excecao aprovada vigente | -20 |
| Evidencia incompleta ou baixa confianca | -15 |

### 7.3 Formula inicial

```text
businessRiskScore = min(100, technicalRiskBase + businessMultipliers - compensatingControls)
```

Faixas:

- 90-100: Critical.
- 70-89: High.
- 40-69: Medium.
- 10-39: Low.
- 0-9: Info.

## 8. Indicadores gerenciais

### 8.1 Executive AD Health Score

Score geral de 0 a 100.

Formula sugerida:

```text
Executive AD Health Score =
  0.25 * Identity Protection Score +
  0.20 * Privileged Access Score +
  0.15 * Cleanup Hygiene Score +
  0.15 * Replication and DC Health Score +
  0.10 * GPO and Baseline Score +
  0.10 * Hybrid Identity Score +
  0.05 * Governance Score
```

Interpretacao:

| Score | Estado | Mensagem gerencial |
| --- | --- | --- |
| 90-100 | Healthy | Ambiente sob controle, manter monitoramento |
| 75-89 | Managed Risk | Risco conhecido com backlog administravel |
| 60-74 | Needs Attention | Priorizar plano de remediacao |
| 40-59 | High Exposure | Exposicao relevante ao negocio |
| 0-39 | Critical Exposure | Risco alto de comprometimento ou indisponibilidade |

### 8.2 Key Risk Indicators

| Indicador | Pergunta de negocio | Formula | Target |
| --- | --- | --- | --- |
| Critical Identity Exposure Count | Quantas exposicoes podem comprometer o dominio? | Count findings Critical em identidade/Tier 0 | 0 |
| Privileged Account Sprawl | Existe excesso de privilegio acumulado? | Contas unicas com privilegio Tier 0 / total de admins justificados | Tendencia decrescente |
| Cleanup Debt | Quanto legado ainda precisa ser limpo? | Objetos candidatos a limpeza ponderados por idade e criticidade | Tendencia decrescente |
| Stale Account Ratio | Quantas contas humanas estao stale? | Stale enabled users / enabled users | Abaixo do threshold do cliente |
| Stale Computer Ratio | Quantos computadores representam inventario morto? | Stale computers / enabled computers | Abaixo do threshold do cliente |
| Service Account Risk | Contas de servico estao controladas? | Service accounts com risco / service accounts inventariadas | Tendencia decrescente |
| Replication Risk | O AD esta convergindo? | DCs/particoes com falha ou atraso relevante | 0 critical |
| GPO Drift | Politicas estao previsiveis? | GPOs conflitantes/sem owner/fora de baseline | Tendencia decrescente |
| Hybrid Identity Conflict | Identidades hibridas estao saudaveis? | UPN/proxy/sourceAnchor conflicts | 0 critical |
| Risk Acceptance Debt | Quanto risco foi aceito e precisa revisao? | Excecoes vencidas + excecoes sem owner | 0 vencidas |

### 8.3 Indicadores de execucao

| Indicador | Formula | Uso |
| --- | --- | --- |
| Findings Open | Count findings com status aberto | Backlog total |
| Findings New | Findings do run atual nao vistos antes | Mudanca recente |
| Findings Resolved | Findings vistos antes e ausentes no run atual | Evolucao |
| Findings Recurring | Findings presentes em 3+ runs | Falha de governanca |
| Mean Time To Remediate | Media entre firstSeen e resolvedAt | SLA operacional |
| Accepted Risk Expiring Soon | Excecoes que vencem em ate 30 dias | Gestao de risco |
| Evidence Coverage | Regras com evidencia completa / regras executadas | Confianca do run |
| Collection Capability Coverage | Pacotes executados / pacotes aplicaveis | Limite da coleta |

## 9. Indicadores tecnicos por dominio

### 9.1 Cleanup e lifecycle

| Indicador | Medicao | Target default |
| --- | --- | --- |
| Enabled stale users | Usuarios habilitados sem logon recente | 0 para privilegiados; reduzir para usuarios comuns |
| Enabled stale computers | Computadores habilitados sem logon/senha recente | Reduzir continuamente |
| Disabled objects retained beyond policy | Objetos desabilitados ha mais que periodo de retencao | 0 fora da politica |
| Orphaned SID references | ACEs ou memberships com SID nao resolvido | 0 em ACLs criticas |
| Duplicate identity attributes | UPN/proxy/mail/SPN duplicados | 0 |
| OU ownership coverage | OUs com owner definido / total de OUs | >= 95% |

### 9.2 Privileged access

| Indicador | Medicao | Target default |
| --- | --- | --- |
| Tier 0 direct members | Membros diretos em grupos Tier 0 | Minimo justificado |
| Tier 0 effective members | Membros diretos + aninhados | 100% conhecidos e aprovados |
| Privileged service accounts | Service accounts em grupos privilegiados | 0 sem justificativa |
| DCSync exposure | Principals nao-DC com direitos de replicacao sensiveis | 0 |
| AdminSDHolder drift | ACL inesperada em AdminSDHolder | 0 |
| Sensitive dormant entities | Entidades sensiveis inativas | 0 |

### 9.3 Authentication e hardening

| Indicador | Medicao | Target default |
| --- | --- | --- |
| LDAP signing enforcement | DC exige signing | Enforced apos compatibilidade validada |
| LDAP unsigned bind events | Eventos 2887/2888 por DC | 0 apos remediacao |
| NTLM dependency count | Sistemas/contas ainda usando NTLM | Tendencia decrescente ate bloqueio planejado |
| Weak Kerberos attributes | DES, preauth desabilitado, RC4-only | 0 em contas sensiveis |
| Cleartext credential exposure | Atributos com senha/hint | 0 |
| MachineAccountQuota | Valor de `ms-DS-MachineAccountQuota` | 0 |

### 9.4 GPO e baseline

| Indicador | Medicao | Target default |
| --- | --- | --- |
| Unlinked GPOs | GPOs sem link | 0 apos retencao |
| Empty GPOs | GPOs sem configuracao | 0 ou justificadas |
| GPOs without owner | GPOs sem owner/documentacao | 0 |
| Domain/DC linked GPO weak permissions | GPOs criticas com modify fora de grupo aprovado | 0 |
| Baseline drift | Divergencia contra baseline Microsoft/CIS aprovada | Reduzir por plano |
| GPO processing errors | Eventos/GPResult com erro | 0 critical |

### 9.5 Replicacao e DC health

| Indicador | Medicao | Target default |
| --- | --- | --- |
| Replication failures | Falhas por DC/particao/parceiro | 0 |
| Last successful replication age | Idade do ultimo sucesso por particao | Abaixo de 24h para alerta; abaixo de TSL sempre |
| Lingering object indicators | Eventos 1388/1988, erros 8606/8614 | 0 |
| SYSVOL/NETLOGON share coverage | DCs expondo shares esperados | 100% |
| DFSR error state | Eventos DFSR criticos ou WMI state problematica | 0 |
| Unsupported/legacy DC OS | DCs fora de suporte ou padrao | 0 |

### 9.6 Trusts, hybrid e infraestrutura de identidade

| Indicador | Medicao | Target default |
| --- | --- | --- |
| Trusts without owner | Trusts sem owner/justificativa | 0 |
| Stale trusts | Trusts sem evidencia de uso recente | 0 apos revisao |
| SID filtering disabled | Trusts com SID filtering ausente quando aplicavel | 0 |
| Entra Connect risk | Servidor sem monitoramento, hardening ou owner | 0 critical |
| Hybrid identity conflicts | UPN/proxy/sourceAnchor duplicados | 0 |
| ADCS/ADFS monitoring gap | Servidores criticos sem monitoramento | 0 |

## 10. Estrutura da regra

Cada regra deve ser declarada neste formato conceitual:

| Campo | Descricao |
| --- | --- |
| `ruleId` | Identificador estavel, ex. `ADCLN-USER-STALE-001` |
| `title` | Nome curto da regra |
| `businessQuestion` | Pergunta que a regra responde para gestao |
| `technicalCondition` | Condicao tecnica avaliada |
| `evidenceSources` | LDAP, ADSI, EventLog, repadmin, dcdiag, GPO XML, fixture etc. |
| `benchmarkRefs` | Microsoft/CIS/NIST/PingCastle/cliente |
| `defaultThreshold` | Threshold default e perfil aplicavel |
| `severityBase` | Severidade inicial |
| `businessDecision` | CleanUp, Adjust, Implement, Investigate, Monitor, Decommission, AcceptRisk |
| `ownerType` | AD Engineering, Security, App Owner, Infrastructure, Governance |
| `slaDefault` | Prazo recomendado para tratar |
| `autoRemediationAllowed` | Sempre `false` no MVP para acao destrutiva |
| `whatIfSupported` | Se pode gerar script seguro em WhatIf |
| `managementSafeFields` | Campos permitidos no dashboard gerencial |
| `decisionSupport` | Evidencias adicionais recomendadas |
| `confidenceModel` | Como calcular confianca da recomendacao |
| `scriptTemplate` | Template de script revisavel, quando aplicavel |
| `externalEvidenceRefs` | Eventos, comandos ou CSVs externos aceitos |

## 11. Catalogo inicial de regras

### 11.1 Cleanup e lifecycle

| Rule ID | Regra | Evidencia | Benchmark/threshold default | Severidade | Decisao |
| --- | --- | --- | --- | --- | --- |
| ADCLN-USER-STALE-001 | Usuario habilitado sem atividade recente | `lastLogonTimestamp`, `lastLogon`, `enabled`, eventos quando disponiveis | MicrosoftDefault: 90 dias; CISStrict: 45 dias; Custom: politica cliente | Medium; High se privilegiado | CleanUp |
| ADCLN-USER-DISABLED-RETENTION-002 | Usuario desabilitado retido alem da politica | `enabled=false`, `whenChanged`, OU, owner | Default: 180 dias; customizavel | Low/Medium | CleanUp |
| ADCLN-COMP-STALE-003 | Computador habilitado sem atividade/senha recente | `lastLogonTimestamp`, `pwdLastSet`, `operatingSystem` | Default: 90 dias; Critical se DC obsoleto | Medium | CleanUp |
| ADCLN-OU-EMPTY-004 | OU vazia ou sem proposito documentado | LDAP OU, child count, owner | Default: sem child e sem owner por 90 dias | Low | CleanUp/Monitor |
| ADCLN-ACL-ORPHANSID-005 | SID orfao em ACL | Security descriptor, SID lookup | Target: 0 em OUs/GPOs/Tier 0 | High se Tier 0; Medium geral | CleanUp |
| ADCLN-ID-DUPUPN-006 | UPN duplicado/invalido | `userPrincipalName` | Target: 0 | High em hybrid; Medium on-prem | Adjust |
| ADCLN-ID-DUPPROXY-007 | `proxyAddresses` duplicado | `proxyAddresses`, `mail` | Target: 0 | High em hybrid | Adjust |
| ADCLN-SPN-DUP-008 | SPN duplicado | `servicePrincipalName`, `setspn -X` quando disponivel | Target: 0 | High | Adjust |
| ADCLN-ATTR-PASSWORD-009 | Senha ou hint em atributos de texto livre | `description`, `info`, `adminComment`, atributos customizados permitidos | Target: 0 | Critical se privilegiado; High geral | CleanUp |
| ADCLN-HYBRID-EVID-010 | Identidade possivelmente hibrida sem evidencia cloud suficiente para cleanup | OU scope, UPN/proxy/mail, Entra Connect local/import cloud | Se hibrido e sem evidencia cloud, nao recomendar delete | Info/Medium | NeedsEvidence |
| ADCLN-RECYCLEBIN-011 | AD Recycle Bin desabilitado ou retencao desconhecida | Optional feature, `tombstoneLifetime`, `msDS-deletedObjectLifetime` | Deve estar conhecido antes de delete | High | Implement |

### 11.2 Privileged access e Tier 0

| Rule ID | Regra | Evidencia | Benchmark/threshold default | Severidade | Decisao |
| --- | --- | --- | --- | --- | --- |
| ADPRV-T0-GROUPS-001 | Membros diretos em grupos Tier 0 acima do aprovado | SID-known groups, membership direto | Target: lista aprovada | High/Critical | Adjust |
| ADPRV-T0-NESTED-002 | Membros efetivos Tier 0 via nested groups | Recursive membership, range retrieval | Target: 100% justificado | High/Critical | Adjust |
| ADPRV-SVC-PRIV-003 | Service account em grupo privilegiado | Service account heuristic + membership | Microsoft MDI recomenda remover se acesso elevado nao for requerido | Critical/High | Adjust |
| ADPRV-DCSYNC-004 | Principal nao-DC com direitos DCSync | ACL domain root: DS-Replication permissions | Target: 0 fora de DC/contas aprovadas | Critical | Adjust |
| ADPRV-ADMINSDHOLDER-005 | Permissao suspeita em AdminSDHolder | ACL AdminSDHolder | Target: somente principals aprovados | Critical | Adjust |
| ADPRV-DORMANT-SENSITIVE-006 | Conta sensivel dormente | Sensitive group membership + last activity | MicrosoftDefault: 180 dias; CISStrict: 45/90 por politica | High/Critical | CleanUp |
| ADPRV-NOTDELEGATED-007 | Conta privilegiada sem flag "sensitive and cannot be delegated" | UAC flag `NOT_DELEGATED` | Microsoft recomenda habilitar para Domain Admins, Enterprise Admins e service accounts privilegiadas | High | Adjust |
| ADPRV-SIDHISTORY-008 | SIDHistory em conta/grupo sensivel | `sIDHistory` | Target: 0 sem justificativa | High | Investigate/Adjust |
| ADPRV-BUILTIN-OPERATORS-009 | Account/Server/Backup/Print Operators com membros | Membership grupos builtin sensiveis | Target: 0 ou aprovado | High | Adjust |
| ADPRV-KRBTGT-AGE-010 | `krbtgt` sem rotacao documentada | `pwdLastSet`, evidencia de processo | Default: revisar se >180 dias; ajustar por politica | High | Implement/Adjust |

### 11.3 Authentication, protocolo e hardening

| Rule ID | Regra | Evidencia | Benchmark/threshold default | Severidade | Decisao |
| --- | --- | --- | --- | --- | --- |
| ADAUTH-LDAP-SIGN-001 | LDAP signing nao exigido no DC | GPO/security option, registry, eventos 2886/2888 | Microsoft recomenda exigir signing no nivel de DC apos validar compatibilidade | High | Implement |
| ADAUTH-LDAP-CBT-002 | LDAP channel binding desabilitado ou nunca exigido | GPO/registry, eventos LDAP | Default: pelo menos "When supported"; evoluir para enforcement planejado | Medium/High | Implement |
| ADAUTH-LDAP-CLEARTEXT-003 | Entidades expondo credenciais em LDAP claro | Eventos 2887, MDI import, logs | Target: 0 | High/Critical | Investigate/Adjust |
| ADAUTH-NTLM-AUDIT-004 | Auditoria NTLM nao habilitada | Security policy, NTLM operational log | Microsoft recomenda auditar para entender trafego antes de bloquear | Medium | Implement |
| ADAUTH-NTLM-DEPENDENCY-005 | Dependencias NTLM sem owner | NTLM logs, event 4624/4625, Operational logs | Target: reduzir e ter plano de excecao | Medium/High | Investigate |
| ADAUTH-KRB-PREAUTH-006 | Kerberos pre-auth desabilitado | UAC `DONT_REQ_PREAUTH` | Target: 0 em contas sensiveis | High | Adjust |
| ADAUTH-KRB-DES-007 | DES habilitado | UAC `USE_DES_KEY_ONLY` | Target: 0 | High | Adjust |
| ADAUTH-KRB-AES-008 | AES nao suportado em conta relevante | `msDS-SupportedEncryptionTypes` | Target: AES habilitado onde aplicavel | Medium | Adjust |
| ADAUTH-MAQ-009 | `ms-DS-MachineAccountQuota` maior que 0 | Domain attribute | Microsoft MDI recomenda 0 | High | Adjust |
| ADAUTH-LAPS-010 | LAPS ausente ou senha nao rotacionada | LAPS attributes, GPO, device inventory | Microsoft MDI avalia cobertura e rotacao em 60 dias | High | Implement |

### 11.4 Service accounts e delegation

| Rule ID | Regra | Evidencia | Benchmark/threshold default | Severidade | Decisao |
| --- | --- | --- | --- | --- | --- |
| ADSVC-INACTIVE-001 | Service account inativa | SPN, logon, service binding, last activity | Microsoft MDI: 90 dias para inactive service accounts | Medium/High | CleanUp |
| ADSVC-PWDNEVER-002 | Service account com senha sem expiracao e sem controle | UAC `DONT_EXPIRE_PASSWORD`, owner, rotation evidence | Target: justificativa + rotacao/gMSA | Medium/High | Adjust |
| ADSVC-GMSA-READY-003 | Conta elegivel para gMSA ainda user-based | SPN, services, host binding | Target: migrar quando tecnicamente viavel | Medium | Implement |
| ADSVC-UNCONSTR-004 | Unconstrained delegation | UAC `TRUSTED_FOR_DELEGATION` | Target: 0; redesign para constrained/RBCD seguro quando necessario | Critical/High | Adjust |
| ADSVC-CONSTR-SENSITIVE-005 | Delegation para servicos sensiveis sem aprovacao | `msDS-AllowedToDelegateTo`, owner | Target: aprovado/documentado | High | Investigate/Adjust |
| ADSVC-RBCD-RISK-006 | Resource-based constrained delegation suspeita | `msDS-AllowedToActOnBehalfOfOtherIdentity` | Target: principals aprovados | High/Critical | Investigate/Adjust |

### 11.5 GPO, baseline e configuracao

| Rule ID | Regra | Evidencia | Benchmark/threshold default | Severidade | Decisao |
| --- | --- | --- | --- | --- | --- |
| ADGPO-UNLINKED-001 | GPO sem link | GPO inventory/link count | Retencao default: 90 dias | Low/Medium | CleanUp |
| ADGPO-EMPTY-002 | GPO vazia | GPO settings XML/report | Target: 0 sem justificativa | Low | CleanUp |
| ADGPO-NOOWNER-003 | GPO sem owner ou descricao | `description`, owner metadata | Target: 100% owner em GPO critica | Medium | Adjust |
| ADGPO-DC-PERM-004 | GPO linkada a Domain Controllers com modify indevido | ACL GPO, link target | Target: somente grupos aprovados | Critical | Adjust |
| ADGPO-DOMAIN-PERM-005 | GPO de dominio com permissao ampla | ACL GPO, delegated permissions | Target: minimo privilegio | High | Adjust |
| ADGPO-WMI-BROKEN-006 | WMI filter invalido ou sem alvo | WMI filter, GPResult, processing events | Target: 0 | Medium | Adjust |
| ADGPO-BASELINE-DRIFT-007 | Drift contra baseline aprovada | GPO backup, SCT/Policy Analyzer importado | Target: conforme baseline cliente | Medium/High | Adjust |
| ADGPO-PROCESS-ERR-008 | Erro de processamento de GPO | GroupPolicy Operational log, gpresult | Target: 0 critical | Medium/High | Investigate |

### 11.6 Replicacao, SYSVOL e domain controllers

| Rule ID | Regra | Evidencia | Benchmark/threshold default | Severidade | Decisao |
| --- | --- | --- | --- | --- | --- |
| ADDC-REPL-FAIL-001 | Falha de replicacao atual | `repadmin /replsummary`, `/showrepl`, events | Target: 0 | Critical/High | Investigate |
| ADDC-REPL-AGE-002 | Ultimo sucesso antigo | `last success`, parceiros, particoes | High >24h; Critical perto/maior que Tombstone Lifetime | High/Critical | Investigate |
| ADDC-LINGERING-003 | Indicador de lingering objects | Eventos 1388/1988, erros 8606/8614 | Target: 0 | Critical | Investigate |
| ADDC-SYSVOL-004 | SYSVOL/NETLOGON ausente | `net view`, shares, WMI | Target: 100% DCs com shares | Critical | Investigate |
| ADDC-DFSR-005 | DFSR em erro | DFSR event log, WMI replicated folder state | Target: 0 critical | High/Critical | Investigate |
| ADDC-FRS-006 | SYSVOL ainda em FRS em ambiente elegivel | DFSRMIG/global state, OS/domain functional level | Target: DFSR | Medium/High | Implement |
| ADDC-OS-LEGACY-007 | DC com OS fora de suporte/padrao | OS inventory | Target: 0 | High | Decommission/Implement |
| ADDC-PATCH-UNKNOWN-008 | Patch/compliance desconhecido | WMI hotfix, update evidence | Target: 100% conhecido | Medium | Investigate |

### 11.7 Trusts, dominios legados e hybrid

| Rule ID | Regra | Evidencia | Benchmark/threshold default | Severidade | Decisao |
| --- | --- | --- | --- | --- | --- |
| ADTRUST-NOOWNER-001 | Trust sem owner | Trust inventory + metadata | Target: 100% owner | Medium | Adjust |
| ADTRUST-STALE-002 | Trust sem evidencia de uso | Trust inventory, event/log evidence quando disponivel | Revisar se sem uso em 90/180 dias | Medium/High | Decommission |
| ADTRUST-SIDFILTER-003 | SID filtering ausente quando aplicavel | Trust attributes, `netdom trust` quando disponivel | Target: habilitado quando aplicavel | High | Adjust |
| ADTRUST-OLDPROTO-004 | Trust/protocolo legado | Trust type, OS/domain info | Target: 0 legado sem justificativa | Medium/High | Decommission |
| ADHYB-UPN-CONFLICT-005 | UPN invalido/conflitante para sync | UPN, verified domains importados | Target: 0 | High | Adjust |
| ADHYB-SOURCEANCHOR-006 | Conflito source anchor/immutable ID | on-prem attributes + import cloud opcional | Target: 0 | High | Adjust |
| ADHYB-CONNECT-RISK-007 | Entra Connect sem owner/monitoramento/hardening | Server inventory, services, optional MDI/MDE evidence | Target: 0 critical | Critical/High | Implement |
| ADHYB-ADFS-ADCS-MON-008 | ADFS/ADCS sem monitoramento evidenciado | Server role inventory, optional Defender evidence | Target: 0 critical | High/Critical | Implement |

### 11.8 Governanca, documentacao e excecoes

| Rule ID | Regra | Evidencia | Benchmark/threshold default | Severidade | Decisao |
| --- | --- | --- | --- | --- | --- |
| ADGOV-OWNER-001 | Objeto critico sem owner | OU/GPO/group/service account metadata | Target: 100% para criticos | Medium | Adjust |
| ADGOV-EXPIRED-002 | Excecao de risco vencida | Exception file, expiry date | Target: 0 vencidas | High | AcceptRisk/Adjust |
| ADGOV-NOJUST-003 | Excecao sem justificativa suficiente | Exception file fields | Target: 0 | Medium | Adjust |
| ADGOV-EVIDENCE-004 | Regra sem evidencia suficiente | run manifest, evidence refs | Target: evidencia completa em P0/P1 | Medium | Investigate |
| ADGOV-BACKUP-005 | GPO/AD config sem backup recente evidenciado | Backup manifest | Target: backup antes de mudanca | Medium | Implement |
| ADGOV-RECYCLEBIN-001 | Lixeira do AD desabilitada ou retencao abaixo do minimo | `msDS-EnabledFeature` (Recycle Bin), `msDS-DeletedObjectLifetime`/`tombstoneLifetime` lidos do Configuration NC | Default: habilitada + retencao >= `recycleBinMinRetentionDays` (180) | High se desabilitada; Medium se retencao curta | Implement |

> **Nota de implementacao:** a regra de Lixeira do AD foi implementada como
> `ADGOV-RECYCLEBIN-001` (categoria Governanca). Ela cobre o item de design
> `ADCLN-RECYCLEBIN-011` listado na secao 9.1 e e pre-requisito para acoes de
> `Remove` (a remocao so e segura com a Lixeira habilitada/retencao conhecida).
> A leitura da Lixeira tambem alimenta a classificacao de resolucao de riscos
> removidos (ver `docs/DATA_LIFECYCLE_MODEL.md`).

## 12. SLA recomendado

| Severidade | SLA tratamento | Observacao |
| --- | --- | --- |
| Critical | 7 dias ou plano formal em 48h | Aceite de risco exige aprovador executivo/seguranca |
| High | 30 dias | Aceite exige owner e data de expiracao |
| Medium | 60-90 dias | Pode entrar em backlog trimestral |
| Low | 180 dias | Pode ser agrupado em ciclos de cleanup |
| Info | Sem SLA de remediacao | Usado para inventario e contexto |

## 13. Modelo de aceite de risco

Um risco so pode mudar para `AcceptRisk` se tiver:

- Owner tecnico.
- Owner de negocio ou aprovador de risco.
- Justificativa.
- Escopo exato.
- Data de expiracao.
- Controle compensatorio, se houver.
- Plano de saida ou proxima revisao.
- Evidencia de que a decisao foi revisada.

Riscos que nao podem ser aceitos sem aprovacao executiva:

- DCSync fora de principals aprovados.
- AdminSDHolder com ACL insegura.
- Unconstrained delegation em Tier 0.
- Replicacao quebrada proxima ou acima do Tombstone Lifetime.
- Credenciais em texto claro em objetos privilegiados.
- GPO de Domain Controllers modificavel por grupo nao aprovado.

## 14. Visao UX dos indicadores

### 14.1 Gestao

O dashboard gerencial deve mostrar:

- Score geral.
- Score por area.
- Top 10 riscos de negocio.
- Itens que exigem decisao: implementar, ajustar, limpar ou aceitar risco.
- Evolucao dos ultimos runs.
- Riscos aceitos vencidos ou proximos do vencimento.
- Maturidade por dominio: Cleanup, Privileged Access, Authentication, Replication, GPO, Hybrid, Governance.

### 14.2 Tecnico

O dashboard tecnico deve mostrar:

- Regra, evidencia, objeto, severidade e recomendacao.
- Benchmark associado.
- Threshold usado.
- Diferenca entre valor medido e valor esperado.
- Comando de investigacao ou WhatIf quando aplicavel.
- Impacto e dependencias.
- Historico firstSeen/lastSeen.
- Action Center com proximo passo, confianca, export e script/runbook.
- Cleanup candidates com coluna de evidencia hibrida e risco de execucao.
- Tabela ordenavel por risco, idade, owner, status, confianca e decisao.

## 15. Requisitos para desenvolvimento

1. Implementar regras como catalogo versionado.
2. Separar threshold default de threshold do cliente.
3. Salvar `ruleCatalogVersion` e `policyProfile` em cada run.
4. Permitir que uma regra gere mais de um finding.
5. Permitir que um finding seja associado a mais de um benchmark.
6. Permitir override de severidade por criticidade do ativo.
7. Nao misturar dados sensiveis no payload gerencial.
8. Registrar `notApplicable`, `notMeasured` e `capabilityMissing` separadamente.
9. Manter regras P0 testaveis por fixtures.
10. Garantir que score historico indique versao do algoritmo.
11. Persistir estado operacional do finding.
12. Calcular confianca de cleanup.
13. Gerar `decisionSupport` quando faltar evidencia.
14. Export CSV deve usar colunas versionadas.
15. Geracao de script deve produzir artefato revisavel, nao execucao.
16. Regras de delete devem depender de validacao de AD Recycle Bin/retencao.

## 16. Requisitos para QA

QA deve validar:

- Threshold default por perfil.
- Override de threshold por cliente.
- Severidade base e severidade apos multiplicador de negocio.
- Decisao recomendada por regra.
- Geracao correta de indicadores gerenciais.
- Separacao entre finding tecnico e resumo gerencial.
- `AcceptRisk` com campos obrigatorios.
- Ausencia de false positive conhecido nas fixtures.
- Capacidade ausente nao contada como "compliant".
- Mesmo input gera mesmo score.
- Tabela ordena e exporta o recorte filtrado.
- Cleanup hibrido sem evidencia cloud vira `NeedsEvidence`.
- AD Recycle Bin desabilitado bloqueia `Remove`.
- Script gerado contem `WhatIf` quando suportado e nao e executado.
- Import externo de SIEM/CSV pode ser anexado como evidencia.

## 17. Exemplo de finding normalizado

```json
{
  "id": "finding-20260620-000123",
  "ruleId": "ADAUTH-MAQ-009",
  "ruleCatalogVersion": "0.1",
  "title": "Domain allows non-privileged users to join computers",
  "category": "AuthenticationHardening",
  "severityBase": "High",
  "businessRiskScore": 82,
  "decision": "Adjust",
  "affectedObject": {
    "type": "Domain",
    "displayName": "corp.example",
    "sensitive": false
  },
  "measuredValue": "10",
  "expectedValue": "0",
  "thresholdProfile": "MicrosoftDefault",
  "benchmarkRefs": [
    "Microsoft Defender for Identity - Resolve unsecure domain configurations"
  ],
  "businessImpact": "Aumenta a chance de criacao indevida de contas de computador e abuso em caminhos de escalada.",
  "recommendation": "Avaliar dependencias e alterar ms-DS-MachineAccountQuota para 0.",
  "status": "NeedsReview",
  "evidenceRefs": [
    "evidence/domain/corp.example.domain-attributes.json"
  ],
  "managementSafe": true
}
```

## 18. Decisoes de produto pendentes

| Decisao | Opcoes | Recomendacao |
| --- | --- | --- |
| Threshold default principal | MicrosoftDefault, CISStrict, OperationalBalanced | Usar MicrosoftDefault no MVP |
| Aceite de risco | Arquivo JSON local, CSV, dashboard editavel | Comecar com JSON local versionado |
| Baseline tecnico | Microsoft SCT importado, CIS importado, ambos | Comecar com Microsoft SCT e mapeamento manual |
| Cloud/hybrid | API Graph, import manual, ambos | MVP com import manual; API como fase futura |
| Score | Simples por contagem, ponderado por risco, ponderado por ativo | Ponderado por risco + criticidade do ativo |

## 19. Novas regras de limpeza e governanca (v0.5)

Regras adicionadas para cobrir tipos antes coletados mas nao avaliados (OUs, GPOs, Grupos) e
recomendacoes de mercado (PingCastle/Purple Knight/Microsoft). Severidade calibrada: limpeza = Low;
endurecimento perigoso = High/Medium. Remediacao sempre com dois comandos (pre-visualizar -WhatIf / aplicar).

| ID | Titulo | Categoria | Severidade | Tipos req. | Sinal |
| --- | --- | --- | --- | --- | --- |
| ADCLN-GROUP-EMPTY-011 | Grupo de seguranca vazio | Higiene | Low | Group | `member` vazio (exceto built-in/primary-group) |
| ADCLN-OU-EMPTY-012 | OU vazia | Higiene | Low | OU,User,Computer,Group | sem objetos-filhos (exceto OU "Domain Controllers") |
| ADCLN-GPO-UNLINKED-013 | GPO nao vinculada | Governanca | Low | GPO,OU | GUID ausente de gPLink (dominio/OUs/sites) |
| ADCLN-GPO-EMPTY-014 | GPO sem configuracoes | Governanca | Low | GPO | `versionNumber == 0` |
| ADCLN-GPO-DISABLED-015 | GPO com ambas as secoes desabilitadas | Governanca | Low | GPO | `flags == 3` |
| ADHARD-PWDNOTREQD-016 | Conta permite senha em branco | Endurecimento | High | User | UAC PASSWD_NOTREQD |
| ADHARD-PWDNOEXPIRE-017 | Senha que nunca expira | Endurecimento | Medium | User | UAC DONT_EXPIRE_PASSWORD (habilitada) |
| ADHARD-REVERSIBLEPWD-018 | Criptografia reversivel de senha | Endurecimento | High | User | UAC ENCRYPTED_TEXT_PWD_ALLOWED |
| ADPRV-KERBEROAST-019 | Usuario com SPN (kerberoastable) | Acesso Privilegiado | High (Critical se privilegiado) | User | servicePrincipalName presente |
| ADCLN-COMP-LEGACYOS-020 | SO sem suporte | Infraestrutura | Medium | Computer | operatingSystem legado (XP/7/8/2003/2008/2012...) |
| ADPRV-ADMINCOUNT-ORPHAN-021 | adminCount=1 orfao | Acesso Privilegiado | Medium | User,Group | adminCount=1 sem ser membro de grupo privilegiado |
| ADGOV-CONFLICT-022 | Objetos de conflito (CNF) | Governanca | Medium | Domain | objetos `*\0ACNF:*` no diretorio |
| ADGOV-LOSTFOUND-023 | Objetos orfaos em LostAndFound | Governanca | Medium | Domain | objetos em CN=LostAndFound |

Notas:
- "Grupo vazio" exclui principais internos (RID < 1000) e grupos com membros implicitos via primaryGroupID.
- "OU vazia" so e confiavel quando todos os tipos de objeto-filho foram coletados (escopo completo).
- "GPO nao vinculada" considera links de dominio, OUs e sites; sem acesso ao Configuration NC os links de
  site sao pulados (vira warning) e pode haver falso positivo.
- CNF/LostAndFound sao sinais de saude do banco do AD; ver `docs/AD_DATABASE_HEALTH.md`.

## 20. Indicadores operacionais (v0.7)

Diferente de um **risco** (Finding: tem severidade, remediacao e reconciliacao por `stableFindingKey`), um
**indicador operacional** e informacao de acompanhamento do dia a dia: uma **contagem + lista de objetos**
(drill-down com display, DN e SID), versionada por run (tabela `indicator_results`, schema v8) e exibida em
cards no Painel e no "O que mudou desde ontem". Rodam junto da coleta do perfil ativo — inclusive a agendada.

Novos atributos coletados (read-only): User += `lockoutTime`, `msDS-UserPasswordExpiryTimeComputed`
(data de expiracao efetiva ja calculada pelo AD, considerando politicas refinadas/PSO); Domain += `maxPwdAge`,
`lockoutDuration`.

### 20.1 Indicadores built-in

| ID | Titulo | Categoria | Sinal | Exclusoes |
| --- | --- | --- | --- | --- |
| IND-PWD-EXPIRING | Senhas vencendo (<= N dias) | Senha | `msDS-UserPasswordExpiryTimeComputed` dentro do horizonte (marca "hoje") | desabilitada, gerenciada (`$`), DONT_EXPIRE_PASSWORD |
| IND-PWD-EXPIRED | Senhas expiradas | Senha | expiracao <= agora (inclui `0` = trocar no proximo logon) | idem |
| IND-ACCT-LOCKED | Contas bloqueadas | Conta | `lockoutTime > 0` e ainda dentro de `lockoutDuration` | built-in (RID < 1000) |
| IND-ACCT-EXPIRING | Contas a expirar (<= N dias) | Conta | `accountExpires` dentro do horizonte | desabilitada, built-in |

Horizonte `N` (padrao 7 dias) e configuravel por perfil (`indicatorHorizonDays`). Cada built-in pode ser
desligado por perfil (`disabledIndicators`).

### 20.2 Indicadores customizados (por perfil)

O usuario define no perfil uma consulta propria (`customIndicators`): nome + tipo de objeto + a query em
**LDAP** ou **PowerShell**. **O sistema sempre executa como busca LDAP read-only**, pelo mesmo caminho que
coleta os demais objetos — **PowerShell nunca e executado**; e apenas conforto de digitacao: extraimos o
`-LDAPFilter` (verbatim) ou traduzimos um subconjunto comum de `-Filter` (`-eq/-ne/-like/-and/-or`, `Enabled`)
para filtro LDAP. O que nao for traduzivel retorna erro pedindo `-LDAPFilter`. Isso preserva a garantia
**read-only** e o principio de menor privilegio (gMSA) — nenhuma execucao de codigo arbitrario no servico.

Os indicadores customizados habilitados rodam junto de **toda** coleta do perfil, inclusive a **agendada**
(`ScheduledCollectionService`). A tela de Configuracoes valida/traduz a query (`POST /api/v1/indicators/validate`)
antes de salvar, e todas as alteracoes de perfil (incl. indicadores) sao auditadas.
