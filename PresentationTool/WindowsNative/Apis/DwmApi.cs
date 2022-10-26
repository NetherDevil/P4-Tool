using System.Runtime.InteropServices;

namespace P4T.WindowsNative.Apis {
    internal class DwmApi {
        [DllImport("dwmapi.dll", EntryPoint = "DwmGetColorizationColor", ExactSpelling = true, SetLastError = true)]
        static public extern void DwmGetColorizationColor(out uint ColorizationColor, [MarshalAs(UnmanagedType.Bool)] out bool ColorizationOpaqueBlend);
    }
}
