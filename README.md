# OpenCode Sender — VSIX для Visual Studio (C#)

Расширение для Visual Studio, которое отправляет код в чат приложения **OpenCode** одним нажатием.

Как это работает:

1. Выделяете фрагмент кода в редакторе Visual Studio.
2. Нажимаете **`Ctrl+Alt+Shift+O`** (или пункт меню `Tools → Send to OpenCode`, или правый клик в редакторе → `Send to OpenCode` — сочетание клавиш показано справа и в меню «Средства», и в контекстном меню).
3. Код копируется в буфер обмена.
4. Окно OpenCode выводится на передний план и в него отправляется `Ctrl+V` — код оказывается в поле ввода чата.

Если выделен **весь файл** (или выделения нет — тогда файл берётся целиком), вставляется **только полный путь к файлу**:

```
D:\Projects\App\Services\UserService.cs
```

Так чат получает ссылку на файл, а не его содержимое. При желании можно включить в настройках добавление кода после пути.

Если выделен только фрагмент, вставляется только сам фрагмент, без пути.

Если OpenCode **закрыт**, расширение запускает его автоматически, ждёт появления окна и вставляет текст в чат — в Visual Studio текст не попадает никогда.

## Что внутри

```
OpenCodeSender.sln
src/OpenCodeSender/
    OpenCodeSender.csproj           проект (SDK-style, net472 + VS SDK из NuGet)
    source.extension.vsixmanifest    манифест VSIX (VS 2026, amd64)
    VSCommandTable.en-US.vsct       команда, меню, иконка, подсказка и горячая клавиша (английский)
    VSCommandTable.ru-RU.vsct       локализованный текст команды (русский)
    Resources.resx                  пустой нейтральный набор ресурсов (без него VS не находит Menus.ctmenu)
    Resources.en-US.resx            культура en-US (CommandCaption)
    Resources.ru-RU.resx            культура ru-RU (CommandCaption)
    AssemblyInfo.cs                 NeutralResourcesLanguage("en-US", Satellite)
    ru-RU/Extension.vsixlangpack    локализация карточки в диспетчере расширений
    OpenCodeSenderPackage.cs        пакет VS (регистрация команд, настройки, привязка клавиши)
    SendToOpenCodeCommand.cs        логика команды
    OpenCodeOptionsPage.cs          настройки
    Services/SelectionCapture.cs    чтение выделения через DTE + полный путь
    Services/ClipboardWriter.cs     запись в буфер обмена с повторами
    Services/OpenCodeWindow.cs      поиск и активация окна OpenCode (Win32), защита от вставки в чужие окна
    Services/OpenCodeLauncher.cs    автозапуск OpenCode.exe (поиск установленного приложения)
    Services/PasteFlow.cs           поиск → запуск → активация окна + Ctrl+V через SendInput
    Interop/NativeMethods.cs        P/Invoke
assets/                             генератор иконок (make-icon.ps1) и готовые PNG
docs/marketplace/                   описание, метаданные и changelog для Visual Studio Marketplace
```

## Локализация

Команда называется **Send to OpenCode** в английской Visual Studio и **Отправить в OpenCode** в русской. Это не переопределение заголовка в рантайме (через `OleMenuCommand.Text` оно не работает), а штатный механизм VSCT: две таблицы команд компилируются в `Menus.ctmenu`, вливаются в культуро-зависимые ресурсы (`MergeWithCTO`) и попадают в сателлиты `en-US\` и `ru-RU\OpenCodeSender.resources.dll`.

Важные детали, без которых меню пропадает или остаётся английским:

- В основной сборке обязателен **нейтральный** набор ресурсов (`Resources.resx` без `MergeWithCTO`). Без него Visual Studio не находит `Menus.ctmenu` и команда исчезает целиком (например, в русской VS).
- `LogicalName` у культуро-зависимых ресурсов должен быть уникальным, иначе MSBuild падает с `MSB3577` (два выхода в один файл).
- Нейтральный и культуро-зависимый VSCT не могут сосуществовать: `VSSDK1006: Two cto files have the same resource name 'Menus.ctmenu'`. Поэтому оба VSCT только с культурой в имени.
- Горячая клавиша из сателлита не регистрируется, поэтому пакет при старте проверяет привязку и при необходимости назначает её через `EnvDTE`.
- Пункт контекстного меню — это та же команда, размещённая через `<CommandPlacements>`: так Visual Studio сама показывает `Ctrl+Alt+Shift+O` справа и в «Средствах», и в меню по правому клику, без дублирования команды и второй горячей клавиши.
- Каноническое имя команды в DTE зависит от языка (`Tools.ОтправитьвOpenCode`), поэтому поиск идёт по GUID и ID команды.
- DTE разбирает клавишу привязки через активную раскладку: при русской раскладке `Ctrl+Alt+Shift+O` превращается в `Ctrl+Shift+Alt+·`. Пакет переводит `O` в символ текущей раскладки (`ToUnicodeEx`), VS сохраняет и показывает `Ctrl+Shift+Alt+O`.

Карточка расширения в диспетчере расширений локализуется через `ru-RU\Extension.vsixlangpack`.

## Сборка

Нужен MSBuild из Visual Studio (не `dotnet build`) — VSIX-у нужны задачи VSSDK.

```powershell
.\build.ps1
```

или вручную:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe" `
    "OpenCodeSender.sln" -restore -p:Configuration=Release
```

Результат: `src\OpenCodeSender\bin\Release\net472\OpenCodeSender.vsix`.

## Установка

Официальный способ (Visual Studio должна быть закрыта):

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\IDE\VSIXInstaller.exe" `
    "src\OpenCodeSender\bin\Release\net472\OpenCodeSender.vsix"
```

После установки (или после копирования файлов расширения) нужно **перезапустить Visual Studio**.

## Настройки

`Tools → Options → OpenCode Sender → General`:

| Параметр | По умолчанию | Назначение |
| --- | --- | --- |
| Paste delay (ms) | 350 | Пауза после активации окна OpenCode перед `Ctrl+V`. Увеличьте, если текст иногда не вставляется. |
| Start OpenCode when it is not running | включено | Если окно OpenCode не найдено, запустить приложение, дождаться окна и вставить текст туда. |
| Startup timeout (seconds) | 30 | Сколько ждать появления окна после запуска OpenCode. |
| OpenCode executable path | пусто | Путь к `OpenCode.exe`. Пусто — автопоиск: `Programs\ai.opencode.desktop`, App Paths, ярлык в меню «Пуск», запись деинсталляции. |
| OpenCode window title filter | `opencode` | Запасной поиск окна по заголовку — только среди терминалов (Windows Terminal, cmd, PowerShell, conhost, mintty и т. п.), если OpenCode запущен в терминале. |
| Send the full file path when the whole file is selected | включено | При выделении всего файла отправлять путь вместо кода. |
| Also append the file contents for a whole file | выключено | Дополнительно дописать код после пути. |
| Treat an empty selection as the whole file | включено | Если ничего не выделено — считать, что выбран весь файл. |

## Как ищется окно OpenCode

1. Сначала ищется видимое окно процесса `OpenCode` (приложение OpenCode Desktop). Из нескольких окон берётся то, что выше по Z-порядку.
2. Затем — любое окно, в имени процесса которого есть `opencode`.
3. Затем — окно терминала, в заголовке которого есть строка из настройки `OpenCode window title filter`.

Окна Visual Studio (процесс, в котором работает расширение), браузеров и проводника исключены всегда: в них текст не попадёт даже при совпадении заголовка. Перед отправкой `Ctrl+V` дополнительно проверяется, что на переднем плане находится именно окно целевого процесса OpenCode.

Если окно так и не появилось, код остаётся в буфере обмена — можно переключиться в OpenCode и нажать `Ctrl+V` вручную.

## Замечания

- Горячая клавиша по умолчанию: `Ctrl+Alt+Shift+O` (меняется в `Tools → Options → Environment → Keyboard`, команда `Tools.SendToOpenCode`). Комбинация `Ctrl+Alt+O` в Visual Studio уже занята командой `View.Output`, поэтому выбрана свободная.
- Если VSCT-привязка не применилась или в настройках осталась «битая» привязка (например, с не-ASCII именем клавиши после смены раскладки), пакет при загрузке сам назначает `Ctrl+Alt+Shift+O` через модель автоматизации. Имя области берётся из существующих команд шелла, потому что в русской VS это не `Global`, а `Везде`.
- Текст вставляется «как есть», без обрамления в markdown-блок; завершающие переводы строки обрезаются, чтобы чат не отправил сообщение сразу.
- Диагностика пишется в `%TEMP%\opencode-sender.log`: инициализация горячей клавиши и результат каждой отправки (запуск, найдено окно, активация, вставка).
- Расширение написано против VS SDK 17.14 (сборки совместимы с Visual Studio 2026), манифест при этом разрешает установку в VS 2026 (`[18.0,19.0)`).

## Публикация в Visual Studio Marketplace

Материалы карточки лежат в `docs/marketplace/`:

- `listing-metadata.md` — название, краткое описание, теги, категория, ссылки и черновики ответов для Q&A;
- `description-en-US.md` и `description-ru-RU.md` — полные описания на английском и русском;
- `CHANGELOG.md` — история версий (1.1.0 и 1.0.0).

Иконки: `icon-128.png` (карточка), `preview-200.png` (страница расширения), `command-16.png` (меню и панели инструментов) — всё генерируется скриптом `assets/make-icon.ps1`.
