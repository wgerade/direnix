// Direnix — modo demonstração: ambiente fictício "corp.exemplo.local".
// Nada aqui toca o AD nem o banco: o app intercepta os GETs e responde deste fixture.
(function () {
  const now = Date.now();
  const iso = (hoursAgo) => new Date(now - hoursAgo * 3600 * 1000).toISOString();
  const isoIn = (hoursAhead) => new Date(now + hoursAhead * 3600 * 1000).toISOString();
  const RUN2 = "demo-run-0000000000000002";
  const RUN1 = "demo-run-0000000000000001";
  const DOMAIN = "corp.exemplo.local";
  const DN = (cn, ou) => `CN=${cn},OU=${ou || "Usuarios"},DC=corp,DC=exemplo,DC=local`;

  const finding = (key, ruleId, title, category, severity, risk, action, objectDisplay, objectKey, firstHrs, evidence) => ({
    stableFindingKey: key, ruleId, title, category, severity, risk, action, status: "Open",
    objectDisplay, objectKey, firstSeen: iso(firstHrs), lastSeen: iso(6), lastRunId: RUN2, resolutionReason: null,
    evidence: evidence || {},
    remediation: {
      manual: ["Abra o ADUC (dsa.msc) e localize o objeto.", "Revise a configuração apontada pela evidência.", "Registre a mudança no seu processo de change."],
      preview: `Get-ADObject -Identity "${objectKey}" -Properties * | Select-Object Name`,
      apply: "# Ajuste conforme o passo a passo manual — cada regra tem seu comando específico."
    }
  });

  const findings = [
    finding("F-KRB-01", "AD-HRD-010", "Senha do krbtgt sem rotação há 380 dias", "Hardening", "Critical", 95, "Implement", "krbtgt", DN("krbtgt"), 400),
    finding("F-DELEG-01", "AD-PRIV-020", "Delegação irrestrita em servidor de aplicação", "PrivilegedAccess", "Critical", 92, "Adjust", "SRV-APP01$", DN("SRV-APP01", "Servidores"), 300),
    finding("F-SPN-01", "AD-PRIV-011", "Conta privilegiada com SPN (kerberoastable)", "PrivilegedAccess", "High", 85, "Investigate", "svc-sql", DN("svc-sql", "Servicos"), 160),
    finding("F-PWDNEVER-01", "AD-PRIV-005", "Senha nunca expira em conta privilegiada", "PrivilegedAccess", "High", 80, "Adjust", "adm-legado", DN("adm-legado", "Admins"), 700),
    finding("F-MAQ-01", "AD-HRD-002", "MachineAccountQuota acima do esperado (10)", "Hardening", "High", 74, "Adjust", DOMAIN, "DC=corp,DC=exemplo,DC=local", 700),
    finding("F-GUEST-01", "AD-HRD-004", "Conta Guest habilitada", "Hardening", "High", 70, "CleanUp", "Guest", DN("Guest"), 700),
    finding("F-ADMCNT-01", "AD-PRIV-008", "adminCount órfão (conta fora de grupos protegidos)", "PrivilegedAccess", "Medium", 55, "CleanUp", "j.pereira", DN("j.pereira"), 350),
    finding("F-STALE-01", "AD-HYG-001", "Conta sem uso há 120 dias", "Hygiene", "Medium", 50, "Decommission", "m.santos", DN("m.santos"), 120),
    finding("F-STALE-02", "AD-HYG-001", "Conta sem uso há 96 dias", "Hygiene", "Medium", 48, "Decommission", "r.almeida", DN("r.almeida"), 96),
    finding("F-CMPOLD-01", "AD-HYG-002", "Computador obsoleto (sem logon há 200 dias)", "Hygiene", "Medium", 45, "Decommission", "WS-0142$", DN("WS-0142", "Workstations"), 200),
    finding("F-DISAB-01", "AD-HYG-003", "Conta desabilitada retida há 210 dias", "Hygiene", "Low", 30, "CleanUp", "ex.func01", DN("ex.func01", "Desligados"), 210),
    finding("F-GRPEMPTY-01", "AD-HYG-010", "Grupo vazio há 90 dias", "Hygiene", "Low", 25, "CleanUp", "GRP-Projeto-Antigo", DN("GRP-Projeto-Antigo", "Grupos"), 90),
    finding("F-OUEMPTY-01", "AD-HYG-011", "OU vazia", "Hygiene", "Low", 20, "CleanUp", "OU=Temp", "OU=Temp,DC=corp,DC=exemplo,DC=local", 60),
    finding("F-RECYC-01", "AD-GOV-001", "Lixeira do AD desativada", "Governance", "Medium", 60, "Implement", DOMAIN, "DC=corp,DC=exemplo,DC=local", 700),
    finding("F-PWDNOTREQ-01", "AD-HRD-006", "PasswordNotRequired em conta ativa", "Hardening", "High", 78, "Adjust", "svc-print", DN("svc-print", "Servicos"), 500)
  ];

  const sev = { Critical: 2, High: 5, Medium: 5, Low: 3, Info: 0 };
  const indItem = (display, detail, cn) => ({ display, detail, distinguishedName: DN(cn || display), objectSid: null });

  const shell = {
    generatedAt: iso(0),
    product: { name: "Direnix", mode: "Demo", portalUrl: "http://127.0.0.1:8787/" },
    dataContext: { source: "demo", hasCompletedCollection: true, latestRunId: RUN2, latestRunStartedAt: iso(7), latestRunCompletedAt: iso(6.8), domainName: DOMAIN, coverageMode: "Full" },
    metrics: [
      { key: "riskScore", label: "riskScore", value: 71, unit: "0-100", state: "measured" },
      { key: "findings", label: "activeRisks", value: 15, unit: "items", state: "measured" },
      { key: "staleObjects", label: "staleObjects", value: 34, unit: "objects", state: "measured" },
      { key: "privilegedExposure", label: "privilegedExposure", value: 6, unit: "items", state: "measured" }
    ],
    inventory: [
      { objectType: "User", totalCount: 412, isCurrent: true },
      { objectType: "Computer", totalCount: 187, isCurrent: true },
      { objectType: "Group", totalCount: 96, isCurrent: true },
      { objectType: "OrganizationalUnit", totalCount: 23, isCurrent: true },
      { objectType: "GroupPolicyContainer", totalCount: 31, isCurrent: true }
    ],
    severityBreakdown: Object.entries(sev).map(([severity, count]) => ({ severity, count })),
    categoryBreakdown: [
      { category: "Hygiene", count: 6 }, { category: "PrivilegedAccess", count: 4 },
      { category: "Hardening", count: 4 }, { category: "Governance", count: 1 }
    ],
    activeFindings: 15, identityScore: 71, tier0Score: 64, health: "yellow", criticalFindings: 2,
    installation: { serviceName: "Direnix.Service", bind: { listenAddress: "127.0.0.1", port: 8787 }, dataRoot: "(demo)", database: "(demo — nada é salvo)" }
  };

  const routes = [
    ["/health/live", { status: "Live" }],
    ["/health/ready", { status: "Ready" }],
    ["/api/v1/system/about", { product: "Direnix", service: "Direnix.Service", mode: "Demo", portal: { url: "http://127.0.0.1:8787/" }, installation: { database: "(demo)" } }],
    ["/api/v1/app/shell", shell],
    ["/api/v1/findings/detail", null], // tratado por prefixo abaixo
    ["/api/v1/changes/summary", {
      items: [
        { changeType: "PrivilegedMemberAdded", count: 1 }, { changeType: "ObjectCreated", count: 4 },
        { changeType: "MemberAdded", count: 9 }, { changeType: "AccountDisabled", count: 2 },
        { changeType: "SpnAdded", count: 1 }, { changeType: "AttributeChanged", count: 17 }
      ],
      newFindings: 3
    }],
    ["/api/v1/indicators/catalog", { items: [] }],
    ["/api/v1/indicators", {
      items: [
        { id: "IND-PWD-EXPIRING", title: "Senhas vencendo em 7 dias", category: "Contas", isCustom: false, count: 14, items: [indItem("a.costa", "vence em 2 dias"), indItem("b.lima", "vence em 3 dias"), indItem("c.souza", "vence em 4 dias"), indItem("d.rocha", "vence em 6 dias"), indItem("e.melo", "vence em 7 dias")] },
        { id: "IND-ACCT-LOCKED", title: "Contas bloqueadas agora", category: "Contas", isCustom: false, count: 5, items: [indItem("f.silva", "bloqueada às 08:12"), indItem("g.nunes", "bloqueada às 08:47"), indItem("h.dias", "bloqueada às 09:03")] },
        { id: "IND-PWD-EXPIRED", title: "Senhas expiradas", category: "Contas", isCustom: false, count: 3, items: [indItem("i.reis", "expirou ontem"), indItem("j.gomes", "expirou há 3 dias")] },
        { id: "IND-ACCT-EXPIRING", title: "Contas a expirar em 30 dias", category: "Contas", isCustom: false, count: 2, items: [indItem("terceirizado01", "expira em 12 dias", "terceirizado01")] },
        { id: "CUSTOM-PSO", title: "Contas de serviço sem PSO", category: "Custom", isCustom: true, count: 8, items: [indItem("svc-sql"), indItem("svc-print"), indItem("svc-backup")] }
      ]
    }],
    ["/api/v1/changes", {
      items: [
        { runId: RUN2, observedAt: iso(7), objectKey: DN("Domain Admins", "Grupos"), objectType: "Group", objectDisplay: "Domain Admins", changeType: "PrivilegedMemberAdded", attribute: "member", oldValue: null, newValue: "svc-backup", severity: "Critical" },
        { runId: RUN2, observedAt: iso(7), objectKey: DN("svc-novo", "Servicos"), objectType: "User", objectDisplay: "svc-novo", changeType: "ObjectCreated", attribute: "objectClass", oldValue: null, newValue: "user", severity: "Info" },
        { runId: RUN2, observedAt: iso(7), objectKey: DN("n.ferreira"), objectType: "User", objectDisplay: "n.ferreira", changeType: "ObjectCreated", attribute: "objectClass", oldValue: null, newValue: "user", severity: "Info" },
        { runId: RUN2, observedAt: iso(7), objectKey: DN("GRP-Financeiro", "Grupos"), objectType: "Group", objectDisplay: "GRP-Financeiro", changeType: "MemberAdded", attribute: "member", oldValue: null, newValue: "n.ferreira", severity: "Low" },
        { runId: RUN2, observedAt: iso(7), objectKey: DN("svc-sql", "Servicos"), objectType: "User", objectDisplay: "svc-sql", changeType: "SpnAdded", attribute: "servicePrincipalName", oldValue: null, newValue: "MSSQLSvc/srv-db02:1433", severity: "High" },
        { runId: RUN2, observedAt: iso(7), objectKey: DN("ex.func02", "Desligados"), objectType: "User", objectDisplay: "ex.func02", changeType: "AccountDisabled", attribute: "userAccountControl", oldValue: "512", newValue: "514", severity: "Low" },
        { runId: RUN2, observedAt: iso(7), objectKey: DN("m.souza"), objectType: "User", objectDisplay: "m.souza", changeType: "AttributeChanged", attribute: "title", oldValue: "Analista", newValue: "Coordenador", severity: "Info" }
      ]
    }],
    ["/api/v1/runs", {
      items: [
        { runId: RUN2, startedAt: iso(7), completedAt: iso(6.8), coverageMode: "Full", operator: "demo", executedAs: "CORP\\svc-direnix$", credentialPrincipal: null, objectCount: 749, findingCount: 15 },
        { runId: RUN1, startedAt: iso(31), completedAt: iso(30.8), coverageMode: "Full", operator: "demo", executedAs: "CORP\\svc-direnix$", credentialPrincipal: null, objectCount: 745, findingCount: 14 }
      ]
    }],
    ["/api/v1/inventory", { items: shell.inventory.map((i) => ({ ...i, lastObservedAt: iso(7) })) }],
    ["/api/v1/exceptions", { items: [] }],
    ["/api/v1/search", { items: [] }],
    ["/api/v1/audit", {
      items: [
        { timestamp: iso(6.8), actorUserId: "demo", action: "AssessmentStarted", targetType: "Run", targetId: RUN2, result: "Success" },
        { timestamp: iso(30.8), actorUserId: "demo", action: "AssessmentStarted", targetType: "Run", targetId: RUN1, result: "Success" },
        { timestamp: iso(32), actorUserId: "demo", action: "ScheduleSaved", targetType: "Schedule", targetId: "daily-02:00", result: "Success" }
      ]
    }],
    ["/api/v1/schedule", { enabled: true, frequency: "Daily", timeOfDay: "02:00", intervalHours: 24, weekdays: [1, 2, 3, 4, 5], host: "dc01.corp.exemplo.local", profile: "MicrosoftDefault", lastRunAt: iso(6.8), nextRunAt: isoIn(19) }],
    ["/api/v1/service/status", { installed: false }],
    ["/api/v1/notifications/config", { smtpEnabled: false, smtpHost: "smtp.corp.exemplo.local", smtpPort: 587, smtpUseStartTls: true, smtpUsername: "direnix@corp.exemplo.local", smtpHasPassword: true, smtpFrom: "direnix@corp.exemplo.local", smtpTo: "identidade@corp.exemplo.local", webhookEnabled: false, webhookUrl: "", policy: "OnlyWhenActivity", lang: "pt", lastOutcome: { at: iso(6.7), skipped: false, skipReason: null, results: [{ ok: true, channel: "smtp", detail: "Enviado para 1 destinatário(s)." }] } }],
    ["/api/v1/system/update", { enabled: false, current: "0.9.0", latest: null, updateAvailable: false, releaseUrl: "https://github.com/wgerade/direnix/releases/latest", checkedAt: null, note: null }]
  ];

  window.DEMO_RESOLVE = function (url) {
    const path = url.split("?")[0];
    if (path === "/api/v1/findings/detail") {
      const key = new URLSearchParams(url.split("?")[1] || "").get("key");
      const f = findings.find((x) => x.stableFindingKey === key) || findings[0];
      return JSON.parse(JSON.stringify(f));
    }
    if (path === "/api/v1/findings") {
      const params = new URLSearchParams(url.split("?")[1] || "");
      const view = params.get("view") || "active";
      const category = params.get("category");
      let items = view === "active" ? findings : [];
      if (category) items = items.filter((f) => f.category === category);
      const limit = Number(params.get("limit") || 0);
      if (limit > 0) items = items.slice(0, limit);
      return { count: items.length, items: JSON.parse(JSON.stringify(items)) };
    }
    // Rotas exatas (querystring ignorada para changes/audit/runs etc.)
    for (const [route, data] of routes) {
      if (data !== null && (path === route)) return JSON.parse(JSON.stringify(data));
    }
    return null;
  };
})();
