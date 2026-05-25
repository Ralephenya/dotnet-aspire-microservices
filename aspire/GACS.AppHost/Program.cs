var builder = DistributedApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════════
// GACS — Aspire Developer Dashboard
// ═══════════════════════════════════════════════════════════════════════════════
// This dashboard helps junior developers understand and navigate the system.
// Each resource has a friendly name and description explaining what it does.
// Click on the URLs to open services in your browser.
// ═══════════════════════════════════════════════════════════════════════════════

// ── Observability Stack (Monitoring & Logs) ───────────────────────────────────
// These tools help you see what's happening inside the running applications.
// They are ONLY for local development — production uses Azure Monitor.

var prometheusConfig = Path.Combine(builder.AppHostDirectory,
    "observability", "prometheus", "prometheus.yml");

var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "v3.4.0")
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml",
              "--storage.tsdb.path=/prometheus",
              "--web.console.libraries=/usr/share/prometheus/console_libraries",
              "--web.console.templates=/usr/share/prometheus/consoles")
    .WithBindMount(prometheusConfig, "/etc/prometheus/prometheus.yml", isReadOnly: true)
    .WithHttpEndpoint(targetPort: 9090, name: "http", port: 9090)
    .WithUrlForEndpoint("http", url => url.DisplayText = "Open Prometheus UI")
    .WithLifetime(ContainerLifetime.Persistent);

var lokiConfig = Path.Combine(builder.AppHostDirectory,
    "observability", "loki", "loki-config.yml");

var loki = builder.AddContainer("loki", "grafana/loki", "3.5.0")
    .WithArgs("-config.file=/etc/loki/local-config.yaml")
    .WithBindMount(lokiConfig, "/etc/loki/local-config.yaml", isReadOnly: true)
    .WithHttpEndpoint(targetPort: 3100, name: "http", port: 3100)
    .WithUrlForEndpoint("http", url => url.DisplayText = "Open Loki API")
    .WithLifetime(ContainerLifetime.Persistent);

var grafanaProvisioning = Path.Combine(builder.AppHostDirectory,
    "observability", "grafana", "provisioning");

var grafana = builder.AddContainer("grafana", "grafana/grafana", "12.0.1")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")
    .WithEnvironment("GF_AUTH_DISABLE_LOGIN_FORM", "true")
    .WithBindMount(grafanaProvisioning, "/etc/grafana/provisioning", isReadOnly: true)
    .WithHttpEndpoint(targetPort: 3000, name: "http", port: 3000)
    .WithUrlForEndpoint("http", url => url.DisplayText = "Open Grafana (No login required)")
    .WithLifetime(ContainerLifetime.Persistent);

// ── Data & Caching Infrastructure ──────────────────────────────────────────
// These are the databases and storage that power the application.
// Data persists even when you stop debugging (F5 cycles).

// 🔐 SQL Server Password
// Value is read from Parameters:sql-password in appsettings.json (local dev default)
// or from user secrets / env vars to override. See MASTER-ARCHITECTURE.md §Environments.
var sqlPassword = builder.AddParameter("sql-password", secret: true);

// 🗄️ SQL Server — Main Database
// Stores: Users, visitors, incidents, audit logs, compliance records
// Connection string is automatically injected into services that need it.
var sql = builder.AddSqlServer("sql-server", password: sqlPassword)
                 .WithDataVolume("gacs-sql-data")
                 .WithLifetime(ContainerLifetime.Persistent);

var gacsDb = sql.AddDatabase("gacsdb", databaseName: "GACS");

// ⚡ Redis — Cache & Real-time
// Used for: Session cache, real-time location data, rate limiting
// Fast in-memory store for temporary data that doesn't need to persist.
var redis = builder.AddRedis("redis")
                   .WithDataVolume("gacs-redis-data")
                   .WithLifetime(ContainerLifetime.Persistent);

// ☁️ Azure Storage Emulator — Blob Storage
// Used for: Visitor photos, documents, export files
// Runs locally as an emulator (Azurite) — no Azure account needed for dev.
var storage = builder.AddAzureStorage("azure-storage")
                     .RunAsEmulator();

var blobStorage = storage.AddBlobs("blob-storage");

// ── Backend Services (APIs) ─────────────────────────────────────────────────
// These are the services that handle business logic and data access.
// They start automatically and connect to databases automatically.

// 🔐 Identity & Tenant API — Authentication Service
// Handles: User login, JWT tokens, multi-tenant isolation, role management
// API Docs (Scalar): http://localhost:5211/scalar/v1
var identityApi = builder.AddProject<Projects.GACS_IdentityTenant_Api>("identity-api")
                         .WithReference(gacsDb)
                         .WithReference(redis)
                         .WithReference(blobStorage)
                         .WithUrlForEndpoint("http", url =>
                         {
                             url.Url = "http://localhost:5211/scalar/v1";
                             url.DisplayText = "📖 API Documentation (Scalar)";
                         })
                         .WaitFor(sql)
                         .WaitFor(redis);

// 🚪 API Gateway — Entry Point
// Routes requests to the right service. All frontend calls go through here.
// Health check: http://localhost:5142/health
var gateway = builder.AddProject<Projects.GACS_Gateway>("gateway")
                     .WithReference(identityApi)
                     .WithReference(redis)
                     .WithExternalHttpEndpoints()
                     .WithUrlForEndpoint("http", url =>
                     {
                         url.Url = "http://localhost:5142/health";
                         url.DisplayText = "✅ Health Check";
                     })
                     .WaitFor(identityApi);

// ── Frontend Applications ────────────────────────────────────────────────────
// These are the user interfaces that people interact with.

// 🖥️ Admin Portal — Management Interface
// For: Property managers, Information Officers, Auditors
// Features: Visitor management, compliance reports, user administration
var webAdmin = builder.AddProject<Projects.GACS_Web_Admin>("admin-portal")
       .WithReference(gateway)
       .WithExternalHttpEndpoints()
       .WithUrlForEndpoint("http", url =>
       {
           url.Url = "http://localhost:5162";
           url.DisplayText = "🚀 Open Admin Portal";
       })
       .WaitFor(gateway);

// ═══════════════════════════════════════════════════════════════════════════════
// BUILD & RUN
// ═══════════════════════════════════════════════════════════════════════════════
// The dashboard will open automatically in your browser.
// Look for the friendly names above to understand what each resource does.
// ═══════════════════════════════════════════════════════════════════════════════

builder.Build().Run();
