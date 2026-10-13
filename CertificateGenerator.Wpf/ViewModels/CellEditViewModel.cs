using CommunityToolkit.Mvvm.ComponentModel;

namespace CertificateGenerator.Wpf.ViewModels;

/// <summary>
/// ViewModel одной ячейки таблицы в режиме редактирования.
/// Хранит исходное значение и текущее, позволяет отследить изменения.
/// </summary>
public sealed partial class CellEditViewModel : ObservableObject
{
    /// <summary>
    /// Исходное значение ячейки. Не меняется в течение сессии редактирования.
    /// </summary>
    public string OriginalValue { get; }

    /// <summary>
    /// Текущее значение. Меняется пользователем через DataGrid.
    /// </summary>
    [ObservableProperty]
    private string _value;

    /// <summary>
    /// Изменено ли значение относительно исходного.
    /// </summary>
    public bool IsDirty => Value != OriginalValue;

    /// <summary>
    /// Создать ячейку.
    /// </summary>
    /// <param name="originalValue">Исходное значение.
    /// <c>null</c> превращается в пустую строку.</param>
    public CellEditViewModel(string? originalValue)
    {
        OriginalValue = originalValue ?? string.Empty;
        _value = OriginalValue;
    }

    /// <summary>
    /// Откатить значение к исходному.
    /// </summary>
    public void Reset()
    {
        Value = OriginalValue;
    }

    partial void OnValueChanged(string value)
    {
        OnPropertyChanged(nameof(IsDirty));
    }
}