using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CertificateGenerator.Core.Models;
using CertificateGenerator.Wpf.ViewModels;

namespace CertificateGenerator.Wpf;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

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

        DataContext = _viewModel;
    }

    /// <summary>
    /// Пара «значение — подпись» для ComboBox выравнивания.
    /// </summary>
    public sealed record AlignmentOption(FieldAlignment Value, string Label);

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

        // Колонка «№».
        var numberColumn = new DataGridTextColumn
        {
            Header = "№",
            Binding = new Binding(nameof(ValidationRowViewModel.DisplayRowNumber)),
            Width = 50,
            IsReadOnly = true,
        };
        ValidationGrid.Columns.Add(numberColumn);

        // Колонки полей.
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

    private void OnPlacementMarkerClick(object sender, RoutedEventArgs e)
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
    }
}