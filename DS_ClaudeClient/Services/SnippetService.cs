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
        var snippets = JsonSerializer.Deserialize<List<Snippet>>(json) ?? new List<Snippet>();

        // Assign new IDs to avoid conflicts
        foreach (var snippet in snippets)
        {
            snippet.Id = Guid.NewGuid().ToString();
        }

        return snippets;
    }

    public void Export(List<Snippet> snippets, string filePath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(snippets, options);
        File.WriteAllText(filePath, json);
    }

    private List<Snippet> GetDefaultSnippets()
    {
        return new List<Snippet>();
    }
}
