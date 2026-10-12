using System.Windows;
using CertificateGenerator.Core.Interfaces;
using Microsoft.Win32;

namespace CertificateGenerator.Wpf.Services;

/// <summary>
/// Реализация IFolderPicker поверх стандартного диалога WPF.
/// </summary>
public sealed class WpfFolderPicker : IFolderPicker
{
    public string? PickFolder(string title)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false
        };

        var result = dialog.ShowDialog();
        return result == true ? dialog.FolderName : null;
    }
}