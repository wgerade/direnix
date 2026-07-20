# Plano de Integração Entra ID (Nuvem) — Identity Operations Center

> Status: PROPOSTA (aguardando aprovação do dono do produto)
> Data: 2026-07-13
> Pré-requisito de leitura: `TARGET_PRODUCT_ARCHITECTURE.md`, `COLLECTION_ACCESS_AND_SCOPE.md`, `DIRENIX_RULES_AND_INDICATORS.md`

Este documento elabora, em cinco perspectivas (Product Owner, Especialista IAM/Infra, Arquiteto, Tech Lead, e Operação diária de IAM — §5), o plano para estender o produto do AD DS on-premises para **identidade híbrida (AD DS + Microsoft Entra ID)** — mantendo a filosofia atual: **coleta read-only, banco local SQLCipher, remediação sugerida (nunca executada), zero egress de dados para terceiros**.

---

## 1. Visão de Product Owner

### 1.1 Por que agora

- **O ambiente do cliente-alvo é híbrido por padrão.** Quase toda organização com AD on-prem já sincroniza com Entra ID (Entra Connect / Cloud Sync). Uma ferramenta que enxerga só metade da identidade responde só metade da pergunta do IOC: "o que mudou / o que é risco / qual a próxima ação".
- **O atacante não respeita a fronteira.** Os caminhos de ataque modernos pivotam AD↔Entra (conta sincronizada comprometida on-prem vira Global Admin na nuvem, e vice-versa). Higiene e postura precisam ser avaliadas nas duas pontas *e na costura entre elas*.
- **Diferenciação de mercado.** Ferramentas pontuais existem para cada lado (PingCastle/Purple Knight para AD; relatórios nativos do Entra para nuvem). O espaço competitivo real é a **correlação híbrida local, sem SaaS** — nosso banco local criptografado é a vantagem: o concorrente SaaS exige egress dos dados do diretório; nós não.

### 1.2 Proposta de valor (uma frase)

> "Uma única visão de higiene, risco e mudança para a identidade híbrida — AD e Entra ID correlacionados, 100% local, sem enviar um byte do seu diretório para fora."

### 1.3 Personas e jobs-to-be-done

| Persona | Job | O que a extensão entrega |
|---|---|---|
| Admin AD/infra | "quero limpar e endurecer o diretório" | Mesmo fluxo de hoje, agora com objetos cloud e sincronizados |
| Admin IAM/cloud | "quero governar guests, apps e roles no tenant" | Inventário + regras + indicadores Entra (guests obsoletos, secrets vencendo, GAs em excesso) |
| CISO / auditoria | "quero um score e evidências do ambiente inteiro" | Identity Score híbrido + findings com frameworks (MITRE/CIS/NIST) nas duas fontes |

### 1.4 Princípios de produto (invariantes)

1. **Read-only sempre.** Nenhuma escrita no tenant. Remediação = comando Graph PowerShell sugerido com preview (`-WhatIf`), igual ao RuleCatalog atual.
2. **Dados ficam locais.** As chamadas Graph saem do host do cliente direto para o tenant *dele* (Microsoft é o próprio provedor do diretório — não é egress para terceiro). Nada vai para nós.
3. **Degradação graciosa por licença.** `signInActivity` exige Entra P1; risky users exige P2. O produto detecta a licença e desabilita regras/indicadores não suportados com explicação clara (estado `CapabilityMissing`, que já existe).
4. **AD continua primeiro.** Entra é uma segunda fonte, não uma reescrita. Tudo que existe hoje continua funcionando sem tenant configurado.

### 1.5 Fases (releases)

- **E1 — Fundação cloud (v0.8):** conectar tenant, coletar inventário Entra (users/guests/grupos/apps/SPs/roles/devices), ~10 regras de higiene/postura cloud, indicadores operacionais cloud, UI com filtro por fonte.
- **E2 — Correlação híbrida (v0.9):** vincular objeto AD ↔ objeto Entra (SID/ImmutableID), página de objeto unificada, regras híbridas (as de maior valor), Identity Score híbrido, Timeline/Morning View unificados.
- **E3 — Postura avançada (v1.0):** Conditional Access, PIM (atribuições elegíveis vs permanentes), registro de MFA, painel de postura estilo secure score, delta queries para coleta agendada barata.

### 1.6 Não-objetivos (por enquanto)

- Escrita/remediação automática no tenant.
- Multi-tenant MSP (1 tenant por instalação na E1; a modelagem já prevê `tenant_id` para não fechar a porta).
- Detecção em tempo real (sign-in logs streaming, risky users P2) — fica para depois da E3.
- Entra ID Governance (access reviews, entitlement management) — só leitura de resultados, se houver demanda.

---

## 2. Visão de Especialista IAM (o "o quê" coletar e avaliar)

### 2.1 Objetos a coletar (Microsoft Graph v1.0)

| Objeto | Endpoint | Atributos-chave |
|---|---|---|
| Usuários (member + guest) | `/users` | `accountEnabled`, `userType`, `signInActivity`*, `onPremisesSyncEnabled`, `onPremisesSecurityIdentifier`, `onPremisesImmutableId`, `createdDateTime`, `passwordPolicies`, `assignedLicenses` |
| Grupos | `/groups` | tipo (security/M365/dynamic), `membershipRule`, owners, membros, `isAssignableToRole` |
| App registrations | `/applications` | `passwordCredentials` (secrets: validade), `keyCredentials` (certs), owners, `requiredResourceAccess` |
| Service principals | `/servicePrincipals` | `accountEnabled`, permissões app-only concedidas (`appRoleAssignments`), `oauth2PermissionGrants`, sign-in de SP* |
| Roles de diretório | `/roleManagement/directory/roleAssignments` + `roleEligibilitySchedules` (PIM) | principal, role, permanente vs elegível |
| Devices | `/devices` | `approximateLastSignInDateTime`, `trustType`, `accountEnabled` |
| Políticas | `/policies/conditionalAccessPolicies`, `/policies/authorizationPolicy`, security defaults | estado, condições, exclusões |
| Organização | `/organization`, `/domains`, `/subscribedSkus` | `onPremisesLastSyncDateTime`, licenças, domínios federados |

\* exige Entra ID P1 (`signInActivity` via `AuditLog.Read.All`).

### 2.2 Permissões do app registration (application permissions, admin consent)

Mínimo E1: `User.Read.All`, `Group.Read.All`, `Application.Read.All`, `Directory.Read.All`, `AuditLog.Read.All`.
E3 adiciona: `Policy.Read.All`, `RoleManagement.Read.Directory`, `Device.Read.All`, `Reports.Read.All` (registro de MFA).
Opcional/futuro (P2): `IdentityRiskyUser.Read.All`.

**Todas read-only.** O guia de onboarding deve mostrar exatamente esse conjunto e por quê (transparência = confiança, mesmo argumento do modo LDAP read-only atual).

### 2.3 Catálogo inicial de regras (convenção `ENTRA-*`, mesmo modelo do RuleCatalog)

**Higiene (análogas às de AD):**
- `ENTRA-USER-STALE-101` — usuário habilitado sem sign-in há N dias (`signInActivity.lastSignInDateTime`; degrade para `NeedsEvidence` sem P1).
- `ENTRA-GUEST-STALE-102` — guest sem atividade há 90+ dias ou convite nunca aceito (`externalUserState=PendingAcceptance`). Fluxo seguro de mercado: desabilitar → esperar 30 dias → excluir.
- `ENTRA-USER-NEVERLOGGED-103` — conta criada há >N dias que nunca autenticou.
- `ENTRA-GROUP-EMPTY-104` / `ENTRA-GROUP-NOOWNER-105` — grupos vazios / sem dono.
- `ENTRA-DEVICE-STALE-106` — device sem sign-in há N dias.

**Postura de apps e identidades não-humanas (não existe análogo on-prem — maior valor novo):**
- `ENTRA-APP-SECRET-EXPIRED-110` / secret com validade > 180 dias / preferir certificado.
- `ENTRA-APP-NOOWNER-111` — app registration sem dono (pré-requisito de governança).
- `ENTRA-SP-OVERPRIV-112` — service principal com permissões app-only de alto impacto (`RoleManagement.ReadWrite.Directory`, `Application.ReadWrite.All`, `Mail.Read` tenant-wide…), priorizando as não exercidas.
- `ENTRA-SP-ORPHAN-113` — SP sem app correspondente ou sem uso.

**Acesso privilegiado:**
- `ENTRA-PRIV-GA-COUNT-120` — Global Admins fora da faixa recomendada (2–5) ou % alto de atribuições permanentes vs PIM-elegíveis.
- `ENTRA-PRIV-NOMFA-121` — conta privilegiada sem MFA registrado (`Reports.Read.All`).
- `ENTRA-PRIV-SYNCED-GA-122` — **híbrida**: conta *sincronizada do AD* com role privilegiada no Entra (viola a recomendação Microsoft de admins cloud-only; caminho de escalada on-prem→cloud).
- `ENTRA-PRIV-BREAKGLASS-123` — checagem de **presença** (alerta se NÃO encontrar): a recomendação Microsoft é ter 2 contas de acesso de emergência cloud-only com Global Admin permanente, excluídas de CA. Heurística: contas cloud-only com GA permanente e naming/atributos de break-glass; se nada plausível existir, finding informativo com o guia.

**Configuração do tenant (análogo ao MachineAccountQuota):**
- `ENTRA-CFG-USERCONSENT-130` — usuários podem consentir apps / registrar apps / convidar guests sem restrição (`authorizationPolicy.defaultUserRolePermissions`).
- `ENTRA-CFG-NOCA-131` — sem security defaults e sem política de CA exigindo MFA para admins; legacy auth não bloqueado.
- `ENTRA-CFG-SYNCSTALE-132` — Entra Connect sem sincronizar há > X horas (`onPremisesLastSyncDateTime`).

**Correlação híbrida (E2 — o diferencial):**
- `HYB-ORPHAN-140` — objeto no Entra com `onPremisesSyncEnabled=true` cujo objeto de origem no AD não existe mais (órfão de sync).
- `HYB-STATE-DIVERGE-141` — divergência de estado entre as pontas (ex.: obsoleto no AD mas ativo na nuvem — indica uso cloud-only de conta que o time de AD julga morta, ou vice-versa).
- `HYB-TIER0-CROSS-142` — mesmo humano é Tier 0 nos dois planos com a mesma credencial sincronizada (incl. password hash sync de conta Domain Admin).

### 2.4 Indicadores operacionais (contagem + drill-down, modelo atual)

- Secrets/certificados de apps vencendo no horizonte do perfil (análogo direto a "senhas vencendo").
- Guests com convite pendente de aceite.
- Contas cloud a expirar / recém-desabilitadas.
- Licenças atribuídas a contas desabilitadas (desperdício — quick win adorado por gestores).
- Última sincronização do Entra Connect (idade).

### 2.5 Correlação AD ↔ Entra (regras de vinculação)

Ordem de confiança:
1. `onPremisesSecurityIdentifier` (Entra) == `objectSid` (AD) — determinístico; já persistimos SID em `current_object_state` desde a v0.6.
2. `onPremisesImmutableId` == base64(`objectGUID`) — determinístico.
3. UPN/`sAMAccountName` — heurístico, marcado com confiança baixa, nunca gera finding sozinho.

---

## 3. Visão de Arquiteto

### 3.1 Decisões estruturais

1. **Segunda implementação de `ICollectionEngine`, não um fork.** `GraphCollectionEngine` em `Direnix.Infrastructure` ao lado de `LdapCollector`. O Core permanece agnóstico de protocolo.
2. **Fonte como dimensão de primeira classe.** Novo enum `DirectorySource { AdDs, EntraId }`; `objectKey` ganha namespace: hoje `ad:<domain>:<guid>` (formato atual preservado), cloud vira `entra:<tenantId>:<objectId>`. Todas as tabelas de estado/eventos ganham colunas `source` + `tenant_id` (schema v9), com default `AdDs` no migration — dados existentes intocados.
3. **Tipos de objeto:** estender `AdObjectType` quebraria a semântica ("Ad"); criar `DirectoryObjectType` unificado com mapeamento de compatibilidade, ou (mais barato) novo enum `EntraObjectType` + união nos contratos de storage. **Recomendação: enum unificado `DirectoryObjectType`** com migração mecânica — paga o custo uma vez, evita `if source` espalhado.
4. **Cliente Graph: SDK oficial (`Microsoft.Graph` v5+).** Justificativa: retry/throttling (`Retry-After`), paginação e `$batch` já resolvidos pelo middleware; o custo de dependência é aceitável num serviço Windows self-hosted. Encapsular atrás de `IGraphDirectoryProbe`/`IGraphClient` próprios para testabilidade (fixtures JSON, sem SDK nos testes de regra).
5. **Autenticação: client credentials com CERTIFICADO, nunca secret.** Cert no Windows Certificate Store (LocalMachine, chave não exportável) referenciado por thumbprint na config; alternativa DPAPI como o key store atual. Suporte a nuvens soberanas via endpoint configurável (`graph.microsoft.com` default).
6. **Reuso integral dos motores existentes:** `ChangeDetector` (diff de snapshots já é agnóstico de origem — só precisa da chave estável), `HygieneRuleEngine`/`RuleCatalog` (regras `ENTRA-*` entram no mesmo catálogo com `RequiredObjectTypes`), `IndicatorEngine`, timeline/morning view (badge de fonte na UI).
7. **Correlação como pós-processamento:** serviço `IdentityLinker` roda após persistir um run de qualquer fonte; grava em nova tabela `identity_links (ad_object_key, entra_object_key, method, confidence, first_seen, last_confirmed)`. Regras `HYB-*` são avaliadas num passo próprio que lê os links + estado atual das duas fontes (não dentro do run de coleta de uma fonte só).
8. **Identity Score híbrido:** subscore por fonte + penalidade por findings `HYB-*`; Tier 0 passa a incluir roles privilegiadas do Entra no mapa (`IdentityScore` já isola o conceito de Tier 0).

### 3.2 Fluxo (E1)

```
UI "Fontes" → cadastra tenant (tenantId, appId, cert thumbprint)
  → Probe: GET /organization (valida auth, detecta licenças via /subscribedSkus)
  → CollectionRequest { Source=EntraId, ... } → GraphCollectionEngine
     → pagina /users, /groups, /applications, /servicePrincipals, /roleAssignments…
     → normaliza para CollectedObject (mesmo contrato)
  → HygieneRuleEngine (regras ENTRA-*) + IndicatorEngine
  → IProductStore.SaveRunAsync (schema v9, source=EntraId)
  → ChangeDetector diff vs run anterior → change_events (source=EntraId)
```

### 3.3 Preocupações transversais

- **Throttling:** Graph limita por app+tenant; coleta agendada deve usar `$select` estrito, `$top=999`, `$batch` para expansões (ex.: `signInActivity` vem no `$select` de `/users`, sem chamada extra) e, na E3, **delta queries** (`/users/delta`) para runs incrementais.
- **Licença como capability:** o probe grava as capacidades do tenant (P1? P2?); regras/indicadores declaram a capability exigida e caem em `CapabilityMissing` — padrão que o produto já tem para LDAP.
- **Segurança da credencial:** o app tem leitura ampla do tenant — o cert é um alvo. Chave não exportável + documentação exigindo host tratado como Tier 0 (mesma recomendação que já fazemos para a gMSA da coleta agendada).
- **Privacidade/egress:** atualizar `LOCAL_DATA_SECURITY_AND_AUTH.md` deixando explícito o novo fluxo de rede (host→Graph, TLS, somente leitura) e que continua não havendo telemetria para o fabricante.

---

## 4. Visão de Tech Lead (o "como" entregar)

### M0 — Spike (1 semana)
- Guia de onboarding do app registration (doc + tela): criar app, subir cert, admin consent com as permissões E1.
- Protótipo `GraphDirectoryProbe`: auth por cert + `GET /organization` + `/subscribedSkus` → `DirectoryProbeResult` (reusa `CapabilityState`).
- Decisões a validar no spike: custo real do SDK (tamanho/trimming no publish), tempo de coleta em tenant de ~5k usuários, formato final do `objectKey`.
- Tenant de desenvolvimento: Microsoft 365 Developer Program + tenant pago mínimo com P1 para testar `signInActivity`.

### M1 — E1: Fundação cloud (v0.8, ~4–6 semanas)
1. **Schema v9:** colunas `source`/`tenant_id` (+ índices) em `current_object_state`, `findings`, `change_events`, `indicator_results`, `runs`; tabela `tenant_config` (criptografada como o resto).
2. **Core:** `DirectoryObjectType` unificado (migração mecânica), `DirectorySource`, contratos de tenant.
3. **Infrastructure:** `GraphCollectionEngine` (users, guests, groups, applications, servicePrincipals, roleAssignments) com normalização para `CollectedObject`.
4. **Regras:** ENTRA-101/102/103/110/111/120/130/131/132 (9 regras) no `RuleCatalog` com remediação Graph PowerShell (`Update-MgUser -AccountEnabled:$false` etc., preview com `-WhatIf`).
5. **Indicadores:** secrets vencendo, guests pendentes, licenças em contas desabilitadas.
6. **UI:** página "Fontes" (cadastro/health do tenant), filtro por fonte no inventário/findings/timeline, badge de fonte.
7. **Testes:** fixtures JSON de respostas Graph (handler fake, sem rede); regras testadas como as de AD hoje; teste de integração manual contra tenant dev (runbook).

### M2 — E2: Correlação híbrida (v0.9, ~3–4 semanas)
1. `IdentityLinker` (SID → ImmutableID → UPN heurístico) + tabela `identity_links`.
2. Regras `HYB-140/141/142` + passo de avaliação híbrida pós-run.
3. Página de objeto unificada (atributos AD + Entra lado a lado quando vinculado).
4. Identity Score híbrido + Morning View unificado ("o que mudou nas duas pontas").

### M3 — E3: Postura avançada (v1.0, ~4 semanas)
1. Conditional Access + authorization policy (regras 130/131 completas), PIM elegível vs permanente, relatório de registro de MFA.
2. Delta queries na coleta agendada (custo/tempo de run agendado ↓).
3. Painel de postura consolidado (estilo secure score próprio, local).

### Riscos e mitigação

| Risco | Mitigação |
|---|---|
| Throttling em tenants grandes | SDK middleware + `$select`/`$batch`; delta na E3; `MaxObjectsPerType` já existe |
| Variação de licença entre clientes | capability-gating desde o probe (M0), nunca falhar a coleta inteira |
| Endpoints beta (ex.: alguns campos de SP sign-in) | **política: só v1.0 do Graph**; o que for beta fica fora até promover |
| Gestão de certificado pelo cliente | wizard de onboarding + health check com erro acionável ("cert expira em X dias" vira indicador do próprio produto) |
| Escopo crescer (Governance, risky users…) | fases fechadas; backlog explícito de não-objetivos (§1.6) |

---

## 5. Operação diária de IAM — da console para o produto

> Fontes: Microsoft Entra operations reference guide ([IAM](https://learn.microsoft.com/en-us/entra/architecture/ops-guide-iam), [Governança](https://learn.microsoft.com/en-us/entra/architecture/ops-guide-govern), [Autenticação](https://learn.microsoft.com/en-us/entra/architecture/ops-guide-auth), [Operações gerais](https://learn.microsoft.com/en-us/entra/architecture/ops-guide-ops)) + prática de mercado (checklists de auditoria Entra, guias de guest governance e PIM).

### 5.1 O problema que a console não resolve

Um operador de IAM que "abre a console" (entra.microsoft.com) para a ronda diária precisa visitar **pelo menos cinco áreas desconexas**:

1. **Identity → Monitoring** (audit logs, sign-in logs, provisioning logs)
2. **Identity → Hybrid management** (Entra Connect Health, erros de sync)
3. **ID Governance** (PIM: ativações e aprovações; access reviews pendentes)
4. **Enterprise apps / App registrations** (consentimentos, secrets vencendo — sem visão consolidada nativa)
5. **Protection** (Identity Protection, Conditional Access) e **Billing → Licenses** (erros de atribuição)

Não existe na console uma tela nativa que responda "**o que mudou / o que é risco / qual a próxima ação**" — a pergunta do IOC. A tese do produto na nuvem é a mesma do on-prem: **o produto é onde o operador descobre o que fazer; a console é onde ele age.** O Morning View híbrido é a materialização disso.

### 5.2 Ronda diária (mapa console → produto)

| # | Pergunta do dia | Onde na console hoje | Como o produto responde | Fase |
|---|---|---|---|---|
| 1 | A sincronização AD↔Entra está saudável? | Hybrid management / Connect Health | Indicador "idade do último sync" + `ENTRA-CFG-SYNCSTALE-132` | E1 |
| 2 | O que mudou desde ontem (users, grupos, apps, roles)? | Audit logs (filtros manuais, retenção 30 dias) | `ChangeDetector` → Timeline + Morning View com badge de fonte; retenção = a do banco local | E1 |
| 3 | Alguém ganhou role privilegiada? | Audit logs filtrado por RoleManagement / PIM audit history | `change_events` de roleAssignments com destaque Tier 0 no Morning View | E1/E2 |
| 4 | Secrets/certs de apps vencendo? | App registrations, app por app (**sem consolidação nativa** — dor real reconhecida do mercado) | Indicador "credenciais de app vencendo no horizonte" + `ENTRA-APP-SECRET-110` | E1 |
| 5 | Consentimento novo / SP com permissão alta apareceu? | Enterprise apps → Consent + audit log | `ENTRA-SP-OVERPRIV-112` + evento de mudança (novo SP/grant = change event) | E1 |
| 6 | Guests novos ou com convite pendente? | External identities | Indicador "convites pendentes" + `ENTRA-GUEST-STALE-102` | E1 |
| 7 | Licenças em erro / desperdiçadas? | Billing → Licenses (estado de erro de group-based licensing) | Indicador "licenças em contas desabilitadas"; erros de atribuição ficam no backlog | E1 |
| 8 | Ativações PIM nas últimas 24h? Elegível virou permanente? | ID Governance → PIM | Coleta de `roleEligibilitySchedules` + diff permanente vs elegível | E3 |
| 9 | Política de CA mudou? Lista de exclusão cresceu? | Protection → Conditional Access | Coleta de políticas CA + change events sobre elas (exclusão de CA que cresce = clássico ponto cego de auditoria) | E3 |
| 10 | Risky users / sign-ins arriscados? | Protection → Identity Protection (**exige P2**) | Fora do escopo até pós-E3; `CapabilityMissing` declarado na UI | pós-E3 |

Itens 1–7 são exatamente o formato do **subsistema de indicadores** que já existe (contagem + drill-down, v0.7) — a E1 entrega a ronda diária cloud quase completa reusando o motor atual.

### 5.3 Cadências semanal e mensal (alimentam regras, não indicadores)

- **Semanal:** guests inativos 90+ dias (`ENTRA-GUEST-STALE-102`), devices obsoletos (`ENTRA-DEVICE-STALE-106`), contas nunca logadas (`ENTRA-USER-NEVERLOGGED-103`), grupos vazios/sem dono, apps sem dono (`ENTRA-APP-NOOWNER-111`).
- **Mensal/trimestral:** revisão de quem tem role privilegiada (relatório Tier 0 híbrido serve de evidência para access review), erros de sync não resolvidos há >100 dias (recomendação Microsoft literal — vira regra híbrida candidata `HYB-SYNC-ERROR`), versão do Entra Connect >6 meses desatualizada (candidata a indicador E2/E3), contas de acesso de emergência: **existem 2 break-glass cloud-only com GA?** (candidata a regra `ENTRA-PRIV-BREAKGLASS-123` — checagem de *presença*, o oposto das outras: alerta se NÃO encontrar).

### 5.4 Fronteira: o que fica na console (e fora do produto)

Coerente com os invariantes (§1.4): **agir** é sempre na console ou via comando sugerido — desabilitar guest, revogar consent, rotacionar secret, aprovar ativação PIM, executar access review. O produto entrega a fila priorizada e a evidência; não executa, não aprova, não abre workflow no tenant. Aprovações/fila própria são o item "Minha Fila" do V2 on-prem e valem também para cloud quando chegarem.

### 5.5 Implicação de produto

A E1 deve ser dimensionada pela **ronda diária (§5.2, itens 1–7)**, não pelo catálogo de regras: é o que o operador sente falta todo dia às 9h. Regras de postura (GA count, user consent, CA) agregam no score, mas o hábito diário — abrir o Morning View em vez de cinco blades da console — é o que gera retenção. Recomendação de priorização dentro da E1: indicadores cloud primeiro, regras de higiene depois, postura por último.

---

## 6. Decisões pendentes do dono do produto

1. Aprovar o faseamento E1→E3 e a prioridade (E1 antes de qualquer item deferido do V2 on-prem, ex. Trusts/Sites/FSMO?).
2. Confirmar princípio "cert obrigatório, secret não suportado" (mais seguro, porém onboarding um pouco mais longo).
3. Enum unificado `DirectoryObjectType` (refactor mecânico agora) vs enum paralelo (menos churn, mais `if`s para sempre) — recomendação: unificado.
4. Nome/posicionamento: os findings cloud entram no mesmo "Direnix" ou já ancoram o rebrand IOC?
