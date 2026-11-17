# Paste into all

**Paste code from Visual Studio into any application — one keystroke, no copy‑paste gymnastics.**

You are debugging a method in Visual Studio and want to ask OpenCode about it, show it to a colleague in VS Code or drop it into Notepad. Normally that means: select the code, press `Ctrl+C`, switch windows, click the input box, press `Ctrl+V`, switch back. Every single time.

Paste into all collapses that into **`Ctrl+Alt+Shift+O`**.

---

## How it works

1. Select a fragment of code in the Visual Studio editor.
2. Press **`Ctrl+Alt+Shift+O`** — or use **Tools → Paste into all**, or right‑click in the editor and pick **Paste into all**.
3. The target window is brought to the front and receives `Ctrl+V` — the text lands where you are working.

By default the text goes to the **OpenCode** chat. If OpenCode is not running, it is started for you and the text is delivered as soon as the window appears.

### Any target application

Need the code in VS Code, a browser or Notepad instead of OpenCode? Set **Target application** to a process name (`Code`, `chrome`, `notepad`) or a fragment of a window title and the text goes there. Compact spellings work too: `vscode` finds the “Visual Studio Code” window.

> **The application must already be open.** Only OpenCode is ever started by the extension: if no window of the configured process is found, the text stays on the clipboard and the extension tells you to press `Ctrl+V` manually.

### Tested with

- browsers (Chrome, Edge);
- OpenCode;
- VS Code;
- Notepad.

### Fragments vs. whole files

The extension is deliberately clever about *what* it pastes:

| What you select | What the application receives |
| --- | --- |
| A fragment of code | The fragment itself, verbatim — no path noise around it |
| The whole file, or nothing at all | Only the **full path** to the file, e.g. `D:\Projects\App\Services\UserService.cs` |

Sending a path instead of a thousand lines of code keeps the chat light and lets OpenCode open the file itself. If you prefer to send the path *and* the contents, there is a setting for that.

---

## Why you will like it

- **Zero window juggling.** No `Alt+Tab`, no hunting for the input box.
- **A real shortcut, not a menu hunt.** `Ctrl+Alt+Shift+O` works anywhere in the IDE, including the code editor.
- **Any recipient.** OpenCode by default, or any window you point the `Target application` setting at — VS Code, a browser, a terminal, Notepad.
- **Context‑aware payloads.** A fragment stays a fragment; a file becomes a path.
- **Autostart.** Closed OpenCode? It opens, waits for the window, and pastes.
- **Careful pasting.** The window is activated first, and `Ctrl+V` is only pressed after verifying that the window is really in front. Text never lands inside Visual Studio itself.
- **Works in your language.** The command is called *Paste into all* in English Visual Studio and *Вставить в приложение* in Russian Visual Studio; the options page (setting names, descriptions and the “Вставка”/“Содержимое” groups) and the Extensions Manager entry are localized too.
- **No network, no telemetry.** The extension never talks to the internet. It moves text between two applications on your machine and nothing else.

---

## Settings

**Tools → Options → Paste into all → General**

| Setting | Default | What it does |
| --- | --- | --- |
| Paste delay (ms) | `350` | Pause after the target window is activated, before `Ctrl+V`. Raise it on slower machines if text occasionally does not arrive. |
| Start OpenCode when it is not running | On | Launch OpenCode automatically when no window is found. |
| Startup timeout (seconds) | `30` | How long to wait for the window after launch. |
| OpenCode executable path | *(empty)* | Optional explicit path to `OpenCode.exe`. Empty means auto‑detect. |
| Target application (process or window title) | `opencode` | Where to paste: a process name (`opencode`, `chrome`, `Code`, `notepad`) or a fragment of a window title. The application must **already be open**. Empty or `opencode` means OpenCode. |
| Send the full file path when the whole file is selected | On | Whole file → path only. |
| Also append the file contents for a whole file | Off | Paste the path followed by the code. |
| Treat an empty selection as the whole file | On | No selection → behaves like the whole file was selected. |

You can rebind the command at **Tools → Options → Environment → Keyboard** (command `Tools.PasteIntoAll`). If the shortcut ever goes missing — for example after importing settings from another machine — the extension restores `Ctrl+Alt+Shift+O` on startup.

---

## Requirements

- **Visual Studio 2026** (version 18.0 or later), Community, Professional or Enterprise, 64‑bit.
- **.NET Framework 4.7.2** or later.
- The **OpenCode** desktop application (only needed when you paste into OpenCode). If it lives in a non‑standard folder, point the extension at it in the settings.
- A keyboard layout that can produce `O` — the shortcut is registered against the physical `O` key, so it keeps working under a Cyrillic layout too.

---

## Privacy

Paste into all has no network access, no analytics, no telemetry and no background services. The only data it handles is the text you explicitly paste, and it goes from the Visual Studio editor to the target window through the standard Windows clipboard. Diagnostics are written locally to `%TEMP%\paste-into-all.log` and never leave your machine.
