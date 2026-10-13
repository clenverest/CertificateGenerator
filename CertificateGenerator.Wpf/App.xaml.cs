using System.Windows;
using System.Windows.Threading;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using CertificateGenerator.Wpf.Services;
using CertificateGenerator.Wpf.ViewModels;
using CertificateGenerator.Infrastructure.Readers;

namespace CertificateGenerator.Wpf;

/// <summary>
/// Точка входа приложения. Собирает DI-контейнер и запускает стартовое окно.
/// </summary>
public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    /// <summary>
    /// Регистрация всех зависимостей приложения.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Инфраструктура.
        services.AddSingleton<IProjectStorage, JsonProjectStorage>();
        services.AddSingleton<IRecentProjectsStore, JsonRecentProjectsStore>();
        services.AddSingleton<ITableReaderResolver, TableReaderResolver>();

        // Wpf-сервисы.
        services.AddSingleton<IFolderPicker, WpfFolderPicker>();
        services.AddSingleton<ICreateProjectDialogService, CreateProjectDialogService>();
        services.AddSingleton<IWindowNavigator, WpfWindowNavigator>();
        

        // ViewModels.
        services.AddTransient<ProjectViewModel>();
        services.AddTransient<MainViewModel>();

        // Окна.
        services.AddTransient<StartupWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var startupWindow = _serviceProvider.GetRequiredService<StartupWindow>();
        Application.Current.MainWindow = startupWindow;
        startupWindow.Show();
    }

    /// <summary>
    /// Глобальный обработчик необработанных исключений UI-потока.
    /// Показывает сообщение и не даёт приложению молча упасть.
    /// </summary>
    private void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"Произошла непредвиденная ошибка:\n\n{e.Exception.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }
}