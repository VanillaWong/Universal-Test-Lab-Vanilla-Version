// ModernShell.Support.cs
// Small view models, palette/text/glass/DWM helpers and shell storage.
// Split from ModernShell.cs during the 2026-09-05 partial-class refactor; members are byte-identical.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using Microsoft.Win32;

namespace UniversalTestLab
{
    internal sealed class AircraftView
    {
        public Aircraft Source { get; private set; }
        public string Name { get { return Source.Display; } }
        public string Meta { get { return Source.Nation + "  •  RANK " + Roman(Source.Rank) + "  •  " + Source.Kind.ToUpperInvariant(); } }
        public string Nation { get { return Source.Nation; } }
        public string Kind { get { return Source.Kind; } }
        public int Rank { get { return Source.Rank; } }

        public AircraftView(Aircraft source) { Source = source; }
        public override string ToString() { return Name; }

        private static string Roman(int rank)
        {
            string[] values = { "—", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
            return rank >= 0 && rank < values.Length ? values[rank] : rank.ToString(CultureInfo.InvariantCulture);
        }
    }
    internal sealed class TargetView
    {
        public TargetUnit Source { get; private set; }
        public string Name { get { return Source.Display; } }
        public string Nation { get { return Source.Nation; } }
        public int Rank { get { return Source.Rank; } }
        public TargetView(TargetUnit source) { Source = source; }
        public override string ToString() { return Name; }
    }
    internal sealed class AmmoPreset
    {
        public string Name;
        public string VehicleId;
        public GroundAmmoLoadout[] Slots = new GroundAmmoLoadout[4];
    }
    internal static class ModernShellStorage
    {
        private static string AmmoPresetPath
        {
            get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniversalTestLab", "ammo_loadouts.tsv"); }
        }

        public static List<AmmoPreset> LoadAmmoPresets()
        {
            List<AmmoPreset> result = new List<AmmoPreset>();
            try
            {
                List<object> list = ConfigStore.GetList("ammo_loadouts");
                if (list == null) return result;
                foreach (object item in list)
                {
                    Dictionary<string, object> o = item as Dictionary<string, object>;
                    if (o == null) continue;
                    AmmoPreset preset = new AmmoPreset { Name = Str(o, "name"), VehicleId = Str(o, "vehicle_id") };
                    if (String.IsNullOrWhiteSpace(preset.Name)) continue;
                    List<object> slots = ListOf(o, "slots");
                    if (slots != null)
                    {
                        foreach (object s in slots)
                        {
                            Dictionary<string, object> so = s as Dictionary<string, object>;
                            if (so == null) continue;
                            int slot = Int(so, "slot", -1);
                            if (slot < 0 || slot > 3) continue;
                            preset.Slots[slot] = new GroundAmmoLoadout { Slot = slot, Count = Math.Max(1, Int(so, "count", 1)), SourceBlk = Str(so, "source_blk"), BulletName = Str(so, "bullet_name") };
                        }
                    }
                    result.Add(preset);
                }
            }
            catch { }
            return result;
        }

        public static void SaveAmmoPreset(AmmoPreset preset)
        {
            try
            {
                List<AmmoPreset> all = LoadAmmoPresets();
                all.RemoveAll(x => x.Name != null && x.VehicleId != null && x.Name.Equals(preset.Name, StringComparison.OrdinalIgnoreCase) && x.VehicleId.Equals(preset.VehicleId, StringComparison.OrdinalIgnoreCase));
                all.Add(preset);
                SaveAmmoPresets(all);
            }
            catch { }
        }

        public static void SaveAmmoPresets(List<AmmoPreset> all)
        {
            try
            {
                List<object> list = new List<object>();
                foreach (AmmoPreset item in all)
                {
                    Dictionary<string, object> o = new Dictionary<string, object>();
                    o.Add("name", item.Name ?? String.Empty);
                    o.Add("vehicle_id", item.VehicleId ?? String.Empty);
                    List<object> slots = new List<object>();
                    for (int s = 0; s < 4; s++)
                    {
                        GroundAmmoLoadout slot = item.Slots == null || s >= item.Slots.Length ? null : item.Slots[s];
                        if (slot == null) continue;
                        Dictionary<string, object> so = new Dictionary<string, object>();
                        so.Add("slot", s);
                        so.Add("count", Math.Max(1, slot.Count));
                        so.Add("source_blk", slot.SourceBlk ?? String.Empty);
                        so.Add("bullet_name", slot.BulletName ?? String.Empty);
                        slots.Add(so);
                    }
                    o.Add("slots", slots);
                    list.Add(o);
                }
                ConfigStore.SetList("ammo_loadouts", list);
                ConfigStore.Save();
            }
            catch { }
        }

        public static void DeleteAmmoPreset(string name, string vehicleId)
        {
            try
            {
                List<AmmoPreset> all = LoadAmmoPresets();
                all.RemoveAll(x => x.Name != null && x.VehicleId != null && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && x.VehicleId.Equals(vehicleId, StringComparison.OrdinalIgnoreCase));
                SaveAmmoPresets(all);
            }
            catch { }
        }

        internal static string Str(Dictionary<string, object> o, string key)
        {
            object v;
            return o != null && o.TryGetValue(key, out v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : "";
        }

        internal static int Int(Dictionary<string, object> o, string key, int fallback)
        {
            object v;
            if (o != null && o.TryGetValue(key, out v) && v != null)
            {
                try { return Convert.ToInt32(v, CultureInfo.InvariantCulture); }
                catch { }
            }
            return fallback;
        }

        internal static List<object> ListOf(Dictionary<string, object> o, string key)
        {
            object v;
            if (o != null && o.TryGetValue(key, out v) && v != null)
            {
                if (v is List<object>) return (List<object>)v;
                if (v is object[]) return new List<object>((object[])v);
                if (v is System.Collections.ArrayList)
                {
                    List<object> list = new List<object>();
                    foreach (object x in (System.Collections.ArrayList)v) list.Add(x);
                    return list;
                }
            }
            return null;
        }
    }
    internal sealed class EraPreset
    {
        public string Name;
        public string[] GroundIds;
        public string[] AirIds;
        public int[] AirCounts;
        public string ShipId;
        public int ShipCount;

        public EraPreset(string name, string[] ground, string[] air, int[] airCounts, string ship, int shipCount)
        {
            Name = name;
            GroundIds = ground;
            AirIds = air;
            AirCounts = airCounts;
            ShipId = ship;
            ShipCount = shipCount;
        }
    }
    internal sealed class WeaponView
    {
        public DonorWeapon Source { get; private set; }
        public string Name { get { return Source.Name; } }
        public string Category { get { return Source.Category; } }
        public string Ammo { get { return Source.Bullets.ToString(CultureInfo.InvariantCulture); } }
        public string Mass { get { return Source.TotalMass.ToString("0.0", CultureInfo.InvariantCulture) + " kg"; } }
        public string Mode { get; private set; }
        public WeaponView(DonorWeapon source, bool injected) { Source = source; Mode = injected ? "INJECTED" : "NATIVE"; }
    }
    internal static class ModernText
{
    public static bool Chinese = true;
    public static string L(string en, string zh)
    {
        return Chinese ? zh : en;
    }

    // XAML placeholder texts (buttons, tab captions, labels) are static in the
    // template and cannot call L(), so the window replaces them after loading
    // by walking the visual tree. This map only applies when Chinese is active.
    public static readonly Dictionary<string, string> XamlMap = new Dictionary<string, string>
    {
        { "GAME DIRECTORY", "游戏目录" },
        { "BROWSE", "浏览" },
        { "SYNC BASE", "同步基础" },
        { "MISSIONS", "任务" },
        { "PRESETS", "预设" },
        { "SUPPORT", "支持" },
        { "VEHICLE", "载具" },
        { "TARGETS", "目标" },
        { "OPTIONS", "选项" },
        { "GARAGE", "机库" },
        { "EXPERIMENTAL", "实验" },
        { "CHOOSE VEHICLE", "选择载具" },
        { "Air and ground vehicles", "空中与地面载具" },
        { "SEARCH", "搜索" },
        { "NATION", "国家" },
        { "RANK", "等级" },
        { "TYPE", "类型" },
        { "AVAILABLE VEHICLES", "可用载具" },
        { "BUILD LOADOUT", "构建挂载" },
        { "Select a station, then mount a weapon", "选择挂架，然后挂载武器" },
        { "WEAPON SOURCE", "武器来源" },
        { "INJECT ANY WEAPON", "注入任意武器" },
        { "WEAPON TYPE", "武器类型" },
        { "SORT", "排序" },
        { "Tip: double-click a weapon to mount it", "提示：双击武器即可挂载" },
        { "CLEAR STATION", "清空挂架" },
        { "MOUNT WEAPON", "挂载武器" },
        { "CONFIGURE TEST", "配置测试" },
        { "Flight, targets and launch profile", "飞行、目标与发射配置" },
        { "MISSION SETUP", "任务设置" },
        { "MAP & SCENARIO", "地图与场景" },
        { "FLIGHT PROFILE", "飞行配置" },
        { "MAP PROFILE", "地图配置" },
        { "MISSION OPTIONS", "任务选项" },
        { "GENERATE TEST MISSION", "生成测试任务" },
        { "AIR HOT LOAD", "空中热装载" },
        { "GROUND PROXY RELOAD", "地面代理再装填" },
        { "Universal Test Lab |   /  Mission Studio", "Universal Test Lab |   /  任务工坊" },
        { "Universal Test Lab | AIR & GROUND VEHICLE TEST WORKSPACE", "Universal Test Lab | 空中与地面载具测试工作区" },
        { "TARGETS — GROUND / AIR / NAVAL TARGETS", "目标 — 地面 / 空中 / 海上目标" },
        { "GARAGE — COLLECTION & PRESETS", "机库 — 收藏与预设" },
        { "EXPERIMENTAL — OVERRIDES & INJECTION", "实验 — 覆盖与注入" },
        { "●  READY", "● 就绪" },
    };
}
internal static class ModernPalette
    {
        public const string Window = "#29354D";
        public const string Surface = "#80505B74";
        public const string SurfaceSolid = "#3B4862";
        public const string Field = "#B81B2740";
        public const string Border = "#58759F";
        public const string Text = "#F3F6FF";
        public const string Muted = "#9EACCE";
        public const string Accent = "#6C63FF";
        public const string AccentDark = "#4A55CC";
        public const string Cyan = "#4BD5FF";
        public const string Good = "#48DEB3";
        public const string Danger = "#FF5B8B";

        public static Brush Brush(string value)
        {
            return (Brush)new BrushConverter().ConvertFromString(value);
        }
    }
    internal static class ModernNumericBox
    {
        public static TextBox Create()
        {
            return new TextBox { Height = 32, Padding = new Thickness(8, 3, 8, 3), VerticalContentAlignment = VerticalAlignment.Center, HorizontalContentAlignment = HorizontalAlignment.Right };
        }

        public static void Bind(Slider slider, TextBox box)
        {
            if (slider == null || box == null) return;
            bool syncing = false;
            slider.ValueChanged += delegate
            {
                if (syncing) return;
                syncing = true;
                string text = slider.Value.ToString("0.###", CultureInfo.InvariantCulture);
                if (!String.Equals(box.Text, text, StringComparison.Ordinal)) box.Text = text;
                syncing = false;
            };
            box.TextChanged += delegate
            {
                if (syncing) return;
                double value;
                if (Double.TryParse(box.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) || Double.TryParse(box.Text.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                {
                    value = Math.Max(slider.Minimum, Math.Min(slider.Maximum, value));
                    syncing = true;
                    if (slider.Value != value) slider.Value = value;
                    syncing = false;
                }
            };
            box.LostFocus += delegate
            {
                double value;
                if (!Double.TryParse(box.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !Double.TryParse(box.Text.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                    box.Text = slider.Value.ToString("0.###", CultureInfo.InvariantCulture);
            };
        }
    }
    internal static class ModernComboSizing
    {
        public static void Attach(DependencyObject root)
        {
            if (root == null) return;
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                ComboBox combo = child as ComboBox;
                if (combo != null)
                {
                    combo.DropDownOpened -= Fit;
                    combo.DropDownOpened += Fit;
                }
                Attach(child);
            }
        }

        private static void Fit(object sender, EventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo == null) return;
            combo.UpdateLayout();
            double contentHeight = 12;
            int measured = 0;
            for (int index = 0; index < combo.Items.Count; index++)
            {
                FrameworkElement item = combo.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
                if (item == null || item.ActualHeight <= 0) continue;
                contentHeight += item.ActualHeight;
                measured++;
            }
            if (measured < combo.Items.Count) contentHeight += (combo.Items.Count - measured) * (measured > 0 ? (contentHeight - 12) / measured : 31);
            combo.MaxDropDownHeight = Math.Min(320, Math.Max(34, Math.Ceiling(contentHeight)));
        }
    }
    internal static class DwmGlass
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Margins { public int Left; public int Right; public int Top; public int Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect { public int Left; public int Top; public int Right; public int Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MinMaxInfo
        {
            public NativePoint Reserved;
            public NativePoint MaxSize;
            public NativePoint MaxPosition;
            public NativePoint MinTrackSize;
            public NativePoint MaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MonitorInfo
        {
            public int Size;
            public NativeRect Monitor;
            public NativeRect Work;
            public int Flags;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

        public static void EnablePerMonitorDpi()
        {
            try { if (!SetProcessDpiAwarenessContext(new IntPtr(-4))) SetProcessDPIAware(); }
            catch { try { SetProcessDPIAware(); } catch { } }
        }

        public static void Apply(Window window)
        {
            try
            {
                IntPtr handle = new WindowInteropHelper(window).Handle;
                HwndSource source = HwndSource.FromHwnd(handle);
                if (source != null) source.AddHook(WindowProc);

                // Keep DWM's backdrop without extending the non-client glass over the
                // first client rows. Full-sheet glass masked roughly 22 px below the
                // custom caption on some DPI/scaling combinations.
                Margins margins = new Margins { Left = 0, Right = 0, Top = 0, Bottom = 0 };
                DwmExtendFrameIntoClientArea(handle, ref margins);
                // Windows' transient Acrylic policy recolours the entire HWND and changes
                // dramatically when a dialog deactivates its owner. Mica keeps the DWM
                // blur but leaves our neutral glass palette stable in both focus states.
                int backdrop = 2;
                DwmSetWindowAttribute(handle, 38, ref backdrop, 4);
                int corner = 2;
                DwmSetWindowAttribute(handle, 33, ref corner, 4);
                int dark = 1;
                DwmSetWindowAttribute(handle, 20, ref dark, 4);
            }
            catch { }
        }

        private static IntPtr WindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WmGetMinMaxInfo = 0x0024;
            const uint MonitorDefaultToNearest = 2;
            if (message != WmGetMinMaxInfo || lParam == IntPtr.Zero) return IntPtr.Zero;
            try
            {
                IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
                if (monitor == IntPtr.Zero) return IntPtr.Zero;
                MonitorInfo monitorInfo = new MonitorInfo { Size = Marshal.SizeOf(typeof(MonitorInfo)) };
                if (!GetMonitorInfo(monitor, ref monitorInfo)) return IntPtr.Zero;
                MinMaxInfo minMax = (MinMaxInfo)Marshal.PtrToStructure(lParam, typeof(MinMaxInfo));
                minMax.MaxPosition.X = monitorInfo.Work.Left - monitorInfo.Monitor.Left;
                minMax.MaxPosition.Y = monitorInfo.Work.Top - monitorInfo.Monitor.Top;
                minMax.MaxSize.X = monitorInfo.Work.Right - monitorInfo.Work.Left;
                minMax.MaxSize.Y = monitorInfo.Work.Bottom - monitorInfo.Work.Top;
                minMax.MaxTrackSize = minMax.MaxSize;
                Marshal.StructureToPtr(minMax, lParam, true);
                handled = true;
            }
            catch { }
            return IntPtr.Zero;
        }
    }
}
