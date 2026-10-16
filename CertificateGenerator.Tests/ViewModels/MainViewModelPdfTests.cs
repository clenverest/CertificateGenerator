using CertificateGenerator.Core.Models;
using CertificateGenerator.Core.Services;
using CertificateGenerator.Tests.Fakes;
using CertificateGenerator.Wpf.ViewModels;

namespace CertificateGenerator.Tests.ViewModels;

public sealed class MainViewModelPdfTests
{
    /// <summary>
    /// Путь к «временному» шаблону, который как бы лежит в проекте.
    /// Фейковый IProjectStorage не проверяет его существование.
    /// </summary>
    private const string TemplatePath = @"C:\Projects\A\template\template.png";

    /// <summary>
    /// Создать MainViewModel с готовым проектом:
    /// шаблон + одно поле ФИО + одно размещение + одна строка таблицы.
    /// </summary>
    private static MainViewModel CreateReadyViewModel(
        FakePdfGenerator? pdfGenerator = null,
        FakeFilePicker? filePicker = null,
        FakeMessageBoxService? messageBox = null,
        FakeTemplateStorage? templateStorage = null)
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов Иван" } });

        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));

        var ts = templateStorage ?? new FakeTemplateStorage
        {
            TemplatePathToReturn = TemplatePath
        };
        var fp = filePicker ?? new FakeFilePicker();
        var mb = messageBox ?? new FakeMessageBoxService();

        var pdf = pdfGenerator ?? new FakePdfGenerator();
        var zip = new FakeZipPacker();
        var batchGen = new CertificateBatchGenerator(pdf, zip);
        var generation = new GenerationViewModel(batchGen, mb);

        var vm = new MainViewModel(
            resolver,
            storage,
            new FakeWindowNavigator(),
            new FieldsViewModel(new FakeFieldEditDialogService()),
            new FakeValidator(),
            new TemplateViewModel(ts, fp, new FakeTemplateRenderer()),
            ts,
            pdf,
            fp,
            mb,
            generation);

        // Проект с шаблоном и одним placement.
        var templateInfo = new TemplateInfo("template.png", 100, 100);
        var field = new Field("ФИО", FieldKind.FullName, new[] { 0 });

        var placement = new FieldPlacement(
            "ФИО", 50, 50, 24, "Arial", FieldAlignment.Center);

        var project = new Project(
            "A",
            DateTime.Now,
            "data.xlsx",
            new[] { field },
            rowOverrides: null,
            template: templateInfo,
            placements: new[] { placement });

        vm.Initialize(project, @"C:\Projects\A");

        // Загружаем таблицу.
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        // Выбираем первую строку.
        if (vm.Rows.Count > 0)
        {
            vm.SelectedRow = vm.Rows[0];
        }

        return vm;
    }

    // ---------------------------------------------------------------------
    // CanGeneratePdf
    // ---------------------------------------------------------------------

    [Fact]
    public void CanGeneratePdf_NoTemplate_False()
    {
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "Иванов" } });

        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var ts = new FakeTemplateStorage();
        var fp = new FakeFilePicker();

        var pdfGen = new FakePdfGenerator();
        var zip = new FakeZipPacker();
        var mb = new FakeMessageBoxService();
        var batchGen = new CertificateBatchGenerator(pdfGen, zip);
        var generation = new GenerationViewModel(batchGen, mb);

        var vm = new MainViewModel(
            resolver,
            storage,
            new FakeWindowNavigator(),
            new FieldsViewModel(new FakeFieldEditDialogService()),
            new FakeValidator(),
            new TemplateViewModel(ts, fp, new FakeTemplateRenderer()),
            ts,
            pdfGen,
            fp,
            mb,
            generation);

        // Проект БЕЗ шаблона.
        var project = new Project(
            "A", DateTime.Now, "data.xlsx", Array.Empty<Field>());

        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");

        Assert.False(vm.GeneratePdfCommand.CanExecute(null));
    }

    [Fact]
    public void CanGeneratePdf_NoPlacements_False()
    {
        var vm = CreateReadyViewModel();

        // Убираем все размещения.
        foreach (var placement in vm.Template.Placements.ToList())
        {
            vm.Template.RemovePlacement(placement);
        }

        Assert.False(vm.GeneratePdfCommand.CanExecute(null));
    }

    [Fact]
    public void CanGeneratePdf_NoSelectedRow_False()
    {
        var vm = CreateReadyViewModel();

        vm.SelectedRow = null;

        Assert.False(vm.GeneratePdfCommand.CanExecute(null));
    }

    [Fact]
    public void CanGeneratePdf_AllReady_True()
    {
        var vm = CreateReadyViewModel();

        Assert.True(vm.GeneratePdfCommand.CanExecute(null));
    }

    // ---------------------------------------------------------------------
    // GeneratePdf
    // ---------------------------------------------------------------------

    [Fact]
    public void GeneratePdf_CallsPdfGenerator()
    {
        var pdfGenerator = new FakePdfGenerator();
        var filePicker = new FakeFilePicker
        {
            SaveFileToReturn = null  // пользователь отменил — файл не пишем
        };

        var vm = CreateReadyViewModel(
            pdfGenerator: pdfGenerator,
            filePicker: filePicker);

        vm.GeneratePdfCommand.Execute(null);

        Assert.Equal(1, pdfGenerator.CallCount);
        Assert.Equal(TemplatePath, pdfGenerator.LastTemplatePath);
        Assert.NotNull(pdfGenerator.LastFieldValues);
        Assert.Equal("Иванов Иван", pdfGenerator.LastFieldValues!["ФИО"]);
    }

    [Fact]
    public void GeneratePdf_UserCancelsDialog_NoFileWritten()
    {
        var filePicker = new FakeFilePicker
        {
            SaveFileToReturn = null  // отмена
        };
        var vm = CreateReadyViewModel(filePicker: filePicker);

        vm.GeneratePdfCommand.Execute(null);

        // Диалог вызывался.
        Assert.Single(filePicker.SaveRequests);
        // Но PdfStatusMessage не заполнилось.
        Assert.Null(vm.PdfStatusMessage);
    }

    [Fact]
    public void GeneratePdf_WritesFileToChosenPath()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var filePicker = new FakeFilePicker
            {
                SaveFileToReturn = tempFile
            };
            var pdfGenerator = new FakePdfGenerator
            {
                Result = new byte[] { 0x25, 0x50, 0x44, 0x46 }  // "%PDF"
            };

            var vm = CreateReadyViewModel(
                pdfGenerator: pdfGenerator,
                filePicker: filePicker);

            vm.GeneratePdfCommand.Execute(null);

            Assert.True(File.Exists(tempFile));
            var written = File.ReadAllBytes(tempFile);
            Assert.Equal(pdfGenerator.Result, written);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void GeneratePdf_SetsPdfStatusMessage()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var filePicker = new FakeFilePicker
            {
                SaveFileToReturn = tempFile
            };
            var vm = CreateReadyViewModel(filePicker: filePicker);

            vm.GeneratePdfCommand.Execute(null);

            Assert.NotNull(vm.PdfStatusMessage);
            Assert.Contains(tempFile, vm.PdfStatusMessage!);
            Assert.True(vm.HasPdfStatus);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void GeneratePdf_GeneratorThrows_ShowsError()
    {
        var pdfGenerator = new FakePdfGenerator
        {
            ThrowOnGenerate = new IOException("Тестовая ошибка")
        };
        var messageBox = new FakeMessageBoxService();
        var vm = CreateReadyViewModel(
            pdfGenerator: pdfGenerator,
            messageBox: messageBox);

        vm.GeneratePdfCommand.Execute(null);

        Assert.Single(messageBox.ErrorCalls);
        Assert.Null(vm.PdfStatusMessage);
    }

    [Fact]
    public void GeneratePdf_EmptyFullName_ShowsWarning()
    {
        // Таблица с пустым ФИО.
        var table = new TableData(
            new[] { new ColumnInfo(0, "ФИО") },
            new[] { (IReadOnlyList<string>)new[] { "   " } });

        var resolver = new FakeTableReaderResolver(new FakeTableReader(table));
        var storage = new FakeProjectStorage
        {
            GetSourceTablePathToReturn = @"C:\Projects\A\source\data.xlsx"
        };
        var ts = new FakeTemplateStorage { TemplatePathToReturn = TemplatePath };
        var fp = new FakeFilePicker();
        var mb = new FakeMessageBoxService();
        var pdfGenerator = new FakePdfGenerator();

        var zip = new FakeZipPacker();
        var batchGen = new CertificateBatchGenerator(pdfGenerator, zip);
        var generation = new GenerationViewModel(batchGen, mb);

        var vm = new MainViewModel(
            resolver,
            storage,
            new FakeWindowNavigator(),
            new FieldsViewModel(new FakeFieldEditDialogService()),
            new FakeValidator(),
            new TemplateViewModel(ts, fp, new FakeTemplateRenderer()),
            ts,
            pdfGenerator,
            fp,
            mb,
            generation);

        var field = new Field("ФИО", FieldKind.FullName, new[] { 0 });
        var placement = new FieldPlacement(
            "ФИО", 50, 50, 24, "Arial", FieldAlignment.Center);
        var templateInfo = new TemplateInfo("template.png", 100, 100);

        var project = new Project(
            "A", DateTime.Now, "data.xlsx",
            new[] { field },
            rowOverrides: null,
            template: templateInfo,
            placements: new[] { placement });

        vm.Initialize(project, @"C:\Projects\A");
        vm.LoadTableIntoProjectFromPath(@"C:\Temp\data.xlsx");
        vm.SelectedRow = vm.Rows[0];

        vm.GeneratePdfCommand.Execute(null);

        Assert.Single(mb.WarningCalls);
        Assert.Equal(0, pdfGenerator.CallCount);
    }
}