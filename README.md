# RBG - RScreenFlash

RBG - RScreenFlash is a fast, lightweight screenshot utility for Windows designed to deliver professional captures with zero fuss.

Windows release executables are Authenticode-signed with Paolo Rubagotti's self-signed code-signing certificate. Each release includes the public `.cer` and its SHA-256 fingerprint; the private `.pfx` is never distributed. Because this certificate is self-signed rather than issued by a public CA, Windows can still show an unknown-publisher or SmartScreen warning until the user chooses to trust it.

## Key Features

✅ **Instant capture** – Grab the entire screen with a single click.

✅ **Custom selection** – Hold **Shift** while launching to draw a capture region.

✅ **Visual feedback** – A cyan flash outlines the active monitor so you know the shot succeeded.

✅ **Automatic saving** – Screenshots are saved to disk and copied to the clipboard automatically.

✅ **Multi-monitor aware** – Works seamlessly across multiple displays.

✅ **Minimal footprint** – Small, responsive UI that stays out of your way.

## How to Use RBG - RScreenFlash

### Full-screen capture
1. Launch `RBG - RScreenFlash.exe`.
2. The active monitor briefly highlights with a cyan border.
3. The screenshot is saved and copied to the clipboard automatically.

### Custom area capture
1. Hold **Shift** while starting `RBG - RScreenFlash.exe`.
2. A transparent overlay covers all connected monitors.
3. Click and drag to select the area you want to capture.
4. The selected region is saved and placed on the clipboard.

## Configurable keyboard shortcuts

Hold **Ctrl+Shift while launching the executable** to open Shortcut Settings.
Assign separate shortcuts to **full-screen capture** and **area selection**.
Suggested combinations are `Ctrl+Alt+S` and `Ctrl+Alt+A`; click **Save settings** to enable them.

- Click a shortcut field and press **Ctrl+Alt+letter** or **Ctrl+Alt+F1–F12** (optionally with Shift). Delete clears an assignment.
- The app saves per-user shortcuts to the Start Menu's `RBG Utilities` folder. A small windowless shortcut helper stays active and starts again at Windows sign-in, so the keys work while the capture app is closed. It handles only the configured combinations and does not save typed keys. The Start Menu links remain available if the helper is stopped. Clear both shortcuts and save to stop the helper and remove its Startup link. Reserved or conflicting combinations may still be unavailable.
- If the executable is moved, launch it with Ctrl+Shift again and save shortcuts to update the link targets.
- Settings uses text-sized fields and a scrollable body; Save and Cancel remain visible when the window is resized.
- Direct launching remains unchanged: ordinary launch captures the monitor, Shift+launch selects an area, Ctrl+Shift+launch opens settings.
- The cyan **border** fades in and out smoothly after capture, without delaying screenshot saving or clipboard copying. Its duration is adjustable from 120 to 500 ms (180 ms default).

### Optional pixel-precision loupe

Enable **Show optional pixel magnifier when selecting an area** in Ctrl+Shift settings. The loupe previews nearby pixels at higher magnification while dragging a selection. It is disabled by default, so normal screenshots do not allocate an extra desktop bitmap. Its cached backdrop is discarded as soon as selection ends or is canceled.

### Release download

Download `RBG-RScreenFlash-1.2.3-windows-x64.zip` from GitHub Releases, extract the archive, then run `RBG - RScreenFlash.exe`. Open Settings from this new executable and save once to update shortcuts that still point to an older extracted version.

The archive contains `Paolo-Rubagotti-CodeSigning.cer` and `CERTIFICATE-FINGERPRINT-SHA256.txt`. On a PC you administer, import the `.cer` into **Trusted Root Certification Authorities** and **Trusted Publishers** before running the app to have Windows recognize this specific signer. Verify the fingerprint against the one published with the release before trusting it. SmartScreen reputation warnings can remain even after importing a self-signed certificate.

## File Organization

Screenshots are stored in the following directory:

```
%USERPROFILE%\Pictures\Screenshots
```

Files use the naming pattern:

```
screen_<timestamp>_<random-id>.png
```

Example: `screen_2026-10-22_16-00-00-123_a1b2c3d4.png`

## Performance and privacy verification

This utility captures the currently visible screen and saves the image locally.
Test it only with non-sensitive content visible. It does not upload captures.

Before merging performance changes, verify on Windows 10/11 with .NET Framework 4.8:

1. Build the Release x64 configuration.
2. Display a neutral test window without personal or confidential information.
3. Launch normally, confirm the cyan outline disappears before capture, and verify the PNG and clipboard image.
4. Repeat several times, comparing the first launch with subsequent launches.
5. Hold Shift while launching, select a region, and confirm the selected region is saved.
6. Cancel selection with Escape and confirm no screenshot is saved.
7. Repeat with two monitors and different display scaling settings.
8. Temporarily occupy the clipboard with a separate test process; confirm a PNG is still saved.
9. Test a screenshot folder with many existing files; ensure new filenames are unique.

No screenshots or machine-specific logs need to be committed to this repository.

## Measuring first-launch latency

Build the application in Release mode on Windows, then open Windows PowerShell 5.1
from the repository root and run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Measure-CaptureStartup.ps1 -Executable ".\bin\x64\Release\RBG - RScreenFlash.exe" -Runs 5 -ShiftRuns 5 -CancelRuns 1 -CsvPath .\capture-times.csv
```

The harness displays a synthetic test backdrop, measures standard and Shift
selection captures, and verifies the PNG dimensions, successful exit, and
clipboard image. Shift runs use varied selection sizes, including portrait,
panoramic, 4:3, and odd dimensions. Cancel runs send Escape and verify that no
PNG or clipboard update is produced and that the process exits.

The optional CSV contains timing metadata only; screenshots are stored in a
temporary test folder and removed when the run finishes. If a fast baseline
process exits between polling intervals, its measured capture/save/clipboard
times use process exit as an upper bound, with the output file and clipboard
checked after exit. The first run may include operating-system and .NET
cold-start effects. Compare baseline and optimized builds under the same
conditions before drawing conclusions.

The FlaUI settings smoke project, `tests/RScreenFlash.SettingsUiAutomation.csproj`, checks opening Settings, readable inputs and buttons at normal and compact sizes, saving and reopening shortcut assignments, the default fade duration and supported boundaries, magnifier toggling, and preference persistence. `tests/Test-Shortcuts.ps1` covers shortcut normalization, conflict handling, clearing assignments, link paths, and key-capture behavior. Shortcut helper restart after Windows sign-in still requires manual acceptance.

## Why Choose RScreenFlash?

- **Speed** – Capture instantly without navigating menus.
- **Convenience** – Every shot is immediately available in the clipboard.
- **Precision** – Draw pixel-perfect selection rectangles.
- **Reliability** – Compatible with Windows 10 and Windows 11.
- **Zero setup** – Ready to use as soon as it is installed.

## Ideal Use Cases

- **Professionals** – Technical documentation and presentations.
- **Content creators** – Tutorials, walkthroughs, and guides.
- **Developers** – Reproduce UI bugs and document features.
- **Students** – Save references and study materials quickly.
- **Business teams** – Share information across the organization.


## System Requirements

- Windows 10 or Windows 11
- .NET Framework 4.8 (typically preinstalled)

## Executable details

The Windows file properties identify Paolo Rubagotti as the author and include the contact email `paolo.ruba.1913@gmail.com`.

## Support the Project

If RScreenFlash helps you work faster, you can buy me a coffee by sending a PayPal donation through the [direct contribution page](https://www.paypal.com/donate?business=paolo.ruba.1913%40gmail.com&no_recurring=0&item_name=Support+RScreenFlash&currency_code=EUR).

---

**RScreenFlash** – The fastest way to capture polished screenshots.
