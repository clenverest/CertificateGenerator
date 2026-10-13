using System.Windows;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using CertificateGenerator.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CertificateGenerator.Wpf.Services;

/// <summary>
/// Реализация IWindowNavigator поверх WPF-окон.
/// </summary>
public sealed class WpfWindowNavigator : IWindowNavigator
{
    private readonly IServiceProvider _serviceProvider;

    public WpfWindowNavigator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateToMain(Project project, string projectFolderPath)
    {
        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        var mainWindow = new MainWindow(mainViewModel);

        // Initialize ПОСЛЕ создания MainWindow —
        // чтобы MainWindow уже подписался на PropertyChanged.
        mainViewModel.Initialize(project, projectFolderPath);

        Application.Current.MainWindow = mainWindow;
        mainWindow.Show();

        CloseStartupWindow();
    }

    public void NavigateToStartup()
    {
        var startupWindow = _serviceProvider.GetRequiredService<StartupWindow>();
        Application.Current.MainWindow = startupWindow;
        startupWindow.Show();

        CloseAllExcept(startupWindow);
    }

    private static void CloseStartupWindow()
    {
        foreach (Window window in Application.Current.Windows)
        {
            if (window is StartupWindow)
            {
                window.Close();
                return;
            }
        }
    }

    private static void CloseAllExcept(Window keep)
    {
        var toClose = new List<Window>();
        foreach (Window window in Application.Current.Windows)
        {
            if (window != keep)
            {
                toClose.Add(window);
            }
        }
        foreach (var window in toClose)
        {
            window.Close();
        }
    }
}