namespace FTG.API.Config;



public interface IConfig
{
    string GetUploadsPath();
    string GetOutputPath();
    string GetFinishedPath();
}

public class Configuration(IWebHostEnvironment environment) : IConfig
{
    public string GetUploadsPath()
    {
        return Path.Combine(environment.ContentRootPath, "uploads");
    }

    public string GetOutputPath()
    {
        return Path.Combine(environment.ContentRootPath, "output");
    }

    public string GetFinishedPath()
    {
        return Path.Combine(environment.ContentRootPath, "complete");
    }
}