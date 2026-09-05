// ModernShell.MainWindow.Events.cs
// Event wiring, tabs, options/targets/experimental/garage builders (segment 3/5).
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
    internal sealed partial class ModernMainWindow
    {
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
            StackPanel presetRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            Button presetButton = new Button { Content = ModernText.L("ONE-CLICK ASSEMBLY (PRESET)", "一键装配（预设）"), Style = (Style)Resources["ButtonStyle"], Padding = new Thickness(12, 3, 12, 3) };
            TextBlock presetHint = new TextBlock { Text = ModernText.L("Built-in verified loadouts - switches vehicle and fills every field.", "内置已验证配置——自动切车并填好全部选项。"), Foreground = ModernPalette.Brush(ModernPalette.Muted), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0), FontSize = 11.5 };
            presetButton.Click += delegate
            {
                List<GroundPresetRowJson> presets = MainForm.GroundPresets;
                if (presets == null || presets.Count == 0) { status.Text = ModernText.L("No presets available.", "没有可用预设。"); return; }
                List<ModernPickerItem> items = new List<ModernPickerItem>();
                foreach (GroundPresetRowJson pr in presets)
                    items.Add(new ModernPickerItem { Display = pr.name, Detail = (pr.note ?? "") + "  ·  " + pr.vehicle, Tag = pr });
                ModernPickerDialog dlg = new ModernPickerDialog(ModernText.L("ONE-CLICK ASSEMBLY", "一键装配"), items, ModernText.L("ONE-CLICK ASSEMBLY", "一键装配"));
                if (dlg.ShowDialog() == true && dlg.Selected != null)
                {
                    GroundPresetRowJson picked = dlg.Selected.Tag as GroundPresetRowJson;
                    if (picked != null) ApplyGroundPreset(picked);
                }
            };
            presetRow.Children.Add(presetButton);
            presetRow.Children.Add(presetHint);
            header.Children.Add(presetRow);
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
                SetStatus("GROUND CONFIGURATION UPDATED — " + selectedAircraft.Display, false);
            }
            else if (experimentalPanel is FlightConfigurePanel)
            {
                AircraftSettings r = ((FlightConfigurePanel)experimentalPanel).Collect();
                controller.WorkspaceSetSettings(selectedAircraft, r);
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

        // One-click assembly: write the preset's settings onto the target vehicle's live
        // settings, switch to that vehicle, then rebuild the experimental panel so every
        // field reflects the preset (cannon, rounds-per-reload, radars, fake-ARH, ammo).
        private void ApplyGroundPreset(GroundPresetRowJson preset)
        {
            if (preset == null || controller == null) return;
            Aircraft target = null;
            if (aircraftViews != null)
                foreach (AircraftView av in aircraftViews)
                    if (av != null && av.Source != null && av.Source.Id.Equals(preset.vehicle, StringComparison.OrdinalIgnoreCase)) { target = av.Source; break; }
            if (target == null) { if (status != null) status.Text = ModernText.L("Preset vehicle not in the catalog.", "预设载具不在目录中。"); return; }
            // WorkspaceGetSettings returns a COPY of the live settings - mutate it, then
            // write it back through WorkspaceSetSettings or the preset never lands.
            AircraftSettings s = controller.WorkspaceGetSettings(target);
            // Empty cannon = keep the vehicle's native weapon (per-vehicle).
            if (String.IsNullOrWhiteSpace(preset.cannon))
            {
                s.InjectedCannonBlk = null; s.InjectedCannonDomain = null; s.InjectedCannonRound = null; s.InjectedCannonRounds = 0;
            }
            else
            {
                s.InjectedCannonBlk = preset.cannon;
                s.InjectedCannonRound = preset.cannonRound;
                s.InjectedCannonRounds = preset.cannonRounds;
            }
            s.UnlimitedAmmo = preset.unlimited;
            s.FakeArhConversion = preset.fakeArh;
            s.InjectNativeLauncher = preset.injectNative;
            s.RadarSearchBlk = preset.radarSearch;
            s.RadarTrackBlk = preset.radarTrack;
            controller.WorkspaceSetSettings(target, s);
            SelectVehicleById(target.Id, null);
            experimentalBuilt = false;
            BuildExperimentalTab();
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

    }
}
