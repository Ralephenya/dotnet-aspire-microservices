# GACS — Backend Architecture

**Gated Access Compliance Suite**
Last updated: 2026-05-25
Status: Architecture definition — pre-build

---

## 1. Overview

The GACS backend is a single .NET 8 Web API structured around the Identity & Tenant Service — the first and foundational microservice. Every other service will follow the same patterns, conventions, and libraries established here. Junior developers learn this service deeply before a second service is introduced.

All traffic enters through YARP (the internal gateway). YARP handles authentication, routing, load balancing, rate limiting, and caching via Redis. No request reaches the backend service without passing through YARP.

---

## 2. Traffic Flow

```
Client (Blazor / MAUI / WhatsApp)
        ↓
Azure Front Door
  — WAF, DDoS protection, SSL termination, global CDN edge
        ↓
YARP (.NET 8 — standalone Container App)
  — API key validation per tenant
  — Entra ID JWT validation
  — Request routing to backend services
  — Load balancing (least requests)
  — Rate limiting (Redis-backed counters)
  — Usage metering (calls per tenant logged to Redis → billing)
  — Response caching (Redis)
        ↓
Identity & Tenant Service (.NET 8 Web API)
        ↓
Azure SQL Server   +   Azure Cache for Redis
```

---

## 3. Gateway — YARP

YARP (Yet Another Reverse Proxy) is Microsoft's reverse proxy library. It runs as a standalone .NET 8 application in its own Azure Container App — separate from all backend services.

**YARP owns:**

| Responsibility | Detail |
|---|---|
| API key validation | Every request must carry a tenant API key in the header. YARP validates it against Redis cache before routing. Invalid key → 401, request never reaches the service. |
| JWT validation | Entra ID tokens validated at the YARP middleware level. Extracts tenant ID and user role, attaches to request context. |
| Routing | Maps incoming paths to the correct backend service. `/api/v1/tenants/*` → Identity & Tenant Service. Future services get new route entries — no changes to APIM or Front Door. |
| Load balancing | Least-requests algorithm. When a service scales to multiple instances, YARP distributes traffic automatically. |
| Rate limiting | Per-tenant rate limits enforced using .NET 8 built-in rate limiting middleware backed by Redis counters. Limits are configurable per tenant subscription tier. |
| Usage metering | Every routed request logs a metering event (tenant ID, endpoint, timestamp) to Redis. A background job reads these for billing. |
| Response caching | Selected read-only endpoints cached in Redis with configurable TTL. Reduces load on backend services for frequently-called lookups. |

**What YARP does NOT own:**
- Business logic of any kind
- Database access
- AI calls
- Notification sending

YARP is infrastructure only.

---

## 4. API Versioning

**Library:** `Asp.Versioning.Mvc` + `Asp.Versioning.Mvc.ApiExplorer`
**Version:** 8.1.0 (targets .NET 8)
**Strategy:** URL segment versioning

Every endpoint is versioned via the URL:

```
/api/v1/tenants
/api/v2/tenants
```

**Why URL segment over header or query string:**
- Version visible in every log line, every YARP route, every support ticket
- CDN and proxy caches work correctly without extra configuration
- Power Platform connectors and Logic Apps (AppSource integrations) work without custom header configuration
- Consistent with how Microsoft's own ecosystem operates (Microsoft Graph uses `/v1.0/`)

**Version lifecycle:**
- New versions added as new route registrations — old versions continue to function
- Deprecated versions flagged with `Deprecated = true` in the version attribute
- Scalar surfaces deprecated operations with a visual warning
- Sunset headers emitted on deprecated version responses (RFC 8594) — gives clients a machine-readable removal date
- Minimum 6 months notice before a version is removed

**Each API version gets its own Scalar document:**
- `/scalar/v1` — version 1 API reference
- `/scalar/v2` — version 2 API reference (when it exists)

---

## 5. API Documentation — Scalar

**No Swagger UI. No Swashbuckle.**

| Package | Role |
|---|---|
| `Microsoft.AspNetCore.OpenApi` | Generates the OpenAPI JSON document natively (.NET 8 built-in) |
| `Scalar.AspNetCore` (2.14.14) | Renders the API reference UI from the OpenAPI document |

Scalar is available at `/scalar/v1` in development and staging environments only. It is disabled in production by environment flag.

Each API version produces a separate named OpenAPI document. Scalar shows a version picker — developers select v1 or v2 and see that version's full endpoint list, schemas, and request/response shapes.

Deprecated endpoints appear with a visual strikethrough in Scalar. The `info.description` field of the OpenAPI document for deprecated versions includes a migration notice.

---

## 6. Request Handling — Wolverine

**No MediatR.** MediatR went commercial (paid licence) in July 2025. For a commercial SaaS product, Wolverine is the correct choice.

**Library:** Wolverine (WolverineFx)
**Licence:** MIT — free for commercial use
**Why Wolverine over MediatR:**
- MIT licensed — no per-developer licence cost
- Compile-time source generators — handler resolution happens at build time, not runtime reflection. Faster.
- Handles both in-process CQRS (commands, queries) and async messaging in one library
- Pairs cleanly with ErrorOr for result handling

**The pattern — every feature follows this flow:**

```
Controller receives HTTP request
        ↓
Controller sends Command or Query via Wolverine
        ↓
Wolverine resolves the correct Handler at compile time
        ↓
Handler calls the Service
        ↓
Service returns ErrorOr<T>
        ↓
Handler returns ErrorOr<T> to Controller
        ↓
Controller calls .Match()
    → Success path: wrap in ApiResponse<T> → HTTP 200/201
    → Error path: map error type to HTTP status → 400/401/404/409
        ↓
Response returned to YARP → forwarded to client
```

No try/catch blocks in controllers, handlers, or services. Ever.

---

## 7. Error Handling — ErrorOr

**Library:** `ErrorOr` 2.1.1 by Amichai Mantinband
**Licence:** MIT

Services never throw exceptions for expected domain failures. They return `ErrorOr<T>`.

**Built-in error types used in GACS:**

| Error Type | When Used | HTTP Status |
|---|---|---|
| `Error.Validation` | Invalid input, missing required fields | 400 |
| `Error.Unauthorized` | Token invalid, insufficient role | 401 |
| `Error.Forbidden` | Valid token, but no permission for this resource | 403 |
| `Error.NotFound` | Tenant, user, or record does not exist | 404 |
| `Error.Conflict` | Duplicate tenant, duplicate API key | 409 |
| `Error.Failure` | Business rule violation | 422 |
| `Error.Unexpected` | Something went wrong that should not have | 500 |

Controllers call `.Match()` on the returned `ErrorOr<T>`. The match delegates produce the correct HTTP response. Controllers contain no if/else error logic beyond the `.Match()` call.

---

## 8. Global Exception Handling

**What it covers:** Unexpected exceptions that ErrorOr did not anticipate — database timeouts, null references, infrastructure failures.

**How it works:**
- `IExceptionHandler` interface (.NET 8 built-in, `Microsoft.AspNetCore.Diagnostics`)
- Multiple handlers registered in priority order:
  1. Validation exception handler
  2. Infrastructure exception handler (DB, Redis, external API failures)
  3. Catch-all handler (any unhandled exception)
- All handlers return RFC 7807 `ProblemDetails` JSON — consistent error shape for every failure
- Registered via `builder.Services.AddExceptionHandler<T>()` and `builder.Services.AddProblemDetails()`

**The result:** No try/catch in any endpoint, handler, or service. The only place exceptions are caught is the global handler middleware. Junior devs never write a try/catch block in business logic.

---

## 9. Data Access — Dapper + Stored Procedures

**No Entity Framework.** Dapper is the data access layer. All database operations go through SQL Server stored procedures. No inline SQL in application code.

**Library:** `Dapper` (maintained by the Stack Overflow / DapperLib team)

**Why stored procedures over inline SQL:**
- All SQL lives in the database project, not scattered through application code
- Stored procedures can be reviewed, tested, and optimised independently of the application
- SQL Server query plan caching works optimally with stored procedures
- Audit trail — every data operation is an explicit named procedure
- POPIA compliance — data access patterns are documented and reviewable

**Stored procedure generation — T4 Templates:**

Junior devs do not hand-write CRUD stored procedures. A T4 template in the SQL Server Database Project reads the entity class definition and generates five stored procedures per entity:

| Generated Procedure | Purpose |
|---|---|
| `usp_[Entity]_SelectById` | Fetch single record by primary key |
| `usp_[Entity]_SelectAll` | Fetch paginated list with optional filters |
| `usp_[Entity]_Insert` | Insert new record, return new ID |
| `usp_[Entity]_Update` | Update existing record |
| `usp_[Entity]_Delete` | Soft delete (sets DeletedAt timestamp — no hard deletes for POPIA audit trail) |

**The workflow for junior devs:**
1. Define the entity class in the application project
2. Run the T4 template in the SQL Server Database Project
3. Five stored procedures are generated and added to the database project
4. Review and adjust generated SQL if needed
5. Deploy database project — procedures are live
6. Call procedures from the repository layer via Dapper

**No hard deletes.** Every entity has a `DeletedAt` (nullable datetime) and `IsDeleted` (bit) column. The Delete procedure sets these fields. Data is retained in the database for the POPIA-mandated audit trail and only purged by the scheduled retention service at the appropriate time.

---

## 10. Generic Response Shapes

Every endpoint in every service returns one of these two shapes. Junior devs never invent a custom response structure.

### ApiResponse — all endpoints

```
ApiResponse<T>
    ├── bool    Success
    ├── T       Data
    ├── string  Message
    └── List<string> Errors
```

### PagedResult — all list endpoints

```
PagedResult<T>
    ├── List<T> Items
    ├── int     TotalCount
    ├── int     PageNumber
    ├── int     PageSize
    ├── int     TotalPages
    ├── bool    HasNextPage
    └── bool    HasPreviousPage
```

List endpoints return `ApiResponse<PagedResult<T>>`.

Both classes live in `GACS.Shared` — a shared class library referenced by all services. They are defined once, never duplicated.

---

## 11. Caching — Azure Cache for Redis

Redis sits between YARP and the backend service, and also inside the backend service for application-level caching.

| Cache Use | TTL | Invalidation |
|---|---|---|
| API key → tenant mapping | 1 hour | On key rotation or revocation |
| JWT validation result | Token expiry | Automatic |
| Rate limit counters | Rolling 1 minute window | Automatic |
| Usage metering counters | Flushed every 5 minutes to billing store | Background job |
| QR token store | 120 seconds | Automatic expiry |
| AI risk score per visitor | 30 minutes | On visitor data change |
| Tenant configuration | 15 minutes | On config update |
| Paginated list responses (read-only) | 5 minutes | On underlying data change |

**Cache-aside pattern only.** The application checks Redis first. On miss, it reads from SQL Server and writes the result to Redis. The database is always the source of truth. Redis is always expendable — if Redis goes down, the system falls back to SQL Server automatically.

---

## 12. Project Structure — First Service

```
GACS.sln
│
├── src/
│   │
│   ├── GACS.Gateway/                        ← YARP standalone app
│   │   ├── Middleware/
│   │   │   ├── ApiKeyValidationMiddleware
│   │   │   ├── TenantResolutionMiddleware
│   │   │   ├── RateLimitingMiddleware
│   │   │   └── UsageMeteringMiddleware
│   │   └── Program.cs
│   │
│   ├── GACS.IdentityTenant.Api/             ← First microservice — Web API
│   │   ├── Controllers/
│   │   │   ├── v1/
│   │   │   │   ├── TenantsController
│   │   │   │   ├── UsersController
│   │   │   │   └── ApiKeysController
│   │   │   └── v2/                          ← Empty until v2 is needed
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── GACS.IdentityTenant.Application/     ← Business logic, commands, queries, handlers
│   │   ├── Tenants/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateTenant/
│   │   │   │   ├── UpdateTenant/
│   │   │   │   └── DeactivateTenant/
│   │   │   └── Queries/
│   │   │       ├── GetTenantById/
│   │   │       └── GetAllTenants/
│   │   ├── Users/
│   │   │   ├── Commands/
│   │   │   └── Queries/
│   │   └── ApiKeys/
│   │       ├── Commands/
│   │       └── Queries/
│   │
│   ├── GACS.IdentityTenant.Domain/          ← Entities, domain errors, value objects
│   │   ├── Entities/
│   │   │   ├── Tenant.cs
│   │   │   ├── User.cs
│   │   │   └── ApiKey.cs
│   │   └── Errors/
│   │       ├── TenantErrors.cs
│   │       ├── UserErrors.cs
│   │       └── ApiKeyErrors.cs
│   │
│   ├── GACS.IdentityTenant.Infrastructure/  ← Dapper repositories, Redis, Entra ID client
│   │   ├── Repositories/
│   │   │   ├── TenantRepository.cs
│   │   │   ├── UserRepository.cs
│   │   │   └── ApiKeyRepository.cs
│   │   ├── Cache/
│   │   │   └── RedisCacheService.cs
│   │   └── Identity/
│   │       └── EntraIdService.cs
│   │
│   └── GACS.Shared/                         ← Shared across ALL services
│       ├── Responses/
│       │   ├── ApiResponse.cs
│       │   └── PagedResult.cs
│       ├── Pagination/
│       │   └── PaginationParameters.cs
│       └── Errors/
│           └── GlobalExceptionHandlers.cs
│
├── database/
│   └── GACS.Database/                       ← SQL Server Database Project
│       ├── StoredProcedures/
│       │   ├── Tenants/
│       │   ├── Users/
│       │   └── ApiKeys/
│       └── Templates/
│           └── CrudGenerator.tt             ← T4 template — generates all 5 procs per entity
│
└── docs/
    ├── FRONTEND-ARCHITECTURE.md
    └── BACKEND-ARCHITECTURE.md              ← This document
```

---

## 13. Identity & Tenant Service — What It Owns

This is the first service built. Nothing else works without it.

**Tenants:**
- Create new tenant (estate, school, hospital onboarding)
- Update tenant configuration (security tier, QR mode, retention periods, language preferences)
- Deactivate tenant (churn, non-payment)
- Get tenant by ID
- List all tenants (paginated — System Admin only)

**Users:**
- Create user within a tenant
- Assign role (Information Officer, Property Manager, Guard, Auditor, Resident, System Admin)
- Update user profile
- Deactivate user
- Get user by ID
- List users within tenant (paginated)

**API Keys:**
- Issue API key for tenant (generated, hashed before storage)
- Rotate API key (issue new, invalidate old, Redis cache cleared)
- Revoke API key (immediate — Redis cache cleared, YARP rejects on next request)
- List active keys for tenant

**Entra ID integration:**
- SSO configuration per tenant
- MFA enforcement per tenant security tier
- Token validation delegated to Entra ID — this service stores GACS-specific user metadata only

**Tenant configuration stored here:**
- Security tier (Basic / Standard / Enhanced / Maximum)
- QR delivery mode (pre-generated / on-arrival / permanent resident)
- Default data minimisation template per access point type
- Retention periods per data category
- Subscription plan and billing metadata
- Enabled languages for consent forms

---

## 14. Standards — Non-Negotiable for Junior Developers

These rules apply to this service and every service that follows.

| Rule | Detail |
|---|---|
| No try/catch in business logic | ErrorOr handles domain errors. IExceptionHandler catches everything else. |
| No inline SQL | All database calls go through named stored procedures via Dapper. |
| No hard deletes | Every delete sets `DeletedAt` and `IsDeleted`. The retention service handles physical deletion on schedule. |
| No custom response shapes | Every endpoint returns `ApiResponse<T>` or `ApiResponse<PagedResult<T>>`. |
| No Swagger | Scalar only. Disabled in production. |
| No magic strings | All error messages, role names, and configuration keys are constants defined in the Domain project. |
| URL versioning only | All routes include `/api/v{version}/`. No header versioning, no query string versioning as primary strategy. |
| Redis for caching | No in-memory caching (`IMemoryCache`). Redis only — so caching works correctly when the service scales to multiple instances. |
| One handler per command/query | Wolverine handlers are single-purpose. No handler handles two different commands. |
| Shared library for shared concerns | `ApiResponse`, `PagedResult`, `PaginationParameters`, and global exception handlers live in `GACS.Shared`. Never duplicated per service. |

---

## 15. NuGet Packages — Identity & Tenant Service

| Package | Version | Purpose |
|---|---|---|
| `WolverineFx` | Latest stable | CQRS command/query handling |
| `ErrorOr` | 2.1.1 | Result type — no exceptions in domain logic |
| `Dapper` | Latest stable | Data access via stored procedures |
| `StackExchange.Redis` | Latest stable | Redis client |
| `Asp.Versioning.Mvc` | 8.1.0 | API versioning |
| `Asp.Versioning.Mvc.ApiExplorer` | 8.1.0 | OpenAPI integration for versioned APIs |
| `Microsoft.AspNetCore.OpenApi` | .NET 8 built-in | OpenAPI document generation |
| `Scalar.AspNetCore` | 2.14.14 | API documentation UI |
| `Microsoft.Identity.Web` | Latest stable | Entra ID / Azure AD token validation |
| `Yarp.ReverseProxy` | Latest stable | YARP gateway (in Gateway project only) |

---

## 16. Pre-Build Checklist — Backend

Before any junior developer writes their first controller action, the following must be in place:

- [ ] `GACS.Shared` project created with `ApiResponse<T>`, `PagedResult<T>`, `PaginationParameters`, and global exception handlers
- [ ] `GACS.IdentityTenant.Domain` project created with entity skeletons and error constant files
- [ ] SQL Server Database Project created with T4 template wired up and tested against one entity
- [ ] YARP gateway project created, connected to Redis, API key middleware stubbed
- [ ] Wolverine registered and wired to the DI container
- [ ] ErrorOr installed and one end-to-end flow tested (command → handler → service → ErrorOr → controller .Match() → response)
- [ ] IExceptionHandler implementations registered and verified (throw a test exception, confirm ProblemDetails JSON is returned)
- [ ] Scalar confirmed working at `/scalar/v1` in development environment
- [ ] API versioning confirmed — `/api/v1/` routes resolve, unversioned routes return 400
- [ ] Redis connection confirmed — cache hit/miss verified for one cached endpoint
- [ ] Entra ID token validation confirmed — protected endpoint returns 401 without a valid token
- [ ] One full CRUD cycle working end-to-end: HTTP request → YARP → Controller → Wolverine → Handler → Service → Dapper → Stored Procedure → SQL Server → Response

---

*Next architecture document: DATA-ARCHITECTURE.md — Azure SQL schema design, Redis topology, Blob Storage, data residency, retention scheduling.*
