using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using CertificateGenerator.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Windows;

namespace CertificateGenerator.Wpf.ViewModels;

/// <summary>
/// ViewModel главного окна: работа с проектом и таблицей участников.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ITableReaderResolver _tableReaderResolver;
    private readonly IProjectStorage _projectStorage;
    private readonly IWindowNavigator _navigator;
    private readonly IValidator _validator;
    private readonly ITemplateStorage _templateStorage;
    private readonly IPdfGenerator _pdfGenerator;
    private readonly IFilePicker _filePicker;
    private readonly IMessageBoxService _messageBox;

    private bool _isLoadingFields;

    [ObservableProperty]
    private TableData? _tableData;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _selectedFilePath;

    [ObservableProperty]
    private Project? _currentProject;

    [ObservableProperty]
    private string? _projectFolderPath;

    [ObservableProperty]
    private IReadOnlyList<ValidationIssue> _validationIssues =
        Array.Empty<ValidationIssue>();

    /// <summary>
    /// Выбранная строка таблицы (участник). Используется для генерации PDF.
    /// </summary>
    [ObservableProperty]
    private RowEditViewModel? _selectedRow;

    /// <summary>
    /// Сообщение о результате генерации PDF (успех / ошибка).
    /// null, если генерация ещё не запускалась.
    /// </summary>
    [ObservableProperty]
    private string? _pdfStatusMessage;

    /// <summary>
    /// ViewModel для работы со списком полей проекта.
    /// </summary>
    public FieldsViewModel Fields { get; }

    /// <summary>
    /// Предпросмотр значений полей для первого участника таблицы.
    /// </summary>
    public ObservableCollection<FieldPreviewItem> FieldPreviews { get; } = new();

    /// <summary>
    /// Строки таблицы в режиме редактирования.
    /// Пересобираются при смене <see cref="TableData"/>.
    /// </summary>
    public ObservableCollection<RowEditViewModel> Rows { get; } = new();

    /// <summary>
    /// Строки для таблицы проверки. Заполняются в <see cref="RunValidation"/>.
    /// </summary>
    public ObservableCollection<ValidationRowViewModel> ValidationRows { get; } = new();

    /// <summary>
    /// ViewModel для работы с шаблоном грамоты.
    /// </summary>
    public TemplateViewModel Template { get; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasPdfStatus => !string.IsNullOrWhiteSpace(PdfStatusMessage);

    public bool HasSummary => TableData is not null;

    /// <summary>
    /// Количество ошибок в текущей валидации.
    /// </summary>
    public int ErrorCount =>
        ValidationIssues.Count(i => i.Severity == IssueSeverity.Error);

    /// <summary>
    /// Количество предупреждений в текущей валидации.
    /// </summary>
    public int WarningCount =>
        ValidationIssues.Count(i => i.Severity == IssueSeverity.Warning);

    /// <summary>
    /// Текстовая сводка для UI.
    /// </summary>
    public string ValidationSummary
    {
        get
        {
            if (TableData is null)
            {
                return string.Empty;
            }

            if (ErrorCount == 0 && WarningCount == 0)
            {
                return "✓ Проблем не найдено";
            }

            if (ErrorCount == 0)
            {
                return $"Предупреждений: {WarningCount}";
            }

            if (WarningCount == 0)
            {
                return $"Ошибок: {ErrorCount}";
            }

            return $"Ошибок: {ErrorCount} · Предупреждений: {WarningCount}";
        }
    }

    /// <summary>
    /// Есть ли проблемы (ошибки или предупреждения).
    /// </summary>
    public bool HasIssues => ErrorCount > 0 || WarningCount > 0;

    public bool HasProject => CurrentProject is not null;

    public bool HasPreviews => FieldPreviews.Count > 0;

    public string TableSummary =>
        TableData is null
            ? string.Empty
            : $"Загружено: {TableData.Rows.Count} × {TableData.Columns.Count}";

    public string ProjectName =>
        CurrentProject?.Name ?? string.Empty;

    public string ProjectPath =>
        ProjectFolderPath ?? string.Empty;

    public GenerationViewModel Generation { get; }

    public MainViewModel(
        ITableReaderResolver tableReaderResolver,
        IProjectStorage projectStorage,
        IWindowNavigator navigator,
        FieldsViewModel fields,
        IValidator validator,
        TemplateViewModel template,
        ITemplateStorage templateStorage,
        IPdfGenerator pdfGenerator,
        IFilePicker filePicker,
        IMessageBoxService messageBox,
        GenerationViewModel generation)
    {
        _tableReaderResolver = tableReaderResolver;
        _projectStorage = projectStorage;
        _navigator = navigator;
        Fields = fields;
        _validator = validator;
        Template = template;
        _templateStorage = templateStorage;
        _pdfGenerator = pdfGenerator;
        _filePicker = filePicker;
        _messageBox = messageBox;
        Generation = generation;

        // Подписки — до любых изменений,
        // чтобы не потерять CollectionChanged / TemplateChanged.
        Fields.Fields.CollectionChanged += OnFieldsCollectionChanged;
        Template.TemplateChanged += OnTemplateChanged;
        Template.PlacementsChanged += OnPlacementsChanged;

        Generation.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(GenerationViewModel.IsRunning))
            {
                GenerateAllCommand.NotifyCanExecuteChanged();
            }
        };
    }

    /// <summary>
    /// Инициализировать ViewModel для работы с проектом.
    /// </summary>
    public void Initialize(Project project, string projectFolderPath)
    {
        CurrentProject = project;
        ProjectFolderPath = projectFolderPath;

        _isLoadingFields = true;
        try
        {
            Fields.LoadFields(project.Fields);
        }
        finally
        {
            _isLoadingFields = false;
        }

        // Шаблон — до таблицы: он не зависит от неё.
        Template.Initialize(project, projectFolderPath);

        // Согласовать размещения с полями проекта.
        Template.SyncFields(project.Fields);

        TryLoadExistingTable();
        UpdateFieldPreviews();
    }

    // ---------------------------------------------------------------------
    // Реакция на изменение списка полей
    // ---------------------------------------------------------------------

    private void OnFieldsCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        if (!_isLoadingFields && CurrentProject is not null && ProjectFolderPath is not null)
        {
            var updated = WithFields(Fields.GetFieldsSnapshot());

            try
            {
                _projectStorage.Save(updated, ProjectFolderPath);
                CurrentProject = updated;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Не удалось сохранить поля: {ex.Message}";
            }
        }

        // Превью и валидация обновляются в любом случае — даже при загрузке.
        UpdateFieldPreviews();
        RunValidation();

        // Согласовать размещения: если поле удалено — убрать его placement.
        if (CurrentProject is not null)
        {
            Template.SyncFields(Fields.GetFieldsSnapshot());
        }
    }

    /// <summary>
    /// Пользователь загрузил новый шаблон.
    /// Сохраняем TemplateInfo в project.json.
    /// </summary>
    private void OnTemplateChanged(object? sender, TemplateInfo info)
    {
        GeneratePdfCommand.NotifyCanExecuteChanged();

        if (CurrentProject is null || ProjectFolderPath is null)
        {
            return;
        }

        var updated = WithTemplate(info);

        try
        {
            _projectStorage.Save(updated, ProjectFolderPath);
            CurrentProject = updated;
        }
        catch (Exception ex)
        {
            Template.ErrorMessage = $"Не удалось сохранить шаблон: {ex.Message}";
        }
    }

    /// <summary>
    /// Размещения полей изменились. Сохраняем в project.json.
    /// </summary>
    private void OnPlacementsChanged(object? sender, EventArgs e)
    {
        GeneratePdfCommand.NotifyCanExecuteChanged();

        if (CurrentProject is null || ProjectFolderPath is null)
        {
            return;
        }

        var updated = WithPlacements(Template.GetPlacementsSnapshot());

        try
        {
            _projectStorage.Save(updated, ProjectFolderPath);
            CurrentProject = updated;
        }
        catch (Exception ex)
        {
            Template.ErrorMessage = $"Не удалось сохранить размещения: {ex.Message}";
        }
    }

    /// <summary>
    /// Создать копию <see cref="CurrentProject"/> с новым списком размещений.
    /// </summary>
    private Project WithPlacements(IReadOnlyList<FieldPlacement> placements)
    {
        var project = CurrentProject
            ?? throw new InvalidOperationException("Проект не открыт.");

        return new Project(
            project.Name,
            project.CreatedAt,
            project.SourceFileName,
            project.Fields,
            project.RowOverrides,
            project.Template,
            placements);
    }

    /// <summary>
    /// Создать копию <see cref="CurrentProject"/> с новым шаблоном.
    /// </summary>
    private Project WithTemplate(TemplateInfo template)
    {
        var project = CurrentProject
            ?? throw new InvalidOperationException("Проект не открыт.");

        return new Project(
            project.Name,
            project.CreatedAt,
            project.SourceFileName,
            project.Fields,
            project.RowOverrides,
            template,
            project.Placements);
    }

    /// <summary>
    /// Создать копию <see cref="CurrentProject"/> с новым списком полей.
    /// </summary>
    private Project WithFields(IReadOnlyList<Field> fields)
    {
        var project = CurrentProject
            ?? throw new InvalidOperationException("Проект не открыт.");

        return new Project(
            project.Name,
            project.CreatedAt,
            project.SourceFileName,
            fields);
    }

    // ---------------------------------------------------------------------
    // Предпросмотр значений полей
    // ---------------------------------------------------------------------

    private void UpdateFieldPreviews()
    {
        FieldPreviews.Clear();

        if (TableData is null || TableData.Rows.Count == 0)
        {
            OnPropertyChanged(nameof(HasPreviews));

            // Пустой словарь — превью грамоты покажет имена полей.
            Template.SetFieldValues(new Dictionary<string, string>());
            return;
        }

        var firstRow = TableData.Rows[0];
        var fields = Fields.GetFieldsSnapshot();

        var values = new Dictionary<string, string>();

        foreach (var field in fields)
        {
            var value = FieldValueResolver.Resolve(field, firstRow);
            FieldPreviews.Add(new FieldPreviewItem(field.Name, value));
            values[field.Name] = value;
        }

        OnPropertyChanged(nameof(HasPreviews));

        // Сообщаем TemplateViewModel значения для превью.
        Template.SetFieldValues(values);
    }

    /// <summary>
    /// Пересчитать валидацию таблицы и полей.
    /// Результат — в <see cref="ValidationIssues"/>.
    /// </summary>
    private void RunValidation()
    {
        if (TableData is null || TableData.Rows.Count == 0)
        {
            ValidationIssues = Array.Empty<ValidationIssue>();
            RebuildValidationRows();
            return;
        }

        // Собираем актуальную таблицу из VM-строк —
        // TableData иммутабельная и не отражает правки пользователя.
        var currentTable = BuildCurrentTable();
        ValidationIssues = _validator.Validate(currentTable, Fields.GetFieldsSnapshot());
        RebuildValidationRows();
    }

    /// <summary>
    /// Собрать актуальную таблицу из текущих VM-строк (с учётом правок).
    /// </summary>
    private TableData BuildCurrentTable()
    {
        var rows = new List<IReadOnlyList<string>>(Rows.Count);
        foreach (var row in Rows)
        {
            var cells = new List<string>(row.Cells.Count);
            foreach (var cell in row.Cells)
            {
                cells.Add(cell.Value);
            }
            rows.Add(cells);
        }

        return new TableData(TableData!.Columns, rows);
    }

    private void RebuildValidationRows()
    {
        ValidationRows.Clear();

        if (TableData is null || TableData.Rows.Count == 0)
        {
            return;
        }

        var fields = Fields.GetFieldsSnapshot();
        if (fields.Count == 0)
        {
            return;
        }

        var issueIndex = BuildIssueIndex(ValidationIssues);

        for (var rowIndex = 0; rowIndex < Rows.Count; rowIndex++)
        {
            var rowValues = Rows[rowIndex].Cells.Select(c => c.Value).ToList();
            var cells = new List<ValidationCellViewModel>(fields.Count);

            foreach (var field in fields)
            {
                var value = FieldValueResolver.Resolve(field, rowValues);

                var hasIssue = issueIndex.TryGetValue(
                    (rowIndex, field.Name),
                    out var flags);

                var hasError = hasIssue && flags.HasError;
                var hasWarning = hasIssue && flags.HasWarning;

                cells.Add(new ValidationCellViewModel(
                    field.Name,
                    value,
                    hasError,
                    hasWarning));
            }

            ValidationRows.Add(new ValidationRowViewModel(rowIndex, cells));
        }
    }

    /// <summary>
    /// Индекс: для каждой (строка, поле) — есть ли Error и Warning.
    /// Позволяет за O(1) проверить проблемы для ячейки.
    /// </summary>
    private static Dictionary<(int RowIndex, string FieldName), (bool HasError, bool HasWarning)>
        BuildIssueIndex(IReadOnlyList<ValidationIssue> issues)
    {
        var index = new Dictionary<(int, string), (bool HasError, bool HasWarning)>();

        foreach (var issue in issues)
        {
            if (issue.RowIndex < 0)
            {
                continue;
            }

            var key = (issue.RowIndex, issue.FieldName);

            // Читаем текущие флаги (или false/false, если записи нет).
            index.TryGetValue(key, out var flags);

            var hasError = flags.HasError || issue.Severity == IssueSeverity.Error;
            var hasWarning = flags.HasWarning || issue.Severity == IssueSeverity.Warning;

            index[key] = (hasError, hasWarning);
        }

        return index;
    }

    /// <summary>
    /// Пересобрать строки редактирования из <see cref="TableData"/>.
    /// Вызывается при смене таблицы.
    /// </summary>
    private void RebuildRows()
    {
        // Отписываемся от старых строк — иначе утечка.
        foreach (var row in Rows)
        {
            row.ValueChanged -= OnRowValueChanged;
        }

        Rows.Clear();

        if (TableData is null)
        {
            return;
        }

        for (var i = 0; i < TableData.Rows.Count; i++)
        {
            var row = new RowEditViewModel(i, TableData.Rows[i]);
            row.ValueChanged += OnRowValueChanged;
            Rows.Add(row);
        }
    }

    /// <summary>
    /// Сохранить (или удалить) override для строки.
    /// Если строка грязная — сохраняем текущие значения.
    /// Если чистая — удаляем override (пользователь вернул исходные данные).
    /// </summary>
    private void SaveRowOverride(RowEditViewModel row)
    {
        if (CurrentProject is null || ProjectFolderPath is null)
        {
            return;
        }

        var overrides = CurrentProject.RowOverrides.ToList();
        var existingIndex = overrides.FindIndex(o => o.RowIndex == row.RowIndex);

        if (row.IsDirty)
        {
            // Собираем значения всех ячеек.
            var values = row.Cells.Select(c => c.Value).ToList();
            var newOverride = new RowOverride(row.RowIndex, values);

            if (existingIndex >= 0)
            {
                overrides[existingIndex] = newOverride;
            }
            else
            {
                overrides.Add(newOverride);
            }
        }
        else
        {
            // Строка чистая — удаляем override, если был.
            if (existingIndex >= 0)
            {
                overrides.RemoveAt(existingIndex);
            }
            else
            {
                // Нечего удалять — выходим без сохранения.
                return;
            }
        }

        // Создаём новый Project с обновлёнными RowOverrides.
        var updated = WithRowOverrides(overrides);

        try
        {
            _projectStorage.Save(updated, ProjectFolderPath);
            CurrentProject = updated;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось сохранить правку: {ex.Message}";
        }
    }

    /// <summary>
    /// Создать копию <see cref="CurrentProject"/> с новым списком правок.
    /// </summary>
    private Project WithRowOverrides(IReadOnlyList<RowOverride> rowOverrides)
    {
        var project = CurrentProject
            ?? throw new InvalidOperationException("Проект не открыт.");

        return new Project(
            project.Name,
            project.CreatedAt,
            project.SourceFileName,
            project.Fields,
            rowOverrides);
    }

    private void OnRowValueChanged(object? sender, EventArgs e)
    {
        if (sender is RowEditViewModel row)
        {
            SaveRowOverride(row);
        }

        RunValidation();
    }

    // ---------------------------------------------------------------------
    // LoadTableIntoProjectCommand
    // ---------------------------------------------------------------------

    [RelayCommand]
    private void LoadTableIntoProject()
    {
        if (CurrentProject is null || ProjectFolderPath is null)
        {
            ErrorMessage = "Проект не открыт.";
            return;
        }

        if (TableData is not null)
        {
            // Замену таблицы пока не поддерживаем через диалог Да/Нет,
            // оставляем как есть — пользователь сам решает.
        }

        var dialog = new OpenFileDialog
        {
            Title = "Выберите файл с участниками",
            Filter = "Таблицы (*.xlsx;*.csv)|*.xlsx;*.csv"
        };

        var result = dialog.ShowDialog();
        if (result != true)
        {
            return;
        }

        LoadTableIntoProjectFromPath(dialog.FileName);
    }

    /// <summary>
    /// Отменить все правки таблицы и вернуться к исходным данным из source/.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanResetChanges))]
    private void ResetChanges()
    {
        if (CurrentProject is null || ProjectFolderPath is null)
        {
            ErrorMessage = "Проект не открыт.";
            return;
        }

        if (CurrentProject.RowOverrides.Count == 0)
        {
            return;
        }

        // 1. Очищаем RowOverrides и сохраняем.
        var cleared = WithRowOverrides(Array.Empty<RowOverride>());

        try
        {
            _projectStorage.Save(cleared, ProjectFolderPath);
            CurrentProject = cleared;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось сохранить отмену: {ex.Message}";
            return;
        }

        // 2. Перечитываем исходную таблицу — без правок.
        ReloadTableFromSource();
    }

    /// <summary>
    /// Доступна ли команда: только если есть правки.
    /// </summary>
    private bool CanResetChanges()
    {
        return CurrentProject is not null && CurrentProject.RowOverrides.Count > 0;
    }

    // ---------------------------------------------------------------------
    // GeneratePdfCommand
    // ---------------------------------------------------------------------

    /// <summary>
    /// Сгенерировать PDF-грамоту для выбранного участника.
    /// Открывает диалог «Сохранить как…», по умолчанию — папка output/.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGeneratePdf))]
    private void GeneratePdf()
    {
        // Дополнительные проверки — на случай, если CanExecute не сработал.
        if (CurrentProject is null || ProjectFolderPath is null)
        {
            _messageBox.ShowWarning(
                "Проект не открыт.",
                "Генерация PDF");
            return;
        }

        if (CurrentProject.Template is null)
        {
            _messageBox.ShowWarning(
                "Сначала загрузите PNG-шаблон на вкладке «Шаблон».",
                "Генерация PDF");
            return;
        }

        if (SelectedRow is null)
        {
            _messageBox.ShowWarning(
                "Выберите участника в таблице на вкладке «Данные».",
                "Генерация PDF");
            return;
        }

        var fields = Fields.GetFieldsSnapshot();
        if (fields.Count == 0)
        {
            _messageBox.ShowWarning(
                "В проекте нет полей. Добавьте поля на вкладке «Данные».",
                "Генерация PDF");
            return;
        }

        // Значения ячеек выбранной строки.
        var rowValues = SelectedRow.Cells.Select(c => c.Value).ToList();

        // Ищем поле «ФИО» — для имени файла.
        var nameField = GetNameFieldForFileName(fields);
        var nameValue = FieldValueResolver.Resolve(nameField, rowValues);

        if (string.IsNullOrWhiteSpace(nameValue))
        {
            _messageBox.ShowWarning(
                "У выбранного участника нет значения для имени файла.",
                "Генерация PDF");
            return;
        }

        // Собираем значения всех полей.
        var fieldValues = new Dictionary<string, string>();
        foreach (var field in fields)
        {
            fieldValues[field.Name] = FieldValueResolver.Resolve(field, rowValues);
        }

        // Путь к PNG-шаблону.
        var templatePath = _templateStorage.GetTemplatePath(ProjectFolderPath);
        if (templatePath is null)
        {
            _messageBox.ShowError(
                "Не удалось найти файл шаблона.",
                "Генерация PDF");
            return;
        }

        // Генерируем PDF.
        byte[] pdf;
        try
        {
            pdf = _pdfGenerator.GenerateCertificate(
                templatePath,
                Template.GetPlacementsSnapshot(),
                fieldValues);
        }
        catch (Exception ex)
        {
            _messageBox.ShowError(
                $"Не удалось сгенерировать PDF:\n\n{ex.Message}",
                "Генерация PDF");
            return;
        }

        // Папка output/ — создаём, если нет.
        var outputFolder = Path.Combine(ProjectFolderPath, "output");
        try
        {
            Directory.CreateDirectory(outputFolder);
        }
        catch (Exception ex)
        {
            _messageBox.ShowError(
                $"Не удалось создать папку output/:\n\n{ex.Message}",
                "Генерация PDF");
            return;
        }

        // Имя файла по умолчанию: «ФИО».pdf
        var safeName = MakeSafeFileName(nameValue);
        if (string.IsNullOrEmpty(safeName))
        {
            safeName = "Грамота";
        }
        var defaultFileName = safeName + ".pdf";

        // Диалог «Сохранить как…»
        var targetPath = _filePicker.PickSaveFile(
            "Сохранить грамоту как…",
            "PDF-документы (*.pdf)|*.pdf",
            defaultFileName,
            outputFolder);

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            // Пользователь отменил — тихо выходим.
            return;
        }

        // Запись файла.
        try
        {
            File.WriteAllBytes(targetPath, pdf);
            PdfStatusMessage = $"PDF сохранён: {targetPath}";
        }
        catch (Exception ex)
        {
            PdfStatusMessage = null;
            _messageBox.ShowError(
                $"Не удалось сохранить файл:\n\n{ex.Message}",
                "Генерация PDF");
        }
    }

    /// <summary>
    /// Доступна ли команда: есть шаблон, размещения и выбранный участник.
    /// </summary>
    private bool CanGeneratePdf()
    {
        return CurrentProject?.Template is not null
            && Template.Placements.Count > 0
            && SelectedRow is not null;
    }

    /// <summary>
    /// Найти поле для имени файла: сначала FullName, потом первое доступное.
    /// </summary>
    private static Field GetNameFieldForFileName(IReadOnlyList<Field> fields)
    {
        var fullName = fields.FirstOrDefault(f => f.Kind == FieldKind.FullName);
        return fullName ?? fields[0];
    }

    /// <summary>
    /// Заменить недопустимые символы имени файла на «_».
    /// </summary>
    private static string MakeSafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder(value.Length);

        foreach (var ch in value)
        {
            sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
        }

        return sb.ToString().Trim();
    }

    internal void LoadTableIntoProjectFromPath(string sourceFilePath)
    {
        if (CurrentProject is null || ProjectFolderPath is null)
        {
            ErrorMessage = "Проект не открыт.";
            return;
        }

        ErrorMessage = null;
        IsBusy = true;

        try
        {
            var updatedProject = _projectStorage.SetSourceTable(
                ProjectFolderPath, CurrentProject, sourceFilePath);

            CurrentProject = updatedProject;

            var projectTablePath = _projectStorage.GetSourceTablePath(
                updatedProject, ProjectFolderPath);

            if (projectTablePath is null)
            {
                ErrorMessage = "Не удалось найти скопированную таблицу.";
                TableData = null;
                return;
            }

            var reader = _tableReaderResolver.Resolve(projectTablePath);
            var rawTable = reader.Read(projectTablePath);

            // Применить сохранённые правки строк, если они есть.
            TableData = RowOverrideApplier.Apply(rawTable, updatedProject.RowOverrides);
            SelectedFilePath = projectTablePath;
        }
        catch (NotSupportedException ex)
        {
            ErrorMessage = ex.Message;
            TableData = null;
        }
        catch (FileNotFoundException ex)
        {
            ErrorMessage = ex.Message;
            TableData = null;
        }
        catch (InvalidDataException ex)
        {
            ErrorMessage = ex.Message;
            TableData = null;
        }
        catch (IOException ex)
        {
            ErrorMessage = ex.Message;
            TableData = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Непредвиденная ошибка: {ex.Message}";
            TableData = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ---------------------------------------------------------------------
    // OpenFileCommand — старая логика с Этапа 1
    // ---------------------------------------------------------------------

    [RelayCommand]
    private void OpenFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите файл с участниками",
            Filter = "Таблицы (*.xlsx;*.csv)|*.xlsx;*.csv|Все файлы (*.*)|*.*"
        };

        var result = dialog.ShowDialog();
        if (result != true)
        {
            return;
        }

        LoadTable(dialog.FileName);
    }

    internal void LoadTable(string filePath)
    {
        ErrorMessage = null;
        IsBusy = true;

        try
        {
            SelectedFilePath = filePath;
            var reader = _tableReaderResolver.Resolve(filePath);
            TableData = reader.Read(filePath);
        }
        catch (NotSupportedException ex)
        {
            ErrorMessage = ex.Message;
            TableData = null;
        }
        catch (FileNotFoundException ex)
        {
            ErrorMessage = ex.Message;
            TableData = null;
        }
        catch (InvalidDataException ex)
        {
            ErrorMessage = ex.Message;
            TableData = null;
        }
        catch (IOException ex)
        {
            ErrorMessage = ex.Message;
            TableData = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Непредвиденная ошибка: {ex.Message}";
            TableData = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ---------------------------------------------------------------------
    // Приватные методы
    // ---------------------------------------------------------------------

    private void TryLoadExistingTable()
    {
        if (CurrentProject is null || ProjectFolderPath is null)
        {
            return;
        }

        var projectTablePath = _projectStorage.GetSourceTablePath(
            CurrentProject, ProjectFolderPath);

        if (projectTablePath is null)
        {
            return;
        }

        ErrorMessage = null;
        IsBusy = true;

        try
        {
            var reader = _tableReaderResolver.Resolve(projectTablePath);
            var rawTable = reader.Read(projectTablePath);

            TableData = RowOverrideApplier.Apply(rawTable, CurrentProject.RowOverrides);
            SelectedFilePath = projectTablePath;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить таблицу из проекта: {ex.Message}";
            TableData = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Перечитать исходную таблицу из source/ и применить текущие правки.
    /// Используется при отмене изменений.
    /// </summary>
    private void ReloadTableFromSource()
    {
        if (CurrentProject is null || ProjectFolderPath is null)
        {
            return;
        }

        var projectTablePath = _projectStorage.GetSourceTablePath(
            CurrentProject, ProjectFolderPath);

        if (projectTablePath is null)
        {
            TableData = null;
            return;
        }

        try
        {
            var reader = _tableReaderResolver.Resolve(projectTablePath);
            var rawTable = reader.Read(projectTablePath);

            TableData = RowOverrideApplier.Apply(rawTable, CurrentProject.RowOverrides);
            SelectedFilePath = projectTablePath;
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось перезагрузить таблицу: {ex.Message}";
        }
    }

    // ---------------------------------------------------------------------
    // Реакция на изменение свойств
    // ---------------------------------------------------------------------

    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnTableDataChanged(TableData? value)
    {
        OnPropertyChanged(nameof(HasSummary));
        OnPropertyChanged(nameof(TableSummary));

        Fields.SetColumns(value?.Columns ?? Array.Empty<ColumnInfo>());
        RebuildRows();
        UpdateFieldPreviews();
        RunValidation();
    }

    partial void OnCurrentProjectChanged(Project? value)
    {
        OnPropertyChanged(nameof(HasProject));
        OnPropertyChanged(nameof(ProjectName));
        ResetChangesCommand.NotifyCanExecuteChanged();
        GeneratePdfCommand.NotifyCanExecuteChanged();
    }

    partial void OnProjectFolderPathChanged(string? value)
    {
        OnPropertyChanged(nameof(ProjectPath));
    }

    partial void OnValidationIssuesChanged(IReadOnlyList<ValidationIssue> value)
    {
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(WarningCount));
        OnPropertyChanged(nameof(ValidationSummary));
        OnPropertyChanged(nameof(HasIssues));
    }

    partial void OnSelectedRowChanged(RowEditViewModel? value)
    {
        GeneratePdfCommand.NotifyCanExecuteChanged();
    }

    partial void OnPdfStatusMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasPdfStatus));
    }

    // ---------------------------------------------------------------------
    // GenerateAllCommand — массовая генерация
    // ---------------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(CanGenerateAll))]
    private async Task GenerateAllAsync()
    {
        // Проверки.
        if (CurrentProject is null || ProjectFolderPath is null)
        {
            _messageBox.ShowWarning("Проект не открыт.", "Массовая генерация");
            return;
        }

        if (CurrentProject.Template is null)
        {
            _messageBox.ShowWarning(
                "Сначала загрузите PNG-шаблон на вкладке «Шаблон».",
                "Массовая генерация");
            return;
        }

        var fields = Fields.GetFieldsSnapshot();
        if (fields.Count == 0)
        {
            _messageBox.ShowWarning(
                "В проекте нет полей. Добавьте поля на вкладке «Данные».",
                "Массовая генерация");
            return;
        }

        if (Rows.Count == 0)
        {
            _messageBox.ShowWarning(
                "Таблица пуста — нечего генерировать.",
                "Массовая генерация");
            return;
        }

        var templatePath = _templateStorage.GetTemplatePath(ProjectFolderPath);
        if (templatePath is null)
        {
            _messageBox.ShowError(
                "Не удалось найти файл шаблона.",
                "Массовая генерация");
            return;
        }

        // Папка output/.
        var outputFolder = Path.Combine(ProjectFolderPath, "output");
        Directory.CreateDirectory(outputFolder);

        var zipName = $"грамоты_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip";
        var zipPath = Path.Combine(outputFolder, zipName);

        // Собираем актуальные строки (значения ячеек).
        var rows = Rows.Select(r => (IReadOnlyList<string>)r.Cells.Select(c => c.Value).ToList()).ToList();

        // Запускаем в GenerationViewModel.
        await Generation.RunAsync(
            templatePath,
            Template.GetPlacementsSnapshot(),
            fields,
            rows,
            zipPath);
    }

    private bool CanGenerateAll()
    {
        return CurrentProject?.Template is not null
            && Template.Placements.Count > 0
            && Rows.Count > 0
            && !Generation.IsRunning;
    }
}