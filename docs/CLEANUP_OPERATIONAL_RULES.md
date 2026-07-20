# Regras Operacionais de Cleanup

Estas regras controlam a tomada de decisao do portal. Elas evitam falsos positivos perigosos, principalmente em coletas parciais.

## Demo Nunca Entra na Vida Real

O dataset demo so pode ser usado quando nao houver dado real ou run local. Em operacao real, o portal deve preferir:

1. `data/direnix-store.adcx`;
2. ultimo `output/runs/*`;
3. demo, apenas como fallback visual explicito.

Quando o modo for demo, a UI deve mostrar isso claramente.

## Estado Atual Conhecido

O dashboard principal deve usar a visao consolidada:

```text
estado atual conhecido = ultima observacao valida por objeto
```

O run especifico deve existir como modo de analise/auditoria, mas nao deve substituir a visao operacional consolidada.

## Nunca Resolver Por Falta de Escopo

Se um objeto/finding nao foi coletado porque estava fora do escopo:

- usar `CarriedForward`;
- preservar ultimo risco conhecido;
- mostrar a data da ultima observacao;
- nao marcar como resolvido;
- nao marcar como deletado;
- nao reduzir risco automaticamente.

## Nunca Deletar Sem Validacao

Um objeto que sumiu do escopo valido deve passar por validacao antes de virar deletado confirmado:

- confirmar sucesso da coleta;
- tentar localizar por GUID/SID;
- validar movimento/renomeacao;
- validar AD Recycle Bin quando disponivel;
- validar ticket/log de mudanca;
- respeitar janela minima de retencao.

## Diferenciar Erro de Mudanca Real

`CollectionFailed` nao e remediacao. `MissingInScope` nao e delecao. `Resolved` exige evidencia de correcao.

## Acoes Operacionais

Cada finding deve orientar uma acao:

- `Recoletar`;
- `Validar permissao`;
- `Validar lixeira`;
- `Exportar para owner`;
- `Aguardar retencao`;
- `Gerar script revisavel`;
- `Aceitar risco com vencimento`;
- `Fechar como resolvido`.

## Remediacao

Scripts gerados pelo portal devem permanecer revisaveis e preferir `WhatIf`/preview. Remocao definitiva exige validacao explicita de lixeira, retencao e owner.

