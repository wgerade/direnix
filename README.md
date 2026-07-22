# Direnix

**Free, open-source daily identity operations for Active Directory.**

Direnix answers, in under 30 seconds every morning: **what changed in my AD since yesterday, what is at risk, and what should I do next** — without ever querying the domain from the UI, without agents on domain controllers, and without your data leaving the machine.

> Point-in-time AD audit tools (PingCastle, Purple Knight) give you a great *photo* once a quarter. Direnix is the *film*: a scheduled read-only collection, a change timeline, daily operational indicators and an Identity Score you watch evolve.

## What you get

- **Today view** — the daily rounds: what changed since yesterday (new privileged members, dangerous flags, created/deleted objects, SPN changes…), passwords expiring, accounts locked out, new risks.
- **Identity Score & Tier 0 score** — posture at a glance, with drill-down into ~40 rules across hygiene, privileged access, hardening and governance.
- **Risk workbench** — each finding ships with evidence, manual remediation steps and PowerShell in two stages (*preview* simulates, *apply* executes — you copy and run them, Direnix never writes to AD).
- **Exceptions with governance** — accept a risk with owner, justification and expiry date; full local audit trail of every action.
- **Operational indicators** — daily counters (passwords expiring/expired, locked accounts, accounts about to expire) plus **custom indicators**: paste your own LDAP filter (or PowerShell `Get-AD*` query — it is translated, never executed) and it runs with every collection.
- **Exportable report** — a self-contained HTML report (score, top risks, indicators, changes) you can e-mail to your boss, plus CSV exports for Excel.
- **Scheduled collection with gMSA** — daily/weekly collection under a group Managed Service Account via Kerberos/LDAPS; no password stored anywhere.
- **Demo mode** — click *"Explore with sample data"* on the login screen and browse a fictional domain before pointing it at anything real.
- Portuguese and English UI, light and dark themes.

## Security & privacy, in one paragraph

Direnix is **read-only against AD** (LDAP/LDAPS searches only, LDAPS by default), runs on any domain-joined Windows machine (**not** on a DC), stores everything in a **local SQLCipher-encrypted database** with the key protected by Windows **DPAPI**, never persists the AD credential you type, and has **zero telemetry and zero egress** — nothing leaves your machine. The full write-up is in [docs/SECURITY_AND_PRIVACY.md](docs/SECURITY_AND_PRIVACY.md). And because it is MIT-licensed open source, you can audit every line of what it does.

## Quickstart (5 minutes)

1. Download `Direnix-x.y.z.msi` from [Releases](../../releases).
2. Install on a domain-joined Windows machine (Windows Server 2019+ or Windows 10/11):
   ```powershell
   msiexec /i Direnix-0.9.0.msi
   ```
   > The MSI is not code-signed yet, so SmartScreen will warn you. Verify the SHA-256 hash published on the release page, then choose *More info → Run anyway*.
3. Open the portal at `http://127.0.0.1:8787/` (or Start Menu → Direnix → Direnix Portal) and create the local administrator.
4. Run the first assessment: point it at a DC over LDAPS with any **read-only domain account** (Domain Admin is *not* required).
5. Tomorrow, after the second collection, the *Today* view starts showing what changed. Enable the daily schedule (ideally with a gMSA) and make it your morning coffee tab.

Just looking? Two zero-commitment ways to try it:
- **Portable** — grab `DirenixPortable.exe`, double-click it. No install, no Windows service, no admin: it runs from your user profile (data in `%LOCALAPPDATA%\Direnix`), opens the portal in your browser and skips the login. Close the window to stop.
- **Demo** — click **"Explore with sample data"** on the login screen to browse a fictional domain. No AD, no admin, no risk.

## Requirements

- Windows x64, domain-joined (workstation or member server — do **not** install on a domain controller).
- Network access to a DC on port 636 (LDAPS, default) or 389 (LDAP).
- A domain account with ordinary read permissions for collections.
- Nothing to install on DCs, no schema changes, no agents.

## Building from source

```powershell
# .NET 8 SDK required
cd product
dotnet build .\Direnix.Product.sln -c Release
dotnet test .\tests\Direnix.Core.Tests\Direnix.Core.Tests.csproj -c Release

# MSI (WiX 5, downloaded automatically via NuGet)
dotnet build .\installer\Direnix.Msi\Direnix.Msi.wixproj -c Release
```

Architecture and design docs live in [`docs/`](docs/) (currently mostly in Portuguese): storage schema, rule catalog, collection scope, UX architecture and the [public launch plan](docs/PUBLIC_LAUNCH_PLAN.md).

## Roadmap

- **v0.9 (current)** — morning digest by e-mail/webhook (Teams/Slack) after each scheduled collection; opt-in update check; portable single-exe mode.
- **Next** — Entra ID (hybrid identity) collection and correlation; trusts/sites/FSMO inventory; approval workflows.

## Contributing & feedback

Issues and PRs are welcome — bug reports from real AD environments are gold. If Direnix found something useful in your domain, a screenshot of your (redacted) report in an issue or a star helps more than you think.

## License

[MIT](LICENSE) © 2026 William Gerade. Built with SQLite/SQLCipher (BSD-style), ASP.NET Core and WiX — see their respective licenses.
