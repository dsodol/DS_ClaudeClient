using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DS_ClaudeClient.Controls.Dialogs;
using DS_ClaudeClient.Controls.Models;
using DS_ClaudeClient.Controls.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace DS_ClaudeClient.Controls;

/// <summary>
/// A self-contained panel for managing text snippets.
/// Zero-config: works out of the box with default settings.
/// </summary>
public partial class SnippetsPanel : UserControl
{
    #region Private Fields

    private SnippetService? _snippetService;
    private List<Snippet> _allSnippets = [];
    private SnippetSortMode _currentSortMode = SnippetSortMode.Custom;
    private bool _sortAscending = true;
    private Point _dragStartPoint;
    private string? _initializationStackTrace;
    private string? _resolvedFilePath;

    #endregion

    #region Dependency Properties

    /// <summary>
    /// Gets or sets the data folder path for storing snippets.
    /// Default: %LocalAppData%/DS_ClaudeClient/
    /// </summary>
    public static readonly DependencyProperty DataFolderPathProperty =
        DependencyProperty.Register(
            nameof(DataFolderPath),
            typeof(string),
            typeof(SnippetsPanel),
            new PropertyMetadata(null, OnDataFolderPathChanged));

    public string? DataFolderPath
    {
        get => (string?)GetValue(DataFolderPathProperty);
        set => SetValue(DataFolderPathProperty, value);
    }

    /// <summary>
    /// Gets or sets the snippets file name.
    /// Default: snippets.json
    /// </summary>
    public static readonly DependencyProperty SnippetsFileNameProperty =
        DependencyProperty.Register(
            nameof(SnippetsFileName),
            typeof(string),
            typeof(SnippetsPanel),
            new PropertyMetadata(null, OnSnippetsFileNameChanged));

    public string? SnippetsFileName
    {
        get => (string?)GetValue(SnippetsFileNameProperty);
        set => SetValue(SnippetsFileNameProperty, value);
    }

    /// <summary>
    /// Gets or sets the full path to the snippets file (overrides DataFolderPath + SnippetsFileName).
    /// </summary>
    public static readonly DependencyProperty SnippetsFilePathProperty =
        DependencyProperty.Register(
            nameof(SnippetsFilePath),
            typeof(string),
            typeof(SnippetsPanel),
            new PropertyMetadata(null, OnSnippetsFilePathChanged));

    public string? SnippetsFilePath
    {
        get => (string?)GetValue(SnippetsFilePathProperty);
        set => SetValue(SnippetsFilePathProperty, value);
    }

    /// <summary>
    /// Gets or sets the current sort mode.
    /// </summary>
    public static readonly DependencyProperty SortModeProperty =
        DependencyProperty.Register(
            nameof(SortMode),
            typeof(SnippetSortMode),
            typeof(SnippetsPanel),
            new PropertyMetadata(SnippetSortMode.Custom, OnSortModeChanged));

    public SnippetSortMode SortMode
    {
        get => (SnippetSortMode)GetValue(SortModeProperty);
        set => SetValue(SortModeProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to execute when a snippet is activated (double-clicked).
    /// The command parameter will be the Snippet object.
    /// </summary>
    public static readonly DependencyProperty ActivateCommandProperty =
        DependencyProperty.Register(
            nameof(ActivateCommand),
            typeof(ICommand),
            typeof(SnippetsPanel),
            new PropertyMetadata(null));

    public ICommand? ActivateCommand
    {
        get => (ICommand?)GetValue(ActivateCommandProperty);
        set => SetValue(ActivateCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to execute when a snippet is selected (single-clicked).
    /// The command parameter will be the Snippet object.
    /// </summary>
    public static readonly DependencyProperty SelectCommandProperty =
        DependencyProperty.Register(
            nameof(SelectCommand),
            typeof(ICommand),
            typeof(SnippetsPanel),
            new PropertyMetadata(null));

    public ICommand? SelectCommand
    {
        get => (ICommand?)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets a callback Action invoked when a snippet is activated (double-clicked).
    /// Alternative to event/command for simple scenarios.
    /// </summary>
    public static readonly DependencyProperty OnSnippetActivatedProperty =
        DependencyProperty.Register(
            nameof(OnSnippetActivated),
            typeof(Action<Snippet>),
            typeof(SnippetsPanel),
            new PropertyMetadata(null));

    public Action<Snippet>? OnSnippetActivated
    {
        get => (Action<Snippet>?)GetValue(OnSnippetActivatedProperty);
        set => SetValue(OnSnippetActivatedProperty, value);
    }

    /// <summary>
    /// Gets or sets the logger for diagnostic output.
    /// Uses Microsoft.Extensions.Logging.ILogger.
    /// </summary>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Gets the stack trace captured when the snippets file was loaded.
    /// </summary>
    public string? InitializationStackTrace => _initializationStackTrace;

    /// <summary>
    /// Gets the resolved file path used for snippets storage.
    /// </summary>
    public string? ResolvedFilePath => _resolvedFilePath;

    #endregion

    #region Events

    /// <summary>
    /// Raised when a snippet is selected (single click).
    /// </summary>
    public event EventHandler<Snippet>? SnippetSelected;

    /// <summary>
    /// Raised when a snippet is activated (double click) - typically used for insertion.
    /// </summary>
    public event EventHandler<Snippet>? SnippetActivated;

    /// <summary>
    /// Raised when snippets are modified (add, edit, delete, reorder).
    /// </summary>
    public event EventHandler? SnippetsChanged;

    #endregion

    #region Constructor

    public SnippetsPanel()
    {
        InitializeComponent();
        Loaded += SnippetsPanel_Loaded;
    }

    #endregion

    #region Initialization

    private void SnippetsPanel_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeService();
        LoadSnippets();
    }

    private void InitializeService()
    {
        _initializationStackTrace = new StackTrace(true).ToString();
        _resolvedFilePath = ResolveSnippetsFilePath();
        _snippetService = new SnippetService(_resolvedFilePath);
        
        Logger?.LogInformation("SnippetsPanel: Requested path: {RequestedPath}", _resolvedFilePath);
        Logger?.LogInformation("SnippetsPanel: SnippetService actual path: {ActualPath}", _snippetService.SnippetsFilePath);
        Logger?.LogInformation("SnippetsPanel: Stack trace:\n{StackTrace}", _initializationStackTrace);
    }

    private string ResolveSnippetsFilePath()
    {
        // Priority 1: Explicit full path
        if (!string.IsNullOrWhiteSpace(SnippetsFilePath))
        {
            return SnippetsFilePath;
        }

        // Priority 2: Custom DataFolderPath or SnippetsFileName
        if (!string.IsNullOrWhiteSpace(DataFolderPath) || !string.IsNullOrWhiteSpace(SnippetsFileName))
        {
            var folder = DataFolderPath ?? ControlsConfig.DefaultDataFolderPath;
            var fileName = SnippetsFileName ?? ControlsConfig.DefaultSnippetsFileName;
            return Path.Combine(folder, fileName);
        }

        // Priority 3: Zero-config default (OneDrive)
        return ControlsConfig.GetSnippetsPath();
    }

    private static void OnDataFolderPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SnippetsPanel panel && panel.IsLoaded)
        {
            panel.InitializeService();
            panel.LoadSnippets();
        }
    }

    private static void OnSnippetsFileNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SnippetsPanel panel && panel.IsLoaded)
        {
            panel.InitializeService();
            panel.LoadSnippets();
        }
    }

    private static void OnSnippetsFilePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SnippetsPanel panel && panel.IsLoaded)
        {
            panel.InitializeService();
            panel.LoadSnippets();
        }
    }

    private static void OnSortModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SnippetsPanel panel)
        {
            panel._currentSortMode = (SnippetSortMode)e.NewValue;
            panel.RefreshList();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads snippets from the configured file.
    /// </summary>
    public void LoadSnippets()
    {
        if (_snippetService == null) return;

        _allSnippets = _snippetService.Load();
        
        Logger?.LogInformation("SnippetsPanel: Loaded {Count} snippets from {Path}", 
            _allSnippets.Count, _snippetService.SnippetsFilePath);
        
        if (_allSnippets.Count > 0)
        {
            var firstFew = string.Join(", ", _allSnippets.Take(3).Select(s => $"\"{s.Title}\""));
            Logger?.LogInformation("SnippetsPanel: First snippets: {Titles}", firstFew);
        }
        
        RefreshList();
    }

    /// <summary>
    /// Refreshes the snippets list display with optional search filter.
    /// </summary>
    /// <param name="searchFilter">Optional search text to filter snippets.</param>
    public void RefreshList(string? searchFilter = null)
    {
        var filter = searchFilter ?? SearchBox?.Text ?? "";
        var filtered = string.IsNullOrWhiteSpace(filter)
            ? _allSnippets
            : _allSnippets.Where(s =>
                s.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                s.Content.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        var sorted = _currentSortMode switch
        {
            SnippetSortMode.Title => _sortAscending
                ? filtered.OrderBy(s => s.Title).ToList()
                : filtered.OrderByDescending(s => s.Title).ToList(),
            SnippetSortMode.DateCreated => _sortAscending
                ? filtered.OrderBy(s => s.CreatedAt).ToList()
                : filtered.OrderByDescending(s => s.CreatedAt).ToList(),
            _ => filtered.OrderBy(s => s.Order).ToList() // Custom
        };

        SnippetsList.ItemsSource = sorted;
    }

    /// <summary>
    /// Gets all snippets.
    /// </summary>
    public List<Snippet> GetAllSnippets() => [.. _allSnippets];

    /// <summary>
    /// Gets the current snippets file path.
    /// </summary>
    public string GetCurrentFilePath() => _snippetService?.SnippetsFilePath ?? "";

    /// <summary>
    /// Imports snippets from a file and adds them to existing snippets.
    /// </summary>
    /// <param name="filePath">Path to the import file.</param>
    /// <returns>Number of snippets imported.</returns>
    public int ImportSnippets(string filePath)
    {
        if (_snippetService == null) return 0;

        var imported = _snippetService.Import(filePath);
        _allSnippets.AddRange(imported);
        _snippetService.Save(_allSnippets);
        RefreshList();
        SnippetsChanged?.Invoke(this, EventArgs.Empty);
        return imported.Count;
    }

    /// <summary>
    /// Exports all snippets to a file.
    /// </summary>
    /// <param name="filePath">Destination file path.</param>
    public void ExportSnippets(string filePath)
    {
        _snippetService?.Export(_allSnippets, filePath);
    }

    #endregion

    #region Event Handlers - UI

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshList(SearchBox.Text);
    }

    private void SnippetsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SnippetsList.SelectedItem is Snippet snippet)
        {
            // Invoke event
            SnippetSelected?.Invoke(this, snippet);

            // Invoke command if set
            if (SelectCommand?.CanExecute(snippet) == true)
            {
                SelectCommand.Execute(snippet);
            }
        }
    }

    private void SnippetsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SnippetsList.SelectedItem is Snippet snippet)
        {
            // Invoke event
            SnippetActivated?.Invoke(this, snippet);

            // Invoke command if set
            if (ActivateCommand?.CanExecute(snippet) == true)
            {
                ActivateCommand.Execute(snippet);
            }

            // Invoke callback if set
            OnSnippetActivated?.Invoke(snippet);
        }
    }

    private void SortSnippets_Click(object sender, RoutedEventArgs e)
    {
        // Cycle through sort modes
        _currentSortMode = _currentSortMode switch
        {
            SnippetSortMode.Custom => SnippetSortMode.Title,
            SnippetSortMode.Title => SnippetSortMode.DateCreated,
            SnippetSortMode.DateCreated => SnippetSortMode.Custom,
            _ => SnippetSortMode.Custom
        };

        // Toggle ascending/descending on repeated clicks of same mode
        if (_currentSortMode == SortMode)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortAscending = true;
        }

        SortMode = _currentSortMode;
        RefreshList();
    }

    private void AddSnippet_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SnippetEditDialog();
        dialog.Owner = Window.GetWindow(this);

        if (dialog.ShowDialog() == true)
        {
            dialog.Snippet.Order = _allSnippets.Count;
            _allSnippets.Add(dialog.Snippet);
            _snippetService?.Save(_allSnippets);
            RefreshList();
            SnippetsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void EditSnippet_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Snippet snippet })
        {
            var dialog = new SnippetEditDialog(snippet);
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
            {
                _snippetService?.Save(_allSnippets);
                RefreshList();
                SnippetsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void DeleteSnippet_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Snippet snippet })
        {
            var result = MessageBox.Show(
                $"Delete snippet '{snippet.Title}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _allSnippets.Remove(snippet);
                UpdateSnippetOrders();
                _snippetService?.Save(_allSnippets);
                RefreshList();
                SnippetsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void ImportSnippets_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Import Snippets"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var count = ImportSnippets(dialog.FileName);
                MessageBox.Show(
                    $"Imported {count} snippets.",
                    "Import Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to import snippets: {ex.Message}",
                    "Import Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    #endregion

    #region Drag and Drop

    private void SnippetsList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void SnippetsList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var diff = _dragStartPoint - e.GetPosition(null);

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            var listView = sender as ListView;
            var listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);

            if (listViewItem == null) return;

            var snippet = (Snippet)listView!.ItemContainerGenerator.ItemFromContainer(listViewItem);
            if (snippet == null) return;

            var dragData = new DataObject("Snippet", snippet);
            DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Move);
        }
    }

    private void SnippetsList_DragEnter(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("Snippet"))
        {
            e.Effects = DragDropEffects.None;
        }
    }

    private void SnippetsList_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("Snippet")) return;

        var droppedSnippet = (Snippet)e.Data.GetData("Snippet");
        var target = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);

        if (target == null) return;

        var targetSnippet = (Snippet)SnippetsList.ItemContainerGenerator.ItemFromContainer(target);
        if (targetSnippet == null || droppedSnippet == targetSnippet) return;

        var oldIndex = _allSnippets.IndexOf(droppedSnippet);
        var newIndex = _allSnippets.IndexOf(targetSnippet);

        _allSnippets.RemoveAt(oldIndex);
        _allSnippets.Insert(newIndex, droppedSnippet);

        UpdateSnippetOrders();
        _snippetService?.Save(_allSnippets);
        RefreshList(SearchBox.Text);
        SnippetsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateSnippetOrders()
    {
        for (int i = 0; i < _allSnippets.Count; i++)
        {
            _allSnippets[i].Order = i;
        }
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T t)
            {
                return t;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    #endregion
}
