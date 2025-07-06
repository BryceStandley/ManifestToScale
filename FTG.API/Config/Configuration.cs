namespace FTG.API.Config;



public interface IConfig
{
    string GetUploadsPath();
    string GetOutputPath();
    string GetFinishedPath();
}

public class Configuration(IConfiguration configuration, IWebHostEnvironment environment) : IConfig
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IWebHostEnvironment _environment = environment;

    public string GetUploadsPath()
    {
        return Path.Combine(_environment.ContentRootPath, "uploads");
    }

    public string GetOutputPath()
    {
        return Path.Combine(_environment.ContentRootPath, "output");
    }

    public string GetFinishedPath()
    {
        return Path.Combine(_environment.ContentRootPath, "complete");
    }
}