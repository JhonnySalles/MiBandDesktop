using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace MiBandDesktop.App.Services;

public static class WindowIconHelper
{
    private const int WM_SETICON = 0x0080;
    private const int ICON_SMALL = 0;
    private const int ICON_BIG = 1;
    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;
    private const uint LR_DEFAULTSIZE = 0x00000040;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static void SetWindowIcon(Window window, string iconFileName = "AppIcon.ico")
    {
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", iconFileName);
            if (!File.Exists(iconPath))
            {
                iconPath = Path.Combine(AppContext.BaseDirectory, iconFileName);
            }

            if (!File.Exists(iconPath)) return;

            // 1. Define via AppWindow (WinUI 3 padrão)
            var appWindow = window.AppWindow;
            if (appWindow != null)
            {
                appWindow.SetIcon(iconPath);
            }

            // 2. Define diretamente na Janela Win32 (Garante exibição na barra de tarefas do Windows e Alt+Tab)
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            if (hwnd != IntPtr.Zero)
            {
                IntPtr hSmallIcon = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 16, 16, LR_LOADFROMFILE);
                if (hSmallIcon != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, hSmallIcon);
                }

                IntPtr hBigIcon = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 32, 32, LR_LOADFROMFILE);
                if (hBigIcon != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG, hBigIcon);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WindowIconHelper] Erro ao definir ícone da janela: {ex.Message}");
        }
    }
}

