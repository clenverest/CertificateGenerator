using System.Collections.ObjectModel;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CertificateGenerator.Wpf.ViewModels;

/// <summary>
/// ViewModel для стартового экрана: создание, открытие и выбор
/// последних проектов.
/// </summary>
public sealed partial class ProjectViewModel : ObservableObject
{
    private readonly IProjectStorage _projectStorage;
    private readonly IRecentProjectsStore _recentStore;
    private readonly IFolderPicker _folderPicker;
    private readonly ICreateProjectDialogService _createProjectDialog;

    [ObservableProperty]
    private Project? _currentProject;

    [ObservableProperty]
    private string? _errorMessage;

    public ObservableCollection<RecentProject> RecentProjects { get; } = new();

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasCurrentProject => CurrentProject is not null;

    public ProjectViewModel(
        IProjectStorage projectStorage,
        IRecentProjectsStore recentStore,
        IFolderPicker folderPicker,
        ICreateProjectDialogService createProjectDialog)
    {
        _projectStorage = projectStorage;
        _recentStore = recentStore;
        _folderPicker = folderPicker;
        _createProjectDialog = createProjectDialog;
        ReloadRecentProjects();
    }

    // ---------------------------------------------------------------------
    // Команды
    // ---------------------------------------------------------------------

    [RelayCommand]
    private void CreateProject()
    {
        var result = _createProjectDialog.Show();
        if (result is null)
        {
            return;
        }

        var projectFolder = System.IO.Path.Combine(
            result.FolderPath, result.ProjectName);
        CreateProjectAt(projectFolder);
    }

    [RelayCommand]
    private void OpenProject()
    {
        var folderPath = _folderPicker.PickFolder("Выберите папку с проектом");
        if (folderPath is null)
        {
            return;
        }

        OpenProjectAt(folderPath);
    }

    [RelayCommand]
    private void OpenRecentProject(RecentProject? recent)
    {
        if (recent is null)
        {
            return;
        }

        OpenProjectAt(recent.ProjectFolderPath);
    }

    // ---------------------------------------------------------------------
    // internal — тестируется напрямую, без диалогов
    // ---------------------------------------------------------------------

    internal void CreateProjectAt(string folderPath)
    {
        ErrorMessage = null;

        try
        {
            var projectName = System.IO.Path.GetFileName(
                folderPath.TrimEnd(
                    System.IO.Path.DirectorySeparatorChar,
                    System.IO.Path.AltDirectorySeparatorChar));

            if (string.IsNullOrWhiteSpace(projectName))
            {
                projectName = "Новый проект";
            }

            var project = _projectStorage.Create(folderPath, projectName);
            CurrentProject = project;
            _recentStore.Add(folderPath, project.Name);
            ReloadRecentProjects();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    internal void OpenProjectAt(string folderPath)
    {
        ErrorMessage = null;

        try
        {
            var project = _projectStorage.Load(folderPath);
            CurrentProject = project;
            _recentStore.Add(folderPath, project.Name);
            ReloadRecentProjects();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    // ---------------------------------------------------------------------
    // Приватные методы
    // ---------------------------------------------------------------------

    private void ReloadRecentProjects()
    {
        RecentProjects.Clear();
        foreach (var entry in _recentStore.GetAll())
        {
            RecentProjects.Add(entry);
        }
    }

    // ---------------------------------------------------------------------
    // Реакция на изменение свойств
    // ---------------------------------------------------------------------

    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnCurrentProjectChanged(Project? value)
    {
        OnPropertyChanged(nameof(HasCurrentProject));
    }
}