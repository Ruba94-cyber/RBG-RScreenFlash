# Changelog

## 1.2.0 — configurable shortcuts and visual selection settings

- Add global shortcuts for screen capture and area selection via per-user Windows Explorer Start Menu links, with no resident process.
- Detect collisions with other RBG utility shortcuts, verify and restore link changes after errors.
- Add lightweight Ctrl+Shift settings for cyan border fade duration and an opt-in area-selection pixel magnifier.
- Replace the abrupt cyan border flash with a configurable smooth fade; screenshot capture and clipboard saving still finish first.
- Add isolated Windows shortcut and preference round-trip tests to the Windows build workflow.


## 1.0.1

- Shortened the capture feedback animation and reused the existing STA UI thread.
- Save screenshots through atomic, collision-safe PNG files before retrying clipboard access.
- Expanded the local benchmark to cover standard capture, varied Shift selections, Escape cancellation, PNG dimensions, and clipboard availability.
- Added baseline comparison notes and verified Release x64 build instructions.
- Embedded Paolo Rubagotti and `paolo.ruba.1913@gmail.com` in the executable's Windows version metadata.
- Renamed the Windows product and executable to `RBG - RScreenFlash` and prepared the x64 release package with a SHA-256 checksum.
