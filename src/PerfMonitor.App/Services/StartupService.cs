using Microsoft.Win32;

namespace PerfMonitor.App.Services;

public class StartupService
{
    private const string StartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "WindowsPerfMonitor";

    /// <summary>
    /// Enable or disable auto-start with Windows.
    /// </summary>
    public static void SetAutoStart(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupKey, writable: true);
        if (key is null) return;

        if (enable)
        {
            var exePath = Environment.ProcessPath;
            if (exePath is not null)
            {
                key.SetValue(AppName, $"\"{exePath}\"");
            }
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }

    /// <summary>
    /// Check whether auto-start is currently enabled.
    /// </summary>
    public static bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupKey, writable: false);
        return key?.GetValue(AppName) is not null;
    }
}
