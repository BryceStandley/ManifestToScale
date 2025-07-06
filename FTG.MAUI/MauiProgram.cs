using System.Reflection;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using FTG.Core.Logging;

namespace FTG.MAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddLogging();
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        
#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        GlobalLogger.Initialize(app.Services);

        return app;
    }
    
    
}