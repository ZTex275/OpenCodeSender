using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using PasteIntoAll.Interop;

namespace PasteIntoAll
{
    internal sealed class WindowTarget
    {
        public IntPtr Handle { get; set; }

        public uint ProcessId { get; set; }

        public string Title { get; set; }

        public string ProcessName { get; set; }

        public override string ToString() =>
            string.IsNullOrEmpty(Title) ? ProcessName : $"{ProcessName} - {Title}";
    }

    internal static class WindowFinder
    {
        public const string OpenCodeProcessName = "OpenCode";

        /// <summary>
        /// Processes whose window must never be used as a paste target. Visual Studio itself is
        /// covered by the "current process" check; ApplicationFrameHost and SearchHost only host
        /// windows of other applications (UWP), so pasting into them would be a guess.
        /// </summary>
        private static readonly HashSet<string> BlockedProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "devenv",
            "ApplicationFrameHost",
            "SearchHost"
        };

        /// <summary>
        /// Processes that host a terminal. The title filter fallback is limited to them so a random
        /// editor/browser window whose title merely contains the project folder name (for example
        /// "...\\VSIX OpenCode\\build.ps1 - Notepad++") can never receive the pasted code.
        /// </summary>
        private static readonly HashSet<string> TerminalProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "WindowsTerminal",
            "wt",
            "OpenConsole",
            "conhost",
            "cmd",
            "powershell",
            "pwsh",
            "mintty",
            "ConEmu64",
            "ConEmu",
            "Hyper",
            "Tabby",
            "alacritty",
            "wezterm-gui",
            "WezTerm",
            "Terminal"
        };

        /// <summary>
        /// Finds the window that should receive the pasted text.
        /// <para>
        /// An empty target (or one that names OpenCode) keeps the original, defensive lookup:
        /// the OpenCode desktop window, then any process whose name contains "opencode", then a
        /// terminal window whose title contains the target text.
        /// </para>
        /// <para>
        /// Any other target (for example "chrome", "Code" or "Excel") is matched against the
        /// process name first and against the window title afterwards, also ignoring spaces so
        /// that "vscode" finds "Visual Studio Code". The window that is highest in the Z-order
        /// wins, i.e. the one the user has seen last.
        /// </para>
        /// </summary>
        public static WindowTarget Find(string targetName)
        {
            List<WindowTarget> windows = EnumerateTopLevelWindows();
            if (windows.Count == 0)
            {
                return null;
            }

            uint ownProcessId = GetCurrentProcessId();
            string target = (targetName ?? string.Empty).Trim();

            if (target.Length == 0 || IsOpenCodeName(target))
            {
                return FindOpenCode(windows, ownProcessId, target);
            }

            foreach (WindowTarget window in windows)
            {
                if (IsUsable(window, ownProcessId) &&
                    string.Equals(window.ProcessName, target, StringComparison.OrdinalIgnoreCase))
                {
                    return window;
                }
            }

            foreach (WindowTarget window in windows)
            {
                if (IsUsable(window, ownProcessId) &&
                    window.ProcessName.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return window;
                }
            }

            foreach (WindowTarget window in windows)
            {
                if (IsUsable(window, ownProcessId) && ContainsLoose(window.Title, target))
                {
                    return window;
                }
            }

            return null;
        }

        private static WindowTarget FindOpenCode(List<WindowTarget> windows, uint ownProcessId, string titleFilter)
        {
            foreach (WindowTarget window in windows)
            {
                if (IsUsable(window, ownProcessId) &&
                    string.Equals(window.ProcessName, OpenCodeProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    return window;
                }
            }

            foreach (WindowTarget window in windows)
            {
                if (IsUsable(window, ownProcessId) &&
                    window.ProcessName.IndexOf("opencode", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return window;
                }
            }

            if (!string.IsNullOrWhiteSpace(titleFilter))
            {
                foreach (WindowTarget window in windows)
                {
                    if (IsUsable(window, ownProcessId) &&
                        IsTerminal(window) &&
                        window.Title.IndexOf(titleFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return window;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// True when the configured target means "OpenCode", i.e. when the extension may start
        /// the OpenCode application itself. A custom target such as "chrome" is never started.
        /// </summary>
        public static bool IsOpenCodeTarget(string targetName)
        {
            string target = (targetName ?? string.Empty).Trim();
            return target.Length == 0 || IsOpenCodeName(target);
        }

        private static bool IsOpenCodeName(string target)
        {
            return target.IndexOf("opencode", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Case-insensitive "contains" that ignores spaces and punctuation, so a target typed
        /// without separators ("notepad++", "visual studio code") still finds its window title.
        /// Word beginnings are matched as well: "vscode" finds "Visual Studio Code".
        /// </summary>
        private static bool ContainsLoose(string text, string needle)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(needle))
            {
                return false;
            }

            if (text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string compactText = KeepLettersAndDigits(text);
            string compactNeedle = KeepLettersAndDigits(needle);

            if (compactNeedle.Length < 3)
            {
                return false;
            }

            if (compactText.IndexOf(compactNeedle, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return MatchesWordBeginnings(text, compactNeedle);
        }

        private static string KeepLettersAndDigits(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// True when the target can be read as the beginnings of consecutive title words,
        /// at least one character per word: "vscode" = "V" + "S" + "Code" in "Visual Studio Code".
        /// </summary>
        private static bool MatchesWordBeginnings(string title, string needle)
        {
            char[] separators = { ' ', '\t', '-', '|', ':', '.', ',', '(', ')', '[', ']', '/', '\\' };
            string[] words = title.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            int position = 0;

            foreach (string word in words)
            {
                if (position >= needle.Length)
                {
                    return true;
                }

                int matched = 0;
                while (matched < word.Length &&
                       position < needle.Length &&
                       char.ToLowerInvariant(word[matched]) == char.ToLowerInvariant(needle[position]))
                {
                    matched++;
                    position++;
                }

                if (matched == 0 && position > 0)
                {
                    return false;
                }
            }

            return position == needle.Length;
        }

        private static bool IsTerminal(WindowTarget window)
        {
            return window.ProcessName != null && TerminalProcesses.Contains(window.ProcessName);
        }

        /// <summary>True when a process called OpenCode is running (with or without a window).</summary>
        public static bool IsProcessRunning()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(OpenCodeProcessName);
                try
                {
                    return processes.Length > 0;
                }
                finally
                {
                    foreach (Process process in processes)
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsUsable(WindowTarget window, uint ownProcessId)
        {
            if (window == null || window.Handle == IntPtr.Zero)
            {
                return false;
            }

            // Never paste into the process that hosts this extension (devenv).
            if (window.ProcessId == ownProcessId)
            {
                return false;
            }

            return !BlockedProcesses.Contains(window.ProcessName ?? string.Empty);
        }

        private static uint GetCurrentProcessId()
        {
            try
            {
                using (Process current = Process.GetCurrentProcess())
                {
                    return (uint)current.Id;
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static List<WindowTarget> EnumerateTopLevelWindows()
        {
            var result = new List<WindowTarget>();
            var processNames = new Dictionary<uint, string>();

            NativeMethods.EnumWindows(
                (handle, _) =>
                {
                    if (!NativeMethods.IsWindowVisible(handle))
                    {
                        return true;
                    }

                    int titleLength = NativeMethods.GetWindowTextLength(handle);
                    if (titleLength <= 0)
                    {
                        return true;
                    }

                    var buffer = new StringBuilder(titleLength + 1);
                    if (NativeMethods.GetWindowText(handle, buffer, buffer.Capacity) <= 0)
                    {
                        return true;
                    }

                    uint processId;
                    NativeMethods.GetWindowThreadProcessId(handle, out processId);

                    string processName;
                    if (!processNames.TryGetValue(processId, out processName))
                    {
                        processName = GetProcessName(processId);
                        processNames[processId] = processName;
                    }

                    result.Add(new WindowTarget
                    {
                        Handle = handle,
                        ProcessId = processId,
                        Title = buffer.ToString(),
                        ProcessName = processName
                    });

                    return true;
                },
                IntPtr.Zero);

            return result;
        }

        private static string GetProcessName(uint processId)
        {
            try
            {
                using (Process process = Process.GetProcessById((int)processId))
                {
                    return process.ProcessName;
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }

    internal static class WindowActivator
    {
        /// <summary>
        /// Brings the window to the foreground. Windows normally forbids stealing the foreground from
        /// another process, therefore several techniques are tried one after another.
        /// </summary>
        public static bool Activate(IntPtr handle, int timeoutMs)
        {
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            if (NativeMethods.IsIconic(handle))
            {
                NativeMethods.ShowWindow(handle, NativeMethods.SwRestore);
            }
            else
            {
                NativeMethods.ShowWindow(handle, NativeMethods.SwShow);
            }

            if (IsForeground(handle))
            {
                return true;
            }

            NativeMethods.SetForegroundWindow(handle);
            if (WaitForForeground(handle, 350))
            {
                return true;
            }

            // Attach to the foreground thread - this lifts the "cannot set foreground" restriction.
            IntPtr foreground = NativeMethods.GetForegroundWindow();
            uint foregroundThread = foreground == IntPtr.Zero
                ? 0
                : NativeMethods.GetWindowThreadProcessId(foreground, out _);
            uint targetThread = NativeMethods.GetWindowThreadProcessId(handle, out _);

            bool attached = false;
            try
            {
                if (foregroundThread != 0 && targetThread != 0 && foregroundThread != targetThread)
                {
                    attached = NativeMethods.AttachThreadInput(foregroundThread, targetThread, true);
                }

                NativeMethods.BringWindowToTop(handle);
                NativeMethods.SetForegroundWindow(handle);
                NativeMethods.SwitchToThisWindow(handle, true);
            }
            finally
            {
                if (attached)
                {
                    NativeMethods.AttachThreadInput(foregroundThread, targetThread, false);
                }
            }

            return WaitForForeground(handle, timeoutMs);
        }

        public static bool IsForeground(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            IntPtr foreground = NativeMethods.GetForegroundWindow();
            if (foreground == IntPtr.Zero)
            {
                return false;
            }

            return foreground == handle || NativeMethods.GetAncestor(foreground, NativeMethods.GaRoot) == handle;
        }

        /// <summary>The process id of the window that currently owns the input focus.</summary>
        public static uint GetForegroundProcessId()
        {
            IntPtr foreground = NativeMethods.GetForegroundWindow();
            if (foreground == IntPtr.Zero)
            {
                return 0;
            }

            uint processId;
            NativeMethods.GetWindowThreadProcessId(foreground, out processId);
            return processId;
        }

        private static bool WaitForForeground(IntPtr handle, int timeoutMs)
        {
            int waited = 0;
            while (waited < timeoutMs)
            {
                if (IsForeground(handle))
                {
                    return true;
                }

                System.Threading.Thread.Sleep(25);
                waited += 25;
            }

            return IsForeground(handle);
        }
    }
}
