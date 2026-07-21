# Direnix — Security & Privacy

This page explains exactly what Direnix touches, what it stores, and what it sends. The short version: **read-only against AD, everything local and encrypted, zero egress**. Because Direnix is open source (MIT), every claim below can be verified in the code.

## What Direnix does to your Active Directory

- **Read-only, always.** Collections are LDAP searches. Direnix contains no code path that writes to Active Directory. Remediation is delivered as PowerShell snippets *you* review, copy and run — with a `preview` (simulation) variant offered before every `apply`.
- **LDAPS by default** (port 636). Plain LDAP (389) is available but must be chosen explicitly.
- **No agents, no DC footprint.** Direnix installs on a domain-joined workstation or member server. Nothing is installed on domain controllers, no schema changes, no GPO changes.
- **Custom indicators cannot execute code.** You may paste a PowerShell `Get-AD*` query for convenience, but Direnix only *extracts/translates the LDAP filter* from it — PowerShell is never executed against your environment.

## Credentials

- The AD credential used for an interactive collection is **request-scoped**: used for that LDAP bind and discarded. It is never written to the database, config files or logs.
- Scheduled collections run under the **service identity** — recommended: a **gMSA** (group Managed Service Account), so Windows manages the password and Direnix stores only the account name.
- The collection account needs **ordinary domain read permissions**. Domain Admin is not required and not recommended.
- The portal login is a **local application user** (bcrypt-style password hash in the local database), unrelated to your AD credentials.

## What is stored, and where

| Data | Where | Protection |
| --- | --- | --- |
| Collected AD objects, findings, timeline | `%ProgramData%\Direnix\Product\data\direnix.adcx` | SQLCipher (encrypted SQLite) |
| Database encryption key | Windows DPAPI (LocalMachine) + ACL | Never stored in plain text |
| Local users & sessions | Same encrypted database | Password hashes only |
| Audit trail of portal actions | Same encrypted database | Append-only |
| Exported reports/CSVs | Wherever you save them | Your responsibility — they contain AD data |

The portal binds to **127.0.0.1:8787** by default — it is not reachable from the network unless you deliberately change the bind address.

## What leaves the machine

**Nothing.**

- No telemetry, no usage analytics, no crash reporting, no "phone home".
- No cloud component. The only network traffic Direnix generates is the LDAP/LDAPS connection to the domain controller you configure.
- Demo mode ("Explore with sample data") is a local fixture: it makes **no** network, AD or database access at all.

## Known limitations / honest notes

- The MSI is currently **not code-signed** — SmartScreen will warn on install. Verify the SHA-256 hash published with each release. Code signing is on the roadmap.
- Exported HTML/CSV reports are *not* encrypted — treat them as sensitive documents.
- Anyone with local admin on the machine where Direnix runs can, by design of DPAPI LocalMachine, access the database key. Install it on a machine you trust at the same level as the data it holds.

Questions or a security finding to report? Open an issue — or if it is sensitive, contact the maintainer directly.
