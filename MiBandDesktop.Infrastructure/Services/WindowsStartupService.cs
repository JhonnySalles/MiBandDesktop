using System;
using System.IO;
using Microsoft.Win32;

namespace MiBandDesktop.Infrastructure.Services;

public class WindowsStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "MiBandDesktop";

    public static bool IsStartupEnabled()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static void SetStartup(bool enable, bool startMinimized = true)
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "MiBandDesktop.App.exe");
                var cmd = startMinimized ? $"\"{exePath}\" --minimized" : $"\"{exePath}\"";
                key.SetValue(AppName, cmd);
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WindowsStartupService] Erro ao alterar registro: {ex.Message}");
        }
    }
}

