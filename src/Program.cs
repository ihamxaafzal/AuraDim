using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

[assembly: System.Reflection.AssemblyVersion("3.3.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("3.3.0.0")]

namespace LumaDeck
{
    internal static class NativeMethods
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        public const int WM_HOTKEY = 0x0312;
        public const int WM_DISPLAYCHANGE = 0x007E;
        public const int WM_DEVICECHANGE = 0x0219;
        public const int DBT_DEVNODES_CHANGED = 0x0007;

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint VK_B = 0x42;
        public const int HOTKEY_TOGGLE_ID = 9101;
        public const uint EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001;

        public const int WS_EX_TRANSPARENT = 0x20;
        public const int WS_EX_LAYERED = 0x80000;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TOOLWINDOW = 0x80;

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;

        public const int WM_SHOWME = 0x8001;
        public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        public static extern bool SetProcessDpiAwarenessContext(IntPtr dpiFlag);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static void EnableBestDpiAwareness()
        {
            try
            {
                // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4
                if (SetProcessDpiAwarenessContext(new IntPtr(-4))) return;
            }
            catch { }
            try { SetProcessDPIAware(); } catch { }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public uint StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetMonitorBrightness(IntPtr hMonitor, out uint pdwMinimumBrightness, out uint pdwCurrentBrightness, out uint pdwMaximumBrightness);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool SetMonitorBrightness(IntPtr hMonitor, uint dwNewBrightness);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetMonitorContrast(IntPtr hMonitor, out uint pdwMinimumContrast, out uint pdwCurrentContrast, out uint pdwMaximumContrast);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool SetMonitorContrast(IntPtr hMonitor, uint dwNewContrast);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetVCPFeatureAndVCPFeatureReply(IntPtr hMonitor, byte bVCPCode, out uint pvct, out uint pdwCurrentValue, out uint pdwMaximumValue);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool SetVCPFeature(IntPtr hMonitor, byte bVCPCode, uint dwNewValue);

        [StructLayout(LayoutKind.Sequential)]
        public struct RAMP
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Red;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Green;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Blue;
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern bool SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);
    }

    public class MonitorBounds
    {
        public int x { get; set; }
        public int y { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class ProfileMonitorValues
    {
        public int brightness { get; set; }
        public int contrast { get; set; }
        public int warmth { get; set; }
    }

    public class MonitorInfoDto
    {
        public string id { get; set; }
        public int index { get; set; }
        public string deviceName { get; set; }
        public string hardwareName { get; set; }
        public string customName { get; set; }
        public string manufacturer { get; set; }
        public string backend { get; set; }
        public string controlMode { get; set; }
        public bool isPrimary { get; set; }
        public bool isInternal { get; set; }
        public bool supportsBrightness { get; set; }
        public bool supportsContrast { get; set; }
        public bool supportsWarmth { get; set; }
        public int brightness { get; set; }
        public int contrast { get; set; }
        public int warmth { get; set; }
        public ProfileMonitorValues defaultValues { get; set; }
        public MonitorBounds bounds { get; set; }
    }

    public class DisplayProfile
    {
        public string id { get; set; }
        public string name { get; set; }
        public string icon { get; set; }
        public string color { get; set; }
        public bool builtin { get; set; }
        public bool dualMode { get; set; }
        public ProfileMonitorValues defaultValues { get; set; }
        public ProfileMonitorValues secondaryValues { get; set; }
        public Dictionary<string, ProfileMonitorValues> monitors { get; set; }

        public DisplayProfile()
        {
            monitors = new Dictionary<string, ProfileMonitorValues>();
            defaultValues = new ProfileMonitorValues { brightness = 75, contrast = 75, warmth = 0 };
        }
    }

    public class AppConfig
    {
        public Dictionary<string, string> monitorNames { get; set; }
        public Dictionary<string, int> monitorWarmth { get; set; }
        public Dictionary<string, int> monitorSoftwareBrightness { get; set; }
        public Dictionary<string, int> monitorSoftwareContrast { get; set; }
        public Dictionary<string, string> monitorControlModes { get; set; }
        public Dictionary<string, ProfileMonitorValues> monitorDefaults { get; set; }
        public List<DisplayProfile> profiles { get; set; }
        public string activeProfileId { get; set; }
        public bool syncAll { get; set; }
        public bool startWithWindows { get; set; }
        public bool minimizeToTray { get; set; }
        public bool globalHotkeyEnabled { get; set; }

        public AppConfig()
        {
            monitorNames = new Dictionary<string, string>();
            monitorWarmth = new Dictionary<string, int>();
            monitorSoftwareBrightness = new Dictionary<string, int>();
            monitorSoftwareContrast = new Dictionary<string, int>();
            monitorControlModes = new Dictionary<string, string>();
            monitorDefaults = new Dictionary<string, ProfileMonitorValues>();
            profiles = new List<DisplayProfile>();
            activeProfileId = "";
            syncAll = false;
            startWithWindows = false;
            minimizeToTray = true;
            globalHotkeyEnabled = true;
        }
    }

    public static class ConfigStore
    {
        public static readonly string DataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LumaDeck");

        public static readonly string ConfigPath = Path.Combine(DataDir, "config.json");
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = 10485760 };

        public static List<DisplayProfile> CreateBuiltinProfiles()
        {
            var list = new List<DisplayProfile>();

            list.Add(new DisplayProfile
            {
                id = "builtin-daylight",
                name = "Daylight Work",
                icon = "\u2600\uFE0F",
                color = "#38bdf8",
                builtin = true,
                defaultValues = new ProfileMonitorValues { brightness = 85, contrast = 75, warmth = 0 }
            });

            list.Add(new DisplayProfile
            {
                id = "builtin-gaming",
                name = "Gaming Pro",
                icon = "\uD83C\uDFAE",
                color = "#a855f7",
                builtin = true,
                defaultValues = new ProfileMonitorValues { brightness = 100, contrast = 85, warmth = 0 }
            });

            list.Add(new DisplayProfile
            {
                id = "builtin-cinema",
                name = "Cinema Mode",
                icon = "\uD83C\uDFAC",
                color = "#f43f5e",
                builtin = true,
                defaultValues = new ProfileMonitorValues { brightness = 65, contrast = 82, warmth = 10 }
            });

            list.Add(new DisplayProfile
            {
                id = "builtin-reading",
                name = "Reading Care",
                icon = "\uD83D\uDCD6",
                color = "#10b981",
                builtin = true,
                defaultValues = new ProfileMonitorValues { brightness = 55, contrast = 68, warmth = 28 }
            });

            list.Add(new DisplayProfile
            {
                id = "builtin-night",
                name = "Night Owl",
                icon = "\uD83C\uDF19",
                color = "#f59e0b",
                builtin = true,
                defaultValues = new ProfileMonitorValues { brightness = 25, contrast = 65, warmth = 52 }
            });

            list.Add(new DisplayProfile
            {
                id = "builtin-dualfocus",
                name = "Dual Focus",
                icon = "\uD83C\uDFAF",
                color = "#6366f1",
                builtin = true,
                dualMode = true,
                defaultValues = new ProfileMonitorValues { brightness = 88, contrast = 78, warmth = 0 },
                secondaryValues = new ProfileMonitorValues { brightness = 42, contrast = 68, warmth = 18 }
            });

            return list;
        }

        public static AppConfig Load()
        {
            Directory.CreateDirectory(DataDir);
            AppConfig cfg = null;

            if (File.Exists(ConfigPath))
            {
                try
                {
                    string raw = File.ReadAllText(ConfigPath, Encoding.UTF8);
                    cfg = Json.Deserialize<AppConfig>(raw);
                }
                catch { cfg = null; }
            }

            if (cfg == null) cfg = new AppConfig();
            if (cfg.monitorNames == null) cfg.monitorNames = new Dictionary<string, string>();
            if (cfg.monitorWarmth == null) cfg.monitorWarmth = new Dictionary<string, int>();
            if (cfg.monitorSoftwareBrightness == null) cfg.monitorSoftwareBrightness = new Dictionary<string, int>();
            if (cfg.monitorSoftwareContrast == null) cfg.monitorSoftwareContrast = new Dictionary<string, int>();
            if (cfg.monitorControlModes == null) cfg.monitorControlModes = new Dictionary<string, string>();
            if (cfg.monitorDefaults == null) cfg.monitorDefaults = new Dictionary<string, ProfileMonitorValues>();
            if (cfg.profiles == null) cfg.profiles = new List<DisplayProfile>();

            if (cfg.profiles.Count == 0)
            {
                cfg.profiles = CreateBuiltinProfiles();
                Save(cfg);
            }
            else
            {
                var builtins = CreateBuiltinProfiles();
                var existingIds = new HashSet<string>();
                foreach (var p in cfg.profiles)
                {
                    if (!string.IsNullOrEmpty(p.id)) existingIds.Add(p.id);
                    if (p.monitors == null) p.monitors = new Dictionary<string, ProfileMonitorValues>();
                    if (p.defaultValues == null) p.defaultValues = new ProfileMonitorValues { brightness = 75, contrast = 75, warmth = 0 };
                }
                bool added = false;
                for (int i = 0; i < builtins.Count; i++)
                {
                    if (!existingIds.Contains(builtins[i].id))
                    {
                        cfg.profiles.Insert(Math.Min(i, cfg.profiles.Count), builtins[i]);
                        added = true;
                    }
                }
                if (added) Save(cfg);
            }

            return cfg;
        }

        public static void Save(AppConfig cfg)
        {
            try
            {
                Directory.CreateDirectory(DataDir);
                string raw = Json.Serialize(cfg);
                File.WriteAllText(ConfigPath, raw, Encoding.UTF8);
            }
            catch { }
        }

        public static void SetStartup(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key == null) return;
                    if (enable)
                    {
                        string exePath = Application.ExecutablePath;
                        key.SetValue("LumaDeck", "\"" + exePath + "\" --tray");
                    }
                    else
                    {
                        key.DeleteValue("LumaDeck", false);
                    }
                }
            }
            catch { }
        }
    }

    internal class DimOverlayForm : Form
    {
        private Rectangle _lastBounds = Rectangle.Empty;
        private double _lastOpacity = -1.0;
        private Color _lastColor = Color.Black;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= NativeMethods.WS_EX_LAYERED
                    | NativeMethods.WS_EX_TRANSPARENT
                    | NativeMethods.WS_EX_NOACTIVATE
                    | NativeMethods.WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        public DimOverlayForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.Black;
            this.Opacity = 0.0;
        }

        public void ApplyOverlay(MonitorBounds bounds, int brightnessPercent, int fallbackWarmthPercent, bool active)
        {
            if (bounds == null || !active || (brightnessPercent >= 100 && fallbackWarmthPercent <= 0))
            {
                if (this.Visible) this.Hide();
                _lastOpacity = 0.0;
                return;
            }

            int clampedBri = Math.Max(0, Math.Min(100, brightnessPercent));
            int clampedWarm = Math.Max(0, Math.Min(100, fallbackWarmthPercent));

            double dimOp = ((100 - clampedBri) / 100.0) * 0.78;
            double warmOp = (clampedWarm / 100.0) * 0.28;
            double opacity = Math.Max(dimOp, warmOp);
            if (opacity <= 0.01)
            {
                if (this.Visible) this.Hide();
                _lastOpacity = 0.0;
                return;
            }

            // If fallback warmth is needed (e.g. HDR / DisplayLink where SetDeviceGammaRamp fails), tint towards warm amber
            Color targetColor = Color.Black;
            if (clampedWarm > 0)
            {
                double ratio = warmOp / opacity;
                int r = (int)Math.Round(125 * ratio);
                int g = (int)Math.Round(55 * ratio);
                targetColor = Color.FromArgb(Math.Min(140, r), Math.Min(70, g), 0);
            }

            var targetRect = new Rectangle(bounds.x, bounds.y, bounds.width, bounds.height);
            bool boundsChanged = (_lastBounds != targetRect);
            bool opChanged = Math.Abs(_lastOpacity - opacity) > 0.004;
            bool colorChanged = (_lastColor != targetColor);

            if (colorChanged)
            {
                this.BackColor = targetColor;
                _lastColor = targetColor;
            }
            if (boundsChanged)
            {
                this.Bounds = targetRect;
                _lastBounds = targetRect;
            }
            if (opChanged)
            {
                this.Opacity = opacity;
                _lastOpacity = opacity;
            }
            if (!this.Visible)
            {
                this.Show();
                NativeMethods.SetWindowPos(
                    this.Handle,
                    NativeMethods.HWND_TOPMOST,
                    targetRect.X,
                    targetRect.Y,
                    targetRect.Width,
                    targetRect.Height,
                    NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
            }
            else if (boundsChanged)
            {
                NativeMethods.SetWindowPos(
                    this.Handle,
                    NativeMethods.HWND_TOPMOST,
                    targetRect.X,
                    targetRect.Y,
                    targetRect.Width,
                    targetRect.Height,
                    NativeMethods.SWP_NOACTIVATE);
            }
        }
    }

    internal class MonitorHandleEntry : IDisposable
    {
        public MonitorInfoDto Info;
        public IntPtr HMonitor;
        public IntPtr HPhysicalMonitor;
        public NativeMethods.PHYSICAL_MONITOR[] PhysicalArray;
        public string WmiInstanceName;
        public byte[] WmiSupportedLevels;
        public byte BriVcpCode = 0x10;
        public uint BriMin;
        public uint BriMax = 100;
        public uint ConMin;
        public uint ConMax = 100;
        public bool HasCustomGammaApplied;
        public bool GammaFailedFallbackNeeded;
        public DateTime LastBriSetUtc;
        public DateTime LastConSetUtc;
        public int LastSetBrightness = -1;
        public int LastSetContrast = -1;

        public readonly object HandleLock = new object();
        public bool Disposed;

        private readonly object _queueLock = new object();
        private bool _workerRunning;
        private int? _pendingBrightness;
        private int? _pendingContrast;
        private int? _pendingWarmth;
        private readonly DisplayEngine _engine;

        public MonitorHandleEntry(DisplayEngine engine)
        {
            _engine = engine;
        }

        public void WaitForPendingWrites(int timeoutMs)
        {
            int waited = 0;
            while (waited < timeoutMs)
            {
                lock (_queueLock)
                {
                    if (!_workerRunning && !_pendingBrightness.HasValue && !_pendingContrast.HasValue && !_pendingWarmth.HasValue)
                    {
                        return;
                    }
                }
                Thread.Sleep(20);
                waited += 20;
            }
        }

        public void EnqueueUpdate(int? brightness, int? contrast, int? warmth)
        {
            lock (_queueLock)
            {
                if (brightness.HasValue) _pendingBrightness = brightness.Value;
                if (contrast.HasValue) _pendingContrast = contrast.Value;
                if (warmth.HasValue) _pendingWarmth = warmth.Value;

                if (!_workerRunning)
                {
                    _workerRunning = true;
                    ThreadPool.QueueUserWorkItem(ProcessQueue);
                }
            }
        }

        private void ProcessQueue(object state)
        {
            while (true)
            {
                int? b = null;
                int? c = null;
                int? w = null;
                lock (_queueLock)
                {
                    b = _pendingBrightness;
                    c = _pendingContrast;
                    w = _pendingWarmth;
                    _pendingBrightness = null;
                    _pendingContrast = null;
                    _pendingWarmth = null;

                    if (!b.HasValue && !c.HasValue && !w.HasValue)
                    {
                        _workerRunning = false;
                        return;
                    }
                }

                try
                {
                    _engine.ApplyToHardware(this, b, c, w);
                }
                catch { }
            }
        }

        public void Dispose()
        {
            lock (HandleLock)
            {
                Disposed = true;
                if (PhysicalArray != null && PhysicalArray.Length > 0)
                {
                    try { NativeMethods.DestroyPhysicalMonitors((uint)PhysicalArray.Length, PhysicalArray); }
                    catch { }
                    PhysicalArray = null;
                    HPhysicalMonitor = IntPtr.Zero;
                }
            }
        }
    }

    public class DisplayEngine : IDisposable
    {
        private readonly object _sync = new object();
        private static readonly object DdcBusLock = new object();
        private readonly List<MonitorHandleEntry> _entries = new List<MonitorHandleEntry>();
        private readonly Dictionary<string, DimOverlayForm> _overlays = new Dictionary<string, DimOverlayForm>(StringComparer.OrdinalIgnoreCase);
        private readonly AppConfig _config;
        private readonly Control _uiInvoker;

        public DisplayEngine(AppConfig config, Control uiInvoker)
        {
            _config = config;
            _uiInvoker = uiInvoker;
        }

        private class WmiIdInfo
        {
            public string InstanceKey;
            public string Manufacturer;
            public string FriendlyName;
        }

        private class WmiBrightnessInfo
        {
            public string FullInstanceName;
            public string InstanceKey;
            public string ShortModelPrefix;
            public int CurrentBrightness;
            public byte[] Levels;
        }

        private static string DecodeUshortArray(ushort[] arr)
        {
            if (arr == null) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == 0) break;
                sb.Append((char)arr[i]);
            }
            return sb.ToString().Trim();
        }

        private static string NormalizeInstanceKey(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string s = raw.Trim();
            if (s.EndsWith("_0", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(0, s.Length - 2);
            }
            if (s.StartsWith(@"\\?\", StringComparison.Ordinal))
            {
                s = s.Substring(4);
            }
            int braceIdx = s.IndexOf("#{", StringComparison.Ordinal);
            if (braceIdx > 0)
            {
                s = s.Substring(0, braceIdx);
            }
            s = s.Replace('#', '\\');
            return s.ToUpperInvariant();
        }

        private static string ExtractModelPrefix(string instanceKey)
        {
            if (string.IsNullOrEmpty(instanceKey)) return "";
            string[] parts = instanceKey.Split('\\');
            return parts.Length >= 2 ? parts[1] : instanceKey;
        }

        private static Dictionary<string, WmiIdInfo> QueryWmiMonitorIds()
        {
            var map = new Dictionary<string, WmiIdInfo>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT InstanceName, ManufacturerName, UserFriendlyName FROM WmiMonitorID"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string inst = obj["InstanceName"] as string;
                        string key = NormalizeInstanceKey(inst);
                        if (string.IsNullOrEmpty(key)) continue;

                        map[key] = new WmiIdInfo
                        {
                            InstanceKey = key,
                            Manufacturer = DecodeUshortArray(obj["ManufacturerName"] as ushort[]),
                            FriendlyName = DecodeUshortArray(obj["UserFriendlyName"] as ushort[])
                        };
                    }
                }
            }
            catch { }
            return map;
        }

        private static HashSet<string> QueryWmiInternalKeys()
        {
            var internalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT InstanceName, VideoOutputTechnology FROM WmiMonitorConnectionParams"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string key = NormalizeInstanceKey(obj["InstanceName"] as string);
                        if (string.IsNullOrEmpty(key)) continue;
                        uint vot = 0;
                        try { vot = Convert.ToUInt32(obj["VideoOutputTechnology"]); } catch { }
                        // 0x80000000 (INTERNAL), 6 (LVDS), 11 (DISPLAYPORT_EMBEDDED)
                        if (vot == 0x80000000 || vot == 6 || vot == 11)
                        {
                            internalKeys.Add(key);
                        }
                    }
                }
            }
            catch { }
            return internalKeys;
        }

        private static List<WmiBrightnessInfo> QueryWmiBrightness()
        {
            var list = new List<WmiBrightnessInfo>();
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT InstanceName, CurrentBrightness, Level FROM WmiMonitorBrightness"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string inst = obj["InstanceName"] as string;
                        int cur = 75;
                        try { cur = Convert.ToInt32(obj["CurrentBrightness"]); } catch { }
                        byte[] levels = obj["Level"] as byte[];
                        string key = NormalizeInstanceKey(inst);
                        list.Add(new WmiBrightnessInfo
                        {
                            FullInstanceName = inst,
                            InstanceKey = key,
                            ShortModelPrefix = ExtractModelPrefix(key),
                            CurrentBrightness = Clamp(cur, 0, 100),
                            Levels = levels
                        });
                    }
                }
            }
            catch { }
            return list;
        }

        private static int Clamp(int val, int min, int max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        public string GetTopologySignature()
        {
            var parts = new List<string>();
            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMon, IntPtr hdc, ref NativeMethods.RECT rc, IntPtr data) =>
            {
                var mi = new NativeMethods.MONITORINFOEX();
                mi.cbSize = Marshal.SizeOf(mi);
                if (!NativeMethods.GetMonitorInfo(hMon, ref mi)) return true;

                var dd = new NativeMethods.DISPLAY_DEVICE();
                dd.cb = Marshal.SizeOf(dd);
                NativeMethods.EnumDisplayDevices(mi.szDevice, 0, ref dd, NativeMethods.EDD_GET_DEVICE_INTERFACE_NAME);
                string key = NormalizeInstanceKey(dd.DeviceID);
                if (string.IsNullOrEmpty(key)) key = mi.szDevice;
                parts.Add(string.Format("{0}@{1},{2},{3}x{4}", key, rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top));
                return true;
            }, IntPtr.Zero);
            parts.Sort(StringComparer.OrdinalIgnoreCase);
            return string.Join("|", parts.ToArray());
        }

        private static bool TryReadBriRaw(IntPtr hCand, byte codeUsed, out uint minV, out uint curV, out uint maxV)
        {
            minV = 0; curV = 0; maxV = 0;
            bool ok = false;
            if (codeUsed == 0x10)
            {
                ok = NativeMethods.GetMonitorBrightness(hCand, out minV, out curV, out maxV);
                if (!ok)
                {
                    uint vt = 0;
                    ok = NativeMethods.GetVCPFeatureAndVCPFeatureReply(hCand, 0x10, out vt, out curV, out maxV);
                    minV = 0;
                }
            }
            else
            {
                uint vt = 0;
                ok = NativeMethods.GetVCPFeatureAndVCPFeatureReply(hCand, codeUsed, out vt, out curV, out maxV);
                minV = 0;
            }
            return ok && maxV > minV;
        }

        private static bool TryReadConRaw(IntPtr hCand, out uint minV, out uint curV, out uint maxV)
        {
            minV = 0; curV = 0; maxV = 0;
            bool ok = NativeMethods.GetMonitorContrast(hCand, out minV, out curV, out maxV);
            if (!ok)
            {
                uint vt = 0;
                ok = NativeMethods.GetVCPFeatureAndVCPFeatureReply(hCand, 0x12, out vt, out curV, out maxV);
                minV = 0;
            }
            return ok && maxV > minV;
        }

        private static bool ReadStableDdcBrightness(IntPtr hCand, byte codeUsed, out uint minV, out uint maxV, out int stablePct, out bool isStable)
        {
            minV = 0; maxV = 100; stablePct = 75; isStable = false;
            uint bMin1 = 0, bCur1 = 0, bMax1 = 0;
            if (!TryReadBriRaw(hCand, codeUsed, out bMin1, out bCur1, out bMax1)) return false;

            minV = bMin1;
            maxV = bMax1;
            int pct1 = Clamp((int)Math.Round(((double)(bCur1 - bMin1) / (bMax1 - bMin1)) * 100.0), 0, 100);

            Thread.Sleep(28);
            uint bMin2 = 0, bCur2 = 0, bMax2 = 0;
            if (!TryReadBriRaw(hCand, codeUsed, out bMin2, out bCur2, out bMax2))
            {
                stablePct = pct1;
                return true;
            }

            int pct2 = Clamp((int)Math.Round(((double)(bCur2 - bMin2) / (bMax2 - bMin2)) * 100.0), 0, 100);
            if (Math.Abs(pct1 - pct2) <= 2)
            {
                stablePct = (pct1 + pct2) / 2;
                isStable = true;
                return true;
            }

            // Tie-breaker read if monitor firmware returned fluctuating first two reads
            Thread.Sleep(32);
            uint bMin3 = 0, bCur3 = 0, bMax3 = 0;
            if (TryReadBriRaw(hCand, codeUsed, out bMin3, out bCur3, out bMax3))
            {
                int pct3 = Clamp((int)Math.Round(((double)(bCur3 - bMin3) / (bMax3 - bMin3)) * 100.0), 0, 100);
                if (Math.Abs(pct2 - pct3) <= 2)
                {
                    stablePct = (pct2 + pct3) / 2;
                    isStable = true;
                    return true;
                }
                if (Math.Abs(pct1 - pct3) <= 2)
                {
                    stablePct = (pct1 + pct3) / 2;
                    isStable = true;
                    return true;
                }
            }

            stablePct = pct1;
            isStable = false;
            return true;
        }

        private static bool ReadStableDdcContrast(IntPtr hCand, out uint minV, out uint maxV, out int stablePct, out bool isStable)
        {
            minV = 0; maxV = 100; stablePct = 75; isStable = false;
            uint cMin1 = 0, cCur1 = 0, cMax1 = 0;
            if (!TryReadConRaw(hCand, out cMin1, out cCur1, out cMax1)) return false;

            minV = cMin1;
            maxV = cMax1;
            int pct1 = Clamp((int)Math.Round(((double)(cCur1 - cMin1) / (cMax1 - cMin1)) * 100.0), 0, 100);

            Thread.Sleep(28);
            uint cMin2 = 0, cCur2 = 0, cMax2 = 0;
            if (!TryReadConRaw(hCand, out cMin2, out cCur2, out cMax2))
            {
                stablePct = pct1;
                return true;
            }

            int pct2 = Clamp((int)Math.Round(((double)(cCur2 - cMin2) / (cMax2 - cMin2)) * 100.0), 0, 100);
            if (Math.Abs(pct1 - pct2) <= 2)
            {
                stablePct = (pct1 + pct2) / 2;
                isStable = true;
                return true;
            }

            Thread.Sleep(32);
            uint cMin3 = 0, cCur3 = 0, cMax3 = 0;
            if (TryReadConRaw(hCand, out cMin3, out cCur3, out cMax3))
            {
                int pct3 = Clamp((int)Math.Round(((double)(cCur3 - cMin3) / (cMax3 - cMin3)) * 100.0), 0, 100);
                if (Math.Abs(pct2 - pct3) <= 2)
                {
                    stablePct = (pct2 + pct3) / 2;
                    isStable = true;
                    return true;
                }
                if (Math.Abs(pct1 - pct3) <= 2)
                {
                    stablePct = (pct1 + pct3) / 2;
                    isStable = true;
                    return true;
                }
            }

            stablePct = pct1;
            isStable = false;
            return true;
        }

        public List<MonitorInfoDto> RefreshMonitors()
        {
            // Flush any queued hardware writes before reading back monitor state or closing handles
            List<MonitorHandleEntry> prevSnapshot;
            lock (_sync)
            {
                prevSnapshot = new List<MonitorHandleEntry>(_entries);
            }
            var prevById = new Dictionary<string, MonitorHandleEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var prev in prevSnapshot)
            {
                prev.WaitForPendingWrites(450);
                if (prev.Info != null && !string.IsNullOrEmpty(prev.Info.id))
                {
                    prevById[prev.Info.id] = prev;
                }
            }

            var wmiIds = QueryWmiMonitorIds();
            var wmiBriList = QueryWmiBrightness();
            var wmiInternalKeys = QueryWmiInternalKeys();

            var tempEnumerated = new List<MonitorHandleEntry>();

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMon, IntPtr hdc, ref NativeMethods.RECT rc, IntPtr data) =>
            {
                var mi = new NativeMethods.MONITORINFOEX();
                mi.cbSize = Marshal.SizeOf(mi);
                if (!NativeMethods.GetMonitorInfo(hMon, ref mi)) return true;

                var dd = new NativeMethods.DISPLAY_DEVICE();
                dd.cb = Marshal.SizeOf(dd);
                NativeMethods.EnumDisplayDevices(mi.szDevice, 0, ref dd, NativeMethods.EDD_GET_DEVICE_INTERFACE_NAME);

                string pnpKey = NormalizeInstanceKey(dd.DeviceID);
                if (string.IsNullOrEmpty(pnpKey))
                {
                    pnpKey = mi.szDevice.ToUpperInvariant();
                }

                MonitorHandleEntry prevEntry = null;
                prevById.TryGetValue(pnpKey, out prevEntry);

                WmiIdInfo wmiMeta = null;
                wmiIds.TryGetValue(pnpKey, out wmiMeta);

                string hwName = "";
                string mfg = "";
                if (wmiMeta != null)
                {
                    mfg = wmiMeta.Manufacturer ?? "";
                    hwName = wmiMeta.FriendlyName ?? "";
                }

                // Match laptop internal panel by exact PnP key, model prefix, or internal connection param
                string pnpPrefix = ExtractModelPrefix(pnpKey);
                WmiBrightnessInfo matchedWmiBri = null;
                foreach (var wb in wmiBriList)
                {
                    if (string.Equals(wb.InstanceKey, pnpKey, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedWmiBri = wb;
                        break;
                    }
                }
                if (matchedWmiBri == null && wmiBriList.Count > 0)
                {
                    foreach (var wb in wmiBriList)
                    {
                        if (!string.IsNullOrEmpty(pnpPrefix) && string.Equals(wb.ShortModelPrefix, pnpPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            matchedWmiBri = wb;
                            break;
                        }
                    }
                }
                if (matchedWmiBri == null && wmiBriList.Count == 1 && wmiInternalKeys.Contains(pnpKey))
                {
                    matchedWmiBri = wmiBriList[0];
                }

                var entry = new MonitorHandleEntry(this);
                entry.HMonitor = hMon;
                if (prevEntry != null)
                {
                    entry.LastBriSetUtc = prevEntry.LastBriSetUtc;
                    entry.LastConSetUtc = prevEntry.LastConSetUtc;
                    entry.LastSetBrightness = prevEntry.LastSetBrightness;
                    entry.LastSetContrast = prevEntry.LastSetContrast;
                }

                string savedModePre = null;
                _config.monitorControlModes.TryGetValue(pnpKey, out savedModePre);

                var dto = new MonitorInfoDto
                {
                    id = pnpKey,
                    deviceName = mi.szDevice,
                    manufacturer = mfg,
                    isPrimary = (mi.dwFlags & 1) != 0,
                    bounds = new MonitorBounds
                    {
                        x = mi.rcMonitor.left,
                        y = mi.rcMonitor.top,
                        width = mi.rcMonitor.right - mi.rcMonitor.left,
                        height = mi.rcMonitor.bottom - mi.rcMonitor.top
                    },
                    supportsWarmth = true,
                    warmth = _config.monitorWarmth.ContainsKey(pnpKey) ? Clamp(_config.monitorWarmth[pnpKey], 0, 100) : 0
                };

                if (matchedWmiBri != null)
                {
                    dto.isInternal = true;
                    dto.backend = "wmi";
                    dto.supportsBrightness = true;
                    dto.supportsContrast = false;

                    int wmiBri = matchedWmiBri.CurrentBrightness;
                    if (prevEntry != null)
                    {
                        if ((DateTime.UtcNow - prevEntry.LastBriSetUtc).TotalSeconds < 5.5)
                        {
                            wmiBri = prevEntry.Info.brightness;
                        }
                        else if (prevEntry.LastSetBrightness >= 0 && Math.Abs(wmiBri - prevEntry.LastSetBrightness) <= 4)
                        {
                            wmiBri = prevEntry.LastSetBrightness;
                        }
                    }
                    dto.brightness = wmiBri;
                    dto.contrast = _config.monitorSoftwareContrast.ContainsKey(pnpKey) ? Clamp(_config.monitorSoftwareContrast[pnpKey], 0, 100) : 75;
                    entry.WmiInstanceName = matchedWmiBri.FullInstanceName;
                    entry.WmiSupportedLevels = matchedWmiBri.Levels;

                    if (string.IsNullOrEmpty(hwName))
                    {
                        hwName = "Built-in Laptop Display";
                    }
                }
                else
                {
                    dto.isInternal = wmiInternalKeys.Contains(pnpKey);
                    uint count = 0;
                    if (NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(hMon, out count) && count > 0)
                    {
                        var arr = new NativeMethods.PHYSICAL_MONITOR[count];
                        if (NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMon, count, arr))
                        {
                            entry.PhysicalArray = arr;

                            int bestIdx = 0;
                            bool foundBri = false;
                            bool briStable = false;
                            uint bMin = 0, bMax = 100;
                            int bPct = 75;
                            byte briCode = 0x10;

                            bool foundCon = false;
                            bool conStable = false;
                            uint cMin = 0, cMax = 100;
                            int cPct = 75;

                            lock (DdcBusLock)
                            {
                                for (int pIdx = 0; pIdx < count; pIdx++)
                                {
                                    IntPtr hCand = arr[pIdx].hPhysicalMonitor;
                                    if (hCand == IntPtr.Zero) continue;

                                    byte codeUsed = 0x10;
                                    uint tMin = 0, tCur = 0, tMax = 0;
                                    bool bOk = TryReadBriRaw(hCand, 0x10, out tMin, out tCur, out tMax);
                                    if (!bOk)
                                    {
                                        Thread.Sleep(20);
                                        bOk = TryReadBriRaw(hCand, 0x13, out tMin, out tCur, out tMax);
                                        if (bOk) codeUsed = 0x13;
                                    }

                                    if (bOk)
                                    {
                                        bestIdx = pIdx;
                                        briCode = codeUsed;
                                        foundBri = ReadStableDdcBrightness(hCand, codeUsed, out bMin, out bMax, out bPct, out briStable);
                                        Thread.Sleep(20);
                                        foundCon = ReadStableDdcContrast(hCand, out cMin, out cMax, out cPct, out conStable);
                                        break;
                                    }
                                }
                            }

                            entry.HPhysicalMonitor = arr[bestIdx].hPhysicalMonitor;
                            entry.BriVcpCode = briCode;

                            if (string.IsNullOrEmpty(hwName))
                            {
                                string desc = (arr[bestIdx].szPhysicalMonitorDescription ?? "").Trim();
                                if (!string.IsNullOrEmpty(desc) && !desc.Equals("Generic PnP Monitor", StringComparison.OrdinalIgnoreCase))
                                {
                                    hwName = desc;
                                }
                                else if (!string.IsNullOrEmpty(dd.DeviceString) && !dd.DeviceString.Equals("Generic PnP Monitor", StringComparison.OrdinalIgnoreCase))
                                {
                                    hwName = dd.DeviceString.Trim();
                                }
                            }

                            if (foundBri && bMax > bMin)
                            {
                                dto.backend = "ddc";
                                dto.supportsBrightness = true;
                                entry.BriMin = bMin;
                                entry.BriMax = bMax;

                                int finalBri = bPct;
                                if (prevEntry != null)
                                {
                                    if (!briStable || (DateTime.UtcNow - prevEntry.LastBriSetUtc).TotalSeconds < 6.0)
                                    {
                                        finalBri = prevEntry.Info.brightness;
                                    }
                                    else if (prevEntry.LastSetBrightness >= 0 && Math.Abs(bPct - prevEntry.LastSetBrightness) <= 4)
                                    {
                                        finalBri = prevEntry.LastSetBrightness;
                                    }
                                }
                                dto.brightness = Clamp(finalBri, 0, 100);
                            }
                            else if (prevEntry != null && prevEntry.Info.backend == "ddc")
                            {
                                // Transient DDC readback failure: preserve known DDC capability and brightness
                                dto.backend = "ddc";
                                dto.supportsBrightness = true;
                                entry.BriMin = prevEntry.BriMin;
                                entry.BriMax = prevEntry.BriMax;
                                entry.BriVcpCode = prevEntry.BriVcpCode;
                                dto.brightness = prevEntry.Info.brightness;
                            }
                            else
                            {
                                dto.backend = "software";
                                dto.supportsBrightness = true;
                                dto.brightness = _config.monitorSoftwareBrightness.ContainsKey(pnpKey) ? Clamp(_config.monitorSoftwareBrightness[pnpKey], 0, 100) : 100;
                            }

                            if (foundCon && cMax > cMin)
                            {
                                dto.supportsContrast = true;
                                entry.ConMin = cMin;
                                entry.ConMax = cMax;

                                int finalCon = cPct;
                                if (prevEntry != null)
                                {
                                    if (!conStable || (DateTime.UtcNow - prevEntry.LastConSetUtc).TotalSeconds < 6.0)
                                    {
                                        finalCon = prevEntry.Info.contrast;
                                    }
                                    else if (prevEntry.LastSetContrast >= 0 && Math.Abs(cPct - prevEntry.LastSetContrast) <= 4)
                                    {
                                        finalCon = prevEntry.LastSetContrast;
                                    }
                                }
                                dto.contrast = Clamp(finalCon, 0, 100);
                            }
                            else if (prevEntry != null && prevEntry.Info.backend == "ddc" && prevEntry.Info.supportsContrast && savedModePre != "universal")
                            {
                                dto.supportsContrast = true;
                                entry.ConMin = prevEntry.ConMin;
                                entry.ConMax = prevEntry.ConMax;
                                dto.contrast = prevEntry.Info.contrast;
                            }
                            else
                            {
                                dto.supportsContrast = false;
                                dto.contrast = _config.monitorSoftwareContrast.ContainsKey(pnpKey) ? Clamp(_config.monitorSoftwareContrast[pnpKey], 0, 100) : 75;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(dto.backend))
                    {
                        dto.backend = "software";
                        dto.supportsBrightness = true;
                        dto.supportsContrast = false;
                        dto.brightness = _config.monitorSoftwareBrightness.ContainsKey(pnpKey) ? Clamp(_config.monitorSoftwareBrightness[pnpKey], 0, 100) : 100;
                        dto.contrast = _config.monitorSoftwareContrast.ContainsKey(pnpKey) ? Clamp(_config.monitorSoftwareContrast[pnpKey], 0, 100) : 75;
                    }
                }

                string savedMode;
                if (_config.monitorControlModes.TryGetValue(pnpKey, out savedMode) &&
                    (savedMode == "hardware" || savedMode == "universal"))
                {
                    dto.controlMode = (dto.backend == "software") ? "universal" : savedMode;
                }
                else
                {
                    dto.controlMode = (dto.backend == "software") ? "universal" : "hardware";
                }

                if (dto.controlMode == "universal")
                {
                    dto.supportsContrast = true;
                    if (_config.monitorSoftwareBrightness.ContainsKey(pnpKey))
                    {
                        dto.brightness = Clamp(_config.monitorSoftwareBrightness[pnpKey], 0, 100);
                    }
                    else if (prevEntry != null && prevEntry.Info.controlMode == "universal")
                    {
                        dto.brightness = prevEntry.Info.brightness;
                    }

                    if (_config.monitorSoftwareContrast.ContainsKey(pnpKey))
                    {
                        dto.contrast = Clamp(_config.monitorSoftwareContrast[pnpKey], 0, 100);
                    }
                    else if (prevEntry != null && prevEntry.Info.controlMode == "universal")
                    {
                        dto.contrast = prevEntry.Info.contrast;
                    }
                }

                ProfileMonitorValues savedDef;
                if (_config.monitorDefaults.TryGetValue(pnpKey, out savedDef) && savedDef != null)
                {
                    dto.defaultValues = new ProfileMonitorValues
                    {
                        brightness = Clamp(savedDef.brightness, 0, 100),
                        contrast = Clamp(savedDef.contrast, 0, 100),
                        warmth = Clamp(savedDef.warmth, 0, 100)
                    };
                }
                else
                {
                    dto.defaultValues = new ProfileMonitorValues { brightness = 75, contrast = 75, warmth = 0 };
                }

                if (string.IsNullOrEmpty(hwName))
                {
                    hwName = !string.IsNullOrEmpty(mfg) ? (mfg + " Display") : ("Display " + mi.szDevice.Replace(@"\\.\", ""));
                }

                dto.hardwareName = hwName;
                string savedCustom;
                if (_config.monitorNames.TryGetValue(pnpKey, out savedCustom) && !string.IsNullOrWhiteSpace(savedCustom))
                {
                    dto.customName = savedCustom.Trim();
                }
                else
                {
                    dto.customName = hwName;
                }

                entry.Info = dto;
                tempEnumerated.Add(entry);
                return true;
            }, IntPtr.Zero);

            tempEnumerated.Sort((a, b) =>
            {
                if (a.Info.isPrimary != b.Info.isPrimary) return a.Info.isPrimary ? -1 : 1;
                int cmpX = a.Info.bounds.x.CompareTo(b.Info.bounds.x);
                if (cmpX != 0) return cmpX;
                return a.Info.bounds.y.CompareTo(b.Info.bounds.y);
            });

            var activeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < tempEnumerated.Count; i++)
            {
                tempEnumerated[i].Info.index = i + 1;
                activeIds.Add(tempEnumerated[i].Info.id);
            }

            lock (_sync)
            {
                foreach (var old in _entries) old.Dispose();
                _entries.Clear();
                _entries.AddRange(tempEnumerated);
            }

            // Re-apply stored warmth or universal contrast ramps first so GammaFailedFallbackNeeded is known
            foreach (var entry in tempEnumerated)
            {
                bool useUniversalCon = (entry.Info.controlMode == "universal" && entry.Info.contrast != 75);
                if (entry.Info.warmth > 0 || useUniversalCon)
                {
                    int swCon = (entry.Info.controlMode == "universal") ? entry.Info.contrast : 75;
                    ApplyGammaRamp(entry, swCon, entry.Info.warmth);
                }
            }

            SyncAllOverlays(activeIds, tempEnumerated);

            return GetMonitorsSnapshot();
        }

        private void SyncAllOverlays(HashSet<string> activeIds, List<MonitorHandleEntry> currentEntries)
        {
            if (_uiInvoker == null || !_uiInvoker.IsHandleCreated) return;
            Action act = () =>
            {
                var toRemove = new List<string>();
                foreach (var kv in _overlays)
                {
                    if (!activeIds.Contains(kv.Key))
                    {
                        try { kv.Value.Close(); kv.Value.Dispose(); } catch { }
                        toRemove.Add(kv.Key);
                    }
                }
                foreach (var k in toRemove) _overlays.Remove(k);

                foreach (var entry in currentEntries)
                {
                    bool useOverlay = entry.Info.controlMode == "universal" || entry.Info.backend == "software" || entry.GammaFailedFallbackNeeded;
                    UpdateOverlayForMonitor(entry, useOverlay);
                }
            };
            if (_uiInvoker.InvokeRequired) _uiInvoker.BeginInvoke(act);
            else act();
        }

        private void UpdateOverlayForMonitor(MonitorHandleEntry entry, bool useOverlay)
        {
            if (_uiInvoker == null || !_uiInvoker.IsHandleCreated || entry == null) return;
            MonitorInfoDto info = entry.Info;
            int bri = (info.controlMode == "universal" || info.backend == "software") ? info.brightness : 100;
            int fallbackWarm = entry.GammaFailedFallbackNeeded ? info.warmth : 0;

            Action act = () =>
            {
                DimOverlayForm form;
                if (!useOverlay || (bri >= 100 && fallbackWarm <= 0))
                {
                    if (_overlays.TryGetValue(info.id, out form))
                    {
                        form.ApplyOverlay(info.bounds, 100, 0, false);
                    }
                    return;
                }

                if (!_overlays.TryGetValue(info.id, out form) || form.IsDisposed)
                {
                    form = new DimOverlayForm();
                    _overlays[info.id] = form;
                }
                form.ApplyOverlay(info.bounds, bri, fallbackWarm, true);
            };
            if (_uiInvoker.InvokeRequired) _uiInvoker.BeginInvoke(act);
            else act();
        }

        public List<MonitorInfoDto> GetMonitorsSnapshot()
        {
            lock (_sync)
            {
                var list = new List<MonitorInfoDto>(_entries.Count);
                foreach (var e in _entries)
                {
                    list.Add(new MonitorInfoDto
                    {
                        id = e.Info.id,
                        index = e.Info.index,
                        deviceName = e.Info.deviceName,
                        hardwareName = e.Info.hardwareName,
                        customName = e.Info.customName,
                        manufacturer = e.Info.manufacturer,
                        backend = e.Info.backend,
                        controlMode = e.Info.controlMode,
                        isPrimary = e.Info.isPrimary,
                        isInternal = e.Info.isInternal,
                        supportsBrightness = e.Info.supportsBrightness,
                        supportsContrast = e.Info.supportsContrast,
                        supportsWarmth = e.Info.supportsWarmth,
                        brightness = e.Info.brightness,
                        contrast = e.Info.contrast,
                        warmth = e.Info.warmth,
                        defaultValues = new ProfileMonitorValues
                        {
                            brightness = e.Info.defaultValues.brightness,
                            contrast = e.Info.defaultValues.contrast,
                            warmth = e.Info.defaultValues.warmth
                        },
                        bounds = e.Info.bounds
                    });
                }
                return list;
            }
        }

        public void RenameMonitor(string id, string newName)
        {
            if (string.IsNullOrEmpty(id)) return;
            string cleaned = (newName ?? "").Trim();
            lock (_sync)
            {
                foreach (var e in _entries)
                {
                    if (string.Equals(e.Info.id, id, StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrEmpty(cleaned))
                        {
                            _config.monitorNames.Remove(id);
                            e.Info.customName = e.Info.hardwareName;
                        }
                        else
                        {
                            _config.monitorNames[id] = cleaned;
                            e.Info.customName = cleaned;
                        }
                        break;
                    }
                }
            }
            ConfigStore.Save(_config);
        }

        public void SetMonitorControlMode(string id, string mode)
        {
            if (string.IsNullOrEmpty(id)) return;
            string normalized = (mode == "universal") ? "universal" : "hardware";
            MonitorHandleEntry target = FindEntry(id);
            if (target == null) return;

            if (target.Info.backend == "software") normalized = "universal";
            target.Info.controlMode = normalized;
            target.Info.supportsContrast = (normalized == "universal") || (target.Info.backend == "ddc" && target.ConMax > target.ConMin);
            _config.monitorControlModes[target.Info.id] = normalized;
            ConfigStore.Save(_config);

            UpdateOverlayForMonitor(target, normalized == "universal" || target.GammaFailedFallbackNeeded);
            target.EnqueueUpdate(
                target.Info.brightness,
                target.Info.supportsContrast ? (int?)target.Info.contrast : null,
                target.Info.warmth
            );
        }

        public void PinMonitorDefault(string id)
        {
            MonitorHandleEntry target = FindEntry(id);
            if (target == null) return;
            var def = new ProfileMonitorValues
            {
                brightness = target.Info.brightness,
                contrast = target.Info.contrast,
                warmth = target.Info.warmth
            };
            target.Info.defaultValues = def;
            _config.monitorDefaults[target.Info.id] = def;
            ConfigStore.Save(_config);
        }

        public void ResetMonitor(string id)
        {
            MonitorHandleEntry target = FindEntry(id);
            if (target == null) return;
            var def = target.Info.defaultValues ?? new ProfileMonitorValues { brightness = 75, contrast = 75, warmth = 0 };
            SetMonitorValues(
                target.Info.id,
                def.brightness,
                target.Info.supportsContrast ? (int?)def.contrast : null,
                def.warmth
            );
        }

        public void ResetAllMonitors()
        {
            List<MonitorHandleEntry> snapshot;
            lock (_sync) { snapshot = new List<MonitorHandleEntry>(_entries); }
            foreach (var entry in snapshot)
            {
                var def = entry.Info.defaultValues ?? new ProfileMonitorValues { brightness = 75, contrast = 75, warmth = 0 };
                SetMonitorValues(
                    entry.Info.id,
                    def.brightness,
                    entry.Info.supportsContrast ? (int?)def.contrast : null,
                    def.warmth
                );
            }
            _config.activeProfileId = "";
            ConfigStore.Save(_config);
        }

        private MonitorHandleEntry FindEntry(string id)
        {
            lock (_sync)
            {
                foreach (var e in _entries)
                {
                    if (string.Equals(e.Info.id, id, StringComparison.OrdinalIgnoreCase)) return e;
                }
            }
            return null;
        }

        public void SetMonitorValues(string id, int? brightness, int? contrast, int? warmth)
        {
            MonitorHandleEntry target = FindEntry(id);
            if (target == null) return;

            if (brightness.HasValue)
            {
                target.Info.brightness = Clamp(brightness.Value, 0, 100);
                target.LastBriSetUtc = DateTime.UtcNow;
                target.LastSetBrightness = target.Info.brightness;
                if (target.Info.backend == "software" || target.Info.controlMode == "universal")
                {
                    _config.monitorSoftwareBrightness[target.Info.id] = target.Info.brightness;
                }
                bool useOverlay = target.Info.controlMode == "universal" || target.Info.backend == "software" || target.GammaFailedFallbackNeeded;
                UpdateOverlayForMonitor(target, useOverlay);
            }
            if (contrast.HasValue && target.Info.supportsContrast)
            {
                target.Info.contrast = Clamp(contrast.Value, 0, 100);
                target.LastConSetUtc = DateTime.UtcNow;
                target.LastSetContrast = target.Info.contrast;
                if (target.Info.backend != "ddc" || target.Info.controlMode == "universal")
                {
                    _config.monitorSoftwareContrast[target.Info.id] = target.Info.contrast;
                }
            }
            if (warmth.HasValue)
            {
                int w = Clamp(warmth.Value, 0, 100);
                target.Info.warmth = w;
                if (w == 0) _config.monitorWarmth.Remove(target.Info.id);
                else _config.monitorWarmth[target.Info.id] = w;
            }

            target.EnqueueUpdate(
                brightness.HasValue ? (int?)target.Info.brightness : null,
                (contrast.HasValue && target.Info.supportsContrast) ? (int?)target.Info.contrast : null,
                warmth.HasValue ? (int?)target.Info.warmth : null
            );
        }

        public void ApplyProfile(DisplayProfile profile)
        {
            if (profile == null) return;
            List<MonitorHandleEntry> snapshot;
            lock (_sync)
            {
                snapshot = new List<MonitorHandleEntry>(_entries);
            }

            for (int i = 0; i < snapshot.Count; i++)
            {
                var entry = snapshot[i];
                ProfileMonitorValues vals = null;

                if (profile.monitors != null && profile.monitors.TryGetValue(entry.Info.id, out vals) && vals != null)
                {
                }
                else if (profile.dualMode && !entry.Info.isPrimary && profile.secondaryValues != null)
                {
                    vals = profile.secondaryValues;
                }
                else
                {
                    vals = profile.defaultValues;
                }

                if (vals == null) continue;

                SetMonitorValues(
                    entry.Info.id,
                    vals.brightness,
                    entry.Info.supportsContrast ? (int?)vals.contrast : null,
                    vals.warmth
                );
            }

            _config.activeProfileId = profile.id;
            ConfigStore.Save(_config);
        }

        internal void ApplyToHardware(MonitorHandleEntry entry, int? brightness, int? contrast, int? warmth)
        {
            lock (entry.HandleLock)
            {
                if (entry.Disposed) return;

                if (brightness.HasValue)
                {
                    int b = Clamp(brightness.Value, 0, 100);
                    if (entry.Info.backend == "wmi" && !string.IsNullOrEmpty(entry.WmiInstanceName))
                    {
                        byte targetLevel = SnapToWmiLevel(entry.WmiSupportedLevels, b);
                        SetWmiBrightness(entry.WmiInstanceName, targetLevel);
                    }
                    else if (entry.Info.backend == "ddc" && entry.HPhysicalMonitor != IntPtr.Zero)
                    {
                        uint raw = (uint)Math.Round(entry.BriMin + (b / 100.0) * (entry.BriMax - entry.BriMin));
                        bool ok;
                        lock (DdcBusLock)
                        {
                            ok = (entry.BriVcpCode == 0x10)
                                ? NativeMethods.SetMonitorBrightness(entry.HPhysicalMonitor, raw)
                                : NativeMethods.SetVCPFeature(entry.HPhysicalMonitor, entry.BriVcpCode, raw);
                            if (!ok)
                            {
                                ok = NativeMethods.SetVCPFeature(entry.HPhysicalMonitor, entry.BriVcpCode, raw);
                            }
                            Thread.Sleep(35);
                        }
                        if (!ok)
                        {
                            UpdateOverlayForMonitor(entry, true);
                        }
                    }
                }

                if (contrast.HasValue && entry.Info.supportsContrast && entry.Info.backend == "ddc" && entry.HPhysicalMonitor != IntPtr.Zero)
                {
                    int c = Clamp(contrast.Value, 0, 100);
                    uint rawC = (uint)Math.Round(entry.ConMin + (c / 100.0) * (entry.ConMax - entry.ConMin));
                    lock (DdcBusLock)
                    {
                        bool ok = NativeMethods.SetMonitorContrast(entry.HPhysicalMonitor, rawC);
                        if (!ok)
                        {
                            NativeMethods.SetVCPFeature(entry.HPhysicalMonitor, 0x12, rawC);
                        }
                        Thread.Sleep(35);
                    }
                }

                bool needGammaUpdate = warmth.HasValue || (contrast.HasValue && (entry.Info.controlMode == "universal" || entry.HasCustomGammaApplied));
                if (needGammaUpdate)
                {
                    int w = Clamp(entry.Info.warmth, 0, 100);
                    int swCon = (entry.Info.controlMode == "universal") ? Clamp(entry.Info.contrast, 0, 100) : 75;
                    if (w > 0 || swCon != 75 || entry.HasCustomGammaApplied)
                    {
                        ApplyGammaRamp(entry, swCon, w);
                        if (entry.GammaFailedFallbackNeeded)
                        {
                            UpdateOverlayForMonitor(entry, true);
                        }
                    }
                }
            }
        }

        private static byte SnapToWmiLevel(byte[] levels, int requested)
        {
            byte target = (byte)Clamp(requested, 0, 100);
            if (levels == null || levels.Length == 0) return target;
            byte best = levels[0];
            int bestDiff = Math.Abs(best - target);
            for (int i = 1; i < levels.Length; i++)
            {
                int diff = Math.Abs(levels[i] - target);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best = levels[i];
                }
            }
            return best;
        }

        private static void SetWmiBrightness(string instanceName, byte brightness)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT InstanceName FROM WmiMonitorBrightnessMethods"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string inst = obj["InstanceName"] as string;
                        if (string.Equals(inst, instanceName, StringComparison.OrdinalIgnoreCase))
                        {
                            obj.InvokeMethod("WmiSetBrightness", new object[] { (uint)1, brightness });
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        private static void ApplyGammaRamp(MonitorHandleEntry entry, int softwareContrast, int warmth)
        {
            IntPtr hdc = IntPtr.Zero;
            try
            {
                hdc = NativeMethods.CreateDC(entry.Info.deviceName, null, null, IntPtr.Zero);
                if (hdc == IntPtr.Zero)
                {
                    entry.GammaFailedFallbackNeeded = true;
                    return;
                }

                int con = Clamp(softwareContrast, 0, 100);
                double factor = (con <= 75)
                    ? (0.50 + (con / 75.0) * 0.50)
                    : (1.0 + ((con - 75) / 25.0) * 0.65);

                double w = Clamp(warmth, 0, 100) / 100.0;
                double rMult = 1.0;
                double gMult = 1.0 - 0.16 * w;
                double bMult = 1.0 - 0.48 * w;

                var ramp = new NativeMethods.RAMP
                {
                    Red = new ushort[256],
                    Green = new ushort[256],
                    Blue = new ushort[256]
                };

                for (int i = 0; i < 256; i++)
                {
                    double norm = i / 255.0;
                    double cVal = (con == 75) ? norm : ((norm - 0.5) * factor + 0.5);
                    if (cVal < 0.0) cVal = 0.0;
                    if (cVal > 1.0) cVal = 1.0;

                    double baseScaled = cVal * 65535.0;
                    ramp.Red[i] = (ushort)Clamp((int)Math.Round(baseScaled * rMult), 0, 65535);
                    ramp.Green[i] = (ushort)Clamp((int)Math.Round(baseScaled * gMult), 0, 65535);
                    ramp.Blue[i] = (ushort)Clamp((int)Math.Round(baseScaled * bMult), 0, 65535);
                }

                bool ok = NativeMethods.SetDeviceGammaRamp(hdc, ref ramp);
                entry.GammaFailedFallbackNeeded = !ok;
                entry.HasCustomGammaApplied = ok && (warmth > 0 || con != 75);
            }
            catch
            {
                entry.GammaFailedFallbackNeeded = true;
            }
            finally
            {
                if (hdc != IntPtr.Zero) NativeMethods.DeleteDC(hdc);
            }
        }

        public void CleanupVisualEffectsOnExit()
        {
            List<MonitorHandleEntry> snapshot;
            lock (_sync) { snapshot = new List<MonitorHandleEntry>(_entries); }
            foreach (var entry in snapshot)
            {
                if (entry.HasCustomGammaApplied)
                {
                    ApplyGammaRamp(entry, 75, 0);
                }
            }

            foreach (var kv in _overlays)
            {
                try { kv.Value.Close(); kv.Value.Dispose(); } catch { }
            }
            _overlays.Clear();
        }

        public void Dispose()
        {
            CleanupVisualEffectsOnExit();
            lock (_sync)
            {
                foreach (var e in _entries) e.Dispose();
                _entries.Clear();
            }
        }
    }

    internal class IdentifyOsdForm : Form
    {
        private readonly System.Windows.Forms.Timer _closeTimer;
        private readonly string _badgeText;
        private readonly string _subText;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        public IdentifyOsdForm(MonitorInfoDto mon)
        {
            _badgeText = string.Format("{0}  \u2022  {1}", mon.index, mon.customName);
            _subText = string.Format("{0}  |  {1}x{2}  |  {3}% Brightness", mon.hardwareName, mon.bounds.width, mon.bounds.height, mon.brightness);

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.FromArgb(12, 16, 28);
            this.Opacity = 0.94;

            int w = 440;
            int h = 112;
            int x = mon.bounds.x + (mon.bounds.width - w) / 2;
            int y = mon.bounds.y + mon.bounds.height - h - 72;
            this.Bounds = new Rectangle(x, y, w, h);

            this.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(1, 1, this.Width - 3, this.Height - 3);
                using (var path = RoundedRect(rect, 18))
                using (var brush = new LinearGradientBrush(rect, Color.FromArgb(20, 26, 46), Color.FromArgb(12, 15, 26), 90f))
                using (var pen = new Pen(Color.FromArgb(99, 102, 241), 2f))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }

                using (var fTitle = new Font("Segoe UI", 17f, FontStyle.Bold))
                using (var fSub = new Font("Segoe UI", 9.5f, FontStyle.Regular))
                using (var bWhite = new SolidBrush(Color.White))
                using (var bMuted = new SolidBrush(Color.FromArgb(165, 180, 210)))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                {
                    e.Graphics.DrawString(_badgeText, fTitle, bWhite, new RectangleF(16, 18, this.Width - 32, 42), sf);
                    e.Graphics.DrawString(_subText, fSub, bMuted, new RectangleF(16, 62, this.Width - 32, 28), sf);
                }
            };

            _closeTimer = new System.Windows.Forms.Timer();
            _closeTimer.Interval = 2200;
            _closeTimer.Tick += (s, e) =>
            {
                _closeTimer.Stop();
                this.Close();
                this.Dispose();
            };
            _closeTimer.Start();
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class MainForm : Form
    {
        private readonly WebView2 webView;
        private readonly NotifyIcon trayIcon;
        private readonly ContextMenuStrip trayMenu;
        private readonly AppConfig config;
        private readonly DisplayEngine engine;
        private readonly JavaScriptSerializer json = new JavaScriptSerializer { MaxJsonLength = 10485760 };
        private bool reallyExiting = false;
        private bool _wasMaximizedBeforeHide = false;
        private readonly bool startMinimized;
        private bool hotkeyRegistered = false;
        private System.Windows.Forms.Timer refreshDebounce;
        private System.Windows.Forms.Timer lateDdcRetryTimer;
        private System.Windows.Forms.Timer topologyWatchTimer;
        private System.Windows.Forms.Timer configSaveDebounce;
        private string lastTopologySig = "";

        public MainForm(bool startInTray)
        {
            this.startMinimized = startInTray;
            this.config = ConfigStore.Load();
            this.engine = new DisplayEngine(this.config, this);

            float dpiScale = 1.0f;
            try
            {
                using (var g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    if (g.DpiX > 0) dpiScale = Math.Max(1.0f, Math.Min(2.5f, g.DpiX / 96.0f));
                }
            }
            catch { }

            Rectangle workArea = Screen.FromPoint(Cursor.Position).WorkingArea;
            int initW = Math.Min((int)Math.Round(1120 * dpiScale), Math.Max(420, workArea.Width - 32));
            int initH = Math.Min((int)Math.Round(790 * dpiScale), Math.Max(460, workArea.Height - 32));
            int minW = Math.Min((int)Math.Round(390 * dpiScale), Math.Max(320, workArea.Width - 24));
            int minH = Math.Min((int)Math.Round(460 * dpiScale), Math.Max(360, workArea.Height - 24));

            this.Text = "LumaDeck — Multi-Display Brightness Studio";
            this.Size = new Size(initW, initH);
            this.MinimumSize = new Size(minW, minH);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(
                workArea.Left + Math.Max(0, (workArea.Width - initW) / 2),
                workArea.Top + Math.Max(0, (workArea.Height - initH) / 2));
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(7, 9, 16);
            this.DoubleBuffered = true;
            this.Resize += (s, ev) => NotifyWindowState();

            Icon appIcon = null;
            try { appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            if (appIcon == null) appIcon = CreateTrayIcon();
            this.Icon = appIcon;

            trayMenu = new ContextMenuStrip();
            trayIcon = new NotifyIcon
            {
                Icon = appIcon,
                Text = "LumaDeck — Display Brightness Studio",
                ContextMenuStrip = trayMenu,
                Visible = true
            };
            trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (this.Visible && this.WindowState != FormWindowState.Minimized)
                        HideToTray();
                    else
                        ShowWindow();
                }
            };
            RebuildTrayMenu();

            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            webView.DefaultBackgroundColor = Color.FromArgb(7, 9, 16);
            this.Controls.Add(webView);

            refreshDebounce = new System.Windows.Forms.Timer();
            refreshDebounce.Interval = 550;
            refreshDebounce.Tick += (s, e) =>
            {
                refreshDebounce.Stop();
                PerformAutoMonitorRescan("Monitors updated");
            };

            lateDdcRetryTimer = new System.Windows.Forms.Timer();
            lateDdcRetryTimer.Interval = 2200;
            lateDdcRetryTimer.Tick += (s, e) =>
            {
                lateDdcRetryTimer.Stop();
                var monitors = engine.RefreshMonitors();
                RebuildTrayMenu();
                PushStateToUi(monitors, null);
            };

            topologyWatchTimer = new System.Windows.Forms.Timer();
            topologyWatchTimer.Interval = 4000;
            topologyWatchTimer.Tick += (s, e) =>
            {
                try
                {
                    string sig = engine.GetTopologySignature();
                    if (!string.IsNullOrEmpty(sig) && !string.Equals(sig, lastTopologySig, StringComparison.Ordinal))
                    {
                        lastTopologySig = sig;
                        ScheduleAutoRescan();
                    }
                }
                catch { }
            };
            topologyWatchTimer.Start();

            configSaveDebounce = new System.Windows.Forms.Timer();
            configSaveDebounce.Interval = 400;
            configSaveDebounce.Tick += (s, e) =>
            {
                configSaveDebounce.Stop();
                ConfigStore.Save(config);
            };

            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;

            this.Load += MainForm_Load;
            this.FormClosing += MainForm_FormClosing;
        }

        private void ScheduleAutoRescan()
        {
            if (!this.IsHandleCreated) return;
            this.BeginInvoke(new Action(() =>
            {
                refreshDebounce.Stop();
                refreshDebounce.Start();
                lateDdcRetryTimer.Stop();
                lateDdcRetryTimer.Start();
            }));
        }

        private void PerformAutoMonitorRescan(string toastMessage)
        {
            lastTopologySig = engine.GetTopologySignature();
            var monitors = engine.RefreshMonitors();
            RebuildTrayMenu();
            PushStateToUi(monitors, toastMessage);
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            ScheduleAutoRescan();
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                ScheduleAutoRescan();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int pref = 2; // DWMWCP_ROUND on Windows 11
                NativeMethods.DwmSetWindowAttribute(this.Handle, 33, ref pref, sizeof(int));
            }
            catch { }
            UpdateHotkeyRegistration();
        }

        private void UpdateHotkeyRegistration()
        {
            if (!this.IsHandleCreated) return;
            if (hotkeyRegistered)
            {
                NativeMethods.UnregisterHotKey(this.Handle, NativeMethods.HOTKEY_TOGGLE_ID);
                hotkeyRegistered = false;
            }
            if (config.globalHotkeyEnabled)
            {
                hotkeyRegistered = NativeMethods.RegisterHotKey(
                    this.Handle,
                    NativeMethods.HOTKEY_TOGGLE_ID,
                    NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT,
                    NativeMethods.VK_B);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == NativeMethods.HOTKEY_TOGGLE_ID)
            {
                if (this.Visible && this.WindowState != FormWindowState.Minimized)
                    HideToTray();
                else
                    ShowWindow();
            }
            else if (m.Msg == NativeMethods.WM_SHOWME)
            {
                ShowWindow();
            }
            else if (m.Msg == NativeMethods.WM_DISPLAYCHANGE ||
                     (m.Msg == NativeMethods.WM_DEVICECHANGE && m.WParam.ToInt32() == NativeMethods.DBT_DEVNODES_CHANGED))
            {
                string sig = engine.GetTopologySignature();
                if (!string.Equals(sig, lastTopologySig, StringComparison.Ordinal))
                {
                    lastTopologySig = sig;
                    ScheduleAutoRescan();
                }
            }
            base.WndProc(ref m);
        }

        private static Icon CreateTrayIcon()
        {
            using (var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using (var bgBrush = new LinearGradientBrush(new Rectangle(1, 1, 30, 30), Color.FromArgb(56, 189, 248), Color.FromArgb(99, 102, 241), 45f))
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(1, 1, 30, 30);
                    g.FillPath(bgBrush, path);
                }

                using (var whiteBrush = new SolidBrush(Color.White))
                {
                    g.FillEllipse(whiteBrush, 10, 10, 12, 12);
                }
                using (var pen = new Pen(Color.White, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    float cx = 16f, cy = 16f;
                    for (int i = 0; i < 8; i++)
                    {
                        double ang = i * Math.PI / 4.0;
                        float x1 = cx + (float)(Math.Cos(ang) * 8.5);
                        float y1 = cy + (float)(Math.Sin(ang) * 8.5);
                        float x2 = cx + (float)(Math.Cos(ang) * 11.5);
                        float y2 = cy + (float)(Math.Sin(ang) * 11.5);
                        g.DrawLine(pen, x1, y1, x2, y2);
                    }
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        private void RebuildTrayMenu()
        {
            trayMenu.Items.Clear();
            var openItem = new ToolStripMenuItem("Open LumaDeck");
            openItem.Font = new Font(openItem.Font, FontStyle.Bold);
            openItem.Click += (s, e) => ShowWindow();
            trayMenu.Items.Add(openItem);
            trayMenu.Items.Add(new ToolStripSeparator());

            var profilesHeader = new ToolStripMenuItem("Quick Profiles") { Enabled = false };
            trayMenu.Items.Add(profilesHeader);

            foreach (var p in config.profiles)
            {
                DisplayProfile captured = p;
                string label = string.Format("{0}  {1}", captured.icon, captured.name);
                var item = new ToolStripMenuItem(label);
                if (string.Equals(config.activeProfileId, captured.id, StringComparison.OrdinalIgnoreCase))
                {
                    item.Checked = true;
                }
                item.Click += (s, e) =>
                {
                    engine.ApplyProfile(captured);
                    RebuildTrayMenu();
                    PushStateToUi(engine.GetMonitorsSnapshot(), null);
                };
                trayMenu.Items.Add(item);
            }

            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Reset All Monitors to Default", null, (s, e) =>
            {
                engine.ResetAllMonitors();
                RebuildTrayMenu();
                PushStateToUi(engine.GetMonitorsSnapshot(), "All monitors reset to default");
            });
            trayMenu.Items.Add("Identify Displays", null, (s, e) => ShowIdentifyOsd());
            trayMenu.Items.Add("Refresh Displays", null, (s, e) => PerformAutoMonitorRescan("Displays refreshed"));
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Exit LumaDeck", null, (s, e) => ExitApplication());
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            if (startMinimized)
            {
                HideToTray();
            }

            try
            {
                string userDataFolder = Path.Combine(ConfigStore.DataDir, "WebView2");
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, null);
                await webView.EnsureCoreWebView2Async(env);

                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;

                string uiDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui");
                if (!Directory.Exists(uiDir))
                {
                    uiDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\src\ui"));
                }

                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "lumadeck.local",
                    uiDir,
                    CoreWebView2HostResourceAccessKind.Allow);

                webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                webView.CoreWebView2.NavigationCompleted += (s2, e2) =>
                {
                    PerformAutoMonitorRescan(null);
                };

                webView.CoreWebView2.Navigate("https://lumadeck.local/index.html");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to initialize LumaDeck UI:\n" + ex.Message, "LumaDeck", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var msg = json.Deserialize<Dictionary<string, object>>(e.WebMessageAsJson);
                if (msg == null || !msg.ContainsKey("type")) return;
                string type = Convert.ToString(msg["type"]);

                if (type == "init" || type == "refresh")
                {
                    PerformAutoMonitorRescan(type == "refresh" ? "Displays refreshed" : null);
                }
                else if (type == "setMonitor")
                {
                    string id = Convert.ToString(msg["id"]);
                    int? bri = msg.ContainsKey("brightness") && msg["brightness"] != null ? (int?)Convert.ToInt32(msg["brightness"]) : null;
                    int? con = msg.ContainsKey("contrast") && msg["contrast"] != null ? (int?)Convert.ToInt32(msg["contrast"]) : null;
                    int? warm = msg.ContainsKey("warmth") && msg["warmth"] != null ? (int?)Convert.ToInt32(msg["warmth"]) : null;

                    engine.SetMonitorValues(id, bri, con, warm);
                    if (!string.IsNullOrEmpty(config.activeProfileId))
                    {
                        config.activeProfileId = "";
                        RebuildTrayMenu();
                    }
                    configSaveDebounce.Stop();
                    configSaveDebounce.Start();
                }
                else if (type == "setAllMonitors")
                {
                    int? bri = msg.ContainsKey("brightness") && msg["brightness"] != null ? (int?)Convert.ToInt32(msg["brightness"]) : null;
                    int? con = msg.ContainsKey("contrast") && msg["contrast"] != null ? (int?)Convert.ToInt32(msg["contrast"]) : null;
                    int? warm = msg.ContainsKey("warmth") && msg["warmth"] != null ? (int?)Convert.ToInt32(msg["warmth"]) : null;

                    var mons = engine.GetMonitorsSnapshot();
                    foreach (var m in mons)
                    {
                        engine.SetMonitorValues(m.id, bri, m.supportsContrast ? con : null, warm);
                    }
                    config.activeProfileId = "";
                    RebuildTrayMenu();
                    configSaveDebounce.Stop();
                    configSaveDebounce.Start();
                }
                else if (type == "setControlMode")
                {
                    string id = Convert.ToString(msg["id"]);
                    string mode = Convert.ToString(msg["mode"]);
                    engine.SetMonitorControlMode(id, mode);
                    PushStateToUi(engine.GetMonitorsSnapshot(), null);
                }
                else if (type == "pinDefault")
                {
                    string id = Convert.ToString(msg["id"]);
                    engine.PinMonitorDefault(id);
                    PushStateToUi(engine.GetMonitorsSnapshot(), "Saved current values as monitor default");
                }
                else if (type == "resetMonitor")
                {
                    string id = Convert.ToString(msg["id"]);
                    engine.ResetMonitor(id);
                    config.activeProfileId = "";
                    RebuildTrayMenu();
                    PushStateToUi(engine.GetMonitorsSnapshot(), "Monitor reset to default");
                }
                else if (type == "resetAllMonitors")
                {
                    engine.ResetAllMonitors();
                    RebuildTrayMenu();
                    PushStateToUi(engine.GetMonitorsSnapshot(), "All monitors reset to default");
                }
                else if (type == "renameMonitor")
                {
                    string id = Convert.ToString(msg["id"]);
                    string name = msg.ContainsKey("name") ? Convert.ToString(msg["name"]) : "";
                    engine.RenameMonitor(id, name);
                    PushStateToUi(engine.GetMonitorsSnapshot(), null);
                }
                else if (type == "applyProfile")
                {
                    string pid = Convert.ToString(msg["id"]);
                    DisplayProfile found = FindProfile(pid);
                    if (found != null)
                    {
                        engine.ApplyProfile(found);
                        RebuildTrayMenu();
                        PushStateToUi(engine.GetMonitorsSnapshot(), null);
                    }
                }
                else if (type == "saveProfile")
                {
                    var rawProf = json.Deserialize<DisplayProfile>(json.Serialize(msg["profile"]));
                    if (rawProf != null)
                    {
                        SaveOrUpdateProfile(rawProf);
                        RebuildTrayMenu();
                        PushStateToUi(engine.GetMonitorsSnapshot(), null);
                    }
                }
                else if (type == "deleteProfile")
                {
                    string pid = Convert.ToString(msg["id"]);
                    config.profiles.RemoveAll(p => !p.builtin && string.Equals(p.id, pid, StringComparison.OrdinalIgnoreCase));
                    if (string.Equals(config.activeProfileId, pid, StringComparison.OrdinalIgnoreCase))
                    {
                        config.activeProfileId = "";
                    }
                    ConfigStore.Save(config);
                    RebuildTrayMenu();
                    PushStateToUi(engine.GetMonitorsSnapshot(), null);
                }
                else if (type == "resetBuiltins")
                {
                    var customOnly = new List<DisplayProfile>();
                    foreach (var p in config.profiles)
                    {
                        if (!p.builtin) customOnly.Add(p);
                    }
                    config.profiles = ConfigStore.CreateBuiltinProfiles();
                    config.profiles.AddRange(customOnly);
                    ConfigStore.Save(config);
                    RebuildTrayMenu();
                    PushStateToUi(engine.GetMonitorsSnapshot(), null);
                }
                else if (type == "updateSettings")
                {
                    if (msg.ContainsKey("syncAll")) config.syncAll = Convert.ToBoolean(msg["syncAll"]);
                    if (msg.ContainsKey("minimizeToTray")) config.minimizeToTray = Convert.ToBoolean(msg["minimizeToTray"]);
                    if (msg.ContainsKey("startWithWindows"))
                    {
                        config.startWithWindows = Convert.ToBoolean(msg["startWithWindows"]);
                        ConfigStore.SetStartup(config.startWithWindows);
                    }
                    if (msg.ContainsKey("globalHotkeyEnabled"))
                    {
                        config.globalHotkeyEnabled = Convert.ToBoolean(msg["globalHotkeyEnabled"]);
                        UpdateHotkeyRegistration();
                    }
                    ConfigStore.Save(config);
                    PushStateToUi(engine.GetMonitorsSnapshot(), null);
                }
                else if (type == "identify")
                {
                    ShowIdentifyOsd();
                }
                else if (type == "openUrl")
                {
                    string rawUrl = msg.ContainsKey("url") ? Convert.ToString(msg["url"]) : "";
                    Uri parsed;
                    if (Uri.TryCreate(rawUrl, UriKind.Absolute, out parsed) &&
                        (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = parsed.AbsoluteUri,
                            UseShellExecute = true
                        });
                    }
                }
                else if (type == "dragWindow")
                {
                    if (this.WindowState == FormWindowState.Normal)
                    {
                        NativeMethods.ReleaseCapture();
                        NativeMethods.SendMessage(this.Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HT_CAPTION, 0);
                    }
                }
                else if (type == "resizeEdge")
                {
                    if (this.WindowState == FormWindowState.Normal && msg.ContainsKey("edge"))
                    {
                        int edge = Convert.ToInt32(msg["edge"]);
                        if (edge >= 10 && edge <= 17)
                        {
                            NativeMethods.ReleaseCapture();
                            NativeMethods.SendMessage(this.Handle, NativeMethods.WM_NCLBUTTONDOWN, edge, 0);
                        }
                    }
                }
                else if (type == "toggleMaximize")
                {
                    if (this.WindowState == FormWindowState.Maximized)
                    {
                        this.WindowState = FormWindowState.Normal;
                    }
                    else
                    {
                        ApplyMaximizedBounds();
                        this.WindowState = FormWindowState.Maximized;
                    }
                    NotifyWindowState();
                }
                else if (type == "minimizeWindow")
                {
                    this.WindowState = FormWindowState.Minimized;
                }
                else if (type == "closeWindow")
                {
                    if (config.minimizeToTray)
                        HideToTray();
                    else
                        ExitApplication();
                }
                else if (type == "exitApp")
                {
                    ExitApplication();
                }
            }
            catch { }
        }

        private DisplayProfile FindProfile(string id)
        {
            foreach (var p in config.profiles)
            {
                if (string.Equals(p.id, id, StringComparison.OrdinalIgnoreCase)) return p;
            }
            return null;
        }

        private void SaveOrUpdateProfile(DisplayProfile incoming)
        {
            if (string.IsNullOrEmpty(incoming.id))
            {
                incoming.id = "custom-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                incoming.builtin = false;
            }
            if (incoming.monitors == null) incoming.monitors = new Dictionary<string, ProfileMonitorValues>();
            if (incoming.defaultValues == null) incoming.defaultValues = new ProfileMonitorValues { brightness = 75, contrast = 75, warmth = 0 };

            int idx = -1;
            for (int i = 0; i < config.profiles.Count; i++)
            {
                if (string.Equals(config.profiles[i].id, incoming.id, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }

            if (idx >= 0)
            {
                incoming.builtin = config.profiles[idx].builtin;
                config.profiles[idx] = incoming;
            }
            else
            {
                incoming.builtin = false;
                config.profiles.Add(incoming);
            }
            ConfigStore.Save(config);
        }

        private void NotifyWindowState()
        {
            try
            {
                if (webView == null || webView.CoreWebView2 == null) return;
                var payload = new Dictionary<string, object>
                {
                    { "type", "windowState" },
                    { "maximized", this.WindowState == FormWindowState.Maximized }
                };
                webView.CoreWebView2.PostWebMessageAsJson(json.Serialize(payload));
            }
            catch { }
        }

        private void PushStateToUi(List<MonitorInfoDto> monitors, string toastText)
        {
            if (webView == null || webView.CoreWebView2 == null) return;
            var payload = new Dictionary<string, object>
            {
                { "type", "state" },
                { "monitors", monitors },
                { "profiles", config.profiles },
                { "activeProfileId", config.activeProfileId ?? "" },
                { "toast", toastText ?? "" },
                { "settings", new Dictionary<string, object> {
                    { "syncAll", config.syncAll },
                    { "startWithWindows", config.startWithWindows },
                    { "minimizeToTray", config.minimizeToTray },
                    { "globalHotkeyEnabled", config.globalHotkeyEnabled }
                }}
            };
            webView.CoreWebView2.PostWebMessageAsJson(json.Serialize(payload));
        }

        private void ShowIdentifyOsd()
        {
            var mons = engine.GetMonitorsSnapshot();
            foreach (var m in mons)
            {
                var osd = new IdentifyOsdForm(m);
                osd.Show();
            }
        }

        private void ApplyMaximizedBounds()
        {
            Screen scr = Screen.FromHandle(this.Handle);
            Rectangle scrWork = scr.WorkingArea;
            this.MaximizedBounds = new Rectangle(
                scrWork.X - scr.Bounds.X,
                scrWork.Y - scr.Bounds.Y,
                scrWork.Width,
                scrWork.Height);
        }

        private void ShowWindow()
        {
            this.Show();
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            if (_wasMaximizedBeforeHide)
            {
                _wasMaximizedBeforeHide = false;
                ApplyMaximizedBounds();
                this.WindowState = FormWindowState.Maximized;
            }
            if (webView != null) webView.Visible = true;
            this.Opacity = 1;
            this.Activate();
            NativeMethods.SetForegroundWindow(this.Handle);
            NotifyWindowState();
            PerformAutoMonitorRescan(null);
        }

        private void HideToTray()
        {
            try
            {
                bool wasMax = (this.WindowState == FormWindowState.Maximized);
                _wasMaximizedBeforeHide = wasMax;
                if (webView != null) webView.Visible = false;
                if (wasMax)
                {
                    // Restore before hiding so WinForms/WebView2 never traps a maximized borderless window in a black frame.
                    this.WindowState = FormWindowState.Normal;
                }
                this.Opacity = 0;
                this.Hide();
            }
            catch { }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!reallyExiting && config.minimizeToTray && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }
            Cleanup();
        }

        private void ExitApplication()
        {
            reallyExiting = true;
            try
            {
                this.Opacity = 0;
                if (webView != null) webView.Visible = false;
                if (this.WindowState == FormWindowState.Maximized)
                {
                    this.WindowState = FormWindowState.Normal;
                }
                this.Hide();
            }
            catch { }
            Cleanup();
            try
            {
                if (webView != null && !webView.IsDisposed)
                {
                    webView.Dispose();
                }
            }
            catch { }
            Application.Exit();
        }

        private void Cleanup()
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            if (topologyWatchTimer != null) { topologyWatchTimer.Stop(); topologyWatchTimer.Dispose(); }
            if (refreshDebounce != null) { refreshDebounce.Stop(); refreshDebounce.Dispose(); }
            if (lateDdcRetryTimer != null) { lateDdcRetryTimer.Stop(); lateDdcRetryTimer.Dispose(); }
            if (configSaveDebounce != null)
            {
                configSaveDebounce.Stop();
                configSaveDebounce.Dispose();
                ConfigStore.Save(config);
            }
            if (hotkeyRegistered && this.IsHandleCreated)
            {
                NativeMethods.UnregisterHotKey(this.Handle, NativeMethods.HOTKEY_TOGGLE_ID);
                hotkeyRegistered = false;
            }
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            if (trayMenu != null)
            {
                trayMenu.Dispose();
            }
            if (engine != null)
            {
                engine.Dispose();
            }
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            bool createdNew;
            using (var mutex = new Mutex(true, @"Local\LumaDeck_SingleInstance_Mutex", out createdNew))
            {
                if (!createdNew)
                {
                    NativeMethods.PostMessage(NativeMethods.HWND_BROADCAST, NativeMethods.WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
                    return;
                }

                NativeMethods.EnableBestDpiAwareness();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                bool startInTray = false;
                if (args != null)
                {
                    foreach (string a in args)
                    {
                        if (string.Equals(a, "--tray", StringComparison.OrdinalIgnoreCase))
                        {
                            startInTray = true;
                        }
                    }
                }

                Application.Run(new MainForm(startInTray));
            }
        }
    }
}
