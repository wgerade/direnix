# Plano de Lançamento Público — Direnix (ferramenta gratuita)

Status: PROPOSTA
Data: 2026-07-20
Nome público do produto: **Direnix** (decidido em 2026-07-20; o rebrand total da base de código foi executado no mesmo dia — ver item 8a).
Objetivo: tornar o Direnix atrativo e confiável o suficiente para divulgação pública (LinkedIn, comunidades de sysadmin/identidade), maximizando o funil **baixar → testar → usar diariamente → indicar**.

Tese de posicionamento: não competir com PingCastle/Purple Knight em profundidade de auditoria pontual ("foto"); vencer na **operação diária de identidade** ("filme"): o que mudou, o que é risco, qual a próxima ação — todo dia, de graça.

---

## Visão geral das fases

| Fase | Tema | Resultado |
|---|---|---|
| v0.8 | Atração: relatório + primeiro contato | Quem baixa vê valor em 10 min e tem algo para mostrar/encaminhar |
| v0.9 | Retenção: digest diário + updates | Quem instalou volta todo dia sem precisar lembrar |
| GTM | Confiança e distribuição (paralelo) | Existe onde baixar, com assinatura, licença e material de divulgação |

As fases v0.8 e GTM podem andar em paralelo. O post de LinkedIn só sai quando **v0.8 + GTM** estiverem completos; v0.9 pode sair 2–4 semanas depois como "segunda onda" de divulgação.

---

## Fase v0.8 — Atração ("Report & First Run")

### 1. Relatório HTML exportável (item de maior impacto) — CONCLUÍDO (2026-07-20)

Um HTML autocontido (CSS inline, sem dependências externas), encaminhável por e-mail, com: Identity Score em destaque, resumo do ambiente, top riscos por severidade, indicadores operacionais, mudanças do período e metadados da coleta (data, cobertura, perfil). Rodapé com nome/versão da ferramenta e link — **o relatório é o principal veículo de divulgação orgânica**.

- Novo `Direnix.Core/Reporting/ReportBuilder.cs`: monta um modelo `ReportModel` a partir dos mesmos contratos usados pelo dashboard (`DashboardContracts`), renderiza via template string (sem lib de template — manter zero dependências novas).
- Novo endpoint em `Direnix.Service/Endpoints/` (`ReportEndpoints.cs`): `GET /api/v1/reports/summary?format=html` (e `format=csv` para findings/inventário). Protegido por sessão, auditado via `PortalAudit`.
- UI: botão "Exportar relatório" no topo do Painel + "Exportar CSV" nas tabelas de Riscos e Inventário.
- Critério de aceite: abrir o HTML num Outlook/Gmail encaminhado e continuar legível; CSV abre no Excel com acentuação correta (BOM UTF-8).
- Esforço: M (3–5 dias).

### 2. Reposicionar a UI na ronda diária ("Hoje" primeiro) — CONCLUÍDO (2026-07-20)

- Nova view inicial **"Hoje"**: Morning View + indicadores operacionais em tela cheia, com data/hora da última coleta e CTA claro quando não houver coleta agendada.
- O atual "Painel" (score, postura, top riscos) vira a segunda aba ("Postura").
- Renomear navegação para a linguagem da operação diária; manter i18n PT/EN.
- Estados vazios que ensinam: 1ª coleta → "sua timeline nasce amanhã, na 2ª coleta — agende agora" com botão direto para agendamento; sem indicadores → link para ativá-los no perfil.
- Esforço: M (2–4 dias, só `wwwroot`).

### 3. Primeira coleta que impressiona sozinha — CONCLUÍDO (2026-07-20)

- Garantir que score + findings + indicadores aparecem com força na 1ª coleta (a timeline é a única coisa que exige 2).
- Ao fim da 1ª coleta, tela de resultado: "Encontramos N riscos, seu score é X — exporte o relatório / agende a coleta diária" (conecta os itens 1 e 2).
- Remover campo "Operador" do wizard (usar o usuário da sessão — hoje `AuthEndpoints` já dá a identidade).
- Esforço: S (1–2 dias).

### 4. Modo demonstração (ver sem apontar para o AD real) — CONCLUÍDO (2026-07-20)

Implementação: fixture frontend (`wwwroot/demo-data.js`, domínio fictício `corp.exemplo.local`) interceptada no `getJson` — em vez de data root temporário — para zero risco ao banco real; botão no login, banner fixo "MODO DEMO", mutações bloqueadas com aviso.

- Fixture de dados sintéticos (domínio fictício com ~500 objetos, riscos e 2 "coletas" para a timeline ter conteúdo) embutida no binário.
- Botão discreto no login/primeiro uso: "Explorar com dados de exemplo" → carrega num data root temporário isolado (o service já aceita `--Direnix:Storage:DataRoot`), banner permanente "MODO DEMO" e botão sair.
- Serve também para screenshots/vídeo do GTM sem expor dados reais.
- Esforço: M (2–4 dias).

### 5. (Opcional, avaliar depois de 1–4) Modo portátil "avaliação única"

Um `DirenixPortable.exe` (mesmo Service em modo console, data root em `%LOCALAPPDATA%`, abre o browser, não instala serviço) para reduzir o atrito do primeiro contato ao nível do PingCastle. Adiar se o modo demo + MSI assinado se mostrarem suficientes.

- Esforço: M–L (3–6 dias, novo profile de publish + ajustes de bootstrap/auth para modo efêmero).

---

## Fase v0.9 — Retenção ("Digest & Updates")

### 6. Digest matinal (e-mail + webhook)

A feature de retenção: o resumo do Morning View chega ao admin, em vez de ele precisar lembrar de abrir o portal.

- Novo `Direnix.Core/Notifications/`: contrato `INotificationChannel` + montagem do digest (reusa o `ReportModel` do item 1 em versão compacta).
- Canais: **SMTP** (host/porta/TLS/credencial — credencial protegida via DPAPI, mesmo padrão do storage) e **webhook genérico** (POST JSON — cobre Teams/Slack/automations).
- Disparo: ao fim da coleta agendada (`ScheduledCollectionService`), com política "enviar sempre" ou "só quando houver mudanças/indicadores acima de zero".
- UI: seção "Notificações" na aba Operação, com botão "enviar teste".
- Critério de aceite: coleta agendada às 02:00 → e-mail na caixa às 02:0x com mudanças/indicadores e link para o portal.
- Esforço: M–L (4–6 dias).

### 7. Verificação de atualização

- `GET` na API de releases do GitHub (1x/dia, opcional/desligável, sem telemetria — só leitura da versão mais recente) → badge "nova versão disponível" no rodapé da sidebar com link para a página de release.
- Importante: falha de rede é silenciosa; ambientes sem internet não veem nada.
- Esforço: S (1 dia).

---

## Fase GTM — Confiança e distribuição (paralela ao código)

### 8a. Rebranding para Direnix — CONCLUÍDO (2026-07-20)

Rebrand **total** executado de uma vez (decisão do dono: o nome antigo não deve existir em lugar nenhum):

- Pastas, projetos, solution e namespaces: `Direnix.Core` / `Direnix.Infrastructure` / `Direnix.Service` / `Direnix.Core.Tests` / `Direnix.Product.sln` / `Direnix.Msi`.
- MSI/WiX: `ProductName` "Direnix", Windows Service `Direnix.Service`, pastas `%ProgramFiles%\Direnix` e `%ProgramData%\Direnix`, atalho "Direnix Portal". O `UpgradeCode` foi **preservado** — instalar o Direnix remove automaticamente instalações da marca antiga (o banco local antigo em `%ProgramData%` não migra; ambientes de teste recomeçam do zero).
- Portal: título, marca ("DX"), i18n; seção de configuração renomeada (`--Direnix:*`).
- Docs renomeados (`DIRENIX_QA_MATRIX.md` etc.) e conteúdo substituído em toda a base, incluindo `legacy/`.

Pendências desta frente:

- **Domínio e handles**: registrar `direnix.com` (ou `.io`) e verificar disponibilidade do nome no GitHub antes de criar o repo público (item 9).
- Artefatos binários antigos (MSIs compilados com a marca anterior) e o nome da pasta raiz do repositório — ver decisões pendentes.

### 8b. Assinatura de código do MSI

- Azure Trusted Signing (caminho mais barato para indie) integrado ao build do `Direnix.Msi.wixproj`; assinar também o `Direnix.Service.exe`.
- Critério de aceite: instalar em VM limpa sem aviso vermelho do SmartScreen.
- **Bloqueador absoluto do lançamento.**

### 9. Repositório público + licença + releases

- Decisão do dono pendente: **modelo de "totalmente livre"** — (a) open-source (MIT/Apache-2.0) ou (b) freeware de binário com repo público só de docs/issues. Recomendação: decidir antes de criar o repo, pois muda o README.
- GitHub: releases versionadas com changelog, MSIs saem da raiz do repo (ficam só nas releases), Issues como canal de feedback.
- README público **em inglês** com screenshots (usar o modo demo), quickstart de 5 passos e requisitos mínimos.

### 10. Página "Security & Privacy" + requisitos

- Condensar `LOCAL_DATA_SECURITY_AND_AUTH.md` e `COLLECTION_ACCESS_AND_SCOPE.md` em uma página pública curta: read-only, LDAPS por padrão, dados 100% locais (SQLCipher + DPAPI), credencial nunca persistida, zero egress/telemetria, o que a conta de leitura precisa, portas usadas, onde instalar (não precisa ser DC).
- É o principal argumento de confiança para o público-alvo.

### 11. Material de divulgação

- 4–6 screenshots (modo demo, EN e PT) + GIF/vídeo de 60–90s: instalar → coletar → "Hoje" mostra o que mudou → exportar relatório.
- Landing de 1 página (pode ser o próprio README ou GitHub Pages): promessa "<30s para saber o que mudou, o que é risco e qual a próxima ação", contraste explícito "auditoria pontual vs operação diária", CTA de download.
- Post de LinkedIn: história ("ferramenta que eu queria ter como admin"), vídeo, link.

### 12. QA de release

- Executar a matriz de `DIRENIX_QA_MATRIX.md` + install/upgrade/uninstall em VM limpa (0.7.x → 0.8 com preservação do banco).
- Smoke test do relatório e do modo demo em cada release candidate.

---

## Sequência sugerida

```
Semana 1–2: item 1 (relatório) + itens 8a+8b (rebrand Direnix + signing, paralelo)
Semana 2–3: itens 2+3 (UI "Hoje" + first-run) + item 9 (repo/licença — decisão do dono)
Semana 3–4: item 4 (demo) + itens 10–11 (security page, screenshots, vídeo, landing)
Semana 4:   item 12 (QA em VM) → release v0.8 assinada → LANÇAMENTO
Semana 5–6: itens 6–7 (digest + updates) → release v0.9 → segunda onda de divulgação
```

## Decisões pendentes do dono

1. Licença: open-source (MIT/Apache-2.0) vs freeware de binário.
2. Incluir o modo portátil (item 5) na v0.8, v0.9 ou descartar.
3. Digest: só SMTP na v0.9 ou SMTP + webhook juntos.
4. TLD/domínio do Direnix (`.com`/`.io`) e nome do repositório GitHub.

Decidido: nome público = **Direnix** (2026-07-20).

## Fora de escopo deste plano

- Entra ID (plano próprio em `ENTRA_ID_INTEGRATION_PLAN.md`, fases E1–E3) — não é pré-requisito de lançamento.
- V2/V3 do PRD (fila/aprovações, correlação, IA, multi-fonte).
- Ampliar o catálogo de regras para competir em profundidade de auditoria — explicitamente não é a estratégia.
