// ModernShell.MainWindow.Ground.cs
// Ground workspace UI: cannon slots, ammo groups, presets (segment 2/5).
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

    }
}
