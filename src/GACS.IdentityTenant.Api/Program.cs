using Asp.Versioning;
using GACS.Shared.Errors;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Identity.Web;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════════
// Startup Delay for Database Readiness
// ═══════════════════════════════════════════════════════════════════════════════
// When running under Aspire, SQL Server may take time to initialize.
// This delay gives the database time to be ready before Hangfire tries to connect.
// ═══════════════════════════════════════════════════════════════════════════════
var startupDelaySeconds = builder.Configuration.GetValue<int>("ASPNETCORE_STARTUP_DELAY", 0);
if (startupDelaySeconds > 0)
{
    Console.WriteLine($"⏱️  Delaying startup by {startupDelaySeconds} seconds to allow database initialization...");
    await Task.Delay(TimeSpan.FromSeconds(startupDelaySeconds));
    Console.WriteLine("✅ Startup delay complete, proceeding with application startup.");
}

builder.AddServiceDefaults();
builder.Services.AddGacsExceptionHandling();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ═══════════════════════════════════════════════════════════════════════════════
// Hangfire Configuration — Background Job Processing
// ═══════════════════════════════════════════════════════════════════════════════
// Hangfire needs a SQL Server database to store jobs and execution history.
// The connection string is automatically provided by Aspire from the gacsdb resource.
// PrepareSchemaIfNotExists ensures the Hangfire tables are created automatically.
// ═══════════════════════════════════════════════════════════════════════════════
var connectionString = builder.Configuration.GetConnectionString("gacsdb");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'gacsdb' is not configured. Ensure Aspire is running and the database resource is available.");
}

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        JobExpirationCheckInterval = TimeSpan.FromHours(1),
        CountersAggregateInterval = TimeSpan.FromMinutes(5),
        DashboardJobListLimit = 50000,
        SchemaName = "hangfire"
    }));

builder.Services.AddHangfireServer();

builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "GACS Identity & Tenant API";
        options.AddPreferredSecuritySchemes("Bearer");
    });
    app.UseHangfireDashboard("/hangfire");
}

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
