using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CertificateGenerator.Wpf.ViewModels;

/// <summary>
/// ViewModel одной строки таблицы в режиме редактирования.
/// Содержит набор редактируемых ячеек.
/// </summary>
public sealed class RowEditViewModel : ObservableObject
{
    /// <summary>
    /// Индекс строки в исходной таблице. Начинается с 0.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// Ячейки строки. Порядок соответствует колонкам таблицы.
    /// </summary>
    public ObservableCollection<CellEditViewModel> Cells { get; }

    /// <summary>
    /// Изменена ли хотя бы одна ячейка строки.
    /// </summary>
    public bool IsDirty => Cells.Any(c => c.IsDirty);

    /// <summary>
    /// Создать строку.
    /// </summary>
    /// <param name="rowIndex">Индекс строки в исходной таблице.</param>
    /// <param name="values">Значения ячеек в порядке колонок.</param>
    public RowEditViewModel(int rowIndex, IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        RowIndex = rowIndex;
        Cells = new ObservableCollection<CellEditViewModel>(
            values.Select(v => new CellEditViewModel(v)));

        // Подписываемся на изменения каждой ячейки,
        // чтобы пересчитывать свой IsDirty.
        foreach (var cell in Cells)
        {
            cell.PropertyChanged += OnCellPropertyChanged;
        }
    }

    /// <summary>
    /// Откатить все ячейки строки к исходным значениям.
    /// </summary>
    public void Reset()
    {
        foreach (var cell in Cells)
        {
            cell.Reset();
        }
    }

    private void OnCellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CellEditViewModel.IsDirty))
        {
            OnPropertyChanged(nameof(IsDirty));
        }
    }
}