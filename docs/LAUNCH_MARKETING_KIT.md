# Kit de Divulgação — Direnix (item 11)

Material para o lançamento. As telas você captura rodando o **modo demo** (login → "Explorar com dados de exemplo") ou o **portátil** — sem tocar em AD real. Recomendo o portátil: `DirenixPortable.exe`, clique duplo, e no login clique em "Explorar com dados de exemplo".

## Roteiro de screenshots (6 telas)

Capture em **1280×800**, tema claro (mais legível em feed), e uma versão em inglês do conjunto principal (troque o idioma no botão PT/EN do topo). Esconda a barra de tarefas e qualquer dado pessoal.

| # | Tela | Como chegar | O que mostra (a mensagem) |
|---|------|-------------|---------------------------|
| 1 | **Hoje** | Abre nela | O gancho: "o que mudou desde ontem" + indicadores. É o diferencial — a ronda diária. **Screenshot herói.** |
| 2 | **Postura** | Menu → Postura | Identity Score 71%, Tier 0 64%, métricas. O "número" que as pessoas adoram comparar. |
| 3 | **Riscos** | Menu → Riscos | Lista com severidade/categoria (krbtgt, delegação, kerberoastable…). Mostra profundidade. |
| 4 | **Remediação** | Riscos → clique num finding | Drawer com PowerShell **Pré-visualizar → Aplicar** + passo a passo. **O que nenhum concorrente grátis tem.** |
| 5 | **Relatório** | (produção) botão Relatório | HTML encaminhável com score e riscos. O artefato que viraliza por e-mail. |
| 6 | **Portátil/Demo** | Tela de login | Botão "Explorar com dados de exemplo" + menção ao portátil. Mostra o atrito zero para testar. |

**Vídeo/GIF (60–90s):** portátil abrindo → tela "Hoje" → clicar num risco → mostrar o PowerShell preview → exportar relatório. Narração curta ou legendas. Ferramentas: ScreenToGif (grátis) ou Xbox Game Bar (Win+G).

## Rascunho do post de LinkedIn (PT)

> **Cansei de descobrir tarde o que mudou no meu Active Directory.**
>
> Ferramentas de auditoria como PingCastle são ótimas — mas são uma *foto* que você tira uma vez por trimestre. O que eu queria como admin era o *filme*: abrir uma aba toda manhã e ver, em 30 segundos, **o que mudou desde ontem, o que virou risco e qual a próxima ação**.
>
> Como não existia (de graça), eu construí. Chama **Direnix**.
>
> 🔹 Ronda diária: novos membros privilegiados, senhas vencendo, contas bloqueadas, mudanças de SPN
> 🔹 Identity Score + ~40 regras de higiene, acesso privilegiado e hardening
> 🔹 Cada risco vem com a remediação em PowerShell (pré-visualiza antes de aplicar)
> 🔹 Digest matinal no e-mail/Teams depois da coleta agendada
> 🔹 100% local e read-only: seus dados nunca saem da máquina, nada é escrito no AD
>
> É **gratuito e open-source (MIT)**. Dá pra testar sem instalar nada: baixa o `DirenixPortable.exe`, clique duplo, e tem até um modo demo com dados fictícios.
>
> 👉 github.com/wgerade/direnix
>
> Feedback de quem vive de AD é ouro — me digam o que quebra e o que falta.
>
> #ActiveDirectory #IdentitySecurity #Cybersecurity #Sysadmin #OpenSource #DFIR #WindowsServer

## Rascunho (EN) — para alcance global

> **I got tired of finding out too late what changed in my Active Directory.**
>
> Point-in-time audit tools (PingCastle, Purple Knight) are great — but they're a *photo* you take once a quarter. What I wanted as an admin was the *film*: open a tab every morning and see, in 30 seconds, **what changed since yesterday, what's now a risk, and the next action**.
>
> It didn't exist for free, so I built it. It's called **Direnix**.
>
> 🔹 Daily rounds: new privileged members, expiring passwords, locked accounts, SPN changes
> 🔹 Identity Score + ~40 hygiene / privileged-access / hardening rules
> 🔹 Every risk ships with PowerShell remediation (preview before you apply)
> 🔹 Morning digest to e-mail/Teams after the scheduled collection
> 🔹 100% local and read-only: your data never leaves the machine, nothing is written to AD
>
> **Free and open-source (MIT).** Try it with zero commitment: download `DirenixPortable.exe`, double-click — there's even a demo mode with fictional data.
>
> 👉 github.com/wgerade/direnix
>
> Feedback from people who live in AD is gold — tell me what breaks and what's missing.
>
> #ActiveDirectory #IdentitySecurity #Cybersecurity #Sysadmin #OpenSource #BlueTeam #WindowsServer

## Onde mais postar (comunidades de sysadmin/identidade)
- r/sysadmin, r/activedirectory, r/AZURE (Entra), r/netsec (Saturday showcase)
- Communidades: BloodHound Slack, TrustedSec/SpecterOps Discord, PatchMyPC
- Hacker News (Show HN), lobste.rs
- Adaptar o tom por canal: LinkedIn = história; Reddit = técnico e humilde ("fiz isso, feedback?"); HN = problema→solução.

## Checklist antes de postar
- [ ] Release v0.9.0 publicada com MSI + portátil + SHA-256
- [ ] Screenshots 1–6 (PT e EN) prontas
- [ ] GIF/vídeo de 60–90s
- [ ] README com pelo menos 1 screenshot herói (tela "Hoje")
- [ ] QA de install/upgrade/uninstall em VM limpa (item 12) OK
