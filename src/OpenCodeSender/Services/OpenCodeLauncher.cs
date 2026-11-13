using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace OpenCodeSender
{
    /// <summary>
    /// Starts the OpenCode desktop application when it is not running yet.
    /// </summary>
    internal static class OpenCodeLauncher
    {
        private const string ExecutableName = "OpenCode.exe";

        /// <summary>
        /// Returns the path of OpenCode.exe: the configured path wins, then the known install
        /// locations, the "App Paths" registry key, the Start menu shortcut and finally the
        /// uninstall entries.
        /// </summary>
        public static string ResolveExecutable(string configuredPath)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                string expanded = Environment.ExpandEnvironmentVariables(configuredPath.Trim().Trim('"'));
                if (File.Exists(expanded))
                {
                    return expanded;
                }
            }

            foreach (string candidate in GetKnownLocations())
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return FindFromAppPaths() ?? FindFromStartMenu() ?? FindFromUninstallEntries();
        }

        /// <summary>
        /// Starts OpenCode. Returns false and fills <paramref name="error"/> when the executable
        /// cannot be found or cannot be started.
        /// </summary>
        public static bool TryStart(string configuredPath, out string error)
        {
            error = null;

            string executable = ResolveExecutable(configuredPath);
            if (string.IsNullOrEmpty(executable))
            {
                error = "OpenCode.exe was not found. Start OpenCode once manually or set its path in " +
                        "Tools > Options > OpenCode Sender.";
                return false;
            }

            try
            {
                var startInfo = new ProcessStartInfo(executable)
                {
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory
                };

                using (Process.Start(startInfo))
                {
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static IEnumerable<string> GetKnownLocations()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            yield return Path.Combine(localAppData, @"Programs\ai.opencode.desktop", ExecutableName);
            yield return Path.Combine(localAppData, @"Programs\opencode", ExecutableName);
            yield return Path.Combine(localAppData, @"Programs\OpenCode", ExecutableName);
            yield return Path.Combine(localAppData, @"OpenCode", ExecutableName);
            yield return Path.Combine(localAppData, @"Programs\opencode-desktop", ExecutableName);

            if (!string.IsNullOrEmpty(programFiles))
            {
                yield return Path.Combine(programFiles, "OpenCode", ExecutableName);
            }

            if (!string.IsNullOrEmpty(programFilesX86))
            {
                yield return Path.Combine(programFilesX86, "OpenCode", ExecutableName);
            }
        }

        private static string FindFromAppPaths()
        {
            foreach (RegistryKey root in new[] { Registry.CurrentUser, Registry.LocalMachine })
            {
                try
                {
                    using (RegistryKey key = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + ExecutableName))
                    {
                        string value = key?.GetValue(null) as string;
                        if (!string.IsNullOrWhiteSpace(value) && File.Exists(value))
                        {
                            return value;
                        }
                    }
                }
                catch (Exception)
                {
                    // Try the next hive.
                }
            }

            return null;
        }

        private static string FindFromStartMenu()
        {
            var folders = new List<string>();

            string userStartMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            if (!string.IsNullOrEmpty(userStartMenu))
            {
                folders.Add(userStartMenu);
            }

            string commonStartMenu = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
            if (!string.IsNullOrEmpty(commonStartMenu))
            {
                folders.Add(commonStartMenu);
            }

            foreach (string folder in folders)
            {
                try
                {
                    foreach (string shortcut in Directory.EnumerateFiles(folder, "*.lnk", SearchOption.AllDirectories))
                    {
                        string name = Path.GetFileNameWithoutExtension(shortcut) ?? string.Empty;
                        if (name.IndexOf("opencode", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        string target = ResolveShortcut(shortcut);
                        if (!string.IsNullOrWhiteSpace(target) && File.Exists(target) &&
                            Path.GetFileName(target).IndexOf("opencode", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return target;
                        }
                    }
                }
                catch (Exception)
                {
                    // Keep looking in the other folder.
                }
            }

            return null;
        }

        private static string ResolveShortcut(string shortcutPath)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    return null;
                }

                object shell = Activator.CreateInstance(shellType);
                try
                {
                    object shortcut = shellType.InvokeMember(
                        "CreateShortcut",
                        System.Reflection.BindingFlags.InvokeMethod,
                        null,
                        shell,
                        new object[] { shortcutPath });

                    if (shortcut == null)
                    {
                        return null;
                    }

                    return shortcut.GetType().InvokeMember(
                        "TargetPath",
                        System.Reflection.BindingFlags.GetProperty,
                        null,
                        shortcut,
                        null) as string;
                }
                finally
                {
                    if (shell != null && System.Runtime.InteropServices.Marshal.IsComObject(shell))
                    {
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string FindFromUninstallEntries()
        {
            string[] subKeys =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (RegistryKey root in new[] { Registry.CurrentUser, Registry.LocalMachine })
            {
                foreach (string subKey in subKeys)
                {
                    try
                    {
                        using (RegistryKey uninstall = root.OpenSubKey(subKey))
                        {
                            if (uninstall == null)
                            {
                                continue;
                            }

                            foreach (string name in uninstall.GetSubKeyNames())
                            {
                                using (RegistryKey entry = uninstall.OpenSubKey(name))
                                {
                                    if (entry == null)
                                    {
                                        continue;
                                    }

                                    string displayName = entry.GetValue("DisplayName") as string;
                                    if (displayName == null ||
                                        displayName.IndexOf("opencode", StringComparison.OrdinalIgnoreCase) < 0)
                                    {
                                        continue;
                                    }

                                    string fromIcon = ExtractPath(entry.GetValue("DisplayIcon") as string);
                                    if (fromIcon != null)
                                    {
                                        return fromIcon;
                                    }

                                    string installLocation = entry.GetValue("InstallLocation") as string;
                                    if (!string.IsNullOrWhiteSpace(installLocation))
                                    {
                                        string candidate = Path.Combine(installLocation, ExecutableName);
                                        if (File.Exists(candidate))
                                        {
                                            return candidate;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Try the next key.
                    }
                }
            }

            return null;
        }

        private static string ExtractPath(string displayIcon)
        {
            if (string.IsNullOrWhiteSpace(displayIcon))
            {
                return null;
            }

            string path = displayIcon.Trim();

            if (path.StartsWith("\"", StringComparison.Ordinal))
            {
                int end = path.IndexOf('"', 1);
                path = end > 0 ? path.Substring(1, end - 1) : path.Trim('"');
            }
            else
            {
                int comma = path.IndexOf(',');
                if (comma > 0)
                {
                    path = path.Substring(0, comma);
                }
            }

            return File.Exists(path) ? path : null;
        }
    }
}
