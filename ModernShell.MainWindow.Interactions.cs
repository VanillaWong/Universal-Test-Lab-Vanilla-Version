// ModernShell.MainWindow.Interactions.cs
// Session, filtering, pylon/weapon mount flow, dialogs, summary (segment 4/5).
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

    }
}
