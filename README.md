# CertificateGenerator

Десктопное приложение для автоматической массовой генерации грамот
на основе загружаемой таблицы участников и PNG-шаблона.

## Проблема

Центр по работе со школами проводит мероприятия, собирает данные
участников в разные таблицы (Excel, Google Sheets, Yandex Tables)
и вручную делает сертификаты. Это долго, ошибкоопасно и не
масштабируется.

## Целевая аудитория

Один сотрудник центра, отвечающий за подготовку грамот и сертификатов.
Работает на Windows. Не программист.

## Функциональные требования

1. Создание и открытие проектов (несколько, состояние сохраняется)
2. Проект — папка с таблицей, шаблоном, настройками и результатами
3. Создание списка полей (ФИО, Класс, Город, Школа + свои)
4. Поле ФИО — склейка из 1–3 колонок таблицы
5. Загрузка таблицы: .xlsx, .csv
6. Сопоставление полей с колонками (выпадающие списки + автоподсказка)
7. Редактирование данных: ячейки, удаление строк, добавление строк
8. Сохранение изменений в проект (исходный Excel не трогается)
9. Отмена изменений (возврат к исходной таблице)
10. Валидация: пустое поле — ошибка, дубликат — предупреждение,
    лишние пробелы — автоисправление
11. Экран проверки: таблица + подсветка + сводка
12. Загрузка PNG-шаблона (один на проект)
13. Настройка координат полей: X, Y, размер, выравнивание, шрифт
14. Статические поля (дата, мероприятие — одинаковые для всех)
15. Превью грамоты с наложенным текстом
16. Генерация одной грамоты для выбранного участника
17. Массовая генерация: прогресс-бар, список результатов, ZIP
18. Имя PDF: «ФИО.pdf», при дубликате «ФИО_2.pdf»
19. «Сохранить как…» для одиночной грамоты

## Нефункциональные требования

- Платформа: Windows
- Без серверов, без базы данных
- Локальное хранение (папка проекта)
- Язык интерфейса: русский
- Стиль: Windows 11 (акцент #0078D4)

## Технологический стек

| Слой | Технология |
|---|---|
| Язык | C# (.NET 8) |
| UI | WPF |
| MVVM | CommunityToolkit.Mvvm |
| Excel | ClosedXML |
| CSV | CsvHelper |
| PDF | QuestPDF |
| Картинки | SkiaSharp |
| ZIP | System.IO.Compression |
| Хранение | JSON (System.Text.Json) |
| DI | Microsoft.Extensions.DependencyInjection |
| IDE | Visual Studio / VS Code |
| Контроль версий | GitHub |

## Архитектура

Трёхслойная:

- **Core** — модели, интерфейсы, валидация, сервисы. Не зависит от WPF и библиотек.
- **Infrastructure** — реализации (Excel, CSV, PDF, ZIP, хранилище).
- **WPF** — UI (ViewModels, Views).

Принципы:
- Разделение ответственности
- Модульность
- Ослабление связей
- Все зависимости в виде интерфейсов
- Иммутабельность моделей: изменения — через создание нового экземпляра

## Текущий статус

### ✅ Этап 1 завершён: загрузка и просмотр таблицы

Загрузка `.xlsx` / `.csv`, отображение в `DataGrid`,
обработка ошибок, сводка.

### ✅ Этап 2 завершён: проект, поля, валидация, редактирование

**2.0 — каркас.**
- Модели: `Project`, `Field`, `FieldKind`, `ValidationIssue`, `IssueSeverity`.
- Интерфейсы: `IProjectStorage`, `IValidator`.
- Тесты на модели.

**2.1 — хранилище проекта.**
- `JsonProjectStorage`: `Create` / `Load` / `Save` / `Exists` / `SetSourceTable` / `GetSourceTablePath`.
- Сериализация `System.Text.Json` (WriteIndented, UnsafeRelaxedJsonEscaping, CamelCase).
- Тесты с `TempFolder`.

**2.2 — стартовый экран, DI, диалоги.**
- DI-контейнер в `App.xaml.cs`.
- `RecentProjectsStore` — список последних проектов в `%APPDATA%`.
- `ProjectViewModel` — создание / открытие проекта, список последних.
- `StartupWindow` — стартовый экран.
- `CreateProjectDialog` — свой диалог создания.
- `IFolderPicker`, `ICreateProjectDialogService` — абстракции.

**2.3 — загрузка таблицы в проект.**
- `IWindowNavigator` — навигация между `StartupWindow` и `MainWindow`.
- `MainViewModel` — работа с проектом и таблицей.
- Загрузка таблицы **в проект**: копирование в `source/`, обновление `project.json`.
- Автозагрузка таблицы из `source/` при открытии проекта.
- `MainWindow` — шапка проекта, кнопка «Загрузить таблицу в проект».

**2.4 — модель `Field` и создание полей.**
- `Field`: `Name`, `Kind` (`Simple` / `FullName` / `Static`), `SourceColumnIndexes`, `StaticValue`, `IsRequired`, `Separator`.
- `Field.ValidateName` — проверка имени (пустое / дубликат).
- `FieldsViewModel` — список полей, команды add/edit/delete, автосохранение в `project.json`.
- `FieldEditDialog` — диалог создания / редактирования (имя, тип, значение для статика, чекбокс «Обязательное поле»).
- `IFieldEditDialogService` + реализация.
- Панель полей в `MainWindow` (справа), превью значений для первого участника.

**2.5 — сопоставление полей с колонками.**
- `ColumnSuggester` — автоподсказка колонок по имени поля.
  - `SuggestSimple(fieldName, columns)` — одна колонка по имени поля.
  - `SuggestFullName(columns)` — либо одна колонка «ФИО», либо три: фамилия, имя, отчество.
- `FieldEditDialog` — выпадающие списки колонок (`Simple` — один, `FullName` — три).
- Блокировка «Добавить поле», если таблица не загружена.

**2.6 — склейка ФИО.**
- `FieldValueResolver` — вычисление значения поля для строки:
  - `Simple` — одна ячейка (с `Trim`).
  - `FullName` — склейка непустых ячеек через `Separator`.
  - `Static` — `StaticValue`.
- Превью значений полей для первого участника в панели полей.

**2.7 — валидация.**
- `RequiredFieldValidator` — обязательные поля (Error).
- `DuplicateValidator` — дубликаты (Warning), регистронезависимо, `ё→е`, пропуск пустых и статических.
- `WhitespaceValidator` — лишние пробелы по краям (Warning), пропуск статических.
- `ProjectValidator` — агрегатор с `CreateDefault()`.
- Регистрация `IValidator` в DI (пока не используется в UI).

**2.8 — редактирование ячеек.**
- `CellEditViewModel` — ячейка (`Value`, `OriginalValue`, `IsDirty`, `Reset`, событие `ValueChanged`).
- `RowEditViewModel` — строка (`Cells`, `IsDirty`, `Reset`, событие `ValueChanged`).
- `MainViewModel.Rows` — `ObservableCollection<RowEditViewModel>`, `RebuildRows`.
- `DataGrid` с `IsReadOnly="False"`, `SelectionUnit="Cell"`.
- Правки хранятся в VM, `TableData` остаётся иммутабельной.

**2.9 — экран проверки.**
- `MainViewModel.ValidationIssues` + `RunValidation`.
- Подписки на `ValueChanged` строк → пересчёт валидации.
- `ValidationCellViewModel` (`FieldName`, `Value`, `HasError`, `HasWarning`, `IsEmpty`).
- `ValidationRowViewModel` (`RowIndex`, `DisplayRowNumber`, `Cells`, `HasError`, `HasWarning`).
- `MainViewModel.ValidationRows` + `ValidationSummary` (`ErrorCount`, `WarningCount`, `HasIssues`).
- `BuildIssueIndex` — быстрый поиск проблем по `(RowIndex, FieldName)`.
- `TabControl` в `MainWindow`: вкладка «Данные» + вкладка «Проверка».
- Подсветка ячеек (`CellStyle` + `DataTrigger`) — красный / жёлтый фон.
- **Важно:** валидация использует актуальные значения из `Rows` (не из `TableData`), т.к. `TableData` не отражает правки пользователя.

**2.10 — сохранение / отмена.**
- `RowOverride` — модель правки строки (`RowIndex`, `Values`).
- `Project.RowOverrides` — `IReadOnlyList<RowOverride>` (опциональный параметр конструктора).
- `RowOverrideApplier.Apply(source, overrides)` — наложение правок на `TableData`.
- `MainViewModel` применяет overrides при загрузке таблицы.
- `MainViewModel.SaveRowOverride` — сохранение / удаление override при правке ячейки.
- `ResetChangesCommand` — очистка overrides + перезагрузка таблицы.
- Кнопка «Отменить изменения» в `MainWindow` (серая, если правок нет).
- Исходный Excel в `source/` не трогается.

**2.11 — приёмка Этапа 2.** Прогнаны сценарии, всё работает сквозным образом.

### ✅ Этап 3 завершён: шаблон и настройка полей

**3.0 — каркас моделей и интерфейсов для шаблона.**
- Модели: `TemplateInfo`, `FieldPlacement`, `FieldAlignment`.
- `Project.Template`, `Project.Placements`.
- Интерфейсы: `ITemplateStorage`, `ITemplateRenderer`.

**3.1 — хранилище шаблона.**
- `FileTemplateStorage`: копирует PNG в `template/template.png`, читает размеры через SkiaSharp.
- Пакет `SkiaSharp` добавлен в `Infrastructure.csproj`.

**3.2 — рендер превью.**
- `SkiaTemplateRenderer` — реализация `ITemplateRenderer`.
- Рисует текст на PNG с учётом выравнивания, шрифта, размера.
- Обрезка по границам шаблона (`ClipRect`).

**3.3 — вкладка «Шаблон» и загрузка PNG.**
- `TemplateViewModel` — загрузка PNG, хранение `TemplateInfo`, превью.
- `WpfFilePicker` — реализация `IFilePicker`.
- Третья вкладка «Шаблон» в `MainWindow`.
- Конвертер `ByteArrayToBitmapImageConverter`.
- Метод `SetTemplate` в `IProjectStorage` / `JsonProjectStorage`.
- Зум шаблона (10%–400%, шаг 25%, слайдер + кнопки).
- Панель зума с кнопками «+/−», слайдером, «100%», процентом.

**3.4 — расстановка полей.**
- `FieldRowViewModel` — строка списка полей на вкладке «Шаблон».
- `FieldPlacementViewModel` — VM одного размещения.
- `TemplateViewModel.Placements`, `FieldRows`, `SelectedPlacement`, `SelectedFieldRow`.
- Методы `SyncFields`, `AddPlacementForField`, `RemovePlacement`, `HasPlacement`, `GetPlacement`, `GetPlacementsSnapshot`.
- Событие `PlacementsChanged` → сохранение в `project.json`.
- Список полей слева (галочка / точка), панель настройки под шаблоном.
- Маркеры полей на шаблоне (эллипсы с подписью), drag-and-drop маркеров.
- Проброс `ZoomLevel` и `TemplateWidth` в placements.

**3.5 — drag-and-drop маркеров.**
- `PreviewMouseLeftButtonDown` на маркере → старт drag.
- `PreviewMouseMove` и `PreviewMouseLeftButtonUp` на окне.
- Порог `SystemParameters.MinimumHorizontalDragDistance` — отличает клик от drag.
- Escape — отмена drag (возврат координат).
- `CaptureMouse` — чтобы MouseUp ловился за пределами кнопки.

**3.6 — превью грамоты с текстом.**
- `TemplateViewModel.RenderedPreview` — PNG-байты готового превью.
- `RenderPreviewNow` + debounce (200 мс) через `DispatcherTimer`.
- `SetFieldValues` — значения от первого участника из `MainViewModel.UpdateFieldPreviews`.
- Fallback: если значения нет — подставляется имя поля.
- Третья колонка с превью (позже убрана — см. 3.6-fix.1).

**3.6-fix.1 — WYSIWYG: текст прямо на шаблоне.**
- Замена маркеров-эллипсов на **реальный текст** прямо на шаблоне.
- `FieldPlacementViewModel.DisplayText`, `EffectiveFontSize`, `AlignmentOffsetX`, `FinalDisplayX`.
- Тонкая синяя рамка вокруг выбранного поля.
- Превью справа **убрано** — редактор сам всё показывает.
- Выравнивание: `Center` / `Right` — сдвиг через `Canvas.Left` = `FinalDisplayX` (а не `RenderTransform`).

**3.6-fix.2 — флаг «По центру шаблона».**
- `FieldPlacement.IsCenteredHorizontally` — bool в модели.
- `WithCenteredHorizontally` — иммутабельное обновление.
- `FieldPlacementViewModel.EffectiveX`, `TemplateWidth`.
- Чекбокс «По центру» рядом с X.
- X-TextBox блокируется, если флаг установлен.
- При drag с флагом — маркер двигается **только по вертикали**.
- `InverseBoolConverter` — новый конвертер.
- Панель зума перенесена к кнопкам «Загрузить / Заменить шаблон» (компактнее).

**3.6-fix.3 — устранение лагов при drag.**
- `FieldPlacementViewModel.BeginDrag` / `EndDrag` / `CancelDrag`.
- Пока идёт drag — `Changed` **не** вызывается, `Save` **не** срабатывает.
- При отпускании — один `Changed` → один `Save`.
- Escape во время drag — `CancelDrag`, координаты возвращаются без сохранения.
- `MainViewModel.OnPlacementsChanged` — убран временный замер `Console.WriteLine`.

### ⏳ Этап 4. Генерация одной грамоты (следующий)

**Что делаем:**
1. Выбрать участника из таблицы (вкладка «Данные» или новая вкладка «Генерация»).
2. Сгенерировать PDF грамоты с данными этого участника.
3. Кнопка «Сохранить как…» — сохранить PDF куда угодно.
4. Имя файла по умолчанию: `«ФИО».pdf`.

**Технически:**
- `IPdfGenerator` в `Core/Interfaces`.
- `QuestPdfGenerator` в `Infrastructure/Generators`.
- Рендер PNG (через `ITemplateRenderer`) → вставка в PDF (QuestPDF).
- `GenerateSingleCertificateCommand` в `MainViewModel` или отдельный `GenerationViewModel`.
- Диалог «Сохранить как» — `IFilePicker` с фильтром `*.pdf`.

**Тест:** получил один корректный PDF.

### ⏳ Этап 5. Массовая генерация и ZIP

Прогресс-бар. Список результатов. ZIP в папке `output/`.

**Тест:** получил ZIP с 10 грамотами.

## Структура проекта
CertificateGenerator/
├── CertificateGenerator.sln
├── README.md
├── .gitignore
├── CertificateGenerator.Core/
│ ├── Models/
│ │ ├── ColumnInfo.cs ✅
│ │ ├── TableData.cs ✅
│ │ ├── Project.cs ✅
│ │ ├── Field.cs ✅
│ │ ├── FieldKind.cs ✅
│ │ ├── RowOverride.cs ✅
│ │ ├── ValidationIssue.cs ✅
│ │ ├── IssueSeverity.cs ✅
│ │ ├── FieldPreviewItem.cs ✅
│ │ ├── RecentProject.cs ✅
│ │ ├── CreateProjectResult.cs ✅
│ │ ├── TemplateInfo.cs ✅
│ │ ├── FieldPlacement.cs ✅
│ │ └── FieldAlignment.cs ✅
│ ├── Interfaces/
│ │ ├── ITableReader.cs ✅
│ │ ├── ITableReaderResolver.cs ✅
│ │ ├── IProjectStorage.cs ✅
│ │ ├── IValidator.cs ✅
│ │ ├── IRecentProjectsStore.cs ✅
│ │ ├── IFolderPicker.cs ✅
│ │ ├── IFilePicker.cs ✅
│ │ ├── ICreateProjectDialogService.cs ✅
│ │ ├── IFieldEditDialogService.cs ✅
│ │ ├── IWindowNavigator.cs ✅
│ │ ├── ITemplateStorage.cs ✅
│ │ ├── ITemplateRenderer.cs ✅
│ │ └── IPdfGenerator.cs ⏳ Этап 4
│ ├── Services/
│ │ ├── ColumnSuggester.cs ✅
│ │ ├── FieldValueResolver.cs ✅
│ │ └── RowOverrideApplier.cs ✅
│ └── Validators/
│ ├── RequiredFieldValidator.cs ✅
│ ├── DuplicateValidator.cs ✅
│ ├── WhitespaceValidator.cs ✅
│ └── ProjectValidator.cs ✅
├── CertificateGenerator.Infrastructure/
│ ├── Readers/
│ │ ├── ClosedXmlTableReader.cs ✅
│ │ ├── CsvTableReader.cs ✅
│ │ └── TableReaderResolver.cs ✅
│ ├── Storage/
│ │ ├── JsonProjectStorage.cs ✅
│ │ ├── JsonRecentProjectsStore.cs ✅
│ │ └── FileTemplateStorage.cs ✅
│ ├── Rendering/
│ │ └── SkiaTemplateRenderer.cs ✅
│ ├── Generators/ ⏳ Этапы 4–5
│ │ └── QuestPdfGenerator.cs ⏳ Этап 4
│ └── ZipPacker.cs ⏳ Этап 5
├── CertificateGenerator.Wpf/
│ ├── Converters/
│ │ ├── ByteArrayToBitmapImageConverter.cs ✅
│ │ └── InverseBoolConverter.cs ✅
│ ├── Services/
│ │ ├── WpfFolderPicker.cs ✅
│ │ ├── WpfFilePicker.cs ✅
│ │ ├── CreateProjectDialogService.cs ✅
│ │ ├── FieldEditDialogService.cs ✅
│ │ └── WpfWindowNavigator.cs ✅
│ ├── Views/
│ │ ├── CreateProjectDialog.xaml + .cs ✅
│ │ └── FieldEditDialog.xaml + .cs ✅
│ ├── ViewModels/
│ │ ├── MainViewModel.cs ✅
│ │ ├── ProjectViewModel.cs ✅
│ │ ├── FieldsViewModel.cs ✅
│ │ ├── CellEditViewModel.cs ✅
│ │ ├── RowEditViewModel.cs ✅
│ │ ├── ValidationCellViewModel.cs ✅
│ │ ├── ValidationRowViewModel.cs ✅
│ │ ├── TemplateViewModel.cs ✅
│ │ ├── FieldPlacementViewModel.cs ✅
│ │ └── FieldRowViewModel.cs ✅
│ ├── App.xaml ✅ (без StartupUri)
│ ├── App.xaml.cs ✅ (DI)
│ ├── MainWindow.xaml + .cs ✅
│ └── StartupWindow.xaml + .cs ✅
└── CertificateGenerator.Tests/
├── Models/ ✅
├── Fakes/ ✅ (FakeProjectStorage, FakeValidator, FakeTemplateStorage,
│ FakeTemplateRenderer, FakeFilePicker, ...)
├── Readers/ ✅
├── Storage/ ✅
├── Services/ ✅
├── Validators/ ✅
├── Rendering/ ✅
├── Converters/ ✅
└── ViewModels/ ✅


## Ограничения MVP (Future work)

- PDF-шаблоны с плейсхолдерами
- Создание шаблона внутри приложения
- Несколько шаблонов в одном проекте
- Автоматическое уменьшение шрифта
- Точное измерение ширины текста (сейчас приблизительное `0.55 × fontSize`)
- Кнопка «Показать точное превью» (SkiaSharp-рендер в модальном окне)
- Google Sheets / Yandex Tables напрямую
- Отправка по email
- QR-коды, подписи, фото
- Синхронизация между сотрудниками

## Как запустить

1. Установить .NET 8 SDK.
2. Клонировать репозиторий.
3. `dotnet build` — собрать.
4. `dotnet test` — прогнать тесты.
5. `dotnet run --project CertificateGenerator.Wpf` — запустить приложение.

## Репозиторий

https://github.com/clenverest/CertificateGenerator

## Применение «Золотых правил»

Проект разрабатывается по 12 «Золотым правилам vibe coding».
Подробности — в отчёте.

## Контекст для продолжения в новом чате

**Где мы:** Этапы 1, 2, 3 завершены полностью (включая 2.11 и 3.6-fix).
Следующий — **Этап 4: генерация одной грамоты (PDF)**.

**Что важно помнить при продолжении:**

- Архитектура трёхслойная: `Core` → `Infrastructure` → `Wpf`.
  Зависимости направлены внутрь, `Core` ничего не знает о внешних слоях.
- Тесты — только в `CertificateGenerator.Tests`. `Core` без внешних пакетов.
  Тесты ссылаются на `Core`, `Infrastructure` и `Wpf`.
  `TargetFramework` тестов — `net8.0-windows`.
- `InternalsVisibleTo("CertificateGenerator.Tests")` в `Wpf.csproj`
  и `Infrastructure.csproj`.
- Иммутабельность моделей: `TableData`, `ColumnInfo`, `Project`, `Field`,
  `RowOverride`, `ValidationIssue`, `RecentProject`, `CreateProjectResult`,
  `TemplateInfo`, `FieldPlacement` — `sealed`, свойства только `get`,
  изменения через создание нового экземпляра (`WithX`-паттерн).
- DI: `Microsoft.Extensions.DependencyInjection` подключён в `App.xaml.cs`.
  Все зависимости — в `ConfigureServices`. **Новая зависимость в конструкторе
  DI-создаваемого класса требует регистрации.**
- Команды: `CommunityToolkit.Mvvm` — `[ObservableProperty]`,
  `[RelayCommand]`, `[RelayCommand(CanExecute = nameof(...))]`,
  `partial void On...Changed` для пересчёта вычисляемых свойств.
  **`NotifyCanExecuteChanged()` надо вызывать вручную** при изменении
  того, от чего зависит `CanExecute`.
- **Подписки на события — до изменений.**
- **Иммутабельный `TableData` не отражает правки.** Правки живут в
  `CellEditViewModel.Value`. Валидация и превью используют
  `Rows[i].Cells[j].Value`, а не `TableData.Rows[i][j]`.
- **UI-правки фиксируются по Enter/Tab/LostFocus.**
- **`RowOverrides` в `project.json`** — только изменённые строки.
  При возврате к исходному значению — override удаляется.
- **Исходный Excel не трогается.** Все правки — в `project.json`.
- **`FieldPlacement.IsCenteredHorizontally`** — если true, X игнорируется,
  позиция по горизонтали = `Template.Width / 2`. При drag такое поле
  двигается только по вертикали.
- **Drag маркеров: `BeginDrag` / `EndDrag` / `CancelDrag`.** Во время drag
  `Changed` не вызывается — Save не срабатывает. Один `Changed` в конце.
- **WYSIWYG:** текст рисуется прямо на шаблоне через `TextBlock`.
  `EffectiveFontSize = FontSize * ZoomLevel`.
  `FinalDisplayX = EffectiveX * ZoomLevel + AlignmentOffsetX`.
  `AlignmentOffsetX` — **приблизительный** (`0.55 × fontSize` на символ).
- **Zoom:** от 10% до 400%, шаг 25%. Проброс в `FieldPlacementViewModel.ZoomLevel`
  и `TemplateWidth` из `TemplateViewModel`.
- Язык интерфейса: русский. Сообщения об ошибках — на русском.
- Стиль: Windows 11. Акцент `#0078D4`. Ошибки — `#C42B1C`
  на фоне `#FDE7E9`. Предупреждения — `#8A6D00` на `#FFF4CE`.
  Успех `#107C10` на `#E6F4EA`. Вторичный текст `#605E5C`.
- Проект как папка: `project.json`, `source/`, `template/`, `output/`.
  Шаблон хранится как `template/template.png`.
- `RecentProjectsStore` — в `%APPDATA%\CertificateGenerator\recent.json`.
- **Актуальное число тестов — проверяй через `dotnet test`.**
- **Планы на Этап 4:**
  - `IPdfGenerator` в `Core/Interfaces` — метод `GenerateCertificate(...)`.
  - `QuestPdfGenerator` в `Infrastructure/Generators` — QuestPDF.
  - Рендер PNG шаблона с текстом (через `SkiaTemplateRenderer`) → вставка
    в PDF-страницу формата A4.
  - Выбор участника — из таблицы.
  - «Сохранить как…» — через `IFilePicker` с фильтром `*.pdf`.
  - Имя PDF по умолчанию: `«ФИО».pdf`, при дубликате — `«ФИО_2.pdf»`.