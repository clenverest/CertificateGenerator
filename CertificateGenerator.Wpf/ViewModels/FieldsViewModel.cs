using System.Collections.ObjectModel;
using System.Windows;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CertificateGenerator.Wpf.ViewModels;

/// <summary>
/// ViewModel для работы со списком полей проекта:
/// добавление, редактирование, удаление, выбор.
/// </summary>
public sealed partial class FieldsViewModel : ObservableObject
{
    private readonly IFieldEditDialogService _dialogService;

    /// <summary>
    /// Список полей проекта. UI биндится на это свойство.
    /// </summary>
    public ObservableCollection<Field> Fields { get; } = new();

    /// <summary>
    /// Колонки загруженной таблицы. Пусто, если таблица ещё не загружена.
    /// </summary>
    public IReadOnlyList<ColumnInfo> Columns { get; private set; } =
        Array.Empty<ColumnInfo>();

    /// <summary>
    /// Есть ли загруженная таблица с колонками.
    /// </summary>
    public bool HasColumns => Columns.Count > 0;

    /// <summary>
    /// Выбранное поле. <c>null</c>, если ничего не выбрано.
    /// </summary>
    [ObservableProperty]
    private Field? _selectedField;

    /// <summary>
    /// Список имён уже существующих полей — для проверки дубликатов.
    /// </summary>
    public IReadOnlyList<string> ExistingNames =>
        Fields.Select(f => f.Name).ToList();

    public FieldsViewModel(IFieldEditDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    /// <summary>
    /// Загрузить список полей из проекта. Заменяет текущее содержимое.
    /// </summary>
    public void LoadFields(IEnumerable<Field> fields)
    {
        Fields.Clear();
        foreach (var field in fields)
        {
            Fields.Add(field);
        }
        SelectedField = null;
    }

    /// <summary>
    /// Установить колонки таблицы. Вызывается из MainViewModel
    /// при загрузке таблицы.
    /// </summary>
    public void SetColumns(IReadOnlyList<ColumnInfo> columns)
    {
        Columns = columns ?? Array.Empty<ColumnInfo>();
        OnPropertyChanged(nameof(Columns));
        OnPropertyChanged(nameof(HasColumns));
        AddFieldCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Снимок текущего списка полей — для сохранения в <see cref="Project"/>.
    /// </summary>
    public IReadOnlyList<Field> GetFieldsSnapshot()
    {
        return Fields.ToList();
    }

    // ---------------------------------------------------------------------
    // Команды
    // ---------------------------------------------------------------------

    /// <summary>
    /// Добавить поле — открыть диалог создания.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddField))]
    private void AddField()
    {
        var created = _dialogService.ShowCreate(ExistingNames, Columns);
        if (created is null)
        {
            return;
        }

        Fields.Add(created);
        SelectedField = created;
    }

    /// <summary>
    /// Редактировать выбранное поле — открыть диалог редактирования.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private void EditField()
    {
        if (SelectedField is null)
        {
            return;
        }

        var otherNames = Fields
            .Where(f => !ReferenceEquals(f, SelectedField))
            .Select(f => f.Name)
            .ToList();

        var updated = _dialogService.ShowEdit(SelectedField, otherNames, Columns);
        if (updated is null)
        {
            return;
        }

        var index = Fields.IndexOf(SelectedField);
        Fields[index] = updated;
        SelectedField = updated;
    }

    /// <summary>
    /// Удалить выбранное поле, с подтверждением.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditOrDelete))]
    private void DeleteField()
    {
        if (SelectedField is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Удалить поле \"{SelectedField.Name}\"?",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        Fields.Remove(SelectedField);
        SelectedField = null;
    }

    private bool CanAddField()
    {
        return HasColumns;
    }

    private bool CanEditOrDelete()
    {
        return SelectedField is not null;
    }

    partial void OnSelectedFieldChanged(Field? value)
    {
        EditFieldCommand.NotifyCanExecuteChanged();
        DeleteFieldCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(ExistingNames));
    }
}