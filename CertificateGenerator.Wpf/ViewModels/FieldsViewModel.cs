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
    [RelayCommand]
    private void AddField()
    {
        var created = _dialogService.ShowCreate(ExistingNames);
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

        // Имена других полей — для проверки дубликата.
        var otherNames = Fields
            .Where(f => !ReferenceEquals(f, SelectedField))
            .Select(f => f.Name)
            .ToList();

        var updated = _dialogService.ShowEdit(SelectedField, otherNames);
        if (updated is null)
        {
            return;
        }

        // Заменяем элемент в коллекции (Field иммутабельный).
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