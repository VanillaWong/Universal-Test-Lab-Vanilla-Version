// UniversalTestLab.Data.cs
// Data models, mission settings and JSON catalog row DTOs.
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
    internal sealed class Aircraft
    {
        public string Id;
        public string Display;
        public string Type;
        public string DefaultPreset;
        public string Nation;
        public int Rank;
        public double MaxLoad;
        public string Kind;
        public string MainWeaponBlk;
        public int MaxAmmo;
        public double NativeMass;
        public double NativeEnginePower;
        public double NativeForwardSpeed;
        public double NativeReverseSpeed;
        public double NativeReloadSeconds;
        public double NativeRecoil;
        public override string ToString() { return Display; }
    }
    internal sealed class AircraftModification
    {
        public string AircraftId;
        public string Id;
        public string Display;
        public int Tier;
        public string ModClass;
        public string Group;
        public string Requires;

        public override string ToString()
        {
            string tier = Tier > 0 ? "TIER " + Tier.ToString(CultureInfo.InvariantCulture) + "  •  " : "";
            return tier + Display + "   [" + Id + "]";
        }
    }
    internal sealed class AircraftSettings
    {
        public bool UseAllModifications = true;
        public readonly HashSet<string> EnabledModifications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public bool OverrideCountermeasures;
        public int FlareRounds = 45;
        public int ChaffRounds = 45;
        public bool UnlimitedCountermeasures;
        public bool FullFuel = true;
        public int FuelMinutes = 60;
        public readonly List<CountermeasureLoadout> CountermeasureLoadouts = new List<CountermeasureLoadout>();
        public readonly Dictionary<int, string> GunBeltSelections = new Dictionary<int, string>();
        public readonly List<GroundAmmoLoadout> GroundAmmoLoadouts = new List<GroundAmmoLoadout>();
        public bool OverrideGroundBallistics;
        public double ProjectileMassMultiplier = 1.0;
        public double MuzzleVelocityMultiplier = 1.0;
        public double ExplosiveMassMultiplier = 1.0;
        public double PenetrationMultiplier = 1.0;
        public double ReloadSeconds;
        public double RecoilMultiplier = 1.0;
        public double EnginePowerMultiplier = 1.0;
        public double VehicleMassMultiplier = 1.0;
        public double ForwardSpeedMultiplier = 1.0;
        public double ReverseSpeedMultiplier = 1.0;
        public string UserSightPath;
        public string InjectedCannonBlk;
        public string InjectedCannonDomain;
        public string InjectedCannonRound;
        public bool InjectNativeLauncher; // inject-shell: mount the chosen missile INTO the native launcher (S-75 V-759 in Osa 209mm), instead of swapping the whole cannon
        public int InjectedCannonRounds; // >0: override the injected cannon's bulletsCartridge (rounds per reload/volley, e.g. 6 on an Osa rack)
        public bool UnlimitedAmmo;
        public bool FakeArhConversion;
        public string RadarSearchBlk;   // sensor blk to install as the player search radar (e.g. su_p_12ma)
        public string RadarTrackBlk;    // sensor blk to install as the player track radar (e.g. su_snr_75)
        public bool RadarStripAiSensors; // remove the AI-only *_ai sensor pair from the proxy
        public string InjectedCannonUnit;

        public AircraftSettings Copy()
        {
            AircraftSettings copy = new AircraftSettings
            {
                UseAllModifications = UseAllModifications,
                OverrideCountermeasures = OverrideCountermeasures,
                FlareRounds = FlareRounds,
                ChaffRounds = ChaffRounds,
                // Legacy presets may still contain this field, but million-round
                // countermeasure magazines distort aircraft mass and systems.
                UnlimitedCountermeasures = false,
                FullFuel = FullFuel,
                FuelMinutes = FuelMinutes,
                OverrideGroundBallistics = OverrideGroundBallistics,
                ProjectileMassMultiplier = ProjectileMassMultiplier,
                MuzzleVelocityMultiplier = MuzzleVelocityMultiplier,
                ExplosiveMassMultiplier = ExplosiveMassMultiplier,
                PenetrationMultiplier = PenetrationMultiplier,
                ReloadSeconds = ReloadSeconds,
                RecoilMultiplier = RecoilMultiplier,
                EnginePowerMultiplier = EnginePowerMultiplier,
                VehicleMassMultiplier = VehicleMassMultiplier,
                ForwardSpeedMultiplier = ForwardSpeedMultiplier,
                ReverseSpeedMultiplier = ReverseSpeedMultiplier,
                UserSightPath = UserSightPath,
                InjectedCannonBlk = InjectedCannonBlk,
                InjectedCannonDomain = InjectedCannonDomain,
                InjectedCannonUnit = InjectedCannonUnit,
                InjectedCannonRound = InjectedCannonRound,
                InjectNativeLauncher = InjectNativeLauncher,
                InjectedCannonRounds = InjectedCannonRounds,
                UnlimitedAmmo = UnlimitedAmmo,
                FakeArhConversion = FakeArhConversion,
                RadarSearchBlk = RadarSearchBlk,
                RadarTrackBlk = RadarTrackBlk,
                RadarStripAiSensors = RadarStripAiSensors
            };
            foreach (string id in EnabledModifications) copy.EnabledModifications.Add(id);
            foreach (CountermeasureLoadout loadout in CountermeasureLoadouts) copy.CountermeasureLoadouts.Add(loadout.Copy());
            foreach (KeyValuePair<int, string> belt in GunBeltSelections) copy.GunBeltSelections[belt.Key] = belt.Value;
            foreach (GroundAmmoLoadout loadout in GroundAmmoLoadouts) copy.GroundAmmoLoadouts.Add(loadout.Copy());
            return copy;
        }
    }
    internal sealed class GroundAmmo
    {
        public string SourceBlk;
        // Named ammunition container (cannon top-level block) the round belongs
        // to, e.g. 125mm_ussr_3BM42_APDS_FS. Empty for anonymous default rounds.
        public string Container;
        public string BulletName;
        public string Display;
        public string Type;
        public double Mass;
        public double Speed;
        public double ExplosiveMass;
        public double Caliber;
        public double Penetration;
        public override string ToString() { return Display + "  •  " + Type + "  •  " + Speed.ToString("0", CultureInfo.InvariantCulture) + " m/s"; }
    }
    internal sealed class GroundAmmoJson
    {
        public string source { get; set; }
        public string container { get; set; }
        public string bulletName { get; set; }
        public string display { get; set; }
        public string kind { get; set; }
        public double mass { get; set; }
        public double speed { get; set; }
        public double explosive { get; set; }
        public double caliber { get; set; }
        public double penetration { get; set; }
    }
    internal sealed class AircraftRowJson
    {
        public string id { get; set; }
        public string display { get; set; }
        public string type { get; set; }
        public string defaultPreset { get; set; }
        public string nation { get; set; }
        public int rank { get; set; }
        public double maxLoad { get; set; }
        public string kind { get; set; }
    }
    internal sealed class GroundRowJson
    {
        public string id { get; set; }
        public string display { get; set; }
        public string defaultPreset { get; set; }
        public string nation { get; set; }
        public int rank { get; set; }
        public string type { get; set; }
        public string mainWeaponBlk { get; set; }
        public int maxAmmo { get; set; }
        public double mass { get; set; }
        public double enginePower { get; set; }
        public double forwardSpeed { get; set; }
        public double reverseSpeed { get; set; }
        public double reloadSeconds { get; set; }
        public double recoil { get; set; }
    }
    internal sealed class ShipRowJson
    {
        public string id { get; set; }
        public string display { get; set; }
        public string defaultPreset { get; set; }
        public string nation { get; set; }
        public int rank { get; set; }
        public string type { get; set; }
    }
    internal sealed class DonorWeaponRowJson
    {
        public string aircraftId { get; set; }
        public string aircraftDisplay { get; set; }
        public int slot { get; set; }
        public string mount { get; set; }
        public string trigger { get; set; }
        public string blk { get; set; }
        public string emitter { get; set; }
        public int bullets { get; set; }
        public string icon { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public double unitMass { get; set; }
        public double totalMass { get; set; }
    }
    internal sealed class UnitWeaponRowJson
    {
        public string unitId { get; set; }
        public string domain { get; set; }
        public string unitDisplay { get; set; }
        public string weaponBlk { get; set; }
        public string weaponDisplay { get; set; }
        public string kind { get; set; }
    }
    internal sealed class PylonSlotRowJson
    {
        public string aircraftId { get; set; }
        public int slot { get; set; }
        public int order { get; set; }
        public int tier { get; set; }
        public double maxLoad { get; set; }
        public string anchorMount { get; set; }
    }
    internal sealed class ModificationRowJson
    {
        public string aircraftId { get; set; }
        public string id { get; set; }
        public string display { get; set; }
        public int tier { get; set; }
        public string modClass { get; set; }
        public string group { get; set; }
        public string requires { get; set; }
    }
    internal sealed class CombinedMapRowJson
    {
        public string id { get; set; }
        public string display { get; set; }
        public string level { get; set; }
        public string kind { get; set; }
        public int side { get; set; }
        public string detail { get; set; }
        public string label { get; set; }
        public string transform { get; set; }
        public string objectClass { get; set; }
    }
    internal sealed class EraPresetRowJson
    {
        public string name { get; set; }
        public string groundIds { get; set; }
        public string airIds { get; set; }
        public string airCounts { get; set; }
        public string shipId { get; set; }
        public int shipCount { get; set; }
    }
    internal sealed class SensorRowJson
    {
        public string id { get; set; }
        public string display { get; set; }
        public string band { get; set; }
        public string role { get; set; }
        public string rangeMax { get; set; }
        public string type { get; set; }
        public string fsm { get; set; }
        public string weaponTargetsMax { get; set; }
        public string irst { get; set; }
        public string domain { get; set; }
    }
    internal sealed class GroundPresetRowJson
    {
        public string id { get; set; }
        public string name { get; set; }
        public string vehicle { get; set; }
        public string cannon { get; set; }
        public string cannonRound { get; set; }
        public int cannonRounds { get; set; }
        public string radarSearch { get; set; }
        public string radarTrack { get; set; }
        public bool fakeArh { get; set; }
        public bool injectNative { get; set; }
        public bool unlimited { get; set; }
        public string note { get; set; }
    }
    internal sealed class NameValueRowJson
    {
        // naval_cannons.tsv -> key/value; air_ordnance.tsv -> blk/display/kind
        public string key { get; set; }
        public string value { get; set; }
        public string blk { get; set; }
        public string display { get; set; }
        public string kind { get; set; }
    }
    internal sealed class GroundAmmoLoadout
    {
        public int Slot;
        public int Count;
        public string SourceBlk;
        public string BulletName;
        // tankModels.bulletsN expects the named ammunition container in the cannon
        // BLK (for example 120mm_britain_L27_APDSFS), not the nested projectile's
        // bulletName (120mm_l27a1). Keep both because projectile editing uses the
        // latter while the mission loadout uses the former.
        public string AmmoGroup;
        // Catalog ammunition type (APFSDS / HE / SAM / ATGM ...). Missiles are
        // excluded from cannon injection and their mission loadout counts are
        // clamped against the vehicle's shared missile racks.
        public string Kind;
        public GroundAmmoLoadout Copy() { return new GroundAmmoLoadout { Slot = Slot, Count = Count, SourceBlk = SourceBlk, BulletName = BulletName, AmmoGroup = AmmoGroup, Kind = Kind }; }
    }
    internal sealed class UserSightEntry
    {
        public string FilePath;
        public string Name;
        public string Folder;
        public bool IsDefault;

        public override string ToString()
        {
            return IsDefault ? "Game / current default sight" : Name + (String.IsNullOrWhiteSpace(Folder) ? "" : "  •  " + Folder);
        }
    }
    internal sealed class CountermeasureLauncher
    {
        public string Key;
        public string Display;
        public int NativeRounds;
        public bool AllowsFlares;
        public bool AllowsChaff;
    }
    internal sealed class CountermeasureLoadout
    {
        public string Key;
        public int Flares;
        public int Chaff;

        public CountermeasureLoadout Copy()
        {
            return new CountermeasureLoadout { Key = Key, Flares = Flares, Chaff = Chaff };
        }
    }
    internal sealed class TargetUnit
    {
        public string Id;
        public string Display;
        public string DefaultPreset;
        public string Nation;
        public int Rank;
        public string Type;
        public string MainWeaponBlk;
        public int MaxAmmo;
        public double NativeMass;
        public double NativeEnginePower;
        public double NativeForwardSpeed;
        public double NativeReverseSpeed;
        public double NativeReloadSeconds;
        public double NativeRecoil;

        public TargetUnit Copy()
        {
            return (TargetUnit)MemberwiseClone();
        }
        public override string ToString() { return Display; }
    }
    internal sealed class FlyingTargetSlot
    {
        public string UnitName;
        public string AircraftId;
        public int Count;

        public FlyingTargetSlot(string unitName, string aircraftId, int count)
        {
            UnitName = unitName;
            AircraftId = aircraftId;
            Count = count;
        }
    }
    internal sealed class CombinedSpawn
    {
        public string Kind;
        public int Side;
        public string Option;
        public string Label;
        public string Transform;
        public string ObjectClass;
        public override string ToString() { return Label; }
    }
    internal sealed class CombinedCapturePoint
    {
        public string Id;
        public string Label;
        public string Transform;
    }
    internal sealed class CombinedMap
    {
        public string Id;
        public string Display;
        public string Level;
        public readonly List<CombinedSpawn> Spawns = new List<CombinedSpawn>();
        public readonly List<CombinedCapturePoint> CapturePoints = new List<CombinedCapturePoint>();
        public override string ToString() { return Display; }
    }
    internal sealed class CombinedScenarioSettings
    {
        public bool Enabled;
        public string MapId;
        public int Side = 1;
        public string SpawnOption;

        public CombinedScenarioSettings Copy()
        {
            return new CombinedScenarioSettings { Enabled = Enabled, MapId = MapId, Side = Side, SpawnOption = SpawnOption };
        }
    }
    internal sealed class UnitWeapon
{
    public string UnitId;
    public string Domain;
    public string UnitDisplay;
    public string WeaponBlk;
    public string WeaponDisplay;
    public string Kind;
}
internal sealed class DonorWeapon
    {
        public string AircraftId;
        public string AircraftDisplay;
        public int Slot;
        public string Mount;
        public string Trigger;
        public string Blk;
        public string Emitter;
        public int Bullets;
        public string Icon;
        public string Name;
        public string Category;
        public string Nations;
        public double UnitMass;
        public double TotalMass;
        public override string ToString() { return Name; }
    }
    internal sealed class PylonSlot
    {
        public string AircraftId;
        public int Slot;
        public int Order;
        public int Tier;
        public double MaxLoad;
        public string AnchorMount;
    }
    internal sealed class PylonAssignment
    {
        public PylonSlot Pylon;
        public DonorWeapon Weapon;
        public bool Injected;
    }
    internal sealed class GeneratedAircraft
    {
        public string ClassId;
        public string PresetId;
        public string ModelId;
        public string FlightModelPath;
        public string PresetPath;
        public int SpawnSpeedKmh;
        public bool IsGround;
        public string UserSightFolder;
        public readonly List<GroundAmmoLoadout> GroundAmmoLoadouts = new List<GroundAmmoLoadout>();
                public readonly List<string> AuxiliaryPaths = new List<string>();
    }
    internal sealed class GroundWeaponInfo
    {
        public string Trigger;
        public string Blk;
        public int NativeAmmo;
        public string Display;
    }
    internal sealed class GroundWeaponCacheData
    {
        public IList<GroundWeaponInfo> Weapons;
        public IList<KeyValuePair<string, string>> Missiles;
        public IList<GroundWeaponBeltOption> BeltOptions;
        public readonly Dictionary<string, int> RackRounds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, int> BeltSizes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public int BeltTypeLimit = 1;
    }
    internal sealed class GroundWeaponBeltOption
    {
        public string Name;
        public int Calibre;
        public IList<GroundAmmo> Rounds;
    }
    internal sealed class GroundWeaponCacheJson
    {
        public List<GroundWeaponInfoJson> weapons { get; set; }
        public List<MissileInfoJson> missiles { get; set; }
        public List<GroundWeaponBeltJson> beltOptions { get; set; }
        public Dictionary<string, int> rackRounds { get; set; }
        public Dictionary<string, int> beltSizes { get; set; }
        public int beltTypeLimit { get; set; }
    }
    internal sealed class GroundWeaponBeltJson
    {
        public string name { get; set; }
        public int calibre { get; set; }
        public List<GroundWeaponRoundJson> rounds { get; set; }
    }
    internal sealed class GroundWeaponRoundJson
    {
        public string bulletName { get; set; }
        public string display { get; set; }
        public string kind { get; set; }
        public double mass { get; set; }
        public double speed { get; set; }
        public double explosive { get; set; }
        public double caliber { get; set; }
        public double penetration { get; set; }
    }
    internal sealed class GroundWeaponInfoJson
    {
        public string trigger { get; set; }
        public string blk { get; set; }
        public int nativeAmmo { get; set; }
    }
    internal sealed class MissileInfoJson
    {
        public string name { get; set; }
        public string blk { get; set; }
    }
    internal sealed class SavedPresetEntry
    {
        public int Slot;
        public bool Injected;
        public string Mount;
        public string Trigger;
        public string Blk;
        public string Emitter;
        public int Bullets;
        public string Icon;
        public string Name;
        public string Category;
        public double UnitMass;
        public double TotalMass;
    }
    internal sealed class SavedPreset
    {
        public string Name;
        public string AircraftId;
        public AircraftSettings Settings;
        public readonly List<SavedPresetEntry> Entries = new List<SavedPresetEntry>();
        public override string ToString() { return Name; }
    }
    internal sealed class MissionSettings
    {
        public double PlayerRespawnDelaySeconds;
        public double TargetRespawnDelaySeconds = 0.25;
        public double RearmSeconds = 1.0;
        public bool LimitedAmmo;
        public bool RapidFireEnabled;
        public double RapidFireInterval = 0.5;
        public bool RapidFireFullRestore = true;
        public bool RearmOverride;
        public string SpawnMode = "air";
public string InjectedCannonBlk;
public string InjectedCannonDomain;
public string InjectedCannonUnit;
public bool FakeArhConversion;
        public bool SpawnSpeedAuto = true;
        public int SpawnSpeedKmh = 450;

        public static MissionSettings Current = new MissionSettings();

        private static string FilePath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniversalTestLab", "mission_options.txt"); }
        }

        public MissionSettings Copy()
        {
            return new MissionSettings
            {
                PlayerRespawnDelaySeconds = PlayerRespawnDelaySeconds,
                TargetRespawnDelaySeconds = TargetRespawnDelaySeconds,
                RearmSeconds = RearmSeconds,
                LimitedAmmo = LimitedAmmo,
                RapidFireEnabled = RapidFireEnabled,
                RapidFireInterval = RapidFireInterval,
                RapidFireFullRestore = RapidFireFullRestore,
            RearmOverride = RearmOverride,
                SpawnMode = SpawnMode,
                SpawnSpeedAuto = SpawnSpeedAuto,
                SpawnSpeedKmh = SpawnSpeedKmh,
                InjectedCannonBlk = InjectedCannonBlk,
                InjectedCannonDomain = InjectedCannonDomain,
                InjectedCannonUnit = InjectedCannonUnit,
                FakeArhConversion = FakeArhConversion
            };
        }

        public void Save()
        {
            try
            {
                Dictionary<string, object> mo = new Dictionary<string, object>();
                mo.Add("player_respawn_delay", PlayerRespawnDelaySeconds);
                mo.Add("target_respawn_delay", TargetRespawnDelaySeconds);
                mo.Add("rearm_seconds", RearmSeconds);
                mo.Add("limited_ammo", LimitedAmmo);
                mo.Add("rapid_fire_enabled", RapidFireEnabled);
                mo.Add("rapid_fire_interval", RapidFireInterval);
                mo.Add("rapid_fire_full_restore", RapidFireFullRestore);
                mo.Add("spawn_mode", String.IsNullOrWhiteSpace(SpawnMode) ? "air" : SpawnMode);
                mo.Add("spawn_speed_auto", SpawnSpeedAuto);
                mo.Add("spawn_speed_kmh", SpawnSpeedKmh);
                if (!String.IsNullOrWhiteSpace(InjectedCannonBlk)) mo.Add("inject_cannon_blk", InjectedCannonBlk);
                if (!String.IsNullOrWhiteSpace(InjectedCannonDomain)) mo.Add("inject_cannon_domain", InjectedCannonDomain);
                if (!String.IsNullOrWhiteSpace(InjectedCannonUnit)) mo.Add("inject_cannon_unit", InjectedCannonUnit);
                mo.Add("fake_arh_conversion", FakeArhConversion);
            mo.Add("rearm_override", RearmOverride);
                ConfigStore.SetObject("mission_options", mo);
                ConfigStore.Save();
            }
            catch { }
        }

        public static void Load()
        {
            try
            {
                Dictionary<string, object> mo = ConfigStore.GetObject("mission_options");
                if (mo == null) return;
                object v;
                double number;
                if (mo.TryGetValue("player_respawn_delay", out v) && v != null && Double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number)) Current.PlayerRespawnDelaySeconds = number;
                if (mo.TryGetValue("target_respawn_delay", out v) && v != null && Double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number)) Current.TargetRespawnDelaySeconds = number;
                if (mo.TryGetValue("rearm_seconds", out v) && v != null && Double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number)) Current.RearmSeconds = number;
                if (mo.TryGetValue("limited_ammo", out v) && v != null) Current.LimitedAmmo = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                if (mo.TryGetValue("rapid_fire_enabled", out v) && v != null) Current.RapidFireEnabled = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                if (mo.TryGetValue("rapid_fire_interval", out v) && v != null && Double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out number)) Current.RapidFireInterval = Math.Max(0.05, Math.Min(60.0, number));
                if (mo.TryGetValue("rapid_fire_full_restore", out v) && v != null) Current.RapidFireFullRestore = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                if (mo.TryGetValue("spawn_mode", out v) && v != null) { string s = Convert.ToString(v, CultureInfo.InvariantCulture); if (!String.IsNullOrWhiteSpace(s)) Current.SpawnMode = s; }
                if (mo.TryGetValue("spawn_speed_auto", out v) && v != null) Current.SpawnSpeedAuto = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                if (mo.TryGetValue("spawn_speed_kmh", out v) && v != null) { int kmh; if (Int32.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out kmh)) Current.SpawnSpeedKmh = Math.Max(0, Math.Min(1100, kmh)); }
                if (mo.TryGetValue("inject_cannon_blk", out v) && v != null) Current.InjectedCannonBlk = Convert.ToString(v, CultureInfo.InvariantCulture);
                if (mo.TryGetValue("inject_cannon_domain", out v) && v != null) Current.InjectedCannonDomain = Convert.ToString(v, CultureInfo.InvariantCulture);
                if (mo.TryGetValue("inject_cannon_unit", out v) && v != null) Current.InjectedCannonUnit = Convert.ToString(v, CultureInfo.InvariantCulture);
                if (mo.TryGetValue("fake_arh_conversion", out v) && v != null) Current.FakeArhConversion = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
            if (mo.TryGetValue("rearm_override", out v) && v != null) Current.RearmOverride = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
            }
            catch { }
        }
    }
    internal sealed class BlockSpan
    {
        public int Start;
        public int Open;
        public int End;
        public string Text;
    }
}
