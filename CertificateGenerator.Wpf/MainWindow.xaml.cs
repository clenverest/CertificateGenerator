using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CertificateGenerator.Core.Models;
using CertificateGenerator.Wpf.ViewModels;

namespace CertificateGenerator.Wpf;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Реагируем на изменение списка полей — перестраиваем колонки проверки.
        _viewModel.Fields.Fields.CollectionChanged += OnFieldsChanged;

        DataContext = _viewModel;
    }

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
}