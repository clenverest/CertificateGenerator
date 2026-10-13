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

- **Core** — модели, интерфейсы, валидация. Не зависит от WPF и библиотек.
- **Infrastructure** — реализации (Excel, CSV, PDF, ZIP, хранилище).
- **WPF** — UI (ViewModels, Views).

Принципы:
- Разделение ответственности
- Модульность
- Ослабление связей
- Все зависимости в виде интерфейсов

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
- Сериализация через `System.Text.Json` (WriteIndented, UnsafeRelaxedJsonEscaping для кириллицы).
- Тесты с `TempFolder`.

**2.2 — стартовый экран, DI, диалоги.**
- DI-контейнер в `App.xaml.cs`.
- `RecentProjectsStore` — список последних проектов в `%APPDATA%`.
- `ProjectViewModel` — создание / открытие проекта, список последних.
- `StartupWindow` — стартовый экран.
- `CreateProjectDialog` — свой диалог создания (имя + расположение).
- `IFolderPicker`, `ICreateProjectDialogService` — абстракции.

**2.3 — загрузка таблицы в проект.**
- `IWindowNavigator` — навигация между `StartupWindow` и `MainWindow`.
- `MainViewModel` — работа с проектом и таблицей.
- Загрузка таблицы **в проект**: копирование в `source/`, обновление `project.json`.
- Автозагрузка таблицы из `source/` при открытии проекта.
- `MainWindow` — шапка проекта, кнопка «Загрузить таблицу в проект».

### 🔄 Этап 2.4 в работе: поля проекта

**Цель:** пользователь создаёт список полей (ФИО, Класс, Город, Школа, свои),
сопоставляет их с колонками таблицы, видит ошибки и предупреждения,
редактирует данные.

**Тест этапа:** исправил ошибку — она исчезла.

#### Подэтапы

- **2.4.** Модель `Field` и создание полей. UI: список полей, кнопки
  «Добавить поле», «Удалить поле», «Редактировать поле».
- **2.5.** Сопоставление полей с колонками: выпадающие списки, автоподсказка.
- **2.6.** Поле ФИО: склейка из 1–3 колонок.
- **2.7.** Валидация: `RequiredFieldValidator`, `DuplicateValidator`,
  `WhitespaceValidator`.
- **2.8.** Редактирование: `DataGrid` с редактируемыми ячейками.
- **2.9.** Экран проверки: подсветка ячеек, сводка.
- **2.10.** Сохранение / отмена.
- **2.11.** Приёмка Этапа 2.

#### Что **не** делаем на Этапе 2

- Шаблон PNG, координаты полей — Этап 3.
- Генерация PDF — Этап 4.
- Массовая генерация и ZIP — Этап 5.
- Google Sheets / Yandex Tables напрямую — Future work.
- Отправка по email — Future work.

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

```
CertificateGenerator/
├── CertificateGenerator.sln
├── readme.md
├── .gitignore
├── CertificateGenerator.Core/
│   ├── Models/
│   │   ├── ColumnInfo.cs ✅
│   │   ├── TableData.cs ✅
│   │   ├── Project.cs ✅
│   │   ├── Field.cs ✅
│   │   ├── FieldKind.cs ✅
│   │   ├── ValidationIssue.cs ✅
│   │   ├── IssueSeverity.cs ✅
│   │   ├── RecentProject.cs ✅
│   │   └── CreateProjectResult.cs ✅
│   ├── Interfaces/
│   │   ├── ITableReader.cs ✅
│   │   ├── ITableReaderResolver.cs ✅
│   │   ├── IProjectStorage.cs ✅
│   │   ├── IValidator.cs ✅
│   │   ├── IRecentProjectsStore.cs ✅
│   │   ├── IFolderPicker.cs ✅
│   │   ├── ICreateProjectDialogService.cs ✅
│   │   └── IWindowNavigator.cs ✅
│   └── Validators/ ⏳ Этап 2.7
├── CertificateGenerator.Infrastructure/
│   ├── Readers/
│   │   ├── ClosedXmlTableReader.cs ✅
│   │   ├── CsvTableReader.cs ✅
│   │   └── TableReaderResolver.cs ✅
│   ├── Storage/
│   │   ├── JsonProjectStorage.cs ✅
│   │   └── JsonRecentProjectsStore.cs ✅
│   ├── Generators/ ⏳ Этапы 4–5
│   └── ZipPacker.cs ⏳ Этап 5
├── CertificateGenerator.Wpf/
│   ├── Services/
│   │   ├── WpfFolderPicker.cs ✅
│   │   ├── CreateProjectDialogService.cs ✅
│   │   └── WpfWindowNavigator.cs ✅
│   ├── Views/
│   │   └── CreateProjectDialog.xaml + .cs ✅
│   ├── ViewModels/
│   │   ├── MainViewModel.cs ✅
│   │   ├── ProjectViewModel.cs ✅
│   │   └── FieldsViewModel.cs ⏳ Этап 2.4
│   ├── App.xaml ✅ (без StartupUri)
│   ├── App.xaml.cs ✅ (DI)
│   ├── MainWindow.xaml + .cs ✅
│   └── StartupWindow.xaml + .cs ✅
└── CertificateGenerator.Tests/
    ├── Models/ ✅
    ├── Fakes/ ✅
    ├── Readers/ ✅
    ├── Storage/ ✅
    ├── ViewModels/ ✅
    └── Validators/ ⏳ Этап 2.7
```

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
4. `dotnet test` — прогнать тесты (128 зелёных на момент завершения 2.3).
5. `dotnet run --project CertificateGenerator.Wpf` — запустить приложение.

## Репозиторий

https://github.com/clenverest/CertificateGenerator

## Применение «Золотых правил»

Проект разрабатывается по 12 «Золотым правилам vibe coding».
Подробности — в отчёте.

## Контекст для продолжения в новом чате

**Где мы:** Этапы 1 и 2 (подэтапы 2.0–2.3) завершены.
Подэтап 2.4 в работе (поля проекта, UI).

**Что важно помнить при продолжении:**

- Архитектура трёхслойная: `Core` → `Infrastructure` → `Wpf`.
  Зависимости направлены внутрь, `Core` ничего не знает о внешних слоях.
- Тесты — только в `CertificateGenerator.Tests`. `Core` без внешних пакетов.
  Тесты ссылаются на `Core`, `Infrastructure` и `Wpf`.
  `TargetFramework` тестов — `net8.0-windows`.
- `InternalsVisibleTo("CertificateGenerator.Tests")` в `Wpf.csproj`
  и `Infrastructure.csproj`.
- Иммутабельность моделей: `TableData`, `ColumnInfo`, `Project`, `Field`,
  `ValidationIssue`, `RecentProject`, `CreateProjectResult` — `sealed`,
  свойства только `get`, изменения через создание нового экземпляра.
- DI: `Microsoft.Extensions.DependencyInjection` подключён в `App.xaml.cs`.
  Все зависимости регистрируются в `ConfigureServices`.
  **Правило:** новая зависимость в конструкторе DI-создаваемого класса
  требует регистрации, иначе приложение упадёт на `GetRequiredService`.
- Диалоги: `Microsoft.Win32.OpenFileDialog` и `OpenFolderDialog` —
  прямо во ViewModel или в отдельном сервисе (`IFolderPicker`,
  `ICreateProjectDialogService`).
- Команды: `CommunityToolkit.Mvvm` — `[ObservableProperty]`,
  `[RelayCommand]`, `partial void On...Changed` для пересчёта
  вычисляемых свойств.
- **Важно про `PropertyChanged`:** подписка на `PropertyChanged`
  в окне **до** того, как ViewModel изменяет свойства. Иначе событие
  теряется. См. `WpfWindowNavigator.NavigateToMain` — `Initialize`
  вызывается **после** `new MainWindow(...)`.
- Язык интерфейса: русский. Сообщения об ошибках — на русском.
- Стиль: Windows 11. Акцент `#0078D4`. Ошибки — `#C42B1C`
  на фоне `#FDE7E9`. Успех `#107C10`. Вторичный текст `#605E5C`.
- Проект как папка: `project.json`, `source/`, `template/`, `output/`.
- `RecentProjectsStore` — в `%APPDATA%\CertificateGenerator\recent.json`.
  Кириллица не экранируется (UnsafeRelaxedJsonEscaping).
- **Актуальное число тестов — проверяй через `dotnet test`.**
  Я (ассистент) часто ошибаюсь в подсчётах. Считай «было N, добавили M,
  стало N+M».

