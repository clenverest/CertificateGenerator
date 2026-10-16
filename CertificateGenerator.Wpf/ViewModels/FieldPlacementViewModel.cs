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
    /// Идёт перетаскивание маркера. Пока true — Changed не вызывается,
    /// чтобы не сохранять project.json на каждое движение мыши.
    /// </summary>
    private bool _isDragging;  

    /// <summary>
    /// Имя поля, к которому относится размещение.
    /// Read-only: ссылка на поле проекта.
    /// </summary>
    public string FieldName => _model.FieldName;

    /// <summary>Координата X в пикселях исходного шаблона.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayX))]
    [NotifyPropertyChangedFor(nameof(FinalDisplayX))]
    private int _x;

    /// <summary>Координата Y в пикселях исходного шаблона.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayY))]
    private int _y;

    /// <summary>Размер шрифта в пикселях (в исходном масштабе шаблона).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectiveFontSize))]
    [NotifyPropertyChangedFor(nameof(AlignmentOffsetX))]
    [NotifyPropertyChangedFor(nameof(FinalDisplayX))]
    private int _fontSize;

    /// <summary>Имя семейства шрифта.</summary>
    [ObservableProperty]
    private string _fontFamily;

    /// <summary>Выравнивание текста.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AlignmentOffsetX))]
    [NotifyPropertyChangedFor(nameof(FinalDisplayX))]
    private FieldAlignment _alignment;

    /// <summary>
    /// Масштаб отображения шаблона. Влияет на DisplayX/DisplayY/EffectiveFontSize.
    /// Синхронизируется из TemplateViewModel.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayX))]
    [NotifyPropertyChangedFor(nameof(DisplayY))]
    [NotifyPropertyChangedFor(nameof(EffectiveFontSize))]
    [NotifyPropertyChangedFor(nameof(AlignmentOffsetX))]
    [NotifyPropertyChangedFor(nameof(FinalDisplayX))]
    private double _zoomLevel = 1.0;

    /// <summary>
    /// Ширина шаблона в пикселях исходного изображения.
    /// Нужна для вычисления позиции при IsCenteredHorizontally.
    /// Синхронизируется из TemplateViewModel.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectiveX))]
    [NotifyPropertyChangedFor(nameof(DisplayX))]
    [NotifyPropertyChangedFor(nameof(FinalDisplayX))]
    private int _templateWidth;

    /// <summary>
    /// Признак «по центру шаблона по горизонтали».
    /// Если true — X игнорируется.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectiveX))]
    [NotifyPropertyChangedFor(nameof(DisplayX))]
    [NotifyPropertyChangedFor(nameof(FinalDisplayX))]
    private bool _isCenteredHorizontally;

    /// <summary>
    /// Выбрано ли это размещение. Управляет видом рамки.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Текст для отображения на шаблоне.
    /// Устанавливается из TemplateViewModel.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AlignmentOffsetX))]
    [NotifyPropertyChangedFor(nameof(FinalDisplayX))]
    private string _displayText = string.Empty;

    /// <summary>
    /// Фактическая координата X: если «по центру» — половина ширины шаблона,
    /// иначе — X.
    /// </summary>
    public int EffectiveX => IsCenteredHorizontally ? TemplateWidth / 2 : X;

    /// <summary>
    /// Координата X для отображения (с учётом масштаба).
    /// Это точка якоря: для Center — центр текста, для Right — правый край.
    /// </summary>
    public double DisplayX => EffectiveX * ZoomLevel;

    /// <summary>
    /// Координата Y для отображения (с учётом масштаба).
    /// </summary>
    public double DisplayY => Y * ZoomLevel;

    /// <summary>
    /// Размер шрифта с учётом масштаба — для отображения на шаблоне.
    /// </summary>
    public double EffectiveFontSize => FontSize * ZoomLevel;

    /// <summary>
    /// Горизонтальное смещение текста относительно точки якоря.
    /// Для Center — половина ширины влево, для Right — вся ширина влево.
    /// </summary>
    public double AlignmentOffsetX => Alignment switch
    {
        FieldAlignment.Center => -ApproximateTextWidth / 2,
        FieldAlignment.Right => -ApproximateTextWidth,
        _ => 0
    };

    /// <summary>
    /// Итоговая позиция X: точка якоря плюс смещение под выравнивание.
    /// Используется как Canvas.Left.
    /// </summary>
    public double FinalDisplayX => DisplayX + AlignmentOffsetX;

    /// <summary>
    /// Приблизительная ширина текста (в отображаемых пикселях).
    /// Грубая оценка — для позиционирования в UI.
    /// Точное позиционирование — при финальном рендере.
    /// </summary>
    private double ApproximateTextWidth =>
        DisplayText.Length * EffectiveFontSize * 0.55;

    /// <summary>
    /// Событие: любое свойство размещения изменилось.
    /// Используется для автосохранения.
    /// </summary>
    public event EventHandler? Changed;

    public FieldPlacementViewModel(FieldPlacement model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));

        _x = model.X;
        _y = model.Y;
        _fontSize = model.FontSize;
        _fontFamily = model.FontFamily;
        _alignment = model.Alignment;
        _isCenteredHorizontally = model.IsCenteredHorizontally;
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

    partial void OnIsCenteredHorizontallyChanged(bool value)
    {
        _model = _model.WithCenteredHorizontally(value);
        RaiseChanged();
    }

    private void RaiseChanged()
    {
        if (_isDragging)
        {
            return;  // Во время drag — не уведомляем.
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Начать перетаскивание. Changed подавляется до EndDrag.
    /// </summary>
    public void BeginDrag()
    {
        _isDragging = true;
    }

    /// <summary>
    /// Завершить перетаскивание. Вызывает Changed один раз
    /// (если были изменения).
    /// </summary>
    public void EndDrag()
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        RaiseChanged();
    }

    /// <summary>
    /// Отменить перетаскивание. Changed не вызывается —
    /// координаты должны быть возвращены вызывающим кодом.
    /// </summary>
    public void CancelDrag()
    {
        _isDragging = false;
    }
}