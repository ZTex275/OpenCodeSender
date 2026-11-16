# OpenCode Sender

**Send code from Visual Studio straight to the OpenCode chat — one keystroke, no copy‑paste gymnastics.**

You are debugging a method in Visual Studio. You want to ask OpenCode about it. Normally that means: select the code, press `Ctrl+C`, switch windows, click the chat box, press `Ctrl+V`, switch back. Every single time.

OpenCode Sender collapses that into **`Ctrl+Alt+Shift+O`**.

---

## How it works

1. Select a fragment of code in the Visual Studio editor.
2. Press **`Ctrl+Alt+Shift+O`** — or use **Tools → Send to OpenCode**, or right‑click in the editor and pick **Send to OpenCode**.
3. The fragment lands in the OpenCode chat input, ready for your question.

OpenCode is brought to the front automatically. If it is not running, it is started for you and the text is delivered as soon as the window appears.

### Fragments vs. whole files

The extension is deliberately clever about *what* it sends:

| What you select | What OpenCode receives |
| --- | --- |
| A fragment of code | The fragment itself, verbatim — no path noise around it |
| The whole file, or nothing at all | Only the **full path** to the file, e.g. `D:\Projects\App\Services\UserService.cs` |

Sending a path instead of a thousand lines of code keeps the chat light and lets OpenCode open the file itself. If you prefer to send the path *and* the contents, there is a setting for that.

---

## Why you will like it

- **Zero window juggling.** No `Alt+Tab`, no hunting for the chat box.
- **A real shortcut, not a menu hunt.** `Ctrl+Alt+Shift+O` works anywhere in the IDE, including the code editor.
- **Context‑aware payloads.** A fragment stays a fragment; a file becomes a path.
- **Autostart.** Closed OpenCode? It opens, waits for the window, and pastes.
- **Safe by design.** Text is only pasted into a window that actually belongs to OpenCode. Visual Studio, browsers and Explorer are explicitly excluded, even if their titles happen to match.
- **Works in your language.** The command is called *Send to OpenCode* in English Visual Studio and *Отправить в OpenCode* in Russian Visual Studio, with a localized Extensions Manager entry.
- **No network, no telemetry.** The extension never talks to the internet. It moves text between two applications on your machine and nothing else.

---

## Settings

**Tools → Options → OpenCode Sender → General**

| Setting | Default | What it does |
| --- | --- | --- |
| Paste delay (ms) | `350` | Pause after the OpenCode window is activated, before `Ctrl+V`. Raise it on slower machines if text occasionally does not arrive. |
| Start OpenCode when it is not running | On | Launch OpenCode automatically when no window is found. |
| Startup timeout (seconds) | `30` | How long to wait for the window after launch. |
| OpenCode executable path | *(empty)* | Optional explicit path to `OpenCode.exe`. Empty means auto‑detect. |
| OpenCode window title filter | `opencode` | Fallback search for an OpenCode instance running inside a terminal. Only terminal windows are searched. |
| Send the full file path when the whole file is selected | On | Whole file → path only. |
| Also append the file contents for a whole file | Off | Send the path followed by the code. |
| Treat an empty selection as the whole file | On | No selection → behaves like the whole file was selected. |

You can rebind the command at **Tools → Options → Environment → Keyboard** (command `Tools.SendtoOpenCode`). If the shortcut ever goes missing — for example after importing settings from another machine — the extension restores `Ctrl+Alt+Shift+O` on startup.

---

## Requirements

- **Visual Studio 2026** (version 18.0 or later), Community, Professional or Enterprise, 64‑bit.
- **.NET Framework 4.7.2** or later.
- The **OpenCode** desktop application. If it lives in a non‑standard folder, point the extension at it in the settings.
- A keyboard layout that can produce `O` — the shortcut is registered against the physical `O` key, so it keeps working under a Cyrillic layout too.

---

## Privacy

OpenCode Sender has no network access, no analytics, no telemetry and no background services. The only data it handles is the text you explicitly send, and it goes from the Visual Studio editor to the OpenCode window through the standard Windows clipboard. Diagnostics are written locally to `%TEMP%\opencode-sender.log` and never leave your machine.
