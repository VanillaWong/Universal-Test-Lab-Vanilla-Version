// UniversalTestLab.BlkTools.cs
// BLK text engine: block parsing, spans and mission/vehicle text transforms.
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
    internal static class BlkTools
    {
        public static string ConfigureRapidFire(string text, bool enabled, double interval, bool fullRestore)
        {
            try
            {
                int start = text.LastIndexOf("\"UTL Rapid Fire\"{", StringComparison.Ordinal);
                if (start < 0) return text;
                int open = text.IndexOf('{', start);
                int depth = 0;
                int end = -1;
                for (int i = open; i < text.Length; i++)
                {
                    if (text[i] == '{') depth++;
                    else if (text[i] == '}')
                    {
                        depth--;
                        if (depth == 0) { end = i; break; }
                    }
                }
                if (end < 0) return text;
                string block = text.Substring(start, end - start + 1);
                block = Regex.Replace(block, @"(?m)^(\s*is_enabled:b\s*=\s*)(?:yes|no|true|false)\s*$", "${1}" + (enabled ? "yes" : "no"), RegexOptions.IgnoreCase);
                block = Regex.Replace(block, @"(?m)^(\s*time:r\s*=\s*)[\d.]+\s*$", "${1}" + Math.Max(0.05, Math.Min(60.0, interval)).ToString("0.###", CultureInfo.InvariantCulture), RegexOptions.IgnoreCase);
                // Restore mode: fullRestore swaps the unitRestore action to the
                // everything-repair variant; partial keeps the critical-parts
                // (barrel/breech/engine/tracks/transmission + crew) + ammo + fuel variant.
                int unitStart = block.IndexOf("unitRestore{", StringComparison.Ordinal);
                if (unitStart >= 0)
                {
                    int unitOpen = block.IndexOf('{', unitStart);
                    int unitDepth = 0;
                    int unitEnd = -1;
                    for (int i = unitOpen; i < block.Length; i++)
                    {
                        if (block[i] == '{') unitDepth++;
                        else if (block[i] == '}')
                        {
                            unitDepth--;
                            if (unitDepth == 0) { unitEnd = i; break; }
                        }
                    }
                    if (unitEnd > unitStart)
                        block = block.Substring(0, unitStart) + (fullRestore ? FullRestoreAction : PartialRestoreAction) + block.Substring(unitEnd + 1);
                }
                return text.Substring(0, start) + block + text.Substring(end + 1);
            }
            catch
            {
                return text;
            }
        }

        private const string FullRestoreAction =
            "unitRestore{\n" +
            "  target_marking:i=0\n" +
            "  ressurectIfDead:b=no\n" +
            "  fullRestore:b=yes\n" +
            "  ammoRestore:b=yes\n" +
            "  target:t=\"You\"\n" +
            "}";

        private const string PartialRestoreAction =
            "unitRestore{\n" +
            "  target_marking:i=0\n" +
            "  ressurectIfDead:b=no\n" +
            "  fullRestore:b=no\n" +
            "  partRestore:b=yes\n" +
            "  tankPart:t=\"gun_barrel_dm\"\n" +
            "  tankPart:t=\"gun_breech_dm\"\n" +
            "  tankPart:t=\"engine_dm\"\n" +
            "  tankPart:t=\"track_l_dm\"\n" +
            "  tankPart:t=\"track_r_dm\"\n" +
            "  tankPart:t=\"transmission_dm\"\n" +
            "  tankPart:t=\"steering_gear_dm\"\n" +
            "  tankPart:t=\"drive_turret_h_dm\"\n" +
            "  tankPart:t=\"drive_turret_v_dm\"\n" +
            "  tankPart:t=\"drive_wheel_dm\"\n" +
            "  tankPart:t=\"idler_dm\"\n" +
            "  tankPart:t=\"autoloader_dm\"\n" +
            "  ammoRestore:b=yes\n" +
            "  fuelRestore:b=yes\n" +
            "  target:t=\"You\"\n" +
            "}";

        public static int MatchingBrace(string text, int open)
        {
            int depth = 0;
            bool quoted = false;
            bool escaped = false;
            for (int i = open; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (escaped) { escaped = false; continue; }
                    if (c == '\\') { escaped = true; continue; }
                    if (c == '"') quoted = false;
                    continue;
                }
                if (c == '"') { quoted = true; continue; }
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }

        public static BlockSpan FirstBlock(string text, string name, int from)
        {
            Match match = Regex.Match(text.Substring(from), @"(?m)^\s*" + Regex.Escape(name) + @"\s*\{");
            if (!match.Success) return null;
            int start = from + match.Index;
            int open = text.IndexOf('{', start);
            int end = MatchingBrace(text, open);
            if (open < 0 || end < 0) return null;
            return new BlockSpan { Start = start, Open = open, End = end, Text = text.Substring(start, end - start + 1) };
        }

        public static List<BlockSpan> Blocks(string text, string name)
        {
            List<BlockSpan> result = new List<BlockSpan>();
            foreach (Match match in Regex.Matches(text, @"(?m)^\s*" + Regex.Escape(name) + @"\s*\{"))
            {
                int open = text.IndexOf('{', match.Index);
                int end = MatchingBrace(text, open);
                if (open >= 0 && end >= 0)
                    result.Add(new BlockSpan { Start = match.Index, Open = open, End = end, Text = text.Substring(match.Index, end - match.Index + 1) });
            }
            return result;
        }

        public static string Field(string text, string field, string type)
        {
            Match match = Regex.Match(text, Regex.Escape(field) + ":" + Regex.Escape(type) + @"\s*=\s*""([^""]*)""");
            return match.Success ? match.Groups[1].Value : null;
        }

        public static string ReplaceStringField(string block, string field, string value)
        {
            Regex regex = new Regex("(" + Regex.Escape(field) + @":t\s*=\s*"")[^""]*("")");
            if (!regex.IsMatch(block)) throw new InvalidOperationException("BLK field not found: " + field);
            return regex.Replace(block, delegate(Match m) { return m.Groups[1].Value + value + m.Groups[2].Value; }, 1);
        }

        public static string ReplaceIntField(string block, string field, int value)
        {
            Regex regex = new Regex("(" + Regex.Escape(field) + @":i\s*=\s*)-?\d+");
            if (!regex.IsMatch(block)) throw new InvalidOperationException("BLK field not found: " + field);
            return regex.Replace(block, delegate(Match m) { return m.Groups[1].Value + value.ToString(CultureInfo.InvariantCulture); }, 1);
        }

        public static string ReplaceSpan(string text, BlockSpan span, string replacement)
        {
            return text.Substring(0, span.Start) + replacement + text.Substring(span.End + 1);
        }

        public static BlockSpan UnitBlockByName(string text, string unitName)
        {
            string needle = "name:t=\"" + unitName + "\"";
            int nameAt = text.IndexOf(needle, StringComparison.Ordinal);
            if (nameAt < 0) throw new InvalidOperationException("Mission unit not found: " + unitName);
            string[] kinds = { "armada", "tankModels", "ships", "wheeled_vehicles", "structures" };
            int best = -1;
            foreach (string kind in kinds)
            {
                int at = text.LastIndexOf("  " + kind + "{", nameAt, StringComparison.Ordinal);
                if (at > best) best = at;
            }
            if (best < 0) throw new InvalidOperationException("Mission unit block not found: " + unitName);
            int open = text.IndexOf('{', best);
            int end = MatchingBrace(text, open);
            if (end < nameAt) throw new InvalidOperationException("Mission unit block is damaged: " + unitName);
            return new BlockSpan { Start = best, Open = open, End = end, Text = text.Substring(best, end - best + 1) };
        }

        public static string UpdateUnit(string text, string name, string unitClass, string preset, int count)
        {
            BlockSpan span = UnitBlockByName(text, name);
            string block = ReplaceStringField(span.Text, "unit_class", unitClass);
            block = ReplaceStringField(block, "weapons", preset);
            block = ReplaceIntField(block, "count", count);
            return ReplaceSpan(text, span, block);
        }

        public static string ConfigureCombinedScenario(string text, CombinedMap map, CombinedSpawn spawn)
        {
            if (map == null || String.IsNullOrWhiteSpace(map.Level)) throw new InvalidOperationException("The selected combined-battles map is invalid.");
            if (spawn == null || String.IsNullOrWhiteSpace(spawn.Transform)) throw new InvalidOperationException("The selected combined-battles spawn is invalid.");
            int side = spawn.Side == 2 ? 2 : 1;

            BlockSpan missionSettings = FirstBlock(text, "mission_settings", 0);
            if (missionSettings == null) throw new InvalidOperationException("Mission settings are missing.");
            BlockSpan mission = FirstBlock(missionSettings.Text, "mission", 0);
            if (mission == null) throw new InvalidOperationException("Mission definition is missing.");
            string missionText = ReplaceStringField(mission.Text, "level", map.Level.Replace("\"", ""));
            string updatedSettings = ReplaceSpan(missionSettings.Text, mission, missionText);
            BlockSpan playerSettings = FirstBlock(updatedSettings, "player", 0);
            if (playerSettings == null) throw new InvalidOperationException("Player-side settings are missing.");
            string playerSettingsText = Regex.Replace(playerSettings.Text, @"(?m)^(\s*army:i\s*=\s*)-?\d+\s*$", delegate(Match match)
            {
                return match.Groups[1].Value + side.ToString(CultureInfo.InvariantCulture);
            }, RegexOptions.IgnoreCase);
            updatedSettings = ReplaceSpan(updatedSettings, playerSettings, playerSettingsText);
            text = ReplaceSpan(text, missionSettings, updatedSettings);

            double verticalOffset = spawn.Kind.Equals("aircraft", StringComparison.OrdinalIgnoreCase) && spawn.Option.Equals("airfield", StringComparison.OrdinalIgnoreCase) ? 3.0 :
                spawn.Kind.Equals("helicopter", StringComparison.OrdinalIgnoreCase) ? 1.5 :
                spawn.Kind.Equals("ground", StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
            string playerTransform = NormalizeTransform(spawn.Transform, verticalOffset, 1.0);
            BlockSpan player = UnitBlockByName(text, "You");
            string playerBlock = ReplaceMatrixField(player.Text, "tm", playerTransform);
            playerBlock = Regex.Replace(playerBlock, @"(?m)^(\s*army:i\s*=\s*)-?\d+\s*$", delegate(Match match)
            {
                return match.Groups[1].Value + side.ToString(CultureInfo.InvariantCulture);
            }, RegexOptions.IgnoreCase);
            text = ReplaceSpan(text, player, playerBlock);

            // Combined-battles mode is intentionally a solo sandbox. Rebuild the
            // units section with only the player and the selected physical runway or
            // helipad object, so no range targets or map bots can be instantiated.
            BlockSpan units = FirstBlock(text, "units", 0);
            if (units == null) throw new InvalidOperationException("Mission units block is missing.");
            player = UnitBlockByName(text, "You");
            StringBuilder unitsText = new StringBuilder();
            unitsText.AppendLine("units{");
            unitsText.AppendLine(player.Text.TrimEnd());
            if (!String.IsNullOrWhiteSpace(spawn.ObjectClass))
            {
                if (!Regex.IsMatch(spawn.ObjectClass, @"^[A-Za-z0-9_./-]+$")) throw new InvalidOperationException("The selected spawn object is invalid.");
                unitsText.AppendLine();
                unitsText.AppendLine("  objectGroups{");
                unitsText.AppendLine("    name:t=\"UTL_Selected_Spawn_Base\"");
                unitsText.AppendLine("    tm:m=" + NormalizeTransform(spawn.Transform, 0.0, 1.0));
                unitsText.AppendLine("    unit_class:t=\"" + spawn.ObjectClass + "\"");
                unitsText.AppendLine("    objLayer:i=2");
                unitsText.AppendLine("    props{");
                unitsText.AppendLine("      army:i=" + side.ToString(CultureInfo.InvariantCulture));
                unitsText.AppendLine("      active:b=yes");
                unitsText.AppendLine("    }");
                unitsText.AppendLine("  }");
            }
            unitsText.Append("}");
            text = ReplaceSpan(text, units, unitsText.ToString());

            BlockSpan triggers = FirstBlock(text, "triggers", 0);
            if (triggers == null) throw new InvalidOperationException("Mission triggers block is missing.");
            StringBuilder triggerText = new StringBuilder();
            triggerText.AppendLine("triggers{");
            triggerText.AppendLine("  isCategory:b=yes");
            triggerText.AppendLine("  is_enabled:b=yes");
            foreach (string triggerName in new[] { "\"Player Full Internal Fuel\"", "\"Player Respawn Flight Profile\"" })
            {
                BlockSpan playerTrigger = FirstBlock(text, triggerName, 0);
                if (playerTrigger != null)
                {
                    triggerText.AppendLine();
                    triggerText.AppendLine(playerTrigger.Text.TrimEnd());
                }
            }
            bool aircraftMap = spawn.Kind.Equals("aircraft", StringComparison.OrdinalIgnoreCase);
            List<CombinedCapturePoint> navigationCaptures = map.CapturePoints
                .Where(x => x != null && !String.IsNullOrWhiteSpace(x.Transform))
                .OrderBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();
            List<CombinedSpawn> navigationSpawns = map.Spawns
                .Where(x => x != null && x.Kind.Equals(spawn.Kind, StringComparison.OrdinalIgnoreCase) && !String.IsNullOrWhiteSpace(x.Transform))
                .OrderBy(x => x.Side)
                .ThenBy(x => x.Option, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (navigationCaptures.Count > 0 || navigationSpawns.Count > 0)
                AppendCombinedNavigationTrigger(triggerText, navigationCaptures, navigationSpawns,
                    spawn.Kind.Equals("ground", StringComparison.OrdinalIgnoreCase));
            if (aircraftMap)
            {
                triggerText.AppendLine(@"
  ""UTL Aircraft Map Extent""{
    is_enabled:b=yes
    comments:t=""Use an aviation-scale tactical map without imposing an out-of-bounds kill zone""

    props{
      actionsType:t=""PERFORM_ONE_BY_ONE""
      conditionsType:t=""ALL""
      enableAfterComplete:b=no
    }

    events{
      initMission{}
    }

    conditions{}

    actions{
      missionBattleArea{
        air:b=yes
        ground:b=no
        mapArea:b=no
        airMapArea:b=yes
        killArea:b=no
        detectionArea:b=no
        killOutOfBattleArea:b=no
        newGridHorizontalCellCount:i=0
        area:t=""UTL_Air_Map_Area""
      }
    }

    else_actions{}
  }");
            }
            triggerText.Append("}");
            text = ReplaceSpan(text, triggers, triggerText.ToString());

            // Clean-test-range zones and waypoints belong to another level. Keeping
            // them would leave stray HUD markers and actions at unrelated coordinates.
            BlockSpan areas = FirstBlock(text, "areas", 0);
            if (areas != null)
            {
                string areaText = BuildCombinedNavigationAreas(map, spawn, aircraftMap, navigationCaptures, navigationSpawns);
                text = ReplaceSpan(text, areas, areaText);
            }
            BlockSpan wayPoints = FirstBlock(text, "wayPoints", 0);
            if (wayPoints != null) text = ReplaceSpan(text, wayPoints, "wayPoints{\r\n}");
            return text;
        }

        public static string CombinedRespawnTransform(CombinedSpawn spawn)
        {
            if (spawn == null) throw new ArgumentNullException("spawn");
            double verticalOffset = spawn.Kind.Equals("aircraft", StringComparison.OrdinalIgnoreCase) && spawn.Option.Equals("airfield", StringComparison.OrdinalIgnoreCase) ? 3.0 :
                spawn.Kind.Equals("helicopter", StringComparison.OrdinalIgnoreCase) ? 1.5 :
                spawn.Kind.Equals("ground", StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
            return NormalizeTransform(spawn.Transform, verticalOffset, 10.0);
        }

        private static void AppendCombinedNavigationTrigger(StringBuilder output, IList<CombinedCapturePoint> captures, IList<CombinedSpawn> spawns, bool showCaptureHud)
        {
            output.AppendLine();
            output.AppendLine("  \"UTL Combined Map Markers\"{");
            output.AppendLine("    is_enabled:b=yes");
            output.AppendLine("    comments:t=\"Native Domination capture and spawn locations for navigation only\"");
            output.AppendLine();
            output.AppendLine("    props{");
            output.AppendLine("      actionsType:t=\"PERFORM_ONE_BY_ONE\"");
            output.AppendLine("      conditionsType:t=\"ALL\"");
            output.AppendLine("      enableAfterComplete:b=no");
            output.AppendLine("    }");
            output.AppendLine();
            output.AppendLine("    events{");
            output.AppendLine("      initMission{}");
            output.AppendLine("    }");
            output.AppendLine();
            output.AppendLine("    conditions{}");
            output.AppendLine();
            output.AppendLine("    actions{");
            foreach (CombinedCapturePoint capture in captures)
            {
                output.AppendLine("      missionMarkAsCaptureZone{");
                output.AppendLine("        army:i=0");
                output.AppendLine("        timeMultiplier:r=1");
                output.AppendLine("        disableZone:b=no");
                output.AppendLine("        name_for_respawn_base:t=\"\"");
                output.AppendLine("        target:t=\"" + CombinedCaptureAreaName(capture) + "\"");
                output.AppendLine("        canCaptureOnGround:b=no");
                output.AppendLine("        canCaptureInAir:b=no");
                output.AppendLine("        playAirfieldSound:b=no");
                output.AppendLine("        canCaptureByGM:b=no");
                output.AppendLine("        onlyPlayersCanCapture:b=yes");
                output.AppendLine("        useHUDMarkers:b=" + (showCaptureHud ? "yes" : "no"));
                output.AppendLine("        showBorderOnMap:b=yes");
                output.AppendLine("        zoneDefenders{}");
                output.AppendLine("        capture_tags{");
                output.AppendLine("          tank:b=no");
                output.AppendLine("        }");
                output.AppendLine("      }");
            }
            foreach (CombinedSpawn marker in spawns)
            {
                bool ground = marker.Kind.Equals("ground", StringComparison.OrdinalIgnoreCase);
                bool airfield = marker.Option.Equals("airfield", StringComparison.OrdinalIgnoreCase) || marker.Kind.Equals("helicopter", StringComparison.OrdinalIgnoreCase);
                string location = ground ? "missions/spawn_01" : airfield ? "missions/airfield_spawn" : "missions/air_spawn";
                output.AppendLine("      missionMarkAsRespawnPoint{");
                output.AppendLine("        loc_name:t=\"" + location + "\"");
                output.AppendLine("        spawnEffect:b=no");
                output.AppendLine("        isStrictSpawn:b=no");
                output.AppendLine("        resetStrictSpawnIndex:b=no");
                output.AppendLine("        isAirfield:b=" + (airfield ? "yes" : "no"));
                output.AppendLine("        isUnit:b=no");
                output.AppendLine("        forceCreate:b=no");
                output.AppendLine("        useExisting:b=no");
                output.AppendLine("        ignoreTeamsOnReuse:b=no");
                output.AppendLine("        isIndividual:b=" + (ground ? "yes" : "no"));
                output.AppendLine("        onlyOnePlayerPerSpawnPoint:b=no");
                output.AppendLine("        removeAreas:b=no");
                output.AppendLine("        replaceAreas:b=no");
                output.AppendLine("        canSpawnOnNeutral:b=no");
                output.AppendLine("        showOnMap:b=yes");
                output.AppendLine("        radius:r=-1");
                output.AppendLine("        target:t=\"" + CombinedSpawnAreaName(marker) + "\"");
                output.AppendLine("        team:t=\"" + (marker.Side == 2 ? "B" : "A") + "\"");
                output.AppendLine("        tags{");
                output.AppendLine("          tank:b=" + (ground ? "yes" : "no"));
                output.AppendLine("        }");
                output.AppendLine("      }");
            }
            output.AppendLine("    }");
            output.AppendLine();
            output.AppendLine("    else_actions{}");
            output.AppendLine("  }");
        }

        private static string BuildCombinedNavigationAreas(CombinedMap map, CombinedSpawn selectedSpawn, bool aircraftMap, IList<CombinedCapturePoint> captures, IList<CombinedSpawn> spawns)
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine("areas{");
            if (aircraftMap)
                AppendCombinedArea(output, "UTL_Air_Map_Area", CombinedAirMapTransform(map, selectedSpawn));
            foreach (CombinedCapturePoint capture in captures)
                AppendCombinedArea(output, CombinedCaptureAreaName(capture), CanonicalTransform(capture.Transform));
            foreach (CombinedSpawn marker in spawns)
            {
                double radius = marker.Kind.Equals("aircraft", StringComparison.OrdinalIgnoreCase) ? 180.0 :
                    marker.Kind.Equals("helicopter", StringComparison.OrdinalIgnoreCase) ? 90.0 : 45.0;
                AppendCombinedArea(output, CombinedSpawnAreaName(marker), NormalizeTransform(marker.Transform, 0.0, radius));
            }
            output.Append("}");
            return output.ToString();
        }

        private static void AppendCombinedArea(StringBuilder output, string name, string transform)
        {
            output.AppendLine("  " + name + "{");
            output.AppendLine("    type:t=\"Sphere\"");
            output.AppendLine("    tm:m=" + transform);
            output.AppendLine("    objLayer:i=0");
            output.AppendLine();
            output.AppendLine("    props{}");
            output.AppendLine("  }");
        }

        private static string CombinedCaptureAreaName(CombinedCapturePoint capture)
        {
            return "UTL_Capture_" + CombinedToken(capture == null ? "Point" : capture.Label);
        }

        private static string CombinedSpawnAreaName(CombinedSpawn spawn)
        {
            return "UTL_Spawn_S" + (spawn != null && spawn.Side == 2 ? "2" : "1") + "_" + CombinedToken(spawn == null ? "point" : spawn.Option);
        }

        private static string CombinedToken(string value)
        {
            string token = Regex.Replace(value ?? "", @"[^A-Za-z0-9_]+", "_").Trim('_');
            return String.IsNullOrEmpty(token) ? "point" : token;
        }

        private static string CanonicalTransform(string source)
        {
            MatchCollection matches = Regex.Matches(source ?? "", @"[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?");
            if (matches.Count != 12) throw new InvalidOperationException("Map marker transform does not contain a 3x4 mission matrix.");
            double[] values = matches.Cast<Match>().Select(x => Double.Parse(x.Value, NumberStyles.Float, CultureInfo.InvariantCulture)).ToArray();
            Func<double, string> number = value => Math.Abs(value) < 0.0000001 ? "0" : value.ToString("0.######", CultureInfo.InvariantCulture);
            return "[[" + number(values[0]) + ", " + number(values[1]) + ", " + number(values[2]) + "] [" +
                number(values[3]) + ", " + number(values[4]) + ", " + number(values[5]) + "] [" +
                number(values[6]) + ", " + number(values[7]) + ", " + number(values[8]) + "] [" +
                number(values[9]) + ", " + number(values[10]) + ", " + number(values[11]) + "]]";
        }

        private static string ReplaceMatrixField(string block, string field, string value)
        {
            Regex regex = new Regex("(?m)^(\\s*)" + Regex.Escape(field) + @":m\s*=\s*\[\[[^\r\n]+\]\]\s*$");
            if (!regex.IsMatch(block)) throw new InvalidOperationException("BLK matrix field not found: " + field);
            return regex.Replace(block, delegate(Match match) { return match.Groups[1].Value + field + ":m=" + value; }, 1);
        }

        private static string NormalizeTransform(string source, double verticalOffset, double orientationScale)
        {
            MatchCollection matches = Regex.Matches(source ?? "", @"[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?");
            if (matches.Count != 12) throw new InvalidOperationException("Spawn transform does not contain a 3x4 mission matrix.");
            double[] values = matches.Cast<Match>().Select(x => Double.Parse(x.Value, NumberStyles.Float, CultureInfo.InvariantCulture)).ToArray();
            for (int row = 0; row < 3; row++)
            {
                int offset = row * 3;
                double length = Math.Sqrt(values[offset] * values[offset] + values[offset + 1] * values[offset + 1] + values[offset + 2] * values[offset + 2]);
                if (length < 0.000001) throw new InvalidOperationException("Spawn transform contains an empty orientation row.");
                for (int column = 0; column < 3; column++) values[offset + column] = values[offset + column] / length * orientationScale;
            }
            values[10] += verticalOffset;
            Func<double, string> number = value => Math.Abs(value) < 0.0000001 ? "0" : value.ToString("0.######", CultureInfo.InvariantCulture);
            return "[[" + number(values[0]) + ", " + number(values[1]) + ", " + number(values[2]) + "] [" +
                number(values[3]) + ", " + number(values[4]) + ", " + number(values[5]) + "] [" +
                number(values[6]) + ", " + number(values[7]) + ", " + number(values[8]) + "] [" +
                number(values[9]) + ", " + number(values[10]) + ", " + number(values[11]) + "]]";
        }

        private static string CombinedAirMapTransform(CombinedMap map, CombinedSpawn selectedSpawn)
        {
            List<double[]> points = new List<double[]>();
            IEnumerable<CombinedSpawn> candidates = (map == null ? Enumerable.Empty<CombinedSpawn>() : map.Spawns)
                .Where(x => x != null && x.Kind.Equals("aircraft", StringComparison.OrdinalIgnoreCase));
            foreach (CombinedSpawn candidate in candidates.Concat(new[] { selectedSpawn }).Where(x => x != null))
            {
                MatchCollection values = Regex.Matches(candidate.Transform ?? "", @"[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?");
                if (values.Count != 12) continue;
                points.Add(new[]
                {
                    Double.Parse(values[9].Value, NumberStyles.Float, CultureInfo.InvariantCulture),
                    Double.Parse(values[11].Value, NumberStyles.Float, CultureInfo.InvariantCulture)
                });
            }
            if (points.Count == 0) throw new InvalidOperationException("Aircraft map coordinates are missing.");
            double centerX = (points.Min(x => x[0]) + points.Max(x => x[0])) / 2.0;
            double centerZ = (points.Min(x => x[1]) + points.Max(x => x[1])) / 2.0;
            const double radius = 40000.0;
            Func<double, string> number = value => Math.Abs(value) < 0.0000001 ? "0" : value.ToString("0.######", CultureInfo.InvariantCulture);
            return "[[" + number(radius) + ", 0, 0] [0, " + number(radius) + ", 0] [0, 0, " + number(radius) + "] [" + number(centerX) + ", 0, " + number(centerZ) + "]]";
        }

        public static string ConfigureUnitModifications(string text, string name, bool applyAll, IEnumerable<string> modifications)
        {
            BlockSpan span = UnitBlockByName(text, name);
            string block = Regex.Replace(span.Text, @"(?m)^[ \t]*modification:t\s*=\s*""[^""]*""[ \t]*\r?\n", "");
            Regex applyRegex = new Regex(@"(?m)^(\s*)applyAllMods:b\s*=\s*(?:yes|no|true|false)\s*$", RegexOptions.IgnoreCase);
            Match apply = applyRegex.Match(block);
            if (!apply.Success) throw new InvalidOperationException("Mission unit has no applyAllMods field: " + name);
            block = applyRegex.Replace(block, delegate(Match m)
            {
                return m.Groups[1].Value + "applyAllMods:b=" + (applyAll ? "yes" : "no");
            }, 1);
            {
                string indent = apply.Groups[1].Value;
                string lines = String.Join(Environment.NewLine, (modifications ?? Enumerable.Empty<string>())
                    .Where(x => !String.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .Select(x => indent + "modification:t=\"" + x.Replace("\"", "") + "\"").ToArray());
                if (!String.IsNullOrEmpty(lines))
                {
                    Match insertion = applyRegex.Match(block);
                    block = block.Insert(insertion.Index, lines + Environment.NewLine);
                }
            }
            return ReplaceSpan(text, span, block);
        }

        public static string MakeGroundTargetHostile(string text, string name)
        {
            BlockSpan unit = UnitBlockByName(text, name);
            string block = new Regex(@"crewSkillK:r\s*=\s*[0-9.]+").Replace(unit.Text, "crewSkillK:r=1", 1);
            block = new Regex(@"applyAllMods:b\s*=\s*(?:no|false)").Replace(block, "applyAllMods:b=yes", 1);
            block = new Regex(@"attack_type:t\s*=\s*""[^""]*""").Replace(block, "attack_type:t=\"fire_at_will\"", 1);
            text = ReplaceSpan(text, unit, block);
            BlockSpan triggers = FirstBlock(text, "triggers", 0);
            if (triggers == null) throw new InvalidOperationException("Mission triggers block is missing.");
            string trigger = @"
  ""UTL Hostile Ground Target - " + name + @"""{
    is_enabled:b=yes
    comments:t=""Keep the selected enemy ground unit actively engaging the player""
    props{
      actionsType:t=""PERFORM_ONE_BY_ONE""
      conditionsType:t=""ALL""
      enableAfterComplete:b=yes
    }
    events{ periodicEvent{ time:r=1 } }
    conditions{}
    actions{
      unitSetProperties{
        object:t=""" + name + @"""
        isImmortal:b=no
        attack_type:t=""fire_at_will""
      }
      unitAttackTarget{
        playerAttracted:b=yes
        object:t=""" + name + @"""
        target:t=""You""
        fireRandom:b=no
        fireMode:t=""auto""
      }
    }
    else_actions{}
  }
";
            return text.Insert(triggers.End, trigger);
        }

        public static string SetSamSites(string text, string mode, string selection)
        {
            // The CTR_ SAM sites are spawned by dedicated triggers (spawn_ctr_s300_sites /
            // spawn_ctr_patriot_sites / spawn_ctr_buk_sites) from isDelayed tank models.
            // mode: "active" | "passive" | "friendly" | "disabled"
            //   passive keeps the sites on the field but marks every CTR_ unit
            //   attack_type:t="dont_aim" so they never engage the player.
            //   friendly flips every CTR_ unit to army1 so the sites intercept
            //   the enemy air targets (Target_Air / Heli) instead of the player.
            // selection: "all" | "s300" | "patriot" | "hawk" | "buk"
            string[] triggers = { "spawn_ctr_s300_sites", "spawn_ctr_hawk_sites", "spawn_ctr_patriot_sites", "spawn_ctr_buk_sites", "spawn_ctr_aew_55j6", "spawn_ctr_aew_tps59", "create_ctr_sites", "ctr_s300_sites", "ctr_hawk_sites", "ctr_patriot_sites", "ctr_buk_sites" };
            string[] keys = { "s300", "hawk", "patriot", "buk", "s300", "hawk|patriot", "*", "s300", "hawk", "patriot", "buk" };
            for (int i = 0; i < triggers.Length; i++)
            {
                BlockSpan trigger = FirstBlock(text, triggers[i], 0);
                if (trigger == null) continue;
                bool enabled = mode != "disabled" && (keys[i] == "*" || selection == "all" || keys[i].Split('|').Contains(selection));
                string block = new Regex(@"(?m)^(\s*is_enabled:b\s*=\s*)(?:yes|no|true|false)\s*$").Replace(trigger.Text, "$1" + (enabled ? "yes" : "no"), 1);
                text = ReplaceSpan(text, trigger, block);
            }
            if (mode == "passive")
            {
                text = new Regex(@"(tankModels\{\s*name:t=""CTR_[^""]+""(?:(?!attack_type)[\s\S])*?props\{)").Replace(text, "$1\n      attack_type:t=\"dont_aim\"");
            }
            if (mode == "friendly")
            {
                text = new Regex(@"(name:t=""CTR_[^""]+""[\s\S]*?props\{\s*army:i=)2").Replace(text, "${1}1");
            }
            return text;
        }

        public static string DisablePlayerSwitch(string text)
        {
            int marker = text.IndexOf("comments:t=\"UTL_PLAYER_SWITCH\"", StringComparison.Ordinal);
            if (marker < 0) throw new InvalidOperationException("UTL player-switch marker is missing.");
            int triggerStart = text.LastIndexOf("\"Universal aircraft switch\"{", marker, StringComparison.Ordinal);
            int enabled = text.IndexOf("is_enabled:b=", triggerStart, StringComparison.Ordinal);
            if (triggerStart < 0 || enabled < 0 || enabled > marker) throw new InvalidOperationException("Player-switch trigger is invalid.");
            int valueStart = enabled + "is_enabled:b=".Length;
            int valueEnd = text.IndexOfAny(new[] { '\r', '\n' }, valueStart);
            if (valueEnd < 0) valueEnd = text.Length;
            return text.Substring(0, valueStart) + "no" + text.Substring(valueEnd);
        }

        public static string ConfigureAirPlayerSwitch(string text, string unitClass, string preset, AircraftSettings settings)
        {
            int marker = text.IndexOf("comments:t=\"UTL_PLAYER_SWITCH\"", StringComparison.Ordinal);
            if (marker < 0) throw new InvalidOperationException("UTL player-switch marker is missing.");
            int triggerStart = text.LastIndexOf("\"Universal aircraft switch\"{", marker, StringComparison.Ordinal);
            int triggerOpen = triggerStart < 0 ? -1 : text.IndexOf('{', triggerStart);
            int triggerEnd = triggerOpen < 0 ? -1 : MatchingBrace(text, triggerOpen);
            if (triggerStart < 0 || triggerOpen < 0 || triggerEnd < 0) throw new InvalidOperationException("Player-switch trigger is invalid.");
            string trigger = text.Substring(triggerStart, triggerEnd - triggerStart + 1);
            trigger = Regex.Replace(trigger, @"(?m)^(\s*is_enabled:b\s*=\s*)(?:no|false|yes|true)\s*$", "$1yes", RegexOptions.IgnoreCase);
            trigger = ReplaceStringField(trigger, "unit_class", unitClass);
            trigger = ReplaceStringField(trigger, "weaponPreset", preset);
            BlockSpan change = FirstBlock(trigger, "changeUnit", 0);
            if (change == null) throw new InvalidOperationException("Player-switch changeUnit action is missing.");
            StringBuilder options = new StringBuilder();
            bool allMods = settings == null || settings.UseAllModifications;
            options.AppendLine("        applyAllMods:b=" + (allMods ? "yes" : "no"));
            if (!allMods && settings != null)
                foreach (string modification in settings.EnabledModifications.Where(x => !String.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
                    options.AppendLine("        modification:t=\"" + modification.Replace("\"", "") + "\"");
            if (settings != null)
                foreach (KeyValuePair<int, string> belt in settings.GunBeltSelections.OrderBy(x => x.Key))
                    if (belt.Key >= 0 && belt.Key < 4 && !String.IsNullOrWhiteSpace(belt.Value))
                        options.AppendLine("        bullets" + belt.Key.ToString(CultureInfo.InvariantCulture) + ":t=\"" + belt.Value.Replace("\"", "") + "\"");
            string changeText = change.Text.Insert(change.Text.LastIndexOf('}'), options.ToString());
            trigger = ReplaceSpan(trigger, change, changeText);
            return text.Substring(0, triggerStart) + trigger + text.Substring(triggerEnd + 1);
        }

        public static string UpdateMissionLabels(string text, string name, string description)
        {
            Regex nameRegex = new Regex(@"(locName:t\s*=\s*"")[^""]*("")");
            Regex descRegex = new Regex(@"(locDesc:t\s*=\s*"")[^""]*("")");
            text = nameRegex.Replace(text, delegate(Match m) { return m.Groups[1].Value + name.Replace("\"", "'") + m.Groups[2].Value; }, 1);
            return descRegex.Replace(text, delegate(Match m) { return m.Groups[1].Value + description.Replace("\"", "'") + m.Groups[2].Value; }, 1);
        }

        public static string CleanLegacyMenuKeys(string text)
        {
            text = Regex.Replace(text, @"campaign:t\s*=\s*""(?:UniversalTestLab|CleanTestDrive)""", "campaign:t=\"UserMissions\"");
            text = Regex.Replace(text, @"(?m)^[ \t]*chapter:t\s*=\s*""TestDrive""[ \t]*\r?\n", "");
            return text;
        }

        public static string AddFpvDetonationTriggers(string text)
        {
            BlockSpan triggers = FirstBlock(text, "triggers", 0);
            if (triggers == null) throw new InvalidOperationException("Mission triggers block is missing.");
            StringBuilder result = new StringBuilder();
            string[] groundTargets =
            {
                "Target_01", "Target_02", "Target_03", "Target_04", "Target_05", "Target_06", "Target_07",
                "AI_Shooting_01", "AI_Shooting_02", "AI_Shooting_03", "AI_Shooting_04",
                "AI_Target_01", "AI_Target_02", "AI_Target_03", "AI_Target_04", "AI_Driving"
            };
            foreach (string target in groundTargets) result.Append(FpvTargetTrigger(target, target, 6));
            foreach (string target in new[] { "Target_Air_01", "Target_Air_02", "Heli_Target", "Heli_Target_02" })
                result.Append(FpvTargetTrigger(target, target, 8));
            result.Append(FpvTargetTrigger("Ship_Target", "Ship_Target", 45));
            result.Append(FpvDeathEffectTrigger());
            result.Append(FpvRespawnRearmTrigger());
            return text.Insert(triggers.End, result.ToString());
        }

        private static string FpvTargetTrigger(string label, string target, int distance)
        {
            return @"
  ""UTL FPV Detonation - " + label + @"""{
    is_enabled:b=yes
    comments:t=""Detonate the FPV only when it reaches this target""
    props{
      actionsType:t=""PERFORM_ONE_BY_ONE""
      conditionsType:t=""ALL""
      enableAfterComplete:b=yes
    }
    events{ periodicEvent{ time:r=0.01 } }
    conditions{
      unitDistanceBetween{
        value:r=" + distance.ToString(CultureInfo.InvariantCulture) + @"
        math:t=""3D""
        object_type:t=""any""
        target_type:t=""any""
        check_objects:t=""any""
        check_targets:t=""any""
        object_marking:i=0
        target_marking:i=0
        object_var_name:t=""""
        object_var_comp_op:t=""equal""
        object_var_value:i=0
        object:t=""You""
        target:t=""" + target + @"""
      }
    }
    actions{
      unitDamage{
        power:r=0.35
        useEffect:b=false
        countEffects:i=1
        delay:p2=1, 1
        offset:p3=0, 0, 0
        radiusOffset:p2=0, 0
        target:t=""" + target + @"""
        randomTargetsCount:i=1
        doExplosion:b=true
      }
      unitDamage{
        power:r=1
        useEffect:b=false
        countEffects:i=1
        delay:p2=1, 1
        offset:p3=0, 0, 0
        radiusOffset:p2=0, 0
        target:t=""You""
        doExplosion:b=true
      }
    }
    else_actions{}
  }
";
        }

        private static string FpvDeathEffectTrigger()
        {
            return @"
  ""UTL FPV Detonation Effect""{
    is_enabled:b=yes
    comments:t=""Show one local HEAT explosion whenever the FPV is destroyed""
    props{
      actionsType:t=""PERFORM_ONE_BY_ONE""
      conditionsType:t=""ALL""
      enableAfterComplete:b=no
    }
    events{ periodicEvent{ time:r=0.02 } }
    conditions{
      unitWhenStatus{
        object_type:t=""isKilled""
        check_objects:t=""any""
        object_marking:i=0
        object_var_name:t=""""
        object_var_comp_op:t=""equal""
        object_var_value:i=0
        target_type:t=""isAlive""
        check_period:r=0.02
        object:t=""You""
      }
    }
    actions{
      unitPlayEffect{
        effect_type:t=""specify""
        effect:t=""hit_81_132mm_heat""
        offset:p3=0, 0, 0
        radiusOffset:p2=0, 0
        show:b=true
        attach:b=false
        scale:r=1
        loopSpawn:b=false
        delay:p2=1, 1
        target:t=""You""
      }
    }
    else_actions{}
  }
";
        }

        private static string FpvRespawnRearmTrigger()
        {
            return @"
  ""UTL FPV Re-arm Detonator""{
    is_enabled:b=yes
    comments:t=""Re-enable the one-shot FPV explosion after each respawn""
    props{
      actionsType:t=""PERFORM_ONE_BY_ONE""
      conditionsType:t=""ALL""
      enableAfterComplete:b=yes
    }
    events{ periodicEvent{ time:r=0.1 } }
    conditions{
      unitWhenRespawn{
        object_var_name:t=""""
        object_var_comp_op:t=""equal""
        object:t=""You""
      }
    }
    actions{
      triggerEnable{ target:t=""UTL FPV Detonation Effect"" }
    }
    else_actions{}
  }
";
        }

        public static string RemoveBotNotifications(string text)
        {
            List<BlockSpan> remove = new List<BlockSpan>();
            foreach (BlockSpan hint in Blocks(text, "playHint"))
            {
                string name = Field(hint.Text, "name", "t") ?? "";
                if (name.IndexOf("Respawning", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Rearmed", StringComparison.OrdinalIgnoreCase) >= 0)
                    remove.Add(hint);
            }
            foreach (BlockSpan hint in remove.OrderByDescending(x => x.Start))
                text = text.Remove(hint.Start, hint.End - hint.Start + 1);
            return text;
        }
        public static string MakeShipPassive(string text, string name)
        {
            BlockSpan unit = UnitBlockByName(text, name);
            string block = Regex.Replace(unit.Text, @"crewSkillK:r\s*=\s*[0-9.]+", "crewSkillK:r=0", RegexOptions.IgnoreCase);
            block = Regex.Replace(block, @"attack_type:t\s*=\s*""[^""]*""", "attack_type:t=\"dont_aim\"", RegexOptions.IgnoreCase);
            return ReplaceSpan(text, unit, block);
        }

        public static string ConfigureGroundPlayer(string text, string unitClass, string modelId, string preset, AircraftSettings settings, IEnumerable<GroundAmmoLoadout> missionAmmo = null)
        {
            BlockSpan old = UnitBlockByName(text, "You");
            List<GroundAmmoLoadout> ammunition = (missionAmmo ?? (settings == null ? Enumerable.Empty<GroundAmmoLoadout>() : settings.GroundAmmoLoadouts))
                .Where(x => x != null && x.Slot >= 0 && x.Slot < 4 && x.Count > 0)
                .GroupBy(x => x.Slot)
                .Select(x => x.Last().Copy())
                .ToList();
            StringBuilder block = new StringBuilder();
            block.AppendLine("  tankModels{");
            block.AppendLine("    name:t=\"You\"");
            block.AppendLine("    tm:m=[[-0.5, 0, 0.866025] [0, 1, 0] [-0.866025, 0, -0.5] [6.3526, 41.581, -622.332]]");
            block.AppendLine("    unit_class:t=\"" + unitClass + "\"");
            block.AppendLine("    objLayer:i=1");
            block.AppendLine("    closed_waypoints:b=no");
            block.AppendLine("    isShipSpline:b=no");
            block.AppendLine("    shipTurnRadius:r=100");
            block.AppendLine("    weapons:t=\"" + (preset ?? "").Replace("\"", "") + "\"");
            // Keep the known-working reserve proxy and native tank controller. The
            // projectile IDs and counts select the real ammunition carried by the
            // included vehicle; the proxy class can still supply fallback HUD metadata.
            for (int i = 0; i < 4; i++)
            {
                GroundAmmoLoadout loadout = ammunition.FirstOrDefault(x => x.Slot == i);
                string ammunitionId = loadout == null ? "" : (!String.IsNullOrWhiteSpace(loadout.AmmoGroup) ? loadout.AmmoGroup : loadout.BulletName);
                block.AppendLine("    bullets" + i.ToString(CultureInfo.InvariantCulture) + ":t=\"" + (ammunitionId ?? "").Replace("\"", "") + "\"");
            }
            if (ammunition.Count == 0)
            {
                block.AppendLine("    bulletsCount0:i=9999");
                block.AppendLine("    bulletsCount1:i=0");
                block.AppendLine("    bulletsCount2:i=0");
                block.AppendLine("    bulletsCount3:i=0");
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    GroundAmmoLoadout loadout = ammunition.FirstOrDefault(x => x.Slot == i);
                    block.AppendLine("    bulletsCount" + i.ToString(CultureInfo.InvariantCulture) + ":i=" + Math.Max(0, loadout == null ? 0 : loadout.Count).ToString(CultureInfo.InvariantCulture));
                }
            }
            // Player test vehicles should use a fully trained crew. Leaving this at zero
            // made reload, targeting and rangefinding behave like a stock crew.
            block.AppendLine("    crewSkillK:r=1");
            block.AppendLine("    applyAllMods:b=no");
            block.AppendLine("    props{");
            block.AppendLine("      army:i=1");
            block.AppendLine("      count:i=1");
            block.AppendLine("      formation_type:t=\"rows\"");
            block.AppendLine("      formation_div:i=3");
            block.AppendLine("      formation_step:p2=2.5, 2");
            block.AppendLine("      formation_noise:p2=0.1, 0.1");
            block.AppendLine("      uniqueName:t=\"Player\"");
            block.AppendLine("      attack_type:t=\"fire_at_will\"");
            block.AppendLine("    }");
            block.AppendLine("    way{}");
            block.Append("  }");
            string result = ReplaceSpan(text, old, block.ToString());
            foreach (string triggerName in new[] { "\"Player Full Internal Fuel\"", "\"Player Respawn Flight Profile\"" })
            {
                BlockSpan aircraftOnly = FirstBlock(result, triggerName, 0);
                if (aircraftOnly == null) continue;
                string disabled = Regex.Replace(aircraftOnly.Text, @"(?m)^(\s*is_enabled:b\s*=\s*)(?:yes|true)\s*$", "$1no", RegexOptions.IgnoreCase);
                result = ReplaceSpan(result, aircraftOnly, disabled);
            }
            return result;
        }

        public static List<BlockSpan> DirectChildBlocks(string containerText)
        {
            List<BlockSpan> result = new List<BlockSpan>();
            if (String.IsNullOrWhiteSpace(containerText)) return result;
            int outerOpen = containerText.IndexOf('{');
            int outerEnd = outerOpen < 0 ? -1 : MatchingBrace(containerText, outerOpen);
            if (outerOpen < 0 || outerEnd <= outerOpen) return result;
            int cursor = outerOpen + 1;
            Regex header = new Regex(@"(?m)^\s*""?([A-Za-z0-9_.@:$-]+)""?\s*\{");
            while (cursor < outerEnd)
            {
                Match match = header.Match(containerText, cursor);
                if (!match.Success || match.Index >= outerEnd) break;
                int open = containerText.IndexOf('{', match.Index);
                if (open < 0 || open >= outerEnd) break;
                int end = MatchingBrace(containerText, open);
                if (end < 0 || end > outerEnd) break;
                result.Add(new BlockSpan { Start = match.Index, Open = open, End = end, Text = containerText.Substring(match.Index, end - match.Index + 1) });
                cursor = end + 1;
            }
            return result;
        }

        public static List<BlockSpan> RootBlocks(string text)
        {
            List<BlockSpan> result = new List<BlockSpan>();
            if (String.IsNullOrWhiteSpace(text)) return result;
            int cursor = 0;
            Regex header = new Regex(@"(?m)^\s*""?([A-Za-z0-9_.@:$-]+)""?\s*\{");
            while (cursor < text.Length)
            {
                Match match = header.Match(text, cursor);
                if (!match.Success) break;
                int open = text.IndexOf('{', match.Index);
                if (open < 0) break;
                int end = MatchingBrace(text, open);
                if (end < 0) break;
                result.Add(new BlockSpan { Start = match.Index, Open = open, End = end, Text = text.Substring(match.Index, end - match.Index + 1) });
                cursor = end + 1;
            }
            return result;
        }

        public static string BlockName(BlockSpan block)
        {
            if (block == null || String.IsNullOrWhiteSpace(block.Text)) return "";
            Match match = Regex.Match(block.Text, @"^\s*""?([A-Za-z0-9_.@:$-]+)""?\s*\{");
            return match.Success ? match.Groups[1].Value : "";
        }

        public static string ConfigureInstantPlayerRespawn(string text, bool ground, int airSpeedKmh)
        {
            return ConfigureInstantPlayerRespawn(text, ground, airSpeedKmh, null);
        }

        public static string ConfigureInstantPlayerRespawn(string text, bool ground, int airSpeedKmh, string customSpawnTransform, double respawnDelay = 0, bool airportTakeoff = false)
        {
            if (ground)
                text = RemoveAirfieldContent(text);
            BlockSpan mission = FirstBlock(text, "mission", 0);
            if (mission == null) throw new InvalidOperationException("Mission settings block is missing.");
            string missionBlock = mission.Text;
            string restoreTypeValue = airportTakeoff ? "manual" : "attempts";
            if (Regex.IsMatch(missionBlock, @"(?m)^\s*restoreType:t\s*="))
                missionBlock = new Regex(@"(?m)^(\s*)restoreType:t\s*=\s*""[^""]*""").Replace(missionBlock, "$1restoreType:t=\"" + restoreTypeValue + "\"", 1);
            else missionBlock = missionBlock.Insert(missionBlock.IndexOf('{') + 1, Environment.NewLine + "    restoreType:t=\"" + restoreTypeValue + "\"");
            text = ReplaceSpan(text, mission, missionBlock);

            BlockSpan triggers = FirstBlock(text, "triggers", 0);
            if (triggers == null) throw new InvalidOperationException("Mission triggers block is missing.");
            string spawn = ground ? "UTL_Player_Ground_Spawn" : "UTL_Player_Air_Spawn";
            string respawnTarget = spawn;
                        string respawnActions = airportTakeoff
                ? @"      wait{
        time:r=" + respawnDelay.ToString("0.###", CultureInfo.InvariantCulture) + @" }
      spawnOnAirfield{
        runwayName:t = ""airfield_start""
        objects:t = ""You""
      }"
                : @"      wait{
        time:r=" + respawnDelay.ToString("0.###", CultureInfo.InvariantCulture) + @" }
      unitRespawn{
        delay:r=0
        offset:p3=0, 0, 0
        object:t=""You""
        target:t=""" + respawnTarget + @"""
      }";
string trigger = @"
  ""UTL Player Respawn Compatible""{
    is_enabled:b=yes
    comments:t=""Minimal manual player respawn using documented Mission Editor fields""

    props{
      actionsType:t=""PERFORM_ONE_BY_ONE""
      conditionsType:t=""ALL""
      enableAfterComplete:b=yes
    }

    events{
      periodicEvent{
        time:r=0.25
      }
    }

    conditions{
      unitWhenStatus{
        object_type:t=""isKilled""
        check_objects:t=""any""
        object_marking:i=0
        object_var_name:t=""""
        object_var_comp_op:t=""equal""
        object_var_value:i=0
        target_type:t=""isAlive""
        check_period:r=0.25
        object:t=""You""
      }
    }

    actions{
" + respawnActions + @"
    }

    else_actions{}
  }
";
            text = text.Insert(triggers.End, trigger);
            BlockSpan areas = FirstBlock(text, "areas", 0);
            if (areas == null) throw new InvalidOperationException("Mission areas block is missing.");
            string positions = !String.IsNullOrWhiteSpace(customSpawnTransform)
                ? @"
  " + spawn + @"{
    type:t=""Sphere""
    tm:m=" + customSpawnTransform + @"
    objLayer:i=0

    props{}
  }
"
                : ground
                ? @"
  UTL_Player_Ground_Spawn{
    type:t=""Sphere""
    tm:m=[[-5, 0, 8.66025] [0, 10, 0] [-8.66025, 0, -5] [6.3526, 41.581, -622.332]]
    objLayer:i=0

    props{}
  }
"
                : airportTakeoff
                ? @"
  UTL_Player_Air_Spawn{
    type:t=""Sphere""
    tm:m=[[0, 0, -10] [0, 10, 0] [10, 0, 0] [551.7, 30, 575.1]]
    objLayer:i=0

    props{}
  }
"
                : @"
  UTL_Player_Air_Spawn{
    type:t=""Sphere""
    tm:m=[[0, 0, -10] [0, 10, 0] [10, 0, 0] [531.8, 1500, 577]]
    objLayer:i=0

    props{}
  }
";
            if (airportTakeoff)
                text = text.Replace("objects:t=\"UTL_AIRPORT_OBJECTS\"", "objects:t=\"You\"");
            else text = Regex.Replace(text, @"\s*spawnOnAirfield\s*\{[^}]*\}", "", RegexOptions.Singleline);
            return text.Insert(areas.End, positions);
        }

        public static string RemoveAirfieldContent(string text)
        {
            // Ground (tank) missions never use the airfield: strip airport triggers,
            // zones and the dynaf runway unit so tanks stay on their own spawn.
            text = RemoveNamedBlockAnywhere(text, "create_spawns");
            foreach (string zone in new[] { "airfield_area", "airfield_start", "airfield_end", "spawn01", "airfields_area", "airfield_spawnpoint_high" })
                text = RemoveNamedBlockAnywhere(text, zone);
            // NOTE: dynaf runway unit (airfield_target_01) is kept - it is invisible on
            // this map but harmless; removing it via RemoveObjectGroupByName could
            // swallow the adjacent rendInst runway (Airfield_Runway) block.
            return text;
        }

        private static string RemoveNamedBlockAnywhere(string text, string name)
        {
            Match m = Regex.Match(text, @"(?m)^\s*" + Regex.Escape(name) + @"\s*\{");
            if (!m.Success) return text;
            int open = m.Index + m.Length - 1;
            int end = MatchingBrace(text, open);
            if (end < 0) return text;
            int start = m.Index;
            while (start > 0 && (text[start - 1] == '\n' || text[start - 1] == '\r')) start--;
            return text.Remove(start, end - start + 1);
        }

        private static string RemoveObjectGroupByName(string text, string unitName)
        {
            string needle = "name:t=\"" + unitName + "\"";
            int idx = text.IndexOf(needle, StringComparison.Ordinal);
            if (idx < 0) return text;
            int open = text.LastIndexOf('{', idx);
            if (open < 0) return text;
            int headerStart = text.LastIndexOf('\n', open) + 1;
            int end = MatchingBrace(text, open);
            if (end < 0) return text;
            int start = headerStart;
            while (start > 0 && (text[start - 1] == '\n' || text[start - 1] == '\r')) start--;
            return text.Remove(start, end - start + 1);
        }
        public static string AccelerateRangeRecovery(string text)
        {
            return AccelerateRangeRecovery(text, true);
        }

        
        public static string AccelerateRangeRecovery(string text, bool includeRangeRecovery, double targetRespawnDelay = 0.25, double? rearmSeconds = 1.0)
        {
            string respawnDelayText = targetRespawnDelay.ToString("0.###", CultureInfo.InvariantCulture);
            string rearmText = rearmSeconds.HasValue ? rearmSeconds.Value.ToString("0.###", CultureInfo.InvariantCulture) : "";
            text = Regex.Replace(text, @"(wait\s*\{\s*time:r\s*=\s*)(?:5|10|15)(\s*\})", "${1}" + respawnDelayText + "${2}", RegexOptions.IgnoreCase);
            // The old template restored the whole player unit on a timer. Apart
            // from rearming, that also resets active seekers, helicopter optics
            // and targeting-pod state. Disable it and rearm only after the engine
            // reports that the player has no ammunition left.
            foreach (string triggerName in new[] { "\"Player Ammo Reload 10s\"", "\"Player Ammo Reload 1s\"" })
            {
                BlockSpan periodicReload = FirstBlock(text, triggerName, 0);
                if (periodicReload == null) continue;
                string disabled = Regex.Replace(periodicReload.Text, @"(?m)^(\s*is_enabled:b\s*=\s*)(?:yes|true)\s*$", "$1no", RegexOptions.IgnoreCase);
                text = ReplaceSpan(text, periodicReload, disabled);
            }
            BlockSpan triggers = FirstBlock(text, "triggers", 0);
            if (triggers == null) return text;
            string extras = "";
            if (rearmSeconds.HasValue) extras += @"
  ""UTL Fast Rearm Policy""{
    is_enabled:b=yes
    comments:t=""Set the engine's native rearm delay once without restoring or reinitializing the player unit""

    props{
      actionsType:t=""PERFORM_ONE_BY_ONE""
      conditionsType:t=""ALL""
      enableAfterComplete:b=no
    }

    events{
      initMission{}
    }

    conditions{}

    actions{
      unitSetProperties{
        object:t=""You""
        rearmTimeOnField:r=" + rearmText + @"
      }
    }

    else_actions{}
  }
";
            if (includeRangeRecovery) extras += @"

  ""UTL APS Carrier Recovery Compatible""{
    is_enabled:b=yes
    comments:t=""Restore the APS test carrier shortly after destruction""

    props{
      actionsType:t=""PERFORM_ONE_BY_ONE""
      conditionsType:t=""ALL""
      enableAfterComplete:b=yes
    }

    events{
      periodicEvent{
        time:r=0.25
      }
    }

    conditions{
      unitWhenStatus{
        object_type:t=""isKilled""
        check_objects:t=""any""
        object_marking:i=0
        object_var_name:t=""""
        object_var_comp_op:t=""equal""
        object_var_value:i=0
        target_type:t=""isAlive""
        check_period:r=0.25
        object:t=""Target_07""
      }
    }

    actions{
      wait{
        time:r=0.25
      }

      unitRestore{
        target_marking:i=0
        ressurectIfDead:b=yes
        fullRestore:b=yes
        target:t=""Target_07""
        ammoRestore:b=yes
      }
    }

    else_actions{}
  }
";
            return text.Insert(triggers.End, extras);
        }

    }
}
