using FTG.API.Auth;
using FTG.API.Config;
using Microsoft.AspNetCore.Http.Features;
using FTG.Core.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.Services.AddSingleton<IAuth, Auth>();
builder.Services.AddSingleton<IConfig, Configuration>();

var app = builder.Build();
GlobalLogger.Initialize(app.Services);


// Configure the HTTP request pipeline.
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
GlobalLogger.LogInfo("Fresh To Go Processor API started successfully.");

// Create an upload directory if it doesn't exist
if (!Directory.Exists(app.Services.GetService<IConfig>()?.GetUploadsPath()))
{
    var uploadsPath = app.Services.GetService<IConfig>()?.GetUploadsPath();
    if (uploadsPath != null)
    {
        Directory.CreateDirectory(uploadsPath);
        GlobalLogger.LogInfo($"Directory created: {uploadsPath}");
    }
}

if (!Directory.Exists(app.Services.GetService<IConfig>()?.GetOutputPath()))
{
    var outputPath = app.Services.GetService<IConfig>()?.GetOutputPath();
    if (outputPath != null)
    {
        Directory.CreateDirectory(outputPath);
        GlobalLogger.LogInfo($"Directory created: {outputPath}");
    }
}

if (!Directory.Exists(app.Services.GetService<IConfig>()?.GetFinishedPath()))
{
    var finishedPath = app.Services.GetService<IConfig>()?.GetFinishedPath();
    if (finishedPath != null)
    {
        Directory.CreateDirectory(finishedPath);
        GlobalLogger.LogInfo($"Directory created: {finishedPath}");
    }
}

app.Run();