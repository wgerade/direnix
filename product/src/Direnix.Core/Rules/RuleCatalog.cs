using Direnix.Core.Collection;
using Direnix.Core.Findings;

namespace Direnix.Core.Rules;

/// <summary>
/// Metadados estaticos de uma regra: nome legivel, categoria, acao recomendada,
/// referencias de frameworks e remediacao. A remediacao PowerShell tem DOIS
/// comandos: <see cref="RemediationPreview"/> (com -WhatIf, so simula/valida, nao
/// altera nada) e <see cref="RemediationApply"/> (executa de fato a mudanca).
/// <see cref="RemediationApply"/> pode ser vazio quando nao existe um comando unico
/// (ex.: rotacao de krbtgt) — nesse caso o usuario segue o passo a passo manual.
/// Placeholders: {sam}, {dn}, {domain}, {group}.
/// </summary>
public sealed record RuleDefinition(
    string RuleId,
    string Title,
    FindingCategory Category,
    FindingDecision Action,
    string BusinessImpact,
    IReadOnlyList<string> Mitre,
    IReadOnlyList<string> Cis,
    IReadOnlyList<string> Nist,
    string MicrosoftRef,
    IReadOnlyList<string> RemediationManual,
    string RemediationPreview,
    string RemediationApply);

/// <summary>
/// Catalogo central das regras. Fonte unica de titulo/categoria/acao/frameworks/remediacao.
/// </summary>
public static class RuleCatalog
{
    private static readonly IReadOnlyDictionary<string, RuleDefinition> Definitions =
        new[]
        {
            new RuleDefinition(
                "ADCLN-USER-STALE-001",
                "Conta de usuario habilitada sem uso recente",
                FindingCategory.Hygiene,
                FindingDecision.CleanUp,
                "Contas ativas sem uso ampliam a superficie de ataque e podem ser sequestradas sem deteccao.",
                ["T1078"],
                ["CIS 5.3"],
                ["AC-2(3)"],
                "https://learn.microsoft.com/defender-for-identity/security-posture-assessments/accounts",
                [
                    "Abrir 'Usuarios e Computadores do Active Directory' (dsa.msc).",
                    "Localizar a conta pelo nome.",
                    "Botao direito > Desabilitar conta.",
                    "Mover para uma OU de quarentena e, apos o periodo de retencao, excluir."
                ],
                "Disable-ADAccount -Identity \"{sam}\" -WhatIf",
                "Disable-ADAccount -Identity \"{sam}\""),

            new RuleDefinition(
                "ADCLN-COMP-STALE-003",
                "Computador habilitado sem atividade recente",
                FindingCategory.Hygiene,
                FindingDecision.CleanUp,
                "Computadores obsoletos representam inventario morto e credenciais de maquina nao rotacionadas.",
                ["T1078"],
                ["CIS 1.1", "CIS 5.3"],
                ["CM-8"],
                "https://learn.microsoft.com/defender-for-identity/security-posture-assessments/accounts",
                [
                    "Abrir 'Usuarios e Computadores do Active Directory' (dsa.msc).",
                    "Localizar o objeto de computador.",
                    "Botao direito > Desabilitar conta.",
                    "Apos o periodo de retencao, excluir o objeto."
                ],
                "Disable-ADAccount -Identity \"{sam}\" -WhatIf",
                "Disable-ADAccount -Identity \"{sam}\""),

            new RuleDefinition(
                "ADCLN-USER-DISABLED-RETENTION-002",
                "Conta desabilitada retida alem da politica",
                FindingCategory.Hygiene,
                FindingDecision.CleanUp,
                "Objetos desabilitados acumulados dificultam auditoria e podem ser reativados indevidamente.",
                ["T1078"],
                ["CIS 5.3"],
                ["AC-2"],
                "https://learn.microsoft.com/windows-server/identity/ad-ds/plan/security-best-practices/best-practices-for-securing-active-directory",
                [
                    "Confirmar que a conta nao possui dependencias (servicos, ACLs).",
                    "Abrir dsa.msc e localizar a conta desabilitada.",
                    "Excluir o objeto (preferir AD Recycle Bin habilitado)."
                ],
                "Remove-ADUser -Identity \"{sam}\" -WhatIf",
                "Remove-ADUser -Identity \"{sam}\" -Confirm:$false"),

            new RuleDefinition(
                "ADAUTH-MAQ-009",
                "MachineAccountQuota permite ingresso por usuarios comuns",
                FindingCategory.Hardening,
                FindingDecision.Adjust,
                "Usuarios sem privilegio podem criar contas de computador, facilitando caminhos de escalada (ex. RBCD).",
                ["T1136.002"],
                ["CIS 5.1"],
                ["AC-6"],
                "https://learn.microsoft.com/defender-for-identity/security-posture-assessments/identity-infrastructure",
                [
                    "Definir ms-DS-MachineAccountQuota do dominio como 0.",
                    "Delegar o ingresso de maquinas apenas a grupos aprovados.",
                    "Validar dependencias de provisionamento antes de aplicar."
                ],
                "Set-ADDomain -Identity \"{domain}\" -Replace @{\"ms-DS-MachineAccountQuota\"=\"0\"} -WhatIf",
                "Set-ADDomain -Identity \"{domain}\" -Replace @{\"ms-DS-MachineAccountQuota\"=\"0\"}"),

            new RuleDefinition(
                "ADAUTH-KRB-PREAUTH-006",
                "Conta sem pre-autenticacao Kerberos (AS-REP roasting)",
                FindingCategory.Hardening,
                FindingDecision.Adjust,
                "Permite extrair material para quebra de senha offline (AS-REP roasting).",
                ["T1558.004"],
                ["CIS 16.1"],
                ["IA-5"],
                "https://learn.microsoft.com/defender-for-identity/security-posture-assessments/accounts",
                [
                    "Abrir dsa.msc e localizar a conta.",
                    "Propriedades > aba Conta > Opcoes de conta.",
                    "Desmarcar 'Nao exigir pre-autenticacao Kerberos'."
                ],
                "Set-ADAccountControl -Identity \"{sam}\" -DoesNotRequirePreAuth $false -WhatIf",
                "Set-ADAccountControl -Identity \"{sam}\" -DoesNotRequirePreAuth $false"),

            new RuleDefinition(
                "ADSVC-UNCONSTR-004",
                "Delegacao Kerberos irrestrita (unconstrained)",
                FindingCategory.PrivilegedAccess,
                FindingDecision.Adjust,
                "Permite que o host capture e reutilize TGTs de qualquer usuario que se autentique nele.",
                ["T1558", "T1550"],
                ["CIS 5.1"],
                ["AC-6"],
                "https://learn.microsoft.com/defender-for-identity/security-posture-assessments/accounts",
                [
                    "Abrir dsa.msc e localizar o objeto (usuario/computador).",
                    "Propriedades > aba Delegacao.",
                    "Trocar para 'Confiar somente para servicos especificos' (constrained) ou remover a delegacao."
                ],
                "Set-ADAccountControl -Identity \"{sam}\" -TrustedForDelegation $false -WhatIf",
                "Set-ADAccountControl -Identity \"{sam}\" -TrustedForDelegation $false"),

            new RuleDefinition(
                "ADPRV-T0-GROUPS-001",
                "Membro de grupo privilegiado (Tier 0)",
                FindingCategory.PrivilegedAccess,
                FindingDecision.Adjust,
                "Excesso de membros em grupos Tier 0 amplia o risco de comprometimento total do dominio.",
                ["T1078.002", "T1098"],
                ["CIS 5.1", "CIS 6.8"],
                ["AC-6(5)"],
                "https://learn.microsoft.com/windows-server/identity/ad-ds/plan/security-best-practices/best-practices-for-securing-active-directory",
                [
                    "Revisar a necessidade real do acesso privilegiado deste objeto.",
                    "Abrir dsa.msc, localizar o grupo privilegiado e a aba Membros.",
                    "Remover membros nao aprovados; preferir contas administrativas dedicadas e PAM."
                ],
                "Get-ADPrincipalGroupMembership -Identity \"{sam}\" | Where-Object { $_.Name -match 'Admins|Operators' }",
                "Remove-ADGroupMember -Identity \"{group}\" -Members \"{sam}\" -Confirm:$false"),

            new RuleDefinition(
                "ADPRV-KRBTGT-AGE-010",
                "Senha da conta krbtgt sem rotacao recente",
                FindingCategory.PrivilegedAccess,
                FindingDecision.Adjust,
                "Senha antiga do krbtgt facilita ataques de Golden Ticket por longos periodos.",
                ["T1558.001"],
                ["CIS 5.1"],
                ["IA-5"],
                "https://learn.microsoft.com/defender-for-identity/security-posture-assessments/identity-infrastructure",
                [
                    "Planejar janela de manutencao (rotacao dupla com intervalo > replicacao).",
                    "Usar o script oficial da Microsoft para reset do krbtgt.",
                    "Validar autenticacao apos cada rotacao."
                ],
                "# Sem -WhatIf: a rotacao do krbtgt nao tem comando unico seguro. Siga os passos manuais.",
                ""),

            new RuleDefinition(
                "ADGOV-RECYCLEBIN-001",
                "Lixeira do AD desabilitada ou retencao insuficiente",
                FindingCategory.Governance,
                FindingDecision.Implement,
                "Sem a Lixeira do AD (ou com retencao curta) objetos excluidos por engano ou por ataque nao podem ser recuperados, e a investigacao de exclusoes fica comprometida.",
                ["T1485", "T1070"],
                ["CIS 11.1"],
                ["CP-9", "CP-10"],
                "https://learn.microsoft.com/windows-server/identity/ad-ds/get-started/adac/introduction-to-active-directory-administrative-center-enhancements--level-100-#bkmk_restoredeletedobjects",
                [
                    "Pre-requisito: nivel funcional da floresta Windows Server 2008 R2 ou superior. A ativacao e irreversivel.",
                    "Confirme o estado atual: Get-ADOptionalFeature -Filter 'name -eq \"Recycle Bin Feature\"' (campo EnabledScopes).",
                    "Planeje: apos habilitar, objetos excluidos passam a ser recuperaveis pela janela de msDS-deletedObjectLifetime (padrao = tombstoneLifetime, tipicamente 180 dias).",
                    "Habilite pelo Centro Administrativo do AD (dsac.exe) > dominio > 'Habilitar Lixeira', OU via PowerShell (comando abaixo, valide com -WhatIf antes).",
                    "Se ja habilitada porem com retencao curta, ajuste msDS-deletedObjectLifetime conforme a politica de retencao aprovada.",
                    "Valide a recuperacao: exclua um objeto de teste e restaure pelo Centro Administrativo do AD."
                ],
                "Enable-ADOptionalFeature 'Recycle Bin Feature' -Scope ForestOrConfigurationSet -Target \"{domain}\" -WhatIf",
                "Enable-ADOptionalFeature 'Recycle Bin Feature' -Scope ForestOrConfigurationSet -Target \"{domain}\" -Confirm:$false"),

            // ---- Limpeza: Grupos / OUs / GPOs ----
            new RuleDefinition(
                "ADCLN-GROUP-EMPTY-011",
                "Grupo de seguranca vazio (sem membros)",
                FindingCategory.Hygiene,
                FindingDecision.CleanUp,
                "Grupos vazios poluem o diretorio, dificultam o modelo de menor privilegio e podem ser repovoados indevidamente.",
                ["T1098"],
                ["CIS 5.3"],
                ["AC-2"],
                "https://learn.microsoft.com/windows-server/identity/ad-ds/plan/security-best-practices/best-practices-for-securing-active-directory",
                [
                    "Confirme que o grupo nao e usado em ACLs, GPOs ou aplicacoes (membros implicitos via primaryGroupID nao aparecem em 'member').",
                    "Abra dsa.msc e localize o grupo.",
                    "Documente o dono/uso; se realmente sem uso, exclua o grupo."
                ],
                "Remove-ADGroup -Identity \"{sam}\" -WhatIf",
                "Remove-ADGroup -Identity \"{sam}\" -Confirm:$false"),

            new RuleDefinition(
                "ADCLN-OU-EMPTY-012",
                "Unidade organizacional (OU) vazia",
                FindingCategory.Hygiene,
                FindingDecision.CleanUp,
                "OUs vazias acumulam estrutura morta, confundem a delegacao e o escopo de GPOs.",
                [],
                ["CIS 1.1"],
                ["CM-8"],
                "https://learn.microsoft.com/windows-server/identity/ad-ds/plan/delegating-administration-of-account-ous-and-resource-ous",
                [
                    "Confirme que a OU nao tera objetos em breve e que nenhuma GPO importante depende do link nela.",
                    "Se a OU estiver protegida contra exclusao acidental, desmarque a protecao antes (aba Objeto > Recursos avancados).",
                    "Abra dsa.msc e exclua a OU vazia."
                ],
                "Remove-ADOrganizationalUnit -Identity \"{dn}\" -WhatIf",
                "Remove-ADOrganizationalUnit -Identity \"{dn}\" -Confirm:$false"),

            new RuleDefinition(
                "ADCLN-GPO-UNLINKED-013",
                "GPO nao vinculada (orfa)",
                FindingCategory.Governance,
                FindingDecision.CleanUp,
                "GPOs sem nenhum link nao se aplicam a nada: sao manutencao morta e ruido na analise de politicas.",
                [],
                ["CIS 4.1"],
                ["CM-6"],
                "https://learn.microsoft.com/troubleshoot/windows-server/group-policy/group-policy-overview",
                [
                    "Confirme no GPMC (Group Policy Management) que a GPO nao esta vinculada a dominio, OU ou site (inclusive links desabilitados).",
                    "Faca backup da GPO (Backup-GPO) antes de excluir.",
                    "Exclua a GPO se confirmada sem uso."
                ],
                "Get-GPO -Guid {guid} | Select-Object DisplayName, GpoStatus, CreationTime, ModificationTime",
                "Remove-GPO -Guid {guid} -Confirm:$false"),

            new RuleDefinition(
                "ADCLN-GPO-EMPTY-014",
                "GPO sem configuracoes (vazia)",
                FindingCategory.Governance,
                FindingDecision.CleanUp,
                "GPOs sem nenhuma configuracao (versao 0) so adicionam tempo de processamento de logon sem efeito.",
                [],
                ["CIS 4.1"],
                ["CM-6"],
                "https://learn.microsoft.com/troubleshoot/windows-server/group-policy/group-policy-overview",
                [
                    "No GPMC, confirme que a GPO nao tem configuracoes de Computador nem de Usuario (numero de versao 0).",
                    "Faca backup (Backup-GPO) e remova os links, se houver.",
                    "Exclua a GPO vazia."
                ],
                "Get-GPO -Guid {guid} | Select-Object DisplayName, @{n='Computer';e={$_.Computer.DSVersion}}, @{n='User';e={$_.User.DSVersion}}",
                "Remove-GPO -Guid {guid} -Confirm:$false"),

            new RuleDefinition(
                "ADCLN-GPO-DISABLED-015",
                "GPO com ambas as secoes desabilitadas",
                FindingCategory.Governance,
                FindingDecision.Adjust,
                "GPO com as secoes de Usuario e Computador desabilitadas nao tem efeito; se ainda vinculada, e ruido e confunde a governanca.",
                [],
                ["CIS 4.1"],
                ["CM-6"],
                "https://learn.microsoft.com/troubleshoot/windows-server/group-policy/group-policy-overview",
                [
                    "No GPMC, verifique o status da GPO (Computer/User Configuration Settings Disabled).",
                    "Se for intencional e sem uso, remova os links e exclua; se nao, reabilite a secao correta."
                ],
                "Get-GPO -Guid {guid} | Select-Object DisplayName, GpoStatus",
                "Remove-GPO -Guid {guid} -Confirm:$false"),

            // ---- Endurecimento: contas ----
            new RuleDefinition(
                "ADHARD-PWDNOTREQD-016",
                "Conta permite senha em branco (PASSWD_NOTREQD)",
                FindingCategory.Hardening,
                FindingDecision.Adjust,
                "Contas que dispensam senha podem ficar sem credencial, abrindo acesso trivial.",
                ["T1078"],
                ["CIS 5.2"],
                ["IA-5"],
                "https://learn.microsoft.com/defender-for-identity/security-posture-assessments/accounts",
                [
                    "Abra dsa.msc e localize a conta.",
                    "Remova o atributo 'Senha nao e necessaria' e exija a redefinicao de senha forte.",
                    "Valide se nao e conta de aplicacao dependente desse comportamento."
                ],
                "Set-ADAccountControl -Identity \"{sam}\" -PasswordNotRequired $false -WhatIf",
                "Set-ADAccountControl -Identity \"{sam}\" -PasswordNotRequired $false"),

            new RuleDefinition(
                "ADHARD-PWDNOEXPIRE-017",
                "Conta habilitada com senha que nunca expira",
                FindingCategory.Hardening,
                FindingDecision.Adjust,
                "Senhas que nunca expiram permanecem validas indefinidamente, ampliando a janela de abuso de credenciais vazadas.",
                ["T1078"],
                ["CIS 5.2"],
                ["IA-5"],
                "https://learn.microsoft.com/defender-for-identity/security-posture-assessments/accounts",
                [
                    "Avalie se a conta deveria seguir a politica de expiracao (contas de servico: prefira gMSA).",
                    "Abra dsa.msc, aba Conta, desmarque 'A senha nunca expira'.",
                    "Garanta rotacao de senha para contas de servico que nao puderem expirar."
                ],
                "Set-ADUser -Identity \"{sam}\" -PasswordNeverExpires $false -WhatIf",
                "Set-ADUser -Identity \"{sam}\" -PasswordNeverExpires $false"),

            new RuleDefinition(
                "ADHARD-REVERSIBLEPWD-018",
                "Conta com criptografia reversivel de senha",
                FindingCategory.Hardening,
                FindingDecision.Adjust,
                "Armazenar a senha de forma reversivel equivale a guardar a senha em texto recuperavel.",
                ["T1555"],
                ["CIS 5.2"],
                ["IA-5(1)"],
                "https://learn.microsoft.com/windows/security/threat-protection/security-policy-settings/store-passwords-using-reversible-encryption",
                [
                    "Abra dsa.msc, aba Conta, desmarque 'Armazenar senha usando criptografia reversivel'.",
                    "Force a redefinicao de senha da conta apos a mudanca.",
                    "Verifique GPOs que possam reaplicar a configuracao."
                ],
                "Set-ADUser -Identity \"{sam}\" -AllowReversiblePasswordEncryption $false -WhatIf",
                "Set-ADUser -Identity \"{sam}\" -AllowReversiblePasswordEncryption $false"),

            new RuleDefinition(
                "ADPRV-KERBEROAST-019",
                "Conta de usuario com SPN (kerberoastable)",
                FindingCategory.PrivilegedAccess,
                FindingDecision.Investigate,
                "Contas de usuario com SPN permitem solicitar tickets de servico e quebrar a senha offline (Kerberoasting).",
                ["T1558.003"],
                ["CIS 5.2"],
                ["IA-5"],
                "https://learn.microsoft.com/defender-for-identity/security-posture-assessments/accounts",
                [
                    "Prefira contas de servico gerenciadas (gMSA), que rotacionam a senha automaticamente.",
                    "Se precisar manter a conta de usuario, defina senha aleatoria de 25+ caracteres.",
                    "Remova SPNs desnecessarios e monitore eventos 4769 com criptografia RC4."
                ],
                "Get-ADUser -Identity \"{sam}\" -Properties servicePrincipalName | Select-Object -ExpandProperty servicePrincipalName",
                ""),

            new RuleDefinition(
                "ADCLN-COMP-LEGACYOS-020",
                "Computador com sistema operacional sem suporte",
                FindingCategory.Infrastructure,
                FindingDecision.Decommission,
                "Sistemas fora de suporte nao recebem correcoes de seguranca e sao alvo facil de movimentacao lateral.",
                ["T1210"],
                ["CIS 2.2"],
                ["SI-2"],
                "https://learn.microsoft.com/lifecycle/products/",
                [
                    "Confirme se o host ainda existe e e necessario.",
                    "Planeje a substituicao por um SO suportado ou o isolamento do host.",
                    "Como medida interina, desabilite a conta de computador apos validar."
                ],
                "Disable-ADAccount -Identity \"{sam}\" -WhatIf",
                "Disable-ADAccount -Identity \"{sam}\""),

            new RuleDefinition(
                "ADPRV-ADMINCOUNT-ORPHAN-021",
                "adminCount=1 orfao (ACL do AdminSDHolder residual)",
                FindingCategory.PrivilegedAccess,
                FindingDecision.Adjust,
                "Objeto marcado como protegido (adminCount=1) sem estar mais em grupo privilegiado mantem ACL restritiva e heranca desabilitada — sinal de privilegio antigo nao limpo.",
                ["T1078.002"],
                ["CIS 5.1"],
                ["AC-6"],
                "https://learn.microsoft.com/windows-server/identity/ad-ds/plan/security-best-practices/appendix-c--protected-accounts-and-groups-in-active-directory",
                [
                    "Confirme que o objeto realmente nao deve mais ser protegido (nao pertence a grupos Tier 0).",
                    "Limpe o atributo adminCount do objeto.",
                    "Reabilite a heranca de permissoes no objeto (aba Seguranca > Avancado > Habilitar heranca)."
                ],
                "Get-ADObject -Identity \"{dn}\" -Properties adminCount, nTSecurityDescriptor",
                "Set-ADObject -Identity \"{dn}\" -Clear adminCount"),

            // ---- Saude do banco do AD ----
            new RuleDefinition(
                "ADGOV-CONFLICT-022",
                "Objetos de conflito de replicacao (CNF)",
                FindingCategory.Governance,
                FindingDecision.Investigate,
                "Objetos 'CNF:' surgem de conflitos de replicacao (mesmo nome criado em DCs diferentes). Indicam problemas de replicacao e poluem o banco.",
                [],
                ["CIS 1.1"],
                ["CM-8"],
                "https://learn.microsoft.com/troubleshoot/windows-server/active-directory/ad-database-offline-defragmentation",
                [
                    "Liste os objetos de conflito e identifique o objeto 'bom' correspondente.",
                    "Reconcilie: mantenha o correto, migre dependencias e remova o duplicado (CNF).",
                    "Investigue a causa de replicacao (repadmin /showrepl) para evitar reincidencia.",
                    "Apos limpezas grandes, considere defrag offline do NTDS.dit (ntdsutil files compact to) para recuperar espaco."
                ],
                "Get-ADObject -LDAPFilter \"(cn=*\\0ACNF:*)\" -SearchScope Subtree -Properties whenCreated, lastKnownParent",
                ""),

            new RuleDefinition(
                "ADGOV-LOSTFOUND-023",
                "Objetos orfaos em LostAndFound",
                FindingCategory.Governance,
                FindingDecision.Investigate,
                "Objetos no contêiner LostAndFound ficaram sem pai (ex.: criados num DC enquanto a OU pai era excluida em outro). Indicam itens orfaos a reconciliar.",
                [],
                ["CIS 1.1"],
                ["CM-8"],
                "https://learn.microsoft.com/troubleshoot/windows-server/active-directory/ad-database-offline-defragmentation",
                [
                    "Liste os objetos no LostAndFound e decida mover (para a OU correta) ou excluir.",
                    "Mova com Move-ADObject ou exclua se obsoleto.",
                    "Apos limpezas grandes, rode a analise semantica do banco (ntdsutil 'semantic database analysis' > go fixup) e considere defrag offline."
                ],
                "Get-ADObject -SearchBase \"CN=LostAndFound,$((Get-ADDomain).DistinguishedName)\" -SearchScope OneLevel -Filter * -Properties whenCreated",
                ""),
        }
        .ToDictionary(definition => definition.RuleId, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<RuleDefinition> All => (IReadOnlyCollection<RuleDefinition>)Definitions.Values;

    /// <summary>
    /// Tipos de objeto que cada regra precisa ter coletado para poder ser
    /// reconciliada (resolver achados ausentes). Se o tipo nao foi coletado neste
    /// run, os achados da regra sao mantidos (carried forward), nao resolvidos.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, AdObjectType[]> Required =
        new Dictionary<string, AdObjectType[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["ADCLN-USER-STALE-001"] = [AdObjectType.User],
            ["ADCLN-COMP-STALE-003"] = [AdObjectType.Computer],
            ["ADCLN-USER-DISABLED-RETENTION-002"] = [AdObjectType.User],
            ["ADAUTH-MAQ-009"] = [AdObjectType.Domain],
            ["ADAUTH-KRB-PREAUTH-006"] = [AdObjectType.User],
            ["ADSVC-UNCONSTR-004"] = [AdObjectType.User, AdObjectType.Computer],
            ["ADPRV-T0-GROUPS-001"] = [AdObjectType.Group],
            ["ADPRV-KRBTGT-AGE-010"] = [AdObjectType.User],
            ["ADGOV-RECYCLEBIN-001"] = [AdObjectType.Domain],
            ["ADCLN-GROUP-EMPTY-011"] = [AdObjectType.Group],
            ["ADCLN-OU-EMPTY-012"] = [AdObjectType.OrganizationalUnit, AdObjectType.User, AdObjectType.Computer, AdObjectType.Group],
            ["ADCLN-GPO-UNLINKED-013"] = [AdObjectType.GroupPolicyContainer, AdObjectType.OrganizationalUnit],
            ["ADCLN-GPO-EMPTY-014"] = [AdObjectType.GroupPolicyContainer],
            ["ADCLN-GPO-DISABLED-015"] = [AdObjectType.GroupPolicyContainer],
            ["ADHARD-PWDNOTREQD-016"] = [AdObjectType.User],
            ["ADHARD-PWDNOEXPIRE-017"] = [AdObjectType.User],
            ["ADHARD-REVERSIBLEPWD-018"] = [AdObjectType.User],
            ["ADPRV-KERBEROAST-019"] = [AdObjectType.User],
            ["ADCLN-COMP-LEGACYOS-020"] = [AdObjectType.Computer],
            ["ADPRV-ADMINCOUNT-ORPHAN-021"] = [AdObjectType.User, AdObjectType.Group],
            ["ADGOV-CONFLICT-022"] = [AdObjectType.Domain],
            ["ADGOV-LOSTFOUND-023"] = [AdObjectType.Domain]
        };

    public static IReadOnlyList<AdObjectType> RequiredObjectTypes(string ruleId) =>
        Required.TryGetValue(ruleId, out var types) ? types : [];

    public static RuleDefinition Get(string ruleId) =>
        Definitions.TryGetValue(ruleId, out var definition)
            ? definition
            : new RuleDefinition(ruleId, ruleId, FindingCategory.Governance, FindingDecision.Investigate,
                string.Empty, [], [], [], string.Empty, [], string.Empty, string.Empty);

    public static bool TryGet(string ruleId, out RuleDefinition definition) =>
        Definitions.TryGetValue(ruleId, out definition!);
}
