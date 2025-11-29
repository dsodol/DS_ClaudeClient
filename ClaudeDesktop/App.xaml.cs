using System.Windows;

namespace ClaudeDesktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Ensure WebView2 runtime is available
        try
        {
            var version = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
            System.Diagnostics.Debug.WriteLine($"WebView2 Runtime Version: {version}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "WebView2 Runtime is not installed. Please install it from:\nhttps://developer.microsoft.com/en-us/microsoft-edge/webview2/",
                "WebView2 Required",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
