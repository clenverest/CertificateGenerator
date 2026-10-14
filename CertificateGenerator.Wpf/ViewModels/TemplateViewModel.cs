using System.IO;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

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

    /// <summary>Минимальный масштаб отображения — 10%.</summary>
    private const double MinZoom = 0.1;

    /// <summary>Максимальный масштаб — 400%.</summary>
    private const double MaxZoom = 4.0;

    /// <summary>Шаг изменения масштаба — 25%.</summary>
    private const double ZoomStep = 0.25;

    private const int DefaultX = 100;
    private const int DefaultY = 100;
    private const int DefaultFontSize = 48;
    private const string DefaultFontFamily = "Arial";
    private const FieldAlignment DefaultAlignment = FieldAlignment.Center;

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
    /// Масштаб отображения шаблона. 1.0 = 100%.
    /// </summary>
    [ObservableProperty]
    private double _zoomLevel = 1.0;

    /// <summary>
    /// Выбранное размещение. Панель настройки привязана к нему.
    /// </summary>
    [ObservableProperty]
    private FieldPlacementViewModel? _selectedPlacement;   

    /// <summary>
    /// Есть ли загруженный шаблон.
    /// </summary>
    public bool HasTemplate => Template is not null;

    /// <summary>
    /// Есть ли ошибка.
    /// </summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>
    /// Есть ли выбранное размещение (для активации панели настройки).
    /// </summary>
    public bool HasSelectedPlacement => SelectedPlacement is not null;

    /// <summary>
    /// Ширина шаблона в отображаемых пикселях с учётом масштаба.
    /// </summary>
    public double ScaledWidth => (Template?.Width ?? 0) * ZoomLevel;

    /// <summary>
    /// Высота шаблона в отображаемых пикселях с учётом масштаба.
    /// </summary>
    public double ScaledHeight => (Template?.Height ?? 0) * ZoomLevel;

    /// <summary>
    /// Размещения полей на шаблоне. Управляют координатами
    /// и настройками отображения.
    /// </summary>
    public ObservableCollection<FieldPlacementViewModel> Placements { get; } = new();

    /// <summary>
    /// Можно ли увеличить масштаб.
    /// </summary>
    public bool CanZoomIn => ZoomLevel < MaxZoom;

    /// <summary>
    /// Можно ли уменьшить масштаб.
    /// </summary>
    public bool CanZoomOut => ZoomLevel > MinZoom;

    /// <summary>
    /// Вызывается, когда пользователь загрузил новый шаблон.
    /// MainViewModel подписывается и сохраняет TemplateInfo в project.json.
    /// </summary>
    public event EventHandler<TemplateInfo>? TemplateChanged;

    /// <summary>
    /// Вызывается, когда список размещений изменился:
    /// добавлено, удалено или изменено любое свойство.
    /// MainViewModel подписывается и сохраняет Project.Placements.
    /// </summary>
    public event EventHandler? PlacementsChanged;

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

        LoadPlacements(project.Placements);
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

    /// <summary>
    /// Загрузить размещения из проекта. Заменяет текущий список.
    /// </summary>
    private void LoadPlacements(IReadOnlyList<FieldPlacement> placements)
    {
        // Отписываемся от старых.
        foreach (var vm in Placements)
        {
            vm.Changed -= OnPlacementViewModelChanged;
        }

        Placements.Clear();

        foreach (var model in placements)
        {
            var vm = new FieldPlacementViewModel(model);
            vm.Changed += OnPlacementViewModelChanged;
            Placements.Add(vm);
        }

        SelectedPlacement = null;
    }

    /// <summary>
    /// Согласовать список размещений с актуальным списком полей.
    /// Удаляет размещения для полей, которых больше нет.
    /// Новые поля НЕ размещает автоматически.
    /// </summary>
    public void SyncFields(IReadOnlyList<Field> fields)
    {
        if (fields is null)
        {
            throw new ArgumentNullException(nameof(fields));
        }

        var fieldNames = new HashSet<string>(
            fields.Select(f => f.Name),
            StringComparer.OrdinalIgnoreCase);

        // Удаляем placements, для которых нет поля.
        var toRemove = Placements
            .Where(p => !fieldNames.Contains(p.FieldName))
            .ToList();

        foreach (var vm in toRemove)
        {
            vm.Changed -= OnPlacementViewModelChanged;
            Placements.Remove(vm);
        }

        // Если выбранное размещение удалено — сбрасываем выбор.
        if (SelectedPlacement is not null && !Placements.Contains(SelectedPlacement))
        {
            SelectedPlacement = null;
        }

        if (toRemove.Count > 0)
        {
            RaisePlacementsChanged();
        }
    }

    /// <summary>
    /// Есть ли размещение для указанного поля.
    /// </summary>
    public bool HasPlacement(Field field)
    {
        if (field is null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        return Placements.Any(p =>
            string.Equals(p.FieldName, field.Name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Получить размещение для указанного поля. null, если не размещено.
    /// </summary>
    public FieldPlacementViewModel? GetPlacement(Field field)
    {
        if (field is null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        return Placements.FirstOrDefault(p =>
            string.Equals(p.FieldName, field.Name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Создать размещение для поля с координатами по умолчанию.
    /// Ничего не делает, если размещение уже есть или шаблон не загружен.
    /// </summary>
    public void AddPlacementForField(Field field)
    {
        if (field is null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        if (Template is null)
        {
            return;  // Без шаблона размещать некуда.
        }

        if (HasPlacement(field))
        {
            return;  // Уже размещено.
        }

        var model = new FieldPlacement(
            field.Name,
            DefaultX,
            DefaultY,
            DefaultFontSize,
            DefaultFontFamily,
            DefaultAlignment);

        var vm = new FieldPlacementViewModel(model);
        vm.Changed += OnPlacementViewModelChanged;
        Placements.Add(vm);

        SelectedPlacement = vm;

        RaisePlacementsChanged();
    }

    /// <summary>
    /// Удалить размещение для указанного поля.
    /// </summary>
    public void RemovePlacement(FieldPlacementViewModel placement)
    {
        if (placement is null)
        {
            return;
        }

        placement.Changed -= OnPlacementViewModelChanged;

        if (Placements.Remove(placement))
        {
            if (ReferenceEquals(SelectedPlacement, placement))
            {
                SelectedPlacement = null;
            }

            RaisePlacementsChanged();
        }
    }

    /// <summary>
    /// Снимок списка размещений в виде иммутабельных моделей.
    /// Для сохранения в Project.
    /// </summary>
    public IReadOnlyList<FieldPlacement> GetPlacementsSnapshot()
    {
        return Placements.Select(vm => vm.ToModel()).ToList();
    }

    // ---------------------------------------------------------------------
    // Обработчики
    // ---------------------------------------------------------------------

    private void OnPlacementViewModelChanged(object? sender, EventArgs e)
    {
        RaisePlacementsChanged();
    }

    private void RaisePlacementsChanged()
    {
        PlacementsChanged?.Invoke(this, EventArgs.Empty);
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

    /// <summary>
    /// Увеличить масштаб на один шаг.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanZoomIn))]
    private void ZoomIn()
    {
        var next = ZoomLevel + ZoomStep;
        ZoomLevel = next > MaxZoom ? MaxZoom : next;
    }

    /// <summary>
    /// Уменьшить масштаб на один шаг.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanZoomOut))]
    private void ZoomOut()
    {
        var next = ZoomLevel - ZoomStep;
        ZoomLevel = next < MinZoom ? MinZoom : next;
    }

    /// <summary>
    /// Сбросить масштаб к 100%.
    /// </summary>
    [RelayCommand]
    private void ResetZoom()
    {
        ZoomLevel = 1.0;
    }

    // ---------------------------------------------------------------------
    // Реакция на изменение свойств
    // ---------------------------------------------------------------------

    partial void OnTemplateChanged(TemplateInfo? value)
    {
        OnPropertyChanged(nameof(HasTemplate));
        OnPropertyChanged(nameof(ScaledWidth));
        OnPropertyChanged(nameof(ScaledHeight));
    }

    partial void OnZoomLevelChanged(double value)
    {
        OnPropertyChanged(nameof(ScaledWidth));
        OnPropertyChanged(nameof(ScaledHeight));
        OnPropertyChanged(nameof(CanZoomIn));
        OnPropertyChanged(nameof(CanZoomOut));
    }

    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnSelectedPlacementChanged(FieldPlacementViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedPlacement));
    }
}