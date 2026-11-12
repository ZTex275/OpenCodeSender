using System;
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

        public string WindowDescription { get; set; }
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
        /// Activates the OpenCode window, waits until it is really in front and pastes the clipboard.
        /// Runs on a background thread because it uses blocking waits.
        /// </summary>
        public static PasteResult Run(int pasteDelayMs, string titleFilter)
        {
            var result = new PasteResult();

            WindowTarget target = WindowFinder.Find(titleFilter);
            if (target == null)
            {
                return result;
            }

            result.WindowFound = true;
            result.WindowDescription = target.ToString();

            result.Activated = WindowActivator.Activate(target.Handle, 1200);
            if (!result.Activated)
            {
                return result;
            }

            // Give the application a moment to finish activation and put the caret into the chat input.
            Thread.Sleep(Math.Max(120, pasteDelayMs));

            if (!WindowActivator.IsForeground(target.Handle))
            {
                result.Activated = WindowActivator.Activate(target.Handle, 600);
                if (!result.Activated)
                {
                    return result;
                }

                Thread.Sleep(150);
            }

            result.Pasted = PasteSender.SendPaste();
            return result;
        }
    }
}
