using System;
using System.Diagnostics;
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
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: ShortcutSettingsUiSmoke <RBG - RScreenFlash.exe>");
            return 2;
        }

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

            if (success && Directory.Exists(testRoot))
                Directory.Delete(testRoot, true);
            else
                Console.Error.WriteLine("Settings UI test data retained at: " + testRoot);
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
