# DS_ClaudeClient.Controls

A reusable WPF control library for building Claude.ai desktop clients.

## Features

- **Zero-config**: Just drop controls into your XAML - they work out of the box
- **Configurable**: Override defaults when needed
- **Self-contained**: Each control manages its own data and services
- **MVVM-friendly**: Supports events, commands, and callbacks

## Controls

| Control | Description | Documentation |
|---------|-------------|---------------|
| `ClaudeWebViewControl` | WebView2 browser + native text input for Claude.ai | [Full Manual](Docs/ClaudeWebViewControl.md) |
| `SnippetsPanel` | Snippet management with CRUD, search, sort, drag-drop | [Full Manual](Docs/SnippetsPanel.md) |

---

## Quick Start

### Installation

Add project reference:
```xml
<ProjectReference Include="..\DS_ClaudeClient.Controls\DS_ClaudeClient.Controls.csproj" />
```

Add namespace:
```xml
xmlns:controls="clr-namespace:DS_ClaudeClient.Controls;assembly=DS_ClaudeClient.Controls"
```

### Zero-Config Usage

```xml
<!-- Just works - no configuration needed -->
<controls:ClaudeWebViewControl />
<controls:SnippetsPanel />
```

### Typical Usage

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="4"/>
        <ColumnDefinition Width="280"/>
    </Grid.ColumnDefinitions>

    <controls:ClaudeWebViewControl x:Name="WebView" Grid.Column="0" />
    <GridSplitter Grid.Column="1" Width="4" />
    <controls:SnippetsPanel Grid.Column="2" SnippetActivated="OnSnippetActivated" />
</Grid>
```

```csharp
private void OnSnippetActivated(object? sender, Snippet snippet)
{
    WebView.InsertText(snippet.Content);
}
```

---

## Default Storage Locations

| Data | Default Path |
|------|--------------|
| WebView2 (cookies, cache) | `%LocalAppData%\DS_ClaudeClient\WebView2\` |
| Snippets | `%LocalAppData%\DS_ClaudeClient\snippets.json` |

Override by setting `DataFolderPath` on either control to share the same folder.

---

## Snippet Activation Options

The `SnippetsPanel` provides **three ways** to handle double-click activation:

```xml
<!-- Option 1: Event (classic WPF) -->
<controls:SnippetsPanel SnippetActivated="OnSnippetActivated" />

<!-- Option 2: Command (MVVM) -->
<controls:SnippetsPanel ActivateCommand="{Binding InsertCommand}" />

<!-- Option 3: Callback (simple) -->
<controls:SnippetsPanel x:Name="Panel" />
```
```csharp
Panel.OnSnippetActivated = snippet => WebView.InsertText(snippet.Content);
```

See [SnippetsPanel Manual](Docs/SnippetsPanel.md) for details.

---

## Theming

Controls use a built-in dark theme. Override in App.xaml:

```xml
<SolidColorBrush x:Key="BackgroundBrush" Color="#1a1a1a"/>
<SolidColorBrush x:Key="AccentBrush" Color="#d97706"/>
```

---

## Documentation

- [ClaudeWebViewControl Manual](Docs/ClaudeWebViewControl.md) - Complete API reference
- [SnippetsPanel Manual](Docs/SnippetsPanel.md) - Complete API reference
