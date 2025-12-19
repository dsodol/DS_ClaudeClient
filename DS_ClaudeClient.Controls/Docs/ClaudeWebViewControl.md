# ClaudeWebViewControl

A WebView2-based WPF UserControl for browsing Claude.ai with an integrated native text input area.

## Overview

`ClaudeWebViewControl` provides a complete Claude.ai browsing experience with:
- Persistent login sessions (cookies stored locally)
- Native text input area for composing messages
- Multiple keyboard shortcuts for sending
- Message history navigation
- Direct text injection into Claude's editor

---

## Installation

**Add project reference:**
```xml
<ProjectReference Include="..\DS_ClaudeClient.Controls\DS_ClaudeClient.Controls.csproj" />
```

**Add namespace in XAML:**
```xml
xmlns:controls="clr-namespace:DS_ClaudeClient.Controls;assembly=DS_ClaudeClient.Controls"
```

---

## Usage

### Zero-Config (Recommended for Quick Start)

```xml
<controls:ClaudeWebViewControl />
```

This will:
- Navigate to `https://claude.ai`
- Store WebView2 data in `%LocalAppData%\DS_ClaudeClient\WebView2\`
- Use default fonts and sizes
- Use Shift+Enter to send messages

### Configured Usage

```xml
<controls:ClaudeWebViewControl
    x:Name="WebViewControl"
    DataFolderPath="C:\MyApp\Data"
    SourceUrl="https://claude.ai"
    WebViewFontSize="16"
    TextAreaFontFamily="Consolas"
    TextAreaFontSize="14"
    TextAreaHeight="150"
    SendKeyMode="CtrlEnter"
    MessageSent="OnMessageSent"
    WebViewInitialized="OnWebViewReady" />
```

---

## Properties

### Storage Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DataFolderPath` | `string?` | `%LocalAppData%\DS_ClaudeClient\` | Base folder for WebView2 user data (cookies, cache, sessions) |
| `SourceUrl` | `string` | `https://claude.ai` | URL to navigate to on load |

### Font Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `WebViewFontSize` | `int` | `14` | Font size applied to WebView content via CSS injection |
| `TextAreaFontFamily` | `string` | `Segoe UI` | Font family for native text input area |
| `TextAreaFontSize` | `int` | `14` | Font size for native text input area |

### Layout Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `TextAreaHeight` | `double` | `100` | Initial height of the text input area (pixels) |

### Behavior Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SendKeyMode` | `SendKeyMode` | `ShiftEnter` | Keyboard shortcut to send messages |

**SendKeyMode Values:**
- `ShiftEnter` - Shift+Enter sends, Enter adds newline
- `CtrlEnter` - Ctrl+Enter sends, Enter adds newline  
- `Enter` - Enter sends, Shift+Enter adds newline

### Read-Only Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsTextAreaCollapsed` | `bool` | Whether the text area is currently collapsed |
| `InputText` | `string` | Get/set the text in the input area |

---

## Events

| Event | Args | Description |
|-------|------|-------------|
| `MessageSent` | `EventHandler<string>` | Raised when a message is sent to Claude. Arg is the message text. |
| `WebViewInitialized` | `EventHandler` | Raised when WebView2 initialization completes |
| `NavigationCompleted` | `EventHandler` | Raised when page navigation completes |
| `TextAreaCollapsedEvent` | `EventHandler` | Raised when text area is collapsed |
| `TextAreaExpandedEvent` | `EventHandler` | Raised when text area is expanded |

### Event Example

```csharp
public MainWindow()
{
    InitializeComponent();
    WebViewControl.MessageSent += OnMessageSent;
    WebViewControl.WebViewInitialized += OnWebViewReady;
}

private void OnMessageSent(object? sender, string message)
{
    // Log or process the sent message
    Debug.WriteLine($"Sent: {message}");
}

private void OnWebViewReady(object? sender, EventArgs e)
{
    // WebView is ready - can now interact with it
    StatusText.Text = "Ready";
}
```

---

## Methods

### Text Insertion

```csharp
// Insert text into native text area at cursor position
WebViewControl.InsertText("Hello world");

// Insert text directly into Claude's web editor (ProseMirror)
await WebViewControl.InsertTextIntoClaudeInput("Hello Claude!");
```

### Text Area Control

```csharp
// Collapse the text area to a thin bar
WebViewControl.CollapseTextArea();

// Expand the text area
WebViewControl.ExpandTextArea();

// Get current height
double height = WebViewControl.GetCurrentTextAreaHeight();
```

### Message History

```csharp
// Get all sent messages
IReadOnlyList<string> history = WebViewControl.GetMessageHistory();

// Clear history
WebViewControl.ClearMessageHistory();
```

**Keyboard Navigation:**
- `Alt+Up` - Navigate to previous message in history
- `Alt+Down` - Navigate to next message in history

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Shift+Enter` / `Ctrl+Enter` / `Enter` | Send message (based on SendKeyMode) |
| `Alt+Up` | Previous message from history |
| `Alt+Down` | Next message from history |

---

## Data Storage

WebView2 stores its data in a subfolder called `WebView2` under the `DataFolderPath`:

```
DataFolderPath/
└── WebView2/
    ├── EBWebView/
    │   ├── Default/
    │   │   ├── Cookies
    │   │   ├── Cache/
    │   │   └── ...
    │   └── ...
    └── ...
```

This enables:
- Persistent login to Claude.ai
- Saved preferences
- Cached resources for faster loading

---

## Complete Example

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:controls="clr-namespace:DS_ClaudeClient.Controls;assembly=DS_ClaudeClient.Controls"
        Title="My Claude App" Height="700" Width="1000">
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Toolbar -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="8">
            <Button Content="Collapse" Click="Collapse_Click"/>
            <Button Content="Expand" Click="Expand_Click" Margin="8,0"/>
            <TextBlock x:Name="StatusText" VerticalAlignment="Center"/>
        </StackPanel>

        <!-- Claude WebView -->
        <controls:ClaudeWebViewControl 
            x:Name="WebView"
            Grid.Row="1"
            SendKeyMode="ShiftEnter"
            TextAreaHeight="120"
            MessageSent="OnMessageSent"
            WebViewInitialized="OnWebViewReady"/>

        <!-- Status Bar -->
        <TextBlock Grid.Row="2" x:Name="MessageCount" Margin="8"/>
    </Grid>
</Window>
```

```csharp
public partial class MainWindow : Window
{
    private int _messageCount = 0;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnWebViewReady(object? sender, EventArgs e)
    {
        StatusText.Text = "Connected to Claude.ai";
    }

    private void OnMessageSent(object? sender, string message)
    {
        _messageCount++;
        MessageCount.Text = $"Messages sent: {_messageCount}";
    }

    private void Collapse_Click(object sender, RoutedEventArgs e)
    {
        WebView.CollapseTextArea();
    }

    private void Expand_Click(object sender, RoutedEventArgs e)
    {
        WebView.ExpandTextArea();
    }
}
```

---

## Logging

The control supports diagnostic logging via `Microsoft.Extensions.Logging.ILogger`.

### Logger Property

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Logger` | `ILogger?` | `null` | Logger instance for diagnostic output |

### What Gets Logged

| Level | Message | When |
|-------|---------|------|
| Information | `ClaudeWebViewControl: Using WebView2 data folder: {path}` | On control load |

### Usage

```csharp
// In your app, pass your logger to the control
ClaudeWebView.Logger = myLogger;
```

```xml
<!-- Or set in XAML if your logger is available as a resource -->
<controls:ClaudeWebViewControl Logger="{Binding Logger}" />
```

The control logs the resolved WebView2 data folder path at Information level when it initializes. This helps diagnose which folder is being used for cookies, cache, and session data.

---

## Troubleshooting

### WebView2 Runtime Not Installed

If WebView2 fails to initialize, ensure the WebView2 runtime is installed:
- Download from: https://developer.microsoft.com/en-us/microsoft-edge/webview2/
- Or install via NuGet: `Microsoft.Web.WebView2`

### Login Not Persisting

Check that `DataFolderPath` points to a writable location with sufficient permissions.

### Text Not Inserting into Claude

Claude.ai uses a ProseMirror editor which can be tricky. The control uses clipboard-based paste as a fallback. Ensure:
1. WebView has focus
2. The page has fully loaded (wait for `NavigationCompleted`)
