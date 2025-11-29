using System.Windows;
using System.Windows.Input;

namespace ClaudeDesktop;

public partial class SnippetEditDialog : Window
{
    public string SnippetTitle => TitleTextBox.Text.Trim();
    public string SnippetContent => ContentTextBox.Text;

    public SnippetEditDialog(string? title = null, string? content = null)
    {
        InitializeComponent();

        if (!string.IsNullOrEmpty(title))
        {
            DialogTitle.Text = "Edit Snippet";
            TitleTextBox.Text = title;
            ContentTextBox.Text = content ?? string.Empty;
        }

        Loaded += (s, e) => TitleTextBox.Focus();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
        {
            MessageBox.Show("Please enter a title for the snippet.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            TitleTextBox.Focus();
            return;
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
