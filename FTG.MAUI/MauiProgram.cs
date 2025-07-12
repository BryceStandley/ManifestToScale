using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using Serilog;
using CommunityToolkit.Maui;
using FTG.Core.Logging;
using Serilog;

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
        
        // Configure Serilog for file logging
        var appDataPath = Environment.CurrentDirectory;
        var logDirectory = Path.Combine(appDataPath, "Logs");
        Directory.CreateDirectory(logDirectory);
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: Path.Combine(logDirectory, "mts-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Level:u4}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console()
            .CreateLogger();

        builder.Logging.AddSerilog();

        // Register your services
        builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
        
#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        GlobalLogger.Initialize(app.Services);
        
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = AppTheme.Dark;
        }

        return app;
    }
    
    
}