using System;
using System.Runtime.InteropServices;

namespace RotaryMonitor
{
    /// <summary>System tray icon (Shell_NotifyIcon) with a right-click context menu.</summary>
    public sealed class TrayIcon : IDisposable
    {
        private const int WM_TRAYICON = 0x8000 + 1;   // WM_APP + 1
        private const int NIM_ADD = 0x00000000;
        private const int NIM_DELETE = 0x00000002;
        private const int NIF_MESSAGE = 0x00000001;
        private const int NIF_ICON = 0x00000002;
        private const int NIF_TIP = 0x00000004;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONUP = 0x0205;
        private const uint MF_STRING = 0x00000000;
        private const uint MF_SEPARATOR = 0x00000800;
        private const uint TPM_RIGHTBUTTON = 0x00000002;
        private const uint TPM_RETURNCMD = 0x00000100;
        private const uint TPM_LEFTALIGN = 0x00000000;

        public event Action? LeftClicked;
        public event Action? ShowRequested;
        public event Action? ExitRequested;

        private IntPtr _hwnd;
        private NativeMethods.WndProcDelegate _proc;

        public TrayIcon(IntPtr icon, string tooltip)
        {
            _proc = WndProc;
            _hwnd = NativeMethods.CreateMessageWindow(_proc);

            var nid = new NativeMethods.NOTIFYICONDATA();
            nid.cbSize = Marshal.SizeOf(typeof(NativeMethods.NOTIFYICONDATA));
            nid.hWnd = _hwnd;
            nid.uID = 1;
            nid.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
            nid.uCallbackMessage = WM_TRAYICON;
            nid.hIcon = icon;
            nid.szTip = tooltip;
            NativeMethods.Shell_NotifyIcon(NIM_ADD, ref nid);
        }

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_TRAYICON)
            {
                int l = lParam.ToInt32();
                if (l == WM_LBUTTONUP)
                    LeftClicked?.Invoke();
                else if (l == WM_RBUTTONUP)
                    ShowMenu(hWnd);
                return IntPtr.Zero;
            }
            return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void ShowMenu(IntPtr hWnd)
        {
            IntPtr menu = NativeMethods.CreatePopupMenu();
            uint idShow = 1;
            uint idExit = 2;
            NativeMethods.AppendMenu(menu, MF_STRING, idShow, L.Get("TrayShow"));
            NativeMethods.AppendMenu(menu, MF_SEPARATOR, 0, null);
            NativeMethods.AppendMenu(menu, MF_STRING, idExit, L.Get("TrayExit"));

            NativeMethods.GetCursorPos(out NativeMethods.POINT pt);
            NativeMethods.SetForegroundWindow(hWnd);
            uint cmd = NativeMethods.TrackPopupMenu(menu, TPM_LEFTALIGN | TPM_RIGHTBUTTON | TPM_RETURNCMD,
                pt.X, pt.Y, 0, hWnd, IntPtr.Zero);
            NativeMethods.DestroyMenu(menu);

            if (cmd == idShow) ShowRequested?.Invoke();
            else if (cmd == idExit) ExitRequested?.Invoke();
        }

        public void Dispose()
        {
            var nid = new NativeMethods.NOTIFYICONDATA();
            nid.cbSize = Marshal.SizeOf(typeof(NativeMethods.NOTIFYICONDATA));
            nid.hWnd = _hwnd;
            nid.uID = 1;
            NativeMethods.Shell_NotifyIcon(NIM_DELETE, ref nid);
            NativeMethods.DestroyWindow(_hwnd);
        }
    }
}
