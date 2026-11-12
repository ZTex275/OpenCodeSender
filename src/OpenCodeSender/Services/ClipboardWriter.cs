using System;
using System.Threading;

namespace OpenCodeSender
{
    internal static class ClipboardWriter
    {
        /// <summary>
        /// Puts the text on the clipboard. The clipboard can be locked by another application,
        /// therefore the operation is retried a few times.
        /// </summary>
        public static void SetText(string text)
        {
            Exception lastError = null;

            for (int attempt = 0; attempt < 6; attempt++)
            {
                try
                {
                    System.Windows.Forms.Clipboard.SetDataObject(text, true, 10, 60);
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Thread.Sleep(60);
                }
            }

            throw new InvalidOperationException(
                "The clipboard is locked by another application." + (lastError != null ? " " + lastError.Message : string.Empty),
                lastError);
        }
    }
}
