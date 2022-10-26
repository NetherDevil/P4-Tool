using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using P4T.WindowsNative.Apis;
using static P4T.WindowsNative.Enums;
using static P4T.WindowsNative.Structures;

namespace P4T.Utilities {
    internal class WindowCompositionAttributeModifier {
        Window _window;
        AccentPolicy _accentPolicy;
        bool _enabled;
        int _blurColor;
        IntPtr _handle { get => new WindowInteropHelper(_window).EnsureHandle(); }
        public bool IsEnabled { get => _enabled; }
        public Color Color {
            get => Color.FromArgb(
                (byte)((_blurColor & 0x000000ff) >> 0),
                (byte)((_blurColor & 0x0000ff00) >> 8),
                (byte)((_blurColor & 0x00ff0000) >> 16),
                (byte)((_blurColor & 0xff000000) >> 24));
            set {
                _blurColor =
                    value.R << 0 |
                    value.G << 8 |
                    value.B << 16 |
                    value.A << 24;
                Refresh();
            }
        }
        public WindowCompositionAttributeModifier(Window window) {
            if (window == null) {
                throw new ArgumentNullException(nameof(window));
            }
            _window = window;
        }
        private bool RenewAccentPolicy(AccentPolicy accent) {
            int accentPolicySize = Marshal.SizeOf(accent);
            IntPtr accentPtr = Marshal.AllocHGlobal(accentPolicySize);
            Marshal.StructureToPtr(accent, accentPtr, false);
            try {
                WindowCompositionAttributeData data = new WindowCompositionAttributeData();
                data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
                data.SizeOfData = accentPolicySize;
                data.Data = accentPtr;
                User32.SetWindowCompositionAttribute(_handle, ref data);
            }
            catch {
                return false;
            }
            finally {
                Marshal.FreeHGlobal(accentPtr);
            }
            _enabled = true;
            return true;
        }
        public bool Enable() {
            Version osVersion = Environment.OSVersion.Version;
            _accentPolicy = new AccentPolicy();
            if (osVersion.Major >= 10) {
                if (osVersion.Build >= 17763) { // Windows 10 RS4 or greater
                    _accentPolicy.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
                    _accentPolicy.GradientColor = _blurColor;
                }
                else {
                    _accentPolicy.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
                }
            }
            else {
                return false;
            }
            if (RenewAccentPolicy(_accentPolicy)) {
                _enabled = true;
                return true;
            }
            return false;
        }
        public bool Disable() {
            AccentPolicy accent = new AccentPolicy();
            accent.AccentState = AccentState.ACCENT_DISABLED;
            if (RenewAccentPolicy(accent)) {
                _enabled = false;
                return true;
            }
            return false;
        }
        public void Refresh() {
            if (IsEnabled) {
                _accentPolicy.GradientColor = _blurColor;
                RenewAccentPolicy(_accentPolicy);
            }
        }
    }
}
