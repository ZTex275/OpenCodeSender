using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace OpenCodeSender
{
    /// <summary>
    /// Entry point of the extension. Registers the commands and the options page.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("OpenCode Sender", "Sends the selected code to the OpenCode chat window.", "1.0.5")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(OpenCodeOptionsPage), "OpenCode Sender", "General", 0, 0, true)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuidString)]
    public sealed class OpenCodeSenderPackage : AsyncPackage
    {
        public const string PackageGuidString = "8f9d1a2c-6b3e-4c7a-9e21-5a0d3b8c4f61";

        public static readonly Guid CommandSet = new Guid("3c7a1f84-2d95-4b8e-a174-6c0e9f2b5d38");

        public OpenCodeOptionsPage Options =>
            (OpenCodeOptionsPage)GetDialogPage(typeof(OpenCodeOptionsPage));

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await SendToOpenCodeCommand.InitializeAsync(this);
            EnsureDefaultKeyBinding();
        }

        /// <summary>
        /// The VSCT default shortcut can be silently dropped (for example because the combination is
        /// already taken), so the shortcut is applied through the automation model when the command
        /// has no bindings at all. The scope name is localized ("Global" works only in English VS),
        /// therefore it is taken from an existing shell command.
        /// </summary>
        private void EnsureDefaultKeyBinding()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (!(GetService(typeof(EnvDTE.DTE)) is EnvDTE.DTE dte))
                {
                    Log("KeyBinding: DTE service is not available.");
                    return;
                }

                EnvDTE.Command command = dte.Commands.Item(SendToOpenCodeCommand.CanonicalName, -1);
                if (command == null)
                {
                    Log("KeyBinding: command not found.");
                    return;
                }

                object[] existing = command.Bindings as object[];
                if (existing != null && existing.Length > 0)
                {
                    bool matchesDefault = false;
                    bool hasBrokenBinding = false;

                    foreach (object item in existing)
                    {
                        string text = item as string;
                        if (string.IsNullOrEmpty(text))
                        {
                            continue;
                        }

                        string keys = text.Substring(text.IndexOf("::", StringComparison.Ordinal) + 2);
                        if (IsSameShortcut(keys, SendToOpenCodeCommand.DefaultShortcut))
                        {
                            matchesDefault = true;
                        }
                        else if (!IsAsciiShortcut(keys))
                        {
                            hasBrokenBinding = true;
                        }
                    }

                    if (matchesDefault || !hasBrokenBinding)
                    {
                        Log("KeyBinding: kept -> " + string.Join(" | ", Array.ConvertAll(existing, b => b?.ToString())));
                        return;
                    }

                    Log("KeyBinding: replacing broken binding -> " + string.Join(" | ", Array.ConvertAll(existing, b => b?.ToString())));
                }

                string scope = FindGlobalScopeName(dte);
                if (string.IsNullOrEmpty(scope))
                {
                    Log("KeyBinding: global scope name not found.");
                    return;
                }

                string binding = scope + "::" + SendToOpenCodeCommand.DefaultShortcut;
                command.Bindings = new object[] { binding };

                object[] assigned = command.Bindings as object[];
                Log("KeyBinding: assigned '" + binding + "' -> " +
                    (assigned == null ? "<null>" : string.Join(" | ", Array.ConvertAll(assigned, b => b?.ToString()))));
            }
            catch (Exception ex)
            {
                Log("KeyBinding: failed - " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        internal static void Log(string message)
        {
            try
            {
                string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "opencode-sender.log");
                System.IO.File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + Environment.NewLine);
            }
            catch (Exception)
            {
                // Diagnostics must never break the package.
            }
        }

        private static bool IsSameShortcut(string value, string expected)
        {
            return string.Equals(NormalizeShortcut(value), NormalizeShortcut(expected), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeShortcut(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var modifiers = new System.Collections.Generic.List<string>();
            string key = string.Empty;

            foreach (string raw in value.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string part = raw.Trim();
                if (part.Length == 0)
                {
                    continue;
                }

                if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("Control", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers.Add("CTRL");
                }
                else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers.Add("SHIFT");
                }
                else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers.Add("ALT");
                }
                else
                {
                    key = part.ToUpperInvariant();
                }
            }

            modifiers.Sort(StringComparer.Ordinal);
            return string.Join("+", modifiers) + "+" + key;
        }

        /// <summary>
        /// Shortcut descriptions written by Visual Studio are plain ASCII ("Ctrl+Shift+Alt+O").
        /// A value with other characters (for example a key name produced with a wrong keyboard
        /// layout) can never match a real key press and is replaced by the default shortcut.
        /// </summary>
        private static bool IsAsciiShortcut(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            foreach (char c in value)
            {
                if (c > 127)
                {
                    return false;
                }
            }

            return true;
        }

        private static string FindGlobalScopeName(EnvDTE.DTE dte)
        {
            foreach (string commandName in new[] { "View.Output", "Edit.Copy", "File.SaveAll", "Edit.Undo" })
            {
                try
                {
                    EnvDTE.Command command = dte.Commands.Item(commandName, -1);
                    if (command?.Bindings is object[] bindings && bindings.Length > 0)
                    {
                        string binding = bindings[0] as string;
                        int separator = binding?.IndexOf("::") ?? -1;
                        if (separator > 0)
                        {
                            return binding.Substring(0, separator);
                        }
                    }
                }
                catch (Exception)
                {
                    // Try the next command.
                }
            }

            return null;
        }

        internal void SetStatusBarText(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (GetService(typeof(SVsStatusbar)) is IVsStatusbar statusBar)
                {
                    statusBar.SetText(text ?? string.Empty);
                }
            }
            catch (Exception)
            {
                // Status bar is a nicety - never let it break the main flow.
            }
        }

        internal void ShowInfo(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ShowMessage(message, OLEMSGICON.OLEMSGICON_INFO);
        }

        internal void ShowWarning(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ShowMessage(message, OLEMSGICON.OLEMSGICON_WARNING);
        }

        internal void ShowError(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ShowMessage(message, OLEMSGICON.OLEMSGICON_CRITICAL);
        }

        private void ShowMessage(string message, OLEMSGICON icon)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            VsShellUtilities.ShowMessageBox(
                this,
                message,
                "OpenCode Sender",
                icon,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
