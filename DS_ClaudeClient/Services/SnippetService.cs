using System.IO;
using System.Text.Json;
using DS_ClaudeClient.Models;

namespace DS_ClaudeClient.Services;

public class SnippetService
{
    private string _snippetsFilePath;

    public string SnippetsFilePath => _snippetsFilePath;

    public SnippetService(string? customPath = null)
    {
        _snippetsFilePath = GetSnippetsFilePath(customPath);
    }

    public void SetFilePath(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            _snippetsFilePath = path;
        }
    }

    public static string GetDefaultFilePath()
    {
        var oneDrivePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", "ds_snippets.json");
        return oneDrivePath;
    }

    private string GetSnippetsFilePath(string? customPath)
    {
        // Use custom path if provided
        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
        {
            return customPath;
        }

        // Default: OneDrive/ds_snippets.json
        var defaultPath = GetDefaultFilePath();

        // Check if default file exists
        if (File.Exists(defaultPath))
        {
            return defaultPath;
        }

        // Check legacy locations for migration
        var legacyPaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", "DS_ClaudeClient", "snippets.json"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DS_ClaudeClient", "snippets.json"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DS_ClaudeClient", "snippets.json")
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

    public List<Snippet> Load()
    {
        try
        {
            if (File.Exists(_snippetsFilePath))
            {
                var json = File.ReadAllText(_snippetsFilePath);

                // Detect format: Text/Description or Title/Content
                if (json.Contains("\"Text\"") && json.Contains("\"Description\""))
                {
                    // Load from Text/Description format
                    var importItems = JsonSerializer.Deserialize<List<TextDescriptionImport>>(json) ?? new List<TextDescriptionImport>();
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
                    return JsonSerializer.Deserialize<List<Snippet>>(json) ?? new List<Snippet>();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SnippetService] Error loading snippets: {ex.Message}");
        }

        return GetDefaultSnippets();
    }

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
            System.Diagnostics.Debug.WriteLine($"Error saving snippets: {ex.Message}");
        }
    }

    public List<Snippet> Import(string filePath)
    {
        var json = File.ReadAllText(filePath);

        // Try to detect format: Text/Description or Title/Content
        if (json.Contains("\"Text\"") && json.Contains("\"Description\""))
        {
            // Import from Text/Description format
            var importItems = JsonSerializer.Deserialize<List<TextDescriptionImport>>(json) ?? new List<TextDescriptionImport>();
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
            var snippets = JsonSerializer.Deserialize<List<Snippet>>(json) ?? new List<Snippet>();
            foreach (var snippet in snippets)
            {
                snippet.Id = Guid.NewGuid().ToString();
            }
            return snippets;
        }
    }

    public void Export(List<Snippet> snippets, string filePath)
    {
        // Export in Text/Description format
        var exportItems = snippets.Select(s => new TextDescriptionExport
        {
            Text = s.Content,
            Description = s.Title
        }).ToList();

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(exportItems, options);
        File.WriteAllText(filePath, json);
    }

    // Helper classes for Text/Description format
    private class TextDescriptionImport
    {
        public string? Text { get; set; }
        public string? Description { get; set; }
    }

    private class TextDescriptionExport
    {
        public string Text { get; set; } = "";
        public string Description { get; set; } = "";
    }

    private List<Snippet> GetDefaultSnippets()
    {
        return new List<Snippet>();
    }
}
