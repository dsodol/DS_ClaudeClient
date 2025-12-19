# SnippetsPanel

A self-contained WPF UserControl for managing reusable text snippets with full CRUD operations.

## Overview

`SnippetsPanel` provides complete snippet management:
- Create, read, update, delete snippets
- Search and filter
- Multiple sort modes
- Drag-and-drop reordering
- Import/Export (JSON format)
- Persistent storage
- Configurable activation handling (event, command, or callback)

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
<controls:SnippetsPanel />
```

This will:
- Store snippets in `%LocalAppData%\DS_ClaudeClient\snippets.json`
- Use default sort mode (Custom/manual ordering)
- Display with built-in dark theme

### Configured Usage

```xml
<controls:SnippetsPanel
    x:Name="SnippetsPanel"
    DataFolderPath="C:\MyApp\Data"
    SnippetsFileName="my-snippets.json"
    SortMode="Title"
    SnippetActivated="OnSnippetActivated" />
```

Or with explicit file path:
```xml
<controls:SnippetsPanel
    SnippetsFilePath="C:\Users\Me\OneDrive\snippets.json" />
```

---

## Properties

### Storage Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DataFolderPath` | `string?` | `%LocalAppData%\DS_ClaudeClient\` | Base folder for snippets file |
| `SnippetsFileName` | `string?` | `snippets.json` | Name of the snippets file |
| `SnippetsFilePath` | `string?` | `null` | Full path to snippets file (overrides DataFolderPath + SnippetsFileName) |

**Path Resolution Priority:**
1. `SnippetsFilePath` (if set, used directly)
2. `DataFolderPath` + `SnippetsFileName`
3. Default: `%LocalAppData%\DS_ClaudeClient\snippets.json`

### Behavior Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SortMode` | `SnippetSortMode` | `Custom` | How snippets are sorted |

**SnippetSortMode Values:**
- `Custom` - Manual ordering via drag-and-drop
- `Title` - Alphabetical by title
- `DateCreated` - Chronological by creation date

---

## Snippet Activation (Double-Click) Configuration

The panel provides **three ways** to handle snippet activation (double-click). Use whichever fits your architecture:

### Option 1: Event Handler (Classic WPF)

```xml
<controls:SnippetsPanel SnippetActivated="OnSnippetActivated" />
```

```csharp
private void OnSnippetActivated(object? sender, Snippet snippet)
{
    MessageBox.Show($"Activated: {snippet.Title}");
    // Insert snippet.Content somewhere
}
```

### Option 2: ICommand (MVVM)

```xml
<controls:SnippetsPanel ActivateCommand="{Binding InsertSnippetCommand}" />
```

```csharp
// In your ViewModel
public ICommand InsertSnippetCommand { get; }

public MyViewModel()
{
    InsertSnippetCommand = new RelayCommand<Snippet>(snippet =>
    {
        // snippet.Content is available here
        TextInput += snippet.Content;
    });
}
```

### Option 3: Action Callback (Simple Scenarios)

```xml
<controls:SnippetsPanel x:Name="Snippets" />
```

```csharp
public MainWindow()
{
    InitializeComponent();
    
    // Set callback directly
    Snippets.OnSnippetActivated = snippet =>
    {
        WebView.InsertText(snippet.Content);
    };
}
```

### All Three Together

All three mechanisms fire on double-click, so you can mix and match:

```xml
<controls:SnippetsPanel 
    SnippetActivated="OnSnippetActivated"
    ActivateCommand="{Binding LogCommand}"
    OnSnippetActivated="{Binding InsertCallback}" />
```

---

## Selection (Single-Click) Configuration

Similar to activation, selection also supports event and command:

### Event

```xml
<controls:SnippetsPanel SnippetSelected="OnSnippetSelected" />
```

```csharp
private void OnSnippetSelected(object? sender, Snippet snippet)
{
    PreviewText.Text = snippet.Content;
}
```

### Command

```xml
<controls:SnippetsPanel SelectCommand="{Binding PreviewSnippetCommand}" />
```

---

## Events Summary

| Event | Args | Trigger |
|-------|------|---------|
| `SnippetSelected` | `EventHandler<Snippet>` | Single click on snippet |
| `SnippetActivated` | `EventHandler<Snippet>` | Double click on snippet |
| `SnippetsChanged` | `EventHandler` | Any CRUD operation (add, edit, delete, reorder) |

---

## Commands Summary

| Property | Type | Parameter | Trigger |
|----------|------|-----------|---------|
| `SelectCommand` | `ICommand` | `Snippet` | Single click |
| `ActivateCommand` | `ICommand` | `Snippet` | Double click |

---

## Callbacks Summary

| Property | Type | Trigger |
|----------|------|---------|
| `OnSnippetActivated` | `Action<Snippet>` | Double click |

---

## Methods

### Data Operations

```csharp
// Reload snippets from file
SnippetsPanel.LoadSnippets();

// Refresh display with optional search filter
SnippetsPanel.RefreshList();
SnippetsPanel.RefreshList("search term");

// Get all snippets
List<Snippet> all = SnippetsPanel.GetAllSnippets();

// Get current file path
string path = SnippetsPanel.GetCurrentFilePath();
```

### Import/Export

```csharp
// Import snippets from file (adds to existing)
int count = SnippetsPanel.ImportSnippets("C:\\import.json");

// Export all snippets to file
SnippetsPanel.ExportSnippets("C:\\export.json");
```

---

## Snippet Model

```csharp
public class Snippet
{
    public string Id { get; set; }           // Unique GUID
    public string Title { get; set; }        // Display title
    public string Content { get; set; }      // The actual snippet text
    public DateTime CreatedAt { get; set; }  // Creation timestamp
    public DateTime ModifiedAt { get; set; } // Last modified timestamp
    public int Order { get; set; }           // Sort order for Custom mode
    public string Preview { get; }           // First 100 chars (read-only)
}
```

---

## JSON File Format

Snippets are stored in JSON format:

```json
[
  {
    "Id": "a1b2c3d4-...",
    "Title": "Greeting",
    "Content": "Hello! How can I help you today?",
    "CreatedAt": "2024-01-15T10:30:00Z",
    "ModifiedAt": "2024-01-15T10:30:00Z",
    "Order": 0
  },
  {
    "Id": "e5f6g7h8-...",
    "Title": "Code Review Request",
    "Content": "Please review this code for:\n- Bugs\n- Performance\n- Best practices",
    "CreatedAt": "2024-01-16T14:00:00Z",
    "ModifiedAt": "2024-01-16T14:00:00Z",
    "Order": 1
  }
]
```

### Legacy Format Support

The panel also supports importing from a legacy `Text/Description` format:

```json
[
  {
    "Text": "Hello! How can I help you today?",
    "Description": "Greeting"
  }
]
```

---

## UI Features

### Search

Type in the search box to filter snippets by title or content.

### Sort

Click the sort button (↕) to cycle through:
1. Custom (manual order)
2. Title (A-Z, then Z-A)
3. Date Created (oldest first, then newest first)

### Drag and Drop

In Custom sort mode, drag snippets to reorder them. Order is persisted automatically.

### Inline Actions

Each snippet row has:
- ✎ Edit button - Opens edit dialog
- ✕ Delete button - Confirms and deletes

### Import

Click the folder icon (📂) to import snippets from a JSON file. Imported snippets are **added** to existing ones (not replaced).

---

## Complete Example

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:controls="clr-namespace:DS_ClaudeClient.Controls;assembly=DS_ClaudeClient.Controls"
        Title="Snippet Demo" Height="600" Width="900">
    
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="4"/>
            <ColumnDefinition Width="300"/>
        </Grid.ColumnDefinitions>

        <!-- Main content area -->
        <Grid Grid.Column="0">
            <Grid.RowDefinitions>
                <RowDefinition Height="*"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            
            <TextBox x:Name="PreviewBox" 
                     Grid.Row="0"
                     IsReadOnly="True"
                     TextWrapping="Wrap"
                     VerticalScrollBarVisibility="Auto"
                     Margin="8"
                     Text="Select a snippet to preview..."/>
            
            <TextBox x:Name="InputBox" 
                     Grid.Row="1"
                     Height="100"
                     TextWrapping="Wrap"
                     Margin="8"/>
        </Grid>

        <GridSplitter Grid.Column="1" Width="4" HorizontalAlignment="Stretch"/>

        <!-- Snippets Panel -->
        <controls:SnippetsPanel 
            x:Name="Snippets"
            Grid.Column="2"
            SnippetSelected="OnSnippetSelected"
            SnippetActivated="OnSnippetActivated"
            SnippetsChanged="OnSnippetsChanged"/>
    </Grid>
</Window>
```

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnSnippetSelected(object? sender, Snippet snippet)
    {
        // Preview on single click
        PreviewBox.Text = $"Title: {snippet.Title}\n\n{snippet.Content}";
    }

    private void OnSnippetActivated(object? sender, Snippet snippet)
    {
        // Insert on double click
        var caretIndex = InputBox.CaretIndex;
        InputBox.Text = InputBox.Text.Insert(caretIndex, snippet.Content);
        InputBox.CaretIndex = caretIndex + snippet.Content.Length;
        InputBox.Focus();
    }

    private void OnSnippetsChanged(object? sender, EventArgs e)
    {
        // Optional: React to changes
        Title = $"Snippet Demo - {Snippets.GetAllSnippets().Count} snippets";
    }
}
```

---

## MVVM Example

```xml
<controls:SnippetsPanel 
    DataFolderPath="{Binding DataPath}"
    SortMode="{Binding CurrentSortMode}"
    SelectCommand="{Binding PreviewCommand}"
    ActivateCommand="{Binding InsertCommand}" />
```

```csharp
public class MainViewModel : INotifyPropertyChanged
{
    public string DataPath => Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData) + "\\MyApp";
    
    public SnippetSortMode CurrentSortMode { get; set; } = SnippetSortMode.Title;

    public ICommand PreviewCommand { get; }
    public ICommand InsertCommand { get; }

    public string PreviewText { get; set; }
    public string InputText { get; set; }

    public MainViewModel()
    {
        PreviewCommand = new RelayCommand<Snippet>(s => PreviewText = s.Content);
        InsertCommand = new RelayCommand<Snippet>(s => InputText += s.Content);
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

### Diagnostic Properties

| Property | Type | Description |
|----------|------|-------------|
| `InitializationStackTrace` | `string?` | Full stack trace captured when snippets file was loaded |
| `ResolvedFilePath` | `string?` | The resolved path to the snippets file being used |

### What Gets Logged

| Level | Message | When |
|-------|---------|------|
| Information | `SnippetsPanel: Requested path: {path}` | On control load |
| Information | `SnippetsPanel: SnippetService actual path: {path}` | On control load |
| Information | `SnippetsPanel: Loaded {count} snippets from {path}` | After loading |
| Information | `SnippetsPanel: First snippets: {titles}` | After loading (if any) |
| Trace | `SnippetsPanel: Stack trace: {trace}` | On control load |

### Usage

```csharp
// Pass your logger to the control
SnippetsPanel.Logger = myLogger;

// After load, you can access the diagnostic info directly:
string filePath = SnippetsPanel.ResolvedFilePath;
string stackTrace = SnippetsPanel.InitializationStackTrace;
```

The control captures the full stack trace at initialization time. This helps diagnose which code path led to file loading and what file path was resolved.

---

## Troubleshooting

### Snippets Not Saving

- Check that `DataFolderPath` or `SnippetsFilePath` points to a writable location
- Ensure the directory exists or can be created

### Import Not Working

- Ensure the import file is valid JSON
- Check the console/debug output for parsing errors
- Both native format (`Title/Content`) and legacy format (`Text/Description`) are supported

### Drag-Drop Not Working

- Drag-drop reordering only works in `Custom` sort mode
- Switch to Custom mode using the sort button
