using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;

namespace GACS.Mobile.Guard;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.UseMaui()
               .ConfigureFonts(fonts =>
               {
                   fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
               });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddFluentUIComponents();

        // Gate API client via gateway
        // TODO: Inject base URL from configuration (not hardcoded)
        builder.Services.AddScoped(sp => new HttpClient());

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
