# Decisao Tecnica da Interface de Avaliacao AD

Status: v0.2  
Publico: Produto, UX, Arquitetura, Desenvolvimento, QA

## Problema

A tela de avaliacao nao pode comecar pedindo uma OU nem usar exemplos ficticios como se fossem ambiente real. Em operacao real, a ferramenta roda em uma VM ou estacao administrativa, nao diretamente no Domain Controller. Antes de selecionar OU, o usuario precisa apontar qual AD/DC sera consultado, validar conectividade segura e descobrir o naming context.

## Decisao

O fluxo de avaliacao deve ser:

```text
1. Apontar AD
2. Validar conectividade segura
3. Descobrir dominio e naming context
4. Escolher escopo
5. Escolher tipos de objeto
6. Escolher pacotes de avaliacao
7. Escolher profundidade
8. Verificar ambiente
9. Iniciar avaliacao somente quando o alvo estiver pronto
```

## Alvo AD

Campos:

- `Domain Controller / LDAP host`: hostname, FQDN ou IP do DC/endpoint LDAP.
- `Protocolo`: LDAPS por padrao.
- `Porta`: 636 por padrao para LDAPS; 389 somente quando LDAP for escolhido explicitamente como excecao.
- `Dominio esperado`: opcional; usado apenas para sugestao manual. Nao pode aparecer como dominio validado se RootDSE nao responder.
- `Autenticacao`: identidade Windows atual ou credencial AD temporaria.

V1 nao deve persistir senha AD. A credencial explicita pode ser informada para uma validacao/run, mas nao deve ser salva no store, exibida em log, retornada pela API ou reaproveitada automaticamente.

## Validacao de conectividade

O botao `Validar conexao segura` deve checar:

- formato de host;
- porta TCP;
- possibilidade de consultar RootDSE quando acessivel;
- `defaultNamingContext`;
- naming contexts retornados;
- ferramentas opcionais como `repadmin`.

Estados:

- `Ready`: porta e RootDSE OK.
- `ReadyWithWarnings`: RootDSE OK, mas existe capacidade opcional ausente ou excecao explicita como LDAP sem TLS.
- `Blocked`: host/porta indisponivel, falha TLS/credencial, RootDSE indisponivel ou alvo invalido.

RootDSE sem `defaultNamingContext` e falha de TCP nunca podem virar `ReadyWithWarnings` para inicio de avaliacao.

## Escopo

O produto deve tentar facilitar, mas sem travar o ambiente:

1. Se RootDSE retornar `defaultNamingContext`, preencher automaticamente o DN base.
2. Se houver arvore conhecida, mostrar OUs/CNs em arvore limitada.
3. Se a arvore for pesada ou a conta nao conseguir listar, permitir entrada manual assistida.
4. A entrada manual deve aceitar DN raiz (`DC=dominio,DC=local`), OU (`OU=Servidores,DC=dominio,DC=local`) e CN quando o pacote exigir.

Regra de UX: o usuario nao deve digitar o DN raiz se a ferramenta ja conseguiu descobrir o dominio e o naming context.

## Seletores

Todo grupo de opcoes deve ter:

- `Selecionar tudo`;
- `Desmarcar tudo`;
- `Padrao recomendado`;
- help `i` por opcao.

## Profundidade

| Nivel | Significado | Quando usar |
| --- | --- | --- |
| Quick | Avaliacao rapida, poucos atributos, baixo impacto. | Primeiro teste, validacao de conectividade ou ambiente grande. |
| Standard | Assessment normal com inventario, higiene e principais riscos. | Uso diario/recomendado. |
| Deep | Avaliacao extensa: ACLs, delegacao, eventos, GPOs e grupos grandes. | Investigacao planejada, janela aprovada e permissao adequada. |

`Deep` nunca deve ser padrao.

## Portas e liberacoes

Padrao seguro:

- TCP 636 para LDAPS.

Excecao controlada:

- TCP 389 para LDAP, somente quando LDAPS ainda nao estiver disponivel e a rede/risco aceitarem a excecao.

Dependendo do pacote:

- TCP/SMB 445 para SYSVOL/GPO.
- RPC/WMI e Event Log remoto para eventos/DC health, conforme politica local.
- Ferramentas RSAT como `repadmin` e `dcdiag` quando o pacote de replicacao profunda for selecionado.

Se algo nao estiver liberado, a ferramenta deve marcar `CapabilityMissing` ou `ReadyWithWarnings`, nao falhar silenciosamente.

## Double-check de desenvolvimento

Antes de devolver uma versao da tela, o desenvolvedor deve rodar uma validacao local que confirme:

- default de protocolo `LDAPS` e porta `636`;
- nenhum placeholder de dominio ficticio em opcoes da avaliacao;
- nenhum texto visivel `preflight`, `demo`, `mock` ou `simulated` no fluxo de avaliacao;
- falha de TCP ou RootDSE retorna `Blocked`;
- o botao principal fica no topo da tela.

Comando:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-DirenixAssessmentUx.ps1
```

## Fora de escopo na v1

- Persistir credencial AD.
- Rodar remediacao automaticamente.
- Descobrir floresta inteira sem limite.
- Publicar coleta multiusuario via IIS.
