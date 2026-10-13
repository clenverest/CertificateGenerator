using System.IO;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
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

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasSummary => TableData is not null;

    public bool HasProject => CurrentProject is not null;

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
        IWindowNavigator navigator)
    {
        _tableReaderResolver = tableReaderResolver;
        _projectStorage = projectStorage;
        _navigator = navigator;
    }

    /// <summary>
    /// Инициализировать ViewModel для работы с проектом.
    /// Вызывается сразу после создания.
    /// Если в проекте уже есть таблица — пытается её загрузить.
    /// </summary>
    public void Initialize(Project project, string projectFolderPath)
    {
        CurrentProject = project;
        ProjectFolderPath = projectFolderPath;

        TryLoadExistingTable();
    }

    // ---------------------------------------------------------------------
    // LoadTableIntoProjectCommand — загрузка таблицы в проект
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

    /// <summary>
    /// Загрузить таблицу в проект: копирует файл в source/,
    /// обновляет project.json, читает таблицу.
    /// </summary>
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
            // 1. Копируем файл в source/ проекта, получаем обновлённый Project.
            var updatedProject = _projectStorage.SetSourceTable(
                ProjectFolderPath, CurrentProject, sourceFilePath);

            // 2. Обновляем CurrentProject в памяти.
            CurrentProject = updatedProject;

            // 3. Читаем таблицу из source/ проекта.
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
    // OpenFileCommand — старая логика с Этапа 1, оставлена для совместимости
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