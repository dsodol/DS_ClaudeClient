using System.IO;
using System.Text.Json;
using DS_ClaudeClient.Models;

namespace DS_ClaudeClient.Services;

public class SnippetService
{
    private readonly string _snippetsFilePath;

    public SnippetService()
    {
        _snippetsFilePath = GetSnippetsFilePath();
    }

    private string GetSnippetsFilePath()
    {
        // Priority: OneDrive > Documents > AppData
        var possiblePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", "DS_ClaudeClient"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DS_ClaudeClient"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DS_ClaudeClient")
        };

        // Check for existing snippets file
        foreach (var path in possiblePaths)
        {
            var filePath = Path.Combine(path, "snippets.json");
            if (File.Exists(filePath))
            {
                return filePath;
            }
        }

        // Check which directory exists (OneDrive preferred)
        foreach (var path in possiblePaths)
        {
            var parentDir = Path.GetDirectoryName(path);
            if (parentDir != null && Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(path);
                return Path.Combine(path, "snippets.json");
            }
        }

        // Fallback to AppData
        var fallbackPath = possiblePaths[2];
        Directory.CreateDirectory(fallbackPath);
        return Path.Combine(fallbackPath, "snippets.json");
    }

    public List<Snippet> Load()
    {
        try
        {
            if (File.Exists(_snippetsFilePath))
            {
                var json = File.ReadAllText(_snippetsFilePath);
                return JsonSerializer.Deserialize<List<Snippet>>(json) ?? new List<Snippet>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading snippets: {ex.Message}");
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
