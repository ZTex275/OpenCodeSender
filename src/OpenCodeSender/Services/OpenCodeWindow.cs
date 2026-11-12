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

        public string Title { get; set; }

        public string ProcessName { get; set; }

        public override string ToString() =>
            string.IsNullOrEmpty(Title) ? ProcessName : $"{ProcessName} - {Title}";
    }

    internal static class WindowFinder
    {
        private const string OpenCodeProcessName = "OpenCode";

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

            foreach (WindowTarget window in windows)
            {
                if (string.Equals(window.ProcessName, OpenCodeProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    return window;
                }
            }

            foreach (WindowTarget window in windows)
            {
                if (window.ProcessName != null &&
                    window.ProcessName.IndexOf("opencode", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return window;
                }
            }

            if (!string.IsNullOrWhiteSpace(titleFilter))
            {
                foreach (WindowTarget window in windows)
                {
                    if (window.Title != null &&
                        window.Title.IndexOf(titleFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return window;
                    }
                }
            }

            return null;
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
