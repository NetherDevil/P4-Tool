using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WF = System.Windows.Forms;
using PP = Microsoft.Office.Interop.PowerPoint;
using System.Net.NetworkInformation;
using System.Windows.Media;
using WD = System.Drawing;
using System.Net.Http.Headers;

namespace P4T {
    public partial class App : Application {
        PP.Application ppa;
        internal static Config Config;
        delegate void openToolbox(PP.SlideShowWindow Wn);
        bool ShouldExit = false;
        bool AutoDetect = false;
        int retryCount, maxRetry;
        bool StartBackground;
        openToolbox OpenToolbox;
        Dictionary<string, Window> toolboxWindows;
        Queue<PP.SlideShowWindow> sswQueue;
        System.Timers.Timer connectTicker;
        System.Timers.Timer aliveTicker;
        Thread HandlerThread;

        TextBlock statusText;

        static SolidColorBrush RedBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        static SolidColorBrush GreenBrush = new SolidColorBrush(Color.FromRgb(0, 255, 0));
        internal void RemoveConnection(string tbc) {
            lock (toolboxWindows) {
                Window w;
                if (toolboxWindows.TryGetValue(tbc, out w)) {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        try {
                            w.Close();
                        }
                        catch { }
                    }));
                    toolboxWindows.Remove(tbc);
                }
            }
        }
        void SSWQHandler() {
            while (!ShouldExit) {
                if (!sswQueue.Any()) {
                    Thread.Sleep(10);
                    continue;
                }
                PP.SlideShowWindow sw = sswQueue.Dequeue();
                Dispatcher.Invoke(new Action(delegate
                {
                    CreateToolbox(sw);
                }));
            }
        }
        void CreateToolbox(PP.SlideShowWindow Wn) {
            try {
                string tba = Wn.Presentation.Path + Wn.Presentation.FullName;
                lock (toolboxWindows) {
                    if (!toolboxWindows.ContainsKey(tba)) {
                        Window w = new Toolbox(Wn, tba, this);
                        w.Show();
                        try {
                            toolboxWindows[tba] = w;
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }
        void PurgeConnection() {
            lock (toolboxWindows) {
                foreach (Window w in toolboxWindows.Values) {
                    try {
                        w.Close();
                    }
                    catch { }
                }
                toolboxWindows.Clear();
            }
            if (ppa != null) {
                try {
                    ppa.SlideShowBegin -= Ppa_SlideShowBegin;
                    ppa.SlideShowEnd -= Ppa_SlideShowEnd;
                }
                catch { }
                ppa = null;
            }
        }
        //public void SlideShowHandler(PP.SlideShowWindow Wn) {
        //    notify.ShowBalloonTip(0, "PPT Presentation Tool", "Handler Opened", WF.ToolTipIcon.Info);
        //}
        bool TestAlive() {
            if (ppa == null) {
                return false;
            }
            try {
                _ = ppa.Active;
                retryCount = 0;
                return true;
            }
            catch (COMException ex) {
                if (ex.ErrorCode == -2147417846 && (maxRetry < 0 || retryCount < maxRetry)) { // RPC_E_SERVERCALL_RETRYLATER
                    retryCount++;
                    return true;
                }
                try {
                    if (statusText != null) {
                        statusText.Text = "Not Connected";
                        statusText.Foreground = Brushes.Red;
                    }
                }
                catch { }
                return false;
            }
            catch {
                try {
                    if (statusText != null) {
                        statusText.Text = "Not Connected";
                        statusText.Foreground = Brushes.Red;
                    }
                }
                catch { }
                return false;
            }
        }
        bool TryConnect() {
            bool hasTried = false;
        BeginTry:
            if (ppa == null) {
                try {
                    object obj = Marshal.GetActiveObject("PowerPoint.Application");
                    PP.Application application = (PP.Application)obj;
                    ppa = application;
                    ppa.SlideShowBegin += Ppa_SlideShowBegin;
                    ppa.SlideShowEnd += Ppa_SlideShowEnd;
                    try {
                        if (statusText != null) {
                            statusText.Text = "Connected";
                            statusText.Foreground = Brushes.LimeGreen;
                        }
                    }
                    catch { }
                    return true;
                }
                catch {
                    if (StartBackground && !hasTried) {
                        PP.Application application = new PP.Application();
                        hasTried = true;
                        goto BeginTry;
                    }
                    return false;
                }
            }
            else {
                if (!TestAlive()) {
                    PurgeConnection();
                    goto BeginTry;
                }
                return true;
            }
        }
        private void MenuCommand_Connect(object sender, RoutedEventArgs e) {
            if (TestAlive()) {
                notify.ShowBalloonTip(0, "Connection", "Already connected to a PowerPoint Application instance.", WF.ToolTipIcon.Info);
            }
            else {
                bool hasPrevious = (ppa != null);
                if (hasPrevious) {
                    PurgeConnection();
                }
                if (!TryConnect()) {
                    if (hasPrevious) {
                        notify.ShowBalloonTip(0, "Connection - Failed", "The PowerPoint Application instance previously connected to has closed. No further instance found.", WF.ToolTipIcon.Warning);
                    }
                    else {
                        notify.ShowBalloonTip(0, "Connection - Failed", "Instance of PowerPoint.Application not found.", WF.ToolTipIcon.Warning);
                    }
                }
                else {
                    if (hasPrevious) {
                        notify.ShowBalloonTip(0, "Connection", "Refreshed connection to the PowerPoint Application instance.", WF.ToolTipIcon.Info);
                    }
                    else {
                        notify.ShowBalloonTip(0, "Connection", "Connected to a PowerPoint Application instance.", WF.ToolTipIcon.Info);
                    }
                }
            }
        }
        private void MenuCommand_Disconnect(object sender, RoutedEventArgs e) {
            if (ppa == null) {
                notify.ShowBalloonTip(0, "Connection - Failed", "Not connected yet.", WF.ToolTipIcon.Warning);
            }
            else {
                PurgeConnection();
                notify.ShowBalloonTip(0, "Connection", "Disconnected from the PowerPoint Application instance.", WF.ToolTipIcon.Info);
            }
        }
        private void MenuCommand_Quit(object sender, RoutedEventArgs e) {
            Config.SaveProfile();
            notify.ShowBalloonTip(0, "Application", "Shutting down.", WF.ToolTipIcon.Info);
            Shutdown();
        }
        private void MenuCommand_Configure(object sender, RoutedEventArgs e) {
        }

        private void Ppa_SlideShowEnd(PP.Presentation Pres) {
            try {
                RemoveConnection(Pres.Path + Pres.FullName);
            }
            catch { }
        }

        private void Ppa_SlideShowBegin(PP.SlideShowWindow Wn) {
            sswQueue.Enqueue(Wn);
        }

        ContextMenu NotifyIconMenu;
        WF.NotifyIcon notify;
        public App() {
            Config = new Config("PresentationTool.ini");
            notify = new WF.NotifyIcon();
            notify.Icon = new WD.Icon(GetResourceStream(new Uri("Icon.ico", UriKind.Relative)).Stream);
            notify.Visible = true;
            notify.Click += Notify_Click;
            notify.BalloonTipTitle = "PPT Presentation Tool";
            notify.Text = "PPT Presentation Tool";
            OpenToolbox = new openToolbox(CreateToolbox);
            sswQueue = new Queue<PP.SlideShowWindow>();
            toolboxWindows = new Dictionary<string, Window>();
            HandlerThread = new Thread(new ThreadStart(SSWQHandler));
            connectTicker = new System.Timers.Timer();
            aliveTicker = new System.Timers.Timer();
            connectTicker.Interval = Config.GetValue("Detection", "DetectionInterval", 1000.0);
            aliveTicker.Interval = Config.GetValue("Detection", "HeartbeatInterval", 5000.0);
            maxRetry = Config.GetValue("Detection", "MaxBusyRetryCount", -1);
            connectTicker.Elapsed += ConnectionTick;
            aliveTicker.Elapsed += AliveCheckTick;
            AutoDetect = Config.GetValue("Detection", "Enabled", false);
            if (AutoDetect) {
                connectTicker.Start();
            }
            StartBackground = Config.GetValue("Generic", "AutoCreateInstance", false);
            Config.SaveProfile();
            HandlerThread.Start();
        }
        private void AliveCheckTick(object sender, System.Timers.ElapsedEventArgs e) {
            if (!AutoDetect) {
                aliveTicker.Stop();
                return;
            }
            if (ppa != null) {
                if (!TestAlive()) {
                    PurgeConnection();
                    notify.ShowBalloonTip(0, "Detection", "Disconnected from the PowerPoint Application instance.", WF.ToolTipIcon.Info);
                    connectTicker.Start();
                    aliveTicker.Stop();
                }
            }
        }

        private void ConnectionTick(object sender, System.Timers.ElapsedEventArgs e) {
            if (!AutoDetect) {
                connectTicker.Stop();
                return;
            }
            if (TestAlive()) {
                connectTicker.Stop();
                aliveTicker.Start();
            }
            if (TryConnect()) {
                notify.ShowBalloonTip(0, "Detection", "PowerPoint Application instance detected. Connected automatically.", WF.ToolTipIcon.Info);
                connectTicker.Stop();
                aliveTicker.Start();
            }
        }

        private void Notify_Click(object sender, EventArgs e) {
            if (NotifyIconMenu == null) {
                NotifyIconMenu = (ContextMenu)Current.FindResource("NotifyIconMenu");
            }
            if (statusText == null) {
                statusText = (TextBlock)NotifyIconMenu.FindResource("Status");
            }
            try {
                if (TestAlive()) {
                    statusText.Text = "Connected";
                    statusText.Foreground = Brushes.LimeGreen;
                }
                else {
                    statusText.Text = "Not Connected";
                    statusText.Foreground = Brushes.Red;
                }
            }
            catch { }
            NotifyIconMenu.IsOpen = false;
            NotifyIconMenu.IsOpen = true;
            NotifyIconMenu.Focus();
        }

        private void Application_Exit(object sender, ExitEventArgs e) {
            ShouldExit = true;
            Config.SaveProfile();
            HandlerThread.Join();
            notify.Dispose();
        }

        private void MenuItem_Checked(object sender, RoutedEventArgs e) {
            Config.SetValue("Detection", "Enabled", AutoDetect = true);
            connectTicker.Start();
        }

        private void MenuItem_Unchecked(object sender, RoutedEventArgs e) {
            Config.SetValue("Detection", "Enabled" ,AutoDetect = false);
        }

        private void AutoStart_Checked(object sender, RoutedEventArgs e) {
            Config.SetValue("Generic", "AutoCreateInstance", StartBackground = true);
        }

        private void AutoStart_Unchecked(object sender, RoutedEventArgs e) {
            Config.SetValue("Generic", "AutoCreateInstance", StartBackground = false);
        }

        private void AutoDetect_Loaded(object sender, RoutedEventArgs e) {
            ((MenuItem)sender).IsChecked = AutoDetect;
        }

        private void AutoStart_Loaded(object sender, RoutedEventArgs e) {
            ((MenuItem)sender).IsChecked = StartBackground;
        }
    }
}
