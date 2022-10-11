using P4T.Win32.NativeAPI;
using System.IO;
using System.Text;

namespace P4T.Utilities {
    public class Profile {
        public string FilePath { get; protected set; }
        public Profile(string filePath) {
            if (!File.Exists(filePath)) {
                File.Create(filePath);
            }
            FilePath = Path.GetFullPath(filePath);
        }
        public string ReadValue(string section, string key) {
            StringBuilder tmp = new StringBuilder(65536);
            int result = Kernel32.GetPrivateProfileString(section, key, "", tmp, 65536, FilePath);
            if (result == 0) { return null; }
            return tmp.ToString();
        }
        public bool WriteValue(string section, string key, string value) {
            return (Kernel32.WritePrivateProfileString(section, key, value, FilePath) != 0);
        }
    }
}
