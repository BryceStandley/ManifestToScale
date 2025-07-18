﻿using System.Reflection;
using Microsoft.Extensions.Logging;

namespace FTG.Updater;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = AppTheme.Dark;
        }
        
        return builder.Build();
    }
    
    
}