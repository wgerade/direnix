# Modelo de Ciclo de Vida dos Dados

O portal deve exibir o estado atual conhecido do ambiente, nao apenas o ultimo run. Isso e essencial porque uma coleta pode ser total hoje, parcial amanha e importada por evidencia depois.

## Entidades

### `CollectionRun`

Representa uma execucao de coleta ou importacao.

Campos minimos:

- `runId`
- `collectionId`
- `collectionType`: `Full`, `Scoped`, `FeatureOnly`, `ImportedEvidence`
- `coverageMode`: `StandardOrFull`, `Partial`, `NoDirectory`
- `startedAt`
- `completedAt`
- `domainDn`
- `searchBase`
- `objectTypes`
- `featurePacks`
- `collectorVersion`

### `ObjectObservation`

Representa um objeto observado em um run.

Campos minimos:

- `runId`
- `collectionId`
- `objectKey`
- `objectGuid`
- `objectSid`
- `distinguishedName`
- `objectType`
- `attributes`
- `attributeHash`
- `observedAt`
- `scopeMatched`
- `observationState`

### `CurrentObjectState`

Visao derivada com um registro por objeto conhecido.

Campos minimos:

- `objectKey`
- `firstSeenAt`
- `lastSeenAt`
- `lastObservedRunId`
- `lastCollectionId`
- `observationState`
- `lifecycleState`
- `dataFreshness`
- `requiredScopeForRefresh`

## Regra de Coleta Parcial

Exemplo:

- ontem houve coleta full do dominio;
- hoje houve coleta apenas de `OU=Servers`;
- o dashboard continua mostrando todos os objetos conhecidos;
- objetos de `OU=Servers` usam a observacao de hoje;
- objetos fora de `OU=Servers` ficam como `CarriedForward`;
- nenhum objeto fora do escopo vira `Resolved`, `DeletedCandidate` ou `DeletedConfirmed` por causa do run parcial.

## Classificacao de Ausencia

Quando um objeto esperado no escopo nao aparece:

1. Se houve erro de coleta, marcar `CollectionFailed` e `BlockedByCollectionError`.
2. Se GUID/SID aparece em outro DN, marcar `Moved` ou `Renamed`.
3. Se ainda existe mas esta disabled/quarantine, marcar `Disabled` ou `Quarantined`.
4. Se aparece em `CN=Deleted Objects`, marcar `DeletedCandidate` e `NeedsRecycleBinValidation`.
5. Se nao aparece por GUID/SID, nao aparece na lixeira e a coleta foi valida, marcar `DeletedConfirmed`.

## Indicadores Obrigatorios

- data da ultima observacao por objeto;
- idade do dado por objeto;
- modo do dataset: real, run local ou demo;
- cobertura do run: total, parcial ou sem diretorio;
- tempo desde ultima coleta full;
- objetos `CarriedForward`;
- objetos `MissingInScope`;
- findings bloqueados por erro de coleta;
- findings aguardando validacao de lixeira;
- findings resolvidos desde a ultima coleta valida.

## Regras de Resolucao de Riscos

Um risco (finding) so muda de **Ativo** para **Resolvido** quando foi efetivamente
reverificado. A regra, em linguagem simples:

1. A 1a avaliacao encontra o problema -> o risco fica **Ativo**.
2. Se o tipo de objeto **nao foi avaliado** de novo (porque nao estava no escopo /
   perfil), o risco **continua Ativo** (carry-forward). **Nunca** vira Resolvido por
   ausencia de coleta.
3. Se o tipo de objeto **foi avaliado** de novo e o problema **nao aparece mais**, o
   risco vira **Resolvido**, sempre com uma **justificativa** (codigo estavel,
   localizado na interface):
   - `Fixed` (Corrigido) — o objeto ainda existe, mas a condicao do risco deixou de
     se aplicar (ex.: conta desabilitada, senha rotacionada, membro removido do grupo).
   - `RemovedInRecycleBin` (Removido — na Lixeira do AD) — o objeto desapareceu do
     diretorio e foi encontrado em `CN=Deleted Objects` (recuperavel).
   - `RemovedConfirmed` (Removido — confirmado) — o objeto desapareceu e **nao** foi
     encontrado na Lixeira (ou a Lixeira nao pode ser lida).

Observacoes:

- Excecoes/aceites de risco (`AcceptedRisk`) nunca sao resolvidos nem reativados
  automaticamente.
- Na **reavaliacao dirigida** (Reavaliar selecionados), a resolucao fica restrita aos
  objetos escolhidos; demais riscos do mesmo tipo seguem como estao.
- Contas/grupos **internos do Windows** (RID < 1000: Administrator-500, Guest-501,
  grupos internos) sao ignorados pelas regras de higiene/privilegio: nao podem ser
  removidos e seu estado e por design. Regras especificas (rotacao do krbtgt) tratam
  esses objetos deliberadamente.

## Validacao da Lixeira do AD

A cada avaliacao o coletor le, em modo somente leitura:

- o container `CN=Deleted Objects` (control *Show Deleted Objects*,
  OID `1.2.840.113556.1.4.417`) para obter os GUIDs de objetos deletados;
- no Configuration NC, se a **Lixeira do AD** esta habilitada
  (`msDS-EnabledFeature` em `CN=Partitions`) e a janela de retencao
  (`msDS-DeletedObjectLifetime`, com fallback para `tombstoneLifetime`).

Esses dados alimentam:

- a **classificacao de remocao** na resolucao (na Lixeira vs confirmado);
- a regra **`ADGOV-RECYCLEBIN-001`** (Lixeira desabilitada ou retencao abaixo do
  minimo configurado no perfil, `recycleBinMinRetentionDays`, padrao 180 dias).

Se o coletor nao tiver permissao para ler essas areas, gera um aviso (warning) e o run
segue; nesse caso a regra de Lixeira nao gera achado (evita falso positivo) e remocoes
caem para `RemovedConfirmed`.

