using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PP = Microsoft.Office.Interop.PowerPoint;
using WA = P4T.WindowsNative.Apis;
using WF = System.Windows.Forms;

namespace P4T {
    public partial class Toolbox : Window {
        PP.SlideShowWindow ssw;
        App appInstance;
        System.Timers.Timer updateTimer, aliveTimer;
        WF.ColorDialog colorDialog = new WF.ColorDialog();
        // int retryCount, maxRetry;
        string presId;
        IntPtr windowHandle;
        uint AccentColor {
            set {
                Color nc = Color.FromArgb(
                    (byte)((value & 0xff000000) >> 24),
                    (byte)((value & 0x00ff0000) >> 16),
                    (byte)((value & 0x0000ff00) >> 8),
                    (byte)((value & 0x000000ff) >> 0));
                Resources["AccentColor"] = new SolidColorBrush(nc);
            }
        }
        int LastColor = 0;
        double screenDpi = 1;
        bool drag = false;
        Point dragStart, windowStart, targetPos;
        Size screenSize;
        int shouldX = 0, shouldY = 0;
        int actualX = 0, actualY = 0;
        int actualWidth = 0, actualHeight = 0;
        long controlMiss, maxControlMiss;
        PP.PpSlideShowPointerType ptype = PP.PpSlideShowPointerType.ppSlideShowPointerNone;
        bool ExtendsVisible {
            get => ExtendedBar.IsVisible;
            set {
                if (value) {
                    ExtendedBar.Visibility = Visibility.Visible;
                    Toggle.Content = "\xe70d";
                    SetWindowLocation(Left, Top - 216);
                    UpdateTimer_Elapsed(null, null);
                } else {
                    ExtendedBar.Visibility = Visibility.Collapsed;
                    Toggle.Content = "\xe70e";
                    Top += 216;
                }
            }
        }

        public Toolbox(PP.SlideShowWindow Wn, string _presId, App _appInstance) {
            appInstance = _appInstance;
            presId = _presId;
            SourceInitialized += Toolbox_SourceInitialized;
            InitializeComponent();
            ssw = Wn;
            colorDialog.FullOpen = true;
            updateTimer = new System.Timers.Timer();
            aliveTimer = new System.Timers.Timer();
            updateTimer.Interval = App.Config.GetValue("Toolbox", "UpdateInterval", 50.0);
            aliveTimer.Interval = App.Config.GetValue("Toolbox", "HeartbeatInterval", 3000.0);
            updateTimer.Elapsed += UpdateTimer_Elapsed;
            aliveTimer.Elapsed += AliveTimer_Elapsed;
            updateTimer.Start();
            aliveTimer.Start();
            screenSize = new Size(SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
            Utilities.WindowCompositionAttributeModifier modifier = new Utilities.WindowCompositionAttributeModifier(this);
            uint accent;
            bool tmp;
            WA.DwmApi.DwmGetColorizationColor(out accent, out tmp);
            AccentColor = accent;
            modifier.Color = Color.FromArgb(128, 0, 0, 0);
            modifier.Enable();
            ExtendsVisible = false;
            maxControlMiss = App.Config.GetValue<long>("Toolbox", "MaxControlMiss", 20);
            // maxRetry = App.Config.GetValue("Generic", "MaxRetryCount", -1);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            Close();
        }
        private bool ShouldClose(int ErrorCode) {
            if (ErrorCode == -2147417846) { // RPC_E_SERVERCALL_RETRYLATER
                return false;
            }
            return true;
        }

        private void Toolbox_SourceInitialized(object sender, EventArgs e) {
            windowHandle = new WindowInteropHelper(this).Handle;
            HwndSource hs = HwndSource.FromHwnd(windowHandle);
            hs.AddHook(new HwndSourceHook(WindowMessageHook));
        }
        public IntPtr WindowMessageHook(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            switch (msg) {
            case 0x0003: // WM_MOVE
                actualX = (int)(((int)lParam & 0x0000FFFF) >> 0);
                actualY = (int)(((int)lParam & 0xFFFF0000) >> 16);
                break;
            case 0x0005: // WM_SIZE
                actualWidth = (int)(((int)lParam & 0x0000FFFF) >> 0);
                actualHeight = (int)(((int)lParam & 0xFFFF0000) >> 16);
                break;
            case 0x0112: // WM_SYSCOMMAND
                if (wParam == (IntPtr)61458) { // Drag Move
                    handled = true;
                    wParam = IntPtr.Zero;
                }
                break;
            case 0x0320: // WM_DWMCOLORIZATIONCOLORCHANGED 
                AccentColor = (uint)wParam;
                break;
            }
            return IntPtr.Zero;
        }

        private void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            if (!ExtendsVisible) { return; }
            try {
                if (ssw.View.PointerColor.RGB != LastColor) {
                    LastColor = ssw.View.PointerColor.RGB;
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        ColorPreview.Foreground = new SolidColorBrush(Color.FromRgb(
                            (byte)((LastColor & 0x0000FF) >> 0),
                            (byte)((LastColor & 0x00FF00) >> 8),
                            (byte)((LastColor & 0xFF0000) >> 16)
                        ));
                    }));
                }
            }
            catch { }
            try {
                if (ssw.View.PointerType != ptype) {
                    ptype = ssw.View.PointerType;
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        switch (ptype) {
                        case PP.PpSlideShowPointerType.ppSlideShowPointerEraser:
                            EraserSwitch.IsChecked = true;
                            PenSwitch.IsChecked = false;
                            PointerSwitch.IsChecked = false;
                            break;
                        case PP.PpSlideShowPointerType.ppSlideShowPointerPen:
                            EraserSwitch.IsChecked = false;
                            PenSwitch.IsChecked = true;
                            PointerSwitch.IsChecked = false;
                            break;
                        default:
                            EraserSwitch.IsChecked = false;
                            PenSwitch.IsChecked = false;
                            PointerSwitch.IsChecked = true;
                            break;
                        }
                    }));
                }
            }
            catch { }
        }

        void SetWindowLocation(double x, double y) {
            if (x < 0) {
                x = 0;
            }
            if (y < 0) {
                y = 0;
            }
            if (x + Width > screenSize.Width) {
                x = screenSize.Width - Width;
            }
            if (y + Height > screenSize.Height) {
                y = screenSize.Height - Height;
            }
            shouldX = (int)(x * screenDpi);
            shouldY = (int)(y * screenDpi);
            WA.User32.MoveWindow(windowHandle, shouldX, shouldY, actualWidth, actualHeight, false);
        }

        private void Drag_MouseDown(object sender, MouseButtonEventArgs e) {
            //try {
            //    DragMove();
            //}
            //catch { }
            //MessageBox.Show(PointToScreen(e.GetPosition(this)).X.ToString());
            dragStart = PointToScreen(e.GetPosition(this));
            windowStart = new Point(Left, Top);
            ((FrameworkElement)sender).CaptureMouse();
            Resources["GripColor"] = Resources["AccentColor"];
            drag = true;
            //dragMoveTimer.Start();
        }

        private void Drag_MouseMove(object sender, MouseEventArgs e) {
            if (drag) {
                if (controlMiss < maxControlMiss && (shouldX != actualX || shouldY != actualY)) { // Allowed max control miss
                    controlMiss++;
                    return;
                }
                controlMiss = 0;
                Point current = PointToScreen(e.GetPosition(this));
                targetPos.X = (current.X - dragStart.X) / screenDpi + windowStart.X;
                targetPos.Y = (current.Y - dragStart.Y) / screenDpi + windowStart.Y;
                SetWindowLocation(targetPos.X, targetPos.Y);
            }
        }

        private void AliveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            try {
                _ = ssw.IsFullScreen;
            }
            catch (COMException ex) {
                if (ShouldClose(ex.ErrorCode)) {
                    CloseWindow();
                }
            }
            catch {
                CloseWindow();
            }
        }

        private void Drag_MouseUp(object sender, MouseButtonEventArgs e) {
            //dragMoveTimer.Stop();
            drag = false;
            ((FrameworkElement)sender).ReleaseMouseCapture();
            Resources["GripColor"] = Resources["Foreground"];
        }

        private void PointerSwitch_Click(object sender, RoutedEventArgs e) {
            try {
                ssw.Activate();
                ptype = ssw.View.PointerType = PP.PpSlideShowPointerType.ppSlideShowPointerAutoArrow;
                EraserSwitch.IsChecked = false;
                PenSwitch.IsChecked = false;
                PointerSwitch.IsChecked = true;
            }
            catch { }
        }

        private void PenSwitch_Click(object sender, RoutedEventArgs e) {
            try {
                ssw.Activate();
                ptype = ssw.View.PointerType = PP.PpSlideShowPointerType.ppSlideShowPointerPen;
                EraserSwitch.IsChecked = false;
                PenSwitch.IsChecked = true;
                PointerSwitch.IsChecked = false;
            }
            catch { }
        }

        private void EraserSwitch_Click(object sender, RoutedEventArgs e) {
            try {
                ssw.Activate();
                ptype = ssw.View.PointerType = PP.PpSlideShowPointerType.ppSlideShowPointerEraser;
                EraserSwitch.IsChecked = true;
                PenSwitch.IsChecked = false;
                PointerSwitch.IsChecked = false;
            }
            catch { }
        }
        bool closeRequested = false;
        void CloseWindow() {
            if (!closeRequested) {
                appInstance.RemoveConnection(presId);
                closeRequested = true;
            }
            else {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    Close();
                }));
            }
        }

        private void Previous_Click(object sender, RoutedEventArgs e) {
            new Thread(new ThreadStart(() => {
                try {
                    ssw.Activate();
                    ssw.View.Previous();
                }
                catch (COMException ex) {
                    if (ShouldClose(ex.ErrorCode)) {
                        CloseWindow();
                    }
                }
                catch { CloseWindow(); }
            })).Start();
        }

        private void Next_Click(object sender, RoutedEventArgs e) {
            new Thread(new ThreadStart(() => {
                try {
                    ssw.Activate();
                    ssw.View.Next();
                }
                catch (COMException ex) {
                    if (ShouldClose(ex.ErrorCode)) {
                        CloseWindow();
                    }
                }
                catch { CloseWindow(); }
            })).Start();
        }


        private void Window_Closing(object sender, EventArgs e) {
            updateTimer.Stop();
            updateTimer.Dispose();
            aliveTimer.Stop();
            aliveTimer.Dispose();
        }

        private void QuitSwitch_Click(object sender, RoutedEventArgs e) {
            try { 
                ssw.View.Exit();
            }
            catch (COMException ex) {
                if (ShouldClose(ex.ErrorCode)) {
                    CloseWindow();
                }
            }
            catch { CloseWindow(); }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            screenDpi = (PresentationSource.FromVisual(this)?.CompositionTarget.TransformToDevice.M11).GetValueOrDefault(screenDpi);
            SetWindowLocation((screenSize.Width - Width) / 2, screenSize.Height - Height);
        }

        private void ClearSwitch_Click(object sender, RoutedEventArgs e) {
            try { 
                //ssw.View.EraseDrawing();
                ssw.View.PointerType = PP.PpSlideShowPointerType.ppSlideShowPointerAutoArrow;
                ssw.Activate();
                WA.User32.PostMessage((IntPtr)ssw.HWND, 0x0100, (IntPtr)0x45, IntPtr.Zero); // WM_KEYDOWN, 'E'
                WA.User32.PostMessage((IntPtr)ssw.HWND, 0x0101, (IntPtr)0x45, IntPtr.Zero); // WM_KEYUP, 'E'
                if (ssw.IsFullScreen == Microsoft.Office.Core.MsoTriState.msoFalse) {
                    ssw.View.EraseDrawing();
                }
            }
            catch (COMException ex) {
                if (ShouldClose(ex.ErrorCode)) {
                    CloseWindow();
                }
            }
            catch { CloseWindow(); }
        }

        private void SetColor_Click(object sender, RoutedEventArgs e) {
            if (colorDialog.ShowDialog() == WF.DialogResult.OK) {
                try {
                    byte R = colorDialog.Color.R;
                    byte G = colorDialog.Color.G;
                    byte B = colorDialog.Color.B;
                    ssw.View.PointerColor.RGB = ((B << 16) + (G << 8) + R);
                }
                catch (COMException ex) {
                    if (ShouldClose(ex.ErrorCode)) {
                        CloseWindow();
                    }
                }
                catch { CloseWindow(); }
            }
        }

        private void TaskbarSwitch_Click(object sender, RoutedEventArgs e) {
            try {
                IntPtr hWnd = WA.User32.FindWindow("Shell_TrayWnd", null);
                WA.User32.SetForegroundWindow(hWnd);
            }
            catch { }
        }

        private void Window_DpiChanged(object sender, DpiChangedEventArgs e) {
            screenDpi = e.NewDpi.PixelsPerDip;
        }

        private void Toggle_Click(object sender, RoutedEventArgs e) {
            ExtendsVisible = !ExtendsVisible;
        }
    }
}
