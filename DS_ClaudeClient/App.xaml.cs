using System.IO;
using System.Windows;

namespace DS_ClaudeClient;

public partial class App : Application
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DS_ClaudeClient", "app.log");

    public static void Log(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}{Environment.NewLine}";
            File.AppendAllText(LogPath, logMessage);
        }
        catch { }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log("Application starting...");

        // Global exception handlers
        DispatcherUnhandledException += (s, args) =>
        {
            Log($"UNHANDLED UI EXCEPTION: {args.Exception}");
            MessageBox.Show($"An error occurred:\n{args.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Log($"UNHANDLED DOMAIN EXCEPTION: {ex}");
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            Log($"UNOBSERVED TASK EXCEPTION: {args.Exception}");
            args.SetObserved();
        };

        // Ensure WebView2 runtime is available
        try
        {
            var version = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
            Log($"WebView2 Runtime Version: {version}");
        }
        catch (Exception ex)
        {
            Log($"WebView2 not available: {ex.Message}");
            MessageBox.Show(
                "WebView2 Runtime is not installed. Please install it from:\nhttps://developer.microsoft.com/en-us/microsoft-edge/webview2/",
                "WebView2 Required",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }

        Log("Application startup complete");
    }
}
