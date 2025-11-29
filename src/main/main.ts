import { app, BrowserWindow, ipcMain, Menu, shell, nativeTheme } from 'electron';
import * as path from 'path';
import Store from 'electron-store';
import Anthropic from '@anthropic-ai/sdk';

// Initialize electron store for persisting settings
const store = new Store({
  defaults: {
    apiKey: '',
    model: 'claude-sonnet-4-20250514',
    systemPrompt: 'You are Claude, a helpful AI assistant created by Anthropic.',
    theme: 'system',
    windowBounds: { width: 1000, height: 700 },
    conversations: [] as Array<{
      id: string;
      title: string;
      messages: Array<{ role: string; content: string }>;
      createdAt: string;
    }>,
  },
});

let mainWindow: BrowserWindow | null = null;
let anthropicClient: Anthropic | null = null;

function createWindow(): void {
  const { width, height } = store.get('windowBounds') as { width: number; height: number };

  mainWindow = new BrowserWindow({
    width,
    height,
    minWidth: 600,
    minHeight: 400,
    title: 'Claude Desktop',
    backgroundColor: nativeTheme.shouldUseDarkColors ? '#1a1a2e' : '#ffffff',
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
    },
    frame: true,
    titleBarStyle: 'default',
    icon: path.join(__dirname, '../../assets/icon.png'),
  });

  // Load the renderer
  mainWindow.loadFile(path.join(__dirname, '../renderer/index.html'));

  // Save window bounds on resize
  mainWindow.on('resize', () => {
    if (mainWindow) {
      const [width, height] = mainWindow.getSize();
      store.set('windowBounds', { width, height });
    }
  });

  mainWindow.on('closed', () => {
    mainWindow = null;
  });

  // Create application menu
  createMenu();
}

function createMenu(): void {
  const template: Electron.MenuItemConstructorOptions[] = [
    {
      label: 'File',
      submenu: [
        {
          label: 'New Conversation',
          accelerator: 'CmdOrCtrl+N',
          click: () => mainWindow?.webContents.send('new-conversation'),
        },
        { type: 'separator' },
        {
          label: 'Settings',
          accelerator: 'CmdOrCtrl+,',
          click: () => mainWindow?.webContents.send('open-settings'),
        },
        { type: 'separator' },
        { role: 'quit' },
      ],
    },
    {
      label: 'Edit',
      submenu: [
        { role: 'undo' },
        { role: 'redo' },
        { type: 'separator' },
        { role: 'cut' },
        { role: 'copy' },
        { role: 'paste' },
        { role: 'selectAll' },
      ],
    },
    {
      label: 'View',
      submenu: [
        { role: 'reload' },
        { role: 'forceReload' },
        { role: 'toggleDevTools' },
        { type: 'separator' },
        { role: 'resetZoom' },
        { role: 'zoomIn' },
        { role: 'zoomOut' },
        { type: 'separator' },
        { role: 'togglefullscreen' },
      ],
    },
    {
      label: 'Help',
      submenu: [
        {
          label: 'About Claude Desktop',
          click: () => {
            const { dialog } = require('electron');
            dialog.showMessageBox(mainWindow!, {
              type: 'info',
              title: 'About Claude Desktop',
              message: 'Claude Desktop Client',
              detail: 'Version 1.0.0\nA custom desktop client for Claude AI\n\nPowered by Anthropic',
            });
          },
        },
        {
          label: 'Learn More',
          click: () => shell.openExternal('https://www.anthropic.com'),
        },
      ],
    },
  ];

  const menu = Menu.buildFromTemplate(template);
  Menu.setApplicationMenu(menu);
}

// Initialize Anthropic client
function initializeClient(apiKey: string): void {
  if (apiKey) {
    anthropicClient = new Anthropic({ apiKey });
  }
}

// IPC Handlers
ipcMain.handle('get-settings', () => {
  return {
    apiKey: store.get('apiKey'),
    model: store.get('model'),
    systemPrompt: store.get('systemPrompt'),
    theme: store.get('theme'),
  };
});

ipcMain.handle('save-settings', (_event, settings: {
  apiKey?: string;
  model?: string;
  systemPrompt?: string;
  theme?: string;
}) => {
  if (settings.apiKey !== undefined) {
    store.set('apiKey', settings.apiKey);
    initializeClient(settings.apiKey);
  }
  if (settings.model !== undefined) store.set('model', settings.model);
  if (settings.systemPrompt !== undefined) store.set('systemPrompt', settings.systemPrompt);
  if (settings.theme !== undefined) store.set('theme', settings.theme);
  return true;
});

ipcMain.handle('get-conversations', () => {
  return store.get('conversations');
});

ipcMain.handle('save-conversation', (_event, conversation: {
  id: string;
  title: string;
  messages: Array<{ role: string; content: string }>;
  createdAt: string;
}) => {
  const conversations = store.get('conversations') as Array<typeof conversation>;
  const existingIndex = conversations.findIndex((c) => c.id === conversation.id);

  if (existingIndex >= 0) {
    conversations[existingIndex] = conversation;
  } else {
    conversations.unshift(conversation);
  }

  // Keep only the last 100 conversations
  if (conversations.length > 100) {
    conversations.pop();
  }

  store.set('conversations', conversations);
  return true;
});

ipcMain.handle('delete-conversation', (_event, conversationId: string) => {
  const conversations = store.get('conversations') as Array<{ id: string }>;
  const filtered = conversations.filter((c) => c.id !== conversationId);
  store.set('conversations', filtered);
  return true;
});

ipcMain.handle('send-message', async (_event, messages: Array<{ role: 'user' | 'assistant'; content: string }>) => {
  const apiKey = store.get('apiKey') as string;

  if (!apiKey) {
    throw new Error('API key not configured. Please set your Anthropic API key in settings.');
  }

  if (!anthropicClient) {
    initializeClient(apiKey);
  }

  if (!anthropicClient) {
    throw new Error('Failed to initialize Anthropic client.');
  }

  const model = store.get('model') as string;
  const systemPrompt = store.get('systemPrompt') as string;

  try {
    const response = await anthropicClient.messages.create({
      model,
      max_tokens: 4096,
      system: systemPrompt,
      messages: messages.map((m) => ({
        role: m.role,
        content: m.content,
      })),
    });

    const textContent = response.content.find((c) => c.type === 'text');
    return {
      content: textContent ? textContent.text : '',
      model: response.model,
      usage: response.usage,
    };
  } catch (error: any) {
    if (error.status === 401) {
      throw new Error('Invalid API key. Please check your Anthropic API key in settings.');
    }
    if (error.status === 429) {
      throw new Error('Rate limit exceeded. Please wait a moment and try again.');
    }
    throw new Error(`API Error: ${error.message || 'Unknown error occurred'}`);
  }
});

ipcMain.handle('stream-message', async (event, messages: Array<{ role: 'user' | 'assistant'; content: string }>) => {
  const apiKey = store.get('apiKey') as string;

  if (!apiKey) {
    throw new Error('API key not configured. Please set your Anthropic API key in settings.');
  }

  if (!anthropicClient) {
    initializeClient(apiKey);
  }

  if (!anthropicClient) {
    throw new Error('Failed to initialize Anthropic client.');
  }

  const model = store.get('model') as string;
  const systemPrompt = store.get('systemPrompt') as string;

  try {
    const stream = anthropicClient.messages.stream({
      model,
      max_tokens: 4096,
      system: systemPrompt,
      messages: messages.map((m) => ({
        role: m.role,
        content: m.content,
      })),
    });

    stream.on('text', (text) => {
      mainWindow?.webContents.send('stream-chunk', text);
    });

    const finalMessage = await stream.finalMessage();

    mainWindow?.webContents.send('stream-end', {
      model: finalMessage.model,
      usage: finalMessage.usage,
    });

    return { success: true };
  } catch (error: any) {
    mainWindow?.webContents.send('stream-error', error.message);
    throw error;
  }
});

// App lifecycle
app.whenReady().then(() => {
  // Initialize client if API key exists
  const apiKey = store.get('apiKey') as string;
  if (apiKey) {
    initializeClient(apiKey);
  }

  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

// Handle theme changes
nativeTheme.on('updated', () => {
  mainWindow?.webContents.send('theme-changed', nativeTheme.shouldUseDarkColors);
});
