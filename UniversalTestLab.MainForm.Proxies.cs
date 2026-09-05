// UniversalTestLab.MainForm.Proxies.cs
// Ground proxy patches, radar swaps, countermeasures, injection helpers (segment 4/5).
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
        private static void ApplyRadarSwapToProxy(StringBuilder proxy, string nativeUnit, AircraftSettings settings, string root)
        {
            int open = nativeUnit.IndexOf("sensors {", StringComparison.OrdinalIgnoreCase);
            if (open < 0) return;
            int close = BlkTools.MatchingBrace(nativeUnit, open);
            if (close <= open || close >= nativeUnit.Length) return;
            string sensorsText = nativeUnit.Substring(open, close - open + 1);
            List<BlockSpan> sensors = BlkTools.Blocks(sensorsText, "sensor");
            if (sensors.Count == 0) return;

            StringBuilder rebuilt = new StringBuilder("sensors {");
            bool searchInstalled = false;
            bool trackInstalled = false;
            foreach (BlockSpan sensor in sensors)
            {
                string text = sensor.Text;
                string blk = BlkTools.Field(text, "blk", "t");
                bool aiOnly = blk != null && blk.IndexOf("_ai.", StringComparison.OrdinalIgnoreCase) >= 0;
                if (aiOnly)
                {
                    if (settings.RadarStripAiSensors) continue; // drop the AI pair
                }
                else
                {
                    // Only swap sensors mounted on a real antenna (dmPart antenna_*). Buk-style
                    // launchers carry a second copy of the track radar on the optic mount
                    // (optic_gun_dm) - swapping that one too creates a ghost duplicate that
                    // reads like a phased-array / wide-sector emitter.
                    bool onAntenna = text.IndexOf("antenna_", StringComparison.OrdinalIgnoreCase) >= 0;
                    // Role from file name first; many radars don't carry search/track in the
                    // name (su_viking, su_9s35...) so fall back to the sensor blk's fsm modes.
                    bool isSearch = false;
                    bool isTrack = false;
                    if (onAntenna && !String.IsNullOrWhiteSpace(blk))
                    {
                        string lower = blk.ToLowerInvariant();
                        if (lower.IndexOf("search", StringComparison.OrdinalIgnoreCase) >= 0 && lower.IndexOf("track", StringComparison.OrdinalIgnoreCase) < 0) isSearch = true;
                        else if (lower.IndexOf("track", StringComparison.OrdinalIgnoreCase) >= 0) isTrack = true;
                        else
                        {
                            string sensorText = null;
                            try { sensorText = File.ReadAllText(ExtractGameBlk(root, blk.Trim().Replace('\\', '/').TrimStart('/')), Encoding.UTF8); }
                            catch { }
                            if (sensorText != null)
                            {
                                string fsm = sensorText.ToLowerInvariant();
                                if (Regex.IsMatch(fsm, "(?m)^\\s*fsm\\s*:\\s*t\\s*=\\s*\"(search|tws)\"") || Regex.IsMatch(fsm, "(?m)^\\s*fsm\\s*:\\s*t\\s*=\\s*\"[a-z_]*scan[a-z_]*\""))
                                    isSearch = true;
                                else if (Regex.IsMatch(fsm, "(?m)^\\s*fsm\\s*:\\s*t\\s*=\\s*\"(lock|track|acquisition|illumination|designate)\"") || Regex.IsMatch(fsm, "(?m)^\\s*fsm\\s*:\\s*t\\s*=\\s*\"[a-z_]*track[a-z_]*\""))
                                    isTrack = true;
                            }
                        }
                    }
                    if (isSearch)
                    {
                        searchInstalled = true;
                        if (!String.IsNullOrWhiteSpace(settings.RadarSearchBlk))
                            text = BlkTools.ReplaceStringField(text, "blk", "gameData/sensors/" + settings.RadarSearchBlk.Trim() + ".blk");
                    }
                    else if (isTrack)
                    {
                        trackInstalled = true;
                        if (!String.IsNullOrWhiteSpace(settings.RadarTrackBlk))
                            text = BlkTools.ReplaceStringField(text, "blk", "gameData/sensors/" + settings.RadarTrackBlk.Trim() + ".blk");
                    }
                }
                rebuilt.Append(text);
            }
            // A vehicle with no native search (or track) slot gets one appended using
            // the first sensor as a template (same turret mount / DM parts) - otherwise
            // the requested radar could never be installed (e.g. Buk 9A310 launcher,
            // which carries only its 9S35 track radar; fitting the 9S18 site search
            // radar previously did nothing).
            BlockSpan template = sensors.FirstOrDefault(x => !String.IsNullOrWhiteSpace(BlkTools.Field(x.Text, "blk", "t")));
            if (!searchInstalled && !String.IsNullOrWhiteSpace(settings.RadarSearchBlk) && template != null)
            {
                string add = BlkTools.ReplaceStringField(template.Text, "blk", "gameData/sensors/" + settings.RadarSearchBlk.Trim() + ".blk");
                rebuilt.Append(add);
            }
            if (!trackInstalled && !String.IsNullOrWhiteSpace(settings.RadarTrackBlk) && template != null)
            {
                string add = BlkTools.ReplaceStringField(template.Text, "blk", "gameData/sensors/" + settings.RadarTrackBlk.Trim() + ".blk");
                rebuilt.Append(add);
            }
            rebuilt.Append("}");

            proxy.AppendLine("\"@delete:sensors\"{}");
            proxy.AppendLine(rebuilt.ToString());

            // A vehicle whose native SAM menu is disabled (available:b=false - e.g.
            // IR-only launchers like the Strela-10) has no target list to drive the
            // installed radar. Installing radars via the swap lab implies the player
            // needs the SAM interface, so re-enable it with a full target list.
            int menuOpen = nativeUnit.IndexOf("antiAirComplexMenu {", StringComparison.OrdinalIgnoreCase);
            bool menuDisabled = false;
            if (menuOpen >= 0)
            {
                int menuClose = BlkTools.MatchingBrace(nativeUnit, menuOpen);
                if (menuClose > menuOpen && menuClose < nativeUnit.Length)
                    menuDisabled = Regex.IsMatch(nativeUnit.Substring(menuOpen, menuClose - menuOpen + 1), @"available\s*:\s*b\s*=\s*false", RegexOptions.IgnoreCase);
            }
            if (menuDisabled)
            {
                proxy.AppendLine("\"@delete:antiAirComplexMenu\"{}");
                proxy.AppendLine("antiAirComplexMenu {");
                proxy.AppendLine("\tavailable:b = true");
                proxy.AppendLine("\tisVerticalViewAvailable:b = true");
                proxy.AppendLine("\thasTargetList:b = true");
                proxy.AppendLine("\thasTurretView:b = true");
                proxy.AppendLine("\thasVerticalView:b = true");
                proxy.AppendLine("\tverticalViewMaxAltitude:r = 15");
                proxy.AppendLine("}");
            }
        }

        // True when a commonWeapons Weapon is a camera-aiming dummy (dummy:b = true).        // These vehicles (launcher/SAM trucks, e.g. Buk/Osa/Tor TELs) mount the real
        // weapon on a separate gunner1 Weapon - swapping the dummy would hang the
        // injected gun on the observation sight instead of the launcher.
        // Inject-shell assembly (S-75 V-759 into the Osa 209mm rail): keep the vehicle's
        // native launcher file (fire-control, aim, rails) and swap in every bullet block
        // of the chosen site missile. Whole-cannon swaps of AI rocket_launcher files fail
        // silently (no player fire-control in the source launcher) - verified V-759.
        private static string InjectShellCannon(string nativeUnit, string sourceCannon, string root)
        {
            // Native launcher = first non-dummy commonWeapons Weapon that references a blk.
            string launcherPath = null;
            foreach (BlockSpan weapon in BlkTools.Blocks(nativeUnit, "Weapon"))
            {
                if (IsDummyWeapon(weapon)) continue;
                string blk = BlkTools.Field(weapon.Text, "blk", "t");
                if (!String.IsNullOrWhiteSpace(blk) && blk.IndexOf("dummy", StringComparison.OrdinalIgnoreCase) < 0 && blk.IndexOf("utl_ground", StringComparison.OrdinalIgnoreCase) < 0)
                { launcherPath = blk.Trim().Replace('\\', '/').TrimStart('/'); break; }
            }
            if (String.IsNullOrWhiteSpace(launcherPath)) return sourceCannon;
            string launcherText;
            try { launcherText = File.ReadAllText(ExtractGameBlk(root, launcherPath), Encoding.UTF8); }
            catch { return sourceCannon; }
            List<BlockSpan> nativeBullets = BlkTools.Blocks(launcherText, "bullet");
            if (nativeBullets.Count == 0) return sourceCannon;
            List<BlockSpan> sourceBullets = BlkTools.Blocks(sourceCannon, "bullet");
            if (sourceBullets.Count == 0) return sourceCannon;
            StringBuilder srcAll = new StringBuilder();
            foreach (BlockSpan sb in sourceBullets) srcAll.Append(sb.Text);
            string result = launcherText;
            for (int i = nativeBullets.Count - 1; i >= 1; i--) result = BlkTools.ReplaceSpan(result, nativeBullets[i], "");
            result = BlkTools.ReplaceSpan(result, nativeBullets[0], srcAll.ToString());
            return result;
        }

        internal static bool IsDummyWeapon(BlockSpan weapon)
        {
            // dummy:b is an unquoted bool (dummy:b = true) - BlkTools.Field only
            // matches quoted :t values, so scan the raw text directly.
            return Regex.IsMatch(weapon.Text, @"(?m)^\s*dummy\s*:\s*b\s*=\s*(true|yes)\s*$", RegexOptions.IgnoreCase);
        }

        // SARH -> fake-ARH conversion, verified 2026-09-02 on AIM-7E-2 (sparrow v9):
        // 1) radarSeeker active:b = true            -> missile self-illuminates (no radar illumination required)
        // 2) guidance permanentlyActivated:b = true -> TWS launch, no pre-launch lock needed
        // 3) lockDistance / inertialNavigation + datalink / useTargetVel
        // 4) breakLockMaxTime -> 160 (re-acquire window)
        // 5) wider seeker cone (lockAngleMax/angleMax/rateMax), angleGateRate, distGate
        // 6) shotFreq capped (rocketGun native 1000.25 -> sane rate; ground SAMs already sane)
        internal static string ApplyFakeArhPatch(string cannon, double shotsPerSecond)
        {
            if (String.IsNullOrWhiteSpace(cannon)) return cannon;
            string lower = cannon.ToLowerInvariant();
            if (!lower.Contains("guidancetype:t = \"radar\"")) return cannon;
            if (lower.Contains("active:b = true") && lower.Contains("permanentlyactivated:b = true")) return cannon;

            List<string> lines = new List<string>(cannon.Split('\n'));
            for (int i = 0; i < lines.Count; i++)
            {
                Match m = Regex.Match(lines[i], @"^\s*shotFreq:r\s*=\s*([0-9.]+)", RegexOptions.IgnoreCase);
                double rate;
                if (m.Success && Double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out rate) && rate > 10.0)
                    lines[i] = Regex.Replace(lines[i], @"shotFreq:r\s*=\s*[0-9.]+", "shotFreq:r = " + shotsPerSecond.ToString("0.##", CultureInfo.InvariantCulture), RegexOptions.IgnoreCase);
            }

            // Patch EVERY guidance / radarSeeker block (multi-missile files like
            // MIM-23 / 5V55 carry two missile groups - patching only the first one
            // left the second group as true SARH when the player switches shells).
            InsertFieldInEveryBlock(lines, "guidance", "permanentlyActivated", "permanentlyActivated:b = true");
            InsertFieldInEveryBlock(lines, "guidance", "lockDistance", "lockDistance:r = 16000");
            InsertFieldInEveryBlock(lines, "guidance", "inertialNavigation", "inertialNavigation:b = true");
            InsertFieldInEveryBlock(lines, "guidance", "useTargetVel", "useTargetVel:b = true");
            ReplaceFieldInEveryBlock(lines, "guidance", "breakLockMaxTime", "breakLockMaxTime:r = 160");
            InsertInertialGuidanceInAllGuidanceBlocks(lines);

            InsertFieldInEveryBlock(lines, "radarSeeker", "active", "active:b = true");
            InsertFieldInEveryBlock(lines, "radarSeeker", "angleGateRate", "angleGateRate:r = 30");
            RaiseFieldInEveryBlock(lines, "radarSeeker", "lockAngleMax", 60.0);
            RaiseFieldInEveryBlock(lines, "radarSeeker", "angleMax", 60.0);
            RaiseFieldInEveryBlock(lines, "radarSeeker", "rateMax", 20.0);
            AddDistGateToEverySeeker(lines);
            return String.Join("\n", lines);
        }

        private static int IndexOfBlock(List<string> lines, string blockName, int from = 0)
        {
            Regex rx = new Regex(@"^\s*" + blockName + @"\s*\{", RegexOptions.IgnoreCase);
            for (int i = from; i < lines.Count; i++) if (rx.IsMatch(lines[i])) return i;
            return -1;
        }

        private static int BlockEnd(List<string> lines, int openIdx)
        {
            int depth = 0;
            for (int i = openIdx; i < lines.Count; i++)
            {
                depth += lines[i].Count(c => c == '{') - lines[i].Count(c => c == '}');
                if (i > openIdx && depth <= 0) return i;
            }
            return -1;
        }

        private static string FieldIndent(List<string> lines, int openIdx, int closeIdx)
        {
            for (int i = openIdx + 1; i < closeIdx; i++)
            {
                Match m = Regex.Match(lines[i], @"^(\s*)\S");
                if (m.Success) return m.Groups[1].Value;
            }
            Match om = Regex.Match(lines[openIdx], @"^(\s*)");
            return om.Success ? om.Groups[1].Value + "\t" : "\t";
        }

        private static bool RangeContains(List<string> lines, int openIdx, int closeIdx, string field)
        {
            Regex rx = new Regex(@"^\s*" + field + @"\s*[:bri]?\s*=", RegexOptions.IgnoreCase);
            for (int i = openIdx + 1; i < closeIdx; i++) if (rx.IsMatch(lines[i])) return true;
            return false;
        }

        private static int IndexOfField(List<string> lines, int openIdx, int closeIdx, string field)
        {
            Regex rx = new Regex(@"^\s*" + field + @"\s*[:bri]?\s*=", RegexOptions.IgnoreCase);
            for (int i = openIdx + 1; i < closeIdx; i++) if (rx.IsMatch(lines[i])) return i;
            return -1;
        }

        private static void InsertLineIfMissing(List<string> lines, int index, string line, string indent)
        {
            string body = line.TrimStart();
            for (int i = 0; i < lines.Count; i++) if (lines[i].Trim().Equals(body, StringComparison.OrdinalIgnoreCase)) return;
            if (index >= 0 && index <= lines.Count) lines.Insert(index, indent + body);
        }

        private static void InsertFieldInEveryBlock(List<string> lines, string block, string field, string line)
        {
            int from = 0;
            while (true)
            {
                int open = IndexOfBlock(lines, block, from);
                if (open < 0) return;
                int close = BlockEnd(lines, open);
                if (!RangeContains(lines, open, close, field))
                {
                    string indent = FieldIndent(lines, open, close);
                    InsertLineIfMissing(lines, open + 1, line, indent);
                }
                from = open + 1;
            }
        }

        private static void ReplaceFieldInEveryBlock(List<string> lines, string block, string field, string line)
        {
            int from = 0;
            while (true)
            {
                int open = IndexOfBlock(lines, block, from);
                if (open < 0) return;
                int close = BlockEnd(lines, open);
                int idx = IndexOfField(lines, open, close, field);
                if (idx >= 0)
                {
                    lines[idx] = Regex.Replace(lines[idx], @"\b" + Regex.Escape(field) + @"\s*:[a-z]\s*=\s*[^/\r\n]*", line, RegexOptions.IgnoreCase);
                }
                else
                {
                    string indent = FieldIndent(lines, open, close);
                    InsertLineIfMissing(lines, open + 1, line, indent);
                }
                from = open + 1;
            }
        }

        private static void RaiseFieldInEveryBlock(List<string> lines, string block, string field, double target)
        {
            int from = 0;
            while (true)
            {
                int open = IndexOfBlock(lines, block, from);
                if (open < 0) return;
                int close = BlockEnd(lines, open);
                int idx = IndexOfField(lines, open, close, field);
                if (idx < 0)
                {
                    string indent = FieldIndent(lines, open, close);
                    InsertLineIfMissing(lines, open + 1, field + ":r = " + target.ToString("0.##", CultureInfo.InvariantCulture), indent);
                }
                else
                {
                    Match m = Regex.Match(lines[idx], @"\b" + Regex.Escape(field) + @"\s*:r\s*=\s*([0-9.]+)", RegexOptions.IgnoreCase);
                    double v;
                    if (m.Success && Double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && v < target)
                        lines[idx] = Regex.Replace(lines[idx], @"\b" + Regex.Escape(field) + @"\s*:r\s*=\s*[0-9.]+", field + ":r = " + target.ToString("0.##", CultureInfo.InvariantCulture), RegexOptions.IgnoreCase);
                }
                from = open + 1;
            }
        }

        private static void InsertInertialGuidanceInAllGuidanceBlocks(List<string> lines)
        {
            int from = 0;
            while (true)
            {
                int open = IndexOfBlock(lines, "guidance", from);
                if (open < 0) return;
                int close = BlockEnd(lines, open);
                if (!RangeContains(lines, open, close, "inertialGuidance"))
                {
                    string indent = FieldIndent(lines, open, close);
                    string guidanceBlock = indent + "inertialGuidance {\n" + indent + "\tinertialNavigationDriftSpeed:r = 2\n" + indent + "\tdatalink:b = true\n" + indent + "}";
                    InsertLineIfMissing(lines, close, guidanceBlock, "");
                }
                from = open + 1;
            }
        }

        private static void AddDistGateToEverySeeker(List<string> lines)
        {
            int from = 0;
            while (true)
            {
                int open = IndexOfBlock(lines, "radarSeeker", from);
                if (open < 0) return;
                int close = BlockEnd(lines, open);
                if (!RangeContains(lines, open, close, "distGate"))
                {
                    string indent = FieldIndent(lines, open, close);
                    string dist = indent + "distGate {\n" + indent + "\tfilterAlpha:r = 0.8\n" + indent + "\tfilterBetta:r = 0.05\n" + indent + "\tdistGateSearchRange:r = 5000\n" + indent + "}";
                    int tx = IndexOfField(lines, open, close, "transmitter");
                    int dopp = IndexOfField(lines, open, close, "dopplerSpeedGate");
                    int at = tx >= 0 ? tx : (dopp >= 0 ? BlockEnd(lines, dopp) : close);
                    if (at >= 0 && at <= lines.Count) lines.Insert(at, dist);
                }
                from = open + 1;
            }
        }

        internal static bool HasExplicitFlightModel(string unitBlk)
        {
            return Regex.IsMatch(unitBlk ?? "", @"(?m)^\s*fmFile:t\s*=");
        }

        internal static void EnsureExplicitFlightModel(ref string unitBlk, string originalAircraftId)
        {
            if (HasExplicitFlightModel(unitBlk)) return;
            if (String.IsNullOrWhiteSpace(originalAircraftId)) throw new ArgumentException("Original aircraft ID is required.", "originalAircraftId");
            string cleanId = originalAircraftId.Trim().Replace('\\', '/').Trim('/');
            unitBlk = "fmFile:t = \"fm/" + cleanId + ".blk\"" + Environment.NewLine + (unitBlk ?? "");
        }

        internal static void RemoveFuelTankPresets(ref string fm)
        {
            List<BlockSpan> remove = new List<BlockSpan>();
            foreach (BlockSpan preset in BlkTools.Blocks(fm, "WeaponPreset"))
            {
                bool isFuelTank = BlkTools.Blocks(preset.Text, "Weapon").Any(weapon =>
                    String.Equals(BlkTools.Field(weapon.Text, "trigger", "t"), "fuel tanks", StringComparison.OrdinalIgnoreCase));
                if (isFuelTank) remove.Add(preset);
            }
            foreach (BlockSpan preset in remove.OrderByDescending(x => x.Start))
                fm = fm.Remove(preset.Start, preset.End - preset.Start + 1);
        }

        private static int GreatestCommonDivisor(int a, int b)
        {
            a = Math.Abs(a);
            b = Math.Abs(b);
            while (b != 0) { int next = a % b; a = b; b = next; }
            return Math.Max(1, a);
        }

        internal static string BuildCountermeasureBelt(string source, int flares, int chaff)
        {
            if (String.IsNullOrEmpty(source)) throw new ArgumentException("Countermeasure gun source is required.", "source");
            if (flares <= 0 || chaff <= 0) throw new ArgumentException("A mixed belt requires both flares and chaff.");
            List<BlockSpan> bullets = BlkTools.Blocks(source, "bullet");
            BlockSpan flare = bullets.FirstOrDefault(x => String.Equals(BlkTools.Field(x.Text, "bulletType", "t"), "flr", StringComparison.OrdinalIgnoreCase));
            BlockSpan chaffBlock = bullets.FirstOrDefault(x => String.Equals(BlkTools.Field(x.Text, "bulletType", "t"), "chff", StringComparison.OrdinalIgnoreCase));
            if (flare == null || chaffBlock == null) throw new InvalidOperationException("The game countermeasure gun does not contain both flare and chaff definitions.");
            int first = Math.Min(flare.Start, chaffBlock.Start);
            int last = Math.Max(flare.End, chaffBlock.End);
            int divisor = GreatestCommonDivisor(flares, chaff);
            int flareUnits = flares / divisor;
            int chaffUnits = chaff / divisor;
            int units = flareUnits + chaffUnits;
            int emittedChaff = 0;
            StringBuilder belt = new StringBuilder(source.Substring(0, first));
            for (int i = 0; i < units; i++)
            {
                int desiredChaff = (int)Math.Floor(((i + 1) * chaffUnits) / (double)units);
                bool useChaff = desiredChaff > emittedChaff;
                if (useChaff) emittedChaff++;
                belt.AppendLine((useChaff ? chaffBlock.Text : flare.Text).TrimEnd());
            }
            belt.Append(source.Substring(last + 1));
            string result = belt.ToString();
            result = Regex.Replace(result, @"(?m)^(\s*)bullets:i\s*=\s*\d+\s*$", "$1bullets:i = " + (flares + chaff).ToString(CultureInfo.InvariantCulture), RegexOptions.None);
            result = Regex.Replace(result, @"(?m)^(\s*)isBulletBelt:b\s*=\s*(?:yes|no|true|false)\s*$", "$1isBulletBelt:b = true", RegexOptions.IgnoreCase);
            return result;
        }

        private static void PrepareCountermeasureBelts(string root, string token, AircraftSettings settings, List<string> outputPaths, out string smallPath, out string largePath)
        {
            smallPath = null;
            largePath = null;
            if (settings == null || !settings.OverrideCountermeasures || settings.FlareRounds <= 0 || settings.ChaffRounds <= 0) return;
            string[] sizes = { "small", "large" };
            foreach (string size in sizes)
            {
                bool large = size == "large";
                string sourceRelative = large
                    ? "gamedata/weapons/rocketguns/countermeasure_large_split_launcher_jet_with_chaff.blk"
                    : "gamedata/weapons/rocketguns/countermeasure_split_launcher_jet_with_chaff.blk";
                string source = File.ReadAllText(ExtractGameBlk(root, sourceRelative), Encoding.UTF8);
                string custom = BuildCountermeasureBelt(source, settings.FlareRounds, settings.ChaffRounds);
                string fileName = "utl_cm_" + token + "_" + size + ".blk";
                string output = Path.Combine(root, @"content\pkg_user\gameData\Weapons\rocketGuns\utl_cm", fileName);
                WriteBytes(output, new UTF8Encoding(false).GetBytes(custom));
                outputPaths.Add(output);
                string gamePath = "gameData/Weapons/rocketGuns/utl_cm/" + fileName;
                if (large) largePath = gamePath; else smallPath = gamePath;
            }
        }

        private static string CountermeasureBeltKey(int flares, int chaff, bool large)
        {
            return flares.ToString(CultureInfo.InvariantCulture) + ":" + chaff.ToString(CultureInfo.InvariantCulture) + ":" + (large ? "L" : "S");
        }

        private static Dictionary<string, string> PrepareCountermeasureBeltsByLoadout(string root, string token, AircraftSettings settings, List<string> outputPaths)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (settings == null || !settings.OverrideCountermeasures) return result;
            List<CountermeasureLoadout> requested = settings.CountermeasureLoadouts.Count > 0
                ? settings.CountermeasureLoadouts.ToList()
                : new List<CountermeasureLoadout> { new CountermeasureLoadout { Key = "default", Flares = settings.FlareRounds, Chaff = settings.ChaffRounds } };
            List<CountermeasureLoadout> mixed = requested.Where(x => x.Flares > 0 && x.Chaff > 0)
                .GroupBy(x => x.Flares.ToString(CultureInfo.InvariantCulture) + ":" + x.Chaff.ToString(CultureInfo.InvariantCulture))
                .Select(x => x.First()).ToList();
            if (mixed.Count == 0) return result;
            foreach (bool large in new[] { false, true })
            {
                string sourceRelative = large
                    ? "gamedata/weapons/rocketguns/countermeasure_large_split_launcher_jet_with_chaff.blk"
                    : "gamedata/weapons/rocketguns/countermeasure_split_launcher_jet_with_chaff.blk";
                string source = File.ReadAllText(ExtractGameBlk(root, sourceRelative), Encoding.UTF8);
                foreach (CountermeasureLoadout loadout in mixed)
                {
                    string custom = BuildCountermeasureBelt(source, loadout.Flares, loadout.Chaff);
                    string fileName = "utl_cm_" + token + "_" + (large ? "large_" : "small_") + loadout.Flares.ToString(CultureInfo.InvariantCulture) + "_" + loadout.Chaff.ToString(CultureInfo.InvariantCulture) + ".blk";
                    string output = Path.Combine(root, @"content\pkg_user\gameData\Weapons\rocketGuns\utl_cm", fileName);
                    WriteBytes(output, new UTF8Encoding(false).GetBytes(custom));
                    outputPaths.Add(output);
                    result[CountermeasureBeltKey(loadout.Flares, loadout.Chaff, large)] = "gameData/Weapons/rocketGuns/utl_cm/" + fileName;
                }
            }
            return result;
        }

        internal static string CountermeasureWeaponPath(string original, int flares, int chaff, string customSmall, string customLarge)
        {
            bool large = (original ?? "").IndexOf("large", StringComparison.OrdinalIgnoreCase) >= 0;
            string file;
            if (flares <= 0 && chaff > 0)
                file = large ? "countermeasure_chaff_only_large.blk" : "countermeasure_chaff_only.blk";
            else if (chaff <= 0 && flares > 0)
                file = large ? "countermeasure_split_launcher_jet_only_flare_large.blk" : "countermeasure_split_launcher_jet.blk";
            else
            {
                string custom = large ? customLarge : customSmall;
                if (!String.IsNullOrEmpty(custom)) return custom;
                file = large ? "countermeasure_large_split_launcher_jet_with_chaff.blk" : "countermeasure_split_launcher_jet_with_chaff.blk";
            }
            return "gameData/Weapons/rocketGuns/" + file;
        }

        internal static void ApplyCountermeasureSettings(ref string fm, AircraftSettings settings, string customSmall, string customLarge)
        {
            if (settings == null || !settings.OverrideCountermeasures) return;
            int flares = settings.FlareRounds;
            int chaff = settings.ChaffRounds;
            int rounds = Math.Max(1, flares + chaff);
            foreach (BlockSpan weapon in BlkTools.Blocks(fm, "Weapon").OrderByDescending(x => x.Start))
            {
                if (!String.Equals(BlkTools.Field(weapon.Text, "trigger", "t"), "countermeasures", StringComparison.OrdinalIgnoreCase)) continue;
                string block = weapon.Text;
                string original = BlkTools.Field(block, "blk", "t");
                if (!String.IsNullOrEmpty(original))
                    block = BlkTools.ReplaceStringField(block, "blk", CountermeasureWeaponPath(original, flares, chaff, customSmall, customLarge));
                Regex bullets = new Regex(@"(?m)^(\s*)bullets:i\s*=\s*\d+\s*$");
                if (bullets.IsMatch(block))
                    block = bullets.Replace(block, delegate(Match m) { return m.Groups[1].Value + "bullets:i = " + rounds.ToString(CultureInfo.InvariantCulture); }, 1);
                else
                {
                    int close = block.LastIndexOf('}');
                    block = block.Insert(close, "\tbullets:i = " + rounds.ToString(CultureInfo.InvariantCulture) + Environment.NewLine);
                }
                fm = fm.Substring(0, weapon.Start) + block + fm.Substring(weapon.End + 1);
            }
        }

        internal static void ApplyCountermeasureSettings(ref string fm, AircraftSettings settings, IDictionary<string, string> customBelts)
        {
            if (settings == null || !settings.OverrideCountermeasures) return;
            List<BlockSpan> launchers = BlkTools.Blocks(fm, "Weapon")
                .Where(x => String.Equals(BlkTools.Field(x.Text, "trigger", "t"), "countermeasures", StringComparison.OrdinalIgnoreCase)).ToList();
            List<string> launcherKeys = new List<string>();
            int anonymous = 0;
            foreach (BlockSpan launcher in launchers)
            {
                string launcherEmitter = BlkTools.Field(launcher.Text, "emitter", "t");
                launcherKeys.Add(String.IsNullOrWhiteSpace(launcherEmitter)
                    ? "launcher-" + (++anonymous).ToString(CultureInfo.InvariantCulture)
                    : launcherEmitter);
            }
            for (int index = launchers.Count - 1; index >= 0; index--)
            {
                BlockSpan weapon = launchers[index];
                string key = launcherKeys[index];
                CountermeasureLoadout selected = settings.CountermeasureLoadouts.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) ??
                    settings.CountermeasureLoadouts.FirstOrDefault(x => x.Key.Equals("default", StringComparison.OrdinalIgnoreCase));
                int flares = selected == null ? settings.FlareRounds : selected.Flares;
                int chaff = selected == null ? settings.ChaffRounds : selected.Chaff;
                int rounds = Math.Max(1, flares + chaff);
                string block = weapon.Text;
                string original = BlkTools.Field(block, "blk", "t");
                bool large = (original ?? "").IndexOf("large", StringComparison.OrdinalIgnoreCase) >= 0;
                string custom = null;
                if (customBelts != null) customBelts.TryGetValue(CountermeasureBeltKey(flares, chaff, large), out custom);
                if (!String.IsNullOrEmpty(original))
                    block = BlkTools.ReplaceStringField(block, "blk", CountermeasureWeaponPath(original, flares, chaff, large ? null : custom, large ? custom : null));
                Regex bullets = new Regex(@"(?m)^(\s*)bullets:i\s*=\s*\d+\s*$");
                if (bullets.IsMatch(block))
                    block = bullets.Replace(block, delegate(Match m) { return m.Groups[1].Value + "bullets:i = " + rounds.ToString(CultureInfo.InvariantCulture); }, 1);
                else
                {
                    int close = block.LastIndexOf('}');
                    block = block.Insert(close, "\tbullets:i = " + rounds.ToString(CultureInfo.InvariantCulture) + Environment.NewLine);
                }
                fm = fm.Substring(0, weapon.Start) + block + fm.Substring(weapon.End + 1);
            }
        }

        internal static void MaterializeHelicopterThermalSight(ref string fm, AircraftSettings settings)
        {
            if (String.IsNullOrWhiteSpace(fm) || settings == null) return;
            BlockSpan modifications = BlkTools.FirstBlock(fm, "modifications", 0);
            if (modifications == null) return;
            BlockSpan activeNightVision = BlkTools.Blocks(fm, "nightVision").FirstOrDefault(x => x.Start < modifications.Start);
            if (activeNightVision == null || BlkTools.FirstBlock(activeNightVision.Text, "sightThermal", 0) != null) return;

            BlockSpan thermal = null;
            if (settings.UseAllModifications)
                thermal = BlkTools.FirstBlock(modifications.Text, "sightThermal", 0);
            else
            {
                foreach (string id in settings.EnabledModifications)
                {
                    BlockSpan enabled = BlkTools.FirstBlock(modifications.Text, id, 0);
                    if (enabled == null) continue;
                    thermal = BlkTools.FirstBlock(enabled.Text, "sightThermal", 0);
                    if (thermal != null) break;
                }
            }
            if (thermal == null) return;
            int close = activeNightVision.Text.LastIndexOf('}');
            if (close < 0) return;
            string thermalText = Regex.Replace(thermal.Text.Trim(), @"(?m)^", "\t");
            string replacement = activeNightVision.Text.Insert(close, "\t" + thermalText + Environment.NewLine);
            fm = BlkTools.ReplaceSpan(fm, activeNightVision, replacement);
        }

        private static string PrepareInjectedWeapon(string root, DonorWeapon donor)
        {
            const string prefix = "utl-sam:";
            if (donor == null || String.IsNullOrEmpty(donor.Blk) || !donor.Blk.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return donor == null ? "" : donor.Blk;
            string descriptor = donor.Blk.Substring(prefix.Length);
            int separator = descriptor.LastIndexOf('#');
            if (separator <= 0 || separator >= descriptor.Length - 1) throw new InvalidOperationException("Ground SAM descriptor is invalid: " + donor.Blk);
            string sourceRelative = descriptor.Substring(0, separator);
            string bulletName = descriptor.Substring(separator + 1);
            string safeName = Regex.Replace(bulletName.ToLowerInvariant(), @"[^a-z0-9_]+", "_").Trim('_');
            // The in-game selector localizes a rocket gun by its file basename. Preserve the
            // real missile ID and isolate the adapter in a subfolder to avoid overriding a
            // native aircraft rocket gun with the same name.
            string fileName = safeName + ".blk";
            string output = Path.Combine(root, @"content\pkg_user\gameData\Weapons\rocketGuns\utl_sam", fileName);
            string source = File.ReadAllText(ExtractGameBlk(root, sourceRelative), Encoding.UTF8);
            string adapter = BuildGroundSamAdapter(source, bulletName);
            WriteBytes(output, new UTF8Encoding(false).GetBytes(adapter));
            return "gameData/Weapons/rocketGuns/utl_sam/" + fileName;
        }

        internal static string BuildGroundSamAdapter(string source, string bulletName)
        {
            foreach (BlockSpan bullet in BlkTools.Blocks(source, "bullet"))
            {
                if (!String.Equals(BlkTools.Field(bullet.Text, "bulletName", "t"), bulletName, StringComparison.OrdinalIgnoreCase)) continue;
                BlockSpan rocket = BlkTools.FirstBlock(bullet.Text, "rocket", 0);
                if (rocket == null) continue;
                string rocketText = rocket.Text;
                int open = rocketText.IndexOf('{');
                string additions = Environment.NewLine + "\tbulletName:t = \"" + bulletName.Replace("\"", "") + "\"" + Environment.NewLine + "\ticonType:t = \"missile_type_b_air_to_air\"";
                rocketText = rocketText.Insert(open + 1, additions);
                // The ground IRIS-T SLM uses a deployed launcher animation. On an aircraft
                // that animation renders the whole launch container as the flying projectile.
                // Its static rocket mesh is valid, so use that without the launcher animation.
                if (bulletName.Equals("us_iris_t_sl", StringComparison.OrdinalIgnoreCase))
                {
                    rocketText = Regex.Replace(rocketText, @"(?m)^(\s*)mesh:t\s*=\s*""iris_t_sl_rocket""", "$1mesh:t = \"iris_t_rocket\"");
                    rocketText = Regex.Replace(rocketText, @"(?m)^\s*shellAnimChar:t\s*=\s*""[^""]*""\s*\r?\n", "");
                    int meshLine = rocketText.IndexOf("mesh:t = \"iris_t_rocket\"", StringComparison.Ordinal);
                    if (meshLine >= 0)
                    {
                        int lineEnd = rocketText.IndexOf('\n', meshLine);
                        if (lineEnd < 0) lineEnd = rocketText.Length;
                        rocketText = rocketText.Insert(lineEnd, Environment.NewLine + "\t\tshellAnimChar:t = \"iris_t_rocket_char\"");
                    }
                }
                string mesh = BlkTools.Field(rocketText, "mesh", "t") ?? "";
                StringBuilder adapter = new StringBuilder();
                adapter.AppendLine("rocketGun:b = true");
                adapter.AppendLine("bullets:i = 1");
                adapter.AppendLine("shotFreq:r = 1000.25");
                adapter.AppendLine("sound:t = \"weapon.rocketgun_132\"");
                if (!String.IsNullOrEmpty(mesh)) adapter.AppendLine("mesh:t = \"" + mesh + "\"");
                adapter.AppendLine("tags {");
                adapter.AppendLine("}");
                adapter.AppendLine(rocketText);
                return adapter.ToString();
            }
            throw new InvalidOperationException("Ground SAM missile was not found in its launcher file: " + bulletName);
        }

        internal static string BuildDownloadedFpvVariant(string quadcopter, string originalFpv)
        {
            BlockSpan warhead = BlkTools.FirstBlock(originalFpv, "warhead", 0);
            if (warhead == null) throw new InvalidOperationException("The FPV drone warhead definition is missing from the game resources.");
            int firstLine = quadcopter.IndexOf('\n');
            if (firstLine < 0) throw new InvalidOperationException("The installed UAV flight model is damaged.");
            string fpvProperties = @"
verifyEcsTemplate:b = false
useSimpleDeathConditionsAndEffects:b = false
humanDrone:b = true
drawFovOnTacticalMap:b = true
sceneCollisionTickStep:i = 2
overrideIndicatorIcon:t = ""iconKamikazeDrone""
hasFPVCamera:b = true
disableFPVHud:b = true
fpvCameraOffset:p3 = 0.2, -0.1, 0
";
            string result = quadcopter.Insert(firstLine + 1, fpvProperties);
            result += Environment.NewLine + warhead.Text + Environment.NewLine;
            return result;
        }

        internal static void AddInjectedMount(ref string fm, PylonSlot pylon, DonorWeapon donor, string mountId, string weaponBlk = null)
        {
            BlockSpan slotBlock = null;
            foreach (BlockSpan candidate in BlkTools.Blocks(fm, "WeaponSlot"))
            {
                Match index = Regex.Match(candidate.Text, @"index:i\s*=\s*(\d+)");
                if (index.Success && Int32.Parse(index.Groups[1].Value, CultureInfo.InvariantCulture) == pylon.Slot) { slotBlock = candidate; break; }
            }
            if (slotBlock == null) throw new InvalidOperationException("Aircraft station not found: " + pylon.Slot);
            BlockSpan anchor = null;
            foreach (BlockSpan candidate in BlkTools.Blocks(slotBlock.Text, "WeaponPreset"))
            {
                if (BlkTools.Field(candidate.Text, "name", "t") == pylon.AnchorMount) { anchor = candidate; break; }
            }
            if (anchor == null) throw new InvalidOperationException("Pylon anchor mount not found: " + pylon.AnchorMount);
            string emitter = null;
            foreach (BlockSpan weapon in BlkTools.Blocks(anchor.Text, "Weapon"))
            {
                emitter = BlkTools.Field(weapon.Text, "emitter", "t");
                if (!String.IsNullOrEmpty(emitter)) break;
            }
            if (String.IsNullOrEmpty(emitter)) throw new InvalidOperationException("Pylon emitter is missing for station " + pylon.Slot + ".");
            string replacement = anchor.Text;
            foreach (BlockSpan weapon in BlkTools.Blocks(replacement, "Weapon").OrderByDescending(x => x.Start))
                replacement = replacement.Remove(weapon.Start, weapon.End - weapon.Start + 1);
            replacement = Regex.Replace(replacement, @"(?m)^\s*showInWeaponMenu:b\s*=\s*(?:true|yes)\s*\r?\n", "");
            if (!String.IsNullOrEmpty(donor.Icon))
            {
                if (Regex.IsMatch(replacement, @"iconType:t\s*=")) replacement = BlkTools.ReplaceStringField(replacement, "iconType", donor.Icon);
                else
                {
                    int open = replacement.IndexOf('{');
                    replacement = replacement.Insert(open + 1, Environment.NewLine + "\t\t\ticonType:t = \"" + donor.Icon + "\"");
                }
            }
            if (String.Equals(donor.Trigger, "targetingPod", StringComparison.OrdinalIgnoreCase))
            {
                int open = replacement.IndexOf('{');
                string podProperties = Environment.NewLine + "\t\t\thasTargetingPod:b = true" + Environment.NewLine + "\t\t\tremoveGunnerOpticFps:i = 0";
                replacement = replacement.Insert(open + 1, podProperties);
            }
            StringBuilder weaponBlock = new StringBuilder();
            weaponBlock.AppendLine("\t\t\tWeapon {");
            weaponBlock.AppendLine("\t\t\t\ttrigger:t = \"" + donor.Trigger + "\"");
            weaponBlock.AppendLine("\t\t\t\tblk:t = \"" + (String.IsNullOrEmpty(weaponBlk) ? donor.Blk : weaponBlk) + "\"");
            weaponBlock.AppendLine("\t\t\t\temitter:t = \"" + emitter + "\"");
            weaponBlock.AppendLine("\t\t\t\texternal:b = true");
            weaponBlock.AppendLine("\t\t\t\tseparate:b = true");
            weaponBlock.AppendLine("\t\t\t\tbullets:i = " + Math.Max(1, donor.Bullets).ToString(CultureInfo.InvariantCulture));
            weaponBlock.Append("\t\t\t}");
            replacement = replacement.Insert(replacement.LastIndexOf('}'), Environment.NewLine + weaponBlock.ToString() + Environment.NewLine + "\t\t");
            int absoluteAnchorStart = slotBlock.Start + anchor.Start;
            fm = fm.Substring(0, absoluteAnchorStart) + replacement + fm.Substring(absoluteAnchorStart + anchor.Text.Length);
        }

        private static string PresetCore(string presetName)
        {
            // 170mm_57e6m_aam -> 57e6m ; 170mm_tkb_1055_aam -> tkb_1055
            if (String.IsNullOrWhiteSpace(presetName)) return String.Empty;
            string core = presetName.Replace("_aam", String.Empty).Replace("_sam", String.Empty);
            if (core.StartsWith("170mm_", StringComparison.OrdinalIgnoreCase)) core = core.Substring(6);
            return core;
        }

        private static bool IsMissileKind(string kind)
        {
            return String.Equals(kind, "SAM", StringComparison.OrdinalIgnoreCase) || String.Equals(kind, "ATGM", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMissileLoadout(GroundAmmoLoadout loadout, IList<GroundAmmo> catalog)
        {
            if (loadout == null) return false;
            if (!String.IsNullOrWhiteSpace(loadout.Kind)) return IsMissileKind(loadout.Kind);
            if (catalog != null && !String.IsNullOrWhiteSpace(loadout.SourceBlk) && !String.IsNullOrWhiteSpace(loadout.BulletName))
            {
                GroundAmmo ammo = catalog.FirstOrDefault(x => x.SourceBlk != null && x.BulletName != null &&
                    NormalizeGameResourcePath(x.SourceBlk).Equals(NormalizeGameResourcePath(loadout.SourceBlk), StringComparison.OrdinalIgnoreCase) &&
                    x.BulletName.Equals(loadout.BulletName, StringComparison.OrdinalIgnoreCase));
                if (ammo != null) return IsMissileKind(ammo.Type);
            }
            return false;
        }

        private static int RackRoundsFor(GroundAmmoLoadout loadout, Dictionary<string, int> perRack, Dictionary<string, int> nameRack, Dictionary<string, int> rackCache)
        {
            if (loadout == null || String.IsNullOrWhiteSpace(loadout.SourceBlk)) return 1;
            string key = NormalizeGameResourcePath(loadout.SourceBlk);
            string cacheKey = key + "|" + (loadout.BulletName ?? String.Empty);
            int cached;
            if (rackCache.TryGetValue(cacheKey, out cached)) return cached;
            int rr = 1;
            if (perRack.TryGetValue(key, out rr)) { }
            if (rr <= 1 && nameRack != null && !String.IsNullOrWhiteSpace(loadout.BulletName))
            {
                foreach (KeyValuePair<string, int> pair in nameRack)
                {
                    if (loadout.BulletName.IndexOf(pair.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        rr = pair.Value;
                        break;
                    }
                }
            }
            rackCache[cacheKey] = rr;
            return rr;
        }

        private static string CsvValue(string value)
        {
            return (value ?? "").Replace(";", ",").Replace("\r", " ").Replace("\n", " ");
        }

        private static void WriteMissionLocalization(string root, GeneratedAircraft generated, Aircraft source)
        {
            string display = CsvValue(source == null ? "Custom Aircraft" : source.Display);
            StringBuilder csv = new StringBuilder(Embedded.Text("UTL.usr.csv").TrimEnd());
            csv.AppendLine();
            csv.AppendLine("missions/universal_test_lab/date;Custom test session");
            csv.AppendLine("location/Clean_Testdrive;Clean Test Range");
            foreach (string key in new[] { generated.ClassId, generated.ClassId + "_0", generated.ClassId + "_1", generated.ClassId + "_2" })
                csv.AppendLine(key + ";" + display);
            WriteBytes(Path.Combine(root, MissionFolderRelative, "usr.csv"), new UTF8Encoding(false).GetBytes(csv.ToString()));
        }

        internal static void RegisterPreset(ref string fm, string presetId)
        {
            BlockSpan presets = BlkTools.FirstBlock(fm, "weapon_presets", 0);
            if (presets == null) throw new InvalidOperationException("Aircraft weapon_presets block is missing.");
            string registration = Environment.NewLine + "\tpreset {" + Environment.NewLine + "\t\tname:t = \"" + presetId + "\"" + Environment.NewLine + "\t\tblk:t = \"gameData/FlightModels/weaponPresets/" + presetId + ".blk\"" + Environment.NewLine + "\t}";
            fm = fm.Insert(presets.End, registration);
        }

        internal static string EnsureHelicopterExperienceClass(string fm)
        {
            // A copied helicopter FM is a new usermodel class, so it has no shop entry from
            // which the engine can infer ES_UNIT_TYPE_HELICOPTER. Without expClass the model
            // flies, but War Thunder loads aircraft HUD/keybinds and ignores the helicopter
            // gunner optic / sight-stabilisation controls.
            return SetOrInsertString(fm, "expClass", "exp_helicopter");
        }

        internal static IEnumerable<PylonAssignment> OrderAssignmentsForPreset(IEnumerable<PylonAssignment> source)
        {
            return (source ?? Enumerable.Empty<PylonAssignment>())
                .Where(x => x != null && x.Pylon != null)
                // Preserve the aircraft definition's native station order. The slot
                // numbers on mirrored helicopter pylons are not their serialization
                // order; re-sorting them made the HUD expose only the turret.
                .OrderBy(x => x.Pylon.Order);
        }

        internal static void AppendCommonWeaponsToLoadout(StringBuilder loadout, string fm, ISet<int> explicitlyAssignedSlots, bool commonIsImplicit)
        {
            // commonWeapons contain fixed guns and, on helicopters, the turret and
            // countermeasure launchers. The caller controls placement: copied helicopter
            // usermodels append this block after their external pylons.
            if (loadout == null || String.IsNullOrWhiteSpace(fm) || commonIsImplicit) return;
            int presetsAt = fm.IndexOf("weapon_presets", StringComparison.Ordinal);
            string unitHeader = presetsAt < 0 ? fm : fm.Substring(0, presetsAt);
            BlockSpan common = BlkTools.Blocks(unitHeader, "commonWeapons").LastOrDefault();
            if (common == null) return;
            foreach (BlockSpan weapon in BlkTools.Blocks(common.Text, "Weapon"))
            {
                Match slotMatch = Regex.Match(weapon.Text, @"(?m)^\s*slot:i\s*=\s*(-?\d+)\s*$");
                string preset = BlkTools.Field(weapon.Text, "preset", "t");
                int slot;
                if (!slotMatch.Success || !Int32.TryParse(slotMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out slot) || String.IsNullOrWhiteSpace(preset)) continue;
                if (explicitlyAssignedSlots != null && explicitlyAssignedSlots.Contains(slot)) continue;
                loadout.AppendLine("Weapon {");
                loadout.AppendLine("\tslot:i = " + slot.ToString(CultureInfo.InvariantCulture));
                loadout.AppendLine("\tpreset:t = \"" + preset.Replace("\"", "") + "\"");
                loadout.AppendLine("}");
            }
        }

    }

}
