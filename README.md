# OpenCode Sender — VSIX для Visual Studio (C#)

Расширение для Visual Studio, которое отправляет код в чат приложения **OpenCode** одним нажатием.

Как это работает:

1. Выделяете фрагмент кода в редакторе Visual Studio.
2. Нажимаете **`Ctrl+Alt+O`** (или пункт меню `Tools → Send to OpenCode`, или правый клик в редакторе → `Send to OpenCode`).
3. Код копируется в буфер обмена.
4. Окно OpenCode выводится на передний план и в него отправляется `Ctrl+V` — код оказывается в поле ввода чата.

Если выделен **весь файл** (или выделения нет — тогда файл берётся целиком), перед кодом вставляется **полный путь к файлу**:

```
D:\Projects\App\Services\UserService.cs

public class UserService
{
    ...
}
```

Если выделен только фрагмент, вставляется только сам фрагмент, без пути.

## Что внутри

```
OpenCodeSender.sln
src/OpenCodeSender/
    OpenCodeSender.csproj          проект (SDK-style, net472 + VS SDK из NuGet)
    source.extension.vsixmanifest   манифест VSIX (VS 2026, amd64)
    VSCommandTable.vsct            команда, меню и горячая клавиша
    OpenCodeSenderPackage.cs       пакет VS (регистрация команд и страницы настроек)
    SendToOpenCodeCommand.cs       логика команды
    OpenCodeOptionsPage.cs         настройки
    Services/SelectionCapture.cs   чтение выделения через DTE + полный путь
    Services/ClipboardWriter.cs    запись в буфер обмена с повторами
    Services/OpenCodeWindow.cs     поиск и активация окна OpenCode (Win32)
    Services/PasteFlow.cs          активация окна + Ctrl+V через SendInput
    Interop/NativeMethods.cs       P/Invoke
```

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
| OpenCode window title filter | `opencode` | Запасной поиск окна по заголовку — на случай, если OpenCode запущен в терминале, а не отдельным приложением. |
| Include the full file path when the whole file is selected | включено | Вставлять полный путь к файлу перед кодом. |
| Treat an empty selection as the whole file | включено | Если ничего не выделено — отправлять файл целиком. |

## Как ищется окно OpenCode

1. Сначала ищется видимое окно процесса `OpenCode` (приложение OpenCode Desktop).
2. Затем — любое окно, в имени процесса которого есть `opencode` (например, терминал).
3. Затем — окно, в заголовке которого есть строка из настройки `OpenCode window title filter`.

Если окно не найдено, код всё равно остаётся в буфере обмена — можно переключиться в OpenCode и нажать `Ctrl+V` вручную.

## Замечания

- Горячая клавиша по умолчанию: `Ctrl+Alt+O` (меняется в `Tools → Options → Environment → Keyboard`, команда `Tools.SendToOpenCode`).
- Текст вставляется «как есть», без обрамления в markdown-блок; завершающие переводы строки обрезаются, чтобы чат не отправил сообщение сразу.
- Расширение написано против VS SDK 17.14 (сборки совместимы с Visual Studio 2026), манифест при этом разрешает установку в VS 2026 (`[18.0,19.0)`).
