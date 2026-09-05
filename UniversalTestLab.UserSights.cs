// UniversalTestLab.UserSights.cs
// War Thunder UserSights management (backup, bind, vehicle selection).
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
    internal static class UserSightStore
    {
        private const string GeneratedMarker = ".universal-test-lab-generated";

        public static List<UserSightEntry> Discover(string gameRoot)
        {
            List<UserSightEntry> result = new List<UserSightEntry>
            {
                new UserSightEntry { IsDefault = true, Name = "Game / current default sight", FilePath = "" }
            };
            HashSet<string> roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            AddSaveRoots(roots, Path.Combine(documents, "My Games", "WarThunder", "Saves"));
            AddSaveRoots(roots, Path.Combine(profile, "Documents", "My Games", "WarThunder", "Saves"));
            AddSaveRoots(roots, Path.Combine(profile, "OneDrive", "Documents", "My Games", "WarThunder", "Saves"));
            try
            {
                DirectoryInfo documentsParent = Directory.GetParent(documents);
                if (documentsParent != null) AddSaveRoots(roots, Path.Combine(documentsParent.FullName, "OneDrive", "Documents", "My Games", "WarThunder", "Saves"));
            }
            catch { }
            string oneDrive = Environment.GetEnvironmentVariable("OneDrive");
            if (!String.IsNullOrWhiteSpace(oneDrive)) AddSaveRoots(roots, Path.Combine(oneDrive, "Documents", "My Games", "WarThunder", "Saves"));
            if (!String.IsNullOrWhiteSpace(gameRoot))
            {
                try
                {
                    string legacy = Path.Combine(Path.GetFullPath(gameRoot.Trim().Trim('"')), "UserSights");
                    if (Directory.Exists(legacy)) roots.Add(legacy);
                }
                catch { }
            }

            foreach (string root in roots.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    foreach (string file in Directory.EnumerateFiles(root, "*.blk", SearchOption.AllDirectories))
                    {
                        string relative = file.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        string first = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).FirstOrDefault() ?? "";
                        if (first.Equals("tank_sight_presets", StringComparison.OrdinalIgnoreCase) ||
                            first.StartsWith("utl_run_", StringComparison.OrdinalIgnoreCase)) continue;
                        string name = Path.GetFileNameWithoutExtension(file);
                        if (String.IsNullOrWhiteSpace(name)) continue;
                        string folder = Path.GetDirectoryName(relative) ?? "UserSights";
                        result.Add(new UserSightEntry { FilePath = Path.GetFullPath(file), Name = name, Folder = folder });
                    }
                }
                catch { }
            }
            return result.GroupBy(x => x.IsDefault ? "<default>" : x.FilePath, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First()).OrderBy(x => x.IsDefault ? 0 : 1).ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x.Folder, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        private static void AddSaveRoots(HashSet<string> roots, string saves)
        {
            try
            {
                if (!Directory.Exists(saves)) return;
                foreach (string account in Directory.GetDirectories(saves))
                {
                    string root = Path.Combine(account, "production", "UserSights");
                    if (Directory.Exists(root)) roots.Add(Path.GetFullPath(root));
                }
            }
            catch { }
        }

        public static string InstallForGeneratedVehicle(string sourcePath, string classId, out string generatedFolder)
        {
            generatedFolder = null;
            if (String.IsNullOrWhiteSpace(sourcePath)) return null;
            string source = Path.GetFullPath(sourcePath);
            if (!File.Exists(source) || !Path.GetExtension(source).Equals(".blk", StringComparison.OrdinalIgnoreCase))
                throw new FileNotFoundException("The custom sight stored with this preset was not found.", sourcePath);
            if (!Regex.IsMatch(classId ?? "", @"^[A-Za-z0-9_.-]+$"))
                throw new InvalidOperationException("The generated ground-vehicle ID is not safe for a UserSights folder.");
            string sightName = Path.GetFileNameWithoutExtension(source);
            if (String.IsNullOrWhiteSpace(sightName) || sightName.IndexOfAny(new[] { '\r', '\n', '"' }) >= 0)
                throw new InvalidOperationException("The selected custom sight has an invalid filename: " + Path.GetFileName(source));

            DirectoryInfo folder = Directory.GetParent(source);
            DirectoryInfo userSights = null;
            while (folder != null)
            {
                if (folder.Name.Equals("UserSights", StringComparison.OrdinalIgnoreCase)) { userSights = folder; break; }
                folder = folder.Parent;
            }
            if (userSights == null) throw new InvalidOperationException("The selected .blk file is not inside a UserSights folder.");

            generatedFolder = Path.Combine(userSights.FullName, classId);
            Directory.CreateDirectory(generatedFolder);
            File.WriteAllText(Path.Combine(generatedFolder, GeneratedMarker), "Created by Universal Test Lab for a generated ground vehicle.", new UTF8Encoding(false));
            File.Copy(source, Path.Combine(generatedFolder, Path.GetFileName(source)), true);
            BindGeneratedVehicleSelection(Path.Combine(userSights.Parent.FullName, "global.blk"), classId, sightName);
            return sightName;
        }

        private static void BindGeneratedVehicleSelection(string globalPath, string classId, string sightName)
        {
            if (!File.Exists(globalPath))
                throw new FileNotFoundException("War Thunder's global.blk was not found beside the selected UserSights folder. Start the game once, then try again.", globalPath);
            string original = File.ReadAllText(globalPath, Encoding.UTF8);
            string updated = BindGeneratedVehicleSelectionText(original, classId, sightName);
            if (String.Equals(original, updated, StringComparison.Ordinal)) return;

            string backup = globalPath + ".universal-test-lab-backup";
            string temporary = globalPath + ".universal-test-lab-tmp";
            try
            {
                if (!File.Exists(backup)) File.Copy(globalPath, backup, false);
                File.WriteAllText(temporary, updated, new UTF8Encoding(false));
                File.Replace(temporary, globalPath, null);
            }
            catch (Exception ex)
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); } catch { }
                throw new IOException("The custom sight could not be attached to the generated vehicle. Close War Thunder, generate the mission once, then start the game again.", ex);
            }
        }

        internal static string BindGeneratedVehicleSelectionText(string text, string classId, string sightName)
        {
            if (String.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("War Thunder global.blk is empty.");
            if (!Regex.IsMatch(classId ?? "", @"^[A-Za-z0-9_.-]+$")) throw new InvalidOperationException("Invalid generated vehicle ID.");
            if (String.IsNullOrWhiteSpace(sightName) || sightName.IndexOfAny(new[] { '\r', '\n', '"' }) >= 0) throw new InvalidOperationException("Invalid custom sight name.");
            string newline = text.IndexOf("\r\n", StringComparison.Ordinal) >= 0 ? "\r\n" : "\n";
            BlockSpan settings = BlkTools.Blocks(text, "tankSightSettings").FirstOrDefault();
            if (settings == null)
            {
                BlockSpan profile = BlkTools.FirstBlock(text, "profile", 0);
                if (profile == null) throw new InvalidOperationException("The profile block was not found in War Thunder global.blk.");
                Match profileLine = Regex.Match(profile.Text, @"(?m)^(\s*)profile\s*\{");
                string settingsIndent = (profileLine.Success ? profileLine.Groups[1].Value : "  ") + "  ";
                string entryIndent = settingsIndent + "  ";
                string fieldIndent = entryIndent + "  ";
                string block = newline + settingsIndent + "tankSightSettings{" + newline +
                    entryIndent + classId + "{" + newline + fieldIndent + "crosshair:t=\"" + sightName + "\"" + newline +
                    entryIndent + "}" + newline + settingsIndent + "}" + newline;
                string patchedProfile = profile.Text.Insert(profile.Text.LastIndexOf('}'), block);
                return BlkTools.ReplaceSpan(text, profile, patchedProfile);
            }

            string settingsText = settings.Text;
            foreach (Match generated in Regex.Matches(settingsText, @"(?m)^\s*utl_run_[A-Za-z0-9_.-]+_ground\s*\{").Cast<Match>().OrderByDescending(x => x.Index))
            {
                int open = settingsText.IndexOf('{', generated.Index);
                int close = BlkTools.MatchingBrace(settingsText, open);
                if (open < 0 || close < 0) continue;
                int removeEnd = close + 1;
                while (removeEnd < settingsText.Length && (settingsText[removeEnd] == '\r' || settingsText[removeEnd] == '\n')) removeEnd++;
                settingsText = settingsText.Remove(generated.Index, removeEnd - generated.Index);
            }

            Match settingsLine = Regex.Match(settingsText, @"(?m)^(\s*)tankSightSettings\s*\{");
            string settingsBaseIndent = settingsLine.Success ? settingsLine.Groups[1].Value : "    ";
            string generatedIndent = settingsBaseIndent + "  ";
            string generatedFieldIndent = generatedIndent + "  ";
            string generatedBlock = newline + generatedIndent + classId + "{" + newline +
                generatedFieldIndent + "crosshair:t=\"" + sightName + "\"" + newline + generatedIndent + "}" + newline;
            settingsText = settingsText.Insert(settingsText.LastIndexOf('}'), generatedBlock);
            return BlkTools.ReplaceSpan(text, settings, settingsText);
        }

        public static void CleanupGeneratedFolders(string currentFolder)
        {
            if (String.IsNullOrWhiteSpace(currentFolder)) return;
            string root = Path.GetDirectoryName(Path.GetFullPath(currentFolder));
            if (String.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return;
            foreach (string folder in Directory.GetDirectories(root, "utl_run_*_ground", SearchOption.TopDirectoryOnly))
            {
                if (Path.GetFullPath(folder).Equals(Path.GetFullPath(currentFolder), StringComparison.OrdinalIgnoreCase)) continue;
                if (!File.Exists(Path.Combine(folder, GeneratedMarker))) continue;
                try { Directory.Delete(folder, true); }
                catch { }
            }
        }
    }
}
