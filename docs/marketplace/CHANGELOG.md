# Changelog

## 1.1.0

### Added

- **Localized command caption.** The command is now *Send to OpenCode* in English Visual Studio and *Отправить в OpenCode* in Russian Visual Studio, through culture‑specific VSCT command tables merged into satellite assemblies (`en-US`, `ru-RU`).
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
- Diagnostics written to `%TEMP%\opencode-sender.log`.
