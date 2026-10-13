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

### 🔄 Этап 2.11 — приёмка (в работе)

Прогнать сценарии из README, убедиться, что всё работает сквозным образом.
Никакого нового кода — только проверка.

### ⏳ Этап 3. Шаблон и настройка полей

Загрузить PNG. Задать координаты, размер, выравнивание, шрифт.
Добавить статические поля. Увидеть превью.

**Тест:** вижу, как будет выглядеть грамота.

### ⏳ Этап 4. Генерация одной грамоты

Выбрать участника. Сгенерировать PDF. «Сохранить как…»

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
│ │ └── CreateProjectResult.cs ✅
│ ├── Interfaces/
│ │ ├── ITableReader.cs ✅
│ │ ├── ITableReaderResolver.cs ✅
│ │ ├── IProjectStorage.cs ✅
│ │ ├── IValidator.cs ✅
│ │ ├── IRecentProjectsStore.cs ✅
│ │ ├── IFolderPicker.cs ✅
│ │ ├── ICreateProjectDialogService.cs ✅
│ │ ├── IFieldEditDialogService.cs ✅
│ │ └── IWindowNavigator.cs ✅
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
│ │ └── JsonRecentProjectsStore.cs ✅
│ ├── Generators/ ⏳ Этапы 4–5
│ └── ZipPacker.cs ⏳ Этап 5
├── CertificateGenerator.Wpf/
│ ├── Services/
│ │ ├── WpfFolderPicker.cs ✅
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
│ │ └── ValidationRowViewModel.cs ✅
│ ├── App.xaml ✅ (без StartupUri)
│ ├── App.xaml.cs ✅ (DI)
│ ├── MainWindow.xaml + .cs ✅
│ └── StartupWindow.xaml + .cs ✅
└── CertificateGenerator.Tests/
├── Models/ ✅
├── Fakes/ ✅ (FakeProjectStorage, FakeValidator, FakeFieldEditDialogService, ...)
├── Readers/ ✅
├── Storage/ ✅
├── Services/ ✅
├── Validators/ ✅
└── ViewModels/ ✅


## Ограничения MVP (Future work)

- PDF-шаблоны с плейсхолдерами
- Создание шаблона внутри приложения
- Несколько шаблонов в одном проекте
- Drag-and-drop полей на шаблоне
- Автоматическое уменьшение шрифта
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

**Где мы:** Этапы 1 и 2 (подэтапы 2.0–2.10) завершены.
Подэтап 2.11 (приёмка Этапа 2) — в работе. Дальше — Этап 3 (PNG-шаблон).

**Что важно помнить при продолжении:**

- Архитектура трёхслойная: `Core` → `Infrastructure` → `Wpf`.
  Зависимости направлены внутрь, `Core` ничего не знает о внешних слоях.
- Тесты — только в `CertificateGenerator.Tests`. `Core` без внешних пакетов.
  Тесты ссылаются на `Core`, `Infrastructure` и `Wpf`.
  `TargetFramework` тестов — `net8.0-windows`.
- `InternalsVisibleTo("CertificateGenerator.Tests")` в `Wpf.csproj`
  и `Infrastructure.csproj`.
- Иммутабельность моделей: `TableData`, `ColumnInfo`, `Project`, `Field`,
  `RowOverride`, `ValidationIssue`, `RecentProject`, `CreateProjectResult` —
  `sealed`, свойства только `get`, изменения через создание нового экземпляра.
- DI: `Microsoft.Extensions.DependencyInjection` подключён в `App.xaml.cs`.
  Все зависимости — в `ConfigureServices`. **Новая зависимость в конструкторе
  DI-создаваемого класса требует регистрации.**
- Команды: `CommunityToolkit.Mvvm` — `[ObservableProperty]`,
  `[RelayCommand]`, `[RelayCommand(CanExecute = nameof(...))]`,
  `partial void On...Changed` для пересчёта вычисляемых свойств.
  **`NotifyCanExecuteChanged()` надо вызывать вручную** при изменении
  того, от чего зависит `CanExecute`.
- **Подписки на события — до изменений.** См. `WpfWindowNavigator` и
  `RebuildRows` (подписка на `RowEditViewModel.ValueChanged` — в момент
  создания строки, **до** `Rows.Add`).
- **Иммутабельный `TableData` не отражает правки.** Правки живут в
  `CellEditViewModel.Value`. **Валидация и превью должны использовать
  `Rows[i].Cells[j].Value`, а не `TableData.Rows[i][j]`.**
  См. `BuildCurrentTable` в `MainViewModel`.
- **UI-правки фиксируются по Enter/Tab/LostFocus**, а не на каждое
  нажатие клавиши. Значит, `ValueChanged` срабатывает умеренно часто.
- **`RowOverrides` в `project.json`** — только изменённые строки.
  При возврате к исходному значению — override удаляется.
- **Исходный Excel не трогается.** Все правки — в `project.json`.
- Язык интерфейса: русский. Сообщения об ошибках — на русском.
- Стиль: Windows 11. Акцент `#0078D4`. Ошибки — `#C42B1C`
  на фоне `#FDE7E9`. Предупреждения — `#8A6D00` на `#FFF4CE`.
  Успех `#107C10` на `#E6F4EA`. Вторичный текст `#605E5C`.
- Проект как папка: `project.json`, `source/`, `template/`, `output/`.
- `RecentProjectsStore` — в `%APPDATA%\CertificateGenerator\recent.json`.
  Кириллица не экранируется (UnsafeRelaxedJsonEscaping).
- **Актуальное число тестов — проверяй через `dotnet test`.**