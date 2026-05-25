# GACS — Master Architecture

**Gated Access Compliance Suite**
Last updated: 2026-05-25
Status: Architecture definition — pre-build

---

## 1. Read This First

This is the document every junior developer reads before writing a single line of code.

It ties together all architecture decisions made across the five layer documents. It explains what we are building, in what order, and why every technology choice was made. When you are unsure about anything — a pattern, a convention, a naming decision — the answer is in one of these documents.

### The Five Layer Documents

| Document | What It Covers |
|---|---|
| [FRONTEND-ARCHITECTURE.md](FRONTEND-ARCHITECTURE.md) | Blazor, MAUI, Fluent UI, design system, layout shells, custom components |
| [BACKEND-ARCHITECTURE.md](BACKEND-ARCHITECTURE.md) | YARP, Wolverine, ErrorOr, Dapper, Scalar, API versioning, project structure |
| [DATA-ARCHITECTURE.md](DATA-ARCHITECTURE.md) | Azure SQL, Redis, Blob Storage, RLS, soft deletes, audit log, Hangfire jobs |
| [MASTER-ARCHITECTURE.md](MASTER-ARCHITECTURE.md) | This document — full system picture, Aspire, build order, environments |

Read them in that order. Do not start building until you have read all of them.

---

## 2. What GACS Is

GACS (Gated Access Compliance Suite) is a multi-tenant SaaS platform built entirely on the Microsoft stack. It helps South African estates, schools, hospitals, and office parks comply with the POPIA Draft Code of Conduct for gated access points (published April 2026).

**The problem it solves:** Most South African gated access points are currently non-compliant — open visitor books, unconsented fingerprint readers, CCTV without proper disclosure. Fines reach R10 million. GACS replaces paper-based, non-compliant access control with a digital, auditable, legally defensible system.

**The platform:** Listed on Microsoft AppSource. Built on Azure. Runs in South Africa North (Johannesburg) to meet POPIA data residency requirements.

---

## 3. The Full System — All Layers Connected

```
╔══════════════════════════════════════════════════════════════════════════╗
║                         CLIENT LAYER                                      ║
║                                                                            ║
║  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────────────────┐  ║
║  │  Admin / IO /   │  │  Visitor Self-  │  │  Guard Check-In App      │  ║
║  │  Compliance     │  │  Registration   │  │  MAUI Blazor Hybrid       │  ║
║  │  Portal         │  │  Blazor WASM    │  │  (Surface / Android)      │  ║
║  │  Blazor Server  │  │  (Static)       │  │                           │  ║
║  └────────┬────────┘  └────────┬────────┘  └────────────┬─────────────┘  ║
║           │                   │                          │                 ║
║  ┌────────┴───────────────────┴──────────────────────────┴─────────────┐  ║
║  │              Resident / Host Portal — Blazor WASM (Static)          │  ║
║  └──────────────────────────────────────────────────────────────────────┘  ║
╚══════════════════════════════════════════════════════════════════════════╝
                                    │
                                    ▼
╔══════════════════════════════════════════════════════════════════════════╗
║                         EDGE LAYER                                        ║
║                                                                            ║
║              Azure Front Door                                              ║
║              WAF · DDoS Protection · SSL Termination · CDN Edge           ║
╚══════════════════════════════════════════════════════════════════════════╝
                                    │
                                    ▼
╔══════════════════════════════════════════════════════════════════════════╗
║                         GATEWAY LAYER                                     ║
║                                                                            ║
║              YARP — .NET 8 (Azure Container App)                          ║
║                                                                            ║
║              API Key Validation → Tenant Resolution                        ║
║              JWT Validation (Entra ID)                                     ║
║              Request Routing → Load Balancing                              ║
║              Rate Limiting (Redis-backed)                                  ║
║              Usage Metering (Redis → Hangfire flush)                       ║
║              Response Caching (Redis)                                      ║
╚══════════════════════════════════════════════════════════════════════════╝
                                    │
                                    ▼
╔══════════════════════════════════════════════════════════════════════════╗
║                         BACKEND LAYER                                     ║
║                                                                            ║
║   ┌──────────────────────────────────────────────────────────────────┐   ║
║   │  Identity & Tenant Service — .NET 8 Web API (Phase 1 — first)   │   ║
║   │                                                                   │   ║
║   │  Wolverine (CQRS)  ·  ErrorOr (results)  ·  Dapper (data)       │   ║
║   │  Scalar (API docs)  ·  Asp.Versioning (/api/v1/)                 │   ║
║   │  IExceptionHandler + ProblemDetails (global errors)              │   ║
║   └──────────────────────────────────────────────────────────────────┘   ║
║                                                                            ║
║   ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────┐  ║
║   │ Visitor &        │  │ Notification     │  │ Compliance Service   │  ║
║   │ Consent Service  │  │ Service          │  │ (Phase 2+)           │  ║
║   │ (Phase 2)        │  │ (Phase 2)        │  │                      │  ║
║   └──────────────────┘  └──────────────────┘  └──────────────────────┘  ║
╚══════════════════════════════════════════════════════════════════════════╝
                                    │
                    ┌───────────────┼───────────────┐
                    ▼               ▼               ▼
╔══════════════════════════════════════════════════════════════════════════╗
║                         DATA LAYER                                        ║
║                                                                            ║
║  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────────────────┐  ║
║  │  Azure SQL      │  │  Azure Cache    │  │  Azure Blob Storage      │  ║
║  │  Server         │  │  for Redis      │  │                          │  ║
║  │                 │  │                 │  │  gacs-consent-pdfs       │  ║
║  │  identity.*     │  │  DB 0 API keys  │  │  (WORM — 7yr lock)       │  ║
║  │  consent.*      │  │  DB 1 QR tokens │  │                          │  ║
║  │  visitor.*      │  │  DB 2 rate lmt  │  │  gacs-visitor-photos     │  ║
║  │  compliance.*   │  │  DB 3 tenant    │  │                          │  ║
║  │  audit.*        │  │  DB 4 risk score│  │  gacs-audit-exports      │  ║
║  │  hangfire.*     │  │  DB 5 metering  │  │                          │  ║
║  │                 │  │  DB 6 general   │  │                          │  ║
║  └─────────────────┘  └─────────────────┘  └──────────────────────────┘  ║
╚══════════════════════════════════════════════════════════════════════════╝
                                    │
╔══════════════════════════════════════════════════════════════════════════╗
║                         BACKGROUND JOBS                                   ║
║                                                                            ║
║              Hangfire (hangfire.* schema in SQL Server)                   ║
║                                                                            ║
║  RetentionExpiryJob     · QrTokenCleanupJob   · UsageMeteringFlushJob    ║
║  ConsentExpiryReminder  · StaleVisitCleanup   · AuditLogArchiveJob       ║
╚══════════════════════════════════════════════════════════════════════════╝
                                    │
╔══════════════════════════════════════════════════════════════════════════╗
║                         PLATFORM LAYER                                    ║
║                                                                            ║
║  Microsoft Entra ID          Azure Key Vault                              ║
║  (SSO, MFA, RBAC)            (all secrets, connection strings, keys)      ║
║                                                                            ║
║  Azure Monitor + App Insights                                             ║
║  (traces, logs, metrics, alerts)                                          ║
║                                                                            ║
║  Azure Communication Services                                             ║
║  (WhatsApp Business API, SMS, Email)                                      ║
╚══════════════════════════════════════════════════════════════════════════╝
```

---

## 4. .NET Aspire — Local Development Orchestration

**.NET Aspire** is the thread that connects every layer during development. It is not a deployment tool — it is a local orchestration and observability framework that makes every service, database, and cache available with a single `F5` in Visual Studio.

### What Aspire Does for GACS

Without Aspire, a junior developer starting work needs to:
- Manually start SQL Server
- Manually start Redis
- Manually start YARP
- Manually start the API
- Manually start the Blazor frontend
- Hope all the connection strings match

With Aspire, they press `F5` on the AppHost project and everything starts in the correct order, with the correct configuration, wired together automatically.

### Aspire Projects

Two Aspire-specific projects are added to the solution:

```
GACS.AppHost/           ← Aspire orchestrator — defines what runs and how it connects
GACS.ServiceDefaults/   ← Shared Aspire defaults — telemetry, health checks,
                           service discovery, applied to every service project
```

### What the AppHost Orchestrates

| Resource | Aspire Integration |
|---|---|
| SQL Server | `AddSqlServer()` — starts a SQL Server container locally |
| Redis | `AddRedis()` — starts a Redis container locally |
| Azure Blob Storage | `AddAzureStorage()` — uses Azurite emulator locally |
| YARP Gateway | `AddProject<GACS.Gateway>()` — starts YARP as a .NET project |
| Identity & Tenant API | `AddProject<GACS.IdentityTenant.Api>()` |
| Admin Portal (Blazor Server) | `AddProject<GACS.Web.Admin>()` |
| Visitor Registration (Blazor WASM) | `AddProject<GACS.Web.Visitor>()` |
| Hangfire | Runs inside the API project — no separate orchestration needed |

### Service Discovery

Aspire injects service URLs as environment variables at startup. Projects reference each other by name — not by hardcoded `localhost` ports. YARP finds the backend API via `http://gacs-identitytenant-api` — Aspire resolves this to the correct port automatically.

Junior developers never configure ports manually. Never edit connection strings locally. `F5` and everything is connected.

### Aspire Dashboard

When the AppHost runs, a local dashboard opens automatically at `http://localhost:15888`. It shows:

- All running services and their health status
- Distributed traces — follow a single HTTP request through YARP → API → SQL Server
- Structured logs from every service in one place
- Metrics — CPU, memory, request rates per service
- Resource graph — see which service is talking to which

This replaces the need for any local monitoring setup. Junior devs can see exactly what their code is doing across the full stack.

### From Local to Production

Aspire uses the same resource definitions for local development and production. In production, Aspire publishes to **Azure Container Apps** — the SQL Server resource maps to Azure SQL, Redis maps to Azure Cache for Redis, Blob Storage maps to the real Azure storage account. No rewriting of configuration — Aspire handles the promotion.

---

## 5. Environments

Three environments. Each is isolated — no shared data, no shared secrets.

| Environment | Purpose | Hosted On | Aspire |
|---|---|---|---|
| **Local** | Developer machine — daily development | Developer laptop / desktop | Aspire AppHost + containers |
| **Staging** | Integration testing, UAT, demo | Azure Container Apps | Aspire publish target |
| **Production** | Live clients | Azure Container Apps | Aspire publish target |

### Environment Rules

- Secrets never in `appsettings.json` — ever. Local uses Aspire-injected connection strings. Staging and production use Azure Key Vault references.
- Scalar API docs enabled in Local and Staging. Disabled in Production.
- Hangfire dashboard enabled in Local and Staging. Disabled in Production.
- Production has no developer-accessible database console — all data access is through the application or through audited DBA access.
- Each environment has its own Entra ID app registration — local tokens cannot be used in staging or production.

---

## 6. Solution Structure — Complete

```
GACS.sln
│
├── aspire/
│   ├── GACS.AppHost/                        ← Aspire orchestrator
│   └── GACS.ServiceDefaults/                ← Shared Aspire defaults (telemetry, health checks)
│
├── src/
│   │
│   ├── GACS.Gateway/                        ← YARP standalone — full gateway
│   │   ├── Middleware/
│   │   │   ├── ApiKeyValidationMiddleware
│   │   │   ├── TenantResolutionMiddleware
│   │   │   ├── RateLimitingMiddleware
│   │   │   └── UsageMeteringMiddleware
│   │   └── Program.cs
│   │
│   ├── GACS.IdentityTenant.Api/             ← Phase 1 — first microservice
│   │   ├── Controllers/
│   │   │   └── v1/
│   │   │       ├── TenantsController
│   │   │       ├── UsersController
│   │   │       └── ApiKeysController
│   │   └── Program.cs
│   │
│   ├── GACS.IdentityTenant.Application/     ← Commands, queries, handlers (Wolverine)
│   │   ├── Tenants/
│   │   │   ├── Commands/
│   │   │   └── Queries/
│   │   ├── Users/
│   │   │   ├── Commands/
│   │   │   └── Queries/
│   │   └── ApiKeys/
│   │       ├── Commands/
│   │       └── Queries/
│   │
│   ├── GACS.IdentityTenant.Domain/          ← Entities, domain errors
│   │   ├── Entities/
│   │   └── Errors/
│   │
│   ├── GACS.IdentityTenant.Infrastructure/  ← Dapper repos, Redis, Entra ID, Hangfire
│   │   ├── Repositories/
│   │   ├── Cache/
│   │   ├── Identity/
│   │   └── Jobs/                            ← Hangfire job definitions
│   │       ├── RetentionExpiryJob
│   │       ├── QrTokenCleanupJob
│   │       ├── UsageMeteringFlushJob
│   │       ├── ConsentExpiryReminderJob
│   │       ├── StaleVisitCleanupJob
│   │       └── AuditLogArchiveJob
│   │
│   ├── GACS.Shared/                         ← Shared across ALL services — never duplicated
│   │   ├── Responses/
│   │   │   ├── ApiResponse.cs
│   │   │   └── PagedResult.cs
│   │   ├── Pagination/
│   │   │   └── PaginationParameters.cs
│   │   └── Errors/
│   │       └── GlobalExceptionHandlers.cs
│   │
│   ├── GACS.Web.Admin/                      ← Blazor Server — admin, IO, compliance portal
│   ├── GACS.Web.Visitor/                    ← Blazor WASM — visitor registration
│   ├── GACS.Web.Resident/                   ← Blazor WASM — resident and host portal
│   ├── GACS.Mobile.Guard/                   ← MAUI Blazor Hybrid — guard check-in app
│   └── GACS.Components/                     ← Shared Razor Class Library
│                                               All custom components, tokens, layout shells
│
├── database/
│   └── GACS.Database/                       ← SQL Server Database Project
│       ├── Schemas/
│       │   ├── identity/
│       │   ├── consent/
│       │   ├── visitor/
│       │   ├── compliance/
│       │   └── audit/
│       ├── StoredProcedures/
│       │   ├── Identity/
│       │   ├── Consent/
│       │   ├── Visitor/
│       │   └── Audit/
│       └── Templates/
│           └── CrudGenerator.tt             ← T4 template — generates 5 procs per entity
│
└── docs/
    ├── FRONTEND-ARCHITECTURE.md
    ├── BACKEND-ARCHITECTURE.md
    ├── DATA-ARCHITECTURE.md
    └── MASTER-ARCHITECTURE.md               ← This document
```

---

## 7. Hangfire — Background Jobs

Hangfire runs inside the `GACS.IdentityTenant.Infrastructure` project. Its schema (`hangfire.*`) lives in the main SQL Server database. No separate process, no separate database.

### Job Definitions

| Job | Trigger | What It Does |
|---|---|---|
| `RetentionExpiryJob` | Daily 02:00 SAST | Finds visits where `RetentionExpiresAt` has passed. Soft-deletes visitor personal data fields. Writes deletion record to audit log. |
| `QrTokenCleanupJob` | Every 5 minutes | Removes expired QR tokens from Redis DB 1. Defensive — Redis TTL handles most expiry, this cleans edge cases. |
| `UsageMeteringFlushJob` | Every 5 minutes | Reads per-tenant call counters from Redis DB 5. Writes to billing table in SQL. Resets Redis counters. |
| `ConsentExpiryReminderJob` | Daily 06:00 SAST | Finds consent records expiring within 30 days. Queues re-consent notification. Does not send directly — queues for the Notification Service (Phase 2). In Phase 1, writes to a notification queue table. |
| `StaleVisitCleanupJob` | Every hour | Finds active visits with no check-out after 12 hours. Sets `Status = Expired`. Writes to audit log. |
| `AuditLogArchiveJob` | Weekly Sunday 03:00 SAST | Exports audit log entries older than 90 days to Blob Storage. Marks them as archived in SQL. Archived entries remain queryable but offloaded from the hot table. |

### Hangfire Rules

- Every job is **idempotent** — running it twice produces the same result as running it once
- Every job writes a completion entry to `audit.AuditLog` on success
- Failed jobs retry **3 times** with exponential backoff — then move to dead-letter queue
- No job runs longer than **10 minutes** — long operations process in batches
- No job deletes personal data without first re-verifying the retention period in the same transaction
- Hangfire dashboard available at `/hangfire` in Local and Staging only

---

## 8. Technology Stack — Complete Reference

### Frontend

| Technology | Version | Purpose |
|---|---|---|
| Blazor Server | .NET 8 | Admin portal, IO portal, compliance dashboard |
| Blazor WASM | .NET 8 | Visitor registration, resident/host portal |
| MAUI Blazor Hybrid | .NET 8 | Guard check-in app (Android + Windows) |
| Microsoft Fluent UI Blazor | Latest stable | Component library — all surfaces |
| Fluent System Icons | Included in Fluent UI | Iconography — no other icon sets |
| Segoe UI Variable | System font | Typography — zero download time on Windows/Surface |
| SignalR | .NET 8 built-in | Real-time gate alerts, dashboard streaming |

### Backend

| Technology | Version | Purpose |
|---|---|---|
| .NET 8 | 8.x LTS | All backend services |
| YARP | Latest stable | Gateway — routing, auth, rate limiting, caching |
| Wolverine (WolverineFx) | Latest stable | CQRS — commands, queries, handlers |
| ErrorOr | 2.1.1 | Result type — no try/catch in business logic |
| Dapper | Latest stable | Data access via stored procedures |
| Asp.Versioning.Mvc | 8.1.0 | API versioning — URL segment strategy |
| Scalar.AspNetCore | 2.14.14 | API documentation — replaces Swagger UI |
| Microsoft.AspNetCore.OpenApi | .NET 8 built-in | OpenAPI document generation |
| Microsoft.Identity.Web | Latest stable | Entra ID token validation |
| Hangfire | Latest stable | Background job scheduling and execution |
| StackExchange.Redis | Latest stable | Redis client |

### Data

| Technology | Purpose |
|---|---|
| Azure SQL Server | Primary relational database — all application data |
| Azure Cache for Redis | Caching, QR tokens, rate limiting, metering |
| Azure Blob Storage | Consent PDFs (WORM), visitor photos, audit exports |
| SQL Server Database Project | Schema and stored procedure source control |
| T4 Templates | CRUD stored procedure generation from entity classes |

### Platform

| Technology | Purpose |
|---|---|
| .NET Aspire | Local dev orchestration, service discovery, observability dashboard |
| Microsoft Entra ID | SSO, MFA, RBAC — authentication and authorisation |
| Azure Key Vault | All secrets — connection strings, API keys, storage keys |
| Azure Front Door | WAF, DDoS, SSL, CDN edge |
| Azure Container Apps | Production hosting for all services |
| Azure Monitor + App Insights | Telemetry, traces, logs, metrics, alerts |
| Azure Communication Services | WhatsApp Business API, SMS, email |

---

## 9. The Golden Rules

These rules apply to every project, every service, every developer. They are not suggestions.

| Rule | Detail |
|---|---|
| No try/catch in business logic | ErrorOr for domain errors. IExceptionHandler for unexpected exceptions. |
| No inline SQL | All database calls go through named stored procedures via Dapper. |
| No hard deletes from application code | Only Hangfire retention jobs perform physical deletion. |
| No custom response shapes | Every endpoint returns `ApiResponse<T>` or `ApiResponse<PagedResult<T>>` from `GACS.Shared`. |
| No Swagger | Scalar only. Disabled in production. |
| No secrets in appsettings.json | All secrets in Azure Key Vault. Aspire injects locally. |
| No hardcoded URLs | Service discovery via Aspire. No `localhost:5000` in any code. |
| No public blob URLs | All blob access via short-lived SAS tokens generated at runtime. |
| No IMemoryCache | Redis only — so caching works when services scale to multiple instances. |
| URL versioning only | All routes include `/api/v{version}/`. No header or query-string versioning as primary. |
| UTC everywhere in the database | All `DATETIME2` columns store UTC. SAST conversion in the application layer only. |
| Shared components in GACS.Components | No surface project defines its own components. If used in two places, it moves to the shared library. |
| Shared classes in GACS.Shared | `ApiResponse`, `PagedResult`, `PaginationParameters`, global exception handlers — never duplicated. |
| Audit log always written | Stored procedures write to `audit.AuditLog` within the same transaction. Cannot be bypassed. |

---

## 10. Build Order for Junior Developers

Follow this order. Do not skip ahead. Each step depends on the previous one being solid.

```
Step 1 — Solution scaffold
    Create the solution structure exactly as shown in Section 6.
    Reference GACS.Shared from all service projects.
    Reference GACS.Components from all web projects.
    Add Aspire AppHost and ServiceDefaults projects.
    Verify solution builds with zero errors before writing any logic.

Step 2 — Database project
    Create all six schemas in the SQL Server Database Project.
    Apply standard columns template to the project.
    Run T4 template against one entity (Tenant) — verify 5 stored procs generated.
    Apply RLS policy to identity.Tenants — verify cross-tenant query returns zero rows.
    Initialise Hangfire schema.

Step 3 — Aspire AppHost
    Wire up SQL Server, Redis, Blob Storage (Azurite), YARP, and the API project.
    Verify F5 starts everything and the Aspire dashboard opens.
    Verify service discovery — API can resolve its SQL Server connection string from Aspire.

Step 4 — GACS.Shared
    Implement ApiResponse<T>, PagedResult<T>, PaginationParameters.
    Implement GlobalExceptionHandlers (IExceptionHandler + ProblemDetails).
    Write unit tests for the response wrappers.
    No other code until this is done and tested.

Step 5 — YARP Gateway
    Implement ApiKeyValidationMiddleware — reads key, checks Redis, attaches TenantId to context.
    Implement TenantResolutionMiddleware — reads TenantId, validates against SQL.
    Implement RateLimitingMiddleware — Redis-backed counters per tenant.
    Implement UsageMeteringMiddleware — logs call count to Redis.
    Test: valid key routes through. Invalid key returns 401. Rate limit triggers 429.

Step 6 — Identity & Tenant Service — Domain layer
    Implement entity classes (Tenant, User, ApiKey) in the Domain project.
    Implement domain error constants in Errors/ folder.
    No logic — just the shape of the data and the named errors.

Step 7 — Identity & Tenant Service — Infrastructure layer
    Implement Dapper repositories for Tenant, User, ApiKey.
    Each repository method calls a stored procedure — no inline SQL.
    Implement RedisCacheService — wraps StackExchange.Redis.
    Implement Dapper connection factory — sets TenantId session context on every connection.
    Test: insert a tenant via stored proc, retrieve it, soft-delete it, verify audit log row written.

Step 8 — Identity & Tenant Service — Application layer
    Implement Wolverine commands and queries for Tenant, User, ApiKey.
    Each handler calls the repository, returns ErrorOr<T>.
    No HTTP concerns in this layer — pure business logic.

Step 9 — Identity & Tenant Service — API layer
    Implement versioned controllers (v1) for Tenants, Users, ApiKeys.
    Each controller action sends to Wolverine, calls .Match() on ErrorOr<T>.
    Returns ApiResponse<T> or ApiResponse<PagedResult<T>>.
    Wire up Scalar at /scalar/v1.
    Test every endpoint through Scalar — verify request/response shapes.

Step 10 — Hangfire jobs
    Implement all 6 job classes in the Infrastructure project.
    Register schedules in Program.cs.
    Verify RetentionExpiryJob runs and soft-deletes the correct records.
    Verify UsageMeteringFlushJob reads Redis and writes to SQL correctly.

Step 11 — GACS.Components
    Implement the 3 layout shells (Admin, Gate, Visitor).
    Implement the 8 custom components (Risk Badge, Visitor Profile Card, etc.).
    Implement GACS Fluent UI theme with color tokens.
    Verify components render correctly in Storybook-style isolation page.

Step 12 — Frontend surfaces
    Admin portal (Blazor Server) — wired to Identity & Tenant Service API.
    Visitor registration (Blazor WASM) — static, no auth.
    Resident portal (Blazor WASM) — Entra ID auth, wired to API.
    Guard app (MAUI Blazor Hybrid) — camera, QR scanner, SignalR connection.
```

---

## 11. What Comes in Phase 2

These are confirmed for Phase 2. They are not forgotten — they are deliberately deferred until Phase 1 is stable and deployed with real clients.

| Feature | Why Deferred |
|---|---|
| Visitor & Consent Service | Needs Identity & Tenant Service as foundation |
| Notification Service | Needs Visitor Service to know who to notify |
| AI Risk Scoring (Azure OpenAI) | Needs real visitor data to score |
| Azure Face API (liveness, photo match) | Needs visitor registration flow first |
| Hardware integrations (ZKTeco, Hikvision, Suprema) | Needs full visitor check-in flow first |
| Biometric governance module | Needs hardware integrations |
| Power BI Embedded dashboards | Needs data to report on |
| Azure Service Bus (async messaging) | Needed when 2+ services exist |
| AppSource listing and billing pipeline | Build the product first |
| Geo-replication to Cape Town | Azure SA West AZ support needs verification |
| Microsoft Teams integration | Phase 2 enhancement |
| AI compliance gap analyzer | Phase 2 sales/diagnostic tool |

---

*This document is the source of truth for the GACS system architecture. All layer documents are referenced from here. When a decision changes, update the relevant layer document and update this document to reflect it.*
