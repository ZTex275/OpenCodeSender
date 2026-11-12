using System;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace OpenCodeSender
{
    /// <summary>
    /// Code that is going to be sent to OpenCode.
    /// </summary>
    internal sealed class CapturedSelection
    {
        public string Code { get; set; }

        public string FullPath { get; set; }

        public string FileName { get; set; }

        public bool IsWholeFile { get; set; }

        public int LineCount { get; set; }
    }

    internal static class SelectionCapture
    {
        /// <summary>
        /// Reads the current selection (or the whole document) from the active editor.
        /// </summary>
        public static CapturedSelection FromActiveDocument(DTE dte, bool emptySelectionMeansWholeFile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Document document = dte?.ActiveDocument;
            if (document == null)
            {
                return null;
            }

            if (!(document.Object("TextDocument") is TextDocument textDocument))
            {
                return null;
            }

            TextSelection selection = document.Selection as TextSelection;
            string selectedText = selection?.Text ?? string.Empty;

            string fullText = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);

            bool isWholeFile = IsWholeFile(selectedText, fullText);

            if (!isWholeFile && string.IsNullOrWhiteSpace(selectedText) && !emptySelectionMeansWholeFile)
            {
                return null;
            }

            string code = isWholeFile ? fullText : selectedText;
            if (string.IsNullOrWhiteSpace(code) && !isWholeFile)
            {
                return null;
            }

            return new CapturedSelection
            {
                Code = code,
                IsWholeFile = isWholeFile,
                FullPath = GetFullPath(document),
                FileName = GetFileName(document),
                LineCount = CountLines(code)
            };
        }

        private static bool IsWholeFile(string selectedText, string fullText)
        {
            // No selection at all is treated as "the whole file".
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                return true;
            }

            return string.Equals(Normalize(selectedText), Normalize(fullText), StringComparison.Ordinal);
        }

        private static string Normalize(string text)
        {
            if (text == null)
            {
                return string.Empty;
            }

            return text
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .TrimEnd(' ', '\t', '\n');
        }

        private static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            int lines = 1;
            foreach (char c in text)
            {
                if (c == '\n')
                {
                    lines++;
                }
            }

            return lines;
        }

        private static string GetFullPath(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                string fullName = document.FullName;
                return string.IsNullOrWhiteSpace(fullName) ? null : fullName;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetFileName(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                return document.Name;
            }
            catch (Exception)
            {
                return "untitled";
            }
        }
    }

    internal static class PayloadBuilder
    {
        /// <summary>
        /// Builds the text that is placed on the clipboard.
        /// A whole file is prefixed with its absolute path, a fragment is pasted as is.
        /// </summary>
        public static string Build(CapturedSelection capture, OpenCodeOptionsPage options)
        {
            string code = capture.Code ?? string.Empty;

            // Never send a trailing newline: some chat inputs treat Enter as "send".
            code = code.TrimEnd('\r', '\n');

            if (!capture.IsWholeFile || options == null || !options.IncludeFullPathForWholeFile)
            {
                return code;
            }

            string path = string.IsNullOrEmpty(capture.FullPath) ? capture.FileName : capture.FullPath;

            var builder = new StringBuilder();
            builder.Append(path);
            builder.Append("\r\n\r\n");
            builder.Append(code);
            return builder.ToString();
        }
    }
}
