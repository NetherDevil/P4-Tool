using System;
using System.Runtime.InteropServices;

namespace P4T.WindowsNative {
    internal class Structures {
        [StructLayout(LayoutKind.Sequential)]
        public struct AccentPolicy {
            public Enums.AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WindowCompositionAttributeData {
            public Enums.WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }
    }
}
