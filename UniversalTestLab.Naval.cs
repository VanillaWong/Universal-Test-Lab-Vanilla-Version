using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UniversalTestLab
{
    // One Weapon entry of a ship's commonWeapons, as produced by
    // tools/build-naval-weapons.js.
    internal sealed class NavalGroup
    {
        public string group = "";
        public string trigger = "";
        public string blk = "";
        public string guidance = "";
        public string cone = "";
        public bool semiActive;
        public bool fixedMount;
        public List<string> containers = new List<string>();

        // A guided weapon needs the mount to be able to face the target. Fixed VLS
        // blocks cannot, so the fire cone is relaxed instead (proven on the 054A).
        public bool NeedsRelaxedCone { get { return fixedMount && !String.IsNullOrEmpty(guidance); } }
        // Semi-active rounds need a continuous illuminating radar. Turning them into
        // active seekers lets any TWS track feed them, exactly like ground SAMs.
        public bool NeedsActiveSeeker { get { return semiActive; } }
    }

    internal sealed class NavalShip
    {
        public string nation = "";
        public int rank;
        public bool aiOnly;
        public List<NavalGroup> groups = new List<NavalGroup>();
    }

    internal sealed class NavalMissile
    {
        public string name = "";
        public string blk = "";
        public string container = "";
        public string guidance = "";
        public bool semiActive;
        public bool isAam;
        public int caliber;
        public double mass;
        public List<string> ships = new List<string>();
    }

    internal sealed class NavalWeaponFile
    {
        public Dictionary<string, NavalShip> ships = new Dictionary<string, NavalShip>(StringComparer.OrdinalIgnoreCase);
        public List<NavalMissile> missiles = new List<NavalMissile>();
    }

    internal static class NavalWeapons
    {
        private static NavalWeaponFile data;

        internal static NavalWeaponFile Data
        {
            get
            {
                if (data != null) return data;
                try
                {
                    System.Web.Script.Serialization.JavaScriptSerializer serializer =
                        new System.Web.Script.Serialization.JavaScriptSerializer();
                    serializer.MaxJsonLength = 64 * 1024 * 1024;
                    data = serializer.Deserialize<NavalWeaponFile>(Embedded.Text("UTL.naval_weapons.json"));
                }
                catch { data = new NavalWeaponFile(); }
                if (data == null) data = new NavalWeaponFile();
                if (data.ships == null) data.ships = new Dictionary<string, NavalShip>(StringComparer.OrdinalIgnoreCase);
                if (data.missiles == null) data.missiles = new List<NavalMissile>();
                return data;
            }
        }

        internal static NavalShip Ship(string id)
        {
            if (String.IsNullOrWhiteSpace(id)) return null;
            NavalShip ship;
            return Data.ships.TryGetValue(id, out ship) ? ship : null;
        }

        internal static bool IsAiOnly(string id)
        {
            NavalShip ship = Ship(id);
            return ship != null && ship.aiOnly;
        }

        // Mission ammo slots: exactly one slot per weapon group, in file order, at
        // most four. A group whose launcher declares no named container still takes
        // its slot with an empty string: skipping it shifts every later group into the
        // wrong slot, so the engine hands HHQ-9 rounds to the HHQ-10 mount and the
        // missile ends up with no round type at all.
        internal static List<string> MissionAmmo(string shipId)
        {
            List<string> result = new List<string>();
            NavalShip ship = Ship(shipId);
            if (ship == null) return result;
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> guided = new List<string>();
            List<string> guns = new List<string>();
            int guidedGroups = 0;
            foreach (NavalGroup group in ship.groups)
            {
                if (String.IsNullOrEmpty(group.guidance) || !seen.Add(group.group ?? "")) continue;
                guidedGroups++;
                string container = (group.containers ?? new List<string>())
                    .FirstOrDefault(c => !String.IsNullOrWhiteSpace(c));
                guided.Add(container ?? "");
            }
            // A shell list that starts with gun rounds pushes the missiles out of the
            // four mission slots and the launcher reports "out of ammunition". Ask3lad
            // lists only the guided rounds for missile ships; follow that whenever the
            // hull carries more than one guided group.
            if (guidedGroups > 1)
            {
                result.AddRange(guided);
                if (result.Count >= 4) return result.GetRange(0, 4);
            }
            seen.Clear();
            foreach (NavalGroup group in ship.groups)
            {
                if (!seen.Add(group.group ?? "")) continue;
                if (guidedGroups > 1 && !String.IsNullOrEmpty(group.guidance)) continue;
                string container = (group.containers ?? new List<string>())
                    .FirstOrDefault(c => !String.IsNullOrWhiteSpace(c));
                result.Add(container ?? "");
                if (result.Count == 4) break;
            }
            return result;
        }
    }

    // Builds the player-owned copy of an AI ship. The game's AI hulls keep the
    // missile hardware in states a player cannot use: fixed launcher mounts, AI-only
    // sensor files and semi-active rounds that need an illumination channel the
    // player-side fire control never opens. Patching the per-player copy fixes all
    // three without touching any shipped game file.
    internal static class NavalShipPatcher
    {
        internal static string Build(string root, string shipId, out List<string> written)
        {
            written = new List<string>();
            string source = File.ReadAllText(MainForm.ExtractGameBlk(root, "gamedata/units/ships/" + shipId + ".blk"), Encoding.UTF8);
            NavalShip ship = NavalWeapons.Ship(shipId);
            if (ship == null) return source;
            source = PlayerRadars(source, ship);
            source = DeclareAmmo(root, source, shipId);

            Dictionary<string, NavalGroup> byBlk = new Dictionary<string, NavalGroup>(StringComparer.OrdinalIgnoreCase);
            foreach (NavalGroup group in ship.groups)
                if (!String.IsNullOrWhiteSpace(group.blk) && !byBlk.ContainsKey(group.blk)) byBlk[group.blk] = group;

            // Which groups already carry a guided weapon: guns sharing such a group
            // are what makes "CIWS and missiles are one button" on some AI ships.
            HashSet<string> guidedGroups = new HashSet<string>(
                ship.groups.Where(g => !String.IsNullOrEmpty(g.guidance)).Select(g => g.group ?? ""),
                StringComparer.OrdinalIgnoreCase);

            List<BlockSpan> weapons = BlkTools.Blocks(source, "Weapon");
            // Patch from the end so earlier spans stay valid.
            for (int i = weapons.Count - 1; i >= 0; i--)
            {
                BlockSpan span = weapons[i];
                string text = span.Text;
                string blk = BlkTools.Field(text, "blk", "t") ?? "";
                if (blk.Length == 0) continue;
                NavalGroup group;
                if (!byBlk.TryGetValue(blk, out group)) continue;
                string updated = text;

                if (group.NeedsRelaxedCone)
                    updated = Regex.Replace(updated, @"fireConeAngle:r\s*=\s*[\d.]+", "fireConeAngle:r = 180");

                if (String.IsNullOrEmpty(group.guidance) && guidedGroups.Contains(group.group ?? ""))
                    updated = Regex.Replace(updated, @"(triggerGroup:t\s*=\s*"")[a-z_]+("")", "${1}machinegun${2}");

                // "special" is an AI-only slot on most hulls: the player HUD never shows
                // it, so a missile mounted there simply does not exist for a player.
                if (!String.IsNullOrEmpty(group.guidance) &&
                    String.Equals(group.group, "special", StringComparison.OrdinalIgnoreCase))
                    updated = Regex.Replace(updated, @"(triggerGroup:t\s*=\s*"")special("")", "${1}rockets${2}");

                if (!String.IsNullOrEmpty(group.guidance))
                {
                    string patchedBlk = GuidedCopy(root, blk, written);
                    if (patchedBlk != null) updated = updated.Replace(blk, patchedBlk);
                }

                if (!String.Equals(updated, text, StringComparison.Ordinal))
                    source = BlkTools.ReplaceSpan(source, span, updated);
            }
            return source;
        }

        // The mission's bullets0..3 must name a bulletMod the unit declares. AI ships
        // list only their gun rounds in modifications, so a missile container written
        // into a mission slot fails with "Cannot find bulletMod for bullet ...".
        // Declaring an empty block registers the container without changing research.
        private static string DeclareAmmo(string root, string text, string shipId)
        {
            List<string> ammo = NavalWeapons.MissionAmmo(shipId);
            if (ammo.Count == 0) return text;
            BlockSpan mods = BlkTools.FirstBlock(text, "modifications", 0);
            if (mods == null) return text;
            StringBuilder addition = new StringBuilder();
            foreach (string container in ammo)
            {
                if (String.IsNullOrWhiteSpace(container)) continue;
                if (Regex.IsMatch(mods.Text, "(?m)^\\t" + Regex.Escape(container) + @"\s*\{")) continue;
                addition.AppendLine("\t" + container + " {");
                addition.AppendLine("\t}");
            }
            if (addition.Length == 0) return text;
            string updated = mods.Text.Insert(mods.Text.LastIndexOf('}'), addition.ToString());
            return BlkTools.ReplaceSpan(text, mods, updated);
        }

        // AI hulls carry bot-driven tracking radars (the *_ai / *_ec variants). A
        // player cannot drive their state machines, which shows up as radar modes
        // flipping on their own and missiles launching without a commanded lock.
        // Swap them for the player-side equivalents used by tech-tree ships.
        private static string PlayerRadars(string text, NavalShip ship)
        {
            // Radar substitution is currently disabled while we test the stock sensor
            // set on the 052D: its own SPY-1 already provides the track and the swap
            // may have been hiding the missile guidance problem. Re-enable the four
            // Replace calls below if lock behaviour regresses.
            // text = text.Replace("gamedata/sensors/naval/su_mr_90_ai.blk", "gamedata/sensors/naval/su_mr_90.blk");
            // text = text.Replace("gamedata/sensors/naval/cn_hljp_345_ai.blk", "gamedata/sensors/naval/us_an_spg_51.blk");
            // text = text.Replace("gamedata/sensors/naval/us_an_spy_1_ai.blk", "gamedata/sensors/naval/us_an_spg_51.blk");
            // text = text.Replace("gamedata/sensors/naval/us_an_spg_51_ec.blk", "gamedata/sensors/naval/us_an_spg_51.blk");
            return EnsureOpticalSight(text, ship);
        }

        // Optical-guided rounds (HHQ-10, RAM, SM-2 IIIB) need a sight to lock with.
        // AI hulls never carry one because their bots aim through the unit itself.
        private static string EnsureOpticalSight(string text, NavalShip ship)
        {
            bool needsOptical = ship.groups.Any(g => (g.guidance ?? "").IndexOf("optical", StringComparison.OrdinalIgnoreCase) >= 0);
            if (!needsOptical) return text;
            if (text.IndexOf("ship_optical_sight.blk", StringComparison.Ordinal) >= 0) return text;
            BlockSpan sensors = BlkTools.FirstBlock(text, "sensors", 0);
            if (sensors == null) return text;
            Match dm = Regex.Match(sensors.Text, "(?m)^\\s*dmPart:t\\s*=\\s*\"([^\"]+)\"");
            string dmPart = dm.Success ? dm.Groups[1].Value : "";
            StringBuilder entry = new StringBuilder();
            entry.Append("\tsensor {").Append(Environment.NewLine);
            entry.Append("\t\tblk:t = \"gamedata/sensors/naval/ship_optical_sight.blk\"").Append(Environment.NewLine);
            if (dmPart.Length > 0) entry.Append("\t\tdmPart:t = \"").Append(dmPart).Append("\"").Append(Environment.NewLine);
            entry.Append("\t}").Append(Environment.NewLine);
            string updated = sensors.Text.Insert(sensors.Text.LastIndexOf('}'), entry.ToString());
            return BlkTools.ReplaceSpan(text, sensors, updated);
        }

        // Copies a guided launcher and removes the two flags that stop a player from
        // using it: semi-active terminal homing (needs an illumination channel the
        // player-side fire control never opens) and the pre-launch seeker cage
        // (uncageBeforeLaunch keeps the round ballistic unless a lock UI is present).
        // Legacy naval SAMs (HHQ-9) drive their self-homing autopilot with three fixed
        // gains, so they turn like barges. Newer rounds (SM-6) use a gain schedule:
        // base speed, proportional-navigation multiplier, time-to-hit gains and four
        // PID stages. Reusing that schedule is what gives the US rounds their agility.
        private static string DonorAutopilot(string root, string weapon)
        {
            BlockSpan mine = BlkTools.FirstBlock(weapon, "selfHomingAutopilot", 0);
            if (mine == null || mine.Text.IndexOf("pid0", StringComparison.Ordinal) >= 0) return weapon;
            try
            {
                string donor = File.ReadAllText(MainForm.ExtractGameBlk(root, "gamedata/weapons/navalModels_weapons/rocket_rim174_sm6_vls_naval_rocket_launcher.blk"), Encoding.UTF8);
                BlockSpan good = BlkTools.FirstBlock(donor, "selfHomingAutopilot", 0);
                if (good == null) return weapon;
                return BlkTools.ReplaceSpan(weapon, mine, good.Text);
            }
            catch { return weapon; }
        }

        // The control law is only half of the agility story: HHQ-9 also flies with
        // finsAoa 0.125 and loadFactorMax 20 against 0.35 / 24 on the SM-6, so the
        // fins barely deflect no matter what the autopilot asks for. Match the US
        // figures on the player copy.
        private static string RestoreFinAuthority(string weapon)
        {
            return weapon
                .Replace("finsAoaHor:r = 0.125", "finsAoaHor:r = 0.35")
                .Replace("finsAoaVer:r = 0.125", "finsAoaVer:r = 0.35")
                .Replace("loadFactorMax:r = 20", "loadFactorMax:r = 24");
        }

        // Optical point-defence rounds ship with a 15 degree seeker cage; the US round
        // that hits reliably uses 90 degrees and takes its designation from the radar.
        private static string WidenOpticalSeeker(string weapon)
        {
            if (weapon.IndexOf("opticalSeeker", StringComparison.Ordinal) < 0) return weapon;
            return weapon
                .Replace("lockAngleMax:r = 15", "lockAngleMax:r = 90")
                .Replace("angleMax:r = 40", "angleMax:r = 90")
                .Replace("designationSourceType:t = \"ins\"", "designationSourceType:t = \"radar\"");
        }

        private static string GuidedCopy(string root, string relative, List<string> written)
        {
            string fileName = Path.GetFileName(relative);
            string outputRelative = "gameData/weapons/navalModels_weapons/utl_" + fileName;
            string outputPath = Path.Combine(root, @"content\pkg_local", outputRelative.Replace('/', '\\'));
            string patchedBlk = "gamedata/weapons/navalModels_weapons/utl_" + fileName;
            if (written.Contains(outputPath)) return patchedBlk;
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            string source = File.ReadAllText(MainForm.ExtractGameBlk(root, relative.Replace('\\', '/')), Encoding.UTF8);
            string patched = source
                .Replace("semiActive:b = true", "semiActive:b = false")
                .Replace("uncageBeforeLaunch:b = true", "uncageBeforeLaunch:b = false")
                .Replace("band:i = 9", "band:i = 8");
            patched = DonorAutopilot(root, patched);
            patched = RestoreFinAuthority(patched);
            patched = WidenOpticalSeeker(patched);

            if (String.Equals(patched, source, StringComparison.Ordinal)) return null;
            File.WriteAllBytes(outputPath, new UTF8Encoding(false).GetBytes(patched));
            written.Add(outputPath);
            return patchedBlk;
        }
    }
}
