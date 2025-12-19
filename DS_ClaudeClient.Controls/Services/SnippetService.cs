using System.Text.Json;
using DS_ClaudeClient.Controls.Models;

namespace DS_ClaudeClient.Controls.Services;

/// <summary>
/// Service for loading, saving, importing, and exporting snippets.
/// </summary>
public class SnippetService
{
    private string _snippetsFilePath;

    /// <summary>
    /// Gets the current snippets file path.
    /// </summary>
    public string SnippetsFilePath => _snippetsFilePath;

    /// <summary>
    /// Creates a new SnippetService with optional custom path.
    /// Uses default path if not specified.
    /// </summary>
    /// <param name="customPath">Optional custom file path for snippets.</param>
    public SnippetService(string? customPath = null)
    {
        _snippetsFilePath = ResolveFilePath(customPath);
    }

    /// <summary>
    /// Sets a new file path for snippets storage.
    /// </summary>
    /// <param name="path">The new file path.</param>
    public void SetFilePath(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            _snippetsFilePath = path;
        }
    }

    /// <summary>
    /// Gets the default snippets file path.
    /// </summary>
    public static string GetDefaultFilePath()
    {
        return ControlsConfig.GetSnippetsPath();
    }

    private string ResolveFilePath(string? customPath)
    {
        // Use custom path if provided and file exists
        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
        {
            return customPath;
        }

        // Check default path
        var defaultPath = GetDefaultFilePath();
        if (File.Exists(defaultPath))
        {
            return defaultPath;
        }

        // Check legacy locations for migration (OneDrive, Documents, etc.)
        var legacyPaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", "ds_snippets.json"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", "DS_ClaudeClient", "snippets.json"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DS_ClaudeClient", "snippets.json"),
        };

        foreach (var legacyPath in legacyPaths)
        {
            if (File.Exists(legacyPath))
            {
                return legacyPath;
            }
        }

        // Return default path (will be created when saving)
        return defaultPath;
    }

    /// <summary>
    /// Loads snippets from the configured file path.
    /// </summary>
    /// <returns>List of snippets, or empty list if file doesn't exist.</returns>
    public List<Snippet> Load()
    {
        try
        {
            if (File.Exists(_snippetsFilePath))
            {
                var json = File.ReadAllText(_snippetsFilePath);

                // Detect format: Text/Description (legacy) or Title/Content (native)
                if (json.Contains("\"Text\"") && json.Contains("\"Description\""))
                {
                    // Load from Text/Description format
                    var importItems = JsonSerializer.Deserialize<List<TextDescriptionFormat>>(json) ?? [];
                    return importItems.Select(item => new Snippet
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = item.Description ?? "",
                        Content = item.Text ?? "",
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow
                    }).ToList();
                }
                else
                {
                    // Load from native format (Title/Content)
                    return JsonSerializer.Deserialize<List<Snippet>>(json) ?? [];
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SnippetService] Error loading snippets: {ex.Message}");
        }

        return [];
    }

    /// <summary>
    /// Saves snippets to the configured file path.
    /// </summary>
    /// <param name="snippets">List of snippets to save.</param>
    public void Save(List<Snippet> snippets)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(snippets, options);

            var directory = Path.GetDirectoryName(_snippetsFilePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_snippetsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SnippetService] Error saving snippets: {ex.Message}");
        }
    }

    /// <summary>
    /// Imports snippets from a file.
    /// </summary>
    /// <param name="filePath">Path to the import file.</param>
    /// <returns>List of imported snippets with new IDs.</returns>
    public List<Snippet> Import(string filePath)
    {
        var json = File.ReadAllText(filePath);

        // Try to detect format: Text/Description or Title/Content
        if (json.Contains("\"Text\"") && json.Contains("\"Description\""))
        {
            // Import from Text/Description format
            var importItems = JsonSerializer.Deserialize<List<TextDescriptionFormat>>(json) ?? [];
            return importItems.Select(item => new Snippet
            {
                Id = Guid.NewGuid().ToString(),
                Title = item.Description ?? item.Text ?? "",
                Content = item.Text ?? "",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            }).ToList();
        }
        else
        {
            // Import from native format (Title/Content)
            var snippets = JsonSerializer.Deserialize<List<Snippet>>(json) ?? [];
            foreach (var snippet in snippets)
            {
                snippet.Id = Guid.NewGuid().ToString();
            }
            return snippets;
        }
    }

    /// <summary>
    /// Exports snippets to a file in Text/Description format for compatibility.
    /// </summary>
    /// <param name="snippets">Snippets to export.</param>
    /// <param name="filePath">Destination file path.</param>
    public void Export(List<Snippet> snippets, string filePath)
    {
        var exportItems = snippets.Select(s => new TextDescriptionFormat
        {
            Text = s.Content,
            Description = s.Title
        }).ToList();

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(exportItems, options);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Helper class for Text/Description format compatibility.
    /// </summary>
    private class TextDescriptionFormat
    {
        public string? Text { get; set; }
        public string? Description { get; set; }
    }
}
