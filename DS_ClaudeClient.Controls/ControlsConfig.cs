namespace DS_ClaudeClient.Controls;

/// <summary>
/// Shared configuration for DS_ClaudeClient controls.
/// Provides zero-config defaults that can be overridden.
/// </summary>
public static class ControlsConfig
{
    /// <summary>
    /// Default application name used for data folder.
    /// </summary>
    public static string ApplicationName { get; set; } = "DS_ClaudeClient";

    /// <summary>
    /// Default data folder path. Uses %LocalAppData%/{ApplicationName}/ by default.
    /// </summary>
    public static string DefaultDataFolderPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ApplicationName);

    /// <summary>
    /// Default WebView2 user data subfolder name.
    /// </summary>
    public static string WebView2FolderName { get; set; } = "WebView2";

    /// <summary>
    /// Default snippets file name.
    /// </summary>
    public static string DefaultSnippetsFileName { get; set; } = "snippets.json";

    /// <summary>
    /// Default settings file name.
    /// </summary>
    public static string DefaultSettingsFileName { get; set; } = "settings.json";

    /// <summary>
    /// Gets the full path to the WebView2 user data folder.
    /// </summary>
    public static string GetWebView2Path(string? dataFolderPath = null)
    {
        var basePath = dataFolderPath ?? DefaultDataFolderPath;
        return Path.Combine(basePath, WebView2FolderName);
    }

    /// <summary>
    /// Gets the full path to the snippets file.
    /// Default: OneDrive/ds_snippets.json (matches main app for cloud sync)
    /// </summary>
    public static string GetSnippetsPath(string? dataFolderPath = null, string? fileName = null)
    {
        if (dataFolderPath != null)
        {
            var file = fileName ?? DefaultSnippetsFileName;
            return Path.Combine(dataFolderPath, file);
        }

        // Default to OneDrive path (matches main app)
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "OneDrive", "ds_snippets.json");
    }

    /// <summary>
    /// Gets the full path to the settings file.
    /// </summary>
    public static string GetSettingsPath(string? dataFolderPath = null, string? fileName = null)
    {
        var basePath = dataFolderPath ?? DefaultDataFolderPath;
        var file = fileName ?? DefaultSettingsFileName;
        return Path.Combine(basePath, file);
    }

    /// <summary>
    /// Ensures the data folder exists.
    /// </summary>
    public static void EnsureDataFolderExists(string? dataFolderPath = null)
    {
        var path = dataFolderPath ?? DefaultDataFolderPath;
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
