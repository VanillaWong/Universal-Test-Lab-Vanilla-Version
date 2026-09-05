// UniversalTestLab.MainForm.BuildUi.cs
// Legacy WinForms UI construction, selection and installers (segment 2/5).
// Split from UniversalTestLab.cs during the 2026-09-05 partial-class refactor; members are byte-identical.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace UniversalTestLab
{
    internal sealed partial class MainForm
    {
        private void BuildUi()
        {
            GradientBackdrop backdrop = new GradientBackdrop { Dock = DockStyle.Fill };
            Controls.Add(backdrop);
            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Color.Transparent };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            backdrop.Controls.Add(root);
            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildWorkspace(), 0, 1);
            status = new Label { Dock = DockStyle.Fill, Text = "●  READY — choose a vehicle to begin", ForeColor = Theme.Good, BackColor = Color.FromArgb(210, 12, 15, 39), Padding = new Padding(20, 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft };
            root.Controls.Add(status, 0, 2);
        }

        private Control BuildHeader()
        {
            GlassPanel shell = new GlassPanel { Dock = DockStyle.Fill, HeaderStyle = true, Margin = new Padding(14, 10, 14, 2) };
            TableLayoutPanel bar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 7, BackColor = Color.Transparent, Padding = new Padding(16, 7, 12, 7) };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 255));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 102));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
            TableLayoutPanel brand = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.Transparent, Margin = new Padding(0) };
            brand.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            brand.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Label title = Theme.Label("Universal Test Lab", true);
            title.Font = new Font("Segoe UI Semibold", 15.5f);
            title.ForeColor = Theme.Text;
            brand.Controls.Add(title, 0, 0);
            Label subtitle = Theme.Label("WAR THUNDER MISSION STUDIO", false);
            subtitle.ForeColor = Theme.AccentLight;
            subtitle.Font = new Font("Segoe UI Semibold", 7.7f);
            brand.Controls.Add(subtitle, 0, 1);
            bar.Controls.Add(brand, 0, 0);
            TableLayoutPanel folder = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.Transparent, Margin = new Padding(8, 0, 0, 0) };
            folder.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            folder.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Label folderLabel = Theme.Label("GAME DIRECTORY", false);
            folderLabel.Font = new Font("Segoe UI Semibold", 7.4f);
            folder.Controls.Add(folderLabel, 0, 0);
            gameFolder = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 2, 0, 3) };
            Theme.Input(gameFolder);
            folder.Controls.Add(gameFolder, 0, 1);
            bar.Controls.Add(folder, 1, 0);
            Button browse = new Button { Text = "BROWSE", Dock = DockStyle.Fill, Margin = new Padding(8, 8, 0, 8) };
            Theme.Button(browse, false);
            browse.Click += delegate { BrowseFolder(); };
            bar.Controls.Add(browse, 2, 0);
            Button install = new Button { Text = "SYNC BASE", Dock = DockStyle.Fill, Margin = new Padding(8, 8, 0, 8) };
            Theme.Button(install, false);
            install.Click += delegate { InstallClicked(); };
            bar.Controls.Add(install, 3, 0);
            Button open = new Button { Text = "MISSIONS", Dock = DockStyle.Fill, Margin = new Padding(8, 8, 0, 8) };
            Theme.Button(open, false);
            open.Click += delegate { OpenMissionFolder(); };
            bar.Controls.Add(open, 4, 0);
            Button presets = new Button { Text = "PRESETS", Dock = DockStyle.Fill, Margin = new Padding(8, 8, 0, 8) };
            Theme.Button(presets, false);
            presets.Click += delegate { ShowPresets(); };
            bar.Controls.Add(presets, 5, 0);
            Button about = new Button { Text = "SUPPORT", Dock = DockStyle.Fill, Margin = new Padding(8, 8, 0, 8) };
            Theme.Button(about, false);
            about.Click += delegate { ShowAbout(); };
            bar.Controls.Add(about, 6, 0);
            shell.Controls.Add(bar);
            return shell;
        }

        private Control BuildWorkspace()
        {
            TableLayoutPanel workspace = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(8, 4, 8, 8), BackColor = Color.Transparent };
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 316));
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 328));
            workspace.Controls.Add(BuildAircraftBrowser(), 0, 0);
            workspace.Controls.Add(BuildLoadoutBuilder(), 1, 0);
            workspace.Controls.Add(BuildMissionPanel(), 2, 0);
            return workspace;
        }

        private Control SurfacePanel()
        {
            return new GlassPanel { Dock = DockStyle.Fill, Margin = new Padding(6) };
        }

        private Control BuildAircraftBrowser()
        {
            Panel panel = (Panel)SurfacePanel();
            TableLayoutPanel grid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 8, ColumnCount = 1, Padding = new Padding(15), BackColor = Color.Transparent };
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            grid.Controls.Add(Theme.StepHeader("01", "CHOOSE VEHICLE", "Aircraft, helicopters and experimental airframes"), 0, 0);
            grid.Controls.Add(Theme.Label("SEARCH", false), 0, 1);
            aircraftSearch = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 2, 0, 5) };
            Theme.Input(aircraftSearch);
            aircraftSearch.TextChanged += delegate { FilterAircraft(); };
            grid.Controls.Add(aircraftSearch, 0, 2);
            grid.Controls.Add(Theme.Label("NATION  /  RANK  /  TYPE", false), 0, 3);
            TableLayoutPanel filters = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31));
            nationFilter = DarkCombo();
            rankFilter = DarkCombo();
            vehicleFilter = DarkCombo();
            nationFilter.SelectedIndexChanged += delegate { FilterAircraft(); };
            rankFilter.SelectedIndexChanged += delegate { FilterAircraft(); };
            vehicleFilter.SelectedIndexChanged += delegate { FilterAircraft(); };
            filters.Controls.Add(nationFilter, 0, 0);
            filters.Controls.Add(rankFilter, 1, 0);
            filters.Controls.Add(vehicleFilter, 2, 0);
            grid.Controls.Add(filters, 0, 4);
            grid.Controls.Add(Theme.Label("AVAILABLE VEHICLES", false), 0, 5);
            Label countHint = Theme.Label("Live catalog from your installed game files", false);
            countHint.Font = new Font("Segoe UI", 8.2f);
            countHint.ForeColor = Theme.AccentLight;
            grid.Controls.Add(countHint, 0, 6);
            aircraftList = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, BorderStyle = BorderStyle.None, BackColor = Theme.Field, ForeColor = Theme.Text, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 31 };
            aircraftList.DrawItem += DrawAircraftItem;
            aircraftList.SelectedIndexChanged += delegate { AircraftChanged(); };
            grid.Controls.Add(aircraftList, 0, 7);
            panel.Controls.Add(grid);
            return panel;
        }

        private Control BuildLoadoutBuilder()
        {
            Panel panel = (Panel)SurfacePanel();
            TableLayoutPanel grid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1, Padding = new Padding(15), BackColor = Color.Transparent };
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            TableLayoutPanel heading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
            heading.Controls.Add(Theme.StepHeader("02", "BUILD LOADOUT", "Select a station, then mount a weapon"), 0, 0);
            massLabel = Theme.Label("MASS: 0 kg", false);
            massLabel.TextAlign = ContentAlignment.MiddleRight;
            massLabel.ForeColor = Theme.AccentLight;
            massLabel.Font = new Font("Segoe UI Semibold", 8.5f);
            heading.Controls.Add(massLabel, 1, 0);
            grid.Controls.Add(heading, 0, 0);
            stationLabel = Theme.Label("Choose a vehicle to reveal its weapon stations.", false);
            grid.Controls.Add(stationLabel, 0, 1);
            pylonStrip = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight, BackColor = Theme.Field, Padding = new Padding(7), Margin = new Padding(0, 2, 0, 7) };
            Theme.Round(pylonStrip, 12);
            grid.Controls.Add(pylonStrip, 0, 2);
            grid.Controls.Add(BuildWeaponFilters(), 0, 3);
            weaponList = new ListView
            {
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false,
                BorderStyle = BorderStyle.None, BackColor = Theme.Field, ForeColor = Theme.Text
            };
            weaponList.Columns.Add("Weapon", 330);
            weaponList.Columns.Add("Type", 185);
            weaponList.Columns.Add("Ammo", 62, HorizontalAlignment.Center);
            weaponList.Columns.Add("Mass", 85, HorizontalAlignment.Right);
            weaponList.Columns.Add("Mode", 78, HorizontalAlignment.Center);
            weaponList.DoubleClick += delegate { AssignSelectedWeapon(); };
            grid.Controls.Add(weaponList, 0, 4);
            TableLayoutPanel actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, Padding = new Padding(0, 6, 0, 0) };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142));
            actions.Controls.Add(Theme.Label("Tip: double-click a weapon to mount it", false), 0, 0);
            aircraftSettingsButton = new Button { Text = "MODULES", Dock = DockStyle.Fill, Margin = new Padding(5, 0, 0, 0) };
            Theme.Button(aircraftSettingsButton, false);
            aircraftSettingsButton.Click += delegate { ShowAircraftSettings(); };
            actions.Controls.Add(aircraftSettingsButton, 1, 0);
            Button clear = new Button { Text = "CLEAR STATION", Dock = DockStyle.Fill, Margin = new Padding(5, 0, 0, 0) };
            Theme.Button(clear, false);
            clear.Click += delegate { ClearSelectedStation(); };
            actions.Controls.Add(clear, 2, 0);
            Button clearAll = new Button { Text = "CLEAR ALL", Dock = DockStyle.Fill, Margin = new Padding(5, 0, 0, 0) };
            Theme.Button(clearAll, false);
            clearAll.Click += delegate { assignments.Clear(); RefreshPylons(); };
            actions.Controls.Add(clearAll, 3, 0);
            Button add = new Button { Text = "MOUNT WEAPON", Dock = DockStyle.Fill, Margin = new Padding(7, 0, 0, 0) };
            Theme.Button(add, true);
            add.Click += delegate { AssignSelectedWeapon(); };
            actions.Controls.Add(add, 4, 0);
            grid.Controls.Add(actions, 0, 5);
            panel.Controls.Add(grid);
            return panel;
        }

        private Control BuildWeaponFilters()
        {
            TableLayoutPanel row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 2, Padding = new Padding(0, 3, 0, 3), BackColor = Color.Transparent };
            row.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            row.Controls.Add(Theme.Label("WEAPON SOURCE", false), 0, 0);
            row.Controls.Add(Theme.Label("SEARCH", false), 1, 0);
            row.Controls.Add(Theme.Label("WEAPON TYPE", false), 2, 0);
            row.Controls.Add(Theme.Label("NATION", false), 3, 0);
            row.Controls.Add(Theme.Label("SORT", false), 4, 0);
            injectionToggle = new CheckBox { Text = "INJECT ANY WEAPON", Dock = DockStyle.Fill, Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter };
            Theme.Toggle(injectionToggle);
            injectionToggle.ForeColor = Theme.AccentLight;
            injectionToggle.CheckedChanged += delegate { RefreshWeaponCatalog(); };
            row.Controls.Add(injectionToggle, 0, 1);
            weaponSearch = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(7, 3, 7, 3) };
            Theme.Input(weaponSearch);
            weaponSearch.Text = "";
            weaponSearch.TextChanged += delegate { RefreshWeaponCatalog(); };
            row.Controls.Add(weaponSearch, 1, 1);
            categoryFilter = DarkCombo();
            categoryFilter.SelectedIndexChanged += delegate { RefreshWeaponCatalog(); };
            row.Controls.Add(categoryFilter, 2, 1);
            weaponNationFilter = DarkCombo();
            weaponNationFilter.SelectedIndexChanged += delegate { RefreshWeaponCatalog(); };
            row.Controls.Add(weaponNationFilter, 3, 1);
            sortFilter = DarkCombo();
            sortFilter.Items.AddRange(new object[] { "Mass: low to high", "Mass: high to low", "Name: A to Z" });
            sortFilter.SelectedIndexChanged += delegate { RefreshWeaponCatalog(); };
            row.Controls.Add(sortFilter, 4, 1);
            return row;
        }

        private Control BuildMissionPanel()
        {
            Panel panel = (Panel)SurfacePanel();
            TableLayoutPanel grid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 14, ColumnCount = 1, Padding = new Padding(15), BackColor = Color.Transparent };
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 145));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            grid.Controls.Add(Theme.StepHeader("03", "CONFIGURE TEST", "Choose targets and generate the mission"), 0, 0);
            preview = new AircraftPreview { Dock = DockStyle.Fill };
            grid.Controls.Add(preview, 0, 1);
            grid.Controls.Add(Theme.Label("TARGETS", true), 0, 2);
            grid.Controls.Add(Theme.Label("AIR TARGET", false), 0, 3);
            airTargetBox = TargetRowCombo(aircraft.Cast<object>().ToList());
            airCount = CountBox(1);
            grid.Controls.Add(ComboAndCount(airTargetBox, airCount), 0, 4);
            grid.Controls.Add(Theme.Label("GROUND TARGET / ENEMY AIR DEFENCE", false), 0, 5);
            groundTargetBox = TargetRowCombo(groundTargets.Cast<object>().ToList());
            groundCount = CountBox(1);
            hostileGround = new CheckBox { Text = "HOSTILE", Dock = DockStyle.Fill, Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter };
            Theme.Toggle(hostileGround);
            hostileGround.ForeColor = Theme.Danger;
            grid.Controls.Add(ComboCountAndOption(groundTargetBox, groundCount, hostileGround), 0, 6);
            samSites = new CheckBox { Text = "SAM SITES: ACTIVE", Dock = DockStyle.Fill, Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter, Checked = true };
            Theme.Toggle(samSites);
            samSites.ForeColor = Theme.Danger;
            samSites.CheckedChanged += delegate { samSites.Text = samSites.Checked ? "SAM SITES: ACTIVE" : "SAM SITES: DISABLED"; };
            grid.Controls.Add(samSites, 0, 7);
            grid.Controls.Add(Theme.Label("NAVAL TARGET", false), 0, 8);
            shipTargetBox = TargetRowCombo(shipTargets.Cast<object>().ToList());
            shipCount = CountBox(1);
            grid.Controls.Add(ComboAndCount(shipTargetBox, shipCount), 0, 9);
            Label details = Theme.Label("FLIGHT PROFILE\r\nFull fuel • adaptive air start • no external tanks\r\nAmmo restoration: every 10 seconds\r\nHOSTILE: active enemy air defence\r\nSAM SITES: ACTIVE keeps the S-300/Patriot/Buk sites engaging you\r\nNuclear weapons: native detonation", false);
            details.Padding = new Padding(4, 12, 4, 4);
            grid.Controls.Add(details, 0, 10);
            Label hint = Theme.Label("Aircraft/helicopters: reopen User Missions. Ground vehicle changes: restart War Thunder once so the tank proxy is reloaded.", false);
            hint.ForeColor = Theme.Good;
            grid.Controls.Add(hint, 0, 11);
            Button apply = new Button { Text = "GENERATE TEST MISSION", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 11f), Margin = new Padding(0, 4, 0, 2) };
            Theme.Button(apply, true);
            apply.Click += delegate { ApplyClicked(); };
            grid.Controls.Add(apply, 0, 12);
            Label version = Theme.Label("HOT LOAD  •  NO GAME RESTART", false);
            version.TextAlign = ContentAlignment.MiddleCenter;
            version.ForeColor = Theme.AccentLight;
            grid.Controls.Add(version, 0, 13);
            panel.Controls.Add(grid);
            return panel;
        }

        private ComboBox DarkCombo()
        {
            ComboBox box = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Theme.Field, ForeColor = Theme.Text, Margin = new Padding(2, 2, 2, 5), DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 21 };
            box.DrawItem += DrawDarkComboItem;
            return box;
        }

        private void DrawDarkComboItem(object sender, DrawItemEventArgs e)
        {
            ComboBox box = sender as ComboBox;
            if (box == null) return;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (SolidBrush background = new SolidBrush(selected ? Theme.AccentDark : Theme.Field))
                e.Graphics.FillRectangle(background, e.Bounds);
            string value = e.Index >= 0 && e.Index < box.Items.Count ? box.Items[e.Index].ToString() : (box.SelectedItem == null ? "" : box.SelectedItem.ToString());
            TextRenderer.DrawText(e.Graphics, value, box.Font, new Rectangle(e.Bounds.X + 6, e.Bounds.Y, Math.Max(1, e.Bounds.Width - 8), e.Bounds.Height),
                Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private ComboBox TargetRowCombo(List<object> values)
        {
            ComboBox box = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown, FlatStyle = FlatStyle.Flat, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems, BackColor = Theme.Field, ForeColor = Theme.Text, Margin = new Padding(0, 2, 4, 4) };
            box.Items.AddRange(values.ToArray());
            return box;
        }

        private NumericUpDown CountBox(int value)
        {
            return new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 20, Value = value, BackColor = Theme.Field, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(2, 2, 0, 4) };
        }

        private Control ComboAndCount(ComboBox box, NumericUpDown count)
        {
            TableLayoutPanel row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62));
            row.Controls.Add(box, 0, 0);
            row.Controls.Add(count, 1, 0);
            return row;
        }

        private Control ComboCountAndOption(ComboBox box, NumericUpDown count, CheckBox option)
        {
            TableLayoutPanel row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = Color.Transparent };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
            row.Controls.Add(box, 0, 0);
            row.Controls.Add(count, 1, 0);
            row.Controls.Add(option, 2, 0);
            return row;
        }

        private void DrawAircraftItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= aircraftList.Items.Count) return;
            Aircraft item = aircraftList.Items[e.Index] as Aircraft;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Rectangle row = e.Bounds;
            using (SolidBrush background = new SolidBrush(selected ? Color.FromArgb(76, 62, 174) : Theme.Field))
                e.Graphics.FillRectangle(background, row);
            if (selected)
                using (SolidBrush accent = new SolidBrush(Theme.AccentLight)) e.Graphics.FillRectangle(accent, row.X, row.Y + 5, 3, Math.Max(1, row.Height - 10));
            string title = item == null ? aircraftList.Items[e.Index].ToString() : item.Display;
            string meta = item == null ? "" : item.Nation + "  •  RANK " + item.Rank.ToString(CultureInfo.InvariantCulture) + "  •  " + item.Kind;
            using (Font titleFont = new Font("Segoe UI Semibold", 8.8f))
            using (Font metaFont = new Font("Segoe UI", 7.2f))
            using (SolidBrush titleBrush = new SolidBrush(Theme.Text))
            using (SolidBrush metaBrush = new SolidBrush(selected ? Color.FromArgb(213, 220, 255) : Theme.Muted))
            {
                e.Graphics.DrawString(title, titleFont, titleBrush, new RectangleF(row.X + 10, row.Y + 2, row.Width - 14, 16));
                e.Graphics.DrawString(meta, metaFont, metaBrush, new RectangleF(row.X + 10, row.Y + 17, row.Width - 14, 13));
            }
            e.DrawFocusRectangle();
        }

        private void SelectDefaults()
        {
            nationFilter.Items.Add("All Nations");
            foreach (string nation in aircraft.Select(a => a.Nation).Distinct().OrderBy(x => x)) nationFilter.Items.Add(nation);
            rankFilter.Items.Add("Any");
            for (int i = 1; i <= Math.Max(9, aircraft.Max(a => a.Rank)); i++) rankFilter.Items.Add("Rank " + i.ToString(CultureInfo.InvariantCulture));
            vehicleFilter.Items.Add("All Types");
            foreach (string kind in aircraft.Select(a => a.Kind).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
                vehicleFilter.Items.Add(kind == "Aircraft" ? "Aircraft" : kind + "s");
            categoryFilter.Items.Add("All Weapon Types");
            foreach (string category in globalWeapons.Select(w => w.Category).Distinct().OrderBy(x => x)) categoryFilter.Items.Add(category);
            weaponNationFilter.Items.Add("All Nations");
            foreach (string nation in aircraft.Select(a => a.Nation).Distinct().OrderBy(x => x)) weaponNationFilter.Items.Add(nation);
            nationFilter.SelectedIndex = 0;
            rankFilter.SelectedIndex = 0;
            vehicleFilter.SelectedIndex = 0;
            categoryFilter.SelectedIndex = 0;
            weaponNationFilter.SelectedIndex = 0;
            sortFilter.SelectedIndex = 0;
            FilterAircraft();
            Aircraft defaultAircraft = aircraftList.Items.Cast<object>().OfType<Aircraft>().FirstOrDefault(a => a.Id == "ef_2000_typhoon_aesa");
            if (defaultAircraft != null) aircraftList.SelectedItem = defaultAircraft;
            SelectComboById(airTargetBox, "j_10c");
            SelectComboById(groundTargetBox, "ussr_bmpt");
            SelectComboById(shipTargetBox, "jp_battleship_yamato");
        }

        private static void SelectComboById(ComboBox combo, string id)
        {
            foreach (object item in combo.Items)
            {
                Aircraft a = item as Aircraft;
                TargetUnit t = item as TargetUnit;
                if ((a != null && a.Id == id) || (t != null && t.Id == id)) { combo.SelectedItem = item; return; }
            }
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private Aircraft SelectedAircraft { get { return aircraftList.SelectedItem as Aircraft; } }

        internal static bool IsGroundVehicle(Aircraft item)
        {
            return item != null && String.Equals(item.Kind, "Ground Vehicle", StringComparison.OrdinalIgnoreCase);
        }

        private void FilterAircraft()
        {
            if (aircraftList == null || nationFilter == null || rankFilter == null || vehicleFilter == null) return;
            string keep = SelectedAircraft == null ? null : SelectedAircraft.Id;
            string search = aircraftSearch.Text.Trim();
            string nation = nationFilter.SelectedIndex <= 0 ? null : nationFilter.SelectedItem as string;
            int rank = rankFilter.SelectedIndex <= 0 ? 0 : rankFilter.SelectedIndex;
            string vehicle = vehicleFilter.SelectedIndex <= 0 ? null : (vehicleFilter.SelectedItem as string ?? "").TrimEnd('s');
            IEnumerable<Aircraft> query = aircraft;
            if (!String.IsNullOrEmpty(search)) query = query.Where(a => a.Display.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 || a.Id.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!String.IsNullOrEmpty(nation)) query = query.Where(a => a.Nation == nation);
            if (rank > 0) query = query.Where(a => a.Rank == rank);
            if (!String.IsNullOrEmpty(vehicle)) query = query.Where(a => a.Kind.Equals(vehicle, StringComparison.OrdinalIgnoreCase));
            List<Aircraft> filtered = query.OrderByDescending(a => a.Rank).ThenBy(a => a.Display).ToList();
            aircraftList.BeginUpdate();
            aircraftList.Items.Clear();
            aircraftList.Items.AddRange(filtered.Cast<object>().ToArray());
            aircraftList.EndUpdate();
            Aircraft previous = filtered.FirstOrDefault(a => a.Id == keep);
            if (previous != null) aircraftList.SelectedItem = previous;
            else if (filtered.Count > 0) aircraftList.SelectedIndex = 0;
        }

        private void AircraftChanged()
        {
            Aircraft selected = SelectedAircraft;
            assignments.Clear();
            selectedPylon = null;
            preview.Aircraft = selected;
            preview.Invalidate();
            BuildPylonStrip();
            UpdateAircraftSettingsButton();
            if (selected != null)
                SetStatus("VEHICLE READY — " + selected.Display + "  •  " + pylonButtons.Count.ToString(CultureInfo.InvariantCulture) + " editable stations", false);
        }

        private static bool IsFpvDrone(Aircraft item)
        {
            return item != null && item.Id.Equals("uav_inf_fpv_strike_drone", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsHelicopter(Aircraft item, string unitBlk)
        {
            if (item != null && String.Equals(item.Kind, "Helicopter", StringComparison.OrdinalIgnoreCase)) return true;
            return Regex.IsMatch(unitBlk ?? "", @"(?i)hellicopters_metaparts|(?m)^\s*helicopter\s*\{");
        }

        internal static bool LooksLikeJetAircraft(string unitBlk)
        {
            string text = unitBlk ?? "";
            return Regex.IsMatch(text, @"(?i)(jet_(?:fighter|bomber)_metaparts|armor_jet_engine|(?:standard|afterburner|start)ExhaustFxType:t\s*=\s*""jet_)");
        }

        internal static int ResolveSpawnSpeed(Aircraft item, string unitBlk)
        {
            if (IsFpvDrone(item)) return 100;
            // Helicopters must spawn from a stationary hover. The fixed-wing mission
            // speed field is not interpreted as km/h for helicopter usermodels; using
            // 180 here launches an Apache at destructive overspeed.
            if (IsHelicopter(item, unitBlk)) return 0;
            if (LooksLikeJetAircraft(unitBlk))
                return item != null && item.Rank > 0 && item.Rank <= 5 ? 700 : 1100;
            return 450;
        }

        internal static int ResolveConfiguredSpawnSpeed(Aircraft item, string unitBlk, MissionSettings mission)
        {
            if (mission == null) return ResolveSpawnSpeed(item, unitBlk);
            // Airport takeoff must always start stationary on the runway.
            if (mission.SpawnMode != null && mission.SpawnMode.Equals("airport", StringComparison.OrdinalIgnoreCase)) return 0;
            if (!mission.SpawnSpeedAuto) return Math.Max(0, Math.Min(1100, mission.SpawnSpeedKmh));
            int speed = ResolveSpawnSpeed(item, unitBlk);
            if (speed <= 0) return speed;
            // Airframe structural limits are not exposed directly, so clamp the
            // spawn speed to the lowest published maxSpeed (m/s) in the flight
            // model. Overspeeding a fragile airframe at spawn tears it apart.
            double lowest = double.MaxValue;
            foreach (Match match in Regex.Matches(unitBlk ?? "", @"(?m)^\s*maxSpeed:r\s*=\s*([0-9.]+)"))
            {
                double value;
                if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value > 0)
                    lowest = Math.Min(lowest, value);
            }
            if (lowest < double.MaxValue)
                speed = Math.Min(speed, (int)Math.Round(lowest * 3.6));
            return speed;
        }

        private AircraftSettings GetAircraftSettings(Aircraft item)
        {
            if (item == null) return new AircraftSettings();
            AircraftSettings settings;
            if (!aircraftSettings.TryGetValue(item.Id, out settings))
            {
                settings = new AircraftSettings();
                aircraftSettings[item.Id] = settings;
            }
            return settings;
        }

        private static string AircraftSettingsStorePath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniversalTestLab", "aircraft_settings.txt"); }
        }

        private void LoadAircraftSettings()
        {
            try
            {
                Dictionary<string, object> all = ConfigStore.GetObject("aircraft_settings");
                if (all == null) return;
                foreach (KeyValuePair<string, object> pair in all)
                {
                    Dictionary<string, object> obj = pair.Value as Dictionary<string, object>;
                    if (obj == null) continue;
                    AircraftSettings settings = PresetStore.DeserializeSettingsJson(obj);
                    if (settings == null) continue;
                    aircraftSettings[pair.Key] = settings;
                }
            }
            catch { }
        }

        private void PersistAircraftSettings()
        {
            try
            {
                Dictionary<string, object> all = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, AircraftSettings> pair in aircraftSettings.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                    all[pair.Key] = PresetStore.SerializeSettingsJson(pair.Value);
                ConfigStore.SetObject("aircraft_settings", all);
                ConfigStore.Save();
            }
            catch { }
        }

        private void ShowAircraftSettings()
        {
            Aircraft selected = SelectedAircraft;
            if (selected == null)
            {
                MessageBox.Show(this, "Select an aircraft or helicopter first.", "Universal Test Lab", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            List<AircraftModification> available = modifications.Where(x => x.AircraftId.Equals(selected.Id, StringComparison.OrdinalIgnoreCase)).ToList();
            using (AircraftSettingsForm dialog = new AircraftSettingsForm(selected, available, GetAircraftSettings(selected), IsHelicopter(selected, null)))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result == null) return;
                aircraftSettings[selected.Id] = dialog.Result;
                PersistAircraftSettings();
            }
            UpdateAircraftSettingsButton();
            SetStatus("Modification and countermeasure settings updated for " + selected.Display + ".", false);
        }

        private void UpdateAircraftSettingsButton()
        {
            if (aircraftSettingsButton == null) return;
            Aircraft selected = SelectedAircraft;
            AircraftSettings settings;
            bool custom = selected != null && aircraftSettings.TryGetValue(selected.Id, out settings) &&
                (!settings.UseAllModifications || settings.OverrideCountermeasures || !settings.FullFuel || settings.GunBeltSelections.Count > 0);
            aircraftSettingsButton.Text = custom ? "MODULES  •" : "MODULES";
            aircraftSettingsButton.ForeColor = custom ? Theme.AccentLight : Theme.Text;
        }

        internal static string ApplyPlayerSpawnSpeed(string mission, int speedKmh)
        {
            if (String.IsNullOrEmpty(mission)) throw new ArgumentException("Mission text is required.", "mission");
            if (speedKmh < 0) throw new ArgumentOutOfRangeException("speedKmh");
            Regex marker = new Regex(@"(?m)^(\s*)speed:r=1100\s*$");
            if (!marker.IsMatch(mission)) throw new InvalidOperationException("Player spawn-speed markers are missing from the mission template.");
            return marker.Replace(mission, delegate(Match match)
            {
                return match.Groups[1].Value + "speed:r=" + speedKmh.ToString(CultureInfo.InvariantCulture);
            });
        }

        internal static string ApplyPlayerFuel(string mission, AircraftSettings settings)
        {
            if (String.IsNullOrEmpty(mission)) throw new ArgumentException("Mission text is required.", "mission");
            int percent = settings == null || settings.FullFuel ? 100 : Math.Max(8, Math.Min(100, (int)Math.Round(settings.FuelMinutes * 100.0 / 60.0)));
            Regex marker = new Regex(@"(?m)^(\s*)fuel:r=100\s*$");
            if (!marker.IsMatch(mission)) throw new InvalidOperationException("Player fuel markers are missing from the mission template.");
            return marker.Replace(mission, delegate(Match match)
            {
                return match.Groups[1].Value + "fuel:r=" + percent.ToString(CultureInfo.InvariantCulture);
            });
        }

        internal static string ApplyPlayerGunBelts(string mission, AircraftSettings settings)
        {
            if (String.IsNullOrEmpty(mission)) throw new ArgumentException("Mission text is required.", "mission");
            if (settings == null || settings.GunBeltSelections.Count == 0) return mission;
            BlockSpan player = BlkTools.UnitBlockByName(mission, "You");
            if (player == null) throw new InvalidOperationException("Player unit is missing from the mission template.");
            string block = player.Text;
            foreach (KeyValuePair<int, string> selection in settings.GunBeltSelections.OrderBy(x => x.Key))
            {
                if (selection.Key < 0 || selection.Key > 3 || String.IsNullOrWhiteSpace(selection.Value)) continue;
                string field = "bullets" + selection.Key.ToString(CultureInfo.InvariantCulture);
                Regex marker = new Regex("(?m)^(\\s*)" + Regex.Escape(field) + ":t\\s*=\\s*\"[^\"]*\"\\s*$");
                string safe = selection.Value.Replace("\"", "");
                if (marker.IsMatch(block))
                    block = marker.Replace(block, delegate(Match match) { return match.Groups[1].Value + field + ":t=\"" + safe + "\""; }, 1);
                else
                {
                    int open = block.IndexOf('{');
                    block = block.Insert(open + 1, Environment.NewLine + "    " + field + ":t=\"" + safe + "\"");
                }
            }
            return mission.Substring(0, player.Start) + block + mission.Substring(player.End + 1);
        }

        private void BuildPylonStrip()
        {
            pylonStrip.SuspendLayout();
            pylonStrip.Controls.Clear();
            pylonButtons.Clear();
            Aircraft selected = SelectedAircraft;
            if (selected != null)
            {
                foreach (PylonSlot pylon in pylons.Where(p => p.AircraftId == selected.Id).OrderBy(p => p.Order).ThenBy(p => p.Slot))
                {
                    Button button = new Button { Width = 103, Height = 78, Tag = pylon, Margin = new Padding(3), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 8.2f) };
                    Theme.Button(button, false);
                    button.Click += delegate(object sender, EventArgs e) { SelectPylon((PylonSlot)((Button)sender).Tag); };
                    pylonStrip.Controls.Add(button);
                    pylonButtons[pylon.Slot] = button;
                }
            }
            pylonStrip.ResumeLayout();
            if (pylonButtons.Count > 0) SelectPylon((PylonSlot)pylonButtons.Values.First().Tag);
            else
            {
                stationLabel.Text = IsFpvDrone(selected)
                    ? "FPV DRONE — no external pylons. Fly into the target to detonate the built-in HEAT warhead."
                    : (selected != null && selected.Id.StartsWith("nt_", StringComparison.OrdinalIgnoreCase)
                        ? "This Nuclear Escalation variant has a fixed event loadout. Choose the standard aircraft to edit its bomb-bay stations."
                        : "This vehicle has no editable weapon stations in the current catalog.");
                RefreshPylons();
                RefreshWeaponCatalog();
            }
        }

        private void SelectPylon(PylonSlot pylon)
        {
            selectedPylon = pylon;
            stationLabel.Text = "STATION " + pylon.Slot + " — choose a compatible weapon, or enable Injection for the full catalog.";
            RefreshPylons();
            RefreshWeaponCatalog();
        }

        private void RefreshPylons()
        {
            double total = 0;
            foreach (KeyValuePair<int, Button> pair in pylonButtons)
            {
                PylonAssignment assignment;
                bool has = assignments.TryGetValue(pair.Key, out assignment);
                string weapon = has ? ShortName(assignment.Weapon.Name, 18) : "EMPTY";
                pair.Value.Text = "STATION " + pair.Key + "\r\n" + weapon + (has && assignment.Injected ? "\r\nINJECTED" : "");
                pair.Value.BackColor = selectedPylon != null && selectedPylon.Slot == pair.Key ? Theme.AccentDark : (has ? Color.FromArgb(34, 92, 98) : Theme.Surface3);
                pair.Value.FlatAppearance.BorderColor = selectedPylon != null && selectedPylon.Slot == pair.Key ? Theme.AccentLight : Theme.Border;
                if (has) total += assignment.Weapon.TotalMass;
            }
            Aircraft selected = SelectedAircraft;
            string limit = selected != null && selected.MaxLoad > 0 ? " / " + selected.MaxLoad.ToString("0", CultureInfo.InvariantCulture) + " kg" : "";
            massLabel.Text = "MASS: " + total.ToString("0.0", CultureInfo.InvariantCulture) + " kg" + limit;
        }

        private static string ShortName(string value, int length)
        {
            if (String.IsNullOrEmpty(value)) return "WEAPON";
            string cleaned = Regex.Replace(value, @"\s+(air-to-air|air-to-ground|guided)?\s*(missile|missiles|bomb|bombs)$", "", RegexOptions.IgnoreCase).Trim();
            return cleaned.Length <= length ? cleaned.ToUpperInvariant() : cleaned.Substring(0, Math.Max(3, length - 1)).ToUpperInvariant() + "…";
        }

        private void RefreshWeaponCatalog()
        {
            if (weaponList == null) return;
            weaponList.BeginUpdate();
            weaponList.Items.Clear();
            weaponList.Groups.Clear();
            if (selectedPylon == null || SelectedAircraft == null) { weaponList.EndUpdate(); return; }
            IEnumerable<DonorWeapon> source = injectionToggle.Checked
                ? globalWeapons
                : nativeWeapons.Where(w => w.AircraftId == SelectedAircraft.Id && w.Slot == selectedPylon.Slot)
                    .GroupBy(w => w.Blk + "|" + w.Bullets).Select(g => g.First());
            string search = weaponSearch.Text.Trim();
            if (!String.IsNullOrEmpty(search)) source = source.Where(w => w.Name.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 || w.Category.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 || w.Blk.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            string category = categoryFilter.SelectedIndex <= 0 ? null : categoryFilter.SelectedItem as string;
            if (!String.IsNullOrEmpty(category)) source = source.Where(w => w.Category == category);
            string weaponNation = weaponNationFilter.SelectedIndex <= 0 ? null : weaponNationFilter.SelectedItem as string;
            if (!String.IsNullOrEmpty(weaponNation)) source = source.Where(w => (w.Nations ?? "").Split('|').Any(n => n.Equals(weaponNation, StringComparison.OrdinalIgnoreCase)));
            if (sortFilter.SelectedIndex == 1) source = source.OrderByDescending(w => w.TotalMass).ThenBy(w => w.Name);
            else if (sortFilter.SelectedIndex == 2) source = source.OrderBy(w => w.Name).ThenBy(w => w.TotalMass);
            else source = source.OrderBy(w => w.TotalMass).ThenBy(w => w.Name);
            Dictionary<string, ListViewGroup> groups = new Dictionary<string, ListViewGroup>();
            foreach (DonorWeapon weapon in source.Take(5000))
            {
                ListViewGroup group;
                if (!groups.TryGetValue(weapon.Category, out group))
                {
                    group = new ListViewGroup(weapon.Category, HorizontalAlignment.Left);
                    groups[weapon.Category] = group;
                    weaponList.Groups.Add(group);
                }
                ListViewItem item = new ListViewItem(weapon.Name, group);
                item.SubItems.Add(weapon.Category);
                item.SubItems.Add(weapon.Bullets.ToString(CultureInfo.InvariantCulture));
                item.SubItems.Add(weapon.TotalMass > 0 ? weapon.TotalMass.ToString("0.0", CultureInfo.InvariantCulture) + " kg" : "—");
                bool risky = injectionToggle.Checked && IsRiskyForSelectedPylon(weapon);
                item.SubItems.Add(risky ? "RISK" : (injectionToggle.Checked ? "INJECT" : "NATIVE"));
                item.Tag = weapon;
                if (weapon.Category == "Nuclear Weapons") item.ForeColor = Theme.Accent;
                else if (risky) item.ForeColor = Color.FromArgb(225, 142, 90);
                weaponList.Items.Add(item);
            }
            weaponList.EndUpdate();
        }

        private void AssignSelectedWeapon()
        {
            if (selectedPylon == null || weaponList.SelectedItems.Count == 0) return;
            DonorWeapon weapon = weaponList.SelectedItems[0].Tag as DonorWeapon;
            if (weapon == null) return;
            assignments[selectedPylon.Slot] = new PylonAssignment { Pylon = selectedPylon, Weapon = weapon, Injected = injectionToggle.Checked };
            RefreshPylons();
        }

        private bool IsRiskyForSelectedPylon(DonorWeapon weapon)
        {
            return selectedPylon != null && IsRiskyForPylon(selectedPylon, weapon);
        }

        private bool IsRiskyForPylon(PylonSlot pylon, DonorWeapon weapon)
        {
            if (pylon == null || weapon == null) return true;
            IEnumerable<DonorWeapon> native = nativeWeapons.Where(w => w.AircraftId == pylon.AircraftId && w.Slot == pylon.Slot);
            if (native.Any(w => String.Equals(w.Blk, weapon.Blk, StringComparison.OrdinalIgnoreCase) && w.Bullets == weapon.Bullets)) return false;
            if (!native.Any(w => String.Equals(w.Trigger, weapon.Trigger, StringComparison.OrdinalIgnoreCase))) return true;
            string path = (weapon.Blk ?? "").Replace('\\', '/').ToLowerInvariant();
            return path.Contains("/containers/") || path.Contains("/equipment/") || path.Contains("/payloadguns/");
        }

        private bool ConfirmRiskyLoadout()
        {
            Aircraft selected = SelectedAircraft;
            if (selected == null) return false;
            double total = assignments.Values.Sum(a => a.Weapon.TotalMass);
            if (selected.MaxLoad > 0 && total > selected.MaxLoad)
            {
                const string overload = "The configured weapon mass exceeds this vehicle's external load limit. Reduce the loadout before building the mission.";
                if (workspaceOperation) throw new InvalidOperationException(overload);
                MessageBox.Show(this, overload, "Loadout limit exceeded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            List<PylonAssignment> risky = assignments.Values.Where(a => a.Injected && IsRiskyForPylon(a.Pylon, a.Weapon)).OrderBy(a => a.Pylon.Order).ToList();
            if (risky.Count == 0) return true;
            string stations = String.Join(Environment.NewLine, risky.Take(8).Select(a => "• Station " + a.Pylon.Slot + " — " + a.Weapon.Name).ToArray());
            if (risky.Count > 8) stations += Environment.NewLine + "• …and " + (risky.Count - 8).ToString(CultureInfo.InvariantCulture) + " more";
            string message = "The game has no native mount of this weapon type on the selected stations:" + Environment.NewLine + Environment.NewLine + stations + Environment.NewLine + Environment.NewLine +
                "These Frankenstein mounts may work, but a structurally incompatible pylon can prevent the mission from loading. Build anyway?";
            if (WorkspaceConfirmation != null) return WorkspaceConfirmation("Injection Compatibility Warning", message);
            return MessageBox.Show(this, message, "Injection compatibility warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        private void ClearSelectedStation()
        {
            if (selectedPylon == null) return;
            assignments.Remove(selectedPylon.Slot);
            RefreshPylons();
        }

        private static string DetectGameFolder()
        {
            string applicationFolder = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string[] candidates =
            {
                SettingsStore.LoadGameFolder(),
                applicationFolder,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WarThunder"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Steam\steamapps\common\War Thunder"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"War Thunder")
            };
            foreach (string candidate in candidates)
                if (!String.IsNullOrWhiteSpace(candidate) && File.Exists(Path.Combine(candidate, "aces.vromfs.bin"))) return candidate;
            return applicationFolder;
        }

        private string ValidGameRoot()
        {
            string root = gameFolder.Text.Trim().Trim('"');
            if (!File.Exists(Path.Combine(root, "aces.vromfs.bin"))) throw new InvalidOperationException("The selected folder does not contain aces.vromfs.bin. Select the War Thunder root folder.");
            root = Path.GetFullPath(root);
            SettingsStore.SaveGameFolder(root);
            return root;
        }

        private void BrowseFolder()
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the War Thunder root folder";
                dialog.SelectedPath = Directory.Exists(gameFolder.Text) ? gameFolder.Text : "";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    gameFolder.Text = dialog.SelectedPath;
                    SettingsStore.SaveGameFolder(dialog.SelectedPath);
                }
            }
        }

        private static void WriteBytes(string path, byte[] data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".new";
            File.WriteAllBytes(temp, data);
            if (File.Exists(path))
            {
                string backup = path + ".bak";
                try { File.Replace(temp, path, backup, true); }
                catch { File.Copy(temp, path, true); File.Delete(temp); }
            }
            else File.Move(temp, path);
        }

        private void InstallBase(string root, bool overwrite)
        {
            string mission = Path.Combine(root, MissionFolderRelative, StarterMissionName);
            if (overwrite || !File.Exists(mission)) WriteBytes(mission, Embedded.Bytes("UTL.universal_test_lab.blk"));
            WriteEmbeddedWhenNeeded(Path.Combine(root, MissionFolderRelative, "usr.csv"), "UTL.usr.csv", overwrite);
            string obsoleteLocalization = Path.Combine(root, MissionFolderRelative, "usr_universal_test_lab.csv");
            if (File.Exists(obsoleteLocalization))
            {
                try { File.Delete(obsoleteLocalization); }
                catch { }
            }
            WriteEmbeddedWhenNeeded(Path.Combine(root, @"content\pkg_user\levels\Clean_Testdrive.bin"), "UTL.Clean_Testdrive.bin", overwrite);
            WriteEmbeddedWhenNeeded(Path.Combine(root, @"content\pkg_user\levels\Clean_Testdrive.blk"), "UTL.Clean_Testdrive.blk", overwrite);
            WriteEmbeddedWhenNeeded(Path.Combine(root, @"content\pkg_user\levels\Clean_Testdrive_map.png"), "UTL.Clean_Testdrive_map.png", overwrite);
            WriteEmbeddedWhenNeeded(Path.Combine(root, @"content\pkg_user\gameData\flightModels\utl_safe_player.blk"), "UTL.utl_safe_player.blk", overwrite);
            CleanLegacyMissionMenus(root);
        }

        private static void CleanLegacyMissionMenus(string root)
        {
            string userMissions = Path.Combine(root, "UserMissions");
            if (!Directory.Exists(userMissions)) return;
            foreach (string path in Directory.GetFiles(userMissions, "*.blk", SearchOption.AllDirectories))
            {
                string text;
                try { text = File.ReadAllText(path); }
                catch { continue; }
                if (text.IndexOf("UniversalTestLab", StringComparison.Ordinal) < 0 &&
                    text.IndexOf("CleanTestDrive", StringComparison.Ordinal) < 0 &&
                    text.IndexOf("chapter:t=\"TestDrive\"", StringComparison.Ordinal) < 0 &&
                    text.IndexOf("name:t=\"universal_test_lab\"", StringComparison.Ordinal) < 0) continue;
                string cleaned = BlkTools.CleanLegacyMenuKeys(text);
                if (!cleaned.Equals(text, StringComparison.Ordinal)) WriteBytes(path, new UTF8Encoding(false).GetBytes(cleaned));
            }
        }

        private static void WriteEmbeddedWhenNeeded(string path, string resource, bool overwrite)
        {
            if (overwrite || !File.Exists(path)) WriteBytes(path, Embedded.Bytes(resource));
        }

        private void InstallClicked()
        {
            try
            {
                InstallBase(ValidGameRoot(), true);
                SetStatus("Base mission and clean test range installed.", false);
                MessageBox.Show(this, "Base mission installed. Close the User Missions tab in War Thunder and open it again; no game restart is required.", "Universal Test Lab", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private static string ExtractResourceTool()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniversalTestLab", "tools");
            Directory.CreateDirectory(dir);
            string exe = Path.Combine(dir, "wt_ext_cli.exe");
            WriteBytes(exe, Embedded.Bytes("UTL.wt_ext_cli.exe"));
            WriteBytes(Path.Combine(dir, "WT_EXT_LICENSE.txt"), Embedded.Bytes("UTL.WT_EXT_LICENSE.txt"));
            return exe;
        }

        internal static string NormalizeGameResourcePath(string relative)
        {
            if (String.IsNullOrWhiteSpace(relative)) throw new ArgumentException("Game resource path cannot be empty.", "relative");
            return relative.Trim().Replace('\\', '/').TrimStart('/').ToLowerInvariant();
        }

        internal static string ExtractGameBlk(string root, string relative)
        {
            string normalizedRelative = NormalizeGameResourcePath(relative);
            // Zero-extract fast path: a full pre-extracted game-data tree laid out as
            // <tree>/aces.vromfs.bin_u/gamedata/... (the project's universal_units_data /
            // universal_weapons_data extraction). Reading it avoids launching wt_ext_cli
            // per uncached resource - the per-weapon-swap stall.
            foreach (string tree in FindPreExtractedTrees())
            {
                string candidate = Path.Combine(tree, "aces.vromfs.bin_u", normalizedRelative.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate)) return candidate;
            }
            // Deterministic per-resource cache directory (stable across calls
            // and sessions) so selecting vehicles / building missions does not
            // re-launch wt_ext_cli for the same game resource every time.
            string cacheRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniversalTestLab", "cache");
            string cacheDir = Path.Combine(cacheRoot, "r" + GetStableHash(normalizedRelative).ToString("x8"));
            string resultPath = Path.Combine(cacheDir, "aces.vromfs.bin_u", normalizedRelative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(resultPath)) return resultPath;
            Directory.CreateDirectory(cacheDir);
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = ExtractResourceTool(),
                Arguments = "unpack_vromf --input_dir_or_file \"" + Path.Combine(root, "aces.vromfs.bin") + "\" --output_dir \"" + cacheDir + "\" --format BlkText --folder \"" + normalizedRelative + "\" --continue Quiet",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (Process process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0) throw new InvalidOperationException("Could not read game resource: " + normalizedRelative + Environment.NewLine + output + Environment.NewLine + error);
            }
            if (!File.Exists(resultPath))
                throw new FileNotFoundException("The War Thunder folder is valid, but this game resource was not found after extraction:" + Environment.NewLine + normalizedRelative, resultPath);
            return resultPath;
        }

        private static string[] preExtractedTrees = null;
        // Project-local full vromfs extractions (universal_units_data for gamedata/units,
        // universal_weapons_data for gamedata/weapons). Both may sit next to the exe's
        // parent (project root) or in the exe folder itself. Auto-detected once.
        private static string[] FindPreExtractedTrees()
        {
            if (preExtractedTrees != null) return preExtractedTrees;
            preExtractedTrees = new string[0];
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                List<string> found = new List<string>();
                string[] roots =
                {
                    Path.GetFullPath(Path.Combine(baseDir, "..")),
                    Path.GetFullPath(Path.Combine(baseDir, "..", "..")),
                    Path.GetFullPath(baseDir)
                };
                string[] names = { "universal_units_data", "universal_weapons_data" };
                foreach (string root in roots)
                {
                    foreach (string name in names)
                    {
                        string candidate = Path.Combine(root, name);
                        try
                        {
                            if (Directory.Exists(Path.Combine(candidate, "aces.vromfs.bin_u", "gamedata")) && !found.Contains(candidate))
                                found.Add(candidate);
                        }
                        catch { }
                    }
                }
                preExtractedTrees = found.ToArray();
            }
            catch { }
            return preExtractedTrees;
        }

        private static uint GetStableHash(string value)
        {
            if (value == null) return 0;
            uint hash = 2166136261;
            foreach (char c in value) { hash ^= c; hash *= 16777619; }
            return hash;
        }

    }

}
