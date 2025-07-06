using System;
using System.Collections.Generic;
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
    
    public MainPage()
    {
        InitializeComponent();
        _updaterService = new UpdaterService();
        BindingContext = this;
        Loaded += MainPage_Loaded;
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
            
            if (await _updaterService.CheckForUpdatesAsync())
            {
                StatusText = "Update available. Downloading...";
                
                var progress = new Progress<float>(value => 
                {
                    ProgressValue = value;
                });
                
                var success = await _updaterService.DownloadAndInstallUpdateAsync(progress);
                
                if (success)
                {
                    StatusText = "Update completed successfully!";
                }
                else
                {
                    StatusText = "Update failed. Launching current version.";
                }
            }
            else
            {
                StatusText = "No updates available.";
            }
        }
        catch (Exception ex)
        {
            StatusText = "Update check failed. Launching current version.";
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
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
            "mts.exe");
        
        if (File.Exists(appPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = appPath,
                UseShellExecute = true
            });
        }
    }
}