using System.Runtime.InteropServices;
using System.Text;

namespace P4T.WindowsNative.Apis {
    internal class Kernel32 {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetPrivateProfileStringW", ExactSpelling = true, SetLastError = true)]
        public static extern int GetPrivateProfileString(string section, string key, string defVal, StringBuilder retVal, int size, string filePath);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "WritePrivateProfileStringW", ExactSpelling = true, SetLastError = true)]
        public static extern int WritePrivateProfileString(string section, string key, string val, string filePath);
    }
}
