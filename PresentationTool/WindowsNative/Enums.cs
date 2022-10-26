using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P4T.WindowsNative {
    internal class Enums {
        public enum WindowCompositionAttribute {
            WCA_ACCENT_POLICY = 19,
        }
        public enum BlurSupportedLevel {
            NotSupported,
            Aero,
            Blur,
            Acrylic,
        }
        public enum AccentState {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_INVALID_STATE = 5,
        }
    }
}
