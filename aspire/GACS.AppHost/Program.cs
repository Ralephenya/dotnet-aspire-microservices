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

// 📊 Prometheus — Metrics Collector
// What it does: Collects performance metrics (CPU, memory, request counts) from all services
// Why it matters: Helps you identify slow services, memory leaks, and performance bottlenecks
// How to use: Click the URL to view metrics graphs and query service performance
var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "v3.4.0")
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml",
              "--storage.tsdb.path=/prometheus",
              "--web.console.libraries=/usr/share/prometheus/console_libraries",
              "--web.console.templates=/usr/share/prometheus/consoles")
    .WithBindMount(prometheusConfig, "/etc/prometheus/prometheus.yml", isReadOnly: true)
    .WithHttpEndpoint(targetPort: 9090, name: "http", port: 9090)
    .WithUrlForEndpoint("http", url => url.DisplayText = "📊 View Metrics Dashboard")
    .WithLifetime(ContainerLifetime.Persistent);

var lokiConfig = Path.Combine(builder.AppHostDirectory,
    "observability", "loki", "loki-config.yml");

// 📝 Loki — Log Aggregator
// What it does: Collects and stores logs from all services in one place
// Why it matters: When something breaks, you need to see the error messages
// How to use: Access logs through Grafana (see below) — click Grafana URL, then Explore
var loki = builder.AddContainer("loki", "grafana/loki", "3.5.0")
    .WithArgs("-config.file=/etc/loki/local-config.yaml")
    .WithBindMount(lokiConfig, "/etc/loki/local-config.yaml", isReadOnly: true)
    .WithHttpEndpoint(targetPort: 3100, name: "http", port: 3100)
    // Note: Loki has no UI — access logs through Grafana's Explore panel
    .WithLifetime(ContainerLifetime.Persistent);

var grafanaProvisioning = Path.Combine(builder.AppHostDirectory,
    "observability", "grafana", "provisioning");

// 📈 Grafana — Visualization Dashboard
// What it does: Shows pretty charts and graphs from Prometheus metrics and Loki logs
// Why it matters: Easier to understand data than raw numbers or log text
// How to use: Click the URL to see dashboards. Use "Explore" to search logs or build custom queries
var grafana = builder.AddContainer("grafana", "grafana/grafana", "12.0.1")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")
    .WithEnvironment("GF_AUTH_DISABLE_LOGIN_FORM", "true")
    .WithBindMount(grafanaProvisioning, "/etc/grafana/provisioning", isReadOnly: true)
    .WithHttpEndpoint(targetPort: 3000, name: "http", port: 3000)
    .WithUrlForEndpoint("http", url => url.DisplayText = "📈 Open Grafana Dashboard")
    .WithLifetime(ContainerLifetime.Persistent);

// ── Data & Caching Infrastructure ──────────────────────────────────────────
// These are the databases and storage that power the application.
// Data persists even when you stop debugging (F5 cycles).

// 🔐 SQL Server Password
// Value is read from Parameters:sql-password in appsettings.json (local dev default)
// or from user secrets / env vars to override. See MASTER-ARCHITECTURE.md §Environments.
var sqlPassword = builder.AddParameter("sql-password", secret: true);

// 🗄️ SQL Server — Main Database
// What it does: Stores all persistent data (users, visitors, incidents, audit logs, compliance records)
// Why it matters: Without this, no data would be saved when you restart the application
// Connection string is automatically injected into services that need it.
//
// 🔐 Password Configuration:
// Default password: "Dev_Password1!" (from appsettings.json)
// For production: Use user secrets or environment variables
// See MASTER-ARCHITECTURE.md §Environments for details
//
// 📝 Environment Variables:
// ACCEPT_EULA=Y — Required to accept SQL Server license terms
// MSSQL_PID=Developer — Use Developer edition (free for local dev)
var sql = builder.AddSqlServer("sql-server", password: sqlPassword)
                 .WithEnvironment("ACCEPT_EULA", "Y")
                 .WithEnvironment("MSSQL_PID", "Developer")
                 .WithDataVolume("gacs-sql-data")
                 .WithLifetime(ContainerLifetime.Persistent);

var gacsDb = sql.AddDatabase("gacsdb", databaseName: "GACS");

// ⚡ Redis — Cache & Real-time
// What it does: Fast in-memory store for temporary data (sessions, real-time location updates, rate limiting)
// Why it matters: Much faster than database for data that changes frequently or doesn't need to persist
// Data here is temporary — it's cleared when you stop the container
//
// 🔒 TLS Security Note:
// Aspire automatically enables TLS for Redis in local development.
// This ensures secure communication between services and Redis.
// Connection strings are automatically configured with TLS settings.
// No manual appsettings configuration needed — Aspire handles it.
var redis = builder.AddRedis("redis")
                   .WithDataVolume("gacs-redis-data")
                   .WithLifetime(ContainerLifetime.Persistent);

// ☁️ Azure Storage Emulator — Blob Storage
// What it does: Stores files (visitor photos, documents, export files) like cloud storage
// Why it matters: Files are too big for the database — need specialized storage
// Runs locally as an emulator (Azurite) — no Azure account needed for dev.
var storage = builder.AddAzureStorage("azure-storage")
                     .RunAsEmulator();

var blobStorage = storage.AddBlobs("blob-storage");

// ── Backend Services (APIs) ─────────────────────────────────────────────────
// These are the services that handle business logic and data access.
// They start automatically and connect to databases automatically.

// 🔐 Identity & Tenant API — Authentication Service
// What it does: Handles user login, JWT tokens, multi-tenant isolation, role management
// Why it matters: Without this, users can't log in or access the system
// API Docs: Click the endpoint URL to see all available API endpoints (Scalar)
//
// ⏱️ Startup Delay:
// Added 30-second delay to ensure SQL Server is fully initialized before Hangfire tries to connect.
// This prevents "BackgroundServerProcess is in the Failed state" errors during startup.
var identityApi = builder.AddProject<Projects.GACS_IdentityTenant_Api>("identity-api")
                         .WithReference(gacsDb)
                         .WithReference(redis)
                         .WithReference(blobStorage)
                         .WithExternalHttpEndpoints()
                         .WithEnvironment("ASPNETCORE_STARTUP_DELAY", "30")
                         .WaitFor(sql)
                         .WaitFor(redis);

// 🚪 API Gateway — Entry Point
// What it does: Routes requests to the right service (Identity API, etc.)
// Why it matters: Frontends don't need to know which service handles what — gateway handles routing
// Health check: Click the endpoint URL and append /health to check if gateway is running
var gateway = builder.AddProject<Projects.GACS_Gateway>("gateway")
                     .WithReference(identityApi)
                     .WithReference(redis)
                     .WithExternalHttpEndpoints()
                     .WaitFor(identityApi);

// ── Frontend Applications ────────────────────────────────────────────────────
// These are the user interfaces that people interact with.

// 🖥️ Admin Portal — Management Interface
// What it does: Web app for property managers, Information Officers, and Auditors
// Why it matters: This is where users manage visitors, view compliance reports, and administer the system
// Features: Visitor management, compliance reports, user administration
// Landing page: Click the endpoint URL to open the admin portal in your browser
var webAdmin = builder.AddProject<Projects.GACS_Web_Admin>("admin-portal")
       .WithReference(gateway)
       .WithExternalHttpEndpoints()
       .WaitFor(gateway);

// ═══════════════════════════════════════════════════════════════════════════════
// BUILD & RUN
// ═══════════════════════════════════════════════════════════════════════════════
// The dashboard will open automatically in your browser.
// Look for the friendly names above to understand what each resource does.
// ═══════════════════════════════════════════════════════════════════════════════

builder.Build().Run();
