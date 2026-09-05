// UniversalTestLab.Program.cs
// Entry point, command-line self-tests and screenshot renderers.
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
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            // Diagnostic crash log (next to the exe and under %LOCALAPPDATA%\UniversalTestLab)
            // so players can share an exact stack trace. Built with /debug+ so line numbers appear.
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                try
                {
                    string detail = e.ExceptionObject == null ? "(null exception)" : e.ExceptionObject.ToString();
                    try
                    {
                        detail += Environment.NewLine + "OS=" + Environment.OSVersion
                            + " | culture=" + System.Threading.Thread.CurrentThread.CurrentUICulture.Name
                            + " | lang=" + ConfigStore.GetString("language");
                    }
                    catch { }
                    string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    string[] targets =
                    {
                        Path.Combine(exeDir, "selftest_crash.log"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniversalTestLab", "crash.log")
                    };
                    foreach (string log in targets)
                    {
                        try { File.WriteAllText(log, detail); } catch { }
                    }
                }
                catch { }
            };

            if (args != null)
            {
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == "--config-dir")
                    {
                        try { ConfigStore.Root = Path.GetFullPath(args[i + 1]); }
                        catch { }
                        break;
                    }
                }
            }
            if (args != null && args.Contains("--selftest-config"))
            {
                string dir = "";
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == "--selftest-config") { dir = args[i + 1]; break; }
                }
                if (String.IsNullOrWhiteSpace(dir))
                {
                    for (int i = 0; i < args.Length - 1; i++)
                    {
                        if (args[i] == "--config-dir") { dir = args[i + 1]; break; }
                    }
                }
                try { ConfigStore.Root = Path.GetFullPath(dir); }
                catch { }
                Console.WriteLine("CONFIG-DIAG root=" + ConfigStore.Root + " exists=" + File.Exists(Path.Combine(ConfigStore.Root, "config.json")) + " args=" + String.Join("|", args));
                try
                {
                    Dictionary<string, object> data = ConfigStore.Data;
                    string configPath = Path.Combine(ConfigStore.Root, "config.json");
                    Console.WriteLine("CONFIG-DIAG loaded configPath=" + configPath + " file=" + File.Exists(configPath) + " dataKeys=" + String.Join(",", data.Keys));
                    int aircraft = 0;
                    Dictionary<string, object> aso = ConfigStore.GetObject("aircraft_settings");
                    if (aso != null) aircraft = aso.Count;
                    int era = 0;
                    List<object> eraList = ConfigStore.GetList("era_presets");
                    if (eraList != null) era = eraList.Count;
                    int mission = 0;
                    Dictionary<string, object> mo = ConfigStore.GetObject("mission_options");
                    if (mo != null) mission = mo.Count;
                    int session = 0;
                    Dictionary<string, object> so = ConfigStore.GetObject("session");
                    if (so != null) session = so.Count;
                    string folder = ConfigStore.GetString("game_folder");
                    if (!File.Exists(configPath) || aircraft < 1 || era < 1 || mission < 1 || session < 1 || String.IsNullOrWhiteSpace(folder))
                        throw new InvalidOperationException("Config migration self-test failed.");
                    Console.WriteLine("CONFIG SELFTEST OK aircraft=" + aircraft + " era=" + era + " mission=" + mission + " session=" + session + " game=" + (folder.Length > 30 ? folder.Substring(0, 30) + "..." : folder));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("CONFIG SELFTEST ERROR: " + ex.Message);
                }
                return;
            }
            if (args != null && args.Contains("--selftest-session"))
            {
                string dir = "";
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == "--selftest-session") { dir = args[i + 1]; break; }
                }
                if (String.IsNullOrWhiteSpace(dir))
                {
                    for (int i = 0; i < args.Length - 1; i++)
                    {
                        if (args[i] == "--config-dir") { dir = args[i + 1]; break; }
                    }
                }
                try { ConfigStore.Root = Path.GetFullPath(dir); }
                catch { }
                try
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Dictionary<string, object> obj = ConfigStore.GetObject("session");
                    string expected = obj == null || !obj.ContainsKey("vehicle_id") ? "" : Convert.ToString(obj["vehicle_id"], CultureInfo.InvariantCulture);
                    System.Windows.Application app = new System.Windows.Application();
                    ModernMainWindow window = new ModernMainWindow();
                    window.Show();
                    window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                    string actual = window.SessionSelectedVehicleId;
                    bool pass = expected.Length > 0 && actual != null && actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
                    Console.WriteLine("SESSION SELFTEST expected=" + expected + " actual=" + (actual ?? "(null)") + " => " + (pass ? "PASS" : "FAIL"));
                    window.Close();
                    app.Shutdown();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SESSION SELFTEST ERROR: " + ex.Message);
                }
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-flight-configure")
            {
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); ModernUi.RenderFlightConfigure(args[1]); return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-flight-configure-bottom")
            {
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); ModernUi.RenderFlightConfigureBottom(args[1]); return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-map")
            {
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); ModernUi.RenderMap(args[1]); return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-generated")
            {
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); ModernUi.RenderGenerated(args[1]); return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-weapon-scrollbar")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ModernUi.RenderWeaponScrollbar(args[1]);
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-helicopter")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ModernUi.RenderMainKind(args[1], "Helicopter");
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-drone")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ModernUi.RenderMainKind(args[1], "Drone");
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-experimental")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ModernUi.RenderExperimental(args[1]);
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-targets")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ModernUi.RenderTargets(args[1]);
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-garage")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ModernUi.RenderGarage(args[1]);
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-options")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ModernUi.RenderOptions(args[1]);
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-ground")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ModernUi.RenderMainKind(args[1], "Ground Vehicle");
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-ground-preset")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ModernUi.RenderGroundPreset(args[1]);
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-message-info")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ModernUi.RenderMessage(args[1], false);
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-message-error")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ModernUi.RenderMessage(args[1], true);
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-about")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ModernUi.RenderAbout(args[1]);
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-settings")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ModernUi.RenderSettings(args[1]);
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-ground-configure")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ModernUi.RenderGroundConfigure(args[1]);
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot-maximized")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ModernUi.RenderMainMaximized(args[1]);
                return;
            }
            if (args != null && args.Length >= 2 && args[0] == "--screenshot")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ModernUi.RenderMain(args[1]);
                return;
            }
            if (args != null && args.Any(a => a == "--uiselftest"))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ModernUi.SelfTest();
                return;
            }
                if (args != null && args.Any(a => a == "--selftest-ground-cache"))
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    try
                    {
                        MainForm cacheForm = new MainForm();
                        string gameRoot = cacheForm.WorkspaceGameFolder;
                        if (String.IsNullOrWhiteSpace(gameRoot) || !Directory.Exists(gameRoot))
                        {
                            Console.WriteLine("GROUND-CACHE SKIP: no valid game folder ({0})", gameRoot ?? "");
                            return;
                        }
                        Aircraft cacheSample = new Aircraft { Id = "sw_t_72m1", Display = "T-72M1 (self-test)", Kind = "Ground Vehicle", Nation = "USSR", Rank = 6 };
                        System.Diagnostics.Stopwatch cacheTimer = System.Diagnostics.Stopwatch.StartNew();
                        GroundWeaponCacheData cacheFirst = cacheForm.WorkspaceGetGroundWeaponCache(cacheSample);
                        cacheTimer.Stop();
                        long cacheFirstMs = cacheTimer.ElapsedMilliseconds;
                        cacheTimer.Restart();
                        GroundWeaponCacheData cacheSecond = cacheForm.WorkspaceGetGroundWeaponCache(cacheSample);
                        cacheTimer.Stop();
                        long cacheSecondMs = cacheTimer.ElapsedMilliseconds;
                        bool cacheHit = Object.ReferenceEquals(cacheFirst, cacheSecond);
                        int cacheWeapons = cacheFirst == null || cacheFirst.Weapons == null ? 0 : cacheFirst.Weapons.Count;
                        int cacheMissiles = cacheFirst == null || cacheFirst.Missiles == null ? 0 : cacheFirst.Missiles.Count;
                        int cacheBelts = cacheFirst == null || cacheFirst.BeltOptions == null ? 0 : cacheFirst.BeltOptions.Count;
                        bool prebuiltSource = MainForm.prebuiltGroundWeapons != null && MainForm.prebuiltGroundWeapons.ContainsKey("sw_t_72m1");
                        Console.WriteLine("GROUND-CACHE first={0}ms second={1}ms cache-hit={2} source={3} weapons={4} missiles={5} belt-options={6}", cacheFirstMs, cacheSecondMs, cacheHit ? "yes" : "no", prebuiltSource ? "prebuilt" : "live", cacheWeapons, cacheMissiles, cacheBelts);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("GROUND-CACHE ERROR: {0}: {1}", ex.GetType().Name, ex.Message);
                    }
                    return;
                }
                if (args != null && args.Any(a => a == "--selftest"))
                {
                string normalizedWeaponPath = MainForm.NormalizeGameResourcePath(@"gameData\Weapons\groundModels_weapons\120mm_L30A1_2e_user_cannon.blk");
                if (normalizedWeaponPath != "gamedata/weapons/groundmodels_weapons/120mm_l30a1_2e_user_cannon.blk")
                    throw new InvalidOperationException("VROM resource-path normalization self-test failed.");
                if (MainForm.HotMissionName != "universal_test_lab_hot.blk")
                    throw new InvalidOperationException("Stable hot-mission path self-test failed.");
                string text = Embedded.Text("UTL.universal_test_lab.blk");
                text = BlkTools.DisablePlayerSwitch(text);
                text = BlkTools.RemoveBotNotifications(text);
                text = BlkTools.UpdateUnit(text, "You", "utl_run_selftest_player", "utl_run_selftest_loadout", 1);
                BlockSpan directAirPlayer = BlkTools.UnitBlockByName(text, "You");
                BlockSpan disabledAirSwitch = BlkTools.FirstBlock(text, "\"Universal aircraft switch\"", 0);
                string fpvMission = BlkTools.AddFpvDetonationTriggers(text);
                string hostileMission = BlkTools.MakeGroundTargetHostile(text, "Target_03");
                if (text.Count(c => c == '{') != text.Count(c => c == '}') ||
                    text.IndexOf("doNuclearExplosion", StringComparison.Ordinal) >= 0 ||
                    text.IndexOf("ID_FIRE_SECONDARY", StringComparison.Ordinal) >= 0 ||
                    text.IndexOf("campaign:t=\"UTL\"", StringComparison.Ordinal) < 0 ||
                    text.IndexOf("campaign:t=\"UserMissions\"", StringComparison.Ordinal) >= 0 ||
                    text.IndexOf("campaign:t=\"UniversalTestLab\"", StringComparison.Ordinal) >= 0 ||
                    text.IndexOf("chapter:t=\"TestDrive\"", StringComparison.Ordinal) >= 0 ||
                    directAirPlayer.Text.IndexOf("unit_class:t=\"utl_run_selftest_player\"", StringComparison.Ordinal) < 0 ||
                    directAirPlayer.Text.IndexOf("weapons:t=\"utl_run_selftest_loadout\"", StringComparison.Ordinal) < 0 ||
                    directAirPlayer.Text.IndexOf("unit_class:t=\"utl_safe_player\"", StringComparison.Ordinal) >= 0 ||
                    disabledAirSwitch == null || disabledAirSwitch.Text.IndexOf("is_enabled:b=no", StringComparison.Ordinal) < 0 ||
                    text.IndexOf("Player Respawn Flight Profile", StringComparison.Ordinal) < 0 ||
                    // Bot respawn/rearm notices are playHint blocks named "...Respawning"/
                    // "...Rearmed". Plain descriptive text may legitimately contain the
                    // words, so scan block names instead of the whole document.
                    BlkTools.Blocks(text, "playHint").Any(h =>
                    {
                        string hintName = BlkTools.Field(h.Text, "name", "t") ?? "";
                        return hintName.IndexOf("Respawning", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            hintName.IndexOf("Rearmed", StringComparison.OrdinalIgnoreCase) >= 0;
                    }))
                    throw new InvalidOperationException("Mission self-test failed.");
                if (hostileMission.Count(c => c == '{') != hostileMission.Count(c => c == '}') ||
                    hostileMission.IndexOf("UTL Hostile Ground Target", StringComparison.Ordinal) < 0 ||
                    hostileMission.IndexOf("attack_type:t=\"fire_at_will\"", StringComparison.Ordinal) < 0 ||
                    hostileMission.IndexOf("object:t=\"Target_03\"", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Hostile ground-target self-test failed.");
                string samSitesDisabled = BlkTools.SetSamSites(text, "disabled", "all");
                if (samSitesDisabled.Count(c => c == '{') != samSitesDisabled.Count(c => c == '}'))
                    throw new InvalidOperationException("SAM-sites disable self-test failed.");
                foreach (string samTriggerName in new[] { "spawn_ctr_s300_sites", "spawn_ctr_patriot_sites", "spawn_ctr_buk_sites" })
                {
                    BlockSpan samTrigger = BlkTools.FirstBlock(samSitesDisabled, samTriggerName, 0);
                    if (samTrigger == null || samTrigger.Text.IndexOf("is_enabled:b=no", StringComparison.Ordinal) < 0)
                        throw new InvalidOperationException("SAM-sites disable self-test failed: " + samTriggerName);
                }
                string samSitesPassive = BlkTools.SetSamSites(text, "passive", "s300");
                if (samSitesPassive.IndexOf("attack_type:t=\"dont_aim\"", StringComparison.Ordinal) < 0 ||
                    samSitesPassive.Count(c => c == '{') != samSitesPassive.Count(c => c == '}'))
                    throw new InvalidOperationException("SAM-sites passive self-test failed.");
                string samSitesFriendly = BlkTools.SetSamSites(text, "friendly", "all");
                if (!Regex.IsMatch(samSitesFriendly, @"name:t=""CTR_[^""]+""[\s\S]*?props\{\s*army:i=1") ||
                    samSitesFriendly.Count(c => c == '{') != samSitesFriendly.Count(c => c == '}'))
                    throw new InvalidOperationException("SAM-sites friendly self-test failed.");
                CombinedMap combinedTestMap = new CombinedMap { Id = "selftest", Display = "Self Test", Level = "levels/avg_abandoned_factory.bin" };
                CombinedSpawn combinedTestSpawn = new CombinedSpawn
                {
                    Kind = "aircraft", Side = 2, Option = "airfield", Label = "Airfield",
                    Transform = "[[0.6, 0, -0.8] [0, 1, 0] [0.8, 0, 0.6] [8171.8, 49.45, -11873.2]]",
                    ObjectClass = "dynaf_pg_1line_3000_universal"
                };
                combinedTestMap.Spawns.Add(new CombinedSpawn
                {
                    Kind = "aircraft", Side = 1, Option = "airfield", Label = "Airfield",
                    Transform = "[[1, 0, 0] [0, 1, 0] [0, 0, 1] [-8100, 44, 11900]]",
                    ObjectClass = "dynaf_pg_1line_3000_universal"
                });
                combinedTestMap.Spawns.Add(combinedTestSpawn);
                CombinedSpawn combinedGroundTestSpawn = new CombinedSpawn
                {
                    Kind = "ground", Side = 1, Option = "ground_1", Label = "Ground spawn 1",
                    Transform = "[[1, 0, 0] [0, 1, 0] [0, 0, 1] [1000, 15, 1500]]"
                };
                combinedTestMap.Spawns.Add(combinedGroundTestSpawn);
                combinedTestMap.Spawns.Add(new CombinedSpawn
                {
                    Kind = "ground", Side = 2, Option = "ground_1", Label = "Ground spawn 1",
                    Transform = "[[-1, 0, 0] [0, 1, 0] [0, 0, -1] [3000, 16, 3500]]"
                });
                combinedTestMap.CapturePoints.Add(new CombinedCapturePoint
                {
                    Id = "capture_a", Label = "A",
                    Transform = "[[45, 0, 0] [0, 35, 0] [0, 0, 45] [100, 5, 200]]"
                });
                combinedTestMap.CapturePoints.Add(new CombinedCapturePoint
                {
                    Id = "capture_b", Label = "B",
                    Transform = "[[50, 0, 0] [0, 35, 0] [0, 0, 50] [400, 6, 500]]"
                });
                combinedTestMap.CapturePoints.Add(new CombinedCapturePoint
                {
                    Id = "capture_c", Label = "C",
                    Transform = "[[55, 0, 0] [0, 35, 0] [0, 0, 55] [700, 7, 800]]"
                });
                string combinedMission = BlkTools.ConfigureCombinedScenario(text, combinedTestMap, combinedTestSpawn);
                combinedMission = BlkTools.AccelerateRangeRecovery(combinedMission, false);
                combinedMission = BlkTools.ConfigureInstantPlayerRespawn(combinedMission, false, 0, BlkTools.CombinedRespawnTransform(combinedTestSpawn));
                BlockSpan combinedUnits = BlkTools.FirstBlock(combinedMission, "units", 0);
                BlockSpan combinedPlayer = BlkTools.UnitBlockByName(combinedMission, "You");
                if (combinedMission.Count(c => c == '{') != combinedMission.Count(c => c == '}') ||
                    combinedMission.IndexOf("level:t=\"levels/avg_abandoned_factory.bin\"", StringComparison.Ordinal) < 0 ||
                    combinedMission.IndexOf("name:t=\"Target_03\"", StringComparison.Ordinal) >= 0 ||
                    combinedMission.IndexOf("unit_class:t=\"dynaf_pg_1line_3000_universal\"", StringComparison.Ordinal) < 0 ||
                    combinedMission.IndexOf("UTL_Selected_Spawn_Base", StringComparison.Ordinal) < 0 ||
                    combinedMission.IndexOf("[8171.8, 52.45, -11873.2]", StringComparison.Ordinal) < 0 ||
                    combinedMission.IndexOf("UTL_Player_Air_Spawn", StringComparison.Ordinal) < 0 ||
                    combinedMission.IndexOf("UTL Aircraft Map Extent", StringComparison.Ordinal) < 0 ||
                    combinedMission.IndexOf("airMapArea:b=yes", StringComparison.Ordinal) < 0 ||
                    combinedMission.IndexOf("killOutOfBattleArea:b=no", StringComparison.Ordinal) < 0 ||
                    combinedMission.IndexOf("UTL_Air_Map_Area", StringComparison.Ordinal) < 0 ||
                    combinedMission.IndexOf("[[40000, 0, 0] [0, 40000, 0] [0, 0, 40000]", StringComparison.Ordinal) < 0 ||
                    combinedMission.IndexOf("UTL Combined Map Markers", StringComparison.Ordinal) < 0 ||
                    combinedMission.IndexOf("target:t=\"UTL_Capture_A\"", StringComparison.Ordinal) < 0 ||
                    combinedMission.IndexOf("target:t=\"UTL_Capture_B\"", StringComparison.Ordinal) < 0 ||
                    combinedMission.IndexOf("target:t=\"UTL_Capture_C\"", StringComparison.Ordinal) < 0 ||
                    Regex.Matches(combinedMission, "missionMarkAsCaptureZone\\{").Count != 3 ||
                    Regex.Matches(combinedMission, "missionMarkAsRespawnPoint\\{").Count != 2 ||
                    combinedMission.IndexOf("canCaptureOnGround:b=no", StringComparison.Ordinal) < 0 ||
                    combinedMission.IndexOf("canCaptureInAir:b=no", StringComparison.Ordinal) < 0 ||
                    combinedMission.IndexOf("useHUDMarkers:b=no", StringComparison.Ordinal) < 0 ||
                    combinedMission.IndexOf("showOnMap:b=yes", StringComparison.Ordinal) < 0 ||
                    combinedMission.IndexOf("Starting Capzone", StringComparison.Ordinal) >= 0 ||
                    combinedMission.IndexOf("UTL APS Carrier Recovery Compatible", StringComparison.Ordinal) >= 0 ||
                    combinedMission.IndexOf("UTL Fast Rearm Policy", StringComparison.Ordinal) < 0 ||
                    combinedUnits == null || combinedPlayer.Text.IndexOf("army:i=2", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Combined-battles scenario self-test failed.");
                string combinedGroundMission = BlkTools.ConfigureCombinedScenario(text, combinedTestMap, combinedGroundTestSpawn);
                if (combinedGroundMission.IndexOf("useHUDMarkers:b=yes", StringComparison.Ordinal) < 0 ||
                    combinedGroundMission.IndexOf("UTL Aircraft Map Extent", StringComparison.Ordinal) >= 0 ||
                    Regex.Matches(combinedGroundMission, "missionMarkAsRespawnPoint\\{").Count != 2)
                    throw new InvalidOperationException("Combined ground-map marker self-test failed.");
                if (fpvMission.Count(c => c == '{') != fpvMission.Count(c => c == '}') ||
                    fpvMission.IndexOf("UTL FPV Detonation - Target_03", StringComparison.Ordinal) < 0 ||
                    fpvMission.IndexOf("effect:t=\"hit_81_132mm_heat\"", StringComparison.Ordinal) < 0 ||
                    fpvMission.IndexOf("target:t=\"Target_03\"", StringComparison.Ordinal) < 0 ||
                    fpvMission.IndexOf("math:t=\"3D\"", StringComparison.Ordinal) < 0 ||
                    fpvMission.IndexOf("value:r=6", StringComparison.Ordinal) < 0 ||
                    fpvMission.IndexOf("power:r=0.35", StringComparison.Ordinal) < 0 ||
                    fpvMission.IndexOf("UTL FPV Re-arm Detonator", StringComparison.Ordinal) < 0 ||
                    fpvMission.IndexOf("unitWhenRespawn", StringComparison.Ordinal) < 0 ||
                    fpvMission.IndexOf("doNuclearExplosion", StringComparison.Ordinal) >= 0)
                    throw new InvalidOperationException("FPV detonation self-test failed.");
                string legacyMenu = "  name:t=\"universal_test_lab\"\r\n  chapter:t=\"TestDrive\"\r\n  campaign:t=\"CleanTestDrive\"\r\n";
                string cleanMenu = BlkTools.CleanLegacyMenuKeys(legacyMenu);
                if (cleanMenu.IndexOf("campaign:t=\"UserMissions\"", StringComparison.Ordinal) < 0 ||
                    cleanMenu.IndexOf("CleanTestDrive", StringComparison.Ordinal) >= 0 ||
                    cleanMenu.IndexOf("TestDrive", StringComparison.Ordinal) >= 0 ||
                    cleanMenu.IndexOf("name:t=\"universal_test_lab\"", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Legacy menu cleanup self-test failed.");
                string selectiveMission = BlkTools.ConfigureUnitModifications(text, "You", false, new[] { "yak9ut_ns45_mod", "yak9ut_ns45_new_gun" });
                BlockSpan selectivePlayer = BlkTools.UnitBlockByName(selectiveMission, "You");
                if (selectivePlayer.Text.IndexOf("applyAllMods:b=no", StringComparison.Ordinal) < 0 ||
                    selectivePlayer.Text.IndexOf("modification:t=\"yak9ut_ns45_mod\"", StringComparison.Ordinal) < 0 ||
                    selectivePlayer.Text.IndexOf("modification:t=\"yak9ut_ns45_new_gun\"", StringComparison.Ordinal) < 0 ||
                    Regex.Matches(selectivePlayer.Text, @"(?m)^\s*modification:t=").Count != 2)
                    throw new InvalidOperationException("Selective modification self-test failed.");
                string groupedCannon = "cannon:b=true\r\n120mm_us_M829A3_APDSFS {\r\n  bullet {\r\n    bulletName:t=\"120mm_us_m829a3\"\r\n  }\r\n}\r\nbullet {\r\n  bulletName:t=\"stock_round\"\r\n}\r\n";
                if (MainForm.FindGroundAmmoGroup(groupedCannon, "120mm_us_m829a3") != "120mm_us_M829A3_APDSFS" ||
                    MainForm.FindGroundAmmoGroup(groupedCannon, "stock_round") != "")
                    throw new InvalidOperationException("Ground ammunition-group resolution self-test failed.");
                string beltCannon = "cannon:b=true\r\n30mm_belt_group {\r\n  bullet {\r\n    bulletName:t=\"30mm_p1\"\r\n  }\r\n  bullet {\r\n    bulletName:t=\"30mm_p2\"\r\n  }\r\n}\r\nbullet {\r\n  bulletName:t=\"30mm_single\"\r\n}\r\n";
                if (MainForm.ResolveAmmoSlotId(beltCannon, "30mm_belt_group") != "30mm_p1" ||
                    MainForm.ResolveAmmoSlotId(beltCannon, "30mm_p1") != "30mm_p1" ||
                    MainForm.ResolveAmmoSlotId(beltCannon, "30mm_single") != "" ||
                    MainForm.ResolveAmmoSlotId(groupedCannon, "120mm_us_m829a3") != "120mm_us_M829A3_APDSFS" ||
                    MainForm.ResolveAmmoSlotId(groupedCannon, "120mm_us_M829A3_APDSFS") != "120mm_us_M829A3_APDSFS")
                    throw new InvalidOperationException("Ground ammo-slot id resolution self-test failed.");
                AircraftSettings moduleEffectsSettings = new AircraftSettings();
                StringBuilder moduleEffectsProxy = new StringBuilder("include \"native.blk\"\r\n");
                string moduleEffectsNative = "modifications {\r\n  laser_rangefinder_lws {\r\n    effects {\r\n      rangefinderMounted:b=true\r\n      isLaser:b=true\r\n      sensors { sensor { blk:t=\"laser.blk\" } }\r\n    }\r\n  }\r\n}\r\n";
                MainForm.AppendGroundModuleEffectOverrides(moduleEffectsProxy, moduleEffectsNative, moduleEffectsSettings);
                if (moduleEffectsProxy.ToString().IndexOf("\"@override:rangefinderMounted\":b = true", StringComparison.Ordinal) < 0 ||
                    moduleEffectsProxy.ToString().IndexOf("\"@override:isLaser\":b = true", StringComparison.Ordinal) < 0 ||
                    moduleEffectsProxy.ToString().IndexOf("\"@override:sensors\"", StringComparison.Ordinal) < 0 ||
                    moduleEffectsProxy.ToString().IndexOf("rangefinderMounted:b=true", StringComparison.Ordinal) >= 0)
                    throw new InvalidOperationException("Ground module-effect materialization self-test failed.");
                AircraftSettings groundSettings = new AircraftSettings();
                groundSettings.GroundAmmoLoadouts.Add(new GroundAmmoLoadout { Slot = 0, Count = 22, BulletName = "120mm_us_m829a3", AmmoGroup = "120mm_us_M829A3_APDSFS" });
                string groundMission = BlkTools.ConfigureGroundPlayer(text, MainForm.GroundProxyClassId, "m1a2_sep3", "us_m1a2_sep3_abrams_default", groundSettings);
                groundMission = BlkTools.ConfigureInstantPlayerRespawn(groundMission, true, 0);
                // Follow the real generation path (AccelerateRangeRecovery 4-arg call at the
                // mission builder): the rearmTimeOnField policy block is injected only when
                // the user enables the rearm override, which is OFF by default.
                groundMission = BlkTools.AccelerateRangeRecovery(groundMission, true, 0.25, null);
                groundMission = BlkTools.MakeShipPassive(groundMission, "Ship_Target");
                BlockSpan groundPlayer = BlkTools.UnitBlockByName(groundMission, "You");
                BlockSpan legacyTimedReload = BlkTools.FirstBlock(groundMission, "\"Player Ammo Reload 10s\"", 0);
                BlockSpan groundFuelTrigger = BlkTools.FirstBlock(groundMission, "\"Player Full Internal Fuel\"", 0);
                BlockSpan groundSpeedTrigger = BlkTools.FirstBlock(groundMission, "\"Player Respawn Flight Profile\"", 0);
                if (groundMission.Count(c => c == '{') != groundMission.Count(c => c == '}') ||
                    groundPlayer.Text.IndexOf("tankModels{", StringComparison.Ordinal) < 0 ||
                    groundPlayer.Text.IndexOf("unit_class:t=\"" + MainForm.GroundProxyClassId + "\"", StringComparison.Ordinal) < 0 ||
                    groundPlayer.Text.IndexOf("[6.3526, 41.581, -622.332]", StringComparison.Ordinal) < 0 ||
                    groundPlayer.Text.IndexOf("[-0.5, 0, 0.866025]", StringComparison.Ordinal) < 0 ||
                    groundPlayer.Text.IndexOf("bullets0:t=\"120mm_us_M829A3_APDSFS\"", StringComparison.Ordinal) < 0 ||
                    groundPlayer.Text.IndexOf("bulletsCount0:i=22", StringComparison.Ordinal) < 0 ||
                    groundPlayer.Text.IndexOf("crewSkillK:r=1", StringComparison.Ordinal) < 0 ||
                    groundPlayer.Text.IndexOf("applyAllMods:b=no", StringComparison.Ordinal) < 0 ||
                    groundMission.IndexOf("UTL Ground Weapon Initialization", StringComparison.Ordinal) >= 0 ||
                    groundMission.IndexOf("restoreType:t=\"attempts\"", StringComparison.Ordinal) < 0 ||
                    
                    
                                                            (MissionSettings.Current.RearmOverride
                        ? groundMission.IndexOf("UTL Fast Rearm Policy", StringComparison.Ordinal) < 0 || groundMission.IndexOf("rearmTimeOnField:r=1", StringComparison.Ordinal) < 0
                        : groundMission.IndexOf("UTL Fast Rearm Policy", StringComparison.Ordinal) >= 0) ||
                    groundMission.IndexOf("UTL Player Rearm When Empty Compatible", StringComparison.Ordinal) >= 0 ||
                    groundMission.IndexOf("object_type:t=\"noAmmo\"", StringComparison.Ordinal) >= 0 ||
                    legacyTimedReload == null || legacyTimedReload.Text.IndexOf("is_enabled:b=no", StringComparison.Ordinal) < 0 ||
                    groundFuelTrigger == null || groundFuelTrigger.Text.IndexOf("is_enabled:b=no", StringComparison.Ordinal) < 0 ||
                    groundSpeedTrigger == null || groundSpeedTrigger.Text.IndexOf("is_enabled:b=no", StringComparison.Ordinal) < 0 ||
                    groundMission.IndexOf("UTL APS Carrier Recovery Compatible", StringComparison.Ordinal) < 0 ||
                    groundMission.IndexOf("UTL Target Ammunition Restore Compatible", StringComparison.Ordinal) >= 0 ||
                    groundMission.IndexOf("restoreType:t=\"attempts\"", StringComparison.Ordinal) < 0 ||
                    groundMission.IndexOf("attack_type:t=\"fire_at_will\"", StringComparison.Ordinal) < 0 ||
                    groundMission.IndexOf("UTL_Player_Ground_Spawn", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Ground vehicle and unlimited-respawn self-test failed.");
                string topGroundMission = BlkTools.ConfigureUnitModifications(groundMission, "You", true, Enumerable.Empty<string>());
                BlockSpan topGroundPlayer = BlkTools.UnitBlockByName(topGroundMission, "You");
                if (topGroundPlayer.Text.IndexOf("crewSkillK:r=1", StringComparison.Ordinal) < 0 ||
                    topGroundPlayer.Text.IndexOf("applyAllMods:b=yes", StringComparison.Ordinal) < 0 ||
                    Regex.Matches(topGroundPlayer.Text, @"(?m)^\s*modification:t=").Count != 0)
                    throw new InvalidOperationException("Top ground modification and crew self-test failed.");
                // Positive check for the rearm override path: enabling it must inject the
                // one-second on-field rearm policy into the template's trigger set.
                string rearmOverrideMission = BlkTools.AccelerateRangeRecovery(text, true, 0.25, 1.0);
                if (rearmOverrideMission.IndexOf("UTL Fast Rearm Policy", StringComparison.Ordinal) < 0 ||
                    rearmOverrideMission.IndexOf("rearmTimeOnField:r=1", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Fast rearm override self-test failed.");
                string selectiveGroundMission = BlkTools.ConfigureUnitModifications(groundMission, "You", false, new[] { "laser_rangefinder_lws", "120mm_britain_L27_APDSFS" });
                BlockSpan selectiveGroundPlayer = BlkTools.UnitBlockByName(selectiveGroundMission, "You");
                if (selectiveGroundPlayer.Text.IndexOf("crewSkillK:r=1", StringComparison.Ordinal) < 0 ||
                    selectiveGroundPlayer.Text.IndexOf("applyAllMods:b=no", StringComparison.Ordinal) < 0 ||
                    selectiveGroundPlayer.Text.IndexOf("modification:t=\"laser_rangefinder_lws\"", StringComparison.Ordinal) < 0 ||
                    selectiveGroundPlayer.Text.IndexOf("modification:t=\"120mm_britain_L27_APDSFS\"", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Selective ground modification and crew self-test failed.");
                string nativeAmmoGround = BlkTools.ConfigureGroundPlayer(text, MainForm.GroundProxyClassId, "m1a2_sep3", "us_m1a2_sep3_abrams_default", new AircraftSettings());
                BlockSpan nativeAmmoPlayer = BlkTools.UnitBlockByName(nativeAmmoGround, "You");
                if (nativeAmmoPlayer.Text.IndexOf("bulletsCount0:i=9999", StringComparison.Ordinal) < 0 ||
                    nativeAmmoGround.IndexOf("UTL Ground Weapon Initialization", StringComparison.Ordinal) >= 0)
                    throw new InvalidOperationException("Native ground-ammunition fallback self-test failed.");
                string sightUnit = MainForm.SetOrInsertString("model:t = \"m1_abrams\"\r\ncrosshairPreset:t = \"native\"\r\n", "crosshairPreset", "AstraSEP_fixed");
                if (sightUnit.IndexOf("crosshairPreset:t = \"AstraSEP_fixed\"", StringComparison.Ordinal) < 0 ||
                    sightUnit.IndexOf("crosshairPreset:t = \"native\"", StringComparison.Ordinal) >= 0 ||
                    Regex.Matches(sightUnit, @"(?m)^\s*crosshairPreset:t").Count != 1)
                    throw new InvalidOperationException("Ground custom-sight binding self-test failed.");
                string globalSight = "content{\r\n  profile{\r\n    tankSightSettings{\r\n      utl_run_old_ground{\r\n        crosshair:t=\"old\"\r\n      }\r\n      us_m1_abrams{\r\n        crosshair:t=\"native\"\r\n      }\r\n    }\r\n  }\r\n}\r\n";
                globalSight = UserSightStore.BindGeneratedVehicleSelectionText(globalSight, "utl_run_selftest_ground", "AstraSEP fixed");
                if (globalSight.IndexOf("utl_run_old_ground", StringComparison.Ordinal) >= 0 ||
                    globalSight.IndexOf("utl_run_selftest_ground", StringComparison.Ordinal) < 0 ||
                    globalSight.IndexOf("crosshair:t=\"AstraSEP fixed\"", StringComparison.Ordinal) < 0 ||
                    globalSight.IndexOf("us_m1_abrams", StringComparison.Ordinal) < 0 ||
                    globalSight.Count(c => c == '{') != globalSight.Count(c => c == '}'))
                    throw new InvalidOperationException("War Thunder global custom-sight selection self-test failed.");
                string emptyGlobalSight = UserSightStore.BindGeneratedVehicleSelectionText("content{\n  profile{\n  }\n}\n", "utl_run_empty_ground", "sight_1");
                if (emptyGlobalSight.IndexOf("tankSightSettings", StringComparison.Ordinal) < 0 || emptyGlobalSight.IndexOf("crosshair:t=\"sight_1\"", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("New War Thunder custom-sight settings block self-test failed.");
                if (!MainForm.JsonRows<AircraftRowJson>("UTL.aircraft.json").Any(x => x != null && x.id == "uav_inf_fpv_strike_drone" && x.kind == "Drone"))
                    throw new InvalidOperationException("FPV drone catalog self-test failed.");
                Dictionary<string, GroundWeaponCacheData> prebuiltWeapons = MainForm.LoadPrebuiltGroundWeapons();
                GroundWeaponCacheData prebuiltT72;
                if (prebuiltWeapons == null || prebuiltWeapons.Count < 1000 ||
                    !prebuiltWeapons.TryGetValue("sw_t_72m1", out prebuiltT72) ||
                    prebuiltT72.Weapons == null || prebuiltT72.Weapons.Count == 0)
                    throw new InvalidOperationException("Prebuilt vehicle weapons catalog self-test failed.");
                GroundWeaponCacheData prebuiltM16;
                if (!prebuiltWeapons.TryGetValue("us_halftrack_m16", out prebuiltM16) ||
                    prebuiltM16.Weapons == null || prebuiltM16.Weapons.Count == 0 ||
                    prebuiltM16.Weapons[0].NativeAmmo < 4800 ||
                    !prebuiltM16.BeltSizes.ContainsKey("12") || prebuiltM16.BeltSizes["12"] != 200)
                    throw new InvalidOperationException("Prebuilt multi-mount/belt-size self-test failed.");
                List<AircraftRowJson> aircraftCatalogRows = MainForm.JsonRows<AircraftRowJson>("UTL.aircraft.json");
                List<GroundRowJson> groundCatalogRows = MainForm.JsonRows<GroundRowJson>("UTL.ground.json");
                List<GroundAmmoJson> groundAmmoCatalogRows = MainForm.JsonRows<GroundAmmoJson>("UTL.ground_ammo.json");
                List<PylonSlotRowJson> slotCatalogRows = MainForm.JsonRows<PylonSlotRowJson>("UTL.aircraft_slots.json");
                List<ModificationRowJson> modificationCatalogRows = MainForm.JsonRows<ModificationRowJson>("UTL.modifications.json");
                if (aircraftCatalogRows.Count < 1400 ||
                    !aircraftCatalogRows.Any(x => x != null && x.id == "nt_b_52h" && x.display.IndexOf("B-52H", StringComparison.Ordinal) >= 0) ||
                    !aircraftCatalogRows.Any(x => x != null && x.id == "nt_tu_95m" && x.display.IndexOf("Tu-95M", StringComparison.Ordinal) >= 0) ||
                    !aircraftCatalogRows.Any(x => x != null && x.id == "fau-1" && x.type == "typeTransport") ||
                    !aircraftCatalogRows.Any(x => x != null && x.id == "ah_64d" && x.kind == "Helicopter") ||
                    !groundCatalogRows.Any(x => x != null && x.id == "us_m1a2_sep2_abrams") ||
                    !groundCatalogRows.Any(x => x != null && x.id == "us_m1a2_sep3_abrams" && x.maxAmmo == 42 && x.mass == 54000) ||
                    !groundCatalogRows.Any(x => x != null && x.id == "germ_leichter_ladungstrager_303a") ||
                    !groundAmmoCatalogRows.Any(x => x != null && x.bulletName == "120mm_m829a2") ||
                    !modificationCatalogRows.Any(x => x != null && x.aircraftId == "yak-9ut" && x.id == "yak9ut_n37_mod") ||
                    !modificationCatalogRows.Any(x => x != null && x.aircraftId == "yak-9ut" && x.id == "yak9ut_ns45_mod") ||
                    modificationCatalogRows.Any(x => x != null && x.aircraftId == "us_m1a2_sep2_abrams" && x.id == "tank_medical_kit_expendable") ||
                    slotCatalogRows.Count(x => x != null && x.aircraftId == "b_52h") != 5 ||
                    slotCatalogRows.Count(x => x != null && x.aircraftId == "tu_95m") != 1 ||
                    slotCatalogRows.Count(x => x != null && x.aircraftId == "ah_64d") != 6)
                    throw new InvalidOperationException("Aircraft/helicopter catalog self-test failed.");
                StringBuilder helicopterLoadout = new StringBuilder();
                string helicopterUnit = "commonWeapons {\nWeapon {\nslot:i = 0\npreset:t = \"m230e1_common\"\n}\nWeapon {\nslot:i = 2\npreset:t = \"fixed_optional\"\n}\n}\nweapon_presets {\n}\n";
                helicopterLoadout.AppendLine("Weapon {\nslot:i = 1\npreset:t = \"agm_179_ir_x4\"\n}");
                MainForm.AppendCommonWeaponsToLoadout(helicopterLoadout, helicopterUnit, new HashSet<int> { 1, 2 }, true);
                string helicopterLoadoutText = helicopterLoadout.ToString();
                if (helicopterLoadoutText.IndexOf("preset:t = \"m230e1_common\"", StringComparison.Ordinal) >= 0 ||
                    helicopterLoadoutText.IndexOf("fixed_optional", StringComparison.Ordinal) >= 0 ||
                    helicopterLoadoutText.IndexOf("agm_179_ir_x4", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Native helicopter external-only loadout self-test failed.");
                StringBuilder aircraftCommonLoadout = new StringBuilder();
                MainForm.AppendCommonWeaponsToLoadout(aircraftCommonLoadout, helicopterUnit, new HashSet<int> { 2 }, false);
                if (aircraftCommonLoadout.ToString().IndexOf("preset:t = \"m230e1_common\"", StringComparison.Ordinal) < 0 ||
                    aircraftCommonLoadout.ToString().IndexOf("fixed_optional", StringComparison.Ordinal) >= 0)
                    throw new InvalidOperationException("Explicit aircraft common-weapon loadout self-test failed.");
                List<PylonAssignment> mirroredHelicopterStations = new List<PylonAssignment>
                {
                    new PylonAssignment { Pylon = new PylonSlot { Slot = 1, Order = 1 } },
                    new PylonAssignment { Pylon = new PylonSlot { Slot = 4, Order = 2 } },
                    new PylonAssignment { Pylon = new PylonSlot { Slot = 2, Order = 3 } },
                    new PylonAssignment { Pylon = new PylonSlot { Slot = 3, Order = 4 } }
                };
                string orderedStations = String.Join(",", MainForm.OrderAssignmentsForPreset(mirroredHelicopterStations)
                    .Select(x => x.Pylon.Slot.ToString(CultureInfo.InvariantCulture)).ToArray());
                if (orderedStations != "1,4,2,3")
                    throw new InvalidOperationException("Native aircraft weapon-preset ordering self-test failed.");
                string helicopterClassified = MainForm.EnsureHelicopterExperienceClass("model:t = \"ah_64e\"\nexpClass:t = \"exp_fighter\"\n");
                if (helicopterClassified.IndexOf("expClass:t = \"exp_helicopter\"", StringComparison.Ordinal) < 0 ||
                    helicopterClassified.IndexOf("exp_fighter", StringComparison.Ordinal) >= 0)
                    throw new InvalidOperationException("Helicopter HUD/input classification self-test failed.");
                string fm = Embedded.Text("UTL.utl_safe_player.blk");
                PylonSlot pylon = new PylonSlot { Slot = 2, AnchorMount = "aim_120c_slot2_x2" };
                DonorWeapon weapon = new DonorWeapon { Trigger = "aam", Blk = "gameData/Weapons/rocketGuns/us_aim_120d.blk", Bullets = 1, Icon = "missile_type_c_air_to_air_midrange" };
                MainForm.AddInjectedMount(ref fm, pylon, weapon, "utl_run_selftest_slot_2");
                MainForm.RegisterPreset(ref fm, "utl_run_selftest_loadout");
                if (fm.Count(c => c == '{') != fm.Count(c => c == '}') ||
                    fm.IndexOf("us_aim_120d.blk", StringComparison.Ordinal) < 0 ||
                    fm.IndexOf("name:t = \"aim_120c_slot2_x2\"", StringComparison.Ordinal) < 0 ||
                    fm.IndexOf("name:t = \"utl_run_selftest_slot_2\"", StringComparison.Ordinal) >= 0)
                    throw new InvalidOperationException("Loadout/F2 replacement self-test failed.");
                string podFm = Embedded.Text("UTL.utl_safe_player.blk");
                DonorWeapon pod = new DonorWeapon { Trigger = "targetingPod", Blk = "gameData/Weapons/equipment/gr_litening_iii_targeting_pod.blk", Bullets = 1, Icon = "flir_container" };
                MainForm.AddInjectedMount(ref podFm, pylon, pod, "utl_run_selftest_pod_2");
                if (podFm.Count(c => c == '{') != podFm.Count(c => c == '}') ||
                    podFm.IndexOf("hasTargetingPod:b = true", StringComparison.Ordinal) < 0 ||
                    podFm.IndexOf("gr_litening_iii_targeting_pod.blk", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Targeting-pod replacement self-test failed.");
                string tankCleanup = "WeaponSlot {\nindex:i=1\nWeaponPreset {\nname:t=\"ptb\"\nWeapon {\ntrigger:t=\"fuel tanks\"\nblk:t=\"drop_tank.blk\"\n}\n}\nWeaponPreset {\nname:t=\"aam\"\nWeapon {\ntrigger:t=\"aam\"\nblk:t=\"missile.blk\"\n}\n}\n}";
                MainForm.RemoveFuelTankPresets(ref tankCleanup);
                if (tankCleanup.IndexOf("fuel tanks", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tankCleanup.IndexOf("missile.blk", StringComparison.Ordinal) < 0 ||
                    tankCleanup.Count(c => c == '{') != tankCleanup.Count(c => c == '}'))
                    throw new InvalidOperationException("Phantom fuel-tank cleanup self-test failed.");
                string legacyAircraft = "model:t = \"cw_21\"\nweapon_presets {\n}\n";
                MainForm.EnsureExplicitFlightModel(ref legacyAircraft, "cw_21");
                string modernAircraft = "model:t = \"modern\"\nfmFile:t = \"fm/modern.blk\"\n";
                MainForm.EnsureExplicitFlightModel(ref modernAircraft, "modern");
                if (legacyAircraft.IndexOf("fmFile:t = \"fm/cw_21.blk\"", StringComparison.Ordinal) < 0 ||
                    Regex.Matches(legacyAircraft, @"(?m)^\s*fmFile:t\s*=").Count != 1 ||
                    Regex.Matches(modernAircraft, @"(?m)^\s*fmFile:t\s*=").Count != 1 ||
                    modernAircraft.IndexOf("fm/modern.blk", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Legacy aircraft flight-model reference self-test failed.");
                Aircraft propAircraft = new Aircraft { Id = "cw_21", Rank = 1 };
                Aircraft earlyJet = new Aircraft { Id = "f-80", Rank = 5 };
                Aircraft modernJet = new Aircraft { Id = "ef_2000_typhoon_aesa", Rank = 9 };
                Aircraft helicopter = new Aircraft { Id = "ah_64d", Rank = 7, Kind = "Helicopter" };
                string jetDefinition = "MetaPartsBlk:t = \"gameData/FlightModels/dm/metaparts/jet_fighter_metaparts.blk\"\nstandardExhaustFxType:t = \"jet_exhaust\"\n";
                if (MainForm.ResolveSpawnSpeed(propAircraft, legacyAircraft) != 450 ||
                    MainForm.ResolveSpawnSpeed(earlyJet, jetDefinition) != 700 ||
                    MainForm.ResolveSpawnSpeed(modernJet, jetDefinition) != 1100 ||
                    MainForm.ResolveSpawnSpeed(helicopter, jetDefinition) != 0 ||
                    MainForm.ResolveSpawnSpeed(new Aircraft { Id = "uav_inf_fpv_strike_drone", Rank = 8 }, jetDefinition) != 100)
                    throw new InvalidOperationException("Aircraft spawn-speed profile self-test failed.");
                string earlyJetMission = MainForm.ApplyPlayerSpawnSpeed(Embedded.Text("UTL.universal_test_lab.blk"), 700);
                if (earlyJetMission.IndexOf("speed:r=1100", StringComparison.Ordinal) >= 0 ||
                    Regex.Matches(earlyJetMission, @"(?m)^\s*speed:r=700\s*$").Count != 4)
                    throw new InvalidOperationException("Mission spawn-speed replacement self-test failed.");
                string helicopterMission = MainForm.ApplyPlayerSpawnSpeed(Embedded.Text("UTL.universal_test_lab.blk"), 0);
                if (Regex.Matches(helicopterMission, @"(?m)^\s*speed:r=0\s*$").Count != 4)
                    throw new InvalidOperationException("Helicopter stationary-spawn self-test failed.");
                string halfFuelMission = MainForm.ApplyPlayerFuel(Embedded.Text("UTL.universal_test_lab.blk"), new AircraftSettings { FullFuel = false, FuelMinutes = 30 });
                if (Regex.Matches(halfFuelMission, @"(?m)^\s*fuel:r=50\s*$").Count == 0 ||
                    Regex.Matches(halfFuelMission, @"(?m)^\s*fuel:r=100\s*$").Count != 0)
                    throw new InvalidOperationException("Mission starting-fuel replacement self-test failed.");
                AircraftSettings beltMissionSettings = new AircraftSettings();
                beltMissionSettings.GunBeltSelections[0] = "bk_27_air_targets";
                beltMissionSettings.GunBeltSelections[2] = "50cal_stealth";
                string beltMission = MainForm.ApplyPlayerGunBelts(Embedded.Text("UTL.universal_test_lab.blk"), beltMissionSettings);
                BlockSpan beltPlayer = BlkTools.UnitBlockByName(beltMission, "You");
                if (beltPlayer == null || beltPlayer.Text.IndexOf("bullets0:t=\"bk_27_air_targets\"", StringComparison.Ordinal) < 0 ||
                    beltPlayer.Text.IndexOf("bullets2:t=\"50cal_stealth\"", StringComparison.Ordinal) < 0 ||
                    beltPlayer.Text.IndexOf("bullets1:t=\"\"", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Mission gun-belt selection self-test failed.");
                string samSource = "bullet {\nbulletName:t=\"us_iris_t_sl\"\nbulletType:t=\"sam_tank\"\nrocket {\nmass:r=155\nmesh:t=\"iris_t_sl_rocket\"\nshellAnimChar:t=\"iris_t_sl_rocket_deployed_char\"\nguidance {\nuncageBeforeLaunch:b=true\n}\n}\n}";
                string samAdapter = MainForm.BuildGroundSamAdapter(samSource, "us_iris_t_sl");
                if (samAdapter.IndexOf("rocketGun:b = true", StringComparison.Ordinal) < 0 ||
                    samAdapter.IndexOf("bulletName:t = \"us_iris_t_sl\"", StringComparison.Ordinal) < 0 ||
                    samAdapter.IndexOf("uncageBeforeLaunch:b=true", StringComparison.Ordinal) < 0 ||
                    samAdapter.IndexOf("mesh:t = \"iris_t_rocket\"", StringComparison.Ordinal) < 0 ||
                    samAdapter.IndexOf("shellAnimChar:t = \"iris_t_rocket_char\"", StringComparison.Ordinal) < 0 ||
                    samAdapter.IndexOf("iris_t_sl_rocket_deployed_char", StringComparison.Ordinal) >= 0 ||
                    samAdapter.Count(c => c == '{') != samAdapter.Count(c => c == '}'))
                    throw new InvalidOperationException("Ground SAM adapter self-test failed.");
                List<DonorWeaponRowJson> weaponCatalogRows = MainForm.JsonRows<DonorWeaponRowJson>("UTL.weapon_catalog.json");
                if (weaponCatalogRows.Count < 2000 ||
                    !weaponCatalogRows.Any(x => x != null && x.blk != null && x.blk.IndexOf("#us_aim_9x_block_2", StringComparison.Ordinal) >= 0) ||
                    !weaponCatalogRows.Any(x => x != null && x.category == "Ground SAM Missiles") ||
                    !weaponCatalogRows.Any(x => x != null && x.category == "Targeting & Sensor Pods") ||
                    !weaponCatalogRows.Any(x => x != null && x.blk != null && x.blk.IndexOf("us_b28.blk", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    !weaponCatalogRows.Any(x => x != null && x.blk != null && x.blk.IndexOf("su_rds37.blk", StringComparison.OrdinalIgnoreCase) >= 0))
                    throw new InvalidOperationException("Extended weapon catalog self-test failed.");
                List<CombinedMapRowJson> combinedCatalogRows = MainForm.JsonRows<CombinedMapRowJson>("UTL.combined_maps.json");
                List<IGrouping<string, CombinedMapRowJson>> combinedCatalogMaps = combinedCatalogRows
                    .Where(x => x != null && !String.IsNullOrWhiteSpace(x.id))
                    .GroupBy(x => x.id, StringComparer.OrdinalIgnoreCase).ToList();
                if (combinedCatalogMaps.Count != 48 || combinedCatalogMaps.Any(group =>
                    group.Count(x => x.kind == null || !x.kind.Equals("capture", StringComparison.OrdinalIgnoreCase)) != 12 ||
                    group.Count(x => x.kind != null && x.kind.Equals("capture", StringComparison.OrdinalIgnoreCase)) < 2 ||
                    group.Count(x => x.kind != null && x.kind.Equals("capture", StringComparison.OrdinalIgnoreCase)) > 3))
                    throw new InvalidOperationException("Combined map/spawn/marker catalog self-test failed.");
                string countermeasureSource = "bullets:i = 90\nisBulletBelt:b = false\nbullet {\n bulletType:t = \"flr\"\n bulletName:t = \"flare_launcher\"\n rocket { mass:r=0.1 }\n}\nbullet {\n bulletType:t = \"chff\"\n bulletName:t = \"chaffs_launcher\"\n rocket { mass:r=0.01 }\n}\n";
                string customBelt = MainForm.BuildCountermeasureBelt(countermeasureSource, 6, 3);
                if (customBelt.IndexOf("bullets:i = 9", StringComparison.Ordinal) < 0 ||
                    customBelt.IndexOf("isBulletBelt:b = true", StringComparison.Ordinal) < 0 ||
                    Regex.Matches(customBelt, "bulletType:t = \"flr\"").Count != 2 ||
                    Regex.Matches(customBelt, "bulletType:t = \"chff\"").Count != 1)
                    throw new InvalidOperationException("Custom flare/chaff belt self-test failed.");
                string countermeasureFm = "Weapon {\n trigger:t = \"countermeasures\"\n blk:t = \"gameData/Weapons/rocketGuns/countermeasure_split_launcher_jet.blk\"\n bullets:i = 30\n}\nWeapon {\n trigger:t = \"countermeasures\"\n blk:t = \"gameData/Weapons/rocketGuns/countermeasure_large_split_launcher_jet.blk\"\n bullets:i = 60\n}\n";
                AircraftSettings cmSettings = new AircraftSettings { OverrideCountermeasures = true, FlareRounds = 6, ChaffRounds = 3 };
                MainForm.ApplyCountermeasureSettings(ref countermeasureFm, cmSettings, "gameData/Weapons/rocketGuns/utl_cm/small.blk", "gameData/Weapons/rocketGuns/utl_cm/large.blk");
                if (Regex.Matches(countermeasureFm, @"bullets:i = 9").Count != 2 ||
                    countermeasureFm.IndexOf("utl_cm/small.blk", StringComparison.Ordinal) < 0 ||
                    countermeasureFm.IndexOf("utl_cm/large.blk", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Countermeasure launcher override self-test failed.");
                string perLauncherFm = "Weapon {\n trigger:t = \"countermeasures\"\n emitter:t = \"internal\"\n blk:t = \"gameData/Weapons/rocketGuns/countermeasure_split_launcher_jet.blk\"\n bullets:i = 30\n}\nWeapon {\n trigger:t = \"countermeasures\"\n emitter:t = \"bol\"\n blk:t = \"gameData/Weapons/rocketGuns/countermeasure_large_split_launcher_jet.blk\"\n bullets:i = 60\n}\n";
                AircraftSettings stationSettings = new AircraftSettings { OverrideCountermeasures = true };
                stationSettings.CountermeasureLoadouts.Add(new CountermeasureLoadout { Key = "internal", Flares = 8, Chaff = 0 });
                stationSettings.CountermeasureLoadouts.Add(new CountermeasureLoadout { Key = "bol", Flares = 0, Chaff = 12 });
                MainForm.ApplyCountermeasureSettings(ref perLauncherFm, stationSettings, new Dictionary<string, string>());
                if (Regex.Matches(perLauncherFm, @"bullets:i = 8").Count != 1 || Regex.Matches(perLauncherFm, @"bullets:i = 12").Count != 1 ||
                    perLauncherFm.IndexOf("countermeasure_split_launcher_jet.blk", StringComparison.Ordinal) < 0 ||
                    perLauncherFm.IndexOf("countermeasure_chaff_only_large.blk", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Per-launcher countermeasure self-test failed.");
                string upgradedFm = "Weapon {\n trigger:t = \"countermeasures\"\n blk:t = \"gameData/Weapons/rocketGuns/countermeasure_large_split_launcher_jet.blk\"\n bullets:i = 15\n}\nmodifications {\n countermeasures_launcher_chaff {\n }\n countermeasures_belt_pack {\n  group:t = \"countermeasures\"\n }\n}\n";
                if (!MainForm.HasCountermeasureUpgradeModules(upgradedFm) ||
                    MainForm.HasCountermeasureUpgradeModules("modifications {\n M60_air_targets {\n }\n}\n"))
                    throw new InvalidOperationException("Countermeasure module detection self-test failed.");
                string helicopterThermal = "nightVision {\n gunnerIr {\n  resolution:ip2 = 800, 600\n }\n}\nmodifications {\n heli_night_vision_system {\n  effects {\n   nightVision {\n    sightThermal {\n     resolution:ip2 = 800, 600\n    }\n   }\n  }\n }\n}\n";
                MainForm.MaterializeHelicopterThermalSight(ref helicopterThermal, new AircraftSettings { UseAllModifications = true });
                BlockSpan activeThermalVision = BlkTools.FirstBlock(helicopterThermal, "nightVision", 0);
                if (activeThermalVision == null || BlkTools.FirstBlock(activeThermalVision.Text, "sightThermal", 0) == null)
                    throw new InvalidOperationException("Helicopter thermal-sight activation self-test failed.");
                AircraftSettings presetSettings = new AircraftSettings
                {
                    UseAllModifications = false, OverrideCountermeasures = true, FlareRounds = 36, ChaffRounds = 18,
                    UnlimitedCountermeasures = false,
                    FullFuel = false, FuelMinutes = 25,
                    UserSightPath = @"C:\Users\Tester\Documents\My Games\WarThunder\Saves\1\production\UserSights\all_tanks\AstraSEP_fixed.blk"
                };
                presetSettings.EnabledModifications.Add("yak9ut_ns45_mod");
                presetSettings.CountermeasureLoadouts.Add(new CountermeasureLoadout { Key = "emtr_flare1", Flares = 24, Chaff = 8 });
                presetSettings.GunBeltSelections[0] = "bk_27_air_targets";
                AircraftSettings restoredSettings = PresetStore.DeserializeSettings(PresetStore.SerializeSettings(presetSettings));
                if (restoredSettings == null || restoredSettings.UseAllModifications || !restoredSettings.OverrideCountermeasures ||
                    restoredSettings.FlareRounds != 36 || restoredSettings.ChaffRounds != 18 ||
                    !restoredSettings.EnabledModifications.Contains("yak9ut_ns45_mod") ||
                    restoredSettings.FullFuel || restoredSettings.FuelMinutes != 25 || restoredSettings.CountermeasureLoadouts.Count != 1 ||
                    restoredSettings.CountermeasureLoadouts[0].Key != "emtr_flare1" || restoredSettings.CountermeasureLoadouts[0].Flares != 24 ||
                    restoredSettings.CountermeasureLoadouts[0].Chaff != 8 || restoredSettings.GunBeltSelections.Count != 1 ||
                    restoredSettings.GunBeltSelections[0] != "bk_27_air_targets" || restoredSettings.UserSightPath != presetSettings.UserSightPath)
                    throw new InvalidOperationException("Preset aircraft-settings self-test failed.");
                string fpv = MainForm.BuildDownloadedFpvVariant("model:t = \"uav_quadcopter\"\nweapon_presets {\n}\n", "warhead {\n\tmass:r = 2.6\n}\n");
                if (fpv.IndexOf("model:t = \"uav_quadcopter\"", StringComparison.Ordinal) < 0 ||
                    fpv.IndexOf("humanDrone:b = true", StringComparison.Ordinal) < 0 ||
                    fpv.IndexOf("hasFPVCamera:b = true", StringComparison.Ordinal) < 0 ||
                    fpv.IndexOf("mass:r = 2.6", StringComparison.Ordinal) < 0 ||
                    fpv.Count(c => c == '{') != fpv.Count(c => c == '}'))
                    throw new InvalidOperationException("Downloaded FPV compatibility self-test failed.");
                Console.WriteLine("SELFTEST OK aircraft={0} ground-vehicles=yes ground-ammo=yes ground-user-sights=yes ground-pkg-local=yes stable-mission=yes instant-respawn=yes rapid-target-recovery=yes helicopters=yes heli-thermal=yes modifications=yes countermeasures=yes gun-belts=yes native-preset-order=yes preset-settings=yes weapons={1} native-nuclear=yes fpv-impact=yes clean-menu=yes f2-injected=yes pods=yes ground-sam=yes legacy-fm=yes adaptive-spawn=yes vrom-paths=yes", MainForm.JsonRows<AircraftRowJson>("UTL.aircraft.json").Count, MainForm.JsonRows<DonorWeaponRowJson>("UTL.weapon_catalog.json").Count);
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ModernUi.Run();
        }

        private static int LinesForTest(string resource)
        {
            return Embedded.Text(resource).Replace("\r", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}
