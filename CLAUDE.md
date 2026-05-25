# GACS — Claude Code Project Context

> Read this before making any changes. It tells you what the project is, how it's structured, and which patterns are mandatory.

## What this project is

**Gated Access Compliance Suite (GACS)** — a multi-tenant B2B SaaS platform for South African gated estates. It handles visitor management, AI risk scoring, and POPIA (South Africa's data protection law) compliance.

Stack: .NET 10 · Blazor Server · Blazor WASM · MAUI Hybrid · YARP gateway · Wolverine message bus · Dapper · Azure SQL · Redis · .NET Aspire · Fluent UI Blazor

## How to run

```bash
cd aspire/GACS.AppHost
dotnet run --launch-profile http
# Dashboard: http://localhost:15082  (token printed in console)
# Admin portal: http://localhost:5162
```

Docker Desktop must be running before starting Aspire.

## Solution structure

```
aspire/GACS.AppHost/          ← Orchestrator. Start here. Edit to add new services.
aspire/GACS.ServiceDefaults/  ← Call builder.AddServiceDefaults() in every service.
src/GACS.Gateway/             ← YARP proxy. All browser traffic goes here first.
src/GACS.IdentityTenant.Api/  ← REST API for tenants, users, API keys.
src/GACS.IdentityTenant.Application/ ← Wolverine commands/queries/handlers.
src/GACS.IdentityTenant.Domain/      ← Entities, domain errors (ErrorOr).
src/GACS.IdentityTenant.Infrastructure/ ← Dapper repos, Redis, Hangfire jobs.
src/GACS.Shared/              ← ApiResponse<T>, PagedResult<T>, GlobalExceptionHandler.
src/GACS.Components/          ← Shared Blazor component library. Consumed by all frontends.
src/GACS.Web.Admin/           ← Blazor Server — admin portal (main UI).
src/GACS.Web.Visitor/         ← Blazor WASM — visitor self-registration.
src/GACS.Web.Resident/        ← Blazor WASM — resident portal.
src/GACS.Mobile.Guard/        ← MAUI Blazor Hybrid — guard app.
docs/                         ← Architecture documents. Read before implementing features.
ONBOARDING.md                 ← Full developer onboarding guide.
```

## Mandatory patterns — enforce these in every change

### API responses
All API endpoints return `ApiResponse<T>` from `GACS.Shared`:
```csharp
return Ok(new ApiResponse<TenantDto>(data, true, null, null));
// Never return raw objects from controllers
```

### Error handling
Use `ErrorOr` library — never throw exceptions for domain errors:
```csharp
// Domain
public static readonly Error NotFound = Error.NotFound("Tenant.NotFound", "Tenant does not exist");

// Handler  
ErrorOr<Tenant> result = await _repo.GetAsync(id);
return result.Match(
    tenant => Ok(new ApiResponse<TenantDto>(tenant.ToDto(), true, null, null)),
    errors => Problem(errors.First().Description)
);
```

### Layering rule
Controllers → Application (Wolverine handlers) → Domain → Infrastructure (Dapper)
**Never call repositories from controllers. Never call HTTP clients from handlers.**

### Multi-tenancy
Row-Level Security on Azure SQL scopes every query automatically.
**Never add `WHERE TenantId = @X` to Dapper queries** — RLS does this at the database level.
TenantId is set as a SQL session context variable by `DapperConnectionFactory` before each query.

### Logging — POPIA compliance
**Never log PII** (names, ID numbers, emails, phone numbers, photos).
Log entity IDs only:
```csharp
_logger.LogInformation("Tenant created. TenantId={TenantId}", tenant.Id);
// ❌ _logger.LogInformation("Created tenant for {Name}", tenant.Name);
```

### CSS design tokens
All colours, spacing, shadows, and radii are CSS custom properties from `gacs-tokens.css`.
Never hard-code colour values in component CSS:
```css
/* ✅ */ color: var(--colorBrandPrimary, #0F6CBD);   /* var + fallback */
/* ❌ */ color: #0F6CBD;
```

### Blazor component CSS isolation
Each component has a `.razor.css` sibling file. All styles are scoped automatically by Blazor.
No BEM naming required — use simple descriptive class names.
Always include fallback values in `var()` calls in case tokens load late.

### Blazor lifecycle
JS interop goes in `OnAfterRenderAsync(firstRender)`, never in `OnInitializedAsync`.
Blazor Server prerenders twice — guard state initialisation with a `_initialized` flag.

## CSS loading order in _Host.cshtml

```html
1. Google Fonts <link> (preconnect + stylesheet)
2. _content/GACS.Components/gacs-tokens.css   (design tokens)
3. css/site.css                                (global baseline reset)
4. GACS.Web.Admin.styles.css                   (scoped component bundle)
5. Fluent UI module script (async)
```

Never add `@import url(...)` inside any CSS file — it blocks the CSS parser.
Always load fonts and external CSS as `<link>` tags in `_Host.cshtml`.

## Gateway middleware order (DO NOT change)

```
ApiKeyValidationMiddleware  → validate X-Api-Key header, set TenantId
TenantResolutionMiddleware  → resolve full tenant context
UseRateLimiter()            → sliding window, 1000 req/min per tenant
UsageMeteringMiddleware     → INCR Redis counter after response
MapReverseProxy()           → YARP forwards to downstream
```

## Key files to know

| File | Why it matters |
|---|---|
| `aspire/GACS.AppHost/Program.cs` | Add new services here |
| `src/GACS.Gateway/appsettings.json` | Add new YARP routes here |
| `src/GACS.Components/wwwroot/gacs-tokens.css` | All design tokens |
| `src/GACS.Components/Shells/AdminShell.razor` | Admin page layout shell |
| `docs/MASTER-ARCHITECTURE.md` | System overview and tech decisions |

## Build commands

```bash
# Full solution build
dotnet build GACS.slnx

# Clean + rebuild (run this when CSS changes don't appear)
dotnet clean GACS.slnx && dotnet build GACS.slnx

# Run just the admin portal (without Aspire)
cd src/GACS.Web.Admin && dotnet run
```

## What NOT to do

- Do not add `@import url(...)` to any CSS file
- Do not hard-code ports in YARP config — use Aspire service discovery names
- Do not call repos from controllers (bypass the application layer)
- Do not add `WHERE TenantId = ...` to Dapper queries (breaks RLS)
- Do not log any PII (names, IDs, emails, phone numbers)
- Do not commit secrets — use `dotnet user-secrets` or Azure Key Vault
- Do not use `OnInitializedAsync` for JS interop
