// ModernShell.Maps.cs
// Targets map panel, combined-map window and the vehicle/map picker dialogs.
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
    internal sealed class MapPanel : StackPanel
    {
        private readonly List<TargetView> allGround;
        private readonly List<TargetView> allShips;
        private readonly List<CombinedMap> allCombinedMaps;
        private readonly string playerKind;
        private readonly Style toggleStyle;
        private ComboBox modeBox;
        private ComboBox eraBox;
        private CombinedMap currentMap;
        private Button mapPickerButton;
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


            // 新代码：按钮 + 文本显示
            mapPickerButton = new Button
            {
                Style = toggleStyle,
                Margin = new Thickness(0, 6, 0, 0),
                Padding = new Thickness(8, 4, 8, 4),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Content = ModernText.L("PICK MAP...", "点击选择地图...")
            };
            currentMap = allCombinedMaps.FirstOrDefault(x => x.Id != null && x.Id.Equals(currentScenario.MapId, StringComparison.OrdinalIgnoreCase)) ?? allCombinedMaps.FirstOrDefault();
            mapPickerButton.Click += delegate { PickCombinedMap(); };
            mapStack.Children.Add(mapPickerButton); Grid.SetColumn(mapStack, 0); combinedFields.Children.Add(mapStack);
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

        private void PickCombinedMap()
        {
            List<ModernPickerItem> items = new List<ModernPickerItem>();
            foreach (CombinedMap map in allCombinedMaps)
                items.Add(new ModernPickerItem { Display = map.Display, Detail = map.Level ?? "", Tag = map });
            ModernPickerDialog dlg = new ModernPickerDialog(ModernText.L("SELECT MAP", "选择地图"), items, ModernText.L("SELECT MAP", "选择地图")) { Owner = System.Windows.Window.GetWindow(this) };
            if (dlg.ShowDialog() == true && dlg.Selected != null && dlg.Selected.Tag is CombinedMap)
            {
                currentMap = (CombinedMap)dlg.Selected.Tag;
                mapPickerButton.Content = currentMap.Display;
                RefreshCombinedSpawns();
            }
        }


        private void RefreshCombinedSpawns()
        {
            CombinedMap map = currentMap;
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
            CombinedMap map = currentMap;
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
}
