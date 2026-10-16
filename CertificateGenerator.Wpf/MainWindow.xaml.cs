using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CertificateGenerator.Core.Models;
using CertificateGenerator.Wpf.ViewModels;

namespace CertificateGenerator.Wpf;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    // ---------------------------------------------------------------------
    // Состояние drag маркеров
    // ---------------------------------------------------------------------

    private FieldPlacementViewModel? _draggedPlacement;
    private Button? _draggedButton;
    private Point _dragStartMouse;
    private int _dragStartX;
    private int _dragStartY;
    private bool _isDragging;

    /// <summary>
    /// Варианты выравнивания для ComboBox. XAML биндится сюда.
    /// </summary>
    public static IReadOnlyList<AlignmentOption> AlignmentOptions { get; } = new[]
    {
        new AlignmentOption(FieldAlignment.Left, "По левому краю"),
        new AlignmentOption(FieldAlignment.Center, "По центру"),
        new AlignmentOption(FieldAlignment.Right, "По правому краю")
    };

    /// <summary>
    /// Доступные системные шрифты для ComboBox.
    /// </summary>
    public static IReadOnlyList<string> AvailableFonts { get; } =
        Fonts.SystemFontFamilies
            .Select(f => f.Source)
            .OrderBy(s => s)
            .ToList();

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Реагируем на изменение списка полей — перестраиваем колонки проверки.
        _viewModel.Fields.Fields.CollectionChanged += OnFieldsChanged;

        // Обработка drag маркеров: движение мыши и отпускание — на окне,
        // чтобы ловить их даже за пределами кнопки-маркера.
        PreviewMouseMove += OnWindowPreviewMouseMove;
        PreviewMouseLeftButtonUp += OnWindowPreviewMouseLeftButtonUp;
        KeyDown += OnWindowKeyDown;

        DataContext = _viewModel;
    }

    /// <summary>
    /// Пара «значение — подпись» для ComboBox выравнивания.
    /// </summary>
    public sealed record AlignmentOption(FieldAlignment Value, string Label);

    // ---------------------------------------------------------------------
    // Обновление таблиц
    // ---------------------------------------------------------------------

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.TableData))
        {
            UpdateGrid();
            UpdateValidationGrid();
        }
    }

    private void OnFieldsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateValidationGrid();
    }

    private void UpdateGrid()
    {
        ParticipantsGrid.Columns.Clear();
        ParticipantsGrid.ItemsSource = null;

        var table = _viewModel.TableData;
        if (table is null)
        {
            return;
        }

        foreach (var column in table.Columns)
        {
            var gridColumn = new DataGridTextColumn
            {
                Header = column.Name,
                Binding = new Binding($"Cells[{column.Index}].Value"),
            };
            ParticipantsGrid.Columns.Add(gridColumn);
        }

        ParticipantsGrid.ItemsSource = _viewModel.Rows;
    }

    private void UpdateValidationGrid()
    {
        ValidationGrid.Columns.Clear();
        ValidationGrid.ItemsSource = null;

        var fields = _viewModel.Fields.Fields;
        if (fields.Count == 0)
        {
            return;
        }

        var numberColumn = new DataGridTextColumn
        {
            Header = "№",
            Binding = new Binding(nameof(ValidationRowViewModel.DisplayRowNumber)),
            Width = 50,
            IsReadOnly = true,
        };
        ValidationGrid.Columns.Add(numberColumn);

        for (var i = 0; i < fields.Count; i++)
        {
            var fieldName = fields[i].Name;
            var gridColumn = new DataGridTextColumn
            {
                Header = fieldName,
                Binding = new Binding($"Cells[{i}].Value"),
                IsReadOnly = true,
            };
            ValidationGrid.Columns.Add(gridColumn);
        }

        ValidationGrid.ItemsSource = _viewModel.ValidationRows;
    }

    // ---------------------------------------------------------------------
    // Кнопки размещения
    // ---------------------------------------------------------------------

    private void OnPlaceFieldClick(object sender, RoutedEventArgs e)
    {
        var row = _viewModel.Template.SelectedFieldRow;
        if (row is null || row.HasPlacement)
        {
            return;
        }

        _viewModel.Template.AddPlacementForField(row.Field);
    }

    private void OnRemovePlacementClick(object sender, RoutedEventArgs e)
    {
        var placement = _viewModel.Template.SelectedPlacement;
        if (placement is null)
        {
            return;
        }

        _viewModel.Template.RemovePlacement(placement);
    }

    // ---------------------------------------------------------------------
    // Drag маркеров
    // ---------------------------------------------------------------------

    private void OnPlacementMarkerPreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (button.DataContext is not FieldPlacementViewModel placement)
        {
            return;
        }

        _viewModel.Template.SelectPlacement(placement);

        _draggedPlacement = placement;
        _draggedButton = button;
        _dragStartMouse = e.GetPosition(TemplateCanvas);
        _dragStartX = placement.X;
        _dragStartY = placement.Y;
        _isDragging = false;

        // Подавляем Changed до завершения drag.
        placement.BeginDrag();

        button.CaptureMouse();
    }

    private void OnWindowPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_draggedPlacement is null || _draggedButton is null)
        {
            return;
        }

        var current = e.GetPosition(TemplateCanvas);
        var deltaX = current.X - _dragStartMouse.X;
        var deltaY = current.Y - _dragStartMouse.Y;

        if (!_isDragging)
        {
            var threshold = SystemParameters.MinimumHorizontalDragDistance;
            if (Math.Abs(deltaX) < threshold && Math.Abs(deltaY) < threshold)
            {
                return;
            }

            _isDragging = true;
        }

        var zoom = _viewModel.Template.ZoomLevel;
        if (zoom <= 0)
        {
            return;
        }

        var newY = _dragStartY + (int)Math.Round(deltaY / zoom);

        // Если поле «по центру» — тянем только по вертикали.
        if (!_draggedPlacement.IsCenteredHorizontally)
        {
            var newX = _dragStartX + (int)Math.Round(deltaX / zoom);
            _draggedPlacement.X = newX;
        }

        _draggedPlacement.Y = newY;
    }

    private void OnWindowPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggedPlacement is null)
        {
            return;
        }

        EndDrag();
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        if (_draggedPlacement is null || !_isDragging)
        {
            return;
        }

        // Отмена: возвращаем исходные координаты.
        _draggedPlacement.X = _dragStartX;
        _draggedPlacement.Y = _dragStartY;

        // Не сохраняем отменённые координаты.
        _draggedPlacement.CancelDrag();

        EndDrag();
        e.Handled = true;
    }

    private void EndDrag()
    {
        if (_draggedButton is not null && _draggedButton.IsMouseCaptured)
        {
            _draggedButton.ReleaseMouseCapture();
        }

        // Завершаем drag: если не отменён — Changed сработает один раз.
        _draggedPlacement?.EndDrag();

        _draggedPlacement = null;
        _draggedButton = null;
        _isDragging = false;
    }
}