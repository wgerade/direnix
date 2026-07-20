# Ajuda Contextual e Diretrizes de UX

O produto deve explicar conceitos operacionais sem transformar o dashboard em documentacao longa.

## Padrao de Ajuda

Usar tres niveis:

1. Tooltip curto no icone `i`.
2. Popover curto ao clicar no termo.
3. Tela `Ajuda` com glossario e explicacao operacional.

O texto curto deve caber em uma frase. O texto detalhado fica no menu `Ajuda` e nos documentos.

## Onde Mostrar `i`

Prioridade:

- modo de dados;
- cobertura;
- coleta parcial;
- `collectionId`;
- estado do finding;
- decisao;
- confianca;
- `CarriedForward`;
- `MissingInScope`;
- `CollectionFailed`;
- `NeedsRecycleBinValidation`;
- export/script/validacao.

## Linguagem

Usar texto direto, orientado a acao:

- "Nao marcar como resolvido quando o objeto ficou fora do escopo."
- "Recolete o escopo antes de decidir cleanup."
- "Valide AD Recycle Bin antes de confirmar delecao."

Evitar:

- texto promocional;
- explicacao longa dentro de tabela;
- termos genericos sem decisao operacional.

## Assistente Local

A primeira versao do assistente deve ser local e deterministica: pesquisar no glossario embarcado e devolver o conceito correspondente. Ele nao deve inventar resposta nem substituir validacao tecnica.

Futuro chatbot deve responder apenas com base em:

- glossario local;
- documentos do produto;
- estado do run;
- finding selecionado;
- runbook vinculado.

