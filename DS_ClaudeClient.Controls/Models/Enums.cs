namespace DS_ClaudeClient.Controls.Models;

/// <summary>
/// Defines the keyboard shortcut used to send messages.
/// </summary>
public enum SendKeyMode
{
    /// <summary>
    /// Shift+Enter sends the message.
    /// </summary>
    ShiftEnter,

    /// <summary>
    /// Ctrl+Enter sends the message.
    /// </summary>
    CtrlEnter,

    /// <summary>
    /// Enter sends the message (Shift+Enter for new line).
    /// </summary>
    Enter
}

/// <summary>
/// Defines how snippets are sorted in the panel.
/// </summary>
public enum SnippetSortMode
{
    /// <summary>
    /// Custom order (drag-drop manual ordering).
    /// </summary>
    Custom,

    /// <summary>
    /// Sort by title alphabetically.
    /// </summary>
    Title,

    /// <summary>
    /// Sort by creation date.
    /// </summary>
    DateCreated
}
