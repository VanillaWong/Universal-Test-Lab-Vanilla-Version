// UniversalTestLab.Storage.cs
// Config/preset/settings persistence stores and the embedded-resource reader.
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
    internal static class PresetStore
    {
        public static string FilePath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniversalTestLab", "custom_presets.tsv"); }
        }

        internal static string B64(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? ""));
        }

        internal static string FromB64(string value)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }

        internal static string SerializeSettings(AircraftSettings settings)
        {
            if (settings == null) return "";
            string launcherSettings = String.Join(";", settings.CountermeasureLoadouts.Select(x =>
                B64(x.Key) + ":" + x.Flares.ToString(CultureInfo.InvariantCulture) + ":" + x.Chaff.ToString(CultureInfo.InvariantCulture)).ToArray());
            string gunBeltSettings = String.Join(";", settings.GunBeltSelections.OrderBy(x => x.Key).Select(x =>
                x.Key.ToString(CultureInfo.InvariantCulture) + ":" + B64(x.Value)).ToArray());
                        string groundAmmoSettings = String.Join(";", settings.GroundAmmoLoadouts.OrderBy(x => x.Slot).Select(x =>
                x.Slot.ToString(CultureInfo.InvariantCulture) + ":" + x.Count.ToString(CultureInfo.InvariantCulture) + ":" + B64(x.SourceBlk ?? String.Empty) + ":" + B64(x.BulletName ?? String.Empty) + ":" + B64(x.AmmoGroup ?? String.Empty)).ToArray());
            return (settings.UseAllModifications ? "1" : "0") + "|" +
                (settings.OverrideCountermeasures ? "1" : "0") + "|" +
                settings.FlareRounds.ToString(CultureInfo.InvariantCulture) + "|" +
                settings.ChaffRounds.ToString(CultureInfo.InvariantCulture) + "|" +
                "0|" +
                // Fields 5/6 are retained as inert placeholders so presets saved by
                // experimental builds still deserialize without shifting later fields.
                "0|1|" +
                String.Join(",", settings.EnabledModifications.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()) + "|" +
                (settings.FullFuel ? "1" : "0") + "|" +
                settings.FuelMinutes.ToString(CultureInfo.InvariantCulture) + "|" + launcherSettings + "|" + gunBeltSettings + "|" +
                groundAmmoSettings + "|" + (settings.OverrideGroundBallistics ? "1" : "0") + "|" +
                settings.ProjectileMassMultiplier.ToString("R", CultureInfo.InvariantCulture) + "|" +
                settings.MuzzleVelocityMultiplier.ToString("R", CultureInfo.InvariantCulture) + "|" +
                settings.ExplosiveMassMultiplier.ToString("R", CultureInfo.InvariantCulture) + "|" +
                settings.PenetrationMultiplier.ToString("R", CultureInfo.InvariantCulture) + "|" +
                settings.ReloadSeconds.ToString("R", CultureInfo.InvariantCulture) + "|" +
                settings.RecoilMultiplier.ToString("R", CultureInfo.InvariantCulture) + "|" +
                settings.EnginePowerMultiplier.ToString("R", CultureInfo.InvariantCulture) + "|" +
                settings.VehicleMassMultiplier.ToString("R", CultureInfo.InvariantCulture) + "|" +
                settings.ForwardSpeedMultiplier.ToString("R", CultureInfo.InvariantCulture) + "|" +
                settings.ReverseSpeedMultiplier.ToString("R", CultureInfo.InvariantCulture) + "|" +
                B64(settings.UserSightPath) + "|" +
                "-1|-1";
        }

        internal static AircraftSettings DeserializeSettings(string payload)
        {
            if (String.IsNullOrWhiteSpace(payload)) return null;
            string[] p = payload.Split('|');
            int flares, chaff;
            double spread;
            if (p.Length < 8 || !Int32.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out flares) ||
                !Int32.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out chaff) ||
                !Double.TryParse(p[6], NumberStyles.Float, CultureInfo.InvariantCulture, out spread)) return null;
            AircraftSettings settings = new AircraftSettings
            {
                UseAllModifications = p[0] == "1", OverrideCountermeasures = p[1] == "1",
                FlareRounds = flares, ChaffRounds = chaff, UnlimitedCountermeasures = false
            };
            foreach (string id in p[7].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) settings.EnabledModifications.Add(id);
            int fuelMinutes;
            if (p.Length >= 10)
            {
                settings.FullFuel = p[8] == "1";
                if (Int32.TryParse(p[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out fuelMinutes))
                    settings.FuelMinutes = Math.Max(5, Math.Min(60, fuelMinutes));
            }
            if (p.Length >= 11)
            {
                foreach (string encoded in p[10].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] item = encoded.Split(':');
                    int flaresAtLauncher, chaffAtLauncher;
                    if (item.Length != 3 || !Int32.TryParse(item[1], out flaresAtLauncher) || !Int32.TryParse(item[2], out chaffAtLauncher)) continue;
                    try { settings.CountermeasureLoadouts.Add(new CountermeasureLoadout { Key = FromB64(item[0]), Flares = flaresAtLauncher, Chaff = chaffAtLauncher }); }
                    catch { }
                }
            }
            if (p.Length >= 12)
            {
                foreach (string encoded in p[11].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int separator = encoded.IndexOf(':');
                    int group;
                    if (separator <= 0 || !Int32.TryParse(encoded.Substring(0, separator), NumberStyles.Integer, CultureInfo.InvariantCulture, out group) || group < 0 || group > 3) continue;
                    try
                    {
                        string belt = FromB64(encoded.Substring(separator + 1));
                        if (!String.IsNullOrWhiteSpace(belt)) settings.GunBeltSelections[group] = belt;
                    }
                    catch { }
                }
            }
            if (p.Length >= 13)
            {
                foreach (string encoded in p[12].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] item = encoded.Split(':');
                    int slot, count;
                    if ((item.Length != 4 && item.Length != 5) || !Int32.TryParse(item[0], out slot) || !Int32.TryParse(item[1], out count)) continue;
                    try { settings.GroundAmmoLoadouts.Add(new GroundAmmoLoadout { Slot = slot, Count = count, SourceBlk = FromB64(item[2]), BulletName = FromB64(item[3]), AmmoGroup = item.Length > 4 ? FromB64(item[4]) : "" }); }
                    catch { }
                }
            }
            if (p.Length >= 24)
            {
                settings.OverrideGroundBallistics = p[13] == "1";
                settings.ProjectileMassMultiplier = ParseStoredDouble(p[14], 1);
                settings.MuzzleVelocityMultiplier = ParseStoredDouble(p[15], 1);
                settings.ExplosiveMassMultiplier = ParseStoredDouble(p[16], 1);
                settings.PenetrationMultiplier = ParseStoredDouble(p[17], 1);
                settings.ReloadSeconds = ParseStoredDouble(p[18], 0);
                settings.RecoilMultiplier = ParseStoredDouble(p[19], 1);
                settings.EnginePowerMultiplier = ParseStoredDouble(p[20], 1);
                settings.VehicleMassMultiplier = ParseStoredDouble(p[21], 1);
                settings.ForwardSpeedMultiplier = ParseStoredDouble(p[22], 1);
                settings.ReverseSpeedMultiplier = ParseStoredDouble(p[23], 1);
            }
            if (p.Length >= 25)
            {
                try { settings.UserSightPath = FromB64(p[24]); }
                catch { settings.UserSightPath = ""; }
            }
            return settings;
        }

        internal static Dictionary<string, object> SerializeSettingsJson(AircraftSettings settings)
        {
            Dictionary<string, object> o = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (settings == null) return o;
            if (!settings.UseAllModifications) o.Add("use_all_modifications", false);
            if (settings.OverrideCountermeasures) o.Add("override_countermeasures", true);
            if (settings.FlareRounds != 45) o.Add("flare_rounds", settings.FlareRounds);
            if (settings.ChaffRounds != 45) o.Add("chaff_rounds", settings.ChaffRounds);
            if (settings.EnabledModifications.Count > 0)
            {
                List<object> list = new List<object>();
                foreach (string id in settings.EnabledModifications.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) list.Add(id);
                o.Add("enabled_modifications", list);
            }
            if (!settings.FullFuel) o.Add("full_fuel", false);
            if (settings.FuelMinutes != 60) o.Add("fuel_minutes", settings.FuelMinutes);
            if (settings.CountermeasureLoadouts.Count > 0)
            {
                Dictionary<string, object> cm = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (CountermeasureLoadout x in settings.CountermeasureLoadouts)
                {
                    Dictionary<string, object> sub = new Dictionary<string, object>();
                    if (x.Flares != 0) sub.Add("flares", x.Flares);
                    if (x.Chaff != 0) sub.Add("chaff", x.Chaff);
                    cm[x.Key] = sub;
                }
                o.Add("countermeasure_loadouts", cm);
            }
            if (settings.GunBeltSelections.Count > 0)
            {
                Dictionary<string, object> gb = new Dictionary<string, object>();
                foreach (KeyValuePair<int, string> kv in settings.GunBeltSelections.OrderBy(x => x.Key))
                    gb[kv.Key.ToString(CultureInfo.InvariantCulture)] = kv.Value;
                o.Add("gun_belts", gb);
            }
            if (settings.GroundAmmoLoadouts.Count > 0)
            {
                List<object> ga = new List<object>();
                foreach (GroundAmmoLoadout x in settings.GroundAmmoLoadouts.OrderBy(x => x.Slot))
                {
                    Dictionary<string, object> sub = new Dictionary<string, object>();
                    sub.Add("slot", x.Slot);
                    sub.Add("count", x.Count);
                    if (!String.IsNullOrWhiteSpace(x.SourceBlk)) sub.Add("source_blk", x.SourceBlk);
                    if (!String.IsNullOrWhiteSpace(x.BulletName)) sub.Add("bullet_name", x.BulletName);
                    if (!String.IsNullOrWhiteSpace(x.AmmoGroup)) sub.Add("ammo_group", x.AmmoGroup);
                    ga.Add(sub);
                }
                o.Add("ground_ammo_loadouts", ga);
            }
            if (settings.OverrideGroundBallistics) o.Add("override_ground_ballistics", true);
            if (settings.ProjectileMassMultiplier != 1.0) o.Add("projectile_mass_multiplier", settings.ProjectileMassMultiplier);
            if (settings.MuzzleVelocityMultiplier != 1.0) o.Add("muzzle_velocity_multiplier", settings.MuzzleVelocityMultiplier);
            if (settings.ExplosiveMassMultiplier != 1.0) o.Add("explosive_mass_multiplier", settings.ExplosiveMassMultiplier);
            if (settings.PenetrationMultiplier != 1.0) o.Add("penetration_multiplier", settings.PenetrationMultiplier);
            if (settings.ReloadSeconds != 0.0) o.Add("reload_seconds", settings.ReloadSeconds);
            if (settings.RecoilMultiplier != 1.0) o.Add("recoil_multiplier", settings.RecoilMultiplier);
            if (settings.EnginePowerMultiplier != 1.0) o.Add("engine_power_multiplier", settings.EnginePowerMultiplier);
            if (settings.VehicleMassMultiplier != 1.0) o.Add("vehicle_mass_multiplier", settings.VehicleMassMultiplier);
            if (settings.ForwardSpeedMultiplier != 1.0) o.Add("forward_speed_multiplier", settings.ForwardSpeedMultiplier);
            if (settings.ReverseSpeedMultiplier != 1.0) o.Add("reverse_speed_multiplier", settings.ReverseSpeedMultiplier);
            if (!String.IsNullOrWhiteSpace(settings.UserSightPath)) o.Add("user_sight_path", settings.UserSightPath);
            return o;
        }

        internal static AircraftSettings DeserializeSettingsJson(Dictionary<string, object> o)
        {
            if (o == null) return null;
            AircraftSettings s = new AircraftSettings();
            s.UseAllModifications = JsonBool(o, "use_all_modifications", true);
            s.OverrideCountermeasures = JsonBool(o, "override_countermeasures", false);
            s.FlareRounds = JsonInt(o, "flare_rounds", 45);
            s.ChaffRounds = JsonInt(o, "chaff_rounds", 45);
            object v;
            if (o.TryGetValue("enabled_modifications", out v) && v != null)
            {
                List<object> list = AsList(v);
                if (list != null)
                {
                    foreach (object x in list)
                    {
                        string id = Convert.ToString(x, CultureInfo.InvariantCulture);
                        if (!String.IsNullOrWhiteSpace(id)) s.EnabledModifications.Add(id);
                    }
                }
            }
            s.FullFuel = JsonBool(o, "full_fuel", true);
            s.FuelMinutes = Math.Max(5, Math.Min(60, JsonInt(o, "fuel_minutes", 60)));
            if (o.TryGetValue("countermeasure_loadouts", out v) && v is Dictionary<string, object>)
            {
                foreach (KeyValuePair<string, object> kv in (Dictionary<string, object>)v)
                {
                    Dictionary<string, object> sub = kv.Value as Dictionary<string, object>;
                    if (sub == null) continue;
                    s.CountermeasureLoadouts.Add(new CountermeasureLoadout { Key = kv.Key, Flares = JsonInt(sub, "flares", 0), Chaff = JsonInt(sub, "chaff", 0) });
                }
            }
            if (o.TryGetValue("gun_belts", out v) && v is Dictionary<string, object>)
            {
                foreach (KeyValuePair<string, object> kv in (Dictionary<string, object>)v)
                {
                    int group;
                    if (Int32.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out group) && group >= 0 && group <= 3)
                        s.GunBeltSelections[group] = Convert.ToString(kv.Value, CultureInfo.InvariantCulture);
                }
            }
            if (o.TryGetValue("ground_ammo_loadouts", out v) && v != null)
            {
                List<object> list = AsList(v);
                if (list != null)
                {
                    foreach (object x in list)
                    {
                        Dictionary<string, object> sub = x as Dictionary<string, object>;
                        if (sub == null) continue;
                        s.GroundAmmoLoadouts.Add(new GroundAmmoLoadout
                        {
                            Slot = JsonInt(sub, "slot", 0),
                            Count = JsonInt(sub, "count", 1),
                            SourceBlk = JsonStr(sub, "source_blk"),
                            BulletName = JsonStr(sub, "bullet_name"),
                            AmmoGroup = JsonStr(sub, "ammo_group")
                        });
                    }
                }
            }
            s.OverrideGroundBallistics = JsonBool(o, "override_ground_ballistics", false);
            s.ProjectileMassMultiplier = JsonDouble(o, "projectile_mass_multiplier", 1.0);
            s.MuzzleVelocityMultiplier = JsonDouble(o, "muzzle_velocity_multiplier", 1.0);
            s.ExplosiveMassMultiplier = JsonDouble(o, "explosive_mass_multiplier", 1.0);
            s.PenetrationMultiplier = JsonDouble(o, "penetration_multiplier", 1.0);
            s.ReloadSeconds = JsonDouble(o, "reload_seconds", 0.0);
            s.RecoilMultiplier = JsonDouble(o, "recoil_multiplier", 1.0);
            s.EnginePowerMultiplier = JsonDouble(o, "engine_power_multiplier", 1.0);
            s.VehicleMassMultiplier = JsonDouble(o, "vehicle_mass_multiplier", 1.0);
            s.ForwardSpeedMultiplier = JsonDouble(o, "forward_speed_multiplier", 1.0);
            s.ReverseSpeedMultiplier = JsonDouble(o, "reverse_speed_multiplier", 1.0);
            s.UserSightPath = JsonStr(o, "user_sight_path");
            return s;
        }

        private static List<object> AsList(object value)
        {
            if (value is List<object>) return (List<object>)value;
            if (value is object[]) return new List<object>((object[])value);
            if (value is System.Collections.ArrayList)
            {
                List<object> list = new List<object>();
                foreach (object x in (System.Collections.ArrayList)value) list.Add(x);
                return list;
            }
            return null;
        }

        private static bool JsonBool(Dictionary<string, object> o, string key, bool fallback)
        {
            object v;
            if (o.TryGetValue(key, out v) && v != null)
            {
                try { return Convert.ToBoolean(v, CultureInfo.InvariantCulture); }
                catch { }
            }
            return fallback;
        }

        private static int JsonInt(Dictionary<string, object> o, string key, int fallback)
        {
            object v;
            if (o.TryGetValue(key, out v) && v != null)
            {
                try { return Convert.ToInt32(v, CultureInfo.InvariantCulture); }
                catch { }
            }
            return fallback;
        }

        private static double JsonDouble(Dictionary<string, object> o, string key, double fallback)
        {
            object v;
            if (o.TryGetValue(key, out v) && v != null)
            {
                try { return Convert.ToDouble(v, CultureInfo.InvariantCulture); }
                catch { }
            }
            return fallback;
        }

        private static string JsonStr(Dictionary<string, object> o, string key)
        {
            object v;
            return o.TryGetValue(key, out v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : "";
        }

        private static double ParseStoredDouble(string value, double fallback)
        {
            double parsed;
            return Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ? parsed : fallback;
        }

        public static List<SavedPreset> Load()
        {
            List<SavedPreset> result = new List<SavedPreset>();
            if (!File.Exists(FilePath)) return result;
            foreach (string line in File.ReadAllLines(FilePath, Encoding.UTF8))
            {
                try
                {
                    string[] p = line.Split('\t');
                    if (p.Length < 3) continue;
                    SavedPreset preset = new SavedPreset { Name = FromB64(p[0]), AircraftId = FromB64(p[1]) };
                    if (p.Length >= 4) preset.Settings = DeserializeSettings(FromB64(p[3]));
                    string payload = FromB64(p[2]);
                    foreach (string record in payload.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] e = record.Split('|');
                        int slot, bullets;
                        if (e.Length < 12 || !Int32.TryParse(e[0], out slot) || !Int32.TryParse(e[6], out bullets)) continue;
                        preset.Entries.Add(new SavedPresetEntry
                        {
                            Slot = slot, Injected = e[1] == "1", Mount = FromB64(e[2]), Trigger = FromB64(e[3]), Blk = FromB64(e[4]),
                            Emitter = FromB64(e[5]), Bullets = bullets, Icon = FromB64(e[7]), Name = FromB64(e[8]), Category = FromB64(e[9]),
                            UnitMass = MainForm.ParseNumber(e[10]), TotalMass = MainForm.ParseNumber(e[11])
                        });
                    }
                    if (!String.IsNullOrWhiteSpace(preset.Name) && !String.IsNullOrWhiteSpace(preset.AircraftId)) result.Add(preset);
                }
                catch { }
            }
            return result.OrderBy(x => x.Name).ToList();
        }

        public static void Save(IEnumerable<SavedPreset> presets)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            List<string> lines = new List<string>();
            foreach (SavedPreset preset in presets.OrderBy(x => x.Name))
            {
                StringBuilder payload = new StringBuilder();
                foreach (SavedPresetEntry e in preset.Entries.OrderBy(x => x.Slot))
                {
                    payload.Append(e.Slot.ToString(CultureInfo.InvariantCulture)).Append('|')
                        .Append(e.Injected ? "1" : "0").Append('|').Append(B64(e.Mount)).Append('|').Append(B64(e.Trigger)).Append('|')
                        .Append(B64(e.Blk)).Append('|').Append(B64(e.Emitter)).Append('|').Append(e.Bullets.ToString(CultureInfo.InvariantCulture)).Append('|')
                        .Append(B64(e.Icon)).Append('|').Append(B64(e.Name)).Append('|').Append(B64(e.Category)).Append('|')
                        .Append(e.UnitMass.ToString("R", CultureInfo.InvariantCulture)).Append('|').Append(e.TotalMass.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
                }
                lines.Add(B64(preset.Name) + "\t" + B64(preset.AircraftId) + "\t" + B64(payload.ToString()) + "\t" + B64(SerializeSettings(preset.Settings)));
            }
            File.WriteAllLines(FilePath, lines.ToArray(), new UTF8Encoding(false));
        }
    }
    internal static class Json
    {
        public static string Serialize(object value)
        {
            try
            {
                System.Web.Script.Serialization.JavaScriptSerializer s = new System.Web.Script.Serialization.JavaScriptSerializer();
                s.MaxJsonLength = int.MaxValue;
                return s.Serialize(value);
            }
            catch { return "{}"; }
        }

        public static T Deserialize<T>(string text)
        {
            if (String.IsNullOrWhiteSpace(text)) return default(T);
            try
            {
                System.Web.Script.Serialization.JavaScriptSerializer s = new System.Web.Script.Serialization.JavaScriptSerializer();
                s.MaxJsonLength = int.MaxValue;
                return s.Deserialize<T>(text);
            }
            catch { return default(T); }
        }
    }
    internal static class ConfigStore
    {
        public static string Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniversalTestLab");
        private static string ConfigPath { get { return Path.Combine(Root, "config.json"); } }
        private static Dictionary<string, object> data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private static bool loaded;

        public static Dictionary<string, object> Data
        {
            get { if (!loaded) { loaded = true; Load(); } return data; }
        }

        public static string GetString(string key)
        {
            var d = Data;
            object v;
            return data.TryGetValue(key, out v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : "";
        }

        public static void SetString(string key, string value) { var d = Data; d[key] = value ?? ""; }

        public static Dictionary<string, object> GetObject(string key)
        {
            var d = Data;
            object v;
            if (data.TryGetValue(key, out v) && v is Dictionary<string, object>) return (Dictionary<string, object>)v;
            return null;
        }

        public static void SetObject(string key, Dictionary<string, object> value) { var d = Data; d[key] = value ?? new Dictionary<string, object>(); }

        public static List<object> GetList(string key)
        {
            var d = Data;
            object v;
            if (!data.TryGetValue(key, out v) || v == null) return null;
            if (v is List<object>) return (List<object>)v;
            if (v is object[]) return new List<object>((object[])v);
            if (v is System.Collections.ArrayList)
            {
                List<object> list = new List<object>();
                foreach (object x in (System.Collections.ArrayList)v) list.Add(x);
                return list;
            }
            return null;
        }

        public static void SetList(string key, List<object> value) { var d = Data; d[key] = value ?? new List<object>(); }

        private static void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    Dictionary<string, object> parsed = Json.Deserialize<Dictionary<string, object>>(File.ReadAllText(ConfigPath, Encoding.UTF8));
                    if (parsed != null) { data = parsed; return; }
                }
            }
            catch { }
            MigrateLegacy();
            Save();
        }

        private static void MigrateLegacy()
        {
            try
            {
                string gameFolderPath = Path.Combine(Root, "game_folder.txt");
                if (!data.ContainsKey("game_folder") && File.Exists(gameFolderPath))
                {
                    string p = File.ReadAllText(gameFolderPath, Encoding.UTF8).Trim().Trim('"');
                    if (!String.IsNullOrWhiteSpace(p)) data["game_folder"] = p;
                }
                string missionPath = Path.Combine(Root, "mission_options.txt");
                if (!data.ContainsKey("mission_options") && File.Exists(missionPath))
                {
                    Dictionary<string, object> mo = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (string line in File.ReadAllLines(missionPath, Encoding.UTF8))
                    {
                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;
                        string key = line.Substring(0, eq).Trim();
                        string value = line.Substring(eq + 1).Trim();
                        double number;
                        switch (key)
                        {
                            case "player_respawn_delay":
                            case "target_respawn_delay":
                            case "rearm_seconds":
                            case "rapid_fire_interval":
                                if (Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) mo[key] = number;
                                break;
                            case "limited_ammo":
                            case "rapid_fire_enabled":
                            case "rapid_fire_full_restore":
                            case "spawn_speed_auto":
                                mo[key] = value.Equals("1");
                                break;
                            case "spawn_mode":
                                mo[key] = value;
                                break;
                            case "spawn_speed_kmh":
                                { int kmh; if (Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out kmh)) mo[key] = kmh; }
                                break;
                            case "inject_cannon_blk":
                            case "inject_cannon_domain":
                            case "inject_cannon_unit":
                                mo[key] = value;
                                break;
                        }
                    }
                    data["mission_options"] = mo;
                }
                string aircraftPath = Path.Combine(Root, "aircraft_settings.txt");
                if (!data.ContainsKey("aircraft_settings") && File.Exists(aircraftPath))
                {
                    Dictionary<string, object> all = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (string line in File.ReadAllLines(aircraftPath, Encoding.UTF8))
                    {
                        if (String.IsNullOrWhiteSpace(line)) continue;
                        int tab = line.IndexOf('\t');
                        if (tab <= 0) continue;
                        try
                        {
                            string vehicleId = PresetStore.FromB64(line.Substring(0, tab));
                            AircraftSettings settings = PresetStore.DeserializeSettings(PresetStore.FromB64(line.Substring(tab + 1)));
                            if (String.IsNullOrWhiteSpace(vehicleId) || settings == null) continue;
                            all[vehicleId] = PresetStore.SerializeSettingsJson(settings);
                        }
                        catch { }
                    }
                    if (all.Count > 0) data["aircraft_settings"] = all;
                }
                string eraPath = Path.Combine(Root, "era_presets.tsv");
                if (!data.ContainsKey("era_presets") && File.Exists(eraPath))
                {
                    List<object> list = new List<object>();
                    string[] lines = File.ReadAllLines(eraPath, Encoding.UTF8);
                    for (int i = 1; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        if (String.IsNullOrWhiteSpace(line)) continue;
                        string[] parts = line.Split('\t');
                        if (parts.Length < 6) continue;
                        try
                        {
                            Dictionary<string, object> o = new Dictionary<string, object>();
                            o.Add("name", parts[0].Trim());
                            List<object> ground = new List<object>();
                            foreach (string g in parts[1].Trim().Split(',')) ground.Add(g.Trim());
                            o.Add("ground", ground);
                            List<object> air = new List<object>();
                            foreach (string a in parts[2].Trim().Split(',')) air.Add(a.Trim() == "-" ? null : a.Trim());
                            o.Add("air", air);
                            List<object> counts = new List<object>();
                            foreach (string c in parts[3].Trim().Split(',')) { int v; counts.Add(Int32.TryParse(c.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? (object)v : (object)0); }
                            o.Add("air_counts", counts);
                            o.Add("ship", parts[4].Trim());
                            int sc;
                            Int32.TryParse(parts[5].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out sc);
                            o.Add("ship_count", sc);
                            list.Add(o);
                        }
                        catch { }
                    }
                    if (list.Count > 0) data["era_presets"] = list;
                }
                string ammoPath = Path.Combine(Root, "ammo_loadouts.tsv");
                if (!data.ContainsKey("ammo_loadouts") && File.Exists(ammoPath))
                {
                    List<object> list = new List<object>();
                    string[] lines = File.ReadAllLines(ammoPath, Encoding.UTF8);
                    for (int i = 1; i < lines.Length; i++)
                    {
                        if (String.IsNullOrWhiteSpace(lines[i])) continue;
                        string[] p = lines[i].Split('\t');
                        if (p.Length < 5) continue;
                        Dictionary<string, object> o = new Dictionary<string, object>();
                        o.Add("name", p[0]);
                        o.Add("vehicle_id", p[1]);
                        List<object> slots = new List<object>();
                        for (int s = 0; s < 4; s++)
                        {
                            int b = 2 + s * 3;
                            if (b + 2 >= p.Length) break;
                            if (String.IsNullOrWhiteSpace(p[b]) || String.IsNullOrWhiteSpace(p[b + 1])) continue;
                            Dictionary<string, object> slot = new Dictionary<string, object>();
                            slot.Add("slot", s);
                            int count;
                            Int32.TryParse(p[b + 2], NumberStyles.Integer, CultureInfo.InvariantCulture, out count);
                            slot.Add("count", Math.Max(1, count));
                            slot.Add("source_blk", p[b]);
                            slot.Add("bullet_name", p[b + 1]);
                            slots.Add(slot);
                        }
                        o.Add("slots", slots);
                        list.Add(o);
                    }
                    if (list.Count > 0) data["ammo_loadouts"] = list;
                }
                string sessionPath = Path.Combine(Root, "session.txt");
                if (!data.ContainsKey("session") && File.Exists(sessionPath))
                {
                    Dictionary<string, object> kv = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    foreach (string line in File.ReadAllLines(sessionPath, Encoding.UTF8))
                    {
                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;
                        kv[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                    }
                    if (kv.Count > 0) data["session"] = kv;
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                var d = Data;
                Directory.CreateDirectory(Root);
                string temp = ConfigPath + ".tmp";
                File.WriteAllText(temp, Json.Serialize(data), new UTF8Encoding(false));
                if (File.Exists(ConfigPath)) File.Delete(ConfigPath);
                File.Move(temp, ConfigPath);
            }
            catch { }
        }
    }
    internal static class SettingsStore
    {
        public static string FilePath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniversalTestLab", "game_folder.txt"); }
        }

        public static string LoadGameFolder()
        {
            try
            {
                string path = ConfigStore.GetString("game_folder").Trim().Trim('"');
                if (String.IsNullOrWhiteSpace(path)) return "";
                path = Path.GetFullPath(path);
                return File.Exists(Path.Combine(path, "aces.vromfs.bin")) ? path : "";
            }
            catch { return ""; }
        }

        public static void SaveGameFolder(string path)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(path)) return;
                path = Path.GetFullPath(path.Trim().Trim('"'));
                if (!File.Exists(Path.Combine(path, "aces.vromfs.bin"))) return;
                ConfigStore.SetString("game_folder", path);
                ConfigStore.Save();
            }
            catch { }
        }
    }
    internal static class Embedded
    {
        public static byte[] Bytes(string name)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (stream == null) throw new InvalidOperationException("Embedded resource is missing: " + name);
                using (MemoryStream memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
        }

        public static string Text(string name) { return Encoding.UTF8.GetString(Bytes(name)); }
    }
}
