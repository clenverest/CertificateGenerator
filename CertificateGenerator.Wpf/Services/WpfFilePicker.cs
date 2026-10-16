using CertificateGenerator.Core.Interfaces;
using Microsoft.Win32;

namespace CertificateGenerator.Wpf.Services;

/// <summary>
/// Реализация IFilePicker поверх стандартного диалога WPF.
/// </summary>
public sealed class WpfFilePicker : IFilePicker
{
    public string? PickFile(string title, string filter)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            Multiselect = false,
            CheckFileExists = true
        };

        var result = dialog.ShowDialog();
        return result == true ? dialog.FileName : null;
    }

    public string? PickSaveFile(
    string title,
    string filter,
    string defaultFileName,
    string? defaultFolderPath = null)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            FileName = defaultFileName,
            InitialDirectory = defaultFolderPath ?? string.Empty,
            OverwritePrompt = true,
            AddExtension = true,
            DefaultExt = ".pdf"
        };

        var result = dialog.ShowDialog();
        return result == true ? dialog.FileName : null;
    }
}