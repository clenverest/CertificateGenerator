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
        var progress = new Progress<BatchProgress>(p =>
        {
            lock (progressCalls) { progressCalls.Add(p); }
        });

        gen.GenerateAll(
            "template.png",
            new[] { MakePlacement() },
            new[] { MakeNameField() },
            rows,
            @"C:\out\gramoty.zip",
            progress);

        // Progress<T> может вызывать асинхронно — но с TestSynchronizationContext
        // (или без него) обычно выполняет синхронно. Дадим чуть-чуть времени.
        Thread.Sleep(50);

        lock (progressCalls)
        {
            // 2 вызова в цикле + 1 финальный = 3.
            Assert.True(progressCalls.Count >= 3);
            Assert.Equal(2, progressCalls[0].Processed);
            Assert.Equal(2, progressCalls[0].Total);
            Assert.Equal("Иванов", progressCalls[0].CurrentName);
        }
    }

    [Fact]
    public void GenerateAll_Cancelled_StopsAndReports()
    {
        var pdfGenerator = new FakePdfGenerator();
        var zipPacker = new FakeZipPacker();
        var gen = new CertificateBatchGenerator(pdfGenerator, zipPacker);

        // Много строк, чтобы отменить после первой.
        var rows = Enumerable.Range(0, 100)
            .Select(i => (IReadOnlyList<string>)new[] { $"Участник {i}" })
            .ToArray();

        var cts = new CancellationTokenSource();
        var progress = new Progress<BatchProgress>(p =>
        {
            // Отменяем после 3 обработанных.
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
        Assert.True(report.TotalCount < 100);
        Assert.NotNull(report.ZipPath);  // ZIP всё же создан — то, что успели.
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
}