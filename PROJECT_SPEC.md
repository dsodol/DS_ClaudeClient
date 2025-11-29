# DS_ClaudeClient - Project Specification

## Overview

DS_ClaudeClient is a custom Windows desktop client for Claude AI built with .NET 10 and WPF. It wraps the Claude.ai web interface using WebView2 and adds enhanced features for power users.

## Technology Stack

- **Framework:** .NET 10 (Windows-specific)
- **UI:** WPF (Windows Presentation Foundation)
- **Web Engine:** Microsoft WebView2
- **Language:** C# 13
- **Data Format:** JSON (for settings and snippets storage)

## Architecture

```
DS_ClaudeClient/
├── App.xaml / App.xaml.cs       # Application entry, global styles/resources
├── MainWindow.xaml / .cs        # Main window with WebView2 and snippets panel
├── SnippetEditDialog.xaml / .cs # Dialog for creating/editing snippets
├── Models/
│   ├── AppSettings.cs           # User preferences model
│   └── Snippet.cs               # Snippet data model
├── Services/
│   ├── SettingsService.cs       # Persists app settings to JSON
│   └── SnippetService.cs        # Manages snippet storage with cloud sync
├── Scripts/
│   └── ClaudeEnhancements.js    # Injected JS for UI enhancements
└── Converters/
    └── BoolToVisibilityConverter.cs
```

## Features

### 1. Custom Chrome/Title Bar
- Borderless window with custom title bar
- Hamburger menu with quick actions (Refresh, Go to Claude.ai, Dev Tools, Import/Export)
- Always-on-top toggle (pin icon)
- Snippets panel toggle
- Standard window controls (minimize, maximize, close)
- Draggable title bar with double-click maximize

### 2. Snippets System
- **Side panel** with searchable list of reusable text snippets
- **CRUD operations:** Add, Edit, Delete snippets
- **Double-click to insert** snippet content into Claude's input field
- **Search/filter** snippets by title or content
- **Import/Export** to JSON for backup and sharing
- **Cloud sync priority:** Snippets stored in OneDrive > Documents > AppData (first available location)

### 3. WebView2 Integration
- Embedded Claude.ai web interface
- JavaScript injection for enhancements:
  - Expandable/resizable text areas (min 100px, max 400px)
  - Shift+Enter support for multi-line input
  - `window.insertSnippetText()` API for snippet insertion
  - MutationObserver for dynamic content

### 4. User Settings
- **Font size slider** (10px - 20px) with live preview
- **Always on top** preference
- **Snippets panel visibility** state
- **Window dimensions** (restored on startup)
- Settings persisted to `%LocalAppData%\DS_ClaudeClient\settings.json`

### 5. UI Theme
Dark theme with amber accent:
- Background: `#1a1a2e`
- Surface: `#252542`
- Border: `#3d3d5c`
- Primary (accent): `#D97706` (amber)
- Text: `#ffffff` / Secondary: `#a0a0b0`

## Data Storage

| Data | Location | Format |
|------|----------|--------|
| Settings | `%LocalAppData%\DS_ClaudeClient\settings.json` | JSON |
| Snippets | OneDrive/DS_ClaudeClient or Documents/DS_ClaudeClient or %LocalAppData%/DS_ClaudeClient | JSON |

## Build & Deployment

### Prerequisites
- .NET 10 SDK
- Windows 10/11 with WebView2 runtime

### Build Commands
```bash
# Development build
dotnet build

# Release build (framework-dependent)
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish

# Self-contained build (no .NET required on target)
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

### Output
- Primary executable: `DS_ClaudeClient.exe`
- Dependencies: WebView2 DLLs, .NET runtime (if framework-dependent)

## Key Behaviors

1. **Snippet Insertion:** Uses `window.insertSnippetText()` injected function to find Claude's input area and insert text at cursor position, handling both `<textarea>` and `contenteditable` elements.

2. **Settings Persistence:** Settings auto-save on:
   - Font size change
   - Always-on-top toggle
   - Snippets panel toggle
   - Window close

3. **Snippet Storage Priority:** Checks for existing `snippets.json` in priority order (OneDrive → Documents → AppData), enabling cross-device sync for OneDrive users.

## Version
- Current: 1.0.0
