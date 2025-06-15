using FTG_PDF_API;
using Microsoft.AspNetCore.Http.Features;
using FTG_PDF_API.Logging;


await StartWebApiApplication([]);

static IConfiguration BuildConfiguration()
{
    return new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build();
}

static async Task StartMauiApplication()
{
    //var app = MauiProgram.CreateMauiApp();
    //await app.RunAsync();
}

static async Task StartWebApiApplication(string[] args)
{

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddOpenApi();
    builder.Services.AddControllers();
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100MB
        options.ValueLengthLimit = int.MaxValue;
        options.MultipartHeadersLengthLimit = int.MaxValue;
    });

    builder.Services.AddLogging();
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();
    GlobalLogger.Initialize(app.Services);

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseRouting();
    app.MapControllers();

    GlobalLogger.LogInfo("Fresh To Go PDF API started successfully.");
    app.Run();
}
