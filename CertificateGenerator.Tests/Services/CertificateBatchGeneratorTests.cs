using CertificateGenerator.Core.Models;
using CertificateGenerator.Core.Services;
using CertificateGenerator.Tests.Fakes;

namespace CertificateGenerator.Tests.Services;

public sealed class CertificateBatchGeneratorTests
{
    private static Field MakeNameField()
    {
        return new Field("ФИО", FieldKind.FullName, new[] { 0 });
    }

    private static FieldPlacement MakePlacement()
    {
        return new FieldPlacement("ФИО", 50, 50, 24, "Arial", FieldAlignment.Center);
    }

    [Fact]
    public void Constructor_NullGenerator_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CertificateBatchGenerator(null!, new FakeZipPacker()));
    }

    [Fact]
    public void Constructor_NullZipPacker_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CertificateBatchGenerator(new FakePdfGenerator(), null!));
    }

    [Fact]
    public void GenerateAll_EmptyRows_ReturnsEmptyReport()
    {
        var gen = new CertificateBatchGenerator(
            new FakePdfGenerator(),
            new FakeZipPacker());

        var report = gen.GenerateAll(
            "template.png",
            new[] { MakePlacement() },
            new[] { MakeNameField() },
            Array.Empty<IReadOnlyList<string>>(),
            @"C:\out\gramoty.zip");

        Assert.Empty(report.Items);
        Assert.Equal(0, report.SuccessCount);
        Assert.Null(report.ZipPath);  // ZIP не создан — нечего писать.
    }

    [Fact]
    public void GenerateAll_TwoRows_CreatesBoth()
    {
        var pdfGenerator = new FakePdfGenerator();
        var zipPacker = new FakeZipPacker();
        var gen = new CertificateBatchGenerator(pdfGenerator, zipPacker);

        var rows = new[]
        {
            (IReadOnlyList<string>)new[] { "Иванов Иван" },
            (IReadOnlyList<string>)new[] { "Петров Пётр" }
        };

        var report = gen.GenerateAll(
            "template.png",
            new[] { MakePlacement() },
            new[] { MakeNameField() },
            rows,
            @"C:\out\gramoty.zip");

        Assert.Equal(2, report.TotalCount);
        Assert.Equal(2, report.SuccessCount);
        Assert.Equal(0, report.SkippedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Equal(2, pdfGenerator.CallCount);
        Assert.Single(zipPacker.Calls);
        Assert.Equal(2, zipPacker.Calls[0].Files.Count);
        Assert.Equal(@"C:\out\gramoty.zip", report.ZipPath);
    }

    [Fact]
    public void GenerateAll_DuplicateNames_UniqueInZip()
    {
        var pdfGenerator = new FakePdfGenerator();
        var zipPacker = new FakeZipPacker();
        var gen = new CertificateBatchGenerator(pdfGenerator, zipPacker);

        var rows = new[]
        {
            (IReadOnlyList<string>)new[] { "Иванов Иван" },
            (IReadOnlyList<string>)new[] { "Иванов Иван" },
            (IReadOnlyList<string>)new[] { "Иванов Иван" }
        };

        gen.GenerateAll(
            "template.png",
            new[] { MakePlacement() },
            new[] { MakeNameField() },
            rows,
            @"C:\out\gramoty.zip");

        var files = zipPacker.Calls[0].Files.Keys.ToList();
        Assert.Equal(3, files.Count);
        Assert.Contains("Иванов Иван.pdf", files);
        Assert.Contains("Иванов Иван_2.pdf", files);
        Assert.Contains("Иванов Иван_3.pdf", files);
    }

    [Fact]
    public void GenerateAll_EmptyName_Skipped()
    {
        var pdfGenerator = new FakePdfGenerator();
        var zipPacker = new FakeZipPacker();
        var gen = new CertificateBatchGenerator(pdfGenerator, zipPacker);

        var rows = new[]
        {
            (IReadOnlyList<string>)new[] { "Иванов Иван" },
            (IReadOnlyList<string>)new[] { "   " }  // пустое
        };

        var report = gen.GenerateAll(
            "template.png",
            new[] { MakePlacement() },
            new[] { MakeNameField() },
            rows,
            @"C:\out\gramoty.zip");

        Assert.Equal(2, report.TotalCount);
        Assert.Equal(1, report.SuccessCount);
        Assert.Equal(1, report.SkippedCount);
        Assert.Equal(1, pdfGenerator.CallCount);  // генератор вызван 1 раз.
    }

    [Fact]
    public void GenerateAll_GeneratorThrows_FailedItem()
    {
        var pdfGenerator = new FakePdfGenerator
        {
            ThrowOnGenerate = new IOException("Тестовая ошибка")
        };
        var zipPacker = new FakeZipPacker();
        var gen = new CertificateBatchGenerator(pdfGenerator, zipPacker);

        var rows = new[]
        {
            (IReadOnlyList<string>)new[] { "Иванов Иван" }
        };

        var report = gen.GenerateAll(
            "template.png",
            new[] { MakePlacement() },
            new[] { MakeNameField() },
            rows,
            @"C:\out\gramoty.zip");

        Assert.Equal(1, report.TotalCount);
        Assert.Equal(0, report.SuccessCount);
        Assert.Equal(1, report.FailedCount);
        Assert.Null(report.ZipPath);  // ZIP не создан — нечего писать.
    }

    [Fact]
    public void GenerateAll_ReportsProgress()
    {
        var pdfGenerator = new FakePdfGenerator();
        var zipPacker = new FakeZipPacker();
        var gen = new CertificateBatchGenerator(pdfGenerator, zipPacker);

        var rows = new[]
        {
            (IReadOnlyList<string>)new[] { "Иванов" },
            (IReadOnlyList<string>)new[] { "Петров" }
        };

        var progressCalls = new List<BatchProgress>();
        var progress = new SyncProgress<BatchProgress>(progressCalls.Add);

        gen.GenerateAll(
            "template.png",
            new[] { MakePlacement() },
            new[] { MakeNameField() },
            rows,
            @"C:\out\gramoty.zip",
            progress);

        // 2 вызова в цикле (перед каждой строкой) + 1 финальный = 3.
        Assert.Equal(3, progressCalls.Count);

        // Первый вызов — перед обработкой строки 0.
        Assert.Equal(0, progressCalls[0].Processed);
        Assert.Equal(2, progressCalls[0].Total);
        Assert.Equal("Иванов", progressCalls[0].CurrentName);

        // Второй вызов — перед обработкой строки 1.
        Assert.Equal(1, progressCalls[1].Processed);
        Assert.Equal("Петров", progressCalls[1].CurrentName);

        // Финальный — 100%.
        Assert.Equal(2, progressCalls[2].Processed);
        Assert.Equal(2, progressCalls[2].Total);
        Assert.Equal(string.Empty, progressCalls[2].CurrentName);
    }

    [Fact]
    public void GenerateAll_Cancelled_StopsAndReports()
    {
        var pdfGenerator = new FakePdfGenerator();
        var zipPacker = new FakeZipPacker();
        var gen = new CertificateBatchGenerator(pdfGenerator, zipPacker);

        // Много строк, чтобы отменить после нескольких.
        var rows = Enumerable.Range(0, 100)
            .Select(i => (IReadOnlyList<string>)new[] { $"Участник {i}" })
            .ToArray();

        var cts = new CancellationTokenSource();
        var progress = new SyncProgress<BatchProgress>(p =>
        {
            // Отменяем, когда обработано 3 участника.
            if (p.Processed >= 3)
            {
                cts.Cancel();
            }
        });

        var report = gen.GenerateAll(
            "template.png",
            new[] { MakePlacement() },
            new[] { MakeNameField() },
            rows,
            @"C:\out\gramoty.zip",
            progress,
            cts.Token);

        Assert.True(report.WasCancelled);

        // Обработано: 0, 1, 2 (три), затем отмена. На 4-й итерации — выход.
        // Значит, результатов примерно 4 (0,1,2 + отмена перед 3-й или 4-й).
        Assert.True(report.TotalCount < 100);
        Assert.True(report.TotalCount >= 3);

        Assert.NotNull(report.ZipPath);  // ZIP создан — то, что успели.
    }

    [Fact]
    public void GenerateAll_EmptyFields_Throws()
    {
        var gen = new CertificateBatchGenerator(
            new FakePdfGenerator(),
            new FakeZipPacker());

        var rows = new[]
        {
            (IReadOnlyList<string>)new[] { "Иванов" }
        };

        Assert.Throws<InvalidOperationException>(() =>
            gen.GenerateAll(
                "template.png",
                new[] { MakePlacement() },
                Array.Empty<Field>(),
                rows,
                @"C:\out\gramoty.zip"));
    }

    /// <summary>
    /// Синхронный IProgress: вызывает колбэк сразу, а не через SynchronizationContext.
    /// Для тестов — чтобы не ждать асинхронного Progress&lt;T&gt;.
    /// </summary>
    private sealed class SyncProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;

        public SyncProgress(Action<T> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public void Report(T value) => _handler(value);
    }
}