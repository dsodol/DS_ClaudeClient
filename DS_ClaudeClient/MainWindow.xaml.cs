using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DS_ClaudeClient.Models;
using DS_ClaudeClient.Services;
using Microsoft.Web.WebView2.Core;

namespace DS_ClaudeClient;

public partial class MainWindow : Window
{
    private readonly SnippetService _snippetService;
    private readonly SettingsService _settingsService;
    private List<Snippet> _allSnippets = new();
    private bool _isSnippetsPanelVisible = true;
    private int _currentFontSize = 14;
    private string _currentFontFamily = "Segoe UI";
    private int _currentTextAreaWidth = 800;
    private string _currentTextAreaFontFamily = "Segoe UI";
    private int _currentTextAreaFontSize = 14;
    private double _currentTextAreaHeight = 100;

    public MainWindow()
    {
        App.Log("MainWindow constructor starting...");
        try
        {
            InitializeComponent();
            App.Log("InitializeComponent completed");

            _snippetService = new SnippetService();
            _settingsService = new SettingsService();

            Loaded += MainWindow_Loaded;
            ClaudeWebView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
            ClaudeWebView.NavigationCompleted += WebView_NavigationCompleted;
            App.Log("MainWindow constructor completed");
        }
        catch (Exception ex)
        {
            App.Log($"MainWindow constructor error: {ex}");
            throw;
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        App.Log("MainWindow_Loaded starting...");
        try
        {
            // Load settings
            var settings = _settingsService.Load();
            _currentFontSize = settings.FontSize;
            _currentFontFamily = settings.FontFamily;
            _currentTextAreaWidth = settings.TextAreaWidth;
            _currentTextAreaFontFamily = settings.TextAreaFontFamily;
            _currentTextAreaFontSize = settings.TextAreaFontSize;
            _currentTextAreaHeight = settings.TextAreaHeight;
            Topmost = settings.AlwaysOnTop;
            UpdateTopmostButton();
            ApplyTextAreaWidth(_currentTextAreaWidth);
            ApplyTextAreaFont(_currentTextAreaFontFamily, _currentTextAreaFontSize);
            ApplyTextAreaHeight(_currentTextAreaHeight);

            // Restore window position and size
            if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
            {
                Width = settings.WindowWidth;
                Height = settings.WindowHeight;
            }

            if (settings.WindowLeft >= 0 && settings.WindowTop >= 0)
            {
                Left = settings.WindowLeft;
                Top = settings.WindowTop;
                WindowStartupLocation = WindowStartupLocation.Manual;
            }

            if (settings.IsMaximized)
            {
                WindowState = WindowState.Maximized;
            }

            _isSnippetsPanelVisible = settings.SnippetsPanelVisible;
            SnippetsPanel.Visibility = _isSnippetsPanelVisible ? Visibility.Visible : Visibility.Collapsed;

            // Load snippets
            _allSnippets = _snippetService.Load();
            RefreshSnippetsList();
            App.Log("Settings and snippets loaded");

            // Initialize WebView2 with persistent user data folder
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DS_ClaudeClient", "WebView2");

            App.Log($"Creating WebView2 environment at: {userDataFolder}");
            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            App.Log("WebView2 environment created");
            await ClaudeWebView.EnsureCoreWebView2Async(env);
            App.Log("WebView2 initialized, navigating to Claude.ai");
            ClaudeWebView.Source = new Uri("https://claude.ai");
            App.Log("MainWindow_Loaded completed");
        }
        catch (Exception ex)
        {
            App.Log($"MainWindow_Loaded error: {ex}");
            throw;
        }
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
            ApplyFontSize(_currentFontSize);
        }
    }

    private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            // Inject enhancement scripts after page loads
            await InjectEnhancementScripts();
            ApplyFontSize(_currentFontSize);
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
        var resourceName = $"DS_ClaudeClient.Scripts.{scriptName}";

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
                // Expose function to insert snippet text into Claude's ProseMirror editor
                window.insertSnippetText = function(text) {
                    // Claude.ai uses a ProseMirror editor with contenteditable
                    const selectors = [
                        '[contenteditable="true"].ProseMirror',
                        'div[contenteditable="true"]',
                        '[role="textbox"][contenteditable="true"]',
                        'textarea'
                    ];

                    let targetInput = null;
                    for (const selector of selectors) {
                        targetInput = document.querySelector(selector);
                        if (targetInput) break;
                    }

                    if (!targetInput) {
                        console.log('DS Claude Client: No input found');
                        return false;
                    }

                    targetInput.focus();

                    if (targetInput.tagName === 'TEXTAREA') {
                        const start = targetInput.selectionStart || 0;
                        const end = targetInput.selectionEnd || 0;
                        const value = targetInput.value || '';
                        targetInput.value = value.substring(0, start) + text + value.substring(end);
                        targetInput.selectionStart = targetInput.selectionEnd = start + text.length;
                        targetInput.dispatchEvent(new Event('input', { bubbles: true }));
                    } else {
                        // For ProseMirror/contenteditable, use execCommand or direct insertion
                        // First, try to set cursor at the end
                        const selection = window.getSelection();
                        const range = document.createRange();

                        // Move cursor to end of content
                        range.selectNodeContents(targetInput);
                        range.collapse(false);
                        selection.removeAllRanges();
                        selection.addRange(range);

                        // Insert text using execCommand (works with ProseMirror)
                        document.execCommand('insertText', false, text);
                    }

                    console.log('DS Claude Client: Text inserted');
                    return true;
                };

                // Expose function to click the send button
                window.clickSendButton = function() {
                    // Try multiple selectors for the send button
                    const selectors = [
                        'button[aria-label="Send Message"]',
                        'button[aria-label*="Send"]',
                        'button[data-testid="send-button"]',
                        'button[type="submit"]',
                        'form button:last-of-type',
                        'button svg[viewBox]' // Button containing an SVG (arrow icon)
                    ];

                    for (const selector of selectors) {
                        let button = document.querySelector(selector);
                        // If we found an SVG, get its parent button
                        if (button && button.tagName === 'svg') {
                            button = button.closest('button');
                        }
                        if (button && !button.disabled) {
                            button.click();
                            console.log('DS Claude Client: Send button clicked via', selector);
                            return true;
                        }
                    }

                    // Fallback: Find button by looking for arrow/send icon
                    const buttons = document.querySelectorAll('button');
                    for (const btn of buttons) {
                        if (btn.querySelector('svg') && !btn.disabled) {
                            const rect = btn.getBoundingClientRect();
                            // Look for buttons in the input area (bottom of page, reasonable size)
                            if (rect.bottom > window.innerHeight - 200 && rect.width < 100) {
                                btn.click();
                                console.log('DS Claude Client: Send button clicked via fallback');
                                return true;
                            }
                        }
                    }

                    // Last resort: simulate Enter key
                    const input = document.querySelector('[contenteditable="true"].ProseMirror, div[contenteditable="true"]');
                    if (input) {
                        input.dispatchEvent(new KeyboardEvent('keydown', {
                            key: 'Enter',
                            code: 'Enter',
                            keyCode: 13,
                            which: 13,
                            bubbles: true,
                            cancelable: true
                        }));
                        console.log('DS Claude Client: Enter key simulated');
                        return true;
                    }

                    console.log('DS Claude Client: Could not find send button');
                    return false;
                };

                console.log('DS Claude Client enhancements loaded');
            })();
            """;
    }

    private async void ApplyFontSize(int fontSize)
    {
        if (ClaudeWebView.CoreWebView2 == null) return;

        var script = $@"
            (function() {{
                document.body.style.fontSize = '{fontSize}px';
                const style = document.getElementById('ds-claude-client-font-style') || document.createElement('style');
                style.id = 'ds-claude-client-font-style';
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

        contextMenu.Items.Add(new Separator());

        var optionsItem = new MenuItem { Header = "Options..." };
        optionsItem.Click += OptionsButton_Click;
        contextMenu.Items.Add(optionsItem);

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
                MessageBoxButton.OK,
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
            FileName = "ds-claude-snippets.json"
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

    #region Message Input

    private async void MessageInput_KeyDown(object sender, KeyEventArgs e)
    {
        // Shift+Enter or Ctrl+Enter to send
        if (e.Key == Key.Enter &&
            ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ||
             (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
        {
            await SendMessage();
            e.Handled = true;
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await SendMessage();
    }

    private async Task SendMessage()
    {
        var message = MessageInput.Text?.Trim();
        if (string.IsNullOrEmpty(message)) return;

        // Insert text into Claude's input and submit
        await InsertSnippetIntoClaudeInput(message);

        // Try to click the send button in Claude.ai
        await ClickClaudeSendButton();

        // Clear the input
        MessageInput.Text = string.Empty;
        MessageInput.Focus();
    }

    private async Task ClickClaudeSendButton()
    {
        if (ClaudeWebView.CoreWebView2 == null) return;

        await ClaudeWebView.CoreWebView2.ExecuteScriptAsync("window.clickSendButton();");
    }

    #endregion

    #region Settings

    private void OptionsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OptionsDialog(_currentFontSize, _currentFontFamily, _currentTextAreaWidth,
            _currentTextAreaFontFamily, _currentTextAreaFontSize);
        dialog.Owner = this;

        if (dialog.ShowDialog() == true)
        {
            _currentFontSize = dialog.FontSize;
            _currentFontFamily = dialog.SelectedFontFamily;
            _currentTextAreaWidth = dialog.TextAreaWidth;
            _currentTextAreaFontFamily = dialog.TextAreaFontFamily;
            _currentTextAreaFontSize = dialog.TextAreaFontSize;
            ApplyFontSize(_currentFontSize);
            ApplyTextAreaWidth(_currentTextAreaWidth);
            ApplyTextAreaFont(_currentTextAreaFontFamily, _currentTextAreaFontSize);
            SaveSettings();
        }
    }

    private void ApplyTextAreaWidth(int width)
    {
        TextAreaGrid.MaxWidth = width * 2;
        TextAreaGrid.ColumnDefinitions[0].MinWidth = width;
    }

    private void ApplyTextAreaFont(string fontFamily, int fontSize)
    {
        MessageInput.FontFamily = new System.Windows.Media.FontFamily(fontFamily);
        MessageInput.FontSize = fontSize;
    }

    private void ApplyTextAreaHeight(double height)
    {
        TextAreaRow.Height = new GridLength(height);
    }

    private void SaveSettings()
    {
        // Store restore bounds when maximized
        var restoreBounds = WindowState == WindowState.Maximized ? RestoreBounds : new Rect(Left, Top, Width, Height);

        // Get current text area height from the row definition
        var textAreaHeight = TextAreaRow.Height.Value;

        var settings = new AppSettings
        {
            FontSize = _currentFontSize,
            FontFamily = _currentFontFamily,
            TextAreaWidth = _currentTextAreaWidth,
            TextAreaFontFamily = _currentTextAreaFontFamily,
            TextAreaFontSize = _currentTextAreaFontSize,
            TextAreaHeight = textAreaHeight,
            AlwaysOnTop = Topmost,
            SnippetsPanelVisible = _isSnippetsPanelVisible,
            WindowWidth = restoreBounds.Width,
            WindowHeight = restoreBounds.Height,
            WindowLeft = restoreBounds.Left,
            WindowTop = restoreBounds.Top,
            IsMaximized = WindowState == WindowState.Maximized
        };

        _settingsService.Save(settings);
    }

    #endregion
}
