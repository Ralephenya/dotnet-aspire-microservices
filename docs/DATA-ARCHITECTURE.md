# GACS — Data Architecture

**Gated Access Compliance Suite**
Last updated: 2026-05-25
Status: Architecture definition — pre-build

---

## 1. Overview

GACS uses three data stores in Phase 1:

| Store | Technology | Purpose |
|---|---|---|
| Primary database | Azure SQL Server | All application data — tenants, users, visitors, consent records, audit logs |
| Cache | Azure Cache for Redis | QR tokens, risk score cache, rate limit counters, session data, tenant config |
| Document store | Azure Blob Storage (WORM) | Consent PDFs, uploaded visitor photos, audit report exports |

All data stays within **Azure South Africa North (Johannesburg)**. No personal data leaves South Africa. This is a hard requirement under POPIA Section 72.

---

## 2. Multi-Tenant Isolation — Shared Database, Row-Level Security

Every table that contains tenant-specific data has a `TenantId` column. SQL Server Row-Level Security (RLS) enforces that every query — regardless of what the application code does — can only return rows belonging to the authenticated tenant.

**How RLS works in GACS:**

- A security policy is applied at the database level per table
- The policy reads the current `TenantId` from the session context (set by the application on every connection before any query runs)
- Any query that does not set the session context returns zero rows — not an error, just no data
- Even if a junior developer writes a query with no `WHERE TenantId = @TenantId` clause, RLS silently filters it

**What this means for junior developers:**

They do not need to add `TenantId` filters to every stored procedure manually. RLS handles it at the database engine level. They do need to ensure the application sets the session context correctly on every database connection — this is done once in the Dapper connection factory, not repeated in every repository.

**Tenant escalation for System Admin role:**

System Admins need to query across tenants (for platform monitoring, support, billing). A separate elevated database user with RLS bypass is used for System Admin connections only. This connection string is stored in Key Vault and never exposed to application code that runs in tenant context.

---

## 3. Azure SQL Server — Schema Design

### 3.1 Schema Organisation

Tables are grouped into named schemas by domain. Hangfire gets its own schema.

| Schema | Owns |
|---|---|
| `identity` | Tenants, users, roles, API keys |
| `consent` | Consent records, consent purposes, signatures, withdrawals |
| `visitor` | Visitor profiles, visit records, check-in/out logs |
| `compliance` | DSAR requests, retention schedules, incident records, IO registrations |
| `audit` | Immutable audit log — every data change recorded here |
| `hangfire` | Hangfire job storage — managed by Hangfire, not touched by application code |

### 3.2 Standard Columns — Every Table

Every table in every schema includes these columns. No exceptions. The T4 template generates them automatically.

| Column | Type | Purpose |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` (NEWSEQUENTIALID) | Primary key — sequential GUIDs for index performance |
| `TenantId` | `UNIQUEIDENTIFIER` | Multi-tenant isolation — RLS policy applied on this column |
| `CreatedAt` | `DATETIME2` | When the record was created — UTC always |
| `CreatedBy` | `UNIQUEIDENTIFIER` | User ID who created the record |
| `UpdatedAt` | `DATETIME2` | When the record was last updated — UTC always |
| `UpdatedBy` | `UNIQUEIDENTIFIER` | User ID who last updated the record |
| `IsDeleted` | `BIT` | Soft delete flag — 0 = active, 1 = deleted |
| `DeletedAt` | `DATETIME2` (nullable) | When the record was soft deleted — UTC |
| `DeletedBy` | `UNIQUEIDENTIFIER` (nullable) | User ID who soft deleted the record |

### 3.3 No Hard Deletes

The application never issues a `DELETE` statement. Every delete stored procedure sets `IsDeleted = 1`, `DeletedAt = GETUTCDATE()`, and `DeletedBy = @UserId`.

Physical deletion of personal data (required by POPIA retention schedules) is handled exclusively by Hangfire retention jobs — not by application endpoints. This ensures deletion is logged, scheduled, auditable, and never accidental.

### 3.4 Core Tables

#### identity.Tenants

Stores one record per client organisation (estate, school, hospital, office park).

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK |
| `TenantId` | `UNIQUEIDENTIFIER` | Same as Id for tenant root record |
| `Name` | `NVARCHAR(200)` | Organisation name |
| `Slug` | `NVARCHAR(100)` | URL-safe identifier — unique |
| `SecurityTier` | `TINYINT` | 1=Basic, 2=Standard, 3=Enhanced, 4=Maximum |
| `SubscriptionPlan` | `NVARCHAR(50)` | Trial / Standard / Enterprise |
| `SubscriptionStatus` | `NVARCHAR(50)` | Active / Suspended / Cancelled |
| `DefaultLanguage` | `NVARCHAR(10)` | BCP-47 language code — default `en` |
| `EnabledLanguages` | `NVARCHAR(500)` | Comma-separated BCP-47 codes |
| `QrDeliveryMode` | `TINYINT` | 1=PreGenerated, 2=OnArrival, 3=PermanentResident |
| `DefaultRetentionDays` | `INT` | Default visitor record retention in days |
| `IsActive` | `BIT` | Tenant active flag — separate from IsDeleted |
| + standard columns | | CreatedAt, UpdatedAt, IsDeleted etc. |

#### identity.Users

Stores GACS-specific user profile. Authentication is handled by Entra ID — this table stores what Entra ID does not know about.

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK |
| `TenantId` | `UNIQUEIDENTIFIER` | FK → identity.Tenants |
| `EntraObjectId` | `NVARCHAR(100)` | Entra ID object ID — links to Entra user |
| `Email` | `NVARCHAR(320)` | User email |
| `FullName` | `NVARCHAR(200)` | Display name |
| `Role` | `NVARCHAR(50)` | InformationOfficer / PropertyManager / Guard / Auditor / Resident / SystemAdmin |
| `PhoneNumber` | `NVARCHAR(20)` | For WhatsApp and SMS notifications |
| `PreferredLanguage` | `NVARCHAR(10)` | BCP-47 |
| `LastLoginAt` | `DATETIME2` | Last successful login |
| `IsActive` | `BIT` | Active flag |
| + standard columns | | |

#### identity.ApiKeys

One or more API keys per tenant. Used by YARP for client identification and billing.

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK |
| `TenantId` | `UNIQUEIDENTIFIER` | FK → identity.Tenants |
| `KeyHash` | `NVARCHAR(500)` | SHA-256 hash of the key — raw key never stored |
| `KeyPrefix` | `NVARCHAR(10)` | First 8 chars of key — for identification in UI (e.g. `gacs_abc1...`) |
| `Label` | `NVARCHAR(100)` | Human-readable label (e.g. "Production", "Guard App") |
| `LastUsedAt` | `DATETIME2` | Updated on each use via Redis — not on every DB call |
| `ExpiresAt` | `DATETIME2` (nullable) | Optional expiry — null = never expires |
| `IsActive` | `BIT` | Active flag — set to 0 on revocation |
| + standard columns | | |

#### consent.ConsentRecords

The heart of POPIA compliance. One record per visitor per visit per purpose.

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK |
| `TenantId` | `UNIQUEIDENTIFIER` | FK → identity.Tenants |
| `VisitorId` | `UNIQUEIDENTIFIER` | FK → visitor.Visitors |
| `VisitId` | `UNIQUEIDENTIFIER` | FK → visitor.Visits |
| `PurposeCode` | `NVARCHAR(50)` | e.g. `ACCESS_CONTROL`, `CCTV`, `BIOMETRIC` |
| `PurposeDescription` | `NVARCHAR(500)` | Plain language description shown to visitor |
| `LegalBasis` | `NVARCHAR(50)` | Consent / LegitimateInterest / LegalObligation |
| `ConsentGiven` | `BIT` | 1 = consented, 0 = declined |
| `ConsentGivenAt` | `DATETIME2` | Timestamp of consent decision |
| `ConsentMethod` | `NVARCHAR(50)` | Digital / QR / InPerson / WhatsApp |
| `SignatureUrl` | `NVARCHAR(500)` | Blob Storage URL of signature image |
| `PdfUrl` | `NVARCHAR(500)` | Blob Storage URL of immutable consent PDF |
| `IpAddress` | `NVARCHAR(45)` | IPv4 or IPv6 of device at time of consent |
| `DeviceFingerprint` | `NVARCHAR(200)` | Browser/device identifier |
| `ExpiresAt` | `DATETIME2` | Consent expiry — default 12 months from consent date |
| `WithdrawnAt` | `DATETIME2` (nullable) | If consent was withdrawn |
| `WithdrawnBy` | `NVARCHAR(200)` (nullable) | Who withdrew (visitor self, guard, IO) |
| `Language` | `NVARCHAR(10)` | Language consent was presented in |
| + standard columns | | |

#### visitor.Visitors

Visitor profile — one record per unique person across all visits.

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK |
| `TenantId` | `UNIQUEIDENTIFIER` | FK → identity.Tenants |
| `FullName` | `NVARCHAR(200)` | |
| `PhoneNumber` | `NVARCHAR(20)` | Primary contact — used for WhatsApp |
| `Email` | `NVARCHAR(320)` | (nullable) — not always provided |
| `PhotoUrl` | `NVARCHAR(500)` | Blob Storage URL of visitor photo |
| `IsMinor` | `BIT` | Triggers guardian consent flow |
| `GuardianVisitorId` | `UNIQUEIDENTIFIER` (nullable) | FK → visitor.Visitors (guardian record) |
| `RiskScoreLatest` | `TINYINT` | Last AI risk score 0–100 — cached here for guard app speed |
| `VisitCount` | `INT` | Total visits to this tenant — used in risk scoring |
| `FirstVisitAt` | `DATETIME2` | |
| `LastVisitAt` | `DATETIME2` | |
| + standard columns | | |

#### visitor.Visits

One record per visit (each arrival at a gate).

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK |
| `TenantId` | `UNIQUEIDENTIFIER` | FK → identity.Tenants |
| `VisitorId` | `UNIQUEIDENTIFIER` | FK → visitor.Visitors |
| `HostUserId` | `UNIQUEIDENTIFIER` | FK → identity.Users (resident/host who invited) |
| `AccessPointId` | `UNIQUEIDENTIFIER` | FK → identity.AccessPoints (which gate) |
| `PurposeOfVisit` | `NVARCHAR(200)` | |
| `VisitorTypeCode` | `NVARCHAR(50)` | RoutineVisitor / Contractor / Delivery / Emergency / VIP |
| `CheckInAt` | `DATETIME2` | |
| `CheckOutAt` | `DATETIME2` (nullable) | Null until checked out |
| `CheckInMethod` | `NVARCHAR(50)` | QR / FaceScan / Manual / LPR |
| `QrToken` | `NVARCHAR(100)` | The QR token used for this visit |
| `GuardUserId` | `UNIQUEIDENTIFIER` | Guard who processed check-in |
| `RiskScore` | `TINYINT` | AI risk score at time of check-in |
| `RiskLevel` | `NVARCHAR(10)` | Green / Yellow / Red |
| `WasEscalated` | `BIT` | Guard escalated to supervisor |
| `RetentionExpiresAt` | `DATETIME2` | When this visit record should be deleted |
| `Status` | `NVARCHAR(20)` | Active / CheckedOut / Expired / Deleted |
| + standard columns | | |

#### audit.AuditLog

Immutable. No update or delete stored procedures for this table. Ever.

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK |
| `TenantId` | `UNIQUEIDENTIFIER` | |
| `EntityType` | `NVARCHAR(100)` | e.g. `Visitor`, `ConsentRecord`, `ApiKey` |
| `EntityId` | `UNIQUEIDENTIFIER` | ID of the affected record |
| `Action` | `NVARCHAR(50)` | Created / Updated / SoftDeleted / Restored / ConsentWithdrawn etc. |
| `PerformedByUserId` | `UNIQUEIDENTIFIER` | |
| `PerformedByRole` | `NVARCHAR(50)` | Role at time of action |
| `OldValues` | `NVARCHAR(MAX)` | JSON snapshot of record before change |
| `NewValues` | `NVARCHAR(MAX)` | JSON snapshot of record after change |
| `IpAddress` | `NVARCHAR(45)` | |
| `Timestamp` | `DATETIME2` | UTC — no UpdatedAt on this table |
| `CorrelationId` | `UNIQUEIDENTIFIER` | Links all DB changes from one HTTP request |

---

## 4. Stored Procedure Conventions

All stored procedures follow a consistent naming and parameter convention. The T4 template enforces this.

**Naming:** `usp_[Schema]_[Entity]_[Action]`

Examples:
```
usp_Identity_Tenant_SelectById
usp_Identity_Tenant_SelectAll
usp_Identity_Tenant_Insert
usp_Identity_Tenant_Update
usp_Identity_Tenant_Delete
usp_Visitor_Visit_SelectById
usp_Consent_ConsentRecord_Insert
usp_Audit_AuditLog_Insert
```

**SelectAll always supports pagination:**

Every `SelectAll` procedure accepts `@PageNumber INT` and `@PageSize INT` parameters and returns `TotalCount` alongside the result set. No stored procedure returns an unbounded result set.

**Output parameters on Insert:**

Every `Insert` procedure returns the new record's `Id` as an output parameter. The application does not generate GUIDs — the database generates them via `NEWSEQUENTIALID()`.

**Audit log written inside stored procedures:**

Every `Insert`, `Update`, and `Delete` procedure writes a corresponding row to `audit.AuditLog` within the same transaction. The audit record cannot be skipped by the application layer.

---

## 5. Azure Cache for Redis — Topology

One Redis instance. Logical databases separate concern areas.

| Redis DB | Purpose | TTL Strategy |
|---|---|---|
| DB 0 | API key → tenant mapping | 1 hour — invalidated on key rotation/revocation |
| DB 1 | QR token store | 120 seconds — hard expiry, no renewal |
| DB 2 | Rate limit counters | Rolling 1-minute window |
| DB 3 | Tenant configuration cache | 15 minutes — invalidated on config update |
| DB 4 | Visitor risk score cache | 30 minutes — invalidated on visitor data change |
| DB 5 | Usage metering counters | Flushed every 5 minutes by Hangfire job |
| DB 6 | Session / general app cache | Varies per entry |

**Cache-aside pattern — always:**

1. Application checks Redis
2. On hit — return cached value
3. On miss — read from SQL Server, write to Redis, return value
4. Redis is always expendable — if Redis is unavailable, fall through to SQL Server

No write-through or write-behind caching in Phase 1. Cache-aside only.

**Key naming convention:**

```
{tenantId}:{entity}:{identifier}

Examples:
abc123:apikey:gacs_abc1xxxx
abc123:qr:visit-xyz-789
abc123:config:tenant
abc123:risk:visitor-def456
platform:ratelimit:abc123:2026-05-25T14:03
```

Tenant-prefixed keys ensure no cross-tenant cache collisions even within a shared Redis instance.

---

## 6. Azure Blob Storage — Structure

**Account:** One storage account for Phase 1.
**Redundancy:** ZRS (Zone-Redundant Storage) within Johannesburg.
**Access:** Private — no public access. All access via SAS tokens generated by the application with minimum required permissions and short expiry.

### Container Structure

```
gacs-consent-pdfs/          ← WORM enabled — immutable
    {tenantId}/
        {visitorId}/
            {visitId}-consent-{timestamp}.pdf

gacs-visitor-photos/        ← Standard — deleted when visitor record deleted
    {tenantId}/
        {visitorId}/
            photo-{timestamp}.jpg

gacs-audit-exports/         ← Standard — deleted after download confirmed
    {tenantId}/
        {exportId}/
            audit-report-{date}.pdf
```

### WORM Policy — Consent PDFs

The `gacs-consent-pdfs` container has an **immutability policy** enabled:

- **Time-based retention:** 7 years minimum (POPIA legal defence requirement)
- **Lock state:** Locked — cannot be modified or deleted by anyone, including storage account administrators, until the retention period expires
- The application can write to this container but never modify or delete existing blobs
- Even if a visitor withdraws consent, the PDF record of the original consent remains — withdrawal is recorded as a new document, not a deletion of the original

### SAS Token Rules

- Read SAS: 15 minutes expiry — for displaying consent PDFs in the IO portal
- Write SAS: 5 minutes expiry — for uploading visitor photos and consent PDFs
- No permanent access keys in application code — all SAS tokens generated at runtime via Key Vault-stored account credentials

---

## 7. Hangfire — Background Jobs

**Schema:** `hangfire.*` inside the main SQL Server database.
**Dashboard:** Enabled in development and staging only. Disabled in production UI — jobs monitored via Azure Monitor alerts.

### Jobs in Phase 1

| Job | Schedule | Purpose |
|---|---|---|
| `RetentionExpiryJob` | Daily at 02:00 SAST | Finds visit records where `RetentionExpiresAt` has passed → soft deletes visitor data → logs to audit |
| `QrTokenCleanupJob` | Every 5 minutes | Removes expired QR tokens from Redis DB 1 |
| `UsageMeteringFlushJob` | Every 5 minutes | Reads Redis DB 5 usage counters → writes to billing table in SQL → resets counters |
| `ConsentExpiryReminderJob` | Daily at 06:00 SAST | Finds consent records expiring within 30 days → queues re-consent notification via Notification Service |
| `StaleVisitCleanupJob` | Every hour | Finds visits with no check-out after 12 hours → auto-closes with `Status = Expired` |
| `AuditLogArchiveJob` | Weekly Sunday 03:00 SAST | Exports audit logs older than 90 days to Blob Storage → marks as archived in DB |

### Hangfire Rules

- Every job is idempotent — running it twice produces the same result as running it once
- Every job writes a completion record to `audit.AuditLog` on success
- Failed jobs retry 3 times with exponential backoff — then move to dead-letter queue
- No job deletes personal data without first verifying the retention period has genuinely expired
- No job runs longer than 10 minutes — long operations are batched

---

## 8. Data Residency

All data stores are provisioned in **Azure South Africa North (Johannesburg)**.

| Store | Region | Redundancy |
|---|---|---|
| Azure SQL Server | South Africa North | Zone-redundant (ZRS) |
| Azure Cache for Redis | South Africa North | Zone-redundant |
| Azure Blob Storage | South Africa North | Zone-redundant (ZRS) |

**No geo-replication to Cape Town in Phase 1.** The Azure South Africa West (Cape Town) region has limited availability zone support. Phase 1 uses zone-redundancy within Johannesburg for resilience. Geo-replication is a Phase 2 decision once the Cape Town region's AZ support is confirmed.

**No data crosses the South African border.** Azure OpenAI and Azure AI services will be evaluated for South Africa region availability before Phase 2 AI pipeline work begins. If not available in-region, a data processing agreement and explicit cross-border transfer consent mechanism must be implemented before those services are used.

---

## 9. Data Access Rules — Non-Negotiable

| Rule | Detail |
|---|---|
| RLS always active | Every tenant-scoped query filtered by RLS — no exceptions |
| Session context always set | Dapper connection factory sets `TenantId` context before every query |
| No raw SQL in application code | All queries go through named stored procedures |
| No unbounded queries | Every `SelectAll` proc requires `@PageNumber` and `@PageSize` |
| No hard deletes from application | Only Hangfire retention jobs perform physical deletion — and only after verifying retention period |
| Audit log always written | Stored procedures write to `audit.AuditLog` within the same transaction |
| Blob URLs never public | All blob access via short-lived SAS tokens — generated at runtime |
| UTC everywhere | All `DATETIME2` columns store UTC. Conversion to SAST (UTC+2) happens in the application layer, never in SQL |
| GUID primary keys | `UNIQUEIDENTIFIER` with `NEWSEQUENTIALID()` — sequential for index performance, globally unique for multi-tenant safety |

---

## 10. Pre-Build Checklist — Data Layer

- [ ] Azure SQL Server provisioned in South Africa North with zone redundancy
- [ ] All six schemas created (`identity`, `consent`, `visitor`, `compliance`, `audit`, `hangfire`)
- [ ] Row-Level Security policy created and tested — verify cross-tenant query returns zero rows
- [ ] T4 template tested — generate stored procs for one entity, verify all five procs created correctly
- [ ] Hangfire schema initialised and one test job running
- [ ] Azure Cache for Redis provisioned — six logical databases configured and connection verified
- [ ] Azure Blob Storage account provisioned — three containers created, WORM policy applied to `gacs-consent-pdfs` and locked
- [ ] SAS token generation tested — write a test blob, retrieve via SAS, confirm expiry enforced
- [ ] Key Vault provisioned — all connection strings, storage account keys, and Redis keys stored there. Zero secrets in `appsettings.json`
- [ ] Dapper connection factory verified — sets session context on every connection before first query
- [ ] Audit log verified — run an insert, confirm audit row written in same transaction
- [ ] UTC verified — insert a record, confirm `CreatedAt` stored as UTC, displayed as SAST in UI

---

*Next architecture document: HANGFIRE-ARCHITECTURE.md — job definitions, retry policies, monitoring, and scheduling rules.*
