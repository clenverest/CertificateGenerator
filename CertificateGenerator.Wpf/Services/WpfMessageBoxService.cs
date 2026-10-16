using System.Windows;
using CertificateGenerator.Core.Interfaces;

namespace CertificateGenerator.Wpf.Services;

/// <summary>
/// Реализация IMessageBoxService через стандартный WPF MessageBox.
/// </summary>
public sealed class WpfMessageBoxService : IMessageBoxService
{
    public void ShowInfo(string message, string title)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public void ShowWarning(string message, string title)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    public void ShowError(string message, string title)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}