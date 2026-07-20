param(
    [int]$Port = 8787,
    [ValidatePattern('^[A-Za-z0-9\.\-\+\*]+$')]
    [string]$ListenAddress = '127.0.0.1',
    [string]$DataRoot = '',
    [string]$OutputRoot = '',
    [string]$ConfigPath = '',
    [switch]$ResetDemoStore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptRoot
$DashboardRoot = Join-Path $ProjectRoot 'dashboard'

if ([string]::IsNullOrWhiteSpace($DataRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($env:DIRENIX_DATA_ROOT)) {
        $DataRoot = $env:DIRENIX_DATA_ROOT
    }
    else {
        $DataRoot = Join-Path $ProjectRoot 'data'
    }
}
$DataRoot = [IO.Path]::GetFullPath($DataRoot)

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($env:DIRENIX_OUTPUT_ROOT)) {
        $OutputRoot = $env:DIRENIX_OUTPUT_ROOT
    }
    elseif ($DataRoot -ieq [IO.Path]::GetFullPath((Join-Path $ProjectRoot 'data'))) {
        $OutputRoot = Join-Path $ProjectRoot 'output'
    }
    else {
        $OutputRoot = Join-Path (Split-Path -Parent $DataRoot) 'output'
    }
}
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)

if ([string]::IsNullOrWhiteSpace($ConfigPath) -and -not [string]::IsNullOrWhiteSpace($env:DIRENIX_CONFIG_PATH)) {
    $ConfigPath = $env:DIRENIX_CONFIG_PATH
}

$StorePath = Join-Path $DataRoot 'demo-store.adcx'
$OperationalStorePath = Join-Path $DataRoot 'direnix-store.adcx'
$Entropy = [Text.Encoding]::UTF8.GetBytes('DirenixDemoStore.v1')
. (Join-Path $ProjectRoot 'src\Direnix.Mfa.ps1')
. (Join-Path $ProjectRoot 'src\Direnix.Store.ps1')

Add-Type -AssemblyName System.Security

function Get-PortalDataProtectionScope {
    if ($env:DIRENIX_DPAPI_SCOPE -and $env:DIRENIX_DPAPI_SCOPE -ieq 'LocalMachine') {
        return [Security.Cryptography.DataProtectionScope]::LocalMachine
    }
    [Security.Cryptography.DataProtectionScope]::CurrentUser
}

function New-DirectoryIfMissing {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Force -Path $Path | Out-Null
    }
}

function ConvertTo-JsonBytes {
    param([object]$Value)
    [Text.Encoding]::UTF8.GetBytes((ConvertTo-Json -InputObject $Value -Depth 32))
}

function ConvertFrom-JsonBytes {
    param([byte[]]$Bytes)
    [Text.Encoding]::UTF8.GetString($Bytes) | ConvertFrom-Json
}

function Protect-Bytes {
    param([byte[]]$Bytes)
    [Security.Cryptography.ProtectedData]::Protect(
        $Bytes,
        $Entropy,
        (Get-PortalDataProtectionScope)
    )
}

function Unprotect-Bytes {
    param([byte[]]$Bytes)
    [Security.Cryptography.ProtectedData]::Unprotect(
        $Bytes,
        $Entropy,
        (Get-PortalDataProtectionScope)
    )
}

function New-Salt {
    $salt = New-Object byte[] 16
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($salt)
    }
    finally {
        $rng.Dispose()
    }
    $salt
}

function New-PasswordRecord {
    param([string]$Password)
    $salt = New-Salt
    $iterations = 120000
    $derive = New-Object Security.Cryptography.Rfc2898DeriveBytes($Password, $salt, $iterations)
    try {
        $hash = $derive.GetBytes(32)
    }
    finally {
        $derive.Dispose()
    }
    [pscustomobject]@{
        algorithm = 'PBKDF2-HMACSHA1'
        iterations = $iterations
        salt = [Convert]::ToBase64String($salt)
        hash = [Convert]::ToBase64String($hash)
    }
}

function Test-Password {
    param(
        [string]$Password,
        [object]$Record
    )
    $salt = [Convert]::FromBase64String([string]$Record.salt)
    $expected = [Convert]::FromBase64String([string]$Record.hash)
    $derive = New-Object Security.Cryptography.Rfc2898DeriveBytes($Password, $salt, [int]$Record.iterations)
    try {
        $actual = $derive.GetBytes($expected.Length)
    }
    finally {
        $derive.Dispose()
    }
    if ($actual.Length -ne $expected.Length) { return $false }
    $diff = 0
    for ($i = 0; $i -lt $actual.Length; $i++) {
        $diff = $diff -bor ($actual[$i] -bxor $expected[$i])
    }
    $diff -eq 0
}

function New-DemoStore {
    $now = (Get-Date).ToUniversalTime().ToString('o')
    [pscustomobject]@{
        schemaVersion = 'demo-0.1'
        createdAt = $now
        product = 'Direnix Demo'
        users = @(
            [pscustomobject]@{ id='u-admin'; username='admin@local'; displayName='Local Admin'; role='LocalAdmin'; password=New-PasswordRecord 'DemoAdmin!2026'; disabled=$false; failedAttempts=0; lockedUntil=$null; mfa=[pscustomobject]@{ enabled=$false; secret=$null; issuer='Direnix'; label='admin@local' } },
            [pscustomobject]@{ id='u-ops'; username='operator@local'; displayName='Collector Operator'; role='CollectorOperator'; password=New-PasswordRecord 'DemoOperator!2026'; disabled=$false; failedAttempts=0; lockedUntil=$null; mfa=[pscustomobject]@{ enabled=$false; secret=$null; issuer='Direnix'; label='operator@local' } },
            [pscustomobject]@{ id='u-risk'; username='risk@local'; displayName='Risk Manager'; role='RiskManager'; password=New-PasswordRecord 'DemoRisk!2026'; disabled=$false; failedAttempts=0; lockedUntil=$null; mfa=[pscustomobject]@{ enabled=$false; secret=$null; issuer='Direnix'; label='risk@local' } },
            [pscustomobject]@{ id='u-audit'; username='auditor@local'; displayName='Auditor'; role='Auditor'; password=New-PasswordRecord 'DemoAuditor!2026'; disabled=$false; failedAttempts=0; lockedUntil=$null; mfa=[pscustomobject]@{ enabled=$false; secret=$null; issuer='Direnix'; label='auditor@local' } },
            [pscustomobject]@{ id='u-mfa'; username='mfa@local'; displayName='MFA Auditor'; role='Auditor'; password=New-PasswordRecord 'DemoMfa!2026'; disabled=$false; failedAttempts=0; lockedUntil=$null; mfa=[pscustomobject]@{ enabled=$true; secret='JBSWY3DPEHPK3PXP'; issuer='Direnix'; label='mfa@local' } },
            [pscustomobject]@{ id='u-exec'; username='executive@local'; displayName='Executive Viewer'; role='ExecutiveViewer'; password=New-PasswordRecord 'DemoExec!2026'; disabled=$false; failedAttempts=0; lockedUntil=$null; mfa=[pscustomobject]@{ enabled=$false; secret=$null; issuer='Direnix'; label='executive@local' } }
        )
        runs = @(
            [pscustomobject]@{ runId='run-2026-06-19-0800'; startedAt='2026-06-19T08:00:00Z'; completedAt='2026-06-19T08:12:31Z'; domain='corp.example'; policyProfile='MicrosoftDefault'; depth='Standard'; objectCount=48210; findingCount=43; criticalCount=4; healthScore=62 },
            [pscustomobject]@{ runId='run-2026-06-20-0830'; startedAt='2026-06-20T08:30:00Z'; completedAt='2026-06-20T08:41:44Z'; domain='corp.example'; policyProfile='MicrosoftDefault'; depth='Standard'; objectCount=48732; findingCount=39; criticalCount=3; healthScore=68 }
        )
        scores = [pscustomobject]@{
            executive = 68
            identityProtection = 61
            privilegedAccess = 56
            cleanupHygiene = 73
            replicationDcHealth = 84
            gpoBaseline = 69
            hybridIdentity = 72
            governance = 63
        }
        trends = @(
            [pscustomobject]@{ label='Jun 19'; healthScore=62; critical=4; high=12; cleanupDebt=1286 },
            [pscustomobject]@{ label='Jun 20'; healthScore=68; critical=3; high=10; cleanupDebt=1194 }
        )
        metrics = @(
            [pscustomobject]@{ key='criticalIdentityExposure'; label='Critical identity exposure'; value=3; previous=4; target=0; unit='count'; status='High' },
            [pscustomobject]@{ key='cleanupDebt'; label='Cleanup debt'; value=1194; previous=1286; target=600; unit='objects'; status='Medium' },
            [pscustomobject]@{ key='staleUsers'; label='Enabled stale users'; value=214; previous=239; target=0; unit='users'; status='Medium' },
            [pscustomobject]@{ key='staleComputers'; label='Enabled stale computers'; value=822; previous=861; target=0; unit='computers'; status='Medium' },
            [pscustomobject]@{ key='replicationRisk'; label='Replication risk'; value=1; previous=2; target=0; unit='signals'; status='Medium' },
            [pscustomobject]@{ key='acceptedRiskExpiring'; label='Accepted risks expiring'; value=2; previous=1; target=0; unit='exceptions'; status='High' }
        )
        findings = @(
            [pscustomobject]@{
                id='F-0001'; stableKey='ADPRV-DCSYNC-004|corp.example|svc-sync-legacy'; ruleId='ADPRV-DCSYNC-004'; title='Non-approved principal with DCSync-equivalent permissions'; category='Privileged Access'; severity='Critical'; decision='Adjust'; status='Open'; businessRiskScore=96; domain='corp.example'; businessUnit='Identity Platform'; owner='AD Engineering'; objectType='ServiceAccount'; objectName='svc-sync-legacy'; objectDn='CN=svc-sync-legacy,OU=Service Accounts,DC=corp,DC=example'; objectSid='S-1-5-21-1000-2000-3000-4421'; firstSeen='2026-06-19T08:00:00Z'; lastSeen='2026-06-20T08:30:00Z'; ageDays=1; benchmark='Microsoft Defender for Identity / AD security best practices'; measuredValue='DS-Replication-Get-Changes-All'; expectedValue='Only DCs and approved sync principals'; managementSummary='One non-approved identity can replicate directory secrets.'; technicalImpact='This permission set can enable credential extraction paths similar to DCSync.'; recommendation='Validate dependency, remove replication permissions, and replace with approved sync principal if needed.'; evidence=@('evidence/acl/domain-root-dcsync.json'); managementSafe=$true
            },
            [pscustomobject]@{
                id='F-0002'; stableKey='ADSVC-UNCONSTR-004|corp.example|APP01'; ruleId='ADSVC-UNCONSTR-004'; title='Unconstrained delegation enabled'; category='Service Accounts'; severity='Critical'; decision='Adjust'; status='New'; businessRiskScore=91; domain='corp.example'; businessUnit='ERP'; owner='Application Team'; objectType='Computer'; objectName='APP01'; objectDn='CN=APP01,OU=Servers,DC=corp,DC=example'; objectSid='S-1-5-21-1000-2000-3000-8911'; firstSeen='2026-06-20T08:30:00Z'; lastSeen='2026-06-20T08:30:00Z'; ageDays=0; benchmark='Microsoft Defender for Identity delegation assessment'; measuredValue='TRUSTED_FOR_DELEGATION'; expectedValue='No unconstrained delegation'; managementSummary='A server can receive delegated Kerberos credentials broadly.'; technicalImpact='Unconstrained delegation can expose reusable credentials if a privileged user authenticates to the host.'; recommendation='Move to constrained delegation/RBCD with explicit target services after application validation.'; evidence=@('evidence/accounts/app01-delegation.json'); managementSafe=$true
            },
            [pscustomobject]@{
                id='F-0003'; stableKey='ADAUTH-MAQ-009|corp.example|domain'; ruleId='ADAUTH-MAQ-009'; title='MachineAccountQuota is above approved value'; category='Authentication'; severity='High'; decision='Adjust'; status='Open'; businessRiskScore=82; domain='corp.example'; businessUnit='Core Directory'; owner='AD Engineering'; objectType='Domain'; objectName='corp.example'; objectDn='DC=corp,DC=example'; objectSid=''; firstSeen='2026-06-19T08:00:00Z'; lastSeen='2026-06-20T08:30:00Z'; ageDays=1; benchmark='Microsoft Defender for Identity insecure domain configurations'; measuredValue='10'; expectedValue='0'; managementSummary='Non-privileged users can still create computer accounts.'; technicalImpact='Default MachineAccountQuota can be abused in several AD attack paths.'; recommendation='Confirm provisioning process and set ms-DS-MachineAccountQuota to 0.'; evidence=@('evidence/domain/domain-attributes.json'); managementSafe=$true
            },
            [pscustomobject]@{
                id='F-0004'; stableKey='ADCLN-USER-STALE-001|corp.example|users'; ruleId='ADCLN-USER-STALE-001'; title='Enabled stale users exceed policy threshold'; category='Cleanup'; severity='Medium'; decision='CleanUp'; status='Open'; businessRiskScore=58; domain='corp.example'; businessUnit='Multiple'; owner='IAM Operations'; objectType='UserSet'; objectName='214 enabled stale users'; objectDn='redacted'; objectSid='redacted'; firstSeen='2026-06-19T08:00:00Z'; lastSeen='2026-06-20T08:30:00Z'; ageDays=1; benchmark='Microsoft Defender for Identity stale accounts / NIST AC-2'; measuredValue='214 users > 90 days'; expectedValue='0 unowned stale enabled users'; managementSummary='214 enabled user accounts have no recent activity and should be reviewed.'; technicalImpact='Stale enabled accounts increase account takeover and audit scope.'; recommendation='Export list to IAM owners, disable confirmed stale accounts, and document exceptions.'; evidence=@('evidence/cleanup/stale-users.csv'); managementSafe=$true
            },
            [pscustomobject]@{
                id='F-0005'; stableKey='ADDC-REPL-FAIL-001|corp.example|DC02'; ruleId='ADDC-REPL-FAIL-001'; title='Replication warning remains on one domain controller'; category='Replication'; severity='Medium'; decision='Investigate'; status='Recurring'; businessRiskScore=64; domain='corp.example'; businessUnit='Infrastructure'; owner='Directory Operations'; objectType='DomainController'; objectName='DC02'; objectDn='CN=DC02,OU=Domain Controllers,DC=corp,DC=example'; objectSid='redacted'; firstSeen='2026-06-18T08:00:00Z'; lastSeen='2026-06-20T08:30:00Z'; ageDays=2; benchmark='Microsoft AD replication troubleshooting'; measuredValue='1 intermittent partner warning'; expectedValue='0 replication failures'; managementSummary='One domain controller has recurring replication warnings.'; technicalImpact='Persistent replication issues can lead to policy inconsistency and authentication failures.'; recommendation='Review repadmin output, event logs, site link latency and DNS for DC02.'; evidence=@('evidence/replication/dc02-replsummary.txt'); managementSafe=$true
            },
            [pscustomobject]@{
                id='F-0006'; stableKey='ADGOV-EXPIRED-002|corp.example|EX-104'; ruleId='ADGOV-EXPIRED-002'; title='Accepted risk exception is expiring soon'; category='Governance'; severity='High'; decision='AcceptRisk'; status='AcceptedRisk'; businessRiskScore=74; domain='corp.example'; businessUnit='Manufacturing'; owner='Risk Office'; objectType='RiskException'; objectName='EX-104 legacy NTLM dependency'; objectDn='redacted'; objectSid='redacted'; firstSeen='2026-06-19T08:00:00Z'; lastSeen='2026-06-20T08:30:00Z'; ageDays=1; benchmark='NIST risk governance'; measuredValue='Expires in 12 days'; expectedValue='Renew, remediate, or close before expiry'; managementSummary='A legacy NTLM risk exception needs review in 12 days.'; technicalImpact='NTLM dependency remains in a regulated manufacturing workflow.'; recommendation='RiskManager must renew with compensating controls or create remediation plan.'; evidence=@('evidence/governance/exception-ex104.json'); managementSafe=$true
            },
            [pscustomobject]@{
                id='F-0007'; stableKey='ADGPO-DC-PERM-004|corp.example|Default Domain Controllers Policy'; ruleId='ADGPO-DC-PERM-004'; title='Domain Controllers linked GPO has unsafe modify permissions'; category='Group Policy'; severity='Critical'; decision='Adjust'; status='Resolved'; businessRiskScore=0; domain='corp.example'; businessUnit='Core Directory'; owner='AD Engineering'; objectType='GPO'; objectName='Default Domain Controllers Policy'; objectDn='CN={6AC1786C-016F-11D2-945F-00C04FB984F9},CN=Policies,CN=System,DC=corp,DC=example'; objectSid=''; firstSeen='2026-06-19T08:00:00Z'; lastSeen='2026-06-19T08:00:00Z'; ageDays=1; benchmark='Microsoft Security Baselines'; measuredValue='Helpdesk group had Modify'; expectedValue='Approved GPO admins only'; managementSummary='A critical GPO permission issue was resolved since the last run.'; technicalImpact='Previously allowed unauthorized modification of DC policy.'; recommendation='Keep monitoring and require change approval for GPO permissions.'; evidence=@('evidence/gpo/dc-gpo-permissions.json'); managementSafe=$true
            },
            [pscustomobject]@{
                id='F-0008'; stableKey='ADDC-REPL-TOOLS|corp.example|capability'; ruleId='ADGOV-EVIDENCE-004'; title='Replication deep evidence capability missing'; category='Evidence'; severity='Info'; decision='Investigate'; status='CapabilityMissing'; businessRiskScore=5; domain='corp.example'; businessUnit='Infrastructure'; owner='Directory Operations'; objectType='Capability'; objectName='repadmin not available'; objectDn='redacted'; objectSid='redacted'; firstSeen='2026-06-20T08:30:00Z'; lastSeen='2026-06-20T08:30:00Z'; ageDays=0; benchmark='Collection coverage'; measuredValue='repadmin missing'; expectedValue='RSAT tool or DC Health Reader access'; managementSummary='Deep replication checks were partially unavailable in this run.'; technicalImpact='The run used available event/log evidence but could not execute full repadmin collection.'; recommendation='Run from a host with RSAT or grant the DC Health Reader path.'; evidence=@('evidence/preflight/capabilities.json'); managementSafe=$true
            }
        )
        collectionOptions = [pscustomobject]@{
            domains = @()
            ous = @()
            objectTypes = @('Users','Computers','Groups','Service Accounts','gMSA','OUs','GPOs','Domain Controllers','Trusts')
            featurePacks = @('Inventory','Cleanup Hygiene','Privileged Access','ACL and Delegation','Authentication Hardening','Service Accounts','GPO and Baseline','Replication and DC Health','Governance')
            depths = @('Quick','Standard','Deep')
            recommended = [pscustomobject]@{
                objectTypes = @('Users','Computers','Groups','OUs')
                featurePacks = @('Inventory','Cleanup Hygiene','Authentication Hardening','Replication and DC Health')
                depth = 'Standard'
            }
            defaultConnection = [pscustomobject]@{
                host = ''
                protocol = 'LDAPS'
                port = 636
                expectedDomain = ''
                authMode = 'CurrentWindowsIdentity'
            }
        }
        audit = @(
            [pscustomobject]@{ timestamp='2026-06-20T08:30:00Z'; actor='system'; role='System'; action='RunImported'; target='run-2026-06-20-0830'; result='Success' },
            [pscustomobject]@{ timestamp='2026-06-20T08:42:14Z'; actor='risk@local'; role='RiskManager'; action='RiskReviewed'; target='EX-104'; result='PendingDecision' }
        )
    }
}

function Save-Store {
    param([object]$Store)
    New-DirectoryIfMissing $DataRoot
    $plain = ConvertTo-JsonBytes $Store
    $protected = Protect-Bytes $plain
    $envelope = [pscustomobject]@{
        format = 'DirenixDemoEncryptedStore'
        version = 1
        protection = 'DPAPI CurrentUser'
        createdAt = (Get-Date).ToUniversalTime().ToString('o')
        payload = [Convert]::ToBase64String($protected)
    }
    $envelope | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $StorePath -Encoding ASCII
}

function Load-Store {
    if ($ResetDemoStore -or -not (Test-Path -LiteralPath $StorePath)) {
        $store = New-DemoStore
        Save-Store $store
        return $store
    }
    try {
        $envelope = Get-Content -Raw -LiteralPath $StorePath | ConvertFrom-Json
        $protected = [Convert]::FromBase64String([string]$envelope.payload)
        ConvertFrom-JsonBytes (Unprotect-Bytes $protected)
    }
    catch [Security.Cryptography.CryptographicException] {
        Write-Warning "Demo store cannot be opened by this Windows user. Recreating demo login store only: $StorePath"
        $store = New-DemoStore
        Save-Store $store
        $store
    }
}

function Get-ObjectProperty {
    param(
        [object]$Object,
        [string]$Name,
        [object]$Default = $null
    )
    if ($null -eq $Object) { return $Default }
    if ($Object.PSObject.Properties[$Name]) { return $Object.$Name }
    $Default
}

function Get-ArrayValue {
    param([object]$Value)
    if ($null -eq $Value) { return @() }
    @($Value)
}

function Get-DateValue {
    param([object]$Value)
    if ($null -eq $Value) { return [datetime]'1900-01-01T00:00:00Z' }
    try { return ([datetime]$Value).ToUniversalTime() }
    catch { return [datetime]'1900-01-01T00:00:00Z' }
}

function Convert-DomainNameToDn {
    param([string]$DomainName)
    $parts = @([string]$DomainName -split '\.' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($parts.Count -eq 0) { return '' }
    ($parts | ForEach-Object { 'DC=' + ($_ -replace '[^A-Za-z0-9_-]', '') }) -join ','
}

function Get-DirectoryPropertyValue {
    param(
        [object]$DirectoryEntry,
        [string]$Name
    )
    try {
        $property = $DirectoryEntry.Properties[$Name]
        if ($property -and $property.Count -gt 0) { return [string]$property[0] }
    }
    catch {}
    $null
}

function Test-TcpEndpoint {
    param(
        [string]$HostName,
        [int]$Port,
        [int]$TimeoutMs = 2500
    )
    $client = New-Object Net.Sockets.TcpClient
    try {
        $task = $client.ConnectAsync($HostName, $Port)
        $completed = $false
        try {
            $completed = $task.Wait($TimeoutMs)
        }
        catch {
            $message = if ($task.Exception -and $task.Exception.InnerException) {
                $task.Exception.InnerException.Message
            }
            else {
                $_.Exception.GetBaseException().Message
            }
            if ($message -match '(?i)refused|recus') {
                $message = 'Connection refused or port closed'
            }
            return [pscustomobject]@{ ok=$false; error=$message }
        }
        if (-not $completed) {
            return [pscustomobject]@{ ok=$false; error='Timeout' }
        }
        if ($client.Connected) {
            return [pscustomobject]@{ ok=$true; error='' }
        }
        [pscustomobject]@{ ok=$false; error='NotConnected' }
    }
    catch {
        [pscustomobject]@{ ok=$false; error=$_.Exception.Message }
    }
    finally {
        $client.Close()
        $client.Dispose()
    }
}

function Test-CollectionTarget {
    param([object]$Target)
    $hostName = ''
    $expectedDomain = ''
    $authMode = 'CurrentWindowsIdentity'
    $username = ''
    $password = ''
    $protocol = 'LDAPS'
    $port = 636
    if ($Target) {
        $hostName = ([string](Get-ObjectProperty $Target 'host' '')).Trim()
        $expectedDomain = ([string](Get-ObjectProperty $Target 'expectedDomain' '')).Trim()
        $requestedAuthMode = ([string](Get-ObjectProperty $Target 'authMode' '')).Trim()
        if ($requestedAuthMode -in @('CurrentWindowsIdentity','ExplicitCredential')) { $authMode = $requestedAuthMode }
        $username = ([string](Get-ObjectProperty $Target 'username' '')).Trim()
        $password = [string](Get-ObjectProperty $Target 'password' '')
        $requestedProtocol = ([string](Get-ObjectProperty $Target 'protocol' '')).Trim().ToUpperInvariant()
        if ($requestedProtocol -in @('LDAP','LDAPS')) { $protocol = $requestedProtocol }
        $requestedPort = Get-ObjectProperty $Target 'port' $null
        if ($requestedPort) { $port = [int]$requestedPort }
    }

    $warnings = New-Object Collections.Generic.List[string]
    $errors = New-Object Collections.Generic.List[string]
    $suggestedNamingContext = Convert-DomainNameToDn $expectedDomain
    $defaultNamingContext = ''
    $rootDseOk = $false
    $tcpOk = $false
    $tcpTested = $false
    $detectedDomain = ''
    $namingContexts = @()

    if ([string]::IsNullOrWhiteSpace($hostName)) {
        $errors.Add('Informe o hostname, FQDN ou IP do controlador de dominio antes de iniciar a avaliacao.')
    }
    elseif ($hostName -notmatch '^[A-Za-z0-9][A-Za-z0-9\.\-_:]*$') {
        $errors.Add('Formato de host invalido. Use hostname, FQDN, IPv4 ou IPv6 sem prefixo de protocolo.')
    }
    elseif ($port -lt 1 -or $port -gt 65535) {
        $errors.Add('Porta TCP invalida.')
    }
    elseif ($authMode -eq 'ExplicitCredential' -and ([string]::IsNullOrWhiteSpace($username) -or [string]::IsNullOrWhiteSpace($password))) {
        $errors.Add('Informe usuario e senha AD para validar com credencial temporaria.')
    }
    else {
        if ($protocol -eq 'LDAP') {
            $warnings.Add('LDAP na porta 389 nao e criptografado por padrao. Prefira LDAPS na 636; use LDAP apenas como excecao explicita em rede controlada.')
        }
        $tcpTested = $true
        $tcp = Test-TcpEndpoint -HostName $hostName -Port $port
        $tcpOk = [bool]$tcp.ok
        if (-not $tcpOk) {
            $errors.Add("TCP $hostName`:$port falhou: $($tcp.error)")
        }
        else {
            try {
                $rootPath = 'LDAP://{0}:{1}/RootDSE' -f $hostName, $port
                $authType = [System.DirectoryServices.AuthenticationTypes]::Secure
                if ($protocol -eq 'LDAPS') {
                    $authType = $authType -bor [System.DirectoryServices.AuthenticationTypes]::SecureSocketsLayer
                }
                if ($authMode -eq 'ExplicitCredential') {
                    $rootDse = New-Object System.DirectoryServices.DirectoryEntry($rootPath, $username, $password, $authType)
                }
                else {
                    $rootDse = New-Object System.DirectoryServices.DirectoryEntry($rootPath)
                    $rootDse.AuthenticationType = $authType
                }
                $defaultNamingContext = Get-DirectoryPropertyValue $rootDse 'defaultNamingContext'
                $configurationNamingContext = Get-DirectoryPropertyValue $rootDse 'configurationNamingContext'
                $schemaNamingContext = Get-DirectoryPropertyValue $rootDse 'schemaNamingContext'
                $dnsHostName = Get-DirectoryPropertyValue $rootDse 'dnsHostName'
                $rootDseOk = -not [string]::IsNullOrWhiteSpace($defaultNamingContext)
                $namingContexts = @($defaultNamingContext, $configurationNamingContext, $schemaNamingContext) | Where-Object { $_ }
                if ($defaultNamingContext -match 'DC=') {
                    $detectedDomain = (($defaultNamingContext -split ',') | Where-Object { $_ -match '^DC=' } | ForEach-Object { ($_ -replace '^DC=','') }) -join '.'
                }
                if (-not $rootDseOk) {
                    $errors.Add('TCP conectou, mas RootDSE nao retornou defaultNamingContext. O alvo nao esta pronto para avaliacao.')
                }
                if ($dnsHostName) {
                    $warnings.Add("Endpoint conectado reportou o DC $dnsHostName.")
                }
            }
            catch {
                $bindMessage = $_.Exception.Message
                $errors.Add("TCP conectou, mas a consulta RootDSE falhou: $bindMessage")
                if ($bindMessage -match 'usu.rio|senha|credentials|logon|operacional|operational') {
                    if ($authMode -eq 'CurrentWindowsIdentity') {
                        if ($protocol -eq 'LDAPS') {
                            $errors.Add('RootDSE por LDAPS nao autenticou ou falhou em TLS/certificado. Confirme certificado LDAPS confiavel na VM e, se a VM estiver fora do dominio, use uma credencial AD temporaria.')
                        }
                        else {
                            $errors.Add('A identidade Windows atual nao autenticou no AD. Use uma credencial AD temporaria para esta validacao ou execute a VM com uma conta do dominio.')
                        }
                    }
                    else {
                        $errors.Add('A credencial AD temporaria nao autenticou. Confirme usuario, formato UPN/dominio e senha.')
                    }
                }
            }
        }
    }

    $status = if ($errors.Count -gt 0 -or -not $rootDseOk) { 'Blocked' } elseif ($warnings.Count -gt 0) { 'ReadyWithWarnings' } else { 'Ready' }
    $scopeExamples = if ($defaultNamingContext) {
        @(
            $defaultNamingContext,
            "OU=Servers,$defaultNamingContext",
            "OU=Workstations,$defaultNamingContext",
            "CN=Users,$defaultNamingContext"
        )
    }
    elseif ($suggestedNamingContext) {
        @(
            $suggestedNamingContext,
            "OU=Servers,$suggestedNamingContext",
            "CN=Users,$suggestedNamingContext"
        )
    }
    else {
        @()
    }

    [pscustomobject]@{
        status = $status
        host = $hostName
        protocol = $protocol
        port = $port
        authMode = $authMode
        tcpTested = $tcpTested
        tcpOk = $tcpOk
        rootDseOk = $rootDseOk
        detectedDomain = $detectedDomain
        expectedDomain = $expectedDomain
        defaultNamingContext = $defaultNamingContext
        suggestedNamingContext = $suggestedNamingContext
        namingContexts = $namingContexts
        scopeExamples = $scopeExamples
        warnings = @($warnings)
        errors = @($errors)
    }
}

function Get-LatestRun {
    param([object[]]$Runs)
    @(Get-ArrayValue $Runs) |
        Sort-Object @{ Expression = { Get-DateValue (Get-ObjectProperty $_ 'completedAt' (Get-ObjectProperty $_ 'startedAt' $null)) } } -Descending |
        Select-Object -First 1
}

function Get-LatestOutputDataset {
    if (-not (Test-Path -LiteralPath $OutputRoot)) { return $null }
    $runDirectory = Get-ChildItem -LiteralPath (Join-Path $OutputRoot 'runs') -Directory -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if (-not $runDirectory) { return $null }

    $manifestPath = Join-Path $runDirectory.FullName 'run-manifest.json'
    $findingsPath = Join-Path $runDirectory.FullName 'findings.json'
    $scorecardPath = Join-Path $runDirectory.FullName 'scorecard.json'
    if (-not (Test-Path -LiteralPath $manifestPath)) { return $null }

    $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
    $findings = if (Test-Path -LiteralPath $findingsPath) { @(Get-Content -Raw -LiteralPath $findingsPath | ConvertFrom-Json) } else { @() }
    $scorecard = if (Test-Path -LiteralPath $scorecardPath) { Get-Content -Raw -LiteralPath $scorecardPath | ConvertFrom-Json } else { $null }
    [pscustomobject]@{
        source = 'OutputRun'
        sourcePath = $runDirectory.FullName
        store = [pscustomobject]@{
            runs = @($manifest)
            findings = $findings
            audit = @()
            scores = $null
            metrics = $null
            trends = $null
        }
        scorecard = $scorecard
        warnings = @()
    }
}

function Get-PortalDataset {
    $warnings = @()

    if (Test-Path -LiteralPath $OperationalStorePath) {
        try {
            $operational = Read-DirenixStore -StorePath $OperationalStorePath
            if (@(Get-ArrayValue $operational.runs).Count -gt 0) {
                return [pscustomobject]@{
                    source = 'RealStore'
                    sourcePath = $OperationalStorePath
                    store = $operational
                    scorecard = $null
                    warnings = $warnings
                }
            }
        }
        catch {
            $warnings += "Operational store unavailable: $($_.Exception.Message)"
        }
    }

    try {
        $outputDataset = Get-LatestOutputDataset
        if ($outputDataset) {
            $outputDataset.warnings = $warnings
            return $outputDataset
        }
    }
    catch {
        $warnings += "Output run unavailable: $($_.Exception.Message)"
    }

    [pscustomobject]@{
        source = 'DemoStore'
        sourcePath = $StorePath
        store = $script:Store
        scorecard = $null
        warnings = $warnings
    }
}

function Get-CoverageMode {
    param([object]$Run)
    $mode = Get-ObjectProperty $Run 'coverageMode' $null
    if ($mode) { return [string]$mode }
    $searchBase = @(Get-ArrayValue (Get-ObjectProperty $Run 'searchBase' @()))
    $objectTypes = @(Get-ArrayValue (Get-ObjectProperty $Run 'objectTypes' @()))
    if ($searchBase.Count -eq 0) { return 'NoDirectory' }
    if ($searchBase.Count -le 1 -and $objectTypes.Count -ge 4) { return 'StandardOrFull' }
    'Partial'
}

function Get-RunFindings {
    param(
        [object]$Dataset,
        [object]$LatestRun
    )
    $all = @(Get-ArrayValue $Dataset.store.findings)
    if (-not $LatestRun) { return $all }
    $runId = [string](Get-ObjectProperty $LatestRun 'runId' '')
    $byRun = @($all | Where-Object { (Get-ObjectProperty $_ 'runId' '') -eq $runId })
    if ($byRun.Count -gt 0) { return $byRun }
    if ($Dataset.source -ne 'DemoStore') { return $all }
    $all
}

function Get-HealthScore {
    param(
        [object]$LatestRun,
        [object[]]$Findings,
        [object]$Scorecard
    )
    $score = Get-ObjectProperty $LatestRun 'healthScore' $null
    if ($null -ne $score) { return [int]$score }
    $score = Get-ObjectProperty $Scorecard 'healthScore' $null
    if ($null -ne $score) { return [int]$score }
    $value = 100
    foreach ($finding in @(Get-ArrayValue $Findings)) {
        switch ([string](Get-ObjectProperty $finding 'severity' 'Info')) {
            'Critical' { $value -= 12 }
            'High' { $value -= 8 }
            'Medium' { $value -= 4 }
            'Low' { $value -= 2 }
        }
    }
    [Math]::Max(0, [Math]::Min(100, $value))
}

function Get-PortalMetrics {
    param(
        [object[]]$Findings,
        [int]$HealthScore
    )
    $critical = @($Findings | Where-Object { (Get-ObjectProperty $_ 'severity' '') -eq 'Critical' }).Count
    $high = @($Findings | Where-Object { (Get-ObjectProperty $_ 'severity' '') -eq 'High' }).Count
    $cleanup = @($Findings | Where-Object { (Get-ObjectProperty $_ 'decision' '') -eq 'CleanUp' }).Count
    $evidence = @($Findings | Where-Object { (Get-ObjectProperty $_ 'status' '') -eq 'CapabilityMissing' }).Count
    @(
        [pscustomobject]@{ key='healthScore'; label='Health score'; value=$HealthScore; previous=$null; target=100; unit='score'; status= if ($HealthScore -lt 70) { 'High' } elseif ($HealthScore -lt 85) { 'Medium' } else { 'Low' } },
        [pscustomobject]@{ key='criticalIdentityExposure'; label='Critical identity exposure'; value=$critical; previous=$null; target=0; unit='count'; status= if ($critical -gt 0) { 'High' } else { 'Low' } },
        [pscustomobject]@{ key='highRisk'; label='High risk findings'; value=$high; previous=$null; target=0; unit='count'; status= if ($high -gt 0) { 'Medium' } else { 'Low' } },
        [pscustomobject]@{ key='cleanupQueue'; label='Cleanup queue'; value=$cleanup; previous=$null; target=0; unit='findings'; status='Medium' },
        [pscustomobject]@{ key='evidenceGaps'; label='Evidence gaps'; value=$evidence; previous=$null; target=0; unit='gaps'; status= if ($evidence -gt 0) { 'Medium' } else { 'Low' } }
    )
}

function Get-PortalTrends {
    param(
        [object[]]$Runs,
        [object[]]$Findings
    )
    @(Get-ArrayValue $Runs) |
        Sort-Object @{ Expression = { Get-DateValue (Get-ObjectProperty $_ 'completedAt' (Get-ObjectProperty $_ 'startedAt' $null)) } } |
        Select-Object -Last 12 |
        ForEach-Object {
            $run = $_
            $runId = [string](Get-ObjectProperty $run 'runId' '')
            $runFindings = @($Findings | Where-Object { (Get-ObjectProperty $_ 'runId' '') -eq $runId })
            if ($runFindings.Count -eq 0) { $runFindings = @($Findings) }
            [pscustomobject]@{
                label = (Get-DateValue (Get-ObjectProperty $run 'completedAt' (Get-ObjectProperty $run 'startedAt' $null))).ToString('MM-dd HH:mm')
                runId = $runId
                healthScore = Get-HealthScore -LatestRun $run -Findings $runFindings -Scorecard $null
                critical = @($runFindings | Where-Object { (Get-ObjectProperty $_ 'severity' '') -eq 'Critical' }).Count
                high = @($runFindings | Where-Object { (Get-ObjectProperty $_ 'severity' '') -eq 'High' }).Count
                cleanupDebt = @($runFindings | Where-Object { (Get-ObjectProperty $_ 'decision' '') -eq 'CleanUp' }).Count
            }
        }
}

function Get-RolePerspective {
    param([string]$Role)
    switch ($Role) {
        'ExecutiveViewer' { [pscustomobject]@{ home='monitor'; lens='Gestao'; focus='Saude, tendencia, risco aceito e exposicao executiva'; allowedViews=@('monitor','overview','governance','help') } }
        'RiskManager' { [pscustomobject]@{ home='governance'; lens='Risco e governanca'; focus='Aceitar, vencer, renovar ou cobrar remediacao'; allowedViews=@('monitor','overview','operations','governance','findings','help') } }
        'Auditor' { [pscustomobject]@{ home='audit'; lens='Auditoria'; focus='Evidencia, trilha, escopo e conformidade'; allowedViews=@('monitor','overview','findings','audit','help') } }
        'CollectorOperator' { [pscustomobject]@{ home='operations'; lens='Operacao de coleta'; focus='Coletar, importar evidencia, validar cobertura e corrigir lacunas'; allowedViews=@('monitor','operations','collection','findings','help') } }
        default { [pscustomobject]@{ home='operations'; lens='AD Engineering'; focus='Remediar, gerar script, validar AD e acompanhar evolucao'; allowedViews=@('monitor','operations','overview','findings','collection','governance','audit','help') } }
    }
}

function Get-PortalOverview {
    param([object]$User)
    $dataset = Get-PortalDataset
    $runs = @(Get-ArrayValue $dataset.store.runs)
    $latestRun = Get-LatestRun -Runs $runs
    $findings = @(Get-RunFindings -Dataset $dataset -LatestRun $latestRun)
    $openFindings = @($findings | Where-Object { (Get-ObjectProperty $_ 'status' '') -ne 'Resolved' })
    $healthScore = Get-HealthScore -LatestRun $latestRun -Findings $findings -Scorecard $dataset.scorecard
    $rolePerspective = Get-RolePerspective -Role $User.role
    $coverageMode = Get-CoverageMode -Run $latestRun
    $domain = Get-ObjectProperty $latestRun 'domain' 'unknown'
    [pscustomobject]@{
        user = $User
        latestRun = $latestRun
        dataContext = [pscustomobject]@{
            mode = $dataset.source
            sourcePath = $dataset.sourcePath
            runId = Get-ObjectProperty $latestRun 'runId' ''
            collectionId = Get-ObjectProperty $latestRun 'collectionId' ''
            coverageMode = $coverageMode
            isPartial = ($coverageMode -ne 'StandardOrFull')
            domain = $domain
            warnings = $dataset.warnings
        }
        rolePerspective = $rolePerspective
        scores = [pscustomobject]@{
            executive = $healthScore
            identityProtection = $healthScore
            privilegedAccess = $healthScore
            cleanupHygiene = $healthScore
            replicationDcHealth = $healthScore
            gpoBaseline = $healthScore
            hybridIdentity = $healthScore
            governance = $healthScore
        }
        trends = @(Get-PortalTrends -Runs $runs -Findings @(Get-ArrayValue $dataset.store.findings))
        timeline = @(Get-PortalTrends -Runs $runs -Findings @(Get-ArrayValue $dataset.store.findings))
        metrics = @(Get-PortalMetrics -Findings $findings -HealthScore $healthScore)
        topRisks = @($openFindings | Sort-Object businessRiskScore -Descending | Select-Object -First 8 | ForEach-Object { Convert-FindingForRole $_ $User.role })
        counts = [pscustomobject]@{
            open = @($openFindings).Count
            critical = @($openFindings | Where-Object { (Get-ObjectProperty $_ 'severity' '') -eq 'Critical' }).Count
            high = @($openFindings | Where-Object { (Get-ObjectProperty $_ 'severity' '') -eq 'High' }).Count
            new = @($findings | Where-Object { (Get-ObjectProperty $_ 'status' '') -eq 'New' }).Count
            resolved = @($findings | Where-Object { (Get-ObjectProperty $_ 'status' '') -eq 'Resolved' }).Count
            acceptedRisk = @($findings | Where-Object { (Get-ObjectProperty $_ 'status' '') -eq 'AcceptedRisk' }).Count
            capabilityMissing = @($findings | Where-Object { (Get-ObjectProperty $_ 'status' '') -eq 'CapabilityMissing' }).Count
        }
    }
}

function New-Token {
    $bytes = New-Object byte[] 32
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($bytes)
    }
    finally {
        $rng.Dispose()
    }
    [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+','-').Replace('/','_')
}

function Write-JsonResponse {
    param(
        [Net.HttpListenerResponse]$Response,
        [object]$Value,
        [int]$StatusCode = 200
    )
    $Response.StatusCode = $StatusCode
    $Response.ContentType = 'application/json; charset=utf-8'
    $bytes = [Text.Encoding]::UTF8.GetBytes((ConvertTo-Json -InputObject $Value -Depth 32))
    $Response.ContentLength64 = $bytes.Length
    $Response.OutputStream.Write($bytes, 0, $bytes.Length)
}

function Write-TextResponse {
    param(
        [Net.HttpListenerResponse]$Response,
        [string]$Text,
        [string]$ContentType,
        [int]$StatusCode = 200
    )
    $Response.StatusCode = $StatusCode
    $Response.ContentType = $ContentType
    $bytes = [Text.Encoding]::UTF8.GetBytes($Text)
    $Response.ContentLength64 = $bytes.Length
    $Response.OutputStream.Write($bytes, 0, $bytes.Length)
}

function Read-JsonBody {
    param([Net.HttpListenerRequest]$Request)
    $reader = New-Object IO.StreamReader($Request.InputStream, $Request.ContentEncoding)
    try {
        $text = $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
    if ([string]::IsNullOrWhiteSpace($text)) { return $null }
    $text | ConvertFrom-Json
}

function Get-CookieValue {
    param(
        [Net.HttpListenerRequest]$Request,
        [string]$Name
    )
    foreach ($cookie in $Request.Cookies) {
        if ($cookie.Name -eq $Name) { return $cookie.Value }
    }
    $null
}

function Add-Audit {
    param(
        [string]$Actor,
        [string]$Role,
        [string]$Action,
        [string]$Target,
        [string]$Result
    )
    $script:Store.audit += [pscustomobject]@{
        timestamp = (Get-Date).ToUniversalTime().ToString('o')
        actor = $Actor
        role = $Role
        action = $Action
        target = $Target
        result = $Result
    }
    Save-Store $script:Store
}

function Get-SessionUser {
    param([Net.HttpListenerRequest]$Request)
    $token = $Request.Headers['X-Direnix-Session']
    if (-not $token) {
        $token = Get-CookieValue $Request 'DirenixDemoSession'
    }
    if (-not $token -or -not $script:Sessions.ContainsKey($token)) { return $null }
    $session = $script:Sessions[$token]
    if ((Get-Date).ToUniversalTime() -gt $session.expiresAt) {
        $script:Sessions.Remove($token)
        return $null
    }
    $session.expiresAt = (Get-Date).ToUniversalTime().AddMinutes(30)
    $session.user
}

function Assert-Role {
    param(
        [object]$User,
        [string[]]$AllowedRoles
    )
    if (-not $User) { return $false }
    if ($AllowedRoles -contains $User.role) { return $true }
    $false
}

function Convert-FindingForRole {
    param(
        [object]$Finding,
        [string]$Role
    )
    if ($Role -eq 'ExecutiveViewer') {
        return [pscustomobject]@{
            id = $Finding.id
            title = $Finding.title
            category = $Finding.category
            severity = $Finding.severity
            decision = $Finding.decision
            status = $Finding.status
            businessRiskScore = $Finding.businessRiskScore
            domain = $Finding.domain
            businessUnit = $Finding.businessUnit
            owner = $Finding.owner
            firstSeen = $Finding.firstSeen
            lastSeen = $Finding.lastSeen
            managementSummary = $Finding.managementSummary
            recommendation = $Finding.recommendation
        }
    }
    $Finding
}

function Get-ContentType {
    param([string]$Path)
    switch ([IO.Path]::GetExtension($Path).ToLowerInvariant()) {
        '.html' { 'text/html; charset=utf-8' }
        '.js' { 'application/javascript; charset=utf-8' }
        '.css' { 'text/css; charset=utf-8' }
        '.png' { 'image/png' }
        default { 'application/octet-stream' }
    }
}

function Serve-Static {
    param(
        [Net.HttpListenerResponse]$Response,
        [string]$RelativePath
    )
    if ([string]::IsNullOrWhiteSpace($RelativePath) -or $RelativePath -eq '/') {
        $RelativePath = 'index.html'
    }
    $RelativePath = $RelativePath.TrimStart('/').Replace('/','\')
    $target = [IO.Path]::GetFullPath((Join-Path $DashboardRoot $RelativePath))
    $root = [IO.Path]::GetFullPath($DashboardRoot)
    if (-not $target.StartsWith($root, [StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath $target -PathType Leaf)) {
        Write-JsonResponse $Response ([pscustomobject]@{ error='NotFound' }) 404
        return
    }
    $bytes = [IO.File]::ReadAllBytes($target)
    $Response.StatusCode = 200
    $Response.ContentType = Get-ContentType $target
    $Response.Headers.Add('Cache-Control', 'no-store, no-cache, must-revalidate, max-age=0')
    $Response.Headers.Add('Pragma', 'no-cache')
    $Response.ContentLength64 = $bytes.Length
    $Response.OutputStream.Write($bytes, 0, $bytes.Length)
}

New-DirectoryIfMissing $DataRoot
$script:Store = Load-Store
$script:Sessions = @{}

$listener = New-Object Net.HttpListener
$prefix = 'http://{0}:{1}/' -f $ListenAddress, $Port
$listener.Prefixes.Add($prefix)
$listener.Start()

Write-Host "Direnix portal running at $prefix"
Write-Host "Demo users:"
Write-Host "  admin@local / DemoAdmin!2026"
Write-Host "  operator@local / DemoOperator!2026"
Write-Host "  risk@local / DemoRisk!2026"
Write-Host "  auditor@local / DemoAuditor!2026"
Write-Host "  mfa@local / DemoMfa!2026 / TOTP secret: JBSWY3DPEHPK3PXP"
Write-Host "  executive@local / DemoExec!2026"
Write-Host "Press Ctrl+C to stop."

try {
    while ($listener.IsListening) {
        $context = $listener.GetContext()
        $request = $context.Request
        $response = $context.Response
        try {
            $path = $request.Url.AbsolutePath
            if ($path -eq '/api/login' -and $request.HttpMethod -eq 'POST') {
                $body = Read-JsonBody $request
                $username = ([string]$body.username).Trim().ToLowerInvariant()
                $password = [string]$body.password
                $mfaCode = ''
                if ($body -and $body.PSObject.Properties['mfaCode']) {
                    $mfaCode = [string]$body.mfaCode
                }
                $user = @($script:Store.users | Where-Object { $_.username -eq $username -and -not $_.disabled }) | Select-Object -First 1
                if (-not $user -or -not (Test-Password $password $user.password)) {
                    Add-Audit $username 'Unknown' 'LoginFailed' 'local-session' 'Denied'
                    Write-JsonResponse $response ([pscustomobject]@{ error='InvalidCredentials' }) 401
                }
                elseif ($user.PSObject.Properties['mfa'] -and $user.mfa.enabled -and [string]::IsNullOrWhiteSpace($mfaCode)) {
                    Add-Audit $username $user.role 'LoginMfaRequired' 'local-session' 'Denied'
                    Write-JsonResponse $response ([pscustomobject]@{ error='MfaRequired'; mfaRequired=$true }) 401
                }
                elseif ($user.PSObject.Properties['mfa'] -and $user.mfa.enabled -and -not (Test-DirenixTotpCode -Secret ([string]$user.mfa.secret) -Code $mfaCode)) {
                    Add-Audit $username $user.role 'LoginMfaFailed' 'local-session' 'Denied'
                    Write-JsonResponse $response ([pscustomobject]@{ error='InvalidMfaCode'; mfaRequired=$true }) 401
                }
                else {
                    $token = New-Token
                    $script:Sessions[$token] = [pscustomobject]@{
                        user = [pscustomobject]@{ id=$user.id; username=$user.username; displayName=$user.displayName; role=$user.role }
                        expiresAt = (Get-Date).ToUniversalTime().AddMinutes(30)
                    }
                    $response.Headers.Add('Set-Cookie', "DirenixDemoSession=$token; Path=/; HttpOnly; SameSite=Lax")
                    Add-Audit $user.username $user.role 'Login' 'local-session' 'Success'
                    Write-JsonResponse $response ([pscustomobject]@{ ok=$true; token=$token; user=$script:Sessions[$token].user })
                }
                continue
            }

            if ($path.StartsWith('/api/')) {
                $user = Get-SessionUser $request
                if (-not $user) {
                    Write-JsonResponse $response ([pscustomobject]@{ error='AuthenticationRequired' }) 401
                    continue
                }

                switch ($path) {
                    '/api/logout' {
                        $token = Get-CookieValue $request 'DirenixDemoSession'
                        if ($token) { $script:Sessions.Remove($token) }
                        Add-Audit $user.username $user.role 'Logout' 'local-session' 'Success'
                        Write-JsonResponse $response ([pscustomobject]@{ ok=$true })
                    }
                    '/api/me' {
                        Write-JsonResponse $response ([pscustomobject]@{ user=$user })
                    }
                    '/api/overview' {
                        Write-JsonResponse $response (Get-PortalOverview -User $user)
                    }
                    '/api/findings' {
                        $overview = Get-PortalOverview -User $user
                        $dataset = Get-PortalDataset
                        $latestRun = Get-LatestRun -Runs @(Get-ArrayValue $dataset.store.runs)
                        $findings = @(Get-RunFindings -Dataset $dataset -LatestRun $latestRun)
                        Write-JsonResponse $response (@($findings | ForEach-Object { Convert-FindingForRole $_ $user.role }))
                    }
                    '/api/collection/validate-target' {
                        if (-not (Assert-Role $user @('LocalAdmin','CollectorOperator'))) {
                            Write-JsonResponse $response ([pscustomobject]@{ error='AccessDenied' }) 403
                        }
                        else {
                            $body = Read-JsonBody $request
                            $result = Test-CollectionTarget -Target $body
                            Add-Audit $user.username $user.role 'ValidateCollectionTarget' ([string]$result.host) $result.status
                            Write-JsonResponse $response $result
                        }
                    }
                    '/api/collection/options' {
                        if (-not (Assert-Role $user @('LocalAdmin','CollectorOperator'))) {
                            Write-JsonResponse $response ([pscustomobject]@{ error='AccessDenied' }) 403
                        }
                        else {
                            Write-JsonResponse $response $script:Store.collectionOptions
                        }
                    }
                    '/api/collection/preflight' {
                        if (-not (Assert-Role $user @('LocalAdmin','CollectorOperator'))) {
                            Write-JsonResponse $response ([pscustomobject]@{ error='AccessDenied' }) 403
                        }
                        else {
                            $body = Read-JsonBody $request
                            $targetResult = Test-CollectionTarget -Target (Get-ObjectProperty $body 'target' $null)
                            $selectedScopes = @(Get-ArrayValue (Get-ObjectProperty $body 'selectedScopes' (Get-ObjectProperty $body 'selectedOus' @())))
                            $objectTypes = @(Get-ArrayValue (Get-ObjectProperty $body 'objectTypes' @()))
                            $featurePacks = @(Get-ArrayValue (Get-ObjectProperty $body 'featurePacks' @()))
                            $depth = [string](Get-ObjectProperty $body 'depth' 'Standard')
                            $warnings = New-Object Collections.Generic.List[string]
                            foreach ($warning in @(Get-ArrayValue $targetResult.warnings)) { $warnings.Add([string]$warning) }
                            if ($targetResult.status -eq 'Blocked') {
                                foreach ($errorItem in @(Get-ArrayValue $targetResult.errors)) { $warnings.Add([string]$errorItem) }
                            }
                            else {
                                if ($depth -eq 'Deep') {
                                    $warnings.Add('Avaliacao profunda pode ser mais lenta e exigir leitura de ACLs, SYSVOL, Event Log, WMI/CIM ou ferramentas RSAT.')
                                }
                                if (@($featurePacks | Where-Object { $_ -match 'Replication|DC Health' }).Count -gt 0 -and -not (Get-Command repadmin.exe -ErrorAction SilentlyContinue)) {
                                    $warnings.Add('repadmin.exe nao foi encontrado nesta VM; evidencias profundas de replicacao ficarao como capacidade ausente ate instalar RSAT ou importar evidencia.')
                                }
                                if ($selectedScopes.Count -eq 0) {
                                    $warnings.Add('Nenhum escopo validado foi selecionado. Valide o alvo AD para descobrir o DN do dominio ou informe um DN manual.')
                                }
                            }
                            $status = if ($targetResult.status -eq 'Blocked') { 'Blocked' } elseif ($warnings.Count -gt 0) { 'ReadyWithWarnings' } else { 'Ready' }
                            Add-Audit $user.username $user.role 'AssessmentReadinessCheck' ([string]$targetResult.host) $status
                            Write-JsonResponse $response ([pscustomobject]@{
                                status=$status
                                domain= if ($targetResult.detectedDomain) { $targetResult.detectedDomain } else { '' }
                                target=$targetResult
                                selectedScopes=$selectedScopes
                                objectTypes=$objectTypes
                                featurePacks=$featurePacks
                                depth=$depth
                                storage='DPAPI CurrentUser encrypted local store'
                                listener='127.0.0.1'
                                warnings=@($warnings)
                                selectedCapabilities=@('TCP validation','RootDSE discovery','Read-only assessment planning','Timeline comparator','RBAC')
                            })
                        }
                    }
                    '/api/audit' {
                        if (-not (Assert-Role $user @('LocalAdmin','Auditor'))) {
                            Write-JsonResponse $response ([pscustomobject]@{ error='AccessDenied' }) 403
                        }
                        else {
                            Write-JsonResponse $response $script:Store.audit
                        }
                    }
                    default {
                        if ($path -eq '/api/finding') {
                            $id = $request.QueryString['id']
                            $dataset = Get-PortalDataset
                            $latestRun = Get-LatestRun -Runs @(Get-ArrayValue $dataset.store.runs)
                            $findings = @(Get-RunFindings -Dataset $dataset -LatestRun $latestRun)
                            $finding = @($findings | Where-Object { $_.id -eq $id }) | Select-Object -First 1
                            if (-not $finding) {
                                Write-JsonResponse $response ([pscustomobject]@{ error='NotFound' }) 404
                            }
                            elseif ($user.role -eq 'ExecutiveViewer' -and -not $finding.managementSafe) {
                                Write-JsonResponse $response ([pscustomobject]@{ error='AccessDenied' }) 403
                            }
                            else {
                                if ($user.role -ne 'ExecutiveViewer') {
                                    Add-Audit $user.username $user.role 'ViewFindingDetail' $id 'Success'
                                }
                                Write-JsonResponse $response (Convert-FindingForRole $finding $user.role)
                            }
                        }
                        else {
                            Write-JsonResponse $response ([pscustomobject]@{ error='NotFound' }) 404
                        }
                    }
                }
                continue
            }

            Serve-Static $response $path
        }
        catch {
            Write-JsonResponse $response ([pscustomobject]@{ error='ServerError'; message=$_.Exception.Message }) 500
        }
        finally {
            $response.OutputStream.Close()
        }
    }
}
finally {
    if ($listener.IsListening) { $listener.Stop() }
    $listener.Close()
}

