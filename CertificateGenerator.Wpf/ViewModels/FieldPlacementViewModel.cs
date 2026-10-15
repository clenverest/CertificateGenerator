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
    [NotifyPropertyChangedFor(nameof(DisplayX))]
    private int _x;

    /// <summary>Координата Y в пикселях исходного шаблона.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayY))]
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
    /// Масштаб отображения шаблона. Влияет на DisplayX/DisplayY.
    /// Синхронизируется из TemplateViewModel.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayX))]
    [NotifyPropertyChangedFor(nameof(DisplayY))]
    private double _zoomLevel = 1.0;

    /// <summary>
    /// Выбрано ли это размещение. Управляет видом маркера.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Координата X для отображения (с учётом масштаба).
    /// </summary>
    public double DisplayX => X * ZoomLevel;

    /// <summary>
    /// Координата Y для отображения (с учётом масштаба).
    /// </summary>
    public double DisplayY => Y * ZoomLevel;

    /// <summary>
    /// Событие: любое свойство размещения изменилось.
    /// Используется для автосохранения.
    /// </summary>
    public event EventHandler? Changed;

    public FieldPlacementViewModel(FieldPlacement model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));

        // Присваиваем поля напрямую — partial-методы не вызовутся,
        // Changed не сработает до подписки.
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