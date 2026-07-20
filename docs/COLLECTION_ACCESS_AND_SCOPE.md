# Requisitos de Acesso, Coleta e Escopo

Status: Draft v0.1  
Data: 2026-06-20  
Publico: Arquitetura, Desenvolvimento, Seguranca, AD Engineering, QA

## 1. Objetivo

Definir quais niveis de acesso sao necessarios para coletar dados de Active Directory, como o engenheiro seleciona escopo, quais pacotes de coleta existem, como tratar permissoes ausentes e como evitar que uma coleta de postura vire uma acao insegura.

## 2. Principios

1. Coleta read-only por padrao.
2. Nao exigir Domain Admin para o fluxo normal.
3. Separar usuario do app local de credencial usada para consultar AD.
4. Nao persistir credencial AD.
5. Degradar por capacidade: se uma permissao ou ferramenta falta, marcar `capabilityMissing`.
6. O usuario deve escolher escopo antes de coletar.
7. Coleta profunda deve ser explicita.
8. Remediacao fica fora da coleta.
9. Cleanup deve ser suportado por evidencia suficiente, nao por um unico atributo.
10. Identidade hibrida sem evidencia cloud suficiente deve ser marcada como `NeedsEvidence`.

## 3. Niveis de acesso de coleta

### 3.1 Nivel A - Domain Read Basico

Uso:

- Inventario basico.
- Usuarios.
- Computadores.
- Grupos.
- OUs.
- Atributos comuns.
- Duplicidade de UPN/proxy/SPN quando legivel.

Permissao:

- Conta autenticada no dominio.
- Leitura LDAP comum.

Nao cobre bem:

- Event logs remotos.
- WMI/CIM em DCs.
- Algumas ACLs protegidas.
- Dados locais de servidores.

### 3.2 Nivel B - Security Posture Reader

Uso:

- ACLs.
- Delegacoes.
- AdminSDHolder.
- DCSync exposure.
- Membership efetivo de grupos grandes.
- GPO permissions.
- SYSVOL read.

Permissao recomendada:

- Conta dedicada de leitura.
- Delegacao de leitura em objetos AD relevantes.
- Acesso de leitura a SYSVOL.
- Capacidade de ler security descriptors.

Observacao:

- Deve ser desenhada como role delegada, nao como Domain Admin.

### 3.3 Nivel C - DC Health Reader

Uso:

- Replicacao.
- DFSR/SYSVOL events.
- Directory Services events.
- DNS/DC health.
- WMI/CIM em DCs.
- `repadmin`, `dcdiag`, `nltest` quando disponiveis.

Permissao recomendada:

- Event Log Readers nos DCs, quando suficiente.
- Permissao remota WMI/CIM quando necessaria.
- Leitura de shares administrativos somente se aprovada.

Observacao:

- Algumas validacoes podem exigir privilegio local no DC ou execucao em um servidor autorizado. O produto deve explicar a limitacao.

### 3.4 Nivel D - Hybrid/Identity Infrastructure Reader

Uso:

- Entra Connect local.
- ADFS.
- ADCS.
- Servidores de identidade.
- Logs locais e configuracoes acessiveis.

Permissao recomendada:

- Leitura local nos servidores relevantes.
- Event Log Readers.
- Acesso aos arquivos/logs de produto quando permitido.

### 3.5 Nivel E - Remediation Executor

Uso:

- Alterar AD, GPO, ACL, atributos ou configuracoes.

Status no MVP:

- Fora do fluxo normal.
- Apenas gerar recomendacao e WhatIf quando aplicavel.
- Execucao real requer aprovacao e perfil separado em fase futura.

## 4. Credenciais

### 4.1 Modos de execucao

| Modo | Descricao | Persistencia |
| --- | --- | --- |
| Current Windows Identity | Usa usuario atual | Nenhuma |
| Prompt Credential | Solicita credencial para o run | Nao persistir |
| Scheduled Credential | Futuro; exige desenho especifico | Nao no MVP |
| Service Account/gMSA | Futuro para operacao recorrente | Requer arquitetura propria |

### 4.2 Regras

- Nao gravar senha.
- Nao gravar token.
- Nao gravar hash reutilizavel de credencial AD.
- Registrar apenas o identificador saneado do principal executor.
- Separar credencial do app local da credencial AD.

## 5. Seletor de escopo de coleta

O cliente deve oferecer seletores flexiveis antes de executar:

### 5.1 Escopo de diretorio

- Floresta.
- Dominio.
- OU include.
- OU exclude.
- Site.
- Domain Controller preferencial.
- Trusts incluidos/excluidos.

### 5.2 Tipos de objeto

Checkboxes:

- Users.
- Computers.
- Groups.
- Service Accounts.
- gMSA/sMSA.
- OUs.
- GPOs.
- Domain Controllers.
- Sites/Subnets.
- Trusts.
- Foreign Security Principals.
- Contacts.
- ADCS/ADFS/Entra Connect assets quando detectados.

### 5.3 Pacotes de feature

Checkboxes:

- Inventory.
- Cleanup Hygiene.
- Privileged Access.
- ACL and Delegation.
- Authentication Hardening.
- Service Accounts.
- GPO and Baseline.
- Replication and DC Health.
- SYSVOL/DFSR.
- Trusts and Legacy Domains.
- Hybrid Identity Readiness.
- Governance and Ownership.
- Evidence Integrity.

### 5.4 Nivel de profundidade

| Nivel | Uso | Caracteristica |
| --- | --- | --- |
| Quick | Primeiro diagnostico | Poucos atributos, rapido, baixo impacto |
| Standard | Assessment normal | Regras principais e historico |
| Deep | Investigacao completa | ACLs, eventos, grupos grandes, GPO detalhado |
| Forensic | Futuro | Coleta extensa, exige aprovacao e janela |

### 5.5 Filtros operacionais

- Somente habilitados.
- Incluir desabilitados.
- Idade minima em dias.
- Somente objetos com owner ausente.
- Somente objetos privilegiados.
- Somente objetos com findings anteriores.
- Limite maximo de objetos por pacote.
- Janela de eventos em dias.
- Excluir OUs sensiveis ou fora de escopo.
- Modo somente verificacao de ambiente.
- Incluir/excluir identidades sincronizadas.
- Exigir evidencia hibrida antes de sugerir cleanup.
- Exportar somente candidatos de alta confianca.

## 6. Verificacao de ambiente

Antes de coletar, o produto deve validar:

- Conectividade LDAPS por padrao.
- LDAP 389 somente como excecao explicita e sinalizada.
- Identidade atual.
- Dominio/floresta detectados.
- Ferramentas opcionais presentes.
- Permissoes estimadas.
- Acesso a SYSVOL.
- Acesso a Event Logs quando selecionado.
- Acesso WMI/CIM quando selecionado.
- Caminho de output/banco.
- Estado de criptografia do banco.
- Espaco em disco.
- Porta local do dashboard.
- Estado do AD Recycle Bin.
- `tombstoneLifetime`.
- `msDS-deletedObjectLifetime` quando acessivel.
- Indicios locais de Entra Connect/ADSync quando pacote hibrido estiver selecionado.

Resultado da verificacao:

| Estado | Significado |
| --- | --- |
| `Ready` | Pode executar |
| `ReadyWithWarnings` | RootDSE OK, mas pode executar com cobertura parcial |
| `Blocked` | Nao deve executar ate corrigir conectividade, TLS, credencial ou RootDSE |
| `CapabilityMissing` | Pacote ou regra especifica nao podera medir |
| `NeedsEvidence` | Coleta basica executou, mas falta evidencia para decisao operacional segura |

## 7. Impacto e performance

Requisitos:

- LDAP paginado.
- Range retrieval para grupos grandes.
- Atributos selecionados por pacote.
- Timeouts configuraveis.
- Checkpoint por pacote.
- Cancelamento seguro.
- Retomada ou run parcial marcado corretamente.
- Limite de concorrencia configuravel.
- Logs por etapa.

Metas iniciais:

- Quick: diagnostico em minutos.
- Standard: adequado para 100k objetos em ate 60 minutos em ambiente saudavel.
- Deep: sem SLA rigido; deve mostrar estimativa e progresso.

## 8. Mapeamento pacote x acesso

| Pacote | Nivel minimo | Nivel recomendado |
| --- | --- | --- |
| Inventory | A | A |
| Cleanup Hygiene | A | A/B |
| Privileged Access | A | B |
| ACL and Delegation | B | B |
| Authentication Hardening | A | B |
| Service Accounts | A | B |
| GPO and Baseline | A + SYSVOL read | B |
| Replication and DC Health | A | C |
| SYSVOL/DFSR | A + SYSVOL read | C |
| Trusts and Legacy Domains | A | B |
| Hybrid Identity Readiness | A | D |
| Governance and Ownership | A | B |
| Decision Support for Cleanup | A | A/B/D para hibrido |
| SIEM/Event Evidence Reference | A | C/D ou import externo |

## 9. Saida da coleta

Cada run deve registrar:

- Escopo selecionado.
- Objetos incluidos/excluidos.
- Pacotes selecionados.
- Profundidade.
- Identidade de execucao saneada.
- Capacidades presentes/ausentes.
- Ferramentas usadas.
- Erros por pacote.
- Tempo por pacote.
- Quantidade de objetos lidos.
- Quantidade de findings.
- Hash de evidencias.
- Estado de AD Recycle Bin e retencao.
- Evidencias usadas para classificacao de confianca.
- Lacunas de evidencia que impedem cleanup direto.
- Referencias externas recomendadas para coleta manual/SIEM.

## 10. UX do seletor de coleta

Fluxo recomendado:

```text
1. Apontar AD / Domain Controller
2. Validar conectividade segura por LDAPS
3. Descobrir dominio e naming context
4. Escolher escopo
5. Escolher tipos de objeto
6. Escolher pacotes
7. Escolher profundidade
8. Verificar ambiente
9. Iniciar avaliacao somente se o alvo estiver pronto
```

Requisitos de interface:

- Campo para hostname/FQDN/IP do DC ou endpoint AD.
- Protocolo LDAPS/LDAP com LDAPS 636 como padrao seguro.
- Botao de validacao de conectividade antes do seletor de escopo.
- Checkboxes para pacotes.
- Selecionar tudo, desmarcar tudo e padrao recomendado por grupo.
- Helps curtos para tipos de objeto, pacotes e profundidade.
- Tree picker para OU.
- Search para OU/objeto.
- Segment control para profundidade.
- Badges de permissao necessaria.
- Estimativa de cobertura.
- Aviso quando pacote exige permissao maior.
- Resumo antes de executar.

Detalhe de produto/UX: ver `docs/COLLECTION_UI_DECISION.md`.

## 11. Tratamento de permissao ausente

Se uma permissao faltar:

- Nao mascarar como compliant.
- Marcar regra como `capabilityMissing`.
- Mostrar qual permissao/capacidade faltou.
- Sugerir nivel de acesso necessario.
- Permitir exportar relatorio de gaps de permissao.

Exemplo:

```text
Replication Health: capabilityMissing
Motivo: repadmin indisponivel e Event Log remoto sem acesso.
Impacto: nao foi possivel validar falhas de replicacao neste run.
Acao: executar em host com RSAT ou conceder acesso DC Health Reader.
```

## 12. Apoio a decisao operacional

Algumas decisoes exigem enriquecimento antes da acao. O produto deve coletar ou orientar a coleta de:

### 12.1 Cleanup de usuarios

- `lastLogonTimestamp` como indicador replicado e aproximado.
- `lastLogon` por DC quando profundidade permitir.
- `pwdLastSet`.
- estado `enabled/disabled`.
- grupos privilegiados.
- OU, owner e business unit.
- indicio de sincronizacao hibrida.
- evidencia cloud importada quando aplicavel.

Saida esperada:

- `confidence`: `High`, `Medium`, `Low`.
- `recommendedAction`: `Disable`, `MoveToQuarantine`, `Remove`, `NeedsOwnerDecision`, `NeedsEvidence`.
- `riskNotes`: explicacao curta do que impede acao direta.

### 12.2 Cleanup de computadores

- `lastLogonTimestamp`.
- `pwdLastSet`.
- `operatingSystem`.
- OU/site.
- se e DC, servidor, workstation ou objeto desconhecido.
- vinculos com grupos/GPOs criticos quando disponivel.

Guardrail:

- DCs e servidores nunca entram em exclusao direta no MVP.
- Workstations podem ser exportadas como candidatos se confianca for alta.

### 12.3 Eventos e SIEM

Quando a ferramenta nao tiver acesso a logs, deve gerar referencia para coleta externa:

- 4624 e 4625 para logon.
- 4648 para credencial explicita.
- 4720/4722/4725/4726 para lifecycle de contas.
- 4728/4729/4732/4733/4756/4757 para membership.
- 4768/4769/4771 para Kerberos.
- 4776 para NTLM.
- 2887/2888/2889 para LDAP signing/channel binding.

O produto deve permitir anexar CSV/JSON externo como evidencia importada no run.

## 13. AD Recycle Bin e recuperabilidade

Antes de gerar qualquer plano `Remove`, validar:

- AD Recycle Bin habilitado.
- nivel funcional minimo.
- retencao configurada.
- politica de quarentena.

Se AD Recycle Bin estiver desabilitado:

- bloquear `Remove` no MVP.
- recomendar `Disable` ou `MoveToQuarantine`.
- abrir iniciativa `Implement` para habilitacao planejada do recurso, com aviso de irreversibilidade.

## 14. Requisitos para QA

QA deve validar:

- Escopo por OU include/exclude.
- Seletores de tipo de objeto.
- Seletores de pacote.
- Profundidade Quick/Standard/Deep.
- Verificacao de ambiente com warning.
- Verificacao de ambiente bloqueada quando TCP ou RootDSE falhar.
- Falta de permissao vira `capabilityMissing`.
- Coleta parcial nao quebra o run.
- Credencial AD nao e persistida.
- Pacote nao selecionado nao gera findings.
- Historico registra escopo usado.
- Identidade hibrida sem evidencia cloud nao vira cleanup direto.
- AD Recycle Bin desabilitado bloqueia plano de delete.
- Export CSV de candidatos contem colunas obrigatorias.
- Lacuna de SIEM/log gera runbook externo, nao falso negativo.

## 15. Decisoes pendentes

| Decisao | Opcoes | Recomendacao |
| --- | --- | --- |
| Coleta agendada | MVP, fase 2, fora | Fase 2 |
| gMSA para coleta recorrente | Sim, nao | Fase 2 apos desenho de seguranca |
| Deep por padrao | Sim, nao | Nao |
| Multi-floresta | MVP, fase 2 | Fase 2 |
| Remediacao real | MVP, fase futura | Fase futura com aprovacao |
