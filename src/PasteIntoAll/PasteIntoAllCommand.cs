using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace PasteIntoAll
{
    internal static class CommandIds
    {
        public const int SendSelection = 0x0100;
    }
    /// <summary>
    /// "Paste into all": copies the selection to the clipboard, activates the target window
    /// (OpenCode by default) and pastes the text there.
    /// </summary>
    internal sealed class PasteIntoAllCommand
    {
        /// <summary>
        /// Canonical (DTE) name used as a fast path when looking the command up. In a localized
        /// Visual Studio the name differs, so the lookup falls back to the GUID and command id.
        /// </summary>
        public const string CanonicalName = "Tools.Pasteintoall";

        /// <summary>Default keyboard shortcut, must match the one in VSCommandTable.vsct.</summary>
        public const string DefaultShortcut = "Ctrl+Alt+Shift+O";

        private readonly PasteIntoAllPackage _package;

        private PasteIntoAllCommand(PasteIntoAllPackage package)
        {
            _package = package;
        }

        public static async Task InitializeAsync(PasteIntoAllPackage package)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (!(await package.GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService))
            {
                return;
            }

            var command = new PasteIntoAllCommand(package);

            commandService.AddCommand(command.CreateMenuCommand(CommandIds.SendSelection));
        }

        private OleMenuCommand CreateMenuCommand(int commandId)
        {
            return new OleMenuCommand(Execute, new CommandID(PasteIntoAllPackage.CommandSet, commandId));
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _ = _package.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await ExecuteAsync();
                }
                catch (Exception ex)
                {
                    await _package.JoinableTaskFactory.SwitchToMainThreadAsync();
                    _package.ShowError(Localization.SendFailed(ex.Message));
                }
            });
        }

        private async Task ExecuteAsync()
        {
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await _package.GetServiceAsync(typeof(DTE)) as DTE;
            if (dte == null)
            {
                _package.ShowError(Localization.DteUnavailable);
                return;
            }

            PasteIntoAllOptionsPage options = _package.Options;

            CapturedSelection capture;
            try
            {
                capture = SelectionCapture.FromActiveDocument(dte, options.EmptySelectionMeansWholeFile);
            }
            catch (COMException ex)
            {
                _package.ShowError(Localization.ReadEditorFailed(ex.Message));
                return;
            }

            if (capture == null)
            {
                _package.ShowInfo(Localization.NoTextFile);
                return;
            }

            string payload = PayloadBuilder.Build(capture, options);

            try
            {
                ClipboardWriter.SetText(payload);
            }
            catch (Exception ex)
            {
                _package.ShowError(Localization.ClipboardFailed(ex.Message));
                return;
            }

            // Window activation and pasting involve blocking waits, so they run on a background thread.
            int delay = options.PasteDelayMs;
            string targetName = options.TargetApplication;
            bool launchIfMissing = options.LaunchOpenCodeIfNotRunning;
            string executablePath = options.OpenCodeExecutablePath;
            int launchTimeout = options.LaunchTimeoutSeconds;

            PasteResult result = await Task.Run(() => PasteFlow.Run(delay, targetName, launchIfMissing, executablePath, launchTimeout));
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync();

            PasteIntoAllPackage.Log(
                $"Send: wholeFile={capture.IsWholeFile} payloadLength={payload.Length} " +
                $"launched={result.Launched} windowFound={result.WindowFound} target='{result.WindowDescription}' " +
                $"activated={result.Activated} pasted={result.Pasted}" +
                (result.LaunchFailed ? " launchError='" + result.LaunchError + "'" : string.Empty));

            string targetDisplayName = WindowFinder.IsOpenCodeTarget(targetName)
                ? WindowFinder.OpenCodeProcessName
                : targetName.Trim();

            if (!result.WindowFound)
            {
                if (result.LaunchFailed)
                {
                    _package.ShowWarning(Localization.LaunchFailed(targetDisplayName, result.LaunchError));
                    return;
                }

                _package.ShowWarning(Localization.WindowNotFound(targetDisplayName));
                return;
            }

            if (!result.Activated)
            {
                _package.ShowWarning(Localization.NotActivated(targetDisplayName));
                return;
            }

            if (!result.Pasted)
            {
                _package.ShowWarning(Localization.PasteBlocked(targetDisplayName));
                return;
            }

            string status;
            if (capture.IsWholeFile)
            {
                status = Localization.StatusFilePath(targetDisplayName, capture.FileName);
            }
            else
            {
                status = Localization.StatusFragment(targetDisplayName, capture.LineCount);
            }

            _package.SetStatusBarText(status);
        }
    }
}
