import { contextBridge, ipcRenderer } from 'electron';

// Expose protected methods that allow the renderer process to use
// ipcRenderer without exposing the entire object
contextBridge.exposeInMainWorld('electronAPI', {
  // Settings
  getSettings: () => ipcRenderer.invoke('get-settings'),
  saveSettings: (settings: {
    apiKey?: string;
    model?: string;
    systemPrompt?: string;
    theme?: string;
  }) => ipcRenderer.invoke('save-settings', settings),

  // Conversations
  getConversations: () => ipcRenderer.invoke('get-conversations'),
  saveConversation: (conversation: {
    id: string;
    title: string;
    messages: Array<{ role: string; content: string }>;
    createdAt: string;
  }) => ipcRenderer.invoke('save-conversation', conversation),
  deleteConversation: (conversationId: string) => ipcRenderer.invoke('delete-conversation', conversationId),

  // Messaging
  sendMessage: (messages: Array<{ role: 'user' | 'assistant'; content: string }>) =>
    ipcRenderer.invoke('send-message', messages),
  streamMessage: (messages: Array<{ role: 'user' | 'assistant'; content: string }>) =>
    ipcRenderer.invoke('stream-message', messages),

  // Event listeners
  onNewConversation: (callback: () => void) => {
    ipcRenderer.on('new-conversation', callback);
    return () => ipcRenderer.removeListener('new-conversation', callback);
  },
  onOpenSettings: (callback: () => void) => {
    ipcRenderer.on('open-settings', callback);
    return () => ipcRenderer.removeListener('open-settings', callback);
  },
  onStreamChunk: (callback: (chunk: string) => void) => {
    const handler = (_event: any, chunk: string) => callback(chunk);
    ipcRenderer.on('stream-chunk', handler);
    return () => ipcRenderer.removeListener('stream-chunk', handler);
  },
  onStreamEnd: (callback: (data: { model: string; usage: any }) => void) => {
    const handler = (_event: any, data: { model: string; usage: any }) => callback(data);
    ipcRenderer.on('stream-end', handler);
    return () => ipcRenderer.removeListener('stream-end', handler);
  },
  onStreamError: (callback: (error: string) => void) => {
    const handler = (_event: any, error: string) => callback(error);
    ipcRenderer.on('stream-error', handler);
    return () => ipcRenderer.removeListener('stream-error', handler);
  },
  onThemeChanged: (callback: (isDark: boolean) => void) => {
    const handler = (_event: any, isDark: boolean) => callback(isDark);
    ipcRenderer.on('theme-changed', handler);
    return () => ipcRenderer.removeListener('theme-changed', handler);
  },
});

// Type definitions for the exposed API
export interface ElectronAPI {
  getSettings: () => Promise<{
    apiKey: string;
    model: string;
    systemPrompt: string;
    theme: string;
  }>;
  saveSettings: (settings: {
    apiKey?: string;
    model?: string;
    systemPrompt?: string;
    theme?: string;
  }) => Promise<boolean>;
  getConversations: () => Promise<Array<{
    id: string;
    title: string;
    messages: Array<{ role: string; content: string }>;
    createdAt: string;
  }>>;
  saveConversation: (conversation: {
    id: string;
    title: string;
    messages: Array<{ role: string; content: string }>;
    createdAt: string;
  }) => Promise<boolean>;
  deleteConversation: (conversationId: string) => Promise<boolean>;
  sendMessage: (messages: Array<{ role: 'user' | 'assistant'; content: string }>) => Promise<{
    content: string;
    model: string;
    usage: any;
  }>;
  streamMessage: (messages: Array<{ role: 'user' | 'assistant'; content: string }>) => Promise<{ success: boolean }>;
  onNewConversation: (callback: () => void) => () => void;
  onOpenSettings: (callback: () => void) => () => void;
  onStreamChunk: (callback: (chunk: string) => void) => () => void;
  onStreamEnd: (callback: (data: { model: string; usage: any }) => void) => () => void;
  onStreamError: (callback: (error: string) => void) => () => void;
  onThemeChanged: (callback: (isDark: boolean) => void) => () => void;
}

declare global {
  interface Window {
    electronAPI: ElectronAPI;
  }
}
