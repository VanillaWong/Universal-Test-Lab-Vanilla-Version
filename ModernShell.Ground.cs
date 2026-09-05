// ModernShell.Ground.cs
// Ground vehicle configuration panel and window with ammo/module editors.
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
            StackPanel cannonPage = new StackPanel(), radarPage = new StackPanel(), ammoPage = new StackPanel(), tuningPage = new StackPanel();
            StackPanel[] labPages = new StackPanel[] { cannonPage, radarPage, ammoPage, tuningPage };
            StackPanel labHeader = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            ToggleButton[] labTabs = new ToggleButton[labPages.Length];
            string[] labTitles =
            {
                ModernText.L("CANNON INJECT", "换炮注入"),
                ModernText.L("RADAR SWAP", "雷达替换"),
                ModernText.L("AMMO SWITCHES", "弹药开关"),
                ModernText.L("VEHICLE TUNING", "载具数值")
            };
            for (int lab = 0; lab < labPages.Length; lab++)
            {
                int page = lab;
                ToggleButton tab = new ToggleButton { Content = labTitles[lab], Style = toggleStyle, Margin = new Thickness(0, 0, 8, 0) };
                tab.Checked += delegate
                {
                    for (int t = 0; t < labTabs.Length; t++) if (t != page && labTabs[t] != null) labTabs[t].IsChecked = false;
                    for (int pi = 0; pi < labPages.Length; pi++) labPages[pi].Visibility = pi == page ? Visibility.Visible : Visibility.Collapsed;
                };
                labTabs[lab] = tab;
                labHeader.Children.Add(tab);
            }
            Button resetMods = new Button { Content = ModernText.L("RESET ALL MODS", "清空全部爆改"), Style = buttonStyle, Padding = new Thickness(14, 2, 14, 2), Margin = new Thickness(10, 10, 0, 4) };
            resetMods.Click += delegate
            {
                // ---- 1. 清 UI 工作副本（original——炮选择器/数值的显示源头）----
                if (original != null)
                {
                    // 换炮
                    original.InjectedCannonBlk = null;
                    original.InjectedCannonDomain = null;
                    original.InjectedCannonUnit = null;
                    original.InjectedCannonRound = null;
                    original.InjectedCannonRounds = 0;
                    original.InjectNativeLauncher = false;
                    // 弹药开关
                    original.UnlimitedAmmo = false;
                    original.FakeArhConversion = false;
                    // 雷达
                    original.RadarSearchBlk = null;
                    original.RadarTrackBlk = null;
                    original.RadarStripAiSensors = false;
                    // 数值倍率
                    original.OverrideGroundBallistics = false;
                    original.ProjectileMassMultiplier = 1;
                    original.MuzzleVelocityMultiplier = 1;
                    original.ExplosiveMassMultiplier = 1;
                    original.PenetrationMultiplier = 1;
                    original.ReloadSeconds = 0;
                    original.RecoilMultiplier = 1;
                    original.EnginePowerMultiplier = 1;
                    original.VehicleMassMultiplier = 1;
                    original.ForwardSpeedMultiplier = 1;
                    original.ReverseSpeedMultiplier = 1;
                    // 弹药槽
                    original.GroundAmmoLoadouts.Clear();
                }
                // 弹药槽 UI 源（4 个槽位的挂载显示）
                loadouts.Clear();

                if (currentSettings != null)
                {
                    // 换炮
                    currentSettings.InjectedCannonBlk = null;
                    currentSettings.InjectedCannonDomain = null;
                    currentSettings.InjectedCannonUnit = null;
                    currentSettings.InjectedCannonRound = null;
                    currentSettings.InjectedCannonRounds = 0;
                    currentSettings.InjectNativeLauncher = false;
                    // 弹药开关
                    currentSettings.UnlimitedAmmo = false;
                    currentSettings.FakeArhConversion = false;
                    // 雷达
                    currentSettings.RadarSearchBlk = null;
                    currentSettings.RadarTrackBlk = null;
                    currentSettings.RadarStripAiSensors = false;
                    // 数值倍率
                    currentSettings.OverrideGroundBallistics = false;
                    currentSettings.ProjectileMassMultiplier = 1;
                    currentSettings.MuzzleVelocityMultiplier = 1;
                    currentSettings.ExplosiveMassMultiplier = 1;
                    currentSettings.PenetrationMultiplier = 1;
                    currentSettings.ReloadSeconds = 0;
                    currentSettings.RecoilMultiplier = 1;
                    currentSettings.EnginePowerMultiplier = 1;
                    currentSettings.VehicleMassMultiplier = 1;
                    currentSettings.ForwardSpeedMultiplier = 1;
                    currentSettings.ReverseSpeedMultiplier = 1;
                    // 弹药槽
                    currentSettings.GroundAmmoLoadouts.Clear();
                }
                ResetAllValues();
                overrideBallistics.IsChecked = false;
                if (ammoUnlimitedBox != null) ammoUnlimitedBox.IsChecked = false;
                if (fakeArhBox != null) fakeArhBox.IsChecked = false;
                radarSearchSel = null; radarTrackSel = null;
                if (stripAiBox != null) stripAiBox.IsChecked = false;
                UpdateRadarStatus();
                SelectInitialCannon(); RefreshAmmo(); RefreshSlotEditors();
            };
            labHeader.Children.Add(resetMods);
            StackPanel labBody = new StackPanel();
            foreach (StackPanel pagePanel in labPages) labBody.Children.Add(pagePanel);
            labTabs[0].IsChecked = true;
            cannonPage.Children.Add(Heading("CROSS-DOMAIN CANNON", 15));
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
            cannonPage.Children.Add(domainRow);
            cannonPage.Children.Add(unitBox);
            cannonPage.Children.Add(cannonBox);
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
            cannonPage.Children.Add(roundBox);
            bool roundsSyncing = true;
            StackPanel roundsRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
            roundsRow.Children.Add(new TextBlock { Text = ModernText.L("ROUNDS PER RELOAD (0 = source)", "每次装填弹数（0 = 沿用原值）"), Foreground = ModernPalette.Brush(ModernPalette.Muted), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            TextBox roundsBox = new TextBox { Text = original.InjectedCannonRounds > 0 ? original.InjectedCannonRounds.ToString(CultureInfo.InvariantCulture) : "0", Width = 64, Height = 26, Padding = new Thickness(6, 2, 6, 2), VerticalContentAlignment = VerticalAlignment.Center, ToolTip = "Osa + S-300: 6 fills the native 6-rail rack (S-300 source carries 4)." };
            roundsRow.Children.Add(roundsBox); cannonPage.Children.Add(roundsRow);
            roundsBox.TextChanged += delegate
            {
                if (roundsSyncing) return;
                int v;
                if (int.TryParse(roundsBox.Text.Trim(), out v) && v >= 0 && v <= 999) { original.InjectedCannonRounds = v; if (currentSettings != null) currentSettings.InjectedCannonRounds = v; }
            };
            roundsSyncing = false;
            bool injectSyncing = true;
            CheckBox injectBox = new CheckBox { Content = ModernText.L("Inject into native launcher (inject-shell)", "注入原生发射器（inject-shell）"), IsChecked = original.InjectNativeLauncher, Foreground = ModernPalette.Brush(ModernPalette.Text), Margin = new Thickness(0, 2, 0, 0), ToolTip = "S-75 V-759 style: mounts the chosen missile into the vehicle's own launcher mechanism instead of swapping the whole cannon file (needed for AI site missiles)." };
            injectBox.Checked += delegate { if (!injectSyncing) { original.InjectNativeLauncher = true; if (currentSettings != null) currentSettings.InjectNativeLauncher = true; } };
            injectBox.Unchecked += delegate { if (!injectSyncing) { original.InjectNativeLauncher = false; if (currentSettings != null) currentSettings.InjectNativeLauncher = false; } };
            cannonPage.Children.Add(injectBox);
            injectSyncing = false;
            ammoUnlimitedBox = new CheckBox { Content = ModernText.L("Unlimited ammunition (9999 per slot)", "无限弹药（每槽 9999）"), IsChecked = original.UnlimitedAmmo, Foreground = ModernPalette.Brush(ModernPalette.Text), Margin = new Thickness(0, 6, 0, 0) };
            ammoPage.Children.Add(ammoUnlimitedBox);
            fakeArhBox = new CheckBox { Content = ModernText.L("Fake-ARH conversion (SARH missiles self-guide, TWS launch)", "伪ARH转换（半主动弹自主制导，TWS直射）"), IsChecked = original.FakeArhConversion, Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 6, 0, 0), ToolTip = "Injects active seeker + permanently-activated guidance into radar missiles so they launch without a pre-launch lock (SARH -> ARH). Verified on AIM-7E-2: active:b, permanentlyActivated, lockDistance, inertialNavigation+datalink, breakLockMaxTime=160, wider seeker angles, distGate, shotFreq cap." };
            ammoPage.Children.Add(fakeArhBox);
            radarStatus = new TextBlock { Foreground = ModernPalette.Brush(ModernPalette.Muted), FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) };
            radarSearchSel = original.RadarSearchBlk; radarTrackSel = original.RadarTrackBlk;
            stripAiBox = new CheckBox { Content = ModernText.L("Radar swap strips the AI-only radar pair", "雷达替换时移除 AI 专用雷达组"), IsChecked = original.RadarStripAiSensors, Margin = new Thickness(0, 1, 0, 0) };
            Button radarPick = new Button { Content = ModernText.L("CHANGE RADARS (SEARCH / TRACK)", "更换雷达（搜索 / 跟踪）"), Style = buttonStyle, Padding = new Thickness(14, 4, 14, 4), Margin = new Thickness(0, 3, 0, 1), HorizontalAlignment = HorizontalAlignment.Left };
            radarPick.Click += delegate { PickRadars(); };
            Button radarReset = new Button { Content = ModernText.L("RESET RADARS TO NATIVE", "恢复原生雷达"), Style = buttonStyle, Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(8, 3, 0, 1), HorizontalAlignment = HorizontalAlignment.Left };
            radarReset.Click += delegate { radarSearchSel = null; radarTrackSel = null; if (currentSettings != null) { currentSettings.RadarSearchBlk = null; currentSettings.RadarTrackBlk = null; } UpdateRadarStatus(); };
            radarPage.Children.Add(Heading("RADAR & SENSOR SWAP", 15));
            StackPanel radarRow = new StackPanel { Orientation = Orientation.Horizontal }; radarRow.Children.Add(radarPick); radarRow.Children.Add(radarReset); radarPage.Children.Add(radarRow);
            // Swap is meaningless on vehicles without any native sensor structure - disable.
            if (nativeSearchSensor == null && nativeTrackSensor == null)
            {
                radarPick.IsEnabled = false;
                radarPick.ToolTip = ModernText.L("This vehicle has no radar at all - installing one needs a sensor structure first.", "此车完全没有雷达——更换不可用（需先有传感器结构）。");
            }
            radarPage.Children.Add(stripAiBox);
            radarPage.Children.Add(radarStatus);
            Border radarCard = new Border { CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1), BorderBrush = ModernPalette.Brush(ModernPalette.Border), Background = ModernPalette.Brush("#8A24324D"), Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(0, 8, 0, 4) };
            StackPanel radarCardStack = new StackPanel();
            radarCardStack.Children.Add(new TextBlock { Text = ModernText.L("RADAR DETAILS", "雷达详情"), Foreground = ModernPalette.Brush(ModernPalette.Cyan), FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            radarCardStack.Children.Add(radarDetailSearch);
            radarCardStack.Children.Add(radarDetailTrack);
            radarCard.Child = radarCardStack;
            radarPage.Children.Add(radarCard);
            UpdateRadarStatus();
            domainBox.SelectedItem = savedDomainItem;
            RefreshCannonBox();
            BuildCannonSelector();
            SelectInitialCannon();
            cannonPage.Children.Add(new TextBlock { Text = "Pick the source unit (e.g. Yamato), then its weapon (460/155/127 mm). Ground, naval and air units are all supported; air also includes missiles and rockets. Ammunition slots and projectile tuning below then apply to the injected weapon.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) });
            cannonPage.Children.Add(new Border { Height = 1, Background = ModernPalette.Brush(ModernPalette.Border), Margin = new Thickness(0, 10, 0, 10) });
            tuningPage.Children.Add(Heading("REAL VEHICLE VALUES", 15));

            overrideBallistics = new CheckBox { Content = ModernText.L("Override native values", "覆盖原生数值"), IsChecked = original.OverrideGroundBallistics, Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 12, 0, 7) }; tuningPage.Children.Add(overrideBallistics);
            tuningPage.Children.Add(new TextBlock { Text = "Projectile values follow the selected ammunition slot. Every field can be typed directly.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) });
            projectileReference = ResolveProjectileReference();
            AddValue(tuningPage, "PROJECTILE MASS", "projectileMass", projectileReference == null ? 0 : projectileReference.Mass, original.ProjectileMassMultiplier, "kg");
            AddValue(tuningPage, "MUZZLE VELOCITY", "velocity", projectileReference == null ? 0 : projectileReference.Speed, original.MuzzleVelocityMultiplier, "m/s");
            AddValue(tuningPage, "EXPLOSIVE FILLER", "explosive", projectileReference == null ? 0 : projectileReference.ExplosiveMass, original.ExplosiveMassMultiplier, "kg");
            AddValue(tuningPage, "REFERENCE PENETRATION", "penetration", projectileReference == null ? 0 : projectileReference.Penetration, original.PenetrationMultiplier, "mm");
            AddValue(tuningPage, "FIRE RATE OVERRIDE (SEC)", "reload", 0, 0, "s");
            if (original.ReloadSeconds > 0 && tuning.ContainsKey("reload")) tuning["reload"].Text = FormatValue(original.ReloadSeconds);
            AddValue(tuningPage, "RECOIL TRAVEL", "recoil", vehicle.NativeRecoil, original.RecoilMultiplier, "m");
            tuningPage.Children.Add(new Border { Height = 1, Background = ModernPalette.Brush(ModernPalette.Border), Margin = new Thickness(0, 8, 0, 7) });
            AddValue(tuningPage, "ENGINE POWER", "engine", vehicle.NativeEnginePower, original.EnginePowerMultiplier, "hp");
            AddValue(tuningPage, "VEHICLE MASS", "mass", vehicle.NativeMass, original.VehicleMassMultiplier, "kg");
            AddValue(tuningPage, "FORWARD SPEED LIMIT", "forward", vehicle.NativeForwardSpeed, original.ForwardSpeedMultiplier, "km/h");
            AddValue(tuningPage, "REVERSE SPEED LIMIT", "reverse", vehicle.NativeReverseSpeed, original.ReverseSpeedMultiplier, "km/h");
            Button resetAll = new Button { Content = ModernText.L("RESET ALL TO CURRENT STOCK", "重置为当前默认弹"), Style = buttonStyle, Padding = new Thickness(14, 2, 14, 2), Margin = new Thickness(0, 10, 0, 4) };             
            resetAll.Click += delegate { ResetAllValues(); }; tuningPage.Children.Add(resetAll);

            tuningPage.Children.Add(new TextBlock { Text = "Stock reset uses this vehicle's current game definition; selected research modules remain configured separately in Modules.", Foreground = ModernPalette.Brush(ModernPalette.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 4) });
            tuningScroll.Content = labBody;
            StackPanel labHost = new StackPanel();
            labHost.Children.Add(labHeader);
            labHost.Children.Add(tuningScroll);
            tuningCard.Child = labHost; Grid.SetColumn(tuningCard, 2); body.Children.Add(tuningCard);
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
            // Only ask for a radar the vehicle actually has a native slot for - vehicles with
            // just one radar (track-only launchers like the 9A310 / Strela-10, search-only
            // TWS vehicles) shouldn't have to dismiss an irrelevant picker first.
            if (nativeSearchSensor != null && String.IsNullOrWhiteSpace(radarSearchSel))
            {
                ModernPickerDialog searchDlg = new ModernPickerDialog(searchTitle, items, searchTitle) { Owner = System.Windows.Window.GetWindow(this) };
                if (searchDlg.ShowDialog() == true && searchDlg.Selected != null) { radarSearchSel = (string)searchDlg.Selected.Tag; searchPick = searchDlg.Selected; }
            }
            ModernPickerItem trackPick = null;
            string trackTitle = ModernText.L("SELECT TRACK RADAR", "选择跟踪雷达");
            if (nativeTrackSensor != null)
            {
                ModernPickerDialog dlg = new ModernPickerDialog(trackTitle, items, trackTitle) { Owner = System.Windows.Window.GetWindow(this) };
                if (dlg.ShowDialog() == true && dlg.Selected != null) { radarTrackSel = (string)dlg.Selected.Tag; trackPick = dlg.Selected; }
            }
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
}
