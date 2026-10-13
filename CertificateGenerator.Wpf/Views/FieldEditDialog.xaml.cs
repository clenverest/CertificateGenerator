using System.Windows;
using System.Windows.Controls;
using CertificateGenerator.Core.Models;
using CertificateGenerator.Core.Services;

namespace CertificateGenerator.Wpf.Views;

public partial class FieldEditDialog : Window
{
    /// <summary>
    /// Индекс-заглушка для «— не выбрано —» во 2-й/3-й колонке ФИО.
    /// </summary>
    private const int NoColumnIndex = -1;

    private readonly IReadOnlyList<string> _existingNames;
    private readonly IReadOnlyList<ColumnInfo> _columns;
    private readonly bool _isEditMode;

    /// <summary>
    /// Результат диалога. <c>null</c>, если отменён.
    /// </summary>
    public Field? Result { get; private set; }

    /// <summary>
    /// Создать диалог для создания нового поля.
    /// </summary>
    public FieldEditDialog(
        IReadOnlyList<string> existingNames,
        IReadOnlyList<ColumnInfo> columns)
    {
        _existingNames = existingNames;
        _columns = columns;
        _isEditMode = false;

        InitializeComponent();

        Title = "Новое поле";
        FillColumns();

        UpdatePanelVisibility();

        // Автоподсказка для Simple — по имени пока ничего не введено,
        // поэтому подсказка появится при смене типа.
        ApplySuggestionForCurrentKind();

        NameBox.Focus();
        UpdateSaveButtonState();
    }

    /// <summary>
    /// Создать диалог для редактирования существующего поля.
    /// </summary>
    public FieldEditDialog(
        Field field,
        IReadOnlyList<string> existingNames,
        IReadOnlyList<ColumnInfo> columns)
        : this(existingNames, columns)
    {
        _isEditMode = true;

        Title = "Редактирование поля";
        NameBox.Text = field.Name;
        NameBox.CaretIndex = NameBox.Text.Length;

        KindBox.SelectedIndex = field.Kind switch
        {
            FieldKind.Simple => 0,
            FieldKind.FullName => 1,
            FieldKind.Static => 2,
            _ => 0
        };

        IsRequiredBox.IsChecked = field.IsRequired;

        if (field.Kind == FieldKind.Static)
        {
            StaticValueBox.Text = field.StaticValue ?? string.Empty;
        }
        else if (field.Kind == FieldKind.Simple)
        {
            SetSelectedIndex(SimpleColumnBox, field.SourceColumnIndexes, 0);
        }
        else if (field.Kind == FieldKind.FullName)
        {
            SetSelectedIndex(FullNameColumn1Box, field.SourceColumnIndexes, 0);
            SetSelectedIndex(FullNameColumn2Box, field.SourceColumnIndexes, 1);
            SetSelectedIndex(FullNameColumn3Box, field.SourceColumnIndexes, 2);
        }

        UpdatePanelVisibility();
        UpdateSaveButtonState();
    }

    // ---------------------------------------------------------------------
    // Заполнение ComboBox колонками
    // ---------------------------------------------------------------------

    private void FillColumns()
    {
        // Simple: обычный список без заглушки.
        SimpleColumnBox.ItemsSource = _columns;

        // FullName: 1-я колонка без заглушки, 2-я и 3-я — с заглушкой.
        FullNameColumn1Box.ItemsSource = _columns;

        var columnsWithEmpty = new List<ColumnInfo>
        {
            new(NoColumnIndex, "— не выбрано —")
        };
        columnsWithEmpty.AddRange(_columns);

        FullNameColumn2Box.ItemsSource = columnsWithEmpty;
        FullNameColumn3Box.ItemsSource = columnsWithEmpty;
    }

    /// <summary>
    /// Выделить в ComboBox колонку по её позиции в <paramref name="indexes"/>.
    /// </summary>
    private static void SetSelectedIndex(
        ComboBox box,
        IReadOnlyList<int> indexes,
        int position)
    {
        if (position >= indexes.Count)
        {
            box.SelectedIndex = -1;
            return;
        }

        box.SelectedValue = indexes[position];
    }

    // ---------------------------------------------------------------------
    // Автоподсказка
    // ---------------------------------------------------------------------

    private void ApplySuggestionForCurrentKind()
    {
        if (_isEditMode)
        {
            return;  // В режиме редактирования — не подсказываем, оставляем как есть.
        }

        var kind = GetSelectedKind();
        if (kind == FieldKind.Simple)
        {
            var suggested = ColumnSuggester.SuggestSimple(NameBox.Text, _columns);
            if (suggested.Count == 1)
            {
                SimpleColumnBox.SelectedValue = suggested[0];
            }
        }
        else if (kind == FieldKind.FullName)
        {
            var suggested = ColumnSuggester.SuggestFullName(_columns);
            if (suggested.Count == 1)
            {
                FullNameColumn1Box.SelectedValue = suggested[0];
            }
            else if (suggested.Count == 3)
            {
                FullNameColumn1Box.SelectedValue = suggested[0];
                FullNameColumn2Box.SelectedValue = suggested[1];
                FullNameColumn3Box.SelectedValue = suggested[2];
            }
        }
    }

    // ---------------------------------------------------------------------
    // Обработчики
    // ---------------------------------------------------------------------

    private void OnNameChanged(object sender, TextChangedEventArgs e)
    {
        // Если пользователь стёр имя и ввёл другое — новая подсказка.
        ApplySuggestionForCurrentKind();
        UpdateSaveButtonState();
    }

    private void OnStaticValueChanged(object sender, TextChangedEventArgs e)
    {
        UpdateSaveButtonState();
    }

    private void OnColumnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSaveButtonState();
    }

    private void OnKindChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SimpleColumnPanel is null || FullNamePanel is null || StaticValuePanel is null)
        {
            return;
        }

        UpdatePanelVisibility();
        ApplySuggestionForCurrentKind();
        UpdateSaveButtonState();
    }

    private void OnIsRequiredChanged(object sender, RoutedEventArgs e)
    {
        UpdateSaveButtonState();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (!TryBuildResult(out var field, out var errorMessage))
        {
            ShowError(errorMessage);
            return;
        }

        Result = field;
        DialogResult = true;
    }

    // ---------------------------------------------------------------------
    // Показ/скрытие панелей
    // ---------------------------------------------------------------------

    private void UpdatePanelVisibility()
    {
        var kind = GetSelectedKind();

        SimpleColumnPanel.Visibility =
            kind == FieldKind.Simple ? Visibility.Visible : Visibility.Collapsed;

        FullNamePanel.Visibility =
            kind == FieldKind.FullName ? Visibility.Visible : Visibility.Collapsed;

        StaticValuePanel.Visibility =
            kind == FieldKind.Static ? Visibility.Visible : Visibility.Collapsed;
    }

    // ---------------------------------------------------------------------
    // Логика
    // ---------------------------------------------------------------------

    private FieldKind GetSelectedKind()
    {
        if (KindBox.SelectedItem is ComboBoxItem item &&
            item.Tag is string tag &&
            Enum.TryParse<FieldKind>(tag, out var kind))
        {
            return kind;
        }
        return FieldKind.Simple;
    }

    private bool TryBuildResult(out Field field, out string errorMessage)
    {
        field = null!;
        errorMessage = string.Empty;

        var nameError = Field.ValidateName(NameBox.Text, _existingNames);
        if (nameError is not null)
        {
            errorMessage = nameError;
            return false;
        }

        var kind = GetSelectedKind();
        var trimmedName = NameBox.Text.Trim();
        var isRequired = IsRequiredBox.IsChecked == true;

        if (kind == FieldKind.Static)
        {
            if (string.IsNullOrWhiteSpace(StaticValueBox.Text))
            {
                errorMessage = "Введите значение для статического поля.";
                return false;
            }

            field = new Field(
                trimmedName,
                FieldKind.Static,
                Array.Empty<int>(),
                isRequired: isRequired,
                staticValue: StaticValueBox.Text.Trim());
            return true;
        }

        if (kind == FieldKind.Simple)
        {
            var index = GetSelectedIndex(SimpleColumnBox);
            if (index is null)
            {
                errorMessage = "Выберите колонку-источник.";
                return false;
            }

            field = new Field(
                trimmedName,
                FieldKind.Simple,
                new[] { index.Value },
                isRequired: isRequired);
            return true;
        }

        // FullName
        var indexes = new List<int>();

        var idx1 = GetSelectedIndex(FullNameColumn1Box);
        if (idx1 is null)
        {
            errorMessage = "Выберите первую колонку для склейки ФИО.";
            return false;
        }
        indexes.Add(idx1.Value);

        var idx2 = GetSelectedIndex(FullNameColumn2Box);
        if (idx2 is not null && idx2.Value != NoColumnIndex)
        {
            indexes.Add(idx2.Value);
        }

        var idx3 = GetSelectedIndex(FullNameColumn3Box);
        if (idx3 is not null && idx3.Value != NoColumnIndex)
        {
            indexes.Add(idx3.Value);
        }

        field = new Field(
            trimmedName,
            FieldKind.FullName,
            indexes,
            isRequired: isRequired);
        return true;
    }

    /// <summary>
    /// Получить выбранный индекс из ComboBox.
    /// <c>null</c>, если ничего не выбрано.
    /// </summary>
    private static int? GetSelectedIndex(ComboBox box)
    {
        if (box.SelectedValue is int value)
        {
            return value;
        }
        return null;
    }

    private void UpdateSaveButtonState()
    {
        var hasName = !string.IsNullOrWhiteSpace(NameBox.Text);
        var kind = GetSelectedKind();

        bool hasRequiredData = kind switch
        {
            FieldKind.Simple => GetSelectedIndex(SimpleColumnBox) is not null,
            FieldKind.FullName => GetSelectedIndex(FullNameColumn1Box) is not null,
            FieldKind.Static => !string.IsNullOrWhiteSpace(StaticValueBox.Text),
            _ => false
        };

        SaveButton.IsEnabled = hasName && hasRequiredData;

        if (SaveButton.IsEnabled)
        {
            HideError();
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorPanel.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        ErrorPanel.Visibility = Visibility.Collapsed;
    }
}