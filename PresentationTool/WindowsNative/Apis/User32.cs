using System;
using System.Runtime.InteropServices;

namespace P4T.WindowsNative.Apis {
    internal class User32 {
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "FindWindowW", ExactSpelling = true, SetLastError = true)]
        static public extern IntPtr FindWindow(string lpszClass, string lpszWindow);
        [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "SetForegroundWindow", ExactSpelling = true, SetLastError = true)]
        static public extern IntPtr SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessageW", ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "PostMessageW", ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "MoveWindow", ExactSpelling = true, SetLastError = true)]
        public static extern int MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, [MarshalAs(UnmanagedType.Bool)] bool bRepaint);
        [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "SetWindowCompositionAttribute", ExactSpelling = true, SetLastError = true)]
        public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref Structures.WindowCompositionAttributeData data);
    }
}
