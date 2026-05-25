using GACS.Shared.Errors;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddGacsExceptionHandling();

// ═══════════════════════════════════════════════════════════════════════════════
// Authentication Configuration
// ═══════════════════════════════════════════════════════════════════════════════
// In development, authentication is disabled to allow the landing page to load
// without requiring Azure AD configuration. In production, authentication is required.
// To enable authentication in development, set AzureAd:Enabled to true in appsettings.
// ═══════════════════════════════════════════════════════════════════════════════
var enableAuth = builder.Configuration.GetValue<bool>("AzureAd:Enabled", !builder.Environment.IsDevelopment());

if (enableAuth)
{
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
    builder.Services.AddControllersWithViews()
        .AddMicrosoftIdentityUI();
}
else
{
    // Development mode: No authentication required
    builder.Services.AddControllersWithViews();
}

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

if (enableAuth)
{
    builder.Services.AddMicrosoftIdentityConsentHandler();
}

builder.Services.AddFluentUIComponents();

builder.Services.AddHttpClient("gacs-gateway", client =>
{
    client.BaseAddress = new Uri("https+http://gacs-gateway");
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseExceptionHandler(app.Environment.IsDevelopment() ? "/Error" : "/Error");
app.UseStaticFiles();
app.UseRouting();

if (enableAuth)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
