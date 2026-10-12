using CertificateGenerator.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CertificateGenerator.Wpf.ViewModels;

/// <summary>
/// ViewModel главного экрана.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    /// <summary>
    /// Загруженная таблица. <c>null</c>, если файл ещё не загружен.
    /// </summary>
    [ObservableProperty]
    private TableData? _tableData;

    /// <summary>
    /// Сообщение об ошибке. <c>null</c> или пусто, если ошибок нет.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Признак того, что идёт длительная операция (чтение файла).
    /// </summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// Команда «Открыть файл». Пока ничего не делает —
    /// логика появится на следующих шагах.
    /// </summary>
    [RelayCommand]
    private void OpenFile()
    {
        // TODO: шаг 7 — диалог выбора файла.
        // TODO: шаг 8 — чтение через ITableReaderResolver и установка TableData.
    }
}