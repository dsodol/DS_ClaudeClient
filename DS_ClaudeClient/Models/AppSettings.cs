namespace DS_ClaudeClient.Models;

public enum SendKeyMode
{
    ShiftEnter,
    CtrlEnter,
    Enter
}

public class AppSettings
{
    public SendKeyMode SendKeyMode { get; set; } = SendKeyMode.ShiftEnter;
    public int FontSize { get; set; } = 14;
    public string FontFamily { get; set; } = "Segoe UI";
    public bool AlwaysOnTop { get; set; } = false;
    public bool SnippetsPanelVisible { get; set; } = true;
    public double SnippetsPanelWidth { get; set; } = 280;
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 700;
    public double WindowLeft { get; set; } = -1;
    public double WindowTop { get; set; } = -1;
    public bool IsMaximized { get; set; } = false;
    public int TextAreaWidth { get; set; } = 800;
    public string TextAreaFontFamily { get; set; } = "Segoe UI";
    public int TextAreaFontSize { get; set; } = 14;
    public double TextAreaHeight { get; set; } = 100;
    public string SnippetsFilePath { get; set; } = "";
}
