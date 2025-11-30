# DS Claude Client

A custom Windows desktop client for Claude AI with enhanced features, built with .NET 10 and WPF.

## Features

- **Embedded Claude.ai** - Full Claude.ai experience in a native Windows app via WebView2
- **Enhanced Text Input Area** - Native WPF text input below the WebView
  - Shift+Enter or Ctrl+Enter to send messages
  - Resizable input area (drag the splitter)
  - Configurable font family and size
  - Configurable width
- **Message History** - In-memory history of sent messages
  - Access via title bar button or Menu > Message History
  - Copy any previous message to clipboard
- **Snippets Panel** - Resizable panel for quick text snippet insertion
  - Double-click to insert snippets into text input or Claude's input
  - Search/filter snippets
  - Inline add/edit/delete
  - Drag-and-drop reordering
  - Sort by title, date, or custom order
  - Import/export snippets as JSON
  - JSON persistence (auto-detects OneDrive, Documents, or app folder)
- **Modern UI** - Clean, light-themed interface
  - Custom title bar with window controls
  - Resizable snippets panel (200-500px)
  - Resizable text input area (60-400px height)
- **Always on Top** - Toggle to keep window above other applications
- **Configurable Fonts** -
  - Browser font size (10-20px)
  - Text area font family (including JetBrains Mono, Cascadia Code, etc.)
  - Text area font size (10-24px)
- **Settings Persistence** - Remembers all preferences including:
  - Window size, position, and maximized state
  - Font settings
  - Panel visibility and sizes
  - Always on top state

## Download

Download the latest installer from the [Releases](https://github.com/dsodol/DS_ClaudeClient/releases) page.

## Requirements

- Windows 10 version 1809 or later
- [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (usually pre-installed on Windows 10/11)

For development:
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Building from Source

```bash
# Clone the repository
git clone https://github.com/dsodol/DS_ClaudeClient.git
cd DS_ClaudeClient

# Build the project
dotnet build DS_ClaudeClient.sln

# Run the application
dotnet run --project DS_ClaudeClient/DS_ClaudeClient.csproj
```

## Publishing

To create a standalone executable:

```bash
# Self-contained single-file executable
dotnet publish DS_ClaudeClient/DS_ClaudeClient.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish

# Framework-dependent (requires .NET runtime installed)
dotnet publish DS_ClaudeClient/DS_ClaudeClient.csproj -c Release -r win-x64 --self-contained false -o ./publish
```

## Usage

1. Launch the application
2. Log in to Claude.ai in the embedded browser
3. Type messages in the text input area at the bottom
4. Press Shift+Enter or Ctrl+Enter to send (or click Send button)
5. Use the snippets panel on the right to quickly insert common prompts
6. Access message history via the scroll icon in the title bar

## Keyboard Shortcuts

- **Shift+Enter** or **Ctrl+Enter** - Send message
- **Enter** - New line in text input

## Title Bar Buttons

- **Menu (hamburger)** - Access menu options
- **Options (gear)** - Configure fonts and text area settings
- **History (scroll)** - View and copy previous messages
- **Pin** - Toggle always on top
- **Clipboard** - Toggle snippets panel
- **Minimize/Maximize/Close** - Standard window controls

## Data Storage

- **Snippets**: Stored in `OneDrive/DS_ClaudeClient/snippets.json`, `Documents/DS_ClaudeClient/snippets.json`, or `%LOCALAPPDATA%/DS_ClaudeClient/snippets.json` (first found location wins)
- **Settings**: Stored in `%LOCALAPPDATA%/DS_ClaudeClient/settings.json`
- **WebView2 Data**: Stored in `%LOCALAPPDATA%/DS_ClaudeClient/WebView2/` (login sessions persist here)

## License

MIT
