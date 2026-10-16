### ✅ Этап 4 завершён: генерация одной грамоты в PDF

**4.1 — интерфейс и фейк.**
- `IPdfGenerator` в Core — зеркалит `ITemplateRenderer.RenderPreview`.
- `FakePdfGenerator` в Tests — заглушка для тестов ViewModel.

**4.2 — QuestPDF.**
- Пакет QuestPDF 2026.8.0 в Infrastructure.
- `QuestPdfGenerator` — рендерит PNG через Skia, вставляет в PDF-страницу ровно того же размера (300 DPI).
- Обработка битого PNG через `InvalidDataException`.
- Лицензия `LicenseType.Community` устанавливается в `App.xaml.cs`.
- `QuestPdfGeneratorTests` (7 тестов).

**4.3 — `IFilePicker.PickSaveFile`.**
- Новый метод в `IFilePicker` — диалог сохранения файла.
- Реализация через `SaveFileDialog`.
- `FakeFilePicker.SaveFileToReturn` и `SaveRequests`.

**4.4 — `IMessageBoxService`.**
- Абстракция над `MessageBox` — чтобы ViewModel не зависел от WPF и сценарии с ошибками покрывались тестами.
- `WpfMessageBoxService` — реализация.
- `FakeMessageBoxService` — запоминает вызовы.
- Замена всех `MessageBox.Show` в `MainViewModel` на `_messageBox.ShowWarning/ShowError`.

**4.5 — `GeneratePdfCommand`.**
- `SelectedRow` в `MainViewModel`.
- `GeneratePdfCommand` с `CanExecute`.
- Сбор `fieldValues` через `FieldValueResolver`.
- Поиск поля ФИО (`FullName`, fallback — первое поле).
- `MakeSafeFileName` — замена недопустимых символов.
- Диалог «Сохранить как…» с папкой `output/` по умолчанию.
- Плашка `PdfStatusMessage` в UI.
- `MainViewModelPdfTests` (10 тестов).

**4.6 — UI.**
- `DataGrid.SelectionUnit="FullRow"` — выделение строки, а не ячейки.
- `SelectedItem="{Binding SelectedRow}"`.
- Кнопка **«Сгенерировать PDF»** (зелёная) на вкладке «Данные».
- Плашка результата под кнопками.

**4.7 — фикс центровки.**
- `SkiaTemplateRenderer` учитывает `IsCenteredHorizontally` — поле центрируется по ширине шаблона в PDF, как и в превью.

**4.8 — фикс `SetSourceTable`.**
- `JsonProjectStorage.SetSourceTable` **больше не теряет** `Template` и `Placements` — это был баг: при повторной загрузке таблицы пропадал шаблон.
- `FakeProjectStorage.SetSourceTable` синхронизирован.

## Структура проекта

CertificateGenerator/
├── CertificateGenerator.sln
├── README.md
├── .gitignore
├── CertificateGenerator.Core/
│   ├── Models/
│   │   ├── ColumnInfo.cs ✅
│   │   ├── TableData.cs ✅
│   │   ├── Project.cs ✅
│   │   ├── Field.cs ✅
│   │   ├── FieldKind.cs ✅
│   │   ├── RowOverride.cs ✅
│   │   ├── ValidationIssue.cs ✅
│   │   ├── IssueSeverity.cs ✅
│   │   ├── FieldPreviewItem.cs ✅
│   │   ├── RecentProject.cs ✅
│   │   ├── CreateProjectResult.cs ✅
│   │   ├── TemplateInfo.cs ✅
│   │   ├── FieldPlacement.cs ✅
│   │   ├── FieldAlignment.cs ✅
│   │   ├── BatchItemStatus.cs ✅
│   │   ├── BatchItemResult.cs ✅
│   │   ├── BatchReport.cs ✅
│   │   └── BatchProgress.cs ✅
│   ├── Interfaces/
│   │   ├── ITableReader.cs ✅
│   │   ├── ITableReaderResolver.cs ✅
│   │   ├── IProjectStorage.cs ✅
│   │   ├── IValidator.cs ✅
│   │   ├── IRecentProjectsStore.cs ✅
│   │   ├── IFolderPicker.cs ✅
│   │   ├── IFilePicker.cs ✅
│   │   ├── ICreateProjectDialogService.cs ✅
│   │   ├── IFieldEditDialogService.cs ✅
│   │   ├── IWindowNavigator.cs ✅
│   │   ├── ITemplateStorage.cs ✅
│   │   ├── ITemplateRenderer.cs ✅
│   │   ├── IPdfGenerator.cs ✅
│   │   ├── IMessageBoxService.cs ✅
│   │   └── IZipPacker.cs ✅
│   ├── Services/
│   │   ├── ColumnSuggester.cs ✅
│   │   ├── FieldValueResolver.cs ✅
│   │   ├── RowOverrideApplier.cs ✅
│   │   ├── FileNameUniquifier.cs ✅
│   │   └── CertificateBatchGenerator.cs ✅
│   └── Validators/
│       ├── RequiredFieldValidator.cs ✅
│       ├── DuplicateValidator.cs ✅
│       ├── WhitespaceValidator.cs ✅
│       └── ProjectValidator.cs ✅
├── CertificateGenerator.Infrastructure/
│   ├── Readers/
│   │   ├── ClosedXmlTableReader.cs ✅
│   │   ├── CsvTableReader.cs ✅
│   │   └── TableReaderResolver.cs ✅
│   ├── Storage/
│   │   ├── JsonProjectStorage.cs ✅
│   │   ├── JsonRecentProjectsStore.cs ✅
│   │   └── FileTemplateStorage.cs ✅
│   ├── Rendering/
│   │   └── SkiaTemplateRenderer.cs ✅
│   └── Generators/
│       ├── QuestPdfGenerator.cs ✅
│       └── ZipPacker.cs ✅
├── CertificateGenerator.Wpf/
│   ├── Converters/
│   │   ├── ByteArrayToBitmapImageConverter.cs ✅
│   │   └── InverseBoolConverter.cs ✅
│   ├── Services/
│   │   ├── WpfFolderPicker.cs ✅
│   │   ├── WpfFilePicker.cs ✅
│   │   ├── CreateProjectDialogService.cs ✅
│   │   ├── FieldEditDialogService.cs ✅
│   │   ├── WpfWindowNavigator.cs ✅
│   │   └── WpfMessageBoxService.cs ✅
│   ├── Views/
│   │   ├── CreateProjectDialog.xaml + .cs ✅
│   │   └── FieldEditDialog.xaml + .cs ✅
│   ├── ViewModels/
│   │   ├── MainViewModel.cs ✅
│   │   ├── ProjectViewModel.cs ✅
│   │   ├── FieldsViewModel.cs ✅
│   │   ├── CellEditViewModel.cs ✅
│   │   ├── RowEditViewModel.cs ✅
│   │   ├── ValidationCellViewModel.cs ✅
│   │   ├── ValidationRowViewModel.cs ✅
│   │   ├── TemplateViewModel.cs ✅
│   │   ├── FieldPlacementViewModel.cs ✅
│   │   ├── FieldRowViewModel.cs ✅
│   │   ├── GenerationViewModel.cs ✅
│   │   └── GenerationItemViewModel.cs ✅
│   ├── App.xaml ✅ (без StartupUri)
│   ├── App.xaml.cs ✅ (DI)
│   ├── MainWindow.xaml + .cs ✅
│   └── StartupWindow.xaml + .cs ✅
└── CertificateGenerator.Tests/
    ├── Models/ ✅
    ├── Fakes/ ✅
    ├── Readers/ ✅
    ├── Storage/ ✅
    ├── Services/ ✅
    ├── Validators/ ✅
    ├── Rendering/ ✅
    ├── Converters/ ✅
    ├── Generators/ ✅
    └── ViewModels/ ✅

## Ограничения MVP (Future work)

- PDF-шаблоны с плейсхолдерами
- Создание шаблона внутри приложения
- Несколько шаблонов в одном проекте
- Автоматическое уменьшение шрифта
- **Точное измерение ширины текста** — сейчас в WPF-превью приблизительное (`0.55 × fontSize`), в PDF — точное (Skia)
- **Y-сдвиг** в WPF-превью vs PDF — WPF рисует от top, Skia от baseline; возможен сдвиг на несколько пикселей
- **`AskYesNo` в `IMessageBoxService`** — для подтверждения замены таблицы и отмены изменений
- Кнопка «Показать точное превью» (SkiaSharp-рендер в модальном окне)
- Google Sheets / Yandex Tables напрямую
- Отправка по email
- QR-коды, подписи, фото
- Синхронизация между сотрудниками