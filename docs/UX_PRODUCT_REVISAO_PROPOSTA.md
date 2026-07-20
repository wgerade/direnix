# Proposta de Revisão de UX e Produto — Direnix

Status: Proposta para aprovação
Data: 2026-06-24
Objetivo: alinhar nomenclatura, modelo de severidade, ação operacional, categorias,
configuração de regras, exceções e o conteúdo do painel — antes de implementar.

> Nada deste documento foi implementado ainda. É para revisão e decisão.

---

## 1. Vocabulário (nomes mais profissionais, alinhados ao mercado)

Referências de mercado: PingCastle ("Health Check", "Risk Rules"), Purple Knight/Semperis
("Indicators of Exposure"), Tenable Identity Exposure ("Indicadores de Exposição",
"Deviances"), Microsoft Defender for Identity ("posture assessments", "recommendations",
"Secure Score").

| Hoje | Problema | Opções de mercado | Recomendação |
| --- | --- | --- | --- |
| Coleta / Nova coleta | técnico, genérico | Avaliação, Análise, Varredura, Assessment | **Avaliação** — botão "Nova avaliação" |
| Achados | vago | Riscos, Exposições, Indicadores de Exposição, Recomendações | **Riscos** (itens = "indicadores de exposição") |
| Painel | ok | Visão geral, Panorama | manter **Painel** (ou "Visão geral") |
| Inventário | ok (mercado usa) | — | manter |
| Operação | ok | Saúde / Diagnóstico | manter |
| Coluna "Regra" | mostra um código (ex. ADCLN-USER-STALE-001), não uma regra legível | Indicador, Verificação, Controle | **Indicador** (texto legível; código fica no detalhe) |
| Coluna "Decisão" | confuso | Ação recomendada, Recomendação, Tratamento | **Ação recomendada** |

Decisão pendente sua: confirmar **"Avaliação"** e **"Riscos"** (ou escolher outra das opções).

---

## 2. Severidade + número (a confusão do "Alto/Crítico" e do "100/90")

Hoje aparecem dois indicadores juntos sem explicação. Esclarecimento e proposta:

- **Escala de severidade (padrão de mercado, do maior para o menor):**
  `Crítico > Alto > Médio > Baixo > Informativo`. Ou seja, **Crítico é maior que Alto.**
- **O número (0–100) é o "Risco de negócio"** (businessRiskScore): severidade técnica +
  criticidade do ativo. É o que ordena a lista.

Proposta de exibição (uma coisa só, clara):
- Uma coluna **"Risco"** com uma barra/etiqueta colorida + número: ex. `■ 92 · Crítico`.
- Ordenação sempre por risco (maior no topo).
- Legenda fixa com as faixas (90–100 Crítico, 70–89 Alto, 40–69 Médio, 10–39 Baixo, 0–9 Info).
- Cores consistentes (vermelho→cinza) para reforçar a ordem visualmente.

---

## 3. Colunas dos Riscos (ex-"Achados")

Proposta de colunas:

| Coluna | Conteúdo |
| --- | --- |
| Risco | `92 · Crítico` (barra colorida) |
| Categoria | Higiene / Acesso Privilegiado / Endurecimento / Infraestrutura / Governança |
| Indicador | nome legível (ex. "Membro de grupo privilegiado Tier 0"); código no detalhe |
| Objeto | objeto afetado (sAMAccountName / DN) |
| Ação recomendada | verbo curto (Ajustar, Limpar, Implementar, Investigar) + selo de framework |
| Status | Novo / Aberto / Recorrente / Exceção / Resolvido |

Clique na linha → **painel de detalhe** (seção 5).

---

## 4. Categorias dos riscos

Mercado costuma agrupar em poucas categorias (PingCastle usa 4; Purple Knight 5).
Proposta (nomes + mapeamento das regras atuais):

| Categoria | O que entra |
| --- | --- |
| **Higiene** (limpeza) | contas/computadores obsoletos, objetos desabilitados retidos, OUs vazias |
| **Acesso Privilegiado** (segurança) | membros Tier 0, delegação irrestrita, DCSync, AdminSDHolder, krbtgt |
| **Endurecimento** (hardening) | MachineAccountQuota, LDAP signing, Kerberos preauth/DES, GPO baseline |
| **Infraestrutura & Saúde** (infra) | replicação, DCs, SYSVOL/DFSR |
| **Governança** (operação) | dono ausente, exceções vencidas, documentação |

Equivalência com o que você falou: Limpeza→Higiene, Segurança→Acesso Privilegiado+Endurecimento,
Infra→Infraestrutura, Operação→Governança. (Nomes ajustáveis.)

---

## 5. Ação operacional — o coração da revisão

Cada risco deve dizer **o que fazer**, em dois formatos, no painel de detalhe:

1. **Passo a passo manual** (para quem usa o console):
   - ex.: "Abrir *Usuários e Computadores do AD* → localizar `OU=...` → botão direito no objeto →
     Propriedades → aba Conta → marcar/desmarcar X → Aplicar."
2. **Comando PowerShell** com cmdlets nativos do AD (módulo `ActiveDirectory`), parametrizado
   com o objeto e **em modo revisão/`-WhatIf`** (nunca executa sozinho — princípio do produto):
   - ex.: `Set-ADUser -Identity "krbtgt" -WhatIf` / `Set-ADDomain -Identity wsg.local -Replace @{...} -WhatIf`.
   - botão "copiar".

E o **contexto de boas práticas**, sucinto, como selos:
- **MITRE ATT&CK** (ex. `T1078`, `T1098`, `T1558.003` Kerberoasting),
- **CIS Controls** (ex. `CIS 5.3`),
- **NIST 800-53** (ex. `AC-2`, `AC-6`),
- referência Microsoft (link curto).

Para suportar isso, cada regra ganha campos novos no catálogo:
`category`, `mitre[]`, `cis[]`, `nist[]`, `remediationManual[]`, `remediationPowerShell`.

---

## 6. Regras de negócio configuráveis pelo usuário final

Tela de **Configurações → Regras** (persistido no banco, em `app_settings`):
- Perfil de política: `MicrosoftDefault` / `CISStrict` / `OperationalBalanced` / `Custom`.
- Thresholds editáveis: "conta sem uso há **X** dias" (90 padrão), computador (90),
  objeto desabilitado retido (180), krbtgt (180), MachineAccountQuota esperado (0), etc.
- Cada avaliação registra qual perfil/threshold foi usado (rastreabilidade).
- Opcional: sobrescrever thresholds por avaliação.

---

## 7. Exceções (aceite de risco simples)

Modelo leve, do jeito que você pediu (flag + validade):
- Em qualquer risco: botão **"Marcar exceção"** → form simples (responsável, motivo, **validade**:
  30/90/180/365 dias ou data).
- Risco em exceção sai dos "ativos" e aparece como **Exceção** (com data de expiração).
- Na expiração, **reativa automaticamente** (vira "Recorrente"/"Aberto").
- Lista de exceções vencidas/perto de vencer (regra de governança `ADGOV-EXPIRED-002`).

---

## 8. Painel = só visão dos dados (remover info de produto)

Remover do Painel o que é "sobre o produto/instalação":
- bloco "Fluxo do produto" (Instalação / Storage / Identidade / Coleta),
- caminho do banco, nome do serviço, etc.

Manter / colocar no Painel só **dados do ambiente**:
- Score geral do ambiente + faixa (Healthy/Managed/Needs Attention/High/Critical),
- riscos por severidade (e por categoria),
- top riscos de negócio,
- resumo de inventário,
- tendência entre avaliações (novo/resolvido/recorrente),
- exceções vencendo.

Info de produto/serviço/banco vai para **Operação** e **Configurações**.

---

## 9. Itens a verificar durante a implementação

- **Achado de quota não aparece:** hipótese = dispara, mas com risco 70 fica abaixo dos de
  privilégio (risco 100) na lista; confirmar e, se for o caso, garantir visibilidade
  (filtro por categoria/severidade resolve). Verificar também se o objeto de domínio é
  coletado em toda avaliação real.
- Mensagem de erro de conexão genérica ("LDAP server unavailable") → separar em
  "DNS não resolveu / porta fechada / falha TLS / falha de credencial".

---

## 10. Ordem de implementação sugerida (quando aprovado)

1. Renomear vocabulário + Painel só-dados (rápido, alto impacto visual).
2. Coluna de risco unificada + categorias + ordenação/filtros.
3. Catálogo de regras estendido (frameworks + remediação) e painel de detalhe com
   passo a passo + PowerShell.
4. Configurações de thresholds/perfil.
5. Exceções com validade.
6. Verificações da seção 9.

> Aguardando sua aprovação (total ou por partes) antes de codar.
