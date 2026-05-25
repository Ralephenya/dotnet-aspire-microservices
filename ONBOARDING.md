# GACS Developer Onboarding Guide

> **Gated Access Compliance Suite** — POPIA-compliant visitor management for South African gated estates.  
> Stack: .NET 10 · Blazor Server / WASM · MAUI Hybrid · YARP · Wolverine · Dapper · Azure SQL · Redis · Aspire

---

## 1. What is GACS?

GACS is a multi-tenant SaaS platform that gives South African gated residential communities and commercial estates:

| Problem | GACS Solution |
|---|---|
| Paper visitor books | Digital pre-registration with QR codes |
| Manual gate decisions | AI-powered real-time risk scoring |
| POPIA compliance guesswork | Structured consent capture, retention timers, full audit trail |
| Siloed systems | Single platform — four surfaces, one codebase |

**Four frontend surfaces** — all built in C# with Blazor:

| Portal | URL | Stack | Users |
|---|---|---|---|
| Admin & Compliance | `/` (this app) | Blazor Server | Estate managers, Information Officers, Auditors |
| Visitor Registration | `gacs-visitor` | Blazor WASM | Public visitors |
| Guard Check-In App | MAUI app | MAUI Blazor Hybrid | Guards, gate operators |
| Resident Portal | `gacs-resident` | Blazor WASM | Residents, household hosts |

---

## 2. Prerequisites

Install everything before your first `dotnet run`:

| Tool | Version | Where to get it |
|---|---|---|
| .NET SDK | **10.0 (latest)** | https://dot.net |
| Visual Studio 2022 | **17.14+** | https://visualstudio.microsoft.com |
| Docker Desktop | **4.40+** | https://docker.com/products/docker-desktop |
| Git | Any recent | https://git-scm.com |
| Node.js | 20 LTS (optional — only for Tailwind if added later) | https://nodejs.org |

> **Docker must be running before you press F5.** Aspire starts SQL Server, Redis, Azurite (Azure Storage), Prometheus, Loki, and Grafana as Docker containers. If Docker is not running, the AppHost will fail immediately.

Verify your setup:
```bash
dotnet --version          # must show 10.x
docker info               # must not error
dotnet dev-certs https --trust   # run once to avoid browser cert warnings
```

---

## 3. Running the Project

### Quickest path — Visual Studio

1. Open `Gated Access Compliance Suite.sln`
2. Set **`GACS.AppHost`** as the startup project
3. Press **F5** (or Ctrl+F5 for without debugging)
4. The Aspire Dashboard opens automatically in your browser

### From a terminal

```bash
cd "aspire/GACS.AppHost"
dotnet run --launch-profile http
```

### What starts up

The Aspire AppHost orchestrates **12 resources**:

| Resource | Type | Description |
|---|---|---|
| `gacs-sql` | SQL Server container | Main database — persistent across restarts |
| `gacsdb` | SQL database | The GACS application database |
| `gacs-redis` | Redis container | Caching, session, rate limiting |
| `gacs-storage` | Azurite emulator | Azure Blob Storage (photos, documents) |
| `gacs-blobs` | Blob container | Visitor photo & document storage |
| `gacs-prometheus` | Prometheus container | Scrapes `/metrics` from all services |
| `gacs-loki` | Loki container | Log aggregation |
| `gacs-grafana` | Grafana container | Dashboards — auto-provisioned, no login needed |
| `gacs-identitytenant-api` | .NET API | Identity, tenant, user management |
| `gacs-gateway` | .NET API | YARP reverse proxy / API gateway |
| `gacs-web-admin` | Blazor Server | Admin portal — this app |
| `aspire-dashboard` | Aspire built-in | Observability dashboard |

---

## 4. Key URLs (once running)

| URL | What it is |
|---|---|
| `http://localhost:15082` | **Aspire Dashboard** — resource health, logs, traces, metrics |
| `http://localhost:5162` | **GACS Admin Portal** (Web.Admin — landing page + dashboard) |
| `http://localhost:5211/scalar/v1` | **API Explorer** — interactive Scalar docs for Identity API |
| `http://localhost:5142` | **API Gateway** (YARP) — routes to downstream services |
| `http://localhost:9090` | **Prometheus** — raw metrics scrape UI |
| `http://localhost:3000` | **Grafana** — dashboards (anonymous access enabled in dev) |
| `http://localhost:3100` | **Loki** — log storage (query via Grafana) |

> The Aspire Dashboard login token is printed in the console when the AppHost starts. Look for a line like:  
> `Login to the dashboard at http://localhost:15082/login?t=xxxxxxxx`

---

## 5. Solution Structure

```
Gated Access Compliance Suite/
├── aspire/
│   ├── GACS.AppHost/                  ← Start here. Orchestrates everything.
│   │   ├── Program.cs                 ← Wires all resources & services
│   │   ├── appsettings.json           ← Dev defaults (SQL password, etc.)
│   │   └── observability/             ← Prometheus + Loki + Grafana config
│   └── GACS.ServiceDefaults/          ← Shared telemetry, health, discovery
│       └── Extensions.cs              ← AddServiceDefaults() — call in every service
│
├── src/
│   ├── GACS.Gateway/                  ← YARP reverse proxy (entry point for APIs)
│   │   └── Program.cs                 ← Route config, middleware stubs
│   │
│   ├── GACS.IdentityTenant.Api/       ← REST API — tenants, users, API keys
│   │   └── Controllers/v1/            ← TenantsController, UsersController, ApiKeysController
│   │
│   ├── GACS.IdentityTenant.Application/  ← Commands, queries, handlers (Wolverine)
│   ├── GACS.IdentityTenant.Domain/       ← Entities, domain errors (ErrorOr)
│   ├── GACS.IdentityTenant.Infrastructure/ ← Dapper repos, Redis cache, Hangfire jobs
│   │
│   ├── GACS.Shared/                   ← ApiResponse<T>, PagedResult<T>, GlobalExceptionHandler
│   │
│   ├── GACS.Components/               ← Shared Blazor component library
│   │   ├── Components/                ← LandingPage, LoginPage, RiskBadge, etc.
│   │   ├── Shells/                    ← AdminShell, GateShell, VisitorShell
│   │   ├── Themes/                    ← GacsTokens.cs (CSS token constants)
│   │   └── wwwroot/gacs-tokens.css    ← CSS custom properties (design system)
│   │
│   ├── GACS.Web.Admin/                ← Blazor Server — Admin portal
│   │   ├── Pages/Home.razor           ← Landing page at /
│   │   ├── Pages/Login.razor          ← Sign-in page at /login
│   │   ├── Pages/Dashboard.razor      ← Admin dashboard at /dashboard
│   │   └── Shared/MainLayout.razor    ← Bare passthrough (each page owns its shell)
│   │
│   ├── GACS.Web.Visitor/              ← Blazor WASM — visitor self-registration
│   ├── GACS.Web.Resident/             ← Blazor WASM — resident portal
│   └── GACS.Mobile.Guard/             ← MAUI Blazor Hybrid — guard app
│
├── database/
│   └── GACS.Database/                 ← SQL Server Database Project
│
├── docs/
│   ├── FRONTEND-ARCHITECTURE.md       ← Read before writing any UI
│   ├── BACKEND-ARCHITECTURE.md        ← Read before writing any API
│   ├── DATA-ARCHITECTURE.md           ← Read before touching the database
│   └── MASTER-ARCHITECTURE.md         ← Start here for the big picture
│
└── ONBOARDING.md                      ← This file
```

---

## 6. Architecture Quick Reference

### How a request flows through GACS

```
Browser (Admin Portal)
    │
    ▼
GACS.Web.Admin  (Blazor Server — port 5162)
    │  calls API via HttpClient with service discovery
    ▼
GACS.Gateway    (YARP — port 5142)
    │  routes to downstream service
    ▼
GACS.IdentityTenant.Api  (port 5211)
    │  Wolverine handles command/query
    ▼
GACS.IdentityTenant.Application  →  Domain  →  Infrastructure
                                                    │
                                             Azure SQL (Dapper)
                                             Redis (cache)
                                             Blob Storage (files)
```

### Layer responsibilities

| Layer | Lives in | What goes here |
|---|---|---|
| **API** | `*.Api` | HTTP controllers, versioning, Scalar docs, auth middleware |
| **Application** | `*.Application` | Commands, queries, Wolverine handlers, validation |
| **Domain** | `*.Domain` | Entities, value objects, domain errors (ErrorOr) |
| **Infrastructure** | `*.Infrastructure` | Dapper repos, Redis, Hangfire jobs, SQL connection |
| **Shared** | `GACS.Shared` | Cross-cutting DTOs: `ApiResponse<T>`, `PagedResult<T>` |

### Adding a new feature — the right order

1. **Domain** — add the entity or value object
2. **Domain** — add an error constant in `*Errors.cs` using ErrorOr
3. **Infrastructure** — add the repository interface + Dapper implementation
4. **Application** — add the command/query + Wolverine handler
5. **API** — add the controller action (versioned, returning `ApiResponse<T>`)
6. **Frontend** — add the Blazor component or page that calls the API

> **Never skip layers.** Controllers must not call repositories directly. Handlers must not know about HTTP.

---

## 7. Design System

All UI colours, spacing, shadows, and border radii are CSS custom properties defined in `gacs-tokens.css`. Use them everywhere — never hard-code a colour value.

```css
/* ✅ Correct */
color: var(--colorBrandPrimary);
box-shadow: var(--shadowM);
border-radius: var(--radiusL);

/* ❌ Wrong */
color: #0F6CBD;
box-shadow: 0 4px 16px rgba(0,0,0,0.08);
```

**Token reference:**

| Token | Value | Use |
|---|---|---|
| `--colorBrandPrimary` | `#0F6CBD` | Buttons, links, active states |
| `--colorBrandSecondary` | `#0C3B5E` | Nav bars, hero sections |
| `--colorBrandDark` | `#040F1E` | Footers, dark panels |
| `--colorNeutralBackground` | `#F8FAFC` | Page background |
| `--colorNeutralSurface` | `#FFFFFF` | Cards, panels |
| `--colorNeutralBorder` | `#E2E8F0` | Dividers, card borders |
| `--colorRiskGreen` / `Yellow` / `Red` | See file | Risk badge colours |
| `--shadowS/M/L/XL` | See file | Card elevation |
| `--radiusM/L/XL` | 10/16/24px | Border radius scale |
| `--spacingVerticalM/L/XL` | 16/24/32px | Spacing grid (4px base) |

**Fonts:** Inter (loaded from Google Fonts via `gacs-tokens.css`). The CSS variable is `--fontFamilyBase`.

### Component library (`GACS.Components`)

All shared UI lives here. Import it via `@using GACS.Components.Components` (already in `_Imports.razor`).

| Component | Parameters | Use |
|---|---|---|
| `LandingPage` | `OnLogin`, `OnRequestDemo` | Public landing surface |
| `LoginPage` | `OnSignIn`, `OnGoHome` | Authentication page |
| `AdminShell` | `ChildContent`, `SidebarContent`, `TopBarContent` | Admin layout shell |
| `RiskBadge` | `RiskScore`, `IsLoading` | Visitor risk level chip |
| `ComplianceScoreRing` | (parameters TBD) | Estate compliance score ring |
| `VisitorProfileCard` | (parameters TBD) | Visitor summary card |
| `RetentionCountdown` | `RetentionStartDate`, `RetentionExpiresAt` | Days until auto-deletion |
| `GateStatusIndicator` | (parameters TBD) | Live gate open/closed state |
| `ConsentCard` | (parameters TBD) | POPIA consent capture UI |
| `AiCoachingPanel` | (parameters TBD) | AI recommendation display |

---

## 8. Adding a New Blazor Page

1. Create `Pages/MyFeature.razor` in `GACS.Web.Admin` (or the appropriate project)
2. Add `@page "/my-feature"` at the top
3. Choose a shell based on what the page needs:
   ```razor
   @* Use AdminShell for authenticated admin pages *@
   <AdminShell>
       <TopBarContent>My Feature</TopBarContent>
       <SidebarContent><!-- nav items --></SidebarContent>
       <ChildContent>
           <!-- your page content -->
       </ChildContent>
   </AdminShell>
   ```
4. Create `Pages/MyFeature.razor.css` for scoped styles (Blazor auto-scopes these — no BEM needed)
5. Inject `HttpClient` (named `"gateway"`) to call APIs:
   ```csharp
   @inject IHttpClientFactory HttpClientFactory
   
   var client = HttpClientFactory.CreateClient("gateway");
   var result = await client.GetFromJsonAsync<ApiResponse<MyDto>>("/api/v1/my-resource");
   ```

### Blazor lifecycle — the one thing you must know

```
OnInitialized / OnInitializedAsync
    ↓ (runs twice in Blazor Server with prerendering — guard with a flag)
OnParametersSet / OnParametersSetAsync
    ↓
OnAfterRender / OnAfterRenderAsync  ← JS interop goes HERE, not OnInitialized
    ↓
StateHasChanged()  ← call to trigger re-render after async updates
```

> **Common mistake**: calling JS interop in `OnInitializedAsync`. The DOM doesn't exist yet during prerendering. Always use `OnAfterRenderAsync`.

---

## 9. API Patterns

### Returning data from a controller

```csharp
// ✅ All API responses are wrapped in ApiResponse<T>
[HttpGet("{id}")]
public IActionResult GetTenant(Guid id)
{
    var result = _handler.Handle(new GetTenantQuery(id));
    return result.Match(
        tenant => Ok(new ApiResponse<TenantDto>(tenant, true, null, null)),
        errors => Problem(errors.First().Description)
    );
}
```

### Calling the API from Blazor

```csharp
var http = HttpClientFactory.CreateClient("gateway");
var response = await http.GetFromJsonAsync<ApiResponse<TenantDto>>($"/api/v1/tenants/{id}");

if (response?.Success == true)
    _tenant = response.Data;
```

### Paginated lists

```csharp
// Query string: /api/v1/tenants?page=1&pageSize=25
var response = await http.GetFromJsonAsync<ApiResponse<PagedResult<TenantDto>>>(
    $"/api/v1/tenants?page={_page}&pageSize=25");
```

---

## 10. Multi-tenancy

Every database query is scoped to the current tenant via **Row-Level Security (RLS)** on Azure SQL. The tenant ID is set as a session context variable before each query in `DapperConnectionFactory.cs`.

**You never need to add `WHERE TenantId = @TenantId` to your queries.** RLS handles this at the database level.

The tenant ID comes from the authenticated user's Entra ID claims and is injected by `TenantResolutionMiddleware` in the gateway.

---

## 11. Observability

Every service ships telemetry automatically via `GACS.ServiceDefaults`.

| What | Where | How |
|---|---|---|
| **Traces** | Aspire Dashboard → Traces tab | Auto — every HTTP request + DB call |
| **Logs** | Aspire Dashboard → Logs tab, also Grafana → Loki | Auto — `ILogger<T>` writes to OTLP |
| **Metrics** | Prometheus `http://localhost:9090`, Grafana | Auto — `/metrics` endpoint on all services |
| **Health** | Aspire Dashboard → Resources | Auto — `/health` and `/alive` endpoints |

To add a custom trace in your code:

```csharp
private static readonly ActivitySource _tracer = new("GACS.IdentityTenant");

using var activity = _tracer.StartActivity("CreateTenant");
activity?.SetTag("tenant.id", tenantId);
// ... your code
```

Register the source in `Program.cs`:
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddSource("GACS.IdentityTenant"));
```

---

## 12. Hangfire Background Jobs

6 recurring jobs are defined in `GACS.IdentityTenant.Infrastructure/Jobs/`. Each implements `IHangfireJob` with a single `ExecuteAsync()` method.

| Job | Schedule | What it does |
|---|---|---|
| `RetentionExpiryJob` | Daily 2am | Marks expired visitor records for deletion |
| `QrTokenCleanupJob` | Every 15min | Removes expired QR tokens |
| `UsageMeteringFlushJob` | Every hour | Writes API usage metrics to billing tables |
| `ConsentExpiryReminderJob` | Daily 8am | Emails data subjects before consent expires |
| `StaleVisitCleanupJob` | Daily 3am | Archives visits with no check-out after 24h |
| `AuditLogArchiveJob` | Weekly Sunday 1am | Archives old audit logs to Blob Storage |

The Hangfire dashboard is available at `http://localhost:5211/hangfire` (Identity API port).

---

## 13. POPIA Compliance Cheat Sheet

> This section exists so junior developers understand **why** certain patterns are mandatory.

| Requirement | How GACS implements it |
|---|---|
| **Lawful basis** | Every data collection point records the legal basis (consent, legitimate interest, contract) |
| **Purpose limitation** | Consent is captured per-purpose (e.g., "entry photo" ≠ "marketing") |
| **Retention limits** | `RetentionCountdown` component + `RetentionExpiryJob` enforce automatic deletion |
| **Data subject rights** | Visitor can request deletion — soft-delete + anonymisation, not hard delete |
| **Audit trail** | Every data access, change, and deletion is recorded in the audit log table |
| **Security** | RLS at DB level, Entra ID auth, HTTPS enforced, no PII in logs |

**Never log personal information.** This includes names, ID numbers, phone numbers, email addresses, photos. Log entity IDs only.

---

## 14. Common Mistakes to Avoid

| Mistake | Correct approach |
|---|---|
| Hard-coding colours in CSS | Use `var(--colorBrandPrimary)` |
| Calling repos from controllers | Controllers → Application layer → Infrastructure |
| Adding `WHERE TenantId = @X` to Dapper queries | RLS handles it — adding this breaks the security model |
| Logging PII | Log IDs only (`tenantId`, `visitId`) — never names or contact info |
| Using `OnInitializedAsync` for JS interop | Use `OnAfterRenderAsync(firstRender)` |
| Committing secrets to git | All secrets go in `dotnet user-secrets` or Azure Key Vault |
| Skipping `ApiResponse<T>` wrapper | All API endpoints must return the standard wrapper |
| Creating a new CSS file for colours | All tokens live in `gacs-tokens.css` |

---

## 15. Getting Help

| Resource | Location |
|---|---|
| Frontend decisions | `docs/FRONTEND-ARCHITECTURE.md` |
| Backend decisions | `docs/BACKEND-ARCHITECTURE.md` |
| Database & POPIA | `docs/DATA-ARCHITECTURE.md` |
| Full system picture | `docs/MASTER-ARCHITECTURE.md` |
| API docs | `http://localhost:5211/scalar/v1` (while running) |
| Aspire Dashboard | `http://localhost:15082` (while running) |
| Grafana | `http://localhost:3000` (while running) |

---

*Built with .NET 10, Blazor, MAUI, .NET Aspire, Fluent UI, YARP, Wolverine, Dapper, and a lot of care for POPIA compliance.*
