using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using OpenCodeSender.Interop;

namespace OpenCodeSender
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
        /// covered by the "current process" check, the rest avoid pasting into browsers/Explorer
        /// when the title filter accidentally matches them.
        /// </summary>
        private static readonly HashSet<string> BlockedProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "devenv",
            "explorer",
            "chrome",
            "msedge",
            "firefox",
            "opera",
            "brave",
            "vivaldi",
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
        /// Finds the top-level OpenCode window. The window that is highest in the Z-order wins,
        /// i.e. the one the user has seen last.
        /// </summary>
        public static WindowTarget Find(string titleFilter)
        {
            List<WindowTarget> windows = EnumerateTopLevelWindows();
            if (windows.Count == 0)
            {
                return null;
            }

            uint ownProcessId = GetCurrentProcessId();

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
