using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DS_ClaudeClient.Models;
using DS_ClaudeClient.Services;

namespace DS_ClaudeClient;

public partial class OptionsDialog : Window
{
    public new int FontSize { get; private set; }
    public string SelectedFontFamily { get; private set; }
    public string TextAreaFontFamily { get; private set; }
    public int TextAreaFontSize { get; private set; }
    public SendKeyMode SendKeyMode { get; private set; }
    public string SnippetsFilePath { get; private set; }

    private readonly SnippetService? _snippetService;
    private readonly List<Snippet>? _snippets;

    private static readonly (SendKeyMode Mode, string Display)[] SendKeyOptions = new[]
    {
        (SendKeyMode.ShiftEnter, "Shift+Enter"),
        (SendKeyMode.CtrlEnter, "Ctrl+Enter"),
        (SendKeyMode.Enter, "Enter (plain)")
    };

    private static readonly string[] CommonFonts = new[]
    {
        "Segoe UI",
        "Arial",
        "Calibri",
        "Cascadia Code",
        "Cascadia Mono",
        "Consolas",
        "Courier New",
        "Fira Code",
        "Georgia",
        "JetBrains Mono",
        "Source Code Pro",
        "Tahoma",
        "Times New Roman",
        "Trebuchet MS",
        "Verdana"
    };

    private static readonly string[] TextAreaFonts = new[]
    {
        "Segoe UI",
        "JetBrains Mono",
        "Cascadia Code",
        "Cascadia Mono",
        "Consolas",
        "Courier New",
        "Fira Code",
        "Source Code Pro",
        "Monaco",
        "Menlo"
    };

    public OptionsDialog(int currentFontSize, string currentFontFamily,
        string currentTextAreaFontFamily, int currentTextAreaFontSize, SendKeyMode currentSendKeyMode,
        string currentSnippetsFilePath = "", SnippetService? snippetService = null, List<Snippet>? snippets = null)
    {
        InitializeComponent();
        _snippetService = snippetService;
        _snippets = snippets;
        FontSize = currentFontSize;
        SelectedFontFamily = currentFontFamily;
        TextAreaFontFamily = currentTextAreaFontFamily;
        TextAreaFontSize = currentTextAreaFontSize;
        SendKeyMode = currentSendKeyMode;
        SnippetsFilePath = string.IsNullOrWhiteSpace(currentSnippetsFilePath)
            ? SnippetService.GetDefaultFilePath()
            : currentSnippetsFilePath;

        FontSizeSlider.Value = currentFontSize;
        FontSizeLabel.Text = $"{currentFontSize}px";
        TextAreaFontSizeSlider.Value = currentTextAreaFontSize;
        TextAreaFontSizeLabel.Text = $"{currentTextAreaFontSize}px";
        SnippetsFilePathTextBox.Text = SnippetsFilePath;

        // Populate font family combo box
        foreach (var font in CommonFonts)
        {
            FontFamilyComboBox.Items.Add(font);
        }

        // Select current font
        var index = Array.IndexOf(CommonFonts, currentFontFamily);
        FontFamilyComboBox.SelectedIndex = index >= 0 ? index : 0;

        // Populate text area font family combo box
        foreach (var font in TextAreaFonts)
        {
            TextAreaFontFamilyComboBox.Items.Add(font);
        }

        // Select current text area font
        var textAreaFontIndex = Array.IndexOf(TextAreaFonts, currentTextAreaFontFamily);
        TextAreaFontFamilyComboBox.SelectedIndex = textAreaFontIndex >= 0 ? textAreaFontIndex : 0;

        // Populate send key combo box
        foreach (var option in SendKeyOptions)
        {
            SendKeyComboBox.Items.Add(option.Display);
        }

        // Select current send key mode
        var sendKeyIndex = Array.FindIndex(SendKeyOptions, o => o.Mode == currentSendKeyMode);
        SendKeyComboBox.SelectedIndex = sendKeyIndex >= 0 ? sendKeyIndex : 0;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void FontFamilyComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (FontFamilyComboBox.SelectedItem is string fontName)
        {
            SelectedFontFamily = fontName;
        }
    }

    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FontSizeLabel == null) return;
        FontSizeLabel.Text = $"{(int)e.NewValue}px";
    }

    private void TextAreaFontFamilyComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (TextAreaFontFamilyComboBox.SelectedItem is string fontName)
        {
            TextAreaFontFamily = fontName;
        }
    }

    private void TextAreaFontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TextAreaFontSizeLabel == null) return;
        TextAreaFontSizeLabel.Text = $"{(int)e.NewValue}px";
    }

    private void SendKeyComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (SendKeyComboBox.SelectedIndex >= 0 && SendKeyComboBox.SelectedIndex < SendKeyOptions.Length)
        {
            SendKeyMode = SendKeyOptions[SendKeyComboBox.SelectedIndex].Mode;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        FontSize = (int)FontSizeSlider.Value;
        SelectedFontFamily = FontFamilyComboBox.SelectedItem as string ?? "Segoe UI";
        TextAreaFontFamily = TextAreaFontFamilyComboBox.SelectedItem as string ?? "Segoe UI";
        TextAreaFontSize = (int)TextAreaFontSizeSlider.Value;
        SnippetsFilePath = SnippetsFilePathTextBox.Text;
        if (SendKeyComboBox.SelectedIndex >= 0 && SendKeyComboBox.SelectedIndex < SendKeyOptions.Length)
        {
            SendKeyMode = SendKeyOptions[SendKeyComboBox.SelectedIndex].Mode;
        }
        DialogResult = true;
        Close();
    }

    private void BrowseSnippetsPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Select Snippets Storage Location",
            FileName = System.IO.Path.GetFileName(SnippetsFilePathTextBox.Text),
            InitialDirectory = System.IO.Path.GetDirectoryName(SnippetsFilePathTextBox.Text) ?? ""
        };

        if (dialog.ShowDialog() == true)
        {
            SnippetsFilePathTextBox.Text = dialog.FileName;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ShowLogButton_Click(object sender, RoutedEventArgs e)
    {
        if (_snippetService == null || _snippets == null)
        {
            MessageBox.Show("Snippet service not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var logWindow = new LogWindow(_snippetService, _snippets)
        {
            Owner = this
        };
        logWindow.ShowDialog();
    }
}
