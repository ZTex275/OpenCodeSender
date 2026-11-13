using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using OpenCodeSender.Interop;

namespace OpenCodeSender
{
    internal sealed class PasteResult
    {
        public bool WindowFound { get; set; }

        public bool Activated { get; set; }

        public bool Pasted { get; set; }

        public bool Launched { get; set; }

        public bool LaunchFailed { get; set; }

        public string LaunchError { get; set; }

        public string WindowDescription { get; set; }

        /// <summary>Process that owns the window we were about to paste into (diagnostics).</summary>
        public string TargetProcess { get; set; }
    }

    internal static class PasteSender
    {
        /// <summary>
        /// Sends Ctrl+V to the foreground window using SendInput (works with Electron/Chromium apps).
        /// </summary>
        public static bool SendPaste()
        {
            NativeMethods.INPUT[] inputs =
            {
                CreateKeyInput(NativeMethods.VkControl, keyUp: false),
                CreateKeyInput(NativeMethods.VkV, keyUp: false),
                CreateKeyInput(NativeMethods.VkV, keyUp: true),
                CreateKeyInput(NativeMethods.VkControl, keyUp: true)
            };

            int size = Marshal.SizeOf(typeof(NativeMethods.INPUT));
            uint sent = NativeMethods.SendInput((uint)inputs.Length, inputs, size);
            return sent == inputs.Length;
        }

        private static NativeMethods.INPUT CreateKeyInput(ushort virtualKey, bool keyUp)
        {
            return new NativeMethods.INPUT
            {
                type = NativeMethods.InputKeyboard,
                U = new NativeMethods.InputUnion
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = virtualKey,
                        wScan = 0,
                        dwFlags = keyUp ? NativeMethods.KeyEventFKeyUp : 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }
    }

    internal static class PasteFlow
    {
        /// <summary>
        /// Activates the OpenCode window (starts OpenCode when needed), waits until it is really in
        /// front and pastes the clipboard. Runs on a background thread because it uses blocking waits.
        /// The clipboard is never pasted into any other application.
        /// </summary>
        public static PasteResult Run(
            int pasteDelayMs,
            string titleFilter,
            bool launchIfMissing,
            string executablePath,
            int launchTimeoutSeconds)
        {
            var result = new PasteResult();

            WindowTarget target = WindowFinder.Find(titleFilter);

            if (target == null && launchIfMissing)
            {
                result.Launched = OpenCodeLauncher.TryStart(executablePath, out string error);
                if (!result.Launched)
                {
                    result.LaunchFailed = true;
                    result.LaunchError = error;
                    return result;
                }

                target = WaitForWindow(titleFilter, launchTimeoutSeconds);
            }

            if (target == null)
            {
                return result;
            }

            result.WindowFound = true;
            result.WindowDescription = target.ToString();
            result.TargetProcess = target.ProcessName;

            result.Activated = WindowActivator.Activate(target.Handle, 1500);
            if (!result.Activated)
            {
                return result;
            }

            // A freshly started application needs more time to restore the chat and focus its input.
            int delay = result.Launched ? Math.Max(1200, pasteDelayMs) : Math.Max(150, pasteDelayMs);
            Thread.Sleep(delay);

            if (!WindowActivator.IsForeground(target.Handle))
            {
                result.Activated = WindowActivator.Activate(target.Handle, 800);
                if (!result.Activated)
                {
                    return result;
                }

                Thread.Sleep(200);
            }

            // Last line of defence: never send Ctrl+V into our own process or another application.
            if (!IsSafeTarget(target))
            {
                result.Activated = false;
                return result;
            }

            result.Pasted = PasteSender.SendPaste();
            return result;
        }

        private static WindowTarget WaitForWindow(string titleFilter, int timeoutSeconds)
        {
            int timeout = Math.Max(5, timeoutSeconds) * 1000;
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < timeout)
            {
                WindowTarget target = WindowFinder.Find(titleFilter);
                if (target != null)
                {
                    // Let the window finish its startup animation before activating it.
                    Thread.Sleep(600);
                    return target;
                }

                Thread.Sleep(400);
            }

            return null;
        }

        private static bool IsSafeTarget(WindowTarget target)
        {
            if (!WindowActivator.IsForeground(target.Handle))
            {
                return false;
            }

            uint foregroundProcessId = WindowActivator.GetForegroundProcessId();
            if (foregroundProcessId == 0)
            {
                return false;
            }

            using (Process current = Process.GetCurrentProcess())
            {
                if (foregroundProcessId == (uint)current.Id)
                {
                    return false;
                }
            }

            return foregroundProcessId == target.ProcessId;
        }
    }
}
