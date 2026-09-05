// UniversalTestLab.MainForm.Actions.cs
// Generate/save/apply actions, presets and cleanup (segment 5/5).
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
        private void ApplyClicked()
        {
            try
            {
                Aircraft selected = SelectedAircraft;
                if (selected == null) throw new InvalidOperationException("Select an aircraft, helicopter, drone or ground vehicle.");
                if (!ConfirmRiskyLoadout()) return;
                string root = ValidGameRoot();
                InstallBase(root, false);
                string token = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture) + "_" + Process.GetCurrentProcess().Id;
                bool groundPlayer = IsGroundVehicle(selected);
                bool helicopterPlayer = !groundPlayer && IsHelicopter(selected, null);
                CombinedMap combinedMap = null;
                CombinedSpawn combinedSpawn = null;
                if (workspaceCombinedScenario != null && workspaceCombinedScenario.Enabled)
                {
                    combinedMap = combinedMaps.FirstOrDefault(x => x.Id.Equals(workspaceCombinedScenario.MapId ?? "", StringComparison.OrdinalIgnoreCase));
                    if (combinedMap == null) throw new InvalidOperationException("Select a valid combined-battles map.");
                    string playerKind = groundPlayer ? "ground" : helicopterPlayer ? "helicopter" : "aircraft";
                    int selectedSide = workspaceCombinedScenario.Side == 2 ? 2 : 1;
                    combinedSpawn = combinedMap.Spawns.FirstOrDefault(x => x.Side == selectedSide && x.Kind.Equals(playerKind, StringComparison.OrdinalIgnoreCase) && x.Option.Equals(workspaceCombinedScenario.SpawnOption ?? "", StringComparison.OrdinalIgnoreCase));
                    if (combinedSpawn == null)
                        combinedSpawn = combinedMap.Spawns.FirstOrDefault(x => x.Side == selectedSide && x.Kind.Equals(playerKind, StringComparison.OrdinalIgnoreCase));
                    if (combinedSpawn == null) throw new InvalidOperationException("This map has no compatible spawn for the selected vehicle and side.");
                }
                GeneratedAircraft generated = groundPlayer ? BuildCustomGroundVehicle(root, selected, token) : BuildCustomAircraft(root, selected, token);
                WriteMissionLocalization(root, generated, selected);
                Aircraft air = ResolveAircraft(airTargetBox);
                TargetUnit ground = ResolveTarget(groundTargetBox, groundTargets);
                TargetUnit ship = ResolveTarget(shipTargetBox, shipTargets);
                if (air == null || ground == null || ship == null) throw new InvalidOperationException("Check all target selections.");
                AircraftSettings settings = GetAircraftSettings(selected);
                string text = Embedded.Text("UTL.universal_test_lab.blk");
                text = BlkTools.DisablePlayerSwitch(text);
                text = BlkTools.RemoveBotNotifications(text);
                if (groundPlayer)
                    text = BlkTools.ConfigureGroundPlayer(text, generated.ClassId, generated.ModelId, generated.PresetId, settings, generated.GroundAmmoLoadouts);
                else
                {
                    text = BlkTools.UpdateUnit(text, "You", generated.ClassId, generated.PresetId, 1);
                    int playerSpawnSpeed = combinedSpawn != null && !combinedSpawn.Option.Equals("air", StringComparison.OrdinalIgnoreCase) ? 0 : generated.SpawnSpeedKmh;
                    text = ApplyPlayerSpawnSpeed(text, playerSpawnSpeed);
                    text = ApplyPlayerFuel(text, settings);
                    text = ApplyPlayerGunBelts(text, settings);
                }
                // A selected helicopter store can depend on several hidden weapon
                // research nodes. A partial set leaves the pylons visible while the
                // in-flight selector contains only Turret, so helicopter test models
                // always receive the complete native weapon-controller set. Ground
                // vehicles, however, must receive their requested research state too;
                // skipping this call forced every player tank to a stock configuration.
                text = BlkTools.ConfigureUnitModifications(text, "You", helicopterPlayer || settings.UseAllModifications, helicopterPlayer ? Enumerable.Empty<string>() : settings.EnabledModifications);
                if (combinedMap != null && combinedSpawn != null)
                {
                    text = BlkTools.ConfigureCombinedScenario(text, combinedMap, combinedSpawn);
                }
                else
                {
                    if (workspaceFlyingTargets != null && workspaceFlyingTargets.Count > 0)
                    {
                        // The Map window exposes every flying hostile the template carries:
                        // Target_Air_01 (Typhoon), Target_Air_02, Heli_Target (Mi-28NM),
                        // Heli_Target_02 (Ka-52). Replace each configured armada; slots left
                        // with no selection keep their template vehicle.
                        foreach (FlyingTargetSlot flying in workspaceFlyingTargets)
                        {
                            Aircraft flyingTarget = aircraft.FirstOrDefault(x => x.Id != null && x.Id.Equals(flying.AircraftId, StringComparison.OrdinalIgnoreCase));
                            if (flyingTarget == null) continue;
                            text = BlkTools.UpdateUnit(text, flying.UnitName, flyingTarget.Id, flyingTarget.DefaultPreset, Math.Max(0, Math.Min(20, flying.Count)));
                        }
                    }
                    else
                    {
                        text = BlkTools.UpdateUnit(text, "Target_Air_02", air.Id, air.DefaultPreset, (int)airCount.Value);
                    }
                    if (workspaceGroundTargetOverrides != null && workspaceGroundTargetOverrides.Count > 0)
                    {
                        for (int index = 0; index < Math.Min(7, workspaceGroundTargetOverrides.Count); index++)
                        {
                            TargetUnit configured = groundTargets.FirstOrDefault(x => x.Id.Equals(workspaceGroundTargetOverrides[index], StringComparison.OrdinalIgnoreCase));
                            if (configured == null) continue;
                            string unitName = "Target_" + (index + 1).ToString("00", CultureInfo.InvariantCulture);
                            text = BlkTools.UpdateUnit(text, unitName, configured.Id, configured.DefaultPreset, 1);
                            if (hostileGround.Checked) text = BlkTools.MakeGroundTargetHostile(text, unitName);
                        }
                    }
                    else
                    {
                        text = BlkTools.UpdateUnit(text, "Target_03", ground.Id, ground.DefaultPreset, (int)groundCount.Value);
                        if (hostileGround.Checked) text = BlkTools.MakeGroundTargetHostile(text, "Target_03");
                    }
                    text = BlkTools.UpdateUnit(text, "Ship_Target", ship.Id, ship.DefaultPreset, (int)shipCount.Value);
                    if (workspacePassiveShip) text = BlkTools.MakeShipPassive(text, "Ship_Target");
                    string samMode = samSites != null && !samSites.Checked ? "disabled" : pendingSamMode;
                    text = BlkTools.SetSamSites(text, samMode, pendingSamSelection);
                }
                if (MissionSettings.Current.LimitedAmmo)
                    text = Regex.Replace(text, @"(?m)^(\s*isLimitedAmmo:b\s*=\s*)(?:true|false)\s*$", "$1true", RegexOptions.IgnoreCase);
                text = BlkTools.AccelerateRangeRecovery(text, combinedMap == null, MissionSettings.Current.TargetRespawnDelaySeconds, MissionSettings.Current.RearmOverride ? (double?)MissionSettings.Current.RearmSeconds : null);
                text = BlkTools.ConfigureInstantPlayerRespawn(text, groundPlayer, generated.SpawnSpeedKmh,
                    combinedSpawn == null ? null : BlkTools.CombinedRespawnTransform(combinedSpawn), MissionSettings.Current.PlayerRespawnDelaySeconds,
                    MissionSettings.Current.SpawnMode != null && MissionSettings.Current.SpawnMode.Equals("airport", StringComparison.OrdinalIgnoreCase));
                bool nuclear = assignments.Values.Any(a => a.Weapon.Category == "Nuclear Weapons");
                if (IsFpvDrone(selected)) text = BlkTools.AddFpvDetonationTriggers(text);
                string title = combinedMap != null
                    ? "HOT UTL - " + selected.Display + " - " + combinedMap.Display
                    : groundPlayer
                    ? "HOT UTL - " + selected.Display + " - Ground Test"
                    : IsFpvDrone(selected)
                    ? "HOT UTL - FPV Strike Drone"
                    : "HOT UTL - " + selected.Display + " - Custom " + assignments.Count + " stations";
                if (title.Length > 150) title = title.Substring(0, 150);
                string description = combinedMap != null
                    ? "Solo combined-battles sandbox on the " + combinedMap.Display + " Domination layout. Side " + combinedSpawn.Side.ToString(CultureInfo.InvariantCulture) + ", " + combinedSpawn.Label + ". No AI units."
                    : groundPlayer
                    ? "Custom ground vehicle, ammunition, modules and mobility test."
                    : IsFpvDrone(selected)
                    ? "Player-controlled FPV strike drone with local impact detonation."
                    : (nuclear ? "Custom hot-load air vehicle with native nuclear weapons." : "Custom hot-load air vehicle and pylon setup.");
                description += " Close and reopen the User Missions tab after applying.";
                text = BlkTools.UpdateMissionLabels(text, title, description);
                text = BlkTools.ConfigureRapidFire(text, MissionSettings.Current.RapidFireEnabled, MissionSettings.Current.RapidFireInterval, MissionSettings.Current.RapidFireFullRestore);
                string missionDir = Path.Combine(root, MissionFolderRelative);
                Directory.CreateDirectory(missionDir);
                string missionPath = Path.Combine(missionDir, HotMissionName);
                WriteBytes(missionPath, new UTF8Encoding(false).GetBytes(text));
                if (!File.Exists(missionPath) || new FileInfo(missionPath).Length == 0)
                    throw new IOException("The generated mission could not be verified on disk: " + missionPath);
                CleanupPreviousGeneratedFiles(root, missionPath, generated);
                string refreshInstructions = groundPlayer
                    ? "Ground mission generated successfully.\r\n\r\nWar Thunder caches the playable reserve-tank proxy. After changing the player tank:\r\n1. Exit War Thunder completely.\r\n2. Start War Thunder again.\r\n3. Open User Missions and launch the current HOT UTL mission.\r\n4. If a custom ground sight is attached, press Alt + F9 once in the mission."
                    : "Mission generated successfully.\r\n\r\nIn War Thunder:\r\n1. Close the User Missions tab.\r\n2. Open User Missions again to refresh the mission list.\r\n3. Launch the current HOT UTL mission.";
                SetStatus(groundPlayer ? "Ground mission generated. Restart War Thunder once to reload the tank proxy." : "Mission generated. Close and reopen the User Missions tab in War Thunder to refresh it.", false);
                lastGenerationSucceeded = true;
                if (!suppressSuccessDialog) MessageBox.Show(this, refreshInstructions, "Mission generated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lastGenerationSucceeded = false;
                if (workspaceOperation) workspaceLastError = ex;
                else ShowError(ex);
            }
        }

        private Aircraft ResolveAircraft(ComboBox combo)
        {
            Aircraft selected = combo.SelectedItem as Aircraft;
            if (selected != null) return selected;
            string value = combo.Text.Trim();
            return aircraft.FirstOrDefault(a => a.Id.Equals(value, StringComparison.OrdinalIgnoreCase) || a.Display.Equals(value, StringComparison.CurrentCultureIgnoreCase));
        }

        private static TargetUnit ResolveTarget(ComboBox combo, IEnumerable<TargetUnit> source)
        {
            TargetUnit selected = combo.SelectedItem as TargetUnit;
            if (selected != null) return selected;
            string value = combo.Text.Trim();
            return source.FirstOrDefault(t => t.Id.Equals(value, StringComparison.OrdinalIgnoreCase) || t.Display.Equals(value, StringComparison.CurrentCultureIgnoreCase));
        }

        private static void CleanupPreviousGeneratedFiles(string root, string currentMission, GeneratedAircraft current)
        {
            string missionDir = Path.Combine(root, MissionFolderRelative);
            foreach (string file in Directory.GetFiles(missionDir, "universal_test_lab_*.blk"))
            {
                if (!Path.GetFullPath(file).Equals(Path.GetFullPath(currentMission), StringComparison.OrdinalIgnoreCase)) try { File.Delete(file); } catch { }
            }
            foreach (string fmDir in new[]
            {
                Path.Combine(root, @"content\pkg_user\gameData\flightModels"),
                Path.Combine(root, @"content\pkg_local\gameData\flightModels")
            })
            {
                if (Directory.Exists(fmDir)) foreach (string file in Directory.GetFiles(fmDir, "utl_run_*_player.blk"))
                {
                    if (!Path.GetFullPath(file).Equals(Path.GetFullPath(current.FlightModelPath), StringComparison.OrdinalIgnoreCase)) try { File.Delete(file); } catch { }
                }
                string presetDir = Path.Combine(fmDir, "weaponPresets");
                if (Directory.Exists(presetDir))
                {
                    foreach (string file in Directory.GetFiles(presetDir, "utl_run_*_loadout.blk"))
                    {
                        // Preset-style aircraft (legacy planes without WeaponSlot
                        // trees) reference a native loadout by name and publish no
                        // utl_run_*_loadout file, so PresetPath is null. Skip the
                        // comparison and clean up any stale generated loadouts.
                        if (String.IsNullOrEmpty(current.PresetPath) || !Path.GetFullPath(file).Equals(Path.GetFullPath(current.PresetPath), StringComparison.OrdinalIgnoreCase)) try { File.Delete(file); } catch { }
                    }
                }
            }
            string countermeasureDir = Path.Combine(root, @"content\pkg_user\gameData\Weapons\rocketGuns\utl_cm");
            if (Directory.Exists(countermeasureDir))
            {
                HashSet<string> keep = new HashSet<string>(current.AuxiliaryPaths.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
                foreach (string file in Directory.GetFiles(countermeasureDir, "utl_cm_*.blk"))
                    if (!keep.Contains(Path.GetFullPath(file))) try { File.Delete(file); } catch { }
            }
            string helicopterGunDir = Path.Combine(root, @"content\pkg_user\gameData\Weapons\utl_guns");
            if (Directory.Exists(helicopterGunDir))
            {
                HashSet<string> keep = new HashSet<string>(current.AuxiliaryPaths.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
                foreach (string file in Directory.GetFiles(helicopterGunDir, "utl_gun_*.blk"))
                    if (!keep.Contains(Path.GetFullPath(file))) try { File.Delete(file); } catch { }
            }
            string legacyTankDir = Path.Combine(root, @"content\pkg_user\gameData\units\tankModels");
            if (Directory.Exists(legacyTankDir))
            {
                foreach (string file in Directory.GetFiles(legacyTankDir, "utl_run_*_ground.blk"))
                    if (!Path.GetFullPath(file).Equals(Path.GetFullPath(current.FlightModelPath), StringComparison.OrdinalIgnoreCase)) try { File.Delete(file); } catch { }
            }
            string proxyTankDir = Path.Combine(root, @"content\pkg_local\gameData\units\tankModels\userVehicles");
            if (Directory.Exists(proxyTankDir))
            {
                foreach (string proxyTankPath in Directory.GetFiles(proxyTankDir, "*.blk", SearchOption.AllDirectories))
                {
                    if (Path.GetFullPath(proxyTankPath).Equals(Path.GetFullPath(current.FlightModelPath), StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        string parentName = new DirectoryInfo(Path.GetDirectoryName(proxyTankPath)).Name;
                        string proxyText = File.ReadAllText(proxyTankPath, Encoding.UTF8);
                        if (parentName.StartsWith("utl_run_", StringComparison.OrdinalIgnoreCase) ||
                            proxyText.IndexOf("gameData/Weapons/groundModels_weapons/utl_ground/", StringComparison.OrdinalIgnoreCase) >= 0)
                            File.Delete(proxyTankPath);
                    }
                    catch { }
                }
                foreach (string oldDirectory in Directory.GetDirectories(proxyTankDir, "utl_run_*", SearchOption.TopDirectoryOnly))
                {
                    try { if (!Directory.EnumerateFileSystemEntries(oldDirectory).Any()) Directory.Delete(oldDirectory, false); }
                    catch { }
                }
            }
            foreach (string groundWeaponDir in new[]
            {
                Path.Combine(root, @"content\pkg_user\gameData\Weapons\groundModels_weapons\utl_ground"),
                Path.Combine(root, @"content\pkg_local\gameData\Weapons\groundModels_weapons\utl_ground")
            })
            {
                if (Directory.Exists(groundWeaponDir))
                {
                    HashSet<string> keep = new HashSet<string>(current.AuxiliaryPaths.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
                    foreach (string file in Directory.GetFiles(groundWeaponDir, "utl_*_cannon.blk"))
                        if (!keep.Contains(Path.GetFullPath(file))) try { File.Delete(file); } catch { }
                }
            }
            UserSightStore.CleanupGeneratedFolders(current.UserSightFolder);
        }

        private void OpenMissionFolder()
        {
            try
            {
                string path = Path.Combine(ValidGameRoot(), MissionFolderRelative);
                Directory.CreateDirectory(path);
                Process.Start("explorer.exe", "\"" + path + "\"");
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void ShowPresets()
        {
            using (PresetManagerForm dialog = new PresetManagerForm(this)) dialog.ShowDialog(this);
        }

        private void ShowAbout()
        {
            using (AboutForm dialog = new AboutForm(aircraft.Count, globalWeapons.Count)) dialog.ShowDialog(this);
        }

        internal SavedPreset CaptureCurrentPreset(string name)
        {
            Aircraft selected = SelectedAircraft;
            if (selected == null) throw new InvalidOperationException("Select an aircraft or helicopter before saving a preset.");
            SavedPreset preset = new SavedPreset { Name = name.Trim(), AircraftId = selected.Id, Settings = GetAircraftSettings(selected).Copy() };
            foreach (PylonAssignment assignment in assignments.Values.OrderBy(a => a.Pylon.Order))
            {
                DonorWeapon w = assignment.Weapon;
                preset.Entries.Add(new SavedPresetEntry
                {
                    Slot = assignment.Pylon.Slot, Injected = assignment.Injected, Mount = w.Mount, Trigger = w.Trigger, Blk = w.Blk,
                    Emitter = w.Emitter, Bullets = w.Bullets, Icon = w.Icon, Name = w.Name, Category = w.Category,
                    UnitMass = w.UnitMass, TotalMass = w.TotalMass
                });
            }
            return preset;
        }

        internal string CurrentAircraftName
        {
            get { return SelectedAircraft == null ? "Custom Loadout" : SelectedAircraft.Display + " Custom"; }
        }

        internal string AircraftName(string id)
        {
            Aircraft item = aircraft.FirstOrDefault(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            return item == null ? id : item.Display;
        }

        internal void LoadSavedPreset(SavedPreset preset)
        {
            Aircraft target = aircraft.FirstOrDefault(a => a.Id.Equals(preset.AircraftId, StringComparison.OrdinalIgnoreCase));
            if (target == null) throw new InvalidOperationException("The preset air vehicle is not present in the current catalog: " + preset.AircraftId);
            aircraftSearch.Text = "";
            nationFilter.SelectedIndex = 0;
            rankFilter.SelectedIndex = 0;
            vehicleFilter.SelectedIndex = 0;
            FilterAircraft();
            aircraftList.SelectedItem = aircraftList.Items.Cast<object>().OfType<Aircraft>().FirstOrDefault(a => a.Id == target.Id);
            assignments.Clear();
            if (preset.Settings != null) aircraftSettings[target.Id] = preset.Settings.Copy();
            int skipped = 0;
            foreach (SavedPresetEntry entry in preset.Entries)
            {
                PylonSlot pylon = pylons.FirstOrDefault(p => p.AircraftId == target.Id && p.Slot == entry.Slot);
                if (pylon == null) { skipped++; continue; }
                DonorWeapon weapon = entry.Injected
                    ? globalWeapons.FirstOrDefault(w => String.Equals(w.Blk, entry.Blk, StringComparison.OrdinalIgnoreCase) && String.Equals(w.Trigger, entry.Trigger, StringComparison.OrdinalIgnoreCase) && w.Bullets == entry.Bullets)
                    : nativeWeapons.FirstOrDefault(w => w.AircraftId == target.Id && w.Slot == entry.Slot && String.Equals(w.Mount, entry.Mount, StringComparison.OrdinalIgnoreCase) && String.Equals(w.Blk, entry.Blk, StringComparison.OrdinalIgnoreCase));
                if (weapon == null)
                {
                    weapon = new DonorWeapon
                    {
                        Mount = entry.Mount, Trigger = entry.Trigger, Blk = entry.Blk, Emitter = entry.Emitter, Bullets = entry.Bullets,
                        Icon = entry.Icon, Name = entry.Name, Category = entry.Category, UnitMass = entry.UnitMass, TotalMass = entry.TotalMass,
                        AircraftId = target.Id, AircraftDisplay = target.Display, Slot = entry.Slot, Nations = target.Nation
                    };
                }
                assignments[entry.Slot] = new PylonAssignment { Pylon = pylon, Weapon = weapon, Injected = entry.Injected };
            }
            BuildPylonStrip();
            RefreshPylons();
            UpdateAircraftSettingsButton();
            SetStatus("Loaded preset: " + preset.Name + (skipped > 0 ? " (skipped " + skipped + " missing stations)" : ""), false);
        }

        private void SetStatus(string message, bool error)
        {
            status.Text = (error ? "●  ERROR — " : "●  ") + message;
            status.ForeColor = error ? Theme.Danger : Theme.Good;
        }

        private void ShowError(Exception ex)
        {
            SetStatus("Error: " + ex.Message, true);
            MessageBox.Show(this, ex.Message, "Universal Test Lab", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

}
