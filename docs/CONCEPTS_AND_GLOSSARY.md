# Conceitos e Glossario Operacional

Este glossario define os termos que aparecem no portal, nos exports e nos runbooks. A regra principal e simples: a ferramenta deve separar o que foi observado, o que foi herdado de coleta anterior, o que falhou por coleta e o que realmente mudou no Active Directory.

## Termo de produto: "Avaliacao"

Na interface, no portal e nas mensagens ao usuario o termo unico e **"Avaliacao"**
(em ingles, *Assessment*) — nao "coleta". "Avaliacao" cobre o ciclo completo: ler o
AD em modo somente leitura, aplicar as regras do perfil e gravar/atualizar os riscos.
"Coleta" permanece apenas como termo tecnico interno (codigo, este glossario de ciclo
de vida e os contratos `CollectionRun`/`Collector`).

## Termos de Coleta

| Termo | Tooltip curto | Significado operacional | Acao recomendada |
| --- | --- | --- | --- |
| `Observed` | Objeto visto na coleta atual. | A coleta incluiu o escopo do objeto e ele foi retornado pelo AD ou evidencia importada. | Usar esta observacao para calcular estado atual e risco. |
| `CarriedForward` | Estado mantido de uma coleta anterior. | O objeto conhecido nao estava no escopo da coleta atual. Ele continua no dashboard, mas com data antiga. | Nao marcar como resolvido, deletado ou ausente. Coletar o escopo dele para confirmar mudanca. |
| `NotInScope` | Fora do escopo deste run. | O run foi parcial e nao prometia validar aquele objeto ou aquela OU. | Mostrar como cobertura parcial e preservar o ultimo estado conhecido. |
| `CollectionFailed` | A coleta falhou para este escopo. | Houve erro de LDAP, timeout, acesso negado, DC indisponivel, SizeLimitExceeded ou atributo essencial ausente. | Corrigir acesso/conectividade e recoletar antes de decidir cleanup. |
| `MissingInScope` | Esperado no escopo, mas nao apareceu. | O objeto estava dentro do escopo coletado e existia antes, mas nao veio no resultado atual. | Validar se foi movido, renomeado, deletado, ocultado por permissao ou afetado por erro de coleta. |

## Termos de Ciclo de Vida

| Termo | Tooltip curto | Significado operacional | Acao recomendada |
| --- | --- | --- | --- |
| `Active` | Objeto existe e esta ativo. | O objeto foi observado e nao ha evidencia de encerramento, quarentena ou remediacao. | Tratar findings abertos normalmente. |
| `StaleCandidate` | Candidato a limpeza. | O objeto parece obsoleto por ultimo logon, senha, owner ou regra de higiene. | Exportar para owner, validar dependencia e planejar disable/move. |
| `Disabled` | Objeto desabilitado. | Existe no AD, mas nao esta ativo para logon/uso normal. | Validar se esta em retencao ou se pode ir para quarentena/remocao futura. |
| `Quarantined` | Objeto movido para quarentena. | O objeto foi preservado em OU controlada enquanto aguarda retencao ou validacao. | Acompanhar prazo e owner. |
| `Moved` | GUID/SID bate, DN mudou. | O mesmo objeto foi encontrado em outro DN. | Atualizar inventario e validar se o movimento foi autorizado. |
| `Renamed` | Mesmo objeto, nome alterado. | GUID/SID bate, mas nome principal mudou. | Atualizar referencia e revisar impacto em scripts ou owners. |
| `DeletedCandidate` | Pode ter sido deletado. | O objeto sumiu do escopo, mas ainda precisa validar lixeira/retencao/evidencia. | Consultar AD Recycle Bin ou evidencia de ticket antes de fechar. |
| `DeletedConfirmed` | Delecao confirmada. | Coleta do escopo foi bem-sucedida, objeto nao aparece por GUID/SID, nao esta na lixeira e ha evidencia suficiente. | Fechar cleanup e registrar evidencia. |
| `Resolved` | Condicao corrigida. | O objeto ainda existe, mas o atributo ou configuracao problematica foi corrigido. | Manter em validacao ate a proxima coleta confirmar tendencia. |
| `Unknown` | Sem evidencia suficiente. | A ferramenta nao consegue classificar com seguranca. | Nao automatizar decisao. Coletar mais evidencia. |

## Estados de Finding

| Termo | Tooltip curto | Quando usar | Acao recomendada |
| --- | --- | --- | --- |
| `Open` | Ainda precisa de acao. | Finding valido e nao resolvido. | Priorizar por risco e owner. |
| `ValidationPending` | Correcao vista, aguardando confirmacao. | Um valor aparenta ter sido corrigido, mas precisa nova coleta ou evidencia. | Recoletar e manter evidencia. |
| `Resolved` | Resolvido com evidencia. | A condicao deixou de existir em coleta valida. | Fechar, mas manter historico. |
| `SuppressedByScope` | Suprimido pelo escopo atual. | O run nao cobriu a area do finding. | Nao alterar status final. |
| `CarriedForward` | Herdado da coleta anterior. | O objeto/finding nao foi reavaliado no run atual. | Mostrar idade do dado e escopo necessario. |
| `BlockedByCollectionError` | Bloqueado por erro de coleta. | O escopo deveria ter sido coletado, mas falhou. | Corrigir coleta antes de decidir risco. |
| `NeedsRecycleBinValidation` | Precisa validar lixeira. | Objeto sumiu de escopo valido e pode ter sido deletado. | Validar AD Recycle Bin, ticket ou log de auditoria. |
| `NeedsOwnerDecision` | Precisa decisao de owner. | Cleanup pode impactar aplicacao, usuario, servico ou processo. | Exportar para owner com prazo e decisao. |
| `AcceptedRisk` | Risco aceito temporariamente. | Owner aprovou manter risco por periodo definido. | Acompanhar vencimento e exigir renovacao ou remediacao. |

## Regra de Ouro

Ultimo run nao e igual a estado atual. O estado atual conhecido e a melhor composicao das observacoes validas mais recentes por objeto, com idade e cobertura explicitas.
