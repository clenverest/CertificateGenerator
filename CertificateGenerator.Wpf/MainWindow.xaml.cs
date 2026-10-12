using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CertificateGenerator.Infrastructure.Readers;
using CertificateGenerator.Wpf.ViewModels;

namespace CertificateGenerator.Wpf;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        var resolver = new TableReaderResolver();
        _viewModel = new MainViewModel(resolver);
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        DataContext = _viewModel;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.TableData))
        {
            UpdateGrid();
        }
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
                Binding = new Binding($"[{column.Index}]"),
            };
            ParticipantsGrid.Columns.Add(gridColumn);
        }

        ParticipantsGrid.ItemsSource = table.Rows;
    }
}