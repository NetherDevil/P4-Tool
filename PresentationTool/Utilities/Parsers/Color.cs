using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Media = System.Windows.Media;

namespace P4T.Utilities.Parsers {
    internal static class Color {
        static public Media.Color ParseHexRGB(string value) {
            if (value == null) {
                throw new ArgumentNullException("value");
            }
            uint _color;
            try {
                _color = Convert.ToUInt32(value, 16);
            }
            catch {
                throw new ArgumentException("Invalid value.");
            }
            if (_color >= 16777216) {
                throw new ArgumentOutOfRangeException("value");
            }
            return Media.Color.FromRgb(
                (byte)((_color & 0x00ff0000) >> 16),
                (byte)((_color & 0x0000ff00) >> 8),
                (byte)((_color & 0x000000ff) >> 0));
        }
    }
}
