using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DS_ClaudeClient;

public partial class OptionsDialog : Window
{
    public new int FontSize { get; private set; }
    public string SelectedFontFamily { get; private set; }
    public int TextAreaWidth { get; private set; }
    public string TextAreaFontFamily { get; private set; }
    public int TextAreaFontSize { get; private set; }

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

    public OptionsDialog(int currentFontSize, string currentFontFamily, int currentTextAreaWidth,
        string currentTextAreaFontFamily, int currentTextAreaFontSize)
    {
        InitializeComponent();
        FontSize = currentFontSize;
        SelectedFontFamily = currentFontFamily;
        TextAreaWidth = currentTextAreaWidth;
        TextAreaFontFamily = currentTextAreaFontFamily;
        TextAreaFontSize = currentTextAreaFontSize;

        FontSizeSlider.Value = currentFontSize;
        FontSizeLabel.Text = $"{currentFontSize}px";
        TextAreaWidthSlider.Value = currentTextAreaWidth;
        TextAreaWidthLabel.Text = $"{currentTextAreaWidth}px";
        TextAreaFontSizeSlider.Value = currentTextAreaFontSize;
        TextAreaFontSizeLabel.Text = $"{currentTextAreaFontSize}px";

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

    private void TextAreaWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TextAreaWidthLabel == null) return;
        TextAreaWidthLabel.Text = $"{(int)e.NewValue}px";
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

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        FontSize = (int)FontSizeSlider.Value;
        SelectedFontFamily = FontFamilyComboBox.SelectedItem as string ?? "Segoe UI";
        TextAreaWidth = (int)TextAreaWidthSlider.Value;
        TextAreaFontFamily = TextAreaFontFamilyComboBox.SelectedItem as string ?? "Segoe UI";
        TextAreaFontSize = (int)TextAreaFontSizeSlider.Value;
        DialogResult = true;
        Close();
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
}
