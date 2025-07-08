using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FTG.Updater;

public partial class MainPage : ContentPage
{
    private readonly UpdaterService _updaterService;
    private string _statusText = "Checking for updates...";
    private float _progressValue = 0;
    private string _outputInfo = string.Empty;
    private bool _launchEnabled = false;
    private string? _appDirectory;
    
    public MainPage()
    {
        InitializeComponent();
        _updaterService = new UpdaterService();
        BindingContext = this;
        OutputScrollView.PropertyChanged += OutputScrollView_PropertyChanged;
        _appDirectory = _updaterService.CurrentAppDirectory;
        Loaded += MainPage_Loaded;
    }
    
    private async void OutputScrollView_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OutputInfo))
        {
            // Scroll to bottom after UI updates
            await Dispatcher.DispatchAsync(async () =>
            {
                await Task.Delay(50); // Small delay to ensure content is rendered
                await OutputScrollView.ScrollToAsync(0, double.MaxValue, false);
            });
        }
    }
    
    public bool LaunchEnabled
    {
        get => _launchEnabled;
        set
        {
            _launchEnabled = value;
            OnPropertyChanged(nameof(LaunchEnabled));
        }
    }
    
    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
            OnPropertyChanged(nameof(StatusText));
        }
    }
    
    
    public float ProgressValue
    {
        get => _progressValue;
        set
        {
            _progressValue = value;
            OnPropertyChanged(nameof(ProgressValue));
        }
    }
    
    public bool HasOutputInfo => !string.IsNullOrWhiteSpace(OutputInfo);
    public string OutputInfo
    {
        get => _outputInfo;
        set
        {
            _outputInfo = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasOutputInfo));
        }
    }
    
    private async void MainPage_Loaded(object? sender, EventArgs e)
    {
        try
        {
            StatusText = "Checking for updates...";

            var res = await _updaterService.CheckForUpdatesAsync(OutputInfo);
            OutputInfo += res.OutputInfo;
            if (res.Result)
            {
                StatusText = "Update available. Downloading...";
                OutputInfo += "Update available. Downloading...\n";
                
                var progress = new Progress<float>(value => 
                {
                    ProgressValue = value;
                });
                
                var success = await _updaterService.DownloadAndInstallUpdateAsync(progress, OutputInfo);
                OutputInfo += success.OutputInfo;
                
                if (success.Result)
                {
                    StatusText = "Update completed successfully!";
                }
                else
                {
                    StatusText = "Update failed... Launching current version.";
                    LaunchButton_Click(sender, e);
                }
            }
            else
            {
                StatusText = "No updates available.";
            }
        }
        catch (Exception ex)
        {
            StatusText = "Update check failed... Launching current version.";
            OutputInfo = $"Error: {ex.Message}\n{ex.StackTrace}\n";
            LaunchButton_Click(sender, e);
        }
        finally
        {
            LaunchEnabled = true;
            ProgressValue = 1.0f;
        }
    }
    
    private void LaunchButton_Click(object? sender, EventArgs e)
    {
        LaunchMainApp();
        
        Application.Current?.Quit();
    }
    
    private void LaunchMainApp()
    {
        var appPath = Path.Combine(
            Path.GetDirectoryName(Environment.ProcessPath) ?? string.Empty,
            "../app",
            "mts.exe");
        OutputInfo += $"App Directory: {_appDirectory}\n";
        OutputInfo += $"Launching {appPath}\n";
        
        if (File.Exists(appPath))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = appPath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(appPath),
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal
            };
            
        
            Process.Start(startInfo);
        }
    }
}