using CertificateGenerator.Core.Models;
using CertificateGenerator.Tests.Fakes;
using CertificateGenerator.Wpf.ViewModels;

namespace CertificateGenerator.Tests.ViewModels;

public sealed class ProjectViewModelTests
{
    private static (ProjectViewModel vm,
                    FakeProjectStorage storage,
                    FakeRecentProjectsStore recentStore,
                    FakeFolderPicker picker,
                    FakeCreateProjectDialogService createDialog,
                    FakeWindowNavigator navigator)
        CreateViewModel()
    {
        var storage = new FakeProjectStorage();
        var recentStore = new FakeRecentProjectsStore();
        var picker = new FakeFolderPicker();
        var createDialog = new FakeCreateProjectDialogService();
        var navigator = new FakeWindowNavigator();
        var vm = new ProjectViewModel(storage, recentStore, picker, createDialog, navigator);
        return (vm, storage, recentStore, picker, createDialog, navigator);
    }

    // ---------------------------------------------------------------------
    // Начальное состояние
    // ---------------------------------------------------------------------

    [Fact]
    public void Constructor_InitialState_HasNoProjectAndNoError()
    {
        var (vm, _, _, _, _, _) = CreateViewModel();

        Assert.Null(vm.CurrentProject);
        Assert.Null(vm.ErrorMessage);
        Assert.False(vm.HasError);
        Assert.False(vm.HasCurrentProject);
        Assert.Empty(vm.RecentProjects);
    }

    // ---------------------------------------------------------------------
    // CreateProjectAt
    // ---------------------------------------------------------------------

    [Fact]
    public void CreateProjectAt_ValidPath_SetsCurrentProjectAndAddsToRecent()
    {
        var (vm, storage, recentStore, _, _, _) = CreateViewModel();
        storage.ProjectToReturnOnCreate = new Project(
            "Мой проект", DateTime.Now, string.Empty, Array.Empty<Field>());

        vm.CreateProjectAt(@"C:\Projects\Мой проект");

        Assert.NotNull(vm.CurrentProject);
        Assert.Equal("Мой проект", vm.CurrentProject!.Name);
        Assert.Null(vm.ErrorMessage);
        Assert.True(vm.HasCurrentProject);
        Assert.Single(vm.RecentProjects);
        Assert.Equal("Мой проект", vm.RecentProjects[0].ProjectName);
        Assert.Contains(@"C:\Projects\Мой проект", storage.CreatedPaths);
    }

    [Fact]
    public void CreateProjectAt_FolderNotEmpty_SetsErrorMessage()
    {
        var (vm, storage, _, _, _, _) = CreateViewModel();
        storage.ExceptionToThrowOnCreate = new InvalidOperationException(
            "Папка не пуста.");

        vm.CreateProjectAt(@"C:\Projects\Занято");

        Assert.Null(vm.CurrentProject);
        Assert.Equal("Папка не пуста.", vm.ErrorMessage);
        Assert.True(vm.HasError);
        Assert.False(vm.HasCurrentProject);
    }

    // ---------------------------------------------------------------------
    // OpenProjectAt
    // ---------------------------------------------------------------------

    [Fact]
    public void OpenProjectAt_ValidPath_SetsCurrentProject()
    {
        var (vm, storage, _, _, _, _) = CreateViewModel();
        storage.ProjectToReturnOnLoad = new Project(
            "Существующий", DateTime.Now, "data.xlsx", Array.Empty<Field>());

        vm.OpenProjectAt(@"C:\Projects\Существующий");

        Assert.NotNull(vm.CurrentProject);
        Assert.Equal("Существующий", vm.CurrentProject!.Name);
        Assert.Null(vm.ErrorMessage);
        Assert.Contains(@"C:\Projects\Существующий", storage.LoadedPaths);
    }

    [Fact]
    public void OpenProjectAt_ProjectNotFound_SetsErrorMessage()
    {
        var (vm, storage, _, _, _, _) = CreateViewModel();
        storage.ExceptionToThrowOnLoad = new FileNotFoundException(
            "В папке нет файла проекта.");

        vm.OpenProjectAt(@"C:\Projects\Пусто");

        Assert.Null(vm.CurrentProject);
        Assert.Equal("В папке нет файла проекта.", vm.ErrorMessage);
        Assert.True(vm.HasError);
    }

    // ---------------------------------------------------------------------
    // OpenRecentProjectCommand
    // ---------------------------------------------------------------------

    [Fact]
    public void OpenRecentProjectCommand_NullParameter_DoesNothing()
    {
        var (vm, storage, _, _, _, _) = CreateViewModel();

        vm.OpenRecentProjectCommand.Execute(null);

        Assert.Empty(storage.LoadedPaths);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public void OpenRecentProjectCommand_ValidEntry_LoadsProject()
    {
        var (vm, storage, _, _, _, _) = CreateViewModel();
        storage.ProjectToReturnOnLoad = new Project(
            "Из списка", DateTime.Now, string.Empty, Array.Empty<Field>());
        var recent = new RecentProject(@"C:\Projects\Из списка", "Из списка", DateTime.Now);

        vm.OpenRecentProjectCommand.Execute(recent);

        Assert.NotNull(vm.CurrentProject);
        Assert.Equal("Из списка", vm.CurrentProject!.Name);
        Assert.Contains(@"C:\Projects\Из списка", storage.LoadedPaths);
    }

    // ---------------------------------------------------------------------
    // CreateProjectCommand (с фейковым picker-ом)
    // ---------------------------------------------------------------------

    [Fact]
    public void CreateProjectCommand_UserCancels_DoesNothing()
    {
        var (vm, storage, _, _, createDialog, _) = CreateViewModel();
        createDialog.ResultToReturn = null;

        vm.CreateProjectCommand.Execute(null);

        Assert.Empty(storage.CreatedPaths);
        Assert.Null(vm.CurrentProject);
        Assert.Null(vm.ErrorMessage);
        Assert.Equal(1, createDialog.ShowCallCount);
    }

    [Fact]
    public void CreateProjectCommand_UserPicksFolder_CreatesProject()
    {
        var (vm, storage, _, _, createDialog, _) = CreateViewModel();
        createDialog.ResultToReturn = new CreateProjectResult(
            @"C:\Projects", "Новый");
        storage.ProjectToReturnOnCreate = new Project(
            "Новый", DateTime.Now, string.Empty, Array.Empty<Field>());

        vm.CreateProjectCommand.Execute(null);

        Assert.NotNull(vm.CurrentProject);
        Assert.Equal("Новый", vm.CurrentProject!.Name);
        Assert.Contains(@"C:\Projects\Новый", storage.CreatedPaths);
        Assert.Equal(1, createDialog.ShowCallCount);
    }

    // ---------------------------------------------------------------------
    // ReloadRecentProjects
    // ---------------------------------------------------------------------

    [Fact]
    public void Constructor_WithExistingRecentEntries_LoadsThem()
    {
        var storage = new FakeProjectStorage();
        var recentStore = new FakeRecentProjectsStore();
        recentStore.Add(@"C:\A", "A");
        recentStore.Add(@"C:\B", "B");
        var picker = new FakeFolderPicker();

        var vm = new ProjectViewModel(
            storage, recentStore, picker,
            new FakeCreateProjectDialogService(), 
            new FakeWindowNavigator());

        Assert.Equal(2, vm.RecentProjects.Count);
        Assert.Equal("B", vm.RecentProjects[0].ProjectName);
        Assert.Equal("A", vm.RecentProjects[1].ProjectName);
    }

    [Fact]
    public void CreateProjectAt_ValidPath_CallsNavigator()
    {
        var (vm, storage, _, _, _, navigator) = CreateViewModel();
        storage.ProjectToReturnOnCreate = new Project(
            "Мой проект", DateTime.Now, string.Empty, Array.Empty<Field>());

        vm.CreateProjectAt(@"C:\Projects\Мой проект");

        Assert.Single(navigator.MainNavigations);
        Assert.Equal(@"C:\Projects\Мой проект", navigator.MainNavigations[0].ProjectFolderPath);
        Assert.Equal("Мой проект", navigator.MainNavigations[0].Project.Name);
    }

    [Fact]
    public void CreateProjectAt_FolderNotEmpty_DoesNotCallNavigator()
    {
        var (vm, storage, _, _, _, navigator) = CreateViewModel();
        storage.ExceptionToThrowOnCreate = new InvalidOperationException("Папка не пуста.");

        vm.CreateProjectAt(@"C:\Projects\Занято");

        Assert.Empty(navigator.MainNavigations);
        Assert.True(vm.HasError);
    }

    [Fact]
    public void OpenProjectAt_ValidPath_CallsNavigator()
    {
        var (vm, storage, _, _, _, navigator) = CreateViewModel();
        storage.ProjectToReturnOnLoad = new Project(
            "Существующий", DateTime.Now, "data.xlsx", Array.Empty<Field>());

        vm.OpenProjectAt(@"C:\Projects\Существующий");

        Assert.Single(navigator.MainNavigations);
        Assert.Equal(@"C:\Projects\Существующий", navigator.MainNavigations[0].ProjectFolderPath);
    }

    [Fact]
    public void OpenProjectAt_ProjectNotFound_DoesNotCallNavigator()
    {
        var (vm, storage, _, _, _, navigator) = CreateViewModel();
        storage.ExceptionToThrowOnLoad = new FileNotFoundException("Нет файла.");

        vm.OpenProjectAt(@"C:\Projects\Пусто");

        Assert.Empty(navigator.MainNavigations);
        Assert.True(vm.HasError);
    }
}