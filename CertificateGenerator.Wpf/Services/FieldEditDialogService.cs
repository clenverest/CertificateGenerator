using System.Windows;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using CertificateGenerator.Wpf.Views;

namespace CertificateGenerator.Wpf.Services;

public sealed class FieldEditDialogService : IFieldEditDialogService
{
    public Field? ShowCreate(IReadOnlyList<string> existingNames)
    {
        var dialog = new FieldEditDialog(existingNames)
        {
            Owner = Application.Current.MainWindow
        };

        var result = dialog.ShowDialog();

        return result == true ? dialog.Result : null;
    }

    public Field? ShowEdit(Field field, IReadOnlyList<string> existingNames)
    {
        var dialog = new FieldEditDialog(field, existingNames)
        {
            Owner = Application.Current.MainWindow
        };

        var result = dialog.ShowDialog();

        return result == true ? dialog.Result : null;
    }
}