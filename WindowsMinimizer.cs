using System;
using System.Runtime.InteropServices;

public static class WindowsMinimizer
{
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private const int WM_COMMAND = 0x0111;
    private const int MIN_ALL = 419;
    private const int MIN_ALL_UNDO = 416;

    public static void MinimizeAllWindows()
    {
        IntPtr lHwnd = FindWindow("Shell_TrayWnd", null);
        if (lHwnd != IntPtr.Zero)
            SendMessage(lHwnd, WM_COMMAND, (IntPtr)MIN_ALL, IntPtr.Zero);
    }

    public static void RestoreAllWindows()
    {
        IntPtr lHwnd = FindWindow("Shell_TrayWnd", null);
        if (lHwnd != IntPtr.Zero)
            SendMessage(lHwnd, WM_COMMAND, (IntPtr)MIN_ALL_UNDO, IntPtr.Zero);
    }
}
