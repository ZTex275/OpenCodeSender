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
    [InstalledProductRegistration("OpenCode Sender", "Sends the selected code to the OpenCode chat window.", "1.0.1")]
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
        /// The VSCT default shortcut can be silently dropped (Ctrl+Alt+O belongs to View.Output in
        /// Visual Studio), so the shortcut is applied through the automation model when the command
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
                    return;
                }

                EnvDTE.Command command = dte.Commands.Item(SendToOpenCodeCommand.CanonicalName, -1);
                if (command == null)
                {
                    return;
                }

                if (command.Bindings is object[] existing && existing.Length > 0)
                {
                    return;
                }

                string scope = FindGlobalScopeName(dte);
                if (string.IsNullOrEmpty(scope))
                {
                    return;
                }

                command.Bindings = new object[] { scope + "::" + SendToOpenCodeCommand.DefaultShortcut };
            }
            catch (Exception)
            {
                // A missing shortcut must never break the package.
            }
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
