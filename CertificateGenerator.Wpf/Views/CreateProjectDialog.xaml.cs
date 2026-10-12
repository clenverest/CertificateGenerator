using System.IO;
using System.Windows;
using System.Windows.Controls;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using Microsoft.Win32;

namespace CertificateGenerator.Wpf.Views;

public partial class CreateProjectDialog : Window
{
    private readonly IFolderPicker _folderPicker;

    public CreateProjectResult? Result { get; private set; }

    public CreateProjectDialog(IFolderPicker folderPicker)
    {
        _folderPicker = folderPicker;
        InitializeComponent();
    }

    // ---------------------------------------------------------------------
    // Обработчики кнопок
    // ---------------------------------------------------------------------

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var folder = _folderPicker.PickFolder("Выберите папку для проекта");
        if (folder is null)
        {
            return;
        }

        FolderPathBox.Text = folder;
        UpdateCreateButtonState();
    }

    private void OnCreateClick(object sender, RoutedEventArgs e)
    {
        if (!TryValidate(out var errorMessage))
        {
            ShowError(errorMessage);
            return;
        }

        var parentFolder = FolderPathBox.Text.Trim();
        var projectName = ProjectNameBox.Text.Trim();
        var projectFolder = Path.Combine(parentFolder, projectName);

        Result = new CreateProjectResult(parentFolder, projectName);
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnProjectNameChanged(object sender, TextChangedEventArgs e)
    {
        UpdateCreateButtonState();
    }

    // ---------------------------------------------------------------------
    // Валидация
    // ---------------------------------------------------------------------

    private bool TryValidate(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(FolderPathBox.Text))
        {
            errorMessage = "Выберите расположение проекта.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ProjectNameBox.Text))
        {
            errorMessage = "Введите имя проекта.";
            return false;
        }

        var name = ProjectNameBox.Text.Trim();

        // Проверка на недопустимые символы в имени папки.
        var invalidChars = Path.GetInvalidFileNameChars();
        if (name.IndexOfAny(invalidChars) >= 0)
        {
            errorMessage = "Имя проекта содержит недопустимые символы.";
            return false;
        }

        // Проверка, что папка с таким именем ещё не существует.
        var projectFolder = Path.Combine(FolderPathBox.Text.Trim(), name);
        if (Directory.Exists(projectFolder) &&
            Directory.EnumerateFileSystemEntries(projectFolder).Any())
        {
            errorMessage = $"Папка \"{projectFolder}\" уже существует и не пуста. " +
                           "Выберите другое имя или расположение.";
            return false;
        }

        return true;
    }

    private void UpdateCreateButtonState()
    {
        var hasFolder = !string.IsNullOrWhiteSpace(FolderPathBox.Text);
        var hasName = !string.IsNullOrWhiteSpace(ProjectNameBox.Text);
        CreateButton.IsEnabled = hasFolder && hasName;

        if (CreateButton.IsEnabled)
        {
            HideError();
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorPanel.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        ErrorPanel.Visibility = Visibility.Collapsed;
    }
}