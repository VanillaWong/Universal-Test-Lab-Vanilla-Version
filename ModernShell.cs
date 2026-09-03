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

    // One-click era presets for the Map & Scenario window, Ask3lad style.
    // GroundIds fill the seven range positions, AirIds fill the four flying
    // hostiles (Target_Air_01 / Target_Air_02 / Heli_Target / Heli_Target_02);
    // a null AirId disables that flying slot (count 0).
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

    // Keeps a Slider and a numeric TextBox in sync so users can type an exact
    // value instead of dragging. The slider remains the source of truth; the
    // box normalizes on focus loss and is clamped to the slider range.
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

    internal static class ModernXaml
    {
        public static object Parse(string xaml) { return XamlReader.Parse(xaml); }

        public const string Main = @"
<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
      xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
      x:Name=""Root"" Background=""#B329354D"">
  <Grid.Resources>
    <SolidColorBrush x:Key=""TextBrush"" Color=""#F3F6FF""/>
    <SolidColorBrush x:Key=""MutedBrush"" Color=""#9EACCE""/>
    <SolidColorBrush x:Key=""AccentBrush"" Color=""#6C63FF""/>
    <SolidColorBrush x:Key=""AccentDarkBrush"" Color=""#4A55CC""/>
    <SolidColorBrush x:Key=""CyanBrush"" Color=""#4BD5FF""/>
    <SolidColorBrush x:Key=""Good"" Color=""#48DEB3""/>
    <SolidColorBrush x:Key=""Danger"" Color=""#FF5B8B""/>
    <SolidColorBrush x:Key=""FieldBrush"" Color=""#B81B2740""/>
    <SolidColorBrush x:Key=""SurfaceBrush"" Color=""#80505B74""/>
    <SolidColorBrush x:Key=""BorderBrush"" Color=""#58759F""/>

    <Style TargetType=""TextBlock"">
      <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/>
      <Setter Property=""FontFamily"" Value=""Segoe UI""/>
    </Style>
    <Style x:Key=""Caption"" TargetType=""TextBlock"">
      <Setter Property=""Foreground"" Value=""{StaticResource MutedBrush}""/>
      <Setter Property=""FontSize"" Value=""11""/>
      <Setter Property=""FontWeight"" Value=""SemiBold""/>
    </Style>
    <Style x:Key=""GlassCard"" TargetType=""Border"">
      <Setter Property=""Background"" Value=""{StaticResource SurfaceBrush}""/>
      <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}""/>
      <Setter Property=""BorderThickness"" Value=""1""/>
      <Setter Property=""CornerRadius"" Value=""20""/>
      <Setter Property=""Padding"" Value=""16""/>
    </Style>
    <Style x:Key=""ButtonStyle"" TargetType=""Button"">
      <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/>
      <Setter Property=""Background"" Value=""#24365F""/>
      <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}""/>
      <Setter Property=""BorderThickness"" Value=""1""/>
      <Setter Property=""Padding"" Value=""14,8""/>
      <Setter Property=""FontWeight"" Value=""SemiBold""/>
      <Setter Property=""Cursor"" Value=""Hand""/>
      <Setter Property=""Template"">
        <Setter.Value>
          <ControlTemplate TargetType=""Button"">
            <Border x:Name=""bd"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"" CornerRadius=""10"">
              <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center"" Margin=""{TemplateBinding Padding}""/>
            </Border>
            <ControlTemplate.Triggers>
              <Trigger Property=""IsMouseOver"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#304A78""/></Trigger>
              <Trigger Property=""IsPressed"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#17294B""/></Trigger>
              <Trigger Property=""IsEnabled"" Value=""False""><Setter Property=""Opacity"" Value=""0.42""/></Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>
    <Style x:Key=""PrimaryButton"" TargetType=""Button"" BasedOn=""{StaticResource ButtonStyle}"">
      <Setter Property=""Background"" Value=""{StaticResource AccentDarkBrush}""/>
      <Setter Property=""BorderThickness"" Value=""0""/>
      <Setter Property=""FontSize"" Value=""13""/>
    </Style>
    <Style x:Key=""ChromeButton"" TargetType=""Button"" BasedOn=""{StaticResource ButtonStyle}"">
      <Setter Property=""Width"" Value=""46""/><Setter Property=""Height"" Value=""36""/>
      <Setter Property=""Padding"" Value=""0""/><Setter Property=""Background"" Value=""Transparent""/><Setter Property=""BorderThickness"" Value=""0""/>
      <Setter Property=""FontSize"" Value=""14""/>
    </Style>
    <Style TargetType=""TextBox"">
      <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/>
      <Setter Property=""Background"" Value=""{StaticResource FieldBrush}""/>
      <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}""/>
      <Setter Property=""BorderThickness"" Value=""1""/>
      <Setter Property=""Padding"" Value=""10,7""/>
      <Setter Property=""CaretBrush"" Value=""{StaticResource CyanBrush}""/>
      <Setter Property=""Template"">
        <Setter.Value>
          <ControlTemplate TargetType=""TextBox"">
            <Border x:Name=""bd"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"" CornerRadius=""8"">
              <ScrollViewer x:Name=""PART_ContentHost"" Margin=""{TemplateBinding Padding}""/>
            </Border>
            <ControlTemplate.Triggers><Trigger Property=""IsKeyboardFocused"" Value=""True""><Setter TargetName=""bd"" Property=""BorderBrush"" Value=""{StaticResource CyanBrush}""/></Trigger></ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>
    <Style x:Key=""ComboItemStyle"" TargetType=""ComboBoxItem"">
      <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/><Setter Property=""Background"" Value=""{StaticResource FieldBrush}""/>
      <Setter Property=""Padding"" Value=""10,8""/><Setter Property=""HorizontalContentAlignment"" Value=""Stretch""/>
      <Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""ComboBoxItem""><Border x:Name=""bd"" Background=""{TemplateBinding Background}"" Padding=""{TemplateBinding Padding}"" CornerRadius=""6""><ContentPresenter/></Border><ControlTemplate.Triggers><Trigger Property=""IsHighlighted"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#4A55CC""/></Trigger><Trigger Property=""IsSelected"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#2D4673""/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
    </Style>
    <Style TargetType=""ComboBox"">
      <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/><Setter Property=""Background"" Value=""{StaticResource FieldBrush}""/>
      <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}""/><Setter Property=""BorderThickness"" Value=""1""/>
      <Setter Property=""Padding"" Value=""10,7""/><Setter Property=""ItemContainerStyle"" Value=""{StaticResource ComboItemStyle}""/>
      <Setter Property=""MaxDropDownHeight"" Value=""360""/>
      <Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""ComboBox""><Grid><ToggleButton x:Name=""toggle"" Focusable=""False"" IsChecked=""{Binding IsDropDownOpen, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}"" Background=""Transparent"" Foreground=""{TemplateBinding Foreground}"" BorderThickness=""0"" HorizontalContentAlignment=""Stretch"" VerticalContentAlignment=""Stretch""><ToggleButton.Template><ControlTemplate TargetType=""ToggleButton""><ContentPresenter HorizontalAlignment=""Stretch"" VerticalAlignment=""Stretch""/></ControlTemplate></ToggleButton.Template><Border x:Name=""bd"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"" CornerRadius=""8""><Grid><ContentPresenter Margin=""10,7,34,7"" VerticalAlignment=""Center"" HorizontalAlignment=""Left"" Content=""{TemplateBinding SelectionBoxItem}"" ContentTemplate=""{TemplateBinding SelectionBoxItemTemplate}"" TextElement.Foreground=""{TemplateBinding Foreground}""/><Path Data=""M 0 0 L 5 5 L 10 0 Z"" Fill=""#9EACCE"" HorizontalAlignment=""Right"" VerticalAlignment=""Center"" Margin=""0,0,10,0""/></Grid></Border></ToggleButton><Popup x:Name=""PART_Popup"" IsOpen=""{TemplateBinding IsDropDownOpen}"" Placement=""Bottom"" AllowsTransparency=""True"" Focusable=""False"" PopupAnimation=""Fade""><Border Background=""#0B1632"" BorderBrush=""#4D6D9F"" BorderThickness=""1"" CornerRadius=""10"" Padding=""5"" MinWidth=""{Binding ActualWidth, ElementName=toggle}"" MaxHeight=""{TemplateBinding MaxDropDownHeight}""><ScrollViewer VerticalScrollBarVisibility=""Auto"" CanContentScroll=""True""><ItemsPresenter/></ScrollViewer></Border></Popup></Grid><ControlTemplate.Triggers><Trigger Property=""IsKeyboardFocusWithin"" Value=""True""><Setter TargetName=""bd"" Property=""BorderBrush"" Value=""{StaticResource CyanBrush}""/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
    </Style>
    <Style TargetType=""ListBox"">
      <Setter Property=""Background"" Value=""{StaticResource FieldBrush}""/><Setter Property=""BorderThickness"" Value=""0""/>
      <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/><Setter Property=""ScrollViewer.HorizontalScrollBarVisibility"" Value=""Disabled""/>
    </Style>
    <Style TargetType=""ListBoxItem"">
      <Setter Property=""Padding"" Value=""10,7""/><Setter Property=""HorizontalContentAlignment"" Value=""Stretch""/><Setter Property=""Background"" Value=""Transparent""/>
      <Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""ListBoxItem""><Border x:Name=""bd"" Background=""{TemplateBinding Background}"" CornerRadius=""8"" Padding=""{TemplateBinding Padding}"" Margin=""3,2""><ContentPresenter/></Border><ControlTemplate.Triggers><Trigger Property=""IsMouseOver"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#24365F""/></Trigger><Trigger Property=""IsSelected"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#4A55CC""/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
    </Style>
    <Style TargetType=""ListViewItem"">
      <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/><Setter Property=""Background"" Value=""Transparent""/><Setter Property=""Padding"" Value=""6,7""/><Setter Property=""HorizontalContentAlignment"" Value=""Stretch""/>
      <Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""ListViewItem""><Border x:Name=""bd"" Background=""{TemplateBinding Background}"" CornerRadius=""6"" Padding=""{TemplateBinding Padding}""><GridViewRowPresenter Content=""{TemplateBinding Content}"" Columns=""{Binding View.Columns, RelativeSource={RelativeSource AncestorType=ListView}}""/></Border><ControlTemplate.Triggers><Trigger Property=""IsMouseOver"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#24365F""/></Trigger><Trigger Property=""IsSelected"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#4A55CC""/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
    </Style>
    <Style TargetType=""GridViewColumnHeader""><Setter Property=""Background"" Value=""#1D315C""/><Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/><Setter Property=""BorderBrush"" Value=""#A8C7ECFF""/><Setter Property=""Padding"" Value=""8,7""/><Setter Property=""FontWeight"" Value=""SemiBold""/><Setter Property=""HorizontalContentAlignment"" Value=""Center""/><Setter Property=""Focusable"" Value=""False""/><Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""GridViewColumnHeader""><Border x:Name=""HeaderBorder"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""0,0,1,1"" Padding=""{TemplateBinding Padding}""><ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/></Border></ControlTemplate></Setter.Value></Setter></Style>
    <Style x:Key=""LastGridHeader"" TargetType=""GridViewColumnHeader"" BasedOn=""{StaticResource {x:Type GridViewColumnHeader}}""><Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""GridViewColumnHeader""><Border Background=""{TemplateBinding Background}"" BorderBrush=""#A8C7ECFF"" BorderThickness=""0,0,1,1"" CornerRadius=""0,11,11,0"" Padding=""{TemplateBinding Padding}""><ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/></Border></ControlTemplate></Setter.Value></Setter></Style>
    <Style TargetType=""ScrollBar"">
      <Setter Property=""Background"" Value=""Transparent""/><Setter Property=""Width"" Value=""8""/><Setter Property=""Height"" Value=""8""/>
      <Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""ScrollBar""><Grid Background=""Transparent""><Track x:Name=""PART_Track"" Orientation=""{TemplateBinding Orientation}"" Minimum=""{TemplateBinding Minimum}"" Maximum=""{TemplateBinding Maximum}"" Value=""{TemplateBinding Value}"" ViewportSize=""{TemplateBinding ViewportSize}"" IsDirectionReversed=""False""><Track.DecreaseRepeatButton><RepeatButton x:Name=""dec"" Command=""{x:Static ScrollBar.PageUpCommand}"" Opacity=""0""/></Track.DecreaseRepeatButton><Track.Thumb><Thumb><Thumb.Template><ControlTemplate TargetType=""Thumb""><Border Background=""#4D6D9F"" CornerRadius=""4"" Margin=""1""/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb><Track.IncreaseRepeatButton><RepeatButton x:Name=""inc"" Command=""{x:Static ScrollBar.PageDownCommand}"" Opacity=""0""/></Track.IncreaseRepeatButton></Track></Grid><ControlTemplate.Triggers><Trigger Property=""Orientation"" Value=""Horizontal""><Setter Property=""Width"" Value=""Auto""/><Setter Property=""Height"" Value=""8""/><Setter TargetName=""PART_Track"" Property=""IsDirectionReversed"" Value=""False""/><Setter TargetName=""dec"" Property=""Command"" Value=""{x:Static ScrollBar.PageLeftCommand}""/><Setter TargetName=""inc"" Property=""Command"" Value=""{x:Static ScrollBar.PageRightCommand}""/></Trigger><Trigger Property=""Orientation"" Value=""Vertical""><Setter Property=""Width"" Value=""8""/><Setter Property=""Height"" Value=""Auto""/><Setter TargetName=""PART_Track"" Property=""IsDirectionReversed"" Value=""True""/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
    </Style>
    <Style x:Key=""ToggleStyle"" TargetType=""ToggleButton"">
      <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/><Setter Property=""Background"" Value=""#24365F""/><Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}""/><Setter Property=""BorderThickness"" Value=""1""/><Setter Property=""Padding"" Value=""12,8""/><Setter Property=""FontWeight"" Value=""SemiBold""/><Setter Property=""Cursor"" Value=""Hand""/>
      <Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""ToggleButton""><Border x:Name=""bd"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"" CornerRadius=""10""><ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center"" Margin=""{TemplateBinding Padding}""/></Border><ControlTemplate.Triggers><Trigger Property=""IsMouseOver"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#304A78""/></Trigger><Trigger Property=""IsChecked"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""{StaticResource AccentDarkBrush}""/><Setter TargetName=""bd"" Property=""BorderBrush"" Value=""{StaticResource CyanBrush}""/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
    </Style>
    <Style x:Key=""StatusToggleStyle"" TargetType=""ToggleButton""><Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/><Setter Property=""Background"" Value=""#24365F""/><Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}""/><Setter Property=""BorderThickness"" Value=""1""/><Setter Property=""Padding"" Value=""12,8""/><Setter Property=""FontWeight"" Value=""SemiBold""/><Setter Property=""Cursor"" Value=""Hand""/><Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""ToggleButton""><Border x:Name=""bd"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"" CornerRadius=""10""><ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center"" Margin=""{TemplateBinding Padding}""/></Border><ControlTemplate.Triggers><Trigger Property=""IsMouseOver"" Value=""True""><Setter TargetName=""bd"" Property=""Opacity"" Value=""0.86""/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
  </Grid.Resources>

  <Grid.RowDefinitions><RowDefinition Height=""38""/><RowDefinition Height=""*""/></Grid.RowDefinitions>
  <Border x:Name=""TitleBar"" Grid.Row=""0"" Background=""#FF35415E"" BorderBrush=""#664BD5FF"" BorderThickness=""0,0,0,1"">
    <Grid><Grid.ColumnDefinitions><ColumnDefinition Width=""*""/><ColumnDefinition Width=""Auto""/></Grid.ColumnDefinitions>
      <StackPanel Orientation=""Horizontal"" VerticalAlignment=""Center"" Margin=""14,0""><Border Width=""20"" Height=""20"" CornerRadius=""6"" Background=""#4A55CC"" Margin=""0,0,9,0""><TextBlock Text=""U"" FontWeight=""Bold"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" FontSize=""11""/></Border><TextBlock Text=""Universal Test Lab"" FontWeight=""SemiBold"" VerticalAlignment=""Center""/><TextBlock Text=""  /  Mission Studio"" Foreground=""#9EACCE"" VerticalAlignment=""Center""/></StackPanel>
      <StackPanel Grid.Column=""1"" Orientation=""Horizontal""><Button x:Name=""MinimizeButton"" Style=""{StaticResource ChromeButton}"" Content=""—""/><Button x:Name=""MaximizeButton"" Style=""{StaticResource ChromeButton}"" Content=""□""/><Button x:Name=""CloseButton"" Style=""{StaticResource ChromeButton}"" Content=""×""/></StackPanel>
    </Grid>
  </Border>

  <Grid Grid.Row=""1"" Margin=""0""><Grid.RowDefinitions><RowDefinition Height=""64""/><RowDefinition Height=""Auto""/><RowDefinition Height=""*""/><RowDefinition Height=""28""/></Grid.RowDefinitions>
    <Border Style=""{StaticResource GlassCard}"" Padding=""22,4"" Margin=""0"" CornerRadius=""0"" BorderThickness=""0,0,0,1""><Grid><Grid.ColumnDefinitions><ColumnDefinition Width=""270""/><ColumnDefinition Width=""*""/><ColumnDefinition Width=""Auto""/></Grid.ColumnDefinitions>
      <StackPanel VerticalAlignment=""Center""><TextBlock Text=""Universal Test Lab"" FontSize=""20"" FontWeight=""SemiBold""/><TextBlock Text=""AIR &amp; GROUND VEHICLE TEST WORKSPACE"" Foreground=""{StaticResource CyanBrush}"" FontSize=""10"" FontWeight=""SemiBold""/></StackPanel>
      <StackPanel Grid.Column=""1"" Margin=""10,0,12,0"" VerticalAlignment=""Center""><TextBlock Text=""GAME DIRECTORY"" Style=""{StaticResource Caption}"" Margin=""2,0,0,3""/><TextBox x:Name=""GameFolderBox"" Height=""30"" Padding=""10,3"" Margin=""0"" VerticalContentAlignment=""Center""/></StackPanel>
      <StackPanel Grid.Column=""2"" Orientation=""Horizontal"" VerticalAlignment=""Center""><Button x:Name=""BrowseButton"" Style=""{StaticResource ButtonStyle}"" Content=""BROWSE"" Margin=""4,0""/><Button x:Name=""SyncButton"" Style=""{StaticResource ButtonStyle}"" Content=""SYNC BASE"" Margin=""4,0""/><Button x:Name=""MissionsButton"" Style=""{StaticResource ButtonStyle}"" Content=""MISSIONS"" Margin=""4,0""/><Button x:Name=""PresetsButton"" Style=""{StaticResource ButtonStyle}"" Content=""PRESETS"" Margin=""4,0""/><Button x:Name=""AboutButton"" Style=""{StaticResource ButtonStyle}"" Content=""SUPPORT"" Margin=""4,0,0,0""/></StackPanel>
    </Grid></Border>

    <Border Grid.Row=""1"" Margin=""0,8,0,0"" Background=""Transparent""><StackPanel Orientation=""Horizontal""><ToggleButton x:Name=""TabVehicleButton"" Style=""{StaticResource ToggleStyle}"" Content=""VEHICLE"" IsChecked=""True"" Margin=""0,0,6,0""/><ToggleButton x:Name=""TabTargetsButton"" Style=""{StaticResource ToggleStyle}"" Content=""TARGETS"" Margin=""0,0,6,0""/><ToggleButton x:Name=""TabOptionsButton"" Style=""{StaticResource ToggleStyle}"" Content=""OPTIONS"" Margin=""0,0,6,0""/><ToggleButton x:Name=""TabGarageButton"" Style=""{StaticResource ToggleStyle}"" Content=""GARAGE"" Margin=""0,0,6,0""/><ToggleButton x:Name=""TabExperimentalButton"" Style=""{StaticResource ToggleStyle}"" Content=""EXPERIMENTAL""/></StackPanel></Border>

    <Grid x:Name=""TabVehicleContent"" Grid.Row=""2"" Margin=""12,10,12,10""><Grid.ColumnDefinitions><ColumnDefinition Width=""330""/><ColumnDefinition Width=""12""/><ColumnDefinition Width=""*"" MinWidth=""500""/><ColumnDefinition Width=""12""/><ColumnDefinition Width=""330""/></Grid.ColumnDefinitions>
      <Border Grid.Column=""0"" Style=""{StaticResource GlassCard}""><Grid><Grid.RowDefinitions><RowDefinition Height=""58""/><RowDefinition Height=""Auto""/><RowDefinition Height=""Auto""/><RowDefinition Height=""Auto""/><RowDefinition Height=""*""/></Grid.RowDefinitions>
        <Grid><Grid.ColumnDefinitions><ColumnDefinition Width=""48""/><ColumnDefinition Width=""*""/></Grid.ColumnDefinitions><Border Width=""44"" Height=""44"" CornerRadius=""13"" Background=""{StaticResource AccentDarkBrush}""><TextBlock Text=""01"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" FontWeight=""Bold""/></Border><StackPanel Grid.Column=""1"" Margin=""10,2,0,0""><TextBlock Text=""CHOOSE VEHICLE"" FontSize=""16"" FontWeight=""SemiBold""/><TextBlock Text=""Air and ground vehicles"" Foreground=""{StaticResource MutedBrush}"" FontSize=""11""/></StackPanel></Grid>
        <StackPanel Grid.Row=""1"" Margin=""0,8,0,10""><TextBlock Text=""SEARCH"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><TextBox x:Name=""AircraftSearch""/></StackPanel>
        <Grid Grid.Row=""2"" Margin=""0,0,0,10""><Grid.ColumnDefinitions><ColumnDefinition Width=""1.25*""/><ColumnDefinition Width=""1.2*""/><ColumnDefinition Width=""1*""/></Grid.ColumnDefinitions><StackPanel Margin=""0,0,5,0""><TextBlock Text=""NATION"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><ComboBox x:Name=""NationFilter""/></StackPanel><StackPanel Grid.Column=""1"" Margin=""5,0""><TextBlock Text=""RANK"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><ComboBox x:Name=""RankFilter""/></StackPanel><StackPanel Grid.Column=""2"" Margin=""5,0,0,0""><TextBlock Text=""TYPE"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><ComboBox x:Name=""TypeFilter""/></StackPanel></Grid>
        <StackPanel Grid.Row=""3"" Margin=""2,0,0,8""><TextBlock Text=""AVAILABLE VEHICLES"" Style=""{StaticResource Caption}""/><TextBlock x:Name=""VehicleCountText"" Foreground=""{StaticResource CyanBrush}"" FontSize=""11"" Margin=""0,4,0,0""/></StackPanel>
        <Border Grid.Row=""4"" Background=""{StaticResource FieldBrush}"" CornerRadius=""12"" Padding=""3""><ListBox x:Name=""AircraftList""><ListBox.ItemTemplate><DataTemplate><StackPanel><TextBlock Text=""{Binding Name}"" FontWeight=""SemiBold"" TextTrimming=""CharacterEllipsis""/><TextBlock Text=""{Binding Meta}"" Foreground=""#AEB9D8"" FontSize=""10"" Margin=""0,2,0,0"" TextTrimming=""CharacterEllipsis""/></StackPanel></DataTemplate></ListBox.ItemTemplate></ListBox></Border>
      </Grid></Border>

      <Border Grid.Column=""2"" Style=""{StaticResource GlassCard}""><Grid><Grid.RowDefinitions><RowDefinition Height=""58""/><RowDefinition Height=""32""/><RowDefinition Height=""Auto""/><RowDefinition Height=""Auto""/><RowDefinition Height=""*""/><RowDefinition Height=""Auto""/></Grid.RowDefinitions>
        <Grid><Grid.ColumnDefinitions><ColumnDefinition Width=""48""/><ColumnDefinition Width=""*""/><ColumnDefinition Width=""Auto""/></Grid.ColumnDefinitions><Border Width=""44"" Height=""44"" CornerRadius=""13"" Background=""{StaticResource AccentDarkBrush}""><TextBlock Text=""02"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" FontWeight=""Bold""/></Border><StackPanel Grid.Column=""1"" Margin=""10,2,0,0""><TextBlock x:Name=""BuildTitle"" Text=""BUILD LOADOUT"" FontSize=""16"" FontWeight=""SemiBold""/><TextBlock x:Name=""BuildSubtitle"" Text=""Select a station, then mount a weapon"" Foreground=""{StaticResource MutedBrush}"" FontSize=""11""/></StackPanel><TextBlock x:Name=""MassText"" Grid.Column=""2"" Foreground=""{StaticResource CyanBrush}"" FontWeight=""SemiBold"" VerticalAlignment=""Center""/></Grid>
        <TextBlock x:Name=""StationText"" Grid.Row=""1"" Foreground=""{StaticResource MutedBrush}"" VerticalAlignment=""Center"" TextTrimming=""CharacterEllipsis""/>
        <Border x:Name=""PylonCard"" Grid.Row=""2"" Background=""{StaticResource FieldBrush}"" CornerRadius=""12"" Padding=""5"" Margin=""0,2,0,8""><UniformGrid x:Name=""PylonPanel"" Rows=""1"" VerticalAlignment=""Center""/></Border>
        <Grid x:Name=""WeaponFilterPanel"" Grid.Row=""3""><Grid.ColumnDefinitions><ColumnDefinition Width=""175""/><ColumnDefinition Width=""*""/><ColumnDefinition Width=""155""/><ColumnDefinition Width=""125""/><ColumnDefinition Width=""145""/></Grid.ColumnDefinitions><StackPanel Margin=""0,0,5,0""><TextBlock Text=""WEAPON SOURCE"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><ToggleButton x:Name=""InjectionToggle"" Style=""{StaticResource ToggleStyle}"" Content=""INJECT ANY WEAPON""/></StackPanel><StackPanel Grid.Column=""1"" Margin=""5,0""><TextBlock Text=""SEARCH"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><TextBox x:Name=""WeaponSearch""/></StackPanel><StackPanel Grid.Column=""2"" Margin=""5,0""><TextBlock Text=""WEAPON TYPE"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><ComboBox x:Name=""CategoryFilter""/></StackPanel><StackPanel Grid.Column=""3"" Margin=""5,0""><TextBlock Text=""NATION"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><ComboBox x:Name=""WeaponNationFilter""/></StackPanel><StackPanel Grid.Column=""4"" Margin=""5,0,0,0""><TextBlock Text=""SORT"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><ComboBox x:Name=""SortFilter""/></StackPanel></Grid>
        <Grid x:Name=""WeaponTableFrame"" Grid.Row=""4"" Margin=""0,10,0,10""><Border Background=""{StaticResource FieldBrush}"" CornerRadius=""12""/><Grid x:Name=""WeaponTableClipContent""><ListView x:Name=""WeaponList"" Background=""Transparent"" BorderThickness=""0"" Foreground=""{StaticResource TextBrush}"" ScrollViewer.HorizontalScrollBarVisibility=""Disabled"" ScrollViewer.CanContentScroll=""True"" VirtualizingStackPanel.IsVirtualizing=""True"" VirtualizingStackPanel.VirtualizationMode=""Recycling""><ListView.Resources><Style TargetType=""ScrollBar"" BasedOn=""{StaticResource {x:Type ScrollBar}}""><Style.Triggers><Trigger Property=""Orientation"" Value=""Vertical""><Setter Property=""Margin"" Value=""0,32,0,1""/></Trigger></Style.Triggers></Style></ListView.Resources><ListView.GroupStyle><GroupStyle><GroupStyle.HeaderTemplate><DataTemplate><Border Background=""#D9152340"" BorderBrush=""#49698F"" BorderThickness=""0,1,0,1"" Padding=""10,6"" Margin=""0,4,0,2""><TextBlock Foreground=""{StaticResource CyanBrush}"" FontWeight=""SemiBold""><Run Text=""—  ""/><Run Text=""{Binding Name, Mode=OneWay}""/><Run Text=""  —""/></TextBlock></Border></DataTemplate></GroupStyle.HeaderTemplate></GroupStyle></ListView.GroupStyle><ListView.View><GridView><GridViewColumn Header=""Weapon"" Width=""330"" DisplayMemberBinding=""{Binding Name}""/><GridViewColumn Header=""Type"" Width=""185"" DisplayMemberBinding=""{Binding Category}""/><GridViewColumn Header=""Ammo"" Width=""70"" DisplayMemberBinding=""{Binding Ammo}""/><GridViewColumn Header=""Mass"" Width=""85"" DisplayMemberBinding=""{Binding Mass}""/><GridViewColumn Width=""82""><GridViewColumn.Header><GridViewColumnHeader Content=""Mode""/></GridViewColumn.Header><GridViewColumn.CellTemplate><DataTemplate><TextBlock Text=""{Binding Mode}"" HorizontalAlignment=""Center"" TextAlignment=""Center""/></DataTemplate></GridViewColumn.CellTemplate></GridViewColumn></GridView></ListView.View></ListView></Grid><Border BorderBrush=""#A8C7ECFF"" BorderThickness=""1"" CornerRadius=""12"" IsHitTestVisible=""False""/></Grid>
        <Grid Grid.Row=""5""><Grid.ColumnDefinitions><ColumnDefinition Width=""*""/><ColumnDefinition Width=""145""/><ColumnDefinition Width=""128""/><ColumnDefinition Width=""94""/><ColumnDefinition Width=""145""/></Grid.ColumnDefinitions><TextBlock Text=""Tip: double-click a weapon to mount it"" Foreground=""{StaticResource MutedBrush}"" VerticalAlignment=""Center""/><Button x:Name=""SystemsButton"" Grid.Column=""1"" Style=""{StaticResource ButtonStyle}"" Content=""模块"" Margin=""4,0""/><Button x:Name=""ClearStationButton"" Grid.Column=""2"" Style=""{StaticResource ButtonStyle}"" Content=""CLEAR STATION"" Margin=""4,0""/><Button x:Name=""ClearAllButton"" Grid.Column=""3"" Style=""{StaticResource ButtonStyle}"" Content=""全部清空"" Margin=""4,0""/><Button x:Name=""MountButton"" Grid.Column=""4"" Style=""{StaticResource PrimaryButton}"" Content=""MOUNT WEAPON"" Margin=""4,0,0,0""/></Grid>
      </Grid></Border>

      <Border Grid.Column=""4"" Style=""{StaticResource GlassCard}""><Grid><Grid.RowDefinitions><RowDefinition Height=""58""/><RowDefinition Height=""150""/><RowDefinition Height=""34""/><RowDefinition Height=""48""/><RowDefinition Height=""48""/><RowDefinition Height=""*""/><RowDefinition Height=""56""/><RowDefinition Height=""26""/></Grid.RowDefinitions>
        <Grid><Grid.ColumnDefinitions><ColumnDefinition Width=""48""/><ColumnDefinition Width=""*""/></Grid.ColumnDefinitions><Border Width=""44"" Height=""44"" CornerRadius=""13"" Background=""{StaticResource AccentDarkBrush}""><TextBlock Text=""03"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" FontWeight=""Bold""/></Border><StackPanel Grid.Column=""1"" Margin=""10,2,0,0""><TextBlock Text=""CONFIGURE TEST"" FontSize=""16"" FontWeight=""SemiBold""/><TextBlock Text=""Flight, targets and launch profile"" Foreground=""{StaticResource MutedBrush}"" FontSize=""11""/></StackPanel></Grid>
        <Border x:Name=""PreviewCard"" Grid.Row=""1"" CornerRadius=""15"" BorderBrush=""#78A7DFFF"" BorderThickness=""1"" Background=""#7A1D315C""><Grid x:Name=""PreviewClipContent""><Ellipse Width=""155"" Height=""105"" Fill=""#284BD5FF"" VerticalAlignment=""Top"" Margin=""0,12,0,0""/><Grid x:Name=""PreviewAircraftVisual""><Image x:Name=""PreviewAircraftImage"" Width=""220"" Height=""112"" Stretch=""Uniform"" Opacity=""0.92"" VerticalAlignment=""Top"" Margin=""0,4,0,0""/></Grid><Grid x:Name=""PreviewHelicopterVisual"" Visibility=""Collapsed""><Image x:Name=""PreviewHelicopterImage"" Width=""270"" Height=""108"" Stretch=""Uniform"" Opacity=""0.94"" VerticalAlignment=""Top"" Margin=""0,5,0,0""/></Grid><Grid x:Name=""PreviewDroneVisual"" Visibility=""Collapsed""><Image x:Name=""PreviewDroneImage"" Width=""270"" Height=""108"" Stretch=""Uniform"" Opacity=""0.94"" VerticalAlignment=""Top"" Margin=""0,5,0,0""/></Grid><Border VerticalAlignment=""Bottom"" Background=""#900A142E"" Padding=""12,10""><StackPanel><TextBlock x:Name=""PreviewName"" FontSize=""15"" FontWeight=""SemiBold"" TextTrimming=""CharacterEllipsis""/><TextBlock x:Name=""PreviewMeta"" Foreground=""{StaticResource MutedBrush}"" FontSize=""10"" Margin=""0,3,0,0"" TextTrimming=""CharacterEllipsis""/></StackPanel></Border></Grid></Border>
        <TextBlock Grid.Row=""2"" Text=""MISSION SETUP"" FontSize=""14"" FontWeight=""SemiBold"" VerticalAlignment=""Bottom""/>
        <Button x:Name=""FlightConfigureButton"" Grid.Row=""3"" Style=""{StaticResource ButtonStyle}"" Content=""飞行配置"" Margin=""0,7,0,0""/>
        <Button x:Name=""MapButton"" Grid.Row=""4"" Style=""{StaticResource ButtonStyle}"" Content=""MAP &amp; SCENARIO"" Margin=""0,7,0,0""/>
        <StackPanel Grid.Row=""5"" Margin=""2,16,2,8""><TextBlock Text=""FLIGHT PROFILE"" Style=""{StaticResource Caption}""/><TextBlock x:Name=""FlightProfileText"" Foreground=""{StaticResource MutedBrush}"" FontSize=""11"" TextWrapping=""Wrap"" Margin=""0,3,0,0""/><TextBlock Text=""MAP PROFILE"" Style=""{StaticResource Caption}"" Margin=""0,14,0,0""/><TextBlock x:Name=""TargetSummaryText"" Foreground=""{StaticResource MutedBrush}"" FontSize=""11"" TextWrapping=""Wrap"" Margin=""0,3,0,0""/><TextBlock Text=""Aircraft/helicopters: reopen User Missions. Ground vehicle changes: restart War Thunder once."" Foreground=""{StaticResource Good}"" FontSize=""11"" TextWrapping=""Wrap"" Margin=""0,14,0,0""/></StackPanel>
        <Grid Visibility=""Collapsed""><ComboBox x:Name=""AirTargetBox""/><ComboBox x:Name=""AirCountBox""/><ComboBox x:Name=""GroundTargetBox""/><ComboBox x:Name=""GroundCountBox""/><ToggleButton x:Name=""HostileToggle""/><ToggleButton x:Name=""SamSitesToggle""/><TextBlock x:Name=""SamSitesMode""/><TextBlock x:Name=""SamSitesSelection""/><ComboBox x:Name=""ShipTargetBox""/><ComboBox x:Name=""ShipCountBox""/></Grid>
        <Grid Grid.Row=""6"" Margin=""0,7,0,0""><Grid.ColumnDefinitions><ColumnDefinition Width=""132""/><ColumnDefinition Width=""*""/></Grid.ColumnDefinitions><Button x:Name=""MissionOptionsButton"" Style=""{StaticResource ButtonStyle}"" Content=""MISSION OPTIONS""/><Button x:Name=""GenerateButton"" Grid.Column=""1"" Margin=""6,0,0,0"" Style=""{StaticResource PrimaryButton}"" Content=""GENERATE TEST MISSION""/></Grid>
        <TextBlock Grid.Row=""7"" Text=""AIR HOT LOAD  •  GROUND PROXY RELOAD"" Foreground=""{StaticResource CyanBrush}"" FontSize=""10"" HorizontalAlignment=""Center"" VerticalAlignment=""Bottom""/>
      </Grid></Border>
    </Grid>
    <Grid x:Name=""TabTargetsContent"" Grid.Row=""2"" Visibility=""Collapsed"" IsHitTestVisible=""False""><StackPanel VerticalAlignment=""Center"" HorizontalAlignment=""Center""><TextBlock Text=""TARGETS — GROUND / AIR / NAVAL TARGETS"" FontSize=""18"" FontWeight=""SemiBold""/><TextBlock Text=""Stage1: migrating Map &amp; Scenario here"" Foreground=""{StaticResource MutedBrush}"" Margin=""0,8,0,0""/></StackPanel></Grid>
    <Grid x:Name=""TabOptionsContent"" Grid.Row=""2"" Visibility=""Collapsed"" IsHitTestVisible=""False""><StackPanel VerticalAlignment=""Center"" HorizontalAlignment=""Center""><TextBlock Text=""选项 — 任务设置"" FontSize=""18"" FontWeight=""SemiBold""/><TextBlock Text=""Stage1: migrating Mission Options here"" Foreground=""{StaticResource MutedBrush}"" Margin=""0,8,0,0""/></StackPanel></Grid>
    <Grid x:Name=""TabGarageContent"" Grid.Row=""2"" Visibility=""Collapsed"" IsHitTestVisible=""False""><StackPanel VerticalAlignment=""Center"" HorizontalAlignment=""Center""><TextBlock Text=""GARAGE — COLLECTION &amp; PRESETS"" FontSize=""18"" FontWeight=""SemiBold""/><TextBlock Text=""Stage2: recently used / favourites / presets"" Foreground=""{StaticResource MutedBrush}"" Margin=""0,8,0,0""/></StackPanel></Grid>
    <Grid x:Name=""TabExperimentalContent"" Grid.Row=""2"" Visibility=""Collapsed"" IsHitTestVisible=""False""><StackPanel VerticalAlignment=""Center"" HorizontalAlignment=""Center""><TextBlock Text=""EXPERIMENTAL — OVERRIDES &amp; INJECTION"" FontSize=""18"" FontWeight=""SemiBold""/><TextBlock Text=""Stage1: migrating Ground/Flight Configure here"" Foreground=""{StaticResource MutedBrush}"" Margin=""0,8,0,0""/></StackPanel></Grid>
    <Border Grid.Row=""3"" Background=""#D01A263D"" CornerRadius=""0"" Margin=""0"" Padding=""14,0"" BorderBrush=""#664BD5FF"" BorderThickness=""0,1,0,0""><TextBlock x:Name=""StatusText"" Text=""●  READY"" Foreground=""{StaticResource Good}"" VerticalAlignment=""Center""/></Border>
  </Grid>
</Grid>";
    }

    internal sealed class ModernMainWindow : Window
    {
        private readonly MainForm controller;
        private readonly Grid root;
        private readonly Grid windowHost;
        private readonly Grid overlayLayer;
        private readonly Border overlayBackdrop;
        private readonly Stack<ModernDialogWindow> overlayDialogs = new Stack<ModernDialogWindow>();
        private Border titleBar;
        private TextBox gameFolder;
        private TextBox aircraftSearch;
        private ComboBox nationFilter;
        private ComboBox rankFilter;
        private ComboBox typeFilter;
        private ListBox aircraftList;
        private TextBlock vehicleCount;
        private Border previewCard;
        private Grid previewClipContent;
        private TextBlock previewName;
        private TextBlock previewMeta;
        private Grid previewAircraftVisual;
        private Grid previewHelicopterVisual;
        private Grid previewDroneVisual;
        private Grid previewGroundVisual;
        private Image previewAircraftImage;
        private Image previewHelicopterImage;
        private Image previewDroneImage;
        private Image previewGroundImage;
        private TextBlock buildTitle;
        private TextBlock buildSubtitle;
        private Border pylonCard;
        private Grid weaponFilterPanel;
        private Grid groundWorkspacePanel;
        private ComboBox groundCannonBox;
        private List<ComboBox> groundSlotBoxes = new List<ComboBox>();
        private List<TextBox> groundSlotCounts = new List<TextBox>();
        private StackPanel groundGroupsPanel;
        private List<GroundAmmoSlotGroup> groundSlotGroups = new List<GroundAmmoSlotGroup>();
        private Dictionary<string, int> groundNativeTotals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, int> groundNativeByCalibre = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private TextBlock groundAmmoPoolText;
        private bool groundHasMainWeapon;

        private sealed class GroundAmmoEntry
        {
            public GroundAmmo Ammo;
            public int Native;
            public string Text;
            public override string ToString() { return Text; }
        }

        private sealed class GroundAmmoSlotGroup
        {
            public string WeaponBlk;
            public string Display;
            public int Calibre;
            public bool IsBelt;
            public int SlotCount;
            public int MaxTotal;
            public int FirstSlot;
            public List<GroundAmmoEntry> Options = new List<GroundAmmoEntry>();
            public TextBlock TotalText;
        }
        private ComboBox ammoPresetBox;
        private List<AmmoPreset> ammoPresets = new List<AmmoPreset>();
        private ToggleButton tabVehicleButton, tabTargetsButton, tabOptionsButton, tabGarageButton, tabExperimentalButton;
        private Grid tabVehicleContent, tabTargetsContent, tabOptionsContent, tabGarageContent, tabExperimentalContent;
        private bool suppressGarageTrack;
        private ListBox garageRecentlyBox;
        private ListBox garageFavBox;
        private ListBox garagePresetBox;
        private MapPanel targetsPanel;
        private bool experimentalBuilt;
        private object experimentalPanel;
        private bool groundUpdating;
        private string groundCannonBlk;
        private bool groundCannonNative = true;
        private Button systemsButton;
        private Button flightConfigureButton;
        private Button clearStationButton;
        private Button clearAllButton;
        private Button mountButton;
        private TextBlock stationText;
        private TextBlock massText;
        private UniformGrid pylonPanel;
        private ToggleButton injectionToggle;
        private System.Windows.Threading.DispatcherTimer weaponSearchTimer;
        private bool weaponColumnsPending;
        private TextBox weaponSearch;
        private ComboBox categoryFilter;
        private ComboBox weaponNationFilter;
        private ComboBox sortFilter;
        private Grid weaponTableFrame;
        private Grid weaponTableClipContent;
        private ListView weaponList;
        private ComboBox airTarget;
        private ComboBox groundTarget;
        private ComboBox shipTarget;
        private ComboBox airCount;
        private ComboBox groundCount;
        private ComboBox shipCount;
        private ToggleButton hostileToggle;
        private ToggleButton samSitesToggle;
        private TextBlock samSitesMode;
        private TextBlock samSitesSelection;
        private AircraftView airTarget01;
        private AircraftView heliTarget01;
        private AircraftView heliTarget02;
        private int airTarget01Count = 1;
        private int heliTarget01Count = 3;
        private int heliTarget02Count = 2;
        private TextBlock flightProfileText;
        private TextBlock targetSummaryText;
        private TextBlock status;
        private Aircraft selectedAircraft;
        private PylonSlot selectedPylon;
        private List<AircraftView> aircraftViews;
        private readonly List<TargetView> configuredGroundTargets = new List<TargetView>();
        private bool passiveShip;
        private CombinedScenarioSettings combinedScenario = new CombinedScenarioSettings();
        private bool updatingWeaponColumns;

        public ModernMainWindow()
        {
            controller = new MainForm();
            Title = ModernText.L("Universal Test Lab — Mission Studio", "Universal Test Lab — 任务工坊");
            ModernText.Chinese = ConfigStore.GetString("language") != "en";
        Width = 1500;
            Height = 920;
            MinWidth = 1200;
            MinHeight = 640;
            WindowStartupLocation = WindowStartupLocation.Manual;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;
            Background = Brushes.Transparent;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 38,
                ResizeBorderThickness = new Thickness(7),
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(0),
                UseAeroCaptionButtons = false
            });

            root = (Grid)ModernXaml.Parse(ModernXaml.Main);
            windowHost = new Grid { ClipToBounds = true };
            windowHost.Children.Add(root);
            overlayLayer = new Grid
            {
                Visibility = Visibility.Collapsed,
                Background = Brushes.Transparent,
                ClipToBounds = true
            };
            overlayBackdrop = new Border
            {
                Background = ModernPalette.Brush("#A60A142B"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            overlayLayer.Children.Add(overlayBackdrop);
            windowHost.Children.Add(overlayLayer);
            Content = windowHost;
            BindControls();
            ApplyChromeAccent();
            LoadPreviewImages();
        ApplyXamlLocalization();
            WireEvents();
            PopulateControls();
            controller.WorkspaceConfirmation = ConfirmWorkspaceAction;
            FitToWorkingArea();
            Loaded += delegate { ModernComboSizing.Attach(this); };
            SourceInitialized += delegate { DwmGlass.Apply(this); };
            Closed += delegate { SessionSave(); controller.Dispose(); };
        }

        
    private void ApplyXamlLocalization()
    {
        ApplyXamlLocalizationNode(this);
    }

    private static void ApplyXamlLocalizationNode(DependencyObject node)
    {
        if (node == null) return;
        TextBlock tb = node as TextBlock;
        if (tb != null && tb.Text != null)
        {
            string zh;
            if (ModernText.Chinese && ModernText.XamlMap.TryGetValue(tb.Text, out zh)) tb.Text = zh;
        }
        ContentControl cc = node as ContentControl;
        if (cc != null && cc.Content is string)
        {
            string zh;
            if (ModernText.Chinese && ModernText.XamlMap.TryGetValue((string)cc.Content, out zh)) cc.Content = zh;
        }
        ContentControl tt = node as ContentControl;
        if (tt != null && tt.ToolTip is string)
        {
            string zh;
            if (ModernText.Chinese && ModernText.XamlMap.TryGetValue((string)tt.ToolTip, out zh)) tt.ToolTip = zh;
        }
        int count = VisualTreeHelper.GetChildrenCount(node);
        for (int i = 0; i < count; i++) ApplyXamlLocalizationNode(VisualTreeHelper.GetChild(node, i));
    }

    internal void ShowOverlay(ModernDialogWindow dialog)
        {
            if (dialog == null) return;
            ModernDialogWindow previous = overlayDialogs.Count > 0 ? overlayDialogs.Peek() : null;
            if (previous != null)
            {
                previous.IsHitTestVisible = false;
                previous.Effect = new BlurEffect { Radius = 6, KernelType = KernelType.Gaussian, RenderingBias = RenderingBias.Quality };
                previous.Opacity = 0.52;
            }
            else
            {
                root.IsHitTestVisible = false;
                root.Effect = new BlurEffect { Radius = 12, KernelType = KernelType.Gaussian, RenderingBias = RenderingBias.Quality };
                overlayLayer.Visibility = Visibility.Visible;
            }

            dialog.AttachOverlay(this);
            dialog.HorizontalAlignment = HorizontalAlignment.Center;
            dialog.VerticalAlignment = VerticalAlignment.Center;
            dialog.Margin = new Thickness(24);
            dialog.MaxWidth = Math.Max(480, ActualWidth - 48);
            dialog.MaxHeight = Math.Max(420, ActualHeight - 48);
            dialog.MinWidth = 0;
            dialog.MinHeight = 0;
            overlayDialogs.Push(dialog);
            overlayLayer.Children.Add(dialog);
            dialog.Focus();
            Keyboard.Focus(dialog);
        }

        internal void CloseOverlay(ModernDialogWindow dialog)
        {
            if (dialog == null || !overlayDialogs.Contains(dialog)) return;
            if (!ReferenceEquals(overlayDialogs.Peek(), dialog))
            {
                overlayLayer.Children.Remove(dialog);
                return;
            }

            overlayDialogs.Pop();
            overlayLayer.Children.Remove(dialog);
            dialog.DetachOverlay();
            if (overlayDialogs.Count > 0)
            {
                ModernDialogWindow previous = overlayDialogs.Peek();
                previous.IsHitTestVisible = true;
                previous.Effect = null;
                previous.Opacity = 1;
                previous.Focus();
            }
            else
            {
                overlayLayer.Visibility = Visibility.Collapsed;
                root.Effect = null;
                root.IsHitTestVisible = true;
            }
        }

        private T Find<T>(string name) where T : DependencyObject { return (T)root.FindName(name); }

        private void BindControls()
        {
            titleBar = Find<Border>("TitleBar");
            gameFolder = Find<TextBox>("GameFolderBox");
            aircraftSearch = Find<TextBox>("AircraftSearch");
            nationFilter = Find<ComboBox>("NationFilter");
            rankFilter = Find<ComboBox>("RankFilter");
            typeFilter = Find<ComboBox>("TypeFilter");
            aircraftList = Find<ListBox>("AircraftList");
            vehicleCount = Find<TextBlock>("VehicleCountText");
            previewCard = Find<Border>("PreviewCard");
            previewClipContent = Find<Grid>("PreviewClipContent");
            previewName = Find<TextBlock>("PreviewName");
            previewMeta = Find<TextBlock>("PreviewMeta");
            previewAircraftVisual = Find<Grid>("PreviewAircraftVisual");
            previewHelicopterVisual = Find<Grid>("PreviewHelicopterVisual");
            previewDroneVisual = Find<Grid>("PreviewDroneVisual");
            previewAircraftImage = Find<Image>("PreviewAircraftImage");
            previewHelicopterImage = Find<Image>("PreviewHelicopterImage");
            previewDroneImage = Find<Image>("PreviewDroneImage");
            buildTitle = Find<TextBlock>("BuildTitle");
            buildSubtitle = Find<TextBlock>("BuildSubtitle");
            stationText = Find<TextBlock>("StationText");
            massText = Find<TextBlock>("MassText");
            pylonPanel = Find<UniformGrid>("PylonPanel");
            pylonCard = Find<Border>("PylonCard");
            weaponFilterPanel = Find<Grid>("WeaponFilterPanel");
            injectionToggle = Find<ToggleButton>("InjectionToggle");
            weaponSearchTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            weaponSearchTimer.Tick += delegate { weaponSearchTimer.Stop(); RefreshWeapons(); };
            weaponSearch = Find<TextBox>("WeaponSearch");
            categoryFilter = Find<ComboBox>("CategoryFilter");
            weaponNationFilter = Find<ComboBox>("WeaponNationFilter");
            sortFilter = Find<ComboBox>("SortFilter");
            weaponTableFrame = Find<Grid>("WeaponTableFrame");
            weaponTableClipContent = Find<Grid>("WeaponTableClipContent");
            weaponList = Find<ListView>("WeaponList");
            airTarget = Find<ComboBox>("AirTargetBox");
            groundTarget = Find<ComboBox>("GroundTargetBox");
            shipTarget = Find<ComboBox>("ShipTargetBox");
            airCount = Find<ComboBox>("AirCountBox");
            groundCount = Find<ComboBox>("GroundCountBox");
            shipCount = Find<ComboBox>("ShipCountBox");
            hostileToggle = Find<ToggleButton>("HostileToggle");
            samSitesToggle = Find<ToggleButton>("SamSitesToggle");
            samSitesMode = Find<TextBlock>("SamSitesMode");
            samSitesSelection = Find<TextBlock>("SamSitesSelection");
            flightProfileText = Find<TextBlock>("FlightProfileText");
            targetSummaryText = Find<TextBlock>("TargetSummaryText");
            status = Find<TextBlock>("StatusText");
            systemsButton = Find<Button>("SystemsButton");
            flightConfigureButton = Find<Button>("FlightConfigureButton");
            clearStationButton = Find<Button>("ClearStationButton");
            clearAllButton = Find<Button>("ClearAllButton");
            mountButton = Find<Button>("MountButton");
        }

        private void ApplyChromeAccent()
        {
            Color accent = SystemParameters.WindowGlassColor;
            if (accent.A < 64) accent = Color.FromRgb(210, 122, 242);
            accent.A = 255;
            SolidColorBrush brush = new SolidColorBrush(accent);
            brush.Freeze();
            titleBar.Background = brush;
        }

        private void LoadPreviewImages()
        {
            BitmapImage yf23 = LoadEmbeddedImage("UTL.preview-yf23.png");
            TransformedBitmap horizontalYf23 = new TransformedBitmap(yf23, new RotateTransform(90));
            horizontalYf23.Freeze();
            BitmapImage apache = LoadEmbeddedImage("UTL.preview-ah64e.png");
            previewAircraftImage.Source = horizontalYf23;
            previewHelicopterImage.Source = apache;
            // The FPV/drone preview intentionally uses the same AH-64E side asset.
            previewDroneImage.Source = apache;
            previewGroundVisual = new Grid { Visibility = Visibility.Collapsed };
            previewGroundImage = new Image { Width = 290, Height = 110, Stretch = Stretch.Uniform, Opacity = 0.96, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 3, 0, 0), Source = LoadEmbeddedImage("UTL.preview-m1a2-sepv3.png") };
            previewGroundVisual.Children.Add(previewGroundImage);
            previewClipContent.Children.Insert(Math.Max(0, previewClipContent.Children.Count - 1), previewGroundVisual);
            groundWorkspacePanel = new Grid { Visibility = Visibility.Collapsed, MaxWidth = 720 };
            BuildGroundWorkspace();
            weaponTableClipContent.Children.Add(groundWorkspacePanel);
        }

        private void BuildGroundWorkspace()
{
    groundWorkspacePanel.Children.Clear();
    groundWorkspacePanel.RowDefinitions.Clear();
    if (selectedAircraft == null) return;
    AircraftSettings settings = controller.WorkspaceGetSettings(selectedAircraft);
    GroundWeaponCacheData groundCache = controller.WorkspaceGetGroundWeaponCache(selectedAircraft);
    List<GroundAmmoLoadout> loadouts = new List<GroundAmmoLoadout>();
    for (int i = 0; i < 4; i++) loadouts.Add(null);
    List<ComboBox> boxes = new List<ComboBox>();
    List<TextBox> counts = new List<TextBox>();
    StackPanel stack = new StackPanel();
    int primaryCal = 0;
    if (groundCache != null && groundCache.Weapons != null)
    {
        GroundWeaponInfo primary = groundCache.Weapons.FirstOrDefault(x => x != null && !String.IsNullOrWhiteSpace(x.Blk) && !IsSecondaryGroundWeapon(x.Blk));
        if (primary == null) primary = groundCache.Weapons.FirstOrDefault(x => x != null && x.NativeAmmo > 0);
        if (primary != null && !String.IsNullOrWhiteSpace(primary.Blk))
        {
            primaryCal = GroundCalibre(primary.Blk);
            string unit = primaryCal > 0 && primaryCal <= 40 ? "chains" : "rds";
            stack.Children.Add(new TextBlock { Text = ModernText.L("CANNON: ", "主炮: ") + (primaryCal > 0 ? primaryCal.ToString(CultureInfo.InvariantCulture) + " mm \u2022 " : "") + primary.NativeAmmo + " " + unit + " total", Foreground = ModernPalette.Brush(ModernPalette.Text), Margin = new Thickness(0, 2, 0, 8), HorizontalAlignment = HorizontalAlignment.Center });
        }
    }
    List<GroundAmmoOption> options = new List<GroundAmmoOption>();
    options.Add(new GroundAmmoOption { Display = ModernText.L("STOCK \u2022 default ammunition", "STOCK \u2022 default ammunition"), Value = "", Calibre = primaryCal });
    if (groundCache != null && groundCache.BeltOptions != null)
    {
        foreach (GroundWeaponBeltOption belt in groundCache.BeltOptions)
        {
            if (belt == null || String.IsNullOrWhiteSpace(belt.Name)) continue;
            int beltCal = GroundCalibre(belt.Name);
            if (belt.Rounds != null && belt.Rounds.Count > 0)
            {
                foreach (GroundAmmo round in belt.Rounds)
                    if (round != null && !String.IsNullOrWhiteSpace(round.Display))
                        options.Add(new GroundAmmoOption { Display = round.Display + " (" + round.Type + ")", Value = belt.Name, Calibre = beltCal });
            }
            else {
                options.Add(new GroundAmmoOption { Display = belt.Name.Replace('_', ' ').Trim(), Value = belt.Name, Calibre = beltCal });
            }
        }
    }
    TextBlock counter = new TextBlock { Text = "", Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 6, 0, 4) };
    UpdateGroundLoadoutCounter(counter, loadouts, groundCache); // 初始即显示（如 125mm: 0/44 rds）
    for (int slot = 0; slot < 4; slot++)
    {
        Grid row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
        row.Children.Add(new TextBlock { Text = ModernText.L("SLOT ", "槽位 ") + (slot + 1).ToString(CultureInfo.InvariantCulture), Foreground = ModernPalette.Brush(ModernPalette.Text), VerticalAlignment = VerticalAlignment.Center });
        ComboBox combo = new ComboBox { Height = 30, Padding = new Thickness(6, 2, 6, 2), Foreground = ModernPalette.Brush(ModernPalette.Text), Background = ModernPalette.Brush("#FF16283E"), BorderBrush = ModernPalette.Brush(ModernPalette.Border), ItemsSource = options, DisplayMemberPath = "Display", IsTextSearchEnabled = true, IsTextSearchCaseSensitive = false };
        TextBox countBox = new TextBox { Height = 30, Text = "0", Foreground = ModernPalette.Brush(ModernPalette.Text), Background = ModernPalette.Brush("#FF16283E"), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Padding = new Thickness(6, 3, 6, 3), TextAlignment = TextAlignment.Center };
        int slotCopy = slot;
        combo.SelectionChanged += delegate {
            if (groundLoadoutSyncing || combo.SelectedItem == null) return;
            GroundAmmoOption opt = combo.SelectedItem as GroundAmmoOption;
            if (opt == null) return;
            int cal = opt.Calibre;
            bool isBelt = cal > 0 && cal <= 40;
            int count = 0;
            Int32.TryParse(countBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out count);
            if (count < 0) count = 0;
            if (count == 0)
            {
                // Ask3lad-style: an empty slot auto-fills the remaining pool budget.
                int maxTotal = GroundAmmoCapacity(groundCache, cal);
                int remaining = Math.Max(0, maxTotal - GroundLoadoutUsed(loadouts, cal));
                count = remaining;
                if (remaining > 0) countBox.Text = count.ToString(CultureInfo.InvariantCulture);
            }
            loadouts[slotCopy] = new GroundAmmoLoadout { Slot = slotCopy, Count = count, SourceBlk = String.IsNullOrEmpty(opt.Value) ? "stock:" + (cal > 0 ? cal.ToString(CultureInfo.InvariantCulture) : "0") : null, BulletName = opt.Value };
            SyncGroundLoadoutBoxes(boxes, counts, loadouts);
            UpdateGroundLoadoutCounter(counter, loadouts, groundCache);
        };
        countBox.LostFocus += delegate {
            int count = 0;
            Int32.TryParse(countBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out count);
            GroundAmmoLoadout lo = loadouts[slotCopy];
            if (lo != null)
            {
                if (count <= 0) { loadouts[slotCopy] = null; }
                else
                {
                    // 所有槽合计不能超过总量：只 clamp 当前槽，不削减其他已配槽
                    int loCal = GroundLoadoutCalibre(lo);
                    int maxTotal = GroundAmmoCapacity(groundCache, loCal);
                    int others = GroundLoadoutUsed(loadouts, loCal) - lo.Count;
                    int maxForSlot = Math.Max(0, maxTotal - others);
                    if (count > maxForSlot) count = maxForSlot;
                    lo.Count = Math.Max(0, count);
                }
            }
            SyncGroundLoadoutBoxes(boxes, counts, loadouts);
            UpdateGroundLoadoutCounter(counter, loadouts, groundCache);
        };
        Grid.SetColumn(combo, 1); row.Children.Add(combo);
        Grid.SetColumn(countBox, 2); row.Children.Add(countBox);
        stack.Children.Add(row);
        boxes.Add(combo); counts.Add(countBox);
    }
    stack.Children.Add(counter);
    Grid actionRow = new Grid { Margin = new Thickness(0, 6, 0, 0) };
    actionRow.ColumnDefinitions.Add(new ColumnDefinition());
    actionRow.ColumnDefinitions.Add(new ColumnDefinition());
    Button clearAll = new Button { Content = ModernText.L("CLEAR ALL", "全部清空"), Style = (Style)Resources["ButtonStyle"], Padding = new Thickness(18, 2, 18, 2), Margin = new Thickness(0, 0, 6, 0), HorizontalAlignment = HorizontalAlignment.Right, Foreground = ModernPalette.Brush(ModernPalette.Muted) };
    clearAll.Click += delegate {
        for (int i = 0; i < 4; i++)
        {
            loadouts[i] = null;
            if (boxes[i] != null) boxes[i].SelectedItem = null;
            if (counts[i] != null) counts[i].Text = "0";
        }
        UpdateGroundLoadoutCounter(counter, loadouts, groundCache);
    };
    actionRow.Children.Add(clearAll);
    Button apply = new Button { Content = ModernText.L("APPLY TO MISSION", "应用到任务"), Style = (Style)Resources["ButtonStyle"], Padding = new Thickness(18, 2, 18, 2), HorizontalAlignment = HorizontalAlignment.Center };
    apply.Click += delegate {
        if (selectedAircraft == null) return;
        settings.GroundAmmoLoadouts.Clear();
        foreach (GroundAmmoLoadout lo in loadouts) if (lo != null && lo.Count > 0) settings.GroundAmmoLoadouts.Add(lo);
        controller.WorkspaceSetSettings(selectedAircraft, settings);
    };
    Grid.SetColumn(apply, 1); actionRow.Children.Add(apply);
    stack.Children.Add(actionRow);
    groundWorkspacePanel.Children.Add(stack);
}

private static bool groundLoadoutSyncing;

private sealed class GroundAmmoOption
{
    public string Display { get; set; }
    public string Value { get; set; }
    public int Calibre { get; set; }
    public override string ToString() { return Display ?? ""; }
}

private static int GroundAmmoCapacity(GroundWeaponCacheData cache, int cal)
{
    if (cache == null || cache.Weapons == null) return 38;
    bool isBelt = cal > 0 && cal <= 40;
    int beltSize = 0;
    if (isBelt && cache.BeltSizes != null) cache.BeltSizes.TryGetValue(cal.ToString(CultureInfo.InvariantCulture), out beltSize);
    int total = 0;
    foreach (GroundWeaponInfo w in cache.Weapons)
    {
        if (w == null || String.IsNullOrWhiteSpace(w.Blk) || w.NativeAmmo <= 0) continue;
        if (cal > 0 && GroundCalibre(w.Blk) != cal) continue;
        total += isBelt && beltSize > 0 ? Math.Max(1, w.NativeAmmo / beltSize) : w.NativeAmmo;
    }
    return Math.Max(1, total);
}

private static int GroundLoadoutCalibre(GroundAmmoLoadout lo)
{
    if (lo == null) return 0;
    string name = lo.BulletName != null && lo.BulletName.Length > 0 ? lo.BulletName : (lo.SourceBlk ?? "");
    int cal = GroundCalibre(name);
    if (cal <= 0 && lo.SourceBlk != null && lo.SourceBlk.StartsWith("stock:", StringComparison.OrdinalIgnoreCase))
    {
        string num = lo.SourceBlk.Substring(6);
        int v;
        if (Int32.TryParse(num, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) cal = v;
    }
    return cal;
}

private static int GroundLoadoutUsed(List<GroundAmmoLoadout> loadouts, int cal)
{
    int used = 0;
    if (loadouts == null) return 0;
    foreach (GroundAmmoLoadout lo in loadouts)
    {
        if (lo == null || lo.Count <= 0) continue;
        int loCal = GroundLoadoutCalibre(lo);
        if (cal <= 0 || loCal == cal || loCal <= 0) used += lo.Count;
    }
    return used;
}

private static void TrimGroundLoadouts(List<GroundAmmoLoadout> loadouts, GroundWeaponCacheData cache)
{
    if (loadouts == null || cache == null) return;
    HashSet<int> cals = new HashSet<int>();
    foreach (GroundAmmoLoadout lo in loadouts)
    {
        if (lo == null || lo.Count <= 0) continue;
        int loCal = GroundLoadoutCalibre(lo);
        cals.Add(loCal);
    }
    foreach (int cal in cals)
    {
        int maxTotal = GroundAmmoCapacity(cache, cal);
        int used = GroundLoadoutUsed(loadouts, cal);
        if (used <= maxTotal) continue;
        for (int i = loadouts.Count - 1; i >= 0 && used > maxTotal; i--)
        {
            GroundAmmoLoadout lo = loadouts[i];
            if (lo == null || lo.Count <= 0) continue;
            int loCal = GroundLoadoutCalibre(lo);
            if (cal != loCal) continue;
            int cut = Math.Min(lo.Count, used - maxTotal);
            lo.Count -= cut;
            used -= cut;
            if (lo.Count <= 0) loadouts[i] = null;
        }
    }
}

private static void SyncGroundLoadoutBoxes(List<ComboBox> boxes, List<TextBox> counts, List<GroundAmmoLoadout> loadouts)
{
    if (groundLoadoutSyncing || boxes == null || loadouts == null) return;
    groundLoadoutSyncing = true;
    try
    {
        for (int i = 0; i < boxes.Count && i < 4; i++)
    {
        if (boxes[i] == null) continue;
        GroundAmmoLoadout lo = loadouts[i];
        if (lo == null)
        {
            boxes[i].SelectedItem = null;
            continue;
        }
        GroundAmmoOption match = null;
        foreach (object o in boxes[i].Items)
        {
            GroundAmmoOption opt = o as GroundAmmoOption;
            if (opt == null) continue;
            if (String.IsNullOrEmpty(lo.BulletName) && String.IsNullOrEmpty(opt.Value) && lo.SourceBlk != null && lo.SourceBlk.StartsWith("stock:", StringComparison.OrdinalIgnoreCase)) { match = opt; break; }
            if (!String.IsNullOrEmpty(lo.BulletName) && String.Equals(opt.Value, lo.BulletName, StringComparison.OrdinalIgnoreCase)) { match = opt; break; }
        }
            if (match != null) boxes[i].SelectedItem = match;
            if (counts != null && i < counts.Count && counts[i] != null) counts[i].Text = lo.Count.ToString(CultureInfo.InvariantCulture);
        }
    }
    finally { groundLoadoutSyncing = false; }
}

private static void UpdateGroundLoadoutCounter(TextBlock counter, List<GroundAmmoLoadout> loadouts, GroundWeaponCacheData cache)
{
    if (counter == null) return;
    if (cache == null || cache.Weapons == null) { counter.Text = ""; return; }
    // 每口径一个弹药池（跳过次要武器：机枪/烟雾），Ask3lad 格式：
    //   "30mm: 0/2 belts  |  152mm: 8/8"
    // 只显示需要用户选弹药的武器口径：该口径存在弹药包容器（beltOptions）或导弹挂载；
    // 机枪（含 NSV 这类名字不含 machinegun 的）和烟雾弹没有弹药包，因此不显示。
    List<int> beltCals = new List<int>();
    if (cache.BeltOptions != null)
    {
        foreach (GroundWeaponBeltOption b in cache.BeltOptions)
        {
            if (b == null || b.Calibre <= 0) continue;
            if (!beltCals.Contains(b.Calibre)) beltCals.Add(b.Calibre);
        }
    }
    List<GroundWeaponInfo> pools = new List<GroundWeaponInfo>();
    foreach (GroundWeaponInfo w in cache.Weapons)
    {
        if (w == null || String.IsNullOrWhiteSpace(w.Blk) || w.NativeAmmo <= 0) continue;
        if (IsSecondaryGroundWeapon(w.Blk)) continue;
        int wcal = GroundCalibre(w.Blk);
        if (wcal <= 0) continue;
        if (!beltCals.Contains(wcal))
        {
            bool hasMissiles = cache.Missiles != null && cache.Missiles.Any(x => !String.IsNullOrWhiteSpace(x.Key) && GroundCalibre(x.Key) == wcal);
            if (!hasMissiles) continue;
        }
        if (!pools.Any(x => GroundCalibre(x.Blk) == wcal)) pools.Add(w);
    }
    if (pools.Count == 0) { counter.Text = ""; return; }
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    foreach (GroundWeaponInfo pool in pools)
    {
        int pcal = GroundCalibre(pool.Blk);
        int total = GroundAmmoCapacity(cache, pcal);
        int used = GroundLoadoutUsed(loadouts, pcal);
        if (used > total) used = total; // 仅显示，不裁剪
        string unit = pcal <= 40 ? ModernText.L("belts", "链") : ModernText.L("rds", "发");
        if (sb.Length > 0) sb.Append("  |  ");
        sb.Append(pcal.ToString(CultureInfo.InvariantCulture)).Append("mm: ").Append(used.ToString(CultureInfo.InvariantCulture)).Append("/").Append(total.ToString(CultureInfo.InvariantCulture)).Append(" ").Append(unit);
    }
    counter.Text = sb.ToString();
}

private void RefreshGroundWorkspace()
        {
            if (selectedAircraft == null) return;
            BuildGroundWorkspace();
        }

        private void GroundCannonChanged()
        {
            try
            {
                if (groundUpdating) return;
                ComboBoxItem item = groundCannonBox == null ? null : groundCannonBox.SelectedItem as ComboBoxItem;
                GroundCannonTag tag = item == null ? null : item.Tag as GroundCannonTag;
                if (tag == null) return;
                groundCannonBlk = tag.Blk;
                groundCannonNative = tag.Native;
                GroundRefreshAmmo();
                GroundUpdateSettings();
            }
            catch { }
        }

        private void GroundRefreshAmmo()
        {
            try
            {
                if (selectedAircraft == null) return;
                IList<GroundAmmo> catalog = controller.WorkspaceGroundAmmo;
                if (catalog == null) return;
                List<string> blks = new List<string>();
                GroundWeaponCacheData groundCache = controller.WorkspaceGetGroundWeaponCache(selectedAircraft);
                IList<GroundWeaponInfo> weapons = groundCache == null ? null : groundCache.Weapons;
                if (weapons != null)
                    foreach (GroundWeaponInfo w in weapons)
                        if (!String.IsNullOrWhiteSpace(w.Blk) && !blks.Any(x => GroundSame(x, w.Blk))) blks.Add(w.Blk);
                groundHasMainWeapon = false;
                if (weapons != null)
                    foreach (GroundWeaponInfo w in weapons)
                        if (!String.IsNullOrWhiteSpace(w.Blk) && !IsSecondaryGroundWeapon(w.Blk)) { groundHasMainWeapon = true; break; }
                // Native totals: missiles = racks x rounds per rack (launcher/container BLK bullets:i);
                // guns (belt weapons, calibre <=40mm) = belt chains (total rounds / belt size);
                // tank guns keep the plain native round count.
                groundNativeTotals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                groundNativeByCalibre = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, int> calibreTotals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, int> calibreBeltSize = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                if (weapons != null)
                {
                    // First pass: aggregate native rounds per calibre; remember one belt
                    // size (single gun bullets:i) per belt calibre (<=40mm).
                    foreach (GroundWeaponInfo w in weapons)
                    {
                        if (String.IsNullOrWhiteSpace(w.Blk) || w.NativeAmmo <= 0) continue;
                        int cal = GroundCalibre(w.Blk);
                        if (cal <= 0) continue;
                        string calKey = cal.ToString(CultureInfo.InvariantCulture);
                        int total;
                        calibreTotals.TryGetValue(calKey, out total);
                        calibreTotals[calKey] = total + w.NativeAmmo;
                        if (cal <= 40 && !calibreBeltSize.ContainsKey(calKey))
                        {
                            int beltSize;
                            if (groundCache != null && groundCache.BeltSizes != null && groundCache.BeltSizes.TryGetValue(calKey, out beltSize) && beltSize > 0)
                                calibreBeltSize[calKey] = beltSize;
                            else calibreBeltSize[calKey] = w.NativeAmmo;
                        }
                    }
                    foreach (KeyValuePair<string, int> pair in calibreTotals)
                    {
                        int cal;
                        if (Int32.TryParse(pair.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out cal))
                        {
                            int beltSize;
                            if (cal <= 40 && calibreBeltSize.TryGetValue(pair.Key, out beltSize) && beltSize > 0)
                                groundNativeByCalibre[pair.Key] = Math.Max(1, pair.Value / beltSize); // belt chains
                            else
                                groundNativeByCalibre[pair.Key] = pair.Value; // plain rounds
                        }
                    }
                    // Second pass: missiles keep per-weapon capacity (racks x rounds per rack).
                    foreach (GroundWeaponInfo w in weapons)
                    {
                        if (String.IsNullOrWhiteSpace(w.Blk) || w.NativeAmmo <= 0 || groundNativeTotals.ContainsKey(w.Blk)) continue;
                        if (w.Blk.IndexOf("launcher", StringComparison.OrdinalIgnoreCase) >= 0 || w.Blk.IndexOf("container", StringComparison.OrdinalIgnoreCase) >= 0)
                            groundNativeTotals[w.Blk] = w.NativeAmmo * Math.Max(1, controller.WorkspaceRackRoundsCached(groundCache, w.Blk));
                    }
                }
                List<GroundAmmoSlotGroup> slotGroups = BuildGroundAmmoSlotGroups(groundCache);
                RebuildGroundSlotUi(slotGroups);
                UpdateAmmoPoolText();
            }
            catch { }
        }

        internal static int GroundCalibre(string blk)
        {
            if (String.IsNullOrWhiteSpace(blk)) return 0;
            Match m = Regex.Match(blk, @"(\d+)(?:_\d+)?mm", RegexOptions.IgnoreCase);
            if (!m.Success) return 0;
            int value;
            return Int32.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : 0;
        }

        internal static bool IsSecondaryGroundWeapon(string blk)
        {
            if (String.IsNullOrWhiteSpace(blk)) return false;
            return blk.IndexOf("machinegun", StringComparison.OrdinalIgnoreCase) >= 0 || blk.IndexOf("smoke", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void GroundLoadSlots(AircraftSettings settings)
        {
            foreach (ComboBox slotBox in groundSlotBoxes) if (slotBox != null) slotBox.SelectedItem = null;
            foreach (TextBox countBox in groundSlotCounts) if (countBox != null) countBox.Text = "0";
            if (settings == null || settings.GroundAmmoLoadouts == null) { GroundUpdateSlotTotals(); return; }
            foreach (GroundAmmoLoadout loadout in settings.GroundAmmoLoadouts)
            {
                if (loadout == null || loadout.Slot < 0 || loadout.Slot >= groundSlotBoxes.Count) continue;
                GroundAmmoEntry entry = GroundFindEntry(loadout);
                if (entry == null || entry.Ammo == null) continue;
                groundSlotBoxes[loadout.Slot].SelectedItem = entry;
                groundSlotCounts[loadout.Slot].Text = Math.Max(1, loadout.Count).ToString(CultureInfo.InvariantCulture);
            }
            GroundUpdateSlotTotals();
            // Note: persisted loadouts that cannot be shown in the current options
            // (e.g. catalog gun projectiles mounted through GROUND CONFIGURE, which
            // are not part of the belt-type option list) are intentionally kept -
            // they are still written into the mission. Dropping them silently ate
            // user configuration (T-80BVM 3BM60 was lost this way).
        }

        private GroundAmmoEntry GroundFindEntry(GroundAmmoLoadout loadout)
        {
            if (loadout == null) return null;
            return GroundFindEntry(loadout.SourceBlk, loadout.BulletName);
        }

        private GroundAmmoEntry GroundFindEntry(string sourceBlk, string bulletName)
        {
            foreach (GroundAmmoSlotGroup group in groundSlotGroups)
                foreach (GroundAmmoEntry entry in group.Options)
                {
                    if (entry == null || entry.Ammo == null) continue;
                    if (String.IsNullOrWhiteSpace(bulletName))
                    {
                        // STOCK entry: match by the calibre pool tag stored in SourceBlk ("stock:125").
                        if (!String.IsNullOrWhiteSpace(sourceBlk) && sourceBlk.StartsWith("stock:", StringComparison.OrdinalIgnoreCase))
                        {
                            string cal = sourceBlk.Substring(6);
                            if (entry.Ammo.Display != null && String.IsNullOrWhiteSpace(entry.Ammo.BulletName)
                                && entry.Ammo.Display.StartsWith(cal + "mm", StringComparison.OrdinalIgnoreCase))
                                return entry;
                        }
                        continue;
                    }
                    if (entry.Ammo.BulletName != null
                        && (entry.Ammo.BulletName.Equals(bulletName, StringComparison.OrdinalIgnoreCase)
                            || (entry.Ammo.Display != null && entry.Ammo.Display.Equals(bulletName, StringComparison.OrdinalIgnoreCase)))
                        && GroundSame(entry.Ammo.SourceBlk, sourceBlk)) return entry;
                }
            return null;
        }

        private void GroundRefreshAmmoPresets()
        {
            try
            {
                ammoPresets = ModernShellStorage.LoadAmmoPresets();
                string vehicleId = selectedAircraft == null ? null : selectedAircraft.Id;
                object current = ammoPresetBox == null ? null : ammoPresetBox.SelectedItem;
                ammoPresetBox.Items.Clear();
                foreach (AmmoPreset preset in ammoPresets)
                    if (preset.VehicleId != null && preset.VehicleId.Equals(vehicleId, StringComparison.OrdinalIgnoreCase) && !String.IsNullOrWhiteSpace(preset.Name))
                        ammoPresetBox.Items.Add(preset.Name);
                if (current != null && ammoPresetBox.Items.Contains(current)) ammoPresetBox.SelectedItem = current;
            }
            catch { }
        }

        private void GroundSaveAmmoPreset()
        {
            try
            {
                if (selectedAircraft == null) return;
                string suggested = selectedAircraft.Display + " - " + DateTime.Now.ToString("MMdd-HHmm", CultureInfo.InvariantCulture);
                ModernInputWindow input = new ModernInputWindow("SAVE AMMO PRESET", "Name this ammunition preset. It is stored per vehicle in ammo_loadouts.tsv (LocalAppData) and can be loaded back anytime.", suggested) { Owner = Owner };
                if (input.ShowDialog() != true) return;
                string name = input.Value == null ? null : input.Value.Trim();
                if (String.IsNullOrWhiteSpace(name)) return;
                AmmoPreset preset = new AmmoPreset { Name = name, VehicleId = selectedAircraft.Id };
                for (int i = 0; i < groundSlotBoxes.Count; i++)
                {
                    GroundAmmoEntry entry = groundSlotBoxes[i] == null ? null : groundSlotBoxes[i].SelectedItem as GroundAmmoEntry;
                    if (entry == null || entry.Ammo == null) { preset.Slots[i] = null; continue; }
                    int count;
                    if (!Int32.TryParse(groundSlotCounts[i].Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out count)) count = 1;
                    int max = entry.Native > 0 ? entry.Native : 9999;
                    preset.Slots[i] = new GroundAmmoLoadout { Slot = i, Count = Math.Max(1, Math.Min(max, count)), SourceBlk = entry.Ammo.SourceBlk, BulletName = entry.Ammo.BulletName, Kind = entry.Ammo.Type };
                }
                ModernShellStorage.SaveAmmoPreset(preset);
                GroundRefreshAmmoPresets();
                ammoPresetBox.SelectedItem = name;
            }
            catch { }
        }

        private void GroundLoadAmmoPreset()
        {
            try
            {
                if (selectedAircraft == null || ammoPresetBox == null) return;
                string name = ammoPresetBox.SelectedItem as string;
                if (String.IsNullOrWhiteSpace(name)) return;
                AmmoPreset preset = ammoPresets.FirstOrDefault(x => x.VehicleId != null && x.VehicleId.Equals(selectedAircraft.Id, StringComparison.OrdinalIgnoreCase) && x.Name != null && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (preset == null) return;
                groundUpdating = true;
                try
                {
                    for (int i = 0; i < groundSlotBoxes.Count; i++)
                    {
                        groundSlotBoxes[i].SelectedItem = null;
                        groundSlotCounts[i].Text = "0";
                        GroundAmmoLoadout slot = preset.Slots == null || i >= preset.Slots.Length ? null : preset.Slots[i];
                        if (slot == null) continue;
                        GroundAmmoEntry entry = GroundFindEntry(slot.SourceBlk, slot.BulletName);
                        if (entry == null) continue;
                        groundSlotBoxes[i].SelectedItem = entry;
                        groundSlotCounts[i].Text = Math.Max(1, slot.Count).ToString(CultureInfo.InvariantCulture);
                    }
                }
                finally { groundUpdating = false; }
                GroundUpdateSettings();
            }
            catch { }
        }

        private int GroundNativeFor(string blk)
        {
            if (String.IsNullOrWhiteSpace(blk)) return 0;
            foreach (KeyValuePair<string, int> pair in groundNativeTotals)
                if (GroundSame(pair.Key, blk)) return pair.Value;
            return 0;
        }

        private int GroundNativeForCalibre(int cal)
        {
            if (groundNativeByCalibre == null) return 0;
            int value;
            return groundNativeByCalibre.TryGetValue(cal.ToString(CultureInfo.InvariantCulture), out value) ? value : 0;
        }

        private void UpdateAmmoPoolText()
        {
            if (groundAmmoPoolText == null) return;
            if (groundNativeByCalibre == null || groundNativeByCalibre.Count == 0)
            {
                groundAmmoPoolText.Text = String.Empty;
                return;
            }
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (KeyValuePair<string, int> pair in groundNativeByCalibre.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                int cal;
                if (!Int32.TryParse(pair.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out cal)) continue;
                if (groundHasMainWeapon && cal < 20) continue;
                int used = 0;
                for (int i = 0; i < groundSlotBoxes.Count; i++)
                {
                    GroundAmmoEntry entry = groundSlotBoxes[i] == null ? null : groundSlotBoxes[i].SelectedItem as GroundAmmoEntry;
                    if (entry == null || entry.Ammo == null) continue;
                    int entryCal = GroundCalibre(entry.Ammo.BulletName.Length > 0 ? entry.Ammo.BulletName : entry.Ammo.Display);
                    if (entryCal != cal) continue;
                    int count;
                    if (Int32.TryParse(groundSlotCounts[i].Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
                        used += Math.Max(0, count);
                }
                bool gunBelt = cal <= 40;
                if (sb.Length > 0) sb.Append("    ");
                sb.Append(pair.Key + "mm: " + used.ToString(CultureInfo.InvariantCulture) + "/" + pair.Value.ToString(CultureInfo.InvariantCulture) + (gunBelt ? " chains" : " rds"));
            }
            groundAmmoPoolText.Text = sb.ToString();
        }

        private void GroundUpdateSettings()
        {
            if (groundUpdating || selectedAircraft == null) return;
            try
            {
                AircraftSettings settings = controller.WorkspaceGetSettings(selectedAircraft) ?? new AircraftSettings();
                settings.GroundAmmoLoadouts.Clear();
                for (int i = 0; i < groundSlotBoxes.Count; i++)
                {
                    GroundAmmoEntry entry = groundSlotBoxes[i].SelectedItem as GroundAmmoEntry;
                    if (entry == null || entry.Ammo == null) continue;
                    int count;
                    if (!Int32.TryParse(groundSlotCounts[i].Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out count)) count = 0;
                    if (count <= 0) continue; // 0 = empty slot (mission slot omitted)
                    GroundAmmoSlotGroup group = GroundSlotGroupFor(i);
                    int max = group != null && group.MaxTotal > 0 ? group.MaxTotal : 9999;
                    count = Math.Min(max, count);
                    string saveBlk = entry.Ammo.SourceBlk;
                    if (String.IsNullOrWhiteSpace(entry.Ammo.BulletName))
                        saveBlk = "stock:" + GroundCalibre(entry.Ammo.Display).ToString(CultureInfo.InvariantCulture);
                    settings.GroundAmmoLoadouts.Add(new GroundAmmoLoadout { Slot = i, Count = count, SourceBlk = saveBlk, BulletName = entry.Ammo.BulletName, Kind = entry.Ammo.Type });
                }
                // per-weapon cap: a weapon's slots share its total capacity; trim from the tail
                if (groundSlotGroups != null)
                    foreach (GroundAmmoSlotGroup group in groundSlotGroups)
                    {
                        int used = 0;
                        for (int s = 0; s < group.SlotCount; s++)
                        {
                            int idx = group.FirstSlot + s;
                            if (idx >= groundSlotBoxes.Count) continue;
                            GroundAmmoLoadout lo = settings.GroundAmmoLoadouts.FirstOrDefault(x => x.Slot == idx);
                            if (lo != null) used += lo.Count;
                        }
                        for (int s = group.SlotCount - 1; s >= 0 && used > group.MaxTotal; s--)
                        {
                            int idx = group.FirstSlot + s;
                            if (idx >= groundSlotBoxes.Count) continue;
                            GroundAmmoLoadout lo = settings.GroundAmmoLoadouts.FirstOrDefault(x => x.Slot == idx);
                            if (lo == null) continue;
                            int cut = Math.Min(lo.Count, used - group.MaxTotal);
                            lo.Count -= cut;
                            used -= cut;
                            if (lo.Count <= 0) settings.GroundAmmoLoadouts.Remove(lo);
                            else groundSlotCounts[idx].Text = lo.Count.ToString(CultureInfo.InvariantCulture);
                        }
                    }
                settings.InjectedCannonBlk = null;
                settings.InjectedCannonDomain = null;
                settings.InjectedCannonUnit = null;
                UpdateAmmoPoolText();
                GroundUpdateSlotTotals();
                controller.WorkspaceSetSettings(selectedAircraft, settings);
            }
            catch (Exception groundUpdateEx)
            {
                try
                {
                    System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniversalTestLab", "ground_settings_error.log"),
                        DateTime.Now.ToString("HH:mm:ss") + " " + groundUpdateEx.ToString() + Environment.NewLine);
                }
                catch { }
            }
        }

        private List<GroundAmmoSlotGroup> BuildGroundAmmoSlotGroups(GroundWeaponCacheData cache)
        {
            List<GroundAmmoSlotGroup> groups = new List<GroundAmmoSlotGroup>();
            if (cache == null || cache.Weapons == null) return groups;
            // ammo options per calibre from belt-type modification modules (excluding MG
            // calibres when a main weapon exists, matching Ask3lad behaviour).
            Dictionary<int, List<string>> optionsByCal = new Dictionary<int, List<string>>();
            HashSet<string> missileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (cache.Missiles != null)
                    foreach (KeyValuePair<string, string> pair in cache.Missiles)
                        missileNames.Add(pair.Key);
            }
            catch { }
            if (cache.BeltOptions != null)
                foreach (GroundWeaponBeltOption option in cache.BeltOptions)
                {
                    if (option == null || String.IsNullOrWhiteSpace(option.Name) || option.Name.IndexOf("_ammo_pack", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    if (missileNames.Contains(option.Name)) continue;
                    int cal = option.Calibre >0 ? option.Calibre : GroundCalibre(option.Name);
                    if (cal <= 0) continue;
                    if (groundHasMainWeapon && cal < 20) continue;
                    List<string> list;
                    if (!optionsByCal.TryGetValue(cal, out list)) { list = new List<string>(); optionsByCal[cal] = list; }
                    list.Add(option.Name);
                }
            // Concrete rounds per belt-option container (from ground_ammo.json) so the
            // UI can show e.g. 3BM60 while still writing the container name.
            Dictionary<string, IList<GroundAmmo>> roundsByContainer = new Dictionary<string, IList<GroundAmmo>>(StringComparer.OrdinalIgnoreCase);
            if (cache.BeltOptions != null)
                foreach (GroundWeaponBeltOption option in cache.BeltOptions)
                {
                    if (option == null || String.IsNullOrWhiteSpace(option.Name) || option.Rounds == null || option.Rounds.Count == 0) continue;
                    roundsByContainer[option.Name] = option.Rounds;
                }
            int nextSlot = 0;
            foreach (GroundWeaponInfo w in cache.Weapons)
            {
                if (String.IsNullOrWhiteSpace(w.Blk) || w.NativeAmmo <= 0) continue;
                if (groundHasMainWeapon && IsSecondaryGroundWeapon(w.Blk)) continue;
                if (nextSlot >= 4) break;
                int cal = GroundCalibre(w.Blk);
                if (cal <= 0) continue;
                string calKey = cal.ToString(CultureInfo.InvariantCulture);
                bool isBelt = cal <= 40;
                List<string> options;
                optionsByCal.TryGetValue(cal, out options);
                int optionCount = options == null ? 0 : options.Count;
                bool hasMissilesForCal = false;
                try
                {
                    if (cache.Missiles != null)
                        foreach (KeyValuePair<string, string> pair in cache.Missiles)
                            if (GroundCalibre(pair.Value) == cal) { hasMissilesForCal = true; break; }
                }
                catch { }
                // Weapons with no configurable ammunition (e.g. the 81mm Tucha smoke
                // grenade launcher) do not occupy ammunition slots.
                if (optionCount <= 0 && !hasMissilesForCal) continue;
                int slots;
                int maxTotal;
                if (isBelt)
                {
                    slots = Math.Max(1, Math.Min(Math.Max(1, cache.BeltTypeLimit), Math.Max(1, optionCount)));
                    int beltSize = 0;
                    if (cache.BeltSizes != null) cache.BeltSizes.TryGetValue(calKey, out beltSize);
                    if (beltSize <= 0) beltSize = w.NativeAmmo;
                    maxTotal = Math.Max(1, w.NativeAmmo / beltSize);
                }
                else
                {
                    slots = Math.Max(1, Math.Min(Math.Max(1, optionCount), 4 - nextSlot));
                    maxTotal = w.NativeAmmo;
                }
                slots = Math.Min(slots, 4 - nextSlot);
                if (slots <= 0) continue;
                GroundAmmoSlotGroup group = new GroundAmmoSlotGroup
                {
                    WeaponBlk = w.Blk, Calibre = cal, IsBelt = isBelt, SlotCount = slots,
                    MaxTotal = maxTotal, FirstSlot = nextSlot
                };
                string fileName = w.Blk;
                int slash = fileName.LastIndexOf('/');
                if (slash >= 0) fileName = fileName.Substring(slash + 1);
                fileName = fileName.Replace("_user_cannon.blk", "").Replace("_user_machinegun.blk", "").Replace(".blk", "").Replace('_', ' ');
                group.Display = fileName;
                // STOCK option (empty = default ammo) plus the calibre's ammunition types.
                group.Options.Add(new GroundAmmoEntry
                {
                    Ammo = new GroundAmmo { SourceBlk = "stock:" + calKey, BulletName = String.Empty, Display = calKey + "mm STOCK (default ammo)", Type = isBelt ? "Belt" : "Shell" },
                    Native = maxTotal,
                    Text = calKey + "mm STOCK (default ammo) \u2022 " + maxTotal.ToString(CultureInfo.InvariantCulture) + (isBelt ? " chains" : " rds")
                });
                if (options != null)
                    foreach (string option in options)
                    {
                        // Belt-option containers may carry concrete rounds (bulletName) - show
                        // those (e.g. 3BM60) while keeping the container name as the written value.
                        IList<GroundAmmo> rounds = null;
                        if (roundsByContainer != null && roundsByContainer.TryGetValue(option, out rounds) && rounds != null && rounds.Count > 0)
                        {
                            foreach (GroundAmmo round in rounds)
                            {
                                string display = round.BulletName.Replace('_', ' ').Trim();
                                group.Options.Add(new GroundAmmoEntry
                                {
                                    Ammo = new GroundAmmo { SourceBlk = null, BulletName = option, Display = round.BulletName, Type = round.Type },
                                    Native = maxTotal,
                                    Text = display + " \u2022 " + maxTotal.ToString(CultureInfo.InvariantCulture) + (isBelt ? " chains" : " rds")
                                });
                            }
                        }
                        else
                        {
                            string display = option.Replace('_', ' ').Trim();
                            group.Options.Add(new GroundAmmoEntry
                            {
                                Ammo = new GroundAmmo { SourceBlk = null, BulletName = option, Display = display, Type = isBelt ? "Belt" : "Shell" },
                                Native = maxTotal,
                                Text = display + " \u2022 " + maxTotal.ToString(CultureInfo.InvariantCulture) + (isBelt ? " chains" : " rds")
                            });
                        }
                    }
                // missile preset names for this calibre (e.g. 170mm_57e6_aam).
                try
                {
                    if (cache.Missiles != null)
                        foreach (KeyValuePair<string, string> pair in cache.Missiles)
                        {
                            if (GroundCalibre(pair.Value) == cal)
                            {
                                string display = pair.Key.Replace('_', ' ');
                                group.Options.Add(new GroundAmmoEntry
                                {
                                    Ammo = new GroundAmmo { SourceBlk = pair.Value, BulletName = pair.Key, Display = display, Type = "SAM" },
                                    Native = maxTotal,
                                    Text = display + " \u2022 " + maxTotal.ToString(CultureInfo.InvariantCulture) + " rds"
                                });
                            }
                        }
                }
                catch { }
                groups.Add(group);
                nextSlot += slots;
            }
            // Fallback: vehicles whose weapons have no modification modules at all still
            // get one STOCK-only slot so the ammunition panel stays usable.
            if (groups.Count == 0 && cache.Weapons != null)
            {
                foreach (GroundWeaponInfo w in cache.Weapons)
                {
                    if (String.IsNullOrWhiteSpace(w.Blk) || w.NativeAmmo <= 0) continue;
                    if (IsSecondaryGroundWeapon(w.Blk)) continue;
                    int cal = GroundCalibre(w.Blk);
                    if (cal <= 0) continue;
                    string calKey = cal.ToString(CultureInfo.InvariantCulture);
                    bool isBelt = cal <= 40;
                    GroundAmmoSlotGroup group = new GroundAmmoSlotGroup
                    {
                        WeaponBlk = w.Blk, Calibre = cal, IsBelt = isBelt, SlotCount = 1,
                        MaxTotal = w.NativeAmmo, FirstSlot = 0
                    };
                    string fileName = w.Blk;
                    int slash = fileName.LastIndexOf('/');
                    if (slash >= 0) fileName = fileName.Substring(slash + 1);
                    group.Display = fileName.Replace("_user_cannon.blk", "").Replace(".blk", "").Replace('_', ' ');
                    group.Options.Add(new GroundAmmoEntry
                    {
                        Ammo = new GroundAmmo { SourceBlk = "stock:" + calKey, BulletName = String.Empty, Display = calKey + "mm STOCK (default ammo)", Type = isBelt ? "Belt" : "Shell" },
                        Native = w.NativeAmmo,
                        Text = calKey + "mm STOCK (default ammo) \u2022 " + w.NativeAmmo.ToString(CultureInfo.InvariantCulture) + (isBelt ? " chains" : " rds")
                    });
                    groups.Add(group);
                    break;
                }
            }
            return groups;
        }

        private void RebuildGroundSlotUi(List<GroundAmmoSlotGroup> groups)
        {
            groundSlotGroups = groups ?? new List<GroundAmmoSlotGroup>();
            groundSlotBoxes = new List<ComboBox>();
            groundSlotCounts = new List<TextBox>();
            if (groundGroupsPanel == null) return;
            groundGroupsPanel.Children.Clear();
            int globalSlot = 0;
            foreach (GroundAmmoSlotGroup group in groundSlotGroups)
            {
                StackPanel groupPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
                TextBlock totalText = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Muted), FontSize = 11, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
                group.TotalText = totalText;
                TextBlock title = new TextBlock { Text = group.Display, Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 12, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
                Grid header = new Grid();
                header.ColumnDefinitions.Add(new ColumnDefinition());
                header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                header.Children.Add(title);
                Grid.SetColumn(totalText, 1);
                header.Children.Add(totalText);
                groupPanel.Children.Add(header);
                WrapPanel slotsRow = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
                for (int s = 0; s < group.SlotCount; s++)
                {
                    Grid slot = new Grid { Margin = new Thickness(0, 0, 8, 6) };
                    slot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                    slot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
                    ComboBox combo = new ComboBox { Height = 28, VerticalContentAlignment = VerticalAlignment.Center, Width = 150, ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel))) };
                    combo.ItemsSource = group.Options;
                    combo.SelectionChanged += delegate
                    {
                        if (groundUpdating) return;
                        // Selecting a round auto-fills a count so the choice is not silently
                        // dropped (0 = empty slot). STOCK (empty bullet name) also gets a count:
                        // Ask3lad writes bulletsN:t="" + count to load the native default round
                        // (e.g. T-80BVM 3BK18M) alongside other slots. The count fills the
                        // remaining ammo-pool budget (maxTotal minus the other slots of this
                        // group), so combinations like "half STOCK + half round" stay in range.
                        int idx = groundSlotBoxes.IndexOf(combo);
                        if (idx >= 0 && idx < groundSlotCounts.Count && groundSlotCounts[idx] != null && groundSlotCounts[idx].Text.Trim() == "0")
                        {
                            GroundAmmoEntry sel = combo.SelectedItem as GroundAmmoEntry;
                            if (sel != null && sel.Ammo != null)
                            {
                                GroundAmmoSlotGroup grp = GroundSlotGroupFor(idx);
                                int otherUsed = 0;
                                if (grp != null)
                                {
                                    for (int k = grp.FirstSlot; k < grp.FirstSlot + grp.SlotCount && k < groundSlotCounts.Count; k++)
                                    {
                                        if (k == idx || groundSlotCounts[k] == null) continue;
                                        int oc;
                                        if (Int32.TryParse(groundSlotCounts[k].Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out oc)) otherUsed += oc;
                                    }
                                }
                                int pool = grp != null && grp.MaxTotal > 0 ? grp.MaxTotal : (sel.Native > 0 ? sel.Native : 38);
                                int fill = Math.Max(1, pool - otherUsed);
                                groundSlotCounts[idx].Text = fill.ToString(CultureInfo.InvariantCulture);
                            }
                        }
                        GroundUpdateSettings();
                    };
                    Grid.SetColumn(combo, 0);
                    slot.Children.Add(combo);
                    TextBox countBox = new TextBox { Height = 28, Padding = new Thickness(6, 2, 6, 2), VerticalContentAlignment = VerticalAlignment.Center, Width = 56, Text = "0", ToolTip = "Ammunition count (0 = empty slot)" };
                    countBox.LostFocus += delegate { if (!groundUpdating) GroundUpdateSettings(); };
                    Grid.SetColumn(countBox, 1);
                    slot.Children.Add(countBox);
                    slotsRow.Children.Add(slot);
                    groundSlotBoxes.Add(combo);
                    groundSlotCounts.Add(countBox);
                    globalSlot++;
                }
                groupPanel.Children.Add(slotsRow);
                groundGroupsPanel.Children.Add(groupPanel);
            }
            GroundUpdateSlotTotals();
        }

        private GroundAmmoSlotGroup GroundSlotGroupFor(int slotIndex)
        {
            if (groundSlotGroups == null) return null;
            foreach (GroundAmmoSlotGroup group in groundSlotGroups)
                if (slotIndex >= group.FirstSlot && slotIndex < group.FirstSlot + group.SlotCount) return group;
            return null;
        }

        private void GroundUpdateSlotTotals()
        {
            if (groundSlotGroups == null) return;
            foreach (GroundAmmoSlotGroup group in groundSlotGroups)
            {
                if (group.TotalText == null) continue;
                int used = 0;
                for (int s = 0; s < group.SlotCount; s++)
                {
                    int idx = group.FirstSlot + s;
                    if (idx >= groundSlotBoxes.Count || groundSlotBoxes[idx] == null || groundSlotBoxes[idx].SelectedItem == null) continue;
                    int count;
                    if (Int32.TryParse(groundSlotCounts[idx].Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out count)) used += count;
                }
                group.TotalText.Text = used.ToString(CultureInfo.InvariantCulture) + "/" + group.MaxTotal.ToString(CultureInfo.InvariantCulture) + (group.IsBelt ? " chains" : " rds");
            }
        }

        private static string GroundNorm(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) return String.Empty;
            return path.Replace('\\', '/').ToLowerInvariant();
        }

        private static bool GroundSame(string a, string b)
        {
            return GroundNorm(a).Equals(GroundNorm(b));
        }

        private sealed class GroundCannonTag
        {
            public string Blk;
            public bool Native;
        }

        private static BitmapImage LoadEmbeddedImage(string resourceName)
        {
            BitmapImage image = new BitmapImage();
            using (MemoryStream stream = new MemoryStream(Embedded.Bytes(resourceName)))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
            }
            return image;
        }

        private void WireEvents()
        {
            previewClipContent.SizeChanged += delegate { UpdatePreviewClip(); };
            previewClipContent.Loaded += delegate { UpdatePreviewClip(); };

            titleBar.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                if (e.ClickCount == 2) ToggleMaximize();
                else if (e.ButtonState == MouseButtonState.Pressed) DragMove();
            };
            Button minimize = Find<Button>("MinimizeButton");
            Button maximize = Find<Button>("MaximizeButton");
            Button close = Find<Button>("CloseButton");
            WindowChrome.SetIsHitTestVisibleInChrome(minimize, true);
            WindowChrome.SetIsHitTestVisibleInChrome(maximize, true);
            WindowChrome.SetIsHitTestVisibleInChrome(close, true);
            minimize.Click += delegate { SystemCommands.MinimizeWindow(this); };
            maximize.Click += delegate { ToggleMaximize(); };
            close.Click += delegate { Close(); };

            Find<Button>("BrowseButton").Click += delegate { BrowseGameFolder(); };
            Find<Button>("SyncButton").Click += delegate { SyncBaseMission(); };
            Find<Button>("MissionsButton").Click += delegate { OpenMissionFolder(); };
            Find<Button>("PresetsButton").Click += delegate { ShowPresets(); };
            Find<Button>("AboutButton").Click += delegate { ShowAbout(); };

            aircraftSearch.TextChanged += delegate { FilterAircraft(); };
            nationFilter.SelectionChanged += delegate { FilterAircraft(); };
            rankFilter.SelectionChanged += delegate { FilterAircraft(); };
            typeFilter.SelectionChanged += delegate { FilterAircraft(); };
            aircraftList.SelectionChanged += delegate { AircraftChanged(); TrackVehicleSelection(); };
            injectionToggle.Checked += delegate { RefreshWeapons(); };
            injectionToggle.Unchecked += delegate { RefreshWeapons(); };
            weaponSearch.TextChanged += delegate { weaponSearchTimer.Stop(); weaponSearchTimer.Start(); };
            categoryFilter.SelectionChanged += delegate { RefreshWeapons(); };
            weaponNationFilter.SelectionChanged += delegate { RefreshWeapons(); };
            sortFilter.SelectionChanged += delegate { RefreshWeapons(); };
            weaponList.MouseDoubleClick += delegate { MountWeapon(); };
            weaponTableClipContent.SizeChanged += delegate { UpdateWeaponTableClip(); };
            weaponTableClipContent.Loaded += delegate { UpdateWeaponTableClip(); };
            weaponList.SizeChanged += delegate { UpdateWeaponColumns(); };
            weaponList.Loaded += delegate { UpdateWeaponColumns(); };
            Find<Button>("MountButton").Click += delegate { MountWeapon(); };
            Find<Button>("ClearStationButton").Click += delegate { ClearStation(); };
            Find<Button>("ClearAllButton").Click += delegate { controller.WorkspaceClearAll(); RefreshPylons(); };
            Find<Button>("SystemsButton").Click += delegate { ShowFlightSystems(); };
            Find<Button>("FlightConfigureButton").Click += delegate { ShowFlightConfigure(); };
            Find<Button>("MapButton").Click += delegate { ShowMap(); };
            Find<Button>("MissionOptionsButton").Click += delegate { ShowMissionOptions(); };
            Find<Button>("GenerateButton").Click += delegate { GenerateMission(); };
            tabVehicleButton = Find<ToggleButton>("TabVehicleButton");
            tabTargetsButton = Find<ToggleButton>("TabTargetsButton");
            tabOptionsButton = Find<ToggleButton>("TabOptionsButton");
            tabGarageButton = Find<ToggleButton>("TabGarageButton");
            tabExperimentalButton = Find<ToggleButton>("TabExperimentalButton");
            tabVehicleContent = Find<Grid>("TabVehicleContent");
            tabTargetsContent = Find<Grid>("TabTargetsContent");
            tabOptionsContent = Find<Grid>("TabOptionsContent");
            tabGarageContent = Find<Grid>("TabGarageContent");
            tabExperimentalContent = Find<Grid>("TabExperimentalContent");
            BuildOptionsTab();
            BuildGarageTab();
            WireTabs();
            ShowWorkspaceTab(0);
        }

        private void WireTabs()
        {
            tabVehicleButton.Click += delegate { ShowWorkspaceTab(0); };
            tabTargetsButton.Click += delegate { ShowWorkspaceTab(1); };
            tabOptionsButton.Click += delegate { ShowWorkspaceTab(2); };
            tabGarageButton.Click += delegate { ShowWorkspaceTab(3); };
            tabExperimentalButton.Click += delegate { ShowWorkspaceTab(4); };
        }

        private void ShowLanguageBusy(ToggleButton zh, ToggleButton en, bool chinese)
        {
            zh.IsEnabled = false; en.IsEnabled = false;
            zh.Content = chinese ? "切换中…" : zh.Content;
            en.Content = !chinese ? "切换中…" : en.Content;
            Dispatcher.BeginInvoke(new Action(delegate { SwitchInterfaceLanguage(chinese); }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private static void SwitchInterfaceLanguage(bool chinese)
        {
            ModernText.Chinese = chinese;
            try { ConfigStore.SetString("language", chinese ? "zh" : "en"); } catch { }
            if (Application.Current == null) return;
            ModernMainWindow previous = Application.Current.MainWindow as ModernMainWindow;
            ModernMainWindow next = new ModernMainWindow();
            next.Show();
            if (previous != null) { Application.Current.MainWindow = next; previous.Close(); }
        }

        private void BuildOptionsTab()
        {
            if (tabOptionsContent == null) return;
            tabOptionsContent.Children.Clear();
            tabOptionsContent.IsHitTestVisible = true;
            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition());
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            StackPanel header = new StackPanel { Margin = new Thickness(16, 12, 16, 4) };
            header.Children.Add(new TextBlock { Text = ModernText.L("OPTIONS — MISSION SETTINGS", "选项 — 任务设置"), Foreground = ModernPalette.Brush(ModernPalette.Text), FontSize = 18, FontWeight = FontWeights.SemiBold });
            header.Children.Add(new TextBlock { Text = ModernText.L("Global — applies to every generated mission", "全局设置 — 应用于所有生成的任务"), Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 4, 0, 0) });
        StackPanel langRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
        langRow.Children.Add(new TextBlock { Text = ModernText.L("Interface language:", "界面语言: "), Foreground = ModernPalette.Brush(ModernPalette.Text), VerticalAlignment = VerticalAlignment.Center });
        ToggleButton langZh = new ToggleButton { Content = "中文", Style = (Style)Resources["ToggleStyle"], IsChecked = ModernText.Chinese, Padding = new Thickness(10, 1, 10, 1), Margin = new Thickness(8, 0, 0, 0) };
        ToggleButton langEn = new ToggleButton { Content = "English", Style = (Style)Resources["ToggleStyle"], IsChecked = !ModernText.Chinese, Padding = new Thickness(10, 1, 10, 1), Margin = new Thickness(6, 0, 0, 0) };
        langZh.Click += delegate { if (!ModernText.Chinese) ShowLanguageBusy(langZh, langEn, true); };
        langEn.Click += delegate { if (ModernText.Chinese) ShowLanguageBusy(langZh, langEn, false); };
        langRow.Children.Add(langZh); langRow.Children.Add(langEn);
        header.Children.Add(langRow);
            layout.Children.Add(header);
            MissionOptionsPanel panel = new MissionOptionsPanel(MissionSettings.Current);
            ScrollViewer scroll = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(16, 0, 16, 8), Padding = new Thickness(0, 0, 8, 20) };
            Grid.SetRow(scroll, 1);
            layout.Children.Add(scroll);
            Button apply = new Button { Content = ModernText.L("APPLY OPTIONS", "应用选项"), Height = 34, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(16, 0, 16, 14), Padding = new Thickness(24, 2, 24, 2) };
            apply.Style = (Style)Resources["ButtonStyle"];
            apply.Click += delegate
            {
                MissionSettings updated = panel.Apply();
                MissionSettings.Current = updated;
                updated.Save();
                if (status != null) status.Text = ModernText.L("●  MISSION OPTIONS APPLIED", "● 任务选项已应用");
            };
            Grid.SetRow(apply, 2);
            layout.Children.Add(apply);
            tabOptionsContent.Children.Add(layout);
        }

        private void BuildTargetsTab()
        {
            if (tabTargetsContent == null) return;
            tabTargetsContent.Children.Clear();
            tabTargetsContent.IsHitTestVisible = true;
            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition());
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            StackPanel header = new StackPanel { Margin = new Thickness(16, 12, 16, 4) };
            header.Children.Add(new TextBlock { Text = ModernText.L("TARGETS — GROUND / AIR / NAVAL", "目标 — 地面 / 空中 / 海上"), Foreground = ModernPalette.Brush(ModernPalette.Text), FontSize = 18, FontWeight = FontWeights.SemiBold });
            header.Children.Add(new TextBlock { Text = ModernText.L("Seven range positions, four flying targets, one naval target and the optional combined-battles spawn.", "七个距离位置、四个空中目标、一个海上目标，以及可选的联合战役出生点。"), Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 4, 0, 0) });
            layout.Children.Add(header);
            targetsPanel = new MapPanel(BuildMapPanelState(), (Style)Resources["StatusToggleStyle"]);
            ScrollViewer scroll = new ScrollViewer { Content = targetsPanel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(16, 0, 16, 8), Padding = new Thickness(0, 0, 8, 20) };
            Grid.SetRow(scroll, 1);
            layout.Children.Add(scroll);
            Button apply = new Button { Content = ModernText.L("APPLY TARGETS", "应用目标"), Height = 34, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(16, 0, 16, 14), Padding = new Thickness(24, 2, 24, 2) };
            apply.Style = (Style)Resources["ButtonStyle"];
            apply.Click += delegate { ApplyTargetsPanel(); };
            Grid.SetRow(apply, 2);
            layout.Children.Add(apply);
            tabTargetsContent.Children.Add(layout);
        }

        private MapPanelState BuildMapPanelState()
        {
            MapPanelState state = new MapPanelState();
            state.Aircraft = aircraftViews ?? new List<AircraftView>();
            IEnumerable<TargetView> groundSource = groundTarget == null ? null : groundTarget.ItemsSource as IEnumerable<TargetView>;
            state.Ground = groundSource == null ? new List<TargetView>() : groundSource.OrderBy(x => x.Name).ToList();
            IEnumerable<TargetView> shipSource = shipTarget == null ? null : shipTarget.ItemsSource as IEnumerable<TargetView>;
            state.Ships = shipSource == null ? new List<TargetView>() : shipSource.OrderBy(x => x.Name).ToList();
            state.CombinedMaps = controller.WorkspaceCombinedMaps.ToList();
            state.PlayerKind = GroundSelected ? "ground" : MainForm.IsHelicopter(selectedAircraft, null) ? "helicopter" : "aircraft";
            state.CurrentAir = airTarget == null ? null : airTarget.SelectedItem as AircraftView;
            state.CurrentAirCount = SelectedCount(airCount);
            state.CurrentAir01 = airTarget01;
            state.CurrentAir01Count = airTarget01Count;
            state.CurrentHeli01 = heliTarget01;
            state.CurrentHeli01Count = heliTarget01Count;
            state.CurrentHeli02 = heliTarget02;
            state.CurrentHeli02Count = heliTarget02Count;
            state.CurrentGround = configuredGroundTargets;
            state.Hostile = hostileToggle == null ? false : hostileToggle.IsChecked == true;
            state.SamSites = samSitesToggle == null ? true : samSitesToggle.IsChecked == true;
            state.SamSitesMode = samSitesMode == null ? "active" : samSitesMode.Text;
            state.SamSitesSelection = samSitesSelection == null ? "s300" : samSitesSelection.Text;
            state.CurrentShip = shipTarget == null ? null : shipTarget.SelectedItem as TargetView;
            state.CurrentShipCount = SelectedCount(shipCount);
            state.PassiveShip = passiveShip;
            state.Scenario = combinedScenario;
            return state;
        }

        private void ApplyTargetsPanel()
        {
            if (targetsPanel == null) return;
            MapPanelResult r = targetsPanel.Collect();
            if (r == null)
            {
                if (status != null) status.Text = ModernText.L("● SELECT A MAP, SIDE AND COMPATIBLE SPAWN FOR THE COMBINED SCENARIO", "● 为联合场景选择地图、阵营与兼容出生点");
                return;
            }
            airTarget.SelectedItem = r.AirTarget;
            airCount.SelectedItem = r.AirCount;
            airTarget01 = r.AirTarget01;
            airTarget01Count = r.AirCount01;
            heliTarget01 = r.HeliTarget01;
            heliTarget01Count = r.HeliCount01;
            heliTarget02 = r.HeliTarget02;
            heliTarget02Count = r.HeliCount02;
            configuredGroundTargets.Clear();
            configuredGroundTargets.AddRange(r.GroundTargets);
            if (configuredGroundTargets.Count > 0) groundTarget.SelectedItem = configuredGroundTargets[0];
            groundCount.SelectedItem = configuredGroundTargets.Count > 0 ? 1 : 0;
            hostileToggle.IsChecked = r.Hostile;
            samSitesToggle.IsChecked = r.SamSitesMode != "disabled";
            samSitesMode.Text = r.SamSitesMode;
            samSitesSelection.Text = r.SamSitesSelection;
            shipTarget.SelectedItem = r.ShipTarget;
            shipCount.SelectedItem = r.ShipCount;
            passiveShip = r.PassiveShip;
            combinedScenario = r.Scenario == null ? new CombinedScenarioSettings() : r.Scenario.Copy();
            UpdateConfigurationSummary();
            if (status != null) status.Text = ModernText.L("● TARGETS APPLIED — READY TO GENERATE", "● 目标已应用 — 可生成任务");
        }

        private void BuildExperimentalTab()
        {
            if (tabExperimentalContent == null || experimentalBuilt) return;
            experimentalBuilt = true;
            tabExperimentalContent.Children.Clear();
            tabExperimentalContent.IsHitTestVisible = true;
            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition());
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            StackPanel header = new StackPanel { Margin = new Thickness(16, 12, 16, 4) };
            header.Children.Add(new TextBlock { Text = ModernText.L("EXPERIMENTAL — GROUND / FLIGHT CONFIGURE", "实验 — 地面 / 飞行配置"), Foreground = ModernPalette.Brush(ModernPalette.Text), FontSize = 18, FontWeight = FontWeights.SemiBold });
            header.Children.Add(new TextBlock { Text = ModernText.L("Cross-domain cannon injection, ammunition slots, projectile & mobility tuning (ground), or fuel, belts and countermeasures (flight).", "跨域换炮注入、弹药槽、弹道与机动性调校（地面）；燃油、弹带与干扰弹（飞行）。"), Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 4, 0, 0) });
            layout.Children.Add(header);
            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(16, 0, 16, 8), Padding = new Thickness(0, 0, 8, 20) };
            Grid.SetRow(scroll, 1);
            layout.Children.Add(scroll);
            if (selectedAircraft == null)
            {
                scroll.Content = new TextBlock { Text = ModernText.L("Select a vehicle first.", "请先选择载具。"), Foreground = ModernPalette.Brush(ModernPalette.Muted), Margin = new Thickness(8, 8, 0, 0) };
            }
            else if (GroundSelected)
            {
                GroundConfigurePanel panel = new GroundConfigurePanel(selectedAircraft, controller.WorkspaceGetSettings(selectedAircraft), controller.WorkspaceGroundAmmo, controller.WorkspaceGroundTargets, controller.WorkspaceUnitWeapons, controller.WorkspaceGroundWeapons(selectedAircraft), new GroundAmmo[0], controller.WorkspaceGunBeltOptions(selectedAircraft), controller.WorkspaceResolveCannonAmmo, (Style)Resources["ButtonStyle"], (Style)Resources["ToggleStyle"], false, gameFolder.Text);
                experimentalPanel = panel;
                scroll.Content = panel;
            }
            else
            {
                FlightConfigurePanel panel = new FlightConfigurePanel(selectedAircraft, controller.WorkspaceGetSettings(selectedAircraft), controller.WorkspaceCountermeasureLaunchers(selectedAircraft), controller.WorkspaceModifications.Where(x => x.AircraftId.Equals(selectedAircraft.Id, StringComparison.OrdinalIgnoreCase)));
                experimentalPanel = panel;
                scroll.Content = panel;
            }
            Grid buttons = new Grid { Margin = new Thickness(16, 0, 16, 14) };
            buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            buttons.ColumnDefinitions.Add(new ColumnDefinition());
            buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Button refresh = new Button { Content = ModernText.L("REFRESH FOR CURRENT VEHICLE", "刷新当前载具"), Height = 34, Padding = new Thickness(16, 2, 16, 2) };
            refresh.Style = (Style)Resources["ButtonStyle"];
            refresh.Click += delegate { experimentalBuilt = false; BuildExperimentalTab(); };
            buttons.Children.Add(refresh);
            Button apply = new Button { Content = ModernText.L("APPLY CONFIGURATION", "应用配置"), Height = 34, Padding = new Thickness(24, 2, 24, 2), Margin = new Thickness(10, 0, 0, 0) };
            apply.Style = (Style)Resources["ButtonStyle"];
            apply.Click += delegate { ApplyExperimentalPanel(); };
            Grid.SetColumn(apply, 2);
            buttons.Children.Add(apply);
            Grid.SetRow(buttons, 2);
            layout.Children.Add(buttons);
            tabExperimentalContent.Children.Add(layout);
        }

        private void ApplyExperimentalPanel()
        {
            if (experimentalPanel == null || selectedAircraft == null) return;
            if (experimentalPanel is GroundConfigurePanel)
            {
                AircraftSettings r = ((GroundConfigurePanel)experimentalPanel).Collect();
                controller.WorkspaceSetSettings(selectedAircraft, r);
                MissionSettings.Current.InjectedCannonBlk = r.InjectedCannonBlk;
                MissionSettings.Current.InjectedCannonDomain = r.InjectedCannonDomain;
                MissionSettings.Current.InjectedCannonUnit = r.InjectedCannonUnit;
                MissionSettings.Current.FakeArhConversion = r.FakeArhConversion;
                MissionSettings.Current.Save();
                SetStatus("GROUND CONFIGURATION UPDATED — " + selectedAircraft.Display, false);
            }
            else if (experimentalPanel is FlightConfigurePanel)
            {
                AircraftSettings r = ((FlightConfigurePanel)experimentalPanel).Collect();
                controller.WorkspaceSetSettings(selectedAircraft, r);
                MissionSettings.Current.InjectedCannonBlk = r.InjectedCannonBlk;
                MissionSettings.Current.InjectedCannonDomain = r.InjectedCannonDomain;
                MissionSettings.Current.InjectedCannonUnit = r.InjectedCannonUnit;
                MissionSettings.Current.Save();
                SetStatus("FLIGHT CONFIGURATION UPDATED — " + selectedAircraft.Display, false);
            }
            UpdateConfigurationSummary();
        }

        private sealed class GarageEntry
        {
            public string Id;
            public string Kind;
            public string Display;
            public GarageEntry(string id, string kind, string display) { Id = id; Kind = kind; Display = display; }
            public override string ToString() { return Display; }
        }

        private sealed class PresetEntry
        {
            public AmmoPreset Preset;
            public PresetEntry(AmmoPreset preset) { Preset = preset; }
            public override string ToString() { return Preset.Name + "  •  " + Preset.VehicleId; }
        }

        private void BuildGarageTab()
        {
            if (tabGarageContent == null) return;
            tabGarageContent.Children.Clear();
            tabGarageContent.IsHitTestVisible = true;
            Grid root = new Grid { Margin = new Thickness(16, 12, 16, 14) };
            root.ColumnDefinitions.Add(new ColumnDefinition());
            root.ColumnDefinitions.Add(new ColumnDefinition());
            root.ColumnDefinitions.Add(new ColumnDefinition());
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition());
            StackPanel header = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            header.Children.Add(new TextBlock { Text = ModernText.L("GARAGE — COLLECTION & PRESETS", "机库 — 收藏与预设"), Foreground = ModernPalette.Brush(ModernPalette.Text), FontSize = 18, FontWeight = FontWeights.SemiBold });
            header.Children.Add(new TextBlock { Text = ModernText.L("Recently used vehicles, favourites and saved ammunition loadouts (Ask3lad style).", "最近使用的载具、收藏与已保存的弹药配置（Ask3lad 风格）。"), Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 4, 0, 0) });
            root.Children.Add(header);
            StackPanel col0 = GarageColumn("RECENTLY USED");
            garageRecentlyBox = GarageListBox();
            garageRecentlyBox.SelectionChanged += delegate { SelectGarageVehicle(garageRecentlyBox.SelectedItem as GarageEntry); };
            col0.Children.Add(garageRecentlyBox);
            col0.Children.Add(new TextBlock { Text = ModernText.L("Click a vehicle to jump to it.", "点击载具即可跳转。"), Foreground = ModernPalette.Brush(ModernPalette.Muted), FontSize = 11, Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap });
            StackPanel col1 = GarageColumn("FAVOURITES");
            garageFavBox = GarageListBox();
            garageFavBox.SelectionChanged += delegate { SelectGarageVehicle(garageFavBox.SelectedItem as GarageEntry); };
            col1.Children.Add(garageFavBox);
            Button addFav = new Button { Content = ModernText.L("★  ADD CURRENT TO FAVOURITES", "★ 将当前加入收藏"), Style = (Style)Resources["ButtonStyle"], Height = 30, Margin = new Thickness(0, 8, 0, 0) };
            addFav.Click += delegate { ToggleFavourite(); };
            Button removeFav = new Button { Content = ModernText.L("REMOVE SELECTED", "移除所选"), Style = (Style)Resources["ButtonStyle"], Height = 30, Margin = new Thickness(0, 6, 0, 0) };
            removeFav.Click += delegate { RemoveFavourite(); };
            col1.Children.Add(addFav);
            col1.Children.Add(removeFav);
            StackPanel col2 = GarageColumn("AMMO PRESETS");
            garagePresetBox = GarageListBox();
            col2.Children.Add(garagePresetBox);
            Button savePreset = new Button { Content = ModernText.L("SAVE CURRENT AS PRESET…", "保存当前为预设…"), Style = (Style)Resources["ButtonStyle"], Height = 30, Margin = new Thickness(0, 8, 0, 0) };
            savePreset.Click += delegate { SaveGaragePreset(); };
            Button loadPreset = new Button { Content = ModernText.L("LOAD SELECTED", "加载所选"), Style = (Style)Resources["ButtonStyle"], Height = 30, Margin = new Thickness(0, 6, 0, 0) };
            loadPreset.Click += delegate { LoadGaragePreset(); };
            Button deletePreset = new Button { Content = ModernText.L("DELETE SELECTED", "删除所选"), Style = (Style)Resources["ButtonStyle"], Height = 30, Margin = new Thickness(0, 6, 0, 0) };
            deletePreset.Click += delegate { DeleteGaragePreset(); };
            col2.Children.Add(savePreset);
            col2.Children.Add(loadPreset);
            col2.Children.Add(deletePreset);
            Grid.SetColumn(col0, 0);
            Grid.SetColumn(col1, 1);
            Grid.SetColumn(col2, 2);
            Grid.SetRow(col0, 1);
            Grid.SetRow(col1, 1);
            Grid.SetRow(col2, 1);
            root.Children.Add(col0);
            root.Children.Add(col1);
            root.Children.Add(col2);
            tabGarageContent.Children.Add(root);
        }

        private void RefreshGarageLists()
        {
            RefreshGarageRecentlyUsed();
            RefreshGarageFavourites();
            RefreshGaragePresets();
        }

        private StackPanel GarageColumn(string title)
        {
            StackPanel sp = new StackPanel { Margin = new Thickness(12, 10, 12, 0) };
            sp.Children.Add(new TextBlock { Text = title, Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            return sp;
        }

        private ListBox GarageListBox()
        {
            return new ListBox
            {
                Height = 250,
                Background = ModernPalette.Brush("#FF16283E"),
                Foreground = ModernPalette.Brush(ModernPalette.Text),
                BorderBrush = ModernPalette.Brush(ModernPalette.Border),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Top
            };
        }

        private static List<string> GarageList(Dictionary<string, object> garage, string key)
        {
            List<string> result = new List<string>();
            if (garage == null) return result;
            object v;
            if (!garage.TryGetValue(key, out v) || v == null) return result;
            if (v is List<object>) { foreach (object x in (List<object>)v) result.Add(Convert.ToString(x, CultureInfo.InvariantCulture)); }
            else if (v is object[]) { foreach (object x in (object[])v) result.Add(Convert.ToString(x, CultureInfo.InvariantCulture)); }
            else if (v is System.Collections.ArrayList) { foreach (object x in (System.Collections.ArrayList)v) result.Add(Convert.ToString(x, CultureInfo.InvariantCulture)); }
            return result;
        }

        private static void GarageSet(Dictionary<string, object> garage, string key, List<string> values)
        {
            List<object> list = new List<object>();
            foreach (string s in values) list.Add(s);
            garage[key] = list;
        }

        private void TrackVehicleSelection()
        {
            if (suppressGarageTrack) return;
            AircraftView view = aircraftList.SelectedItem as AircraftView;
            if (view == null || view.Source == null) return;
            string id = view.Source.Id;
            Dictionary<string, object> garage = ConfigStore.GetObject("garage") ?? new Dictionary<string, object>();
            List<string> recent = GarageList(garage, "recently_used");
            recent.RemoveAll(x => x.Equals(id, StringComparison.OrdinalIgnoreCase));
            recent.Insert(0, id);
            if (recent.Count > 20) recent = recent.Take(20).ToList();
            GarageSet(garage, "recently_used", recent);
            ConfigStore.SetObject("garage", garage);
            ConfigStore.Save();
            RefreshGarageRecentlyUsed();
        }

        private void ToggleFavourite()
        {
            AircraftView view = aircraftList.SelectedItem as AircraftView;
            if (view == null || view.Source == null) return;
            string id = view.Source.Id;
            Dictionary<string, object> garage = ConfigStore.GetObject("garage") ?? new Dictionary<string, object>();
            List<string> fav = GarageList(garage, "favourites");
            bool exists = fav.Any(x => x.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (exists) fav.RemoveAll(x => x.Equals(id, StringComparison.OrdinalIgnoreCase));
            else fav.Insert(0, id);
            GarageSet(garage, "favourites", fav);
            ConfigStore.SetObject("garage", garage);
            ConfigStore.Save();
            RefreshGarageFavourites();
        }

        private void RemoveFavourite()
        {
            GarageEntry entry = garageFavBox == null ? null : garageFavBox.SelectedItem as GarageEntry;
            if (entry == null) return;
            Dictionary<string, object> garage = ConfigStore.GetObject("garage") ?? new Dictionary<string, object>();
            List<string> fav = GarageList(garage, "favourites");
            fav.RemoveAll(x => x.Equals(entry.Id, StringComparison.OrdinalIgnoreCase));
            GarageSet(garage, "favourites", fav);
            ConfigStore.SetObject("garage", garage);
            ConfigStore.Save();
            RefreshGarageFavourites();
        }

        private void SelectGarageVehicle(GarageEntry entry)
        {
            if (entry == null) return;
            SelectVehicleById(entry.Id, entry.Kind);
        }

        private void SelectVehicleById(string id, string kind)
        {
            if (String.IsNullOrWhiteSpace(id)) return;
            if (!String.IsNullOrWhiteSpace(kind))
            {
                int kindIndex = typeFilter.Items.OfType<string>().ToList().FindIndex(x => x.Equals(kind, StringComparison.OrdinalIgnoreCase));
                if (kindIndex > 0) typeFilter.SelectedIndex = kindIndex;
            }
            suppressGarageTrack = true;
            try
            {
                AircraftView view = FindVehicleView(id);
                if (view == null)
                {
                    aircraftSearch.Text = "";
                    if (nationFilter.SelectedIndex > 0) nationFilter.SelectedIndex = 0;
                    if (rankFilter.SelectedIndex > 0) rankFilter.SelectedIndex = 0;
                    if (typeFilter.SelectedIndex > 0) typeFilter.SelectedIndex = 0;
                    view = FindVehicleView(id);
                }
                if (view != null) aircraftList.SelectedItem = view;
            }
            finally { suppressGarageTrack = false; }
        }

        private AircraftView FindVehicleView(string id)
        {
            System.Collections.IEnumerable items = aircraftList.ItemsSource as System.Collections.IEnumerable;
            if (items == null) return null;
            foreach (object o in items)
            {
                AircraftView v = o as AircraftView;
                if (v != null && v.Source != null && v.Source.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) return v;
            }
            return null;
        }

        private string GarageDisplay(string id)
        {
            AircraftView a = aircraftViews.FirstOrDefault(x => x.Source != null && x.Source.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (a != null) return a.Name;
            IEnumerable<TargetView> ground = groundTarget == null ? null : groundTarget.ItemsSource as IEnumerable<TargetView>;
            if (ground != null)
            {
                TargetView g = ground.FirstOrDefault(x => x.Source != null && x.Source.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
                if (g != null) return g.Name;
            }
            IEnumerable<TargetView> ships = shipTarget == null ? null : shipTarget.ItemsSource as IEnumerable<TargetView>;
            if (ships != null)
            {
                TargetView s = ships.FirstOrDefault(x => x.Source != null && x.Source.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
                if (s != null) return s.Name;
            }
            return id;
        }

        private string GarageKind(string id)
        {
            AircraftView a = aircraftViews.FirstOrDefault(x => x.Source != null && x.Source.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (a != null) return a.Source.Kind;
            IEnumerable<TargetView> ground = groundTarget == null ? null : groundTarget.ItemsSource as IEnumerable<TargetView>;
            if (ground != null && ground.Any(x => x.Source != null && x.Source.Id.Equals(id, StringComparison.OrdinalIgnoreCase))) return "Ground Vehicle";
            IEnumerable<TargetView> ships = shipTarget == null ? null : shipTarget.ItemsSource as IEnumerable<TargetView>;
            if (ships != null && ships.Any(x => x.Source != null && x.Source.Id.Equals(id, StringComparison.OrdinalIgnoreCase))) return "Ships";
            return "";
        }

        private void RefreshGarageRecentlyUsed()
        {
            if (garageRecentlyBox == null) return;
            Dictionary<string, object> garage = ConfigStore.GetObject("garage");
            List<GarageEntry> items = new List<GarageEntry>();
            foreach (string id in GarageList(garage, "recently_used"))
                items.Add(new GarageEntry(id, GarageKind(id), GarageDisplay(id)));
            garageRecentlyBox.ItemsSource = items;
        }

        private void RefreshGarageFavourites()
        {
            if (garageFavBox == null) return;
            Dictionary<string, object> garage = ConfigStore.GetObject("garage");
            List<GarageEntry> items = new List<GarageEntry>();
            foreach (string id in GarageList(garage, "favourites"))
                items.Add(new GarageEntry(id, GarageKind(id), GarageDisplay(id)));
            garageFavBox.ItemsSource = items;
        }

        private void RefreshGaragePresets()
        {
            if (garagePresetBox == null) return;
            List<AmmoPreset> presets = ModernShellStorage.LoadAmmoPresets();
            List<PresetEntry> items = new List<PresetEntry>();
            foreach (AmmoPreset p in presets) items.Add(new PresetEntry(p));
            garagePresetBox.ItemsSource = items;
        }

        private void SaveGaragePreset()
        {
            try
            {
                if (selectedAircraft == null) return;
                string suggested = selectedAircraft.Display + " - " + DateTime.Now.ToString("MMdd-HHmm", CultureInfo.InvariantCulture);
                ModernInputWindow input = new ModernInputWindow("SAVE AMMO PRESET", "Name this ammunition preset. Stored per vehicle in config.json (ammo_loadouts).", suggested) { Owner = Owner };
                if (input.ShowDialog() != true) return;
                string name = input.Value == null ? null : input.Value.Trim();
                if (String.IsNullOrWhiteSpace(name)) return;
                AmmoPreset preset = new AmmoPreset { Name = name, VehicleId = selectedAircraft.Id };
                AircraftSettings settings = controller.WorkspaceGetSettings(selectedAircraft);
                if (settings != null && settings.GroundAmmoLoadouts != null)
                {
                    foreach (GroundAmmoLoadout lo in settings.GroundAmmoLoadouts)
                    {
                        if (lo == null || lo.Slot < 0 || lo.Slot > 3) continue;
                        preset.Slots[lo.Slot] = new GroundAmmoLoadout { Slot = lo.Slot, Count = lo.Count, SourceBlk = lo.SourceBlk, BulletName = lo.BulletName, AmmoGroup = lo.AmmoGroup, Kind = lo.Kind };
                    }
                }
                ModernShellStorage.SaveAmmoPreset(preset);
                RefreshGaragePresets();
                if (ammoPresetBox != null)
                {
                    GroundRefreshAmmoPresets();
                    ammoPresetBox.SelectedItem = name;
                }
            }
            catch { }
        }

        private void LoadGaragePreset()
        {
            try
            {
                PresetEntry entry = garagePresetBox == null ? null : garagePresetBox.SelectedItem as PresetEntry;
                if (entry == null || entry.Preset == null) return;
                AmmoPreset preset = entry.Preset;
                if (selectedAircraft == null || !selectedAircraft.Id.Equals(preset.VehicleId, StringComparison.OrdinalIgnoreCase))
                    SelectVehicleById(preset.VehicleId, "");
                AircraftSettings settings = controller.WorkspaceGetSettings(selectedAircraft);
                settings.GroundAmmoLoadouts.Clear();
                for (int i = 0; i < 4; i++)
                {
                    GroundAmmoLoadout slot = preset.Slots == null || i >= preset.Slots.Length ? null : preset.Slots[i];
                    if (slot == null) continue;
                    settings.GroundAmmoLoadouts.Add(new GroundAmmoLoadout { Slot = slot.Slot, Count = slot.Count, SourceBlk = slot.SourceBlk, BulletName = slot.BulletName, AmmoGroup = slot.AmmoGroup, Kind = slot.Kind });
                }
                controller.WorkspaceSetSettings(selectedAircraft, settings);
                GroundRefreshAmmo();
                GroundLoadSlots(settings);
                ShowWorkspaceTab(0);
            }
            catch { }
        }

        private void DeleteGaragePreset()
        {
            try
            {
                PresetEntry entry = garagePresetBox == null ? null : garagePresetBox.SelectedItem as PresetEntry;
                if (entry == null || entry.Preset == null) return;
                ModernShellStorage.DeleteAmmoPreset(entry.Preset.Name, entry.Preset.VehicleId);
                RefreshGaragePresets();
                if (ammoPresetBox != null) GroundRefreshAmmoPresets();
            }
            catch { }
        }

        internal void SelectWorkspaceTabForScreenshot(int index) { ShowWorkspaceTab(index); }

        private void ShowWorkspaceTab(int index)
        {
            if (index == 4 && !experimentalBuilt) BuildExperimentalTab();
            tabVehicleButton.IsChecked = index == 0;
            tabTargetsButton.IsChecked = index == 1;
            tabOptionsButton.IsChecked = index == 2;
            tabGarageButton.IsChecked = index == 3;
            tabExperimentalButton.IsChecked = index == 4;
            tabVehicleContent.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
            tabTargetsContent.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
            tabOptionsContent.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
            tabGarageContent.Visibility = index == 3 ? Visibility.Visible : Visibility.Collapsed;
            tabExperimentalContent.Visibility = index == 4 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FitToWorkingArea()
        {
            Rect work = SystemParameters.WorkArea;
            const double edge = 12;
            if (work.Width < MinWidth + edge * 2) MinWidth = Math.Max(960, work.Width - edge * 2);
            if (work.Height < MinHeight + edge * 2) MinHeight = Math.Max(560, work.Height - edge * 2);
            Width = Math.Min(1500, Math.Max(MinWidth, work.Width - edge * 2));
            Height = Math.Min(1100, Math.Max(MinHeight, work.Height - edge * 2));
            Left = work.Left + Math.Max(edge, (work.Width - Width) / 2);
            Top = work.Top + Math.Max(edge, (work.Height - Height) / 2);
        }

        private void UpdatePreviewClip()
        {
            double width = Math.Max(0, previewClipContent.ActualWidth);
            double height = Math.Max(0, previewClipContent.ActualHeight);
            previewClipContent.Clip = new RectangleGeometry(new Rect(0, 0, width, height), 14, 14);
        }

        private void UpdateWeaponTableClip()
        {
            double width = Math.Max(0, weaponTableClipContent.ActualWidth);
            double height = Math.Max(0, weaponTableClipContent.ActualHeight);
            weaponTableClipContent.Clip = new RectangleGeometry(new Rect(0, 0, width, height), 12, 12);
        }

        private void PopulateControls()
        {
            gameFolder.Text = controller.WorkspaceGameFolder;
            aircraftViews = controller.WorkspaceAircraft.Select(x => new AircraftView(x)).ToList();
            nationFilter.Items.Add(ModernText.L("All Nations", "全部国家"));
            foreach (string value in controller.WorkspaceNations) nationFilter.Items.Add(value);
            rankFilter.Items.Add(ModernText.L("Any Rank", "任意等级"));
            for (int i = 1; i <= Math.Max(9, controller.WorkspaceAircraft.Max(x => x.Rank)); i++) rankFilter.Items.Add(ModernText.L("Rank ", "等级 ") + AircraftViewRoman(i));
            typeFilter.Items.Add(ModernText.L("All Types", "全部类型"));
            foreach (string value in controller.WorkspaceAircraft.Select(x => x.Kind).Distinct().OrderBy(x => x)) typeFilter.Items.Add(value);
            categoryFilter.Items.Add(ModernText.L("All Weapon Types", "全部武器类型"));
            foreach (string value in controller.WorkspaceWeaponCategories) categoryFilter.Items.Add(value);
            weaponNationFilter.Items.Add(ModernText.L("All Nations", "全部国家"));
            foreach (string value in controller.WorkspaceNations) weaponNationFilter.Items.Add(value);
            sortFilter.Items.Add(ModernText.L("Mass: low to high", "质量: 低到高"));
            sortFilter.Items.Add(ModernText.L("Mass: high to low", "质量: 高到低"));
            sortFilter.Items.Add(ModernText.L("Name: A to Z", "名称: A到Z"));
            nationFilter.SelectedIndex = rankFilter.SelectedIndex = typeFilter.SelectedIndex = categoryFilter.SelectedIndex = weaponNationFilter.SelectedIndex = sortFilter.SelectedIndex = 0;

            List<AircraftView> targets = aircraftViews.OrderBy(x => x.Name).ToList();
            airTarget.ItemsSource = targets;
            // Template flying hostiles: Typhoon (Target_Air_01), Mi-28NM (Heli_Target),
            // Ka-52 (Heli_Target_02). The Map window can restyle all of them; keep the
            // stock vehicles as defaults so fresh sessions match the template.
            airTarget01 = aircraftViews.FirstOrDefault(x => x.Source.Id == "ef_2000_typhoon_aesa") ?? aircraftViews.FirstOrDefault();
            heliTarget01 = aircraftViews.FirstOrDefault(x => x.Source.Id == "mi_28nm") ?? aircraftViews.FirstOrDefault();
            heliTarget02 = aircraftViews.FirstOrDefault(x => x.Source.Id == "ka_52") ?? aircraftViews.FirstOrDefault();
            groundTarget.ItemsSource = controller.WorkspaceGroundTargets.Select(x => new TargetView(x)).OrderBy(x => x.Name).ToList();
            shipTarget.ItemsSource = controller.WorkspaceShipTargets.Select(x => new TargetView(x)).OrderBy(x => x.Name).ToList();
            List<int> counts = Enumerable.Range(0, 21).ToList();
            airCount.ItemsSource = counts;
            groundCount.ItemsSource = counts;
            shipCount.ItemsSource = counts;
            airCount.SelectedItem = groundCount.SelectedItem = shipCount.SelectedItem = 1;
            SelectAircraftTarget("j_10c");
            SelectGroundTarget("ussr_bmpt");
            SelectShipTarget("jp_battleship_yamato");
            string[] defaultGround = { "ussr_t_34_1941_57", "us_m4_sherman_calliope", "ussr_bmpt", "us_m1a2_sep2_abrams", "ussr_t_90m_arena_m", "us_adats_bradley", "us_m901_itv" };
            IEnumerable<TargetView> availableGround = (groundTarget.ItemsSource as IEnumerable<TargetView>) ?? Enumerable.Empty<TargetView>();
            foreach (string id in defaultGround)
            {
                TargetView value = availableGround.FirstOrDefault(x => x.Source.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
                if (value != null) configuredGroundTargets.Add(value);
            }
            UpdateConfigurationSummary();
            FilterAircraft();
            suppressGarageTrack = true;
            try { SessionRestore(); }
            finally { suppressGarageTrack = false; }
            if (aircraftList.SelectedItem == null)
            {
                suppressGarageTrack = true;
                try
                {
                    AircraftView initial = aircraftViews.FirstOrDefault(x => x.Source.Id == "ef_2000_typhoon_aesa") ?? aircraftViews.FirstOrDefault();
                    if (initial != null) aircraftList.SelectedItem = initial;
                }
                finally { suppressGarageTrack = false; }
            }
            RefreshGarageLists();
            BuildTargetsTab();
        }

        internal string SessionSelectedVehicleId
        {
            get
            {
                AircraftView view = aircraftList == null ? null : aircraftList.SelectedItem as AircraftView;
                return view == null || view.Source == null ? null : view.Source.Id;
            }
        }

        private void SessionSave()
        {
            try
            {
                Dictionary<string, object> kv = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                if (selectedAircraft != null) kv.Add("vehicle_id", selectedAircraft.Id);
                if (typeFilter != null && typeFilter.SelectedItem != null) kv.Add("vehicle_kind", Convert.ToString(typeFilter.SelectedItem, CultureInfo.InvariantCulture));
                AircraftView air = airTarget.SelectedItem as AircraftView;
                if (air != null) kv.Add("air_target", air.Source.Id);
                int airCountValue = SelectedCount(airCount);
                if (airCountValue > 0) kv.Add("air_count", airCountValue);
                if (configuredGroundTargets.Count > 0) kv.Add("ground_targets", String.Join(",", configuredGroundTargets.Select(x => x.Source.Id)));
                kv.Add("hostile", hostileToggle.IsChecked == true ? "1" : "0");
                kv.Add("sam_sites", samSitesToggle.IsChecked == true ? "1" : "0");
                kv.Add("sam_sites_mode", samSitesMode == null ? "active" : samSitesMode.Text);
                kv.Add("sam_sites_selection", samSitesSelection == null ? "s300" : samSitesSelection.Text);
                TargetView ship = shipTarget.SelectedItem as TargetView;
                if (ship != null) kv.Add("ship_target", ship.Source.Id);
                int shipCountValue = SelectedCount(shipCount);
                if (shipCountValue > 0) kv.Add("ship_count", shipCountValue);
                kv.Add("passive_ship", passiveShip ? "1" : "0");
                if (combinedScenario != null)
                {
                    kv.Add("combined_enabled", combinedScenario.Enabled ? "1" : "0");
                    if (!String.IsNullOrWhiteSpace(combinedScenario.MapId)) kv.Add("combined_map", combinedScenario.MapId);
                    kv.Add("combined_side", combinedScenario.Side);
                    if (!String.IsNullOrWhiteSpace(combinedScenario.SpawnOption)) kv.Add("combined_spawn", combinedScenario.SpawnOption);
                }
                ConfigStore.SetObject("session", kv);
                ConfigStore.Save();
            }
            catch { }
        }

        private void SessionRestore()
        {
            try
            {
                Dictionary<string, string> kv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, object> obj = ConfigStore.GetObject("session");
                if (obj != null)
                {
                    foreach (KeyValuePair<string, object> pair in obj)
                    {
                        if (pair.Value == null) continue;
                        kv[pair.Key] = Convert.ToString(pair.Value, CultureInfo.InvariantCulture);
                    }
                }
                if (kv.Count == 0) return;
                string value;
                if (kv.TryGetValue("vehicle_kind", out value) && !String.IsNullOrWhiteSpace(value) && typeFilter != null)
                {
                    int kindIndex = typeFilter.Items.OfType<string>().ToList().FindIndex(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));
                    if (kindIndex > 0) typeFilter.SelectedIndex = kindIndex;
                }
                if (kv.TryGetValue("vehicle_id", out value) && !String.IsNullOrWhiteSpace(value))
                {
                    AircraftView saved = aircraftViews.FirstOrDefault(x => x.Source.Id.Equals(value, StringComparison.OrdinalIgnoreCase));
                    if (saved != null)
                    {
                        // Assign the catalog instance directly: the filtered ItemsSource
                        // shares the same object references, so SelectedItem matches even
                        // before the list is laid out (Items.Cast is unreliable here).
                        aircraftList.SelectedItem = saved;
                    }
                }
                if (kv.TryGetValue("air_target", out value)) SelectAircraftTarget(value);
                if (kv.TryGetValue("air_count", out value)) { int c; if (int.TryParse(value, out c)) SelectCountValue(airCount, c); }
                if (kv.TryGetValue("ground_targets", out value))
                {
                    configuredGroundTargets.Clear();
                    string[] ids = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var pool = (groundTarget.ItemsSource as IEnumerable<TargetView>) ?? Enumerable.Empty<TargetView>();
                    foreach (string id in ids)
                    {
                        TargetView t = pool.FirstOrDefault(x => x.Source.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (t != null) configuredGroundTargets.Add(t);
                    }
                    if (configuredGroundTargets.Count > 0) groundTarget.SelectedItem = configuredGroundTargets[0];
                    groundCount.SelectedItem = configuredGroundTargets.Count > 0 ? 1 : 0;
                }
                if (kv.TryGetValue("hostile", out value)) hostileToggle.IsChecked = value == "1";
                if (kv.TryGetValue("sam_sites", out value)) samSitesToggle.IsChecked = value == "1";
                if (kv.TryGetValue("sam_sites_mode", out value)) samSitesMode.Text = value;
                if (kv.TryGetValue("sam_sites_selection", out value)) samSitesSelection.Text = value;
                if (kv.ContainsKey("sam_sites") && !kv.ContainsKey("sam_sites_mode") && kv.TryGetValue("sam_sites", out value) && value == "0") samSitesMode.Text = "disabled";
                if (kv.TryGetValue("ship_target", out value)) SelectShipTarget(value);
                if (kv.TryGetValue("ship_count", out value)) { int c; if (int.TryParse(value, out c)) SelectCountValue(shipCount, c); }
                if (kv.TryGetValue("passive_ship", out value)) passiveShip = value == "1";
                bool combinedEnabled = false; string mapId = null; int side = 1; string spawn = null;
                if (kv.TryGetValue("combined_enabled", out value)) combinedEnabled = value == "1";
                if (kv.TryGetValue("combined_map", out value)) mapId = value;
                if (kv.TryGetValue("combined_side", out value)) { int s; if (int.TryParse(value, out s)) side = s; }
                if (kv.TryGetValue("combined_spawn", out value)) spawn = value;
                if (combinedEnabled || !String.IsNullOrEmpty(mapId))
                    combinedScenario = new CombinedScenarioSettings { Enabled = combinedEnabled, MapId = mapId, Side = side, SpawnOption = spawn };
                UpdateConfigurationSummary();
            }
            catch { }
        }

        private static void SelectCountValue(ComboBox box, int value)
        {
            foreach (object item in box.Items)
            {
                if (item is int && (int)item == value) { box.SelectedItem = item; return; }
            }
        }

        private static string AircraftViewRoman(int rank)
        {
            string[] values = { "—", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
            return rank >= 0 && rank < values.Length ? values[rank] : rank.ToString(CultureInfo.InvariantCulture);
        }

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            Find<Button>("MaximizeButton").Content = WindowState == WindowState.Maximized ? "❐" : "□";
        }

        private void FilterAircraft()
        {
            if (aircraftViews == null) return;
            string search = (aircraftSearch.Text ?? "").Trim();
            string nation = nationFilter.SelectedIndex > 0 ? nationFilter.SelectedItem as string : null;
            int rank = rankFilter.SelectedIndex > 0 ? rankFilter.SelectedIndex : 0;
            string kind = typeFilter.SelectedIndex > 0 ? typeFilter.SelectedItem as string : null;
            string keep = selectedAircraft == null ? null : selectedAircraft.Id;
            IEnumerable<AircraftView> query = aircraftViews;
            if (!String.IsNullOrEmpty(search)) query = query.Where(x => x.Name.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 || x.Source.Id.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!String.IsNullOrEmpty(nation)) query = query.Where(x => x.Nation == nation);
            if (rank > 0) query = query.Where(x => x.Rank == rank);
            if (!String.IsNullOrEmpty(kind)) query = query.Where(x => x.Kind == kind);
            List<AircraftView> result = query.OrderByDescending(x => x.Rank).ThenBy(x => x.Name).ToList();
            aircraftList.ItemsSource = result;
            vehicleCount.Text = result.Count.ToString("N0", CultureInfo.InvariantCulture) + " vehicles in the current catalog";
            AircraftView previous = result.FirstOrDefault(x => x.Source.Id == keep);
            if (previous != null) aircraftList.SelectedItem = previous;
            else if (result.Count > 0 && aircraftList.SelectedItem == null) aircraftList.SelectedIndex = 0;
        }

        private void AircraftChanged()
        {
            AircraftView view = aircraftList.SelectedItem as AircraftView;
            if (view == null) return;
            selectedAircraft = view.Source;
            controller.WorkspaceSelectAircraft(selectedAircraft.Id);
            previewName.Text = selectedAircraft.Display.ToUpperInvariant();
            previewMeta.Text = selectedAircraft.Kind.ToUpperInvariant() + "  •  " + selectedAircraft.Nation.ToUpperInvariant() + "  •  RANK " + AircraftViewRoman(selectedAircraft.Rank);
            UpdatePreviewKind(selectedAircraft.Kind);
            UpdateVehicleWorkspaceMode();
            if (combinedScenario != null && combinedScenario.Enabled)
            {
                string combinedKind = GroundSelected ? "ground" : MainForm.IsHelicopter(selectedAircraft, null) ? "helicopter" : "aircraft";
                CombinedMap combinedMap = controller.WorkspaceCombinedMaps.FirstOrDefault(x => x.Id.Equals(combinedScenario.MapId ?? "", StringComparison.OrdinalIgnoreCase));
                int combinedSide = combinedScenario.Side == 2 ? 2 : 1;
                if (combinedMap != null && !combinedMap.Spawns.Any(x => x.Side == combinedSide && x.Kind.Equals(combinedKind, StringComparison.OrdinalIgnoreCase) && x.Option.Equals(combinedScenario.SpawnOption ?? "", StringComparison.OrdinalIgnoreCase)))
                {
                    CombinedSpawn fallback = combinedMap.Spawns.FirstOrDefault(x => x.Side == combinedSide && x.Kind.Equals(combinedKind, StringComparison.OrdinalIgnoreCase));
                    combinedScenario.SpawnOption = fallback == null ? null : fallback.Option;
                }
            }
            RefreshPylons();
            UpdateConfigurationSummary();
            SetStatus("VEHICLE READY — " + selectedAircraft.Display, false);
        }

        private bool GroundSelected { get { return selectedAircraft != null && String.Equals(selectedAircraft.Kind, "Ground Vehicle", StringComparison.OrdinalIgnoreCase); } }

        private void UpdateVehicleWorkspaceMode()
        {
            bool ground = GroundSelected;
            buildTitle.Text = ground ? "CONFIGURE GROUND VEHICLE" : "BUILD LOADOUT";
            buildSubtitle.Text = ground ? "Modules, ammunition, ballistics and mobility" : "Select a station, then mount a weapon";
            pylonCard.Visibility = ground ? Visibility.Collapsed : Visibility.Visible;
            weaponFilterPanel.Visibility = ground ? Visibility.Collapsed : Visibility.Visible;
            weaponList.Visibility = ground ? Visibility.Collapsed : Visibility.Visible;
            if (groundWorkspacePanel != null) groundWorkspacePanel.Visibility = ground ? Visibility.Visible : Visibility.Collapsed;
            if (ground) RefreshGroundWorkspace();
            systemsButton.Content = ModernText.L("MODULES", "模块");
            flightConfigureButton.Content = ground ? ModernText.L("GROUND CONFIGURE", "地面配置") : ModernText.L("FLIGHT CONFIGURE", "飞行配置");
            clearStationButton.Visibility = clearAllButton.Visibility = mountButton.Visibility = ground ? Visibility.Collapsed : Visibility.Visible;
            massText.Text = ground ? ModernText.L("GROUND UNIT", "地面单位") : massText.Text;
        }

        private void UpdatePreviewKind(string kind)
        {
            bool helicopter = String.Equals(kind, "Helicopter", StringComparison.OrdinalIgnoreCase);
            bool drone = String.Equals(kind, "Drone", StringComparison.OrdinalIgnoreCase);
            bool ground = String.Equals(kind, "Ground Vehicle", StringComparison.OrdinalIgnoreCase);
            previewAircraftVisual.Visibility = !helicopter && !drone && !ground ? Visibility.Visible : Visibility.Collapsed;
            previewHelicopterVisual.Visibility = helicopter ? Visibility.Visible : Visibility.Collapsed;
            previewDroneVisual.Visibility = drone ? Visibility.Visible : Visibility.Collapsed;
            if (previewGroundVisual != null) previewGroundVisual.Visibility = ground ? Visibility.Visible : Visibility.Collapsed;
        }

        private static int DisplayStation(PylonSlot slot)
        {
            return slot != null && slot.Order > 0 ? slot.Order : (slot == null ? 0 : slot.Slot);
        }

        private void RefreshPylons()
        {
            pylonPanel.Children.Clear();
            selectedPylon = null;
            if (selectedAircraft == null) return;
            if (GroundSelected)
            {
                stationText.Text = ModernText.L("CUSTOM GROUND UNIT — choose research modules and create a projectile/mobility profile.", "自定义地面单位 — 选择研发模块并创建弹道/机动配置。");
                weaponList.ItemsSource = null;
                UpdateMass();
                return;
            }
            List<PylonSlot> slots = controller.WorkspacePylons(selectedAircraft.Id);
            Dictionary<int, PylonAssignment> mounted = controller.WorkspaceAssignments;
            foreach (PylonSlot slot in slots)
            {
                PylonAssignment assignment;
                mounted.TryGetValue(slot.Slot, out assignment);
                int stationNumber = DisplayStation(slot);
                StackPanel label = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                label.Children.Add(new TextBlock
                {
                    Text = stationNumber.ToString("00", CultureInfo.InvariantCulture),
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                label.Children.Add(new TextBlock
                {
                    Text = assignment == null ? ModernText.L("EMPTY", "空") : ShortName(assignment.Weapon.Name, 9),
                    FontSize = 8,
                    Foreground = ModernPalette.Brush(ModernPalette.Muted),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                Button button = new Button
                {
                    MinWidth = 0,
                    Height = 62,
                    Margin = new Thickness(2, 0, 2, 0),
                    Padding = new Thickness(2),
                    Tag = slot,
                    ToolTip = ModernText.L("Station ", "挂架 ") + stationNumber.ToString(CultureInfo.InvariantCulture),
                    Content = label,
                    Style = (Style)root.Resources["ButtonStyle"],
                    Background = assignment == null ? ModernPalette.Brush("#24365F") : ModernPalette.Brush("#225C62")
                };
                button.Click += PylonClicked;
                pylonPanel.Children.Add(button);
            }
            if (slots.Count > 0) SelectPylon(slots[0]);
            else
            {
                stationText.Text = selectedAircraft.Id.Equals("uav_inf_fpv_strike_drone", StringComparison.OrdinalIgnoreCase)
                    ? "FPV DRONE — no external pylons. Fly into the target to detonate the built-in HEAT warhead."
                    : "This vehicle has no editable weapon stations in the current catalog.";
                RefreshWeapons();
            }
            UpdateMass();
        }

        private void PylonClicked(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            SelectPylon(button == null ? null : button.Tag as PylonSlot);
        }

        private void SelectPylon(PylonSlot slot)
        {
            if (slot == null) return;
            selectedPylon = slot;
            stationText.Text = ModernText.L("STATION ", "挂架 ") + DisplayStation(slot).ToString(CultureInfo.InvariantCulture) + " — choose a compatible weapon, or enable Injection for the full catalog.";
            foreach (Button button in pylonPanel.Children.OfType<Button>())
            {
                PylonSlot current = button.Tag as PylonSlot;
                button.BorderBrush = current != null && current.Slot == slot.Slot ? ModernPalette.Brush(ModernPalette.Cyan) : ModernPalette.Brush(ModernPalette.Border);
                button.BorderThickness = current != null && current.Slot == slot.Slot ? new Thickness(2) : new Thickness(1);
            }
            RefreshWeapons();
        }

        private void RefreshWeapons()
        {
            if (selectedAircraft == null || selectedPylon == null)
            {
                weaponList.ItemsSource = null;
                return;
            }
            bool injected = injectionToggle.IsChecked == true;
            string category = categoryFilter.SelectedIndex > 0 ? categoryFilter.SelectedItem as string : null;
            string nation = weaponNationFilter.SelectedIndex > 0 ? weaponNationFilter.SelectedItem as string : null;
            int sort = Math.Max(0, sortFilter.SelectedIndex);
            List<WeaponView> weapons = controller.WorkspaceWeapons(selectedAircraft.Id, selectedPylon.Slot, injected, weaponSearch.Text, category, nation, sort)
                .Select(x => new WeaponView(x, injected)).ToList();
            // Grouping disables UI virtualization (every row gets materialized), so the
            // weapon grid always binds flat; the Type column already shows the category.
            weaponList.ItemsSource = weapons;
            if (!weaponColumnsPending)
            {
                weaponColumnsPending = true;
                weaponList.Dispatcher.BeginInvoke(new Action(() => { weaponColumnsPending = false; UpdateWeaponColumns(); }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void UpdateWeaponColumns()
        {
            if (updatingWeaponColumns || weaponList == null) return;
            GridView view = weaponList.View as GridView;
            if (view == null || view.Columns.Count < 5 || weaponList.ActualWidth <= 0) return;
            updatingWeaponColumns = true;
            try
            {
                ScrollBar vertical = FindVisibleVerticalScrollBar(weaponList);
                GridViewColumnHeader modeHeader = view.Columns[4].Header as GridViewColumnHeader;
                if (modeHeader != null)
                {
                    if (vertical == null) modeHeader.ClearValue(FrameworkElement.StyleProperty);
                    else modeHeader.Style = (Style)root.Resources["LastGridHeader"];
                }
                double gutter = vertical == null ? 0 : Math.Max(8, vertical.ActualWidth);
                double available = Math.Max(360, weaponList.ActualWidth - gutter - 2);
                view.Columns[0].Width = available * 0.43;
                view.Columns[1].Width = available * 0.24;
                view.Columns[2].Width = available * 0.09;
                view.Columns[3].Width = available * 0.12;
                view.Columns[4].Width = available * 0.12;
            }
            finally { updatingWeaponColumns = false; }
        }

        private static ScrollBar FindVisibleVerticalScrollBar(DependencyObject parent)
        {
            if (parent == null) return null;
            int children = VisualTreeHelper.GetChildrenCount(parent);
            for (int index = 0; index < children; index++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, index);
                ScrollBar bar = child as ScrollBar;
                if (bar != null && bar.Orientation == Orientation.Vertical && bar.Visibility == Visibility.Visible)
                    return bar;
                ScrollBar nested = FindVisibleVerticalScrollBar(child);
                if (nested != null) return nested;
            }
            return null;
        }

        private void MountWeapon()
        {
            WeaponView weapon = weaponList.SelectedItem as WeaponView;
            if (weapon == null || selectedPylon == null) return;
            if (controller.WorkspaceAssignWeapon(selectedPylon.Slot, weapon.Source, injectionToggle.IsChecked == true))
            {
                RefreshPylonsKeeping(selectedPylon.Slot);
                SetStatus("MOUNTED — " + weapon.Name + " on station " + DisplayStation(selectedPylon).ToString(CultureInfo.InvariantCulture), false);
            }
        }

        private void ClearStation()
        {
            if (selectedPylon == null) return;
            int slot = selectedPylon.Slot;
            controller.WorkspaceClearStation(slot);
            RefreshPylonsKeeping(slot);
        }

        private void RefreshPylonsKeeping(int slot)
        {
            RefreshPylons();
            PylonSlot keep = controller.WorkspacePylons(selectedAircraft.Id).FirstOrDefault(x => x.Slot == slot);
            if (keep != null) SelectPylon(keep);
        }

        private void UpdateMass()
        {
            if (GroundSelected) { massText.Text = ModernText.L("GROUND UNIT", "地面单位"); return; }
            double total = controller.WorkspaceAssignments.Values.Sum(x => x.Weapon.TotalMass);
            string limit = selectedAircraft != null && selectedAircraft.MaxLoad > 0 ? " / " + selectedAircraft.MaxLoad.ToString("0", CultureInfo.InvariantCulture) + " kg" : "";
            massText.Text = ModernText.L("MASS: ", "质量: ") + total.ToString("0.0", CultureInfo.InvariantCulture) + " kg" + limit;
        }

        private static string ShortName(string value, int length)
        {
            if (String.IsNullOrEmpty(value)) return ModernText.L("WEAPON", "武器");
            string clean = value.Trim();
            return clean.Length <= length ? clean.ToUpperInvariant() : clean.Substring(0, Math.Max(3, length - 1)).ToUpperInvariant() + "…";
        }

        private void GenerateMission()
        {
            try
            {
                controller.WorkspaceGameFolder = gameFolder.Text;
                AircraftView air = airTarget.SelectedItem as AircraftView;
                TargetView ship = shipTarget.SelectedItem as TargetView;
                // The template carries four flying hostiles; the Map window can
                // restyle each of them (Typhoon, air target, Mi-28NM, Ka-52).
                List<FlyingTargetSlot> flying = new List<FlyingTargetSlot>();
                if (airTarget01 != null) flying.Add(new FlyingTargetSlot("Target_Air_01", airTarget01.Source.Id, Math.Max(0, airTarget01Count)));
                if (air != null) flying.Add(new FlyingTargetSlot("Target_Air_02", air.Source.Id, SelectedCount(airCount)));
                if (heliTarget01 != null) flying.Add(new FlyingTargetSlot("Heli_Target", heliTarget01.Source.Id, Math.Max(0, heliTarget01Count)));
                if (heliTarget02 != null) flying.Add(new FlyingTargetSlot("Heli_Target_02", heliTarget02.Source.Id, Math.Max(0, heliTarget02Count)));
                bool generated = controller.WorkspaceGenerateMission(air == null ? null : air.Source.Id, SelectedCount(airCount), configuredGroundTargets.Select(x => x.Source.Id).ToList(),
                    hostileToggle.IsChecked == true, ship == null ? null : ship.Source.Id, SelectedCount(shipCount), passiveShip, flying, combinedScenario, samSitesToggle.IsChecked == true ? samSitesMode.Text : "disabled", samSitesSelection.Text);
                if (generated)
                {
                    SetStatus("MISSION GENERATED — reopen User Missions in War Thunder", false);
                    ModernMissionGeneratedWindow dialog = new ModernMissionGeneratedWindow(GroundSelected) { Owner = this };
                    dialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, true);
                ModernMessageDialog error = new ModernMessageDialog("Universal Test Lab", ex.Message, "关闭", null, true) { Owner = this };
                error.ShowDialog();
            }
        }

        private void BrowseGameFolder()
        {
            try
            {
                string selected = controller.WorkspaceBrowseFolder(gameFolder.Text, new WindowInteropHelper(this).Handle);
                if (String.IsNullOrWhiteSpace(selected)) return;
                gameFolder.Text = selected;
                gameFolder.CaretIndex = gameFolder.Text.Length;
                gameFolder.ScrollToEnd();
                controller.WorkspaceGameFolder = selected;
                SetStatus("GAME DIRECTORY SAVED", false);
            }
            catch (Exception ex) { ShowWorkspaceMessage("Game Directory", ex.Message, true); }
        }

        private void SyncBaseMission()
        {
            try
            {
                controller.WorkspaceGameFolder = gameFolder.Text;
                controller.WorkspaceSyncBase();
                gameFolder.Text = controller.WorkspaceGameFolder;
                SetStatus("BASE MISSION INSTALLED", false);
                ShowWorkspaceMessage("Base Mission Installed", "Base mission installed. Close the User Missions tab in War Thunder and open it again; no game restart is required.", false);
            }
            catch (Exception ex) { ShowWorkspaceMessage("Base Mission", ex.Message, true); }
        }

        private void OpenMissionFolder()
        {
            try
            {
                controller.WorkspaceGameFolder = gameFolder.Text;
                controller.WorkspaceOpenMissions();
                gameFolder.Text = controller.WorkspaceGameFolder;
            }
            catch (Exception ex) { ShowWorkspaceMessage("User Missions", ex.Message, true); }
        }

        private bool ConfirmWorkspaceAction(string title, string message)
        {
            ModernMessageDialog dialog = new ModernMessageDialog(title, message, "CONTINUE", "取消", false) { Owner = this };
            return dialog.ShowDialog() == true;
        }

        private void ShowWorkspaceMessage(string title, string message, bool danger)
        {
            SetStatus(message, danger);
            ModernMessageDialog dialog = new ModernMessageDialog(title, message, "关闭", null, danger) { Owner = this };
            dialog.ShowDialog();
        }

        private static int SelectedCount(ComboBox box) { return box.SelectedItem is int ? (int)box.SelectedItem : 0; }

        private void SelectAircraftTarget(string id)
        {
            airTarget.SelectedItem = (airTarget.ItemsSource as IEnumerable<AircraftView>).FirstOrDefault(x => x.Source.Id == id);
        }

        private void SelectGroundTarget(string id)
        {
            groundTarget.SelectedItem = (groundTarget.ItemsSource as IEnumerable<TargetView>).FirstOrDefault(x => x.Source.Id == id);
        }

        private void SelectShipTarget(string id)
        {
            shipTarget.SelectedItem = (shipTarget.ItemsSource as IEnumerable<TargetView>).FirstOrDefault(x => x.Source.Id == id);
        }

        private void ShowFlightSystems()
        {
            if (selectedAircraft == null) return;
            ModernFlightSystemsWindow dialog = new ModernFlightSystemsWindow(selectedAircraft,
                controller.WorkspaceModifications.Where(x => x.AircraftId.Equals(selectedAircraft.Id, StringComparison.OrdinalIgnoreCase)),
                controller.WorkspaceGetSettings(selectedAircraft), MainForm.IsHelicopter(selectedAircraft, null));
            dialog.Owner = this;
            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                controller.WorkspaceSetSettings(selectedAircraft, dialog.Result);
            // Remember the cannon injection globally so every vehicle reuses it.
            MissionSettings.Current.InjectedCannonBlk = dialog.Result.InjectedCannonBlk;
            MissionSettings.Current.InjectedCannonDomain = dialog.Result.InjectedCannonDomain;
            MissionSettings.Current.InjectedCannonUnit = dialog.Result.InjectedCannonUnit;
            MissionSettings.Current.Save();
                SetStatus("MODULES UPDATED — " + selectedAircraft.Display, false);
                UpdateConfigurationSummary();
            }
        }

        private void ShowFlightConfigure()
        {
            if (selectedAircraft == null) return;
            if (GroundSelected)
            {
                ModernGroundConfigureWindow groundDialog = new ModernGroundConfigureWindow(selectedAircraft, controller.WorkspaceGetSettings(selectedAircraft), controller.WorkspaceGroundAmmo, controller.WorkspaceGroundTargets, controller.WorkspaceUnitWeapons, controller.WorkspaceGroundWeapons(selectedAircraft), new GroundAmmo[0], controller.WorkspaceGunBeltOptions(selectedAircraft), controller.WorkspaceResolveCannonAmmo);
                groundDialog.Owner = this;
                if (groundDialog.ShowDialog() == true && groundDialog.Result != null)
                {
                    controller.WorkspaceSetSettings(selectedAircraft, groundDialog.Result);
                    SetStatus("GROUND CONFIGURATION UPDATED — " + selectedAircraft.Display, false);
                    UpdateConfigurationSummary();
                }
                return;
            }
            ModernFlightConfigureWindow dialog = new ModernFlightConfigureWindow(selectedAircraft,
                controller.WorkspaceGetSettings(selectedAircraft), controller.WorkspaceCountermeasureLaunchers(selectedAircraft),
                controller.WorkspaceModifications.Where(x => x.AircraftId.Equals(selectedAircraft.Id, StringComparison.OrdinalIgnoreCase)));
            dialog.Owner = this;
            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                controller.WorkspaceSetSettings(selectedAircraft, dialog.Result);
            // Remember the cannon injection globally so every vehicle reuses it.
            MissionSettings.Current.InjectedCannonBlk = dialog.Result.InjectedCannonBlk;
            MissionSettings.Current.InjectedCannonDomain = dialog.Result.InjectedCannonDomain;
            MissionSettings.Current.InjectedCannonUnit = dialog.Result.InjectedCannonUnit;
            MissionSettings.Current.Save();
                SetStatus("FLIGHT CONFIGURATION UPDATED — " + selectedAircraft.Display, false);
                UpdateConfigurationSummary();
            }
        }

        private void ShowMissionOptions()
        {
            ModernMissionOptionsWindow dialog = new ModernMissionOptionsWindow();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                MissionSettings.Current = dialog.Result;
                MissionSettings.Current.Save();
                SetStatus("MISSION OPTIONS UPDATED (global)", false);
                UpdateConfigurationSummary();
            }
        }

        private void ShowMap()
        {
            string playerKind = GroundSelected ? "ground" : MainForm.IsHelicopter(selectedAircraft, null) ? "helicopter" : "aircraft";
            ModernMapWindow dialog = new ModernMapWindow(
                (airTarget.ItemsSource as IEnumerable<AircraftView>) ?? Enumerable.Empty<AircraftView>(),
                (groundTarget.ItemsSource as IEnumerable<TargetView>) ?? Enumerable.Empty<TargetView>(),
                (shipTarget.ItemsSource as IEnumerable<TargetView>) ?? Enumerable.Empty<TargetView>(),
                airTarget.SelectedItem as AircraftView, SelectedCount(airCount), configuredGroundTargets,
                hostileToggle.IsChecked == true, samSitesMode.Text, samSitesSelection.Text, shipTarget.SelectedItem as TargetView, SelectedCount(shipCount), passiveShip,
                controller.WorkspaceCombinedMaps, playerKind, combinedScenario,
                airTarget01, airTarget01Count, heliTarget01, heliTarget01Count, heliTarget02, heliTarget02Count);
            dialog.Owner = this;
            if (dialog.ShowDialog() != true) return;
            airTarget.SelectedItem = dialog.AirTarget;
            airCount.SelectedItem = dialog.AirCount;
            airTarget01 = dialog.AirTarget01;
            airTarget01Count = dialog.AirCount01;
            heliTarget01 = dialog.HeliTarget01;
            heliTarget01Count = dialog.HeliCount01;
            heliTarget02 = dialog.HeliTarget02;
            heliTarget02Count = dialog.HeliCount02;
            configuredGroundTargets.Clear();
            configuredGroundTargets.AddRange(dialog.GroundTargets);
            if (configuredGroundTargets.Count > 0) groundTarget.SelectedItem = configuredGroundTargets[0];
            groundCount.SelectedItem = configuredGroundTargets.Count > 0 ? 1 : 0;
            hostileToggle.IsChecked = dialog.Hostile;
            samSitesToggle.IsChecked = dialog.SamSitesMode != "disabled";
            samSitesMode.Text = dialog.SamSitesMode;
            samSitesSelection.Text = dialog.SamSitesSelection;
            shipTarget.SelectedItem = dialog.ShipTarget;
            shipCount.SelectedItem = dialog.ShipCount;
            passiveShip = dialog.PassiveShip;
            combinedScenario = dialog.Scenario == null ? new CombinedScenarioSettings() : dialog.Scenario.Copy();
            UpdateConfigurationSummary();
        }

        private void UpdateConfigurationSummary()
        {
            if (flightProfileText == null || targetSummaryText == null) return;
            AircraftSettings settings = selectedAircraft == null ? new AircraftSettings() : controller.WorkspaceGetSettings(selectedAircraft);
            if (GroundSelected)
            {
                string ammo = settings.GroundAmmoLoadouts.Count == 0 ? "native ammunition" : settings.GroundAmmoLoadouts.Count.ToString(CultureInfo.InvariantCulture) + " custom ammunition slots";
                string tuning = settings.OverrideGroundBallistics ? "custom ballistics & mobility" : "native ballistics & mobility";
                string sight = String.IsNullOrWhiteSpace(settings.UserSightPath) ? "game/default sight" : System.IO.Path.GetFileNameWithoutExtension(settings.UserSightPath) + " sight";
                flightProfileText.Text = ammo + "  •  " + tuning + "\n" + sight + "  •  rearm 1 second after depletion\n" +
                    (combinedScenario != null && combinedScenario.Enabled ? "Instant respawn at the selected combined-battles spawn" : "Instant zero-delay respawn at the range hangar");
            }
            else
            {
            string fuel = settings.FullFuel ? ModernText.L("Full internal fuel", "满内部燃油") : settings.FuelMinutes.ToString(CultureInfo.InvariantCulture) + " minutes of internal fuel";
            string countermeasures = !settings.OverrideCountermeasures ? "Native countermeasure load" :
                settings.CountermeasureLoadouts.Count.ToString(CultureInfo.InvariantCulture) + " configured dispenser groups";
            string belts = settings.GunBeltSelections.Count == 0 ? "default gun belts" : settings.GunBeltSelections.Count.ToString(CultureInfo.InvariantCulture) + " selected gun belt groups";
            flightProfileText.Text = fuel + "  •  " + (combinedScenario != null && combinedScenario.Enabled ? "selected map spawn profile" : "adaptive air-start speed") + "\n" + countermeasures + "  •  " + belts + "\nAmmunition restored 1 second after depletion";
            }
            if (combinedScenario != null && combinedScenario.Enabled)
            {
                CombinedMap map = controller.WorkspaceCombinedMaps.FirstOrDefault(x => x.Id.Equals(combinedScenario.MapId ?? "", StringComparison.OrdinalIgnoreCase));
                string playerKind = GroundSelected ? "ground" : MainForm.IsHelicopter(selectedAircraft, null) ? "helicopter" : "aircraft";
                CombinedSpawn spawn = map == null ? null : map.Spawns.FirstOrDefault(x => x.Side == (combinedScenario.Side == 2 ? 2 : 1) && x.Kind.Equals(playerKind, StringComparison.OrdinalIgnoreCase) && x.Option.Equals(combinedScenario.SpawnOption ?? "", StringComparison.OrdinalIgnoreCase));
                targetSummaryText.Text = "Combined Battles — Domination\n" + (map == null ? "Select a map" : map.Display) + "  •  Side " + (combinedScenario.Side == 2 ? "2" : "1") + "\n" + (spawn == null ? "Select a compatible spawn" : spawn.Label) + "  •  no AI units";
                return;
            }
            AircraftView air = airTarget == null ? null : airTarget.SelectedItem as AircraftView;
            TargetView ground = groundTarget == null ? null : groundTarget.SelectedItem as TargetView;
            TargetView ship = shipTarget == null ? null : shipTarget.SelectedItem as TargetView;
            targetSummaryText.Text = ModernText.L("Air: ", "空中: ") + SelectedCount(airCount).ToString(CultureInfo.InvariantCulture) + " × " + (air == null ? "none" : air.Name) +
                ModernText.L("\nGround: ", "\n地面: ") + configuredGroundTargets.Count.ToString(CultureInfo.InvariantCulture) + " positions  •  " + (hostileToggle.IsChecked == true ? "ATTACKING" : "PASSIVE") +
                ModernText.L("\nNaval: ", "\n海上: ") + SelectedCount(shipCount).ToString(CultureInfo.InvariantCulture) + " × " + (ship == null ? "none" : ship.Name) + "  •  " + (passiveShip ? "PASSIVE" : "RETURNS FIRE") +
                ModernText.L("\nAir Defence: ", "\n防空: ") + (samSitesToggle.IsChecked == true ? ModernText.L("SAM SITES ", "SAM 阵地 ") + (samSitesMode != null && samSitesMode.Text == "passive" ? "PASSIVE" : samSitesMode != null && samSitesMode.Text == "friendly" ? "FRIENDLY" : "ACTIVE") : "SAM SITES DISABLED") + "  •  " + (samSitesSelection == null ? "S300" : samSitesSelection.Text.ToUpperInvariant());
        }

        private void ShowPresets()
        {
            ModernPresetWindow dialog = new ModernPresetWindow(controller, this);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true) RefreshFromController();
        }

        private void ShowAbout()
        {
            ModernAboutWindow dialog = new ModernAboutWindow(controller.WorkspaceAircraft.Count, controller.WorkspaceWeaponCount);
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        internal void RefreshFromController()
        {
            Aircraft current = controller.WorkspaceSelectedAircraft;
            if (current == null) return;
            aircraftSearch.Text = "";
            nationFilter.SelectedIndex = rankFilter.SelectedIndex = typeFilter.SelectedIndex = 0;
            FilterAircraft();
            aircraftList.SelectedItem = aircraftList.Items.Cast<AircraftView>().FirstOrDefault(x => x.Source.Id == current.Id);
            selectedAircraft = current;
            RefreshPylons();
            UpdateConfigurationSummary();
        }

        internal void ExerciseDropdownForSelfTest()
        {
            rankFilter.IsDropDownOpen = true;
            UpdateLayout();
            rankFilter.IsDropDownOpen = false;
        }

        internal void SelectVehicleKindForScreenshot(string kind)
        {
            AircraftView target = aircraftList.Items.Cast<AircraftView>().FirstOrDefault(x => String.Equals(x.Kind, kind, StringComparison.OrdinalIgnoreCase));
            if (target != null)
            {
                aircraftList.SelectedItem = target;
                aircraftList.ScrollIntoView(target);
            }
        }

        internal void EnableInjectionForScreenshot()
        {
            injectionToggle.IsChecked = true;
            UpdateLayout();
            UpdateWeaponColumns();
        }

        internal void ShowGroundPresetForScreenshot()
        {
            SelectVehicleKindForScreenshot("Ground Vehicle");
            ModernPresetWindow dialog = new ModernPresetWindow(controller, this) { Owner = this };
            ShowOverlay(dialog);
            dialog.SelectFirstCustomSightForScreenshot();
        }

        internal void ShowMessageForScreenshot(bool danger)
        {
            ModernMessageDialog dialog = new ModernMessageDialog(
                danger ? "Game Resource" : "Base Mission Installed",
                danger ? "Extracted game resource was not found." : "Base mission installed. Close the User Missions tab in War Thunder and open it again; no game restart is required.",
                "关闭", null, danger) { Owner = this };
            ShowOverlay(dialog);
        }

        internal bool ExerciseOverlayForSelfTest()
        {
            int windowCountBefore = System.Windows.Application.Current.Windows.Count;
            ModernMissionGeneratedWindow dialog = new ModernMissionGeneratedWindow { Owner = this };
            ShowOverlay(dialog);
            UpdateLayout();
            bool firstShown = overlayLayer.Visibility == Visibility.Visible &&
                overlayDialogs.Count == 1 &&
                overlayLayer.Children.Contains(dialog) &&
                dialog.OverlayChromeReadyForSelfTest() &&
                root.Effect is BlurEffect &&
                !root.IsHitTestVisible &&
                System.Windows.Application.Current.Windows.Count == windowCountBefore;

            ModernMessageDialog nested = new ModernMessageDialog("Overlay Test", "Nested confirmations stay inside the same application window.", "OK", "取消", false) { Owner = this };
            ShowOverlay(nested);
            UpdateLayout();
            bool nestedShown = overlayDialogs.Count == 2 &&
                overlayLayer.Children.Contains(nested) &&
                nested.OverlayChromeReadyForSelfTest() &&
                dialog.Effect is BlurEffect &&
                !dialog.IsHitTestVisible &&
                System.Windows.Application.Current.Windows.Count == windowCountBefore;
            nested.Close();
            UpdateLayout();
            bool nestedClosed = overlayDialogs.Count == 1 &&
                dialog.Effect == null && dialog.IsHitTestVisible && dialog.Opacity == 1;
            dialog.Close();
            UpdateLayout();
            bool closed = overlayLayer.Visibility == Visibility.Collapsed &&
                overlayDialogs.Count == 0 &&
                root.Effect == null &&
                root.IsHitTestVisible;
            return firstShown && nestedShown && nestedClosed && closed;
        }

        internal bool LayoutFixesReadyForSelfTest()
        {
            RectangleGeometry clip = previewClipContent.Clip as RectangleGeometry;
            Rect work = SystemParameters.WorkArea;
            bool insideWorkArea = Left >= work.Left - 1 && Top >= work.Top - 1 &&
                Left + ActualWidth <= work.Right + 1 && Top + ActualHeight <= work.Bottom + 1;
            ScrollBar testBar = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Style = (Style)root.Resources[typeof(ScrollBar)]
            };
            testBar.ApplyTemplate();
            Track track = testBar.Template.FindName("PART_Track", testBar) as Track;
            RectangleGeometry weaponClip = weaponTableClipContent.Clip as RectangleGeometry;
            injectionToggle.IsChecked = false;
            UpdateLayout();
            UpdateWeaponColumns();
            GridView normalView = weaponList.View as GridView;
            GridViewColumnHeader normalMode = normalView == null ? null : normalView.Columns[4].Header as GridViewColumnHeader;
            bool unroundedModeWithoutScroll = FindVisibleVerticalScrollBar(weaponList) == null &&
                normalMode != null && !Object.ReferenceEquals(normalMode.Style, root.Resources["LastGridHeader"]);
            if (normalMode != null) normalMode.ApplyTemplate();
            bool staticHeaderTemplate = normalMode != null && normalMode.Template != null &&
                normalMode.Template.FindName("HeaderBorder", normalMode) is Border &&
                normalMode.Template.Triggers.Count == 0;
            injectionToggle.IsChecked = true;
            UpdateLayout();
            UpdateWeaponColumns();
            GridView weaponView = weaponList.View as GridView;
            GridViewColumnHeader scrollingMode = weaponView == null ? null : weaponView.Columns[4].Header as GridViewColumnHeader;
            double columnWidth = weaponView == null ? 0 : weaponView.Columns.Sum(x => x.Width);
            ScrollBar weaponScroll = FindVisibleVerticalScrollBar(weaponList);
            double weaponGutter = weaponScroll == null ? 0 : Math.Max(8, weaponScroll.ActualWidth);
            double expectedColumnWidth = Math.Max(360, weaponList.ActualWidth - weaponGutter - 2);
            List<PylonSlot> stationSlots = pylonPanel.Children.OfType<Button>().Select(x => x.Tag as PylonSlot).Where(x => x != null).ToList();
            bool stationOrder = stationSlots.Count > 0 && stationSlots.Select(DisplayStation).SequenceEqual(stationSlots.Select(DisplayStation).OrderBy(x => x));
            bool stationFit = pylonPanel.Children.OfType<Button>().All(x => x.ActualWidth <= 100 && x.ActualHeight <= 64);
            SolidColorBrush rootBrush = root.Background as SolidColorBrush;
            Border titleBar = Find<Border>("TitleBar");
            SolidColorBrush titleBrush = titleBar.Background as SolidColorBrush;
            bool gameFolderVisible = gameFolder.ActualHeight >= 29 && gameFolder.Padding.Top <= 3 &&
                gameFolder.VerticalContentAlignment == VerticalAlignment.Center && !String.IsNullOrWhiteSpace(gameFolder.Text);
            UpdatePreviewKind("Aircraft");
            bool aircraftPreview = previewAircraftVisual.Visibility == Visibility.Visible && previewHelicopterVisual.Visibility == Visibility.Collapsed && previewDroneVisual.Visibility == Visibility.Collapsed && previewAircraftImage.Source != null;
            UpdatePreviewKind("Helicopter");
            bool helicopterPreview = previewAircraftVisual.Visibility == Visibility.Collapsed && previewHelicopterVisual.Visibility == Visibility.Visible && previewDroneVisual.Visibility == Visibility.Collapsed && previewHelicopterImage.Source != null;
            UpdatePreviewKind("Drone");
            bool dronePreview = previewAircraftVisual.Visibility == Visibility.Collapsed && previewHelicopterVisual.Visibility == Visibility.Collapsed && previewDroneVisual.Visibility == Visibility.Visible && Object.ReferenceEquals(previewHelicopterImage.Source, previewDroneImage.Source);
            UpdatePreviewKind(selectedAircraft == null ? "Aircraft" : selectedAircraft.Kind);
            return clip != null && clip.RadiusX == 14 && clip.RadiusY == 14 && previewCard.Clip == null &&
                previewClipContent.ActualWidth > 0 && previewClipContent.ActualHeight > 0 && insideWorkArea &&
                weaponClip != null && weaponClip.RadiusX == 12 && weaponTableFrame.Clip == null &&
                weaponScroll != null && unroundedModeWithoutScroll && staticHeaderTemplate && scrollingMode != null &&
                Object.ReferenceEquals(scrollingMode.Style, root.Resources["LastGridHeader"]) &&
                weaponList.ItemsSource != null &&
                Math.Abs(columnWidth - expectedColumnWidth) < 2 && stationOrder && stationFit &&
                rootBrush != null && rootBrush.Color.A < 255 && titleBrush != null && titleBrush.Color.A == 255 && gameFolderVisible &&
                aircraftPreview && helicopterPreview && dronePreview &&
                track != null && track.IsDirectionReversed &&
                !ModernXaml.Main.Contains("ChromeFill") &&
                ModernXaml.Main.Contains("Margin=\"10,7,34,7\"") &&
                ModernXaml.Main.Contains("Grid Grid.Row=\"1\" Margin=\"12,10,12,10\"");
        }

        internal bool CombinedCatalogReadyForSelfTest()
        {
            return controller.WorkspaceCombinedMaps.Count >= 40 && controller.WorkspaceCombinedMaps.All(map =>
                !String.IsNullOrWhiteSpace(map.Level) && map.Spawns.Count == 12 && new[] { 1, 2 }.All(side =>
                    new[] { "ground_1", "ground_2", "airfield", "air", "heli_near", "heli_far" }.All(option =>
                        map.Spawns.Count(spawn => spawn.Side == side && spawn.Option.Equals(option, StringComparison.OrdinalIgnoreCase)) == 1)));
        }

        private void SetStatus(string message, bool error)
        {
            status.Text = error ? "●  ERROR — " + message : "●  " + message;
            status.Foreground = ModernPalette.Brush(error ? ModernPalette.Danger : ModernPalette.Good);
        }
    }

    internal abstract class ModernDialogWindow : ContentControl
    {
        protected readonly Grid DialogRoot;
        protected readonly Border ContentCard;
        private ModernMainWindow overlayOwner;
        private Window standaloneHost;
        private System.Windows.Threading.DispatcherFrame dialogFrame;
        private bool isOpen;

        public string Title { get; set; }
        public Window Owner { get; set; }
        public ResizeMode ResizeMode { get; set; }
        public WindowStartupLocation WindowStartupLocation { get; set; }
        public WindowState WindowState { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public bool? DialogResult { get; set; }

        protected ModernDialogWindow(string title, double width, double height)
        {
            Title = title;
            Width = width;
            Height = height;
            MinWidth = Math.Min(width, 720);
            MinHeight = Math.Min(height, 520);
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResize;
            Background = Brushes.Transparent;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            Focusable = true;

            DialogRoot = new Grid { Background = Brushes.Transparent };
            Grid styleSource = (Grid)ModernXaml.Parse(ModernXaml.Main);
            foreach (object key in styleSource.Resources.Keys) DialogRoot.Resources[key] = styleSource.Resources[key];

            ContentCard = new Border
            {
                Margin = new Thickness(8),
                Padding = new Thickness(20),
                CornerRadius = new CornerRadius(18),
                Background = ModernPalette.Brush("#EE34415B"),
                BorderBrush = ModernPalette.Brush(ModernPalette.Border),
                BorderThickness = new Thickness(1),
                ClipToBounds = true
            };
            DialogRoot.Children.Add(ContentCard);

            Border closeCloud = new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(13),
                Background = ModernPalette.Brush(ModernPalette.Danger),
                BorderBrush = ModernPalette.Brush("#FFFFA2BC"),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 14, 14, 0),
                Cursor = Cursors.Hand,
                ToolTip = ModernText.L("Close", "关闭"),
                Tag = "OverlayCloseCloud"
            };
            closeCloud.Child = new TextBlock
            {
                Text = "×",
                Foreground = ModernPalette.Brush("#FFFFE8EF"),
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -2, 0, 0)
            };
            closeCloud.MouseLeftButtonUp += delegate { Close(); };
            Panel.SetZIndex(closeCloud, 20);
            DialogRoot.Children.Add(closeCloud);
            Content = DialogRoot;
            Loaded += delegate { ModernComboSizing.Attach(this); };
            PreviewKeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) { e.Handled = true; Close(); }
            };
        }

        internal bool OverlayChromeReadyForSelfTest()
        {
            Border closeCloud = DialogRoot.Children.OfType<Border>().FirstOrDefault(x => String.Equals(x.Tag as string, "OverlayCloseCloud", StringComparison.Ordinal));
            SolidColorBrush closeFill = closeCloud == null ? null : closeCloud.Background as SolidColorBrush;
            SolidColorBrush dangerFill = ModernPalette.Brush(ModernPalette.Danger) as SolidColorBrush;
            return DialogRoot.RowDefinitions.Count == 0 && ContentCard.CornerRadius.TopLeft >= 18 && ContentCard.BorderThickness.Left > 0 &&
                closeCloud != null && closeCloud.CornerRadius.TopLeft >= 12 && closeCloud.BorderBrush != null && closeFill != null && dangerFill != null &&
                closeFill.Color == dangerFill.Color && closeFill.Color.A == 255;
        }

        internal void AttachOverlay(ModernMainWindow owner)
        {
            overlayOwner = owner;
            Owner = owner;
            isOpen = true;
        }

        internal void DetachOverlay()
        {
            overlayOwner = null;
        }

        public bool? ShowDialog()
        {
            ModernMainWindow main = Owner as ModernMainWindow ?? System.Windows.Application.Current.MainWindow as ModernMainWindow;
            DialogResult = null;
            isOpen = true;
            if (main != null)
            {
                main.ShowOverlay(this);
                dialogFrame = new System.Windows.Threading.DispatcherFrame();
                System.Windows.Threading.Dispatcher.PushFrame(dialogFrame);
                dialogFrame = null;
                return DialogResult;
            }

            Window host = CreateStandaloneHost();
            host.ShowDialog();
            return DialogResult;
        }

        public void Show()
        {
            if (isOpen) return;
            DialogResult = null;
            isOpen = true;
            CreateStandaloneHost().Show();
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            ModernMainWindow main = overlayOwner;
            if (main != null) main.CloseOverlay(this);
            Window host = standaloneHost;
            standaloneHost = null;
            if (host != null) host.Close();
            if (dialogFrame != null) dialogFrame.Continue = false;
        }

        public void DragMove()
        {
            if (standaloneHost != null)
            {
                try { standaloneHost.DragMove(); }
                catch (InvalidOperationException) { }
            }
        }

        private Window CreateStandaloneHost()
        {
            if (standaloneHost != null) return standaloneHost;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Margin = new Thickness(0);
            MaxWidth = Double.PositiveInfinity;
            MaxHeight = Double.PositiveInfinity;
            Window host = new Window
            {
                Title = Title,
                Width = Width,
                Height = Height,
                MinWidth = MinWidth,
                MinHeight = MinHeight,
                WindowStartupLocation = WindowStartupLocation,
                ResizeMode = ResizeMode,
                WindowStyle = WindowStyle.None,
                Background = Brushes.Transparent,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                Content = this
            };
            if (WindowStartupLocation == WindowStartupLocation.Manual)
            {
                host.Left = Left;
                host.Top = Top;
            }
            if (Owner != null && Owner != host) host.Owner = Owner;
            WindowChrome.SetWindowChrome(host, new WindowChrome
            {
                CaptionHeight = 0,
                ResizeBorderThickness = new Thickness(7),
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(0),
                UseAeroCaptionButtons = false
            });
            host.SourceInitialized += delegate { DwmGlass.Apply(host); };
            host.Closed += delegate
            {
                if (standaloneHost == host) standaloneHost = null;
                isOpen = false;
                if (dialogFrame != null) dialogFrame.Continue = false;
            };
            standaloneHost = host;
            return host;
        }

        protected Button DialogButton(string text, bool primary)
        {
            return new Button { Content = text, Style = (Style)DialogRoot.Resources[primary ? "PrimaryButton" : "ButtonStyle"], Margin = new Thickness(4, 0, 0, 0) };
        }

        protected TextBlock Heading(string text, double size)
        {
            return new TextBlock { Text = text, FontSize = size, FontWeight = FontWeights.SemiBold, Foreground = ModernPalette.Brush(ModernPalette.Text) };
        }

        protected TextBlock Caption(string text)
        {
            return new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ModernPalette.Brush(ModernPalette.Muted) };
        }
    }

    internal sealed class ModernMissionGeneratedWindow : ModernDialogWindow
    {
        public ModernMissionGeneratedWindow(bool ground = false) : base("Mission Generated", 590, 455)
        {
            ResizeMode = ResizeMode.NoResize;
            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(82) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });
            ContentCard.Child = layout;
            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            header.ColumnDefinitions.Add(new ColumnDefinition());
            Border badge = new Border { Width = 52, Height = 52, CornerRadius = new CornerRadius(16), Background = ModernPalette.Brush(ModernPalette.Good), VerticalAlignment = VerticalAlignment.Top };
            badge.Child = new TextBlock { Text = "✓", FontSize = 26, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            header.Children.Add(badge);
            StackPanel heading = new StackPanel { Margin = new Thickness(10, 3, 0, 0) };
            heading.Children.Add(Heading("MISSION GENERATED", 21));
            heading.Children.Add(new TextBlock { Text = ground ? "The ground proxy and mission are ready." : "The hot-load mission is ready in War Thunder.", Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 4, 0, 0) });
            Grid.SetColumn(heading, 1); header.Children.Add(heading); layout.Children.Add(header);
            Border instructions = new Border { CornerRadius = new CornerRadius(14), Background = ModernPalette.Brush(ModernPalette.Field), BorderBrush = ModernPalette.Brush(ModernPalette.Border), BorderThickness = new Thickness(1), Padding = new Thickness(18), Margin = new Thickness(0, 8, 0, 10) };
            StackPanel steps = new StackPanel();
            steps.Children.Add(Heading(ground ? "RELOAD THE GROUND PROXY" : "REFRESH USER MISSIONS", 14));
            steps.Children.Add(new TextBlock { Text = ground ? "1   Exit War Thunder completely.\n\n2   Start War Thunder again.\n\n3   Open User Missions and launch the current HOT UTL mission." : "1   Close the User Missions tab.\n\n2   Open User Missions again to refresh the list.\n\n3   Launch the current HOT UTL mission.", Foreground = ModernPalette.Brush(ModernPalette.Text), FontSize = 13, Margin = new Thickness(0, 14, 0, 0) });
            steps.Children.Add(new TextBlock { Text = ground ? "A restart is required only because War Thunder caches the reserve-tank proxy." : "No game restart is required.", Foreground = ModernPalette.Brush(ModernPalette.Good), Margin = new Thickness(0, 16, 0, 0), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            steps.Children.Add(new TextBlock { Text = "Custom ground sight selected? Press Alt + F9 once in the mission to reload UserSights.", Foreground = ModernPalette.Brush(ModernPalette.Cyan), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0), FontSize = 11 });
            instructions.Child = steps; Grid.SetRow(instructions, 1); layout.Children.Add(instructions);
            Button ok = DialogButton("GOT IT", true); ok.Width = 150; ok.HorizontalAlignment = HorizontalAlignment.Right; ok.Click += delegate { Close(); }; Grid.SetRow(ok, 2); layout.Children.Add(ok);
        }
    }

    internal sealed class ModernMessageDialog : ModernDialogWindow
    {
        public ModernMessageDialog(string title, string message, string primaryText, string secondaryText, bool danger)
            : base(title, 620, 390)
        {
            ResizeMode = ResizeMode.NoResize;
            bool confirmation = !String.IsNullOrEmpty(secondaryText);
            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(76) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
            ContentCard.Child = layout;

            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            header.ColumnDefinitions.Add(new ColumnDefinition());
            Border badge = new Border
            {
                Width = 50,
                Height = 50,
                CornerRadius = new CornerRadius(15),
                Background = ModernPalette.Brush(danger ? ModernPalette.Danger : confirmation ? ModernPalette.AccentDark : ModernPalette.Good),
                VerticalAlignment = VerticalAlignment.Top
            };
            badge.Child = new TextBlock
            {
                Text = danger ? "!" : confirmation ? "?" : "✓",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            header.Children.Add(badge);
            StackPanel heading = new StackPanel { Margin = new Thickness(10, 2, 0, 0) };
            heading.Children.Add(Heading(title.ToUpperInvariant(), 20));
            heading.Children.Add(new TextBlock
            {
                Text = danger ? "The requested action could not be completed." : confirmation ? "Please confirm this action." : "The action completed successfully.",
                Foreground = ModernPalette.Brush(danger ? ModernPalette.Danger : confirmation ? ModernPalette.Cyan : ModernPalette.Good),
                Margin = new Thickness(0, 4, 0, 0)
            });
            Grid.SetColumn(heading, 1);
            header.Children.Add(heading);
            layout.Children.Add(header);

            Border messageCard = new Border
            {
                CornerRadius = new CornerRadius(14),
                Background = ModernPalette.Brush(ModernPalette.Field),
                BorderBrush = ModernPalette.Brush(ModernPalette.Border),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(18),
                Margin = new Thickness(0, 6, 0, 10)
            };
            messageCard.Child = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = ModernPalette.Brush(ModernPalette.Text),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(messageCard, 1);
            layout.Children.Add(messageCard);

            Grid footer = new Grid { HorizontalAlignment = HorizontalAlignment.Right };
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            if (!String.IsNullOrEmpty(secondaryText)) footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            if (!String.IsNullOrEmpty(secondaryText))
            {
                Button secondary = DialogButton(secondaryText, false);
                secondary.Click += delegate { DialogResult = false; Close(); };
                footer.Children.Add(secondary);
            }
            Button primary = DialogButton(primaryText, true);
            primary.Click += delegate { DialogResult = true; Close(); };
            if (!String.IsNullOrEmpty(secondaryText)) Grid.SetColumn(primary, 1);
            footer.Children.Add(primary);
            Grid.SetRow(footer, 2);
            layout.Children.Add(footer);
        }
    }

    internal sealed class ModernInputWindow : ModernDialogWindow
    {
        public string Value { get; private set; }

        public ModernInputWindow(string title, string caption, string initialValue) : base(title, 460, 250)
        {
            ResizeMode = ResizeMode.NoResize;
            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
            ContentCard.Child = layout;
            StackPanel panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            panel.Children.Add(Heading(title, 18));
            panel.Children.Add(new TextBlock { Text = caption, Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap, FontSize = 12 });
            TextBox box = new TextBox { Text = initialValue ?? String.Empty, Margin = new Thickness(0, 12, 0, 0), Height = 36, Padding = new Thickness(10, 4, 10, 4), VerticalContentAlignment = VerticalAlignment.Center, FontSize = 14 };
            panel.Children.Add(box);
            Grid.SetRow(panel, 0); layout.Children.Add(panel);
            Grid footer = new Grid { HorizontalAlignment = HorizontalAlignment.Right };
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            Button cancel = DialogButton("取消", false); cancel.Click += delegate { DialogResult = false; Close(); }; footer.Children.Add(cancel);
            Button save = DialogButton("SAVE PRESET", true); Grid.SetColumn(save, 1); save.Click += delegate { Value = box.Text; DialogResult = true; Close(); }; footer.Children.Add(save);
            Grid.SetRow(footer, 1); layout.Children.Add(footer);
        }
    }

    internal sealed class MapPanelState
    {


        public List<AircraftView> Aircraft;
        public List<TargetView> Ground;
        public List<TargetView> Ships;
        public List<CombinedMap> CombinedMaps;
        public string PlayerKind;
        public AircraftView CurrentAir;
        public int CurrentAirCount;
        public AircraftView CurrentAir01;
        public int CurrentAir01Count;
        public AircraftView CurrentHeli01;
        public int CurrentHeli01Count;
        public AircraftView CurrentHeli02;
        public int CurrentHeli02Count;
        public List<TargetView> CurrentGround;
        public bool Hostile;
        public bool SamSites;
        public string SamSitesMode;
        public string SamSitesSelection;
        public TargetView CurrentShip;
        public int CurrentShipCount;
        public bool PassiveShip;
        public CombinedScenarioSettings Scenario;
    }
    // Ask3lad-style search picker: modal search box + instant-filter list.
    // Used for every long list choice (sensors, ammunition, maps, targets...).
    internal sealed class ModernPickerItem
    {
        public string Display { get; set; }
        public string Detail { get; set; }
        public object Tag { get; set; }
        public override string ToString()
        {
            return String.IsNullOrWhiteSpace(Detail) ? Display : Display + "    " + Detail;
        }
    }

    internal sealed class ModernPickerDialog : ModernDialogWindow
    {
        private readonly List<ModernPickerItem> allItems = new List<ModernPickerItem>();
        private readonly List<ModernPickerItem> filtered = new List<ModernPickerItem>();
        private readonly ListBox listBox = new ListBox { Margin = new Thickness(0, 10, 0, 6), Background = ModernPalette.Brush(ModernPalette.Field), BorderBrush = ModernPalette.Brush(ModernPalette.Border), BorderThickness = new Thickness(1), Foreground = ModernPalette.Brush(ModernPalette.Text), Padding = new Thickness(6, 4, 6, 4) };
        private readonly TextBlock countText = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Muted), FontSize = 12, Margin = new Thickness(2, 0, 0, 0) };
        private TextBox searchBox;
        private string searchTerm = "";

        public ModernPickerItem Selected { get; private set; }

        public ModernPickerDialog(string title, IEnumerable<ModernPickerItem> items, string searchPrompt)
            : base(title, 640, 600)
        {
            if (items != null) allItems.AddRange(items);
            ResizeMode = ResizeMode.NoResize;
            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) });
            layout.RowDefinitions.Add(new RowDefinition());
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
            ContentCard.Child = layout;

            // Search row
            StackPanel searchPanel = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
            searchPanel.Children.Add(new TextBlock { Text = ModernText.L(searchPrompt ?? "SEARCH", searchPrompt ?? "搜索"), FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(2, 0, 0, 6) });
            searchBox = new TextBox { Height = 32, Padding = new Thickness(8, 4, 8, 2), Background = ModernPalette.Brush(ModernPalette.Field), Foreground = ModernPalette.Brush(ModernPalette.Text), BorderBrush = ModernPalette.Brush(ModernPalette.Border), BorderThickness = new Thickness(1), CaretBrush = ModernPalette.Brush(ModernPalette.Text) };
            searchBox.TextChanged += delegate { searchTerm = (searchBox.Text ?? "").Trim(); ApplyFilter(); };
            searchBox.PreviewKeyDown += delegate(object s, System.Windows.Input.KeyEventArgs e) { if (e.Key == System.Windows.Input.Key.Escape) { searchBox.Text = ""; e.Handled = true; } };
            searchPanel.Children.Add(searchBox);
            layout.Children.Add(searchPanel);

            // List row
            Grid.SetRow(listBox, 1); layout.Children.Add(listBox);
            listBox.MouseDoubleClick += delegate { ConfirmSelection(); };
            listBox.PreviewKeyDown += delegate(object s, System.Windows.Input.KeyEventArgs e) { if (e.Key == System.Windows.Input.Key.Enter) { ConfirmSelection(); e.Handled = true; } };

            // Count row
            Grid.SetRow(countText, 2); layout.Children.Add(countText);

            // Footer
            Grid footer = new Grid { HorizontalAlignment = HorizontalAlignment.Right };
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            Button cancel = DialogButton(ModernText.L("CANCEL", "取消"), false); cancel.Click += delegate { DialogResult = false; Close(); }; footer.Children.Add(cancel);
            Button select = DialogButton(ModernText.L("SELECT", "选择"), true); Grid.SetColumn(select, 1); select.Click += delegate { ConfirmSelection(); }; footer.Children.Add(select);
            Grid.SetRow(footer, 3); layout.Children.Add(footer);

            ApplyFilter();
            Loaded += delegate { searchBox.Focus(); };
        }

        private void ApplyFilter()
        {
            filtered.Clear();
            string term = searchTerm.ToLowerInvariant();
            if (term.Length == 0)
            {
                filtered.AddRange(allItems);
            }
            else
            {
                foreach (ModernPickerItem item in allItems)
                {
                    if ((item.Display != null && item.Display.ToLowerInvariant().IndexOf(term, StringComparison.Ordinal) >= 0) ||
                        (item.Detail != null && item.Detail.ToLowerInvariant().IndexOf(term, StringComparison.Ordinal) >= 0))
                        filtered.Add(item);
                }
            }
            List<string> rows = new List<string>(filtered.Count);
            foreach (ModernPickerItem item in filtered) rows.Add(item.ToString());
            listBox.ItemsSource = rows;
            if (rows.Count > 0) listBox.SelectedIndex = 0;
            if (filtered.Count == 0 && searchTerm.Length > 0)
                countText.Text = ModernText.L("No matches for \"" + searchTerm + "\" - press Esc to clear.", "没有匹配 \"" + searchTerm + "\" - 按 Esc 清空搜索。");
            else
                countText.Text = filtered.Count + (filtered.Count == allItems.Count ? "" : " / " + allItems.Count) + (filtered.Count == 1 ? " item" : " items");
        }

        private void ConfirmSelection()
        {
            int idx = listBox.SelectedIndex;
            if (idx < 0 || idx >= filtered.Count) return;
            Selected = filtered[idx];
            DialogResult = true;
            Close();
        }
    }

    internal sealed class MapPanelResult
    {
        public AircraftView AirTarget;
        public int AirCount;
        public AircraftView AirTarget01;
        public int AirCount01;
        public AircraftView HeliTarget01;
        public int HeliCount01;
        public AircraftView HeliTarget02;
        public int HeliCount02;
        public List<TargetView> GroundTargets;
        public bool Hostile;
        public bool SamSites;
        public string SamSitesMode;
        public string SamSitesSelection;
        public TargetView ShipTarget;
        public int ShipCount;
        public bool PassiveShip;
        public CombinedScenarioSettings Scenario;
    }

    // Embedded in the main-window TARGETS tab; the standalone Map & Scenario
    // window keeps its own copy of this layout (keep both in sync when editing).
    internal sealed class MapPanel : StackPanel
    {
        private readonly List<TargetView> allGround;
        private readonly List<TargetView> allShips;
        private readonly List<CombinedMap> allCombinedMaps;
        private readonly string playerKind;
        private readonly Style toggleStyle;
        private ComboBox modeBox;
        private ComboBox eraBox;
        private ComboBox mapBox;
        private ComboBox sideBox;
        private ComboBox spawnBox;
        private Border combinedCard;
        private StackPanel targetCards;
        private TextBlock footerHint;
        private ComboBox airBox;
        private ComboBox airCountBox;
        private ComboBox airBox01;
        private ComboBox airCountBox01;
        private ComboBox heliBox01;
        private ComboBox heliCountBox01;
        private ComboBox heliBox02;
        private ComboBox heliCountBox02;
        private readonly List<ComboBox> groundBoxes = new List<ComboBox>();
        private ComboBox groundNation;
        private ComboBox groundRank;
        private ToggleButton hostileBox;
        private ToggleButton samSitesBox;
        private ComboBox samSitesSelectionBox;
        private int samSitesModeState;
        private ComboBox shipBox;
        private ComboBox shipCountBox;
        private ComboBox shipNation;
        private ComboBox shipRank;
        private ToggleButton passiveShipBox;

        private static EraPreset[] EraPresets = LoadEraPresets();

        private static readonly EraPreset[] BuiltinEraPresets = new[]
        {
            new EraPreset("WWI - 1916",
                new[] { "uk_mark_v", "germ_a7v", "uk_mark_v", "germ_a7v", "uk_mark_v", "germ_a7v", "uk_mark_v" },
                new[] { "fokker_d7", "spad_13", null, null }, new[] { 2, 2, 0, 0 },
                "uk_battleship_dreadnought", 1),
            new EraPreset("WWII - 1943",
                new[] { "ussr_t_34_1942", "germ_pzkpfw_vi_ausf_h1_tiger", "us_m4a2_sherman", "germ_pzkpfw_v_ausf_d_panther", "us_m10", "ussr_t_34_1942", "germ_pzkpfw_vi_ausf_h1_tiger" },
                new[] { "bf-109g-2", "bf-109e-3", null, null }, new[] { 2, 2, 0, 0 },
                "germ_battleship_bismarck", 1),
            new EraPreset("GULF WAR - 1991",
                new[] { "us_m1_abrams", "ussr_t_72a", "ussr_t_64a_1971", "us_m60a3_tts", "ussr_t_72av_turms", "us_m1_abrams", "ussr_t_72a" },
                new[] { "f_14a_early", "f_16a_block_10", "mi_24d", "ah_64a" }, new[] { 2, 2, 2, 2 },
                "us_battleship_iowa_class_iowa", 1),
            new EraPreset("MODERN - 2020s",
                new[] { "us_m1a2_abrams", "ussr_t_90a", "cn_ztz_99a", "jp_type_10", "germ_leopard_2a7v", "ussr_t_90m_2020", "us_m1a2_abrams" },
                new[] { "f_16c_block_50", "j_10c", "ka_52", "mi_28nm" }, new[] { 2, 2, 2, 2 },
                "ussr_cruiser_kirov", 1)
        };

        private static EraPreset[] LoadEraPresets()
        {
            try
            {
                List<object> list = ConfigStore.GetList("era_presets");
                if (list != null && list.Count > 0)
                {
                    List<EraPreset> loaded = new List<EraPreset>();
                    foreach (object item in list)
                    {
                        Dictionary<string, object> o = item as Dictionary<string, object>;
                        if (o == null) continue;
                        string name = ModernShellStorage.Str(o, "name");
                        if (String.IsNullOrWhiteSpace(name)) continue;
                        List<object> groundList = ModernShellStorage.ListOf(o, "ground");
                        string[] ground = groundList == null ? new string[0] : groundList.Select(x => x == null ? String.Empty : Convert.ToString(x, CultureInfo.InvariantCulture)).ToArray();
                        List<object> airList = ModernShellStorage.ListOf(o, "air");
                        string[] air = airList == null ? new string[0] : airList.Select(x => x == null ? null : Convert.ToString(x, CultureInfo.InvariantCulture)).ToArray();
                        List<object> counts = ModernShellStorage.ListOf(o, "air_counts");
                        int[] airCounts = counts == null ? new int[0] : counts.Select(x => { int v; Int32.TryParse(x == null ? String.Empty : Convert.ToString(x, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out v); return v; }).ToArray();
                        loaded.Add(new EraPreset(name, ground, air, airCounts, ModernShellStorage.Str(o, "ship"), ModernShellStorage.Int(o, "ship_count", 1)));
                    }
                    if (loaded.Count > 0) return loaded.ToArray();
                }
            }
            catch { }
            return BuiltinEraPresets;
        }

        public MapPanel(MapPanelState state, Style toggleStyleSource)
        {
            allGround = (state.Ground ?? new List<TargetView>()).OrderBy(x => x.Name).ToList();
            allShips = (state.Ships ?? new List<TargetView>()).OrderBy(x => x.Name).ToList();
            allCombinedMaps = (state.CombinedMaps ?? Enumerable.Empty<CombinedMap>()).OrderBy(x => x.Display).ToList();
            playerKind = String.IsNullOrWhiteSpace(state.PlayerKind) ? "aircraft" : state.PlayerKind;
            toggleStyle = toggleStyleSource;
            CombinedScenarioSettings currentScenario = state.Scenario == null ? new CombinedScenarioSettings() : state.Scenario.Copy();
            List<TargetView> selectedGround = (state.CurrentGround ?? Enumerable.Empty<TargetView>()).Take(7).ToList();
            while (selectedGround.Count < 7 && allGround.Count > 0) selectedGround.Add(allGround[Math.Min(selectedGround.Count, allGround.Count - 1)]);

            StackPanel header = new StackPanel();
            Children.Add(header);
            header.Children.Add(Heading("MAP & SCENARIO", 22));
            header.Children.Add(new TextBlock { Text = "Use the clean test range, or a solo combined-battles Domination map with native spawn coordinates.", Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 4, 0, 0) });
            Grid modeLine = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            modeLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            modeLine.ColumnDefinitions.Add(new ColumnDefinition());
            TextBlock modeLabel = Caption("SCENARIO MODE"); modeLabel.VerticalAlignment = VerticalAlignment.Center; modeLine.Children.Add(modeLabel);
            modeBox = new ComboBox { Margin = new Thickness(8, 0, 0, 0) };
            modeBox.Items.Add("Clean Test Range");
            modeBox.Items.Add("Combined Battles — Domination");
            modeBox.SelectedIndex = currentScenario.Enabled ? 1 : 0;
            Grid.SetColumn(modeBox, 1); modeLine.Children.Add(modeBox);
            header.Children.Add(modeLine);
            Grid eraLine = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            eraLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            eraLine.ColumnDefinitions.Add(new ColumnDefinition());
            eraLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
            TextBlock eraLabel = Caption("ERA PRESET"); eraLabel.VerticalAlignment = VerticalAlignment.Center; eraLine.Children.Add(eraLabel);
            eraBox = new ComboBox { Margin = new Thickness(8, 0, 8, 0) };
            eraBox.Items.Add("None (keep current)");
            foreach (EraPreset era in EraPresets) eraBox.Items.Add(era.Name);
            eraBox.SelectedIndex = 0;
            Grid.SetColumn(eraBox, 1); eraLine.Children.Add(eraBox);
            Button savePreset = new Button { Content = ModernText.L("SAVE CURRENT AS PRESET", "保存当前为预设"), Style = toggleStyleSource == null ? null : ButtonStyleFrom(toggleStyleSource), Margin = new Thickness(4, 0, 0, 0), Padding = new Thickness(14, 2, 14, 2) };
            savePreset.Click += delegate { SavePresetClicked(); };
            Grid.SetColumn(savePreset, 2); eraLine.Children.Add(savePreset);
            header.Children.Add(eraLine);

            StackPanel content = new StackPanel();

            combinedCard = SectionCard();
            StackPanel combinedPanel = new StackPanel();
            combinedPanel.Children.Add(Heading("SOLO COMBINED-BATTLES SPAWN", 15));
            combinedPanel.Children.Add(new TextBlock
            {
                Text = "Uses extracted native Domination spawn coordinates. Only your configured vehicle is created; AI units are not added.",
                Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 12)
            });
            Grid combinedFields = new Grid();
            combinedFields.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            combinedFields.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            combinedFields.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
            StackPanel mapStack = new StackPanel { Margin = new Thickness(0, 0, 8, 0) }; mapStack.Children.Add(Caption("MAP"));
            mapBox = new ComboBox { ItemsSource = allCombinedMaps, Margin = new Thickness(0, 6, 0, 0) };
            mapBox.SelectedItem = allCombinedMaps.FirstOrDefault(x => x.Id.Equals(currentScenario.MapId ?? "", StringComparison.OrdinalIgnoreCase)) ?? allCombinedMaps.FirstOrDefault();
            mapStack.Children.Add(mapBox); combinedFields.Children.Add(mapStack);
            StackPanel sideStack = new StackPanel { Margin = new Thickness(0, 0, 8, 0) }; sideStack.Children.Add(Caption("SIDE"));
            sideBox = new ComboBox { ItemsSource = new[] { "Side 1", "Side 2" }, SelectedIndex = currentScenario.Side == 2 ? 1 : 0, Margin = new Thickness(0, 6, 0, 0) };
            sideStack.Children.Add(sideBox); Grid.SetColumn(sideStack, 1); combinedFields.Children.Add(sideStack);
            StackPanel spawnStack = new StackPanel(); spawnStack.Children.Add(Caption("SPAWN"));
            spawnBox = new ComboBox { Margin = new Thickness(0, 6, 0, 0), Tag = currentScenario.SpawnOption };
            spawnStack.Children.Add(spawnBox); Grid.SetColumn(spawnStack, 2); combinedFields.Children.Add(spawnStack);
            combinedPanel.Children.Add(combinedFields);
            combinedCard.Child = combinedPanel;
            content.Children.Add(combinedCard);

            targetCards = new StackPanel();
            content.Children.Add(targetCards);

            Border airCard = SectionCard();
            StackPanel airPanel = new StackPanel();
            List<AircraftView> airChoices = (state.Aircraft ?? new List<AircraftView>()).OrderBy(x => x.Name).ToList();
            AddFlyingRow(airPanel, "AIR TARGET01", airChoices, state.CurrentAir01, state.CurrentAir01Count, out airBox01, out airCountBox01, "ef_2000_typhoon_aesa");
            AddFlyingRow(airPanel, "AIR TARGET02", airChoices, state.CurrentAir, state.CurrentAirCount, out airBox, out airCountBox, null);
            AddFlyingRow(airPanel, "HELI TARGET01", airChoices, state.CurrentHeli01, state.CurrentHeli01Count, out heliBox01, out heliCountBox01, "mi_28nm");
            AddFlyingRow(airPanel, "HELI TARGET02", airChoices, state.CurrentHeli02, state.CurrentHeli02Count, out heliBox02, out heliCountBox02, "ka_52");
            airCard.Child = airPanel; targetCards.Children.Add(airCard);

            Border groundCard = SectionCard();
            StackPanel groundPanel = new StackPanel();
            Grid groundHeader = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            groundHeader.ColumnDefinitions.Add(new ColumnDefinition());
            groundHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            groundHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
            groundHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
            groundHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) });
            TextBlock groundTitle = Heading("GROUND RANGE POSITIONS", 15); groundTitle.VerticalAlignment = VerticalAlignment.Center; groundHeader.Children.Add(groundTitle);
            groundNation = FilterBox(allGround.Select(x => x.Nation), ModernText.L("All Nations", "全部国家")); Grid.SetColumn(groundNation, 1); groundHeader.Children.Add(groundNation);
            groundRank = RankBox(allGround); groundRank.Margin = new Thickness(8, 0, 0, 0); Grid.SetColumn(groundRank, 2); groundHeader.Children.Add(groundRank);
            hostileBox = new ToggleButton { IsChecked = state.Hostile, Style = toggleStyle, Margin = new Thickness(8, 0, 0, 0), ToolTip = "Controls whether all seven selected ground targets actively aim at and fire on the player." }; Grid.SetColumn(hostileBox, 3); groundHeader.Children.Add(hostileBox);
            samSitesBox = new ToggleButton { IsChecked = true, Style = toggleStyle, Margin = new Thickness(8, 0, 0, 0), ToolTip = "Cycles the clean-range SAM sites: ACTIVE (engage the player), PASSIVE (deployed but never attack), FRIENDLY (army 1, intercepts enemy air targets), DISABLED (not spawned)." };
            samSitesBox.Click += delegate { samSitesModeState = (samSitesModeState + 1) % 4; UpdateReactionButtons(); };
            samSitesSelectionBox = new ComboBox { Width = 150, VerticalAlignment = VerticalAlignment.Center };
            samSitesSelectionBox.Items.Add("S300");
            samSitesSelectionBox.Items.Add("PATRIOT");
            samSitesSelectionBox.Items.Add("HAWK");
            samSitesSelectionBox.Items.Add("BUK");
            samSitesSelectionBox.Items.Add("ALL");
            string initialSamSelection = String.IsNullOrWhiteSpace(state.SamSitesSelection) ? "s300" : state.SamSitesSelection;
            samSitesSelectionBox.SelectedIndex = Math.Max(0, Math.Min(4, new[] { "S300", "PATRIOT", "HAWK", "BUK", "ALL" }.ToList().IndexOf(initialSamSelection.ToUpperInvariant())));
            samSitesModeState = state.SamSitesMode == "passive" ? 1 : state.SamSitesMode == "friendly" ? 2 : state.SamSitesMode == "disabled" ? 3 : (state.SamSites ? 0 : 3);
            Grid samRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            samRow.ColumnDefinitions.Add(new ColumnDefinition());
            samRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            samRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) });
            TextBlock samCaption = Heading("SAM SITES", 15); samCaption.VerticalAlignment = VerticalAlignment.Center; samRow.Children.Add(samCaption);
            Grid.SetColumn(samSitesSelectionBox, 1); samRow.Children.Add(samSitesSelectionBox);
            Grid.SetColumn(samSitesBox, 2); samRow.Children.Add(samSitesBox);
            groundPanel.Children.Add(groundHeader);
            groundPanel.Children.Add(samRow);

            Grid groundGrid = new Grid();
            groundGrid.ColumnDefinitions.Add(new ColumnDefinition());
            groundGrid.ColumnDefinitions.Add(new ColumnDefinition());
            for (int row = 0; row < 4; row++) groundGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(68) });
            for (int index = 0; index < 7; index++)
            {
                StackPanel slot = new StackPanel { Margin = new Thickness(index % 2 == 0 ? 0 : 8, 0, index % 2 == 0 ? 8 : 0, 8) };
                slot.Children.Add(Caption("POSITION " + (index + 1).ToString("00", CultureInfo.InvariantCulture)));
                ComboBox box = new ComboBox { ItemsSource = allGround, SelectedItem = selectedGround.Count > index ? selectedGround[index] : null, Margin = new Thickness(0, 5, 0, 0) };
                groundBoxes.Add(box); slot.Children.Add(box);
                Grid.SetColumn(slot, index % 2); Grid.SetRow(slot, index / 2); groundGrid.Children.Add(slot);
            }
            groundPanel.Children.Add(groundGrid);
            groundCard.Child = groundPanel; targetCards.Children.Add(groundCard);

            Border shipCard = SectionCard();
            StackPanel shipPanel = new StackPanel();
            Grid shipFilters = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            shipFilters.ColumnDefinitions.Add(new ColumnDefinition());
            shipFilters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            shipFilters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
            TextBlock shipTitle = Heading("NAVAL TARGET", 15); shipTitle.VerticalAlignment = VerticalAlignment.Center; shipFilters.Children.Add(shipTitle);
            shipNation = FilterBox(allShips.Select(x => x.Nation), ModernText.L("All Nations", "全部国家")); Grid.SetColumn(shipNation, 1); shipFilters.Children.Add(shipNation);
            shipRank = RankBox(allShips); shipRank.Margin = new Thickness(8, 0, 0, 0); Grid.SetColumn(shipRank, 2); shipFilters.Children.Add(shipRank);
            shipPanel.Children.Add(shipFilters);
            Grid shipLine = new Grid(); shipLine.ColumnDefinitions.Add(new ColumnDefinition()); shipLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) }); shipLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            shipBox = new ComboBox { ItemsSource = allShips, SelectedItem = state.CurrentShip, Margin = new Thickness(0, 0, 8, 0) }; shipLine.Children.Add(shipBox);
            shipCountBox = CountBox(state.CurrentShipCount); Grid.SetColumn(shipCountBox, 1); shipLine.Children.Add(shipCountBox);
            passiveShipBox = new ToggleButton { IsChecked = state.PassiveShip, Style = toggleStyle, Margin = new Thickness(8, 0, 0, 0), ToolTip = "Controls whether the naval target stays passive or returns fire after the player attacks it." }; Grid.SetColumn(passiveShipBox, 2); shipLine.Children.Add(passiveShipBox);
            shipPanel.Children.Add(shipLine); shipCard.Child = shipPanel; targetCards.Children.Add(shipCard);

            groundNation.SelectionChanged += delegate { RefreshGround(); };
            groundRank.SelectionChanged += delegate { RefreshGround(); };
            shipNation.SelectionChanged += delegate { RefreshShips(); };
            shipRank.SelectionChanged += delegate { RefreshShips(); };
            hostileBox.Checked += delegate { UpdateReactionButtons(); };
            hostileBox.Unchecked += delegate { UpdateReactionButtons(); };
            passiveShipBox.Checked += delegate { UpdateReactionButtons(); };
            passiveShipBox.Unchecked += delegate { UpdateReactionButtons(); };
            modeBox.SelectionChanged += delegate { UpdateScenarioMode(); };
            mapBox.SelectionChanged += delegate { RefreshCombinedSpawns(); };
            sideBox.SelectionChanged += delegate { RefreshCombinedSpawns(); };
            eraBox.SelectionChanged += delegate { ApplyEraPreset(); };
            UpdateReactionButtons();
            footerHint = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Muted), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) };
            Children.Add(footerHint);
            Children.Add(content);
            RefreshCombinedSpawns();
            UpdateScenarioMode();
        }

        private static Style ButtonStyleFrom(Style source)
        {
            return source;
        }

        private void UpdateScenarioMode()
        {
            bool combined = modeBox.SelectedIndex == 1;
            combinedCard.Visibility = combined ? Visibility.Visible : Visibility.Collapsed;
            targetCards.Visibility = combined ? Visibility.Collapsed : Visibility.Visible;
            footerHint.Text = combined
                ? "The mission contains only your vehicle, the selected spawn base and instant player respawn."
                : "Destroyed targets recover rapidly; player ammunition rearms after depletion.";
        }

        private void RefreshCombinedSpawns()
        {
            CombinedMap map = mapBox.SelectedItem as CombinedMap;
            int side = sideBox.SelectedIndex == 1 ? 2 : 1;
            string preferred = spawnBox.SelectedItem is CombinedSpawn ? ((CombinedSpawn)spawnBox.SelectedItem).Option : spawnBox.Tag as string;
            List<CombinedSpawn> values = map == null ? new List<CombinedSpawn>() : map.Spawns
                .Where(x => x.Side == side && x.Kind.Equals(playerKind, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Option.Equals("airfield", StringComparison.OrdinalIgnoreCase) || x.Option.Equals("ground_1", StringComparison.OrdinalIgnoreCase) || x.Option.Equals("heli_near", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(x => x.Option, StringComparer.OrdinalIgnoreCase).ToList();
            spawnBox.ItemsSource = values;
            spawnBox.SelectedItem = values.FirstOrDefault(x => x.Option.Equals(preferred ?? "", StringComparison.OrdinalIgnoreCase)) ?? values.FirstOrDefault();
            spawnBox.Tag = null;
        }

        private Border SectionCard()
        {
            return new Border { CornerRadius = new CornerRadius(14), Background = ModernPalette.Brush(ModernPalette.Field), BorderBrush = ModernPalette.Brush(ModernPalette.Border), BorderThickness = new Thickness(1), Padding = new Thickness(16), Margin = new Thickness(0, 0, 0, 10) };
        }

        private ComboBox CountBox(int value)
        {
            return new ComboBox { ItemsSource = Enumerable.Range(0, 21).ToList(), SelectedItem = Math.Max(0, Math.Min(20, value)) };
        }

        private void SavePresetClicked()
        {
            string suggested = String.Format(CultureInfo.InvariantCulture, "CUSTOM - {0:MMdd-HHmm}", DateTime.Now);
            ModernInputWindow input = new ModernInputWindow("SAVE ERA PRESET", "Name this preset. It is stored in config.json (era_presets) and appears in the list next time UTL starts.", suggested);
            if (input.ShowDialog() != true) return;
            string name = input.Value == null ? null : input.Value.Trim();
            if (String.IsNullOrWhiteSpace(name)) return;
            List<string> groundIds = new List<string>();
            foreach (ComboBox box in groundBoxes)
            {
                TargetView view = box.SelectedItem as TargetView;
                groundIds.Add(view == null || view.Source == null ? String.Empty : view.Source.Id);
            }
            ComboBox[] airBoxes = new[] { airBox01, airBox, heliBox01, heliBox02 };
            ComboBox[] countBoxes = new[] { airCountBox01, airCountBox, heliCountBox01, heliCountBox02 };
            string[] airIds = new string[4];
            int[] airCounts = new int[4];
            for (int i = 0; i < 4; i++)
            {
                AircraftView view = airBoxes[i].SelectedItem as AircraftView;
                airIds[i] = view == null || view.Source == null ? null : view.Source.Id;
                int count = 0;
                object countSel = countBoxes[i].SelectedItem;
                if (countSel is int) count = (int)countSel;
                else if (countSel != null) Int32.TryParse(countSel.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out count);
                airCounts[i] = count;
            }
            TargetView shipView = shipBox.SelectedItem as TargetView;
            string shipId = shipView == null || shipView.Source == null ? String.Empty : shipView.Source.Id;
            int shipCount = 0;
            object shipSel = shipCountBox.SelectedItem;
            if (shipSel is int) shipCount = (int)shipSel;
            else if (shipSel != null) Int32.TryParse(shipSel.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out shipCount);
            try
            {
                List<object> list = ConfigStore.GetList("era_presets") ?? new List<object>();
                if (list.Count == 0 && BuiltinEraPresets != null)
                {
                    foreach (EraPreset builtin in BuiltinEraPresets)
                    {
                        Dictionary<string, object> bo = new Dictionary<string, object>();
                        bo.Add("name", builtin.Name);
                        bo.Add("ground", builtin.GroundIds == null ? new List<object>() : builtin.GroundIds.Select(x => (object)(x ?? String.Empty)).ToList());
                        bo.Add("air", builtin.AirIds == null ? new List<object>() : builtin.AirIds.Select(x => x == null ? null : (object)x).ToList());
                        bo.Add("air_counts", builtin.AirCounts == null ? new List<object>() : builtin.AirCounts.Select(x => (object)x).ToList());
                        bo.Add("ship", builtin.ShipId ?? String.Empty);
                        bo.Add("ship_count", builtin.ShipCount);
                        list.Add(bo);
                    }
                }
                Dictionary<string, object> o = new Dictionary<string, object>();
                o.Add("name", name);
                o.Add("ground", groundIds.Select(x => (object)x).ToList());
                o.Add("air", airIds.Select(x => x == null ? null : (object)x).ToList());
                o.Add("air_counts", airCounts.Select(x => (object)x).ToList());
                o.Add("ship", shipId);
                o.Add("ship_count", shipCount);
                list.Add(o);
                ConfigStore.SetList("era_presets", list);
                ConfigStore.Save();
            }
            catch { }
            ReloadEraPresets();
            eraBox.SelectedIndex = 0;
        }

        private void ReloadEraPresets()
        {
            if (eraBox == null) return;
            EraPresets = LoadEraPresets();
            eraBox.Items.Clear();
            eraBox.Items.Add("None (keep current)");
            foreach (EraPreset era in EraPresets) eraBox.Items.Add(era.Name);
            eraBox.SelectedIndex = 0;
        }

        private void ApplyEraPreset()
        {
            if (eraBox == null || eraBox.SelectedIndex <= 0) return;
            EraPreset preset = EraPresets[Math.Min(eraBox.SelectedIndex - 1, EraPresets.Length - 1)];
            for (int i = 0; i < groundBoxes.Count && i < preset.GroundIds.Length; i++)
            {
                TargetView match = allGround.FirstOrDefault(x => x.Source.Id != null && x.Source.Id.Equals(preset.GroundIds[i], StringComparison.OrdinalIgnoreCase));
                if (match != null) groundBoxes[i].SelectedItem = match;
            }
            SetFlyingPreset(airBox01, airCountBox01, preset.AirIds.Length > 0 ? preset.AirIds[0] : null, preset.AirCounts.Length > 0 ? preset.AirCounts[0] : 0);
            SetFlyingPreset(airBox, airCountBox, preset.AirIds.Length > 1 ? preset.AirIds[1] : null, preset.AirCounts.Length > 1 ? preset.AirCounts[1] : 0);
            SetFlyingPreset(heliBox01, heliCountBox01, preset.AirIds.Length > 2 ? preset.AirIds[2] : null, preset.AirCounts.Length > 2 ? preset.AirCounts[2] : 0);
            SetFlyingPreset(heliBox02, heliCountBox02, preset.AirIds.Length > 3 ? preset.AirIds[3] : null, preset.AirCounts.Length > 3 ? preset.AirCounts[3] : 0);
            if (!String.IsNullOrWhiteSpace(preset.ShipId))
            {
                TargetView ship = allShips.FirstOrDefault(x => x.Source.Id != null && x.Source.Id.Equals(preset.ShipId, StringComparison.OrdinalIgnoreCase));
                if (ship != null) shipBox.SelectedItem = ship;
                shipCountBox.SelectedItem = Math.Max(0, Math.Min(20, preset.ShipCount));
            }
        }

        private void SetFlyingPreset(ComboBox box, ComboBox countBox, string id, int count)
        {
            if (box == null || countBox == null) return;
            countBox.SelectedItem = Math.Max(0, Math.Min(20, count));
            if (String.IsNullOrWhiteSpace(id)) return;
            IEnumerable<AircraftView> source = box.ItemsSource as IEnumerable<AircraftView>;
            AircraftView match = source == null ? null : source.FirstOrDefault(x => x.Source.Id != null && x.Source.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (match != null) box.SelectedItem = match;
        }

        private void AddFlyingRow(StackPanel host, string caption, List<AircraftView> source, AircraftView current, int count, out ComboBox box, out ComboBox countBox, string templateDefaultId)
        {
            Grid row = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(84) });
            TextBlock label = new TextBlock { Text = caption, Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 12, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
            if (!String.IsNullOrWhiteSpace(templateDefaultId)) label.ToolTip = ModernText.L("Template default: ", "模板默认: ") + templateDefaultId;
            row.Children.Add(label);
            box = new ComboBox { ItemsSource = source, SelectedItem = current, Margin = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
            if (box.SelectedItem == null && source.Count > 0) box.SelectedIndex = 0;
            Grid.SetColumn(box, 1); row.Children.Add(box);
            countBox = CountBox(count); Grid.SetColumn(countBox, 2); row.Children.Add(countBox);
            host.Children.Add(row);
        }

        private ComboBox FilterBox(IEnumerable<string> values, string all)
        {
            ComboBox box = new ComboBox();
            box.Items.Add(all);
            foreach (string value in values.Where(x => !String.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x)) box.Items.Add(value);
            box.SelectedIndex = 0;
            return box;
        }

        private ComboBox RankBox(IEnumerable<TargetView> values)
        {
            ComboBox box = new ComboBox();
            box.Items.Add(ModernText.L("Any Rank", "任意等级"));
            foreach (int rank in values.Select(x => x.Rank).Where(x => x > 0).Distinct().OrderBy(x => x)) box.Items.Add(rank);
            box.SelectedIndex = 0;
            return box;
        }

        private IEnumerable<TargetView> ApplyFilter(IEnumerable<TargetView> source, ComboBox nation, ComboBox rank)
        {
            string selectedNation = nation.SelectedIndex > 0 ? nation.SelectedItem as string : null;
            int selectedRank = rank.SelectedItem is int ? (int)rank.SelectedItem : 0;
            if (!String.IsNullOrEmpty(selectedNation)) source = source.Where(x => x.Nation == selectedNation);
            if (selectedRank > 0) source = source.Where(x => x.Rank == selectedRank);
            return source.OrderBy(x => x.Name).ToList();
        }

        private void RefreshGround()
        {
            List<TargetView> values = ApplyFilter(allGround, groundNation, groundRank).ToList();
            foreach (ComboBox box in groundBoxes)
            {
                TargetView keep = box.SelectedItem as TargetView;
                box.ItemsSource = values;
                box.SelectedItem = keep != null && values.Any(x => x.Source.Id == keep.Source.Id) ? values.First(x => x.Source.Id == keep.Source.Id) : values.FirstOrDefault();
            }
        }

        private void RefreshShips()
        {
            TargetView keep = shipBox.SelectedItem as TargetView;
            List<TargetView> values = ApplyFilter(allShips, shipNation, shipRank).ToList();
            shipBox.ItemsSource = values;
            shipBox.SelectedItem = keep != null && values.Any(x => x.Source.Id == keep.Source.Id) ? values.First(x => x.Source.Id == keep.Source.Id) : values.FirstOrDefault();
        }

        private void UpdateReactionButtons()
        {
            bool groundAttacks = hostileBox.IsChecked == true;
            hostileBox.Content = groundAttacks ? "GROUND TARGETS — ATTACKING" : "GROUND TARGETS — PASSIVE";
            hostileBox.Background = ModernPalette.Brush(groundAttacks ? "#A34B1733" : "#8A1D5148");
            hostileBox.BorderBrush = ModernPalette.Brush(groundAttacks ? ModernPalette.Danger : ModernPalette.Good);
            hostileBox.Foreground = ModernPalette.Brush(groundAttacks ? "#FFFFE7EF" : "#FFE7FFF7");

            bool samsActive = samSitesModeState != 3;
            samSitesBox.Content = samsActive ? (samSitesModeState == 1 ? "SAM SITES PASSIVE" : samSitesModeState == 2 ? "SAM SITES FRIENDLY" : "SAM SITES ACTIVE") : "SAM SITES DISABLED";
            samSitesBox.Background = ModernPalette.Brush(samsActive ? (samSitesModeState == 1 ? "#8A5A1D48" : samSitesModeState == 2 ? "#8A1D4A48" : "#A34B1733") : "#8A1D5148");
            samSitesBox.BorderBrush = ModernPalette.Brush(samsActive ? (samSitesModeState == 1 ? "#FFE0A030" : samSitesModeState == 2 ? "#FF50C8A0" : ModernPalette.Danger) : ModernPalette.Good);
            samSitesBox.Foreground = ModernPalette.Brush(samsActive ? "#FFFFE7EF" : "#FFE7FFF7");

            bool shipPassive = passiveShipBox.IsChecked == true;
            passiveShipBox.Content = shipPassive ? "SHIP — STAYS PASSIVE" : "SHIP — RETURNS FIRE";
            passiveShipBox.Background = ModernPalette.Brush(shipPassive ? "#8A1D5148" : "#A34B1733");
            passiveShipBox.BorderBrush = ModernPalette.Brush(shipPassive ? ModernPalette.Good : ModernPalette.Danger);
            passiveShipBox.Foreground = ModernPalette.Brush(shipPassive ? "#FFE7FFF7" : "#FFFFE7EF");
        }

        private TextBlock Caption(string text)
        {
            return new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ModernPalette.Brush(ModernPalette.Muted) };
        }

        private TextBlock Heading(string text, double size)
        {
            return new TextBlock { Text = text, FontSize = size, FontWeight = FontWeights.SemiBold, Foreground = ModernPalette.Brush(ModernPalette.Text) };
        }

        public MapPanelResult Collect()
        {
            MapPanelResult r = new MapPanelResult();
            r.AirTarget01 = airBox01.SelectedItem as AircraftView;
            r.AirCount01 = (int)(airCountBox01.SelectedItem ?? 0);
            r.AirTarget = airBox.SelectedItem as AircraftView;
            r.AirCount = (int)(airCountBox.SelectedItem ?? 0);
            r.HeliTarget01 = heliBox01.SelectedItem as AircraftView;
            r.HeliCount01 = (int)(heliCountBox01.SelectedItem ?? 0);
            r.HeliTarget02 = heliBox02.SelectedItem as AircraftView;
            r.HeliCount02 = (int)(heliCountBox02.SelectedItem ?? 0);
            r.GroundTargets = groundBoxes.Select(x => x.SelectedItem as TargetView).Where(x => x != null).ToList();
            r.Hostile = hostileBox.IsChecked == true;
            r.SamSitesMode = samSitesModeState == 0 ? "active" : samSitesModeState == 1 ? "passive" : samSitesModeState == 2 ? "friendly" : "disabled";
            r.SamSitesSelection = (samSitesSelectionBox.SelectedItem as string ?? "S300").ToLowerInvariant();
            r.SamSites = samSitesModeState != 3;
            r.ShipTarget = shipBox.SelectedItem as TargetView;
            r.ShipCount = (int)(shipCountBox.SelectedItem ?? 0);
            r.PassiveShip = passiveShipBox.IsChecked == true;
            CombinedMap map = mapBox.SelectedItem as CombinedMap;
            CombinedSpawn spawn = spawnBox.SelectedItem as CombinedSpawn;
            r.Scenario = new CombinedScenarioSettings
            {
                Enabled = modeBox.SelectedIndex == 1,
                MapId = map == null ? null : map.Id,
                Side = sideBox.SelectedIndex == 1 ? 2 : 1,
                SpawnOption = spawn == null ? null : spawn.Option
            };
            if (r.Scenario.Enabled && (map == null || spawn == null)) return null;
            return r;
        }
    }

    internal sealed class ModernMapWindow : ModernDialogWindow
    {
        private readonly List<TargetView> allGround;
        private readonly List<TargetView> allShips;
        private readonly List<CombinedMap> allCombinedMaps;
        private readonly string playerKind;
        private readonly ComboBox modeBox;
        private readonly ComboBox eraBox;
        private readonly ComboBox mapBox;
        private readonly ComboBox sideBox;
        private readonly ComboBox spawnBox;
        private readonly Border combinedCard;
        private readonly StackPanel targetCards;
        private readonly TextBlock footerHint;
        private readonly ComboBox airBox;
        private readonly ComboBox airCountBox;
        private readonly ComboBox airBox01;
        private readonly ComboBox airCountBox01;
        private readonly ComboBox heliBox01;
        private readonly ComboBox heliCountBox01;
        private readonly ComboBox heliBox02;
        private readonly ComboBox heliCountBox02;
        private readonly List<ComboBox> groundBoxes = new List<ComboBox>();
        private readonly ComboBox groundNation;
        private readonly ComboBox groundRank;
        private readonly ToggleButton hostileBox;
        private readonly ToggleButton samSitesBox;
        private readonly ComboBox samSitesSelectionBox;
        private int samSitesModeState;
        private readonly ComboBox shipBox;
        private readonly ComboBox shipCountBox;
        private readonly ComboBox shipNation;
        private readonly ComboBox shipRank;
        private readonly ToggleButton passiveShipBox;

        public AircraftView AirTarget { get; private set; }
        public int AirCount { get; private set; }
        public AircraftView AirTarget01 { get; private set; }
        public int AirCount01 { get; private set; }
        public AircraftView HeliTarget01 { get; private set; }
        public int HeliCount01 { get; private set; }
        public AircraftView HeliTarget02 { get; private set; }
        public int HeliCount02 { get; private set; }
        public IList<TargetView> GroundTargets { get; private set; }
        public bool Hostile { get; private set; }
        public bool SamSites { get; private set; }
        public string SamSitesMode { get; private set; }
        public string SamSitesSelection { get; private set; }
        public TargetView ShipTarget { get; private set; }
        public int ShipCount { get; private set; }
        public bool PassiveShip { get; private set; }
        public CombinedScenarioSettings Scenario { get; private set; }

        private static EraPreset[] EraPresets = LoadEraPresets();

        private static readonly EraPreset[] BuiltinEraPresets = new[]
        {
            new EraPreset("WWI - 1916",
                new[] { "uk_mark_v", "germ_a7v", "uk_mark_v", "germ_a7v", "uk_mark_v", "germ_a7v", "uk_mark_v" },
                new[] { "fokker_d7", "spad_13", null, null }, new[] { 2, 2, 0, 0 },
                "uk_battleship_dreadnought", 1),
            new EraPreset("WWII - 1943",
                new[] { "ussr_t_34_1942", "germ_pzkpfw_vi_ausf_h1_tiger", "us_m4a2_sherman", "germ_pzkpfw_v_ausf_d_panther", "us_m10", "ussr_t_34_1942", "germ_pzkpfw_vi_ausf_h1_tiger" },
                new[] { "bf-109g-2", "bf-109e-3", null, null }, new[] { 2, 2, 0, 0 },
                "germ_battleship_bismarck", 1),
            new EraPreset("GULF WAR - 1991",
                new[] { "us_m1_abrams", "ussr_t_72a", "ussr_t_64a_1971", "us_m60a3_tts", "ussr_t_72av_turms", "us_m1_abrams", "ussr_t_72a" },
                new[] { "f_14a_early", "f_16a_block_10", "mi_24d", "ah_64a" }, new[] { 2, 2, 2, 2 },
                "us_battleship_iowa_class_iowa", 1),
            new EraPreset("MODERN - 2020s",
                new[] { "us_m1a2_abrams", "ussr_t_90a", "cn_ztz_99a", "jp_type_10", "germ_leopard_2a7v", "ussr_t_90m_2020", "us_m1a2_abrams" },
                new[] { "f_16c_block_50", "j_10c", "ka_52", "mi_28nm" }, new[] { 2, 2, 2, 2 },
                "ussr_cruiser_kirov", 1)
        };

        private static EraPreset[] LoadEraPresets()
        {
            try
            {
                List<object> list = ConfigStore.GetList("era_presets");
                if (list != null && list.Count > 0)
                {
                    List<EraPreset> loaded = new List<EraPreset>();
                    foreach (object item in list)
                    {
                        Dictionary<string, object> o = item as Dictionary<string, object>;
                        if (o == null) continue;
                        string name = ModernShellStorage.Str(o, "name");
                        if (String.IsNullOrWhiteSpace(name)) continue;
                        List<object> groundList = ModernShellStorage.ListOf(o, "ground");
                        string[] ground = groundList == null ? new string[0] : groundList.Select(x => x == null ? String.Empty : Convert.ToString(x, CultureInfo.InvariantCulture)).ToArray();
                        List<object> airList = ModernShellStorage.ListOf(o, "air");
                        string[] air = airList == null ? new string[0] : airList.Select(x => x == null ? null : Convert.ToString(x, CultureInfo.InvariantCulture)).ToArray();
                        List<object> counts = ModernShellStorage.ListOf(o, "air_counts");
                        int[] airCounts = counts == null ? new int[0] : counts.Select(x => { int v; Int32.TryParse(x == null ? String.Empty : Convert.ToString(x, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out v); return v; }).ToArray();
                        loaded.Add(new EraPreset(name, ground, air, airCounts, ModernShellStorage.Str(o, "ship"), ModernShellStorage.Int(o, "ship_count", 1)));
                    }
                    if (loaded.Count > 0) return loaded.ToArray();
                }
            }
            catch { }
            return BuiltinEraPresets;
        }

        private static List<EraPreset> ParseEraPresets(string text)
        {
            List<EraPreset> loaded = new List<EraPreset>();
            if (String.IsNullOrWhiteSpace(text)) return loaded;
            string[] lines = text.Replace("\r", "").Split('\n');
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (String.IsNullOrWhiteSpace(line)) continue;
                string[] parts = line.Split('\t');
                if (parts.Length < 6) continue;
                string[] groundIds = parts[1].Trim().Split(',').Select(x => x.Trim()).ToArray();
                string[] airIds = parts[2].Trim().Split(',').Select(x => x.Trim() == "-" ? null : x.Trim()).ToArray();
                int[] airCounts;
                if (!TryParseIntList(parts[3], out airCounts)) continue;
                int shipCount;
                if (!Int32.TryParse(parts[5].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out shipCount)) shipCount = 1;
                loaded.Add(new EraPreset(parts[0].Trim(), groundIds, airIds, airCounts, parts[4].Trim(), shipCount));
            }
            return loaded;
        }

        private static bool TryParseIntList(string text, out int[] values)
        {
            values = new int[0];
            if (String.IsNullOrWhiteSpace(text)) return false;
            string[] parts = text.Split(',');
            int[] result = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                int v;
                if (!Int32.TryParse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) return false;
                result[i] = v;
            }
            values = result;
            return true;
        }

        public ModernMapWindow(IEnumerable<AircraftView> aircraft, IEnumerable<TargetView> ground, IEnumerable<TargetView> ships,
            AircraftView currentAir, int currentAirCount, IEnumerable<TargetView> currentGround, bool hostile, string samSitesMode, string samSitesSelection,
            TargetView currentShip, int currentShipCount, bool passiveShip, IEnumerable<CombinedMap> combinedMaps,
            string currentPlayerKind, CombinedScenarioSettings currentScenario, AircraftView currentAir01, int currentAir01Count,
            AircraftView currentHeli01, int currentHeli01Count, AircraftView currentHeli02, int currentHeli02Count) : base("Map & Scenario", 1000, 820)
        {
            allGround = ground.OrderBy(x => x.Name).ToList();
            allShips = ships.OrderBy(x => x.Name).ToList();
            allCombinedMaps = (combinedMaps ?? Enumerable.Empty<CombinedMap>()).OrderBy(x => x.Display).ToList();
            playerKind = String.IsNullOrWhiteSpace(currentPlayerKind) ? "aircraft" : currentPlayerKind;
            currentScenario = currentScenario == null ? new CombinedScenarioSettings() : currentScenario.Copy();
            List<TargetView> selectedGround = (currentGround ?? Enumerable.Empty<TargetView>()).Take(7).ToList();
            while (selectedGround.Count < 7 && allGround.Count > 0) selectedGround.Add(allGround[Math.Min(selectedGround.Count, allGround.Count - 1)]);

            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(152) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
            ContentCard.Child = layout;
            StackPanel header = new StackPanel();
            header.Children.Add(Heading("MAP & SCENARIO", 22));
            header.Children.Add(new TextBlock { Text = "Use the clean test range, or a solo combined-battles Domination map with native spawn coordinates.", Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 4, 0, 0) });
            Grid modeLine = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            modeLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            modeLine.ColumnDefinitions.Add(new ColumnDefinition());
            TextBlock modeLabel = Caption("SCENARIO MODE"); modeLabel.VerticalAlignment = VerticalAlignment.Center; modeLine.Children.Add(modeLabel);
            modeBox = new ComboBox { Margin = new Thickness(8, 0, 0, 0) };
            modeBox.Items.Add("Clean Test Range");
            modeBox.Items.Add("Combined Battles — Domination");
            modeBox.SelectedIndex = currentScenario.Enabled ? 1 : 0;
            Grid.SetColumn(modeBox, 1); modeLine.Children.Add(modeBox);
            header.Children.Add(modeLine);
            Grid eraLine = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            eraLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            eraLine.ColumnDefinitions.Add(new ColumnDefinition());
            eraLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
            TextBlock eraLabel = Caption("ERA PRESET"); eraLabel.VerticalAlignment = VerticalAlignment.Center; eraLine.Children.Add(eraLabel);
            eraBox = new ComboBox { Margin = new Thickness(8, 0, 8, 0) };
            eraBox.Items.Add("None (keep current)");
            foreach (EraPreset era in EraPresets) eraBox.Items.Add(era.Name);
            eraBox.SelectedIndex = 0;
            Grid.SetColumn(eraBox, 1); eraLine.Children.Add(eraBox);
            Button savePreset = DialogButton(ModernText.L("SAVE CURRENT AS PRESET", "保存当前为预设"), false);
            savePreset.Click += delegate { SavePresetClicked(); };
            Grid.SetColumn(savePreset, 2); eraLine.Children.Add(savePreset);
            header.Children.Add(eraLine);
            layout.Children.Add(header);

            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(0, 6, 0, 8) };
            StackPanel content = new StackPanel();
            scroll.Content = content;
            Grid.SetRow(scroll, 1);
            layout.Children.Add(scroll);

            combinedCard = SectionCard();
            StackPanel combinedPanel = new StackPanel();
            combinedPanel.Children.Add(Heading("SOLO COMBINED-BATTLES SPAWN", 15));
            combinedPanel.Children.Add(new TextBlock
            {
                Text = "Uses extracted native Domination spawn coordinates. Only your configured vehicle is created; AI units are not added.",
                Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 12)
            });
            Grid combinedFields = new Grid();
            combinedFields.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            combinedFields.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            combinedFields.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
            StackPanel mapStack = new StackPanel { Margin = new Thickness(0, 0, 8, 0) }; mapStack.Children.Add(Caption("MAP"));
            mapBox = new ComboBox { ItemsSource = allCombinedMaps, Margin = new Thickness(0, 6, 0, 0) };
            mapBox.SelectedItem = allCombinedMaps.FirstOrDefault(x => x.Id.Equals(currentScenario.MapId ?? "", StringComparison.OrdinalIgnoreCase)) ?? allCombinedMaps.FirstOrDefault();
            mapStack.Children.Add(mapBox); combinedFields.Children.Add(mapStack);
            StackPanel sideStack = new StackPanel { Margin = new Thickness(0, 0, 8, 0) }; sideStack.Children.Add(Caption("SIDE"));
            sideBox = new ComboBox { ItemsSource = new[] { "Side 1", "Side 2" }, SelectedIndex = currentScenario.Side == 2 ? 1 : 0, Margin = new Thickness(0, 6, 0, 0) };
            sideStack.Children.Add(sideBox); Grid.SetColumn(sideStack, 1); combinedFields.Children.Add(sideStack);
            StackPanel spawnStack = new StackPanel(); spawnStack.Children.Add(Caption("SPAWN"));
            spawnBox = new ComboBox { Margin = new Thickness(0, 6, 0, 0), Tag = currentScenario.SpawnOption };
            spawnStack.Children.Add(spawnBox); Grid.SetColumn(spawnStack, 2); combinedFields.Children.Add(spawnStack);
            combinedPanel.Children.Add(combinedFields);
            combinedCard.Child = combinedPanel;
            content.Children.Add(combinedCard);

            targetCards = new StackPanel();
            content.Children.Add(targetCards);

            Border airCard = SectionCard();
            StackPanel airPanel = new StackPanel();
            List<AircraftView> airChoices = aircraft.OrderBy(x => x.Name).ToList();
            AddFlyingRow(airPanel, "AIR TARGET01", airChoices, currentAir01, currentAir01Count, out airBox01, out airCountBox01, "ef_2000_typhoon_aesa");
            AddFlyingRow(airPanel, "AIR TARGET02", airChoices, currentAir, currentAirCount, out airBox, out airCountBox, null);
            AddFlyingRow(airPanel, "HELI TARGET01", airChoices, currentHeli01, currentHeli01Count, out heliBox01, out heliCountBox01, "mi_28nm");
            AddFlyingRow(airPanel, "HELI TARGET02", airChoices, currentHeli02, currentHeli02Count, out heliBox02, out heliCountBox02, "ka_52");
            airCard.Child = airPanel; targetCards.Children.Add(airCard);

            Border groundCard = SectionCard();
            StackPanel groundPanel = new StackPanel();
            Grid groundHeader = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            groundHeader.ColumnDefinitions.Add(new ColumnDefinition());
            groundHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            groundHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
            groundHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
            groundHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) });
            TextBlock groundTitle = Heading("GROUND RANGE POSITIONS", 15); groundTitle.VerticalAlignment = VerticalAlignment.Center; groundHeader.Children.Add(groundTitle);
            groundNation = FilterBox(allGround.Select(x => x.Nation), ModernText.L("All Nations", "全部国家")); Grid.SetColumn(groundNation, 1); groundHeader.Children.Add(groundNation);
            groundRank = RankBox(allGround); groundRank.Margin = new Thickness(8, 0, 0, 0); Grid.SetColumn(groundRank, 2); groundHeader.Children.Add(groundRank);
            hostileBox = new ToggleButton { IsChecked = hostile, Style = (Style)DialogRoot.Resources["StatusToggleStyle"], Margin = new Thickness(8, 0, 0, 0), ToolTip = "Controls whether all seven selected ground targets actively aim at and fire on the player." }; Grid.SetColumn(hostileBox, 3); groundHeader.Children.Add(hostileBox);
            samSitesBox = new ToggleButton { IsChecked = true, Style = (Style)DialogRoot.Resources["StatusToggleStyle"], Margin = new Thickness(8, 0, 0, 0), ToolTip = "Cycles the clean-range SAM sites: ACTIVE (engage the player), PASSIVE (deployed but never attack), FRIENDLY (army 1, intercepts enemy air targets), DISABLED (not spawned)." };
            samSitesBox.Click += delegate { samSitesModeState = (samSitesModeState + 1) % 4; UpdateReactionButtons(); };
            samSitesSelectionBox = new ComboBox { Width = 150, VerticalAlignment = VerticalAlignment.Center };
            samSitesSelectionBox.Items.Add("S300");
            samSitesSelectionBox.Items.Add("PATRIOT");
            samSitesSelectionBox.Items.Add("HAWK");
            samSitesSelectionBox.Items.Add("BUK");
            samSitesSelectionBox.Items.Add("ALL");
            string initialSamSelection = String.IsNullOrWhiteSpace(samSitesSelection) ? "s300" : samSitesSelection;
            samSitesSelectionBox.SelectedIndex = Math.Max(0, Math.Min(4, new[] { "S300", "PATRIOT", "HAWK", "BUK", "ALL" }.ToList().IndexOf(initialSamSelection.ToUpperInvariant())));
            samSitesModeState = samSitesMode == "passive" ? 1 : samSitesMode == "friendly" ? 2 : samSitesMode == "disabled" ? 3 : 0;
            Grid samRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            samRow.ColumnDefinitions.Add(new ColumnDefinition());
            samRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            samRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) });
            TextBlock samCaption = Heading("SAM SITES", 15); samCaption.VerticalAlignment = VerticalAlignment.Center; samRow.Children.Add(samCaption);
            Grid.SetColumn(samSitesSelectionBox, 1); samRow.Children.Add(samSitesSelectionBox);
            Grid.SetColumn(samSitesBox, 2); samRow.Children.Add(samSitesBox);
            groundPanel.Children.Add(groundHeader);
            groundPanel.Children.Add(samRow);

            Grid groundGrid = new Grid();
            groundGrid.ColumnDefinitions.Add(new ColumnDefinition());
            groundGrid.ColumnDefinitions.Add(new ColumnDefinition());
            for (int row = 0; row < 4; row++) groundGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(68) });
            for (int index = 0; index < 7; index++)
            {
                StackPanel slot = new StackPanel { Margin = new Thickness(index % 2 == 0 ? 0 : 8, 0, index % 2 == 0 ? 8 : 0, 8) };
                slot.Children.Add(Caption("POSITION " + (index + 1).ToString("00", CultureInfo.InvariantCulture)));
                ComboBox box = new ComboBox { ItemsSource = allGround, SelectedItem = selectedGround.Count > index ? selectedGround[index] : null, Margin = new Thickness(0, 5, 0, 0) };
                groundBoxes.Add(box); slot.Children.Add(box);
                Grid.SetColumn(slot, index % 2); Grid.SetRow(slot, index / 2); groundGrid.Children.Add(slot);
            }
            groundPanel.Children.Add(groundGrid);
            groundCard.Child = groundPanel; targetCards.Children.Add(groundCard);

            Border shipCard = SectionCard();
            StackPanel shipPanel = new StackPanel();
            Grid shipFilters = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            shipFilters.ColumnDefinitions.Add(new ColumnDefinition());
            shipFilters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            shipFilters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
            TextBlock shipTitle = Heading("NAVAL TARGET", 15); shipTitle.VerticalAlignment = VerticalAlignment.Center; shipFilters.Children.Add(shipTitle);
            shipNation = FilterBox(allShips.Select(x => x.Nation), ModernText.L("All Nations", "全部国家")); Grid.SetColumn(shipNation, 1); shipFilters.Children.Add(shipNation);
            shipRank = RankBox(allShips); shipRank.Margin = new Thickness(8, 0, 0, 0); Grid.SetColumn(shipRank, 2); shipFilters.Children.Add(shipRank);
            shipPanel.Children.Add(shipFilters);
            Grid shipLine = new Grid(); shipLine.ColumnDefinitions.Add(new ColumnDefinition()); shipLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) }); shipLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            shipBox = new ComboBox { ItemsSource = allShips, SelectedItem = currentShip, Margin = new Thickness(0, 0, 8, 0) }; shipLine.Children.Add(shipBox);
            shipCountBox = CountBox(currentShipCount); Grid.SetColumn(shipCountBox, 1); shipLine.Children.Add(shipCountBox);
            passiveShipBox = new ToggleButton { IsChecked = passiveShip, Style = (Style)DialogRoot.Resources["StatusToggleStyle"], Margin = new Thickness(8, 0, 0, 0), ToolTip = "Controls whether the naval target stays passive or returns fire after the player attacks it." }; Grid.SetColumn(passiveShipBox, 2); shipLine.Children.Add(passiveShipBox);
            shipPanel.Children.Add(shipLine); shipCard.Child = shipPanel; targetCards.Children.Add(shipCard);

            groundNation.SelectionChanged += delegate { RefreshGround(); };
            groundRank.SelectionChanged += delegate { RefreshGround(); };
            shipNation.SelectionChanged += delegate { RefreshShips(); };
            shipRank.SelectionChanged += delegate { RefreshShips(); };
            hostileBox.Checked += delegate { UpdateReactionButtons(); };
            hostileBox.Unchecked += delegate { UpdateReactionButtons(); };
            passiveShipBox.Checked += delegate { UpdateReactionButtons(); };
            passiveShipBox.Unchecked += delegate { UpdateReactionButtons(); };
            modeBox.SelectionChanged += delegate { UpdateScenarioMode(); };
            mapBox.SelectionChanged += delegate { RefreshCombinedSpawns(); };
            sideBox.SelectionChanged += delegate { RefreshCombinedSpawns(); };
            eraBox.SelectionChanged += delegate { ApplyEraPreset(); };
            UpdateReactionButtons();

            Grid footer = new Grid(); footer.ColumnDefinitions.Add(new ColumnDefinition()); footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) }); footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
            footerHint = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Muted), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
            footer.Children.Add(footerHint);
            Button cancel = DialogButton("取消", false); cancel.Click += delegate { DialogResult = false; Close(); }; Grid.SetColumn(cancel, 1); footer.Children.Add(cancel);
            Button apply = DialogButton("APPLY MAP", true); apply.Click += delegate { Save(); }; Grid.SetColumn(apply, 2); footer.Children.Add(apply); Grid.SetRow(footer, 2); layout.Children.Add(footer);
            RefreshCombinedSpawns();
            UpdateScenarioMode();
        }

        public ModernMapWindow(IEnumerable<AircraftView> aircraft, IEnumerable<TargetView> ground, IEnumerable<TargetView> ships,
            AircraftView currentAir, int currentAirCount, TargetView currentGround, int currentGroundCount, bool hostile,
            TargetView currentShip, int currentShipCount)
            : this(aircraft, ground, ships, currentAir, currentAirCount, new[] { currentGround }, hostile, "active", "all", currentShip, currentShipCount, false,
                Enumerable.Empty<CombinedMap>(), "aircraft", new CombinedScenarioSettings(), null, 1, null, 1, null, 1) { }

        private void UpdateScenarioMode()
        {
            bool combined = modeBox.SelectedIndex == 1;
            Height = combined ? 520 : 820;
            combinedCard.Visibility = combined ? Visibility.Visible : Visibility.Collapsed;
            targetCards.Visibility = combined ? Visibility.Collapsed : Visibility.Visible;
            footerHint.Text = combined
                ? "The mission contains only your vehicle, the selected spawn base and instant player respawn."
                : "Destroyed targets recover rapidly; player ammunition rearms after depletion.";
        }

        private void RefreshCombinedSpawns()
        {
            CombinedMap map = mapBox.SelectedItem as CombinedMap;
            int side = sideBox.SelectedIndex == 1 ? 2 : 1;
            string preferred = spawnBox.SelectedItem is CombinedSpawn ? ((CombinedSpawn)spawnBox.SelectedItem).Option : spawnBox.Tag as string;
            List<CombinedSpawn> values = map == null ? new List<CombinedSpawn>() : map.Spawns
                .Where(x => x.Side == side && x.Kind.Equals(playerKind, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Option.Equals("airfield", StringComparison.OrdinalIgnoreCase) || x.Option.Equals("ground_1", StringComparison.OrdinalIgnoreCase) || x.Option.Equals("heli_near", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(x => x.Option, StringComparer.OrdinalIgnoreCase).ToList();
            spawnBox.ItemsSource = values;
            spawnBox.SelectedItem = values.FirstOrDefault(x => x.Option.Equals(preferred ?? "", StringComparison.OrdinalIgnoreCase)) ?? values.FirstOrDefault();
            spawnBox.Tag = null;
        }

        private Border SectionCard()
        {
            return new Border { CornerRadius = new CornerRadius(14), Background = ModernPalette.Brush(ModernPalette.Field), BorderBrush = ModernPalette.Brush(ModernPalette.Border), BorderThickness = new Thickness(1), Padding = new Thickness(16), Margin = new Thickness(0, 0, 0, 10) };
        }

        private ComboBox CountBox(int value)
        {
            return new ComboBox { ItemsSource = Enumerable.Range(0, 21).ToList(), SelectedItem = Math.Max(0, Math.Min(20, value)) };
        }

        private void SavePresetClicked()
        {
            string suggested = String.Format(CultureInfo.InvariantCulture, "CUSTOM - {0:MMdd-HHmm}", DateTime.Now);
            ModernInputWindow input = new ModernInputWindow("SAVE ERA PRESET", "Name this preset. It is appended to era_presets.tsv in LocalAppData (" + Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\UniversalTestLab\\era_presets.tsv) and appears in the list next time UTL starts.", suggested);
            input.Owner = Owner;
            if (input.ShowDialog() != true) return;
            string name = input.Value == null ? null : input.Value.Trim();
            if (String.IsNullOrWhiteSpace(name)) return;
            List<string> groundIds = new List<string>();
            foreach (ComboBox box in groundBoxes)
            {
                TargetView view = box.SelectedItem as TargetView;
                groundIds.Add(view == null || view.Source == null ? String.Empty : view.Source.Id);
            }
            ComboBox[] airBoxes = new[] { airBox01, airBox, heliBox01, heliBox02 };
            ComboBox[] countBoxes = new[] { airCountBox01, airCountBox, heliCountBox01, heliCountBox02 };
            string[] airIds = new string[4];
            int[] airCounts = new int[4];
            for (int i = 0; i < 4; i++)
            {
                AircraftView view = airBoxes[i].SelectedItem as AircraftView;
                airIds[i] = view == null || view.Source == null ? null : view.Source.Id;
                int count = 0;
                object countSel = countBoxes[i].SelectedItem;
                if (countSel is int) count = (int)countSel;
                else if (countSel != null) Int32.TryParse(countSel.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out count);
                airCounts[i] = count;
            }
            TargetView shipView = shipBox.SelectedItem as TargetView;
            string shipId = shipView == null || shipView.Source == null ? String.Empty : shipView.Source.Id;
            int shipCount = 0;
            object shipSel = shipCountBox.SelectedItem;
            if (shipSel is int) shipCount = (int)shipSel;
            else if (shipSel != null) Int32.TryParse(shipSel.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out shipCount);
            try
            {
                List<object> list = ConfigStore.GetList("era_presets") ?? new List<object>();
                if (list.Count == 0 && BuiltinEraPresets != null)
                {
                    foreach (EraPreset builtin in BuiltinEraPresets)
                    {
                        Dictionary<string, object> bo = new Dictionary<string, object>();
                        bo.Add("name", builtin.Name);
                        bo.Add("ground", builtin.GroundIds == null ? new List<object>() : builtin.GroundIds.Select(x => (object)(x ?? String.Empty)).ToList());
                        bo.Add("air", builtin.AirIds == null ? new List<object>() : builtin.AirIds.Select(x => x == null ? null : (object)x).ToList());
                        bo.Add("air_counts", builtin.AirCounts == null ? new List<object>() : builtin.AirCounts.Select(x => (object)x).ToList());
                        bo.Add("ship", builtin.ShipId ?? String.Empty);
                        bo.Add("ship_count", builtin.ShipCount);
                        list.Add(bo);
                    }
                }
                Dictionary<string, object> o = new Dictionary<string, object>();
                o.Add("name", name);
                o.Add("ground", groundIds.Select(x => (object)x).ToList());
                o.Add("air", airIds.Select(x => x == null ? null : (object)x).ToList());
                o.Add("air_counts", airCounts.Select(x => (object)x).ToList());
                o.Add("ship", shipId);
                o.Add("ship_count", shipCount);
                list.Add(o);
                ConfigStore.SetList("era_presets", list);
                ConfigStore.Save();
            }
            catch { }
            ReloadEraPresets();
            eraBox.SelectedIndex = 0;
        }

        private void ReloadEraPresets()
        {
            if (eraBox == null) return;
            EraPresets = LoadEraPresets();
            eraBox.Items.Clear();
            eraBox.Items.Add("None (keep current)");
            foreach (EraPreset era in EraPresets) eraBox.Items.Add(era.Name);
            eraBox.SelectedIndex = 0;
        }

        private void ApplyEraPreset()
        {
            if (eraBox == null || eraBox.SelectedIndex <= 0) return;
            EraPreset preset = EraPresets[Math.Min(eraBox.SelectedIndex - 1, EraPresets.Length - 1)];
            for (int i = 0; i < groundBoxes.Count && i < preset.GroundIds.Length; i++)
            {
                TargetView match = allGround.FirstOrDefault(x => x.Source.Id != null && x.Source.Id.Equals(preset.GroundIds[i], StringComparison.OrdinalIgnoreCase));
                if (match != null) groundBoxes[i].SelectedItem = match;
            }
            SetFlyingPreset(airBox01, airCountBox01, preset.AirIds.Length > 0 ? preset.AirIds[0] : null, preset.AirCounts.Length > 0 ? preset.AirCounts[0] : 0);
            SetFlyingPreset(airBox, airCountBox, preset.AirIds.Length > 1 ? preset.AirIds[1] : null, preset.AirCounts.Length > 1 ? preset.AirCounts[1] : 0);
            SetFlyingPreset(heliBox01, heliCountBox01, preset.AirIds.Length > 2 ? preset.AirIds[2] : null, preset.AirCounts.Length > 2 ? preset.AirCounts[2] : 0);
            SetFlyingPreset(heliBox02, heliCountBox02, preset.AirIds.Length > 3 ? preset.AirIds[3] : null, preset.AirCounts.Length > 3 ? preset.AirCounts[3] : 0);
            if (!String.IsNullOrWhiteSpace(preset.ShipId))
            {
                TargetView ship = allShips.FirstOrDefault(x => x.Source.Id != null && x.Source.Id.Equals(preset.ShipId, StringComparison.OrdinalIgnoreCase));
                if (ship != null) shipBox.SelectedItem = ship;
                shipCountBox.SelectedItem = Math.Max(0, Math.Min(20, preset.ShipCount));
            }
        }

        private void SetFlyingPreset(ComboBox box, ComboBox countBox, string id, int count)
        {
            if (box == null || countBox == null) return;
            countBox.SelectedItem = Math.Max(0, Math.Min(20, count));
            if (String.IsNullOrWhiteSpace(id)) return;
            IEnumerable<AircraftView> source = box.ItemsSource as IEnumerable<AircraftView>;
            AircraftView match = source == null ? null : source.FirstOrDefault(x => x.Source.Id != null && x.Source.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (match != null) box.SelectedItem = match;
        }

        private void AddFlyingRow(StackPanel host, string caption, List<AircraftView> source, AircraftView current, int count, out ComboBox box, out ComboBox countBox, string templateDefaultId)
        {
            Grid row = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(84) });
            TextBlock label = new TextBlock { Text = caption, Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 12, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
            if (!String.IsNullOrWhiteSpace(templateDefaultId)) label.ToolTip = ModernText.L("Template default: ", "模板默认: ") + templateDefaultId;
            row.Children.Add(label);
            box = new ComboBox { ItemsSource = source, SelectedItem = current, Margin = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
            if (box.SelectedItem == null && source.Count > 0) box.SelectedIndex = 0;
            Grid.SetColumn(box, 1); row.Children.Add(box);
            countBox = CountBox(count); Grid.SetColumn(countBox, 2); row.Children.Add(countBox);
            host.Children.Add(row);
        }

        private ComboBox FilterBox(IEnumerable<string> values, string all)
        {
            ComboBox box = new ComboBox();
            box.Items.Add(all);
            foreach (string value in values.Where(x => !String.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x)) box.Items.Add(value);
            box.SelectedIndex = 0;
            return box;
        }

        private ComboBox RankBox(IEnumerable<TargetView> values)
        {
            ComboBox box = new ComboBox();
            box.Items.Add(ModernText.L("Any Rank", "任意等级"));
            foreach (int rank in values.Select(x => x.Rank).Where(x => x > 0).Distinct().OrderBy(x => x)) box.Items.Add(rank);
            box.SelectedIndex = 0;
            return box;
        }

        private IEnumerable<TargetView> ApplyFilter(IEnumerable<TargetView> source, ComboBox nation, ComboBox rank)
        {
            string selectedNation = nation.SelectedIndex > 0 ? nation.SelectedItem as string : null;
            int selectedRank = rank.SelectedItem is int ? (int)rank.SelectedItem : 0;
            if (!String.IsNullOrEmpty(selectedNation)) source = source.Where(x => x.Nation == selectedNation);
            if (selectedRank > 0) source = source.Where(x => x.Rank == selectedRank);
            return source.OrderBy(x => x.Name).ToList();
        }

        private void RefreshGround()
        {
            List<TargetView> values = ApplyFilter(allGround, groundNation, groundRank).ToList();
            foreach (ComboBox box in groundBoxes)
            {
                TargetView keep = box.SelectedItem as TargetView;
                box.ItemsSource = values;
                box.SelectedItem = keep != null && values.Any(x => x.Source.Id == keep.Source.Id) ? values.First(x => x.Source.Id == keep.Source.Id) : values.FirstOrDefault();
            }
        }

        private void RefreshShips()
        {
            TargetView keep = shipBox.SelectedItem as TargetView;
            List<TargetView> values = ApplyFilter(allShips, shipNation, shipRank).ToList();
            shipBox.ItemsSource = values;
            shipBox.SelectedItem = keep != null && values.Any(x => x.Source.Id == keep.Source.Id) ? values.First(x => x.Source.Id == keep.Source.Id) : values.FirstOrDefault();
        }

        private void UpdateReactionButtons()
        {
            bool groundAttacks = hostileBox.IsChecked == true;
            hostileBox.Content = groundAttacks ? "GROUND TARGETS — ATTACKING" : "GROUND TARGETS — PASSIVE";
            hostileBox.Background = ModernPalette.Brush(groundAttacks ? "#A34B1733" : "#8A1D5148");
            hostileBox.BorderBrush = ModernPalette.Brush(groundAttacks ? ModernPalette.Danger : ModernPalette.Good);
            hostileBox.Foreground = ModernPalette.Brush(groundAttacks ? "#FFFFE7EF" : "#FFE7FFF7");

            bool samsActive = samSitesModeState != 3;
            samSitesBox.Content = samsActive ? (samSitesModeState == 1 ? "SAM SITES PASSIVE" : samSitesModeState == 2 ? "SAM SITES FRIENDLY" : "SAM SITES ACTIVE") : "SAM SITES DISABLED";
            samSitesBox.Background = ModernPalette.Brush(samsActive ? (samSitesModeState == 1 ? "#8A5A1D48" : samSitesModeState == 2 ? "#8A1D4A48" : "#A34B1733") : "#8A1D5148");
            samSitesBox.BorderBrush = ModernPalette.Brush(samsActive ? (samSitesModeState == 1 ? "#FFE0A030" : samSitesModeState == 2 ? "#FF50C8A0" : ModernPalette.Danger) : ModernPalette.Good);
            samSitesBox.Foreground = ModernPalette.Brush(samsActive ? "#FFFFE7EF" : "#FFE7FFF7");

            bool shipPassive = passiveShipBox.IsChecked == true;
            passiveShipBox.Content = shipPassive ? "SHIP — STAYS PASSIVE" : "SHIP — RETURNS FIRE";
            passiveShipBox.Background = ModernPalette.Brush(shipPassive ? "#8A1D5148" : "#A34B1733");
            passiveShipBox.BorderBrush = ModernPalette.Brush(shipPassive ? ModernPalette.Good : ModernPalette.Danger);
            passiveShipBox.Foreground = ModernPalette.Brush(shipPassive ? "#FFE7FFF7" : "#FFFFE7EF");
        }

        private void Save()
        {
            AirTarget01 = airBox01.SelectedItem as AircraftView;
            AirCount01 = (int)(airCountBox01.SelectedItem ?? 0);
            AirTarget = airBox.SelectedItem as AircraftView;
            AirCount = (int)(airCountBox.SelectedItem ?? 0);
            HeliTarget01 = heliBox01.SelectedItem as AircraftView;
            HeliCount01 = (int)(heliCountBox01.SelectedItem ?? 0);
            HeliTarget02 = heliBox02.SelectedItem as AircraftView;
            HeliCount02 = (int)(heliCountBox02.SelectedItem ?? 0);
            GroundTargets = groundBoxes.Select(x => x.SelectedItem as TargetView).Where(x => x != null).ToList();
            Hostile = hostileBox.IsChecked == true;
            SamSitesMode = samSitesModeState == 0 ? "active" : samSitesModeState == 1 ? "passive" : samSitesModeState == 2 ? "friendly" : "disabled";
            SamSitesSelection = (samSitesSelectionBox.SelectedItem as string ?? "S300").ToLowerInvariant();
            SamSites = SamSitesMode != "disabled";
            ShipTarget = shipBox.SelectedItem as TargetView;
            ShipCount = (int)(shipCountBox.SelectedItem ?? 0);
            PassiveShip = passiveShipBox.IsChecked == true;
            CombinedMap map = mapBox.SelectedItem as CombinedMap;
            CombinedSpawn spawn = spawnBox.SelectedItem as CombinedSpawn;
            Scenario = new CombinedScenarioSettings
            {
                Enabled = modeBox.SelectedIndex == 1,
                MapId = map == null ? null : map.Id,
                Side = sideBox.SelectedIndex == 1 ? 2 : 1,
                SpawnOption = spawn == null ? null : spawn.Option
            };
            if (Scenario.Enabled && (map == null || spawn == null))
            {
                ModernMessageDialog error = new ModernMessageDialog("Map & Scenario", "Select a map, side and compatible spawn.", "关闭", null, true) { Owner = Owner };
                error.ShowDialog();
                return;
            }
            DialogResult = true;
            Close();
        }
    }

    internal sealed class GroundAmmoSlotEditor
    {
        public int Slot;
        public Border Card;
        public Button Select;
        public TextBlock Name;
        public Slider Count;
        public TextBox CountBox;
        public TextBlock Value;
    }

    internal sealed class CannonChoice
    {
        public string Blk;
        public string Display;
        public bool IsNative;
        public string Domain;
        public string UnitId;
        public override string ToString() { return Display; }
    }

    // Embedded in the main-window EXPERIMENTAL tab; the standalone Ground
    // Configure window keeps its own copy (keep both in sync when editing).
    internal sealed class GroundConfigurePanel : StackPanel
    {
        private readonly bool simplified;
        private readonly Aircraft vehicle;
        private readonly AircraftSettings original;
        private readonly List<GroundAmmo> catalog;
        private readonly Dictionary<int, GroundAmmoLoadout> loadouts = new Dictionary<int, GroundAmmoLoadout>();
        private readonly List<GroundAmmoSlotEditor> slotEditors = new List<GroundAmmoSlotEditor>();
        private readonly Dictionary<string, TextBox> tuning = new Dictionary<string, TextBox>();
        private readonly Dictionary<string, double> tuningStock = new Dictionary<string, double>();
        private readonly TextBox searchBox;
        private readonly ComboBox typeBox;
        private readonly ToggleButton injectionToggle;
        private readonly ComboBox domainBox;
        private readonly ComboBox unitBox;
        private readonly ComboBox cannonBox;
        private readonly ComboBox roundBox;
        private readonly Func<string, IList<GroundAmmo>> resolveCannonAmmo;
        private readonly CheckBox ammoUnlimitedBox;
        private readonly CheckBox fakeArhBox;
        private string radarSearchSel;
        private string radarTrackSel;
        private readonly CheckBox stripAiBox;
        private readonly TextBlock radarStatus;
        private readonly string gameRoot;
        private readonly AircraftSettings currentSettings;
        // Native sensor slots of the current vehicle (resolved once from its blk):
        // null = absent slot, non-null = the catalog row of the native radar.
        private SensorRowJson nativeSearchSensor;
        private SensorRowJson nativeTrackSensor;
        private readonly TextBlock radarDetailSearch = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Text), FontSize = 11.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4) };
        private readonly TextBlock radarDetailTrack = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Text), FontSize = 11.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4) };
        private readonly Dictionary<string, IList<GroundAmmo>> cannonAmmoCache = new Dictionary<string, IList<GroundAmmo>>(StringComparer.OrdinalIgnoreCase);
        private readonly Style buttonStyle;
        private readonly Style toggleStyle;
        private ComboBox cannonSelector;
        private CannonChoice currentCannon;
        private bool syncingCannon;
        private readonly IList<UnitWeapon> unitWeapons;
        private readonly IList<GroundAmmo> injectedCannonAmmo;
        private readonly IList<GroundWeaponBeltOption> vehicleBeltOptions;
        private readonly IList<GroundWeaponInfo> groundWeapons;
        private readonly List<TargetUnit> groundVehicles;
        private readonly ListBox ammoList;
        private readonly TextBlock totalAmmoText;
        private readonly CheckBox overrideBallistics;
        private GroundAmmo projectileReference;
        private int selectedSlot;
        private bool updatingSlots;

        private int AmmoCapacity { get { return vehicle.MaxAmmo > 0 ? vehicle.MaxAmmo : 200; } }

        private bool ContainerAllowed(string container)
        {
            // Unnamed default rounds (empty container) are the STOCK ammunition - they
            // cannot be written into a slot by name (the game falls back and shows a
            // wrong round), so they are hidden here; STOCK covers them.
            if (String.IsNullOrWhiteSpace(container)) return false;
            if (vehicleBeltOptions == null || vehicleBeltOptions.Count == 0) return true; // no data -> keep old behaviour
            foreach (GroundWeaponBeltOption belt in vehicleBeltOptions)
                if (belt != null && String.Equals(belt.Name, container, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public GroundConfigurePanel(Aircraft item, AircraftSettings current, IEnumerable<GroundAmmo> ammo, IEnumerable<TargetUnit> groundVehicles, IEnumerable<UnitWeapon> unitWeapons, IEnumerable<GroundWeaponInfo> groundWeapons, IEnumerable<GroundAmmo> injectedCannonAmmo, IEnumerable<GroundWeaponBeltOption> beltOptions, Func<string, IList<GroundAmmo>> resolveCannonAmmo, Style buttonStyleSource, Style toggleStyleSource, bool simplified = false, string gameRoot = null)
        {
            this.simplified = simplified;
            this.gameRoot = gameRoot;
            ResolveNativeSensors(item);
            vehicle = item;
            currentSettings = current;
            original = (current ?? new AircraftSettings()).Copy();
            if (String.IsNullOrWhiteSpace(original.InjectedCannonBlk))
            {
                original.InjectedCannonBlk = MissionSettings.Current.InjectedCannonBlk;
                original.InjectedCannonDomain = MissionSettings.Current.InjectedCannonDomain;
                original.InjectedCannonUnit = MissionSettings.Current.InjectedCannonUnit;
            }
            if (!original.FakeArhConversion && MissionSettings.Current.FakeArhConversion)
                original.FakeArhConversion = true;
            catalog = (ammo ?? Enumerable.Empty<GroundAmmo>()).ToList();
            this.groundVehicles = (groundVehicles ?? Enumerable.Empty<TargetUnit>()).Where(v => !String.IsNullOrWhiteSpace(v.MainWeaponBlk)).ToList();
            this.unitWeapons = (unitWeapons ?? Enumerable.Empty<UnitWeapon>()).ToList();
            this.groundWeapons = (groundWeapons ?? Enumerable.Empty<GroundWeaponInfo>()).ToList();
            this.injectedCannonAmmo = (injectedCannonAmmo ?? Enumerable.Empty<GroundAmmo>()).ToList();
            this.vehicleBeltOptions = (beltOptions ?? Enumerable.Empty<GroundWeaponBeltOption>()).ToList();
            this.resolveCannonAmmo = resolveCannonAmmo;
            buttonStyle = buttonStyleSource;
            toggleStyle = toggleStyleSource;
            foreach (GroundAmmoLoadout entry in original.GroundAmmoLoadouts.Where(x => x.Slot >= 0 && x.Slot < 4)) loadouts[entry.Slot] = entry.Copy();

            if (!simplified)
            {
            StackPanel header = new StackPanel();
            if (!simplified) header.Children.Add(Heading(ModernText.L("GROUND CONFIGURE", "地面配置"), 18));
            header.Children.Add(new TextBlock { Text = item.Display, Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, simplified ? 0 : 4, 0, 0) });
                Children.Add(header);
            }

            Grid body = new Grid { Margin = new Thickness(0, 6, 0, 8), ClipToBounds = true };
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.25, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Children.Add(body);

            Border ammoCard = Card(); Grid ammoGrid = new Grid { ClipToBounds = true };
            ammoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
            ammoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) });
            ammoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
            ammoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            ammoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(simplified ? 0 : 174) });
            ammoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(simplified ? 0 : 48) });
            Grid ammoHeader = new Grid(); ammoHeader.ColumnDefinitions.Add(new ColumnDefinition()); ammoHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ammoHeader.Children.Add(Heading("AMMUNITION & PROJECTILE INJECTION", 15));
            totalAmmoText = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(totalAmmoText, 1); ammoHeader.Children.Add(totalAmmoText); ammoGrid.Children.Add(ammoHeader);
            Grid cannonRow = new Grid { Margin = new Thickness(0, 1, 0, 1) }; cannonRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); cannonRow.ColumnDefinitions.Add(new ColumnDefinition());
            TextBlock cannonCaption = new TextBlock { Text = ModernText.L("CANNON", "主炮"), Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontWeight = FontWeights.SemiBold, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            cannonSelector = new ComboBox { Foreground = ModernPalette.Brush(ModernPalette.Text), Background = ModernPalette.Brush("#FF16283E"), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Padding = new Thickness(8, 3, 8, 3), Height = 32, HorizontalAlignment = HorizontalAlignment.Stretch, IsTextSearchEnabled = true, IsTextSearchCaseSensitive = false, ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel))) };
            cannonSelector.SelectionChanged += delegate { CannonSelectorChanged(); };
            cannonRow.Children.Add(cannonCaption); Grid.SetColumn(cannonSelector, 1); cannonRow.Children.Add(cannonSelector); Grid.SetRow(cannonRow, 1); ammoGrid.Children.Add(cannonRow);
            Grid filters = new Grid(); filters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) }); filters.ColumnDefinitions.Add(new ColumnDefinition()); filters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            injectionToggle = new ToggleButton { Content = ModernText.L("INJECT ANY SHELL", "注入任意炮弹"), Style = toggleStyle, Margin = new Thickness(0, 3, 8, 3) };
            searchBox = new TextBox { Margin = new Thickness(0, 3, 8, 3) }; Grid.SetColumn(searchBox, 1);
            typeBox = new ComboBox { Margin = new Thickness(0, 3, 0, 3) }; Grid.SetColumn(typeBox, 2);
            filters.Children.Add(injectionToggle); filters.Children.Add(searchBox); filters.Children.Add(typeBox); Grid.SetRow(filters, 2); ammoGrid.Children.Add(filters);
            if (simplified) injectionToggle.Visibility = Visibility.Collapsed;
            ammoList = new ListBox { Background = ModernPalette.Brush(ModernPalette.Field), BorderBrush = ModernPalette.Brush(ModernPalette.Border), BorderThickness = new Thickness(1), Margin = new Thickness(0, 4, 0, 7) };
            Grid.SetRow(ammoList, 3); ammoGrid.Children.Add(ammoList);

            UniformGrid slots = new UniformGrid { Rows = 2, Columns = 2, Margin = new Thickness(0, 0, 0, 6) };
            for (int slot = 0; slot < 4; slot++) slots.Children.Add(CreateAmmoSlot(slot));
            Grid.SetRow(slots, 4); ammoGrid.Children.Add(slots);
            Grid mountRow = new Grid(); mountRow.ColumnDefinitions.Add(new ColumnDefinition()); mountRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });
            mountRow.Children.Add(new TextBlock { Text = ModernText.L("Choose a slot, select a round above, then mount it.", "选择槽位，先在上方选择炮弹，再装填。"), Foreground = ModernPalette.Brush(ModernPalette.Muted), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });
            Button mount = new Button { Content = ModernText.L("MOUNT ROUND", "装填炮弹"), Style = buttonStyle, Padding = new Thickness(18, 2, 18, 2), Margin = new Thickness(4, 0, 0, 0) }; mount.Click += delegate { MountSelectedAmmo(); }; Grid.SetColumn(mount, 1); mountRow.Children.Add(mount); Grid.SetRow(mountRow, 5); ammoGrid.Children.Add(mountRow);
            ammoCard.Child = ammoGrid; body.Children.Add(ammoCard);

            Border tuningCard = Card();
            ScrollViewer tuningScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, ClipToBounds = true };
            StackPanel tuningPanel = new StackPanel();
            tuningPanel.Children.Add(Heading("CROSS-DOMAIN CANNON", 15));
            Grid domainRow = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            domainRow.ColumnDefinitions.Add(new ColumnDefinition());
            domainRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            domainBox = new ComboBox { Foreground = ModernPalette.Brush(ModernPalette.Text), Background = ModernPalette.Brush("#FF16283E"), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Padding = new Thickness(8, 4, 8, 4), Height = 32, HorizontalAlignment = HorizontalAlignment.Stretch };
            domainBox.Items.Add(new ComboBoxItem { Content = ModernText.L("GROUND VEHICLE", "地面载具"), Tag = "ground" });
            domainBox.Items.Add(new ComboBoxItem { Content = ModernText.L("NAVAL SHIP", "海上舰船"), Tag = "naval" });
            domainBox.Items.Add(new ComboBoxItem { Content = ModernText.L("AIRCRAFT", "空中载具"), Tag = "aircraft" });
            domainBox.Items.Add(new ComboBoxItem { Content = ModernText.L("HELICOPTER", "直升机"), Tag = "helicopter" });
            string savedDomain = String.IsNullOrWhiteSpace(original.InjectedCannonDomain) ? "ground" : original.InjectedCannonDomain;
            ComboBoxItem savedDomainItem = domainBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => String.Equals(x.Tag as string, savedDomain, StringComparison.OrdinalIgnoreCase)) ?? (ComboBoxItem)domainBox.Items[0];
            unitBox = new ComboBox { Foreground = ModernPalette.Brush(ModernPalette.Text), Background = ModernPalette.Brush("#FF16283E"), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Padding = new Thickness(8, 4, 8, 4), Height = 32, Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
            cannonBox = new ComboBox { Foreground = ModernPalette.Brush(ModernPalette.Text), Background = ModernPalette.Brush("#FF16283E"), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Padding = new Thickness(8, 4, 8, 4), Height = 32, Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
            domainBox.SelectionChanged += delegate { RefreshUnitBox(); };
            unitBox.SelectionChanged += delegate { RefreshCannonBox(); };
            cannonBox.SelectionChanged += delegate { RefreshRoundBox(); SyncLeftCannon(); };
            Button clearCannon = new Button { Content = ModernText.L("CLEAR", "清除"), Style = buttonStyle, Padding = new Thickness(10, 2, 10, 2), Margin = new Thickness(4, 0, 0, 0) };
            clearCannon.Click += delegate { cannonBox.SelectedIndex = -1; };
            domainRow.Children.Add(domainBox);
            Grid.SetColumn(clearCannon, 1);
            domainRow.Children.Add(clearCannon);
            tuningPanel.Children.Add(domainRow);
            tuningPanel.Children.Add(unitBox);
            tuningPanel.Children.Add(cannonBox);
            roundBox = new ComboBox { Foreground = ModernPalette.Brush(ModernPalette.Text), Background = ModernPalette.Brush("#FF16283E"), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Padding = new Thickness(8, 4, 8, 4), Height = 32, Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
            roundBox.Items.Add(new ComboBoxItem { Content = ModernText.L("ALL (native rounds)", "全部（原生炮弹）"), Tag = "" });
            foreach (GroundAmmo injectedRound in injectedCannonAmmo)
                roundBox.Items.Add(new ComboBoxItem { Content = injectedRound.Display, Tag = injectedRound.BulletName });
            if (!String.IsNullOrWhiteSpace(original.InjectedCannonRound))
            {
                ComboBoxItem savedRound = roundBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => String.Equals(x.Tag as string, original.InjectedCannonRound, StringComparison.OrdinalIgnoreCase));
                if (savedRound != null) roundBox.SelectedItem = savedRound;
            }
            if (roundBox.SelectedItem == null) roundBox.SelectedIndex = 0;
            roundBox.SelectionChanged += delegate { SyncRoundToSlot(); };
            tuningPanel.Children.Add(roundBox);
            bool roundsSyncing = true;
            StackPanel roundsRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
            roundsRow.Children.Add(new TextBlock { Text = ModernText.L("ROUNDS PER RELOAD (0 = source)", "每次装填弹数（0 = 沿用原值）"), Foreground = ModernPalette.Brush(ModernPalette.Muted), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            TextBox roundsBox = new TextBox { Text = original.InjectedCannonRounds > 0 ? original.InjectedCannonRounds.ToString(CultureInfo.InvariantCulture) : "0", Width = 64, Height = 26, Padding = new Thickness(6, 2, 6, 2), VerticalContentAlignment = VerticalAlignment.Center, ToolTip = "Osa + S-300: 6 fills the native 6-rail rack (S-300 source carries 4)." };
            roundsRow.Children.Add(roundsBox); tuningPanel.Children.Add(roundsRow);
            roundsBox.TextChanged += delegate
            {
                if (roundsSyncing) return;
                int v;
                if (int.TryParse(roundsBox.Text.Trim(), out v) && v >= 0 && v <= 999) { original.InjectedCannonRounds = v; if (currentSettings != null) currentSettings.InjectedCannonRounds = v; }
            };
            roundsSyncing = false;
            ammoUnlimitedBox = new CheckBox { Content = ModernText.L("Unlimited ammunition (9999 per slot)", "无限弹药（每槽 9999）"), IsChecked = original.UnlimitedAmmo, Foreground = ModernPalette.Brush(ModernPalette.Text), Margin = new Thickness(0, 6, 0, 0) };
            tuningPanel.Children.Add(ammoUnlimitedBox);
            fakeArhBox = new CheckBox { Content = ModernText.L("Fake-ARH conversion (SARH missiles self-guide, TWS launch)", "伪ARH转换（半主动弹自主制导，TWS直射）"), IsChecked = original.FakeArhConversion, Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 6, 0, 0), ToolTip = "Injects active seeker + permanently-activated guidance into radar missiles so they launch without a pre-launch lock (SARH -> ARH). Verified on AIM-7E-2: active:b, permanentlyActivated, lockDistance, inertialNavigation+datalink, breakLockMaxTime=160, wider seeker angles, distGate, shotFreq cap." };
            tuningPanel.Children.Add(fakeArhBox);
            radarStatus = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Muted), FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) };
            radarSearchSel = original.RadarSearchBlk; radarTrackSel = original.RadarTrackBlk;
            stripAiBox = new CheckBox { Content = ModernText.L("Radar swap strips the AI-only radar pair", "雷达替换时移除 AI 专用雷达组"), IsChecked = original.RadarStripAiSensors, Margin = new Thickness(0, 1, 0, 0) };
            Button radarPick = new Button { Content = ModernText.L("CHANGE RADARS (SEARCH / TRACK)", "更换雷达（搜索 / 跟踪）"), Style = buttonStyle, Padding = new Thickness(14, 4, 14, 4), Margin = new Thickness(0, 3, 0, 1), HorizontalAlignment = HorizontalAlignment.Left };
            radarPick.Click += delegate { PickRadars(); };
            Button radarReset = new Button { Content = ModernText.L("RESET RADARS TO NATIVE", "恢复原生雷达"), Style = buttonStyle, Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(8, 3, 0, 1), HorizontalAlignment = HorizontalAlignment.Left };
            radarReset.Click += delegate { radarSearchSel = null; radarTrackSel = null; if (currentSettings != null) { currentSettings.RadarSearchBlk = null; currentSettings.RadarTrackBlk = null; } UpdateRadarStatus(); };
            StackPanel radarRow = new StackPanel { Orientation = Orientation.Horizontal }; radarRow.Children.Add(radarPick); radarRow.Children.Add(radarReset); tuningPanel.Children.Add(radarRow);
            // Swap is meaningless on vehicles without any native sensor structure - disable.
            if (nativeSearchSensor == null && nativeTrackSensor == null)
            {
                radarPick.IsEnabled = false;
                radarPick.ToolTip = ModernText.L("This vehicle has no radar at all - installing one needs a sensor structure first.", "此车完全没有雷达——更换不可用（需先有传感器结构）。");
            }
            tuningPanel.Children.Add(stripAiBox);
            tuningPanel.Children.Add(radarStatus);
            Border radarCard = new Border { CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Background = ModernPalette.Brush("#8A24324D"), Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(0, 8, 0, 4) };
            StackPanel radarCardStack = new StackPanel();
            radarCardStack.Children.Add(new TextBlock { Text = ModernText.L("RADAR DETAILS", "雷达详情"), Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            radarCardStack.Children.Add(radarDetailSearch);
            radarCardStack.Children.Add(radarDetailTrack);
            radarCard.Child = radarCardStack;
            tuningPanel.Children.Add(radarCard);
            UpdateRadarStatus();
            domainBox.SelectedItem = savedDomainItem;
            RefreshCannonBox();
            BuildCannonSelector();
            SelectInitialCannon();
            tuningPanel.Children.Add(new TextBlock { Text = "Pick the source unit (e.g. Yamato), then its weapon (460/155/127 mm). Ground, naval and air units are all supported; air also includes missiles and rockets. Ammunition slots and projectile tuning below then apply to the injected weapon.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) });
            tuningPanel.Children.Add(new Border { Height = 1, Background = ModernPalette.Brush(ModernPalette.Border), Margin = new Thickness(0, 10, 0, 10) });
            tuningPanel.Children.Add(Heading("REAL VEHICLE VALUES", 15));

            overrideBallistics = new CheckBox { Content = ModernText.L("Override native values", "覆盖原生数值"), IsChecked = original.OverrideGroundBallistics, Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 12, 0, 7) }; tuningPanel.Children.Add(overrideBallistics);
            tuningPanel.Children.Add(new TextBlock { Text = "Projectile values follow the selected ammunition slot. Every field can be typed directly.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) });
            projectileReference = ResolveProjectileReference();
            AddValue(tuningPanel, "PROJECTILE MASS", "projectileMass", projectileReference == null ? 0 : projectileReference.Mass, original.ProjectileMassMultiplier, "kg");
            AddValue(tuningPanel, "MUZZLE VELOCITY", "velocity", projectileReference == null ? 0 : projectileReference.Speed, original.MuzzleVelocityMultiplier, "m/s");
            AddValue(tuningPanel, "EXPLOSIVE FILLER", "explosive", projectileReference == null ? 0 : projectileReference.ExplosiveMass, original.ExplosiveMassMultiplier, "kg");
            AddValue(tuningPanel, "REFERENCE PENETRATION", "penetration", projectileReference == null ? 0 : projectileReference.Penetration, original.PenetrationMultiplier, "mm");
            AddValue(tuningPanel, "FIRE RATE OVERRIDE (SEC)", "reload", 0, 0, "s");
            if (original.ReloadSeconds > 0 && tuning.ContainsKey("reload")) tuning["reload"].Text = FormatValue(original.ReloadSeconds);
            AddValue(tuningPanel, "RECOIL TRAVEL", "recoil", vehicle.NativeRecoil, original.RecoilMultiplier, "m");
            tuningPanel.Children.Add(new Border { Height = 1, Background = ModernPalette.Brush(ModernPalette.Border), Margin = new Thickness(0, 8, 0, 7) });
            AddValue(tuningPanel, "ENGINE POWER", "engine", vehicle.NativeEnginePower, original.EnginePowerMultiplier, "hp");
            AddValue(tuningPanel, "VEHICLE MASS", "mass", vehicle.NativeMass, original.VehicleMassMultiplier, "kg");
            AddValue(tuningPanel, "FORWARD SPEED LIMIT", "forward", vehicle.NativeForwardSpeed, original.ForwardSpeedMultiplier, "km/h");
            AddValue(tuningPanel, "REVERSE SPEED LIMIT", "reverse", vehicle.NativeReverseSpeed, original.ReverseSpeedMultiplier, "km/h");
            Button resetAll = new Button { Content = ModernText.L("RESET ALL TO CURRENT STOCK", "重置为当前默认弹"), Style = buttonStyle, Padding = new Thickness(14, 2, 14, 2), Margin = new Thickness(0, 10, 0, 4) }; resetAll.Click += delegate { ResetAllValues(); }; tuningPanel.Children.Add(resetAll);
            tuningPanel.Children.Add(new TextBlock { Text = "Stock reset uses this vehicle's current game definition; selected research modules remain configured separately in Modules.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 4) });
            tuningScroll.Content = tuningPanel; tuningCard.Child = tuningScroll; Grid.SetColumn(tuningCard, 2); body.Children.Add(tuningCard);
            if (simplified)
            {
                // Home panel: the right card holds the 4 ammunition slots (MOUNT + pool)
                // instead of the cross-domain cannon / projectile tuning.
                StackPanel slotPanel = new StackPanel();
                slotPanel.Children.Add(Heading("AMMUNITION SLOTS", 15));
                UniformGrid slotGrid = new UniformGrid { Rows = 2, Columns = 2, Margin = new Thickness(0, 6, 0, 6) };
                for (int slot = 0; slot < 4; slot++) slotGrid.Children.Add(CreateAmmoSlot(slot));
                slotPanel.Children.Add(slotGrid);
                Grid slotMountRow = new Grid(); slotMountRow.ColumnDefinitions.Add(new ColumnDefinition()); slotMountRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                slotMountRow.Children.Add(new TextBlock { Text = ModernText.L("Select a round, pick a slot, mount it.", "选择炮弹与槽位并装填。"), Foreground = ModernPalette.Brush(ModernPalette.Muted), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });
                Button slotMount = new Button { Content = ModernText.L("MOUNT ROUND", "装填炮弹"), Style = buttonStyle, Padding = new Thickness(14, 2, 14, 2), Margin = new Thickness(4, 0, 0, 0) }; slotMount.Click += delegate { MountSelectedAmmo(); }; Grid.SetColumn(slotMount, 1); slotMountRow.Children.Add(slotMount);
                slotPanel.Children.Add(slotMountRow);
                slotPanel.Children.Add(new TextBlock { Text = ModernText.L("STOCK = native default round (empty slot + count).", "STOCK = 原生默认弹（空槽 + 数量）。"), Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
                tuningCard.Child = slotPanel;
            }

            typeBox.Items.Add("All Projectile Types"); foreach (string kind in catalog.Select(x => x.Type).Distinct().OrderBy(x => x)) typeBox.Items.Add(kind); typeBox.SelectedIndex = 0;
            injectionToggle.IsChecked = false; injectionToggle.Checked += delegate { RefreshAmmo(); }; injectionToggle.Unchecked += delegate { RefreshAmmo(); }; searchBox.TextChanged += delegate { RefreshAmmo(); }; typeBox.SelectionChanged += delegate { RefreshAmmo(); };
            overrideBallistics.Checked += delegate { UpdateTuningState(); }; overrideBallistics.Unchecked += delegate { UpdateTuningState(); };
            if (simplified) currentCannon = new CannonChoice { Blk = vehicle.MainWeaponBlk, Display = "PRIMARY", IsNative = true };
            SelectSlot(0); RefreshAmmo(); RefreshSlotEditors();
            UpdateTuningState();
        }

        private Border Card()
        {
            return new Border { CornerRadius = new CornerRadius(14), Background = ModernPalette.Brush(ModernPalette.Field), BorderBrush = ModernPalette.Brush(ModernPalette.Border), BorderThickness = new Thickness(1), Padding = new Thickness(12), ClipToBounds = true };
        }

        private Border CreateAmmoSlot(int slot)
        {
            GroundAmmoSlotEditor editor = new GroundAmmoSlotEditor { Slot = slot };
            editor.Card = new Border { CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Background = ModernPalette.Brush("#8A24324D"), Padding = new Thickness(8), Margin = new Thickness(3) };
            Grid grid = new Grid(); grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) }); grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(22) }); grid.RowDefinitions.Add(new RowDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            editor.Select = new Button { Content = ModernText.L("AMMO ", "槽位 ") + (slot + 1).ToString(CultureInfo.InvariantCulture), Tag = slot, Style = buttonStyle, Padding = new Thickness(5, 1, 5, 1) };
            editor.Select.Click += delegate { SelectSlot(editor.Slot); }; grid.Children.Add(editor.Select);
            Button clear = new Button { Content = "\u00d7", Tag = slot, Style = buttonStyle, Padding = new Thickness(0, 1, 0, 1), Margin = new Thickness(4, 0, 0, 0), ToolTip = "Clear this ammunition slot", FontSize = 12, Foreground = ModernPalette.Brush(ModernPalette.Muted) };
            clear.Click += delegate { if (loadouts.ContainsKey(editor.Slot)) loadouts.Remove(editor.Slot); RefreshSlotEditors(); }; Grid.SetColumn(clear, 1); grid.Children.Add(clear);
            editor.Name = new TextBlock { Text = ModernText.L("EMPTY", "空"), Foreground = ModernPalette.Brush(ModernPalette.Muted), FontSize = 10, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(editor.Name, 1); grid.Children.Add(editor.Name);
            Grid count = new Grid(); count.ColumnDefinitions.Add(new ColumnDefinition()); count.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) }); count.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            editor.Count = new Slider { Minimum = 0, Maximum = AmmoCapacity, TickFrequency = 1, IsSnapToTickEnabled = true, VerticalAlignment = VerticalAlignment.Center };
            editor.CountBox = ModernNumericBox.Create(); editor.CountBox.Height = 28; editor.CountBox.Padding = new Thickness(6, 2, 6, 2); editor.CountBox.Margin = new Thickness(4, 0, 0, 0);
            editor.Value = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            count.Children.Add(editor.Count); Grid.SetColumn(editor.CountBox, 1); count.Children.Add(editor.CountBox); Grid.SetColumn(editor.Value, 2); count.Children.Add(editor.Value); Grid.SetRow(count, 2); grid.Children.Add(count);
            editor.Count.ValueChanged += delegate { if (!updatingSlots) UpdateSlotCount(editor.Slot, (int)editor.Count.Value); };
            ModernNumericBox.Bind(editor.Count, editor.CountBox);
            editor.Card.Child = grid; slotEditors.Add(editor); return editor.Card;
        }

        // Radar swap lab: two Ask3lad-style pickers (search + track) over the 442-entry
        // sensor catalog, plus the AI-pair strip option. Selection is stored on the
        // aircraft settings and applied when the mission is generated.
        private void PickRadars()
        {
            List<ModernPickerItem> items = new List<ModernPickerItem>();
            foreach (SensorRowJson s in MainForm.SensorCatalog)
            {
                string roleTag = "";
                if (s.role == "search") roleTag = ModernText.L("SEARCH", "搜索") + " · ";
                else if (s.role == "track") roleTag = ModernText.L("TRACK", "跟踪") + " · ";
                if (s.domain == "air") roleTag = ModernText.L("AIR", "机载") + " · " + roleTag;
                string kmTag = "";
                double rmM;
                if (!String.IsNullOrWhiteSpace(s.rangeMax) && double.TryParse(s.rangeMax.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out rmM))
                    kmTag = (rmM >= 1000 ? (rmM / 1000.0).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "km" : rmM.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "m") + " · ";
                string bandTag = String.IsNullOrWhiteSpace(s.band) ? "" : "band " + s.band.Trim() + "  ";
                items.Add(new ModernPickerItem { Display = s.display, Detail = roleTag + kmTag + bandTag + s.id, Tag = s.id });
            }
            ModernPickerItem searchPick = null;
            string searchTitle = ModernText.L("SELECT SEARCH RADAR", "选择搜索雷达");
            if (String.IsNullOrWhiteSpace(radarSearchSel))
            {
                ModernPickerDialog searchDlg = new ModernPickerDialog(searchTitle, items, searchTitle);
                if (searchDlg.ShowDialog() == true && searchDlg.Selected != null) { radarSearchSel = (string)searchDlg.Selected.Tag; searchPick = searchDlg.Selected; }
            }
            ModernPickerItem trackPick = null;
            string trackTitle = ModernText.L("SELECT TRACK RADAR", "选择跟踪雷达");
            ModernPickerDialog dlg = new ModernPickerDialog(trackTitle, items, trackTitle);
            if (dlg.ShowDialog() == true && dlg.Selected != null) { radarTrackSel = (string)dlg.Selected.Tag; trackPick = dlg.Selected; }
            // Persist immediately to the live settings so panel rebuilds / vehicle switches keep the choice.
            if (currentSettings != null) { currentSettings.RadarSearchBlk = radarSearchSel; currentSettings.RadarTrackBlk = radarTrackSel; }
            UpdateRadarStatus();
        }

        // Resolve which search/track radar slots the native vehicle actually has, by
        // reading its tankmodel blk for sensor references and looking them up in the
        // 442-entry catalog (role search/track from fsm names). Best effort: on any
        // failure both slots stay null and the swap lab keeps its permissive behaviour.
        private void ResolveNativeSensors(Aircraft item)
        {
            if (String.IsNullOrWhiteSpace(gameRoot) || item == null) return;
            try
            {
                string blk = MainForm.ExtractGameBlk(gameRoot, "gamedata/units/tankmodels/" + item.Id.ToLowerInvariant() + ".blk");
                string text = File.ReadAllText(blk);
                List<string> ids = new List<string>();
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(text, "gameData/sensors/([\\w-]+)\\.blk"))
                    if (!ids.Contains(m.Groups[1].Value)) ids.Add(m.Groups[1].Value);
                foreach (string id in ids)
                {
                    SensorRowJson s = MainForm.SensorCatalog == null ? null : MainForm.SensorCatalog.FirstOrDefault(x => String.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
                    if (s == null) continue;
                    if (s.role == "search" && nativeSearchSensor == null) nativeSearchSensor = s;
                    else if (s.role == "track" && nativeTrackSensor == null) nativeTrackSensor = s;
                }
            }
            catch { }
        }

        // Format a catalog radar row into an in-construction style one-liner.
        private static string DescribeRadar(SensorRowJson s)
        {
            if (s == null) return null;
            List<string> parts = new List<string>();
            parts.Add(s.display + " (" + s.id + ")");
            if (!String.IsNullOrWhiteSpace(s.band))
            {
                int bn;
                if (int.TryParse(s.band.Trim(), out bn) && bn >= 4) parts.Add("band " + "DEFGHIJKLMNOPQRSTUVWX"[bn - 4]);
                else parts.Add("band " + s.band.Trim());
            }
            double rng;
            if (!String.IsNullOrWhiteSpace(s.rangeMax) && double.TryParse(s.rangeMax.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out rng))
                parts.Add((rng >= 1000 ? (rng / 1000.0).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " km" : rng.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " m"));
            if (s.role == "search") parts.Add(ModernText.L("search radar", "搜索雷达"));
            else if (s.role == "track") parts.Add(ModernText.L("track radar", "跟踪雷达"));
            if (!String.IsNullOrWhiteSpace(s.fsm))
            {
                string caps = String.Join("/", s.fsm.Split(',').Select(f => FsmWord(f.Trim())).ToArray());
                if (caps.Length > 0) parts.Add(caps);
            }
            if (!String.IsNullOrWhiteSpace(s.weaponTargetsMax)) parts.Add(ModernText.L("data-link targets", "数据链目标") + " " + s.weaponTargetsMax.Trim());
            if (s.irst == "1") parts.Add(ModernText.L("with IRST", "含红外通道"));
            if (s.domain == "air") parts.Add(ModernText.L("airborne", "机载"));
            return String.Join("  ·  ", parts.ToArray());
        }

        private static string FsmWord(string f)
        {
            switch (f.ToLowerInvariant())
            {
                case "lock": return ModernText.L("lock", "锁定");
                case "track": return ModernText.L("track", "跟踪");
                case "tws": return "TWS";
                case "search": return ModernText.L("search", "搜索");
                case "illumination": return ModernText.L("illumination", "照射");
                case "radartrack": return ModernText.L("radar track", "雷达跟踪");
                default: return f;
            }
        }

        private static SensorRowJson SensorById(string id)
        {
            if (String.IsNullOrWhiteSpace(id) || MainForm.SensorCatalog == null) return null;
            return MainForm.SensorCatalog.FirstOrDefault(x => String.Equals(x.id, id.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private void UpdateRadarStatus()
        {
            if (radarStatus == null) return;
            // Effective slot content: replacement (if set) falls back to the native radar.
            SensorRowJson effSearch = SensorById(radarSearchSel) ?? nativeSearchSensor;
            SensorRowJson effTrack = SensorById(radarTrackSel) ?? nativeTrackSensor;
            bool anyNative = nativeSearchSensor != null || nativeTrackSensor != null;
            string searchName = effSearch != null ? effSearch.display : (anyNative ? ModernText.L("(none)", "无") : ModernText.L("native", "原生"));
            string trackName = effTrack != null ? effTrack.display : (anyNative ? ModernText.L("(none)", "无") : ModernText.L("native", "原生"));
            radarStatus.Text = ModernText.L("SEARCH: ", "搜索雷达：") + searchName + "    " + ModernText.L("TRACK: ", "跟踪雷达：") + trackName;

            string searchLabel = ModernText.L("SEARCH SLOT", "搜索位") + (String.IsNullOrWhiteSpace(radarSearchSel) ? ModernText.L(" (native)", "（原生）") : ModernText.L(" (replaced)", "（替换）"));
            string trackLabel = ModernText.L("TRACK SLOT", "跟踪位") + (String.IsNullOrWhiteSpace(radarTrackSel) ? ModernText.L(" (native)", "（原生）") : ModernText.L(" (replaced)", "（替换）"));
            radarDetailSearch.Text = searchLabel + Environment.NewLine + (effSearch != null ? "   " + DescribeRadar(effSearch)
                : (anyNative ? "   " + ModernText.L("This vehicle has no search slot.", "此车无搜索雷达位（只有跟踪位）。")
                             : "   " + ModernText.L("No native sensors - swap disabled (needs a sensor structure first).", "无原生雷达——更换已禁用（需先有传感器结构）。")));
            radarDetailTrack.Text = trackLabel + Environment.NewLine + (effTrack != null ? "   " + DescribeRadar(effTrack)
                : (anyNative ? "   " + ModernText.L("This vehicle has no track slot.", "此车无跟踪雷达位（只有搜索位）。")
                             : "   " + ModernText.L("No native sensors.", "无原生雷达。")));
        }

        private void AddValue(StackPanel panel, string label, string key, double stock, double multiplier, string unit)
        {
            double initial = stock > 0 ? stock * multiplier : 0;
            tuningStock[key] = stock;
            Grid row = new Grid { Margin = new Thickness(0, 3, 0, 5) };
            row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });
            StackPanel labelStack = new StackPanel(); labelStack.Children.Add(Caption(label)); labelStack.Children.Add(new TextBlock { Text = ModernText.L("Stock: ", "默认弹: ") + FormatValue(stock) + " " + unit, Foreground = ModernPalette.Brush(ModernPalette.Muted), FontSize = 9, Margin = new Thickness(0, 2, 4, 0) }); row.Children.Add(labelStack);
            TextBox box = new TextBox { Text = FormatValue(initial), Height = 34, Padding = new Thickness(8, 3, 8, 3), Tag = unit }; Grid.SetColumn(box, 1); row.Children.Add(box);
            Button reset = new Button { Content = ModernText.L("RESET", "重置"), Style = buttonStyle, FontSize = 9, Padding = new Thickness(2), Margin = new Thickness(5, 0, 0, 0), Tag = key };
            reset.Click += delegate { ResetValue((string)reset.Tag); }; Grid.SetColumn(reset, 2); row.Children.Add(reset);
            panel.Children.Add(row); tuning[key] = box;
        }

        private static string FormatValue(double value) { return value.ToString(value >= 100 ? "0.##" : "0.####", CultureInfo.InvariantCulture); }

        private double ReadValue(string key)
        {
            if (!tuning.ContainsKey(key)) return 0;
            double value;
            string text = (tuning[key].Text ?? "").Trim();
            if (Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || Double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)) return Math.Max(0, value);
            return tuningStock[key];
        }

        private double Ratio(string key)
        {
            if (!tuningStock.ContainsKey(key)) return 1.0;
            double stock = tuningStock[key];
            return stock > 0 ? ReadValue(key) / stock : 1.0;
        }

        private void ResetValue(string key) { tuning[key].Text = FormatValue(tuningStock[key]); }
        private void ResetAllValues() { foreach (string key in tuning.Keys.ToList()) ResetValue(key); }
        private void UpdateTuningState() { bool enabled = overrideBallistics.IsChecked == true; foreach (TextBox value in tuning.Values) value.IsEnabled = enabled; }

        private GroundAmmo FindAmmoForLoadout(GroundAmmoLoadout entry)
        {
            if (entry == null) return null;
            GroundAmmo ammo = catalog.FirstOrDefault(x => x.SourceBlk != null && x.SourceBlk.Equals(entry.SourceBlk ?? "", StringComparison.OrdinalIgnoreCase) && x.BulletName != null && x.BulletName.Equals(entry.BulletName ?? "", StringComparison.OrdinalIgnoreCase));
            if (ammo == null && !String.IsNullOrWhiteSpace(entry.SourceBlk) && !String.IsNullOrWhiteSpace(entry.BulletName))
            {
                IList<GroundAmmo> resolved = ResolveCannonAmmoCached(entry.SourceBlk);
                ammo = resolved.FirstOrDefault(x => x.BulletName != null && x.BulletName.Equals(entry.BulletName, StringComparison.OrdinalIgnoreCase));
            }
            return ammo;
        }

        private GroundAmmo ResolveProjectileReference()
        {
            GroundAmmoLoadout entry;
            if (loadouts.TryGetValue(selectedSlot, out entry))
            {
                GroundAmmo selected = FindAmmoForLoadout(entry);
                if (selected != null) return selected;
            }
            return catalog.FirstOrDefault(x => x.SourceBlk.Equals(vehicle.MainWeaponBlk ?? "", StringComparison.OrdinalIgnoreCase)) ?? catalog.FirstOrDefault();
        }

        private void SetProjectileReference(GroundAmmo ammo)
        {
            if (ammo == null || tuning.Count == 0) return;
            projectileReference = ammo;
            SetProjectileStock("projectileMass", ammo.Mass, original.ProjectileMassMultiplier);
            SetProjectileStock("velocity", ammo.Speed, original.MuzzleVelocityMultiplier);
            SetProjectileStock("explosive", ammo.ExplosiveMass, original.ExplosiveMassMultiplier);
            SetProjectileStock("penetration", ammo.Penetration, original.PenetrationMultiplier);
        }

        private void SetProjectileStock(string key, double stock, double multiplier)
        {
            tuningStock[key] = stock;
            tuning[key].Text = FormatValue(stock * multiplier);
        }

        private List<GroundWeaponInfo> CannonWeapons { get { return groundWeapons.Where(x => !String.IsNullOrWhiteSpace(x.Blk) && x.Blk.IndexOf("_user_cannon", StringComparison.OrdinalIgnoreCase) >= 0).ToList(); } }
        private HashSet<string> CannonBlkSet { get { return new HashSet<string>(CannonWeapons.Select(x => NormalizeBlk(x.Blk)), StringComparer.OrdinalIgnoreCase); } }
        private bool SameBlk(string a, string b) { return !String.IsNullOrWhiteSpace(a) && !String.IsNullOrWhiteSpace(b) && NormalizeBlk(a) == NormalizeBlk(b); }
        private bool SameGun(GroundAmmoLoadout a, GroundAmmoLoadout b) { return SameBlk(a == null ? null : a.SourceBlk, b == null ? null : b.SourceBlk); }
        private int AmmoTotalFor(GroundAmmoLoadout entry)
        {
            if (entry == null || String.IsNullOrWhiteSpace(entry.SourceBlk)) return AmmoCapacity;
            if (currentCannon != null && !currentCannon.IsNative && SameBlk(currentCannon.Blk, entry.SourceBlk)) return 9999;
            GroundWeaponInfo info = groundWeapons.FirstOrDefault(x => SameBlk(x.Blk, entry.SourceBlk));
            return info == null ? AmmoCapacity : (info.NativeAmmo > 0 ? info.NativeAmmo : (info.NativeAmmo < 0 ? 9999 : AmmoCapacity));
        }
        private static string CannonShortName(GroundWeaponInfo x)
        {
            string blk = x.Blk ?? "";
            string f = blk.Substring(blk.LastIndexOf('/') + 1).Replace("_user_cannon", "").Replace(".blk", "").Replace('_', ' ');
            return f;
        }

        private void BuildCannonSelector()
        {
            if (cannonSelector == null) return;
            if (simplified)
            {
                // Home panel: native weapons only (no cross-domain cannon list).
                foreach (GroundWeaponInfo gw in groundWeapons.Where(x => !String.IsNullOrWhiteSpace(x.Blk)).OrderBy(x => x.Display))
                    cannonSelector.Items.Add(new ComboBoxItem { Content = ModernText.L("NATIVE  \u2022  ", "原生  \u2022  ") + gw.Display, Tag = new CannonChoice { Blk = gw.Blk, Display = gw.Display, IsNative = true } });
                if (cannonSelector.Items.Count == 0 && !String.IsNullOrWhiteSpace(vehicle.MainWeaponBlk))
                    cannonSelector.Items.Add(new ComboBoxItem { Content = ModernText.L("NATIVE  \u2022  PRIMARY", "原生  \u2022  主武器"), Tag = new CannonChoice { Blk = vehicle.MainWeaponBlk, Display = "PRIMARY", IsNative = true } });
                return;
            }
            List<GroundWeaponInfo> mains = new List<GroundWeaponInfo>();
            List<GroundWeaponInfo> secondary = new List<GroundWeaponInfo>();
            foreach (GroundWeaponInfo gw in groundWeapons.Where(x => !String.IsNullOrWhiteSpace(x.Blk)))
            {
                if (ModernMainWindow.IsSecondaryGroundWeapon(gw.Blk)) secondary.Add(gw);
                else mains.Add(gw);
            }
            bool anyNative = false;
            foreach (GroundWeaponInfo gw in mains)
            {
                cannonSelector.Items.Add(new ComboBoxItem { Content = ModernText.L("NATIVE  •  ", "原生  •  ") + gw.Display, Tag = new CannonChoice { Blk = gw.Blk, Display = gw.Display, IsNative = true } });
                anyNative = true;
            }
            if (!anyNative && secondary.Count > 0)
            {
                foreach (GroundWeaponInfo gw in secondary)
                {
                    cannonSelector.Items.Add(new ComboBoxItem { Content = ModernText.L("NATIVE  •  ", "原生  •  ") + gw.Display, Tag = new CannonChoice { Blk = gw.Blk, Display = gw.Display, IsNative = true } });
                    anyNative = true;
                }
            }
            if (!anyNative && !String.IsNullOrWhiteSpace(vehicle.MainWeaponBlk))
                cannonSelector.Items.Add(new ComboBoxItem { Content = ModernText.L("NATIVE  •  PRIMARY", "原生  •  主武器"), Tag = new CannonChoice { Blk = vehicle.MainWeaponBlk, Display = "PRIMARY", IsNative = true } });
            if (mains.Count > 0 && secondary.Count > 0)
            {
                cannonSelector.Items.Add(new ComboBoxItem { Content = ModernText.L("— SECONDARY (MACHINE GUNS / SMOKE) —", "— 次要武器（机枪 / 烟雾）—"), IsEnabled = false, Foreground = System.Windows.Media.Brushes.Gray });
                foreach (GroundWeaponInfo gw in secondary)
                    cannonSelector.Items.Add(new ComboBoxItem { Content = ModernText.L("NATIVE  •  ", "原生  •  ") + gw.Display, Foreground = System.Windows.Media.Brushes.Gray, Tag = new CannonChoice { Blk = gw.Blk, Display = gw.Display, IsNative = true } });
            }
            foreach (string domain in new[] { "ground", "naval", "aircraft", "helicopter" })
            {
                List<UnitWeapon> domainWeapons = unitWeapons.Where(x => String.Equals(x.Domain, domain, StringComparison.OrdinalIgnoreCase)).ToList();
                if (domainWeapons.Count == 0) continue;
                foreach (UnitWeapon uw in domainWeapons.OrderBy(x => x.UnitDisplay, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.WeaponDisplay, StringComparer.OrdinalIgnoreCase))
                    cannonSelector.Items.Add(new ComboBoxItem { Content = domain.ToUpperInvariant() + "  •  " + uw.UnitDisplay + "  •  " + uw.WeaponDisplay, Tag = new CannonChoice { Blk = uw.WeaponBlk, Display = uw.WeaponDisplay, IsNative = false, Domain = domain, UnitId = uw.UnitId } });
            }
        }

        private void SelectInitialCannon()
        {
            if (cannonSelector == null || cannonSelector.Items.Count == 0) return;
            CannonChoice initial = null;
            if (!String.IsNullOrWhiteSpace(original.InjectedCannonBlk))
            {
                string norm = NormalizeBlk(original.InjectedCannonBlk);
                initial = cannonSelector.Items.OfType<ComboBoxItem>().Select(x => x.Tag as CannonChoice)
                    .FirstOrDefault(c => c != null && !c.IsNative && NormalizeBlk(c.Blk).Equals(norm, StringComparison.OrdinalIgnoreCase));
            }
            if (initial == null)
                initial = cannonSelector.Items.OfType<ComboBoxItem>().Select(x => x.Tag as CannonChoice).FirstOrDefault(c => c != null && c.IsNative);
            if (initial == null)
                initial = cannonSelector.Items.OfType<ComboBoxItem>().Select(x => x.Tag as CannonChoice).FirstOrDefault(c => c != null);
            if (initial == null) return;
            syncingCannon = true;
            try
            {
                ComboBoxItem item = cannonSelector.Items.OfType<ComboBoxItem>().FirstOrDefault(x => (x.Tag as CannonChoice) == initial);
                if (item != null) cannonSelector.SelectedItem = item;
            }
            finally { syncingCannon = false; }
            currentCannon = initial;
            if (initial.IsNative)
            {
                syncingCannon = true;
                try { if (cannonBox != null && cannonBox.SelectedIndex != -1) cannonBox.SelectedIndex = -1; }
                finally { syncingCannon = false; }
            }
            RefreshAmmo();
            RefreshSlotEditors();
        }

        private void CannonSelectorChanged()
        {
            if (syncingCannon) return;
            ComboBoxItem item = cannonSelector == null ? null : cannonSelector.SelectedItem as ComboBoxItem;
            CannonChoice choice = item == null ? null : item.Tag as CannonChoice;
            if (choice == null) return;
            ApplyCannonSelection(choice, false);
        }

        private void ApplyCannonSelection(CannonChoice choice, bool fromRightSide)
        {
            if (choice == null) return;
            currentCannon = choice;
            RefreshAmmo();
            RefreshSlotEditors();
            if (!fromRightSide) SyncRightSideCannon(choice);
        }

        private void SyncRightSideCannon(CannonChoice choice)
        {
            if (syncingCannon || domainBox == null || unitBox == null || cannonBox == null) return;
            syncingCannon = true;
            try
            {
                if (choice.IsNative)
                {
                    if (cannonBox.SelectedIndex != -1) cannonBox.SelectedIndex = -1;
                    return;
                }
                ComboBoxItem domainItem = domainBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => String.Equals(x.Tag as string, choice.Domain, StringComparison.OrdinalIgnoreCase));
                if (domainItem != null && domainBox.SelectedItem != domainItem) domainBox.SelectedItem = domainItem;
                ComboBoxItem unitItem = unitBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => String.Equals(x.Tag as string, choice.UnitId, StringComparison.OrdinalIgnoreCase));
                if (unitItem != null && unitBox.SelectedItem != unitItem) unitBox.SelectedItem = unitItem;
                ComboBoxItem cannonItem = cannonBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => x.Tag is string && NormalizeBlk((string)x.Tag).Equals(NormalizeBlk(choice.Blk), StringComparison.OrdinalIgnoreCase));
                if (cannonItem != null && cannonBox.SelectedItem != cannonItem) cannonBox.SelectedItem = cannonItem;
            }
            finally { syncingCannon = false; }
        }

        private void SyncLeftCannon()
        {
            if (syncingCannon || cannonSelector == null) return;
            ComboBoxItem item = cannonBox == null ? null : cannonBox.SelectedItem as ComboBoxItem;
            if (item == null || !(item.Tag is string)) return;
            string blk = (string)item.Tag;
            CannonChoice match = cannonSelector.Items.OfType<ComboBoxItem>().Select(x => x.Tag as CannonChoice)
                .FirstOrDefault(c => c != null && !c.IsNative && NormalizeBlk(c.Blk).Equals(NormalizeBlk(blk), StringComparison.OrdinalIgnoreCase));
            if (match == null) return;
            syncingCannon = true;
            try
            {
                ComboBoxItem target = cannonSelector.Items.OfType<ComboBoxItem>().FirstOrDefault(x => (x.Tag as CannonChoice) == match);
                if (target != null && cannonSelector.SelectedItem != target) cannonSelector.SelectedItem = target;
            }
            finally { syncingCannon = false; }
            if (currentCannon == null || !SameBlk(currentCannon.Blk, match.Blk))
            {
                currentCannon = match;
                RefreshAmmo();
                RefreshSlotEditors();
            }
        }

        private void SyncRoundToSlot()
        {
            if (syncingCannon || roundBox == null || currentCannon == null || currentCannon.IsNative) return;
            ComboBoxItem item = roundBox.SelectedItem as ComboBoxItem;
            string tag = item == null ? null : (item.Tag as string);
            if (String.IsNullOrWhiteSpace(tag)) return;
            IEnumerable<GroundAmmo> source = ammoList.ItemsSource as IEnumerable<GroundAmmo>;
            if (source == null) return;
            GroundAmmo ammo = source.FirstOrDefault(x => x.BulletName != null && x.BulletName.Equals(tag, StringComparison.OrdinalIgnoreCase));
            if (ammo == null) return;
            GroundAmmoLoadout existing; loadouts.TryGetValue(selectedSlot, out existing);
            int count = existing == null ? 1 : Math.Max(1, existing.Count);
            loadouts[selectedSlot] = new GroundAmmoLoadout { Slot = selectedSlot, Count = count, SourceBlk = currentCannon.Blk, BulletName = ammo.BulletName };
            SetProjectileReference(ammo);
            RefreshSlotEditors();
            SelectSlot(selectedSlot);
        }

        private IList<GroundAmmo> ResolveCannonAmmoCached(string blk)
        {
            if (String.IsNullOrWhiteSpace(blk)) return new List<GroundAmmo>();
            string key = NormalizeBlk(blk);
            IList<GroundAmmo> cached;
            if (!cannonAmmoCache.TryGetValue(key, out cached))
            {
                cached = resolveCannonAmmo == null ? new List<GroundAmmo>() : (resolveCannonAmmo(blk) ?? new List<GroundAmmo>());
                cannonAmmoCache[key] = cached;
            }
            return cached;
        }

        private void RefreshAmmo()
        {
            IEnumerable<GroundAmmo> query;
            if (currentCannon != null && !currentCannon.IsNative)
            {
                query = ResolveCannonAmmoCached(currentCannon.Blk);
                if (injectionToggle != null && injectionToggle.IsChecked == true) query = query.Concat(catalog);
            }
            else if (currentCannon != null && currentCannon.IsNative && injectionToggle == null || injectionToggle.IsChecked != true)
            {
                // Native cannon: only rounds whose cannon container belongs to this
                // vehicle's ammo packages (beltOptions) - the same cannon can serve
                // vehicles with different ammunition (Type16 vs Type16 FPS).
                query = catalog.Where(x => SameBlk(x.SourceBlk, currentCannon.Blk) && ContainerAllowed(x.Container));
            }
            else
            {
                query = catalog;
                if (injectionToggle.IsChecked != true && CannonBlkSet.Count > 0) query = query.Where(x => CannonBlkSet.Contains(x.SourceBlk));
            }
            if (injectedCannonAmmo != null && injectedCannonAmmo.Count > 0) query = query.Concat(injectedCannonAmmo);
            if (simplified)
            {
                // STOCK (native default round) is the first entry; it is written as an
                // empty slot (bulletsN:t="" + count) exactly like Ask3lad.
                int stockCal = currentCannon == null ? 0 : ModernMainWindow.GroundCalibre(currentCannon.Blk);
                query = new[] { new GroundAmmo { SourceBlk = "stock:" + stockCal.ToString(CultureInfo.InvariantCulture), BulletName = "", Display = ModernText.L("STOCK \u2022 default ammunition", "STOCK \u2022 default ammunition"), Type = "Default", Caliber = stockCal } }.Concat(query);
            }
            string search = (searchBox.Text ?? "").Trim(); if (search.Length > 0) query = query.Where(x => x.Display.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 || x.BulletName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || x.Type.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            string type = typeBox.SelectedIndex > 0 ? typeBox.SelectedItem as string : null; if (!String.IsNullOrEmpty(type)) query = query.Where(x => x.Type == type);
            ammoList.ItemsSource = query.OrderBy(x => x.Caliber).ThenBy(x => x.Type).ThenBy(x => x.Display).ToList();
        }

        private void SelectSlot(int slot)
        {
            selectedSlot = slot;
            foreach (GroundAmmoSlotEditor editor in slotEditors) editor.Card.BorderBrush = editor.Slot == slot ? ModernPalette.Brush(ModernPalette.Cyan) : ModernPalette.Brush(ModernPalette.Border);
            SetProjectileReference(ResolveProjectileReference());
        }

        private void MountSelectedAmmo()
        {
            GroundAmmo ammo = ammoList.SelectedItem as GroundAmmo; if (ammo == null) return;
            GroundAmmoLoadout existing; loadouts.TryGetValue(selectedSlot, out existing);
            if (simplified && ammo.BulletName != null && ammo.BulletName.Length == 0)
            {
                // STOCK: empty slot + count -> the game loads the native default round.
                int stockOthers = loadouts.Values.Where(x => x.Slot != selectedSlot && x.SourceBlk != null && x.SourceBlk.StartsWith("stock:", StringComparison.OrdinalIgnoreCase)).Sum(x => Math.Max(0, x.Count));
                int stockAvailable = Math.Max(0, AmmoCapacity - stockOthers);
                int stockCount = existing == null ? Math.Max(1, stockAvailable) : Math.Min(Math.Max(1, existing.Count), stockAvailable);
                loadouts[selectedSlot] = new GroundAmmoLoadout { Slot = selectedSlot, Count = stockCount, SourceBlk = ammo.SourceBlk, BulletName = "" };
                SetProjectileReference(ammo); RefreshSlotEditors(); SelectSlot(selectedSlot);
                return;
            }
            int others = loadouts.Values.Where(x => x.Slot != selectedSlot && SameBlk(x.SourceBlk, ammo.SourceBlk)).Sum(x => Math.Max(0, x.Count));
            int available = Math.Max(0, AmmoTotalFor(new GroundAmmoLoadout { SourceBlk = ammo.SourceBlk }) - others);
            int count;
            if (currentCannon != null && !currentCannon.IsNative)
            {
                count = existing == null ? 9999 : Math.Max(1, existing.Count);
            }
            else
            {
                count = existing == null ? (simplified ? Math.Max(1, available) : Math.Min(1, available)) : Math.Min(Math.Max(1, existing.Count), available);
            }
            loadouts[selectedSlot] = new GroundAmmoLoadout { Slot = selectedSlot, Count = count, SourceBlk = ammo.SourceBlk, BulletName = ammo.BulletName };
            SetProjectileReference(ammo); RefreshSlotEditors(); SelectSlot(selectedSlot);
        }

        private void UpdateSlotCount(int slot, int count)
        {
            GroundAmmoLoadout entry;
            if (loadouts.TryGetValue(slot, out entry))
            {
                int gunTotal = AmmoTotalFor(entry);
                int others = loadouts.Values.Where(x => x.Slot != slot && SameGun(x, entry)).Sum(x => Math.Max(0, x.Count));
                int allowedMaximum = Math.Max(0, gunTotal - others);
                entry.Count = Math.Max(0, Math.Min(count, allowedMaximum));
            }
            RefreshSlotEditors();
        }

        private void RefreshSlotEditors()
        {
            updatingSlots = true;
            try
            {
                foreach (GroundAmmoLoadout entry in loadouts.Values.OrderBy(x => x.Slot))
                {
                    int gunTotal = AmmoTotalFor(entry);
                    entry.Count = Math.Max(0, Math.Min(entry.Count, gunTotal));
                }
                foreach (GroundAmmoSlotEditor editor in slotEditors)
                {
                    GroundAmmoLoadout entry; loadouts.TryGetValue(editor.Slot, out entry);
                    int others = loadouts.Values.Where(x => x.Slot != editor.Slot && SameGun(x, entry)).Sum(x => Math.Max(0, x.Count));
                    int gunTotal = AmmoTotalFor(entry);
                    int allowedMaximum = Math.Max(0, gunTotal - others);
                    int current = entry == null ? 0 : Math.Max(0, entry.Count);
                    editor.Count.Maximum = gunTotal;
                    editor.Count.Value = current;
                    editor.Value.Text = current.ToString(CultureInfo.InvariantCulture) + " / " + allowedMaximum.ToString(CultureInfo.InvariantCulture);
                    editor.Count.ToolTip = ModernText.L("Loaded: ", "已加载: ") + current.ToString(CultureInfo.InvariantCulture) + "  •  Maximum currently available: " + allowedMaximum.ToString(CultureInfo.InvariantCulture);
                    GroundAmmo ammo = FindAmmoForLoadout(entry);
                    if (entry != null && entry.SourceBlk != null && entry.SourceBlk.StartsWith("stock:", StringComparison.OrdinalIgnoreCase)) editor.Name.Text = ModernText.L("STOCK • default ammunition", "STOCK • 默认弹药");
                    else editor.Name.Text = ammo == null ? ModernText.L("EMPTY", "空") : ammo.Display + "  •  " + ammo.Type;
                }
                if (currentCannon != null && !currentCannon.IsNative)
                {
                    int used = loadouts.Values.Where(l => SameBlk(l.SourceBlk, currentCannon.Blk)).Sum(l => Math.Max(0, l.Count));
                    totalAmmoText.Text = CannonDisplayName(currentCannon.Blk).ToUpperInvariant() + ": " + used.ToString(CultureInfo.InvariantCulture) + "/9999";
                }
                else
                {
                    totalAmmoText.Text = String.Join("    ", CannonWeapons.Select(x =>
                    {
                        int used = loadouts.Values.Where(l => SameBlk(l.SourceBlk, x.Blk)).Sum(l => Math.Max(0, l.Count));
                        return CannonShortName(x) + ": " + used.ToString(CultureInfo.InvariantCulture) + "/" + (x.NativeAmmo > 0 ? x.NativeAmmo : (x.NativeAmmo < 0 ? 9999 : AmmoCapacity)).ToString(CultureInfo.InvariantCulture);
                    }));
                }
            }
            finally { updatingSlots = false; }
        }

        private void RefreshUnitBox()
        {
            if (unitBox == null) return;
            unitBox.Items.Clear();
            ComboBoxItem domainItem = domainBox.SelectedItem as ComboBoxItem;
            string domain = domainItem == null ? "ground" : (domainItem.Tag as string ?? "ground");
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (UnitWeapon uw in unitWeapons.Where(x => String.Equals(x.Domain, domain, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.UnitDisplay))
            {
                if (!seen.Add(uw.UnitId)) continue;
                unitBox.Items.Add(new ComboBoxItem { Content = uw.UnitDisplay, Tag = uw.UnitId });
            }
            if (unitBox.Items.Count == 0) unitBox.Items.Add(new ComboBoxItem { Content = ModernText.L("(no units in this domain)", "（该领域无单位）"), Tag = null });
            if (!String.IsNullOrWhiteSpace(original.InjectedCannonUnit))
            {
                ComboBoxItem match = unitBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => String.Equals(x.Tag as string, original.InjectedCannonUnit, StringComparison.OrdinalIgnoreCase));
                if (match != null) unitBox.SelectedItem = match;
            }
        }

        private void RefreshCannonBox()
        {
            if (cannonBox == null) return;
            cannonBox.Items.Clear();
            ComboBoxItem domainItem = domainBox.SelectedItem as ComboBoxItem;
            string domain = domainItem == null ? "ground" : (domainItem.Tag as string ?? "ground");
            ComboBoxItem unitItem = unitBox == null ? null : unitBox.SelectedItem as ComboBoxItem;
            string unitId = unitItem == null ? null : unitItem.Tag as string;
            if (String.IsNullOrEmpty(unitId))
            {
                cannonBox.Items.Add(new ComboBoxItem { Content = ModernText.L("(select a unit)", "（选择单位）"), Tag = null });
                return;
            }
            foreach (UnitWeapon uw in unitWeapons.Where(x => String.Equals(x.Domain, domain, StringComparison.OrdinalIgnoreCase) && String.Equals(x.UnitId, unitId, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.WeaponDisplay))
                cannonBox.Items.Add(new ComboBoxItem { Content = uw.WeaponDisplay, Tag = uw.WeaponBlk });
            if (!String.IsNullOrWhiteSpace(original.InjectedCannonBlk))
            {
                string saved = NormalizeBlk(original.InjectedCannonBlk);
                ComboBoxItem match = cannonBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => x.Tag is string && NormalizeBlk((string)x.Tag).Equals(saved, StringComparison.OrdinalIgnoreCase));
                if (match != null) cannonBox.SelectedItem = match;
            }
        }
        private void RefreshRoundBox()
        {
            if (roundBox == null || resolveCannonAmmo == null) return;
            ComboBoxItem cannonItem = cannonBox == null ? null : cannonBox.SelectedItem as ComboBoxItem;
            string blk = cannonItem == null ? null : (cannonItem.Tag as string);
            ComboBoxItem previous = roundBox.SelectedItem as ComboBoxItem;
            string previousTag = previous == null ? null : (previous.Tag as string);
            roundBox.Items.Clear();
            roundBox.Items.Add(new ComboBoxItem { Content = ModernText.L("ALL (native rounds)", "全部（原生炮弹）"), Tag = "" });
            if (!String.IsNullOrWhiteSpace(blk))
            {
                foreach (GroundAmmo ammo in ResolveCannonAmmoCached(blk))
                    roundBox.Items.Add(new ComboBoxItem { Content = ammo.Display, Tag = ammo.BulletName });
            }
            ComboBoxItem restored = null;
            if (!String.IsNullOrWhiteSpace(previousTag))
                restored = roundBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => String.Equals(x.Tag as string, previousTag, StringComparison.OrdinalIgnoreCase));
            if (restored == null && !String.IsNullOrWhiteSpace(original.InjectedCannonRound))
                restored = roundBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => String.Equals(x.Tag as string, original.InjectedCannonRound, StringComparison.OrdinalIgnoreCase));
            if (restored != null) roundBox.SelectedItem = restored;
            else roundBox.SelectedIndex = 0;
        }

        private static string NormalizeBlk(string path)
        {
            return (path ?? "").Replace('\\', '/').TrimStart('/').ToLowerInvariant();
        }

        private static string CannonDisplayName(string blk)
        {
            string normalized = NormalizeBlk(blk);
            string file = normalized.Substring(normalized.LastIndexOf('/') + 1);
            file = file.Replace("_user_cannon", "").Replace("_user_machinegun", "").Replace(".blk", "");
            return file.Replace('_', ' ');
        }

        private TextBlock Caption(string text)
        {
            return new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ModernPalette.Brush(ModernPalette.Muted) };
        }

        private TextBlock Heading(string text, double size)
        {
            return new TextBlock { Text = text, FontSize = size, FontWeight = FontWeights.SemiBold, Foreground = ModernPalette.Brush(ModernPalette.Text) };
        }

        public AircraftSettings Collect()
        {
            AircraftSettings result = original.Copy(); result.GroundAmmoLoadouts.Clear(); foreach (GroundAmmoLoadout entry in loadouts.Values.Where(x => x.Count > 0).OrderBy(x => x.Slot)) result.GroundAmmoLoadouts.Add(entry.Copy());
            if (simplified)
            {
                // The home panel only configures native ammunition; leave any injected
                // cross-domain cannon (EXPERIMENTAL GROUND CONFIGURE) untouched.
                result.InjectedCannonBlk = original.InjectedCannonBlk;
                result.InjectedCannonDomain = original.InjectedCannonDomain;
                result.InjectedCannonUnit = original.InjectedCannonUnit;
                result.InjectedCannonRound = original.InjectedCannonRound;
                return result;
            }
            result.OverrideGroundBallistics = overrideBallistics != null && overrideBallistics.IsChecked == true;
            result.ProjectileMassMultiplier = result.OverrideGroundBallistics ? Ratio("projectileMass") : 1; result.MuzzleVelocityMultiplier = result.OverrideGroundBallistics ? Ratio("velocity") : 1; result.ExplosiveMassMultiplier = result.OverrideGroundBallistics ? Ratio("explosive") : 1; result.PenetrationMultiplier = result.OverrideGroundBallistics ? Ratio("penetration") : 1;
            result.ReloadSeconds = ReadValue("reload"); result.RecoilMultiplier = result.OverrideGroundBallistics ? Ratio("recoil") : 1; result.EnginePowerMultiplier = result.OverrideGroundBallistics ? Ratio("engine") : 1; result.VehicleMassMultiplier = result.OverrideGroundBallistics ? Ratio("mass") : 1; result.ForwardSpeedMultiplier = result.OverrideGroundBallistics ? Ratio("forward") : 1; result.ReverseSpeedMultiplier = result.OverrideGroundBallistics ? Ratio("reverse") : 1;
            CannonChoice cannonChoice = cannonSelector == null ? null : ((cannonSelector.SelectedItem as ComboBoxItem) == null ? null : (cannonSelector.SelectedItem as ComboBoxItem).Tag as CannonChoice);
            if (cannonChoice != null && cannonChoice.IsNative)
            {
                result.InjectedCannonBlk = null;
                result.InjectedCannonDomain = null;
                result.InjectedCannonUnit = null;
                result.InjectedCannonRound = null;
            }
            else if (cannonChoice != null)
            {
                result.InjectedCannonBlk = cannonChoice.Blk;
                result.InjectedCannonDomain = cannonChoice.Domain;
                result.InjectedCannonUnit = cannonChoice.UnitId;
                ComboBoxItem roundSelection = roundBox == null ? null : roundBox.SelectedItem as ComboBoxItem;
                result.InjectedCannonRound = roundSelection == null || !(roundSelection.Tag is string) ? null : (string)roundSelection.Tag;
            }
            else
            {
                ComboBoxItem cannonSelection = cannonBox == null ? null : cannonBox.SelectedItem as ComboBoxItem;
                ComboBoxItem domainSelection = domainBox == null ? null : domainBox.SelectedItem as ComboBoxItem;
                ComboBoxItem unitSelection = unitBox == null ? null : unitBox.SelectedItem as ComboBoxItem;
                result.InjectedCannonBlk = cannonSelection == null || !(cannonSelection.Tag is string) ? null : (string)cannonSelection.Tag;
                result.InjectedCannonDomain = cannonSelection == null ? null : (domainSelection == null ? "ground" : (domainSelection.Tag as string ?? "ground"));
                result.InjectedCannonUnit = cannonSelection == null ? null : (unitSelection == null ? null : (unitSelection.Tag as string));
                ComboBoxItem roundSelection = roundBox == null ? null : roundBox.SelectedItem as ComboBoxItem;
                result.InjectedCannonRound = roundSelection == null || !(roundSelection.Tag is string) ? null : (string)roundSelection.Tag;
            }
            result.UnlimitedAmmo = ammoUnlimitedBox == null ? original.UnlimitedAmmo : ammoUnlimitedBox.IsChecked == true;
            result.FakeArhConversion = fakeArhBox == null ? original.FakeArhConversion : fakeArhBox.IsChecked == true;
            result.RadarSearchBlk = radarSearchSel;
            result.RadarTrackBlk = radarTrackSel;
            result.RadarStripAiSensors = stripAiBox == null ? original.RadarStripAiSensors : stripAiBox.IsChecked == true;
            return result;
        }
    }

    internal sealed class ModernGroundConfigureWindow : ModernDialogWindow
    {
        // Home panel simplified mode flag (window is always the full cross-domain lab).
        private readonly bool simplified = false;
        private Style buttonStyle { get { return (Style)DialogRoot.Resources["ButtonStyle"]; } }
        private readonly Aircraft vehicle;
        private readonly AircraftSettings original;
        private readonly List<GroundAmmo> catalog;
        private readonly Dictionary<int, GroundAmmoLoadout> loadouts = new Dictionary<int, GroundAmmoLoadout>();
        private readonly List<GroundAmmoSlotEditor> slotEditors = new List<GroundAmmoSlotEditor>();
        private readonly Dictionary<string, TextBox> tuning = new Dictionary<string, TextBox>();
        private readonly Dictionary<string, double> tuningStock = new Dictionary<string, double>();
        private readonly TextBox searchBox;
        private readonly ComboBox typeBox;
        private readonly ToggleButton injectionToggle;
        private readonly ComboBox domainBox;
        private readonly ComboBox unitBox;
        private readonly ComboBox cannonBox;
        private readonly ComboBox roundBox;
        private readonly Func<string, IList<GroundAmmo>> resolveCannonAmmo;
        private readonly CheckBox ammoUnlimitedBox;
        private readonly Dictionary<string, IList<GroundAmmo>> cannonAmmoCache = new Dictionary<string, IList<GroundAmmo>>(StringComparer.OrdinalIgnoreCase);
        private ComboBox cannonSelector;
        private CannonChoice currentCannon;
        private bool syncingCannon;
        private readonly IList<UnitWeapon> unitWeapons;
        private readonly IList<GroundAmmo> injectedCannonAmmo;
        private readonly IList<GroundWeaponBeltOption> vehicleBeltOptions;
        private readonly IList<GroundWeaponInfo> groundWeapons;
                private readonly List<TargetUnit> groundVehicles;
        private readonly ListBox ammoList;
        private readonly TextBlock totalAmmoText;
        private readonly CheckBox overrideBallistics;
        private GroundAmmo projectileReference;
        private int selectedSlot;
        private bool updatingSlots;
        public AircraftSettings Result { get; private set; }

        private int AmmoCapacity { get { return vehicle.MaxAmmo > 0 ? vehicle.MaxAmmo : 200; } }

        private bool ContainerAllowed(string container)
        {
            // Unnamed default rounds (empty container) are the STOCK ammunition - they
            // cannot be written into a slot by name (the game falls back and shows a
            // wrong round), so they are hidden here; STOCK covers them.
            if (String.IsNullOrWhiteSpace(container)) return false;
            if (vehicleBeltOptions == null || vehicleBeltOptions.Count == 0) return true; // no data -> keep old behaviour
            foreach (GroundWeaponBeltOption belt in vehicleBeltOptions)
                if (belt != null && String.Equals(belt.Name, container, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public ModernGroundConfigureWindow(Aircraft item, AircraftSettings current, IEnumerable<GroundAmmo> ammo, IEnumerable<TargetUnit> groundVehicles, IEnumerable<UnitWeapon> unitWeapons, IEnumerable<GroundWeaponInfo> groundWeapons, IEnumerable<GroundAmmo> injectedCannonAmmo, IEnumerable<GroundWeaponBeltOption> beltOptions, Func<string, IList<GroundAmmo>> resolveCannonAmmo) : base("Ground Configure — " + item.Display, 1180, 780)
        {
            vehicle = item;
            original = (current ?? new AircraftSettings()).Copy();
            // Fall back to the globally remembered cannon injection so the last
            // domain/unit/weapon choice is reused across vehicles and sessions.
            if (String.IsNullOrWhiteSpace(original.InjectedCannonBlk))
            {
                original.InjectedCannonBlk = MissionSettings.Current.InjectedCannonBlk;
                original.InjectedCannonDomain = MissionSettings.Current.InjectedCannonDomain;
                original.InjectedCannonUnit = MissionSettings.Current.InjectedCannonUnit;
            }
            catalog = (ammo ?? Enumerable.Empty<GroundAmmo>()).ToList();
            this.groundVehicles = (groundVehicles ?? Enumerable.Empty<TargetUnit>()).Where(v => !String.IsNullOrWhiteSpace(v.MainWeaponBlk)).ToList();
            this.unitWeapons = (unitWeapons ?? Enumerable.Empty<UnitWeapon>()).ToList();
            this.groundWeapons = (groundWeapons ?? Enumerable.Empty<GroundWeaponInfo>()).ToList();
            this.injectedCannonAmmo = (injectedCannonAmmo ?? Enumerable.Empty<GroundAmmo>()).ToList();
            this.vehicleBeltOptions = (beltOptions ?? Enumerable.Empty<GroundWeaponBeltOption>()).ToList();
            this.resolveCannonAmmo = resolveCannonAmmo;
            foreach (GroundAmmoLoadout entry in original.GroundAmmoLoadouts.Where(x => x.Slot >= 0 && x.Slot < 4)) loadouts[entry.Slot] = entry.Copy();

            Grid layout = new Grid { ClipToBounds = true };
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
            ContentCard.Child = layout;
            StackPanel header = new StackPanel();
            header.Children.Add(Heading(ModernText.L("GROUND CONFIGURE", "地面配置"), 22));
            header.Children.Add(new TextBlock { Text = item.Display + "  •  ammunition, projectile, cannon and mobility setup", Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 4, 0, 0) });
            layout.Children.Add(header);

            Grid body = new Grid { Margin = new Thickness(0, 6, 0, 10), ClipToBounds = true };
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.25, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(body, 1); layout.Children.Add(body);

            Border ammoCard = Card(); Grid ammoGrid = new Grid { ClipToBounds = true };
            ammoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
            ammoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) });
            ammoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
            ammoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            ammoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(simplified ? 0 : 174) });
            ammoGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(simplified ? 0 : 48) });
            Grid ammoHeader = new Grid(); ammoHeader.ColumnDefinitions.Add(new ColumnDefinition()); ammoHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ammoHeader.Children.Add(Heading("AMMUNITION & PROJECTILE INJECTION", 15));
            totalAmmoText = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(totalAmmoText, 1); ammoHeader.Children.Add(totalAmmoText); ammoGrid.Children.Add(ammoHeader);
            Grid cannonRow = new Grid { Margin = new Thickness(0, 1, 0, 1) }; cannonRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); cannonRow.ColumnDefinitions.Add(new ColumnDefinition());
            TextBlock cannonCaption = new TextBlock { Text = ModernText.L("CANNON", "主炮"), Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontWeight = FontWeights.SemiBold, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            cannonSelector = new ComboBox { Foreground = ModernPalette.Brush(ModernPalette.Text), Background = ModernPalette.Brush("#FF16283E"), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Padding = new Thickness(8, 3, 8, 3), Height = 32, HorizontalAlignment = HorizontalAlignment.Stretch, IsTextSearchEnabled = true, IsTextSearchCaseSensitive = false, ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel))) };
            cannonSelector.SelectionChanged += delegate { CannonSelectorChanged(); };
            cannonRow.Children.Add(cannonCaption); Grid.SetColumn(cannonSelector, 1); cannonRow.Children.Add(cannonSelector); Grid.SetRow(cannonRow, 1); ammoGrid.Children.Add(cannonRow);
            Grid filters = new Grid(); filters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) }); filters.ColumnDefinitions.Add(new ColumnDefinition()); filters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            injectionToggle = new ToggleButton { Content = ModernText.L("INJECT ANY SHELL", "注入任意炮弹"), Style = (Style)DialogRoot.Resources["ToggleStyle"], Margin = new Thickness(0, 3, 8, 3) };
            searchBox = new TextBox { Margin = new Thickness(0, 3, 8, 3) }; Grid.SetColumn(searchBox, 1);
            typeBox = new ComboBox { Margin = new Thickness(0, 3, 0, 3) }; Grid.SetColumn(typeBox, 2);
            filters.Children.Add(injectionToggle); filters.Children.Add(searchBox); filters.Children.Add(typeBox); Grid.SetRow(filters, 2); ammoGrid.Children.Add(filters);
            ammoList = new ListBox { Background = ModernPalette.Brush(ModernPalette.Field), BorderBrush = ModernPalette.Brush(ModernPalette.Border), BorderThickness = new Thickness(1), Margin = new Thickness(0, 4, 0, 7) };
            Grid.SetRow(ammoList, 3); ammoGrid.Children.Add(ammoList);

            UniformGrid slots = new UniformGrid { Rows = 2, Columns = 2, Margin = new Thickness(0, 0, 0, 6) };
            for (int slot = 0; slot < 4; slot++) slots.Children.Add(CreateAmmoSlot(slot));
            Grid.SetRow(slots, 4); ammoGrid.Children.Add(slots);
            Grid mountRow = new Grid(); mountRow.ColumnDefinitions.Add(new ColumnDefinition()); mountRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });
            mountRow.Children.Add(new TextBlock { Text = ModernText.L("Choose a slot, select a round above, then mount it.", "选择槽位，先在上方选择炮弹，再装填。"), Foreground = ModernPalette.Brush(ModernPalette.Muted), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });
            Button mount = DialogButton(ModernText.L("MOUNT ROUND", "装填炮弹"), true); mount.Click += delegate { MountSelectedAmmo(); }; Grid.SetColumn(mount, 1); mountRow.Children.Add(mount); Grid.SetRow(mountRow, 5); ammoGrid.Children.Add(mountRow);
            ammoCard.Child = ammoGrid; body.Children.Add(ammoCard);

            Border tuningCard = Card();
            ScrollViewer tuningScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, ClipToBounds = true };
            StackPanel tuningPanel = new StackPanel();
            tuningPanel.Children.Add(Heading("CROSS-DOMAIN CANNON", 15));
            Grid domainRow = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            domainRow.ColumnDefinitions.Add(new ColumnDefinition());
            domainRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            domainBox = new ComboBox { Foreground = ModernPalette.Brush(ModernPalette.Text), Background = ModernPalette.Brush("#FF16283E"), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Padding = new Thickness(8, 4, 8, 4), Height = 32, HorizontalAlignment = HorizontalAlignment.Stretch };
            domainBox.Items.Add(new ComboBoxItem { Content = ModernText.L("GROUND VEHICLE", "地面载具"), Tag = "ground" });
            domainBox.Items.Add(new ComboBoxItem { Content = ModernText.L("NAVAL SHIP", "海上舰船"), Tag = "naval" });
            domainBox.Items.Add(new ComboBoxItem { Content = ModernText.L("AIRCRAFT", "空中载具"), Tag = "aircraft" });
            domainBox.Items.Add(new ComboBoxItem { Content = ModernText.L("HELICOPTER", "直升机"), Tag = "helicopter" });
            string savedDomain = String.IsNullOrWhiteSpace(original.InjectedCannonDomain) ? "ground" : original.InjectedCannonDomain;
            ComboBoxItem savedDomainItem = domainBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => String.Equals(x.Tag as string, savedDomain, StringComparison.OrdinalIgnoreCase)) ?? (ComboBoxItem)domainBox.Items[0];
            unitBox = new ComboBox { Foreground = ModernPalette.Brush(ModernPalette.Text), Background = ModernPalette.Brush("#FF16283E"), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Padding = new Thickness(8, 4, 8, 4), Height = 32, Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
            cannonBox = new ComboBox { Foreground = ModernPalette.Brush(ModernPalette.Text), Background = ModernPalette.Brush("#FF16283E"), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Padding = new Thickness(8, 4, 8, 4), Height = 32, Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
            domainBox.SelectionChanged += delegate { RefreshUnitBox(); };
            unitBox.SelectionChanged += delegate { RefreshCannonBox(); };
            cannonBox.SelectionChanged += delegate { RefreshRoundBox(); SyncLeftCannon(); };
            Button clearCannon = DialogButton(ModernText.L("CLEAR", "清除"), false);
            clearCannon.Click += delegate { cannonBox.SelectedIndex = -1; };
            domainRow.Children.Add(domainBox);
            Grid.SetColumn(clearCannon, 1);
            domainRow.Children.Add(clearCannon);
            tuningPanel.Children.Add(domainRow);
            tuningPanel.Children.Add(unitBox);
            tuningPanel.Children.Add(cannonBox);
            roundBox = new ComboBox { Foreground = ModernPalette.Brush(ModernPalette.Text), Background = ModernPalette.Brush("#FF16283E"), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Padding = new Thickness(8, 4, 8, 4), Height = 32, Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
            roundBox.Items.Add(new ComboBoxItem { Content = ModernText.L("ALL (native rounds)", "全部（原生炮弹）"), Tag = "" });
            foreach (GroundAmmo injectedRound in injectedCannonAmmo)
                roundBox.Items.Add(new ComboBoxItem { Content = injectedRound.Display, Tag = injectedRound.BulletName });
            if (!String.IsNullOrWhiteSpace(original.InjectedCannonRound))
            {
                ComboBoxItem savedRound = roundBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => String.Equals(x.Tag as string, original.InjectedCannonRound, StringComparison.OrdinalIgnoreCase));
                if (savedRound != null) roundBox.SelectedItem = savedRound;
            }
            if (roundBox.SelectedItem == null) roundBox.SelectedIndex = 0;
            roundBox.SelectionChanged += delegate { SyncRoundToSlot(); };
            tuningPanel.Children.Add(roundBox);
            ammoUnlimitedBox = new CheckBox { Content = ModernText.L("Unlimited ammunition (9999 per slot)", "无限弹药（每槽 9999）"), IsChecked = original.UnlimitedAmmo, Foreground = ModernPalette.Brush(ModernPalette.Text), Margin = new Thickness(0, 6, 0, 0) };
            tuningPanel.Children.Add(ammoUnlimitedBox);
            domainBox.SelectedItem = savedDomainItem;
            RefreshCannonBox();
            BuildCannonSelector();
            SelectInitialCannon();
            tuningPanel.Children.Add(new TextBlock { Text = "Pick the source unit (e.g. Yamato), then its weapon (460/155/127 mm). Ground, naval and air units are all supported; air also includes missiles and rockets. Ammunition slots and projectile tuning below then apply to the injected weapon.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) });
            tuningPanel.Children.Add(new Border { Height = 1, Background = ModernPalette.Brush(ModernPalette.Border), Margin = new Thickness(0, 10, 0, 10) });            tuningPanel.Children.Add(new Border { Height = 1, Background = ModernPalette.Brush(ModernPalette.Border), Margin = new Thickness(0, 10, 0, 10) });
            
tuningPanel.Children.Add(Heading("REAL VEHICLE VALUES", 15));
            

            overrideBallistics = new CheckBox { Content = ModernText.L("Override native values", "覆盖原生数值"), IsChecked = original.OverrideGroundBallistics, Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 12, 0, 7) }; tuningPanel.Children.Add(overrideBallistics);
            tuningPanel.Children.Add(new TextBlock { Text = "Projectile values follow the selected ammunition slot. Every field can be typed directly.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) });
            projectileReference = ResolveProjectileReference();
            AddValue(tuningPanel, "PROJECTILE MASS", "projectileMass", projectileReference == null ? 0 : projectileReference.Mass, original.ProjectileMassMultiplier, "kg");
            AddValue(tuningPanel, "MUZZLE VELOCITY", "velocity", projectileReference == null ? 0 : projectileReference.Speed, original.MuzzleVelocityMultiplier, "m/s");
            AddValue(tuningPanel, "EXPLOSIVE FILLER", "explosive", projectileReference == null ? 0 : projectileReference.ExplosiveMass, original.ExplosiveMassMultiplier, "kg");
            AddValue(tuningPanel, "REFERENCE PENETRATION", "penetration", projectileReference == null ? 0 : projectileReference.Penetration, original.PenetrationMultiplier, "mm");
            AddValue(tuningPanel, "FIRE RATE OVERRIDE (SEC)", "reload", 0, 0, "s");
            if (original.ReloadSeconds > 0 && tuning.ContainsKey("reload")) tuning["reload"].Text = FormatValue(original.ReloadSeconds);
            AddValue(tuningPanel, "RECOIL TRAVEL", "recoil", vehicle.NativeRecoil, original.RecoilMultiplier, "m");
            tuningPanel.Children.Add(new Border { Height = 1, Background = ModernPalette.Brush(ModernPalette.Border), Margin = new Thickness(0, 8, 0, 7) });
            AddValue(tuningPanel, "ENGINE POWER", "engine", vehicle.NativeEnginePower, original.EnginePowerMultiplier, "hp");
            AddValue(tuningPanel, "VEHICLE MASS", "mass", vehicle.NativeMass, original.VehicleMassMultiplier, "kg");
            AddValue(tuningPanel, "FORWARD SPEED LIMIT", "forward", vehicle.NativeForwardSpeed, original.ForwardSpeedMultiplier, "km/h");
            AddValue(tuningPanel, "REVERSE SPEED LIMIT", "reverse", vehicle.NativeReverseSpeed, original.ReverseSpeedMultiplier, "km/h");
            Button resetAll = DialogButton(ModernText.L("RESET ALL TO CURRENT STOCK", "重置为当前默认弹"), false); resetAll.Margin = new Thickness(0, 10, 0, 4); resetAll.Click += delegate { ResetAllValues(); }; tuningPanel.Children.Add(resetAll);
            tuningPanel.Children.Add(new TextBlock { Text = "Stock reset uses this vehicle's current game definition; selected research modules remain configured separately in Modules.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 4) });
            tuningScroll.Content = tuningPanel; tuningCard.Child = tuningScroll; Grid.SetColumn(tuningCard, 2); body.Children.Add(tuningCard);
            if (simplified)
            {
                // Home panel: the right card holds the 4 ammunition slots (MOUNT + pool)
                // instead of the cross-domain cannon / projectile tuning.
                StackPanel slotPanel = new StackPanel();
                slotPanel.Children.Add(Heading("AMMUNITION SLOTS", 15));
                UniformGrid slotGrid = new UniformGrid { Rows = 2, Columns = 2, Margin = new Thickness(0, 6, 0, 6) };
                for (int slot = 0; slot < 4; slot++) slotGrid.Children.Add(CreateAmmoSlot(slot));
                slotPanel.Children.Add(slotGrid);
                Grid slotMountRow = new Grid(); slotMountRow.ColumnDefinitions.Add(new ColumnDefinition()); slotMountRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                slotMountRow.Children.Add(new TextBlock { Text = ModernText.L("Select a round, pick a slot, mount it.", "选择炮弹与槽位并装填。"), Foreground = ModernPalette.Brush(ModernPalette.Muted), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });
                Button slotMount = new Button { Content = ModernText.L("MOUNT ROUND", "装填炮弹"), Style = buttonStyle, Padding = new Thickness(14, 2, 14, 2), Margin = new Thickness(4, 0, 0, 0) }; slotMount.Click += delegate { MountSelectedAmmo(); }; Grid.SetColumn(slotMount, 1); slotMountRow.Children.Add(slotMount);
                slotPanel.Children.Add(slotMountRow);
                slotPanel.Children.Add(new TextBlock { Text = ModernText.L("STOCK = native default round (empty slot + count).", "STOCK = 原生默认弹（空槽 + 数量）。"), Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
                tuningCard.Child = slotPanel;
            }

            Grid footer = new Grid(); footer.ColumnDefinitions.Add(new ColumnDefinition()); footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(165) }); footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(185) });
            footer.Children.Add(new TextBlock { Text = ModernText.L("Player ammunition is restored one second after complete depletion.", "玩家弹药耗尽一秒后自动恢复。"), Foreground = ModernPalette.Brush(ModernPalette.Muted), VerticalAlignment = VerticalAlignment.Center });
            Button cancel = DialogButton("取消", false); cancel.Click += delegate { DialogResult = false; Close(); }; Grid.SetColumn(cancel, 1); footer.Children.Add(cancel);
            Button apply = DialogButton("APPLY CONFIG", true); apply.Click += delegate { Save(); }; Grid.SetColumn(apply, 2); footer.Children.Add(apply); Grid.SetRow(footer, 2); layout.Children.Add(footer);

            typeBox.Items.Add("All Projectile Types"); foreach (string kind in catalog.Select(x => x.Type).Distinct().OrderBy(x => x)) typeBox.Items.Add(kind); typeBox.SelectedIndex = 0;
            injectionToggle.IsChecked = false; injectionToggle.Checked += delegate { RefreshAmmo(); }; injectionToggle.Unchecked += delegate { RefreshAmmo(); }; searchBox.TextChanged += delegate { RefreshAmmo(); }; typeBox.SelectionChanged += delegate { RefreshAmmo(); };
            overrideBallistics.Checked += delegate { UpdateTuningState(); }; overrideBallistics.Unchecked += delegate { UpdateTuningState(); };
            if (simplified) currentCannon = new CannonChoice { Blk = vehicle.MainWeaponBlk, Display = "PRIMARY", IsNative = true };
            SelectSlot(0); RefreshAmmo(); RefreshSlotEditors();
            UpdateTuningState();
        }

        private Border Card() { return new Border { CornerRadius = new CornerRadius(14), Background = ModernPalette.Brush(ModernPalette.Field), BorderBrush = ModernPalette.Brush(ModernPalette.Border), BorderThickness = new Thickness(1), Padding = new Thickness(12), ClipToBounds = true }; }

        private Border CreateAmmoSlot(int slot)
        {
            GroundAmmoSlotEditor editor = new GroundAmmoSlotEditor { Slot = slot };
            editor.Card = new Border { CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Background = ModernPalette.Brush("#8A24324D"), Padding = new Thickness(8), Margin = new Thickness(3) };
            Grid grid = new Grid(); grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) }); grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(22) }); grid.RowDefinitions.Add(new RowDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            editor.Select = new Button { Content = ModernText.L("AMMO ", "槽位 ") + (slot + 1).ToString(CultureInfo.InvariantCulture), Tag = slot, Style = (Style)DialogRoot.Resources["ButtonStyle"], Padding = new Thickness(5, 1, 5, 1) };
            editor.Select.Click += delegate { SelectSlot(editor.Slot); }; grid.Children.Add(editor.Select);
            Button clear = new Button { Content = "\u00d7", Tag = slot, Style = (Style)DialogRoot.Resources["ButtonStyle"], Padding = new Thickness(0, 1, 0, 1), Margin = new Thickness(4, 0, 0, 0), ToolTip = "Clear this ammunition slot", FontSize = 12, Foreground = ModernPalette.Brush(ModernPalette.Muted) };
            clear.Click += delegate { if (loadouts.ContainsKey(editor.Slot)) loadouts.Remove(editor.Slot); RefreshSlotEditors(); }; Grid.SetColumn(clear, 1); grid.Children.Add(clear);
            editor.Name = new TextBlock { Text = ModernText.L("EMPTY", "空"), Foreground = ModernPalette.Brush(ModernPalette.Muted), FontSize = 10, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(editor.Name, 1); grid.Children.Add(editor.Name);
            Grid count = new Grid(); count.ColumnDefinitions.Add(new ColumnDefinition()); count.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) }); count.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            editor.Count = new Slider { Minimum = 0, Maximum = AmmoCapacity, TickFrequency = 1, IsSnapToTickEnabled = true, VerticalAlignment = VerticalAlignment.Center };
            editor.CountBox = ModernNumericBox.Create(); editor.CountBox.Height = 28; editor.CountBox.Padding = new Thickness(6, 2, 6, 2); editor.CountBox.Margin = new Thickness(4, 0, 0, 0);
            editor.Value = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            count.Children.Add(editor.Count); Grid.SetColumn(editor.CountBox, 1); count.Children.Add(editor.CountBox); Grid.SetColumn(editor.Value, 2); count.Children.Add(editor.Value); Grid.SetRow(count, 2); grid.Children.Add(count);
            editor.Count.ValueChanged += delegate { if (!updatingSlots) UpdateSlotCount(editor.Slot, (int)editor.Count.Value); };
            ModernNumericBox.Bind(editor.Count, editor.CountBox);
            editor.Card.Child = grid; slotEditors.Add(editor); return editor.Card;
        }

        private void AddValue(StackPanel panel, string label, string key, double stock, double multiplier, string unit)
        {
            double initial = stock > 0 ? stock * multiplier : 0;
            tuningStock[key] = stock;
            Grid row = new Grid { Margin = new Thickness(0, 3, 0, 5) };
            row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });
            StackPanel labelStack = new StackPanel(); labelStack.Children.Add(Caption(label)); labelStack.Children.Add(new TextBlock { Text = ModernText.L("Stock: ", "默认弹: ") + FormatValue(stock) + " " + unit, Foreground = ModernPalette.Brush(ModernPalette.Muted), FontSize = 9, Margin = new Thickness(0, 2, 4, 0) }); row.Children.Add(labelStack);
            TextBox box = new TextBox { Text = FormatValue(initial), Height = 34, Padding = new Thickness(8, 3, 8, 3), Tag = unit }; Grid.SetColumn(box, 1); row.Children.Add(box);
            Button reset = new Button { Content = ModernText.L("RESET", "重置"), Style = (Style)DialogRoot.Resources["ButtonStyle"], FontSize = 9, Padding = new Thickness(2), Margin = new Thickness(5, 0, 0, 0), Tag = key };
            reset.Click += delegate { ResetValue((string)reset.Tag); }; Grid.SetColumn(reset, 2); row.Children.Add(reset);
            panel.Children.Add(row); tuning[key] = box;
        }

        private static string FormatValue(double value) { return value.ToString(value >= 100 ? "0.##" : "0.####", CultureInfo.InvariantCulture); }

        private double ReadValue(string key)
        {
            if (!tuning.ContainsKey(key)) return 0;
            double value;
            string text = (tuning[key].Text ?? "").Trim();
            if (Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || Double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)) return Math.Max(0, value);
            return tuningStock[key];
        }

        private double Ratio(string key)
        {
            if (!tuningStock.ContainsKey(key)) return 1.0;
            double stock = tuningStock[key];
            return stock > 0 ? ReadValue(key) / stock : 1.0;
        }

        private void ResetValue(string key) { tuning[key].Text = FormatValue(tuningStock[key]); }
        private void ResetAllValues() { foreach (string key in tuning.Keys.ToList()) ResetValue(key); }
        private void UpdateTuningState() { bool enabled = overrideBallistics.IsChecked == true; foreach (TextBox value in tuning.Values) value.IsEnabled = enabled; }

        private GroundAmmo FindAmmoForLoadout(GroundAmmoLoadout entry)
        {
            if (entry == null) return null;
            GroundAmmo ammo = catalog.FirstOrDefault(x => x.SourceBlk != null && x.SourceBlk.Equals(entry.SourceBlk ?? "", StringComparison.OrdinalIgnoreCase) && x.BulletName != null && x.BulletName.Equals(entry.BulletName ?? "", StringComparison.OrdinalIgnoreCase));
            if (ammo == null && !String.IsNullOrWhiteSpace(entry.SourceBlk) && !String.IsNullOrWhiteSpace(entry.BulletName))
            {
                // Cross-domain ammunition is resolved from the gun BLK, not the catalog.
                IList<GroundAmmo> resolved = ResolveCannonAmmoCached(entry.SourceBlk);
                ammo = resolved.FirstOrDefault(x => x.BulletName != null && x.BulletName.Equals(entry.BulletName, StringComparison.OrdinalIgnoreCase));
            }
            return ammo;
        }

        private GroundAmmo ResolveProjectileReference()
        {
            GroundAmmoLoadout entry;
            if (loadouts.TryGetValue(selectedSlot, out entry))
            {
                GroundAmmo selected = FindAmmoForLoadout(entry);
                if (selected != null) return selected;
            }
            return catalog.FirstOrDefault(x => x.SourceBlk.Equals(vehicle.MainWeaponBlk ?? "", StringComparison.OrdinalIgnoreCase)) ?? catalog.FirstOrDefault();
        }

        private void SetProjectileReference(GroundAmmo ammo)
        {
            if (ammo == null || tuning.Count == 0) return;
            projectileReference = ammo;
            SetProjectileStock("projectileMass", ammo.Mass, original.ProjectileMassMultiplier);
            SetProjectileStock("velocity", ammo.Speed, original.MuzzleVelocityMultiplier);
            SetProjectileStock("explosive", ammo.ExplosiveMass, original.ExplosiveMassMultiplier);
            SetProjectileStock("penetration", ammo.Penetration, original.PenetrationMultiplier);
        }

        private void SetProjectileStock(string key, double stock, double multiplier)
        {
            tuningStock[key] = stock;
            tuning[key].Text = FormatValue(stock * multiplier);
        }

        private List<GroundWeaponInfo> CannonWeapons { get { return groundWeapons.Where(x => !String.IsNullOrWhiteSpace(x.Blk) && x.Blk.IndexOf("_user_cannon", StringComparison.OrdinalIgnoreCase) >= 0 ).ToList(); } }
        private HashSet<string> CannonBlkSet { get { return new HashSet<string>(CannonWeapons.Select(x => NormalizeBlk(x.Blk)), StringComparer.OrdinalIgnoreCase); } }
        private bool SameBlk(string a, string b) { return !String.IsNullOrWhiteSpace(a) && !String.IsNullOrWhiteSpace(b) && NormalizeBlk(a) == NormalizeBlk(b); }
        private bool SameGun(GroundAmmoLoadout a, GroundAmmoLoadout b) { return SameBlk(a == null ? null : a.SourceBlk, b == null ? null : b.SourceBlk); }
        private int AmmoTotalFor(GroundAmmoLoadout entry)
        {
            if (entry == null || String.IsNullOrWhiteSpace(entry.SourceBlk)) return AmmoCapacity;
            // A cross-domain cannon replaces the whole gun controller and is
            // generated with an effectively unlimited rack (bullets:i = 9999).
            if (currentCannon != null && !currentCannon.IsNative && SameBlk(currentCannon.Blk, entry.SourceBlk)) return 9999;
            GroundWeaponInfo info = groundWeapons.FirstOrDefault(x => SameBlk(x.Blk, entry.SourceBlk));
            return info == null ? AmmoCapacity : (info.NativeAmmo > 0 ? info.NativeAmmo : (info.NativeAmmo < 0 ? 9999 : AmmoCapacity));
        }
        private static string CannonShortName(GroundWeaponInfo x)
        {
            string blk = x.Blk ?? "";
            string f = blk.Substring(blk.LastIndexOf('/') + 1).Replace("_user_cannon", "").Replace(".blk", "").Replace('_', ' ');
            return f;
        }

        private void BuildCannonSelector()
        {
            if (cannonSelector == null) return;
            if (simplified)
            {
                // Home panel: native weapons only (no cross-domain cannon list).
                foreach (GroundWeaponInfo gw in groundWeapons.Where(x => !String.IsNullOrWhiteSpace(x.Blk)).OrderBy(x => x.Display))
                    cannonSelector.Items.Add(new ComboBoxItem { Content = ModernText.L("NATIVE  \u2022  ", "原生  \u2022  ") + gw.Display, Tag = new CannonChoice { Blk = gw.Blk, Display = gw.Display, IsNative = true } });
                if (cannonSelector.Items.Count == 0 && !String.IsNullOrWhiteSpace(vehicle.MainWeaponBlk))
                    cannonSelector.Items.Add(new ComboBoxItem { Content = ModernText.L("NATIVE  \u2022  PRIMARY", "原生  \u2022  主武器"), Tag = new CannonChoice { Blk = vehicle.MainWeaponBlk, Display = "PRIMARY", IsNative = true } });
                return;
            }
            List<GroundWeaponInfo> mains = new List<GroundWeaponInfo>();
            List<GroundWeaponInfo> secondary = new List<GroundWeaponInfo>();
            foreach (GroundWeaponInfo gw in groundWeapons.Where(x => !String.IsNullOrWhiteSpace(x.Blk)))
            {
                if (ModernMainWindow.IsSecondaryGroundWeapon(gw.Blk)) secondary.Add(gw);
                else mains.Add(gw);
            }
            bool anyNative = false;
            foreach (GroundWeaponInfo gw in mains)
            {
                cannonSelector.Items.Add(new ComboBoxItem { Content = ModernText.L("NATIVE  •  ", "原生  •  ") + gw.Display, Tag = new CannonChoice { Blk = gw.Blk, Display = gw.Display, IsNative = true } });
                anyNative = true;
            }
            if (!anyNative && secondary.Count > 0)
            {
                // Machine-gun-only vehicle: the MGs are its primary armament (Ask3lad-style).
                foreach (GroundWeaponInfo gw in secondary)
                {
                    cannonSelector.Items.Add(new ComboBoxItem { Content = ModernText.L("NATIVE  •  ", "原生  •  ") + gw.Display, Tag = new CannonChoice { Blk = gw.Blk, Display = gw.Display, IsNative = true } });
                    anyNative = true;
                }
            }
            if (!anyNative && !String.IsNullOrWhiteSpace(vehicle.MainWeaponBlk))
                cannonSelector.Items.Add(new ComboBoxItem { Content = ModernText.L("NATIVE  •  PRIMARY", "原生  •  主武器"), Tag = new CannonChoice { Blk = vehicle.MainWeaponBlk, Display = "PRIMARY", IsNative = true } });
            if (mains.Count > 0 && secondary.Count > 0)
            {
                cannonSelector.Items.Add(new ComboBoxItem { Content = ModernText.L("— SECONDARY (MACHINE GUNS / SMOKE) —", "— 次要武器（机枪 / 烟雾）—"), IsEnabled = false, Foreground = System.Windows.Media.Brushes.Gray });
                foreach (GroundWeaponInfo gw in secondary)
                    cannonSelector.Items.Add(new ComboBoxItem { Content = ModernText.L("NATIVE  •  ", "原生  •  ") + gw.Display, Foreground = System.Windows.Media.Brushes.Gray, Tag = new CannonChoice { Blk = gw.Blk, Display = gw.Display, IsNative = true } });
            }
            foreach (string domain in new[] { "ground", "naval", "aircraft", "helicopter" })
            {
                List<UnitWeapon> domainWeapons = unitWeapons.Where(x => String.Equals(x.Domain, domain, StringComparison.OrdinalIgnoreCase)).ToList();
                if (domainWeapons.Count == 0) continue;
                foreach (UnitWeapon uw in domainWeapons.OrderBy(x => x.UnitDisplay, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.WeaponDisplay, StringComparer.OrdinalIgnoreCase))
                    cannonSelector.Items.Add(new ComboBoxItem { Content = domain.ToUpperInvariant() + "  •  " + uw.UnitDisplay + "  •  " + uw.WeaponDisplay, Tag = new CannonChoice { Blk = uw.WeaponBlk, Display = uw.WeaponDisplay, IsNative = false, Domain = domain, UnitId = uw.UnitId } });
            }
        }

        private void SelectInitialCannon()
        {
            if (cannonSelector == null || cannonSelector.Items.Count == 0) return;
            CannonChoice initial = null;
            if (!String.IsNullOrWhiteSpace(original.InjectedCannonBlk))
            {
                string norm = NormalizeBlk(original.InjectedCannonBlk);
                initial = cannonSelector.Items.OfType<ComboBoxItem>().Select(x => x.Tag as CannonChoice)
                    .FirstOrDefault(c => c != null && !c.IsNative && NormalizeBlk(c.Blk).Equals(norm, StringComparison.OrdinalIgnoreCase));
            }
            if (initial == null)
                initial = cannonSelector.Items.OfType<ComboBoxItem>().Select(x => x.Tag as CannonChoice).FirstOrDefault(c => c != null && c.IsNative);
            if (initial == null)
                initial = cannonSelector.Items.OfType<ComboBoxItem>().Select(x => x.Tag as CannonChoice).FirstOrDefault(c => c != null);
            if (initial == null) return;
            syncingCannon = true;
            try
            {
                ComboBoxItem item = cannonSelector.Items.OfType<ComboBoxItem>().FirstOrDefault(x => (x.Tag as CannonChoice) == initial);
                if (item != null) cannonSelector.SelectedItem = item;
            }
            finally { syncingCannon = false; }
            currentCannon = initial;
            // Native selection means no cannon injection: clear the right-side tree
            // so the saved cross-domain choice does not leak into the mission.
            if (initial.IsNative)
            {
                syncingCannon = true;
                try { if (cannonBox != null && cannonBox.SelectedIndex != -1) cannonBox.SelectedIndex = -1; }
                finally { syncingCannon = false; }
            }
            RefreshAmmo();
            RefreshSlotEditors();
        }

        private void CannonSelectorChanged()
        {
            if (syncingCannon) return;
            ComboBoxItem item = cannonSelector == null ? null : cannonSelector.SelectedItem as ComboBoxItem;
            CannonChoice choice = item == null ? null : item.Tag as CannonChoice;
            if (choice == null) return;
            ApplyCannonSelection(choice, false);
        }

        private void ApplyCannonSelection(CannonChoice choice, bool fromRightSide)
        {
            if (choice == null) return;
            currentCannon = choice;
            RefreshAmmo();
            RefreshSlotEditors();
            if (!fromRightSide) SyncRightSideCannon(choice);
        }

        private void SyncRightSideCannon(CannonChoice choice)
        {
            if (syncingCannon || domainBox == null || unitBox == null || cannonBox == null) return;
            syncingCannon = true;
            try
            {
                if (choice.IsNative)
                {
                    if (cannonBox.SelectedIndex != -1) cannonBox.SelectedIndex = -1;
                    return;
                }
                ComboBoxItem domainItem = domainBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => String.Equals(x.Tag as string, choice.Domain, StringComparison.OrdinalIgnoreCase));
                if (domainItem != null && domainBox.SelectedItem != domainItem) domainBox.SelectedItem = domainItem;
                ComboBoxItem unitItem = unitBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => String.Equals(x.Tag as string, choice.UnitId, StringComparison.OrdinalIgnoreCase));
                if (unitItem != null && unitBox.SelectedItem != unitItem) unitBox.SelectedItem = unitItem;
                ComboBoxItem cannonItem = cannonBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => x.Tag is string && NormalizeBlk((string)x.Tag).Equals(NormalizeBlk(choice.Blk), StringComparison.OrdinalIgnoreCase));
                if (cannonItem != null && cannonBox.SelectedItem != cannonItem) cannonBox.SelectedItem = cannonItem;
            }
            finally { syncingCannon = false; }
        }

        private void SyncLeftCannon()
        {
            if (syncingCannon || cannonSelector == null) return;
            ComboBoxItem item = cannonBox == null ? null : cannonBox.SelectedItem as ComboBoxItem;
            if (item == null || !(item.Tag is string)) return;
            string blk = (string)item.Tag;
            CannonChoice match = cannonSelector.Items.OfType<ComboBoxItem>().Select(x => x.Tag as CannonChoice)
                .FirstOrDefault(c => c != null && !c.IsNative && NormalizeBlk(c.Blk).Equals(NormalizeBlk(blk), StringComparison.OrdinalIgnoreCase));
            if (match == null) return;
            syncingCannon = true;
            try
            {
                ComboBoxItem target = cannonSelector.Items.OfType<ComboBoxItem>().FirstOrDefault(x => (x.Tag as CannonChoice) == match);
                if (target != null && cannonSelector.SelectedItem != target) cannonSelector.SelectedItem = target;
            }
            finally { syncingCannon = false; }
            if (currentCannon == null || !SameBlk(currentCannon.Blk, match.Blk))
            {
                currentCannon = match;
                RefreshAmmo();
                RefreshSlotEditors();
            }
        }

        private void SyncRoundToSlot()
        {
            if (syncingCannon || roundBox == null || currentCannon == null || currentCannon.IsNative) return;
            ComboBoxItem item = roundBox.SelectedItem as ComboBoxItem;
            string tag = item == null ? null : (item.Tag as string);
            if (String.IsNullOrWhiteSpace(tag)) return; // ModernText.L("ALL (native rounds)", "全部（原生炮弹）")
            IEnumerable<GroundAmmo> source = ammoList.ItemsSource as IEnumerable<GroundAmmo>;
            if (source == null) return;
            GroundAmmo ammo = source.FirstOrDefault(x => x.BulletName != null && x.BulletName.Equals(tag, StringComparison.OrdinalIgnoreCase));
            if (ammo == null) return;
            GroundAmmoLoadout existing; loadouts.TryGetValue(selectedSlot, out existing);
            int count = existing == null ? 1 : Math.Max(1, existing.Count);
            loadouts[selectedSlot] = new GroundAmmoLoadout { Slot = selectedSlot, Count = count, SourceBlk = currentCannon.Blk, BulletName = ammo.BulletName };
            SetProjectileReference(ammo);
            RefreshSlotEditors();
            SelectSlot(selectedSlot);
        }

        private IList<GroundAmmo> ResolveCannonAmmoCached(string blk)
        {
            if (String.IsNullOrWhiteSpace(blk)) return new List<GroundAmmo>();
            string key = NormalizeBlk(blk);
            IList<GroundAmmo> cached;
            if (!cannonAmmoCache.TryGetValue(key, out cached))
            {
                cached = resolveCannonAmmo == null ? new List<GroundAmmo>() : (resolveCannonAmmo(blk) ?? new List<GroundAmmo>());
                cannonAmmoCache[key] = cached;
            }
            return cached;
        }

        private void RefreshAmmo()
        {
            IEnumerable<GroundAmmo> query;
            if (currentCannon != null && !currentCannon.IsNative)
            {
                // Cross-domain cannon: the list shows that gun's own ammunition
                // (belt groups first), resolved once per cannon and cached.
                query = ResolveCannonAmmoCached(currentCannon.Blk);
                if (injectionToggle != null && injectionToggle.IsChecked == true) query = query.Concat(catalog);
            }
            else if (currentCannon != null && currentCannon.IsNative && injectionToggle == null || injectionToggle.IsChecked != true)
            {
                // Native cannon: the list shows the currently selected gun's shells
                // unless INJECT ANY SHELL opens the whole catalog. Rounds are
                // further filtered by this vehicle's ammo containers (beltOptions)
                // because the same cannon can serve vehicles with different
                // ammunition (Type16 vs Type16 FPS).
                query = catalog.Where(x => SameBlk(x.SourceBlk, currentCannon.Blk) && ContainerAllowed(x.Container));
            }
            else
            {
                query = catalog;
                if (injectionToggle.IsChecked != true && CannonBlkSet.Count > 0) query = query.Where(x => CannonBlkSet.Contains(x.SourceBlk));
            }
            if (injectedCannonAmmo != null && injectedCannonAmmo.Count > 0) query = query.Concat(injectedCannonAmmo);
            if (simplified)
            {
                // STOCK (native default round) is the first entry; it is written as an
                // empty slot (bulletsN:t="" + count) exactly like Ask3lad.
                int stockCal = currentCannon == null ? 0 : ModernMainWindow.GroundCalibre(currentCannon.Blk);
                query = new[] { new GroundAmmo { SourceBlk = "stock:" + stockCal.ToString(CultureInfo.InvariantCulture), BulletName = "", Display = ModernText.L("STOCK \u2022 default ammunition", "STOCK \u2022 default ammunition"), Type = "Default", Caliber = stockCal } }.Concat(query);
            }
            string search = (searchBox.Text ?? "").Trim(); if (search.Length > 0) query = query.Where(x => x.Display.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 || x.BulletName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || x.Type.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            string type = typeBox.SelectedIndex > 0 ? typeBox.SelectedItem as string : null; if (!String.IsNullOrEmpty(type)) query = query.Where(x => x.Type == type);
            ammoList.ItemsSource = query.OrderBy(x => x.Caliber).ThenBy(x => x.Type).ThenBy(x => x.Display).ToList();
        }

        private void SelectSlot(int slot)
        {
            selectedSlot = slot;
            foreach (GroundAmmoSlotEditor editor in slotEditors) editor.Card.BorderBrush = editor.Slot == slot ? ModernPalette.Brush(ModernPalette.Cyan) : ModernPalette.Brush(ModernPalette.Border);
            SetProjectileReference(ResolveProjectileReference());
        }

        private void MountSelectedAmmo()
        {
            GroundAmmo ammo = ammoList.SelectedItem as GroundAmmo; if (ammo == null) return;
            GroundAmmoLoadout existing; loadouts.TryGetValue(selectedSlot, out existing);
            if (simplified && ammo.BulletName != null && ammo.BulletName.Length == 0)
            {
                // STOCK: empty slot + count -> the game loads the native default round.
                int stockOthers = loadouts.Values.Where(x => x.Slot != selectedSlot && x.SourceBlk != null && x.SourceBlk.StartsWith("stock:", StringComparison.OrdinalIgnoreCase)).Sum(x => Math.Max(0, x.Count));
                int stockAvailable = Math.Max(0, AmmoCapacity - stockOthers);
                int stockCount = existing == null ? Math.Max(1, stockAvailable) : Math.Min(Math.Max(1, existing.Count), stockAvailable);
                loadouts[selectedSlot] = new GroundAmmoLoadout { Slot = selectedSlot, Count = stockCount, SourceBlk = ammo.SourceBlk, BulletName = "" };
                SetProjectileReference(ammo); RefreshSlotEditors(); SelectSlot(selectedSlot);
                return;
            }
            int others = loadouts.Values.Where(x => x.Slot != selectedSlot && SameBlk(x.SourceBlk, ammo.SourceBlk)).Sum(x => Math.Max(0, x.Count));
            int available = Math.Max(0, AmmoTotalFor(new GroundAmmoLoadout { SourceBlk = ammo.SourceBlk }) - others);
            int count;
            if (currentCannon != null && !currentCannon.IsNative)
            {
                // Cross-domain cannon: the injected gun is generated with an
                // effectively unlimited rack, so mounting a round fills it.
                count = existing == null ? 9999 : Math.Max(1, existing.Count);
            }
            else
            {
                count = existing == null ? (simplified ? Math.Max(1, available) : Math.Min(1, available)) : Math.Min(Math.Max(1, existing.Count), available);
            }
            loadouts[selectedSlot] = new GroundAmmoLoadout { Slot = selectedSlot, Count = count, SourceBlk = ammo.SourceBlk, BulletName = ammo.BulletName };
            SetProjectileReference(ammo); RefreshSlotEditors(); SelectSlot(selectedSlot);
        }

        private void UpdateSlotCount(int slot, int count)
        {
            GroundAmmoLoadout entry;
            if (loadouts.TryGetValue(slot, out entry))
            {
                int gunTotal = AmmoTotalFor(entry);
                int others = loadouts.Values.Where(x => x.Slot != slot && SameGun(x, entry)).Sum(x => Math.Max(0, x.Count));
                int allowedMaximum = Math.Max(0, gunTotal - others);
                entry.Count = Math.Max(0, Math.Min(count, allowedMaximum));
            }
            RefreshSlotEditors();
        }

        private void RefreshSlotEditors()
        {
            updatingSlots = true;
            try
            {
                foreach (GroundAmmoLoadout entry in loadouts.Values.OrderBy(x => x.Slot))
                {
                    int gunTotal = AmmoTotalFor(entry);
                    entry.Count = Math.Max(0, Math.Min(entry.Count, gunTotal));
                }
                foreach (GroundAmmoSlotEditor editor in slotEditors)
                {
                    GroundAmmoLoadout entry; loadouts.TryGetValue(editor.Slot, out entry);
                    int others = loadouts.Values.Where(x => x.Slot != editor.Slot && SameGun(x, entry)).Sum(x => Math.Max(0, x.Count));
                    int gunTotal = AmmoTotalFor(entry);
                    int allowedMaximum = Math.Max(0, gunTotal - others);
                    int current = entry == null ? 0 : Math.Max(0, entry.Count);
                    editor.Count.Maximum = gunTotal;
                    editor.Count.Value = current;
                    editor.Value.Text = current.ToString(CultureInfo.InvariantCulture) + " / " + allowedMaximum.ToString(CultureInfo.InvariantCulture);
                    editor.Count.ToolTip = ModernText.L("Loaded: ", "已加载: ") + current.ToString(CultureInfo.InvariantCulture) + "  •  Maximum currently available: " + allowedMaximum.ToString(CultureInfo.InvariantCulture);
                    GroundAmmo ammo = FindAmmoForLoadout(entry);
                    if (entry != null && entry.SourceBlk != null && entry.SourceBlk.StartsWith("stock:", StringComparison.OrdinalIgnoreCase)) editor.Name.Text = ModernText.L("STOCK • default ammunition", "STOCK • 默认弹药");
                    else editor.Name.Text = ammo == null ? ModernText.L("EMPTY", "空") : ammo.Display + "  •  " + ammo.Type;
                }
                if (currentCannon != null && !currentCannon.IsNative)
                {
                    int used = loadouts.Values.Where(l => SameBlk(l.SourceBlk, currentCannon.Blk)).Sum(l => Math.Max(0, l.Count));
                    totalAmmoText.Text = CannonDisplayName(currentCannon.Blk).ToUpperInvariant() + ": " + used.ToString(CultureInfo.InvariantCulture) + "/9999";
                }
                else
                {
                    totalAmmoText.Text = String.Join("    ", CannonWeapons.Select(x =>
                    {
                        int used = loadouts.Values.Where(l => SameBlk(l.SourceBlk, x.Blk)).Sum(l => Math.Max(0, l.Count));
                        return CannonShortName(x) + ": " + used.ToString(CultureInfo.InvariantCulture) + "/" + (x.NativeAmmo > 0 ? x.NativeAmmo : (x.NativeAmmo < 0 ? 9999 : AmmoCapacity)).ToString(CultureInfo.InvariantCulture);
                    }));
                }
            }
            finally { updatingSlots = false; }
        }

        internal bool AmmoSlidersStableForSelfTest()
        {
            if (slotEditors.Count < 2 || !loadouts.ContainsKey(0) || !loadouts.ContainsKey(1)) return false;
            int firstBefore = loadouts[0].Count;
            int secondBefore = loadouts[1].Count;
            int allowed = Math.Max(0, AmmoCapacity - loadouts.Values.Where(x => x.Slot != 0).Sum(x => Math.Max(0, x.Count)));
            int requested = Math.Min(allowed, firstBefore + 1);
            UpdateSlotCount(0, requested);
            bool stable = loadouts[0].Count == requested && loadouts[1].Count == secondBefore &&
                slotEditors.All(x => Math.Abs(x.Count.Maximum - AmmoCapacity) < 0.01) &&
                slotEditors[0].Value.Text.StartsWith(requested.ToString(CultureInfo.InvariantCulture) + " / ", StringComparison.Ordinal) &&
                slotEditors[1].Value.Text.StartsWith(secondBefore.ToString(CultureInfo.InvariantCulture) + " / ", StringComparison.Ordinal);
            UpdateSlotCount(0, firstBefore);
            return stable;
        }

        private void Save()
        {
            AircraftSettings result = original.Copy(); result.GroundAmmoLoadouts.Clear(); foreach (GroundAmmoLoadout entry in loadouts.Values.Where(x => x.Count > 0).OrderBy(x => x.Slot)) result.GroundAmmoLoadouts.Add(entry.Copy());
            result.OverrideGroundBallistics = overrideBallistics != null && overrideBallistics.IsChecked == true;
                        result.ProjectileMassMultiplier = result.OverrideGroundBallistics ? Ratio("projectileMass") : 1; result.MuzzleVelocityMultiplier = result.OverrideGroundBallistics ? Ratio("velocity") : 1; result.ExplosiveMassMultiplier = result.OverrideGroundBallistics ? Ratio("explosive") : 1; result.PenetrationMultiplier = result.OverrideGroundBallistics ? Ratio("penetration") : 1;
            result.ReloadSeconds = ReadValue("reload"); result.RecoilMultiplier = result.OverrideGroundBallistics ? Ratio("recoil") : 1; result.EnginePowerMultiplier = result.OverrideGroundBallistics ? Ratio("engine") : 1; result.VehicleMassMultiplier = result.OverrideGroundBallistics ? Ratio("mass") : 1; result.ForwardSpeedMultiplier = result.OverrideGroundBallistics ? Ratio("forward") : 1; result.ReverseSpeedMultiplier = result.OverrideGroundBallistics ? Ratio("reverse") : 1;
            // The CANNON selector on the ammunition panel is the single source of
            // truth: native guns keep the classic slot logic, cross-domain guns
            // replace the whole cannon controller. The right-side tree stays as a
            // browse/verify companion and falls back only when the selector is empty.
            CannonChoice cannonChoice = cannonSelector == null ? null : ((cannonSelector.SelectedItem as ComboBoxItem) == null ? null : (cannonSelector.SelectedItem as ComboBoxItem).Tag as CannonChoice);
            if (cannonChoice != null && cannonChoice.IsNative)
            {
                result.InjectedCannonBlk = null;
                result.InjectedCannonDomain = null;
                result.InjectedCannonUnit = null;
                result.InjectedCannonRound = null;
            }
            else if (cannonChoice != null)
            {
                result.InjectedCannonBlk = cannonChoice.Blk;
                result.InjectedCannonDomain = cannonChoice.Domain;
                result.InjectedCannonUnit = cannonChoice.UnitId;
                ComboBoxItem roundSelection = roundBox == null ? null : roundBox.SelectedItem as ComboBoxItem;
                result.InjectedCannonRound = roundSelection == null || !(roundSelection.Tag is string) ? null : (string)roundSelection.Tag;
            }
            else
            {
                ComboBoxItem cannonSelection = cannonBox == null ? null : cannonBox.SelectedItem as ComboBoxItem;
                ComboBoxItem domainSelection = domainBox == null ? null : domainBox.SelectedItem as ComboBoxItem;
                ComboBoxItem unitSelection = unitBox == null ? null : unitBox.SelectedItem as ComboBoxItem;
                result.InjectedCannonBlk = cannonSelection == null || !(cannonSelection.Tag is string) ? null : (string)cannonSelection.Tag;
                result.InjectedCannonDomain = cannonSelection == null ? null : (domainSelection == null ? "ground" : (domainSelection.Tag as string ?? "ground"));
                result.InjectedCannonUnit = cannonSelection == null ? null : (unitSelection == null ? null : (unitSelection.Tag as string));
                ComboBoxItem roundSelection = roundBox == null ? null : roundBox.SelectedItem as ComboBoxItem;
                result.InjectedCannonRound = roundSelection == null || !(roundSelection.Tag is string) ? null : (string)roundSelection.Tag;
            }
            result.UnlimitedAmmo = ammoUnlimitedBox == null ? original.UnlimitedAmmo : ammoUnlimitedBox.IsChecked == true;
            Result = result; DialogResult = true; Close();
        }

        private void RefreshUnitBox()
        {
            if (unitBox == null) return;
            unitBox.Items.Clear();
            ComboBoxItem domainItem = domainBox.SelectedItem as ComboBoxItem;
            string domain = domainItem == null ? "ground" : (domainItem.Tag as string ?? "ground");
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (UnitWeapon uw in unitWeapons.Where(x => String.Equals(x.Domain, domain, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.UnitDisplay))
            {
                if (!seen.Add(uw.UnitId)) continue;
                unitBox.Items.Add(new ComboBoxItem { Content = uw.UnitDisplay, Tag = uw.UnitId });
            }
            if (unitBox.Items.Count == 0) unitBox.Items.Add(new ComboBoxItem { Content = ModernText.L("(no units in this domain)", "（该领域无单位）"), Tag = null });
            if (!String.IsNullOrWhiteSpace(original.InjectedCannonUnit))
            {
                ComboBoxItem match = unitBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => String.Equals(x.Tag as string, original.InjectedCannonUnit, StringComparison.OrdinalIgnoreCase));
                if (match != null) unitBox.SelectedItem = match;
            }
        }

        private void RefreshCannonBox()
        {
            if (cannonBox == null) return;
            cannonBox.Items.Clear();
            ComboBoxItem domainItem = domainBox.SelectedItem as ComboBoxItem;
            string domain = domainItem == null ? "ground" : (domainItem.Tag as string ?? "ground");
            ComboBoxItem unitItem = unitBox == null ? null : unitBox.SelectedItem as ComboBoxItem;
            string unitId = unitItem == null ? null : unitItem.Tag as string;
            if (String.IsNullOrEmpty(unitId))
            {
                cannonBox.Items.Add(new ComboBoxItem { Content = ModernText.L("(select a unit)", "（选择单位）"), Tag = null });
                return;
            }
            foreach (UnitWeapon uw in unitWeapons.Where(x => String.Equals(x.Domain, domain, StringComparison.OrdinalIgnoreCase) && String.Equals(x.UnitId, unitId, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.WeaponDisplay))
                cannonBox.Items.Add(new ComboBoxItem { Content = uw.WeaponDisplay, Tag = uw.WeaponBlk });
            if (!String.IsNullOrWhiteSpace(original.InjectedCannonBlk))
            {
                string saved = NormalizeBlk(original.InjectedCannonBlk);
                ComboBoxItem match = cannonBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => x.Tag is string && NormalizeBlk((string)x.Tag).Equals(saved, StringComparison.OrdinalIgnoreCase));
                if (match != null) cannonBox.SelectedItem = match;
            }
        }
        private void RefreshRoundBox()
        {
            if (roundBox == null || resolveCannonAmmo == null) return;
            ComboBoxItem cannonItem = cannonBox == null ? null : cannonBox.SelectedItem as ComboBoxItem;
            string blk = cannonItem == null ? null : (cannonItem.Tag as string);
            ComboBoxItem previous = roundBox.SelectedItem as ComboBoxItem;
            string previousTag = previous == null ? null : (previous.Tag as string);
            roundBox.Items.Clear();
            roundBox.Items.Add(new ComboBoxItem { Content = ModernText.L("ALL (native rounds)", "全部（原生炮弹）"), Tag = "" });
            if (!String.IsNullOrWhiteSpace(blk))
            {
                foreach (GroundAmmo ammo in ResolveCannonAmmoCached(blk))
                    roundBox.Items.Add(new ComboBoxItem { Content = ammo.Display, Tag = ammo.BulletName });
            }
            ComboBoxItem restored = null;
            if (!String.IsNullOrWhiteSpace(previousTag))
                restored = roundBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => String.Equals(x.Tag as string, previousTag, StringComparison.OrdinalIgnoreCase));
            if (restored == null && !String.IsNullOrWhiteSpace(original.InjectedCannonRound))
                restored = roundBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => String.Equals(x.Tag as string, original.InjectedCannonRound, StringComparison.OrdinalIgnoreCase));
            if (restored != null) roundBox.SelectedItem = restored;
            else roundBox.SelectedIndex = 0;
        }


        private static string NormalizeBlk(string path)
        {
            return (path ?? "").Replace('\\', '/').TrimStart('/').ToLowerInvariant();
        }

        private static string CannonDisplayName(string blk)
        {
            string normalized = NormalizeBlk(blk);
            string file = normalized.Substring(normalized.LastIndexOf('/') + 1);
            file = file.Replace("_user_cannon", "").Replace("_user_machinegun", "").Replace(".blk", "");
            return file.Replace('_', ' ');
        }
    }

    internal sealed class CountermeasureEditor
    {
        public CountermeasureLauncher Launcher;
        public Slider FlareSlider;
        public Slider ChaffSlider;
        public TextBlock FlareValue;
        public TextBlock ChaffValue;
        public TextBlock GamePreview;
        public Border Card;
    }

    internal sealed class GunBeltChoice
    {
        public string Id;
        public string Display;
        public override string ToString() { return Display; }
    }

    internal sealed class GunBeltEditor
    {
        public int GroupIndex;
        public ComboBox Selection;
    }

    // Embedded in the main-window OPTIONS tab; the standalone Mission Options
    // window keeps its own copy of this layout (keep both in sync when editing).
    internal sealed class MissionOptionsPanel : StackPanel
    {
        private readonly MissionSettings original;
        private Slider respawnSlider;
        private Slider targetSlider;
        private Slider rearmSlider;
        private ComboBox ammoMode;
        private ComboBox spawnMode;
        private Slider spawnSpeedSlider;
        private CheckBox spawnSpeedAuto;
        private TextBox spawnSpeedBox;
        private TextBlock spawnSpeedValue;
        private StackPanel speedRow;
        private CheckBox rapidToggle;
        private Slider rapidIntervalSlider;
        private CheckBox rapidFullBox;
        private CheckBox rapidPartialBox;

        public MissionOptionsPanel(MissionSettings source)
        {
            original = source == null ? new MissionSettings() : source.Copy();
            BuildContent();
        }

        private void BuildContent()
        {
            respawnSlider = OptionCard(this, "PLAYER RESPAWN DELAY", "Seconds before the player unit respawns after destruction (0 = instant).", original.PlayerRespawnDelaySeconds, 0, 60, true);
            targetSlider = OptionCard(this, "TARGET RECOVERY DELAY", "Seconds before destroyed target units reappear (stock template: 5 s, lab default: 0.25 s).", original.TargetRespawnDelaySeconds, 0.25, 30, true);
            rearmSlider = OptionCard(this, "GROUND REARM TIME", "Seconds spent on the field before ammunition is replenished (engine rearmTimeOnField).", original.RearmSeconds, 0, 60, true);
            Border ammoCard = Card("AMMUNITION POLICY");
            StackPanel ammoStack = ammoCard.Child as StackPanel;
            ammoMode = new ComboBox { Foreground = ModernPalette.Brush(ModernPalette.Text), Background = ModernPalette.Brush("#FF16283E"), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 260 };
            ammoMode.Items.Add(new ComboBoxItem { Content = ModernText.L("Unlimited ammunition", "无限弹药"), Tag = false });
            ammoMode.Items.Add(new ComboBoxItem { Content = ModernText.L("Limited + rearm on the field", "有限 + 战场再补给"), Tag = true });
            ammoMode.SelectedIndex = original.LimitedAmmo ? 1 : 0;
            ammoStack.Children.Add(ammoMode);
            ammoStack.Children.Add(new TextBlock { Text = "Unlimited keeps isLimitedAmmo=false. Limited enables the flag so ordnance and cannon ammunition can be exhausted and then replenished after the configured ground rearm time.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
            Children.Add(ammoCard);
            Border spawnCard = Card("SPAWN");
            StackPanel spawnStack = spawnCard.Child as StackPanel;
            spawnMode = new ComboBox { Foreground = ModernPalette.Brush(ModernPalette.Text), Background = ModernPalette.Brush("#FF16283E"), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 260 };
            spawnMode.Items.Add(new ComboBoxItem { Content = ModernText.L("Air spawn (with speed)", "空中出生（带速度）"), Tag = "air" });
            spawnMode.Items.Add(new ComboBoxItem { Content = ModernText.L("Airport takeoff (stationary)", "机场起飞（静止）"), Tag = "airport" });
            spawnMode.SelectedIndex = (original.SpawnMode ?? "air").Equals("airport", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            spawnStack.Children.Add(spawnMode);
            speedRow = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            Grid speedGrid = new Grid();
            speedGrid.ColumnDefinitions.Add(new ColumnDefinition());
            speedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
            speedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            spawnSpeedSlider = new Slider { Minimum = 0, Maximum = 800, Value = Math.Max(0, Math.Min(800, original.SpawnSpeedKmh)), TickFrequency = 10, IsSnapToTickEnabled = true, AutoToolTipPlacement = AutoToolTipPlacement.TopLeft, VerticalAlignment = VerticalAlignment.Center };
            spawnSpeedBox = ModernNumericBox.Create();
            spawnSpeedValue = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 15, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            spawnSpeedSlider.ValueChanged += delegate { spawnSpeedValue.Text = (int)Math.Round(spawnSpeedSlider.Value) + " km/h"; };
            spawnSpeedValue.Text = (int)Math.Round(spawnSpeedSlider.Value) + " km/h";
            speedGrid.Children.Add(spawnSpeedSlider);
            Grid.SetColumn(spawnSpeedBox, 1);
            speedGrid.Children.Add(spawnSpeedBox);
            Grid.SetColumn(spawnSpeedValue, 2);
            speedGrid.Children.Add(spawnSpeedValue);
            speedRow.Children.Add(speedGrid);
            ModernNumericBox.Bind(spawnSpeedSlider, spawnSpeedBox);
            spawnSpeedAuto = new CheckBox { Content = ModernText.L("Auto (airframe-safe speed)", "自动（机体安全速度）"), IsChecked = original.SpawnSpeedAuto, Foreground = ModernPalette.Brush(ModernPalette.Text), Margin = new Thickness(0, 8, 0, 0) };
            speedRow.Children.Add(spawnSpeedAuto);
            speedRow.Children.Add(new TextBlock { Text = "Airport takeoff always starts stationary; the speed row only applies to air spawns. Auto clamps the stock spawn speed to the airframe's lowest published maxSpeed so fragile aircraft no longer tear apart on spawn.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
            spawnStack.Children.Add(speedRow);
            spawnMode.SelectionChanged += delegate
            {
                ComboBoxItem item = spawnMode.SelectedItem as ComboBoxItem;
                bool airport = item != null && "airport".Equals(item.Tag as string, StringComparison.OrdinalIgnoreCase);
                speedRow.IsEnabled = !airport;
            };
            ComboBoxItem initial = spawnMode.SelectedItem as ComboBoxItem;
            speedRow.IsEnabled = !(initial != null && "airport".Equals(initial.Tag as string, StringComparison.OrdinalIgnoreCase));
            Children.Add(spawnCard);
            Border rapidCard = Card("RAPID FIRE (AUTO REPAIR + REARM)");
            StackPanel rapidStack = rapidCard.Child as StackPanel;
            rapidToggle = new CheckBox { Content = ModernText.L("ENABLED (auto repair + rearm)", "启用（自动维修 + 补给）"), IsChecked = original.RapidFireEnabled, Foreground = ModernPalette.Brush(ModernPalette.Text), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
            rapidStack.Children.Add(rapidToggle);
            rapidFullBox = new CheckBox { Content = ModernText.L("FULL RESTORE (all parts)", "完全修复（全部部件）"), IsChecked = original.RapidFireFullRestore, Foreground = ModernPalette.Brush(ModernPalette.Text), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 10, 0, 0) };
            rapidPartialBox = new CheckBox { Content = ModernText.L("PARTIAL RESTORE (barrel/breech/engine/tracks + crew + ammo + fuel)", "部分修复（炮管/炮闩/发动机/履带 + 乘员 + 弹药 + 燃油）"), IsChecked = !original.RapidFireFullRestore, Foreground = ModernPalette.Brush(ModernPalette.Text), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 0) };
            rapidFullBox.Checked += delegate { if (rapidPartialBox.IsChecked == true) rapidPartialBox.IsChecked = false; };
            rapidPartialBox.Checked += delegate { if (rapidFullBox.IsChecked == true) rapidFullBox.IsChecked = false; };
            rapidStack.Children.Add(rapidFullBox);
            rapidStack.Children.Add(rapidPartialBox);
            rapidStack.Children.Add(new TextBlock { Text = ModernText.L("REARM INTERVAL (s)", "补给间隔（秒）"), Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 0) });
            Grid rapidGrid = new Grid();
            rapidGrid.ColumnDefinitions.Add(new ColumnDefinition());
            rapidGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });
            rapidGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            rapidIntervalSlider = new Slider { Minimum = 0.1, Maximum = 10, Value = Math.Max(0.1, Math.Min(10, original.RapidFireInterval)), TickFrequency = 0.1, IsSnapToTickEnabled = true, AutoToolTipPlacement = AutoToolTipPlacement.TopLeft, VerticalAlignment = VerticalAlignment.Center };
            TextBox rapidIntervalBox = ModernNumericBox.Create();
            TextBlock rapidIntervalValue = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 15, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            rapidIntervalSlider.ValueChanged += delegate { rapidIntervalValue.Text = FormatSeconds(rapidIntervalSlider.Value); };
            rapidIntervalValue.Text = FormatSeconds(rapidIntervalSlider.Value);
            rapidGrid.Children.Add(rapidIntervalSlider);
            Grid.SetColumn(rapidIntervalBox, 1);
            rapidGrid.Children.Add(rapidIntervalBox);
            Grid.SetColumn(rapidIntervalValue, 2);
            rapidGrid.Children.Add(rapidIntervalValue);
            ModernNumericBox.Bind(rapidIntervalSlider, rapidIntervalBox);
            rapidStack.Children.Add(rapidGrid);
            rapidStack.Children.Add(new TextBlock { Text = "Periodic unitRestore (full or partial restore) with no key press - aircraft flaps (F) stay free. The manual F-key quick restore trigger still exists in the template.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
            Children.Add(rapidCard);
        }

        private Border Card(string title)
        {
            StackPanel stack = new StackPanel();
            stack.Children.Add(Heading(title, 15));
            return new Border { CornerRadius = new CornerRadius(15), BorderBrush = ModernPalette.Brush(ModernPalette.Border), BorderThickness = new Thickness(1), Background = ModernPalette.Brush("#A024324D"), Padding = new Thickness(16), Margin = new Thickness(0, 0, 0, 12), Child = stack };
        }

        private Slider OptionCard(StackPanel host, string title, string hint, double value, double minimum, double maximum, bool tenthStep)
        {
            Border card = Card(title);
            StackPanel stack = card.Child as StackPanel;
            Grid row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            Slider slider = new Slider { Minimum = minimum, Maximum = maximum, Value = Math.Max(minimum, Math.Min(maximum, value)), AutoToolTipPlacement = AutoToolTipPlacement.TopLeft, VerticalAlignment = VerticalAlignment.Center };
            if (tenthStep) { slider.TickFrequency = 0.1; slider.IsSnapToTickEnabled = true; }
            TextBox box = ModernNumericBox.Create();
            TextBlock valueText = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 15, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            slider.ValueChanged += delegate { valueText.Text = FormatSeconds(slider.Value); };
            valueText.Text = FormatSeconds(slider.Value);
            row.Children.Add(slider);
            Grid.SetColumn(box, 1);
            row.Children.Add(box);
            Grid.SetColumn(valueText, 2);
            row.Children.Add(valueText);
            stack.Children.Add(row);
            ModernNumericBox.Bind(slider, box);
            stack.Children.Add(new TextBlock { Text = hint, Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
            host.Children.Add(card);
            return slider;
        }

        private static string FormatSeconds(double value) { return value.ToString("0.###", CultureInfo.InvariantCulture) + " s"; }

        private static TextBlock Heading(string text, double size)
        {
            return new TextBlock { Text = text, Foreground = ModernPalette.Brush(ModernPalette.Text), FontSize = size, FontWeight = FontWeights.SemiBold };
        }

        public MissionSettings Apply()
        {
            MissionSettings updated = original.Copy();
            updated.PlayerRespawnDelaySeconds = respawnSlider.Value;
            updated.TargetRespawnDelaySeconds = targetSlider.Value;
            updated.RearmSeconds = rearmSlider.Value;
            ComboBoxItem ammo = ammoMode.SelectedItem as ComboBoxItem;
            updated.LimitedAmmo = ammo != null && (ammo.Tag as bool? ?? false);
            ComboBoxItem spawn = spawnMode.SelectedItem as ComboBoxItem;
            updated.SpawnMode = spawn != null && spawn.Tag is string ? (string)spawn.Tag : "air";
            updated.SpawnSpeedAuto = spawnSpeedAuto.IsChecked ?? true;
            updated.SpawnSpeedKmh = (int)Math.Round(spawnSpeedSlider.Value);
            updated.RapidFireEnabled = rapidToggle.IsChecked ?? false;
            updated.RapidFireInterval = rapidIntervalSlider.Value;
            updated.RapidFireFullRestore = rapidFullBox.IsChecked ?? true;
            return updated;
        }
    }

    internal sealed class ModernMissionOptionsWindow : ModernDialogWindow
    {
        private readonly MissionSettings original;
        private readonly Slider respawnSlider;
        private readonly Slider targetSlider;
        private readonly Slider rearmSlider;
        private readonly ComboBox ammoMode;
        private readonly ComboBox spawnMode;
        private readonly Slider spawnSpeedSlider;
        private readonly CheckBox spawnSpeedAuto;
        private readonly TextBox spawnSpeedBox;
        private readonly TextBlock spawnSpeedValue;
        private readonly StackPanel speedRow;
        private readonly CheckBox rapidToggle;
        private readonly Slider rapidIntervalSlider;
        private readonly CheckBox rapidFullBox;
        private readonly CheckBox rapidPartialBox;
        public MissionSettings Result { get; private set; }

        public ModernMissionOptionsWindow()
            : base("Mission Options", 640, 560)
        {
            original = MissionSettings.Current.Copy();
            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) });
            layout.RowDefinitions.Add(new RowDefinition());
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
            ContentCard.Child = layout;
            StackPanel header = new StackPanel();
            header.Children.Add(Heading("MISSION OPTIONS", 22));
            header.Children.Add(new TextBlock { Text = ModernText.L("Global — applies to every generated mission", "全局设置 — 应用于所有生成的任务"), Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 4, 0, 0) });
            layout.Children.Add(header);
            StackPanel content = new StackPanel();
            respawnSlider = OptionCard(content, "PLAYER RESPAWN DELAY", "Seconds before the player unit respawns after destruction (0 = instant).", original.PlayerRespawnDelaySeconds, 0, 60, true);
            targetSlider = OptionCard(content, "TARGET RECOVERY DELAY", "Seconds before destroyed target units reappear (stock template: 5 s, lab default: 0.25 s).", original.TargetRespawnDelaySeconds, 0.25, 30, true);
            rearmSlider = OptionCard(content, "GROUND REARM TIME", "Seconds spent on the field before ammunition is replenished (engine rearmTimeOnField).", original.RearmSeconds, 0, 60, true);
            Border ammoCard = Card("AMMUNITION POLICY");
            StackPanel ammoStack = ammoCard.Child as StackPanel;
            ammoMode = new ComboBox { Foreground = ModernPalette.Brush(ModernPalette.Text), Background = ModernPalette.Brush("#FF16283E"), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 260 };
            ammoMode.Items.Add(new ComboBoxItem { Content = ModernText.L("Unlimited ammunition", "无限弹药"), Tag = false });
            ammoMode.Items.Add(new ComboBoxItem { Content = ModernText.L("Limited + rearm on the field", "有限 + 战场再补给"), Tag = true });
            ammoMode.SelectedIndex = original.LimitedAmmo ? 1 : 0;
            ammoStack.Children.Add(ammoMode);
            ammoStack.Children.Add(new TextBlock { Text = "Unlimited keeps isLimitedAmmo=false. Limited enables the flag so ordnance and cannon ammunition can be exhausted and then replenished after the configured ground rearm time.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
            content.Children.Add(ammoCard);
            Border spawnCard = Card("SPAWN");
            StackPanel spawnStack = spawnCard.Child as StackPanel;
            spawnMode = new ComboBox { Foreground = ModernPalette.Brush(ModernPalette.Text), Background = ModernPalette.Brush("#FF16283E"), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 260 };
            spawnMode.Items.Add(new ComboBoxItem { Content = ModernText.L("Air spawn (with speed)", "空中出生（带速度）"), Tag = "air" });
            spawnMode.Items.Add(new ComboBoxItem { Content = ModernText.L("Airport takeoff (stationary)", "机场起飞（静止）"), Tag = "airport" });
            spawnMode.SelectedIndex = (original.SpawnMode ?? "air").Equals("airport", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            spawnStack.Children.Add(spawnMode);
            speedRow = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            Grid speedGrid = new Grid();
            speedGrid.ColumnDefinitions.Add(new ColumnDefinition());
            speedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
            speedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            spawnSpeedSlider = new Slider { Minimum = 0, Maximum = 800, Value = Math.Max(0, Math.Min(800, original.SpawnSpeedKmh)), TickFrequency = 10, IsSnapToTickEnabled = true, AutoToolTipPlacement = AutoToolTipPlacement.TopLeft, VerticalAlignment = VerticalAlignment.Center };
            spawnSpeedBox = ModernNumericBox.Create();
            spawnSpeedValue = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 15, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            spawnSpeedSlider.ValueChanged += delegate { spawnSpeedValue.Text = (int)Math.Round(spawnSpeedSlider.Value) + " km/h"; };
            spawnSpeedValue.Text = (int)Math.Round(spawnSpeedSlider.Value) + " km/h";
            speedGrid.Children.Add(spawnSpeedSlider);
            Grid.SetColumn(spawnSpeedBox, 1);
            speedGrid.Children.Add(spawnSpeedBox);
            Grid.SetColumn(spawnSpeedValue, 2);
            speedGrid.Children.Add(spawnSpeedValue);
            speedRow.Children.Add(speedGrid);
            ModernNumericBox.Bind(spawnSpeedSlider, spawnSpeedBox);
            spawnSpeedAuto = new CheckBox { Content = ModernText.L("Auto (airframe-safe speed)", "自动（机体安全速度）"), IsChecked = original.SpawnSpeedAuto, Foreground = ModernPalette.Brush(ModernPalette.Text), Margin = new Thickness(0, 8, 0, 0) };
            speedRow.Children.Add(spawnSpeedAuto);
            speedRow.Children.Add(new TextBlock { Text = "Airport takeoff always starts stationary; the speed row only applies to air spawns. Auto clamps the stock spawn speed to the airframe's lowest published maxSpeed so fragile aircraft no longer tear apart on spawn.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
            spawnStack.Children.Add(speedRow);
            spawnMode.SelectionChanged += delegate
            {
                ComboBoxItem item = spawnMode.SelectedItem as ComboBoxItem;
                bool airport = item != null && "airport".Equals(item.Tag as string, StringComparison.OrdinalIgnoreCase);
                speedRow.IsEnabled = !airport;
            };
            ComboBoxItem initial = spawnMode.SelectedItem as ComboBoxItem;
            speedRow.IsEnabled = !(initial != null && "airport".Equals(initial.Tag as string, StringComparison.OrdinalIgnoreCase));
            content.Children.Add(spawnCard);
            Border rapidCard = Card("RAPID FIRE (AUTO REPAIR + REARM)");
            StackPanel rapidStack = rapidCard.Child as StackPanel;
            rapidToggle = new CheckBox { Content = ModernText.L("ENABLED (auto repair + rearm)", "启用（自动维修 + 补给）"), IsChecked = original.RapidFireEnabled, Foreground = ModernPalette.Brush(ModernPalette.Text), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
            rapidStack.Children.Add(rapidToggle);
            rapidFullBox = new CheckBox { Content = ModernText.L("FULL RESTORE (all parts)", "完全修复（全部部件）"), IsChecked = original.RapidFireFullRestore, Foreground = ModernPalette.Brush(ModernPalette.Text), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 10, 0, 0) };
            rapidPartialBox = new CheckBox { Content = ModernText.L("PARTIAL RESTORE (barrel/breech/engine/tracks + crew + ammo + fuel)", "部分修复（炮管/炮闩/发动机/履带 + 乘员 + 弹药 + 燃油）"), IsChecked = !original.RapidFireFullRestore, Foreground = ModernPalette.Brush(ModernPalette.Text), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 0) };
            rapidFullBox.Checked += delegate { if (rapidPartialBox.IsChecked == true) rapidPartialBox.IsChecked = false; };
            rapidPartialBox.Checked += delegate { if (rapidFullBox.IsChecked == true) rapidFullBox.IsChecked = false; };
            rapidStack.Children.Add(rapidFullBox);
            rapidStack.Children.Add(rapidPartialBox);
            rapidStack.Children.Add(new TextBlock { Text = ModernText.L("REARM INTERVAL (s)", "补给间隔（秒）"), Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 0) });
            Grid rapidGrid = new Grid();
            rapidGrid.ColumnDefinitions.Add(new ColumnDefinition());
            rapidGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });
            rapidGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            rapidIntervalSlider = new Slider { Minimum = 0.1, Maximum = 10, Value = Math.Max(0.1, Math.Min(10, original.RapidFireInterval)), TickFrequency = 0.1, IsSnapToTickEnabled = true, AutoToolTipPlacement = AutoToolTipPlacement.TopLeft, VerticalAlignment = VerticalAlignment.Center };
            TextBox rapidIntervalBox = ModernNumericBox.Create();
            TextBlock rapidIntervalValue = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 15, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            rapidIntervalSlider.ValueChanged += delegate { rapidIntervalValue.Text = FormatSeconds(rapidIntervalSlider.Value); };
            rapidIntervalValue.Text = FormatSeconds(rapidIntervalSlider.Value);
            rapidGrid.Children.Add(rapidIntervalSlider);
            Grid.SetColumn(rapidIntervalBox, 1);
            rapidGrid.Children.Add(rapidIntervalBox);
            Grid.SetColumn(rapidIntervalValue, 2);
            rapidGrid.Children.Add(rapidIntervalValue);
            ModernNumericBox.Bind(rapidIntervalSlider, rapidIntervalBox);
            rapidStack.Children.Add(rapidGrid);
            rapidStack.Children.Add(new TextBlock { Text = "Periodic unitRestore (full or partial restore) with no key press - aircraft flaps (F) stay free. The manual F-key quick restore trigger still exists in the template.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
            content.Children.Add(rapidCard);
            ScrollViewer scroll = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(0, 6, 0, 14), Padding = new Thickness(0, 0, 8, 30) };
            Grid.SetRow(scroll, 1);
            layout.Children.Add(scroll);
            Grid footer = new Grid();
            footer.ColumnDefinitions.Add(new ColumnDefinition());
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(185) });
            footer.Children.Add(new TextBlock { Text = ModernText.L("Saved globally — no per-aircraft tuning needed.", "全局保存 —无需逐机调校。"), Foreground = ModernPalette.Brush(ModernPalette.Muted), VerticalAlignment = VerticalAlignment.Center });
            Button cancel = DialogButton("取消", false);
            cancel.Click += delegate { DialogResult = false; Close(); };
            Grid.SetColumn(cancel, 1);
            footer.Children.Add(cancel);
            Button apply = DialogButton(ModernText.L("APPLY OPTIONS", "应用选项"), true);
            apply.Click += delegate { Save(); };
            Grid.SetColumn(apply, 2);
            footer.Children.Add(apply);
            Grid.SetRow(footer, 2);
            layout.Children.Add(footer);
        }

        private Border Card(string title)
        {
            StackPanel stack = new StackPanel();
            stack.Children.Add(Heading(title, 15));
            return new Border { CornerRadius = new CornerRadius(15), BorderBrush = ModernPalette.Brush(ModernPalette.Border), BorderThickness = new Thickness(1), Background = ModernPalette.Brush("#A024324D"), Padding = new Thickness(16), Margin = new Thickness(0, 0, 0, 12), Child = stack };
        }

        private Slider OptionCard(StackPanel host, string title, string hint, double value, double minimum, double maximum, bool tenthStep)
        {
            Border card = Card(title);
            StackPanel stack = card.Child as StackPanel;
            Grid row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            Slider slider = new Slider { Minimum = minimum, Maximum = maximum, Value = Math.Max(minimum, Math.Min(maximum, value)), AutoToolTipPlacement = AutoToolTipPlacement.TopLeft, VerticalAlignment = VerticalAlignment.Center };
            if (tenthStep) { slider.TickFrequency = 0.1; slider.IsSnapToTickEnabled = true; }
            TextBox box = ModernNumericBox.Create();
            TextBlock valueText = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 15, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            slider.ValueChanged += delegate { valueText.Text = FormatSeconds(slider.Value); };
            valueText.Text = FormatSeconds(slider.Value);
            row.Children.Add(slider);
            Grid.SetColumn(box, 1);
            row.Children.Add(box);
            Grid.SetColumn(valueText, 2);
            row.Children.Add(valueText);
            stack.Children.Add(row);
            ModernNumericBox.Bind(slider, box);
            stack.Children.Add(new TextBlock { Text = hint, Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
            host.Children.Add(card);
            return slider;
        }

        private static string FormatSeconds(double value) { return value.ToString("0.###", CultureInfo.InvariantCulture) + " s"; }

        private void Save()
        {
            MissionSettings updated = original.Copy();
            updated.PlayerRespawnDelaySeconds = respawnSlider.Value;
            updated.TargetRespawnDelaySeconds = targetSlider.Value;
            updated.RearmSeconds = rearmSlider.Value;
            ComboBoxItem ammo = ammoMode.SelectedItem as ComboBoxItem;
            updated.LimitedAmmo = ammo != null && (ammo.Tag as bool? ?? false);
            ComboBoxItem spawn = spawnMode.SelectedItem as ComboBoxItem;
            updated.SpawnMode = spawn != null && spawn.Tag is string ? (string)spawn.Tag : "air";
            updated.SpawnSpeedAuto = spawnSpeedAuto.IsChecked ?? true;
            updated.SpawnSpeedKmh = (int)Math.Round(spawnSpeedSlider.Value);
            updated.RapidFireEnabled = rapidToggle.IsChecked ?? false;
            updated.RapidFireInterval = rapidIntervalSlider.Value;
            updated.RapidFireFullRestore = rapidFullBox.IsChecked ?? true;
            Result = updated;
            DialogResult = true;
            Close();
        }
    }
    // Embedded in the main-window EXPERIMENTAL tab; the standalone Flight
    // Configure window keeps its own copy (keep both in sync when editing).
    internal sealed class FlightConfigurePanel : StackPanel
    {
        private readonly AircraftSettings original;
        private readonly CheckBox fullFuel;
        private readonly Slider fuelSlider;
        private readonly TextBox fuelBox;
        private readonly TextBlock fuelValue;
        private readonly CheckBox customizeCountermeasures;
        private readonly ScrollViewer contentScroll;
        private readonly List<CountermeasureEditor> editors = new List<CountermeasureEditor>();
        private readonly List<GunBeltEditor> gunBeltEditors = new List<GunBeltEditor>();

        public FlightConfigurePanel(Aircraft aircraft, AircraftSettings current, IEnumerable<CountermeasureLauncher> launchers, IEnumerable<AircraftModification> modifications)
        {
            original = (current ?? new AircraftSettings()).Copy();
            if (String.IsNullOrWhiteSpace(original.InjectedCannonBlk))
            {
                original.InjectedCannonBlk = MissionSettings.Current.InjectedCannonBlk;
                original.InjectedCannonDomain = MissionSettings.Current.InjectedCannonDomain;
                original.InjectedCannonUnit = MissionSettings.Current.InjectedCannonUnit;
            }
            StackPanel header = new StackPanel();
            header.Children.Add(Heading(ModernText.L("FLIGHT CONFIGURE", "飞行配置"), 18));
            header.Children.Add(new TextBlock { Text = (aircraft == null ? "" : aircraft.Display) + "  •  fuel, gun belts and countermeasure stations", Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 4, 0, 0) });
            Children.Add(header);
            StackPanel content = new StackPanel();
            Border fuelCard = Card("STARTING FUEL"); StackPanel fuelContent = fuelCard.Child as StackPanel;
            fullFuel = new CheckBox { Content = ModernText.L("Full internal fuel", "满内部燃油"), IsChecked = original.FullFuel, Foreground = ModernPalette.Brush(ModernPalette.Text), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 8) }; fuelContent.Children.Add(fullFuel);
            Grid fuelRow = new Grid(); fuelRow.ColumnDefinitions.Add(new ColumnDefinition()); fuelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) }); fuelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            fuelSlider = new Slider { Minimum = 5, Maximum = 60, TickFrequency = 5, IsSnapToTickEnabled = true, Value = Math.Max(5, Math.Min(60, original.FuelMinutes)), AutoToolTipPlacement = AutoToolTipPlacement.TopLeft, VerticalAlignment = VerticalAlignment.Center };
            fuelBox = ModernNumericBox.Create();
            fuelValue = ValueText(); fuelRow.Children.Add(fuelSlider); Grid.SetColumn(fuelBox, 1); fuelRow.Children.Add(fuelBox); Grid.SetColumn(fuelValue, 2); fuelRow.Children.Add(fuelValue); fuelContent.Children.Add(fuelRow);
            ModernNumericBox.Bind(fuelSlider, fuelBox);
            fuelContent.Children.Add(new TextBlock { Text = "Minutes are mapped to the aircraft's internal-fuel percentage used by User Missions. External tanks are never added automatically.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) }); content.Children.Add(fuelCard);

            AddGunBeltCard(content, modifications == null ? Enumerable.Empty<AircraftModification>() : modifications);

            Border cmCard = Card("COUNTERMEASURE STATIONS"); StackPanel cmContent = cmCard.Child as StackPanel;
            customizeCountermeasures = new CheckBox { Content = ModernText.L("Customize installed countermeasure stations", "自定义已装干扰弹站"), IsChecked = original.OverrideCountermeasures, Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 8) }; cmContent.Children.Add(customizeCountermeasures);
            foreach (CountermeasureLauncher launcher in launchers) AddLauncher(cmContent, launcher);
            cmContent.Children.Add(new TextBlock { Text = "Each emitter is configured separately. Flare-only or chaff-only dispensers expose only the supported slider; BOL, BKO and MAW modules still decide which stations exist. Ammunition is restored only after it is exhausted so active optics and seekers are not reset in flight.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) }); content.Children.Add(cmCard);
            contentScroll = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(0, 6, 0, 8), Padding = new Thickness(0, 0, 8, 20) };
            Children.Add(contentScroll);
            fuelSlider.ValueChanged += delegate { UpdateState(); }; fullFuel.Checked += delegate { UpdateState(); }; fullFuel.Unchecked += delegate { UpdateState(); };
            customizeCountermeasures.Checked += delegate { UpdateState(); }; customizeCountermeasures.Unchecked += delegate { UpdateState(); }; UpdateState();
        }

        private Border Card(string title)
        {
            StackPanel stack = new StackPanel(); stack.Children.Add(Heading(title, 15));
            return new Border { CornerRadius = new CornerRadius(15), BorderBrush = ModernPalette.Brush(ModernPalette.Border), BorderThickness = new Thickness(1), Background = ModernPalette.Brush("#A024324D"), Padding = new Thickness(16), Margin = new Thickness(0, 0, 0, 12), Child = stack };
        }

        private TextBlock ValueText()
        {
            return new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 15, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        }

        private void AddGunBeltCard(StackPanel host, IEnumerable<AircraftModification> source)
        {
            List<AircraftModification> all = source.ToList();
            List<IGrouping<string, AircraftModification>> families = all.Where(IsGunBeltChoice)
                .GroupBy(x => GunBeltFamily(x.Id), StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Take(4).ToList();
            if (families.Count == 0) return;
            List<AircraftModification> beltPacks = all.Where(x => x.Id.IndexOf("belt_pack", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            Border card = Card("CANNON AMMUNITION BELTS");
            StackPanel stack = card.Child as StackPanel;
            stack.Children.Add(new TextBlock { Text = ModernText.L("Available belts follow the current Modules configuration.", "可用弹带随当前模块配置。"), Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 7, 0, 8) });
            int groupIndex = 0;
            foreach (IGrouping<string, AircraftModification> family in families)
            {
                List<AircraftModification> relatedPacks = beltPacks.Where(x => RelatedBeltPack(family.Key, x.Id)).ToList();
                if (relatedPacks.Count == 0 && families.Count == 1) relatedPacks = beltPacks;
                bool unlocked = original.UseAllModifications || relatedPacks.Count == 0 || relatedPacks.Any(x => original.EnabledModifications.Contains(x.Id));
                List<GunBeltChoice> options = new List<GunBeltChoice> { new GunBeltChoice { Id = "", Display = "Default belt (stock)" } };
                if (unlocked)
                    options.AddRange(family.OrderBy(x => x.Display).Select(x => new GunBeltChoice { Id = x.Id, Display = x.Display }));
                Grid row = new Grid { Margin = new Thickness(0, 4, 0, 7) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) }); row.ColumnDefinitions.Add(new ColumnDefinition());
                string familyName = family.Key.Replace('_', ' ').ToUpperInvariant();
                StackPanel label = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                label.Children.Add(new TextBlock { Text = ModernText.L("GUN GROUP ", "机炮组 ") + (groupIndex + 1).ToString(CultureInfo.InvariantCulture) + "  •  " + familyName, Foreground = ModernPalette.Brush(ModernPalette.Text), FontWeight = FontWeights.SemiBold, FontSize = 11 });
                if (!unlocked) label.Children.Add(new TextBlock { Text = ModernText.L("Enable its Belt Pack in Modules", "在模块中启用其弹带包"), Foreground = ModernPalette.Brush(ModernPalette.Danger), FontSize = 10, Margin = new Thickness(0, 2, 0, 0) });
                row.Children.Add(label);
                ComboBox selector = new ComboBox { ItemsSource = options, Margin = new Thickness(8, 0, 0, 0) };
                string saved;
                original.GunBeltSelections.TryGetValue(groupIndex, out saved);
                selector.SelectedItem = options.FirstOrDefault(x => !String.IsNullOrEmpty(saved) && x.Id.Equals(saved, StringComparison.OrdinalIgnoreCase)) ?? options[0];
                Grid.SetColumn(selector, 1); row.Children.Add(selector); stack.Children.Add(row);
                gunBeltEditors.Add(new GunBeltEditor { GroupIndex = groupIndex, Selection = selector });
                groupIndex++;
            }
            host.Children.Add(card);
        }

        private static bool IsGunBeltChoice(AircraftModification modification)
        {
            if (modification == null || modification.Tier != 0 || String.IsNullOrWhiteSpace(modification.Id)) return false;
            string id = modification.Id.ToLowerInvariant();
            return Regex.IsMatch(id, @"_(?:air_targets?|ground_targets?|armor_targets?|stealth|tracers?|all_tracers|universal|turret_ap(?:_t)?|turret_api)$") ||
                (modification.Display ?? "").IndexOf("ammunition belt", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GunBeltFamily(string id)
        {
            return Regex.Replace((id ?? "").ToLowerInvariant(), @"_(?:air_targets?|ground_targets?|armor_targets?|stealth|tracers?|all_tracers|universal|turret_ap(?:_t)?|turret_api)$", "");
        }

        private static bool RelatedBeltPack(string family, string packId)
        {
            string left = Regex.Replace(family ?? "", @"[^a-z0-9]", "");
            string right = Regex.Replace((packId ?? "").Replace("belt_pack", ""), @"[^a-z0-9]", "");
            return left.Length > 0 && right.Length > 0 && (left.StartsWith(right, StringComparison.OrdinalIgnoreCase) || right.StartsWith(left, StringComparison.OrdinalIgnoreCase));
        }

        private void AddLauncher(StackPanel host, CountermeasureLauncher launcher)
        {
            CountermeasureLoadout saved = original.CountermeasureLoadouts.FirstOrDefault(x => x.Key.Equals(launcher.Key, StringComparison.OrdinalIgnoreCase));
            int flares = saved == null ? (launcher.AllowsFlares ? (launcher.AllowsChaff ? launcher.NativeRounds / 2 : launcher.NativeRounds) : 0) : saved.Flares;
            int chaff = saved == null ? (launcher.AllowsChaff ? (launcher.AllowsFlares ? launcher.NativeRounds - flares : launcher.NativeRounds) : 0) : saved.Chaff;
            Border card = new Border { CornerRadius = new CornerRadius(12), Background = ModernPalette.Brush(ModernPalette.Field), BorderBrush = ModernPalette.Brush("#526F99"), BorderThickness = new Thickness(1), Padding = new Thickness(12), Margin = new Thickness(0, 4, 0, 8) };
            StackPanel stack = new StackPanel(); stack.Children.Add(new TextBlock { Text = launcher.Display, FontWeight = FontWeights.SemiBold, Foreground = ModernPalette.Brush(ModernPalette.Text) });
            stack.Children.Add(new TextBlock { Text = ModernText.L("Native capacity: ", "原生容量: ") + launcher.NativeRounds.ToString(CultureInfo.InvariantCulture), Foreground = ModernPalette.Brush(ModernPalette.Muted), FontSize = 10, Margin = new Thickness(0, 2, 0, 8) });
            CountermeasureEditor editor = new CountermeasureEditor { Launcher = launcher, Card = card };
            if (launcher.AllowsFlares) AddCountermeasureSlider(stack, "FLARES", flares, out editor.FlareSlider, out editor.FlareValue);
            if (launcher.AllowsChaff) AddCountermeasureSlider(stack, "CHAFF", chaff, out editor.ChaffSlider, out editor.ChaffValue);
            editor.GamePreview = new TextBlock { Text = "", FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 6, 0, 0), Foreground = ModernPalette.Brush(ModernPalette.Cyan) };
            stack.Children.Add(editor.GamePreview);
            if (editor.FlareSlider != null) editor.FlareSlider.ValueChanged += delegate { UpdateGamePreview(editor); };
            if (editor.ChaffSlider != null) editor.ChaffSlider.ValueChanged += delegate { UpdateGamePreview(editor); };
            UpdateGamePreview(editor);
            card.Child = stack; host.Children.Add(card); editors.Add(editor);
        }

        private void AddCountermeasureSlider(StackPanel host, string name, int initial, out Slider slider, out TextBlock value)
        {
            Grid row = new Grid { Margin = new Thickness(0, 3, 0, 3) }; row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) }); row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            row.Children.Add(new TextBlock { Text = name, Foreground = ModernPalette.Brush(ModernPalette.Muted), VerticalAlignment = VerticalAlignment.Center, FontSize = 11 });
            slider = new Slider { Minimum = 0, Maximum = 512, TickFrequency = 1, IsSnapToTickEnabled = true, Value = Math.Max(0, Math.Min(512, initial)), AutoToolTipPlacement = AutoToolTipPlacement.TopLeft, VerticalAlignment = VerticalAlignment.Center };
            TextBox box = ModernNumericBox.Create(); box.Height = 30; box.Padding = new Thickness(6, 2, 6, 2); box.Margin = new Thickness(4, 0, 0, 0);
            Grid.SetColumn(slider, 1); row.Children.Add(slider); value = ValueText(); value.Text = ((int)slider.Value).ToString(CultureInfo.InvariantCulture); Grid.SetColumn(box, 2); row.Children.Add(box); Grid.SetColumn(value, 3); row.Children.Add(value); host.Children.Add(row);
            Slider sliderControl = slider; TextBlock valueControl = value; slider.ValueChanged += delegate { valueControl.Text = ((int)sliderControl.Value).ToString(CultureInfo.InvariantCulture); };
            ModernNumericBox.Bind(slider, box);
        }

        private static void UpdateGamePreview(CountermeasureEditor editor)
        {
            if (editor == null || editor.GamePreview == null) return;
            int flares = editor.FlareSlider == null ? 0 : (int)editor.FlareSlider.Value;
            int chaff = editor.ChaffSlider == null ? 0 : (int)editor.ChaffSlider.Value;
            int total = flares + chaff;
            if (total <= 0)
            {
                editor.GamePreview.Text = ModernText.L("IN GAME: 0 FLARE / 0 CHAFF", "游戏内: 0 红外干扰弹 / 0 箔条");
                editor.GamePreview.Foreground = ModernPalette.Brush(ModernPalette.Muted);
                return;
            }
            int eighths = (int)Math.Ceiling(8.0 * flares / total);
            int displayFlares = (int)Math.Ceiling(total * eighths / 8.0);
            int displayChaff = total - displayFlares;
            bool pure = eighths == 0 || eighths == 8;
            editor.GamePreview.Text = ModernText.L("IN GAME: ", "游戏内: ") + displayFlares.ToString(CultureInfo.InvariantCulture) + " FLARE / " + displayChaff.ToString(CultureInfo.InvariantCulture) + " CHAFF" + (pure ? "" : "   (quantized to 1/8 steps)");
            editor.GamePreview.Foreground = ModernPalette.Brush(pure ? ModernPalette.Muted : ModernPalette.Cyan);
        }

        private void UpdateState()
        {
            fuelSlider.IsEnabled = fullFuel.IsChecked != true;
            int fuelPercent = (int)Math.Round(fuelSlider.Value * 100.0 / 60.0);
            fuelValue.Text = fullFuel.IsChecked == true ? "FULL  •  100%" : ((int)fuelSlider.Value).ToString(CultureInfo.InvariantCulture) + " MIN  •  " + fuelPercent.ToString(CultureInfo.InvariantCulture) + "%";
            bool enabled = customizeCountermeasures.IsChecked == true;
            foreach (CountermeasureEditor editor in editors) { if (editor.FlareSlider != null) editor.FlareSlider.IsEnabled = enabled; if (editor.ChaffSlider != null) editor.ChaffSlider.IsEnabled = enabled; editor.Card.Opacity = customizeCountermeasures.IsChecked == true ? 1.0 : 0.62; }
        }

        public AircraftSettings Collect()
        {
            AircraftSettings result = original.Copy(); result.FullFuel = fullFuel.IsChecked == true; result.FuelMinutes = (int)fuelSlider.Value; result.OverrideCountermeasures = customizeCountermeasures.IsChecked == true; result.UnlimitedCountermeasures = false; result.CountermeasureLoadouts.Clear(); result.GunBeltSelections.Clear();
            foreach (GunBeltEditor editor in gunBeltEditors)
            {
                GunBeltChoice selected = editor.Selection.SelectedItem as GunBeltChoice;
                if (selected != null && !String.IsNullOrWhiteSpace(selected.Id)) result.GunBeltSelections[editor.GroupIndex] = selected.Id;
            }
            int totalFlares = 0, totalChaff = 0;
            foreach (CountermeasureEditor editor in editors)
            {
                int flares = editor.FlareSlider == null ? 0 : (int)editor.FlareSlider.Value; int chaff = editor.ChaffSlider == null ? 0 : (int)editor.ChaffSlider.Value;
                result.CountermeasureLoadouts.Add(new CountermeasureLoadout { Key = editor.Launcher.Key, Flares = flares, Chaff = chaff }); totalFlares += flares; totalChaff += chaff;
            }
            result.FlareRounds = totalFlares; result.ChaffRounds = totalChaff;
            return result;
        }

        private TextBlock Heading(string text, double size)
        {
            return new TextBlock { Text = text, FontSize = size, FontWeight = FontWeights.SemiBold, Foreground = ModernPalette.Brush(ModernPalette.Text) };
        }
    }

    internal sealed class ModernFlightConfigureWindow : ModernDialogWindow
    {
        private readonly AircraftSettings original;
        private readonly CheckBox fullFuel;
        private readonly Slider fuelSlider;
        private readonly TextBox fuelBox;
        private readonly TextBlock fuelValue;
        private readonly CheckBox customizeCountermeasures;
        private readonly ScrollViewer contentScroll;
        private readonly List<CountermeasureEditor> editors = new List<CountermeasureEditor>();
        private readonly List<GunBeltEditor> gunBeltEditors = new List<GunBeltEditor>();
        public AircraftSettings Result { get; private set; }

        public ModernFlightConfigureWindow(Aircraft aircraft, AircraftSettings current, IEnumerable<CountermeasureLauncher> launchers, IEnumerable<AircraftModification> modifications)
            : base("Flight Configure — " + aircraft.Display, 940, 780)
        {
            original = (current ?? new AircraftSettings()).Copy();
            // Fall back to the globally remembered cannon injection so the last
            // domain/unit/weapon choice is reused across vehicles and sessions.
            if (String.IsNullOrWhiteSpace(original.InjectedCannonBlk))
            {
                original.InjectedCannonBlk = MissionSettings.Current.InjectedCannonBlk;
                original.InjectedCannonDomain = MissionSettings.Current.InjectedCannonDomain;
                original.InjectedCannonUnit = MissionSettings.Current.InjectedCannonUnit;
            }
            Grid layout = new Grid(); layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) }); layout.RowDefinitions.Add(new RowDefinition()); layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) }); ContentCard.Child = layout;
            StackPanel header = new StackPanel(); header.Children.Add(Heading(ModernText.L("FLIGHT CONFIGURE", "飞行配置"), 22)); header.Children.Add(new TextBlock { Text = aircraft.Display + "  •  fuel, gun belts and countermeasure stations", Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 4, 0, 0) }); layout.Children.Add(header);
            StackPanel content = new StackPanel();
            Border fuelCard = Card("STARTING FUEL"); StackPanel fuelContent = fuelCard.Child as StackPanel;
            fullFuel = new CheckBox { Content = ModernText.L("Full internal fuel", "满内部燃油"), IsChecked = original.FullFuel, Foreground = ModernPalette.Brush(ModernPalette.Text), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 8) }; fuelContent.Children.Add(fullFuel);
            Grid fuelRow = new Grid(); fuelRow.ColumnDefinitions.Add(new ColumnDefinition()); fuelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) }); fuelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            fuelSlider = new Slider { Minimum = 5, Maximum = 60, TickFrequency = 5, IsSnapToTickEnabled = true, Value = Math.Max(5, Math.Min(60, original.FuelMinutes)), AutoToolTipPlacement = AutoToolTipPlacement.TopLeft, VerticalAlignment = VerticalAlignment.Center };
            fuelBox = ModernNumericBox.Create();
            fuelValue = ValueText(); fuelRow.Children.Add(fuelSlider); Grid.SetColumn(fuelBox, 1); fuelRow.Children.Add(fuelBox); Grid.SetColumn(fuelValue, 2); fuelRow.Children.Add(fuelValue); fuelContent.Children.Add(fuelRow);
            ModernNumericBox.Bind(fuelSlider, fuelBox);
            fuelContent.Children.Add(new TextBlock { Text = "Minutes are mapped to the aircraft's internal-fuel percentage used by User Missions. External tanks are never added automatically.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) }); content.Children.Add(fuelCard);

            AddGunBeltCard(content, modifications == null ? Enumerable.Empty<AircraftModification>() : modifications);

            Border cmCard = Card("COUNTERMEASURE STATIONS"); StackPanel cmContent = cmCard.Child as StackPanel;
            customizeCountermeasures = new CheckBox { Content = ModernText.L("Customize installed countermeasure stations", "自定义已装干扰弹站"), IsChecked = original.OverrideCountermeasures, Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 8) }; cmContent.Children.Add(customizeCountermeasures);
            foreach (CountermeasureLauncher launcher in launchers) AddLauncher(cmContent, launcher);
            cmContent.Children.Add(new TextBlock { Text = "Each emitter is configured separately. Flare-only or chaff-only dispensers expose only the supported slider; BOL, BKO and MAW modules still decide which stations exist. Ammunition is restored only after it is exhausted so active optics and seekers are not reset in flight.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) }); content.Children.Add(cmCard);
            contentScroll = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(0, 6, 0, 14), Padding = new Thickness(0, 0, 8, 30) }; Grid.SetRow(contentScroll, 1); layout.Children.Add(contentScroll);
            Grid footer = new Grid(); footer.ColumnDefinitions.Add(new ColumnDefinition()); footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) }); footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(175) }); footer.Children.Add(new TextBlock { Text = ModernText.L("Settings are saved with this aircraft and custom presets.", "设置随该飞机及自定义预设保存。"), Foreground = ModernPalette.Brush(ModernPalette.Muted), VerticalAlignment = VerticalAlignment.Center });
            Button cancel = DialogButton("取消", false); cancel.Click += delegate { DialogResult = false; Close(); }; Grid.SetColumn(cancel, 1); footer.Children.Add(cancel);
            Button apply = DialogButton("APPLY CONFIG", true); apply.Click += delegate { Save(); }; Grid.SetColumn(apply, 2); footer.Children.Add(apply); Grid.SetRow(footer, 2); layout.Children.Add(footer);
            fuelSlider.ValueChanged += delegate { UpdateState(); }; fullFuel.Checked += delegate { UpdateState(); }; fullFuel.Unchecked += delegate { UpdateState(); };
            customizeCountermeasures.Checked += delegate { UpdateState(); }; customizeCountermeasures.Unchecked += delegate { UpdateState(); }; UpdateState();
        }

        internal void ScrollToEndForScreenshot()
        {
            contentScroll.ScrollToEnd();
            UpdateLayout();
        }

        private Border Card(string title)
        {
            StackPanel stack = new StackPanel(); stack.Children.Add(Heading(title, 15));
            return new Border { CornerRadius = new CornerRadius(15), BorderBrush = ModernPalette.Brush(ModernPalette.Border), BorderThickness = new Thickness(1), Background = ModernPalette.Brush("#A024324D"), Padding = new Thickness(16), Margin = new Thickness(0, 0, 0, 12), Child = stack };
        }

        private TextBlock ValueText()
        {
            return new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 15, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        }

        private void AddGunBeltCard(StackPanel host, IEnumerable<AircraftModification> source)
        {
            List<AircraftModification> all = source.ToList();
            List<IGrouping<string, AircraftModification>> families = all.Where(IsGunBeltChoice)
                .GroupBy(x => GunBeltFamily(x.Id), StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Take(4).ToList();
            if (families.Count == 0) return;
            List<AircraftModification> beltPacks = all.Where(x => x.Id.IndexOf("belt_pack", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            Border card = Card("CANNON AMMUNITION BELTS");
            StackPanel stack = card.Child as StackPanel;
            stack.Children.Add(new TextBlock { Text = ModernText.L("Available belts follow the current Modules configuration.", "可用弹带随当前模块配置。"), Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 7, 0, 8) });
            int groupIndex = 0;
            foreach (IGrouping<string, AircraftModification> family in families)
            {
                List<AircraftModification> relatedPacks = beltPacks.Where(x => RelatedBeltPack(family.Key, x.Id)).ToList();
                if (relatedPacks.Count == 0 && families.Count == 1) relatedPacks = beltPacks;
                bool unlocked = original.UseAllModifications || relatedPacks.Count == 0 || relatedPacks.Any(x => original.EnabledModifications.Contains(x.Id));
                List<GunBeltChoice> options = new List<GunBeltChoice> { new GunBeltChoice { Id = "", Display = "Default belt (stock)" } };
                if (unlocked)
                    options.AddRange(family.OrderBy(x => x.Display).Select(x => new GunBeltChoice { Id = x.Id, Display = x.Display }));
                Grid row = new Grid { Margin = new Thickness(0, 4, 0, 7) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) }); row.ColumnDefinitions.Add(new ColumnDefinition());
                string familyName = family.Key.Replace('_', ' ').ToUpperInvariant();
                StackPanel label = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                label.Children.Add(new TextBlock { Text = ModernText.L("GUN GROUP ", "机炮组 ") + (groupIndex + 1).ToString(CultureInfo.InvariantCulture) + "  •  " + familyName, Foreground = ModernPalette.Brush(ModernPalette.Text), FontWeight = FontWeights.SemiBold, FontSize = 11 });
                if (!unlocked) label.Children.Add(new TextBlock { Text = ModernText.L("Enable its Belt Pack in Modules", "在模块中启用其弹带包"), Foreground = ModernPalette.Brush(ModernPalette.Danger), FontSize = 10, Margin = new Thickness(0, 2, 0, 0) });
                row.Children.Add(label);
                ComboBox selector = new ComboBox { ItemsSource = options, Margin = new Thickness(8, 0, 0, 0) };
                string saved;
                original.GunBeltSelections.TryGetValue(groupIndex, out saved);
                selector.SelectedItem = options.FirstOrDefault(x => !String.IsNullOrEmpty(saved) && x.Id.Equals(saved, StringComparison.OrdinalIgnoreCase)) ?? options[0];
                Grid.SetColumn(selector, 1); row.Children.Add(selector); stack.Children.Add(row);
                gunBeltEditors.Add(new GunBeltEditor { GroupIndex = groupIndex, Selection = selector });
                groupIndex++;
            }
            host.Children.Add(card);
        }

        private static bool IsGunBeltChoice(AircraftModification modification)
        {
            if (modification == null || modification.Tier != 0 || String.IsNullOrWhiteSpace(modification.Id)) return false;
            string id = modification.Id.ToLowerInvariant();
            return Regex.IsMatch(id, @"_(?:air_targets?|ground_targets?|armor_targets?|stealth|tracers?|all_tracers|universal|turret_ap(?:_t)?|turret_api)$") ||
                (modification.Display ?? "").IndexOf("ammunition belt", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static string GunBeltFamily(string id)
        {
            return Regex.Replace((id ?? "").ToLowerInvariant(), @"_(?:air_targets?|ground_targets?|armor_targets?|stealth|tracers?|all_tracers|universal|turret_ap(?:_t)?|turret_api)$", "");
        }

        internal static bool RelatedBeltPack(string family, string packId)
        {
            string left = Regex.Replace(family ?? "", @"[^a-z0-9]", "");
            string right = Regex.Replace((packId ?? "").Replace("belt_pack", ""), @"[^a-z0-9]", "");
            return left.Length > 0 && right.Length > 0 && (left.StartsWith(right, StringComparison.OrdinalIgnoreCase) || right.StartsWith(left, StringComparison.OrdinalIgnoreCase));
        }

        private void AddLauncher(StackPanel host, CountermeasureLauncher launcher)
        {
            CountermeasureLoadout saved = original.CountermeasureLoadouts.FirstOrDefault(x => x.Key.Equals(launcher.Key, StringComparison.OrdinalIgnoreCase));
            int flares = saved == null ? (launcher.AllowsFlares ? (launcher.AllowsChaff ? launcher.NativeRounds / 2 : launcher.NativeRounds) : 0) : saved.Flares;
            int chaff = saved == null ? (launcher.AllowsChaff ? (launcher.AllowsFlares ? launcher.NativeRounds - flares : launcher.NativeRounds) : 0) : saved.Chaff;
            Border card = new Border { CornerRadius = new CornerRadius(12), Background = ModernPalette.Brush(ModernPalette.Field), BorderBrush = ModernPalette.Brush("#526F99"), BorderThickness = new Thickness(1), Padding = new Thickness(12), Margin = new Thickness(0, 4, 0, 8) };
            StackPanel stack = new StackPanel(); stack.Children.Add(new TextBlock { Text = launcher.Display, FontWeight = FontWeights.SemiBold, Foreground = ModernPalette.Brush(ModernPalette.Text) });
            stack.Children.Add(new TextBlock { Text = ModernText.L("Native capacity: ", "原生容量: ") + launcher.NativeRounds.ToString(CultureInfo.InvariantCulture), Foreground = ModernPalette.Brush(ModernPalette.Muted), FontSize = 10, Margin = new Thickness(0, 2, 0, 8) });
            CountermeasureEditor editor = new CountermeasureEditor { Launcher = launcher, Card = card };
            if (launcher.AllowsFlares) AddCountermeasureSlider(stack, "FLARES", flares, out editor.FlareSlider, out editor.FlareValue);
            if (launcher.AllowsChaff) AddCountermeasureSlider(stack, "CHAFF", chaff, out editor.ChaffSlider, out editor.ChaffValue);
            editor.GamePreview = new TextBlock { Text = "", FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 6, 0, 0), Foreground = ModernPalette.Brush(ModernPalette.Cyan) };
            stack.Children.Add(editor.GamePreview);
            if (editor.FlareSlider != null) editor.FlareSlider.ValueChanged += delegate { UpdateGamePreview(editor); };
            if (editor.ChaffSlider != null) editor.ChaffSlider.ValueChanged += delegate { UpdateGamePreview(editor); };
            UpdateGamePreview(editor);
            card.Child = stack; host.Children.Add(card); editors.Add(editor);
        }

        private void AddCountermeasureSlider(StackPanel host, string name, int initial, out Slider slider, out TextBlock value)
        {
            Grid row = new Grid { Margin = new Thickness(0, 3, 0, 3) }; row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) }); row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            row.Children.Add(new TextBlock { Text = name, Foreground = ModernPalette.Brush(ModernPalette.Muted), VerticalAlignment = VerticalAlignment.Center, FontSize = 11 });
            slider = new Slider { Minimum = 0, Maximum = 512, TickFrequency = 1, IsSnapToTickEnabled = true, Value = Math.Max(0, Math.Min(512, initial)), AutoToolTipPlacement = AutoToolTipPlacement.TopLeft, VerticalAlignment = VerticalAlignment.Center };
            TextBox box = ModernNumericBox.Create(); box.Height = 30; box.Padding = new Thickness(6, 2, 6, 2); box.Margin = new Thickness(4, 0, 0, 0);
            Grid.SetColumn(slider, 1); row.Children.Add(slider); value = ValueText(); value.Text = ((int)slider.Value).ToString(CultureInfo.InvariantCulture); Grid.SetColumn(box, 2); row.Children.Add(box); Grid.SetColumn(value, 3); row.Children.Add(value); host.Children.Add(row);
            Slider sliderControl = slider; TextBlock valueControl = value; slider.ValueChanged += delegate { valueControl.Text = ((int)sliderControl.Value).ToString(CultureInfo.InvariantCulture); };
            ModernNumericBox.Bind(slider, box);
        }

        // The game quantizes the flare/chaff split to 1/8 (12.5%) steps, rounding
        // the flare share up to the next eighth. Show what the HUD will actually
        // display so the user is not surprised after generating a mission.
        private static void UpdateGamePreview(CountermeasureEditor editor)
        {
            if (editor == null || editor.GamePreview == null) return;
            int flares = editor.FlareSlider == null ? 0 : (int)editor.FlareSlider.Value;
            int chaff = editor.ChaffSlider == null ? 0 : (int)editor.ChaffSlider.Value;
            int total = flares + chaff;
            if (total <= 0)
            {
                editor.GamePreview.Text = ModernText.L("IN GAME: 0 FLARE / 0 CHAFF", "游戏内: 0 红外干扰弹 / 0 箔条");
                editor.GamePreview.Foreground = ModernPalette.Brush(ModernPalette.Muted);
                return;
            }
            int eighths = (int)Math.Ceiling(8.0 * flares / total);
            int displayFlares = (int)Math.Ceiling(total * eighths / 8.0);
            int displayChaff = total - displayFlares;
            bool pure = eighths == 0 || eighths == 8;
            editor.GamePreview.Text = ModernText.L("IN GAME: ", "游戏内: ") + displayFlares.ToString(CultureInfo.InvariantCulture) + " FLARE / " + displayChaff.ToString(CultureInfo.InvariantCulture) + " CHAFF" + (pure ? "" : "   (quantized to 1/8 steps)");
            editor.GamePreview.Foreground = ModernPalette.Brush(pure ? ModernPalette.Muted : ModernPalette.Cyan);
        }

        private void UpdateState()
        {
            fuelSlider.IsEnabled = fullFuel.IsChecked != true;
            int fuelPercent = (int)Math.Round(fuelSlider.Value * 100.0 / 60.0);
            fuelValue.Text = fullFuel.IsChecked == true ? "FULL  •  100%" : ((int)fuelSlider.Value).ToString(CultureInfo.InvariantCulture) + " MIN  •  " + fuelPercent.ToString(CultureInfo.InvariantCulture) + "%";
            bool enabled = customizeCountermeasures.IsChecked == true;
            foreach (CountermeasureEditor editor in editors) { if (editor.FlareSlider != null) editor.FlareSlider.IsEnabled = enabled; if (editor.ChaffSlider != null) editor.ChaffSlider.IsEnabled = enabled; editor.Card.Opacity = customizeCountermeasures.IsChecked == true ? 1.0 : 0.62; }
        }

        private void Save()
        {
            AircraftSettings result = original.Copy(); result.FullFuel = fullFuel.IsChecked == true; result.FuelMinutes = (int)fuelSlider.Value; result.OverrideCountermeasures = customizeCountermeasures.IsChecked == true; result.UnlimitedCountermeasures = false; result.CountermeasureLoadouts.Clear(); result.GunBeltSelections.Clear();
            foreach (GunBeltEditor editor in gunBeltEditors)
            {
                GunBeltChoice selected = editor.Selection.SelectedItem as GunBeltChoice;
                if (selected != null && !String.IsNullOrWhiteSpace(selected.Id)) result.GunBeltSelections[editor.GroupIndex] = selected.Id;
            }
            int totalFlares = 0, totalChaff = 0;
            foreach (CountermeasureEditor editor in editors)
            {
                int flares = editor.FlareSlider == null ? 0 : (int)editor.FlareSlider.Value; int chaff = editor.ChaffSlider == null ? 0 : (int)editor.ChaffSlider.Value;
                result.CountermeasureLoadouts.Add(new CountermeasureLoadout { Key = editor.Launcher.Key, Flares = flares, Chaff = chaff }); totalFlares += flares; totalChaff += chaff;
            }
            result.FlareRounds = totalFlares; result.ChaffRounds = totalChaff; Result = result; DialogResult = true; Close();
        }
    }

    internal sealed class ModificationChoice
    {
        public AircraftModification Definition;
        public CheckBox Check;
    }

    internal sealed class ModernFlightSystemsWindow : ModernDialogWindow
    {
        private readonly Aircraft aircraft;
        private readonly AircraftSettings originalSettings;
        private readonly List<AircraftModification> definitions;
        private readonly List<ModificationChoice> choices = new List<ModificationChoice>();
        private readonly Grid pageHost;
        private readonly CheckBox allMods;
        private readonly StackPanel modificationList;
        private readonly FrameworkElement modulesPage;
        public AircraftSettings Result { get; private set; }

        public ModernFlightSystemsWindow(Aircraft item, IEnumerable<AircraftModification> modifications, AircraftSettings current, bool isHelicopter)
            : base("Modules — " + item.Display, 1080, 740)
        {
            aircraft = item;
            definitions = modifications.OrderBy(x => x.Tier).ThenBy(x => x.Display).ToList();
            AircraftSettings settings = (current ?? new AircraftSettings()).Copy();
            originalSettings = settings.Copy();

            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(54) });
            ContentCard.Child = layout;

            bool ground = String.Equals(item.Kind, "Ground Vehicle", StringComparison.OrdinalIgnoreCase);
            Grid header = StepHeader("MD", ModernText.L("MODULES", "模块"), item.Display + "  •  research modules");
            layout.Children.Add(header);

            pageHost = new Grid { ClipToBounds = true, Margin = new Thickness(0, 0, 0, 2) };
            Grid.SetRow(pageHost, 1); layout.Children.Add(pageHost);
            modulesPage = BuildModulesPage(settings, out allMods, out modificationList);
            pageHost.Children.Add(modulesPage);

            Grid footer = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            footer.ColumnDefinitions.Add(new ColumnDefinition());
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            footer.Children.Add(new TextBlock { Text = ModernText.L("Settings stay with this vehicle and are saved in presets.", "设置随该载具并保存在预设中。"), Foreground = ModernPalette.Brush(ModernPalette.Muted), VerticalAlignment = VerticalAlignment.Center });
            Button cancel = DialogButton("取消", false); Grid.SetColumn(cancel, 1); footer.Children.Add(cancel);
            Button apply = DialogButton("APPLY SETTINGS", true); Grid.SetColumn(apply, 2); footer.Children.Add(apply);
            cancel.Click += delegate { DialogResult = false; Close(); };
            apply.Click += delegate { Save(); };
            Grid.SetRow(footer, 2); layout.Children.Add(footer);
        }

        private Grid StepHeader(string badge, string title, string subtitle)
        {
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            Border icon = new Border { Width = 44, Height = 44, CornerRadius = new CornerRadius(13), Background = ModernPalette.Brush(ModernPalette.AccentDark), VerticalAlignment = VerticalAlignment.Top };
            icon.Child = new TextBlock { Text = badge, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            grid.Children.Add(icon);
            StackPanel text = new StackPanel { Margin = new Thickness(10, 2, 0, 0) };
            text.Children.Add(Heading(title, 17));
            text.Children.Add(new TextBlock { Text = subtitle, Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 11, Margin = new Thickness(0, 3, 0, 0) });
            Grid.SetColumn(text, 1); grid.Children.Add(text);
            return grid;
        }

        private FrameworkElement BuildModulesPage(AircraftSettings settings, out CheckBox all, out StackPanel list)
        {
            Border shell = new Border
            {
                CornerRadius = new CornerRadius(16),
                Background = ModernPalette.Brush("#B51B2944"),
                BorderBrush = ModernPalette.Brush(ModernPalette.Border),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14),
                Margin = new Thickness(2, 4, 2, 4),
                ClipToBounds = true
            };
            Grid page = new Grid { Background = Brushes.Transparent };
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
            shell.Child = page;

            Border selectionCard = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = ModernPalette.Brush("#9A24324D"),
                BorderBrush = ModernPalette.Brush(ModernPalette.Border),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10)
            };
            StackPanel selectionContent = new StackPanel();
            all = new CheckBox { Content = ModernText.L("Enable all research modifications (current default)", "启用全部研发改造（当前默认）"), IsChecked = settings.UseAllModifications, Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontWeight = FontWeights.SemiBold };
            selectionContent.Children.Add(all);
            selectionContent.Children.Add(new TextBlock { Text = "Turn this off to build a stock or selective vehicle. Alternative weapon groups remain mutually exclusive.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 7, 0, 0) });
            selectionCard.Child = selectionContent;
            page.Children.Add(selectionCard);

            list = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Stretch };
            ScrollViewer scroll = new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(6), ClipToBounds = true };
            Border moduleFrame = new Border
            {
                CornerRadius = new CornerRadius(14),
                Background = ModernPalette.Brush(ModernPalette.Field),
                BorderBrush = ModernPalette.Brush(ModernPalette.Border),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4),
                ClipToBounds = true,
                Child = scroll
            };
            Grid.SetRow(moduleFrame, 1); page.Children.Add(moduleFrame);
            foreach (IGrouping<int, AircraftModification> tier in definitions.GroupBy(x => x.Tier).OrderBy(x => x.Key))
            {
                Grid column = new Grid { VerticalAlignment = VerticalAlignment.Stretch };
                column.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                column.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                List<ModificationChoice> tierChoices = new List<ModificationChoice>();
                string rank = tier.Key <= 0 ? "BASE" : "RANK " + RomanTier(tier.Key);
                TextBlock rankTitle = new TextBlock { Text = rank, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = ModernPalette.Brush(ModernPalette.Cyan), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 9) };
                Grid.SetRow(rankTitle, 0);
                column.Children.Add(rankTitle);
                StackPanel moduleChoices = new StackPanel();
                foreach (AircraftModification definition in tier)
                {
                    CheckBox check = new CheckBox
                    {
                        Content = new TextBlock { Text = definition.Display, TextWrapping = TextWrapping.Wrap, FontSize = 11 },
                        Foreground = ModernPalette.Brush(ModernPalette.Text),
                        Margin = new Thickness(2, 5, 2, 5),
                        IsChecked = !settings.UseAllModifications && settings.EnabledModifications.Contains(definition.Id),
                        Tag = definition,
                        ToolTip = definition.Id
                    };
                    check.Checked += AlternativeChecked;
                    ModificationChoice choice = new ModificationChoice { Definition = definition, Check = check };
                    choices.Add(choice); tierChoices.Add(choice);
                    moduleChoices.Children.Add(check);
                }
                Grid.SetRow(moduleChoices, 1);
                column.Children.Add(moduleChoices);
                Button rankToggle = DialogButton("SELECT RANK", false);
                rankToggle.Height = 34; rankToggle.FontSize = 10; rankToggle.Margin = new Thickness(0, 10, 0, 0); rankToggle.Tag = false; rankToggle.VerticalAlignment = VerticalAlignment.Bottom;
                CheckBox allRanks = all;
                rankToggle.Click += delegate
                {
                    allRanks.IsChecked = false;
                    bool select = !(rankToggle.Tag is bool && (bool)rankToggle.Tag);
                    foreach (ModificationChoice choice in tierChoices) choice.Check.IsChecked = select;
                    rankToggle.Tag = select;
                    rankToggle.Content = select ? "CLEAR RANK" : "SELECT RANK";
                };
                Grid.SetRow(rankToggle, 2);
                column.Children.Add(rankToggle);
                Border tierCard = new Border { Width = 188, Margin = new Thickness(3, 2, 3, 4), Padding = new Thickness(10), CornerRadius = new CornerRadius(12), BorderBrush = ModernPalette.Brush(ModernPalette.Border), BorderThickness = new Thickness(1), Background = ModernPalette.Brush("#8A24324D"), Child = column, ClipToBounds = true, VerticalAlignment = VerticalAlignment.Stretch };
                list.Children.Add(tierCard);
            }
            StackPanel listControl = list;
            CheckBox allControl = all;
            all.Checked += delegate { listControl.IsEnabled = false; foreach (ModificationChoice choice in choices) choice.Check.IsChecked = true; };
            all.Unchecked += delegate { listControl.IsEnabled = true; foreach (ModificationChoice choice in choices) choice.Check.IsChecked = false; };
            list.IsEnabled = all.IsChecked != true;

            Grid controls = new Grid { Margin = new Thickness(0, 9, 0, 0), ClipToBounds = true };
            controls.ColumnDefinitions.Add(new ColumnDefinition());
            controls.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(175) });
            controls.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            controls.Children.Add(new TextBlock { Text = definitions.Count.ToString(CultureInfo.InvariantCulture) + " modules found", Foreground = ModernPalette.Brush(ModernPalette.Cyan), VerticalAlignment = VerticalAlignment.Center });
            Button top = DialogButton("SELECT TOP SET", false); Grid.SetColumn(top, 1); controls.Children.Add(top);
            Button clear = DialogButton(ModernText.L("CLEAR", "清除"), false); Grid.SetColumn(clear, 2); controls.Children.Add(clear);
            top.Click += delegate { SelectTopSet(); };
            clear.Click += delegate { allControl.IsChecked = false; foreach (ModificationChoice choice in choices) choice.Check.IsChecked = false; };
            Grid.SetRow(controls, 2); page.Children.Add(controls);
            return shell;
        }

        internal bool ModulesCardReadyForSelfTest()
        {
            Border shell = modulesPage as Border;
            Grid page = shell == null ? null : shell.Child as Grid;
            Border selection = page == null ? null : page.Children.OfType<Border>().FirstOrDefault();
            List<Border> tierCards = modificationList == null ? new List<Border>() : modificationList.Children.OfType<Border>().ToList();
            bool rankButtonsAnchored = tierCards.Count > 0 && tierCards.All(delegate(Border card)
            {
                Grid layout = card.Child as Grid;
                Button button = layout == null ? null : layout.Children.OfType<Button>().FirstOrDefault();
                return layout != null && layout.RowDefinitions.Count == 3 && layout.RowDefinitions[1].Height.IsStar &&
                    button != null && Grid.GetRow(button) == 2 && button.VerticalAlignment == VerticalAlignment.Bottom;
            });
            return shell != null && shell.CornerRadius.TopLeft >= 16 && shell.Padding.Left >= 14 && shell.Margin.Left > 0 &&
                shell.BorderThickness.Left > 0 && selection != null && selection.CornerRadius.TopLeft >= 12 && selection.Margin.Bottom >= 10 && rankButtonsAnchored;
        }

        private static string RomanTier(int rank)
        {
            string[] values = { "—", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
            return rank >= 0 && rank < values.Length ? values[rank] : rank.ToString(CultureInfo.InvariantCulture);
        }

        private FrameworkElement BuildCountermeasuresPage(AircraftSettings settings, out CheckBox enable, out TextBox flares, out TextBox chaff, out CheckBox unlimited)
        {
            Grid page = CardPage();
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition());
            enable = new CheckBox { Content = ModernText.L("Override countermeasure settings", "覆盖干扰弹设置"), IsChecked = settings.OverrideCountermeasures, Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 18) };
            page.Children.Add(enable);
            flares = LabeledTextBox(page, "FLARES PER INSTALLED LAUNCHER", settings.FlareRounds.ToString(CultureInfo.InvariantCulture), 1);
            chaff = LabeledTextBox(page, "CHAFF PER INSTALLED LAUNCHER", settings.ChaffRounds.ToString(CultureInfo.InvariantCulture), 2);
            unlimited = new CheckBox { IsChecked = false, Visibility = Visibility.Collapsed };
            TextBlock hint = new TextBlock { Text = "BOL, BKO and external dispenser modules still decide which launchers exist. A mixed belt is generated with the requested flare/chaff ratio for every installed launcher.", TextWrapping = TextWrapping.Wrap, Foreground = ModernPalette.Brush(ModernPalette.Muted), Margin = new Thickness(0, 14, 0, 0) };
            Grid.SetRow(hint, 4); page.Children.Add(hint);
            TextBox flareControl = flares;
            TextBox chaffControl = chaff;
            CheckBox enableControl = enable;
            Action update = delegate { flareControl.IsEnabled = chaffControl.IsEnabled = enableControl.IsChecked == true; };
            enable.Checked += delegate { update(); }; enable.Unchecked += delegate { update(); };
            update();
            return page;
        }

        private Grid CardPage()
        {
            return new Grid { Background = Brushes.Transparent, Margin = new Thickness(14), ClipToBounds = true };
        }

        private TextBox LabeledTextBox(Grid page, string label, string value, int row)
        {
            StackPanel stack = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            stack.Children.Add(Caption(label));
            TextBox box = new TextBox { Text = value, Margin = new Thickness(0, 6, 0, 0), Height = 38 };
            stack.Children.Add(box); Grid.SetRow(stack, row); page.Children.Add(stack); return box;
        }

        private void AlternativeChecked(object sender, RoutedEventArgs e)
        {
            CheckBox selected = sender as CheckBox;
            AircraftModification definition = selected == null ? null : selected.Tag as AircraftModification;
            if (definition == null || String.IsNullOrWhiteSpace(definition.Group)) return;
            foreach (ModificationChoice choice in choices)
                if (!Object.ReferenceEquals(choice.Check, selected) && String.Equals(choice.Definition.Group, definition.Group, StringComparison.OrdinalIgnoreCase)) choice.Check.IsChecked = false;
        }

        private void SelectTopSet()
        {
            allMods.IsChecked = false;
            foreach (ModificationChoice choice in choices) choice.Check.IsChecked = false;
            HashSet<string> groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ModificationChoice choice in choices.OrderByDescending(x => x.Definition.Tier))
                if (String.IsNullOrWhiteSpace(choice.Definition.Group) || groups.Add(choice.Definition.Group)) choice.Check.IsChecked = true;
        }

        private void Save()
        {
            AircraftSettings result = originalSettings.Copy();
            result.UseAllModifications = allMods.IsChecked == true;
            result.EnabledModifications.Clear();
            if (!result.UseAllModifications)
                foreach (ModificationChoice choice in choices.Where(x => x.Check.IsChecked == true)) result.EnabledModifications.Add(choice.Definition.Id);
            if (!result.UseAllModifications && result.GunBeltSelections.Count > 0)
            {
                foreach (KeyValuePair<int, string> belt in result.GunBeltSelections.ToList())
                {
                    string family = ModernFlightConfigureWindow.GunBeltFamily(belt.Value);
                    List<AircraftModification> packs = definitions.Where(x => x.Id.IndexOf("belt_pack", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        ModernFlightConfigureWindow.RelatedBeltPack(family, x.Id)).ToList();
                    if (packs.Count > 0 && !packs.Any(x => result.EnabledModifications.Contains(x.Id))) result.GunBeltSelections.Remove(belt.Key);
                }
            }
            Result = result;
            DialogResult = true;
            Close();
        }
    }

    internal sealed class ModernPresetWindow : ModernDialogWindow
    {
        private readonly MainForm controller;
        private readonly ModernMainWindow main;
        private readonly Aircraft presetVehicle;
        private readonly TextBox nameBox;
        private readonly ComboBox sightBox;
        private readonly ListBox list;
        private readonly List<SavedPreset> presets;

        public ModernPresetWindow(MainForm source, ModernMainWindow owner) : base(ModernText.L("Custom Presets", "自定义预设"), 760, 560)
        {
            controller = source;
            main = owner;
            presetVehicle = controller.WorkspaceSelectedAircraft;
            presets = PresetStore.Load();
            bool groundPreset = presetVehicle != null && String.Equals(presetVehicle.Kind, "Ground Vehicle", StringComparison.OrdinalIgnoreCase);
            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(groundPreset ? 74 : 0) });
            layout.RowDefinitions.Add(new RowDefinition());
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(54) });
            ContentCard.Child = layout;
            StackPanel heading = new StackPanel();
            heading.Children.Add(Heading("自定义弹药预设", 18));
            heading.Children.Add(new TextBlock { Text = ModernText.L("Save or restore the vehicle, pylons, Modules and configuration settings.", "保存或恢复载具、挂架、模块和配置设置。"), Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 11, Margin = new Thickness(0, 4, 0, 0) });
            layout.Children.Add(heading);
            StackPanel name = new StackPanel(); name.Children.Add(Caption("预设名称"));
            nameBox = new TextBox { Text = controller.WorkspaceSelectedAircraft == null ? "" : controller.WorkspaceSelectedAircraft.Display, Margin = new Thickness(0, 6, 0, 0) }; name.Children.Add(nameBox);
            Grid.SetRow(name, 1); layout.Children.Add(name);
            if (groundPreset)
            {
                StackPanel sight = new StackPanel();
                List<UserSightEntry> sights = UserSightStore.Discover(controller.WorkspaceGameFolder);
                sight.Children.Add(Caption("地面用户瞄准镜 — " + Math.Max(0, sights.Count - 1).ToString(CultureInfo.InvariantCulture) + ModernText.L(" FOUND • SAVED WITH THIS PRESET", " 已找到 \u2022 随此预设保存")));
                sightBox = new ComboBox { ItemsSource = sights, Margin = new Thickness(0, 6, 0, 0) };
                AircraftSettings settings = controller.WorkspaceGetSettings(presetVehicle);
                sightBox.SelectedItem = sights.FirstOrDefault(x => String.Equals(x.FilePath ?? "", settings.UserSightPath ?? "", StringComparison.OrdinalIgnoreCase)) ?? sights.First();
                sight.Children.Add(sightBox);
                Grid.SetRow(sight, 2); layout.Children.Add(sight);
            }
            list = new ListBox { ItemsSource = presets.OrderBy(x => x.Name).ToList(), Margin = new Thickness(0, 10, 0, 10) };
            Grid.SetRow(list, 3); layout.Children.Add(list);
            list.MouseDoubleClick += delegate { LoadSelected(); };
            Grid buttons = new Grid();
            for (int i = 0; i < 4; i++) buttons.ColumnDefinitions.Add(new ColumnDefinition());
            Button save = DialogButton("保存当前", true); buttons.Children.Add(save);
            Button load = DialogButton(ModernText.L("LOAD SELECTED", "加载所选"), false); Grid.SetColumn(load, 1); buttons.Children.Add(load);
            Button delete = DialogButton("删除", false); Grid.SetColumn(delete, 2); buttons.Children.Add(delete);
            Button close = DialogButton("关闭", false); Grid.SetColumn(close, 3); buttons.Children.Add(close);
            save.Click += delegate { SaveCurrent(); }; load.Click += delegate { LoadSelected(); }; delete.Click += delegate { DeleteSelected(); }; close.Click += delegate { DialogResult = false; Close(); };
            Grid.SetRow(buttons, 4); layout.Children.Add(buttons);
        }

        internal void SelectFirstCustomSightForScreenshot()
        {
            if (sightBox != null && sightBox.Items.Count > 1) sightBox.SelectedIndex = 1;
        }

        private SavedPreset Selected { get { return list.SelectedItem as SavedPreset; } }

        private void RefreshList()
        {
            list.ItemsSource = null;
            list.ItemsSource = presets.OrderBy(x => x.Name).ToList();
        }

        private void SaveCurrent()
        {
            string name = (nameBox.Text ?? "").Trim();
            if (String.IsNullOrEmpty(name))
            {
                ModernMessageDialog warning = new ModernMessageDialog(ModernText.L("Presets", "预设"), ModernText.L("Enter a preset name.", "请输入预设名称。"), "确定", null, true) { Owner = Owner };
                warning.ShowDialog();
                return;
            }
            SavedPreset existing = presets.FirstOrDefault(x => x.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
            if (existing != null)
            {
                ModernMessageDialog confirm = new ModernMessageDialog(ModernText.L("Replace Preset", "替换预设"), ModernText.L("Replace the existing preset '", "替换现有预设 '") + existing.Name + "'?", "替换", "取消", false) { Owner = Owner };
                if (confirm.ShowDialog() != true) return;
            }
            if (existing != null) presets.Remove(existing);
            ApplyUserSightSelection();
            presets.Add(controller.CaptureCurrentPreset(name));
            PresetStore.Save(presets);
            RefreshList();
            main.RefreshFromController();
        }

        private void ApplyUserSightSelection()
        {
            if (presetVehicle == null || sightBox == null) return;
            UserSightEntry sight = sightBox.SelectedItem as UserSightEntry;
            AircraftSettings settings = controller.WorkspaceGetSettings(presetVehicle);
            settings.UserSightPath = sight == null || sight.IsDefault ? "" : sight.FilePath;
            controller.WorkspaceSetSettings(presetVehicle, settings);
        }

        private void LoadSelected()
        {
            if (Selected == null) return;
            controller.LoadSavedPreset(Selected);
            DialogResult = true;
            Close();
        }

        private void DeleteSelected()
        {
            SavedPreset selected = Selected;
            if (selected == null) return;
            ModernMessageDialog confirm = new ModernMessageDialog(ModernText.L("Delete Preset", "删除预设"), ModernText.L("Delete preset '", "删除预设 '") + selected.Name + "'?", "删除", "取消", true) { Owner = Owner };
            if (confirm.ShowDialog() != true) return;
            presets.Remove(selected); PresetStore.Save(presets); RefreshList();
        }
    }

    internal sealed class ModernAboutWindow : ModernDialogWindow
    {
        private const string ProjectUrl = "https://github.com/VanillaWong/Universal-Test-Lab-Vanilla-Version";

        public ModernAboutWindow(int aircraftCount, int weaponCount) : base(ModernText.L("Support Universal Test Lab", "支持通用测试实验室"), 900, 670)
        {
            ResizeMode = ResizeMode.NoResize;
            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(72) });
            layout.RowDefinitions.Add(new RowDefinition());
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(62) });
            ContentCard.Child = layout;
            StackPanel header = new StackPanel();
            header.Children.Add(Heading("UNIVERSAL TEST LAB", 24));
            header.Children.Add(new TextBlock { Text = ModernText.L("Public beta  •  community-inspired mission and vehicle test workspace for War Thunder", "公开测试版  \u2022  社区启发的战雷任务与载具测试工作区"), Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 12, Margin = new Thickness(0, 4, 0, 0) });
            layout.Children.Add(header);
            Grid content = new Grid(); content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) }); content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            StackPanel info = new StackPanel { Margin = new Thickness(0, 6, 20, 0) };
            info.Children.Add(Heading("项目", 14));
            info.Children.Add(new TextBlock { Text = "Build experimental vehicles, modules, ammunition, loadouts and reusable test missions from one workspace.", TextWrapping = TextWrapping.Wrap, Foreground = ModernPalette.Brush(ModernPalette.Muted), Margin = new Thickness(0, 4, 0, 5) });
            info.Children.Add(new TextBlock { Text = aircraftCount.ToString("N0", CultureInfo.InvariantCulture) + " playable vehicle entries  •  " + weaponCount.ToString("N0", CultureInfo.InvariantCulture) + " air-weapon entries", Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 0, 0, 15) });
            info.Children.Add(Heading("社区灵感", 14));
            info.Children.Add(new TextBlock { Text = "Originally created by AstraSEP; now maintained by VanillaWong. Inspired by GUI and custom-mission concepts shared by community creators and YouTube channels, for example Ask3lad. They are not project contributors.", TextWrapping = TextWrapping.Wrap, Foreground = ModernPalette.Brush(ModernPalette.Muted), FontSize = 13.5, LineHeight = 20, Margin = new Thickness(0, 0, 0, 14) });
            info.Children.Add(Heading("维护者 / MAINTAINER", 14));
            info.Children.Add(new TextBlock { Text = ModernText.L("Independent fan-made software shaped by community testing and feedback.", "由社区测试与反馈打磨的独立粉丝自制软件。"), TextWrapping = TextWrapping.Wrap, Foreground = ModernPalette.Brush(ModernPalette.Muted), Margin = new Thickness(0, 4, 0, 8) });
            info.Children.Add(Heading("开源", 14));
            info.Children.Add(new TextBlock { Text = "Source, issue tracking and contribution information are available on GitHub. The bundled wt_ext_cli component retains its Apache 2.0 license.", TextWrapping = TextWrapping.Wrap, Foreground = ModernPalette.Brush(ModernPalette.Muted), Margin = new Thickness(0, 4, 0, 10) });
            Button github = DialogButton("在 GitHub 打开项目", false); github.Margin = new Thickness(0, 0, 0, 4); github.Click += delegate { OpenUrl(ProjectUrl); }; info.Children.Add(github);
            content.Children.Add(info);
            Grid.SetRow(content, 1); layout.Children.Add(content);
            Button close = DialogButton("关闭", false); close.Width = 150; close.HorizontalAlignment = HorizontalAlignment.Right; close.Margin = new Thickness(0, 10, 0, 0); close.Click += delegate { Close(); }; Grid.SetRow(close, 2); layout.Children.Add(close);
        }

        private static BitmapImage LoadImage(byte[] bytes)
        {
            BitmapImage image = new BitmapImage();
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = stream; image.EndInit(); image.Freeze();
            }
            return image;
        }

        private void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
            catch (Exception ex)
            {
                ModernMessageDialog error = new ModernMessageDialog("Universal Test Lab", "Could not open the link.\n\n" + url + "\n\n" + ex.Message, "关闭", null, true) { Owner = Owner };
                error.ShowDialog();
            }
        }
    }

    internal static class ModernUi
    {
        public static void Run()
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;
            app.Run(new ModernMainWindow());
        }

        public static void RenderMain(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderMainMaximized(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow { Width = 1920, Height = 1040, WindowStartupLocation = WindowStartupLocation.Manual, Left = 0, Top = 0 };
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderExperimental(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.SelectWorkspaceTabForScreenshot(4);
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderTargets(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.SelectWorkspaceTabForScreenshot(1);
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderGarage(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.SelectWorkspaceTabForScreenshot(3);
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderOptions(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.SelectWorkspaceTabForScreenshot(2);
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderMainKind(string path, string kind)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.SelectVehicleKindForScreenshot(kind);
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderGroundPreset(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.ShowGroundPresetForScreenshot();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderMessage(string path, bool danger)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.ShowMessageForScreenshot(danger);
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderWeaponScrollbar(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.EnableInjectionForScreenshot();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderSettings(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            Aircraft sample = new Aircraft { Id = "ef_2000_aesa", Display = "EF-2000 Typhoon (AESA)", Kind = "Aircraft", Nation = "Great Britain", Rank = 9 };
            List<AircraftModification> sampleMods = new List<AircraftModification>();
            foreach (ModificationRowJson r in MainForm.JsonRows<ModificationRowJson>("UTL.modifications.json"))
            {
                if (r != null && r.aircraftId != null && r.aircraftId.Equals(sample.Id, StringComparison.OrdinalIgnoreCase) && !String.IsNullOrWhiteSpace(r.id))
                    sampleMods.Add(new AircraftModification { AircraftId = r.aircraftId, Id = r.id, Display = r.display, Tier = r.tier, ModClass = r.modClass, Group = r.group, Requires = r.requires });
            }
            ModernFlightSystemsWindow window = new ModernFlightSystemsWindow(sample, sampleMods, new AircraftSettings(), false);
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = 0;
            window.Top = 0;
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderGroundConfigure(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            Aircraft sample = new Aircraft
            {
                Id = "us_m1a2_sep2_abrams", Display = "M1A2 SEP V2", Kind = "Ground Vehicle", Nation = "USA", Rank = 8,
                MainWeaponBlk = "gameData/Weapons/groundModels_weapons/120mm_M256_M1A3_user_cannon.blk", MaxAmmo = 42,
                NativeMass = 54000, NativeEnginePower = 1519, NativeForwardSpeed = 75, NativeReverseSpeed = 10, NativeReloadSeconds = 5, NativeRecoil = 0.5
            };
            List<GroundAmmo> ammo = new List<GroundAmmo>();
            string groundAmmoJsonText = Embedded.Text("UTL.ground_ammo.json");
            if (!String.IsNullOrWhiteSpace(groundAmmoJsonText))
            {
                try
                {
                    System.Web.Script.Serialization.JavaScriptSerializer gaSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    gaSerializer.MaxJsonLength = int.MaxValue;
                    List<GroundAmmoJson> ammoList = gaSerializer.Deserialize<List<GroundAmmoJson>>(groundAmmoJsonText);
                    if (ammoList != null)
                    {
                        foreach (GroundAmmoJson ga in ammoList)
                        {
                            if (ga == null || String.IsNullOrWhiteSpace(ga.source)) continue;
                            if (!ga.source.Equals(sample.MainWeaponBlk, StringComparison.OrdinalIgnoreCase)) continue;
                            ammo.Add(new GroundAmmo { SourceBlk = ga.source, Container = ga.container ?? "", BulletName = ga.bulletName, Display = ga.display ?? "", Type = ga.kind ?? "", Mass = ga.mass, Speed = ga.speed, ExplosiveMass = ga.explosive, Caliber = ga.caliber, Penetration = ga.penetration });
                        }
                    }
                }
                catch { }
            }
            AircraftSettings settings = new AircraftSettings();
            if (ammo.Count > 0) settings.GroundAmmoLoadouts.Add(new GroundAmmoLoadout { Slot = 0, Count = 31, SourceBlk = ammo[0].SourceBlk, BulletName = ammo[0].BulletName });
            if (ammo.Count > 1) settings.GroundAmmoLoadouts.Add(new GroundAmmoLoadout { Slot = 1, Count = 9, SourceBlk = ammo[1].SourceBlk, BulletName = ammo[1].BulletName });
            ModernGroundConfigureWindow window = new ModernGroundConfigureWindow(sample, settings, ammo, new TargetUnit[0], new UnitWeapon[0], new GroundWeaponInfo[0], new GroundAmmo[0], new GroundWeaponBeltOption[0], null);
            window.WindowStartupLocation = WindowStartupLocation.Manual; window.Left = 0; window.Top = 0; window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path); window.Close(); app.Shutdown();
        }

        public static void RenderFlightConfigure(string path) { RenderFlightConfigure(path, false); }

        public static void RenderFlightConfigureBottom(string path) { RenderFlightConfigure(path, true); }

        private static void RenderFlightConfigure(string path, bool bottom)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            Aircraft sample = new Aircraft { Id = "ef_2000_aesa", Display = "EF-2000 Typhoon (AESA)", Kind = "Aircraft", Nation = "Great Britain", Rank = 9 };
            List<CountermeasureLauncher> launchers = new List<CountermeasureLauncher>
            {
                new CountermeasureLauncher { Key = "emtr_flare1", Display = "INTERNAL COUNTERMEASURE DISPENSER", NativeRounds = 32, AllowsFlares = true, AllowsChaff = true },
                new CountermeasureLauncher { Key = "emtr_flare3", Display = "BOL COUNTERMEASURE DISPENSER", NativeRounds = 160, AllowsFlares = true, AllowsChaff = false }
            };
            List<AircraftModification> beltMods = new List<AircraftModification>
            {
                new AircraftModification { AircraftId = sample.Id, Id = "bk_27_air_targets", Display = "Air targets", Tier = 0 },
                new AircraftModification { AircraftId = sample.Id, Id = "bk_27_ground_targets", Display = "Ground targets", Tier = 0 },
                new AircraftModification { AircraftId = sample.Id, Id = "bk_27_stealth", Display = "Stealth", Tier = 0 },
                new AircraftModification { AircraftId = sample.Id, Id = "bk_27_belt_pack", Display = "BK 27 Belt Pack", Tier = 1 }
            };
            ModernFlightConfigureWindow window = new ModernFlightConfigureWindow(sample, new AircraftSettings { FullFuel = false, FuelMinutes = 30, OverrideCountermeasures = true }, launchers, beltMods);
            window.WindowStartupLocation = WindowStartupLocation.Manual; window.Left = 0; window.Top = 0; window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            if (bottom) { window.ScrollToEndForScreenshot(); window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle); }
            RenderWindow(window, path); window.Close(); app.Shutdown();
        }

        public static void RenderMap(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            AircraftView air = new AircraftView(new Aircraft { Id = "j_10c", Display = "J-10C", Nation = "China", Kind = "Aircraft", Rank = 9 });
            TargetView ground = new TargetView(new TargetUnit { Id = "ussr_bmpt", Display = "BMPT" });
            TargetView ship = new TargetView(new TargetUnit { Id = "jp_battleship_yamato", Display = "Yamato-class, IJN Yamato, 1945" });
            CombinedMap map = new CombinedMap { Id = "western_europe", Display = "Western Europe", Level = "levels/avg_western_europe.bin" };
            map.Spawns.Add(new CombinedSpawn { Kind = "aircraft", Side = 1, Option = "airfield", Label = "Airfield" });
            map.Spawns.Add(new CombinedSpawn { Kind = "aircraft", Side = 1, Option = "air", Label = "Air spawn" });
            ModernMapWindow window = new ModernMapWindow(new[] { air }, new[] { ground }, new[] { ship }, air, 1, new[] { ground }, true, "active", "all", ship, 1, false,
                new[] { map }, "aircraft", new CombinedScenarioSettings { Enabled = true, MapId = map.Id, Side = 1, SpawnOption = "airfield" }, null, 1, null, 1, null, 1);
            window.WindowStartupLocation = WindowStartupLocation.Manual; window.Left = 0; window.Top = 0; window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path); window.Close(); app.Shutdown();
        }

        public static void RenderGenerated(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMissionGeneratedWindow window = new ModernMissionGeneratedWindow(); window.WindowStartupLocation = WindowStartupLocation.Manual; window.Left = 0; window.Top = 0; window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path); window.Close(); app.Shutdown();
        }

        public static void RenderAbout(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            ModernAboutWindow about = new ModernAboutWindow(2817, 1838) { Owner = window };
            window.ShowOverlay(about);
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            about.Close();
            window.Close();
            app.Shutdown();
        }

        public static void SelfTest()
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.ExerciseDropdownForSelfTest();
            if (!window.LayoutFixesReadyForSelfTest())
                throw new InvalidOperationException("WPF clipping/dropdown/work-area self-test failed.");
            if (!window.CombinedCatalogReadyForSelfTest())
                throw new InvalidOperationException("WPF combined-battles map catalog self-test failed.");
            if (!window.ExerciseOverlayForSelfTest())
                throw new InvalidOperationException("WPF single-window overlay self-test failed.");

            Aircraft modulesVehicle = new Aircraft { Id = "selftest_modules", Display = "Self-test Vehicle", Kind = "Aircraft", Nation = "USA", Rank = 1 };
            ModernFlightSystemsWindow modules = new ModernFlightSystemsWindow(modulesVehicle,
                new[] { new AircraftModification { AircraftId = modulesVehicle.Id, Id = "engine", Display = "Engine", Tier = 1 } },
                new AircraftSettings(), false);
            if (!modules.ModulesCardReadyForSelfTest())
                throw new InvalidOperationException("WPF Modules glass-card self-test failed.");

            Aircraft groundVehicle = new Aircraft { Id = "selftest_ground", Display = "M1A2 SEP V3", Kind = "Ground Vehicle", Nation = "USA", Rank = 8, MaxAmmo = 42, MainWeaponBlk = "selftest.blk", NativeReloadSeconds = 5 };
            GroundAmmo groundRound = new GroundAmmo { SourceBlk = "selftest.blk", BulletName = "round", Display = "Test Round", Type = "APFSDS", Mass = 5, Speed = 1500 };
            AircraftSettings groundSettings = new AircraftSettings();
            groundSettings.GroundAmmoLoadouts.Add(new GroundAmmoLoadout { Slot = 0, Count = 31, SourceBlk = groundRound.SourceBlk, BulletName = groundRound.BulletName });
            groundSettings.GroundAmmoLoadouts.Add(new GroundAmmoLoadout { Slot = 1, Count = 9, SourceBlk = groundRound.SourceBlk, BulletName = groundRound.BulletName });
            ModernGroundConfigureWindow groundConfigure = new ModernGroundConfigureWindow(groundVehicle, groundSettings, new[] { groundRound }, new TargetUnit[0], new UnitWeapon[0], new GroundWeaponInfo[0], new GroundAmmo[0], new GroundWeaponBeltOption[0], null);
            if (!groundConfigure.AmmoSlidersStableForSelfTest())
                throw new InvalidOperationException("WPF ground-ammunition slider self-test failed.");

            window.WindowState = WindowState.Maximized;
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            if (window.WindowState != WindowState.Maximized || window.ActualWidth < 1100 || window.ActualHeight < 600)
                throw new InvalidOperationException("WPF maximize/layout self-test failed.");
            window.WindowState = WindowState.Normal;
            window.Width = 1500;
            window.Height = 920;
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            if (window.WindowState != WindowState.Normal || window.ActualWidth < 1200 || window.ActualHeight < 640)
                throw new InvalidOperationException("WPF restore/layout self-test failed.");
            ModernAboutWindow about = new ModernAboutWindow(2817, 1838);
            about.Close();
            window.Close();
            app.Shutdown();
            Console.WriteLine("UISELFTEST OK wpf=yes custom-chrome=yes no-client-gap=yes dark-glass=yes dark-dropdowns=yes grouped-weapons=yes rounded-preview=yes vehicle-kind-previews=yes border-retention=yes weapon-table-fit=yes station-order=yes stations-one-row=yes vertical-scroll=yes single-window-overlays=yes styled-messages=yes solid-close=yes visible-game-path=yes blurred-background=yes work-area-fit=yes maximize-restore=yes dpi-aware=yes");
        }

        internal static void RenderWindow(Window window, string path)
        {
            int width = Math.Max(1, (int)Math.Round(window.ActualWidth));
            int height = Math.Max(1, (int)Math.Round(window.ActualHeight));
            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (FileStream stream = File.Create(path)) encoder.Save(stream);
        }

        internal static void RenderWindow(ModernDialogWindow dialog, string path)
        {
            int width = Math.Max(1, (int)Math.Round(dialog.ActualWidth));
            int height = Math.Max(1, (int)Math.Round(dialog.ActualHeight));
            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(dialog);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (FileStream stream = File.Create(path)) encoder.Save(stream);
        }
    }
}