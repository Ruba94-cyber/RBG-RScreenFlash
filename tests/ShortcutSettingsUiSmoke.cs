using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using FlaButton = FlaUI.Core.AutomationElements.Button;
using FlaCheckBox = FlaUI.Core.AutomationElements.CheckBox;
using FlaSpinner = FlaUI.Core.AutomationElements.Spinner;
using FlaTextBox = FlaUI.Core.AutomationElements.TextBox;

internal static class ShortcutSettingsUiSmoke
{
    private const string SettingsTitle = "RBG RScreenFlash - Settings";

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[1] == "--save-installed")
        {
            try { SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch (EntryPointNotFoundException) { }
            return SaveInstalled(Path.GetFullPath(args[0]));
        }
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: ShortcutSettingsUiSmoke <RBG - RScreenFlash.exe>");
            return 2;
        }

        try { SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch (EntryPointNotFoundException) { }
        string executable = Path.GetFullPath(args[0]);
        string testRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "settings-data-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        bool success = false;
        Process activeApp = null;

        try
        {
            using (var automation = new UIA3Automation())
            {
                var desktop = automation.GetDesktop();
                activeApp = Launch(executable, testRoot, true);
                Window settings = WaitForWindow(desktop, activeApp.Id, TimeSpan.FromSeconds(10));
                VerifySettingsLayout(settings);
                string[] launchedShortcuts = GetShortcutInputs(settings).Select(input => input.Text).ToArray();
                if (!HasHotkey(launchedShortcuts, "Ctrl+Alt+S") || !HasHotkey(launchedShortcuts, "Ctrl+Alt+A"))
                    throw new InvalidOperationException("Ctrl+Shift launch did not show the default Flash shortcuts.");
                ClickButton(settings, "Cancel");
                WaitForExit(activeApp, "Closing Ctrl+Shift settings launch");
                activeApp.Dispose();
                activeApp = null;

                activeApp = Launch(executable, testRoot, false);
                settings = WaitForWindow(desktop, activeApp.Id, TimeSpan.FromSeconds(10));
                settings.Focus();
                FlaTextBox[] inputs = GetShortcutInputs(settings);
                if (inputs.Length != 2)
                    throw new InvalidOperationException("Expected two Flash shortcut inputs, found " + inputs.Length + ".");
                if (!HasHotkey(inputs.Select(input => input.Text).ToArray(), "Ctrl+Alt+S") ||
                    !HasHotkey(inputs.Select(input => input.Text).ToArray(), "Ctrl+Alt+A"))
                    throw new InvalidOperationException("Settings did not show the default screen and area shortcuts.");

                FlaSpinner fade = FindFadeSpinner(settings);
                FlaCheckBox magnifier = FindMagnifier(settings);
                if (Math.Abs(fade.Value - 180) > 0.01 || magnifier.IsChecked == true)
                    throw new InvalidOperationException("Flash visual settings did not start at 180 ms with magnifier OFF.");

                fade.Value = 320;
                magnifier.IsChecked = true;
                ClickButton(settings, "Save settings");
                WaitForExit(activeApp, "Saving Flash settings");
                activeApp.Dispose();
                activeApp = null;

                VerifySavedShortcuts(testRoot, "Ctrl+Alt+S", "Ctrl+Alt+A");
                VerifyPreferences(testRoot, "border-fade-ms=320", "selection-magnifier=1");

                activeApp = Launch(executable, testRoot, false);
                settings = WaitForWindow(desktop, activeApp.Id, TimeSpan.FromSeconds(10));
                if (!HasHotkey(GetShortcutInputs(settings).Select(input => input.Text).ToArray(), "Ctrl+Alt+S") ||
                    !HasHotkey(GetShortcutInputs(settings).Select(input => input.Text).ToArray(), "Ctrl+Alt+A"))
                    throw new InvalidOperationException("The default shortcuts did not reload in Settings.");
                fade = FindFadeSpinner(settings);
                magnifier = FindMagnifier(settings);
                if (Math.Abs(fade.Value - 320) > 0.01 || magnifier.IsChecked != true)
                    throw new InvalidOperationException("Saved Flash visual settings did not reload.");

                fade.Value = 500;
                if (Math.Abs(fade.Value - 500) > 0.01)
                    throw new InvalidOperationException("The 500 ms fade maximum was rejected.");
                fade.Value = 120;
                if (Math.Abs(fade.Value - 120) > 0.01)
                    throw new InvalidOperationException("The 120 ms fade minimum was rejected.");
                magnifier.IsChecked = false;
                ClickButton(settings, "Save settings");
                WaitForExit(activeApp, "Saving reset Flash visual settings");
                activeApp.Dispose();
                activeApp = null;

                VerifyPreferences(testRoot, "border-fade-ms=120", "selection-magnifier=0");
                VerifySavedShortcuts(testRoot, "Ctrl+Alt+S", "Ctrl+Alt+A");
            }

            success = true;
            Console.WriteLine("FlaUI Flash settings PASS: Ctrl+Shift launch, settings save/reopen, default shortcut values and .lnk arguments, fade 180/120/500 ms, magnifier toggle, and persistence.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            if (activeApp != null)
            {
                try
                {
                    if (!activeApp.HasExited)
                    {
                        activeApp.Kill();
                        activeApp.WaitForExit(3000);
                    }
                }
                catch { }
                activeApp.Dispose();
            }

            if (Directory.Exists(testRoot))
                foreach (string link in Directory.GetFiles(testRoot, "*.lnk", SearchOption.AllDirectories))
                    File.Move(link, link + ".test-artifact");
            if (success && Directory.Exists(testRoot))
                Directory.Delete(testRoot, true);
            else
                Console.Error.WriteLine("Settings UI test data retained at: " + testRoot);
        }
    }

    [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr window, IntPtr dc, uint flags);
    [DllImport("user32.dll")] private static extern bool MoveWindow(IntPtr window, int x, int y, int width, int height, bool repaint);

    private static int SaveInstalled(string executable)
    {
        Process app = null;
        try
        {
            var start = new ProcessStartInfo(executable, "--settings") { UseShellExecute = false };
            start.EnvironmentVariables.Remove("RBG_SHORTCUT_TEST_ROOT");
            start.EnvironmentVariables.Remove("RBG_VISUAL_SETTINGS_TEST_ROOT");
            app = Process.Start(start);
            using (var automation = new UIA3Automation())
            {
                Window settings = WaitForWindow(automation.GetDesktop(), app.Id, TimeSpan.FromSeconds(10));
                VerifySettingsLayout(settings);
                string before = string.Join(" / ", GetShortcutInputs(settings).Select(input => input.Text));
                ClickButton(settings, "Save settings");
                if (!app.WaitForExit(10000) || app.ExitCode != 0)
                    throw new InvalidOperationException("Saving the installed shortcut registration failed.");
                Console.WriteLine("installed_shortcuts=refreshed; unchanged values=" + before);
            }
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally
        {
            if (app != null)
            {
                try { if (!app.HasExited) { app.Kill(); app.WaitForExit(3000); } } catch { }
                app.Dispose();
            }
        }
    }

    private static void VerifySettingsLayout(Window settings)
    {
        int expected = SettingsTitle.Contains("RScreenRec") ? 3 : 2;
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        FlaTextBox[] inputs;
        do
        {
            inputs = GetShortcutInputs(settings);
            if (inputs.Length == expected && inputs.All(input => input.BoundingRectangle.Height > 0)) break;
            Thread.Sleep(100);
        } while (DateTime.UtcNow < deadline);
        if (inputs.Length != expected) throw new InvalidOperationException("Settings inputs were not ready.");
        Rectangle window = settings.BoundingRectangle;
        IntPtr edit = inputs[0].Properties.NativeWindowHandle.Value;
        using (Font font = Font.FromHfont(SendMessage(edit, 0x31, IntPtr.Zero, IntPtr.Zero)))
        using (Graphics graphics = Graphics.FromHwnd(edit))
        {
            int textHeight = (int)Math.Ceiling(font.GetHeight(graphics));
            foreach (var input in inputs)
            {
                Rectangle rect = input.BoundingRectangle;
                if (rect.Height < textHeight || !window.Contains(rect))
                    throw new InvalidOperationException("Settings shortcut input is clipped.");
            }
            for (int i = 1; i < inputs.Length; i++)
                if (inputs[i - 1].BoundingRectangle.Bottom > inputs[i].BoundingRectangle.Top)
                    throw new InvalidOperationException("Settings shortcut inputs overlap.");
            foreach (string caption in new[] { "Save settings", "Cancel" })
            {
                var button = settings.FindFirstDescendant(cf => cf.ByName(caption).And(cf.ByControlType(ControlType.Button)));
                if (button == null || button.Properties.IsOffscreen.Value)
                    throw new InvalidOperationException("Settings action is not visible: " + caption);
                Rectangle rect = button.BoundingRectangle;
                Size text = System.Windows.Forms.TextRenderer.MeasureText(graphics, caption, font, Size.Empty,
                    System.Windows.Forms.TextFormatFlags.SingleLine | System.Windows.Forms.TextFormatFlags.NoPadding);
                if (!window.Contains(rect) || rect.Width < text.Width || rect.Height < text.Height)
                    throw new InvalidOperationException("Settings button text is clipped: " + caption);
            }
        }
        CaptureSettings(settings, "settings-preview.png");
        IntPtr handle = settings.Properties.NativeWindowHandle.Value;
        MoveWindow(handle, window.Left, window.Top, window.Width * 3 / 4, window.Height * 2 / 3, true);
        Thread.Sleep(200);
        Rectangle compact = settings.BoundingRectangle;
        if (compact.Width >= window.Width || compact.Height >= window.Height)
            throw new InvalidOperationException("The compact-settings test did not actually make the window smaller.");
        foreach (string caption in new[] { "Save settings", "Cancel" })
        {
            var button = settings.FindFirstDescendant(cf => cf.ByName(caption).And(cf.ByControlType(ControlType.Button)));
            if (button == null || button.Properties.IsOffscreen.Value || !compact.Contains(button.BoundingRectangle))
                throw new InvalidOperationException("Settings action disappeared when the window was made smaller.");
        }
        CaptureSettings(settings, "settings-compact-preview.png");
        MoveWindow(handle, window.Left, window.Top, window.Width, window.Height, true);
        Thread.Sleep(200);
        Console.WriteLine("settings_layout=readable; compact_footer=visible");
    }

    private static void CaptureSettings(Window settings, string name)
    {
        Rectangle rect = settings.BoundingRectangle;
        using (var bitmap = new Bitmap(rect.Width, rect.Height))
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            IntPtr dc = graphics.GetHdc();
            try
            {
                if (!PrintWindow(settings.Properties.NativeWindowHandle.Value, dc, 0))
                    throw new InvalidOperationException("Could not capture Settings.");
            }
            finally { graphics.ReleaseHdc(dc); }
            bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name));
        }
    }

    private static Process Launch(string executable, string testRoot, bool useCtrlShift)
    {
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(executable)
        };
        if (!useCtrlShift)
            start.Arguments = "--settings";
        start.EnvironmentVariables["RBG_SHORTCUT_TEST_ROOT"] = testRoot;
        start.EnvironmentVariables["RBG_VISUAL_SETTINGS_TEST_ROOT"] = testRoot;

        if (!useCtrlShift)
            return Process.Start(start);

        Keyboard.Press(VirtualKeyShort.LCONTROL);
        Keyboard.Press(VirtualKeyShort.LSHIFT);
        try
        {
            Process app = Process.Start(start);
            Thread.Sleep(300);
            return app;
        }
        finally
        {
            Keyboard.Release(VirtualKeyShort.LSHIFT);
            Keyboard.Release(VirtualKeyShort.LCONTROL);
        }
    }

    private static Window WaitForWindow(AutomationElement desktop, int processId, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            AutomationElement[] windows = desktop.FindAllChildren(cf => cf
                .ByName(SettingsTitle)
                .And(cf.ByControlType(ControlType.Window)));
            foreach (AutomationElement element in windows)
            {
                uint owner;
                GetWindowThreadProcessId(element.FrameworkAutomationElement.NativeWindowHandle.Value, out owner);
                if (owner == (uint)processId)
                    return element.AsWindow();
            }
            Thread.Sleep(100);
        }
        throw new TimeoutException("FlaUI did not find the Flash settings dialog for process " + processId + ".");
    }

    private static FlaTextBox[] GetShortcutInputs(Window settings)
    {
        return settings.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit))
            .Select(element => element.AsTextBox())
            .Where(input => input.Text.StartsWith("Ctrl+Alt+", StringComparison.OrdinalIgnoreCase) ||
                input.Text.StartsWith("Alt+Ctrl+", StringComparison.OrdinalIgnoreCase))
            .OrderBy(input => input.BoundingRectangle.Y)
            .ToArray();
    }

    private static FlaSpinner FindFadeSpinner(Window settings)
    {
        var element = settings.FindFirstDescendant(cf => cf.ByControlType(ControlType.Spinner));
        if (element == null)
            throw new InvalidOperationException("The cyan border fade spinner is missing.");
        return element.AsSpinner();
    }

    private static FlaCheckBox FindMagnifier(Window settings)
    {
        var element = settings.FindFirstDescendant(cf => cf
            .ByName("Show optional pixel magnifier when selecting an area")
            .And(cf.ByControlType(ControlType.CheckBox)));
        if (element == null)
            throw new InvalidOperationException("The optional magnifier checkbox is missing.");
        return element.AsCheckBox();
    }

    private static bool HasHotkey(string[] values, string expected)
    {
        return values.Any(value => Normalize(value) == Normalize(expected));
    }

    private static string Normalize(string hotkey)
    {
        return string.Join("+", hotkey.Split('+')
            .Select(part => part.Trim().ToUpperInvariant())
            .OrderBy(part => part, StringComparer.Ordinal));
    }

    private static void ClickButton(Window window, string name)
    {
        FlaButton button = window.FindFirstDescendant(cf => cf
            .ByName(name)
            .And(cf.ByControlType(ControlType.Button)))?.AsButton();
        if (button == null)
            throw new InvalidOperationException("Could not find button: " + name);
        button.Click();
    }

    private static void WaitForExit(Process app, string operation)
    {
        if (!app.WaitForExit(5000) || app.ExitCode != 0)
            throw new InvalidOperationException(operation + " did not close cleanly.");
    }

    private static void VerifyPreferences(string root, string fade, string magnifier)
    {
        string path = Path.Combine(root, "RBG", "RScreenFlash", "preferences.ini");
        string contents = File.ReadAllText(path);
        if (!contents.Contains(fade) || !contents.Contains(magnifier))
            throw new InvalidOperationException("Saved visual preferences do not match: " + contents);
    }

    private static void VerifySavedShortcuts(string root, string screenHotkey, string areaHotkey)
    {
        string[] links = Directory.GetFiles(root, "*.lnk", SearchOption.AllDirectories);
        if (links.Length != 2)
            throw new InvalidOperationException("Expected two isolated Flash shortcuts, found " + links.Length + ".");

        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null)
            throw new NotSupportedException("Windows Script Host is unavailable for shortcut verification.");
        object shell = Activator.CreateInstance(shellType);
        try
        {
            bool screenFound = false;
            bool areaFound = false;
            foreach (string path in links)
            {
                object shortcut = null;
                try
                {
                    shortcut = ((dynamic)shell).CreateShortcut(path);
                    string hotkey = (string)((dynamic)shortcut).Hotkey;
                    string arguments = (string)((dynamic)shortcut).Arguments;
                    screenFound |= Normalize(hotkey) == Normalize(screenHotkey) && arguments == "--capture-screen";
                    areaFound |= Normalize(hotkey) == Normalize(areaHotkey) && arguments == "--capture-area";
                }
                finally
                {
                    if (shortcut != null)
                        Marshal.ReleaseComObject(shortcut);
                }
            }
            if (!screenFound || !areaFound)
                throw new InvalidOperationException("Saved Flash shortcuts lost a key or mode argument.");
        }
        finally
        {
            Marshal.ReleaseComObject(shell);
        }
    }
}
