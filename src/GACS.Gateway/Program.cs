using GACS.Gateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseMiddleware<ApiKeyValidationMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<UsageMeteringMiddleware>();

app.MapReverseProxy();
app.MapGet("/", () => Results.Redirect("/health"));

app.Run();
