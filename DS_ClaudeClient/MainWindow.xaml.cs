using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DS_ClaudeClient.Models;
using DS_ClaudeClient.Services;
using Microsoft.Web.WebView2.Core;

namespace DS_ClaudeClient;

public enum SnippetSortMode { Custom, Title, DateCreated }

public partial class MainWindow : Window
{
    private readonly SnippetService _snippetService;
    private readonly SettingsService _settingsService;
    private List<Snippet> _allSnippets = new();
    private bool _isSnippetsPanelVisible = true;
    private int _currentFontSize = 14;
    private string _currentFontFamily = "Segoe UI";
    private string _currentTextAreaFontFamily = "Segoe UI";
    private int _currentTextAreaFontSize = 14;
    private double _currentTextAreaHeight = 100;
    private Point _dragStartPoint;
    private SnippetSortMode _currentSortMode = SnippetSortMode.Custom;
    private bool _sortAscending = true;
    private bool _lastFocusWasMessageInput = false;
    private double _currentSnippetsPanelWidth = 280;
    private readonly List<string> _messageHistory = new();
    private SendKeyMode _currentSendKeyMode = SendKeyMode.ShiftEnter;
    private bool _isTextAreaCollapsed = false;
    private double _savedTextAreaHeight = 100;
    private string _currentSnippetsFilePath = "";

    public MainWindow()
    {
        App.Log("MainWindow constructor starting...");
        try
        {
            InitializeComponent();
            App.Log("InitializeComponent completed");

            // Display version in title bar
            DisplayVersion();

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
            _currentTextAreaFontFamily = settings.TextAreaFontFamily;
            _currentTextAreaFontSize = settings.TextAreaFontSize;
            _currentTextAreaHeight = settings.TextAreaHeight;
            _currentSendKeyMode = settings.SendKeyMode;
            Topmost = settings.AlwaysOnTop;
            UpdateTopmostButton();
            ApplyTextAreaFont(_currentTextAreaFontFamily, _currentTextAreaFontSize);
            ApplyTextAreaHeight(_currentTextAreaHeight);
            UpdateSendButtonTooltip();

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
            _currentSnippetsPanelWidth = settings.SnippetsPanelWidth;
            UpdateSnippetsPanelVisibility();

            // Apply snippets file path from settings
            _currentSnippetsFilePath = settings.SnippetsFilePath;
            if (!string.IsNullOrWhiteSpace(_currentSnippetsFilePath))
            {
                _snippetService.SetFilePath(_currentSnippetsFilePath);
            }
            _currentSnippetsFilePath = _snippetService.SnippetsFilePath;

            // Load snippets
            _allSnippets = _snippetService.Load();
            RefreshSnippetsList();
            App.Log($"Settings and snippets loaded from: {_currentSnippetsFilePath}");

            // Track focus - remember last focused element for snippet insertion
            MessageInput.GotFocus += (s, args) => _lastFocusWasMessageInput = true;
            ClaudeWebView.GotFocus += (s, args) => _lastFocusWasMessageInput = false;

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
                        'div.ProseMirror[contenteditable="true"]',
                        '[contenteditable="true"].ProseMirror',
                        'div[contenteditable="true"][data-placeholder]',
                        'div[contenteditable="true"]',
                        '[role="textbox"][contenteditable="true"]',
                        'textarea'
                    ];

                    let targetInput = null;
                    for (const selector of selectors) {
                        targetInput = document.querySelector(selector);
                        if (targetInput) {
                            console.log('DS Claude Client: Found input via', selector);
                            break;
                        }
                    }

                    if (!targetInput) {
                        console.log('DS Claude Client: No input found');
                        return false;
                    }

                    targetInput.focus();

                    // Small delay to ensure focus is established
                    setTimeout(() => {
                        if (targetInput.tagName === 'TEXTAREA') {
                            const start = targetInput.selectionStart || 0;
                            const end = targetInput.selectionEnd || 0;
                            const value = targetInput.value || '';
                            targetInput.value = value.substring(0, start) + text + value.substring(end);
                            targetInput.selectionStart = targetInput.selectionEnd = start + text.length;
                            targetInput.dispatchEvent(new Event('input', { bubbles: true }));
                        } else {
                            // For ProseMirror/contenteditable
                            const selection = window.getSelection();
                            const range = document.createRange();

                            // Move cursor to end of content
                            range.selectNodeContents(targetInput);
                            range.collapse(false);
                            selection.removeAllRanges();
                            selection.addRange(range);

                            // Insert text - try multiple methods
                            if (document.execCommand('insertText', false, text)) {
                                console.log('DS Claude Client: Text inserted via execCommand');
                            } else {
                                // Fallback: create text node and insert
                                const textNode = document.createTextNode(text);
                                range.insertNode(textNode);
                                range.setStartAfter(textNode);
                                range.collapse(true);
                                selection.removeAllRanges();
                                selection.addRange(range);
                                // Trigger input event for ProseMirror
                                targetInput.dispatchEvent(new InputEvent('input', { bubbles: true, data: text }));
                                console.log('DS Claude Client: Text inserted via textNode');
                            }
                        }
                    }, 50);

                    return true;
                };

                // Expose function to click the send button
                window.clickSendButton = function() {
                    // Try multiple selectors for the send button
                    const selectors = [
                        'button[aria-label="Send message"]',
                        'button[aria-label="Send Message"]',
                        'button[aria-label*="Send"]',
                        'button[data-testid="send-button"]',
                        'button[data-testid*="send"]',
                        'fieldset button[type="button"]',
                        'form button[type="button"]:not([aria-label*="Upload"])',
                        'button[type="submit"]'
                    ];

                    for (const selector of selectors) {
                        const buttons = document.querySelectorAll(selector);
                        for (const button of buttons) {
                            if (button && !button.disabled) {
                                // Check if it's likely a send button (has SVG, is in lower part of page)
                                const rect = button.getBoundingClientRect();
                                if (rect.bottom > window.innerHeight / 2) {
                                    button.click();
                                    console.log('DS Claude Client: Send button clicked via', selector);
                                    return true;
                                }
                            }
                        }
                    }

                    // Fallback: Find the rightmost button near the input that's not disabled
                    const inputArea = document.querySelector('div.ProseMirror[contenteditable="true"], div[contenteditable="true"]');
                    if (inputArea) {
                        const container = inputArea.closest('fieldset') || inputArea.closest('form') || inputArea.parentElement?.parentElement;
                        if (container) {
                            const buttons = container.querySelectorAll('button:not([disabled])');
                            let sendBtn = null;
                            let maxRight = 0;
                            for (const btn of buttons) {
                                const rect = btn.getBoundingClientRect();
                                if (rect.right > maxRight && btn.querySelector('svg')) {
                                    maxRight = rect.right;
                                    sendBtn = btn;
                                }
                            }
                            if (sendBtn) {
                                sendBtn.click();
                                console.log('DS Claude Client: Send button clicked via container search');
                                return true;
                            }
                        }
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

    private void DisplayVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        // Use informational version if available (includes build number), otherwise use assembly version
        var displayVersion = infoVersion ?? version?.ToString() ?? "1.0.0";

        // Remove git hash suffix if present (after the +)
        var plusIndex = displayVersion.IndexOf('+');
        if (plusIndex > 0)
        {
            displayVersion = displayVersion.Substring(0, plusIndex);
        }

        VersionText.Text = $"v{displayVersion}";
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

        var historyItem = new MenuItem { Header = "Message History..." };
        historyItem.Click += ShowHistory_Click;
        contextMenu.Items.Add(historyItem);

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
        UpdateSnippetsPanelVisibility();
        SaveSettings();
    }

    private void UpdateSnippetsPanelVisibility()
    {
        if (_isSnippetsPanelVisible)
        {
            SnippetsPanel.Visibility = Visibility.Visible;
            SnippetsSplitter.Visibility = Visibility.Visible;
            SnippetsPanelColumn.Width = new GridLength(_currentSnippetsPanelWidth);
            SnippetsPanelColumn.MinWidth = 200;
            SnippetsPanelColumn.MaxWidth = 500;
        }
        else
        {
            // Save current width before hiding
            _currentSnippetsPanelWidth = SnippetsPanelColumn.Width.Value;
            SnippetsPanel.Visibility = Visibility.Collapsed;
            SnippetsSplitter.Visibility = Visibility.Collapsed;
            SnippetsPanelColumn.Width = new GridLength(0);
            SnippetsPanelColumn.MinWidth = 0;
            SnippetsPanelColumn.MaxWidth = 0;
        }
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
        // Filter out empty snippets (no title and no content)
        IEnumerable<Snippet> snippets = _allSnippets
            .Where(s => !string.IsNullOrWhiteSpace(s.Title) || !string.IsNullOrWhiteSpace(s.Content));

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(filter))
        {
            snippets = snippets.Where(s =>
                s.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                s.Content.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        // Apply view-only sorting (doesn't modify _allSnippets)
        snippets = _currentSortMode switch
        {
            SnippetSortMode.Title => _sortAscending
                ? snippets.OrderBy(x => x.Title)
                : snippets.OrderByDescending(x => x.Title),
            SnippetSortMode.DateCreated => _sortAscending
                ? snippets.OrderBy(x => x.CreatedAt)
                : snippets.OrderByDescending(x => x.CreatedAt),
            _ => snippets.OrderBy(x => x.Order) // Custom order
        };

        SnippetsList.ItemsSource = snippets.ToList();
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

    private async void SnippetsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SnippetsList.SelectedItem is Snippet snippet)
        {
            // Insert into text input if it was the last focused element, otherwise into browser
            if (_lastFocusWasMessageInput)
            {
                InsertIntoMessageInput(snippet.Content);
            }
            else
            {
                await InsertSnippetIntoClaudeInput(snippet.Content);
            }
        }
    }

    private void InsertIntoMessageInput(string text)
    {
        var caretIndex = MessageInput.CaretIndex;
        MessageInput.Text = MessageInput.Text.Insert(caretIndex, text);
        MessageInput.CaretIndex = caretIndex + text.Length;
        MessageInput.Focus();
    }

    private void SortSnippets_Click(object sender, RoutedEventArgs e)
    {
        var contextMenu = new ContextMenu
        {
            Background = (System.Windows.Media.Brush)FindResource("SurfaceBrush"),
            BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrush"),
            Foreground = (System.Windows.Media.Brush)FindResource("TextBrush")
        };

        var sortByTitleItem = new MenuItem
        {
            Header = "Sort by Title" + (_currentSortMode == SnippetSortMode.Title ? (_sortAscending ? " ▲" : " ▼") : "")
        };
        sortByTitleItem.Click += (s, args) =>
        {
            if (_currentSortMode == SnippetSortMode.Title)
                _sortAscending = !_sortAscending;
            else
            {
                _currentSortMode = SnippetSortMode.Title;
                _sortAscending = true;
            }
            RefreshSnippetsList(SearchBox.Text);
        };
        contextMenu.Items.Add(sortByTitleItem);

        var sortByDateItem = new MenuItem
        {
            Header = "Sort by Date Created" + (_currentSortMode == SnippetSortMode.DateCreated ? (_sortAscending ? " ▲" : " ▼") : "")
        };
        sortByDateItem.Click += (s, args) =>
        {
            if (_currentSortMode == SnippetSortMode.DateCreated)
                _sortAscending = !_sortAscending;
            else
            {
                _currentSortMode = SnippetSortMode.DateCreated;
                _sortAscending = true;
            }
            RefreshSnippetsList(SearchBox.Text);
        };
        contextMenu.Items.Add(sortByDateItem);

        var sortByOrderItem = new MenuItem
        {
            Header = "Sort by Custom Order" + (_currentSortMode == SnippetSortMode.Custom ? " ✓" : "")
        };
        sortByOrderItem.Click += (s, args) =>
        {
            _currentSortMode = SnippetSortMode.Custom;
            RefreshSnippetsList(SearchBox.Text);
        };
        contextMenu.Items.Add(sortByOrderItem);

        contextMenu.IsOpen = true;
    }

    private void UpdateSnippetOrders()
    {
        for (int i = 0; i < _allSnippets.Count; i++)
        {
            _allSnippets[i].Order = i;
        }
        _snippetService.Save(_allSnippets);
    }

    #region Drag and Drop

    private void SnippetsList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void SnippetsList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var currentPos = e.GetPosition(null);
        var diff = _dragStartPoint - currentPos;

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            var listView = sender as ListView;
            var listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);

            if (listViewItem == null) return;

            var snippet = (Snippet)listView!.ItemContainerGenerator.ItemFromContainer(listViewItem);
            if (snippet == null) return;

            var dragData = new DataObject("Snippet", snippet);
            DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Move);
        }
    }

    private void SnippetsList_DragEnter(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("Snippet"))
        {
            e.Effects = DragDropEffects.None;
        }
    }

    private void SnippetsList_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("Snippet")) return;

        var droppedSnippet = (Snippet)e.Data.GetData("Snippet");
        var target = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);

        if (target == null) return;

        var targetSnippet = (Snippet)SnippetsList.ItemContainerGenerator.ItemFromContainer(target);
        if (targetSnippet == null || droppedSnippet == targetSnippet) return;

        var oldIndex = _allSnippets.IndexOf(droppedSnippet);
        var newIndex = _allSnippets.IndexOf(targetSnippet);

        _allSnippets.RemoveAt(oldIndex);
        _allSnippets.Insert(newIndex, droppedSnippet);

        UpdateSnippetOrders();
        RefreshSnippetsList(SearchBox.Text);
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T t)
            {
                return t;
            }
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    #endregion

    private async Task InsertSnippetIntoClaudeInput(string text)
    {
        if (ClaudeWebView.CoreWebView2 == null) return;

        // Use the Windows clipboard to paste text - this is the most reliable method for ProseMirror
        // Save current clipboard content
        string? previousClipboard = null;
        try
        {
            if (System.Windows.Clipboard.ContainsText())
            {
                previousClipboard = System.Windows.Clipboard.GetText();
            }
        }
        catch { /* Ignore clipboard errors */ }

        // Set our text to clipboard
        try
        {
            System.Windows.Clipboard.SetText(text);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set clipboard: {ex.Message}");
            return;
        }

        // Focus the input and trigger paste via JavaScript
        var script = @"
            (function() {
                // Find Claude's input
                const selectors = [
                    'div.ProseMirror[contenteditable=""true""]',
                    '[contenteditable=""true""].ProseMirror',
                    'div[contenteditable=""true""][data-placeholder]',
                    'div[contenteditable=""true""]'
                ];

                let input = null;
                for (const sel of selectors) {
                    input = document.querySelector(sel);
                    if (input) break;
                }

                if (!input) {
                    console.error('DS Claude Client: No input element found');
                    return false;
                }

                input.focus();

                // Move cursor to end
                const sel = window.getSelection();
                const range = document.createRange();
                range.selectNodeContents(input);
                range.collapse(false);
                sel.removeAllRanges();
                sel.addRange(range);

                console.log('DS Claude Client: Input focused, ready for paste');
                return true;
            })();
        ";

        await ClaudeWebView.CoreWebView2.ExecuteScriptAsync(script);

        // Small delay to ensure focus is established
        await Task.Delay(50);

        // Simulate Ctrl+V paste using SendKeys
        // First, send focus to WebView
        ClaudeWebView.Focus();
        await Task.Delay(20);

        // Use SendInput to send Ctrl+V
        SendCtrlV();

        // Small delay then restore clipboard
        await Task.Delay(100);

        // Restore previous clipboard content
        if (previousClipboard != null)
        {
            try
            {
                System.Windows.Clipboard.SetText(previousClipboard);
            }
            catch { /* Ignore clipboard restore errors */ }
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const byte VK_CONTROL = 0x11;
    private const byte VK_V = 0x56;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private void SendCtrlV()
    {
        // Press Ctrl
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        // Press V
        keybd_event(VK_V, 0, 0, UIntPtr.Zero);
        // Release V
        keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        // Release Ctrl
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
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

    private void ShowHistory_Click(object sender, RoutedEventArgs e)
    {
        if (_messageHistory.Count == 0)
        {
            MessageBox.Show("No message history yet.", "History", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new HistoryDialog(_messageHistory);
        dialog.Owner = this;
        dialog.ShowDialog();
    }

    private async void MessageInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Check for send key based on current mode
        if (e.Key == Key.Enter)
        {
            bool shouldSend = _currentSendKeyMode switch
            {
                SendKeyMode.ShiftEnter => (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift,
                SendKeyMode.CtrlEnter => (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control,
                SendKeyMode.Enter => Keyboard.Modifiers == ModifierKeys.None,
                _ => false
            };

            if (shouldSend)
            {
                await SendMessage();
                e.Handled = true;
                return;
            }
        }

        // Handle Tab key - insert 2 spaces instead of default tab
        if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None)
        {
            var caretIndex = MessageInput.CaretIndex;
            MessageInput.Text = MessageInput.Text.Insert(caretIndex, "  ");
            MessageInput.CaretIndex = caretIndex + 2;
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

        // Add to history (avoid duplicates of the last message)
        if (_messageHistory.Count == 0 || _messageHistory[^1] != message)
        {
            _messageHistory.Add(message);
        }

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

        // Self-contained script to find and click Claude's send button
        var script = @"
            (function() {
                // Try multiple selectors for the send button
                const selectors = [
                    'button[aria-label=""Send message""]',
                    'button[aria-label=""Send Message""]',
                    'button[aria-label*=""Send""]',
                    'button[data-testid=""send-button""]',
                    'button[data-testid*=""send""]'
                ];

                for (const sel of selectors) {
                    const btns = document.querySelectorAll(sel);
                    for (const btn of btns) {
                        if (btn && !btn.disabled) {
                            btn.click();
                            console.log('DS Claude Client: Send button clicked via', sel);
                            return true;
                        }
                    }
                }

                // Fallback: find button with SVG near the input
                const input = document.querySelector('div.ProseMirror[contenteditable=""true""], div[contenteditable=""true""]');
                if (input) {
                    const container = input.closest('fieldset') || input.closest('form') || input.parentElement?.parentElement?.parentElement;
                    if (container) {
                        const buttons = container.querySelectorAll('button:not([disabled])');
                        for (const btn of buttons) {
                            if (btn.querySelector('svg')) {
                                btn.click();
                                console.log('DS Claude Client: Send button clicked via container fallback');
                                return true;
                            }
                        }
                    }
                }

                console.error('DS Claude Client: Could not find send button');
                return false;
            })();
        ";

        await ClaudeWebView.CoreWebView2.ExecuteScriptAsync(script);
    }

    #endregion

    #region Settings

    private void OptionsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OptionsDialog(_currentFontSize, _currentFontFamily,
            _currentTextAreaFontFamily, _currentTextAreaFontSize, _currentSendKeyMode, _currentSnippetsFilePath,
            _snippetService, _allSnippets);
        dialog.Owner = this;

        if (dialog.ShowDialog() == true)
        {
            _currentFontSize = dialog.FontSize;
            _currentFontFamily = dialog.SelectedFontFamily;
            _currentTextAreaFontFamily = dialog.TextAreaFontFamily;
            _currentTextAreaFontSize = dialog.TextAreaFontSize;
            _currentSendKeyMode = dialog.SendKeyMode;

            // Handle snippets file path change
            if (_currentSnippetsFilePath != dialog.SnippetsFilePath)
            {
                _currentSnippetsFilePath = dialog.SnippetsFilePath;
                _snippetService.SetFilePath(_currentSnippetsFilePath);
                _allSnippets = _snippetService.Load();
                RefreshSnippetsList();
            }

            ApplyFontSize(_currentFontSize);
            ApplyTextAreaFont(_currentTextAreaFontFamily, _currentTextAreaFontSize);
            UpdateSendButtonTooltip();
            SaveSettings();
        }
    }

    private void UpdateSendButtonTooltip()
    {
        var shortcut = _currentSendKeyMode switch
        {
            SendKeyMode.ShiftEnter => "Shift+Enter",
            SendKeyMode.CtrlEnter => "Ctrl+Enter",
            SendKeyMode.Enter => "Enter",
            _ => "Shift+Enter"
        };
        // Find the send button in the right panel and update its tooltip
        var textAreaBorderGrid = TextAreaBorder.Child as Grid;
        if (textAreaBorderGrid != null)
        {
            var rightPanel = textAreaBorderGrid.Children.OfType<StackPanel>().FirstOrDefault();
            var sendButton = rightPanel?.Children.OfType<Button>().FirstOrDefault(b => b.Content?.ToString() == "Send");
            if (sendButton != null)
            {
                sendButton.ToolTip = $"Send message ({shortcut})";
            }
        }
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

    #endregion

    #region Text Area Collapse/Expand

    private void CollapseTextArea_Click(object sender, RoutedEventArgs e)
    {
        CollapseTextArea();
    }

    private void ExpandTextArea_Click(object sender, RoutedEventArgs e)
    {
        ExpandTextArea();
    }

    private void CollapsedTextAreaBar_Click(object sender, MouseButtonEventArgs e)
    {
        ExpandTextArea();
    }

    private void CollapseTextArea()
    {
        if (_isTextAreaCollapsed) return;

        // Save current height before collapsing
        _savedTextAreaHeight = TextAreaRow.Height.Value;

        // Hide the text area and splitter, show collapsed bar
        TextAreaBorder.Visibility = Visibility.Collapsed;
        TextAreaSplitter.Visibility = Visibility.Collapsed;
        CollapsedTextAreaBar.Visibility = Visibility.Visible;

        // Set row to auto-size for the collapsed bar
        TextAreaRow.Height = new GridLength(32);
        TextAreaRow.MinHeight = 32;
        TextAreaRow.MaxHeight = 32;

        _isTextAreaCollapsed = true;
    }

    private void ExpandTextArea()
    {
        if (!_isTextAreaCollapsed) return;

        // Show the text area and splitter, hide collapsed bar
        TextAreaBorder.Visibility = Visibility.Visible;
        TextAreaSplitter.Visibility = Visibility.Visible;
        CollapsedTextAreaBar.Visibility = Visibility.Collapsed;

        // Restore height constraints and value
        TextAreaRow.MinHeight = 60;
        TextAreaRow.MaxHeight = 400;
        TextAreaRow.Height = new GridLength(_savedTextAreaHeight);

        _isTextAreaCollapsed = false;

        // Focus the message input
        MessageInput.Focus();
    }

    private void SaveSettings()
    {
        // Store restore bounds when maximized
        var restoreBounds = WindowState == WindowState.Maximized ? RestoreBounds : new Rect(Left, Top, Width, Height);

        // Get current text area height from the row definition
        var textAreaHeight = TextAreaRow.Height.Value;

        // Get current snippets panel width (if visible)
        if (_isSnippetsPanelVisible && SnippetsPanelColumn.Width.Value > 0)
        {
            _currentSnippetsPanelWidth = SnippetsPanelColumn.Width.Value;
        }

        var settings = new AppSettings
        {
            SendKeyMode = _currentSendKeyMode,
            FontSize = _currentFontSize,
            FontFamily = _currentFontFamily,
            TextAreaFontFamily = _currentTextAreaFontFamily,
            TextAreaFontSize = _currentTextAreaFontSize,
            TextAreaHeight = textAreaHeight,
            AlwaysOnTop = Topmost,
            SnippetsPanelVisible = _isSnippetsPanelVisible,
            SnippetsPanelWidth = _currentSnippetsPanelWidth,
            SnippetsFilePath = _currentSnippetsFilePath,
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
