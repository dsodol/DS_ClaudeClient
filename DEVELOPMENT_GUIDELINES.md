# WPF Desktop Application Development Guidelines

This document captures reusable development patterns and conventions for building WPF desktop applications with .NET.

---

## 1. Project Structure

```
ProjectName/
├── ProjectName/                    # Main application
│   ├── App.xaml / App.xaml.cs     # Application entry, global resources
│   ├── MainWindow.xaml/.cs        # Primary window
│   ├── Models/                    # Data classes, enums
│   ├── Services/                  # Data access, persistence
│   ├── Converters/                # XAML value converters
│   ├── Resources/                 # Icons, images
│   ├── Scripts/                   # Embedded JS (for WebView2)
│   └── *Dialog.xaml/.cs           # Modal dialogs
├── installer/                     # WiX MSI packaging
│   ├── Package.wxs
│   └── License.rtf
└── publish/                       # Release binaries
```

---

## 2. Architecture Pattern: Code-Behind with Services

Use **Code-Behind** with a **Service Layer** for straightforward applications:

```csharp
public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly DataService _dataService;

    public MainWindow()
    {
        InitializeComponent();
        _settingsService = new SettingsService();
        _dataService = new DataService();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.Load();
        ApplySettings(settings);
    }
}
```

**When to use MVVM instead:** Complex data binding, unit testing requirements, large teams.

---

## 3. Settings Management

### AppSettings Model
```csharp
namespace ProjectName.Models;

public class AppSettings
{
    // Behavior settings with defaults
    public bool FeatureEnabled { get; set; } = true;
    public int DefaultValue { get; set; } = 100;

    // Window state
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 700;
    public double WindowLeft { get; set; } = -1;
    public double WindowTop { get; set; } = -1;
    public bool IsMaximized { get; set; } = false;

    // UI preferences
    public string FontFamily { get; set; } = "Segoe UI";
    public int FontSize { get; set; } = 14;
}
```

### SettingsService Pattern
```csharp
namespace ProjectName.Services;

public class SettingsService
{
    private readonly string _settingsFilePath;

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProjectName");
        Directory.CreateDirectory(appDataPath);
        _settingsFilePath = Path.Combine(appDataPath, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }
}
```

### Standard Paths
```
Settings:     %LOCALAPPDATA%\ProjectName\settings.json
Data:         %LOCALAPPDATA%\ProjectName\data.json
Logs:         %LOCALAPPDATA%\ProjectName\app.log
WebView2:     %LOCALAPPDATA%\ProjectName\WebView2\
```

---

## 4. UI Theming (App.xaml)

### Color Palette
```xaml
<Application.Resources>
    <!-- Define Colors -->
    <Color x:Key="PrimaryColor">#D97706</Color>
    <Color x:Key="PrimaryHoverColor">#B45309</Color>
    <Color x:Key="BackgroundColor">#F5F5F5</Color>
    <Color x:Key="SurfaceColor">#FFFFFF</Color>
    <Color x:Key="BorderColor">#E0E0E0</Color>
    <Color x:Key="TextColor">#1A1A1A</Color>
    <Color x:Key="TextSecondaryColor">#666666</Color>
    <Color x:Key="AccentColor">#FDE68A</Color>
    <Color x:Key="HoverColor">#E8E8E8</Color>

    <!-- Create Brushes from Colors -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}"/>
    <SolidColorBrush x:Key="PrimaryHoverBrush" Color="{StaticResource PrimaryHoverColor}"/>
    <SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource BackgroundColor}"/>
    <SolidColorBrush x:Key="SurfaceBrush" Color="{StaticResource SurfaceColor}"/>
    <SolidColorBrush x:Key="BorderBrush" Color="{StaticResource BorderColor}"/>
    <SolidColorBrush x:Key="TextBrush" Color="{StaticResource TextColor}"/>
    <SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondaryColor}"/>
    <SolidColorBrush x:Key="AccentBrush" Color="{StaticResource AccentColor}"/>
    <SolidColorBrush x:Key="HoverBrush" Color="{StaticResource HoverColor}"/>

    <!-- Converters -->
    <BooleanToVisibilityConverter x:Key="BoolToVisibility"/>
</Application.Resources>
```

### Standard Button Styles
```xaml
<!-- Icon Button (Toolbar/Title bar) -->
<Style x:Key="IconButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="BorderThickness" Value="0"/>
    <Setter Property="Padding" Value="8,4"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Setter Property="Foreground" Value="{StaticResource TextBrush}"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        Padding="{TemplateBinding Padding}"
                        CornerRadius="4">
                    <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter Property="Background" Value="{StaticResource HoverBrush}"/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>

<!-- Primary Action Button -->
<Style x:Key="PrimaryButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="BorderThickness" Value="0"/>
    <Setter Property="Padding" Value="16,8"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border Background="{TemplateBinding Background}"
                        Padding="{TemplateBinding Padding}"
                        CornerRadius="4">
                    <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter Property="Background" Value="{StaticResource PrimaryHoverBrush}"/>
                    </Trigger>
                    <Trigger Property="IsEnabled" Value="False">
                        <Setter Property="Opacity" Value="0.5"/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>

<!-- Secondary/Cancel Button -->
<Style x:Key="SecondaryButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="{StaticResource SurfaceBrush}"/>
    <Setter Property="Foreground" Value="{StaticResource TextBrush}"/>
    <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="Padding" Value="16,8"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        Padding="{TemplateBinding Padding}"
                        CornerRadius="4">
                    <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter Property="Background" Value="{StaticResource HoverBrush}"/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

---

## 5. Dialog Window Pattern

### XAML Template
```xaml
<Window x:Class="ProjectName.MyDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Dialog Title"
        Height="400" Width="350"
        WindowStartupLocation="CenterOwner"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        ResizeMode="NoResize">

    <Border Background="{StaticResource SurfaceBrush}"
            BorderBrush="{StaticResource BorderBrush}"
            BorderThickness="1"
            CornerRadius="8">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="36"/>   <!-- Title bar -->
                <RowDefinition Height="*"/>    <!-- Content -->
                <RowDefinition Height="Auto"/> <!-- Buttons -->
            </Grid.RowDefinitions>

            <!-- Custom Title Bar -->
            <Border Grid.Row="0"
                    Background="{StaticResource BackgroundBrush}"
                    CornerRadius="8,8,0,0"
                    MouseLeftButtonDown="TitleBar_MouseLeftButtonDown">
                <Grid>
                    <TextBlock Text="Dialog Title"
                               Foreground="{StaticResource TextBrush}"
                               FontWeight="SemiBold"
                               VerticalAlignment="Center"
                               HorizontalAlignment="Center"/>
                    <Button HorizontalAlignment="Right"
                            Style="{StaticResource IconButtonStyle}"
                            Click="CloseButton_Click"
                            Margin="0,0,4,0">
                        <TextBlock Text="&#x2715;" FontSize="14"/>
                    </Button>
                </Grid>
            </Border>

            <!-- Content Area -->
            <StackPanel Grid.Row="1" Margin="20">
                <!-- Dialog content here -->
            </StackPanel>

            <!-- Button Footer -->
            <StackPanel Grid.Row="2"
                        Orientation="Horizontal"
                        HorizontalAlignment="Right"
                        Margin="20,10,20,20">
                <Button Content="Cancel"
                        Style="{StaticResource SecondaryButtonStyle}"
                        Click="CancelButton_Click"
                        Margin="0,0,10,0"/>
                <Button Content="Save"
                        Style="{StaticResource PrimaryButtonStyle}"
                        Click="SaveButton_Click"/>
            </StackPanel>
        </Grid>
    </Border>
</Window>
```

### Code-Behind Pattern
```csharp
public partial class MyDialog : Window
{
    // Properties to retrieve dialog results
    public string ResultValue { get; private set; } = string.Empty;
    public int ResultNumber { get; private set; }

    // Constructor with optional initial values
    public MyDialog(string initialValue = "", int initialNumber = 0)
    {
        InitializeComponent();
        ResultValue = initialValue;
        ResultNumber = initialNumber;

        // Initialize UI controls
        ValueTextBox.Text = initialValue;
        NumberSlider.Value = initialNumber;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate and store results
        ResultValue = ValueTextBox.Text;
        ResultNumber = (int)NumberSlider.Value;
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
```

### Dialog Usage
```csharp
var dialog = new MyDialog(currentValue, currentNumber);
dialog.Owner = this;  // Set for modality and centering
if (dialog.ShowDialog() == true)
{
    // Use dialog.ResultValue and dialog.ResultNumber
    _currentValue = dialog.ResultValue;
    _currentNumber = dialog.ResultNumber;
    SaveSettings();
}
```

---

## 6. Error Handling and Logging

### App.xaml.cs Setup
```csharp
public partial class App : Application
{
    public static string LogPath { get; private set; } = string.Empty;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize logging
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProjectName");
        Directory.CreateDirectory(appDataPath);
        LogPath = Path.Combine(appDataPath, "app.log");

        Log("Application starting...");

        // Global UI thread exception handler
        DispatcherUnhandledException += (s, args) =>
        {
            Log($"UNHANDLED UI EXCEPTION: {args.Exception}");
            MessageBox.Show(
                $"An error occurred:\n{args.Exception.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        // Background thread exceptions
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            Log($"UNHANDLED DOMAIN EXCEPTION: {args.ExceptionObject}");
        };

        // Task exceptions
        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            Log($"UNOBSERVED TASK EXCEPTION: {args.Exception}");
            args.SetObserved();
        };
    }

    public static void Log(string message)
    {
        try
        {
            var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}{Environment.NewLine}";
            File.AppendAllText(LogPath, logMessage);
        }
        catch { /* Silently ignore logging errors */ }
    }
}
```

---

## 7. Custom Window with Title Bar

### WindowChrome for Resizable Custom Window
```xaml
<Window x:Class="ProjectName.MainWindow"
        Title="Application Name"
        Height="700" Width="1200"
        MinHeight="400" MinWidth="600"
        WindowStyle="SingleBorderWindow"
        ResizeMode="CanResize">

    <WindowChrome.WindowChrome>
        <WindowChrome CaptionHeight="36"
                      ResizeBorderThickness="6"
                      GlassFrameThickness="0"
                      CornerRadius="0"/>
    </WindowChrome.WindowChrome>

    <Grid Background="{StaticResource BackgroundBrush}">
        <Grid.RowDefinitions>
            <RowDefinition Height="36"/>  <!-- Title bar -->
            <RowDefinition Height="*"/>   <!-- Content -->
        </Grid.RowDefinitions>

        <!-- Custom Title Bar -->
        <Border Grid.Row="0"
                Background="{StaticResource SurfaceBrush}"
                MouseLeftButtonDown="TitleBar_MouseLeftButtonDown">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>  <!-- Menu -->
                    <ColumnDefinition Width="*"/>     <!-- Title -->
                    <ColumnDefinition Width="Auto"/>  <!-- Controls -->
                </Grid.ColumnDefinitions>

                <!-- Menu Button -->
                <Button Grid.Column="0"
                        Style="{StaticResource IconButtonStyle}"
                        Click="MenuButton_Click"
                        WindowChrome.IsHitTestVisibleInChrome="True">
                    <TextBlock Text="&#x2630;" FontSize="16"/>
                </Button>

                <!-- Title -->
                <TextBlock Grid.Column="1"
                           Text="Application Name"
                           Foreground="{StaticResource TextBrush}"
                           FontWeight="SemiBold"
                           VerticalAlignment="Center"
                           HorizontalAlignment="Center"/>

                <!-- Window Controls -->
                <StackPanel Grid.Column="2" Orientation="Horizontal"
                            WindowChrome.IsHitTestVisibleInChrome="True">
                    <Button Style="{StaticResource IconButtonStyle}"
                            Click="MinimizeButton_Click">
                        <TextBlock Text="&#x2212;" FontSize="16"/>
                    </Button>
                    <Button Style="{StaticResource IconButtonStyle}"
                            Click="MaximizeButton_Click"
                            x:Name="MaximizeButton">
                        <TextBlock Text="&#x25A1;" FontSize="14"/>
                    </Button>
                    <Button Style="{StaticResource IconButtonStyle}"
                            Click="CloseButton_Click">
                        <TextBlock Text="&#x2715;" FontSize="14"/>
                    </Button>
                </StackPanel>
            </Grid>
        </Border>

        <!-- Main Content -->
        <Grid Grid.Row="1">
            <!-- Application content here -->
        </Grid>
    </Grid>
</Window>
```

### Title Bar Event Handlers
```csharp
private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    if (e.ClickCount == 2)
        MaximizeButton_Click(sender, e);
    else
        DragMove();
}

private void MinimizeButton_Click(object sender, RoutedEventArgs e)
{
    WindowState = WindowState.Minimized;
}

private void MaximizeButton_Click(object sender, RoutedEventArgs e)
{
    if (WindowState == WindowState.Maximized)
    {
        WindowState = WindowState.Normal;
        ((TextBlock)MaximizeButton.Content).Text = "\u25A1";  // Restore icon
    }
    else
    {
        WindowState = WindowState.Maximized;
        ((TextBlock)MaximizeButton.Content).Text = "\u29C9";  // Maximize icon
    }
}

private void CloseButton_Click(object sender, RoutedEventArgs e)
{
    SaveSettings();
    Close();
}
```

---

## 8. Window State Persistence

### Save Window State
```csharp
private void SaveSettings()
{
    // Get restore bounds when maximized
    var restoreBounds = WindowState == WindowState.Maximized
        ? RestoreBounds
        : new Rect(Left, Top, Width, Height);

    var settings = new AppSettings
    {
        WindowWidth = restoreBounds.Width,
        WindowHeight = restoreBounds.Height,
        WindowLeft = restoreBounds.Left,
        WindowTop = restoreBounds.Top,
        IsMaximized = WindowState == WindowState.Maximized,
        // ... other settings
    };

    _settingsService.Save(settings);
}
```

### Restore Window State
```csharp
private void MainWindow_Loaded(object sender, RoutedEventArgs e)
{
    var settings = _settingsService.Load();

    // Restore window size
    if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
    {
        Width = settings.WindowWidth;
        Height = settings.WindowHeight;
    }

    // Restore window position
    if (settings.WindowLeft >= 0 && settings.WindowTop >= 0)
    {
        Left = settings.WindowLeft;
        Top = settings.WindowTop;
        WindowStartupLocation = WindowStartupLocation.Manual;
    }

    // Restore maximized state
    if (settings.IsMaximized)
    {
        WindowState = WindowState.Maximized;
    }
}
```

---

## 9. WebView2 Integration

### Initialization
```csharp
private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
{
    try
    {
        // Check WebView2 availability
        var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
        App.Log($"WebView2 Runtime Version: {version}");
    }
    catch (Exception ex)
    {
        App.Log($"WebView2 not available: {ex.Message}");
        MessageBox.Show(
            "WebView2 Runtime is required but not installed.\n\n" +
            "Please install it from:\nhttps://developer.microsoft.com/en-us/microsoft-edge/webview2/",
            "WebView2 Required",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        Application.Current.Shutdown(1);
        return;
    }

    // Initialize with persistent user data folder
    var userDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProjectName", "WebView2");

    var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
    await MyWebView.EnsureCoreWebView2Async(env);

    // Configure settings
    MyWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
    MyWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;

    // Navigate
    MyWebView.Source = new Uri("https://example.com");
}
```

### JavaScript Injection
```csharp
private async Task InjectScripts()
{
    var script = @"
        (function() {
            window.myFunction = function(param) {
                // Custom JavaScript
                return true;
            };
            console.log('Custom scripts loaded');
        })();
    ";
    await MyWebView.CoreWebView2.ExecuteScriptAsync(script);
}

// Call injected function
private async Task CallJavaScript()
{
    var result = await MyWebView.CoreWebView2.ExecuteScriptAsync("window.myFunction('value')");
    // result is JSON string
}
```

---

## 10. WiX Installer (v4)

### Package.wxs Template
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"
     xmlns:ui="http://wixtoolset.org/schemas/v4/wxs/ui">
    <Package Name="Application Name"
             Manufacturer="Company Name"
             Version="1.0.0.0"
             UpgradeCode="YOUR-GUID-HERE"
             Compressed="yes"
             InstallerVersion="500"
             Scope="perUser">

        <SummaryInformation Description="Application description"
                            Manufacturer="Company Name"
                            Keywords="keyword1, keyword2" />

        <MajorUpgrade DowngradeErrorMessage="A newer version is already installed."
                      AllowSameVersionUpgrades="yes"
                      Schedule="afterInstallInitialize" />
        <MediaTemplate EmbedCab="yes" CompressionLevel="high" />

        <!-- Add/Remove Programs properties -->
        <Property Id="ARPPRODUCTICON" Value="AppIcon.ico" />
        <Property Id="ARPCOMMENTS" Value="Application description" />
        <Property Id="ARPHELPLINK" Value="https://example.com/help" />
        <Property Id="ARPURLINFOABOUT" Value="https://example.com" />

        <Icon Id="AppIcon.ico" SourceFile="..\ProjectName\Resources\app.ico" />

        <!-- WiX UI with feature selection -->
        <ui:WixUI Id="WixUI_FeatureTree" />
        <WixVariable Id="WixUILicenseRtf" Value="License.rtf" />

        <!-- Launch after install -->
        <Property Id="WIXUI_EXITDIALOGOPTIONALCHECKBOXTEXT" Value="Launch Application" />
        <Property Id="WIXUI_EXITDIALOGOPTIONALCHECKBOX" Value="1" />
        <Property Id="WixShellExecTarget" Value="[#MainExe]" />
        <CustomAction Id="LaunchApplication" DllEntry="WixShellExec"
                      Impersonate="yes" BinaryRef="Wix4UtilCA_X86" />
        <UI>
            <Publish Dialog="ExitDialog" Control="Finish" Event="DoAction"
                     Value="LaunchApplication"
                     Condition="WIXUI_EXITDIALOGOPTIONALCHECKBOX = 1 and NOT Installed" />
        </UI>

        <!-- Install directories -->
        <StandardDirectory Id="LocalAppDataFolder">
            <Directory Id="INSTALLFOLDER" Name="Application Name" />
        </StandardDirectory>

        <StandardDirectory Id="ProgramMenuFolder">
            <Directory Id="ApplicationProgramsFolder" Name="Application Name" />
        </StandardDirectory>

        <StandardDirectory Id="DesktopFolder" />

        <!-- Components -->
        <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
            <Component Id="MainExecutable" Guid="YOUR-GUID-1">
                <File Id="MainExe" Source="..\publish\AppName.exe" KeyPath="yes" />
            </Component>
            <!-- Add more components for DLLs, config files, etc. -->
        </ComponentGroup>

        <ComponentGroup Id="ShortcutComponents" Directory="ApplicationProgramsFolder">
            <Component Id="ApplicationShortcut" Guid="YOUR-GUID-2">
                <Shortcut Id="StartMenuShortcut"
                          Name="Application Name"
                          Description="Application description"
                          Target="[INSTALLFOLDER]AppName.exe"
                          WorkingDirectory="INSTALLFOLDER"
                          Icon="AppIcon.ico" />
                <RemoveFolder Id="CleanUpShortCut" On="uninstall" />
                <RegistryValue Root="HKCU" Key="Software\Company\AppName"
                               Name="installed" Type="integer" Value="1" KeyPath="yes" />
            </Component>
        </ComponentGroup>

        <ComponentGroup Id="DesktopShortcutComponents" Directory="DesktopFolder">
            <Component Id="DesktopShortcut" Guid="YOUR-GUID-3">
                <Shortcut Id="DesktopShortcut"
                          Name="Application Name"
                          Target="[INSTALLFOLDER]AppName.exe"
                          WorkingDirectory="INSTALLFOLDER"
                          Icon="AppIcon.ico" />
                <RegistryValue Root="HKCU" Key="Software\Company\AppName"
                               Name="desktopShortcut" Type="integer" Value="1" KeyPath="yes" />
            </Component>
        </ComponentGroup>

        <!-- Features -->
        <Feature Id="ProductFeature" Title="Application Name"
                 Description="Core application files" Level="1"
                 Display="expand" AllowAbsent="no">
            <ComponentGroupRef Id="ProductComponents" />

            <Feature Id="StartMenuShortcut" Title="Start Menu Shortcut"
                     Description="Create Start Menu shortcut" Level="1" AllowAbsent="yes">
                <ComponentGroupRef Id="ShortcutComponents" />
            </Feature>

            <Feature Id="DesktopShortcut" Title="Desktop Shortcut"
                     Description="Create Desktop shortcut" Level="1" AllowAbsent="yes">
                <ComponentGroupRef Id="DesktopShortcutComponents" />
            </Feature>
        </Feature>
    </Package>
</Wix>
```

### Build Commands
```bash
# Install WiX CLI
dotnet tool install --global wix

# Add extensions (once)
wix extension add -g WixToolset.UI.wixext
wix extension add -g WixToolset.Util.wixext

# Publish application
dotnet publish ProjectName/ProjectName.csproj -c Release -r win-x64 --self-contained false -o publish

# Build installer
cd installer
wix build Package.wxs -ext WixToolset.UI.wixext -ext WixToolset.Util.wixext -o AppName-v1.0.0-x64.msi
```

---

## 11. Project File (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <AssemblyName>AppName</AssemblyName>
    <RootNamespace>ProjectName</RootNamespace>
    <Version>1.0.0</Version>
    <Authors>Author Name</Authors>
    <Description>Application description</Description>
    <ApplicationIcon>Resources\app.ico</ApplicationIcon>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2903.40" />
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Include="Scripts\*.js" />
  </ItemGroup>

</Project>
```

---

## 12. Keyboard Shortcuts

### Configurable Shortcuts Pattern
```csharp
// Model
public enum SendKeyMode { ShiftEnter, CtrlEnter, Enter }

// In PreviewKeyDown handler
private async void Input_PreviewKeyDown(object sender, KeyEventArgs e)
{
    if (e.Key == Key.Enter)
    {
        bool shouldExecute = _currentMode switch
        {
            SendKeyMode.ShiftEnter => (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift,
            SendKeyMode.CtrlEnter => (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control,
            SendKeyMode.Enter => Keyboard.Modifiers == ModifierKeys.None,
            _ => false
        };

        if (shouldExecute)
        {
            await ExecuteAction();
            e.Handled = true;
            return;
        }
    }

    // Tab to spaces
    if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None)
    {
        var textBox = (TextBox)sender;
        var caretIndex = textBox.CaretIndex;
        textBox.Text = textBox.Text.Insert(caretIndex, "  ");
        textBox.CaretIndex = caretIndex + 2;
        e.Handled = true;
    }
}
```

---

## Summary Checklist

- [ ] Project structure with Models/, Services/, Converters/
- [ ] AppSettings model with sensible defaults
- [ ] SettingsService with JSON serialization to LocalAppData
- [ ] Color palette and brush resources in App.xaml
- [ ] Standard button styles (Icon, Primary, Secondary)
- [ ] Dialog window template with custom title bar
- [ ] Global exception handlers in App.xaml.cs
- [ ] File-based logging with timestamp
- [ ] WindowChrome for custom title bar with resize
- [ ] Window state persistence (size, position, maximized)
- [ ] WiX installer with UI extension
- [ ] Feature selection for shortcuts
- [ ] Launch after install option
