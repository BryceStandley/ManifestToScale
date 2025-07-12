namespace FTG.Updater;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        if (Application.Current != null) Application.Current.UserAppTheme = AppTheme.Dark;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell())
        {
            // Set window size
            Width = 800,
            Height = 600,
            // Optional: Set minimum size
            MinimumWidth = 800,
            MinimumHeight = 600,
            // Optional: Set maximum size
            MaximumWidth = 1920,
            MaximumHeight = 1080,
            // Optional: Center the window
            X = -1, // -1 centers horizontally
            Y = -1 // -1 centers vertically
        };

        return window;
    }
}