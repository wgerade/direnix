# Arquitetura Alvo de Produto - Direnix

Status: Target Architecture v0.1  
Data: 2026-06-22  
Publico: Produto, Arquitetura, Desenvolvimento, UX, QA e Seguranca

Execucao iniciada:

- nova base de produto em `product/Direnix.Product.sln`;
- prototipo PowerShell mantido apenas como legado, migracao e fallback;
- instalador MSI inicial em `product/installer/Direnix.Msi`;
- detalhes operacionais em `docs/PRODUCT_REPLATFORMING_PLAN.md`.

## 1. Decisao

Direnix deve evoluir para um software instalado de mercado, nao para uma colecao de scripts com interface em volta.

A decisao tecnica alvo e:

- produto instalado por MSI/EXE assinado;
- Windows Service real para backend, API, jobs e hosting do portal;
- frontend web moderno servido pelo backend local;
- banco local transacional e criptografado;
- autenticacao moderna, RBAC, auditoria e protecao de segredos;
- motor de coleta como componente controlado, com PowerShell apenas como adaptador opcional;
- sem execucao direta em Domain Controller como premissa de produto;
- LDAPS seguro por padrao;
- LDAP inseguro somente como excecao explicita de laboratorio.

## 2. Anti-objetivos

O produto final nao deve depender destes padroes:

- PowerShell visivel para o usuario final;
- Scheduled Task como host principal do portal;
- `HttpListener` PowerShell como backend de produto;
- store JSON/DPAPI caseiro como banco operacional final;
- configuracao manual por edicao de arquivo;
- dados demo aparecendo em fluxo real;
- credencial AD persistida sem cofre e sem politica;
- coleta que varre o dominio sem limite de escopo, pagina, timeout e atributos.

Scripts podem existir para suporte, migracao, diagnostico ou fallback de coleta, mas nao devem ser a plataforma do produto.

## 3. Topologia Recomendada

### 3.1 Instalacao local em VM ou workstation de administracao

```text
Usuario -> Browser -> Direnix Web UI -> Direnix Service -> Banco local criptografado
                                                |
                                                +-> AD Connector por LDAPS
                                                +-> Evidence import
                                                +-> PowerShell adapter opcional
```

Uso padrao:

- instalar em VM ou workstation administrativa;
- apontar para DC/hostname/IP do AD alvo;
- validar conectividade e RootDSE;
- descobrir naming contexts e escopo;
- executar coletas read-only com limites;
- armazenar runs no banco local criptografado;
- apresentar findings e workflow no portal.

### 3.2 Futuro Enterprise

```text
Browser -> Direnix Server/API -> Banco central
                         |
                         +-> Collectors controlados
                         +-> SIEM/importacoes
                         +-> IdP corporativo
```

Essa topologia pode usar SQL Server/PostgreSQL e IdP corporativo, mas nao e dependencia do produto local v1.

## 4. Componentes

| Componente | Decisao alvo | Observacao |
| --- | --- | --- |
| Installer | MSI/EXE assinado, com repair/upgrade/uninstall | WiX/MSI ou empacotador equivalente; sem copiar pasta manual como fluxo final. |
| Backend | .NET Worker/ASP.NET Core como Windows Service | Host profissional para API, jobs, health e portal. |
| Portal Web | React/TypeScript ou equivalente moderno | Build estatico servido pelo backend; sem Node em runtime. |
| API | ASP.NET Core REST + eventos de progresso | Autenticada, versionada, validada por contrato. |
| Banco local | SQLite criptografado com SQLCipher ou provider equivalente | Migrations, WAL, backup, restore e retencao. |
| Protecao de chave | DPAPI/CNG protegendo chave do banco | Chave envelope por maquina; rotacao planejada. |
| Auth | OIDC opcional + login local break-glass | Entra ID/ADFS/Keycloak quando disponivel; local admin para ambientes isolados. |
| RBAC | Perfis por papel e capacidade | LocalAdmin, CollectorOperator, SecurityAnalyst, RiskManager, Auditor, ExecutiveViewer. |
| Coleta AD | .NET `System.DirectoryServices.Protocols` por LDAPS | Paginado, limitado, cancelavel, com atributos allowlist. |
| PowerShell Adapter | Opcional e isolado | Apenas para ferramentas Microsoft ou lacunas especificas; sem UI depender dele. |
| Job Engine | Fila local persistida | Estado, retries, checkpoint, cancelamento e historico. |
| Observabilidade | Logs estruturados + Windows Event Log | Health, erros acionaveis, correlationId por run. |

## 5. Backend e Servico

O servico `Direnix.Service` deve ser o processo principal instalado.

Responsabilidades:

- hospedar API local;
- servir frontend;
- gerenciar jobs de coleta/importacao/exportacao;
- controlar acesso, sessao, RBAC e auditoria;
- falar com banco;
- expor health endpoints internos;
- escrever logs estruturados;
- controlar ciclo de vida de coletores.

Configuracao padrao:

- bind em `127.0.0.1`;
- porta configuravel;
- HTTPS local quando certificado estiver disponivel;
- acesso remoto desligado por padrao;
- firewall rule criada somente por escolha explicita;
- service account com menor privilegio possivel.

## 6. Banco de Dados

O banco alvo nao deve ser JSON criptografado manualmente. A decisao recomendada e SQLite criptografado.

Requisitos:

- schema versionado por migrations;
- tabelas para runs, objetos, findings, evidencias, decisoes, usuarios, sessoes, auditoria e configuracao;
- transacoes reais;
- indices para busca por dominio, objeto, severidade, owner, status e run;
- backup e restore testados;
- retencao configuravel;
- chave criptografica protegida por DPAPI/CNG;
- audit log append-only do ponto de vista da aplicacao.

Tabela minima:

| Area | Entidades |
| --- | --- |
| Identidade app | users, roles, role_assignments, sessions, mfa_devices |
| Configuracao | app_settings, target_directories, credential_refs, policies |
| Coleta | collection_jobs, collection_steps, collection_capabilities, collection_logs |
| AD normalizado | ad_domains, ad_objects, ad_group_memberships, ad_gpos, ad_dcs |
| Findings | findings, finding_evidence, finding_decisions, risk_acceptances |
| Evidencia | evidence_files, evidence_hashes, imports |
| Auditoria | audit_events |

## 7. Autenticacao e Seguranca

### 7.1 Autenticacao

Modos suportados:

1. OIDC/OAuth2 para IdP corporativo quando disponivel.
2. Login local com MFA para ambiente isolado ou break-glass.
3. Windows Integrated Auth pode ser opcional, mas nao deve prender o produto ao AD alvo.

Requisitos:

- MFA local TOTP ou WebAuthn quando possivel;
- senha local com hash forte e salt;
- cookie seguro, HttpOnly e SameSite;
- CSRF em operacoes de escrita;
- Content Security Policy;
- lockout progressivo;
- sessao com expiracao;
- audit de login, logout, falha, export, coleta e decisao.

### 7.2 Segredos e credenciais

- Credencial AD deve ser efemera por padrao.
- Persistencia so pode existir com cofre explicito.
- Se persistir, usar Windows Credential Manager ou DPAPI/CNG com escopo e rotacao.
- Nunca gravar senha em config, log, run manifest ou banco em claro.
- Exibir status de credencial por referencia, nunca por valor.

### 7.3 Autorizacao

O portal deve separar:

- quem configura alvo AD;
- quem executa coleta;
- quem ve dado tecnico sensivel;
- quem exporta;
- quem aceita risco;
- quem ve apenas resumo gerencial.

## 8. Coleta AD Profissional

PowerShell nao deve ser o motor principal. O coletor principal deve usar APIs .NET LDAP/LDAPS.

Regras de coleta:

- LDAPS 636 por padrao;
- LDAP 389 apenas se usuario marcar excecao insegura de laboratorio;
- RootDSE antes de qualquer coleta;
- descoberta de naming contexts;
- SearchBase obrigatorio ou confirmado;
- OU/CN tree carregada por demanda, nao arvore inteira sem limite;
- paginacao LDAP;
- attribute allowlist por pacote;
- timeout por query;
- limite de objetos por etapa;
- cancelamento;
- retry com backoff;
- concorrencia baixa e configuravel por DC;
- preferir DC indicado pelo usuario ou descoberto com validacao;
- nao executar operacoes de escrita no AD no fluxo de assessment.

APIs recomendadas:

- `System.DirectoryServices.Protocols` para LDAP/LDAPS performatico e controlado;
- `System.DirectoryServices.AccountManagement` somente quando simplificar casos pequenos;
- PowerShell/RSAT somente como adaptador para evidencias especificas, como `repadmin`, GPO reports ou fallback.

## 9. UX de Conectividade e Escopo

Fluxo correto:

1. Usuario informa hostname/IP do DC ou endpoint LDAP.
2. UI assume LDAPS 636 e mostra o estado do TLS.
3. Produto valida TCP, TLS, certificado e RootDSE.
4. Produto exibe dominio/naming context descoberto.
5. Usuario escolhe escopo:
   - arvore carregada sob demanda; ou
   - DN manual assistido.
6. Produto sugere `DC=...` automaticamente a partir do dominio descoberto.
7. Produto mostra impacto de profundidade e pacotes.
8. Produto executa coleta como job, sem travar a UI.

Termos de UI:

- `Verificar ambiente` em vez de `preflight`;
- `Avaliacao rapida` em vez de `Quick`;
- `Avaliacao completa` em vez de `Deep`;
- tooltips obrigatorios para profundidade, pacotes, LDAPS, escopo e credencial.

## 10. Instalacao e Atualizacao

O produto final deve entregar:

- MSI ou EXE bootstrapper assinado;
- instalacao por usuario admin;
- service install/upgrade/repair;
- uninstall limpo;
- versionamento sem depender de pasta de origem;
- logs de instalacao;
- rollback em falha;
- deteccao de versao antiga e migracao guiada;
- remocao de artefatos legados.

O pacote transicional antigo foi retirado do caminho ativo. Instalacao de produto usa o MSI em `product/installer/Direnix.Msi`.

## 11. Arquivamento do Prototipo

| Legado | Destino |
| --- | --- |
| Portal PowerShell/HttpListener | Arquivado em `legacy/powershell-prototype`. |
| Store JSON/DPAPI do prototipo | Substituido por SQLCipher + migrations em `product/`. |
| Dashboard vanilla do prototipo | Arquivado; nova UI deve nascer em `product/`. |
| Coletor PowerShell | Arquivado como referencia; coletor principal deve ser .NET LDAP/LDAPS. |
| Wrapper `Direnix.Service.exe` antigo | Removido do caminho ativo; produto usa `Direnix.Service` novo. |
| Bootstrapper `Direnix.Setup.exe` antigo | Removido do caminho ativo; produto usa MSI WiX/MSBuild. |

## 12. Plano de Execucao

### Fase A - Fundacao de produto

- Criar solution .NET.
- Criar Windows Service real.
- Criar API local.
- Criar banco SQLite criptografado.
- Criar migrations.
- Criar auth local inicial.
- Criar audit log.
- Criar health endpoint.

Gate:

- Servico inicia sem PowerShell.
- Portal abre via browser.
- Banco cria schema e exige chave.
- Login e RBAC funcionam.

### Fase B - UI profissional

- Criar frontend TypeScript.
- Criar design system operacional.
- Criar shell autenticado.
- Criar wizard de primeiro uso.
- Criar tela de alvo AD e validacao.
- Criar jobs/progresso sem bloquear UI.

Gate:

- Nenhuma tela demo aparece no fluxo real.
- Usuario entende LDAPS, escopo, profundidade e pacotes via help contextual.

### Fase C - Coletor AD .NET

- Implementar RootDSE.
- Implementar LDAPS/TLS validation.
- Implementar descoberta de naming contexts.
- Implementar browsing de OU/CN sob demanda.
- Implementar coleta paginada de usuarios/computadores/grupos/OUs.
- Implementar limites e cancelamento.

Gate:

- Coleta em lab AD sem travar DC.
- Logs mostram queries, pagina, tempo e contagem.
- UI permanece responsiva.

### Fase D - Regras, findings e evidencia

- Portar regras existentes.
- Persistir evidencias no banco.
- Construir timeline e comparacao entre runs.
- Implementar export tecnico e gerencial saneado.

Gate:

- Findings tem evidencia, owner, decisao e proximo passo.
- Export gerencial nao vaza dado sensivel.

### Fase E - Hardening e release

- MSI/EXE assinado.
- Upgrade/repair/uninstall.
- Backup/restore.
- Logs/eventos.
- Testes de performance em AD grande.
- Threat model.
- QA matrix.

Gate:

- Instala em VM limpa sem comando manual.
- Reboot nao abre console.
- Servico recupera sozinho.
- Banco e auth sobrevivem upgrade.

## 13. Stop Rule

Enquanto essa arquitetura nao existir, qualquer empacotamento com PowerShell como backend deve ser tratado como prototipo tecnico, nao como produto.

Nao investir em polimento visual ou instalador final sobre a base antiga alem do necessario para migracao, limpeza e demonstracao controlada.
