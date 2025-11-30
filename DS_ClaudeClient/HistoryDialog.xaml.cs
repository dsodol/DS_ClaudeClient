using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace DS_ClaudeClient;

public class HistoryItem
{
    public string Content { get; set; } = string.Empty;
    public string Preview => Content.Length > 100 ? Content[..100] + "..." : Content;
}

public partial class HistoryDialog : Window
{
    public HistoryDialog(List<string> history)
    {
        InitializeComponent();

        var items = new List<HistoryItem>();
        for (int i = history.Count - 1; i >= 0; i--)
        {
            items.Add(new HistoryItem { Content = history[i] });
        }
        HistoryList.ItemsSource = items;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            Close();
        }
        else
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void HistoryList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (HistoryList.SelectedItem is HistoryItem item)
        {
            SelectedMessageText.Text = item.Content;
        }
        else
        {
            SelectedMessageText.Text = string.Empty;
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is HistoryItem item)
        {
            Clipboard.SetText(item.Content);
        }
    }
}
