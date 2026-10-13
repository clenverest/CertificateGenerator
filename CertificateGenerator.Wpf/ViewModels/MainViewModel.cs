using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using CertificateGenerator.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Windows;

namespace CertificateGenerator.Wpf.ViewModels;

/// <summary>
/// ViewModel главного окна: работа с проектом и таблицей участников.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ITableReaderResolver _tableReaderResolver;
    private readonly IProjectStorage _projectStorage;
    private readonly IWindowNavigator _navigator;

    private bool _isLoadingFields;

    [ObservableProperty]
    private TableData? _tableData;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _selectedFilePath;

    [ObservableProperty]
    private Project? _currentProject;

    [ObservableProperty]
    private string? _projectFolderPath;

    /// <summary>
    /// ViewModel для работы со списком полей проекта.
    /// </summary>
    public FieldsViewModel Fields { get; }

    /// <summary>
    /// Предпросмотр значений полей для первого участника таблицы.
    /// </summary>
    public ObservableCollection<FieldPreviewItem> FieldPreviews { get; } = new();

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasSummary => TableData is not null;

    public bool HasProject => CurrentProject is not null;

    public bool HasPreviews => FieldPreviews.Count > 0;

    public string TableSummary =>
        TableData is null
            ? string.Empty
            : $"Загружено: {TableData.Rows.Count} × {TableData.Columns.Count}";

    public string ProjectName =>
        CurrentProject?.Name ?? string.Empty;

    public string ProjectPath =>
        ProjectFolderPath ?? string.Empty;

    public MainViewModel(
        ITableReaderResolver tableReaderResolver,
        IProjectStorage projectStorage,
        IWindowNavigator navigator,
        FieldsViewModel fields)
    {
        _tableReaderResolver = tableReaderResolver;
        _projectStorage = projectStorage;
        _navigator = navigator;
        Fields = fields;

        Fields.Fields.CollectionChanged += OnFieldsCollectionChanged;
    }

    /// <summary>
    /// Инициализировать ViewModel для работы с проектом.
    /// </summary>
    public void Initialize(Project project, string projectFolderPath)
    {
        CurrentProject = project;
        ProjectFolderPath = projectFolderPath;

        _isLoadingFields = true;
        try
        {
            Fields.LoadFields(project.Fields);
        }
        finally
        {
            _isLoadingFields = false;
        }

        TryLoadExistingTable();
        UpdateFieldPreviews();
    }

    // ---------------------------------------------------------------------
    // Реакция на изменение списка полей
    // ---------------------------------------------------------------------

    private void OnFieldsCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        if (!_isLoadingFields && CurrentProject is not null && ProjectFolderPath is not null)
        {
            var updated = WithFields(Fields.GetFieldsSnapshot());

            try
            {
                _projectStorage.Save(updated, ProjectFolderPath);
                CurrentProject = updated;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Не удалось сохранить поля: {ex.Message}";
            }
        }

        // Превью обновляется в любом случае — даже при загрузке.
        UpdateFieldPreviews();
    }

    /// <summary>
    /// Создать копию <see cref="CurrentProject"/> с новым списком полей.
    /// </summary>
    private Project WithFields(IReadOnlyList<Field> fields)
    {
        var project = CurrentProject
            ?? throw new InvalidOperationException("Проект не открыт.");

        return new Project(
            project.Name,
            project.CreatedAt,
            project.SourceFileName,
            fields);
    }

    // ---------------------------------------------------------------------
    // Предпросмотр значений полей
    // ---------------------------------------------------------------------

    private void UpdateFieldPreviews()
    {
        FieldPreviews.Clear();

        if (TableData is null || TableData.Rows.Count == 0)
        {
            OnPropertyChanged(nameof(HasPreviews));
            return;
        }

        var firstRow = TableData.Rows[0];
        var fields = Fields.GetFieldsSnapshot();

        foreach (var field in fields)
        {
            var value = FieldValueResolver.Resolve(field, firstRow);
            FieldPreviews.Add(new FieldPreviewItem(field.Name, value));
        }

        OnPropertyChanged(nameof(HasPreviews));
    }

    // ---------------------------------------------------------------------
    // LoadTableIntoProjectCommand
    // ---------------------------------------------------------------------

    [RelayCommand]
    private void LoadTableIntoProject()
    {
        if (CurrentProject is null || ProjectFolderPath is null)
        {
            ErrorMessage = "Проект не открыт.";
            return;
        }

        if (TableData is not null)
        {
            var confirm = MessageBox.Show(
                "В проекте уже загружена таблица. Заменить её?",
                "Замена таблицы",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }
        }

        var dialog = new OpenFileDialog
        {
            Title = "Выберите файл с участниками",
            Filter = "Таблицы (*.xlsx;*.csv)|*.xlsx;*.csv"
        };

        var result = dialog.ShowDialog();
        if (result != true)
        {
            return;
        }

        LoadTableIntoProjectFromPath(dialog.FileName);
    }

    internal void LoadTableIntoProjectFromPath(string sourceFilePath)
    {
        if (CurrentProject is null || ProjectFolderPath is null)
        {
            ErrorMessage = "Проект не открыт.";
            return;
        }

        ErrorMessage = null;
        IsBusy = true;

        try
        {
            var updatedProject = _projectStorage.SetSourceTable(
                ProjectFolderPath, CurrentProject, sourceFilePath);

            CurrentProject = updatedProject;

            var projectTablePath = _projectStorage.GetSourceTablePath(
                updatedProject, ProjectFolderPath);

            if (projectTablePath is null)
            {
                ErrorMessage = "Не удалось найти скопированную таблицу.";
                TableData = null;
                return;
            }

            var reader = _tableReaderResolver.Resolve(projectTablePath);
            TableData = reader.Read(projectTablePath);
            SelectedFilePath = projectTablePath;
        }
        catch (NotSupportedException ex)
        {
            ErrorMessage = ex.Message;
            TableData = null;
        }
        catch (FileNotFoundException ex)
        {
            ErrorMessage = ex.Message;
            TableData = null;
        }
        catch (InvalidDataException ex)
        {
            ErrorMessage = ex.Message;
            TableData = null;
        }
        catch (IOException ex)
        {
            ErrorMessage = ex.Message;
            TableData = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Непредвиденная ошибка: {ex.Message}";
            TableData = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ---------------------------------------------------------------------
    // OpenFileCommand — старая логика с Этапа 1
    // ---------------------------------------------------------------------

    [RelayCommand]
    private void OpenFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите файл с участниками",
            Filter = "Таблицы (*.xlsx;*.csv)|*.xlsx;*.csv|Все файлы (*.*)|*.*"
        };

        var result = dialog.ShowDialog();
        if (result != true)
        {
            return;
        }

        LoadTable(dialog.FileName);
    }

    internal void LoadTable(string filePath)
    {
        ErrorMessage = null;
        IsBusy = true;

        try
        {
            SelectedFilePath = filePath;
            var reader = _tableReaderResolver.Resolve(filePath);
            TableData = reader.Read(filePath);
        }
        catch (NotSupportedException ex)
        {
            ErrorMessage = ex.Message;
            TableData = null;
        }
        catch (FileNotFoundException ex)
        {
            ErrorMessage = ex.Message;
            TableData = null;
        }
        catch (InvalidDataException ex)
        {
            ErrorMessage = ex.Message;
            TableData = null;
        }
        catch (IOException ex)
        {
            ErrorMessage = ex.Message;
            TableData = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Непредвиденная ошибка: {ex.Message}";
            TableData = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ---------------------------------------------------------------------
    // Приватные методы
    // ---------------------------------------------------------------------

    private void TryLoadExistingTable()
    {
        if (CurrentProject is null || ProjectFolderPath is null)
        {
            return;
        }

        var projectTablePath = _projectStorage.GetSourceTablePath(
            CurrentProject, ProjectFolderPath);

        if (projectTablePath is null)
        {
            return;
        }

        ErrorMessage = null;
        IsBusy = true;

        try
        {
            var reader = _tableReaderResolver.Resolve(projectTablePath);
            TableData = reader.Read(projectTablePath);
            SelectedFilePath = projectTablePath;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить таблицу из проекта: {ex.Message}";
            TableData = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ---------------------------------------------------------------------
    // Реакция на изменение свойств
    // ---------------------------------------------------------------------

    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnTableDataChanged(TableData? value)
    {
        OnPropertyChanged(nameof(HasSummary));
        OnPropertyChanged(nameof(TableSummary));

        Fields.SetColumns(value?.Columns ?? Array.Empty<ColumnInfo>());
        UpdateFieldPreviews();
    }

    partial void OnCurrentProjectChanged(Project? value)
    {
        OnPropertyChanged(nameof(HasProject));
        OnPropertyChanged(nameof(ProjectName));
    }

    partial void OnProjectFolderPathChanged(string? value)
    {
        OnPropertyChanged(nameof(ProjectPath));
    }
}