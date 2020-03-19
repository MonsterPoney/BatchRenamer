using System.Runtime.InteropServices;
using System.Text;

namespace BatchRenamer {

    internal static class NativeMethods {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WritePrivateProfileString(
            string lpAppName,   // The name of the section to which the string will be copied.
            string lpKeyName,   // The name of the key to be associated with a string.
            string lpString,    // A null-terminated string to be written to the file
            string lpFileName   // The name of the initialization file (path).
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetPrivateProfileString(
            string lpAppName,               // The name of the section containing the key name.
            string lpKeyName,               // The name of the key whose associated string is to be retrieved.
            string lpDefault,               // A default string.
            StringBuilder lpReturnedString, // A pointer to the buffer that receives the retrieved string.
            int nSize,                      // The size of the buffer pointed to by the lpReturnedString parameter, in characters.
            string lpFileName               // The name of the initialization file (path).
        );
    }
    class ConfigFile {
        public string nameFile;

        public ConfigFile(string _nameFile) {
            nameFile = _nameFile;
        }

        public bool WriteKey(string _section, string _key, string _value) {
            return NativeMethods.WritePrivateProfileString(_section, _key, _value, this.nameFile);
        }

        public string ReadKey(string _section, string _key, string _default = "") {
            var temp = new StringBuilder(255);
            int i = NativeMethods.GetPrivateProfileString(_section, _key, _default, temp, 255, this.nameFile);
            return temp.ToString();
        }
    }
}
