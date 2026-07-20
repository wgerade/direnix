# Saude do banco do AD (NTDS.dit)

Apos muita movimentacao (criacao/exclusao em massa, migracoes, problemas de replicacao), o banco do
Active Directory acumula "lixo" logico e espaco livre interno. Esta pagina resume o que a ferramenta
detecta por LDAP e o que exige manutencao operacional no controlador de dominio.

## O que a ferramenta detecta (por LDAP, sem agente)

- **Objetos de conflito de replicacao (CNF)** — `ADGOV-CONFLICT-022`. Objetos cujo RDN contem o marcador
  `\nCNF:` surgem quando o mesmo objeto e criado em DCs diferentes antes da replicacao. Indicam problema de
  replicacao e poluem a base. A coleta busca `(cn=*\0ACNF:*)` na subarvore do dominio.
- **Objetos orfaos em LostAndFound** — `ADGOV-LOSTFOUND-023`. Objetos cujo pai foi removido em outro DC
  vao parar em `CN=LostAndFound,<dominio>`. A coleta conta e amostra esses objetos.
- **Lixeira do AD / retencao** — `ADGOV-RECYCLEBIN-001` (ver doc de regras): habilitada? quantos dias de
  retencao de objetos deletados (`tombstoneLifetime` / `msDS-DeletedObjectLifetime`).

Esses sinais aparecem como riscos de **Governanca** no painel, com contagem e amostra de DNs na evidencia.

## O que exige manutencao operacional (nao mensuravel por LDAP)

O tamanho e o espaco livre interno do `NTDS.dit` **nao** sao legiveis por LDAP — exigem acesso ao DC.
A ferramenta orienta (nao executa) estas acoes:

- **Desfragmentacao online (automatica)**: ocorre a cada coleta de lixo (padrao ~12h). Otimiza o uso
  interno, mas **nao** reduz o tamanho do arquivo.
- **Desfragmentacao offline (recupera espaco)**: unica forma de reduzir o `NTDS.dit` e remover whitespace.
  Requer o DC em DSRM (Directory Services Restore Mode):
  ```
  ntdsutil
  activate instance ntds
  files
  compact to C:\Temp\ntds
  quit
  quit
  ```
  Depois copiar o `ntds.dit` compactado de volta e apagar os logs, conforme o procedimento da Microsoft.
- **Analise semantica do banco** (integridade logica):
  ```
  ntdsutil
  activate instance ntds
  semantic database analysis
  go fixup
  ```
- **Objetos remanescentes (lingering objects)** apos falhas de replicacao:
  `repadmin /removelingeringobjects <DC> <GUID_DC_referencia> <DN_particao>`.
- **Espaco livre/whitespace**: habilitar o nivel de log de "Garbage Collection" gera o evento **1646**
  (NTDS ISAM) reportando o espaco livre estimado no banco.

## Quando agir

- Apos exclusoes em massa (muitas contas/computadores/objetos) — considere defrag offline para recuperar espaco.
- Quando `ADGOV-CONFLICT-022` ou `ADGOV-LOSTFOUND-023` reportarem objetos — reconcilie/limpe e investigue a
  causa de replicacao.
- Sempre com a Lixeira do AD habilitada e backup do estado do sistema antes de manutencoes offline.

Referencias:
- [Offline defragmentation — Microsoft](https://learn.microsoft.com/en-us/troubleshoot/windows-server/active-directory/ad-database-offline-defragmentation)
- [Active Directory database optimization (ntdsutil) — Microsoft Press](https://www.microsoftpressstore.com/articles/article.aspx?p=3222421&seqNum=8)
