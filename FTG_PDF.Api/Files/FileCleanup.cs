namespace FTG_PDF.Api.Files;

public class FileCleanup
{
    public static void CleanupFiles(string directoryPath, int daysToKeep = 7)
    {
        if (!Directory.Exists(directoryPath))
        {
            return; // Directory does not exist, nothing to clean up
        }

        var files = Directory.GetFiles(directoryPath);
        var thresholdDate = DateTime.Now.AddDays(-daysToKeep);

        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            if (fileInfo.LastWriteTime < thresholdDate)
            {
                try
                {
                    fileInfo.Delete();
                }
                catch (Exception ex)
                {
                    // Log the exception or handle it as needed
                    Console.WriteLine($"Error deleting file {file}: {ex.Message}");
                }
            }
        }
    }
    
}