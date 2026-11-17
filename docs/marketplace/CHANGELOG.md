# Changelog

## 1.3.0

### Changed

- **Renamed to Paste into all.** The extension, the assembly (`PasteIntoAll.dll`), the namespace, the solution, the options page and the documentation all carry the new name. The command is *Paste into all* in English Visual Studio and *Вставить в приложение* in Russian Visual Studio.
- Settings, messages and status‑bar text now speak about pasting into a target application rather than sending to a chat, so they fit every target.
- Diagnostics log moved to `%TEMP%\paste-into-all.log`.

### Added

- The description states explicitly that the target process must already be open, and that the extension has been verified with browsers, OpenCode, VS Code and Notepad.

### Fixed

- The package is now built with the Visual Studio 2026 SDK build tools (`Microsoft.VSSDK.BuildTools` 18.x). A VSIX built with the 17.x tools lacks the `sdk` marker the Marketplace validates, so publishing failed with *“Your extension targets API version 18.0 or greater but was not built with an up-to-date VSSDK”*.

## 1.2.0

### Added

- **Any target application.** The setting formerly known as *OpenCode window title filter* is now **Target application (process or window title)**: enter a process name (`chrome`, `Code`, `notepad`) or a fragment of a window title and the text is pasted there. Compact spellings are understood (`vscode` finds “Visual Studio Code”), and empty or `opencode` keeps the original OpenCode behaviour.
- **Localized options page.** Setting names, descriptions and category groups on *Tools → Options → Paste into all → General* are now shown in Russian Visual Studio, and all dialogs and status‑bar messages follow the UI language.

### Changed

- Autostart of the OpenCode desktop application now happens only when the configured target is OpenCode; a custom target is never started.
- Browsers and Explorer are no longer blocked as paste targets, since they are now legitimate targets. The window that hosts the extension (Visual Studio) is still never used.
- Version bumped to 1.2.0 in the manifest, the assembly and the installed‑product registration; the marketplace description mentions the configurable target.

## 1.1.0

### Added

- **Localized command caption.** The command is now *Paste into all* in English Visual Studio and *Вставить в приложение* in Russian Visual Studio, through culture‑specific VSCT command tables merged into satellite assemblies (`en-US`, `ru-RU`).
- **Localized Extensions Manager entry.** A Russian language pack (`ru-RU/Extension.vsixlangpack`) provides the localized display name and description.
- **Marketplace artwork.** A 128×128 extension icon, a 200×200 preview image and a dedicated 16×16 command icon for the Tools menu.
- **`Ctrl+Alt+Shift+O` in the tooltip.** The command tooltip now advertises the shortcut in both languages.

### Fixed

- The shortcut could be registered as `Ctrl+Shift+Alt+·` under a Cyrillic keyboard layout, because the automation model parses the key name with the active layout. The package now translates `O` through the current layout (`ToUnicodeEx`) before assigning the binding, so Visual Studio stores and displays `Ctrl+Shift+Alt+O`.
- The default binding was not applied when the localized command table produced a localized canonical command name (`Tools.ОтправитьвOpenCode`). Command lookup now goes through the stable GUID and command ID, with the canonical name as a fast path.
- The command could disappear entirely in non‑English Visual Studio when only satellite resource sets were shipped. A neutral resource set is embedded in the main assembly so the command table is always resolvable.

### Changed

- Version bumped to 1.1.0 in the manifest, the assembly and the installed‑product registration.
- Marketplace description, tags and preview image added to `source.extension.vsixmanifest`.

## 1.0.0

- Initial release.
- Send a fragment, a whole file path, or the file path plus contents to the OpenCode chat.
- Automatic OpenCode launch when it is not running, with careful window detection (Visual Studio, browsers and Explorer are excluded).
- Configurable paste delay, startup timeout, executable path, window title filter and whole‑file behaviour.
- Diagnostics written to `%TEMP%\paste-into-all.log`.
