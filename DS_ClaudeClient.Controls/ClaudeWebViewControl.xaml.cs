using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DS_ClaudeClient.Controls.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

namespace DS_ClaudeClient.Controls;

/// <summary>
/// A self-contained control for browsing Claude.ai with an integrated text input area.
/// Zero-config: works out of the box with default settings.
/// </summary>
public partial class ClaudeWebViewControl : UserControl
{
    #region Private Fields

    private bool _isInitialized;
    private bool _isTextAreaCollapsed;
    private double _savedTextAreaHeight = 100;
    private readonly List<string> _messageHistory = [];
    private int _historyIndex = -1;
    private string? _initializationStackTrace;
    private string? _resolvedDataFolderPath;

    #endregion

    #region Dependency Properties

    /// <summary>
    /// Gets or sets the data folder path for WebView2 user data (cookies, cache, etc).
    /// Default: %LocalAppData%/DS_ClaudeClient/
    /// </summary>
    public static readonly DependencyProperty DataFolderPathProperty =
        DependencyProperty.Register(
            nameof(DataFolderPath),
            typeof(string),
            typeof(ClaudeWebViewControl),
            new PropertyMetadata(null));

    public string? DataFolderPath
    {
        get => (string?)GetValue(DataFolderPathProperty);
        set => SetValue(DataFolderPathProperty, value);
    }

    /// <summary>
    /// Gets or sets the URL to navigate to. Default: https://claude.ai
    /// </summary>
    public static readonly DependencyProperty SourceUrlProperty =
        DependencyProperty.Register(
            nameof(SourceUrl),
            typeof(string),
            typeof(ClaudeWebViewControl),
            new PropertyMetadata("https://claude.ai", OnSourceUrlChanged));

    public string SourceUrl
    {
        get => (string)GetValue(SourceUrlProperty);
        set => SetValue(SourceUrlProperty, value);
    }

    /// <summary>
    /// Gets or sets the font size for the WebView content.
    /// </summary>
    public static readonly DependencyProperty WebViewFontSizeProperty =
        DependencyProperty.Register(
            nameof(WebViewFontSize),
            typeof(int),
            typeof(ClaudeWebViewControl),
            new PropertyMetadata(14, OnWebViewFontSizeChanged));

    public int WebViewFontSize
    {
        get => (int)GetValue(WebViewFontSizeProperty);
        set => SetValue(WebViewFontSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the font family for the text input area.
    /// </summary>
    public static readonly DependencyProperty TextAreaFontFamilyProperty =
        DependencyProperty.Register(
            nameof(TextAreaFontFamily),
            typeof(string),
            typeof(ClaudeWebViewControl),
            new PropertyMetadata("Segoe UI", OnTextAreaFontChanged));

    public string TextAreaFontFamily
    {
        get => (string)GetValue(TextAreaFontFamilyProperty);
        set => SetValue(TextAreaFontFamilyProperty, value);
    }

    /// <summary>
    /// Gets or sets the font size for the text input area.
    /// </summary>
    public static readonly DependencyProperty TextAreaFontSizeProperty =
        DependencyProperty.Register(
            nameof(TextAreaFontSize),
            typeof(int),
            typeof(ClaudeWebViewControl),
            new PropertyMetadata(14, OnTextAreaFontChanged));

    public int TextAreaFontSize
    {
        get => (int)GetValue(TextAreaFontSizeProperty);
        set => SetValue(TextAreaFontSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the height of the text input area.
    /// </summary>
    public static readonly DependencyProperty TextAreaHeightProperty =
        DependencyProperty.Register(
            nameof(TextAreaHeight),
            typeof(double),
            typeof(ClaudeWebViewControl),
            new PropertyMetadata(100.0, OnTextAreaHeightChanged));

    public double TextAreaHeight
    {
        get => (double)GetValue(TextAreaHeightProperty);
        set => SetValue(TextAreaHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the keyboard shortcut mode for sending messages.
    /// </summary>
    public static readonly DependencyProperty SendKeyModeProperty =
        DependencyProperty.Register(
            nameof(SendKeyMode),
            typeof(SendKeyMode),
            typeof(ClaudeWebViewControl),
            new PropertyMetadata(SendKeyMode.ShiftEnter, OnSendKeyModeChanged));

    public SendKeyMode SendKeyMode
    {
        get => (SendKeyMode)GetValue(SendKeyModeProperty);
        set => SetValue(SendKeyModeProperty, value);
    }

    /// <summary>
    /// Gets whether the text area is currently collapsed.
    /// </summary>
    public bool IsTextAreaCollapsed => _isTextAreaCollapsed;

    /// <summary>
    /// Gets or sets the text in the input area.
    /// </summary>
    public string InputText
    {
        get => MessageInput.Text;
        set => MessageInput.Text = value;
    }

    /// <summary>
    /// Gets or sets the logger for diagnostic output.
    /// Uses Microsoft.Extensions.Logging.ILogger.
    /// </summary>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Gets the stack trace captured when the WebView2 data folder was initialized.
    /// </summary>
    public string? InitializationStackTrace => _initializationStackTrace;

    /// <summary>
    /// Gets the resolved WebView2 data folder path.
    /// </summary>
    public string? ResolvedDataFolderPath => _resolvedDataFolderPath;

    #endregion

    #region Events

    /// <summary>
    /// Raised when a message is sent to Claude.
    /// </summary>
    public event EventHandler<string>? MessageSent;

    /// <summary>
    /// Raised when the text area is collapsed.
    /// </summary>
    public event EventHandler? TextAreaCollapsedEvent;

    /// <summary>
    /// Raised when the text area is expanded.
    /// </summary>
    public event EventHandler? TextAreaExpandedEvent;

    /// <summary>
    /// Raised when WebView2 initialization is complete.
    /// </summary>
    public event EventHandler? WebViewInitialized;

    /// <summary>
    /// Raised when navigation to Claude.ai is complete.
    /// </summary>
    public event EventHandler? NavigationCompleted;

    #endregion

    #region Constructor

    public ClaudeWebViewControl()
    {
        InitializeComponent();
        Loaded += ClaudeWebViewControl_Loaded;
    }

    #endregion

    #region Initialization

    private async void ClaudeWebViewControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized) return;

        try
        {
            // Set up WebView2 events
            ClaudeWebView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
            ClaudeWebView.NavigationCompleted += WebView_NavigationCompleted;

            // Initialize WebView2 with persistent user data folder
            _initializationStackTrace = new StackTrace(true).ToString();
            _resolvedDataFolderPath = ControlsConfig.GetWebView2Path(DataFolderPath);
            Logger?.LogInformation("ClaudeWebViewControl: Using WebView2 data folder: {DataFolder}", _resolvedDataFolderPath);
            Logger?.LogTrace("ClaudeWebViewControl: Stack trace:\n{StackTrace}", _initializationStackTrace);
            
            if (!Directory.Exists(_resolvedDataFolderPath))
            {
                Directory.CreateDirectory(_resolvedDataFolderPath);
            }

            var env = await CoreWebView2Environment.CreateAsync(null, _resolvedDataFolderPath);
            await ClaudeWebView.EnsureCoreWebView2Async(env);

            // Navigate to Claude.ai
            ClaudeWebView.Source = new Uri(SourceUrl);

            _isInitialized = true;

            // Apply initial settings
            ApplyTextAreaFont();
            ApplyTextAreaHeight();
            UpdateSendButtonTooltip();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClaudeWebViewControl] Initialization error: {ex.Message}");
        }
    }

    private void WebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            ClaudeWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            ClaudeWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            ClaudeWebView.CoreWebView2.Settings.IsZoomControlEnabled = true;

            ApplyWebViewFontSize();
            WebViewInitialized?.Invoke(this, EventArgs.Empty);
        }
    }

    private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            await InjectEnhancementScripts();
            ApplyWebViewFontSize();
            NavigationCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    #endregion

    #region Property Changed Handlers

    private static void OnSourceUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ClaudeWebViewControl control && control._isInitialized)
        {
            control.ClaudeWebView.Source = new Uri((string)e.NewValue);
        }
    }

    private static void OnWebViewFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ClaudeWebViewControl control && control._isInitialized)
        {
            control.ApplyWebViewFontSize();
        }
    }

    private static void OnTextAreaFontChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ClaudeWebViewControl control)
        {
            control.ApplyTextAreaFont();
        }
    }

    private static void OnTextAreaHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ClaudeWebViewControl control)
        {
            control.ApplyTextAreaHeight();
        }
    }

    private static void OnSendKeyModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ClaudeWebViewControl control)
        {
            control.UpdateSendButtonTooltip();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Inserts text into the native text input area.
    /// </summary>
    /// <param name="text">Text to insert at cursor position.</param>
    public void InsertText(string text)
    {
        var caretIndex = MessageInput.CaretIndex;
        MessageInput.Text = MessageInput.Text.Insert(caretIndex, text);
        MessageInput.CaretIndex = caretIndex + text.Length;
        MessageInput.Focus();
    }

    /// <summary>
    /// Inserts text directly into Claude's web input (ProseMirror editor).
    /// </summary>
    /// <param name="text">Text to insert.</param>
    public async Task InsertTextIntoClaudeInput(string text)
    {
        if (ClaudeWebView.CoreWebView2 == null) return;

        // Use clipboard paste method for reliable ProseMirror insertion
        string? previousClipboard = null;
        try
        {
            if (System.Windows.Clipboard.ContainsText())
            {
                previousClipboard = System.Windows.Clipboard.GetText();
            }
        }
        catch { }

        try
        {
            System.Windows.Clipboard.SetText(text);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClaudeWebViewControl] Clipboard error: {ex.Message}");
            return;
        }

        // Focus input and prepare for paste
        var script = @"
            (function() {
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

                if (!input) return false;

                input.focus();
                const sel = window.getSelection();
                const range = document.createRange();
                range.selectNodeContents(input);
                range.collapse(false);
                sel.removeAllRanges();
                sel.addRange(range);
                return true;
            })();
        ";

        await ClaudeWebView.CoreWebView2.ExecuteScriptAsync(script);
        await Task.Delay(50);

        ClaudeWebView.Focus();
        await Task.Delay(20);

        SendCtrlV();

        await Task.Delay(100);

        // Restore clipboard
        if (previousClipboard != null)
        {
            try
            {
                System.Windows.Clipboard.SetText(previousClipboard);
            }
            catch { }
        }
    }

    /// <summary>
    /// Collapses the text input area.
    /// </summary>
    public void CollapseTextArea()
    {
        if (_isTextAreaCollapsed) return;

        _savedTextAreaHeight = TextAreaRow.Height.Value;

        TextAreaBorder.Visibility = Visibility.Collapsed;
        TextAreaSplitter.Visibility = Visibility.Collapsed;
        CollapsedTextAreaBar.Visibility = Visibility.Visible;

        TextAreaRow.Height = new GridLength(32);
        TextAreaRow.MinHeight = 32;
        TextAreaRow.MaxHeight = 32;

        _isTextAreaCollapsed = true;
        TextAreaCollapsedEvent?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Expands the text input area.
    /// </summary>
    public void ExpandTextArea()
    {
        if (!_isTextAreaCollapsed) return;

        TextAreaBorder.Visibility = Visibility.Visible;
        TextAreaSplitter.Visibility = Visibility.Visible;
        CollapsedTextAreaBar.Visibility = Visibility.Collapsed;

        TextAreaRow.MinHeight = 60;
        TextAreaRow.MaxHeight = 400;
        TextAreaRow.Height = new GridLength(_savedTextAreaHeight);

        _isTextAreaCollapsed = false;
        MessageInput.Focus();
        TextAreaExpandedEvent?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the message history.
    /// </summary>
    public IReadOnlyList<string> GetMessageHistory() => _messageHistory.AsReadOnly();

    /// <summary>
    /// Clears the message history.
    /// </summary>
    public void ClearMessageHistory()
    {
        _messageHistory.Clear();
        _historyIndex = -1;
    }

    /// <summary>
    /// Gets the current text area height from the row definition.
    /// </summary>
    public double GetCurrentTextAreaHeight() => TextAreaRow.Height.Value;

    #endregion

    #region Private Methods - UI

    private void ApplyTextAreaFont()
    {
        MessageInput.FontFamily = new System.Windows.Media.FontFamily(TextAreaFontFamily);
        MessageInput.FontSize = TextAreaFontSize;
    }

    private void ApplyTextAreaHeight()
    {
        if (!_isTextAreaCollapsed)
        {
            TextAreaRow.Height = new GridLength(TextAreaHeight);
        }
    }

    private void ApplyWebViewFontSize()
    {
        if (ClaudeWebView.CoreWebView2 == null) return;

        var script = $@"
            (function() {{
                document.documentElement.style.fontSize = '{WebViewFontSize}px';
                const style = document.getElementById('ds-claude-font-style') || document.createElement('style');
                style.id = 'ds-claude-font-style';
                style.textContent = `
                    body, p, span, div, li, td, th {{
                        font-size: {WebViewFontSize}px !important;
                    }}
                    pre, code {{
                        font-size: {Math.Max(12, WebViewFontSize - 2)}px !important;
                    }}
                `;
                if (!style.parentNode) document.head.appendChild(style);
            }})();
        ";

        _ = ClaudeWebView.CoreWebView2.ExecuteScriptAsync(script);
    }

    private void UpdateSendButtonTooltip()
    {
        var shortcut = SendKeyMode switch
        {
            SendKeyMode.ShiftEnter => "Shift+Enter",
            SendKeyMode.CtrlEnter => "Ctrl+Enter",
            SendKeyMode.Enter => "Enter",
            _ => "Shift+Enter"
        };
        SendButton.ToolTip = $"Send message ({shortcut})";
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
        var resourceName = $"DS_ClaudeClient.Controls.Scripts.{scriptName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return GetInlineEnhancementScript();
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string GetInlineEnhancementScript()
    {
        return """
            (function() {
                window.insertSnippetText = function(text) {
                    const selectors = [
                        'div.ProseMirror[contenteditable="true"]',
                        '[contenteditable="true"].ProseMirror',
                        'div[contenteditable="true"][data-placeholder]',
                        'div[contenteditable="true"]'
                    ];

                    let targetInput = null;
                    for (const selector of selectors) {
                        targetInput = document.querySelector(selector);
                        if (targetInput) break;
                    }

                    if (!targetInput) return false;

                    targetInput.focus();
                    const textNode = document.createTextNode(text);
                    const sel = window.getSelection();
                    
                    if (sel.rangeCount > 0) {
                        const range = sel.getRangeAt(0);
                        range.deleteContents();
                        range.insertNode(textNode);
                        range.setStartAfter(textNode);
                        range.setEndAfter(textNode);
                        sel.removeAllRanges();
                        sel.addRange(range);
                    }

                    targetInput.dispatchEvent(new InputEvent('input', { bubbles: true }));
                    return true;
                };
            })();
            """;
    }

    #endregion

    #region Event Handlers - UI

    private void MessageInput_KeyDown(object sender, KeyEventArgs e)
    {
        // Handle send shortcuts
        if (e.Key == Key.Enter)
        {
            bool shouldSend = SendKeyMode switch
            {
                SendKeyMode.Enter => !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift),
                SendKeyMode.ShiftEnter => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift),
                SendKeyMode.CtrlEnter => Keyboard.Modifiers.HasFlag(ModifierKeys.Control),
                _ => false
            };

            if (shouldSend)
            {
                e.Handled = true;
                SendMessage();
            }
        }

        // Handle history navigation
        if (e.Key == Key.Up && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            NavigateHistory(-1);
            e.Handled = true;
        }
        else if (e.Key == Key.Down && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            NavigateHistory(1);
            e.Handled = true;
        }
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        SendMessage();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        MessageInput.Clear();
        MessageInput.Focus();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(MessageInput.Text))
        {
            try
            {
                System.Windows.Clipboard.SetText(MessageInput.Text);
            }
            catch { }
        }
    }

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

    #endregion

    #region Private Methods - Messaging

    private async void SendMessage()
    {
        var text = MessageInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        // Add to history
        _messageHistory.Add(text);
        _historyIndex = _messageHistory.Count;

        // Insert into Claude's input
        await InsertTextIntoClaudeInput(text);

        // Wait and click send
        await Task.Delay(100);
        await ClickClaudeSendButton();

        // Clear input
        MessageInput.Clear();

        MessageSent?.Invoke(this, text);
    }

    private void NavigateHistory(int direction)
    {
        if (_messageHistory.Count == 0) return;

        _historyIndex = Math.Clamp(_historyIndex + direction, 0, _messageHistory.Count - 1);
        MessageInput.Text = _messageHistory[_historyIndex];
        MessageInput.CaretIndex = MessageInput.Text.Length;
    }

    private async Task ClickClaudeSendButton()
    {
        if (ClaudeWebView.CoreWebView2 == null) return;

        var script = @"
            (function() {
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
                            return true;
                        }
                    }
                }

                // Fallback
                const input = document.querySelector('div.ProseMirror[contenteditable=""true""]');
                if (input) {
                    const container = input.closest('fieldset') || input.closest('form') || input.parentElement?.parentElement?.parentElement;
                    if (container) {
                        const buttons = container.querySelectorAll('button:not([disabled])');
                        for (const btn of buttons) {
                            if (btn.querySelector('svg')) {
                                btn.click();
                                return true;
                            }
                        }
                    }
                }
                return false;
            })();
        ";

        await ClaudeWebView.CoreWebView2.ExecuteScriptAsync(script);
    }

    #endregion

    #region Native Methods

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const byte VK_CONTROL = 0x11;
    private const byte VK_V = 0x56;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private static void SendCtrlV()
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(VK_V, 0, 0, UIntPtr.Zero);
        keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    #endregion
}
