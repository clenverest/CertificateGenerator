using CertificateGenerator.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CertificateGenerator.Wpf.ViewModels;

/// <summary>
/// ViewModel одного размещения поля на шаблоне.
/// Обёртка над иммутабельным FieldPlacement.
/// </summary>
public sealed partial class FieldPlacementViewModel : ObservableObject
{
    private FieldPlacement _model;

    /// <summary>
    /// Имя поля, к которому относится размещение.
    /// Read-only: ссылка на поле проекта.
    /// </summary>
    public string FieldName => _model.FieldName;

    /// <summary>Координата X в пикселях исходного шаблона.</summary>
    [ObservableProperty]
    private int _x;

    /// <summary>Координата Y в пикселях исходного шаблона.</summary>
    [ObservableProperty]
    private int _y;

    /// <summary>Размер шрифта в пикселях.</summary>
    [ObservableProperty]
    private int _fontSize;

    /// <summary>Имя семейства шрифта.</summary>
    [ObservableProperty]
    private string _fontFamily;

    /// <summary>Выравнивание текста.</summary>
    [ObservableProperty]
    private FieldAlignment _alignment;

    /// <summary>
    /// Событие: любое свойство размещения изменилось.
    /// Используется для автосохранения.
    /// </summary>
    public event EventHandler? Changed;

    public FieldPlacementViewModel(FieldPlacement model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));

        // Инициализируем observable-свойства значениями из модели.
        // Внимание: это вызовет partial-методы и Changed — надо подавить.
        // Проще: присваиваем напрямую поля через SetProperty, но с флагом.

        _x = model.X;
        _y = model.Y;
        _fontSize = model.FontSize;
        _fontFamily = model.FontFamily;
        _alignment = model.Alignment;
    }

    /// <summary>
    /// Получить текущую иммутабельную модель.
    /// </summary>
    public FieldPlacement ToModel() => _model;

    // ---------------------------------------------------------------------
    // Реакция на изменение свойств
    // ---------------------------------------------------------------------

    partial void OnXChanged(int value)
    {
        _model = _model.WithX(value);
        RaiseChanged();
    }

    partial void OnYChanged(int value)
    {
        _model = _model.WithY(value);
        RaiseChanged();
    }

    partial void OnFontSizeChanged(int value)
    {
        _model = _model.WithFontSize(value);
        RaiseChanged();
    }

    partial void OnFontFamilyChanged(string value)
    {
        _model = _model.WithFontFamily(value);
        RaiseChanged();
    }

    partial void OnAlignmentChanged(FieldAlignment value)
    {
        _model = _model.WithAlignment(value);
        RaiseChanged();
    }

    private void RaiseChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}