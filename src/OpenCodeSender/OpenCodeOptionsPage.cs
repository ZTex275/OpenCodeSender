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
        [DisplayName("OpenCode window title filter")]
        [Description("Used as a fallback when no process called 'OpenCode' is found (for example when OpenCode runs inside a terminal).")]
        [DefaultValue("opencode")]
        public string WindowTitleFilter { get; set; } = "opencode";

        [Category("Content")]
        [DisplayName("Include the full file path when the whole file is selected")]
        [Description("When the selection covers the whole document, the absolute file path is inserted before the code.")]
        [DefaultValue(true)]
        public bool IncludeFullPathForWholeFile { get; set; } = true;

        [Category("Content")]
        [DisplayName("Treat an empty selection as the whole file")]
        [Description("If nothing is selected, the entire document is sent (with its full path).")]
        [DefaultValue(true)]
        public bool EmptySelectionMeansWholeFile { get; set; } = true;
    }
}
