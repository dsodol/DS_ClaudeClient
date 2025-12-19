using System.Windows;
using System.Windows.Input;
using DS_ClaudeClient.Controls.Models;

namespace DS_ClaudeClient.Controls.Dialogs;

/// <summary>
/// Dialog for creating or editing a snippet.
/// </summary>
public partial class SnippetEditDialog : Window
{
    /// <summary>
    /// Gets the snippet being edited.
    /// </summary>
    public Snippet Snippet { get; private set; }

    /// <summary>
    /// Gets whether this is a new snippet (vs editing existing).
    /// </summary>
    public bool IsNew { get; }

    /// <summary>
    /// Creates a new SnippetEditDialog for creating a new snippet.
    /// </summary>
    public SnippetEditDialog()
    {
        InitializeComponent();
        Snippet = new Snippet();
        IsNew = true;
        DialogTitle.Text = "New Snippet";
        TitleTextBox.Focus();
    }

    /// <summary>
    /// Creates a new SnippetEditDialog for editing an existing snippet.
    /// </summary>
    /// <param name="snippet">The snippet to edit.</param>
    public SnippetEditDialog(Snippet snippet)
    {
        InitializeComponent();
        Snippet = snippet;
        IsNew = false;
        DialogTitle.Text = "Edit Snippet";
        TitleTextBox.Text = snippet.Title;
        ContentTextBox.Text = snippet.Content;
        TitleTextBox.Focus();
        TitleTextBox.SelectAll();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            DragMove();
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        Snippet.Title = TitleTextBox.Text.Trim();
        Snippet.Content = ContentTextBox.Text;
        Snippet.ModifiedAt = DateTime.UtcNow;

        if (IsNew)
        {
            Snippet.CreatedAt = DateTime.UtcNow;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
