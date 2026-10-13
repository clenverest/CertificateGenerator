namespace CertificateGenerator.Wpf.ViewModels;

/// <summary>
/// ViewModel одной строки в таблице проверки.
/// Хранит индекс строки и ячейки полей.
/// </summary>
public sealed class ValidationRowViewModel
{
    /// <summary>
    /// Индекс строки в исходной таблице. Начинается с 0.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// Номер строки для отображения. Начинается с 1.
    /// </summary>
    public int DisplayRowNumber => RowIndex + 1;

    /// <summary>
    /// Ячейки по полям. Порядок соответствует порядку полей проекта.
    /// </summary>
    public IReadOnlyList<ValidationCellViewModel> Cells { get; }

    /// <summary>
    /// Есть ли в строке хоть одна ошибка.
    /// </summary>
    public bool HasError => Cells.Any(c => c.HasError);

    /// <summary>
    /// Есть ли в строке хоть одно предупреждение (без учёта ошибок).
    /// </summary>
    public bool HasWarning =>
        !HasError && Cells.Any(c => c.HasWarning);

    /// <summary>
    /// Создать строку.
    /// </summary>
    public ValidationRowViewModel(
        int rowIndex,
        IReadOnlyList<ValidationCellViewModel> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);

        RowIndex = rowIndex;
        Cells = cells;
    }
}