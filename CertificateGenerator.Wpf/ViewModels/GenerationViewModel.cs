using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CertificateGenerator.Core.Interfaces;
using CertificateGenerator.Core.Models;
using CertificateGenerator.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CertificateGenerator.Wpf.ViewModels;

/// <summary>
/// ViewModel вкладки «Генерация»: управляет массовой генерацией грамот.
/// </summary>
public sealed partial class GenerationViewModel : ObservableObject
{
    private readonly CertificateBatchGenerator _batchGenerator;
    private readonly IMessageBoxService _messageBox;

    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private int _processed;

    [ObservableProperty]
    private int _total;

    [ObservableProperty]
    private string _currentName = string.Empty;

    [ObservableProperty]
    private string? _zipPath;

    [ObservableProperty]
    private bool _wasCancelled;

    [ObservableProperty]
    private int _successCount;

    [ObservableProperty]
    private int _skippedCount;

    [ObservableProperty]
    private int _failedCount;

    public ObservableCollection<GenerationItemViewModel> Items { get; } = new();

    public bool HasResults => Items.Count > 0;

    public bool HasZip => !string.IsNullOrWhiteSpace(ZipPath);

    public double ProgressFraction => Total > 0 ? (double)Processed / Total : 0;

    public string ProgressText =>
        Total == 0
            ? "Готов к запуску"
            : $"Обработано: {Processed} из {Total}";

    public string SummaryText
    {
        get
        {
            if (Items.Count == 0)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            if (SuccessCount > 0) parts.Add($"Успешно: {SuccessCount}");
            if (SkippedCount > 0) parts.Add($"Пропущено: {SkippedCount}");
            if (FailedCount > 0) parts.Add($"Ошибок: {FailedCount}");

            var suffix = WasCancelled ? " (отменено)" : string.Empty;

            return string.Join(" · ", parts) + suffix;
        }
    }

    public GenerationViewModel(
        CertificateBatchGenerator batchGenerator,
        IMessageBoxService messageBox)
    {
        _batchGenerator = batchGenerator
            ?? throw new ArgumentNullException(nameof(batchGenerator));
        _messageBox = messageBox
            ?? throw new ArgumentNullException(nameof(messageBox));
    }

    /// <summary>
    /// Запустить массовую генерацию. Вызывается из MainViewModel.
    /// </summary>
    public async Task RunAsync(
        string templatePath,
        IReadOnlyList<FieldPlacement> placements,
        IReadOnlyList<Field> fields,
        IReadOnlyList<IReadOnlyList<string>> rows,
        string zipPath)
    {
        if (IsRunning)
        {
            return;
        }

        Reset();

        IsRunning = true;
        Total = rows.Count;

        _cts = new CancellationTokenSource();

        var progress = new Progress<BatchProgress>(OnProgress);

        BatchReport report;
        try
        {
            report = await Task.Run(() =>
                _batchGenerator.GenerateAll(
                    templatePath,
                    placements,
                    fields,
                    rows,
                    zipPath,
                    progress,
                    _cts.Token));
        }
        catch (Exception ex)
        {
            IsRunning = false;
            _messageBox.ShowError(
                $"Ошибка массовой генерации:\n\n{ex.Message}",
                "Массовая генерация");
            return;
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }

        IsRunning = false;
        WasCancelled = report.WasCancelled;
        ZipPath = report.ZipPath;
        SuccessCount = report.SuccessCount;
        SkippedCount = report.SkippedCount;
        FailedCount = report.FailedCount;

        foreach (var item in report.Items)
        {
            Items.Add(new GenerationItemViewModel(item));
        }

        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(HasZip));
        OnPropertyChanged(nameof(SummaryText));
    }

    /// <summary>
    /// Сбросить состояние к «до запуска».
    /// </summary>
    private void Reset()
    {
        Items.Clear();
        Processed = 0;
        Total = 0;
        CurrentName = string.Empty;
        ZipPath = null;
        WasCancelled = false;
        SuccessCount = 0;
        SkippedCount = 0;
        FailedCount = 0;

        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(HasZip));
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(ProgressFraction));
        OnPropertyChanged(nameof(ProgressText));
    }

    private void OnProgress(BatchProgress p)
    {
        Processed = p.Processed;
        Total = p.Total;
        CurrentName = p.CurrentName;

        OnPropertyChanged(nameof(ProgressFraction));
        OnPropertyChanged(nameof(ProgressText));
    }

    /// <summary>
    /// Отменить текущую генерацию.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    private bool CanCancel() => IsRunning;

    /// <summary>
    /// Открыть папку с ZIP-архивом в проводнике.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOpenFolder))]
    private void OpenFolder()
    {
        if (string.IsNullOrWhiteSpace(ZipPath))
        {
            return;
        }

        var folder = Path.GetDirectoryName(ZipPath);
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _messageBox.ShowError(
                $"Не удалось открыть папку:\n\n{ex.Message}",
                "Массовая генерация");
        }
    }

    private bool CanOpenFolder()
    {
        return HasZip;
    }

    // Реакции на изменение свойств.

    partial void OnIsRunningChanged(bool value)
    {
        CancelCommand.NotifyCanExecuteChanged();
    }

    partial void OnZipPathChanged(string? value)
    {
        OnPropertyChanged(nameof(HasZip));
        OpenFolderCommand.NotifyCanExecuteChanged();
    }

    partial void OnProcessedChanged(int value)
    {
        OnPropertyChanged(nameof(ProgressFraction));
        OnPropertyChanged(nameof(ProgressText));
    }

    partial void OnTotalChanged(int value)
    {
        OnPropertyChanged(nameof(ProgressFraction));
        OnPropertyChanged(nameof(ProgressText));
    }
}