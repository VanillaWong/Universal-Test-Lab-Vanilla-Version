// UniversalTestLab.MainForm.Generate.cs
// Aircraft/ground mission builders, ammo and module resolution (segment 3/5).
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
        private GeneratedAircraft BuildCustomAircraft(string root, Aircraft target, string token)
        {
            string fm;
            if (IsFpvDrone(target))
            {
                string quad = File.ReadAllText(ExtractGameBlk(root, "gamedata/flightmodels/uav_quadcopter.blk"), Encoding.UTF8);
                string originalFpv = File.ReadAllText(ExtractGameBlk(root, "gamedata/flightmodels/uav_inf_fpv_strike_drone.blk"), Encoding.UTF8);
                fm = BuildDownloadedFpvVariant(quad, originalFpv);
            }
            else fm = File.ReadAllText(ExtractGameBlk(root, "gamedata/flightmodels/" + target.Id + ".blk"), Encoding.UTF8);
            int spawnSpeedKmh = ResolveConfiguredSpawnSpeed(target, fm, MissionSettings.Current);
            AircraftSettings settings = GetAircraftSettings(target);
            bool helicopter = IsHelicopter(target, fm);
            // Legacy aircraft (A-20G, A-26, A6M Zero, ...) have no WeaponSlot
            // tree in the flight model; their external stores live in whole
            // loadout presets that the mission must reference by name
            // (weapons:t = <preset name>), exactly like the hangar loadout UI.
            bool presetStyle = !BlkTools.Blocks(fm, "WeaponSlot").Any();
            List<string> auxiliaryPaths = new List<string>();
            Dictionary<string, string> customCountermeasureBelts = PrepareCountermeasureBeltsByLoadout(root, token, settings, auxiliaryPaths);
            ApplyCountermeasureSettings(ref fm, settings, customCountermeasureBelts);
            if (helicopter)
            {
                MaterializeHelicopterThermalSight(ref fm, settings);
                fm = EnsureHelicopterExperienceClass(fm);
            }
            if (!HasExplicitFlightModel(fm))
            {
                ExtractGameBlk(root, "gamedata/flightmodels/fm/" + target.Id + ".blk");
                EnsureExplicitFlightModel(ref fm, target.Id);
            }
            RemoveFuelTankPresets(ref fm);
            string classId = "utl_run_" + token + "_player";
            string presetId = "utl_run_" + token + "_loadout";
            string presetOut = null;
            StringBuilder loadout = new StringBuilder();
            if (presetStyle)
            {
                // The single preset-style station (slot 0) carries the selected
                // native loadout scheme; without a selection fall back to the
                // vehicle's stock preset so the aircraft still spawns armed.
                PylonAssignment scheme = assignments.Values.FirstOrDefault(x => x != null && x.Weapon != null);
                if (scheme != null && scheme.Injected)
                {
                    // Legacy aircraft have no WeaponSlot pylon tree, so injected
                    // ordnance cannot be attached through a station mount. Rebuild
                    // the stock loadout scheme instead: keep its emitter nodes (the
                    // model's store hooks) and swap every store definition for the
                    // injected weapon, then publish the result as a generated preset
                    // that the mission references by name.
                    string weaponBlk = PrepareInjectedWeapon(root, scheme.Weapon);
                    string basePreset = String.Empty;
                    string stockPreset = String.IsNullOrWhiteSpace(target.DefaultPreset) ? null : target.DefaultPreset;
                    if (stockPreset != null)
                    {
                        Match stock = Regex.Match(fm, @"(?s)preset\s*\{\s*name:t\s*=\s*""" + Regex.Escape(stockPreset) + @"""\s*blk:t\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase);
                        if (stock.Success)
                        {
                            string relative = Regex.Replace(stock.Groups[1].Value.Replace('\\', '/'), @"(?i)^gameData/FlightModels/", "gamedata/flightmodels/");
                            basePreset = File.ReadAllText(ExtractGameBlk(root, relative), Encoding.UTF8);
                        }
                    }
                    if (String.IsNullOrWhiteSpace(basePreset))
                    {
                        basePreset = "Weapon {" + Environment.NewLine
                            + "\ttrigger:t = \"" + scheme.Weapon.Trigger + "\"" + Environment.NewLine
                            + "\tblk:t = \"" + weaponBlk + "\"" + Environment.NewLine
                            + "\temitter:t = \"inj1\"" + Environment.NewLine
                            + "\texternal:b = true" + Environment.NewLine
                            + "\tseparate:b = true" + Environment.NewLine
                            + "\tbullets:i = " + Math.Max(1, scheme.Weapon.Bullets).ToString(CultureInfo.InvariantCulture) + Environment.NewLine + "}";
                    }
                    else
                    {
                        BlockSpan[] stores = BlkTools.Blocks(basePreset, "Weapon").OrderByDescending(x => x.Start).ToArray();
                        if (stores.Length == 0)
                        {
                            basePreset = basePreset.TrimEnd() + Environment.NewLine + "Weapon {" + Environment.NewLine
                                + "\ttrigger:t = \"" + scheme.Weapon.Trigger + "\"" + Environment.NewLine
                                + "\tblk:t = \"" + weaponBlk + "\"" + Environment.NewLine
                                + "\temitter:t = \"inj1\"" + Environment.NewLine
                                + "\texternal:b = true" + Environment.NewLine
                                + "\tseparate:b = true" + Environment.NewLine
                                + "\tbullets:i = " + Math.Max(1, scheme.Weapon.Bullets).ToString(CultureInfo.InvariantCulture) + Environment.NewLine + "}";
                        }
                        else
                        {
                            foreach (BlockSpan store in stores)
                            {
                                string block = Regex.Replace(store.Text, @"(?m)^\s*blk:t\s*=\s*""[^""]*""", "blk:t = \"" + weaponBlk + "\"");
                                block = Regex.Replace(block, @"(?m)^\s*trigger:t\s*=\s*""[^""]*""", "trigger:t = \"" + scheme.Weapon.Trigger + "\"");
                                basePreset = basePreset.Substring(0, store.Start) + block + basePreset.Substring(store.End);
                            }
                        }
                    }
                    presetId = "utl_run_" + token + "_loadout";
                    RegisterPreset(ref fm, presetId);
                    presetOut = Path.Combine(root, @"content\pkg_user\gameData\flightModels\weaponPresets", presetId + ".blk");
                    WriteBytes(presetOut, new UTF8Encoding(false).GetBytes(basePreset));
                }
                else if (scheme != null && !String.IsNullOrWhiteSpace(scheme.Weapon.Mount))
                {
                    presetId = scheme.Weapon.Mount;
                }
                else
                {
                    presetId = String.IsNullOrWhiteSpace(target.DefaultPreset) ? presetId : target.DefaultPreset;
                }
            }
            else
            {
                HashSet<int> assignedSlots = new HashSet<int>(assignments.Keys);
                // Native helicopter presets contain external stations only. The turret,
                // fixed gun and countermeasure launchers remain in commonWeapons and are
                // attached implicitly by the helicopter usermodel. Serializing them into
                // the preset turns the common group into the selected secondary group and
                // prevents the normal external-weapon triggers from firing.
                AppendCommonWeaponsToLoadout(loadout, fm, assignedSlots, helicopter);
                // Native War Thunder helicopter presets are serialized by numeric station,
                // not by the mirrored visual order used by the loadout UI. A 1,4,2,3 file
                // mounts the stores, but the in-flight selector only indexes part of it.
                foreach (PylonAssignment assignment in OrderAssignmentsForPreset(assignments.Values))
                {
                    string mount;
                    if (!assignment.Injected)
                    {
                        mount = assignment.Weapon.Mount;
                        if (String.IsNullOrEmpty(mount)) throw new InvalidOperationException("Native mount information is missing for station " + assignment.Pylon.Slot + ".");
                    }
                    else
                    {
                        // Keep the aircraft's native mount ID. The F2 pylon display is built from
                        // these registered station entries and ignores newly appended ad-hoc IDs.
                        mount = assignment.Pylon.AnchorMount;
                        string weaponBlk = PrepareInjectedWeapon(root, assignment.Weapon);
                        AddInjectedMount(ref fm, assignment.Pylon, assignment.Weapon, mount, weaponBlk);
                    }
                    loadout.AppendLine("Weapon {");
                    loadout.AppendLine("\tslot:i = " + assignment.Pylon.Slot.ToString(CultureInfo.InvariantCulture));
                    loadout.AppendLine("\tpreset:t = \"" + mount + "\"");
                    loadout.AppendLine("}");
                }
                string modelId = BlkTools.Field(fm, "model", "t");
                RegisterPreset(ref fm, presetId);
                presetOut = Path.Combine(root, @"content\pkg_user\gameData\flightModels\weaponPresets", presetId + ".blk");
                WriteBytes(presetOut, new UTF8Encoding(false).GetBytes(loadout.ToString()));
            }
            string fmOut = Path.Combine(root, @"content\pkg_user\gameData\flightModels", classId + ".blk");
            WriteBytes(fmOut, new UTF8Encoding(false).GetBytes(fm));
            GeneratedAircraft generated = new GeneratedAircraft { ClassId = classId, PresetId = presetId, ModelId = BlkTools.Field(fm, "model", "t"), FlightModelPath = fmOut, PresetPath = presetOut, SpawnSpeedKmh = spawnSpeedKmh };
            generated.AuxiliaryPaths.AddRange(auxiliaryPaths);
            return generated;
        }

        private static string ReplaceFirstScaledNumber(string text, string field, double multiplier)
        {
            Regex regex = new Regex(@"(?m)^(\s*)" + Regex.Escape(field) + @":r\s*=\s*(-?[0-9]+(?:\.[0-9]+)?)\s*$", RegexOptions.IgnoreCase);
            return regex.Replace(text, delegate(Match match)
            {
                double native = ParseNumber(match.Groups[2].Value);
                return match.Groups[1].Value + field + ":r = " + (native * multiplier).ToString("0.######", CultureInfo.InvariantCulture);
            }, 1);
        }

        private static string ReplaceAllScaledNumbers(string text, string field, double multiplier)
        {
            Regex regex = new Regex(@"(?m)^(\s*)" + Regex.Escape(field) + @":r\s*=\s*(-?[0-9]+(?:\.[0-9]+)?)\s*$", RegexOptions.IgnoreCase);
            return regex.Replace(text, delegate(Match match)
            {
                double native = ParseNumber(match.Groups[2].Value);
                return match.Groups[1].Value + field + ":r = " + (native * multiplier).ToString("0.######", CultureInfo.InvariantCulture);
            });
        }

        private static string SetOrInsertNumber(string block, string field, double value)
        {
            Regex regex = new Regex(@"(?m)^(\s*)" + Regex.Escape(field) + @":r\s*=\s*-?[0-9]+(?:\.[0-9]+)?\s*$", RegexOptions.IgnoreCase);
            string formatted = value.ToString("0.######", CultureInfo.InvariantCulture);
            if (regex.IsMatch(block)) return regex.Replace(block, delegate(Match match) { return match.Groups[1].Value + field + ":r = " + formatted; }, 1);
            int close = block.LastIndexOf('}');
            return close < 0 ? block : block.Insert(close, "\t\t" + field + ":r = " + formatted + Environment.NewLine);
        }

        internal static string SetOrInsertString(string text, string field, string value)
        {
            if (String.IsNullOrWhiteSpace(text) || String.IsNullOrWhiteSpace(field)) return text;
            string clean = (value ?? "").Replace("\"", "");
            Regex regex = new Regex(@"(?m)^(\s*)" + Regex.Escape(field) + @":t\s*=\s*""[^""]*""\s*$", RegexOptions.IgnoreCase);
            if (regex.IsMatch(text)) return regex.Replace(text, delegate(Match match) { return match.Groups[1].Value + field + ":t = \"" + clean + "\""; }, 1);
            return field + ":t = \"" + clean + "\"" + Environment.NewLine + text;
        }

        private static string CustomizeGroundBullet(string bullet, AircraftSettings settings)
        {
            if (settings == null || !settings.OverrideGroundBallistics) return bullet;
            bullet = ReplaceFirstScaledNumber(bullet, "mass", settings.ProjectileMassMultiplier);
            bullet = ReplaceFirstScaledNumber(bullet, "speed", settings.MuzzleVelocityMultiplier);
            bullet = ReplaceAllScaledNumbers(bullet, "explosiveMass", settings.ExplosiveMassMultiplier);
            bullet = ReplaceAllScaledNumbers(bullet, "armorPower", settings.PenetrationMultiplier);
            Regex table = new Regex(@"(?m)^(\s*ArmorPower\d*m:p2\s*=\s*)(-?[0-9]+(?:\.[0-9]+)?)(\s*,\s*-?[0-9]+(?:\.[0-9]+)?\s*)$", RegexOptions.IgnoreCase);
            bullet = table.Replace(bullet, delegate(Match match)
            {
                return match.Groups[1].Value + (ParseNumber(match.Groups[2].Value) * settings.PenetrationMultiplier).ToString("0.######", CultureInfo.InvariantCulture) + match.Groups[3].Value;
            });
            return bullet;
        }

        private static void AppendScaledGroundOverride(StringBuilder output, string nativeUnit, string field, double multiplier)
        {
            if (output == null || String.IsNullOrWhiteSpace(nativeUnit) || Math.Abs(multiplier - 1.0) < 0.000001) return;
            Match match = Regex.Match(nativeUnit, @"(?m)^\s*" + Regex.Escape(field) + @":r\s*=\s*(-?[0-9]+(?:\.[0-9]+)?)\s*$", RegexOptions.IgnoreCase);
            if (!match.Success) return;
            double native = ParseNumber(match.Groups[1].Value);
            output.AppendLine("\"@override:" + field + "\":r = " + (native * multiplier).ToString("0.######", CultureInfo.InvariantCulture));
        }

        private static int GroundAmmoHudPriority(string type)
        {
            string normalized = (type ?? "").Trim().ToUpperInvariant();
            if (normalized.Contains("APFSDS")) return 1000;
            if (normalized.Contains("APDS")) return 900;
            if (normalized.Contains("APHE")) return 820;
            if (normalized.Contains("APCBC")) return 800;
            if (normalized == "AP" || normalized.StartsWith("AP ")) return 760;
            if (normalized.Contains("HEAT-FS")) return 720;
            if (normalized.Contains("HEAT")) return 680;
            if (normalized.Contains("ATGM")) return 650;
            if (normalized.Contains("SAP")) return 600;
            if (normalized.Contains("HESH")) return 520;
            if (normalized.Contains("HE")) return 480;
            if (normalized.Contains("SMOKE")) return 100;
            return 300;
        }

        private List<GroundAmmoLoadout> ResolveGroundMissionAmmo(Aircraft target, AircraftSettings settings, string cannonPathOverride = null)
        {
            List<GroundAmmoLoadout> configured = settings.GroundAmmoLoadouts
                .Where(x => x != null && x.Slot >= 0 && x.Slot < 4 && x.Count > 0)
                .OrderBy(x => x.Slot)
                .Select(x => x.Copy())
                .ToList();
            if (configured.Count > 0) return configured;
            // No user configuration: leave the mission ammo slots empty (bullets0-3:t=""
            // with count0=9999) so the game applies the vehicle's native default
            // ammunition configuration - Ask3lad writes exactly this and the game
            // loads the preset default (e.g. Pantsir-SM-SV gets its stock gun belt).
            // A STOCK slot (BulletName empty, SourceBlk "stock:<cal>") is kept on purpose:
            // Ask3lad writes bulletsN:t="" with a count to load the native default round
            // (e.g. T-80BVM 3BK18M) alongside other slots.
            return configured;
        }

        // Default ammunition count for the native main weapon. Belt weapons (guns,
        // calibre <=40mm) report their load in belt chains (total rounds / belt size,
        // e.g. Pantsir-SM-SV 30mm: 1404 / 351 = 4 chains) - writing the raw round
        // count there makes the game interpret it as chain count and crash. Tank
        // guns keep the plain native round count.
        private int GroundDefaultRoundCount(Aircraft target, string cannonPath)
        {
            int maxAmmo = target.MaxAmmo > 0 ? target.MaxAmmo : 9999;
            if (String.IsNullOrWhiteSpace(cannonPath)) return maxAmmo;
            int cal = GroundCalibre(cannonPath);
            if (cal <= 0 || cal > 40) return maxAmmo;
            try
            {
                string text = File.ReadAllText(ExtractGameBlk(gameFolder.Text, NormalizeGameResourcePath(cannonPath)), Encoding.UTF8);
                Match m = Regex.Match(text, @"(?m)^\s*bullets:i\s*=\s*(\d+)\s*$");
                if (m.Success)
                {
                    int beltSize = Int32.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                    if (beltSize > 0) return Math.Max(1, maxAmmo / beltSize);
                }
            }
            catch { }
            return maxAmmo;
        }

        private static int GroundCalibre(string blk)
        {
            if (String.IsNullOrWhiteSpace(blk)) return 0;
            Match m = Regex.Match(blk, @"(\d+)(?:_\d+)?mm", RegexOptions.IgnoreCase);
            if (!m.Success) return 0;
            int value;
            return Int32.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : 0;
        }

                internal static string FindGroundAmmoGroup(string cannon, string bulletName)
        {
            if (String.IsNullOrWhiteSpace(cannon) || String.IsNullOrWhiteSpace(bulletName)) return "";
            BlockSpan bullet = BlkTools.Blocks(cannon, "bullet").FirstOrDefault(x =>
                String.Equals(BlkTools.Field(x.Text, "bulletName", "t"), bulletName, StringComparison.OrdinalIgnoreCase));
            if (bullet == null) return "";
            // Walk outward from the projectile block and return the nearest named
            // container that is an actual ammunition group (e.g. 120mm_xxx).
            // A cannon file container (xxx_user_cannon{...}) is the weapon
            // definition, not an ammo group: falling back to the bullet name is
            // correct for those, otherwise the game cannot resolve the slot.
            int depth = 0;
            for (int p = bullet.Start - 1; p >= 0; p--)
            {
                char c = cannon[p];
                if (c == '}') { depth++; continue; }
                if (c != '{') continue;
                if (depth > 0) { depth--; continue; }
                int nameStart = p;
                while (nameStart > 0 && cannon[nameStart - 1] != '\n' && cannon[nameStart - 1] != '\r') nameStart--;
                Match nameMatch = Regex.Match(cannon.Substring(nameStart, p - nameStart), @"^\s*""?([A-Za-z0-9_.@:$-]+)""?\s*$");
                string name = nameMatch.Success ? nameMatch.Groups[1].Value : "";
                if (String.IsNullOrEmpty(name)) return "";
                if (name.Equals("bullet", StringComparison.OrdinalIgnoreCase)) continue;
                if (name.IndexOf("_user_", StringComparison.OrdinalIgnoreCase) >= 0) return "";
                // A belt group (multiple projectiles inside one container) cannot be
                // referenced by name in the mission ammo slots - the game requires a
                // single projectile definition. Plain groups (one projectile) keep
                // the group name (Ask3lad-style) which the game accepts.
                int closeBrace = p; int braceDepth = 0;
                for (int q = p; q < cannon.Length; q++)
                {
                    if (cannon[q] == '{') braceDepth++;
                    else if (cannon[q] == '}') { braceDepth--; if (braceDepth == 0) { closeBrace = q; break; } }
                }
                string containerText = cannon.Substring(nameStart, closeBrace - nameStart + 1);
                if (BlkTools.Blocks(containerText, "bullet").Count > 1) return bulletName;
                return name;
            }
            return "";
        }

        // Mission ammo slots accept a plain group name (single-shell container,
        // Ask3lad-style) but require a projectile definition for belt groups. This
        // resolves either a projectile name or a named container to the id that the
        // mission BLK should write into bullets0-3.
        internal static string ResolveAmmoSlotId(string cannon, string bulletOrGroupName)
        {
            if (String.IsNullOrWhiteSpace(cannon) || String.IsNullOrWhiteSpace(bulletOrGroupName)) return bulletOrGroupName ?? "";
            BlockSpan bullet = BlkTools.Blocks(cannon, "bullet").FirstOrDefault(x =>
                String.Equals(BlkTools.Field(x.Text, "bulletName", "t"), bulletOrGroupName, StringComparison.OrdinalIgnoreCase));
            if (bullet != null) return FindGroundAmmoGroup(cannon, bulletOrGroupName);
            BlockSpan group = BlkTools.RootBlocks(cannon).FirstOrDefault(x =>
                !String.Equals(BlkTools.BlockName(x), "bullet", StringComparison.OrdinalIgnoreCase) &&
                String.Equals(BlkTools.BlockName(x), bulletOrGroupName, StringComparison.OrdinalIgnoreCase));
            if (group == null) return bulletOrGroupName;
            List<BlockSpan> bullets = BlkTools.Blocks(group.Text, "bullet");
            if (bullets.Count > 1)
            {
                string firstProjectile = BlkTools.Field(bullets[0].Text, "bulletName", "t");
                return String.IsNullOrWhiteSpace(firstProjectile) ? bulletOrGroupName : firstProjectile;
            }
            return bulletOrGroupName;
        }

        private static string ReplaceBlockHeaderWithOverride(BlockSpan block)
        {
            string name = BlkTools.BlockName(block);
            if (String.IsNullOrWhiteSpace(name)) return block == null ? "" : block.Text;
            return Regex.Replace(block.Text, @"^\s*""?" + Regex.Escape(name) + @"""?\s*\{", "\"@override:" + name + "\" {", RegexOptions.IgnoreCase);
        }

        internal static string AppendGroundModuleEffectOverrides(StringBuilder proxy, string nativeUnit, AircraftSettings settings)
        {
            if (proxy == null || String.IsNullOrWhiteSpace(nativeUnit) || settings == null) return null;
            BlockSpan modifications = BlkTools.FirstBlock(nativeUnit, "modifications", 0);
            if (modifications == null) return null;
            HashSet<string> enabled = new HashSet<string>(settings.EnabledModifications, StringComparer.OrdinalIgnoreCase);
            string commonWeapons = null;
            foreach (BlockSpan module in BlkTools.DirectChildBlocks(modifications.Text))
            {
                string moduleName = BlkTools.BlockName(module);
                if (!settings.UseAllModifications && !enabled.Contains(moduleName)) continue;
                BlockSpan effects = BlkTools.DirectChildBlocks(module.Text)
                    .FirstOrDefault(x => String.Equals(BlkTools.BlockName(x), "effects", StringComparison.OrdinalIgnoreCase));
                if (effects == null) continue;

                List<BlockSpan> effectBlocks = BlkTools.DirectChildBlocks(effects.Text);
                foreach (BlockSpan effectBlock in effectBlocks)
                {
                    string effectName = BlkTools.BlockName(effectBlock);
                    if (String.Equals(effectName, "commonWeapons", StringComparison.OrdinalIgnoreCase))
                        commonWeapons = effectBlock.Text;
                    else
                        proxy.AppendLine(ReplaceBlockHeaderWithOverride(effectBlock));
                }

                // Fields directly inside effects (rangefinderMounted, isLaser, etc.)
                // are root-unit fields. Remove child blocks and append only those
                // scalar/vector fields after the native include. Bare scalar lines are
                // ignored by the user-vehicle loader, so quote them as @override fields
                // ("@override:name":type = value) - the same syntax AppendScaledGroundOverride
                // uses for speed/mass scaling, which the game does apply.
                string scalars = effects.Text;
                foreach (BlockSpan child in effectBlocks.OrderByDescending(x => x.Start))
                    scalars = scalars.Remove(child.Start, child.End - child.Start + 1);
                int open = scalars.IndexOf('{');
                int close = scalars.LastIndexOf('}');
                if (open >= 0 && close > open)
                {
                    foreach (string line in scalars.Substring(open + 1, close - open - 1).Replace("\r", "").Split('\n'))
                    {
                        if (String.IsNullOrWhiteSpace(line)) continue;
                        string overrideLine = ConvertScalarToOverride(line.Trim());
                        if (!String.IsNullOrWhiteSpace(overrideLine)) proxy.AppendLine(overrideLine);
                    }
                }
            }
            return commonWeapons;
        }

        private static string ConvertScalarToOverride(string line)
        {
            // "field:type = value" -> "\"@override:field\":type = value"
            Match match = Regex.Match(line, @"^([A-Za-z_][A-Za-z0-9_]*)(?::([A-Za-z0-9]+))?\s*=\s*(.+)$", RegexOptions.IgnoreCase);
            if (!match.Success) return null;
            string field = match.Groups[1].Value;
            string type = match.Groups[2].Success ? match.Groups[2].Value : "r";
            return "\"@override:" + field + "\":" + type + " = " + match.Groups[3].Value.Trim();
        }

        private GeneratedAircraft BuildCustomGroundVehicle(string root, Aircraft target, string token)
        {
            if (target == null) throw new ArgumentNullException("target");
            AircraftSettings settings = GetAircraftSettings(target);
            string classId = GroundProxyClassId;
            string cleanTargetId = target.Id.Trim().Replace('\\', '/').Trim('/');
            string nativeUnit = File.ReadAllText(ExtractGameBlk(root, "gamedata/units/tankmodels/" + cleanTargetId + ".blk"), Encoding.UTF8);
 // Utility/research modifications (manual extinguisher, tool kit, artillery support,
 // medical kit...) are identified by the engine from their block name and are NOT
 // applied by applyAllMods inside missions. Explicitly collect them so the mission
 // unit receives them when "all modifications" is enabled.
 if (settings.UseAllModifications)
 {
 // Collect EVERY module (not just empty blocks) so the mission unit's
 // modification:t list includes sensor/fire-control modules such as
 // laser_rangefinder_lws. Task-mission units do not reliably apply
 // effects through applyAllMods alone, and the explicit per-module
 // list is the mechanism the game honours for user vehicles.
 BlockSpan utlMods = BlkTools.FirstBlock(nativeUnit, "modifications", 0);
 if (utlMods != null)
 {
 foreach (BlockSpan utlModule in BlkTools.DirectChildBlocks(utlMods.Text))
 {
 string utlModName = BlkTools.BlockName(utlModule);
 if (String.IsNullOrWhiteSpace(utlModName)) continue;
 if (!settings.EnabledModifications.Contains(utlModName))
 settings.EnabledModifications.Add(utlModName);
 }
 }
 }
            // Only build a custom gun controller when something actually needs it:
            // cross-vehicle cannon injection or ballistics tuning. Otherwise keep the
            // proxy's native gun BLK so the game resolves the ammo slots against the
            // vehicle's real cannons (putting foreign rounds like ATGMs into a custom
            // BLK makes the main gun fire them uncontrollably).
            bool customCannonNeeded = !String.IsNullOrWhiteSpace(settings.InjectedCannonBlk) || settings.OverrideGroundBallistics;
            bool hasEditableCannon = !String.IsNullOrWhiteSpace(target.MainWeaponBlk);
            string nativeCannonPath = hasEditableCannon ? target.MainWeaponBlk.Replace('\\', '/').TrimStart('/') : "";
            // Cross-vehicle cannon injection (Ask3lad-style): swap the entire gun
            // controller for the donor vehicle's cannon, then apply the selected
            // ammunition and tuning on top of it.
            string effectiveCannonPath = nativeCannonPath;
            if (hasEditableCannon && !String.IsNullOrWhiteSpace(settings.InjectedCannonBlk))
                effectiveCannonPath = settings.InjectedCannonBlk.Replace('\\', '/').TrimStart('/');
                        // Module modifications (e.g. BMP-1P Konkurs) replace the whole weapon
            // controller through a commonWeapons effect. Detect that up front so the
            // proxy keeps those converted weapons even without injection/ballistics.
            bool moduleShipsWeapons = false;
            BlockSpan moduleBlocks = BlkTools.FirstBlock(nativeUnit, "modifications", 0);
            if (moduleBlocks != null && (settings.UseAllModifications || settings.EnabledModifications.Count > 0))
            {
                HashSet<string> enabledMods = new HashSet<string>(settings.EnabledModifications, StringComparer.OrdinalIgnoreCase);
                foreach (BlockSpan module in BlkTools.DirectChildBlocks(moduleBlocks.Text))
                {
                    string moduleName = BlkTools.BlockName(module);
                    if (!settings.UseAllModifications && !enabledMods.Contains(moduleName)) continue;
                    BlockSpan moduleEffects = BlkTools.DirectChildBlocks(module.Text)
                        .FirstOrDefault(x => String.Equals(BlkTools.BlockName(x), "effects", StringComparison.OrdinalIgnoreCase));
                    if (moduleEffects != null && BlkTools.DirectChildBlocks(moduleEffects.Text)
                        .Any(x => String.Equals(BlkTools.BlockName(x), "commonWeapons", StringComparison.OrdinalIgnoreCase)))
                    { moduleShipsWeapons = true; break; }
                }
            }
string cannon = ((customCannonNeeded || moduleShipsWeapons) && hasEditableCannon) ? File.ReadAllText(ExtractGameBlk(root, effectiveCannonPath), Encoding.UTF8) : null;
            // Inject-shell: instead of swapping the whole cannon, mount the chosen weapon's
            // bullets INTO the vehicle's native launcher (same launch mechanism, new round).
            // Mandatory for AI site missiles (S-75 V-759 into the Osa 209mm rail) whose own
            // launcher files carry no compatible player fire-control.
            if (customCannonNeeded && settings.InjectNativeLauncher && !String.IsNullOrWhiteSpace(cannon))
                cannon = InjectShellCannon(nativeUnit, cannon, root);
            List<GroundAmmoLoadout> missionAmmo = ResolveGroundMissionAmmo(target, settings, effectiveCannonPath);
            if (settings.UnlimitedAmmo)
                foreach (GroundAmmoLoadout unlimited in missionAmmo) unlimited.Count = 9999;
            // Note: without any configured ammo the mission keeps the empty ammo
            // block (bullets0-3 = "" + count 9999), which makes the game use the
            // vehicle preset's native default ammunition - confirmed behaviour on
            // the userVehicles proxy class (full native rack, same as entering the
            // mission without touching ammo). A fallback to the first projectile
            // would silently replace the native default and was therefore removed.
            // An injected cannon brings its own ammunition. When the fused UI
            // mounted rounds for the injected gun (their SourceBlk matches the
            // injected cannon path) those loadouts become the actual mission slots.
            if (!String.IsNullOrWhiteSpace(settings.InjectedCannonBlk))
            {
                List<GroundAmmoLoadout> injectedConfigured = settings.GroundAmmoLoadouts
                    .Where(x => x != null && x.Slot >= 0 && x.Slot < 4 && !String.IsNullOrWhiteSpace(x.BulletName) &&
                        !String.IsNullOrWhiteSpace(x.SourceBlk) &&
                        NormalizeGameResourcePath(x.SourceBlk).Equals(NormalizeGameResourcePath(effectiveCannonPath), StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.Slot).Select(x => x.Copy()).ToList();
                if (injectedConfigured.Count > 0)
                {
                    missionAmmo = injectedConfigured;
                    if (settings.UnlimitedAmmo)
                        foreach (GroundAmmoLoadout unlimited in missionAmmo) unlimited.Count = 9999;
                    foreach (GroundAmmoLoadout loadout in missionAmmo)
                    {
                        if (!String.IsNullOrEmpty(cannon))
                            loadout.AmmoGroup = ResolveAmmoSlotId(cannon, loadout.BulletName);
                    }
                }
            }
            // Pantsir-style missile racks: the vehicle's missile rails are shared between
            // all SAM/ATGM weapons. Native weapon BLKs expose the rail count (bullets:i =
            // 12 on Pantsir-SM-SV); launcher/container BLKs expose the rounds per rail
            // (TKB-1055 container = 4, launchers = -1/1 -> 1). Total occupied rails must
            // stay <= the largest native rail count or the game crashes while loading the
            // mission ammo slots (Ask3lad hits the same crash).
            int maxRacks = 0;
            Dictionary<string, int> perRack = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> nameRack = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (BlockSpan missileWeapon in BlkTools.Blocks(nativeUnit, "Weapon"))
            {
                string missileBlk = BlkTools.Field(missileWeapon.Text, "blk", "t");
                if (String.IsNullOrWhiteSpace(missileBlk)) continue;
                if (missileBlk.IndexOf("launcher", StringComparison.OrdinalIgnoreCase) < 0 &&
                    missileBlk.IndexOf("container", StringComparison.OrdinalIgnoreCase) < 0) continue;
                Match rackMatch = Regex.Match(missileWeapon.Text, @"(?m)^\s*bullets:i\s*=\s*(\d+)\s*$");
                if (rackMatch.Success)
                {
                    int racks = Int32.Parse(rackMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    if (racks > maxRacks) maxRacks = racks;
                }
                string key = NormalizeGameResourcePath(missileBlk);
                if (!perRack.ContainsKey(key))
                {
                    int roundsPerRack = 1;
                    try
                    {
                        string launcherText = File.ReadAllText(ExtractGameBlk(root, missileBlk.Replace('\\', '/').TrimStart('/')), Encoding.UTF8);
                        Match roundsMatch = Regex.Match(launcherText, @"(?m)^\s*bullets:i\s*=\s*(\d+)\s*$");
                        if (roundsMatch.Success)
                        {
                            int v = Int32.Parse(roundsMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                            if (v > 1) roundsPerRack = v;
                        }
                    }
                    catch { }
                    perRack[key] = roundsPerRack;
                    // Rounds fired from this container (Pantsir TKB-1055 = 4 per rail) are
                    // defined inside the launcher BLK the container references, so they share
                    // the launcher's SourceBlk in the catalog. Match them by name fragment
                    // (BulletName contains the container name core).
                    if (missileBlk.IndexOf("container", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        string leaf = missileBlk;
                        int slash = leaf.LastIndexOf('/');
                        if (slash >= 0) leaf = leaf.Substring(slash + 1);
                        leaf = leaf.Replace(".blk", String.Empty).Replace("_container", String.Empty);
                        if (!String.IsNullOrWhiteSpace(leaf) && leaf.Length > 4 && !nameRack.ContainsKey(leaf))
                            nameRack[leaf] = roundsPerRack;
                    }
                }
            }
            if (maxRacks > 0)
            {
                List<GroundAmmoLoadout> missiles = missionAmmo
                    .Where(x => IsMissileLoadout(x, groundAmmo))
                    .OrderByDescending(x => x.Slot).ToList();
                int totalRacks = 0;
                Dictionary<string, int> rackCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (GroundAmmoLoadout m in missiles)
                {
                    int rr = RackRoundsFor(m, perRack, nameRack, rackCache);
                    totalRacks += (int)Math.Ceiling(Math.Max(1, m.Count) / (double)Math.Max(1, rr));
                }
                foreach (GroundAmmoLoadout m in missiles) // trim from the last slot first
                {
                    if (totalRacks <= maxRacks) break;
                    int rr = RackRoundsFor(m, perRack, nameRack, rackCache);
                    int racks = (int)Math.Ceiling(Math.Max(1, m.Count) / (double)Math.Max(1, rr));
                    int allowed = Math.Max(0, racks - (totalRacks - maxRacks));
                    int newCount = allowed * rr;
                    if (newCount < m.Count)
                    {
                        totalRacks -= racks - allowed;
                        m.Count = Math.Max(1, newCount);
                    }
                }
            }
            // Mission ammo slots reference the vehicle's weapon-preset names, not raw
            // bullet names - Ask3lad writes 170mm_57e6_aam and the game accepts it,
            // writing the launcher bulletName (170mm_zur_95ya6) crashes the mission.
            // Pantsir-SM-SV shares one launcher BLK between the 57E6 and 57E6M
            // presets, so map each missile to its preset by name similarity
            // (170mm_tkb_1055_aam -> tkb_1055_aam, 170mm_57e6m -> 57e6m_aam)
            // and fall back to the first unused preset for the same launcher
            // (170mm_zur_95ya6 -> 57e6_aam). Every preset is used at most once.
            Dictionary<string, List<string>> presetsByBlk = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (BlockSpan pylon in BlkTools.Blocks(nativeUnit, "WeaponPilons"))
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
                            string weaponKey = NormalizeGameResourcePath(weaponBlk);
                            if (!presetsByBlk.ContainsKey(weaponKey)) presetsByBlk[weaponKey] = new List<string>();
                            if (!presetsByBlk[weaponKey].Contains(presetName, StringComparer.OrdinalIgnoreCase))
                                presetsByBlk[weaponKey].Add(presetName);
                        }
                    }
                }
            }
            HashSet<string> usedPresets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = missionAmmo.Count - 1; i >= 0; i--)
            {
                GroundAmmoLoadout missileLoadout = missionAmmo[i];
                if (!IsMissileLoadout(missileLoadout, groundAmmo) || String.IsNullOrWhiteSpace(missileLoadout.SourceBlk)) continue;
                List<string> candidates;
                if (!presetsByBlk.TryGetValue(NormalizeGameResourcePath(missileLoadout.SourceBlk), out candidates) || candidates.Count == 0)
                {
                    missionAmmo.RemoveAt(i);
                    continue;
                }
                string best = null;
                foreach (string candidate in candidates.OrderByDescending(x => PresetCore(x).Length))
                {
                    if (usedPresets.Contains(candidate)) continue;
                    string core = PresetCore(candidate);
                    if (!String.IsNullOrWhiteSpace(core) && missileLoadout.BulletName != null
                        && missileLoadout.BulletName.IndexOf(core, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        best = candidate;
                        break;
                    }
                }
                if (best == null) best = candidates.FirstOrDefault(x => !usedPresets.Contains(x));
                if (best == null)
                {
                    missionAmmo.RemoveAt(i);
                    continue;
                }
                missileLoadout.BulletName = best;
                missileLoadout.AmmoGroup = String.Empty;
                usedPresets.Add(best);
            }
            Dictionary<string, string> ammunitionSources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (GroundAmmoLoadout loadout in missionAmmo)
            {
                if (String.IsNullOrWhiteSpace(loadout.SourceBlk) || loadout.SourceBlk.StartsWith("stock:", StringComparison.OrdinalIgnoreCase)) continue;
                string sourcePath = loadout.SourceBlk.Replace('\\', '/').TrimStart('/');
                string source;
                if (!ammunitionSources.TryGetValue(sourcePath, out source))
                {
                    source = File.ReadAllText(ExtractGameBlk(root, sourcePath), Encoding.UTF8);
                    ammunitionSources[sourcePath] = source;
                }
                loadout.AmmoGroup = ResolveAmmoSlotId(source, loadout.BulletName);
            }
            List<GroundAmmoLoadout> selectedAmmo = settings.GroundAmmoLoadouts
                .Where(x => x != null && !String.IsNullOrWhiteSpace(x.BulletName) && !String.IsNullOrWhiteSpace(x.SourceBlk) && !IsMissileLoadout(x, groundAmmo)).GroupBy(x => x.BulletName, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToList();
            if (!customCannonNeeded) selectedAmmo = new List<GroundAmmoLoadout>();
            bool useCustomCannon = hasEditableCannon && (customCannonNeeded || moduleShipsWeapons);

            foreach (GroundAmmoLoadout loadout in selectedAmmo)
            {
                if (!hasEditableCannon) throw new InvalidOperationException("This vehicle can be driven and tuned, but its primary weapon is not exposed as an editable cannon in the current game catalog.");
                // A belt-group entry (BulletName = the named container, not a
                // projectile) keeps the whole group intact; there is no single
                // projectile to retune, so skip it instead of failing the build.
                if (cannon != null && BlkTools.RootBlocks(cannon).Any(x =>
                    !String.Equals(BlkTools.BlockName(x), "bullet", StringComparison.OrdinalIgnoreCase) &&
                    String.Equals(BlkTools.BlockName(x), loadout.BulletName, StringComparison.OrdinalIgnoreCase)))
                    continue;
                string source = File.ReadAllText(ExtractGameBlk(root, loadout.SourceBlk.Replace('\\', '/').TrimStart('/')), Encoding.UTF8);
                BlockSpan sourceBullet = BlkTools.Blocks(source, "bullet").FirstOrDefault(x => String.Equals(BlkTools.Field(x.Text, "bulletName", "t"), loadout.BulletName, StringComparison.OrdinalIgnoreCase));
                if (sourceBullet == null) continue; // belt-type modification modules (30mm_2a38_HE) are not launcher projectiles - keep them out of cannon retune
                string replacement = CustomizeGroundBullet(sourceBullet.Text, settings);
                BlockSpan nativeBullet = BlkTools.Blocks(cannon, "bullet").FirstOrDefault(x => String.Equals(BlkTools.Field(x.Text, "bulletName", "t"), loadout.BulletName, StringComparison.OrdinalIgnoreCase));
                if (nativeBullet != null) cannon = BlkTools.ReplaceSpan(cannon, nativeBullet, replacement);
                else
                {
                    BlockSpan sourceContainer = BlkTools.RootBlocks(source).FirstOrDefault(x => sourceBullet.Start >= x.Start && sourceBullet.End <= x.End);
                    string groupName = BlkTools.BlockName(sourceContainer);
                    if (sourceContainer != null && !String.Equals(groupName, "bullet", StringComparison.OrdinalIgnoreCase))
                    {
                        int relativeStart = sourceBullet.Start - sourceContainer.Start;
                        string replacementGroup = sourceContainer.Text.Substring(0, relativeStart) + replacement + sourceContainer.Text.Substring(relativeStart + sourceBullet.Text.Length);
                        BlockSpan nativeGroup = BlkTools.RootBlocks(cannon).FirstOrDefault(x => String.Equals(BlkTools.BlockName(x), groupName, StringComparison.OrdinalIgnoreCase));
                        cannon = nativeGroup == null
                            ? cannon.TrimEnd() + Environment.NewLine + Environment.NewLine + replacementGroup + Environment.NewLine
                            : BlkTools.ReplaceSpan(cannon, nativeGroup, replacementGroup);
                    }
                    else cannon = cannon.TrimEnd() + Environment.NewLine + Environment.NewLine + replacement + Environment.NewLine;
                }
            }

            if (useCustomCannon && settings.ReloadSeconds > 0)
                cannon = SetOrInsertNumber(cannon, "shotFreq", 1.0 / settings.ReloadSeconds);

            // Preserve the engine's native playable tank registration. Ask3lad's known
            // working generator and the CDK both use an include proxy rather than a
            // decompiled full copy of the vehicle.
            StringBuilder proxy = new StringBuilder();
            proxy.AppendLine("include \"#/develop/gameBase/gameData/units/tankModels/" + cleanTargetId + ".blk\"");
            string moduleCommonWeapons = AppendGroundModuleEffectOverrides(proxy, nativeUnit, settings);
            // Task-mission units do not apply modification "disableModEffects" the way the garage does,
            // so add-on armour (e.g. T-80BVM extra ERA) would appear even on a stock vehicle.
            // Apply the disabled effects (hidden nodes) for every modification that is NOT enabled.
            string stockPreset = null;
            if (!settings.UseAllModifications)
            {
                List<string> disabledNodes = new List<string>();
                BlockSpan modsBlock = BlkTools.FirstBlock(nativeUnit, "modifications", 0);
                if (modsBlock != null)
                {
                    HashSet<string> enabledMods = new HashSet<string>(settings.EnabledModifications, StringComparer.OrdinalIgnoreCase);
                    foreach (BlockSpan module in BlkTools.DirectChildBlocks(modsBlock.Text))
                    {
                        string moduleName = BlkTools.BlockName(module);
                        if (enabledMods.Contains(moduleName)) continue;
                        BlockSpan disable = BlkTools.DirectChildBlocks(module.Text).FirstOrDefault(x => String.Equals(BlkTools.BlockName(x), "disableModEffects", StringComparison.OrdinalIgnoreCase));
                        if (disable == null) continue;
                        foreach (BlockSpan hide in BlkTools.DirectChildBlocks(disable.Text).Where(x => String.Equals(BlkTools.BlockName(x), "hideNodes", StringComparison.OrdinalIgnoreCase)))
                        {
                            foreach (string line in hide.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                string trimmed = line.Trim();
                                if (!trimmed.StartsWith("node:t", StringComparison.OrdinalIgnoreCase)) continue;
                                int firstQuote = trimmed.IndexOf('"');
                                if (firstQuote < 0) continue;
                                int endQuote = trimmed.IndexOf('"', firstQuote + 1);
                                if (endQuote <= firstQuote) continue;
                                string value = trimmed.Substring(firstQuote + 1, endQuote - firstQuote - 1);
                                if (!disabledNodes.Contains(value)) disabledNodes.Add(value);
                            }
                        }
                    }
                    if (disabledNodes.Count > 0)
                    {
                        // hideNodes belongs in a weapon preset (loaded when the mission unit
                        // references it), not at the proxy root - the game ignores a bare
                        // proxy-level hideNodes for user vehicles.
                        string stockPresetName = "utl_stock_" + target.Id.ToLowerInvariant();
                        string presetDir = Path.Combine(root, @"content\pkg_local\gameData\units\tankModels\weaponPresets");
                        Directory.CreateDirectory(presetDir);
                        StringBuilder preset = new StringBuilder("hideNodes {");
                        foreach (string node in disabledNodes) preset.Append("\n    node:t = \"" + node + "\"");
                        preset.Append("\n}");
                        WriteBytes(Path.Combine(presetDir, stockPresetName + ".blk"), new UTF8Encoding(false).GetBytes(preset.ToString()));
                        proxy.AppendLine("\"@override:weapon_presets\" { preset { name:t = \"" + stockPresetName + "\" blk:t = \"gameData/units/tankModels/weaponPresets/" + stockPresetName + ".blk\" } }");
                        stockPreset = stockPresetName;
                    }
                }
            }
            AppendScaledGroundOverride(proxy, nativeUnit, "maxFwdSpeed", settings.ForwardSpeedMultiplier);
            AppendScaledGroundOverride(proxy, nativeUnit, "maxRevSpeed", settings.ReverseSpeedMultiplier);
            AppendScaledGroundOverride(proxy, nativeUnit, "mass", settings.VehicleMassMultiplier);
            // Nested @override inside VehiclePhys (engine / Mass) is not reliably
            // applied by the game for user vehicles, so rewrite the whole block
            // (@delete + redefine), the same mechanism proven by commonWeapons.
            bool physChanged = false;
            if (Math.Abs(settings.EnginePowerMultiplier - 1.0) >= 0.000001 || Math.Abs(settings.VehicleMassMultiplier - 1.0) >= 0.000001)
            {
                BlockSpan vehiclePhys = BlkTools.FirstBlock(nativeUnit, "VehiclePhys", 0);
                if (vehiclePhys != null)
                {
                    string phys = vehiclePhys.Text;
                    if (Math.Abs(settings.EnginePowerMultiplier - 1.0) >= 0.000001)
                    {
                        Match horsepower = Regex.Match(phys, @"(?m)^(\s*)horsePowers:r\s*=\s*(-?[0-9]+(?:\.[0-9]+)?)\s*$", RegexOptions.IgnoreCase);
                        if (horsepower.Success)
                        {
                            double value = ParseNumber(horsepower.Groups[2].Value) * settings.EnginePowerMultiplier;
                            phys = Regex.Replace(phys, @"(?m)^(\s*)horsePowers:r\s*=\s*-?[0-9]+(?:\.[0-9]+)?\s*$", delegate(Match match) { return match.Groups[1].Value + "horsePowers:r = " + value.ToString("0.######", CultureInfo.InvariantCulture); }, RegexOptions.IgnoreCase);
                            physChanged = true;
                        }
                    }
                    if (Math.Abs(settings.VehicleMassMultiplier - 1.0) >= 0.000001)
                    {
                        foreach (string physField in new[] { "Empty", "TakeOff" })
                        {
                            Regex fieldRegex = new Regex(@"(?m)^(\s*)" + physField + @":r\s*=\s*(-?[0-9]+(?:\.[0-9]+)?)\s*$", RegexOptions.IgnoreCase);
                            Match fieldMatch = fieldRegex.Match(phys);
                            if (fieldMatch.Success)
                            {
                                double value = ParseNumber(fieldMatch.Groups[2].Value) * settings.VehicleMassMultiplier;
                                phys = fieldRegex.Replace(phys, delegate(Match match) { return match.Groups[1].Value + physField + ":r = " + value.ToString("0.######", CultureInfo.InvariantCulture); }, 1);
                                physChanged = true;
                            }
                        }
                    }
                    if (physChanged)
                    {
                        proxy.AppendLine("\"@delete:VehiclePhys\"{}");
                        proxy.AppendLine(phys.TrimEnd());
                    }
                }
            }
            string generatedSightFolder;
            string sightVehicleId = Path.GetFileNameWithoutExtension(GroundProxyVehicleFileName);
            string sightName = UserSightStore.InstallForGeneratedVehicle(settings.UserSightPath, sightVehicleId, out generatedSightFolder);
            if (!String.IsNullOrWhiteSpace(sightName))
            {
                string cleanSight = sightName.Replace("\"", "");
                if (Regex.IsMatch(nativeUnit, @"(?m)^\s*crosshairPreset:t\s*=")) proxy.AppendLine("\"@override:crosshairPreset\":t = \"" + cleanSight + "\"");
                else proxy.AppendLine("crosshairPreset:t = \"" + cleanSight + "\"");
            }

            if (useCustomCannon)
            {
                BlockSpan commonWeapons = !String.IsNullOrWhiteSpace(moduleCommonWeapons)
                    ? BlkTools.FirstBlock(moduleCommonWeapons, "commonWeapons", 0)
                    : BlkTools.Blocks(nativeUnit, "commonWeapons").FirstOrDefault();
                if (commonWeapons == null) throw new InvalidOperationException("Native common weapon controller was not found in the ground vehicle.");
                string commonOverride = commonWeapons.Text;
                // Pick the real gun to swap: skip dummy weapons (launcher/SAM vehicles
                // carry a dummy:b=true gunner0 mount that only aims the camera). Prefer
                // a non-dummy gunner0 (normal tank gun), else the first non-dummy
                // Weapon (missile launcher like Buk/Osa/Tor is gunner1).
                List<BlockSpan> weapons = BlkTools.Blocks(commonOverride, "Weapon").ToList();
                BlockSpan mainWeapon = weapons.FirstOrDefault(x => String.Equals(BlkTools.Field(x.Text, "trigger", "t"), "gunner0", StringComparison.OrdinalIgnoreCase) && !IsDummyWeapon(x))
                    ?? weapons.FirstOrDefault(x => !IsDummyWeapon(x));
                if (mainWeapon == null) throw new InvalidOperationException("Primary gun mount was not found in the ground vehicle.");
                string weaponBlock = mainWeapon.Text;
                if (customCannonNeeded)
                    weaponBlock = BlkTools.ReplaceStringField(weaponBlock, "blk", "gameData/Weapons/groundModels_weapons/utl_ground/utl_ground_cannon.blk");
                // The native gun's ammo rack capacity (bullets:i, e.g. 42) is what the
                // game actually uses for the carried ammunition; the mission-level
                // bulletsCount0=9999 is ignored for this field, leaving injected guns
                // empty after the native rack runs dry. Push the injected weapons to
                // effectively unlimited ammunition instead.
                if (!String.IsNullOrWhiteSpace(settings.InjectedCannonBlk))
                {
                    // SetOrInsertNumber only rewrites :r float fields, so a bullets:i
                    // integer would get an ignored bullets:r twin instead. Patch the
                    // integer directly so the gun really carries unlimited ammunition.
                    Regex bulletsRegex = new Regex(@"(?m)^(\s*)bullets:i\s*=\s*-?[0-9]+\s*$", RegexOptions.IgnoreCase);
                    if (bulletsRegex.IsMatch(weaponBlock))
                        weaponBlock = bulletsRegex.Replace(weaponBlock, delegate(Match match) { return match.Groups[1].Value + "bullets:i =9999"; });
                    else
                        weaponBlock = weaponBlock.TrimEnd() + "\n bullets:i =9999\n";
                }
                if (settings.ReloadSeconds > 0) weaponBlock = SetOrInsertNumber(weaponBlock, "shotFreq", 1.0 / settings.ReloadSeconds);
                else weaponBlock = Regex.Replace(weaponBlock, @"(?m)^\s*shotFreq:r\s*=\s*[0-9.]+\s*$", "", RegexOptions.IgnoreCase);
                if (settings.OverrideGroundBallistics) weaponBlock = ReplaceFirstScaledNumber(weaponBlock, "recoilOffset", settings.RecoilMultiplier);
                commonOverride = BlkTools.ReplaceSpan(commonOverride, mainWeapon, weaponBlock);
                // The include proxy inherits the vehicle's native commonWeapons.
                // "@override:commonWeapons" merges with the inherited block instead
                // of replacing it, which leaves both the native gunner0 mount and the
                // proxy gunner0 mount active and makes the tank fire two shells per
                // shot. Delete the inherited block first, then define the customized
                // one (same pattern as War Thunder's own custom_tu_95m mod).
                proxy.AppendLine("\"@delete:commonWeapons\"{}");
                commonOverride = Regex.Replace(commonOverride, @"^\s*commonWeapons\s*\{", "commonWeapons {", RegexOptions.IgnoreCase);
                
                proxy.AppendLine(commonOverride);
            }

                // Radar swap: rebuild the sensors block (@delete + re-define like commonWeapons)
                // installing the requested search/track radars and optionally dropping the AI pair.
                if (settings.RadarSearchBlk != null || settings.RadarTrackBlk != null || settings.RadarStripAiSensors)
                    ApplyRadarSwapToProxy(proxy, nativeUnit, settings, root);
            string unit = proxy.ToString();

            string unitOut = Path.Combine(root, @"content\pkg_local\gameData\units\tankModels\userVehicles", GroundProxyVehicleFileName);
            string cannonOut = Path.Combine(root, @"content\pkg_local\gameData\Weapons\groundModels_weapons\utl_ground", "utl_ground_cannon.blk");
            // Round selection: a belt group name keeps the whole group (the game
            // auto-loads its belt), a projectile name keeps only that projectile.
            if (useCustomCannon && !String.IsNullOrWhiteSpace(settings.InjectedCannonRound))
            {
                BlockSpan group = BlkTools.RootBlocks(cannon).FirstOrDefault(x => !String.Equals(BlkTools.BlockName(x), "bullet", StringComparison.OrdinalIgnoreCase) && String.Equals(BlkTools.BlockName(x), settings.InjectedCannonRound, StringComparison.OrdinalIgnoreCase));
                if (group != null) cannon = group.Text;
                else
                {
                    BlockSpan round = BlkTools.Blocks(cannon, "bullet").FirstOrDefault(x => String.Equals(BlkTools.Field(x.Text, "bulletName", "t"), settings.InjectedCannonRound, StringComparison.OrdinalIgnoreCase));
                    if (round != null) cannon = round.Text;
                }
            }

            // SARH -> fake-ARH conversion (EXPERIMENTAL switch): patch the injected
            // cannon text in place so the game loads an already-converted missile.
            if (useCustomCannon && settings.FakeArhConversion && !String.IsNullOrWhiteSpace(cannon))
                cannon = ApplyFakeArhPatch(cannon, 2.0);

            // Rounds-per-reload override: rewrite the injected cannon's bulletsCartridge
            // so the launcher carries e.g. 6 S-300 missiles on the Osa rack instead of the
            // native S-300 4-canister figure. 0 keeps the source value untouched.
            if (useCustomCannon && settings.InjectedCannonRounds > 0 && !String.IsNullOrWhiteSpace(cannon))
                cannon = Regex.Replace(cannon, @"(?m)^\s*bulletsCartridge\s*:\s*i\s*=\s*\d+\s*$", "bulletsCartridge:i = " + settings.InjectedCannonRounds.ToString(CultureInfo.InvariantCulture));

            // Publish dependencies first. The game must never observe a playable unit
            // whose gun BLK is still absent or was deleted with the previous token.
            if (useCustomCannon) WriteBytes(cannonOut, new UTF8Encoding(false).GetBytes(cannon));
            WriteBytes(unitOut, new UTF8Encoding(false).GetBytes(unit));
            GeneratedAircraft generated = new GeneratedAircraft { ClassId = classId, PresetId = !String.IsNullOrWhiteSpace(stockPreset) ? stockPreset : target.DefaultPreset, ModelId = BlkTools.Field(nativeUnit, "model", "t") ?? target.Id, FlightModelPath = unitOut, PresetPath = useCustomCannon ? cannonOut : unitOut, SpawnSpeedKmh = 0, IsGround = true, UserSightFolder = generatedSightFolder };
            foreach (GroundAmmoLoadout loadout in missionAmmo) generated.GroundAmmoLoadouts.Add(loadout.Copy());
            if (useCustomCannon) generated.AuxiliaryPaths.Add(cannonOut);
            return generated;
        }

        // Rebuilds the vehicle's sensors block in the include proxy: installs the
        // requested player search/track radars and optionally strips the AI-only
        // *_ai sensor pair (AI secondary sight). Used by the radar-swap lab.
    }

}
