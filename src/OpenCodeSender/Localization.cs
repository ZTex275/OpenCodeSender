using System;
using System.Globalization;

namespace OpenCodeSender
{
    /// <summary>
    /// User interface strings. Visual Studio shows the extension in English and Russian;
    /// any other UI language falls back to English.
    /// </summary>
    internal static class Localization
    {
        private static bool Russian
        {
            get
            {
                try
                {
                    return string.Equals(
                        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
                        "ru",
                        StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private static string Pick(string english, string russian) => Russian ? russian : english;

        // ---- Options page: categories ------------------------------------------------

        public static string CategoryPasting => Pick("Pasting", "Вставка");

        public static string CategoryContent => Pick("Content", "Содержимое");

        // ---- Options page: properties ------------------------------------------------

        public static string PasteDelayName => Pick("Paste delay (ms)", "Пауза перед вставкой (мс)");

        public static string PasteDelayDescription => Pick(
            "How long to wait after activating the target window before pressing Ctrl+V. Increase it if the text is not pasted on a slow machine.",
            "Сколько ждать после активации целевого окна перед нажатием Ctrl+V. Увеличьте, если на медленной машине текст не вставляется.");

        public static string LaunchName => Pick(
            "Start OpenCode when it is not running",
            "Запускать OpenCode, если он не запущен");

        public static string LaunchDescription => Pick(
            "Used when the target is OpenCode. If no window is found, the OpenCode desktop application is started, and the text is pasted as soon as its window appears.",
            "Используется, когда целевое приложение — OpenCode. Если окно не найдено, приложение OpenCode запускается, а текст вставляется, как только появится его окно.");

        public static string LaunchTimeoutName => Pick("Startup timeout (seconds)", "Тайм-аут запуска (с)");

        public static string LaunchTimeoutDescription => Pick(
            "How long to wait for the target window after starting the application.",
            "Сколько ждать появления целевого окна после запуска приложения.");

        public static string ExecutableName => Pick("OpenCode executable path", "Путь к OpenCode.exe");

        public static string ExecutableDescription => Pick(
            "Optional. Leave empty to auto-detect OpenCode.exe (Programs\\ai.opencode.desktop, App Paths, Start menu shortcut, uninstall entry).",
            "Необязательно. Пусто — автопоиск OpenCode.exe (Programs\\ai.opencode.desktop, App Paths, ярлык в меню «Пуск», запись деинсталляции).");

        public static string TargetName => Pick(
            "Target application (process or window title)",
            "Целевое приложение (процесс или заголовок окна)");

        public static string TargetDescription => Pick(
            "Process name (for example opencode, chrome, Code) or a fragment of a window title. The first visible window of that process receives the text. Leave empty to send to OpenCode.",
            "Имя процесса (например opencode, chrome, Code) или фрагмент заголовка окна. Текст вставляется в первое видимое окно этого процесса. Пусто — отправлять в OpenCode.");

        public static string WholeFilePathName => Pick(
            "Send the full file path when the whole file is selected",
            "Отправлять полный путь, если выбран весь файл");

        public static string WholeFilePathDescription => Pick(
            "When the selection covers the whole document, its absolute path is sent instead of the code.",
            "Если выделен весь документ, вместо кода отправляется абсолютный путь к файлу.");

        public static string AppendCodeName => Pick(
            "Also append the file contents for a whole file",
            "Дописывать код после пути целого файла");

        public static string AppendCodeDescription => Pick(
            "Off by default: only the path is sent for a whole file. Turn it on to send the path followed by the code.",
            "По умолчанию выключено: для целого файла отправляется только путь. Включите, чтобы отправлять путь, а затем код.");

        public static string EmptySelectionName => Pick(
            "Treat an empty selection as the whole file",
            "Считать пустое выделение целым файлом");

        public static string EmptySelectionDescription => Pick(
            "If nothing is selected, the whole document is treated as selected (and only its path is sent).",
            "Если ничего не выделено, считается, что выбран весь документ (и отправляется только его путь).");

        // ---- Messages ----------------------------------------------------------------

        public static string DteUnavailable => Pick(
            "Visual Studio automation object (DTE) is not available.",
            "Объект автоматизации Visual Studio (DTE) недоступен.");

        public static string SendFailed(string details) => Pick(
            "Sending the code failed:" + Environment.NewLine + Environment.NewLine + details,
            "Не удалось отправить код:" + Environment.NewLine + Environment.NewLine + details);

        public static string ReadEditorFailed(string details) => Pick(
            "Could not read the editor content:" + Environment.NewLine + Environment.NewLine + details,
            "Не удалось прочитать содержимое редактора:" + Environment.NewLine + Environment.NewLine + details);

        public static string ClipboardFailed(string details) => Pick(
            "Could not copy the code to the clipboard:" + Environment.NewLine + Environment.NewLine + details,
            "Не удалось скопировать код в буфер обмена:" + Environment.NewLine + Environment.NewLine + details);

        public static string NoTextFile => Pick(
            "Put the caret into a text file or select a code fragment first.",
            "Поставьте курсор в текстовый файл или сначала выделите фрагмент кода.");

        public static string LaunchFailed(string target, string error) => Pick(
            "The code was copied to the clipboard, but OpenCode could not be started." +
            Environment.NewLine + Environment.NewLine + error +
            Environment.NewLine + Environment.NewLine +
            "Start OpenCode manually and press Ctrl+V there, or set the executable path in Tools > Options > OpenCode Sender.",
            "Код скопирован в буфер обмена, но запустить OpenCode не удалось." +
            Environment.NewLine + Environment.NewLine + error +
            Environment.NewLine + Environment.NewLine +
            "Запустите OpenCode вручную и нажмите в нём Ctrl+V либо укажите путь к OpenCode.exe в Средства → Параметры → OpenCode Sender.");

        public static string WindowNotFound(string target) => Pick(
            "The code was copied to the clipboard, but no window of \"" + target + "\" was found." +
            Environment.NewLine + Environment.NewLine +
            "Start the application and press Ctrl+V there to paste the code." +
            Environment.NewLine + Environment.NewLine +
            "The target is configured in Tools > Options > OpenCode Sender > General.",
            "Код скопирован в буфер обмена, но окно «" + target + "» не найдено." +
            Environment.NewLine + Environment.NewLine +
            "Запустите приложение и нажмите в нём Ctrl+V, чтобы вставить код." +
            Environment.NewLine + Environment.NewLine +
            "Целевое приложение задаётся в Средства → Параметры → OpenCode Sender → General.");

        public static string NotActivated(string target) => Pick(
            "The code was copied to the clipboard, but the window of \"" + target + "\" could not be activated." +
            Environment.NewLine + Environment.NewLine +
            "Switch to it manually and press Ctrl+V.",
            "Код скопирован в буфер обмена, но активировать окно «" + target + "» не удалось." +
            Environment.NewLine + Environment.NewLine +
            "Переключитесь в него вручную и нажмите Ctrl+V.");

        public static string PasteBlocked(string target) => Pick(
            "The code was copied to the clipboard and \"" + target + "\" was activated, but the paste keystroke was blocked." +
            Environment.NewLine + Environment.NewLine +
            "Press Ctrl+V there.",
            "Код скопирован в буфер обмена и окно «" + target + "» активировано, но нажатие Ctrl+V было заблокировано." +
            Environment.NewLine + Environment.NewLine +
            "Нажмите в нём Ctrl+V.");

        public static string StatusFilePath(string target, string fileName) => Pick(
            target + ": file path sent (" + fileName + ")",
            target + ": отправлен путь к файлу (" + fileName + ")");

        public static string StatusFragment(string target, int lineCount) => Pick(
            target + ": fragment sent (" + lineCount + " line(s))",
            target + ": отправлен фрагмент (строк: " + lineCount + ")");
    }
}
