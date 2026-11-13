using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace OpenCodeSender
{
    /// <summary>
    /// Settings available in Tools &gt; Options &gt; OpenCode Sender.
    /// </summary>
    public class OpenCodeOptionsPage : DialogPage
    {
        [Category("Pasting")]
        [DisplayName("Paste delay (ms)")]
        [Description("How long to wait after activating the OpenCode window before pressing Ctrl+V. Increase it if the text is not pasted on a slow machine.")]
        [DefaultValue(350)]
        public int PasteDelayMs { get; set; } = 350;

        [Category("Pasting")]
        [DisplayName("Start OpenCode when it is not running")]
        [Description("When no OpenCode window is found, the extension starts the OpenCode desktop application, waits for its window and pastes the text there.")]
        [DefaultValue(true)]
        public bool LaunchOpenCodeIfNotRunning { get; set; } = true;

        [Category("Pasting")]
        [DisplayName("Startup timeout (seconds)")]
        [Description("How long to wait for the OpenCode window after starting the application.")]
        [DefaultValue(30)]
        public int LaunchTimeoutSeconds { get; set; } = 30;

        [Category("Pasting")]
        [DisplayName("OpenCode executable path")]
        [Description("Optional. Leave empty to auto-detect OpenCode.exe (Programs\\ai.opencode.desktop, App Paths, Start menu shortcut, uninstall entry).")]
        [DefaultValue("")]
        public string OpenCodeExecutablePath { get; set; } = string.Empty;

        [Category("Pasting")]
        [DisplayName("OpenCode window title filter")]
        [Description("Fallback for the case when OpenCode runs inside a terminal. Only terminal windows (Windows Terminal, cmd, PowerShell, conhost, mintty) are searched by this title.")]
        [DefaultValue("opencode")]
        public string WindowTitleFilter { get; set; } = "opencode";

        [Category("Content")]
        [DisplayName("Send the full file path when the whole file is selected")]
        [Description("When the selection covers the whole document, its absolute path is sent instead of the code.")]
        [DefaultValue(true)]
        public bool IncludeFullPathForWholeFile { get; set; } = true;

        [Category("Content")]
        [DisplayName("Also append the file contents for a whole file")]
        [Description("Off by default: only the path is sent for a whole file. Turn it on to send the path followed by the code.")]
        [DefaultValue(false)]
        public bool IncludeCodeWithWholeFilePath { get; set; } = false;

        [Category("Content")]
        [DisplayName("Treat an empty selection as the whole file")]
        [Description("If nothing is selected, the whole document is treated as selected (and only its path is sent).")]
        [DefaultValue(true)]
        public bool EmptySelectionMeansWholeFile { get; set; } = true;
    }
}
