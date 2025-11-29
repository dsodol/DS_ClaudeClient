namespace ClaudeDesktop.Models;

public class Snippet
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    public string Preview => Content.Length > 50
        ? Content.Substring(0, 50).Replace("\n", " ").Replace("\r", "") + "..."
        : Content.Replace("\n", " ").Replace("\r", "");
}
