using System.Windows;
using System.Windows.Controls;
using CertificateGenerator.Core.Models;

namespace CertificateGenerator.Wpf.Views;

public partial class FieldEditDialog : Window
{
    private readonly IReadOnlyList<string> _existingNames;

    /// <summary>
    /// Результат диалога. <c>null</c>, если отменён.
    /// </summary>
    public Field? Result { get; private set; }

    /// <summary>
    /// Создать диалог для создания нового поля.
    /// </summary>
    public FieldEditDialog(IReadOnlyList<string> existingNames)
    {
        _existingNames = existingNames;
        InitializeComponent();

        Title = "Новое поле";
        NameBox.Focus();
    }

    /// <summary>
    /// Создать диалог для редактирования существующего поля.
    /// </summary>
    public FieldEditDialog(Field field, IReadOnlyList<string> existingNames)
        : this(existingNames)
    {
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

        if (field.Kind == FieldKind.Static)
        {
            StaticValueBox.Text = field.StaticValue ?? string.Empty;
        }

        UpdateSaveButtonState();
    }

    // ---------------------------------------------------------------------
    // Обработчики
    // ---------------------------------------------------------------------

    private void OnNameChanged(object sender, TextChangedEventArgs e)
    {
        UpdateSaveButtonState();
    }

    private void OnStaticValueChanged(object sender, TextChangedEventArgs e)
    {
        UpdateSaveButtonState();
    }

    private void OnKindChanged(object sender, SelectionChangedEventArgs e)
    {
        // Первое событие может прийти до инициализации UI.
        if (StaticValueLabel is null || StaticValueBox is null)
        {
            return;
        }

        var kind = GetSelectedKind();
        var isStatic = kind == FieldKind.Static;

        var visibility = isStatic ? Visibility.Visible : Visibility.Collapsed;
        StaticValueLabel.Visibility = visibility;
        StaticValueBox.Visibility = visibility;

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

        if (kind == FieldKind.Static)
        {
            if (string.IsNullOrWhiteSpace(StaticValueBox.Text))
            {
                errorMessage = "Введите значение для статического поля.";
                return false;
            }

            field = new Field(
                NameBox.Text.Trim(),
                FieldKind.Static,
                Array.Empty<int>(),
                staticValue: StaticValueBox.Text.Trim());
            return true;
        }

        // Simple / FullName. Колонки-источники — заглушка [0].
        // Сопоставление с колонками — Этап 2.5.
        var indexes = new[] { 0 };

        field = new Field(NameBox.Text.Trim(), kind, indexes);
        return true;
    }

    private void UpdateSaveButtonState()
    {
        var hasName = !string.IsNullOrWhiteSpace(NameBox.Text);
        var kind = GetSelectedKind();
        var hasValue = kind != FieldKind.Static ||
                       !string.IsNullOrWhiteSpace(StaticValueBox.Text);

        SaveButton.IsEnabled = hasName && hasValue;

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