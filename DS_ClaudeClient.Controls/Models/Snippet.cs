namespace DS_ClaudeClient.Controls.Models;

/// <summary>
/// Represents a text snippet that can be inserted into Claude's input.
/// </summary>
public class Snippet
{
    /// <summary>
    /// Unique identifier for the snippet.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Display title for the snippet.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The actual snippet content/text.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// When the snippet was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the snippet was last modified.
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Custom sort order for manual ordering.
    /// </summary>
    public int Order { get; set; } = 0;

    /// <summary>
    /// Preview of the content for display in lists.
    /// </summary>
    public string Preview => Content.Length > 100
        ? Content[..100].Replace("\n", " ").Replace("\r", "") + "..."
        : Content.Replace("\n", " ").Replace("\r", "");
}
