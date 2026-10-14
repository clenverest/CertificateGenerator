using System.IO;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CertificateGenerator.Wpf.ViewModels;

/// <summary>
/// ViewModel вкладки «Шаблон»: загрузка PNG-шаблона проекта,
/// хранение информации о нём и превью.
/// </summary>
public sealed partial class TemplateViewModel : ObservableObject
{
    private readonly ITemplateStorage _templateStorage;
    private readonly IFilePicker _filePicker;

    private string? _projectFolderPath;

    /// <summary>
    /// Информация о текущем шаблоне. null, если шаблон не загружен.
    /// </summary>
    [ObservableProperty]
    private TemplateInfo? _template;

    /// <summary>
    /// Полный путь к PNG-файлу шаблона.
    /// </summary>
    [ObservableProperty]
    private string? _templatePath;

    /// <summary>
    /// Байты PNG для отображения в UI. null, если шаблона нет.
    /// </summary>
    [ObservableProperty]
    private byte[]? _previewImage;

    /// <summary>
    /// Ошибка вкладки «Шаблон». null, если ошибок нет.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Есть ли загруженный шаблон.
    /// </summary>
    public bool HasTemplate => Template is not null;

    /// <summary>
    /// Есть ли ошибка.
    /// </summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>
    /// Вызывается, когда пользователь загрузил новый шаблон.
    /// MainViewModel подписывается и сохраняет TemplateInfo в project.json.
    /// </summary>
    public event EventHandler<TemplateInfo>? TemplateChanged;

    public TemplateViewModel(
        ITemplateStorage templateStorage,
        IFilePicker filePicker)
    {
        _templateStorage = templateStorage;
        _filePicker = filePicker;
    }

    // ---------------------------------------------------------------------
    // Инициализация
    // ---------------------------------------------------------------------

    /// <summary>
    /// Привязать VM к проекту. Загружает существующий шаблон, если он есть.
    /// </summary>
    public void Initialize(Project project, string projectFolderPath)
    {
        if (project is null)
        {
            throw new ArgumentNullException(nameof(project));
        }

        if (string.IsNullOrWhiteSpace(projectFolderPath))
        {
            throw new ArgumentException(
                "Путь к папке проекта не может быть пустым.",
                nameof(projectFolderPath));
        }

        _projectFolderPath = projectFolderPath;

        ErrorMessage = null;

        // Сначала — что записано в проекте.
        Template = project.Template;

        // Потом — что реально лежит в template/. Файл приоритетнее.
        TryLoadExistingTemplate();
    }

    /// <summary>
    /// Прочитать существующий шаблон из папки проекта.
    /// </summary>
    private void TryLoadExistingTemplate()
    {
        if (_projectFolderPath is null)
        {
            return;
        }

        try
        {
            var info = _templateStorage.GetTemplate(_projectFolderPath);

            if (info is null)
            {
                // Файла нет. Если в проекте была запись — сбрасываем.
                Template = null;
                TemplatePath = null;
                PreviewImage = null;
                return;
            }

            Template = info;
            TemplatePath = _templateStorage.GetTemplatePath(_projectFolderPath);
            PreviewImage = File.ReadAllBytes(TemplatePath);
        }
        catch (Exception ex)
        {
            // Шаблон не прочитался — не падаем, просто показываем ошибку.
            Template = null;
            TemplatePath = null;
            PreviewImage = null;
            ErrorMessage = $"Не удалось загрузить шаблон: {ex.Message}";
        }
    }

    // ---------------------------------------------------------------------
    // Команды
    // ---------------------------------------------------------------------

    /// <summary>
    /// Загрузить PNG-шаблон. Открывает диалог, копирует файл в проект,
    /// обновляет состояние и уведомляет MainViewModel через TemplateChanged.
    /// </summary>
    [RelayCommand]
    private void LoadTemplate()
    {
        if (_projectFolderPath is null)
        {
            ErrorMessage = "Проект не открыт.";
            return;
        }

        ErrorMessage = null;

        var selected = _filePicker.PickFile(
            "Выберите PNG-шаблон грамоты",
            "PNG-картинки (*.png)|*.png");

        if (string.IsNullOrWhiteSpace(selected))
        {
            // Пользователь отменил — ничего не делаем.
            return;
        }

        try
        {
            var info = _templateStorage.SaveTemplate(_projectFolderPath, selected);

            Template = info;
            TemplatePath = _templateStorage.GetTemplatePath(_projectFolderPath);
            PreviewImage = File.ReadAllBytes(TemplatePath);

            // Сообщаем MainViewModel — он сохранит в project.json.
            TemplateChanged?.Invoke(this, info);
        }
        catch (FileNotFoundException ex)
        {
            ErrorMessage = $"Файл шаблона не найден: {ex.Message}";
        }
        catch (InvalidDataException ex)
        {
            ErrorMessage = $"Некорректный файл шаблона: {ex.Message}";
        }
        catch (IOException ex)
        {
            ErrorMessage = $"Не удалось скопировать шаблон: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Непредвиденная ошибка: {ex.Message}";
        }
    }

    // ---------------------------------------------------------------------
    // Реакция на изменение свойств
    // ---------------------------------------------------------------------

    partial void OnTemplateChanged(TemplateInfo? value)
    {
        OnPropertyChanged(nameof(HasTemplate));
    }

    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }
}