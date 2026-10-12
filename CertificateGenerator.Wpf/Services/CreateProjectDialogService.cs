using System.Windows;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using CertificateGenerator.Wpf.Views;

namespace CertificateGenerator.Wpf.Services;

public sealed class CreateProjectDialogService : ICreateProjectDialogService
{
    private readonly IFolderPicker _folderPicker;

    public CreateProjectDialogService(IFolderPicker folderPicker)
    {
        _folderPicker = folderPicker;
    }

    public CreateProjectResult? Show()
    {
        var dialog = new CreateProjectDialog(_folderPicker)
        {
            Owner = Application.Current.MainWindow
        };

        var dialogResult = dialog.ShowDialog();

        return dialogResult == true ? dialog.Result : null;
    }
}