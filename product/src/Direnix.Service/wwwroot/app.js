"use strict";

// ---------------- i18n ----------------
const I18N = {
  pt: {
    "nav.today": "Hoje", "nav.overview": "Postura", "nav.assessments": "Avaliações", "nav.risks": "Riscos",
    "nav.inventory": "Inventário", "nav.settings": "Configurações", "nav.operations": "Operação",
    "nav.timeline": "Linha do tempo", "search.global": "Buscar usuário, grupo, SID, GUID, SPN…",
    "search.none": "Nada encontrado", "search.hint": "Digite ao menos 2 caracteres",
    "score.identity": "Identity Score", "score.tier0": "Tier 0", "score.critical": "Riscos críticos", "score.lastSync": "Última sincronização",
    "morning.title": "O que mudou desde ontem", "morning.empty": "Sem mudanças nas últimas 24h", "morning.newRisks": "Riscos novos (24h)",
    "morning.first": "Nenhuma coleta ainda. Faça a primeira avaliação para ver o seu ambiente aqui.",
    "morning.second": "Baseline criada. A timeline de mudanças nasce na 2ª coleta — agende a coleta diária e amanhã este painel mostra o que mudou.",
    "morning.goCollect": "Fazer primeira avaliação", "morning.goSchedule": "Agendar coleta diária",
    "today.eyebrow": "Ronda diária", "today.last": "Última coleta", "today.next": "Próxima coleta",
    "today.noSched": "Coleta automática desativada", "today.schedOn": "Coleta automática ativada",
    "ind.emptyCta": "Ativar indicadores no perfil de regras",
    "firstrun.done": "Primeira avaliação concluída", "firstrun.risks": "riscos encontrados", "firstrun.view": "Ver riscos",
    "demo.enter": "Explorar com dados de exemplo", "demo.banner": "MODO DEMO — dados fictícios, nada é salvo",
    "demo.exit": "Sair do demo", "demo.readonly": "Ação indisponível no modo demo",
    "indicators.title": "Indicadores operacionais", "indicators.empty": "Nenhum indicador neste ciclo",
    "ind.title": "Indicadores operacionais", "ind.note": "Contagens do dia a dia (senhas vencendo, contas bloqueadas…). Rodam junto da coleta deste perfil, inclusive a agendada.",
    "ind.horizon": "Horizonte \"vencendo/expirar\" (dias)", "ind.custom": "Indicadores customizados",
    "ind.customNote": "Sua própria consulta: um filtro LDAP, ou um comando PowerShell Get-AD* (aceita -Filter, -LDAPFilter ou -Identity). Executamos sempre como busca LDAP read-only — o PowerShell nunca é executado.",
    "ind.cName": "Nome", "ind.cType": "Tipo de objeto", "ind.cKind": "Sintaxe", "ind.cQuery": "Consulta",
    "ind.cValidate": "Validar", "ind.cAdd": "Adicionar", "ind.matches": "objetos", "ind.cNeed": "Informe nome e consulta.",
    "ind.cInvalid": "Consulta inválida:", "ind.cValid": "Filtro LDAP válido:", "ind.cAdded": "Indicador adicionado.", "ind.cReadonly": "Perfil padrão é somente leitura — use 'Salvar como…'.",
    "timeline.title": "Linha do tempo", "timeline.empty": "Nenhuma mudança registrada",
    "timeline.24h": "Últimas 24h", "timeline.7d": "Últimos 7 dias", "timeline.all": "Tudo",
    "obj.detail": "Objeto", "obj.attributes": "Atributos", "obj.risks": "Riscos do objeto", "obj.changes": "Mudanças",
    "field.displayName": "Nome de exibição", "field.sam": "Conta", "field.objectSid": "SID", "field.distinguishedName": "DN",
    "field.enabled": "Habilitada", "field.lastLogon": "Último logon", "field.pwdLastSet": "Senha definida em",
    "field.userPrincipalName": "UPN", "field.manager": "Gerente", "field.operatingSystem": "Sistema operacional",
    "field.servicePrincipalName": "SPN", "field.memberOf": "Grupos (qtd.)", "field.description": "Descrição",
    "field.managedBy": "Responsável", "field.memberCount": "Membros", "field.versionNumber": "Versão", "field.flags": "Flags", "field.gPLink": "Vínculos de GPO",
    "topbar.refresh": "Atualizar", "topbar.new": "Nova avaliação",
    "topbar.report": "Relatório", "export.csv": "Exportar CSV",
    "sub.today": "O que mudou, o que venceu e o que precisa de ação — todo dia.",
    "sub.overview": "Visão consolidada do ambiente avaliado.",
    "sub.collect": "Alvo, perfil e execução somente leitura contra o Active Directory.",
    "sub.findings": "Indicadores de exposição, evidência e ação recomendada.",
    "sub.inventory": "Objetos, grupos, computadores, GPOs e cobertura.",
    "sub.settings": "Perfis de regras e banco local.", "sub.operations": "Saúde do serviço e diagnóstico.",
    "sub.timeline": "Mudanças no ambiente em ordem cronológica.",
    "overview.posture": "Postura atual", "overview.topRisks": "Principais riscos",
    "overview.bySeverity": "Riscos por severidade", "overview.byCategory": "Riscos por categoria",
    "overview.legend": "Ordem: Crítico > Alto > Médio > Baixo > Informativo.",
    "col.risk": "Risco", "col.severity": "Severidade", "col.category": "Categoria",
    "col.indicator": "Indicador", "col.object": "Objeto", "col.action": "Ação", "col.status": "Status",
    "empty.risks": "Sem riscos para exibir",
    "assess.new": "Nova avaliação", "assess.step.target": "Alvo", "assess.domain": "Domínio",
    "assess.dc": "Controlador de domínio", "assess.protocol": "Protocolo", "assess.user": "Usuário (opcional)",
    "assess.secret": "Senha (opcional)", "assess.validate": "Validar conectividade",
    "assess.step.profile": "Perfil de regras", "assess.profile": "Perfil",
    "assess.profileHint": "O perfil define os limites e quais regras são aplicadas. Gerencie em Configurações.",
    "assess.step.advanced": "Avançado (opcional)", "assess.depth": "Profundidade",
    "assess.objectTypes": "Tipos de objeto", "assess.start": "Iniciar avaliação",
    "assess.how": "Como funciona", "assess.readonly": "Somente leitura",
    "obj.users": "Usuários", "obj.computers": "Computadores", "obj.groups": "Grupos",
    "risks.active": "Ativos", "risks.exception": "Em exceção", "risks.resolved": "Resolvidos",
    "risks.search": "Buscar objeto/indicador",
    "cat.all": "Todas as categorias", "cat.Hygiene": "Higiene", "cat.PrivilegedAccess": "Acesso Privilegiado",
    "cat.Hardening": "Endurecimento", "cat.Infrastructure": "Infraestrutura", "cat.Governance": "Governança",
    "settings.profiles": "Perfis de regras", "settings.activeProfile": "Perfil",
    "settings.saveAs": "Salvar como…", "settings.save": "Salvar", "settings.delete": "Excluir",
    "settings.activate": "Tornar ativo", "settings.thresholds": "Limites", "settings.rules": "Regras",
    "th.staleUser": "Conta sem uso (dias)", "th.staleComputer": "Computador sem uso (dias)",
    "th.disabled": "Desabilitado retido (dias)", "th.krbtgt": "Rotação do krbtgt (dias)",
    "th.maq": "MachineAccountQuota esperado", "th.emptyDays": "Grupo/OU vazio por (dias)", "th.recycleBin": "Retenção mín. da Lixeira do AD (dias)",
    "settings.db": "Banco local", "settings.encryption": "Criptografia", "settings.key": "Chave", "settings.path": "Caminho",
    "ops.service": "Serviço local",
    "metric.riskScore": "Índice de risco", "metric.activeRisks": "Riscos ativos",
    "metric.staleObjects": "Objetos obsoletos", "metric.privilegedExposure": "Exposição privilegiada",
    "sev.Critical": "Crítico", "sev.High": "Alto", "sev.Medium": "Médio", "sev.Low": "Baixo", "sev.Info": "Informativo",
    "action.CleanUp": "Limpar", "action.Adjust": "Ajustar", "action.Implement": "Implementar",
    "action.Investigate": "Investigar", "action.Monitor": "Monitorar", "action.Decommission": "Descomissionar",
    "action.AcceptRisk": "Aceitar risco", "action.NeedsEvidence": "Requer evidência",
    "action.ReadyForCleanup": "Pronto p/ limpeza", "action.GenerateScript": "Gerar script",
    "status.New": "Novo", "status.Open": "Aberto", "status.Recurring": "Recorrente", "status.Resolved": "Resolvido",
    "status.AcceptedRisk": "Exceção", "coverage.StandardOrFull": "Completa", "coverage.Partial": "Parcial",
    "coverage.NoDirectory": "Sem diretório", "coverage.NoCollection": "Sem avaliação",
    "obj.User": "Usuários", "obj.Computer": "Computadores", "obj.Group": "Grupos",
    "obj.OrganizationalUnit": "OUs", "obj.GroupPolicyContainer": "GPOs", "obj.Domain": "Domínio",
    "drawer.impact": "Impacto", "drawer.action": "Ação recomendada", "drawer.manual": "Passo a passo (manual)",
    "drawer.ps": "Remediação (PowerShell)", "drawer.copy": "Copiar", "drawer.copied": "Copiado!",
    "drawer.psNote": "Rode primeiro a pré-visualização (não altera nada); confira o resultado; só então rode o comando de aplicação.",
    "drawer.psPreview": "1) Pré-visualizar (WhatIf) — não altera nada",
    "drawer.psPreviewHint": "Simula a ação e mostra o que seria alterado. Seguro para executar.",
    "drawer.psApply": "2) Aplicar correção — executa a mudança",
    "drawer.psApplyHint": "Executa de fato a ação. Rode somente após validar a pré-visualização e com aprovação.",
    "drawer.psApplyManual": "Sem comando único seguro — siga o passo a passo manual acima.",
    "drawer.evidence": "Evidência", "drawer.accept": "Aceitar risco (exceção)",
    "drawer.owner": "Responsável", "drawer.justification": "Justificativa", "drawer.validity": "Validade",
    "drawer.acceptBtn": "Aceitar risco", "drawer.remove": "Remover exceção", "drawer.loading": "Carregando…",
    "env.none": "Nenhuma avaliação concluída", "env.noneSub": "Aponte um controlador de domínio e inicie a primeira avaliação.",
    "env.domain": "Domínio avaliado", "env.risksFrom": "riscos ativos a partir da última avaliação.",
    "how.1": "Aponte o controlador de domínio.", "how.2": "Valide a conectividade (LDAPS recomendado).",
    "how.3": "Escolha o perfil de regras.", "how.4": "Inicie a avaliação (somente leitura).",
    "how.5": "Os riscos aparecem na aba Riscos ao concluir.",
    "pager.prev": "Anterior", "pager.next": "Próxima", "pager.of": "de",
    "exc.need": "Informe responsável e justificativa.", "days.90": "90 dias", "days.180": "180 dias", "days.365": "1 ano",
    "ev.ageDays": "Idade (dias)", "ev.lastLogon": "Último logon", "ev.pwdLastSet": "Senha definida em",
    "ev.thresholdDays": "Limite (dias)", "ev.privileged": "Privilegiado", "ev.distinguishedName": "DN",
    "ev.measuredValue": "Valor medido", "ev.expectedValue": "Valor esperado", "ev.domain": "Domínio",
    "ev.privilegedGroups": "Grupos privilegiados", "ev.objectType": "Tipo de objeto",
    "ev.operatingSystem": "Sistema operacional", "ev.whenChanged": "Alterado em", "ev.sam": "Conta",
    "ev.displayName": "Nome de exibição", "ev.objectSid": "SID", "ev.servicePrincipalName": "SPN",
    "ev.memberCount": "Membros", "ev.ageDays": "Idade (dias)", "ev.count": "Quantidade", "ev.sample": "Exemplos",
    "ev.enabled": "Habilitada", "ev.retentionDays": "Retenção (dias)", "ev.versionNumber": "Versão", "ev.flags": "Flags",
    "assess.history": "Avaliações anteriores",
    "runs.when": "Data", "runs.operator": "Operador", "runs.executedAs": "Executado por",
    "runs.coverage": "Cobertura", "runs.objects": "Objetos", "runs.findings": "Riscos",
    "runs.empty": "Nenhuma avaliação ainda",
    "fresh.current": "Atual", "fresh.stale": "Defasado",
    "col.collected": "Avaliado em",
    "reassess.selected": "Reavaliar selecionados", "reassess.all": "Reavaliar todos (na lista)",
    "reassess.clear": "Cancelar reavaliação", "reassess.banner": "Reavaliação dirigida de {n} objeto(s) selecionado(s). Confirme o alvo e as credenciais e inicie.",
    "reassess.none": "Selecione ao menos um risco para reavaliar.",
    "res.Fixed": "Corrigido", "res.RemovedInRecycleBin": "Removido (na Lixeira do AD)", "res.RemovedConfirmed": "Removido (confirmado)",
    "drawer.resolution": "Motivo da resolução",
    "drawer.history": "Histórico do objeto", "drawer.firstSeen": "Primeira observação",
    "drawer.lastSeen": "Última observação", "drawer.runsSeen": "Observações", "drawer.freshness": "Frescor",
    "profile.builtin": "(padrão)", "profile.custom": "(personalizado)",
    "settings.builtinHint": "Perfil padrão é somente leitura. Use 'Salvar como…' para criar um perfil editável.",
    "settings.customHint": "Perfil personalizado — edite os campos e clique em Salvar.",
    "settings.new": "Novo perfil", "settings.scope": "Escopo da avaliação",
    "settings.newName": "Nome (novo/duplicar)", "settings.needName": "Informe um nome para o perfil.", "settings.exists": "Já existe um perfil com esse nome.",
    "settings.saved": "Perfil salvo.", "settings.activated": "Perfil ativado.", "settings.confirmDelete": "Clique de novo para excluir",
    "settings.scopeNote": "A coleta é automática: avaliamos apenas os tipos de objeto exigidos pelas regras ativas abaixo.",
    "confirm.save": "Salvar alterações no perfil \"{name}\"?", "confirm.delete": "Excluir o perfil \"{name}\"? Esta ação não pode ser desfeita.",
    "ops.audit": "Auditoria de ações", "audit.when": "Data", "audit.actor": "Operador", "audit.action": "Ação", "audit.target": "Alvo", "audit.result": "Resultado", "audit.empty": "Nenhuma ação registrada",
    "auth.login": "Entrar", "auth.create": "Criar administrador", "auth.sub": "Acesso ao portal de operações de identidade.", "auth.subSetup": "Primeiro acesso: defina o administrador local (senha de 8+ caracteres).", "auth.user": "Usuário", "auth.pass": "Senha", "auth.logout": "Sair", "auth.bad": "Usuário ou senha inválidos.",
    "sched.title": "Coleta automática", "sched.note": "A coleta agendada roda sob a identidade do serviço (gMSA) via Kerberos/LDAPS — nenhuma senha é armazenada.", "sched.enabled": "Habilitar", "sched.on": "Ativada", "sched.off": "Desativada", "sched.frequency": "Frequência", "sched.daily": "Diária", "sched.weekly": "Semanal", "sched.interval": "A cada N horas", "sched.time": "Horário (HH:MM)", "sched.intervalHours": "Intervalo (horas)", "sched.host": "Controlador de domínio", "sched.weekdays": "Dias da semana", "sched.profile": "Perfil de regras", "sched.activeProfile": "Perfil ativo no momento", "sched.identity": "Identidade da coleta", "sched.last": "Última execução", "sched.next": "Próxima execução", "sched.test": "Testar conectividade", "sched.saved": "Agendamento salvo.",
    "day.mon": "Seg", "day.tue": "Ter", "day.wed": "Qua", "day.thu": "Qui", "day.fri": "Sex", "day.sat": "Sáb", "day.sun": "Dom",
    "portable.badge": "Portátil",
    "notif.title": "Digest matinal", "notif.note": "Envia o resumo da ronda (o que mudou, novos riscos, indicadores) ao fim da coleta agendada. Assim a informação chega até você — sem precisar lembrar de abrir o portal.", "notif.policy": "Quando enviar", "notif.onlyActivity": "Só quando houver atividade", "notif.always": "Sempre após a coleta", "notif.lang": "Idioma do digest", "notif.on": "Ativado", "notif.off": "Desativado", "notif.smtpTitle": "E-mail (SMTP)", "notif.smtpEnabled": "Habilitar e-mail", "notif.smtpHost": "Servidor SMTP", "notif.smtpPort": "Porta", "notif.smtpTls": "STARTTLS", "notif.smtpUser": "Usuário", "notif.smtpPass": "Senha", "notif.passPlaceholder": "(inalterada)", "notif.smtpFrom": "Remetente", "notif.smtpTo": "Destinatários", "notif.webhookTitle": "Webhook (Teams/Slack/automações)", "notif.webhookEnabled": "Habilitar webhook", "notif.webhookUrl": "URL do webhook", "notif.test": "Enviar teste agora", "notif.last": "Último envio", "notif.saved": "Notificações salvas.", "notif.testing": "Enviando teste…", "notif.skipped": "Pulado", "notif.noChannel": "Nenhum canal habilitado.",
    "svc.title": "Configuração do serviço", "svc.note": "Define como o serviço Windows roda. Para coleta automática segura, use uma gMSA (só o nome da conta; o Windows gerencia a senha).", "svc.curIdentity": "Identidade atual", "svc.curStartup": "Inicialização atual", "svc.startup": "Tipo de inicialização", "svc.startAuto": "Automático", "svc.startDelayed": "Automático (atraso)", "svc.startManual": "Manual", "svc.identity": "Identidade (Log On As)", "svc.idLocalSystem": "LocalSystem", "svc.idGmsa": "Conta gerenciada (gMSA)", "svc.account": "Conta gMSA", "svc.apply": "Aplicar configuração", "svc.restart": "Reinicie o serviço para a nova conta valer.",
    "exc.expires": "Expira em", "exc.empty": "Nenhuma exceção registrada", "exc.remove": "Remover", "exc.newName": "Nome do novo perfil:"
  },
  en: {
    "nav.today": "Today", "nav.overview": "Posture", "nav.assessments": "Assessments", "nav.risks": "Risks",
    "nav.inventory": "Inventory", "nav.settings": "Settings", "nav.operations": "Operations",
    "nav.timeline": "Timeline", "search.global": "Search user, group, SID, GUID, SPN…",
    "search.none": "Nothing found", "search.hint": "Type at least 2 characters",
    "score.identity": "Identity Score", "score.tier0": "Tier 0", "score.critical": "Critical risks", "score.lastSync": "Last sync",
    "morning.title": "What changed since yesterday", "morning.empty": "No changes in the last 24h", "morning.newRisks": "New risks (24h)",
    "morning.first": "No collection yet. Run the first assessment to see your environment here.",
    "morning.second": "Baseline created. The change timeline is born on the 2nd collection — schedule the daily collection and tomorrow this panel shows what changed.",
    "morning.goCollect": "Run first assessment", "morning.goSchedule": "Schedule daily collection",
    "today.eyebrow": "Daily rounds", "today.last": "Last collection", "today.next": "Next collection",
    "today.noSched": "Automatic collection disabled", "today.schedOn": "Automatic collection enabled",
    "ind.emptyCta": "Enable indicators in the rule profile",
    "firstrun.done": "First assessment complete", "firstrun.risks": "risks found", "firstrun.view": "View risks",
    "demo.enter": "Explore with sample data", "demo.banner": "DEMO MODE — fictional data, nothing is saved",
    "demo.exit": "Exit demo", "demo.readonly": "Not available in demo mode",
    "indicators.title": "Operational indicators", "indicators.empty": "No indicators this cycle",
    "ind.title": "Operational indicators", "ind.note": "Day-to-day counts (passwords expiring, locked accounts…). They run with this profile's collection, including the scheduled one.",
    "ind.horizon": "\"Expiring\" horizon (days)", "ind.custom": "Custom indicators",
    "ind.customNote": "Your own query: an LDAP filter, or a PowerShell Get-AD* command (accepts -Filter, -LDAPFilter or -Identity). We always run it as a read-only LDAP search — PowerShell is never executed.",
    "ind.cName": "Name", "ind.cType": "Object type", "ind.cKind": "Syntax", "ind.cQuery": "Query",
    "ind.cValidate": "Validate", "ind.cAdd": "Add", "ind.matches": "objects", "ind.cNeed": "Enter a name and a query.",
    "ind.cInvalid": "Invalid query:", "ind.cValid": "Valid LDAP filter:", "ind.cAdded": "Indicator added.", "ind.cReadonly": "Built-in profile is read-only — use 'Save as…'.",
    "timeline.title": "Timeline", "timeline.empty": "No changes recorded",
    "timeline.24h": "Last 24h", "timeline.7d": "Last 7 days", "timeline.all": "All",
    "obj.detail": "Object", "obj.attributes": "Attributes", "obj.risks": "Object risks", "obj.changes": "Changes",
    "field.displayName": "Display name", "field.sam": "Account", "field.objectSid": "SID", "field.distinguishedName": "DN",
    "field.enabled": "Enabled", "field.lastLogon": "Last logon", "field.pwdLastSet": "Password set",
    "field.userPrincipalName": "UPN", "field.manager": "Manager", "field.operatingSystem": "Operating system",
    "field.servicePrincipalName": "SPN", "field.memberOf": "Groups (count)", "field.description": "Description",
    "field.managedBy": "Owner", "field.memberCount": "Members", "field.versionNumber": "Version", "field.flags": "Flags", "field.gPLink": "GPO links",
    "topbar.refresh": "Refresh", "topbar.new": "New assessment",
    "topbar.report": "Report", "export.csv": "Export CSV",
    "sub.today": "What changed, what expired and what needs action — every day.",
    "sub.overview": "Consolidated view of the assessed environment.",
    "sub.collect": "Target, profile and read-only execution against Active Directory.",
    "sub.findings": "Exposure indicators, evidence and recommended action.",
    "sub.inventory": "Objects, groups, computers, GPOs and coverage.",
    "sub.settings": "Rule profiles and local database.", "sub.operations": "Service health and diagnostics.",
    "sub.timeline": "Environment changes in chronological order.",
    "overview.posture": "Current posture", "overview.topRisks": "Top risks",
    "overview.bySeverity": "Risks by severity", "overview.byCategory": "Risks by category",
    "overview.legend": "Order: Critical > High > Medium > Low > Info.",
    "col.risk": "Risk", "col.severity": "Severity", "col.category": "Category",
    "col.indicator": "Indicator", "col.object": "Object", "col.action": "Action", "col.status": "Status",
    "empty.risks": "No risks to show",
    "assess.new": "New assessment", "assess.step.target": "Target", "assess.domain": "Domain",
    "assess.dc": "Domain controller", "assess.protocol": "Protocol", "assess.user": "User (optional)",
    "assess.secret": "Password (optional)", "assess.validate": "Test connectivity",
    "assess.step.profile": "Rule profile", "assess.profile": "Profile",
    "assess.profileHint": "The profile defines thresholds and which rules run. Manage it in Settings.",
    "assess.step.advanced": "Advanced (optional)", "assess.depth": "Depth",
    "assess.objectTypes": "Object types", "assess.start": "Start assessment",
    "assess.how": "How it works", "assess.readonly": "Read-only",
    "obj.users": "Users", "obj.computers": "Computers", "obj.groups": "Groups",
    "risks.active": "Active", "risks.exception": "Accepted", "risks.resolved": "Resolved",
    "risks.search": "Search object/indicator",
    "cat.all": "All categories", "cat.Hygiene": "Hygiene", "cat.PrivilegedAccess": "Privileged Access",
    "cat.Hardening": "Hardening", "cat.Infrastructure": "Infrastructure", "cat.Governance": "Governance",
    "settings.profiles": "Rule profiles", "settings.activeProfile": "Profile",
    "settings.saveAs": "Save as…", "settings.save": "Save", "settings.delete": "Delete",
    "settings.activate": "Set active", "settings.thresholds": "Thresholds", "settings.rules": "Rules",
    "th.staleUser": "Stale user (days)", "th.staleComputer": "Stale computer (days)",
    "th.disabled": "Disabled retention (days)", "th.krbtgt": "krbtgt rotation (days)",
    "th.maq": "Expected MachineAccountQuota", "th.emptyDays": "Empty group/OU for (days)", "th.recycleBin": "AD Recycle Bin min retention (days)",
    "settings.db": "Local database", "settings.encryption": "Encryption", "settings.key": "Key", "settings.path": "Path",
    "ops.service": "Local service",
    "metric.riskScore": "Risk score", "metric.activeRisks": "Active risks",
    "metric.staleObjects": "Stale objects", "metric.privilegedExposure": "Privileged exposure",
    "sev.Critical": "Critical", "sev.High": "High", "sev.Medium": "Medium", "sev.Low": "Low", "sev.Info": "Info",
    "action.CleanUp": "Clean up", "action.Adjust": "Adjust", "action.Implement": "Implement",
    "action.Investigate": "Investigate", "action.Monitor": "Monitor", "action.Decommission": "Decommission",
    "action.AcceptRisk": "Accept risk", "action.NeedsEvidence": "Needs evidence",
    "action.ReadyForCleanup": "Ready to clean", "action.GenerateScript": "Generate script",
    "status.New": "New", "status.Open": "Open", "status.Recurring": "Recurring", "status.Resolved": "Resolved",
    "status.AcceptedRisk": "Accepted", "coverage.StandardOrFull": "Full", "coverage.Partial": "Partial",
    "coverage.NoDirectory": "No directory", "coverage.NoCollection": "No assessment",
    "obj.User": "Users", "obj.Computer": "Computers", "obj.Group": "Groups",
    "obj.OrganizationalUnit": "OUs", "obj.GroupPolicyContainer": "GPOs", "obj.Domain": "Domain",
    "drawer.impact": "Impact", "drawer.action": "Recommended action", "drawer.manual": "Manual steps",
    "drawer.ps": "Remediation (PowerShell)", "drawer.copy": "Copy", "drawer.copied": "Copied!",
    "drawer.psNote": "Run the preview first (it changes nothing); review the result; only then run the apply command.",
    "drawer.psPreview": "1) Preview (WhatIf) — changes nothing",
    "drawer.psPreviewHint": "Simulates the action and shows what would change. Safe to run.",
    "drawer.psApply": "2) Apply fix — performs the change",
    "drawer.psApplyHint": "Actually performs the action. Run only after validating the preview and with approval.",
    "drawer.psApplyManual": "No single safe command — follow the manual steps above.",
    "drawer.evidence": "Evidence", "drawer.accept": "Accept risk (exception)",
    "drawer.owner": "Owner", "drawer.justification": "Justification", "drawer.validity": "Validity",
    "drawer.acceptBtn": "Accept risk", "drawer.remove": "Remove exception", "drawer.loading": "Loading…",
    "env.none": "No assessment completed", "env.noneSub": "Point to a domain controller and start the first assessment.",
    "env.domain": "Assessed domain", "env.risksFrom": "active risks from the last assessment.",
    "how.1": "Point to the domain controller.", "how.2": "Test connectivity (LDAPS recommended).",
    "how.3": "Choose the rule profile.", "how.4": "Start the assessment (read-only).",
    "how.5": "Risks show up in the Risks tab when done.",
    "pager.prev": "Previous", "pager.next": "Next", "pager.of": "of",
    "exc.need": "Provide owner and justification.", "days.90": "90 days", "days.180": "180 days", "days.365": "1 year",
    "ev.ageDays": "Age (days)", "ev.lastLogon": "Last logon", "ev.pwdLastSet": "Password set",
    "ev.thresholdDays": "Threshold (days)", "ev.privileged": "Privileged", "ev.distinguishedName": "DN",
    "ev.measuredValue": "Measured value", "ev.expectedValue": "Expected value", "ev.domain": "Domain",
    "ev.privilegedGroups": "Privileged groups", "ev.objectType": "Object type",
    "ev.operatingSystem": "Operating system", "ev.whenChanged": "Changed", "ev.sam": "Account",
    "ev.displayName": "Display name", "ev.objectSid": "SID", "ev.servicePrincipalName": "SPN",
    "ev.memberCount": "Members", "ev.ageDays": "Age (days)", "ev.count": "Count", "ev.sample": "Examples",
    "ev.enabled": "Enabled", "ev.retentionDays": "Retention (days)", "ev.versionNumber": "Version", "ev.flags": "Flags",
    "assess.history": "Previous assessments",
    "runs.when": "Date", "runs.operator": "Operator", "runs.executedAs": "Run as",
    "runs.coverage": "Coverage", "runs.objects": "Objects", "runs.findings": "Risks",
    "runs.empty": "No assessment yet",
    "fresh.current": "Current", "fresh.stale": "Stale",
    "col.collected": "Assessed on",
    "reassess.selected": "Re-assess selected", "reassess.all": "Re-assess all (in list)",
    "reassess.clear": "Cancel re-assessment", "reassess.banner": "Targeted re-assessment of {n} selected object(s). Confirm the target and credentials, then start.",
    "reassess.none": "Select at least one risk to re-assess.",
    "res.Fixed": "Fixed", "res.RemovedInRecycleBin": "Removed (in AD Recycle Bin)", "res.RemovedConfirmed": "Removed (confirmed)",
    "drawer.resolution": "Resolution reason",
    "drawer.history": "Object history", "drawer.firstSeen": "First seen",
    "drawer.lastSeen": "Last seen", "drawer.runsSeen": "Observations", "drawer.freshness": "Freshness",
    "profile.builtin": "(built-in)", "profile.custom": "(custom)",
    "settings.builtinHint": "Built-in profile is read-only. Use 'Save as…' to create an editable one.",
    "settings.customHint": "Custom profile — edit the fields and click Save.",
    "settings.new": "New profile", "settings.scope": "Assessment scope",
    "settings.newName": "Name (new/duplicate)", "settings.needName": "Enter a name for the profile.", "settings.exists": "A profile with that name already exists.",
    "settings.saved": "Profile saved.", "settings.activated": "Profile activated.", "settings.confirmDelete": "Click again to delete",
    "settings.scopeNote": "Collection is automatic: we assess only the object types required by the active rules below.",
    "confirm.save": "Save changes to profile \"{name}\"?", "confirm.delete": "Delete profile \"{name}\"? This cannot be undone.",
    "ops.audit": "Action audit log", "audit.when": "When", "audit.actor": "Operator", "audit.action": "Action", "audit.target": "Target", "audit.result": "Result", "audit.empty": "No actions recorded",
    "auth.login": "Sign in", "auth.create": "Create administrator", "auth.sub": "Access to the identity operations portal.", "auth.subSetup": "First run: set the local administrator (8+ char password).", "auth.user": "Username", "auth.pass": "Password", "auth.logout": "Sign out", "auth.bad": "Invalid username or password.",
    "sched.title": "Automatic collection", "sched.note": "Scheduled collection runs as the service identity (gMSA) over Kerberos/LDAPS — no password is stored.", "sched.enabled": "Enabled", "sched.on": "On", "sched.off": "Off", "sched.frequency": "Frequency", "sched.daily": "Daily", "sched.weekly": "Weekly", "sched.interval": "Every N hours", "sched.time": "Time (HH:MM)", "sched.intervalHours": "Interval (hours)", "sched.host": "Domain controller", "sched.profile": "Rule profile", "sched.activeProfile": "Currently active profile", "sched.weekdays": "Weekdays", "sched.identity": "Collection identity", "sched.last": "Last run", "sched.next": "Next run", "sched.test": "Test connectivity", "sched.saved": "Schedule saved.",
    "day.mon": "Mon", "day.tue": "Tue", "day.wed": "Wed", "day.thu": "Thu", "day.fri": "Fri", "day.sat": "Sat", "day.sun": "Sun",
    "portable.badge": "Portable",
    "notif.title": "Morning digest", "notif.note": "Sends the rounds summary (what changed, new risks, indicators) at the end of the scheduled collection. The information reaches you — no need to remember to open the portal.", "notif.policy": "When to send", "notif.onlyActivity": "Only when there is activity", "notif.always": "Always after collection", "notif.lang": "Digest language", "notif.on": "On", "notif.off": "Off", "notif.smtpTitle": "Email (SMTP)", "notif.smtpEnabled": "Enable email", "notif.smtpHost": "SMTP server", "notif.smtpPort": "Port", "notif.smtpTls": "STARTTLS", "notif.smtpUser": "Username", "notif.smtpPass": "Password", "notif.passPlaceholder": "(unchanged)", "notif.smtpFrom": "From", "notif.smtpTo": "Recipients", "notif.webhookTitle": "Webhook (Teams/Slack/automations)", "notif.webhookEnabled": "Enable webhook", "notif.webhookUrl": "Webhook URL", "notif.test": "Send test now", "notif.last": "Last send", "notif.saved": "Notifications saved.", "notif.testing": "Sending test…", "notif.skipped": "Skipped", "notif.noChannel": "No channel enabled.",
    "svc.title": "Service configuration", "svc.note": "Defines how the Windows service runs. For secure scheduled collection, use a gMSA (account name only; Windows manages the password).", "svc.curIdentity": "Current identity", "svc.curStartup": "Current startup", "svc.startup": "Startup type", "svc.startAuto": "Automatic", "svc.startDelayed": "Automatic (delayed)", "svc.startManual": "Manual", "svc.identity": "Identity (Log On As)", "svc.idLocalSystem": "LocalSystem", "svc.idGmsa": "Managed account (gMSA)", "svc.account": "gMSA account", "svc.apply": "Apply configuration", "svc.restart": "Restart the service for the new account to take effect.",
    "exc.expires": "Expires", "exc.empty": "No exceptions registered", "exc.remove": "Remove", "exc.newName": "New profile name:"
  }
};

// Conteudo das regras (titulo, impacto, passos) por ruleId, em PT e EN.
const RULES = {
  "ADCLN-USER-STALE-001": {
    pt: { title: "Conta de usuário habilitada sem uso recente",
      impact: "Contas ativas sem uso ampliam a superfície de ataque e podem ser sequestradas sem detecção.",
      manual: [
        "Pré-requisito: estação com RSAT/ADUC e uma conta com permissão de escrita sobre o objeto.",
        "Confirme a real inatividade: lastLogonTimestamp é aproximado (replica a cada ~9-14 dias). Se possível, verifique o lastLogon real em cada DC ou eventos 4624.",
        "Verifique se NÃO é conta de serviço/aplicação: cheque o atributo servicePrincipalName e a descrição antes de qualquer ação.",
        "Abra 'Usuários e Computadores do Active Directory' (dsa.msc).",
        "Menu Exibir > marque 'Recursos avançados'.",
        "Use Ação > Localizar (ou navegue até a OU) e selecione a conta.",
        "Botão direito na conta > 'Desabilitar conta'. Registre a mudança no chamado/owner.",
        "Mova a conta para uma OU de quarentena (ex.: OU=Quarentena) para isolar GPOs e escopo.",
        "Aguarde o período de retenção definido (ex.: 30-90 dias), monitorando reclamações do usuário/owner.",
        "Confirme que a Lixeira do AD está habilitada (recuperabilidade); então botão direito > Excluir."
      ] },
    en: { title: "Enabled user account with no recent use",
      impact: "Active unused accounts widen the attack surface and can be hijacked without detection.",
      manual: [
        "Prerequisite: a workstation with RSAT/ADUC and an account with write permission on the object.",
        "Confirm real inactivity: lastLogonTimestamp is approximate (replicates every ~9-14 days). If possible, check real lastLogon per DC or 4624 events.",
        "Make sure it is NOT a service/app account: check servicePrincipalName and the description first.",
        "Open 'Active Directory Users and Computers' (dsa.msc).",
        "View menu > enable 'Advanced Features'.",
        "Use Action > Find (or browse the OU) and select the account.",
        "Right-click the account > 'Disable Account'. Log the change in the ticket/owner.",
        "Move the account to a quarantine OU (e.g., OU=Quarantine) to isolate GPOs and scope.",
        "Wait the defined retention period (e.g., 30-90 days), monitoring user/owner complaints.",
        "Confirm AD Recycle Bin is enabled (recoverability); then right-click > Delete."
      ] }
  },
  "ADCLN-COMP-STALE-003": {
    pt: { title: "Computador habilitado sem atividade recente",
      impact: "Computadores obsoletos são inventário morto e têm credenciais de máquina não rotacionadas.",
      manual: [
        "Pré-requisito: RSAT/ADUC e permissão de escrita sobre o objeto de computador.",
        "Confirme a inatividade por lastLogonTimestamp e pwdLastSet (a máquina troca a senha a cada 30 dias por padrão; senha antiga = provavelmente offline).",
        "Garanta que NÃO é um Domain Controller, servidor de aplicação crítico, nó de cluster ou appliance.",
        "Abra dsa.msc e em Exibir marque 'Recursos avançados'.",
        "Localize o objeto de computador (Ação > Localizar ou navegue até a OU).",
        "Botão direito > 'Desabilitar conta' e registre a ação.",
        "Mova para a OU de quarentena.",
        "Aguarde a retenção e confirme que a máquina não voltou (nenhum logon/atualização de senha novos).",
        "Com a Lixeira do AD habilitada, botão direito > Excluir."
      ] },
    en: { title: "Enabled computer with no recent activity",
      impact: "Stale computers are dead inventory and carry non-rotated machine credentials.",
      manual: [
        "Prerequisite: RSAT/ADUC and write permission on the computer object.",
        "Confirm inactivity via lastLogonTimestamp and pwdLastSet (machines rotate their password every 30 days by default; an old password usually means offline).",
        "Ensure it is NOT a Domain Controller, critical app server, cluster node or appliance.",
        "Open dsa.msc and enable View > 'Advanced Features'.",
        "Find the computer object (Action > Find or browse the OU).",
        "Right-click > 'Disable Account' and log the action.",
        "Move it to the quarantine OU.",
        "Wait the retention period and confirm the machine has not returned (no new logon/password update).",
        "With AD Recycle Bin enabled, right-click > Delete."
      ] }
  },
  "ADCLN-USER-DISABLED-RETENTION-002": {
    pt: { title: "Conta desabilitada retida além da política",
      impact: "Objetos desabilitados acumulados dificultam a auditoria e podem ser reativados indevidamente.",
      manual: [
        "Confirme há quanto tempo está desabilitada (atributo whenChanged) e que excede a política de retenção.",
        "Verifique dependências: participação em grupos, ACLs/ACEs com o SID, caixa de correio, owner de objetos.",
        "Faça backup do objeto antes de excluir, ex.: ldifde -d \"<DN>\" -f backup.ldf",
        "Abra dsa.msc, ative Exibir > 'Recursos avançados' e localize a conta.",
        "Confirme que a Lixeira do AD está habilitada (Get-ADOptionalFeature 'Recycle Bin Feature').",
        "Botão direito na conta > Excluir.",
        "Registre a exclusão (data, responsável, chamado)."
      ] },
    en: { title: "Disabled account retained beyond policy",
      impact: "Accumulated disabled objects hinder auditing and may be re-enabled improperly.",
      manual: [
        "Confirm how long it has been disabled (whenChanged attribute) and that it exceeds the retention policy.",
        "Check dependencies: group membership, ACLs/ACEs with the SID, mailbox, object ownership.",
        "Back up the object before deleting, e.g.: ldifde -d \"<DN>\" -f backup.ldf",
        "Open dsa.msc, enable View > 'Advanced Features' and find the account.",
        "Confirm AD Recycle Bin is enabled (Get-ADOptionalFeature 'Recycle Bin Feature').",
        "Right-click the account > Delete.",
        "Log the deletion (date, owner, ticket)."
      ] }
  },
  "ADAUTH-MAQ-009": {
    pt: { title: "MachineAccountQuota permite ingresso por usuários comuns",
      impact: "Usuários sem privilégio podem criar contas de computador, facilitando caminhos de escalada (ex.: RBCD).",
      manual: [
        "Avalie dependências: processos que ingressam máquinas com conta de usuário comum (MDT, Autopilot, Intune, scripts de provisionamento).",
        "Defina uma alternativa: delegue o direito 'Criar objetos de computador' na OU alvo a um grupo aprovado (ex.: Operadores de Ingresso).",
        "Abra 'Editar ADSI' (adsiedit.msc) e conecte-se ao 'Contexto de nomenclatura padrão'.",
        "Selecione o objeto raiz do domínio (DC=...), botão direito > Propriedades.",
        "Localize o atributo ms-DS-MachineAccountQuota e edite o valor para 0.",
        "Aplique e valide: Get-ADObject (Get-ADDomain).DistinguishedName -Properties ms-DS-MachineAccountQuota",
        "Teste o fluxo de provisionamento usando a conta/grupo delegado."
      ] },
    en: { title: "MachineAccountQuota allows join by regular users",
      impact: "Unprivileged users can create computer accounts, enabling escalation paths (e.g., RBCD).",
      manual: [
        "Assess dependencies: processes that join machines with a regular user account (MDT, Autopilot, Intune, provisioning scripts).",
        "Define an alternative: delegate the 'Create Computer objects' right on the target OU to an approved group (e.g., Join Operators).",
        "Open 'ADSI Edit' (adsiedit.msc) and connect to the 'Default naming context'.",
        "Select the domain root object (DC=...), right-click > Properties.",
        "Find the ms-DS-MachineAccountQuota attribute and set the value to 0.",
        "Apply and validate: Get-ADObject (Get-ADDomain).DistinguishedName -Properties ms-DS-MachineAccountQuota",
        "Test the provisioning flow using the delegated account/group."
      ] }
  },
  "ADAUTH-KRB-PREAUTH-006": {
    pt: { title: "Conta sem pré-autenticação Kerberos (AS-REP roasting)",
      impact: "Permite extrair material para quebra de senha offline (AS-REP roasting).",
      manual: [
        "Verifique por que a pré-autenticação foi desabilitada (alguns sistemas legados exigem; confirme com o owner).",
        "Abra dsa.msc e localize a conta.",
        "Botão direito > Propriedades > aba 'Conta'.",
        "Em 'Opções de conta', desmarque 'Não exigir pré-autenticação Kerberos'.",
        "Clique em Aplicar/OK.",
        "Valide que o logon do usuário/serviço continua funcionando.",
        "Se a conta for sensível, redefina a senha (ela pode ter sido exposta a AS-REP roasting) com uma senha forte."
      ] },
    en: { title: "Account without Kerberos pre-authentication (AS-REP roasting)",
      impact: "Allows extracting material for offline password cracking (AS-REP roasting).",
      manual: [
        "Check why pre-authentication was disabled (some legacy systems require it; confirm with the owner).",
        "Open dsa.msc and find the account.",
        "Right-click > Properties > 'Account' tab.",
        "Under 'Account options', uncheck 'Do not require Kerberos preauthentication'.",
        "Click Apply/OK.",
        "Validate that the user/service logon still works.",
        "If the account is sensitive, reset the password (it may have been exposed to AS-REP roasting) with a strong one."
      ] }
  },
  "ADSVC-UNCONSTR-004": {
    pt: { title: "Delegação Kerberos irrestrita (unconstrained)",
      impact: "Permite que o host capture e reutilize TGTs de qualquer usuário que se autentique nele.",
      manual: [
        "Identifique de quais serviços o objeto realmente precisa delegar (mapeie os SPNs de destino).",
        "Abra dsa.msc, ative Exibir > 'Recursos avançados' e localize o objeto (usuário/computador).",
        "Botão direito > Propriedades > aba 'Delegação'.",
        "Se a delegação não for necessária, selecione 'Não confiar neste computador para delegação'.",
        "Se for necessária, selecione 'Confiar somente para delegação a serviços especificados' (constrained) e adicione apenas os SPNs aprovados.",
        "Clique em Aplicar/OK e valide o funcionamento do serviço dependente.",
        "Para contas Tier 0, marque 'A conta é sensível e não pode ser delegada' e considere o grupo 'Protected Users'."
      ] },
    en: { title: "Unconstrained Kerberos delegation",
      impact: "Lets the host capture and reuse TGTs of any user that authenticates to it.",
      manual: [
        "Identify which services the object actually needs to delegate to (map the target SPNs).",
        "Open dsa.msc, enable View > 'Advanced Features' and find the object (user/computer).",
        "Right-click > Properties > 'Delegation' tab.",
        "If delegation is not needed, select 'Do not trust this computer for delegation'.",
        "If needed, select 'Trust for delegation to specified services only' (constrained) and add only the approved SPNs.",
        "Click Apply/OK and validate the dependent service.",
        "For Tier 0 accounts, set 'Account is sensitive and cannot be delegated' and consider the 'Protected Users' group."
      ] }
  },
  "ADPRV-T0-GROUPS-001": {
    pt: { title: "Membro de grupo privilegiado (Tier 0)",
      impact: "Excesso de membros em grupos Tier 0 amplia o risco de comprometimento total do domínio.",
      manual: [
        "Liste a participação atual: Get-ADPrincipalGroupMembership -Identity \"<conta>\" e confirme a justificativa com o owner.",
        "Verifique se já existe uma conta administrativa dedicada (não a conta diária do usuário) que deveria ter esse acesso.",
        "Abra dsa.msc e localize o grupo privilegiado (ex.: Domain Admins, Enterprise Admins, Administrators).",
        "Botão direito no grupo > Propriedades > aba 'Membros'.",
        "Selecione o membro não aprovado e clique em 'Remover'.",
        "Conceda o mínimo necessário via grupo de menor privilégio / PAM / acesso just-in-time, quando aplicável.",
        "Clique em Aplicar/OK e valide que as operações legítimas continuam funcionando.",
        "Documente a justificativa para os membros que permanecerem."
      ] },
    en: { title: "Member of a privileged group (Tier 0)",
      impact: "Excess members in Tier 0 groups increase the risk of full domain compromise.",
      manual: [
        "List current membership: Get-ADPrincipalGroupMembership -Identity \"<account>\" and confirm the justification with the owner.",
        "Check whether a dedicated admin account (not the user's daily account) should hold this access instead.",
        "Open dsa.msc and find the privileged group (e.g., Domain Admins, Enterprise Admins, Administrators).",
        "Right-click the group > Properties > 'Members' tab.",
        "Select the unapproved member and click 'Remove'.",
        "Grant least privilege via a lower-privilege group / PAM / just-in-time access where applicable.",
        "Click Apply/OK and validate that legitimate operations still work.",
        "Document the justification for any members that remain."
      ] }
  },
  "ADPRV-KRBTGT-AGE-010": {
    pt: { title: "Senha da conta krbtgt sem rotação recente",
      impact: "Senha antiga do krbtgt facilita ataques de Golden Ticket por longos períodos.",
      manual: [
        "Planeje uma janela de manutenção. A rotação deve ser DUPLA (dois resets), com intervalo maior que a replicação completa e que o tempo de vida do TGT (padrão 10h).",
        "Verifique a saúde da replicação antes: repadmin /replsummary e repadmin /showrepl.",
        "Baixe o script oficial da Microsoft de reset do krbtgt (New-KrbtgtKeys.ps1 / Reset-KrbtgtKeyInteractive).",
        "Execute a 1ª rotação e force a replicação: repadmin /syncall /AdeP. Confirme que todos os DCs receberam.",
        "Aguarde além do tempo de vida do TGT (>10h) e da convergência da replicação.",
        "Execute a 2ª rotação e force a replicação novamente.",
        "Valide logons, serviços, contas de serviço e tarefas agendadas após cada etapa."
      ] },
    en: { title: "krbtgt account password not rotated recently",
      impact: "An old krbtgt password enables Golden Ticket attacks for long periods.",
      manual: [
        "Plan a maintenance window. The rotation must be DOUBLE (two resets), with an interval larger than full replication and the TGT lifetime (default 10h).",
        "Check replication health first: repadmin /replsummary and repadmin /showrepl.",
        "Download the official Microsoft krbtgt reset script (New-KrbtgtKeys.ps1 / Reset-KrbtgtKeyInteractive).",
        "Run the 1st rotation and force replication: repadmin /syncall /AdeP. Confirm all DCs received it.",
        "Wait beyond the TGT lifetime (>10h) and replication convergence.",
        "Run the 2nd rotation and force replication again.",
        "Validate logons, services, service accounts and scheduled tasks after each step."
      ] },
  },
  "ADGOV-RECYCLEBIN-001": {
    pt: { title: "Lixeira do AD desabilitada ou retenção insuficiente",
      impact: "Sem a Lixeira do AD (ou com retenção curta) objetos excluídos por engano ou por ataque não podem ser recuperados, e a investigação de exclusões fica comprometida.",
      manual: [
        "Pré-requisito: nível funcional da floresta Windows Server 2008 R2 ou superior. A ativação é IRREVERSÍVEL.",
        "Confirme o estado atual: Get-ADOptionalFeature -Filter \"name -eq 'Recycle Bin Feature'\" (veja EnabledScopes).",
        "Planeje: após habilitar, objetos excluídos passam a ser recuperáveis pela janela de msDS-deletedObjectLifetime (padrão = tombstoneLifetime, tipicamente 180 dias).",
        "Habilite pelo Centro Administrativo do AD (dsac.exe) > clique no domínio > 'Habilitar Lixeira', OU via PowerShell (valide com -WhatIf antes).",
        "Se já habilitada porém com retenção curta, ajuste msDS-deletedObjectLifetime conforme a política de retenção aprovada.",
        "Valide a recuperação: exclua um objeto de teste e restaure-o pelo Centro Administrativo do AD."
      ] },
    en: { title: "AD Recycle Bin disabled or insufficient retention",
      impact: "Without the AD Recycle Bin (or with short retention) objects deleted by mistake or by an attacker cannot be recovered, and deletion investigations are compromised.",
      manual: [
        "Prerequisite: forest functional level Windows Server 2008 R2 or higher. Enabling is IRREVERSIBLE.",
        "Confirm current state: Get-ADOptionalFeature -Filter \"name -eq 'Recycle Bin Feature'\" (check EnabledScopes).",
        "Plan: once enabled, deleted objects become recoverable within the msDS-deletedObjectLifetime window (defaults to tombstoneLifetime, typically 180 days).",
        "Enable via the AD Administrative Center (dsac.exe) > click the domain > 'Enable Recycle Bin', OR via PowerShell (validate with -WhatIf first).",
        "If already enabled but with short retention, adjust msDS-deletedObjectLifetime per the approved retention policy.",
        "Validate recovery: delete a test object and restore it from the AD Administrative Center."
      ] },
  // Regras novas: PT vem do catálogo (fallback); aqui só o EN.
  "ADCLN-GROUP-EMPTY-011": { en: { title: "Empty security group (no members)",
    impact: "Empty groups clutter the directory, hinder least-privilege and can be silently repopulated.",
    manual: ["Confirm the group is not used in ACLs, GPOs or apps (implicit primaryGroupID members do not show in 'member').", "Open dsa.msc and locate the group.", "Document the owner/usage; if truly unused, delete the group."] } },
  "ADCLN-OU-EMPTY-012": { en: { title: "Empty organizational unit (OU)",
    impact: "Empty OUs accumulate dead structure and confuse delegation and GPO scoping.",
    manual: ["Confirm no objects are expected soon and no important GPO depends on its link.", "If the OU is protected from accidental deletion, clear the protection first (Object tab > Advanced Features).", "Open dsa.msc and delete the empty OU."] } },
  "ADCLN-GPO-UNLINKED-013": { en: { title: "Unlinked GPO (orphaned)",
    impact: "GPOs with no link apply to nothing: dead maintenance and noise in policy analysis.",
    manual: ["In GPMC confirm the GPO is not linked to domain, OU or site (including disabled links).", "Back up the GPO (Backup-GPO) before deleting.", "Delete the GPO if confirmed unused."] } },
  "ADCLN-GPO-EMPTY-014": { en: { title: "Empty GPO (no settings)",
    impact: "GPOs with no settings (version 0) only add logon processing time with no effect.",
    manual: ["In GPMC confirm there are no Computer nor User settings (version number 0).", "Back up (Backup-GPO) and remove links, if any.", "Delete the empty GPO."] } },
  "ADCLN-GPO-DISABLED-015": { en: { title: "GPO with both sections disabled",
    impact: "A GPO with User and Computer sections disabled has no effect; if still linked it is noise and confuses governance.",
    manual: ["In GPMC check the GPO status (Computer/User Configuration Settings Disabled).", "If intentional and unused, remove links and delete; otherwise re-enable the right section."] } },
  "ADHARD-PWDNOTREQD-016": { en: { title: "Account allows blank password (PASSWD_NOTREQD)",
    impact: "Accounts that do not require a password may have none, enabling trivial access.",
    manual: ["Open dsa.msc and locate the account.", "Remove the 'Password not required' flag and require a strong password reset.", "Validate it is not an app account depending on this behavior."] } },
  "ADHARD-PWDNOEXPIRE-017": { en: { title: "Enabled account with non-expiring password",
    impact: "Passwords that never expire stay valid indefinitely, widening the window for leaked-credential abuse.",
    manual: ["Assess whether the account should follow the expiration policy (service accounts: prefer gMSA).", "Open dsa.msc, Account tab, clear 'Password never expires'.", "Ensure password rotation for service accounts that cannot expire."] } },
  "ADHARD-REVERSIBLEPWD-018": { en: { title: "Account with reversible password encryption",
    impact: "Storing the password reversibly is equivalent to keeping a recoverable plaintext password.",
    manual: ["Open dsa.msc, Account tab, clear 'Store password using reversible encryption'.", "Force a password reset after the change.", "Check GPOs that might re-apply the setting."] } },
  "ADPRV-KERBEROAST-019": { en: { title: "User account with SPN (kerberoastable)",
    impact: "User accounts with an SPN allow requesting service tickets and cracking the password offline (Kerberoasting).",
    manual: ["Prefer group Managed Service Accounts (gMSA), which rotate the password automatically.", "If the user account must stay, set a random 25+ char password.", "Remove unnecessary SPNs and monitor 4769 events with RC4."] } },
  "ADCLN-COMP-LEGACYOS-020": { en: { title: "Computer with unsupported operating system",
    impact: "Out-of-support systems get no security fixes and are easy lateral-movement targets.",
    manual: ["Confirm the host still exists and is needed.", "Plan replacement with a supported OS or isolate the host.", "As an interim measure, disable the computer account after validating."] } },
  "ADPRV-ADMINCOUNT-ORPHAN-021": { en: { title: "Orphaned adminCount=1 (residual AdminSDHolder ACL)",
    impact: "An object marked protected (adminCount=1) but no longer in a privileged group keeps a restrictive ACL and inheritance disabled — a sign of uncleaned old privilege.",
    manual: ["Confirm the object should no longer be protected (not in any Tier 0 group).", "Clear the object's adminCount attribute.", "Re-enable permission inheritance (Security > Advanced > Enable inheritance)."] } },
  "ADGOV-CONFLICT-022": { en: { title: "Replication conflict objects (CNF)",
    impact: "'CNF:' objects come from replication conflicts (same name created on different DCs). They indicate replication problems and clutter the database.",
    manual: ["List the conflict objects and identify the matching 'good' object.", "Reconcile: keep the correct one, migrate dependencies and remove the duplicate (CNF).", "Investigate the replication cause (repadmin /showrepl).", "After large cleanups, consider offline NTDS.dit defrag (ntdsutil files compact to) to reclaim space."] } },
  "ADGOV-LOSTFOUND-023": { en: { title: "Orphaned objects in LostAndFound",
    impact: "Objects in the LostAndFound container lost their parent (e.g., created on one DC while the parent OU was deleted on another). They are orphans to reconcile.",
    manual: ["List the LostAndFound objects and decide to move (to the right OU) or delete.", "Move with Move-ADObject or delete if obsolete.", "After large cleanups, run semantic database analysis (ntdsutil 'semantic database analysis' > go fixup) and consider offline defrag."] } }
  }
};
function ruleText(ruleId, field, fallback) {
  const r = RULES[ruleId]; if (!r) return fallback;
  const loc = r[lang] || r.pt; return (loc && loc[field] !== undefined) ? loc[field] : fallback;
}
function ruleTitle(ruleId, fallback) { return ruleText(ruleId, "title", fallback); }

let lang = localStorage.getItem("adc-lang") || "pt";
function t(key) { return (I18N[lang] && I18N[lang][key]) || I18N.pt[key] || key; }

function applyI18n() {
  document.documentElement.lang = lang === "pt" ? "pt-BR" : "en";
  document.querySelectorAll("[data-i18n]").forEach((el) => { el.textContent = t(el.dataset.i18n); });
  document.querySelectorAll("[data-i18n-ph]").forEach((el) => { el.placeholder = t(el.dataset.i18nPh); });
  const lt = byId("lang-toggle"); if (lt) lt.textContent = lang.toUpperCase();
  const howList = byId("how-list");
  if (howList) howList.innerHTML = ["how.1","how.2","how.3","how.4","how.5"].map((k) => `<li>${t(k)}</li>`).join("");
  updatePageHeader(currentView);
}

// ---------------- theme ----------------
let theme = localStorage.getItem("adc-theme") || "light";
function applyTheme() {
  document.documentElement.dataset.theme = theme;
  const tt = byId("theme-toggle"); if (tt) tt.textContent = theme === "dark" ? "☀" : "◐";
}

// ---------------- helpers ----------------
function byId(id) { return document.getElementById(id); }
function text(id, value) { const el = byId(id); if (el) el.textContent = value; }
function escapeHtml(v) { return String(v ?? "").replace(/[&<>"']/g, (c) => ({ "&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;" }[c])); }
function sevClass(s) { return `sev-${String(s).toLowerCase()}`; }
function sevBadge(s) { return `<span class="sev-badge ${sevClass(s)}">${t("sev." + s)}</span>`; }

// Modo demo: GETs respondem do fixture local (demo-data.js); nada sai para a rede.
let demoMode = sessionStorage.getItem("dx-demo") === "1";
// Modo portátil: sessão única sem login (definido por /auth/me).
let portableMode = false;
async function getJson(url) {
  if (demoMode) {
    const d = window.DEMO_RESOLVE ? window.DEMO_RESOLVE(url) : null;
    if (d) return d;
    throw new Error("demo");
  }
  const r = await fetch(url, { headers: { Accept: "application/json" } }); if (!r.ok) throw new Error(`${r.status}`); return r.json();
}
async function sendJson(url, method, body) {
  if (demoMode) throw new Error(t("demo.readonly"));
  const r = await fetch(url, { method, headers: { "Content-Type": "application/json", Accept: "application/json" }, body: JSON.stringify(body) });
  const p = await r.json().catch(() => ({}));
  if (!r.ok) throw new Error(p?.error ? JSON.stringify(p.error) : `${r.status}`);
  return p;
}
const postJson = (u, b) => sendJson(u, "POST", b);

const endpoints = {
  live: "/health/live", ready: "/health/ready", about: "/api/v1/system/about", shell: "/api/v1/app/shell",
  probe: "/api/v1/collection/probe", runs: "/api/v1/collection/runs", findings: "/api/v1/findings",
  findingDetail: "/api/v1/findings/detail", exception: "/api/v1/findings/exception", exceptions: "/api/v1/exceptions",
  inventory: "/api/v1/inventory", profiles: "/api/v1/settings/profiles", catalog: "/api/v1/rules/catalog",
  runsHistory: "/api/v1/runs", objectHistory: "/api/v1/objects/history", audit: "/api/v1/audit",
  changes: "/api/v1/changes", changesSummary: "/api/v1/changes/summary",
  search: "/api/v1/search", objectDetail: "/api/v1/objects/detail",
  auth: "/api/v1/auth", schedule: "/api/v1/schedule",
  indicators: "/api/v1/indicators", indicatorsCatalog: "/api/v1/indicators/catalog", indicatorValidate: "/api/v1/indicators/validate",
  notifications: "/api/v1/notifications"
};

const CHANGE_LABELS = {
  pt: { ObjectCreated: "Objeto criado", ObjectDeleted: "Objeto excluído", MemberAdded: "Membro adicionado", MemberRemoved: "Membro removido", PrivilegedMemberAdded: "Novo membro privilegiado", PrivilegedMemberRemoved: "Membro privilegiado removido", AccountEnabled: "Conta habilitada", AccountDisabled: "Conta desabilitada", DangerousFlagSet: "Flag perigosa ativada", DangerousFlagCleared: "Flag perigosa removida", AdminCountChanged: "adminCount alterado", SpnAdded: "SPN adicionado", GpoLinkChanged: "Vínculo de GPO alterado", AttributeChanged: "Atributo alterado" },
  en: { ObjectCreated: "Object created", ObjectDeleted: "Object deleted", MemberAdded: "Member added", MemberRemoved: "Member removed", PrivilegedMemberAdded: "New privileged member", PrivilegedMemberRemoved: "Privileged member removed", AccountEnabled: "Account enabled", AccountDisabled: "Account disabled", DangerousFlagSet: "Dangerous flag set", DangerousFlagCleared: "Dangerous flag cleared", AdminCountChanged: "adminCount changed", SpnAdded: "SPN added", GpoLinkChanged: "GPO link changed", AttributeChanged: "Attribute changed" }
};
function changeLabel(type) { return (CHANGE_LABELS[lang] && CHANGE_LABELS[lang][type]) || type; }

const AUDIT_ACTIONS = {
  pt: { AssessmentStarted: "Avaliação iniciada", ProfilesSaved: "Perfis salvos", ProfileCreated: "Perfil criado", ProfileUpdated: "Perfil alterado", ProfileDeleted: "Perfil excluído", ProfileActivated: "Perfil ativado", ExceptionAdded: "Exceção registrada", ExceptionRemoved: "Exceção removida", ScheduleSaved: "Agendamento salvo", ScheduleTested: "Teste de conectividade", ServiceConfigChanged: "Configuração do serviço alterada", AdminCreated: "Administrador criado", LoginSuccess: "Login efetuado", LoginFailed: "Login falhou", Logout: "Logout" },
  en: { AssessmentStarted: "Assessment started", ProfilesSaved: "Profiles saved", ProfileCreated: "Profile created", ProfileUpdated: "Profile updated", ProfileDeleted: "Profile deleted", ProfileActivated: "Profile activated", ExceptionAdded: "Exception added", ExceptionRemoved: "Exception removed", ScheduleSaved: "Schedule saved", ScheduleTested: "Connectivity test", ServiceConfigChanged: "Service config changed", AdminCreated: "Administrator created", LoginSuccess: "Sign in", LoginFailed: "Sign-in failed", Logout: "Sign out" }
};

const state = { shell: null, live: null, ready: null, about: null };
const findingsState = { view: "active", category: "", search: "", sortKey: "risk", sortDir: "desc", page: 0, items: [], selected: new Set() };
// Reavaliacao dirigida pendente (object keys selecionados) + ultimo alvo usado.
let pendingFocus = [];
let profilesState = null, ruleCatalog = [], currentView = "today";
let runsCount = 0;
let indicatorsCache = [], indicatorCatalog = [];
const PAGE_SIZE = 50;

// ---------------- header / nav ----------------
function updatePageHeader(view) {
  currentView = view;
  text("page-title", t("nav." + ({ today:"today", overview:"overview", collect:"assessments", findings:"risks", timeline:"timeline", inventory:"inventory", settings:"settings", operations:"operations" }[view])));
  text("page-subtitle", t("sub." + view));
}
function activateView(view) {
  document.querySelectorAll(".nav-tab").forEach((i) => i.classList.toggle("is-active", i.dataset.view === view));
  document.querySelectorAll(".view").forEach((i) => i.classList.toggle("is-visible", i.id === `view-${view}`));
  updatePageHeader(view);
  if (view === "timeline") loadTimeline();
}

// ---------------- shell render ----------------
function renderMetrics(metrics) {
  const grid = byId("metrics-grid"); if (!grid) return;
  grid.innerHTML = (metrics || []).map((m) => {
    const val = (m.value === null || m.value === undefined) ? "-" : m.value;
    return `<article class="metric-tile"><span>${t("metric." + m.label)}</span><strong>${val}</strong><small>${m.state === "unmeasured" ? "—" : m.unit}</small></article>`;
  }).join("");
}
function renderBacklog(breakdown) {
  const list = byId("backlog-list"); if (!list) return;
  const counts = new Map((breakdown || []).map((i) => [i.severity, i.count]));
  const total = (breakdown || []).reduce((s, i) => s + i.count, 0);
  text("backlog-tag", total > 0 ? `${total}` : "0");
  list.innerHTML = ["Critical","High","Medium","Low","Info"].map((s) => {
    const c = counts.get(s) ?? 0;
    return `<div class="queue-row${c === 0 ? " muted-row" : ""}"><span><span class="dot ${sevClass(s)}"></span>${t("sev." + s)}</span><strong>${c}</strong></div>`;
  }).join("");
}
function renderCategoriesPanel(breakdown) {
  const list = byId("category-list"); if (!list) return;
  const total = (breakdown || []).reduce((s, i) => s + i.count, 0);
  text("category-tag", total > 0 ? `${total}` : "0");
  list.innerHTML = total === 0 ? `<div class="queue-row muted-row"><span>—</span><strong>-</strong></div>`
    : breakdown.slice().sort((a, b) => b.count - a.count).map((i) => `<div class="queue-row"><span>${t("cat." + i.category)}</span><strong>${i.count}</strong></div>`).join("");
}
function renderInventory(items) {
  const grid = byId("inventory-grid"); if (!grid) return;
  if (!items || items.length === 0) { grid.innerHTML = `<section class="inventory-tile"><span>—</span><strong>-</strong><small>${t("env.none")}</small></section>`; return; }
  grid.innerHTML = items.map((i) => {
    const note = `${t("col.collected")}: ${fmtDate(i.lastObservedAt)}`;
    return `<section class="inventory-tile"><span>${t("obj." + i.objectType)}</span><strong>${i.totalCount}</strong><small>${note}</small></section>`;
  }).join("");
}

function healthFromScore(s) { return s == null ? "" : (s >= 90 ? "green" : s >= 70 ? "yellow" : "red"); }
function healthClass(h) { return h === "green" ? "ok" : h === "yellow" ? "warn" : h === "red" ? "crit" : ""; }

function renderScoreCards(shell) {
  const grid = byId("score-cards"); if (!grid) return;
  const idn = shell?.identityScore ?? null;
  const t0 = shell?.tier0Score ?? null;
  const crit = shell?.criticalFindings ?? 0;
  const sync = shell?.dataContext?.latestRunCompletedAt || shell?.dataContext?.latestRunStartedAt;
  const card = (cls, label, value, go) =>
    `<button class="score-card ${cls}" data-go="${go || ""}"><span>${label}</span><strong>${value}</strong></button>`;
  grid.innerHTML = [
    card(healthClass(shell?.health), t("score.identity"), idn == null ? "—" : `${idn}%`, "findings"),
    card(healthClass(healthFromScore(t0)), t("score.tier0"), t0 == null ? "—" : `${t0}%`, "findings"),
    card(crit > 0 ? "crit" : "ok", t("score.critical"), crit, "findings"),
    card("", t("score.lastSync"), sync ? fmtDate(sync) : "—", "timeline")
  ].join("");
  grid.querySelectorAll("[data-go]").forEach((b) => b.addEventListener("click", () => { const v = b.dataset.go; if (v) activateView(v); }));
}

// ---------------- indicadores operacionais ----------------
async function loadIndicators() {
  const grid = byId("indicators-grid"), empty = byId("indicators-empty"); if (!grid) return;
  try {
    const data = await getJson(endpoints.indicators);
    indicatorsCache = data.items || [];
    const total = indicatorsCache.reduce((s, i) => s + i.count, 0);
    text("indicators-tag", `${total}`);
    if (indicatorsCache.length === 0) { grid.innerHTML = ""; renderIndicatorsEmpty(empty); return; }
    if (empty) empty.hidden = true;
    grid.innerHTML = indicatorsCache.map((i) =>
      `<button class="morning-card${i.count > 0 ? "" : " muted-card"}" data-ind="${escapeHtml(i.id)}"><strong>${i.count}</strong><span>${escapeHtml(i.title)}</span></button>`).join("");
    grid.querySelectorAll("[data-ind]").forEach((b) => b.addEventListener("click", () => openIndicatorDetail(b.dataset.ind)));
  } catch { indicatorsCache = []; grid.innerHTML = ""; renderIndicatorsEmpty(empty); }
}

function renderIndicatorsEmpty(el) {
  if (!el) return;
  el.hidden = false;
  el.innerHTML = `<strong>${t("indicators.empty")}</strong><br><button class="secondary-button" type="button" data-goto="settings">${t("ind.emptyCta")}</button>`;
  el.querySelector("[data-goto]")?.addEventListener("click", () => activateView("settings"));
}

function openIndicatorDetail(id) {
  const drawer = byId("object-drawer"), content = byId("object-drawer-content"); if (!drawer || !content) return;
  const ind = indicatorsCache.find((i) => i.id === id); if (!ind) return;
  const items = ind.items || [];
  const rows = items.length === 0 ? `<li class="muted-row">—</li>` : items.map((it) =>
    `<li><span>${escapeHtml(it.display)}</span>${it.detail ? `<small>${escapeHtml(it.detail)}</small>` : ""}<code>${escapeHtml(it.distinguishedName)}</code>${it.objectSid ? `<code>${escapeHtml(it.objectSid)}</code>` : ""}</li>`).join("");
  content.innerHTML = `<div class="drawer-head"><h2>${escapeHtml(ind.title)}</h2>
    <p class="drawer-sub">${escapeHtml(ind.category)} · ${ind.count} ${t("ind.matches")}</p></div>
    <ul class="evidence-list ind-item-list">${rows}</ul>`;
  drawer.hidden = false;
}

async function loadMorning() {
  const grid = byId("morning-grid"), empty = byId("morning-empty"); if (!grid) return;
  try {
    const data = await getJson(`${endpoints.changesSummary}?sinceHours=24`);
    const items = data.items || [];
    const newFindings = data.newFindings || 0;
    // Indicadores operacionais "do dia" (senhas vencendo/expiradas, contas bloqueadas).
    const opIds = ["IND-ACCT-LOCKED", "IND-PWD-EXPIRING", "IND-PWD-EXPIRED"];
    const opInds = (indicatorsCache || []).filter((i) => opIds.includes(i.id) && i.count > 0);
    const opTotal = opInds.reduce((s, i) => s + i.count, 0);
    const total = items.reduce((s, i) => s + i.count, 0) + newFindings + opTotal;
    text("morning-tag", `${total}`);
    if (total === 0) { grid.innerHTML = ""; renderMorningEmpty(empty); return; }
    if (empty) empty.hidden = true;
    const opCards = opInds.map((i) =>
      `<button class="morning-card morning-op" data-ind="${escapeHtml(i.id)}"><strong>${i.count}</strong><span>${escapeHtml(i.title)}</span></button>`).join("");
    const order = ["PrivilegedMemberAdded","ObjectDeleted","DangerousFlagSet","AdminCountChanged","SpnAdded","AccountEnabled","ObjectCreated","MemberAdded","MemberRemoved","GpoLinkChanged","AccountDisabled","PrivilegedMemberRemoved","DangerousFlagCleared","AttributeChanged"];
    items.sort((a, b) => order.indexOf(a.changeType) - order.indexOf(b.changeType));
    // Card de riscos novos (apontamentos das ultimas 24h ainda ativos) em primeiro.
    const riskCard = newFindings > 0
      ? `<button class="morning-card morning-risk" data-newrisks="1"><strong>${newFindings}</strong><span>${escapeHtml(t("morning.newRisks"))}</span></button>`
      : "";
    grid.innerHTML = opCards + riskCard + items.map((i) => `<button class="morning-card" data-type="${escapeHtml(i.changeType)}"><strong>${i.count}</strong><span>${escapeHtml(changeLabel(i.changeType))}</span></button>`).join("");
    grid.querySelectorAll("[data-ind]").forEach((b) => b.addEventListener("click", () => openIndicatorDetail(b.dataset.ind)));
    grid.querySelectorAll("[data-type]").forEach((b) => b.addEventListener("click", () => { timelineState.type = b.dataset.type; timelineState.window = 24; const w = byId("timeline-window"); if (w) w.value = "24"; activateView("timeline"); }));
    grid.querySelector("[data-newrisks]")?.addEventListener("click", () => {
      findingsState.view = "active"; findingsState.sortKey = "collected"; findingsState.sortDir = "desc";
      document.querySelectorAll(".status-tab").forEach((x) => x.classList.toggle("is-active", x.dataset.status === "active"));
      activateView("findings"); loadFindings();
    });
  } catch { grid.innerHTML = ""; renderMorningEmpty(empty); }
}

// Estado vazio que ensina o proximo passo: sem coleta -> coletar;
// so a baseline -> agendar (a timeline nasce na 2a coleta); senao, sem mudancas mesmo.
function renderMorningEmpty(el) {
  if (!el) return;
  el.hidden = false;
  if (runsCount === 0) {
    el.innerHTML = `<strong>${t("morning.first")}</strong><br><button class="primary-button" type="button" data-goto="collect">${t("morning.goCollect")}</button>`;
  } else if (runsCount === 1) {
    el.innerHTML = `<strong>${t("morning.second")}</strong><br><button class="primary-button" type="button" data-goto="operations">${t("morning.goSchedule")}</button>`;
  } else {
    el.innerHTML = `<strong>${t("morning.empty")}</strong>`;
  }
  el.querySelectorAll("[data-goto]").forEach((b) => b.addEventListener("click", () => activateView(b.dataset.goto)));
}

async function renderToday(shell) {
  const hasRun = shell?.dataContext?.hasCompletedCollection === true;
  const domain = shell?.dataContext?.domainName;
  const last = shell?.dataContext?.latestRunCompletedAt || shell?.dataContext?.latestRunStartedAt;
  text("today-domain", hasRun ? (domain || t("env.domain")) : t("env.none"));
  text("today-status", hasRun ? `${shell?.activeFindings ?? 0} ${t("env.risksFrom")}` : t("env.noneSub"));
  text("today-lastrun", `${t("today.last")}: ${last ? fmtDate(last) : "—"}`);
  const sched = byId("today-sched");
  if (!sched) return;
  try {
    const c = await getJson(endpoints.schedule);
    if (c.enabled) {
      sched.textContent = c.nextRunAt ? `${t("today.next")}: ${fmtDate(c.nextRunAt)}` : t("today.schedOn");
    } else {
      sched.innerHTML = `<button class="secondary-button" type="button" data-goto="operations">${t("morning.goSchedule")}</button>`;
      sched.querySelector("[data-goto]")?.addEventListener("click", () => activateView("operations"));
    }
  } catch { sched.textContent = ""; }
}

const timelineState = { window: 24, type: null };
async function loadTimeline() {
  const body = byId("timeline-body"), empty = byId("timeline-empty"); if (!body) return;
  const w = timelineState.window;
  const q = `${endpoints.changes}?limit=500${w ? `&sinceHours=${w}` : ""}${timelineState.type ? `&type=${encodeURIComponent(timelineState.type)}` : ""}`;
  try {
    const data = await getJson(q);
    const items = data.items || [];
    text("timeline-tag", `${items.length}`);
    if (items.length === 0) { body.innerHTML = ""; if (empty) empty.hidden = false; return; }
    if (empty) empty.hidden = true;
    body.innerHTML = items.map((e) => `<button class="timeline-item" data-key="${encodeURIComponent(e.objectKey)}">
      <span class="dot ${sevClass(e.severity)}"></span>
      <span class="tl-time">${fmtDate(e.observedAt)}</span>
      <span class="tl-change">${escapeHtml(changeLabel(e.changeType))}</span>
      <span class="tl-obj">${escapeHtml(e.objectDisplay)}</span>
      <span class="tl-val">${escapeHtml(e.newValue || e.oldValue || "")}</span></button>`).join("");
    body.querySelectorAll("[data-key]").forEach((b) => b.addEventListener("click", () => openObjectDetail(decodeURIComponent(b.dataset.key))));
  } catch { body.innerHTML = ""; if (empty) empty.hidden = false; }
}

let searchTimer = null;
function wireSearch() {
  const inp = byId("global-search"), box = byId("search-results"); if (!inp || !box) return;
  inp.addEventListener("input", () => {
    clearTimeout(searchTimer);
    const q = inp.value.trim();
    if (q.length < 2) { box.hidden = true; box.innerHTML = ""; return; }
    searchTimer = setTimeout(async () => {
      try {
        const data = await getJson(`${endpoints.search}?q=${encodeURIComponent(q)}`);
        const items = data.items || [];
        if (items.length === 0) { box.innerHTML = `<div class="search-empty">${t("search.none")}</div>`; box.hidden = false; return; }
        box.innerHTML = items.map((h) => `<button class="search-hit" data-key="${encodeURIComponent(h.objectKey)}"><span class="hit-type">${t("obj." + h.objectType)}</span><span class="hit-name">${escapeHtml(h.display)}</span><small>${escapeHtml(h.subtitle)}</small></button>`).join("");
        box.hidden = false;
        box.querySelectorAll("[data-key]").forEach((b) => b.addEventListener("click", () => { box.hidden = true; inp.value = ""; openObjectDetail(decodeURIComponent(b.dataset.key)); }));
      } catch { box.hidden = true; }
    }, 250);
  });
  document.addEventListener("click", (e) => { if (!e.target.closest(".global-search")) box.hidden = true; });
}

async function openObjectDetail(key) {
  const drawer = byId("object-drawer"), content = byId("object-drawer-content"); if (!drawer || !content) return;
  content.innerHTML = `<p>${t("drawer.loading")}</p>`; drawer.hidden = false;
  try {
    const d = await getJson(`${endpoints.objectDetail}?key=${encodeURIComponent(key)}`);
    const fieldLabel = (k) => t("field." + k) === ("field." + k) ? k : t("field." + k);
    const fields = (d.fields || []).map((f) => `<li><span>${escapeHtml(fieldLabel(f.label))}</span><code>${escapeHtml(f.value)}</code></li>`).join("");
    const risks = (d.findings || []).map((f) => `<li class="obj-risk" data-fk="${encodeURIComponent(f.stableFindingKey)}">${sevBadge(f.severity)} ${escapeHtml(ruleTitle(f.ruleId, f.title))} <small>${t("status." + f.status)}</small></li>`).join("") || `<li class="muted-row">—</li>`;
    const changes = (d.changes || []).slice(0, 40).map((c) => `<li><span class="dot ${sevClass(c.severity)}"></span> ${fmtDate(c.observedAt)} — ${escapeHtml(changeLabel(c.changeType))} <small>${escapeHtml(c.newValue || c.oldValue || "")}</small></li>`).join("") || `<li class="muted-row">—</li>`;
    content.innerHTML = `
      <div class="drawer-head"><h2>${escapeHtml(d.display)}</h2>
        <p class="drawer-sub">${t("obj." + d.objectType)} · ${escapeHtml(d.distinguishedName)}</p></div>
      <h4>${t("obj.attributes")}</h4><ul class="evidence-list">${fields}</ul>
      <h4>${t("obj.risks")}</h4><ul class="obj-list">${risks}</ul>
      <h4>${t("obj.changes")}</h4><ul class="obj-list">${changes}</ul>`;
    content.querySelectorAll(".obj-risk[data-fk]").forEach((li) => li.addEventListener("click", () => { drawer.hidden = true; openFindingDetail(decodeURIComponent(li.dataset.fk)); }));
  } catch { content.innerHTML = `<p>${t("search.none")}</p>`; }
}

function render() {
  const shell = state.shell;
  const serviceReady = state.live?.status === "Live";
  const storageReady = state.ready?.status === "Ready";
  const hasRun = shell?.dataContext?.hasCompletedCollection === true;
  const domain = shell?.dataContext?.domainName;
  const coverage = shell?.dataContext?.coverageMode ?? "NoCollection";

  text("brand-mode", portableMode ? t("portable.badge") : (shell?.product?.mode ?? "LocalSecure"));
  text("environment-title", hasRun ? (domain ? `${t("env.domain")}: ${domain}` : t("env.domain")) : t("env.none"));
  text("environment-summary", hasRun ? `${shell?.activeFindings ?? 0} ${t("env.risksFrom")}` : t("env.noneSub"));
  text("coverage-mode", t("coverage." + coverage));
  text("latest-run", shell?.dataContext?.latestRunId ? `Run ${shell.dataContext.latestRunId.slice(0, 8)}` : "—");
  text("storage-tag", storageReady ? "OK" : "—");
  text("operation-tag", serviceReady ? "Online" : "Offline");
  text("database-path", shell?.installation?.database ?? "-");

  renderToday(shell);
  renderScoreCards(shell);
  renderMetrics(shell?.metrics ?? []);
  renderBacklog(shell?.severityBreakdown ?? []);
  renderCategoriesPanel(shell?.categoryBreakdown ?? []);

  const raw = byId("raw-status"); if (raw) raw.textContent = JSON.stringify(shell ?? {}, null, 2);

  const light = byId("sidebar-light");
  if (serviceReady && storageReady) { if (light) light.className = "status-light ready"; text("sidebar-status", "Online"); }
  else if (serviceReady) { if (light) light.className = "status-light"; text("sidebar-status", "Online"); }
  else { if (light) light.className = "status-light error"; text("sidebar-status", "Offline"); }
}

async function refresh() {
  try {
    const [live, ready, about, shell] = await Promise.all([getJson(endpoints.live), getJson(endpoints.ready), getJson(endpoints.about), getJson(endpoints.shell)]);
    Object.assign(state, { live, ready, about, shell });
  } catch (e) { state.live = { status: "Unavailable" }; state.ready = { status: "Unavailable" }; state.shell = null; }
  render();
  await Promise.all([loadFindings(), loadInventory(), loadTopRisks(), loadRuns(), loadAudit(), loadIndicators()]);
  await loadMorning();
}

async function loadAudit() {
  const body = byId("audit-body"), empty = byId("audit-empty"); if (!body) return;
  try {
    const data = await getJson(`${endpoints.audit}?limit=200`);
    const items = data.items ?? [];
    text("audit-tag", `${items.length}`);
    if (items.length === 0) { body.innerHTML = ""; if (empty) empty.hidden = false; return; }
    if (empty) empty.hidden = true;
    body.innerHTML = items.map((e) => {
      const act = (AUDIT_ACTIONS[lang] && AUDIT_ACTIONS[lang][e.action]) || e.action;
      return `<tr><td>${fmtDate(e.timestamp)}</td><td>${escapeHtml(e.actorUserId || "—")}</td>
        <td>${escapeHtml(act)}</td><td>${escapeHtml(e.targetId || e.targetType || "—")}</td>
        <td>${escapeHtml(e.result || "—")}</td></tr>`;
    }).join("");
  } catch { body.innerHTML = ""; if (empty) empty.hidden = false; }
}

function fmtDate(value) {
  if (!value) return "—";
  const d = new Date(value);
  return isNaN(d) ? "—" : d.toLocaleString();
}

function latestRunId() { return state.shell?.dataContext?.latestRunId ?? null; }

async function loadRuns() {
  const body = byId("runs-body"), empty = byId("runs-empty"); if (!body) return;
  try {
    const data = await getJson(`${endpoints.runsHistory}?limit=50`);
    const items = data.items ?? [];
    runsCount = items.length;
    text("runs-tag", `${items.length}`);
    if (items.length === 0) { body.innerHTML = ""; if (empty) empty.hidden = false; return; }
    if (empty) empty.hidden = true;
    body.innerHTML = items.map((r) => `<tr>
      <td>${fmtDate(r.startedAt)}</td><td>${escapeHtml(r.operator || "—")}</td>
      <td>${escapeHtml(r.executedAs || "—")}</td><td>${t("coverage." + r.coverageMode)}</td>
      <td>${r.objectCount}</td><td>${r.findingCount}</td></tr>`).join("");
  } catch { body.innerHTML = ""; if (empty) empty.hidden = false; }
}

// ---------------- findings ----------------
async function loadTopRisks() {
  const body = byId("top-risks-body"), empty = byId("top-risks-empty"); if (!body) return;
  try {
    const data = await getJson(`${endpoints.findings}?view=active&limit=6`);
    const items = data.items ?? [];
    text("top-risks-tag", `${data.count ?? items.length}`);
    if (items.length === 0) { body.innerHTML = ""; if (empty) empty.hidden = false; return; }
    if (empty) empty.hidden = true;
    body.innerHTML = items.map((i) => topRow(i)).join("");
    body.querySelectorAll("tr.clickable").forEach((tr) => tr.addEventListener("click", () => openFindingDetail(decodeURIComponent(tr.dataset.key))));
  } catch { body.innerHTML = ""; if (empty) empty.hidden = false; }
}
// Celula "Avaliado em": data da ultima avaliacao que observou o dado deste risco.
function collectedCell(i) { return `<td>${fmtDate(i.lastSeen)}</td>`; }

// Motivo da resolucao (codigo estavel do backend -> rotulo localizado).
function resolutionLabel(code) {
  if (!code) return "";
  const key = "res." + code;
  const label = t(key);
  return label === key ? code : label;
}

function topRow(i) {
  return `<tr class="clickable" data-key="${encodeURIComponent(i.stableFindingKey)}"><td>${sevBadge(i.severity)}</td><td>${t("cat." + i.category)}</td><td>${escapeHtml(ruleTitle(i.ruleId, i.title))}</td><td>${escapeHtml(i.objectDisplay)}</td></tr>`;
}

function toggleExceptionView(isExc) {
  const findingsTable = byId("findings-body")?.closest("table");
  if (findingsTable) findingsTable.hidden = isExc;
  const actions = byId("findings-actions"); if (actions) actions.hidden = isExc;
  const pager = byId("findings-pager"); if (pager) pager.hidden = isExc;
  const excTable = byId("exceptions-table"); if (excTable) excTable.hidden = !isExc;
  if (isExc) { const fe = byId("findings-empty"); if (fe) fe.hidden = true; }
  else { const ee = byId("exceptions-empty"); if (ee) ee.hidden = true; const eb = byId("exceptions-body"); if (eb) eb.innerHTML = ""; }
}

async function loadFindings() {
  if (findingsState.view === "exception") { await loadExceptions(); return; }
  toggleExceptionView(false);
  try {
    const q = `${endpoints.findings}?view=${findingsState.view}${findingsState.category ? "&category=" + encodeURIComponent(findingsState.category) : ""}`;
    const data = await getJson(q);
    findingsState.items = data.items ?? [];
    findingsState.page = 0;
  } catch { findingsState.items = []; }
  renderFindings();
}

async function loadExceptions() {
  toggleExceptionView(true);
  const body = byId("exceptions-body"), empty = byId("exceptions-empty"); if (!body) return;
  try {
    const data = await getJson(`${endpoints.exceptions}?includeExpired=false`);
    const items = data.items ?? [];
    text("findings-tag", `${items.length}`);
    if (items.length === 0) { body.innerHTML = ""; if (empty) empty.hidden = false; return; }
    if (empty) empty.hidden = true;
    body.innerHTML = items.map((e) => `<tr>
      <td>${sevBadge(e.severity)}</td><td>${escapeHtml(ruleTitle(e.ruleId, e.title))}</td>
      <td>${escapeHtml(e.objectDisplay)}</td><td>${escapeHtml(e.owner)}</td>
      <td>${escapeHtml(e.justification)}</td><td>${fmtDate(e.expiresAt)}</td>
      <td><button class="secondary-button danger" type="button" data-exc="${escapeHtml(e.exceptionId)}">${t("exc.remove")}</button></td></tr>`).join("");
    body.querySelectorAll("button[data-exc]").forEach((b) => b.addEventListener("click", async () => {
      await fetch(`${endpoints.exceptions}/${b.dataset.exc}`, { method: "DELETE" });
      await refresh(); loadExceptions();
    }));
  } catch { body.innerHTML = ""; if (empty) empty.hidden = false; }
}

function renderFindings() {
  if (findingsState.view === "exception") return;
  const body = byId("findings-body"), empty = byId("findings-empty"); if (!body) return;
  let items = findingsState.items.slice();
  const s = findingsState.search.toLowerCase();
  if (s) items = items.filter((i) => (ruleTitle(i.ruleId, i.title) + " " + i.objectDisplay).toLowerCase().includes(s));

  const dir = findingsState.sortDir === "asc" ? 1 : -1;
  const key = findingsState.sortKey;
  items.sort((a, b) => {
    let va, vb;
    if (key === "risk") { va = a.risk; vb = b.risk; }
    else if (key === "collected") { va = a.lastSeen || ""; vb = b.lastSeen || ""; }
    else if (key === "object") { va = a.objectDisplay.toLowerCase(); vb = b.objectDisplay.toLowerCase(); }
    else { va = String(a[key] ?? "").toLowerCase(); vb = String(b[key] ?? "").toLowerCase(); }
    return va < vb ? -dir : va > vb ? dir : 0;
  });

  text("findings-tag", `${items.length}`);
  const totalPages = Math.max(1, Math.ceil(items.length / PAGE_SIZE));
  if (findingsState.page >= totalPages) findingsState.page = totalPages - 1;
  const pageItems = items.slice(findingsState.page * PAGE_SIZE, (findingsState.page + 1) * PAGE_SIZE);

  if (items.length === 0) { body.innerHTML = ""; if (empty) empty.hidden = false; }
  else {
    if (empty) empty.hidden = true;
    body.innerHTML = pageItems.map((i) => {
      const selected = findingsState.selected.has(i.objectKey);
      const statusCell = i.resolutionReason
        ? `${t("status." + i.status)} · <span class="res-reason">${escapeHtml(resolutionLabel(i.resolutionReason))}</span>`
        : t("status." + i.status);
      return `<tr class="clickable" data-key="${encodeURIComponent(i.stableFindingKey)}">
      <td class="col-check"><input type="checkbox" class="row-check" data-objkey="${escapeHtml(i.objectKey)}" ${selected ? "checked" : ""}></td>
      <td>${sevBadge(i.severity)}</td><td>${t("cat." + i.category)}</td><td>${escapeHtml(ruleTitle(i.ruleId, i.title))}</td>
      <td>${escapeHtml(i.objectDisplay)}</td>${collectedCell(i)}<td>${t("action." + i.action)}</td><td>${statusCell}</td></tr>`;
    }).join("");
    body.querySelectorAll("tr.clickable").forEach((tr) => tr.addEventListener("click", (e) => {
      if (e.target.classList.contains("row-check")) return;
      openFindingDetail(decodeURIComponent(tr.dataset.key));
    }));
    body.querySelectorAll(".row-check").forEach((c) => c.addEventListener("change", () => {
      if (c.checked) findingsState.selected.add(c.dataset.objkey);
      else findingsState.selected.delete(c.dataset.objkey);
      updateReassessUi();
    }));
  }
  updateReassessUi();

  const pager = byId("findings-pager");
  if (pager) pager.innerHTML = totalPages <= 1 ? "" :
    `<button class="secondary-button" type="button" id="pg-prev" ${findingsState.page === 0 ? "disabled" : ""}>${t("pager.prev")}</button>
     <span>${findingsState.page + 1} ${t("pager.of")} ${totalPages}</span>
     <button class="secondary-button" type="button" id="pg-next" ${findingsState.page >= totalPages - 1 ? "disabled" : ""}>${t("pager.next")}</button>`;
  byId("pg-prev")?.addEventListener("click", () => { findingsState.page--; renderFindings(); });
  byId("pg-next")?.addEventListener("click", () => { findingsState.page++; renderFindings(); });
}

// Atualiza estado dos controles de reavaliacao (botao habilitado, "selecionar todos").
function updateReassessUi() {
  const actions = byId("findings-actions");
  const isException = findingsState.view === "exception";
  if (actions) actions.hidden = isException;
  const btn = byId("reassess-selected"); if (btn) btn.disabled = findingsState.selected.size === 0;
  const all = byId("findings-check-all");
  if (all) {
    const checks = Array.from(document.querySelectorAll("#findings-body .row-check"));
    all.checked = checks.length > 0 && checks.every((c) => c.checked);
  }
}

// Envia a selecao (ou todos os itens da lista) para uma reavaliacao dirigida:
// pre-preenche o painel de Avaliacao com o ultimo alvo e o escopo desses objetos.
function startReassess(objectKeys) {
  const keys = Array.from(new Set(objectKeys.filter(Boolean)));
  if (keys.length === 0) { alert(t("reassess.none")); return; }
  pendingFocus = keys;
  const last = state.lastAssess || {};
  const setVal = (id, v) => { const el = byId(id); if (el && v != null) el.value = v; };
  setVal("collect-host", last.host); setVal("collect-domain", last.domain);
  setVal("collect-protocol", last.protocol);
  const banner = byId("reassess-banner");
  if (banner) { banner.hidden = false; text("reassess-banner-text", t("reassess.banner").replace("{n}", keys.length)); }
  activateView("collect");
}

function clearReassess() {
  pendingFocus = [];
  const banner = byId("reassess-banner"); if (banner) banner.hidden = true;
}

async function loadInventory() {
  try { renderInventory((await getJson(endpoints.inventory)).items ?? []); } catch { renderInventory([]); }
}

// ---------------- drawer ----------------
// Remediacao em dois passos: pre-visualizar (WhatIf, nao altera) e aplicar (executa).
function psCommandBlock(cmd) {
  return `<div class="ps-block"><pre>${escapeHtml(cmd)}</pre><button class="secondary-button" type="button" data-copy="${escapeHtml(cmd)}">${t("drawer.copy")}</button></div>`;
}
function remediationBlocks(preview, apply) {
  const previewHtml = preview
    ? `<div class="ps-group ps-preview"><div class="ps-label">${t("drawer.psPreview")}</div><p class="ps-hint">${t("drawer.psPreviewHint")}</p>${psCommandBlock(preview)}</div>`
    : "";
  const applyHtml = apply
    ? `<div class="ps-group ps-apply"><div class="ps-label">${t("drawer.psApply")}</div><p class="ps-hint">${t("drawer.psApplyHint")}</p>${psCommandBlock(apply)}</div>`
    : `<div class="ps-group ps-apply"><div class="ps-label">${t("drawer.psApply")}</div><p class="ps-hint">${t("drawer.psApplyManual")}</p></div>`;
  if (!preview && !apply) return "";
  return `<h4>${t("drawer.ps")}</h4><p class="ps-note">${t("drawer.psNote")}</p>${previewHtml}${applyHtml}`;
}
function badges(label, items) {
  if (!items || items.length === 0) return "";
  return `<div class="badge-row"><span class="badge-label">${label}</span>${items.map((i) => `<span class="badge">${escapeHtml(i)}</span>`).join("")}</div>`;
}
async function openFindingDetail(key) {
  const drawer = byId("finding-drawer"), content = byId("drawer-content"); if (!drawer || !content) return;
  content.innerHTML = `<p>${t("drawer.loading")}</p>`; drawer.hidden = false;
  try {
    const d = await getJson(`${endpoints.findingDetail}?key=${encodeURIComponent(key)}`);
    const ev = d.evidence ?? {};
    // Mostra primeiro os identificadores que o administrador reconhece.
    const evPriority = ["displayName", "sam", "objectSid", "distinguishedName"];
    const evKeys = [...evPriority.filter((k) => k in ev), ...Object.keys(ev).filter((k) => !evPriority.includes(k))];
    const evRows = evKeys.map((k) => `<li><span>${escapeHtml(t("ev." + k) === "ev." + k ? k : t("ev." + k))}</span><code>${escapeHtml(ev[k])}</code></li>`).join("");
    const manual = ruleText(d.ruleId, "manual", d.remediation?.manual ?? []).map((x) => `<li>${escapeHtml(x)}</li>`).join("");
    const psHtml = remediationBlocks(d.remediation?.preview ?? "", d.remediation?.apply ?? "");
    const ms = d.frameworks?.microsoft ? `<div class="badge-row"><span class="badge-label">Ref</span><a class="badge" href="${escapeHtml(d.frameworks.microsoft)}" target="_blank" rel="noreferrer">Microsoft</a></div>` : "";
    const exc = await getJson(`${endpoints.exceptions}?includeExpired=false`).catch(() => ({ items: [] }));
    const mine = (exc.items || []).find((e) => e.stableFindingKey === key);

    const hist = await getJson(`${endpoints.objectHistory}?key=${encodeURIComponent(d.objectKey || "")}`).catch(() => ({ items: [] }));
    const histItems = hist.items || [];
    let histHtml = "";
    if (histItems.length > 0) {
      const last = histItems[0], first = histItems[histItems.length - 1];
      histHtml = `<h4>${t("drawer.history")}</h4><ul class="evidence-list">
        <li><span>${t("drawer.firstSeen")}</span><code>${fmtDate(first.observedAt)}</code></li>
        <li><span>${t("drawer.lastSeen")}</span><code>${fmtDate(last.observedAt)}</code></li>
        <li><span>${t("drawer.runsSeen")}</span><code>${histItems.length}</code></li></ul>`;
    }

    const resolutionHtml = d.resolutionReason
      ? `<ul class="evidence-list"><li><span>${t("drawer.resolution")}</span><code>${escapeHtml(resolutionLabel(d.resolutionReason))}</code></li></ul>`
      : "";

    content.innerHTML = `
      <div class="drawer-head">${sevBadge(d.severity)}
        <h2>${escapeHtml(ruleTitle(d.ruleId, d.title))}</h2>
        <p class="drawer-sub">${t("cat." + d.category)} · ${t("sev." + d.severity)} · ${escapeHtml(d.objectDisplay)}</p>
      </div>
      ${resolutionHtml}
      <p class="drawer-impact">${escapeHtml(ruleText(d.ruleId, "impact", d.businessImpact ?? ""))}</p>
      ${badges("MITRE", d.frameworks?.mitre)} ${badges("CIS", d.frameworks?.cis)} ${badges("NIST", d.frameworks?.nist)} ${ms}
      <h3>${t("drawer.action")}: ${t("action." + d.action)}</h3>
      <h4>${t("drawer.manual")}</h4><ol class="manual-steps">${manual || "<li>—</li>"}</ol>
      ${psHtml}
      <h4>${t("drawer.evidence")}</h4><ul class="evidence-list">${evRows || "<li>—</li>"}</ul>
      ${histHtml}
      <h4>${t("drawer.accept")}</h4>
      ${mine ? `<div class="exception-row"><div><strong>${escapeHtml(mine.owner)}</strong><small>${escapeHtml(mine.justification)} · ${new Date(mine.expiresAt).toLocaleDateString()}</small></div><button class="secondary-button danger" type="button" id="exc-del" data-id="${escapeHtml(mine.exceptionId)}">${t("drawer.remove")}</button></div>`
      : `<div class="exception-form">
          <label>${t("drawer.owner")}<input id="exc-owner" type="text"></label>
          <label>${t("drawer.justification")}<textarea id="exc-just" rows="2"></textarea></label>
          <label>${t("drawer.validity")}<select id="exc-days"><option value="90">${t("days.90")}</option><option value="180">${t("days.180")}</option><option value="365" selected>${t("days.365")}</option></select></label>
          <button class="primary-button" type="button" id="exc-save">${t("drawer.acceptBtn")}</button>
          <span id="exc-status"></span>
        </div>`}`;

    content.querySelectorAll("button[data-copy]").forEach((b) => b.addEventListener("click", async () => {
      try {
        await navigator.clipboard.writeText(b.dataset.copy || "");
        const original = b.textContent; b.textContent = t("drawer.copied");
        setTimeout(() => { b.textContent = original; }, 1500);
      } catch {}
    }));
    byId("exc-save")?.addEventListener("click", () => submitException(key));
    byId("exc-del")?.addEventListener("click", async (e) => { await fetch(`${endpoints.exceptions}/${e.target.dataset.id}`, { method: "DELETE" }); closeDrawer(); await refresh(); });
  } catch (e) { content.innerHTML = `<p>Erro: ${escapeHtml(e.message)}</p>`; }
}
function closeDrawer() { const d = byId("finding-drawer"); if (d) d.hidden = true; }
async function submitException(key) {
  const owner = byId("exc-owner")?.value.trim(), justification = byId("exc-just")?.value.trim();
  const validForDays = Number(byId("exc-days")?.value ?? 365);
  if (!owner || !justification) { text("exc-status", t("exc.need")); return; }
  try { await postJson(endpoints.exception, { key, owner, justification, validForDays }); closeDrawer(); await refresh(); }
  catch (e) { text("exc-status", `Erro: ${e.message}`); }
}

// ---------------- profiles ----------------
async function loadProfiles() {
  try {
    profilesState = await getJson(endpoints.profiles);
    ruleCatalog = (await getJson(endpoints.catalog)).items ?? [];
    indicatorCatalog = (await getJson(endpoints.indicatorsCatalog)).items ?? [];
  } catch { profilesState = { activeProfile: "MicrosoftDefault", profiles: [] }; ruleCatalog = []; indicatorCatalog = []; }
  populateProfileSelects();
  loadProfileIntoForm(profilesState.activeProfile);
}
function populateProfileSelects() {
  const opts = profilesState.profiles.map((p) => `<option value="${escapeHtml(p.name)}">${escapeHtml(p.name)}</option>`).join("");
  const ps = byId("profile-select"); if (ps) { ps.innerHTML = opts; ps.value = profilesState.activeProfile; }
  const cp = byId("collect-profile"); if (cp) { cp.innerHTML = opts; cp.value = profilesState.activeProfile; }
  text("profiles-status", `${t("settings.activeProfile")}: ${profilesState.activeProfile}`);
}
function findProfile(name) { return profilesState.profiles.find((p) => p.name === name); }
function loadProfileIntoForm(name) {
  const p = findProfile(name) || profilesState.profiles[0]; if (!p) return;
  const th = p.thresholds || {};
  const set = (id, v) => { const el = byId(id); if (el) el.value = v; };
  set("th-staleUser", th.staleUserDays ?? 90); set("th-staleComputer", th.staleComputerDays ?? 90);
  set("th-disabled", th.disabledObjectRetentionDays ?? 180); set("th-krbtgt", th.krbtgtRotationDays ?? 180);
  set("th-maq", th.machineAccountQuotaExpected ?? 0);
  set("th-recycleBin", th.recycleBinMinRetentionDays ?? 180);
  set("th-emptyDays", th.emptyObjectMinDays ?? 30);
  const disabled = new Set((p.disabledRules || []).map((x) => x.toLowerCase()));
  const cat = byId("rules-catalog");
  if (cat) {
    // Agrupa as regras por categoria para facilitar a leitura.
    const order = ["Hygiene", "PrivilegedAccess", "Hardening", "Infrastructure", "Governance"];
    const groups = {};
    ruleCatalog.forEach((r) => { (groups[r.category] = groups[r.category] || []).push(r); });
    const cats = Object.keys(groups).sort((a, b) => order.indexOf(a) - order.indexOf(b));
    cat.innerHTML = cats.map((c) => `<div class="rule-group-title">${t("cat." + c)}</div>` +
      groups[c].map((r) => `<div class="rule-item"><label><input type="checkbox" data-rule="${escapeHtml(r.ruleId)}" ${disabled.has(r.ruleId.toLowerCase()) ? "" : "checked"}> ${escapeHtml(ruleTitle(r.ruleId, r.title))}</label><span class="rule-cat">${escapeHtml(r.ruleId)}</span></div>`).join("")
    ).join("");
  }

  // Indicadores operacionais: horizonte + toggles built-in + customizados.
  set("ind-horizon", p.indicatorHorizonDays ?? 7);
  const disabledInd = new Set((p.disabledIndicators || []).map((x) => x.toLowerCase()));
  const indBox = byId("indicators-toggles");
  if (indBox) {
    indBox.innerHTML = indicatorCatalog.map((d) =>
      `<div class="rule-item"><label><input type="checkbox" data-ind="${escapeHtml(d.id)}" ${disabledInd.has(d.id.toLowerCase()) ? "" : "checked"}> ${escapeHtml(d.title)}</label><span class="rule-cat">${escapeHtml(d.category)}</span></div>`).join("");
  }
  renderCustomIndicators(p);

  const ro = p.builtIn;
  ["th-staleUser","th-staleComputer","th-disabled","th-krbtgt","th-maq","th-recycleBin","th-emptyDays","ind-horizon"].forEach((id) => { const el = byId(id); if (el) el.disabled = ro; });
  document.querySelectorAll("#rules-catalog input, #indicators-toggles input").forEach((i) => { i.disabled = ro; });
  ["profile-save","profile-delete","ci-add","ci-name","ci-query","ci-kind","ci-objtype"].forEach((id) => { const el = byId(id); if (el) el.disabled = ro; });
  disarmDelete(); // troca de perfil cancela um "excluir" pendente
  text("profile-hint", ro ? t("settings.builtinHint") : t("settings.customHint"));
}

function renderCustomIndicators(p) {
  const list = byId("custom-indicators-list"); if (!list) return;
  const items = p.customIndicators || [];
  const ro = p.builtIn;
  list.innerHTML = items.length === 0 ? `<p class="muted-row">—</p>` : items.map((c) =>
    `<div class="custom-ind-item"><label><input type="checkbox" data-cienable="${escapeHtml(c.id)}" ${c.enabled ? "checked" : ""} ${ro ? "disabled" : ""}> ${escapeHtml(c.name)}</label>
      <code>${escapeHtml(c.kind)} · ${escapeHtml(c.objectType)}: ${escapeHtml(c.query)}</code>
      <button class="secondary-button danger" type="button" data-cidel="${escapeHtml(c.id)}" ${ro ? "disabled" : ""}>${t("settings.delete")}</button></div>`).join("");
  list.querySelectorAll("[data-cidel]").forEach((b) => b.addEventListener("click", async () => {
    p.customIndicators = (p.customIndicators || []).filter((x) => x.id !== b.dataset.cidel);
    await saveProfiles(); loadProfileIntoForm(p.name);
  }));
  list.querySelectorAll("[data-cienable]").forEach((cb) => cb.addEventListener("change", async () => {
    const item = (p.customIndicators || []).find((x) => x.id === cb.dataset.cienable);
    if (item) { item.enabled = cb.checked; await saveProfiles(); }
  }));
}
function readFormProfile(name) {
  return {
    name, builtIn: false,
    thresholds: {
      staleUserDays: Number(byId("th-staleUser").value), staleComputerDays: Number(byId("th-staleComputer").value),
      disabledObjectRetentionDays: Number(byId("th-disabled").value), dormantSensitiveEntityDays: 180,
      krbtgtRotationDays: Number(byId("th-krbtgt").value), machineAccountQuotaExpected: Number(byId("th-maq").value),
      recycleBinMinRetentionDays: Number(byId("th-recycleBin").value), emptyObjectMinDays: Number(byId("th-emptyDays").value)
    },
    disabledRules: Array.from(document.querySelectorAll("#rules-catalog input:not(:checked)")).map((i) => i.dataset.rule),
    disabledIndicators: Array.from(document.querySelectorAll("#indicators-toggles input:not(:checked)")).map((i) => i.dataset.ind),
    indicatorHorizonDays: Number(byId("ind-horizon")?.value) || 7,
    // Indicadores customizados sao editados pela lista (add/remove); preserva os existentes.
    customIndicators: (findProfile(name)?.customIndicators) || []
  };
}
async function saveProfiles() { profilesState = await sendJson(endpoints.profiles, "PUT", profilesState); populateProfileSelects(); }

function genIndicatorId() { return "ci-" + Math.random().toString(36).slice(2, 10); }
async function validateCustomIndicator() {
  const kind = byId("ci-kind")?.value, query = byId("ci-query")?.value.trim();
  if (!query) { text("ci-status", t("ind.cNeed")); return null; }
  try {
    const v = await postJson(endpoints.indicatorValidate, { kind, query });
    text("ci-status", v.ok ? `${t("ind.cValid")} ${v.ldapFilter}` : `${t("ind.cInvalid")} ${v.error || ""}`);
    return v;
  } catch (e) { text("ci-status", e.message); return null; }
}
async function addCustomIndicator() {
  const name = byId("ci-name")?.value.trim(), kind = byId("ci-kind")?.value,
        query = byId("ci-query")?.value.trim(), objectType = byId("ci-objtype")?.value;
  if (!name || !query) { text("ci-status", t("ind.cNeed")); return; }
  const pname = byId("profile-select")?.value, p = findProfile(pname);
  if (!p || p.builtIn) { text("ci-status", t("ind.cReadonly")); return; }
  const v = await validateCustomIndicator();
  if (!v || !v.ok) return;
  p.customIndicators = p.customIndicators || [];
  p.customIndicators.push({ id: genIndicatorId(), name, kind, query, objectType, enabled: true });
  await saveProfiles(); loadProfileIntoForm(pname);
  byId("ci-name").value = ""; byId("ci-query").value = "";
  text("ci-status", t("ind.cAdded"));
}

// ---------------- assessment ----------------
function splitPrincipal(v) {
  if (!v || !v.includes("\\")) return { domain: byId("collect-domain")?.value.trim() || null, userName: v || null };
  const [d, ...r] = v.split("\\"); return { domain: d, userName: r.join("\\") };
}
function setProbe(kind, title, msg, details) {
  const r = byId("probe-result"); if (!r) return;
  r.className = `probe-result ${kind || ""}`;
  const dl = details ? `<ul class="probe-detail-list">${details.map((i) => `<li><span>${i.label}</span><code>${i.value ?? "-"}</code></li>`).join("")}</ul>` : "";
  r.innerHTML = `<strong>${title}</strong><span>${msg}</span>${dl}`;
}
async function probeDirectory() {
  const host = byId("collect-host")?.value.trim(); if (!host) { setProbe("blocked", "—", t("assess.dc")); return; }
  const protocol = byId("collect-protocol")?.value ?? "Ldaps";
  const principal = splitPrincipal(byId("collect-user")?.value.trim());
  const secret = byId("collect-secret")?.value ?? "";
  byId("probe-button").disabled = true; setProbe("", t("assess.validate"), host);
  try {
    const res = await postJson(endpoints.probe, { host, protocol, port: protocol === "Ldap" ? 389 : 636, domain: principal.domain || byId("collect-domain")?.value.trim() || null, userName: principal.userName || null, secret: principal.userName ? secret : null, timeoutSeconds: 15 });
    const ok = res.state === "Ready" || res.state === "ReadyWithWarnings";
    setProbe(ok ? "ready" : "blocked", ok ? "OK" : "—", ok ? "RootDSE OK" : (res.errors?.join(" ") || "—"),
      [{ label: "Host", value: res.host }, { label: "NC", value: res.namingContexts?.defaultNamingContext }]);
  } catch (e) { setProbe("blocked", "—", e.message); } finally { byId("probe-button").disabled = false; }
}
async function startCollection() {
  const host = byId("collect-host")?.value.trim(); if (!host) { setProbe("blocked", "—", t("assess.dc")); return; }
  const protocol = byId("collect-protocol")?.value ?? "Ldaps";
  const principal = splitPrincipal(byId("collect-user")?.value.trim());
  const secret = byId("collect-secret")?.value ?? "";
  const profile = byId("collect-profile")?.value;
  if (profile && profilesState && profile !== profilesState.activeProfile) { profilesState = { ...profilesState, activeProfile: profile }; await saveProfiles(); }
  const btn = byId("start-collection-button"), prog = byId("run-progress");
  if (btn) btn.disabled = true; if (prog) prog.hidden = false;
  text("run-progress-text", "…");
  try {
    const operator = sessionUser;
    const domain = principal.domain || byId("collect-domain")?.value.trim() || null;
    // Guarda o ultimo alvo para pre-preencher reavaliacoes dirigidas.
    state.lastAssess = { host, protocol, domain, operator };
    const focusObjectKeys = pendingFocus.length > 0 ? pendingFocus : null;
    const started = await postJson(endpoints.runs, { host, protocol, port: protocol === "Ldap" ? 389 : 636, domain, userName: principal.userName || null, secret: principal.userName ? secret : null, operator, timeoutSeconds: 30, focusObjectKeys });
    await pollRun(started.jobId);
    clearReassess();
    findingsState.selected.clear();
  } catch (e) { text("run-progress-text", e.message); } finally { if (btn) btn.disabled = false; }
}
async function pollRun(jobId) {
  const wasFirstRun = runsCount === 0;
  for (let i = 0; i < 900; i++) {
    const s = await getJson(`${endpoints.runs}/${jobId}`);
    text("run-progress-text", `${s.stage}: ${s.message} (${s.collectedSoFar})`);
    if (s.status === "Completed") {
      await refresh();
      if (wasFirstRun) { renderFirstRunResult(s); return; }
      text("run-progress-text", `OK: ${s.findingCount} (${t("coverage." + s.coverageMode)})`);
      activateView("findings");
      return;
    }
    if (s.status === "Failed") { text("run-progress-text", s.error ?? "—"); return; }
    await new Promise((r) => setTimeout(r, 1000));
  }
}

// Resultado da 1a avaliacao: mostra o que foi encontrado e conecta os proximos
// passos (relatorio + coleta diaria) em vez de so pular para a lista de riscos.
function renderFirstRunResult(s) {
  const prog = byId("run-progress"); if (!prog) return;
  const score = state.shell?.identityScore;
  prog.hidden = false;
  prog.innerHTML = `<div class="firstrun-result">
    <strong>${t("firstrun.done")}</strong>
    <p>${s.findingCount} ${t("firstrun.risks")}${score != null ? ` · Identity Score <strong>${score}%</strong>` : ""}</p>
    <div class="profile-actions">
      <button class="primary-button" type="button" data-fr="findings">${t("firstrun.view")}</button>
      <button class="secondary-button" type="button" data-fr="report">${t("topbar.report")}</button>
      <button class="secondary-button" type="button" data-fr="operations">${t("morning.goSchedule")}</button>
    </div></div>`;
  prog.querySelectorAll("[data-fr]").forEach((b) => b.addEventListener("click", () => {
    const target = b.dataset.fr;
    if (target === "report") { window.location = `/api/v1/reports/summary?lang=${lang}`; return; }
    activateView(target);
  }));
}

// ---------------- wiring ----------------
document.querySelectorAll(".nav-tab").forEach((b) => b.addEventListener("click", () => activateView(b.dataset.view)));
byId("refresh-button")?.addEventListener("click", refresh);
byId("demo-button")?.addEventListener("click", () => { sessionStorage.setItem("dx-demo", "1"); location.reload(); });
byId("demo-exit")?.addEventListener("click", () => { sessionStorage.removeItem("dx-demo"); location.reload(); });
byId("report-button")?.addEventListener("click", () => { window.location = `/api/v1/reports/summary?lang=${lang}`; });
byId("findings-csv")?.addEventListener("click", () => {
  const params = new URLSearchParams({ view: findingsState.view, lang });
  if (findingsState.category) params.set("category", findingsState.category);
  window.location = `/api/v1/reports/findings.csv?${params}`;
});
byId("inventory-csv")?.addEventListener("click", () => { window.location = `/api/v1/reports/inventory.csv?lang=${lang}`; });
byId("top-new-collection-button")?.addEventListener("click", () => activateView("collect"));
byId("probe-button")?.addEventListener("click", probeDirectory);
byId("start-collection-button")?.addEventListener("click", startCollection);
byId("drawer-close")?.addEventListener("click", closeDrawer);
byId("finding-drawer")?.addEventListener("click", (e) => { if (e.target.id === "finding-drawer") closeDrawer(); });
byId("object-drawer-close")?.addEventListener("click", () => { const d = byId("object-drawer"); if (d) d.hidden = true; });
byId("ci-validate")?.addEventListener("click", validateCustomIndicator);
byId("ci-add")?.addEventListener("click", addCustomIndicator);
byId("object-drawer")?.addEventListener("click", (e) => { if (e.target.id === "object-drawer") byId("object-drawer").hidden = true; });
byId("timeline-window")?.addEventListener("change", (e) => { timelineState.window = Number(e.target.value); timelineState.type = null; loadTimeline(); });
byId("theme-toggle")?.addEventListener("click", () => { theme = theme === "dark" ? "light" : "dark"; localStorage.setItem("adc-theme", theme); applyTheme(); });
byId("lang-toggle")?.addEventListener("click", () => {
  lang = lang === "pt" ? "en" : "pt"; localStorage.setItem("adc-lang", lang);
  applyI18n(); render(); renderFindings(); loadTopRisks(); loadRuns();
  if (profilesState) { populateProfileSelects(); loadProfileIntoForm(byId("profile-select")?.value || profilesState.activeProfile); }
});

byId("category-filter")?.addEventListener("change", (e) => { findingsState.category = e.target.value; loadFindings(); });
byId("findings-search")?.addEventListener("input", (e) => { findingsState.search = e.target.value; findingsState.page = 0; renderFindings(); });
document.querySelectorAll(".status-tab").forEach((b) => b.addEventListener("click", () => {
  document.querySelectorAll(".status-tab").forEach((x) => x.classList.toggle("is-active", x === b));
  findingsState.view = b.dataset.status; findingsState.selected.clear(); loadFindings();
}));
byId("reassess-selected")?.addEventListener("click", () => startReassess(Array.from(findingsState.selected)));
byId("reassess-all")?.addEventListener("click", () => startReassess(findingsState.items.map((i) => i.objectKey)));
byId("reassess-clear")?.addEventListener("click", clearReassess);
byId("findings-check-all")?.addEventListener("change", (e) => {
  document.querySelectorAll("#findings-body .row-check").forEach((c) => {
    c.checked = e.target.checked;
    if (c.checked) findingsState.selected.add(c.dataset.objkey); else findingsState.selected.delete(c.dataset.objkey);
  });
  updateReassessUi();
});
document.querySelectorAll("[data-sort]").forEach((th) => th.addEventListener("click", () => {
  const k = th.dataset.sort;
  if (findingsState.sortKey === k) findingsState.sortDir = findingsState.sortDir === "asc" ? "desc" : "asc";
  else { findingsState.sortKey = k; findingsState.sortDir = k === "risk" ? "desc" : "asc"; }
  renderFindings();
}));

// Le o nome do novo perfil do campo inline (sem prompt() nativo, que o navegador
// bloqueia apos alguns usos). Retorna null e sinaliza no hint se invalido.
function readNewProfileName() {
  const input = byId("profile-name-input");
  const name = (input?.value || "").trim();
  if (!name) { text("profile-hint", t("settings.needName")); input?.focus(); return null; }
  if (findProfile(name)) { text("profile-hint", t("settings.exists")); input?.focus(); return null; }
  return name;
}

byId("profile-select")?.addEventListener("change", (e) => loadProfileIntoForm(e.target.value));
byId("profile-save")?.addEventListener("click", async () => {
  const name = byId("profile-select").value; const idx = profilesState.profiles.findIndex((p) => p.name === name);
  if (idx < 0 || profilesState.profiles[idx].builtIn) return;
  profilesState.profiles[idx] = readFormProfile(name); await saveProfiles(); loadProfileIntoForm(name);
  text("profile-hint", t("settings.saved"));
});
byId("profile-new")?.addEventListener("click", async () => {
  const name = readNewProfileName(); if (!name) return;
  profilesState.profiles.push({
    name, builtIn: false,
    thresholds: { staleUserDays: 90, staleComputerDays: 90, disabledObjectRetentionDays: 180, dormantSensitiveEntityDays: 180, krbtgtRotationDays: 180, machineAccountQuotaExpected: 0, recycleBinMinRetentionDays: 180, emptyObjectMinDays: 30 },
    disabledRules: [], disabledIndicators: [], indicatorHorizonDays: 7, customIndicators: []
  });
  await saveProfiles();
  byId("profile-name-input").value = ""; byId("profile-select").value = name; loadProfileIntoForm(name);
});
byId("profile-saveas")?.addEventListener("click", async () => {
  const name = readNewProfileName(); if (!name) return;
  profilesState.profiles.push(readFormProfile(name)); await saveProfiles();
  byId("profile-name-input").value = ""; byId("profile-select").value = name; loadProfileIntoForm(name);
});
// Exclusao com confirmacao em DOIS cliques no proprio botao (sem confirm() nativo).
let deleteArmed = false, deleteTimer = null;
function disarmDelete() {
  deleteArmed = false; if (deleteTimer) { clearTimeout(deleteTimer); deleteTimer = null; }
  const b = byId("profile-delete"); if (b) { b.textContent = t("settings.delete"); b.classList.remove("armed"); }
}
byId("profile-delete")?.addEventListener("click", async () => {
  const name = byId("profile-select").value; const p = findProfile(name); if (!p || p.builtIn) return;
  const b = byId("profile-delete");
  if (!deleteArmed) {
    deleteArmed = true;
    if (b) { b.textContent = t("settings.confirmDelete"); b.classList.add("armed"); }
    deleteTimer = setTimeout(disarmDelete, 4000);
    return;
  }
  disarmDelete();
  profilesState.profiles = profilesState.profiles.filter((x) => x.name !== name);
  if (profilesState.activeProfile === name) profilesState.activeProfile = "MicrosoftDefault";
  await saveProfiles(); loadProfileIntoForm(profilesState.activeProfile);
});
byId("profile-activate")?.addEventListener("click", async () => {
  profilesState.activeProfile = byId("profile-select").value; await saveProfiles();
  text("profile-hint", t("settings.activated"));
});

// ---------------- auth ----------------
let authMode = "login"; // "login" | "setup"
let sessionUser = null; // operador = usuario logado (sem campo manual no wizard)
function showAuth(needsSetup) {
  authMode = needsSetup ? "setup" : "login";
  const ov = byId("auth-overlay"); if (ov) ov.hidden = false;
  text("auth-title", needsSetup ? t("auth.create") : t("auth.login"));
  text("auth-sub", needsSetup ? t("auth.subSetup") : t("auth.sub"));
  text("auth-submit", needsSetup ? t("auth.create") : t("auth.login"));
  const err = byId("auth-error"); if (err) err.hidden = true;
  byId("auth-username")?.focus();
}
async function bootAuth() {
  if (demoMode) { sessionUser = "demo"; const b = byId("demo-banner"); if (b) b.hidden = false; startApp(); return; }
  try {
    const me = await getJson(`${endpoints.auth}/me`);
    if (me.portable) {
      // Portátil: sessão única, sem login. Selo na marca e sem botão Sair.
      portableMode = true;
      const sub = byId("brand-mode"); if (sub) sub.textContent = t("portable.badge");
      const lo = byId("logout-button"); if (lo) lo.hidden = true;
    }
    if (me.authenticated) { sessionUser = me.username || null; startApp(); return; }
    showAuth(me.needsSetup === true);
  } catch { showAuth(false); }
}
async function startApp() {
  const ov = byId("auth-overlay"); if (ov) ov.hidden = true;
  await loadProfiles();
  refresh();
  loadSchedule();
  loadServiceStatus();
  loadNotifications();
}
async function submitAuth() {
  const username = byId("auth-username")?.value.trim();
  const password = byId("auth-password")?.value ?? "";
  const err = byId("auth-error");
  try {
    const url = `${endpoints.auth}/${authMode === "setup" ? "setup" : "login"}`;
    const res = await fetch(url, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ username, password }) });
    if (!res.ok) {
      // Mostra a mensagem exata do servidor (ex.: senha curta), nao um generico.
      let msg = t("auth.bad");
      try { const b = await res.json(); if (b && b.error) msg = b.error; } catch {}
      if (err) { err.hidden = false; err.textContent = msg; }
      return;
    }
    if (err) err.hidden = true;
    try { const b = await res.json(); sessionUser = b.username || username; } catch { sessionUser = username; }
    const pwd = byId("auth-password"); if (pwd) pwd.value = "";
    startApp();
  } catch { if (err) { err.hidden = false; err.textContent = t("auth.bad"); } }
}
async function logout() {
  try { await fetch(`${endpoints.auth}/logout`, { method: "POST" }); } catch {}
  showAuth(false);
}

// ---------------- schedule ----------------
function scheduleFreqUi() {
  const f = byId("sched-frequency")?.value;
  const timeRow = byId("sched-time")?.closest("label");
  const intRow = byId("sched-interval")?.closest("label");
  const days = byId("sched-weekdays-box");
  if (timeRow) timeRow.style.display = f === "IntervalHours" ? "none" : "";
  if (intRow) intRow.style.display = f === "IntervalHours" ? "" : "none";
  if (days) days.style.display = f === "Weekly" ? "" : "none";
}
async function loadSchedule() {
  try {
    const c = await getJson(endpoints.schedule);
    const set = (id, v) => { const el = byId(id); if (el) el.value = v; };
    set("sched-enabled", c.enabled ? "true" : "false");
    set("sched-frequency", c.frequency || "Daily");
    set("sched-time", c.timeOfDay || "02:00");
    set("sched-interval", c.intervalHours || 24);
    set("sched-host", c.host || "");
    // Popula o seletor de perfil (1ª opção = perfil ativo no momento).
    const sel = byId("sched-profile");
    if (sel) {
      const opts = [`<option value="">${t("sched.activeProfile")}</option>`]
        .concat((profilesState?.profiles || []).map((p) => `<option value="${escapeHtml(p.name)}">${escapeHtml(p.name)}</option>`));
      sel.innerHTML = opts.join("");
      sel.value = c.profileName || "";
    }
    const days = new Set((c.weekdays || []).map(Number));
    document.querySelectorAll('input[name="schedWeekday"]').forEach((i) => { i.checked = days.has(Number(i.value)); });
    text("sched-last", c.lastRunAt ? fmtDate(c.lastRunAt) : "—");
    text("sched-next", (c.enabled && c.nextRunAt) ? fmtDate(c.nextRunAt) : "—");
    text("sched-tag", c.enabled ? t("sched.on") : t("sched.off"));
    scheduleFreqUi();
  } catch {}
}
async function saveSchedule() {
  const body = {
    enabled: byId("sched-enabled").value === "true",
    frequency: byId("sched-frequency").value,
    timeOfDay: byId("sched-time").value || "02:00",
    weekdays: Array.from(document.querySelectorAll('input[name="schedWeekday"]:checked')).map((i) => Number(i.value)),
    intervalHours: Number(byId("sched-interval").value) || 24,
    host: byId("sched-host").value.trim(),
    protocol: "Ldaps", profileName: byId("sched-profile")?.value || null, lastRunAt: null, nextRunAt: null
  };
  try {
    await sendJson(endpoints.schedule, "PUT", body);
    text("sched-result", t("sched.saved"));
    await loadSchedule();
  } catch (e) { text("sched-result", e.message); }
}
async function testSchedule() {
  const host = byId("sched-host").value.trim(); if (!host) return;
  text("sched-result", "…");
  try {
    const r = await postJson(`${endpoints.schedule}/test`, { host });
    text("sched-result", `${r.ok ? "OK" : "—"} · ${r.identity} · ${r.namingContext || (r.errors || []).join(" ")}`);
  } catch (e) { text("sched-result", e.message); }
}

// ---------------- notifications (digest) ----------------
function notifOutcomeText(o) {
  if (!o) return "—";
  const when = o.at ? fmtDate(o.at) : "";
  if (o.skipped) return `${when} · ${t("notif.skipped")}: ${o.skipReason || ""}`.trim();
  const parts = (o.results || []).map((r) => `${r.channel}: ${r.ok ? "OK" : "—"}${r.ok ? "" : " (" + (r.detail || "") + ")"}`);
  return `${when} · ${parts.join(" · ") || "—"}`.trim();
}
async function loadNotifications() {
  try {
    const c = await getJson(`${endpoints.notifications}/config`);
    const set = (id, v) => { const el = byId(id); if (el) el.value = v; };
    set("notif-policy", c.policy || "OnlyWhenActivity");
    set("notif-lang", c.lang || "pt");
    set("notif-smtp-enabled", c.smtpEnabled ? "true" : "false");
    set("notif-smtp-host", c.smtpHost || "");
    set("notif-smtp-port", c.smtpPort || 587);
    set("notif-smtp-tls", c.smtpUseStartTls ? "true" : "false");
    set("notif-smtp-user", c.smtpUsername || "");
    set("notif-smtp-from", c.smtpFrom || "");
    set("notif-smtp-to", c.smtpTo || "");
    set("notif-webhook-enabled", c.webhookEnabled ? "true" : "false");
    set("notif-webhook-url", c.webhookUrl || "");
    // Senha nunca volta do servidor: placeholder indica se já existe uma salva.
    const pass = byId("notif-smtp-pass");
    if (pass) { pass.value = ""; pass.placeholder = c.smtpHasPassword ? "••••••••" : t("notif.passPlaceholder"); }
    const on = c.smtpEnabled || c.webhookEnabled;
    text("notif-tag", on ? t("notif.on") : t("notif.off"));
    text("notif-last", notifOutcomeText(c.lastOutcome));
  } catch {}
}
function notifBody() {
  const pass = byId("notif-smtp-pass")?.value ?? "";
  return {
    smtpEnabled: byId("notif-smtp-enabled").value === "true",
    smtpHost: byId("notif-smtp-host").value.trim(),
    smtpPort: Number(byId("notif-smtp-port").value) || 587,
    smtpUseStartTls: byId("notif-smtp-tls").value === "true",
    smtpUsername: byId("notif-smtp-user").value.trim(),
    // "" no campo = manter a senha atual (null); qualquer valor = definir nova.
    smtpPassword: pass.length > 0 ? pass : null,
    smtpFrom: byId("notif-smtp-from").value.trim(),
    smtpTo: byId("notif-smtp-to").value.trim(),
    webhookEnabled: byId("notif-webhook-enabled").value === "true",
    webhookUrl: byId("notif-webhook-url").value.trim(),
    policy: byId("notif-policy").value,
    lang: byId("notif-lang").value
  };
}
async function saveNotifications() {
  try {
    await sendJson(`${endpoints.notifications}/config`, "PUT", notifBody());
    text("notif-result", t("notif.saved"));
    await loadNotifications();
  } catch (e) { text("notif-result", e.message); }
}
async function testNotifications() {
  text("notif-result", t("notif.testing"));
  try {
    // Salva antes de testar, para o envio usar a config atual da tela.
    await sendJson(`${endpoints.notifications}/config`, "PUT", notifBody());
    const o = await postJson(`${endpoints.notifications}/test`, {});
    text("notif-result", notifOutcomeText(o));
    await loadNotifications();
  } catch (e) { text("notif-result", e.message); }
}
byId("notif-save")?.addEventListener("click", saveNotifications);
byId("notif-test")?.addEventListener("click", testNotifications);

// ---------------- service config ----------------
function svcIdentityUi() {
  const mode = byId("svc-identity-mode")?.value;
  const row = byId("svc-account-row");
  if (row) row.style.display = mode === "Gmsa" ? "" : "none";
}
async function loadServiceStatus() {
  try {
    const s = await getJson("/api/v1/service/status");
    text("svc-cur-identity", s.installed ? s.identity : "—");
    text("svc-cur-startup", s.installed ? (t("svc.start" + (s.startupType === "Automatic" ? "Auto" : s.startupType === "AutomaticDelayed" ? "Delayed" : s.startupType === "Manual" ? "Manual" : "")) || s.startupType) : "—");
    text("svc-tag", s.installed ? "OK" : "—");
    const sid = byId("sched-identity"); if (sid && s.installed) sid.textContent = s.identity;
    if (s.installed) {
      const mode = (s.identity && s.identity.endsWith("$")) ? "Gmsa" : (s.identity === "LocalSystem" ? "LocalSystem" : "Gmsa");
      const ms = byId("svc-identity-mode"); if (ms) ms.value = mode;
      if (mode === "Gmsa") { const a = byId("svc-account"); if (a && s.identity.endsWith("$")) a.value = s.identity; }
      const st = byId("svc-startup"); if (st && ["Automatic","AutomaticDelayed","Manual"].includes(s.startupType)) st.value = s.startupType;
    }
    svcIdentityUi();
  } catch {}
}
async function applyServiceConfig() {
  const body = {
    startupType: byId("svc-startup").value,
    identityMode: byId("svc-identity-mode").value,
    accountName: byId("svc-account").value.trim()
  };
  text("svc-result", "…");
  try {
    const res = await fetch("/api/v1/service/config", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
    const data = await res.json().catch(() => ({}));
    text("svc-result", res.ok ? (data.message || t("svc.restart")) : (data.error || "—"));
    await loadServiceStatus();
  } catch (e) { text("svc-result", e.message); }
}
byId("svc-identity-mode")?.addEventListener("change", svcIdentityUi);
byId("svc-apply")?.addEventListener("click", applyServiceConfig);

byId("auth-submit")?.addEventListener("click", submitAuth);
byId("auth-password")?.addEventListener("keydown", (e) => { if (e.key === "Enter") submitAuth(); });
byId("logout-button")?.addEventListener("click", logout);
byId("sched-frequency")?.addEventListener("change", scheduleFreqUi);
byId("sched-save")?.addEventListener("click", saveSchedule);
byId("sched-test")?.addEventListener("click", testSchedule);

applyTheme();
applyI18n();
wireSearch();
bootAuth();
