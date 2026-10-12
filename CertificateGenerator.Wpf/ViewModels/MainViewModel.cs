using CertificateGenerator.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

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
    /// Путь к выбранному файлу. <c>null</c>, если файл ещё не выбран.
    /// </summary>
    [ObservableProperty]
    private string? _selectedFilePath;

    /// <summary>
    /// Команда «Открыть файл». Пока ничего не делает —
    /// логика появится на следующих шагах.
    /// </summary>
    [RelayCommand]
    private void OpenFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите файл с таблицей",
            Filter = "Excel и CSV (*.xlsx;*.csv)|*.xlsx;*.csv|Все файлы (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        SelectedFilePath = dialog.FileName;
        ErrorMessage = null;
    }
}