# Plano de Reformulação v2 — UX, Regras e Avaliação

Status: Proposta para aprovação
Data: 2026-06-24
Base: feedback de uso real + referencias de mercado.

> Nada implementado ainda. Documento para revisao e decisao.

## Referencias de mercado consultadas

- **Microsoft Defender for Identity / Secure Score**: recomendacoes com *severidade* (palavra),
  status (To address / Completed / **Exempted**). Excecao ("Exempt") fica **na propria recomendacao**,
  com motivo e validade; ha um filtro de status. Listas ordenaveis/filtraveis. Tema claro.
- **Semperis Purple Knight**: *Indicators of Exposure/Compromise*, severidade (Critical/Warning/Info),
  abas por categoria, score por categoria + score geral. Cada indicador tem descricao, objetos
  afetados e remediacao. Tema claro, alto contraste.
- **Tenable Identity Exposure**: *Indicators of Exposure* com severidade, filtros, detalhe com
  remediacao passo a passo. Severidade e o indicador primario (nao um numero cru).
- **PingCastle**: relatorio HTML claro; score por dominio (0-100, maior = pior); tabelas ordenaveis.

**Padroes comuns que vamos adotar:**
1. Severidade (palavra + cor) e o indicador primario nas listas; o numero fica no detalhe.
2. Tabelas sempre ordenaveis e filtraveis.
3. Excecao/aceite vive **no finding** e e visto por filtro de status — nao em "Configuracoes".
4. Configurar a varredura e simples por padrao; escopo fino fica em "Avancado".
5. Perfis/baselines selecionaveis e nomeaveis.

---

## A. Legibilidade e tema (CRITICO — bloqueia uso)

Problema: tema escuro com contraste baixo; o detalhe do risco fica ilegivel.

Proposta:
- Mudar para **tema claro profissional** (fundo claro, texto escuro, alto contraste) — padrao da
  maioria das ferramentas enterprise de identidade. (Dark de alto contraste pode virar toggle depois.)
- Tokens de cor consistentes (severidade, categoria, estados) com contraste AA.
- Reescrever o painel de detalhe (drawer) com tipografia legivel e secoes claras.

## B. Indicador de risco: severidade OU score (nao os dois juntos)

Problema: "100 · Alto" na mesma celula confunde.

Proposta (padrao de mercado):
- Na **lista**: badge colorido com a **palavra** (Critico/Alto/Medio/Baixo/Info). Sem numero.
- O **score 0-100** aparece so no **detalhe** do risco (e e usado internamente para ordenar).
- Legenda unica explicando a ordem.

## C. Tabelas: ordenar e filtrar

- Ordenacao por clique no cabecalho (severidade, categoria, indicador, objeto, status, idade).
- Filtros: severidade, categoria, status e busca por objeto/indicador.
- Paginacao para grandes volumes (ex.: 779 itens).
- Acao "exportar recorte atual" (CSV) — fase seguinte.

## D. Regras de negocio (reformular de verdade)

Problema: so da pra editar poucos thresholds globais; "regras diferentes parecem iguais";
nao da pra salvar perfil com nome.

Proposta:
- **Catalogo de regras**: lista de TODAS as regras, cada uma com:
  - ligar/desligar,
  - threshold proprio (quando aplicavel: dias, valor esperado),
  - override de severidade,
  - link para a descricao/remediacao.
- **Perfis nomeados**:
  - built-in: MicrosoftDefault, CISStrict, OperationalBalanced (somente leitura),
  - **"Salvar como..."** cria um perfil customizado **com nome** (varios podem existir),
  - editar/clonar/excluir perfis customizados.
- Cada avaliacao grava qual perfil/versao usou (rastreabilidade).

## E. Fluxo de avaliacao (resolver o Quick/Deep + tipos confusos)

Problema: profundidade Quick/Deep e checkboxes de OU/Computers/GPO confundem e ficam soltos
das regras.

Proposta (wizard guiado, simples por padrao):
1. **Alvo**: DC/dominio + validar conectividade.
2. **Perfil de regras**: escolher o perfil (D) — conecta coleta + regras num lugar so.
3. **Avancado (opcional, recolhido)**: tipos de objeto, profundidade, limites. Por padrao = completo.
4. **Executar**.

- Profundidade some da tela principal (vira "Avancado"); o padrao cobre o caso comum.
- O usuario nao precisa entender Quick/Deep para rodar.

## F. Excecoes (mover + corrigir)

- **Tirar de Configuracoes.**
- Integrar em **Riscos** como filtro de status: `Ativos | Em excecao | Resolvidos`.
- Acao "Aceitar risco" inline no detalhe (responsavel + motivo + validade).
- **Corrigir o bug**: hoje o botao nao grava. Investigar (provavel: chave do finding com
  caracteres especiais na requisicao, ou handler nao disparando). Sera validado com teste.

## G. Bugs/ajustes pontuais

- Botao de aceite/excecao nao grava (F).
- Rotulo do tile "Achados ativos" -> "Riscos ativos".
- Garantir que MAQ (quota) e demais regras aparecam (via filtro/ordenacao da secao C).
- Drawer travando (ja corrigido na 0.2.6: atributo `hidden`).

---

## Ordem de implementacao proposta

1. **Tema claro + legibilidade** (desbloqueia o uso; inclui reescrita do drawer).
2. **Tabelas ordenaveis/filtraveis + indicador de risco unico** (severidade-palavra).
3. **Excecoes**: mover para Riscos como filtro de status + **corrigir o salvar**.
4. **Regras**: catalogo por-regra + perfis nomeados (salvar como).
5. **Fluxo de avaliacao unificado** (Alvo -> Perfil -> Avancado).

> Cada etapa entra com build verificado e instalador novo. Aguardando aprovacao (total ou por etapas).
