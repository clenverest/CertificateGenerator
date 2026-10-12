using System.IO;
using CertificateGenerator.Core.Interfaces;
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
    private readonly ITableReaderResolver _resolver;

    /// <summary>
    /// Создаёт ViewModel с указанным резолвером ридеров.
    /// </summary>
    public MainViewModel(ITableReaderResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

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
    /// Команда «Открыть файл»: открывает диалог выбора и загружает таблицу.
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
        LoadTable(dialog.FileName);
    }

    /// <summary>
    /// Загружает таблицу из указанного файла.
    /// Метод <c>internal</c>, чтобы его можно было тестировать без диалога.
    /// </summary>
    internal void LoadTable(string filePath)
    {
        IsBusy = true;
        ErrorMessage = null;
        TableData = null;

        try
        {
            var reader = _resolver.Resolve(filePath);
            TableData = reader.Read(filePath);
        }
        catch (NotSupportedException ex)
        {
            ErrorMessage = $"Формат файла не поддерживается: {ex.Message}";
        }
        catch (FileNotFoundException)
        {
            ErrorMessage = "Файл не найден. Возможно, он был перемещён или удалён.";
        }
        catch (InvalidDataException ex)
        {
            ErrorMessage = $"Файл повреждён или имеет неверный формат: {ex.Message}";
        }
        catch (IOException ex)
        {
            ErrorMessage = $"Не удалось прочитать файл: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Непредвиденная ошибка: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}