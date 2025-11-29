using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClaudeDesktop.Models;
using ClaudeDesktop.Services;
using Microsoft.Web.WebView2.Core;

namespace ClaudeDesktop;

public partial class MainWindow : Window
{
    private readonly SnippetService _snippetService;
    private readonly SettingsService _settingsService;
    private List<Snippet> _allSnippets = new();
    private bool _isSnippetsPanelVisible = true;

    public MainWindow()
    {
        InitializeComponent();

        _snippetService = new SnippetService();
        _settingsService = new SettingsService();

        Loaded += MainWindow_Loaded;
        ClaudeWebView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
        ClaudeWebView.NavigationCompleted += WebView_NavigationCompleted;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Load settings
        var settings = _settingsService.Load();
        FontSizeSlider.Value = settings.FontSize;
        Topmost = settings.AlwaysOnTop;
        UpdateTopmostButton();

        if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
        {
            Width = settings.WindowWidth;
            Height = settings.WindowHeight;
        }

        _isSnippetsPanelVisible = settings.SnippetsPanelVisible;
        SnippetsPanel.Visibility = _isSnippetsPanelVisible ? Visibility.Visible : Visibility.Collapsed;

        // Load snippets
        _allSnippets = _snippetService.Load();
        RefreshSnippetsList();

        // Initialize WebView2
        await ClaudeWebView.EnsureCoreWebView2Async();
    }

    private void WebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            // Configure WebView2 settings
            ClaudeWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            ClaudeWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            ClaudeWebView.CoreWebView2.Settings.IsZoomControlEnabled = true;

            // Apply font size
            ApplyFontSize((int)FontSizeSlider.Value);
        }
    }

    private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            // Inject enhancement scripts after page loads
            await InjectEnhancementScripts();
            ApplyFontSize((int)FontSizeSlider.Value);
        }
    }

    private async Task InjectEnhancementScripts()
    {
        var script = GetEmbeddedScript("ClaudeEnhancements.js");
        if (!string.IsNullOrEmpty(script))
        {
            await ClaudeWebView.CoreWebView2.ExecuteScriptAsync(script);
        }
    }

    private string GetEmbeddedScript(string scriptName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"ClaudeDesktop.Scripts.{scriptName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            // Return inline script if embedded resource not found
            return GetInlineEnhancementScript();
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private string GetInlineEnhancementScript()
    {
        return """
            (function() {
                // Multi-line entry enhancement
                function enhanceTextarea() {
                    const textareas = document.querySelectorAll('textarea, [contenteditable="true"], div[role="textbox"]');
                    textareas.forEach(textarea => {
                        if (textarea.dataset.enhanced) return;
                        textarea.dataset.enhanced = 'true';

                        // Make textarea expandable
                        if (textarea.tagName === 'TEXTAREA') {
                            textarea.style.minHeight = '100px';
                            textarea.style.maxHeight = '400px';
                            textarea.style.resize = 'vertical';
                        }

                        // Handle Shift+Enter for new line
                        textarea.addEventListener('keydown', function(e) {
                            if (e.key === 'Enter' && e.shiftKey) {
                                e.stopPropagation();
                                // Allow default behavior for new line
                            }
                        });
                    });
                }

                // Run on page load and observe for dynamic content
                enhanceTextarea();

                const observer = new MutationObserver(() => {
                    enhanceTextarea();
                });

                observer.observe(document.body, {
                    childList: true,
                    subtree: true
                });

                // Expose function to insert snippet text
                window.insertSnippetText = function(text) {
                    const activeElement = document.activeElement;
                    const textInputs = document.querySelectorAll('textarea, [contenteditable="true"], div[role="textbox"]');

                    // Find the main input area
                    let targetInput = null;
                    textInputs.forEach(input => {
                        if (input.closest('form') || input.closest('[data-testid]')) {
                            targetInput = input;
                        }
                    });

                    if (!targetInput && textInputs.length > 0) {
                        targetInput = textInputs[textInputs.length - 1];
                    }

                    if (targetInput) {
                        if (targetInput.tagName === 'TEXTAREA' || targetInput.tagName === 'INPUT') {
                            const start = targetInput.selectionStart;
                            const end = targetInput.selectionEnd;
                            const value = targetInput.value;
                            targetInput.value = value.substring(0, start) + text + value.substring(end);
                            targetInput.selectionStart = targetInput.selectionEnd = start + text.length;
                            targetInput.dispatchEvent(new Event('input', { bubbles: true }));
                        } else {
                            // ContentEditable or div with role="textbox"
                            const selection = window.getSelection();
                            if (selection.rangeCount > 0) {
                                const range = selection.getRangeAt(0);
                                range.deleteContents();
                                range.insertNode(document.createTextNode(text));
                                range.collapse(false);
                            } else {
                                targetInput.textContent += text;
                            }
                            targetInput.dispatchEvent(new Event('input', { bubbles: true }));
                        }
                        targetInput.focus();
                    }
                };

                console.log('Claude Desktop enhancements loaded');
            })();
            """;
    }

    private async void ApplyFontSize(int fontSize)
    {
        if (ClaudeWebView.CoreWebView2 == null) return;

        var script = $@"
            (function() {{
                document.body.style.fontSize = '{fontSize}px';
                const style = document.getElementById('claude-desktop-font-style') || document.createElement('style');
                style.id = 'claude-desktop-font-style';
                style.textContent = `
                    body, p, span, div, textarea, input {{
                        font-size: {fontSize}px !important;
                    }}
                    pre, code {{
                        font-size: {fontSize - 2}px !important;
                    }}
                `;
                if (!style.parentNode) {{
                    document.head.appendChild(style);
                }}
            }})();
        ";

        await ClaudeWebView.CoreWebView2.ExecuteScriptAsync(script);
    }

    #region Title Bar Events

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            MaximizeButton_Click(sender, e);
        }
        else
        {
            DragMove();
        }
    }

    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        var contextMenu = new ContextMenu
        {
            Background = (System.Windows.Media.Brush)FindResource("SurfaceBrush"),
            BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrush"),
            Foreground = (System.Windows.Media.Brush)FindResource("TextBrush")
        };

        var refreshItem = new MenuItem { Header = "Refresh Page" };
        refreshItem.Click += (s, args) => ClaudeWebView.Reload();
        contextMenu.Items.Add(refreshItem);

        var homeItem = new MenuItem { Header = "Go to Claude.ai" };
        homeItem.Click += (s, args) => ClaudeWebView.Source = new Uri("https://claude.ai");
        contextMenu.Items.Add(homeItem);

        contextMenu.Items.Add(new Separator());

        var devToolsItem = new MenuItem { Header = "Developer Tools" };
        devToolsItem.Click += (s, args) => ClaudeWebView.CoreWebView2?.OpenDevToolsWindow();
        contextMenu.Items.Add(devToolsItem);

        contextMenu.Items.Add(new Separator());

        var importItem = new MenuItem { Header = "Import Snippets..." };
        importItem.Click += ImportSnippets_Click;
        contextMenu.Items.Add(importItem);

        var exportItem = new MenuItem { Header = "Export Snippets..." };
        exportItem.Click += ExportSnippets_Click;
        contextMenu.Items.Add(exportItem);

        contextMenu.IsOpen = true;
    }

    private void ToggleTopmost_Click(object sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        UpdateTopmostButton();
        SaveSettings();
    }

    private void UpdateTopmostButton()
    {
        var textBlock = (TextBlock)TopmostButton.Content;
        textBlock.Opacity = Topmost ? 1.0 : 0.5;
    }

    private void ToggleSnippets_Click(object sender, RoutedEventArgs e)
    {
        _isSnippetsPanelVisible = !_isSnippetsPanelVisible;
        SnippetsPanel.Visibility = _isSnippetsPanelVisible ? Visibility.Visible : Visibility.Collapsed;
        SaveSettings();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            ((TextBlock)MaximizeButton.Content).Text = "\u25A1";
        }
        else
        {
            WindowState = WindowState.Maximized;
            ((TextBlock)MaximizeButton.Content).Text = "\u29C9";
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        Close();
    }

    #endregion

    #region Snippets Management

    private void RefreshSnippetsList(string? filter = null)
    {
        var snippets = string.IsNullOrWhiteSpace(filter)
            ? _allSnippets
            : _allSnippets.Where(s =>
                s.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                s.Content.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        SnippetsList.ItemsSource = snippets;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshSnippetsList(SearchBox.Text);
    }

    private void AddSnippet_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SnippetEditDialog();
        dialog.Owner = this;

        if (dialog.ShowDialog() == true)
        {
            var snippet = new Snippet
            {
                Id = Guid.NewGuid().ToString(),
                Title = dialog.SnippetTitle,
                Content = dialog.SnippetContent
            };

            _allSnippets.Insert(0, snippet);
            _snippetService.Save(_allSnippets);
            RefreshSnippetsList(SearchBox.Text);
        }
    }

    private void EditSnippet_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Snippet snippet)
        {
            var dialog = new SnippetEditDialog(snippet.Title, snippet.Content);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                snippet.Title = dialog.SnippetTitle;
                snippet.Content = dialog.SnippetContent;
                _snippetService.Save(_allSnippets);
                RefreshSnippetsList(SearchBox.Text);
            }
        }
    }

    private void DeleteSnippet_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Snippet snippet)
        {
            var result = MessageBox.Show(
                $"Delete snippet \"{snippet.Title}\"?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _allSnippets.Remove(snippet);
                _snippetService.Save(_allSnippets);
                RefreshSnippetsList(SearchBox.Text);
            }
        }
    }

    private async void Snippet_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && sender is Border border && border.Tag is Snippet snippet)
        {
            await InsertSnippetIntoClaudeInput(snippet.Content);
        }
    }

    private async Task InsertSnippetIntoClaudeInput(string text)
    {
        if (ClaudeWebView.CoreWebView2 == null) return;

        var escapedText = text
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");

        var script = $"window.insertSnippetText('{escapedText}');";
        await ClaudeWebView.CoreWebView2.ExecuteScriptAsync(script);
    }

    private void ImportSnippets_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Import Snippets"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var imported = _snippetService.Import(dialog.FileName);
                _allSnippets.AddRange(imported);
                _snippetService.Save(_allSnippets);
                RefreshSnippetsList();
                MessageBox.Show($"Imported {imported.Count} snippets.", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to import snippets: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExportSnippets_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            Title = "Export Snippets",
            FileName = "claude-snippets.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _snippetService.Export(_allSnippets, dialog.FileName);
                MessageBox.Show($"Exported {_allSnippets.Count} snippets.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export snippets: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion

    #region Settings

    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FontSizeLabel == null) return;

        var fontSize = (int)e.NewValue;
        FontSizeLabel.Text = $"{fontSize}px";
        ApplyFontSize(fontSize);
        SaveSettings();
    }

    private void SaveSettings()
    {
        var settings = new AppSettings
        {
            FontSize = (int)FontSizeSlider.Value,
            AlwaysOnTop = Topmost,
            SnippetsPanelVisible = _isSnippetsPanelVisible,
            WindowWidth = Width,
            WindowHeight = Height
        };

        _settingsService.Save(settings);
    }

    #endregion
}
