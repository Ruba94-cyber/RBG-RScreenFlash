# RBG - RScreenFlash

**Fast screenshots across multiple monitors.** Capture any individual display or select an area spanning two or more screens, automatically saved as one PNG and copied to your clipboard.

## Download

[Download the latest release](https://github.com/Ruba94-cyber/RBG-RScreenFlash/releases/latest), extract the ZIP, and run `RBG - RScreenFlash.exe`.

Requires Windows 10/11 and .NET Framework 4.8. No installation wizard is needed.

## Features

- **Multi-monitor capture:** take a screenshot of any connected display or select a region crossing multiple screens **in a single image**.
- **One-click capture:** automatically captures the monitor under your mouse pointer.
- **Flexible area selection:** click and drag to capture exactly what you need, across different monitor layouts and resolutions.
- **Automatic saving and copying:** saves a PNG locally and copies the image to your clipboard for immediate pasting.
- **Cyan border feedback:** a brief outline confirms the capture without appearing in the screenshot.
- **Optional magnifier:** zoom in on pixels while selecting an area.
- **Custom shortcuts:** capture without keeping the main application open.

## Multi-monitor capture

With two or more monitors, **move your cursor onto the screen you want to capture** and launch normally to screenshot that display. To capture content **across several monitors at once**, hold **Shift** while launching and drag a selection across the screen boundaries. The entire selected region is saved as **one PNG**, not separate screenshots. Different monitor sizes and Windows display scaling are supported.

## How to use

| Action | What to do |
| --- | --- |
| **Capture any monitor** | Move the mouse onto that monitor, then open `RBG - RScreenFlash.exe`. |
| **Capture an area across screens** | Hold **Shift** while opening the executable, then drag across one or more monitors. |
| **Cancel area selection** | Press **Esc**. |
| **Open Settings** | Hold **Ctrl + Shift** while opening the executable. |

Screenshots are saved automatically to `%USERPROFILE%\Pictures\Screenshots\` as `screen_[timestamp]_[id].png` and copied to the clipboard.

### Optional keyboard shortcuts

Open Settings, assign your preferred keys, and select **Save settings** to activate them. Suggested combinations:

| Shortcut | Action |
| --- | --- |
| `Ctrl + Alt + S` | Capture a monitor |
| `Ctrl + Alt + A` | Select an area |

Shortcuts **are not enabled until you save them**. A small background helper keeps them available while the main app is closed and starts with Windows. Clear both assignments and save to disable it. If you move the executable, save your shortcuts again.

Settings also lets you enable the **pixel magnifier** (off by default) and adjust how long the cyan border appears.

## Support the project

Enjoying RScreenFlash? [Support its development with a PayPal donation](https://www.paypal.com/donate?business=paolo.ruba.1913%40gmail.com&no_recurring=0&item_name=Support+RScreenFlash&currency_code=EUR).
