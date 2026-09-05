// ModernShell.Ui.cs
// Static UI helpers: renderers, WPF self-test entry and screenshot drivers.
// Split from ModernShell.cs during the 2026-09-05 partial-class refactor; members are byte-identical.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using Microsoft.Win32;

namespace UniversalTestLab
{
    internal static class ModernUi
    {
        public static void Run()
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;
            app.Run(new ModernMainWindow());
        }

        public static void RenderMain(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderMainMaximized(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow { Width = 1920, Height = 1040, WindowStartupLocation = WindowStartupLocation.Manual, Left = 0, Top = 0 };
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderExperimental(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.SelectWorkspaceTabForScreenshot(4);
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderTargets(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.SelectWorkspaceTabForScreenshot(1);
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderGarage(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.SelectWorkspaceTabForScreenshot(3);
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderOptions(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.SelectWorkspaceTabForScreenshot(2);
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderMainKind(string path, string kind)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.SelectVehicleKindForScreenshot(kind);
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderGroundPreset(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.ShowGroundPresetForScreenshot();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderMessage(string path, bool danger)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.ShowMessageForScreenshot(danger);
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderWeaponScrollbar(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.EnableInjectionForScreenshot();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderSettings(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            Aircraft sample = new Aircraft { Id = "ef_2000_aesa", Display = "EF-2000 Typhoon (AESA)", Kind = "Aircraft", Nation = "Great Britain", Rank = 9 };
            List<AircraftModification> sampleMods = new List<AircraftModification>();
            foreach (ModificationRowJson r in MainForm.JsonRows<ModificationRowJson>("UTL.modifications.json"))
            {
                if (r != null && r.aircraftId != null && r.aircraftId.Equals(sample.Id, StringComparison.OrdinalIgnoreCase) && !String.IsNullOrWhiteSpace(r.id))
                    sampleMods.Add(new AircraftModification { AircraftId = r.aircraftId, Id = r.id, Display = r.display, Tier = r.tier, ModClass = r.modClass, Group = r.group, Requires = r.requires });
            }
            ModernFlightSystemsWindow window = new ModernFlightSystemsWindow(sample, sampleMods, new AircraftSettings(), false);
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = 0;
            window.Top = 0;
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            window.Close();
            app.Shutdown();
        }

        public static void RenderGroundConfigure(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            Aircraft sample = new Aircraft
            {
                Id = "us_m1a2_sep2_abrams", Display = "M1A2 SEP V2", Kind = "Ground Vehicle", Nation = "USA", Rank = 8,
                MainWeaponBlk = "gameData/Weapons/groundModels_weapons/120mm_M256_M1A3_user_cannon.blk", MaxAmmo = 42,
                NativeMass = 54000, NativeEnginePower = 1519, NativeForwardSpeed = 75, NativeReverseSpeed = 10, NativeReloadSeconds = 5, NativeRecoil = 0.5
            };
            List<GroundAmmo> ammo = new List<GroundAmmo>();
            string groundAmmoJsonText = Embedded.Text("UTL.ground_ammo.json");
            if (!String.IsNullOrWhiteSpace(groundAmmoJsonText))
            {
                try
                {
                    System.Web.Script.Serialization.JavaScriptSerializer gaSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    gaSerializer.MaxJsonLength = int.MaxValue;
                    List<GroundAmmoJson> ammoList = gaSerializer.Deserialize<List<GroundAmmoJson>>(groundAmmoJsonText);
                    if (ammoList != null)
                    {
                        foreach (GroundAmmoJson ga in ammoList)
                        {
                            if (ga == null || String.IsNullOrWhiteSpace(ga.source)) continue;
                            if (!ga.source.Equals(sample.MainWeaponBlk, StringComparison.OrdinalIgnoreCase)) continue;
                            ammo.Add(new GroundAmmo { SourceBlk = ga.source, Container = ga.container ?? "", BulletName = ga.bulletName, Display = ga.display ?? "", Type = ga.kind ?? "", Mass = ga.mass, Speed = ga.speed, ExplosiveMass = ga.explosive, Caliber = ga.caliber, Penetration = ga.penetration });
                        }
                    }
                }
                catch { }
            }
            AircraftSettings settings = new AircraftSettings();
            if (ammo.Count > 0) settings.GroundAmmoLoadouts.Add(new GroundAmmoLoadout { Slot = 0, Count = 31, SourceBlk = ammo[0].SourceBlk, BulletName = ammo[0].BulletName });
            if (ammo.Count > 1) settings.GroundAmmoLoadouts.Add(new GroundAmmoLoadout { Slot = 1, Count = 9, SourceBlk = ammo[1].SourceBlk, BulletName = ammo[1].BulletName });
            ModernGroundConfigureWindow window = new ModernGroundConfigureWindow(sample, settings, ammo, new TargetUnit[0], new UnitWeapon[0], new GroundWeaponInfo[0], new GroundAmmo[0], new GroundWeaponBeltOption[0], null);
            window.WindowStartupLocation = WindowStartupLocation.Manual; window.Left = 0; window.Top = 0; window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path); window.Close(); app.Shutdown();
        }

        public static void RenderFlightConfigure(string path) { RenderFlightConfigure(path, false); }

        public static void RenderFlightConfigureBottom(string path) { RenderFlightConfigure(path, true); }

        private static void RenderFlightConfigure(string path, bool bottom)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            Aircraft sample = new Aircraft { Id = "ef_2000_aesa", Display = "EF-2000 Typhoon (AESA)", Kind = "Aircraft", Nation = "Great Britain", Rank = 9 };
            List<CountermeasureLauncher> launchers = new List<CountermeasureLauncher>
            {
                new CountermeasureLauncher { Key = "emtr_flare1", Display = "INTERNAL COUNTERMEASURE DISPENSER", NativeRounds = 32, AllowsFlares = true, AllowsChaff = true },
                new CountermeasureLauncher { Key = "emtr_flare3", Display = "BOL COUNTERMEASURE DISPENSER", NativeRounds = 160, AllowsFlares = true, AllowsChaff = false }
            };
            List<AircraftModification> beltMods = new List<AircraftModification>
            {
                new AircraftModification { AircraftId = sample.Id, Id = "bk_27_air_targets", Display = "Air targets", Tier = 0 },
                new AircraftModification { AircraftId = sample.Id, Id = "bk_27_ground_targets", Display = "Ground targets", Tier = 0 },
                new AircraftModification { AircraftId = sample.Id, Id = "bk_27_stealth", Display = "Stealth", Tier = 0 },
                new AircraftModification { AircraftId = sample.Id, Id = "bk_27_belt_pack", Display = "BK 27 Belt Pack", Tier = 1 }
            };
            ModernFlightConfigureWindow window = new ModernFlightConfigureWindow(sample, new AircraftSettings { FullFuel = false, FuelMinutes = 30, OverrideCountermeasures = true }, launchers, beltMods);
            window.WindowStartupLocation = WindowStartupLocation.Manual; window.Left = 0; window.Top = 0; window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            if (bottom) { window.ScrollToEndForScreenshot(); window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle); }
            RenderWindow(window, path); window.Close(); app.Shutdown();
        }

        public static void RenderMap(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            AircraftView air = new AircraftView(new Aircraft { Id = "j_10c", Display = "J-10C", Nation = "China", Kind = "Aircraft", Rank = 9 });
            TargetView ground = new TargetView(new TargetUnit { Id = "ussr_bmpt", Display = "BMPT" });
            TargetView ship = new TargetView(new TargetUnit { Id = "jp_battleship_yamato", Display = "Yamato-class, IJN Yamato, 1945" });
            CombinedMap map = new CombinedMap { Id = "western_europe", Display = "Western Europe", Level = "levels/avg_western_europe.bin" };
            map.Spawns.Add(new CombinedSpawn { Kind = "aircraft", Side = 1, Option = "airfield", Label = "Airfield" });
            map.Spawns.Add(new CombinedSpawn { Kind = "aircraft", Side = 1, Option = "air", Label = "Air spawn" });
            ModernMapWindow window = new ModernMapWindow(new[] { air }, new[] { ground }, new[] { ship }, air, 1, new[] { ground }, true, "active", "all", ship, 1, false,
                new[] { map }, "aircraft", new CombinedScenarioSettings { Enabled = true, MapId = map.Id, Side = 1, SpawnOption = "airfield" }, null, 1, null, 1, null, 1);
            window.WindowStartupLocation = WindowStartupLocation.Manual; window.Left = 0; window.Top = 0; window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path); window.Close(); app.Shutdown();
        }

        public static void RenderGenerated(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMissionGeneratedWindow window = new ModernMissionGeneratedWindow(); window.WindowStartupLocation = WindowStartupLocation.Manual; window.Left = 0; window.Top = 0; window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path); window.Close(); app.Shutdown();
        }

        public static void RenderAbout(string path)
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            ModernAboutWindow about = new ModernAboutWindow(2817, 1838) { Owner = window };
            window.ShowOverlay(about);
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            RenderWindow(window, path);
            about.Close();
            window.Close();
            app.Shutdown();
        }

        public static void SelfTest()
        {
            DwmGlass.EnablePerMonitorDpi();
            System.Windows.Application app = new System.Windows.Application();
            ModernMainWindow window = new ModernMainWindow();
            window.Show();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            window.ExerciseDropdownForSelfTest();
            // The pylon/weapon layout assertions below need an aircraft selected so the
            // station panel is populated. The window restores the last session vehicle
            // on startup, which is often a ground vehicle (no pylon stations), so pin
            // a fixed-wing aircraft through the controller and refresh the pylon/weapon
            // panels explicitly (selection events are not guaranteed to fire yet).
            window.SelectFirstFixedWingForSelfTest();
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            if (!window.LayoutFixesReadyForSelfTest())
                throw new InvalidOperationException("WPF clipping/dropdown/work-area self-test failed.");
            if (!window.CombinedCatalogReadyForSelfTest())
                throw new InvalidOperationException("WPF combined-battles map catalog self-test failed.");
            if (!window.ExerciseOverlayForSelfTest())
                throw new InvalidOperationException("WPF single-window overlay self-test failed.");

            Aircraft modulesVehicle = new Aircraft { Id = "selftest_modules", Display = "Self-test Vehicle", Kind = "Aircraft", Nation = "USA", Rank = 1 };
            ModernFlightSystemsWindow modules = new ModernFlightSystemsWindow(modulesVehicle,
                new[] { new AircraftModification { AircraftId = modulesVehicle.Id, Id = "engine", Display = "Engine", Tier = 1 } },
                new AircraftSettings(), false);
            if (!modules.ModulesCardReadyForSelfTest())
                throw new InvalidOperationException("WPF Modules glass-card self-test failed.");

            Aircraft groundVehicle = new Aircraft { Id = "selftest_ground", Display = "M1A2 SEP V3", Kind = "Ground Vehicle", Nation = "USA", Rank = 8, MaxAmmo = 42, MainWeaponBlk = "selftest.blk", NativeReloadSeconds = 5 };
            GroundAmmo groundRound = new GroundAmmo { SourceBlk = "selftest.blk", BulletName = "round", Display = "Test Round", Type = "APFSDS", Mass = 5, Speed = 1500 };
            AircraftSettings groundSettings = new AircraftSettings();
            groundSettings.GroundAmmoLoadouts.Add(new GroundAmmoLoadout { Slot = 0, Count = 31, SourceBlk = groundRound.SourceBlk, BulletName = groundRound.BulletName });
            groundSettings.GroundAmmoLoadouts.Add(new GroundAmmoLoadout { Slot = 1, Count = 9, SourceBlk = groundRound.SourceBlk, BulletName = groundRound.BulletName });
            ModernGroundConfigureWindow groundConfigure = new ModernGroundConfigureWindow(groundVehicle, groundSettings, new[] { groundRound }, new TargetUnit[0], new UnitWeapon[0], new GroundWeaponInfo[0], new GroundAmmo[0], new GroundWeaponBeltOption[0], null);
            if (!groundConfigure.AmmoSlidersStableForSelfTest())
                throw new InvalidOperationException("WPF ground-ammunition slider self-test failed.");

            window.WindowState = WindowState.Maximized;
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            if (window.WindowState != WindowState.Maximized || window.ActualWidth < 1100 || window.ActualHeight < 600)
                throw new InvalidOperationException("WPF maximize/layout self-test failed.");
            window.WindowState = WindowState.Normal;
            window.Width = 1500;
            window.Height = 920;
            window.Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            if (window.WindowState != WindowState.Normal || window.ActualWidth < 1200 || window.ActualHeight < 640)
                throw new InvalidOperationException("WPF restore/layout self-test failed.");
            ModernAboutWindow about = new ModernAboutWindow(2817, 1838);
            about.Close();
            window.Close();
            app.Shutdown();
            Console.WriteLine("UISELFTEST OK wpf=yes custom-chrome=yes no-client-gap=yes dark-glass=yes dark-dropdowns=yes grouped-weapons=yes rounded-preview=yes vehicle-kind-previews=yes border-retention=yes weapon-table-fit=yes station-order=yes stations-one-row=yes vertical-scroll=yes single-window-overlays=yes styled-messages=yes solid-close=yes visible-game-path=yes blurred-background=yes work-area-fit=yes maximize-restore=yes dpi-aware=yes");
        }

        internal static void RenderWindow(Window window, string path)
        {
            int width = Math.Max(1, (int)Math.Round(window.ActualWidth));
            int height = Math.Max(1, (int)Math.Round(window.ActualHeight));
            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (FileStream stream = File.Create(path)) encoder.Save(stream);
        }

        internal static void RenderWindow(ModernDialogWindow dialog, string path)
        {
            int width = Math.Max(1, (int)Math.Round(dialog.ActualWidth));
            int height = Math.Max(1, (int)Math.Round(dialog.ActualHeight));
            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(dialog);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (FileStream stream = File.Create(path)) encoder.Save(stream);
        }
    }
}
