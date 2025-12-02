using System.Text;
using System.Windows;
using System.Windows.Input;
using DS_ClaudeClient.Models;
using DS_ClaudeClient.Services;

namespace DS_ClaudeClient;

public partial class LogWindow : Window
{
    private readonly SnippetService _snippetService;
    private readonly List<Snippet> _snippets;

    public LogWindow(SnippetService snippetService, List<Snippet> snippets)
    {
        InitializeComponent();
        _snippetService = snippetService;
        _snippets = snippets;
        RefreshLog();
    }

    private void RefreshLog()
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== Snippets Loading Log ===");
        sb.AppendLine();
        sb.AppendLine($"Snippets File Path: {_snippetService.SnippetsFilePath}");
        sb.AppendLine($"File Exists: {System.IO.File.Exists(_snippetService.SnippetsFilePath)}");
        sb.AppendLine();
        sb.AppendLine($"Total Snippets Loaded: {_snippets.Count}");
        sb.AppendLine();
        sb.AppendLine("--- Snippet Details ---");
        sb.AppendLine();

        int index = 1;
        foreach (var snippet in _snippets)
        {
            bool isEmpty = string.IsNullOrWhiteSpace(snippet.Title) && string.IsNullOrWhiteSpace(snippet.Content);

            sb.AppendLine($"[{index}] ID: {snippet.Id}");
            sb.AppendLine($"    Title: {(string.IsNullOrWhiteSpace(snippet.Title) ? "(empty)" : snippet.Title)}");
            sb.AppendLine($"    Content Length: {snippet.Content?.Length ?? 0} chars");
            sb.AppendLine($"    Content Preview: {GetPreview(snippet.Content, 80)}");
            sb.AppendLine($"    Created: {snippet.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"    Modified: {snippet.ModifiedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"    Order: {snippet.Order}");
            sb.AppendLine($"    Is Empty: {isEmpty}");
            sb.AppendLine();
            index++;
        }

        if (_snippets.Count == 0)
        {
            sb.AppendLine("(No snippets loaded)");
        }

        LogTextBox.Text = sb.ToString();
    }

    private static string GetPreview(string? content, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "(empty)";

        var preview = content.Replace("\r", "").Replace("\n", " ");
        if (preview.Length > maxLength)
            preview = preview.Substring(0, maxLength) + "...";

        return preview;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshLog();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(LogTextBox.Text);
        MessageBox.Show("Log copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
