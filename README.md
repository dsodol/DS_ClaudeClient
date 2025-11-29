# DS Claude Client

A custom Windows desktop client for Claude AI with enhanced features, built with .NET 10 and WPF.

## Features

- **Embedded Claude.ai** - Full Claude.ai experience in a native Windows app via WebView2
- **Enhanced Multi-line Input** - Proper Shift+Enter support and expandable text areas
- **Snippets Panel** - Collapsible panel for quick text snippet insertion
  - Double-click to insert snippets directly into Claude's input
  - Search/filter snippets
  - Inline add/edit/delete
  - JSON persistence (auto-detects OneDrive, Documents, or app folder)
- **Frameless Window** - Modern borderless design with custom title bar
- **Always on Top** - Toggle to keep window above other applications
- **Configurable Font Size** - Adjust text size (10-20px)
- **Settings Persistence** - Remembers window size, font size, panel state

## Requirements

- Windows 10 version 1809 or later
- [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (or SDK for development)
- [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (usually pre-installed on Windows 10/11)

## Building

```bash
# Clone the repository
git clone <repo-url>
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
3. Use the snippets panel on the right to quickly insert common prompts
4. Toggle the snippets panel with the clipboard icon in the title bar
5. Pin the window on top with the pin icon
6. Adjust font size using the slider at the bottom of the snippets panel

## Keyboard Shortcuts

- **Shift+Enter** - Insert new line in Claude's input (instead of sending)
- Close the snippets panel to maximize the Claude.ai view area

## Data Storage

- **Snippets**: Stored in `OneDrive/DS_ClaudeClient/snippets.json`, `Documents/DS_ClaudeClient/snippets.json`, or `%LOCALAPPDATA%/DS_ClaudeClient/snippets.json` (first found location wins)
- **Settings**: Stored in `%LOCALAPPDATA%/DS_ClaudeClient/settings.json`

## License

MIT
