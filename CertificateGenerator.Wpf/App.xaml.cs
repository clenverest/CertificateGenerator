using System.Windows;
using System.Windows.Threading;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using CertificateGenerator.Wpf.Services;
using CertificateGenerator.Wpf.ViewModels;
using CertificateGenerator.Infrastructure.Readers;
using CertificateGenerator.Core.Validators;
using CertificateGenerator.Infrastructure.Rendering;
using CertificateGenerator.Infrastructure.Generators;

namespace CertificateGenerator.Wpf;

/// <summary>
/// Точка входа приложения. Собирает DI-контейнер и запускает стартовое окно.
/// </summary>
public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        // Лицензия QuestPDF. Community — бесплатно для учебных и небольших проектов.
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

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
        services.AddSingleton<ITemplateStorage, FileTemplateStorage>();
        services.AddSingleton<IPdfGenerator, QuestPdfGenerator>();

        // Валидация.
        services.AddSingleton<IValidator>(ProjectValidator.CreateDefault());

        // Wpf-сервисы.
        services.AddSingleton<IFolderPicker, WpfFolderPicker>();
        services.AddSingleton<IFilePicker, WpfFilePicker>();  
        services.AddSingleton<ICreateProjectDialogService, CreateProjectDialogService>();
        services.AddSingleton<IWindowNavigator, WpfWindowNavigator>();
        services.AddSingleton<IFieldEditDialogService, FieldEditDialogService>();
        services.AddSingleton<ITemplateRenderer, SkiaTemplateRenderer>();
        

        // ViewModels.
        services.AddTransient<ProjectViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<FieldsViewModel>();
        services.AddTransient<TemplateViewModel>();

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