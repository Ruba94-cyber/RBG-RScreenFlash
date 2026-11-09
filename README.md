<p align="center">
  <img src="icon_cyan.ico" width="112" alt="RBG - RScreenFlash official application icon" />
</p>

<h1 align="center">RBG - RScreenFlash</h1>

<p align="center">
  <strong>Fast screenshots for Windows — including selections across multiple monitors.</strong><br />
  Capture, save as PNG and copy to your clipboard in one straightforward workflow.
</p>

<p align="center">
  <a href="https://github.com/Ruba94-cyber/RBG-RScreenFlash/releases/latest"><strong>Download latest release</strong></a> ·
  <a href="#quick-start">Quick start</a> ·
  <a href="#keyboard-shortcuts">Shortcuts</a> ·
  <a href="CHANGELOG.md">What's new</a> ·
  <a href="#support-development">Donate</a>
</p>

<p align="center">
  <img alt="Windows 10/11" src="https://img.shields.io/badge/Windows-10%20%2F%2011-263238?logo=windows&logoColor=white" />
  <img alt=".NET Framework 4.8" src="https://img.shields.io/badge/.NET_Framework-4.8-512BD4" />
  <img alt="Output PNG" src="https://img.shields.io/badge/Output-PNG%20%2B%20clipboard-24786E" />
  <img alt="MIT license" src="https://img.shields.io/badge/License-MIT-4F6573" />
  <img alt="Latest release" src="https://img.shields.io/github/v/release/Ruba94-cyber/RBG-RScreenFlash?label=release" />
</p>

## Why RScreenFlash?

**RBG - RScreenFlash** is designed to take screenshots with as little friction as possible — even if your workspace spans several monitors with different resolutions or scaling.

Use it for **documentation, issue reports, presentations and sharing what is on your screen**, without manually merging screenshots from different displays.

| Capability | What you get |
| --- | --- |
| **Instant monitor capture** | Capture the monitor currently under the mouse pointer. |
| **Cross-monitor selection** | Drag over multiple displays and save the selected area as **one image**. |
| **Automatic PNG save** | Screenshots are saved locally without opening an editor. |
| **Clipboard copy** | Paste the captured image directly into another application. |
| **Precise selection** | Optional pixel magnifier helps with careful area selection. |
| **Visual feedback** | A short cyan outline confirms capture, without becoming part of the screenshot. |
| **Custom shortcuts** | Capture monitors or select regions without keeping the main UI open. |

## Download

**[Download RBG - RScreenFlash for Windows](https://github.com/Ruba94-cyber/RBG-RScreenFlash/releases/latest)**

1. Download the **Windows x64 ZIP** from the latest release.
2. Extract it to a folder you want to keep.
3. Launch **`RBG - RScreenFlash.exe`**.

**Requirements:** Windows 10/11 and **.NET Framework 4.8**. No installation wizard is required.

For authenticity and integrity, use the checksums and signing certificate information provided alongside the release. Verify the published fingerprint before trusting any signing certificate.

## Quick start

### Capture one monitor

1. Put your mouse cursor on the monitor you want.
2. Launch **`RBG - RScreenFlash.exe`** normally.
3. RScreenFlash saves a screenshot of that display as a PNG and copies it to the clipboard.

### Capture a region across several monitors

1. Hold **Shift** while launching the application.
2. Click and drag to select any rectangular region — including one that crosses monitor boundaries.
3. Release the mouse to capture the selected area as **one PNG**, not separate screenshots.
4. Press **Esc** if you want to cancel the selection.

RScreenFlash supports multi-monitor layouts with different display sizes and Windows scaling settings.

**Where is the screenshot?** Images are saved automatically in:

`%USERPROFILE%\Pictures\Screenshots\screen_[timestamp]_[id].png`

The captured image is also copied to the clipboard, ready to paste.

### Quick-reference controls

| Action | How |
| --- | --- |
| Capture a display | Launch normally, with the cursor on that monitor |
| Select an area, even across monitors | Hold **Shift** while launching, then drag |
| Cancel an area selection | Press **Esc** |
| Open settings | Hold **Ctrl + Shift** while launching |
| Adjust the capture feedback | Open Settings to change cyan-border duration |
| Enable pixel magnifier | Open Settings; magnifier is off by default |

## Keyboard shortcuts

Open Settings with **Ctrl + Shift** while launching, choose your shortcuts, then click **Save settings**.

| Suggested shortcut | Action |
| --- | --- |
| `Ctrl + Alt + S` | Capture the monitor under the cursor |
| `Ctrl + Alt + A` | Start area selection |

> [!NOTE]
> Shortcuts are customizable and are **not enabled until you save them**. A small windowless helper starts with Windows to keep them available between captures. Clear both assignments and save to turn it off. After moving the executable or extracting a newer release elsewhere, save your shortcuts again to refresh their target location.

## Tips & troubleshooting

- To capture across two screens, start the **Shift** selection and drag across their shared boundary.
- For accurate selections, enable the **pixel magnifier** in Settings.
- The **cyan border** is only visual confirmation; it is not baked into the PNG.
- The duration of the border animation is adjustable.
- If a saved shortcut opens an old version, re-save the shortcuts from the new executable.
- If pasting fails in a destination app, use the PNG already saved in the screenshots folder.

## Build from source

RScreenFlash is a **.NET Framework 4.8** Windows desktop project. A compatible Visual Studio / MSBuild installation and targeting pack are required.

From the repository root, the Windows build-check script is:

```powershell
.\tools\build-check.ps1
```

Shortcut and settings tests are available in `tests/`. Some UI-related checks require a Windows desktop session. See the [changelog](CHANGELOG.md) for recent fixes and features.

## Feedback & contributions

Suggestions and bug reports are welcome via **[GitHub Issues](https://github.com/Ruba94-cyber/RBG-RScreenFlash/issues)**. Include your Windows version, app release and steps to reproduce; avoid uploading private screenshots.

## Support development

RScreenFlash is independently developed. If it makes your everyday work easier, you can support continued development, maintenance and testing with an optional donation.

**[❤️ Support RScreenFlash with a PayPal donation](https://www.paypal.com/donate?business=paolo.ruba.1913%40gmail.com&no_recurring=0&item_name=Support+RScreenFlash&currency_code=EUR)**

## License

Released under the **[MIT License](LICENSE)**. Copyright © Paolo Rubagotti. See the license file for the full terms.

---

<p align="center"><sub>Made by Paolo Rubagotti · <a href="https://github.com/Ruba94-cyber/RBG-RScreenFlash/releases/latest">Download</a> · <a href="https://github.com/Ruba94-cyber/RBG-RScreenFlash/issues">Feedback</a> · <a href="#support-development">Support the project</a></sub></p>
