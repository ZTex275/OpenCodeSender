using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace OpenCodeSender
{
    internal static class CommandIds
    {
        public const int SendSelection = 0x0100;
        public const int SendSelectionContext = 0x0101;
    }
    /// <summary>
    /// "Send to OpenCode": copies the selection to the clipboard, activates the OpenCode window
    /// and pastes the text into the chat input.
    /// </summary>
    internal sealed class SendToOpenCodeCommand
    {
        /// <summary>Canonical (DTE) name of the command defined in VSCommandTable.vsct.</summary>
        public const string CanonicalName = "Tools.SendtoOpenCode";

        /// <summary>Default keyboard shortcut, must match the one in VSCommandTable.vsct.</summary>
        public const string DefaultShortcut = "Ctrl+Alt+Shift+O";

        private readonly OpenCodeSenderPackage _package;

        private SendToOpenCodeCommand(OpenCodeSenderPackage package)
        {
            _package = package;
        }

        public static async Task InitializeAsync(OpenCodeSenderPackage package)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (!(await package.GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService))
            {
                return;
            }

            var command = new SendToOpenCodeCommand(package);

            commandService.AddCommand(command.CreateMenuCommand(CommandIds.SendSelection));
            commandService.AddCommand(command.CreateMenuCommand(CommandIds.SendSelectionContext));
        }

        private OleMenuCommand CreateMenuCommand(int commandId)
        {
            return new OleMenuCommand(Execute, new CommandID(OpenCodeSenderPackage.CommandSet, commandId));
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
                    _package.ShowError("Sending the code to OpenCode failed:" + Environment.NewLine + Environment.NewLine + ex.Message);
                }
            });
        }

        private async Task ExecuteAsync()
        {
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await _package.GetServiceAsync(typeof(DTE)) as DTE;
            if (dte == null)
            {
                _package.ShowError("Visual Studio automation object (DTE) is not available.");
                return;
            }

            OpenCodeOptionsPage options = _package.Options;

            CapturedSelection capture;
            try
            {
                capture = SelectionCapture.FromActiveDocument(dte, options.EmptySelectionMeansWholeFile);
            }
            catch (COMException ex)
            {
                _package.ShowError("Could not read the editor content:" + Environment.NewLine + Environment.NewLine + ex.Message);
                return;
            }

            if (capture == null)
            {
                _package.ShowInfo("Put the caret into a text file or select a code fragment first.");
                return;
            }

            string payload = PayloadBuilder.Build(capture, options);

            try
            {
                ClipboardWriter.SetText(payload);
            }
            catch (Exception ex)
            {
                _package.ShowError("Could not copy the code to the clipboard:" + Environment.NewLine + Environment.NewLine + ex.Message);
                return;
            }

            // Window activation and pasting involve blocking waits, so they run on a background thread.
            int delay = options.PasteDelayMs;
            string titleFilter = options.WindowTitleFilter;
            bool launchIfMissing = options.LaunchOpenCodeIfNotRunning;
            string executablePath = options.OpenCodeExecutablePath;
            int launchTimeout = options.LaunchTimeoutSeconds;

            PasteResult result = await Task.Run(() => PasteFlow.Run(delay, titleFilter, launchIfMissing, executablePath, launchTimeout));
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync();

            OpenCodeSenderPackage.Log(
                $"Send: wholeFile={capture.IsWholeFile} payloadLength={payload.Length} " +
                $"launched={result.Launched} windowFound={result.WindowFound} target='{result.WindowDescription}' " +
                $"activated={result.Activated} pasted={result.Pasted}" +
                (result.LaunchFailed ? " launchError='" + result.LaunchError + "'" : string.Empty));

            if (!result.WindowFound)
            {
                if (result.LaunchFailed)
                {
                    _package.ShowWarning(
                        "The code was copied to the clipboard, but OpenCode could not be started." + Environment.NewLine + Environment.NewLine +
                        result.LaunchError + Environment.NewLine + Environment.NewLine +
                        "Start OpenCode manually and press Ctrl+V in its chat, or set the executable path in Tools > Options > OpenCode Sender.");
                    return;
                }

                _package.ShowWarning(
                    "The code was copied to the clipboard, but no OpenCode window was found." + Environment.NewLine + Environment.NewLine +
                    "Start OpenCode, then press Ctrl+V in its chat to paste the code." + Environment.NewLine + Environment.NewLine +
                    "If OpenCode runs inside a terminal, set its window title filter in Tools > Options > OpenCode Sender.");
                return;
            }

            if (!result.Activated)
            {
                _package.ShowWarning(
                    "The code was copied to the clipboard, but the OpenCode window could not be activated." + Environment.NewLine + Environment.NewLine +
                    "Switch to OpenCode manually and press Ctrl+V.");
                return;
            }

            if (!result.Pasted)
            {
                _package.ShowWarning(
                    "The code was copied to the clipboard and OpenCode was activated, but the paste keystroke was blocked." + Environment.NewLine + Environment.NewLine +
                    "Press Ctrl+V in the OpenCode chat.");
                return;
            }

            string status;
            if (capture.IsWholeFile)
            {
                status = $"OpenCode: file path sent ({capture.FileName})";
            }
            else
            {
                status = $"OpenCode: fragment sent ({capture.LineCount} line(s))";
            }

            _package.SetStatusBarText(status);
        }
    }
}
