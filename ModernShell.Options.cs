// ModernShell.Options.cs
// Mission options, flight configure/systems windows and preset/about dialogs.
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
    internal sealed class MissionOptionsPanel : StackPanel
    {
        private readonly MissionSettings original;
        private Slider respawnSlider;
        private Slider targetSlider;
        private Slider rearmSlider;
        private CheckBox rearmOverrideBox;
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
            rearmOverrideBox = new CheckBox { Content = ModernText.L("Override ground rearm time (writes rearmTimeOnField)", "改写战场补给时间（写入 rearmTimeOnField）"), IsChecked = original.RearmOverride, Foreground = ModernPalette.Brush(ModernPalette.Text), Margin = new Thickness(0, 8, 0, 0) };
            rearmOverrideBox.Checked += delegate { rearmSlider.IsEnabled = true; };
            rearmOverrideBox.Unchecked += delegate { rearmSlider.IsEnabled = false; };
            rearmSlider.IsEnabled = original.RearmOverride;
            this.Children.Add(rearmOverrideBox);
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
            updated.RearmOverride = rearmOverrideBox.IsChecked == true;
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
        private readonly CheckBox rearmOverrideBox;
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
            rearmOverrideBox = new CheckBox { Content = ModernText.L("Override ground rearm time (writes rearmTimeOnField)", "改写战场补给时间（写入 rearmTimeOnField）"), IsChecked = original.RearmOverride, Foreground = ModernPalette.Brush(ModernPalette.Text), Margin = new Thickness(0, 8, 0, 0) };
            rearmOverrideBox.Checked += delegate { rearmSlider.IsEnabled = true; };
            rearmOverrideBox.Unchecked += delegate { rearmSlider.IsEnabled = false; };
            rearmSlider.IsEnabled = original.RearmOverride;
            content.Children.Add(rearmOverrideBox);
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
            updated.RearmOverride = rearmOverrideBox.IsChecked == true;
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
}
