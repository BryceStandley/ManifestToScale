using System.Text;
using FTG.Core.Logging;

namespace FTG.MAUI;


public interface IProgressReporter
{
    void ReportProgress(string message);
    void ReportError(string message, Exception? exception = null);
}

public class ProgressReporter(Action<string> updateUi) : IProgressReporter
{
    private readonly StringBuilder _uiOutput = new();

    public void ReportProgress(string message)
    {
        var timestampedMessage = $"{DateTime.Now:HH:mm:ss} - {message}";
        _uiOutput.AppendLine(timestampedMessage);
        
        // Update UI on main thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            updateUi(_uiOutput.ToString());
        });
        
        // Also log to file with more detail
        GlobalLogger.LogInfo(message);
    }
    
    public void ReportError(string message, Exception? exception = null)
    {
        var timestampedMessage = $"{DateTime.Now:HH:mm:ss} - ERROR: {message}";
        _uiOutput.AppendLine(timestampedMessage);
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            updateUi(_uiOutput.ToString());
        });
        
        // Log detailed error to file
        GlobalLogger.LogError(message, exception);
    }
}