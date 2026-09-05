// UniversalTestLab.MainForm.cs
// State fields, workspace query API, catalog loaders (segment 1/5).
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
    internal sealed partial class MainForm : Form
    {
        private readonly List<Aircraft> aircraft = new List<Aircraft>();
        private readonly List<TargetUnit> groundTargets = new List<TargetUnit>();
        private readonly List<TargetUnit> shipTargets = new List<TargetUnit>();
    private List<DonorWeapon> nativeWeaponsBacking;
    private List<DonorWeapon> nativeWeapons
    {
        get { if (nativeWeaponsBacking == null) { LoadDonorWeaponsCatalog(); } return nativeWeaponsBacking; }
    }
        private readonly List<DonorWeapon> globalWeapons = new List<DonorWeapon>();
        private readonly List<KeyValuePair<string, string>> navalCannons = new List<KeyValuePair<string, string>>();
        private readonly List<KeyValuePair<string, string>> airOrdnance = new List<KeyValuePair<string, string>>();
    private List<UnitWeapon> unitWeaponsBacking;
    private List<UnitWeapon> unitWeapons
    {
        get { if (unitWeaponsBacking == null) { LoadUnitWeaponsCatalog(); } return unitWeaponsBacking; }
    }
    private List<AircraftModification> modificationsBacking;
    private List<AircraftModification> modifications
    {
        get { if (modificationsBacking == null) { LoadModificationsCatalog(); } return modificationsBacking; }
    }
        private readonly List<GroundAmmo> groundAmmo = new List<GroundAmmo>();
        private readonly List<CombinedMap> combinedMaps = new List<CombinedMap>();
        private readonly List<PylonSlot> pylons = new List<PylonSlot>();
        private readonly Dictionary<int, PylonAssignment> assignments = new Dictionary<int, PylonAssignment>();
        private readonly Dictionary<int, Button> pylonButtons = new Dictionary<int, Button>();
        private readonly Dictionary<string, AircraftSettings> aircraftSettings = new Dictionary<string, AircraftSettings>(StringComparer.OrdinalIgnoreCase);

        private TextBox gameFolder;
        private TextBox aircraftSearch;
        private ComboBox nationFilter;
        private ComboBox rankFilter;
        private ComboBox vehicleFilter;
        private ListBox aircraftList;
        private AircraftPreview preview;
        private FlowLayoutPanel pylonStrip;
        private Label massLabel;
        private Label stationLabel;
        private CheckBox injectionToggle;
        private TextBox weaponSearch;
        private ComboBox categoryFilter;
        private ComboBox weaponNationFilter;
        private ComboBox sortFilter;
        private ListView weaponList;
        private ComboBox airTargetBox;
        private ComboBox groundTargetBox;
        private ComboBox shipTargetBox;
        private NumericUpDown airCount;
        private NumericUpDown groundCount;
        private NumericUpDown shipCount;
        private CheckBox hostileGround;
        private CheckBox samSites;
        private string pendingSamMode = "active";
        private string pendingSamSelection = "all";
        private Label status;
        private Button aircraftSettingsButton;
        private PylonSlot selectedPylon;
        private bool suppressSuccessDialog;
        private bool lastGenerationSucceeded;
        private bool workspaceOperation;
        private Exception workspaceLastError;
        private List<string> workspaceGroundTargetOverrides;
        private List<FlyingTargetSlot> workspaceFlyingTargets;
        private bool workspacePassiveShip;
        private CombinedScenarioSettings workspaceCombinedScenario;

        private const string MissionFolderRelative = @"UserMissions\Universal Test Lab";
        private const string StarterMissionName = "universal_test_lab_start.blk";
        internal const string HotMissionName = "universal_test_lab_hot.blk";
        // War Thunder only accepts player-controlled custom ground units through one of
        // the reserve-tank proxy names in the root userVehicles directory. Nested,
        // tokenized class names can render and drive, but their weapon controllers are
        // not registered as a playable tank.
        internal const string GroundProxyClassId = "userVehicles/ussr_t_26_1940";
        private const string GroundProxyVehicleFileName = "ussr_t_26_1940.blk";

        public MainForm()
        {
            Text = "Universal Test Lab — Mission Studio";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1240, 780);
            Size = new Size(1500, 920);
            BackColor = Theme.Window;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.2f);
            LoadCatalogs();
            LoadAircraftSettings();
            MissionSettings.Load();
            BuildUi();
            gameFolder.Text = DetectGameFolder();
            SelectDefaults();
        }

        internal IList<Aircraft> WorkspaceAircraft { get { return aircraft; } }
        internal IList<TargetUnit> WorkspaceGroundTargets { get { return groundTargets; } }
        internal IList<TargetUnit> WorkspaceShipTargets { get { return shipTargets; } }
        private readonly Dictionary<string, int> groundCannonAmmoCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GroundWeaponCacheData> groundWeaponCacheMap = new Dictionary<string, GroundWeaponCacheData>(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, GroundWeaponCacheData> prebuiltGroundWeaponsBacking;
    internal static Dictionary<string, GroundWeaponCacheData> prebuiltGroundWeapons
    {
        get { if (prebuiltGroundWeaponsBacking == null) { prebuiltGroundWeaponsBacking = LoadPrebuiltGroundWeapons(); } return prebuiltGroundWeaponsBacking; }
    }

        internal static Dictionary<string, GroundWeaponCacheData> LoadPrebuiltGroundWeapons()
        {
            Dictionary<string, GroundWeaponCacheData> result = new Dictionary<string, GroundWeaponCacheData>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string jsonText = Embedded.Text("UTL.vehicle_weapons.json");
                if (String.IsNullOrWhiteSpace(jsonText)) return result;
                System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                serializer.MaxJsonLength = int.MaxValue;
                Dictionary<string, GroundWeaponCacheJson> data = serializer.Deserialize<Dictionary<string, GroundWeaponCacheJson>>(jsonText);
                if (data == null) return result;
                foreach (KeyValuePair<string, GroundWeaponCacheJson> kv in data)
                {
                    GroundWeaponCacheJson src = kv.Value;
                    if (src == null) continue;
                    GroundWeaponCacheData dst = new GroundWeaponCacheData();
                    if (src.weapons != null)
                    {
                        List<GroundWeaponInfo> weapons = new List<GroundWeaponInfo>();
                        foreach (GroundWeaponInfoJson w in src.weapons)
                        {
                            if (w == null || String.IsNullOrWhiteSpace(w.blk)) continue;
                            weapons.Add(new GroundWeaponInfo { Trigger = w.trigger ?? "", Blk = w.blk, NativeAmmo = w.nativeAmmo });
                        }
                        dst.Weapons = weapons;
                    }
                    if (src.missiles != null)
                    {
                        List<KeyValuePair<string, string>> missiles = new List<KeyValuePair<string, string>>();
                        foreach (MissileInfoJson m in src.missiles)
                        {
                            if (m == null) continue;
                            missiles.Add(new KeyValuePair<string, string>(m.name ?? "", m.blk ?? ""));
                        }
                        dst.Missiles = missiles;
                    }
                    if (src.beltOptions != null)
                    {
                        List<GroundWeaponBeltOption> belts = new List<GroundWeaponBeltOption>();
                        foreach (GroundWeaponBeltJson bj in src.beltOptions)
                        {
                            if (bj == null || String.IsNullOrWhiteSpace(bj.name)) continue;
                            GroundWeaponBeltOption bo = new GroundWeaponBeltOption { Name = bj.name, Calibre = bj.calibre };
                            if (bj.rounds != null)
                            {
                                List<GroundAmmo> rounds = new List<GroundAmmo>();
                                foreach (GroundWeaponRoundJson rj in bj.rounds)
                                {
                                    if (rj == null || String.IsNullOrWhiteSpace(rj.bulletName)) continue;
                                    rounds.Add(new GroundAmmo { Container = bj.name, BulletName = rj.bulletName, Display = rj.display ?? "", Type = rj.kind ?? "", Mass = rj.mass, Speed = rj.speed, ExplosiveMass = rj.explosive, Caliber = rj.caliber, Penetration = rj.penetration });
                                }
                                bo.Rounds = rounds;
                            }
                            belts.Add(bo);
                        }
                        dst.BeltOptions = belts;
                    }
                    if (src.rackRounds != null)
                    {
                        foreach (KeyValuePair<string, int> r in src.rackRounds) dst.RackRounds[r.Key] = r.Value;
                    }
                    if (src.beltSizes != null)
                    {
                        foreach (KeyValuePair<string, int> b in src.beltSizes) dst.BeltSizes[b.Key] = b.Value;
                    }
                    dst.BeltTypeLimit = src.beltTypeLimit > 1 ? src.beltTypeLimit : 1;
                    result[kv.Key] = dst;
                }
            }
            catch { }
            return result;
        }

        internal GroundWeaponCacheData WorkspaceGetGroundWeaponCache(Aircraft target)
        {
            if (target == null || String.IsNullOrWhiteSpace(target.Id)) return null;
            GroundWeaponCacheData cache;
            if (groundWeaponCacheMap.TryGetValue(target.Id, out cache)) return cache;
            cache = new GroundWeaponCacheData();
            GroundWeaponCacheData prebuilt = null;
            if (prebuiltGroundWeapons != null && prebuiltGroundWeapons.TryGetValue(target.Id, out prebuilt) && prebuilt != null)
            {
                cache.Weapons = prebuilt.Weapons ?? new List<GroundWeaponInfo>();
                cache.Missiles = prebuilt.Missiles ?? new List<KeyValuePair<string, string>>();
                cache.BeltOptions = prebuilt.BeltOptions ?? new List<GroundWeaponBeltOption>();
                foreach (KeyValuePair<string, int> r in prebuilt.RackRounds) cache.RackRounds[r.Key] = r.Value;
                foreach (KeyValuePair<string, int> b in prebuilt.BeltSizes) cache.BeltSizes[b.Key] = b.Value;
                cache.BeltTypeLimit = prebuilt.BeltTypeLimit > 1 ? prebuilt.BeltTypeLimit : 1;
            }
            else
            {
                cache.Weapons = WorkspaceGroundWeaponsUncached(target);
                cache.Missiles = WorkspaceVehicleMissilesUncached(target);
                cache.BeltOptions = WorkspaceGunBeltOptionsUncached(target);
            }
            groundWeaponCacheMap[target.Id] = cache;
            return cache;
        }

        internal int WorkspaceRackRoundsCached(GroundWeaponCacheData cache, string blk)
        {
            if (cache == null) return WorkspaceRackRounds(blk);
            string key = blk ?? String.Empty;
            int rounds;
            if (cache.RackRounds.TryGetValue(key, out rounds)) return rounds;
            rounds = WorkspaceRackRounds(blk);
            cache.RackRounds[key] = rounds;
            return rounds;
        }

        internal IList<GroundWeaponInfo> WorkspaceGroundWeapons(Aircraft target)
        {
            GroundWeaponCacheData cache = WorkspaceGetGroundWeaponCache(target);
            return cache == null ? new List<GroundWeaponInfo>() : cache.Weapons;
        }

        private IList<GroundWeaponInfo> WorkspaceGroundWeaponsUncached(Aircraft target)
        {
            List<GroundWeaponInfo> result = new List<GroundWeaponInfo>();
            if (target == null) return result;
            try
            {
                string unitBlk = ExtractGameBlk(gameFolder.Text, "gamedata/units/tankmodels/" + target.Id.ToLowerInvariant() + ".blk");
                string native = File.ReadAllText(unitBlk, Encoding.UTF8);
                List<string> seen = new List<string>();
                string lastKey = null;
                foreach (BlockSpan w in BlkTools.Blocks(native, "Weapon"))
                {
                    string trigger = BlkTools.Field(w.Text, "trigger", "t");
                    if (String.IsNullOrWhiteSpace(trigger)) continue;
                    string blk = BlkTools.Field(w.Text, "blk", "t");
                    if (String.IsNullOrWhiteSpace(blk)) continue;
                    string key = (trigger + "|" + NormalizeGameResourcePath(blk)).ToLowerInvariant();
                    if (key.Equals(lastKey, StringComparison.OrdinalIgnoreCase) && result.Count > 0)
                    {
                        // Consecutive identical (trigger, blk) Weapon blocks are multi-mount
                        // weapons (e.g. quad M2 on M16): accumulate the native ammo.
                        int extraAmmo = 0;
                        Match m0 = Regex.Match(w.Text, @"(?m)^\s*bullets:i\s*=\s*(-?[0-9]+)");
                        if (m0.Success) Int32.TryParse(m0.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out extraAmmo);
                        result[result.Count - 1].NativeAmmo += Math.Max(0, extraAmmo);
                        continue;
                    }
                    lastKey = key;
                    if (seen.Any(x => x.Equals(key, StringComparison.OrdinalIgnoreCase))) continue;
                    seen.Add(key);
                    string file = String.IsNullOrWhiteSpace(blk) ? "" : blk.Substring(blk.LastIndexOf('/') + 1).Replace("_user_cannon", "").Replace("_user_machinegun", "").Replace(".blk", "").Replace('_', ' ');
                    int nativeAmmo = 0;
                    Match m = Regex.Match(w.Text, @"(?m)^\s*bullets:i\s*=\s*(-?[0-9]+)");
                    if (m.Success) Int32.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out nativeAmmo);
                    if (nativeAmmo <= 0 && !String.IsNullOrWhiteSpace(blk))
                    {
                        string cannonKey = NormalizeGameResourcePath(blk);
                        if (!groundCannonAmmoCache.TryGetValue(cannonKey, out nativeAmmo))
                        {
                            try
                            {
                                string cannonText = File.ReadAllText(ExtractGameBlk(gameFolder.Text, cannonKey), Encoding.UTF8);
                                Match cm = Regex.Match(cannonText, @"(?m)^\s*bullets:i\s*=\s*(-?[0-9]+)");
                                if (cm.Success) Int32.TryParse(cm.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out nativeAmmo);
                            }
                            catch { nativeAmmo = 0; }
                            groundCannonAmmoCache[cannonKey] = nativeAmmo;
                        }
                    }
                    result.Add(new GroundWeaponInfo { Trigger = trigger, Blk = blk, NativeAmmo = nativeAmmo, Display = (trigger.Equals("gunner0", StringComparison.OrdinalIgnoreCase) ? "PRIMARY" : trigger.ToUpperInvariant()) + " — " + file });
                }
            }
            catch { }
            return result;
        }
        public int WorkspaceRackRounds(string blk)
        {
            if (String.IsNullOrWhiteSpace(blk)) return 1;
            try
            {
                string launcherText = File.ReadAllText(ExtractGameBlk(gameFolder.Text, blk.Replace('\\', '/').TrimStart('/')), Encoding.UTF8);
                Match rackMatch = Regex.Match(launcherText, @"(?m)^\s*bullets:i\s*=\s*(\d+)\s*$");
                if (rackMatch.Success)
                {
                    int v = Int32.Parse(rackMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    if (v > 1) return v;
                }
            }
            catch { }
            return 1;
        }

public IList<GroundAmmo> WorkspaceResolveCannonAmmo(string cannonBlk)
        {
            List<GroundAmmo> result = new List<GroundAmmo>();
            if (String.IsNullOrWhiteSpace(cannonBlk)) return result;
            try
            {
                string cannonPath = NormalizeGameResourcePath(cannonBlk);
                string cannon = File.ReadAllText(ExtractGameBlk(gameFolder.Text, cannonPath), Encoding.UTF8);
                foreach (BlockSpan group in BlkTools.RootBlocks(cannon))
                {
                    string blockName = BlkTools.BlockName(group);
                    if (blockName.Equals("bullet", StringComparison.OrdinalIgnoreCase))
                    {
                        string bname = BlkTools.Field(group.Text, "bulletName", "t");
                        if (!String.IsNullOrWhiteSpace(bname))
                            result.Add(new GroundAmmo { SourceBlk = cannonBlk, BulletName = bname, Display = (bname.Replace('_', ' ')).Trim(), Type = "injected" });
                    }
                    else
                    {
                        List<BlockSpan> bullets = BlkTools.Blocks(group.Text, "bullet");
                        if (bullets.Count > 1)
                        {
                            result.Add(new GroundAmmo { SourceBlk = cannonBlk, BulletName = blockName, Display = (blockName.Replace('_', ' ')).Trim() + " (belt)", Type = "injected" });
                            foreach (BlockSpan pr in bullets)
                            {
                                string bname = BlkTools.Field(pr.Text, "bulletName", "t");
                                if (!String.IsNullOrWhiteSpace(bname))
                                    result.Add(new GroundAmmo { SourceBlk = cannonBlk, BulletName = bname, Display = (bname.Replace('_', ' ')).Trim(), Type = "injected" });
                            }
                        }
                        else if (bullets.Count == 1)
                        {
                            string bname = BlkTools.Field(bullets[0].Text, "bulletName", "t");
                            if (!String.IsNullOrWhiteSpace(bname))
                                result.Add(new GroundAmmo { SourceBlk = cannonBlk, BulletName = bname, Display = (bname.Replace('_', ' ')).Trim(), Type = "injected" });
                        }
                    }
                }
            }
            catch { }
            return result;
        }
        // Missile weapon presets of a ground vehicle (preset name + launcher blk),
        // extracted from the vehicle's weapon_presets tree. The workspace UI uses
        // this to list missiles by their native preset name - mission ammo slots
        // accept preset names (170mm_57e6_aam), not raw launcher bullet names.
        internal IList<KeyValuePair<string, string>> WorkspaceVehicleMissiles(Aircraft target)
        {
            GroundWeaponCacheData cache = WorkspaceGetGroundWeaponCache(target);
            return cache == null ? new List<KeyValuePair<string, string>>() : cache.Missiles;
        }

        private IList<KeyValuePair<string, string>> WorkspaceVehicleMissilesUncached(Aircraft target)
        {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            if (target == null || String.IsNullOrWhiteSpace(target.Id)) return result;
            try
            {
                string native = File.ReadAllText(ExtractGameBlk(gameFolder.Text, "gamedata/units/tankModels/" + target.Id.ToLowerInvariant() + ".blk"), Encoding.UTF8);
                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (BlockSpan pylon in BlkTools.Blocks(native, "WeaponPilons"))
                {
                    foreach (BlockSpan slot in BlkTools.Blocks(pylon.Text, "WeaponSlot"))
                    {
                        foreach (BlockSpan wp in BlkTools.Blocks(slot.Text, "WeaponPreset"))
                        {
                            string presetName = BlkTools.Field(wp.Text, "name", "t");
                            if (String.IsNullOrWhiteSpace(presetName)) continue;
                            foreach (BlockSpan weapon in BlkTools.Blocks(wp.Text, "Weapon"))
                            {
                                string weaponBlk = BlkTools.Field(weapon.Text, "blk", "t");
                                if (String.IsNullOrWhiteSpace(weaponBlk)) continue;
                                string key = NormalizeGameResourcePath(weaponBlk) + "|" + presetName;
                                if (seen.Add(key))
                                    result.Add(new KeyValuePair<string, string>(presetName, weaponBlk));
                            }
                        }
                    }
                }
            }
            catch { }
            return result;
        }

        // Belt (gun) ammunition options of a ground vehicle - the game exposes one
        // empty modification module per belt type (e.g. 30mm_2a38_HE, 30mm_2a42_AP
        // on Pantsir-SM-SV). Ask3lad lists these modification names as the vehicle's
        // ammo and the mission slots accept them directly (bullets0:t="30mm_2a42_AP").
        internal IList<GroundWeaponBeltOption> WorkspaceGunBeltOptions(Aircraft target)
        {
            GroundWeaponCacheData cache = WorkspaceGetGroundWeaponCache(target);
            return cache == null ? new List<GroundWeaponBeltOption>() : cache.BeltOptions;
        }

        private IList<GroundWeaponBeltOption> WorkspaceGunBeltOptionsUncached(Aircraft target)
        {
            List<GroundWeaponBeltOption> result = new List<GroundWeaponBeltOption>();
            if (target == null || String.IsNullOrWhiteSpace(target.Id)) return result;
            try
            {
                string native = File.ReadAllText(ExtractGameBlk(gameFolder.Text, "gamedata/units/tankModels/" + target.Id.ToLowerInvariant() + ".blk"), Encoding.UTF8);
                BlockSpan mods = BlkTools.FirstBlock(native, "modifications", 0);
                if (mods != null)
                    foreach (BlockSpan module in BlkTools.DirectChildBlocks(mods.Text))
                    {
                        string name = BlkTools.BlockName(module);
                        if (String.IsNullOrWhiteSpace(name)) continue;
                        if (!Regex.IsMatch(name, @"^\d+mm_", RegexOptions.IgnoreCase)) continue;
                        if (name.IndexOf("_ammo_pack", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                        if (BlkTools.DirectChildBlocks(module.Text).Count > 0) continue;
                        if (!result.Any(x => String.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
                            result.Add(new GroundWeaponBeltOption { Name = name });
                    }
            }
            catch { }
            return result;
        }

        internal IList<AircraftModification> WorkspaceModifications { get { return modifications; } }
        internal IList<GroundAmmo> WorkspaceGroundAmmo { get { return groundAmmo; } }
        internal IList<KeyValuePair<string, string>> WorkspaceNavalCannons { get { return navalCannons; } }
        internal IList<KeyValuePair<string, string>> WorkspaceAircraftCannons { get { return airOrdnance; } }
        internal IList<UnitWeapon> WorkspaceUnitWeapons { get { return unitWeapons; } }
        internal IList<CombinedMap> WorkspaceCombinedMaps { get { return combinedMaps; } }
        internal string WorkspaceGameFolder
        {
            get { return gameFolder.Text; }
            set
            {
                string selected = (value ?? "").Trim().Trim('"');
                gameFolder.Text = selected;
                groundWeaponCacheMap.Clear();
                if (!String.IsNullOrWhiteSpace(selected) && Directory.Exists(selected)) SettingsStore.SaveGameFolder(selected);
            }
        }
        internal Func<string, string, bool> WorkspaceConfirmation { get; set; }
        internal Aircraft WorkspaceSelectedAircraft { get { return SelectedAircraft; } }

        internal string WorkspaceBrowseFolder(string current, IntPtr ownerHandle)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the War Thunder root folder";
                string initial = (current ?? "").Trim().Trim('"');
                if (!Directory.Exists(initial)) initial = Directory.Exists(gameFolder.Text) ? gameFolder.Text : "";
                dialog.SelectedPath = initial;
                DialogResult answer = ownerHandle == IntPtr.Zero ? dialog.ShowDialog() : dialog.ShowDialog(new WindowHandleOwner(ownerHandle));
                if (answer != DialogResult.OK || String.IsNullOrWhiteSpace(dialog.SelectedPath)) return current ?? gameFolder.Text;
                WorkspaceGameFolder = Path.GetFullPath(dialog.SelectedPath);
                return gameFolder.Text;
            }
        }

        internal void WorkspaceSyncBase()
        {
            InstallBase(ValidGameRoot(), true);
            SetStatus("Base mission and clean test range installed.", false);
        }

        internal void WorkspaceOpenMissions()
        {
            string path = Path.Combine(ValidGameRoot(), MissionFolderRelative);
            Directory.CreateDirectory(path);
            Process.Start("explorer.exe", "\"" + path + "\"");
        }

        internal bool WorkspaceSelectAircraft(string id)
        {
            Aircraft target = aircraft.FirstOrDefault(x => x.Id.Equals(id ?? "", StringComparison.OrdinalIgnoreCase));
            if (target == null) return false;
            if (!aircraftList.Items.Cast<object>().OfType<Aircraft>().Any(x => x.Id.Equals(target.Id, StringComparison.OrdinalIgnoreCase)))
            {
                aircraftSearch.Text = "";
                nationFilter.SelectedIndex = 0;
                rankFilter.SelectedIndex = 0;
                vehicleFilter.SelectedIndex = 0;
                FilterAircraft();
            }
            aircraftList.SelectedItem = aircraftList.Items.Cast<object>().OfType<Aircraft>().FirstOrDefault(x => x.Id.Equals(target.Id, StringComparison.OrdinalIgnoreCase));
            return SelectedAircraft != null && SelectedAircraft.Id.Equals(target.Id, StringComparison.OrdinalIgnoreCase);
        }

        internal List<PylonSlot> WorkspacePylons(string aircraftId)
        {
            return pylons.Where(x => x.AircraftId.Equals(aircraftId ?? "", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Order).ThenBy(x => x.Slot).ToList();
        }

        internal List<DonorWeapon> WorkspaceWeapons(string aircraftId, int slot, bool injected, string search, string category, string nation, int sort)
        {
            IEnumerable<DonorWeapon> source = injected
                ? globalWeapons
                : nativeWeapons.Where(w => w.AircraftId.Equals(aircraftId ?? "", StringComparison.OrdinalIgnoreCase) && w.Slot == slot)
                    .GroupBy(w => w.Blk + "|" + w.Bullets).Select(g => g.First());
            if (!String.IsNullOrWhiteSpace(search))
                source = source.Where(w => w.Name.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 || w.Category.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 || w.Blk.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!String.IsNullOrWhiteSpace(category) && !category.Equals("All Weapon Types", StringComparison.OrdinalIgnoreCase))
                source = source.Where(w => w.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            if (!String.IsNullOrWhiteSpace(nation) && !nation.Equals("All Nations", StringComparison.OrdinalIgnoreCase))
                source = source.Where(w => (w.Nations ?? "").Split('|').Any(n => n.Equals(nation, StringComparison.OrdinalIgnoreCase)));
            if (sort == 1) source = source.OrderByDescending(w => w.TotalMass).ThenBy(w => w.Name);
            else if (sort == 2) source = source.OrderBy(w => w.Name).ThenBy(w => w.TotalMass);
            else source = source.OrderBy(w => w.TotalMass).ThenBy(w => w.Name);
            return source.ToList();
        }

        internal IEnumerable<string> WorkspaceWeaponCategories
        {
            get { return globalWeapons.Select(w => w.Category).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x); }
        }

        internal IEnumerable<string> WorkspaceNations
        {
            get { return aircraft.Select(a => a.Nation).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x); }
        }

        internal Dictionary<int, PylonAssignment> WorkspaceAssignments
        {
            get { return assignments.ToDictionary(x => x.Key, x => x.Value); }
        }

        internal bool WorkspaceAssignWeapon(int slot, DonorWeapon weapon, bool injected)
        {
            Aircraft selected = SelectedAircraft;
            PylonSlot pylon = selected == null ? null : pylons.FirstOrDefault(x => x.AircraftId.Equals(selected.Id, StringComparison.OrdinalIgnoreCase) && x.Slot == slot);
            if (pylon == null || weapon == null) return false;
            if (injected && (IsRiskyForPylon(pylon, weapon) || IsPresetStylePylon(pylon)))
            {
                bool legacyPresetStyle = IsPresetStylePylon(pylon);
                string warning = legacyPresetStyle
                    ? "This legacy aircraft has no native pylon tree, so the injected weapon will replace every store of the whole loadout scheme (the aircraft can carry only one scheme at a time). Display and firing behaviour depend on the model's store hooks and are experimental.\r\n\r\nInject it anyway?"
                    : "This injected weapon exceeds the known station mass or uses a mount that may be incompatible. War Thunder may reject the generated aircraft.\r\n\r\nMount it anyway?";
                bool accepted = WorkspaceConfirmation != null
                    ? WorkspaceConfirmation("Experimental Injection", warning)
                    : MessageBox.Show(this, warning, "Experimental injection", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
                if (!accepted) return false;
            }
            assignments[slot] = new PylonAssignment { Pylon = pylon, Weapon = weapon, Injected = injected };
            selectedPylon = pylon;
            RefreshPylons();
            return true;
        }

        internal static bool IsPresetStylePylon(PylonSlot pylon)
        {
            // Legacy aircraft have no WeaponSlot tree; their catalog rows are a
            // single scheme station (slot 0, no load limit) whose anchors are
            // whole preset names instead of pylon mounts.
            return pylon != null && pylon.Slot == 0 && pylon.MaxLoad == 0;
        }

        internal void WorkspaceClearStation(int slot)
        {
            assignments.Remove(slot);
            RefreshPylons();
        }

        internal void WorkspaceClearAll()
        {
            assignments.Clear();
            RefreshPylons();
        }

        internal AircraftSettings WorkspaceGetSettings(Aircraft item) { return GetAircraftSettings(item).Copy(); }

        internal void WorkspaceSetSettings(Aircraft item, AircraftSettings value)
        {
            if (item == null || value == null) return;
            aircraftSettings[item.Id] = value.Copy();
            PersistAircraftSettings();
            UpdateAircraftSettingsButton();
        }

        internal IList<CountermeasureLauncher> WorkspaceCountermeasureLaunchers(Aircraft item)
        {
            List<CountermeasureLauncher> result = new List<CountermeasureLauncher>();
            if (item == null) return result;
            try
            {
                string root = gameFolder.Text.Trim().Trim('"');
                if (!File.Exists(Path.Combine(root, "aces.vromfs.bin"))) throw new FileNotFoundException();
                string fm = File.ReadAllText(ExtractGameBlk(root, "gamedata/flightmodels/" + item.Id + ".blk"), Encoding.UTF8);
                // Aircraft that carry countermeasure upgrade modules (chaff launchers,
                // belt packs, ...) get a mixed flare/chaff loadout once fully upgraded,
                // while their stock launcher files are flare-only. Expose a chaff
                // slider whenever such modules exist so the flare/chaff ratio can be
                // configured even though the stock paths do not contain "with_chaff".
                bool countermeasureUpgrades = HasCountermeasureUpgradeModules(fm);
                int anonymous = 0;
                foreach (BlockSpan weapon in BlkTools.Blocks(fm, "Weapon"))
                {
                    if (!String.Equals(BlkTools.Field(weapon.Text, "trigger", "t"), "countermeasures", StringComparison.OrdinalIgnoreCase)) continue;
                    string path = BlkTools.Field(weapon.Text, "blk", "t") ?? "";
                    string emitter = BlkTools.Field(weapon.Text, "emitter", "t");
                    string key = String.IsNullOrWhiteSpace(emitter) ? "launcher-" + (++anonymous).ToString(CultureInfo.InvariantCulture) : emitter;
                    Match roundsMatch = Regex.Match(weapon.Text, @"(?m)^\s*bullets:i\s*=\s*(\d+)");
                    int rounds = roundsMatch.Success ? Int32.Parse(roundsMatch.Groups[1].Value, CultureInfo.InvariantCulture) : 30;
                    bool chaffOnly = path.IndexOf("chaff_only", StringComparison.OrdinalIgnoreCase) >= 0;
                    bool flareOnly = !chaffOnly && !countermeasureUpgrades && path.IndexOf("with_chaff", StringComparison.OrdinalIgnoreCase) < 0 &&
                        path.IndexOf("maw", StringComparison.OrdinalIgnoreCase) < 0 && path.IndexOf("bol", StringComparison.OrdinalIgnoreCase) < 0;
                    CountermeasureLauncher launcher = result.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
                    if (launcher == null)
                    {
                        string identity = path + " " + key;
                        string kind = identity.IndexOf("bol", StringComparison.OrdinalIgnoreCase) >= 0 ? "BOL COUNTERMEASURE DISPENSER" :
                            identity.IndexOf("bko", StringComparison.OrdinalIgnoreCase) >= 0 ? "BKO COUNTERMEASURE DISPENSER" :
                            identity.IndexOf("maw", StringComparison.OrdinalIgnoreCase) >= 0 ? "MAW COUNTERMEASURE DISPENSER" :
                            identity.IndexOf("large", StringComparison.OrdinalIgnoreCase) >= 0 ? "LARGE COUNTERMEASURE DISPENSER" : "INTERNAL COUNTERMEASURE DISPENSER";
                        launcher = new CountermeasureLauncher { Key = key, Display = kind, NativeRounds = Math.Max(1, rounds), AllowsFlares = !chaffOnly, AllowsChaff = !flareOnly };
                        result.Add(launcher);
                    }
                    else
                    {
                        launcher.NativeRounds = Math.Max(launcher.NativeRounds, rounds);
                        launcher.AllowsFlares |= !chaffOnly;
                        launcher.AllowsChaff |= !flareOnly;
                    }
                }
            }
            catch { }
            if (result.Count == 0)
            {
                AircraftSettings settings = GetAircraftSettings(item);
                foreach (CountermeasureLoadout saved in settings.CountermeasureLoadouts)
                    result.Add(new CountermeasureLauncher { Key = saved.Key, Display = "COUNTERMEASURE DISPENSER", NativeRounds = Math.Max(1, saved.Flares + saved.Chaff), AllowsFlares = true, AllowsChaff = true });
            }
            if (result.Count == 0)
                result.Add(new CountermeasureLauncher { Key = "default", Display = "INSTALLED COUNTERMEASURE DISPENSERS", NativeRounds = 90, AllowsFlares = true, AllowsChaff = true });
            foreach (IGrouping<string, CountermeasureLauncher> group in result.GroupBy(x => x.Display, StringComparer.OrdinalIgnoreCase))
            {
                if (group.Count() < 2) continue;
                int number = 1;
                foreach (CountermeasureLauncher launcher in group.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                    launcher.Display = group.Key + " " + (number++).ToString(CultureInfo.InvariantCulture);
            }
            return result.OrderBy(x => x.Display, StringComparer.OrdinalIgnoreCase).ToList();
        }

        internal static bool HasCountermeasureUpgradeModules(string flightmodel)
        {
            if (String.IsNullOrWhiteSpace(flightmodel)) return false;
            BlockSpan modifications = BlkTools.FirstBlock(flightmodel, "modifications", 0);
            if (modifications == null) return false;
            foreach (BlockSpan module in BlkTools.DirectChildBlocks(modifications.Text))
            {
                string name = BlkTools.BlockName(module);
                if (String.IsNullOrWhiteSpace(name)) continue;
                if (name.IndexOf("countermeasure", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    String.Equals(BlkTools.Field(module.Text, "group", "t"), "countermeasures", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        internal bool WorkspaceGenerateMission(string airTargetId, int airTargetCount, IList<string> groundTargetIds, bool hostile, string shipTargetId, int shipTargetCount, bool passiveShip)
        {
            return WorkspaceGenerateMission(airTargetId, airTargetCount, groundTargetIds, hostile, shipTargetId, shipTargetCount, passiveShip, null, null, "active", "all");
        }

        internal bool WorkspaceGenerateMission(string airTargetId, int airTargetCount, IList<string> groundTargetIds, bool hostile, string shipTargetId, int shipTargetCount, bool passiveShip, IList<FlyingTargetSlot> flyingTargets)
        {
            return WorkspaceGenerateMission(airTargetId, airTargetCount, groundTargetIds, hostile, shipTargetId, shipTargetCount, passiveShip, flyingTargets, null, "active", "all");
        }

        internal bool WorkspaceGenerateMission(string airTargetId, int airTargetCount, IList<string> groundTargetIds, bool hostile, string shipTargetId, int shipTargetCount, bool passiveShip, IList<FlyingTargetSlot> flyingTargets, CombinedScenarioSettings combinedScenario, string samSitesMode = "active", string samSitesSelection = "all")
        {
            SelectComboById(airTargetBox, airTargetId);
            string firstGround = groundTargetIds == null ? null : groundTargetIds.FirstOrDefault(x => !String.IsNullOrWhiteSpace(x));
            SelectComboById(groundTargetBox, firstGround);
            SelectComboById(shipTargetBox, shipTargetId);
            airCount.Value = Math.Max(0, Math.Min(20, airTargetCount));
            groundCount.Value = firstGround == null ? 0 : 1;
            shipCount.Value = Math.Max(0, Math.Min(20, shipTargetCount));
            hostileGround.Checked = hostile;
            samSites.Checked = samSitesMode != "disabled";
            pendingSamMode = samSitesMode;
            pendingSamSelection = samSitesSelection;
            workspaceGroundTargetOverrides = groundTargetIds == null ? null : groundTargetIds.Where(x => !String.IsNullOrWhiteSpace(x)).Take(7).ToList();
            workspaceFlyingTargets = flyingTargets == null ? null : flyingTargets.Where(x => x != null && !String.IsNullOrWhiteSpace(x.AircraftId)).ToList();
            workspacePassiveShip = passiveShip;
            workspaceCombinedScenario = combinedScenario == null ? null : combinedScenario.Copy();
            suppressSuccessDialog = true;
            lastGenerationSucceeded = false;
            workspaceOperation = true;
            workspaceLastError = null;
            try { ApplyClicked(); }
            finally
            {
                workspaceOperation = false;
                suppressSuccessDialog = false;
                workspaceGroundTargetOverrides = null;
                workspaceFlyingTargets = null;
                workspacePassiveShip = false;
                workspaceCombinedScenario = null;
            }
            if (workspaceLastError != null) throw workspaceLastError;
            return lastGenerationSucceeded;
        }

        internal int WorkspaceWeaponCount { get { return globalWeapons.Count; } }

        private static string[] Lines(string resource)
        {
            return Embedded.Text(resource).Replace("\r", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        // Deserialize an embedded catalog JSON resource into row DTOs. Mirrors the
        // legacy TSV parsers but reads the converted JSON catalogs instead.
        internal static List<T> JsonRows<T>(string resource)
        {
            try
            {
                string text = Embedded.Text(resource);
                if (String.IsNullOrWhiteSpace(text)) return new List<T>();
                System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                serializer.MaxJsonLength = int.MaxValue;
                List<T> rows = serializer.Deserialize<List<T>>(text);
                return rows ?? new List<T>();
            }
            catch { return new List<T>(); }
        }

        internal static double ParseNumber(string value)
        {
            double result;
            return Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ? result : 0;
        }

        private void LoadCatalogs()
        {
            foreach (AircraftRowJson r in MainForm.JsonRows<AircraftRowJson>("UTL.aircraft.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.id)) continue;
                aircraft.Add(new Aircraft { Id = r.id, Display = r.display, Type = r.type, DefaultPreset = r.defaultPreset, Nation = r.nation, Rank = r.rank, MaxLoad = r.maxLoad, Kind = String.IsNullOrWhiteSpace(r.kind) ? "Aircraft" : r.kind });
            }
            foreach (GroundRowJson r in MainForm.JsonRows<GroundRowJson>("UTL.ground.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.id)) continue;
                TargetUnit target = new TargetUnit
                {
                    Id = r.id, Display = r.display, DefaultPreset = r.defaultPreset, Nation = String.IsNullOrWhiteSpace(r.nation) ? "Other" : r.nation, Rank = r.rank,
                    Type = String.IsNullOrWhiteSpace(r.type) ? "Ground Vehicle" : r.type, MainWeaponBlk = r.mainWeaponBlk ?? "", MaxAmmo = r.maxAmmo,
                    NativeMass = r.mass, NativeEnginePower = r.enginePower,
                    NativeForwardSpeed = r.forwardSpeed, NativeReverseSpeed = r.reverseSpeed,
                    NativeReloadSeconds = r.reloadSeconds, NativeRecoil = r.recoil
                };
                groundTargets.Add(target);
                aircraft.Add(new Aircraft
                {
                    Id = target.Id, Display = target.Display, Type = target.Type, DefaultPreset = target.DefaultPreset, Nation = target.Nation,
                    Rank = target.Rank, Kind = "Ground Vehicle", MainWeaponBlk = target.MainWeaponBlk, MaxAmmo = target.MaxAmmo,
                    NativeMass = target.NativeMass, NativeEnginePower = target.NativeEnginePower, NativeForwardSpeed = target.NativeForwardSpeed,
                    NativeReverseSpeed = target.NativeReverseSpeed, NativeReloadSeconds = target.NativeReloadSeconds, NativeRecoil = target.NativeRecoil
                });
            }
            foreach (ShipRowJson r in MainForm.JsonRows<ShipRowJson>("UTL.ships.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.id)) continue;
                shipTargets.Add(new TargetUnit { Id = r.id, Display = r.display, DefaultPreset = r.defaultPreset, Nation = String.IsNullOrWhiteSpace(r.nation) ? "Other" : r.nation, Rank = r.rank, Type = String.IsNullOrWhiteSpace(r.type) ? "Ship" : r.type });
            }
            foreach (DonorWeaponRowJson r in MainForm.JsonRows<DonorWeaponRowJson>("UTL.weapon_catalog.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.blk)) continue;
                globalWeapons.Add(new DonorWeapon { Trigger = r.trigger, Blk = r.blk, Bullets = r.bullets, Icon = r.icon, Name = r.name, Category = r.category, UnitMass = r.unitMass, TotalMass = r.totalMass });
            }
            navalCannons.Clear();
            foreach (NameValueRowJson r in MainForm.JsonRows<NameValueRowJson>("UTL.naval_cannons.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.key)) continue;
                navalCannons.Add(new KeyValuePair<string, string>(r.key.Trim(), (r.value ?? "").Trim()));
            }
            airOrdnance.Clear();
            foreach (NameValueRowJson r in MainForm.JsonRows<NameValueRowJson>("UTL.air_ordnance.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.blk)) continue;
                airOrdnance.Add(new KeyValuePair<string, string>(r.blk.Trim(), (r.display ?? "").Trim()));
            }
            foreach (PylonSlotRowJson r in MainForm.JsonRows<PylonSlotRowJson>("UTL.aircraft_slots.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.aircraftId)) continue;
                pylons.Add(new PylonSlot { AircraftId = r.aircraftId, Slot = r.slot, Order = r.order, Tier = r.tier, MaxLoad = r.maxLoad, AnchorMount = r.anchorMount });
            }
            foreach (CombinedMapRowJson r in MainForm.JsonRows<CombinedMapRowJson>("UTL.combined_maps.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.id)) continue;
                CombinedMap map = combinedMaps.FirstOrDefault(x => x.Id.Equals(r.id, StringComparison.OrdinalIgnoreCase));
                if (map == null)
                {
                    map = new CombinedMap { Id = r.id, Display = r.display, Level = r.level };
                    combinedMaps.Add(map);
                }
                if (!String.IsNullOrWhiteSpace(r.kind) && r.kind.Equals("capture", StringComparison.OrdinalIgnoreCase))
                {
                    map.CapturePoints.Add(new CombinedCapturePoint { Id = r.detail, Label = r.label, Transform = r.transform });
                    continue;
                }
                map.Spawns.Add(new CombinedSpawn
                {
                    Kind = r.kind, Side = r.side, Option = r.detail, Label = r.label, Transform = r.transform, ObjectClass = r.objectClass
                });
            }

            combinedMaps.Sort(delegate(CombinedMap left, CombinedMap right) { return StringComparer.CurrentCultureIgnoreCase.Compare(left.Display, right.Display); });
        }


        internal static List<GroundPresetRowJson> GroundPresets { get { if (groundPresetsBacking == null) { groundPresetsBacking = JsonRows<GroundPresetRowJson>("UTL.ground_presets.json"); } return groundPresetsBacking; } }
        private static List<GroundPresetRowJson> groundPresetsBacking;
        internal static List<SensorRowJson> SensorCatalog { get { if (sensorCatalogBacking == null) { sensorCatalogBacking = JsonRows<SensorRowJson>("UTL.sensors.json"); } return sensorCatalogBacking; } }
        private static List<SensorRowJson> sensorCatalogBacking;

        // Lazy catalog loaders - the big tables parse on first use so the window opens quickly.
        private void LoadDonorWeaponsCatalog()
        {
            nativeWeaponsBacking = new List<DonorWeapon>();
            foreach (DonorWeaponRowJson r in JsonRows<DonorWeaponRowJson>("UTL.donor_weapons.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.blk)) continue;
                nativeWeaponsBacking.Add(new DonorWeapon
                {
                    AircraftId = r.aircraftId, AircraftDisplay = r.aircraftDisplay, Slot = r.slot, Mount = r.mount, Trigger = r.trigger, Blk = r.blk,
                    Emitter = r.emitter, Bullets = r.bullets, Icon = r.icon, Name = r.name, Category = r.category, UnitMass = r.unitMass, TotalMass = r.totalMass
                });
            }
            PopulateWeaponNations();
        }

        private void LoadUnitWeaponsCatalog()
        {
            unitWeaponsBacking = new List<UnitWeapon>();
            foreach (UnitWeaponRowJson r in JsonRows<UnitWeaponRowJson>("UTL.unit_weapons.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.unitId) || String.IsNullOrWhiteSpace(r.weaponBlk)) continue;
                unitWeaponsBacking.Add(new UnitWeapon { UnitId = r.unitId, Domain = r.domain, UnitDisplay = r.unitDisplay, WeaponBlk = r.weaponBlk, WeaponDisplay = r.weaponDisplay, Kind = r.kind });
            }
        }

        private void LoadModificationsCatalog()
        {
            modificationsBacking = new List<AircraftModification>();
            foreach (ModificationRowJson r in JsonRows<ModificationRowJson>("UTL.modifications.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.aircraftId) || String.IsNullOrWhiteSpace(r.id)) continue;
                modificationsBacking.Add(new AircraftModification
                {
                    AircraftId = r.aircraftId, Id = r.id, Display = r.display, Tier = r.tier,
                    ModClass = r.modClass, Group = r.group, Requires = r.requires
                });
            }
        }

        private void PopulateWeaponNations()
        {
            Dictionary<string, string> aircraftNations = aircraft.GroupBy(a => a.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Nation, StringComparer.OrdinalIgnoreCase);
            foreach (DonorWeapon weapon in nativeWeapons)
            {
                string nation;
                weapon.Nations = aircraftNations.TryGetValue(weapon.AircraftId, out nation) ? nation : "";
            }
            Dictionary<string, List<DonorWeapon>> sources = nativeWeapons
                .Where(w => w.AircraftId.IndexOf("killstreak", StringComparison.OrdinalIgnoreCase) < 0 && !w.AircraftId.StartsWith("nt_", StringComparison.OrdinalIgnoreCase))
                .GroupBy(WeaponKey, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            foreach (DonorWeapon weapon in globalWeapons)
            {
                List<DonorWeapon> donors;
                List<string> nations = sources.TryGetValue(WeaponKey(weapon), out donors)
                    ? donors.Select(w => w.Nations).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList()
                    : new List<string>();
                if (!String.IsNullOrWhiteSpace(weapon.Nations))
                    nations.AddRange(weapon.Nations.Split('|').Where(x => !String.IsNullOrWhiteSpace(x)));
                if (nations.Count == 0)
                {
                    string inferred = InferWeaponNation(weapon.Blk);
                    if (!String.IsNullOrEmpty(inferred)) nations.Add(inferred);
                }
                weapon.Nations = String.Join("|", nations.Distinct().OrderBy(x => x).ToArray());
            }
        }

        private static string WeaponKey(DonorWeapon weapon)
        {
            return (weapon.Trigger ?? "") + "|" + (weapon.Blk ?? "") + "|" + weapon.Bullets.ToString(CultureInfo.InvariantCulture);
        }

        private static string InferWeaponNation(string blk)
        {
            string file = Path.GetFileNameWithoutExtension(blk ?? "").ToLowerInvariant();
            if (file.StartsWith("us_") || file.StartsWith("aim_") || file.StartsWith("agm_") || file.StartsWith("gbu_")) return "USA";
            if (file.StartsWith("su_") || file.StartsWith("ussr_") || file.StartsWith("ru_") || file.StartsWith("r_") || file.StartsWith("kh_")) return "USSR";
            if (file.StartsWith("uk_") || file.StartsWith("gb_") || file.Contains("brimstone")) return "Britain";
            if (file.StartsWith("fr_") || file.Contains("magic") || file.Contains("mica")) return "France";
            if (file.StartsWith("de_") || file.StartsWith("ger_")) return "Germany";
            if (file.StartsWith("it_") || file.StartsWith("ita_")) return "Italy";
            if (file.StartsWith("jp_") || file.StartsWith("ja_")) return "Japan";
            if (file.StartsWith("cn_") || file.StartsWith("ch_")) return "China";
            if (file.StartsWith("il_") || file.StartsWith("isr_")) return "Israel";
            if (file.StartsWith("se_") || file.StartsWith("sw_")) return "Sweden";
            return "";
        }

    }
}
