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

[assembly: AssemblyTitle("Universal Test Lab")]
[assembly: AssemblyProduct("Universal Test Lab")]
[assembly: AssemblyDescription("War Thunder User Mission and vehicle test workspace (public beta)")]
[assembly: AssemblyCompany("Vanilla Wong")]
[assembly: AssemblyVersion("0.12.0.0")]
[assembly: AssemblyFileVersion("0.12.0.2")]
[assembly: AssemblyInformationalVersion("0.12.0-beta.2")]

namespace UniversalTestLab
{
    internal static class Theme
    {
        public static readonly Color Window = Color.FromArgb(10, 12, 34);
        public static readonly Color Surface = Color.FromArgb(22, 25, 57);
        public static readonly Color Surface2 = Color.FromArgb(29, 34, 75);
        public static readonly Color Surface3 = Color.FromArgb(42, 47, 94);
        public static readonly Color Field = Color.FromArgb(17, 21, 50);
        public static readonly Color Border = Color.FromArgb(68, 76, 133);
        public static readonly Color Text = Color.FromArgb(242, 245, 255);
        public static readonly Color Muted = Color.FromArgb(157, 168, 205);
        public static readonly Color Accent = Color.FromArgb(126, 91, 255);
        public static readonly Color AccentLight = Color.FromArgb(77, 213, 255);
        public static readonly Color AccentDark = Color.FromArgb(82, 58, 203);
        public static readonly Color Good = Color.FromArgb(72, 222, 179);
        public static readonly Color Danger = Color.FromArgb(255, 91, 139);

        public static void Button(Button button, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = primary ? Accent : Border;
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(111, 79, 237) : Color.FromArgb(52, 59, 113);
            button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(69, 48, 181) : Color.FromArgb(34, 39, 82);
            button.BackColor = primary ? AccentDark : Surface3;
            button.ForeColor = Text;
            button.Cursor = Cursors.Hand;
            button.Font = new Font("Segoe UI Semibold", 8.7f);
            button.Padding = new Padding(4, 0, 4, 0);
            Round(button, 9);
        }

        public static void Input(Control control)
        {
            control.BackColor = Field;
            control.ForeColor = Text;
        }

        public static Label Label(string text, bool title)
        {
            return new Label
            {
                Text = text,
                AutoSize = false,
                Dock = DockStyle.Fill,
                ForeColor = title ? Text : Muted,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = title ? new Font("Segoe UI Semibold", 10.5f) : new Font("Segoe UI", 9f)
            };
        }

        public static Control StepHeader(string step, string title, string subtitle)
        {
            TableLayoutPanel header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Label badge = new Label
            {
                Text = step,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 1, 10, 6),
                BackColor = AccentDark,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Semibold", 9f)
            };
            Round(badge, 10);
            header.Controls.Add(badge, 0, 0);
            header.SetRowSpan(badge, 2);
            Label heading = Label(title, true);
            heading.Font = new Font("Segoe UI Semibold", 11.5f);
            header.Controls.Add(heading, 1, 0);
            Label detail = Label(subtitle, false);
            detail.Font = new Font("Segoe UI", 8.3f);
            header.Controls.Add(detail, 1, 1);
            return header;
        }

        public static void Toggle(CheckBox toggle)
        {
            toggle.FlatStyle = FlatStyle.Flat;
            toggle.FlatAppearance.BorderSize = 1;
            toggle.FlatAppearance.BorderColor = Border;
            toggle.FlatAppearance.CheckedBackColor = AccentDark;
            toggle.FlatAppearance.MouseOverBackColor = Surface3;
            toggle.BackColor = Surface2;
            toggle.ForeColor = Text;
            toggle.Cursor = Cursors.Hand;
            toggle.Font = new Font("Segoe UI Semibold", 8.5f);
            Round(toggle, 9);
        }

        public static void Round(Control control, int radius)
        {
            Action update = delegate
            {
                if (control.Width <= 0 || control.Height <= 0) return;
                using (GraphicsPath path = RoundedPath(new Rectangle(0, 0, control.Width, control.Height), radius))
                {
                    Region old = control.Region;
                    control.Region = new Region(path);
                    if (old != null) old.Dispose();
                }
            };
            control.Resize += delegate { update(); };
            update();
        }

        public static GraphicsPath RoundedPath(Rectangle rectangle, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = Math.Max(2, Math.Min(radius * 2, Math.Min(rectangle.Width, rectangle.Height)));
            Rectangle arc = new Rectangle(rectangle.X, rectangle.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = rectangle.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rectangle.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rectangle.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class GradientBackdrop : Panel
    {
        public GradientBackdrop()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Theme.Window;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Rectangle area = ClientRectangle;
            if (area.Width <= 0 || area.Height <= 0) return;
            using (LinearGradientBrush background = new LinearGradientBrush(area, Color.FromArgb(20, 18, 57), Theme.Window, 24f))
                e.Graphics.FillRectangle(background, area);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            DrawGlow(e.Graphics, new Rectangle(-area.Width / 7, -area.Height / 3, area.Width * 2 / 3, area.Height), Color.FromArgb(72, 75, 111, 255));
            DrawGlow(e.Graphics, new Rectangle(area.Width * 2 / 3, -area.Height / 4, area.Width / 2, area.Height * 3 / 4), Color.FromArgb(62, 52, 210, 255));
            DrawGlow(e.Graphics, new Rectangle(area.Width / 3, area.Height * 3 / 4, area.Width / 2, area.Height / 2), Color.FromArgb(34, 58, 217, 255));
        }

        private static void DrawGlow(Graphics graphics, Rectangle bounds, Color center)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(bounds);
                using (PathGradientBrush glow = new PathGradientBrush(path))
                {
                    glow.CenterColor = center;
                    glow.SurroundColors = new[] { Color.FromArgb(0, center.R, center.G, center.B) };
                    graphics.FillEllipse(glow, bounds);
                }
            }
        }
    }

    internal sealed class GlassPanel : Panel
    {
        public bool HeaderStyle;

        public GlassPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.Transparent;
            Margin = new Padding(6);
            Padding = new Padding(1);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            Rectangle bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using (GraphicsPath path = Theme.RoundedPath(bounds, HeaderStyle ? 15 : 18))
            using (LinearGradientBrush glass = new LinearGradientBrush(bounds,
                HeaderStyle ? Color.FromArgb(238, 25, 28, 66) : Color.FromArgb(242, 24, 28, 66),
                HeaderStyle ? Color.FromArgb(228, 15, 18, 48) : Color.FromArgb(238, 17, 20, 51), 90f))
            using (Pen border = new Pen(Color.FromArgb(170, Theme.Border), 1f))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(glass, path);
                e.Graphics.DrawPath(border, path);
            }
        }
    }

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
        public bool UnlimitedAmmo;
        public bool FakeArhConversion;
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
                UnlimitedAmmo = UnlimitedAmmo,
                FakeArhConversion = FakeArhConversion
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

    // Catalog JSON row DTOs (mirror tools/tsv2json.js schemas; camelCase keys match
    // JavaScriptSerializer property binding).
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

    // One of the template's flying hostiles (Target_Air_01 / Target_Air_02 /
    // Heli_Target / Heli_Target_02). UnitName is the template armada name;
    // an empty AircraftId keeps the template default vehicle.
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

    internal sealed class MissionSettings
    {
        public double PlayerRespawnDelaySeconds;
        public double TargetRespawnDelaySeconds = 0.25;
        public double RearmSeconds = 1.0;
        public bool LimitedAmmo;
        public bool RapidFireEnabled;
        public double RapidFireInterval = 0.5;
        public bool RapidFireFullRestore = true;
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

        
        public static string AccelerateRangeRecovery(string text, bool includeRangeRecovery, double targetRespawnDelay = 0.25, double rearmSeconds = 1.0)
        {
            string respawnDelayText = targetRespawnDelay.ToString("0.###", CultureInfo.InvariantCulture);
            string rearmText = rearmSeconds.ToString("0.###", CultureInfo.InvariantCulture);
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
            string extras = @"
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

    internal sealed class AircraftPreview : Panel
    {
        public Aircraft Aircraft;

        public AircraftPreview()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Rectangle rect = new Rectangle(0, 0, Math.Max(1, ClientRectangle.Width - 1), Math.Max(1, ClientRectangle.Height - 1));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath backgroundPath = Theme.RoundedPath(rect, 15))
            using (LinearGradientBrush bg = new LinearGradientBrush(rect, Color.FromArgb(69, 57, 142), Theme.Field, 58f))
            using (Pen border = new Pen(Color.FromArgb(175, Theme.Border), 1f))
            {
                e.Graphics.FillPath(bg, backgroundPath);
                e.Graphics.DrawPath(border, backgroundPath);
            }
            int cx = rect.Width / 2;
            int cy = Math.Max(55, rect.Height / 2 - 4);
            Point[] silhouette =
            {
                new Point(cx, cy - 47), new Point(cx + 9, cy - 12), new Point(cx + 73, cy + 16),
                new Point(cx + 77, cy + 25), new Point(cx + 18, cy + 15), new Point(cx + 13, cy + 42),
                new Point(cx + 30, cy + 51), new Point(cx + 29, cy + 57), new Point(cx, cy + 50),
                new Point(cx - 29, cy + 57), new Point(cx - 30, cy + 51), new Point(cx - 13, cy + 42),
                new Point(cx - 18, cy + 15), new Point(cx - 77, cy + 25), new Point(cx - 73, cy + 16),
                new Point(cx - 9, cy - 12)
            };
            using (SolidBrush halo = new SolidBrush(Color.FromArgb(26, Theme.AccentLight))) e.Graphics.FillEllipse(halo, cx - 82, cy - 58, 164, 116);
            using (SolidBrush plane = new SolidBrush(Color.FromArgb(125, 177, 201, 231))) e.Graphics.FillPolygon(plane, silhouette);
            using (Pen edge = new Pen(Color.FromArgb(210, Theme.AccentLight), 1.5f)) e.Graphics.DrawPolygon(edge, silhouette);
            Rectangle captionBounds = new Rectangle(1, Math.Max(1, rect.Height - 58), Math.Max(1, rect.Width - 1), 57);
            using (LinearGradientBrush caption = new LinearGradientBrush(captionBounds, Color.FromArgb(220, Theme.Field), Color.FromArgb(248, Theme.Field), 90f))
                e.Graphics.FillRectangle(caption, captionBounds);
            string title = Aircraft == null ? "SELECT AN AIR VEHICLE" : Aircraft.Display.ToUpperInvariant();
            string meta = Aircraft == null ? "" : (Aircraft.Kind ?? "Aircraft").ToUpperInvariant() + "   •   " + Aircraft.Nation.ToUpperInvariant() + "   •   RANK " + Roman(Aircraft.Rank);
            using (Font titleFont = new Font("Segoe UI Semibold", 12f))
            using (Font metaFont = new Font("Segoe UI", 8.5f))
            using (SolidBrush white = new SolidBrush(Theme.Text))
            using (SolidBrush muted = new SolidBrush(Theme.Muted))
            {
                e.Graphics.DrawString(title, titleFont, white, new RectangleF(12, rect.Height - 52, rect.Width - 24, 25));
                e.Graphics.DrawString(meta, metaFont, muted, new RectangleF(12, rect.Height - 28, rect.Width - 24, 20));
            }
        }

        private static string Roman(int rank)
        {
            string[] values = { "—", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
            return rank >= 0 && rank < values.Length ? values[rank] : rank.ToString(CultureInfo.InvariantCulture);
        }
    }

    internal sealed class AircraftSettingsForm : Form
    {
        private readonly List<AircraftModification> definitions;
        private readonly CheckBox allModifications;
        private readonly CheckedListBox modificationList;
        private readonly CheckBox overrideCountermeasures;
        private readonly NumericUpDown flareRounds;
        private readonly NumericUpDown chaffRounds;
        private bool suppressGroupRules;

        public AircraftSettings Result { get; private set; }

        public AircraftSettingsForm(Aircraft item, IEnumerable<AircraftModification> modifications, AircraftSettings current, bool helicopter)
        {
            definitions = modifications.OrderBy(x => x.Tier).ThenBy(x => x.Display).ThenBy(x => x.Id).ToList();
            AircraftSettings settings = (current ?? new AircraftSettings()).Copy();
            Text = "MODIFICATIONS & COUNTERMEASURES — " + item.Display;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(780, 620);
            Size = new Size(940, 780);
            BackColor = Theme.Window;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.2f);

            GradientBackdrop backdrop = new GradientBackdrop { Dock = DockStyle.Fill };
            Controls.Add(backdrop);
            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(18), BackColor = Color.Transparent };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            backdrop.Controls.Add(root);
            root.Controls.Add(Theme.StepHeader("MD", "MODULES", item.Display + "  •  research modules, countermeasures and weapon handling"), 0, 0);

            TabControl tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.FlatButtons,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(277, 36),
                Padding = new Point(12, 4)
            };
            tabs.DrawItem += delegate(object sender, DrawItemEventArgs e)
            {
                bool selectedTab = e.Index == tabs.SelectedIndex;
                Rectangle bounds = e.Bounds;
                using (SolidBrush fill = new SolidBrush(selectedTab ? Theme.AccentDark : Theme.Surface2)) e.Graphics.FillRectangle(fill, bounds);
                TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, new Font("Segoe UI Semibold", 8.5f), bounds,
                    selectedTab ? Theme.Text : Theme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            };
            TabPage modulesTab = new TabPage("MODIFICATIONS") { BackColor = Theme.Surface, ForeColor = Theme.Text, Padding = new Padding(16) };
            TabPage countermeasuresTab = new TabPage("COUNTERMEASURES") { BackColor = Theme.Surface, ForeColor = Theme.Text, Padding = new Padding(16) };
            tabs.TabPages.Add(modulesTab);
            tabs.TabPages.Add(countermeasuresTab);
            root.Controls.Add(tabs, 0, 1);

            TableLayoutPanel moduleGrid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, BackColor = Theme.Surface };
            moduleGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            moduleGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            moduleGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            moduleGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            moduleGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            moduleGrid.Controls.Add(Theme.Label("RESEARCH MODIFICATIONS", true), 0, 0);
            allModifications = new CheckBox
            {
                Text = " ENABLE ALL RESEARCH MODIFICATIONS (CURRENT DEFAULT)", Dock = DockStyle.Fill,
                ForeColor = Theme.Accent, BackColor = Theme.Surface2, Appearance = Appearance.Button,
                TextAlign = ContentAlignment.MiddleLeft, FlatStyle = FlatStyle.Flat, Checked = settings.UseAllModifications
            };
            allModifications.FlatAppearance.BorderColor = Theme.Border;
            allModifications.FlatAppearance.CheckedBackColor = Theme.AccentDark;
            Theme.Toggle(allModifications);
            allModifications.ForeColor = Theme.AccentLight;
            moduleGrid.Controls.Add(allModifications, 0, 1);
            Label moduleHint = Theme.Label("Turn off the default above to build a stock or selective aircraft. Items in the same alternative group are mutually exclusive; choosing one clears the other.", false);
            moduleHint.Padding = new Padding(4, 4, 4, 4);
            moduleGrid.Controls.Add(moduleHint, 0, 2);
            modificationList = new CheckedListBox
            {
                Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false,
                BorderStyle = BorderStyle.None, BackColor = Theme.Field, ForeColor = Theme.Text
            };
            foreach (AircraftModification definition in definitions)
            {
                int index = modificationList.Items.Add(definition);
                if (!settings.UseAllModifications && settings.EnabledModifications.Contains(definition.Id))
                    modificationList.SetItemChecked(index, true);
            }
            modificationList.ItemCheck += ModificationItemCheck;
            moduleGrid.Controls.Add(modificationList, 0, 3);
            TableLayoutPanel moduleButtons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            moduleButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            moduleButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            moduleButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            moduleButtons.Controls.Add(Theme.Label(definitions.Count.ToString(CultureInfo.InvariantCulture) + " modules found in the current game files", false), 0, 0);
            Button selectCompatible = new Button { Text = "SELECT TOP SET", Dock = DockStyle.Fill, Margin = new Padding(5) };
            Theme.Button(selectCompatible, false);
            selectCompatible.Click += delegate { SelectCompatibleSet(); };
            moduleButtons.Controls.Add(selectCompatible, 1, 0);
            Button clear = new Button { Text = "CLEAR", Dock = DockStyle.Fill, Margin = new Padding(5) };
            Theme.Button(clear, false);
            clear.Click += delegate { SetAllModificationChecks(false); };
            moduleButtons.Controls.Add(clear, 2, 0);
            moduleGrid.Controls.Add(moduleButtons, 0, 4);
            modulesTab.Controls.Add(moduleGrid);
            allModifications.CheckedChanged += delegate { UpdateModificationControls(); };
            UpdateModificationControls();

            TableLayoutPanel cmGrid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 7, ColumnCount = 1, BackColor = Theme.Surface };
            cmGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            cmGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            cmGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            cmGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            cmGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            cmGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            cmGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cmGrid.Controls.Add(Theme.Label("FLARES & CHAFF", true), 0, 0);
            overrideCountermeasures = new CheckBox
            {
                Text = " OVERRIDE COUNTERMEASURE SETTINGS", Dock = DockStyle.Fill,
                ForeColor = Theme.Accent, BackColor = Theme.Surface2, Appearance = Appearance.Button,
                TextAlign = ContentAlignment.MiddleLeft, FlatStyle = FlatStyle.Flat, Checked = settings.OverrideCountermeasures
            };
            overrideCountermeasures.FlatAppearance.BorderColor = Theme.Border;
            overrideCountermeasures.FlatAppearance.CheckedBackColor = Theme.AccentDark;
            Theme.Toggle(overrideCountermeasures);
            overrideCountermeasures.ForeColor = Theme.AccentLight;
            cmGrid.Controls.Add(overrideCountermeasures, 0, 1);
            cmGrid.Controls.Add(Theme.Label("FLARES PER INSTALLED LAUNCHER", false), 0, 2);
            flareRounds = new NumericUpDown
            {
                Dock = DockStyle.Fill, Minimum = 0, Maximum = 512, Value = Math.Max(0, Math.Min(512, settings.FlareRounds)),
                BackColor = Theme.Surface2, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle, ThousandsSeparator = true
            };
            cmGrid.Controls.Add(flareRounds, 0, 3);
            cmGrid.Controls.Add(Theme.Label("CHAFF PER INSTALLED LAUNCHER", false), 0, 4);
            chaffRounds = new NumericUpDown
            {
                Dock = DockStyle.Fill, Minimum = 0, Maximum = 512, Value = Math.Max(0, Math.Min(512, settings.ChaffRounds)),
                BackColor = Theme.Surface2, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle, ThousandsSeparator = true
            };
            cmGrid.Controls.Add(chaffRounds, 0, 5);
            Label cmHint = Theme.Label("BOL, BKO and external dispenser modules still control which launchers exist. A mixed custom belt is generated with the exact flare/chaff ratio for every installed launcher. Set one value to 0 for a single-type load.", false);
            cmHint.Padding = new Padding(4, 12, 4, 4);
            cmGrid.Controls.Add(cmHint, 0, 6);
            countermeasuresTab.Controls.Add(cmGrid);
            overrideCountermeasures.CheckedChanged += delegate { UpdateCountermeasureControls(); };
            UpdateCountermeasureControls();

            TableLayoutPanel actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(0, 8, 0, 0) };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            actions.Controls.Add(Theme.Label("Settings stay with this vehicle and are included in saved presets.", false), 0, 0);
            Button cancel = new Button { Text = "CANCEL", Dock = DockStyle.Fill, Margin = new Padding(5, 0, 5, 0), DialogResult = DialogResult.Cancel };
            Theme.Button(cancel, false);
            actions.Controls.Add(cancel, 1, 0);
            Button save = new Button { Text = "APPLY SETTINGS", Dock = DockStyle.Fill, Margin = new Padding(5, 0, 0, 0) };
            Theme.Button(save, true);
            save.Click += delegate { SaveResult(); };
            actions.Controls.Add(save, 2, 0);
            root.Controls.Add(actions, 0, 2);
            AcceptButton = save;
            CancelButton = cancel;
        }

        private void ModificationItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (suppressGroupRules || e.NewValue != CheckState.Checked) return;
            AircraftModification selected = definitions[e.Index];
            if (String.IsNullOrWhiteSpace(selected.Group)) return;
            BeginInvoke((MethodInvoker)delegate
            {
                suppressGroupRules = true;
                try
                {
                    for (int i = 0; i < definitions.Count; i++)
                        if (i != e.Index && String.Equals(definitions[i].Group, selected.Group, StringComparison.OrdinalIgnoreCase))
                            modificationList.SetItemChecked(i, false);
                }
                finally { suppressGroupRules = false; }
            });
        }

        private void UpdateModificationControls()
        {
            modificationList.Enabled = !allModifications.Checked;
        }

        private void SetAllModificationChecks(bool value)
        {
            allModifications.Checked = false;
            suppressGroupRules = true;
            try
            {
                for (int i = 0; i < modificationList.Items.Count; i++) modificationList.SetItemChecked(i, value);
            }
            finally { suppressGroupRules = false; }
        }

        private void SelectCompatibleSet()
        {
            SetAllModificationChecks(false);
            suppressGroupRules = true;
            try
            {
                HashSet<string> selectedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = definitions.Count - 1; i >= 0; i--)
                {
                    AircraftModification definition = definitions[i];
                    if (String.IsNullOrWhiteSpace(definition.Group) || selectedGroups.Add(definition.Group))
                        modificationList.SetItemChecked(i, true);
                }
            }
            finally { suppressGroupRules = false; }
        }

        private void UpdateCountermeasureControls()
        {
            bool enabled = overrideCountermeasures.Checked;
            flareRounds.Enabled = enabled;
            chaffRounds.Enabled = enabled;
        }

        private void SaveResult()
        {
            if (overrideCountermeasures.Checked && flareRounds.Value + chaffRounds.Value <= 0)
            {
                MessageBox.Show(this, "Set at least one flare or chaff round.", "Countermeasure settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            AircraftSettings result = new AircraftSettings
            {
                UseAllModifications = allModifications.Checked,
                OverrideCountermeasures = overrideCountermeasures.Checked,
                FlareRounds = Decimal.ToInt32(flareRounds.Value),
                ChaffRounds = Decimal.ToInt32(chaffRounds.Value),
                UnlimitedCountermeasures = false
            };
            if (!result.UseAllModifications)
            {
                foreach (object checkedItem in modificationList.CheckedItems)
                {
                    AircraftModification definition = checkedItem as AircraftModification;
                    if (definition != null) result.EnabledModifications.Add(definition.Id);
                }
            }
            Result = result;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal sealed class WindowHandleOwner : IWin32Window
    {
        public IntPtr Handle { get; private set; }
        public WindowHandleOwner(IntPtr handle) { Handle = handle; }
    }

    internal sealed class MainForm : Form
    {
        private readonly List<Aircraft> aircraft = new List<Aircraft>();
        private readonly List<TargetUnit> groundTargets = new List<TargetUnit>();
        private readonly List<TargetUnit> shipTargets = new List<TargetUnit>();
        private readonly List<DonorWeapon> nativeWeapons = new List<DonorWeapon>();
        private readonly List<DonorWeapon> globalWeapons = new List<DonorWeapon>();
        private readonly List<KeyValuePair<string, string>> navalCannons = new List<KeyValuePair<string, string>>();
        private readonly List<KeyValuePair<string, string>> airOrdnance = new List<KeyValuePair<string, string>>();
        private readonly List<UnitWeapon> unitWeapons = new List<UnitWeapon>();
        private readonly List<AircraftModification> modifications = new List<AircraftModification>();
        private readonly List<GroundAmmo> groundAmmo = new List<GroundAmmo>();
        private readonly List<CombinedMap> combinedMaps = new List<CombinedMap>();
        private readonly List<PylonSlot> pylons = new List<PylonSlot>();
        private readonly Dictionary<int, PylonAssignment> assignments = new Dictionary<int, PylonAssignment>();
        private readonly Dictionary<int, Button> pylonButtons = new Dictionary<int, Button>();
        private readonly Dictionary<string, AircraftSettings> aircraftSettings = new Dictionary<string, AircraftSettings>(StringComparer.OrdinalIgnoreCase);

        private TextBox gameFolder;
        private TextBox aircraftSearch;
        private ComboBox nationFilter;
        private ComboBox rankFilter;
        private ComboBox vehicleFilter;
        private ListBox aircraftList;
        private AircraftPreview preview;
        private FlowLayoutPanel pylonStrip;
        private Label massLabel;
        private Label stationLabel;
        private CheckBox injectionToggle;
        private TextBox weaponSearch;
        private ComboBox categoryFilter;
        private ComboBox weaponNationFilter;
        private ComboBox sortFilter;
        private ListView weaponList;
        private ComboBox airTargetBox;
        private ComboBox groundTargetBox;
        private ComboBox shipTargetBox;
        private NumericUpDown airCount;
        private NumericUpDown groundCount;
        private NumericUpDown shipCount;
        private CheckBox hostileGround;
        private CheckBox samSites;
        private string pendingSamMode = "active";
        private string pendingSamSelection = "all";
        private Label status;
        private Button aircraftSettingsButton;
        private PylonSlot selectedPylon;
        private bool suppressSuccessDialog;
        private bool lastGenerationSucceeded;
        private bool workspaceOperation;
        private Exception workspaceLastError;
        private List<string> workspaceGroundTargetOverrides;
        private List<FlyingTargetSlot> workspaceFlyingTargets;
        private bool workspacePassiveShip;
        private CombinedScenarioSettings workspaceCombinedScenario;

        private const string MissionFolderRelative = @"UserMissions\Universal Test Lab";
        private const string StarterMissionName = "universal_test_lab_start.blk";
        internal const string HotMissionName = "universal_test_lab_hot.blk";
        // War Thunder only accepts player-controlled custom ground units through one of
        // the reserve-tank proxy names in the root userVehicles directory. Nested,
        // tokenized class names can render and drive, but their weapon controllers are
        // not registered as a playable tank.
        internal const string GroundProxyClassId = "userVehicles/ussr_t_26_1940";
        private const string GroundProxyVehicleFileName = "ussr_t_26_1940.blk";

        public MainForm()
        {
            Text = "Universal Test Lab — Mission Studio";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1240, 780);
            Size = new Size(1500, 920);
            BackColor = Theme.Window;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.2f);
            LoadCatalogs();
            LoadAircraftSettings();
            MissionSettings.Load();
            BuildUi();
            gameFolder.Text = DetectGameFolder();
            SelectDefaults();
        }

        internal IList<Aircraft> WorkspaceAircraft { get { return aircraft; } }
        internal IList<TargetUnit> WorkspaceGroundTargets { get { return groundTargets; } }
        internal IList<TargetUnit> WorkspaceShipTargets { get { return shipTargets; } }
        private readonly Dictionary<string, int> groundCannonAmmoCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GroundWeaponCacheData> groundWeaponCacheMap = new Dictionary<string, GroundWeaponCacheData>(StringComparer.OrdinalIgnoreCase);
        internal static Dictionary<string, GroundWeaponCacheData> prebuiltGroundWeapons;

        internal static Dictionary<string, GroundWeaponCacheData> LoadPrebuiltGroundWeapons()
        {
            Dictionary<string, GroundWeaponCacheData> result = new Dictionary<string, GroundWeaponCacheData>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string jsonText = Embedded.Text("UTL.vehicle_weapons.json");
                if (String.IsNullOrWhiteSpace(jsonText)) return result;
                System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                serializer.MaxJsonLength = int.MaxValue;
                Dictionary<string, GroundWeaponCacheJson> data = serializer.Deserialize<Dictionary<string, GroundWeaponCacheJson>>(jsonText);
                if (data == null) return result;
                foreach (KeyValuePair<string, GroundWeaponCacheJson> kv in data)
                {
                    GroundWeaponCacheJson src = kv.Value;
                    if (src == null) continue;
                    GroundWeaponCacheData dst = new GroundWeaponCacheData();
                    if (src.weapons != null)
                    {
                        List<GroundWeaponInfo> weapons = new List<GroundWeaponInfo>();
                        foreach (GroundWeaponInfoJson w in src.weapons)
                        {
                            if (w == null || String.IsNullOrWhiteSpace(w.blk)) continue;
                            weapons.Add(new GroundWeaponInfo { Trigger = w.trigger ?? "", Blk = w.blk, NativeAmmo = w.nativeAmmo });
                        }
                        dst.Weapons = weapons;
                    }
                    if (src.missiles != null)
                    {
                        List<KeyValuePair<string, string>> missiles = new List<KeyValuePair<string, string>>();
                        foreach (MissileInfoJson m in src.missiles)
                        {
                            if (m == null) continue;
                            missiles.Add(new KeyValuePair<string, string>(m.name ?? "", m.blk ?? ""));
                        }
                        dst.Missiles = missiles;
                    }
                    if (src.beltOptions != null)
                    {
                        List<GroundWeaponBeltOption> belts = new List<GroundWeaponBeltOption>();
                        foreach (GroundWeaponBeltJson bj in src.beltOptions)
                        {
                            if (bj == null || String.IsNullOrWhiteSpace(bj.name)) continue;
                            GroundWeaponBeltOption bo = new GroundWeaponBeltOption { Name = bj.name, Calibre = bj.calibre };
                            if (bj.rounds != null)
                            {
                                List<GroundAmmo> rounds = new List<GroundAmmo>();
                                foreach (GroundWeaponRoundJson rj in bj.rounds)
                                {
                                    if (rj == null || String.IsNullOrWhiteSpace(rj.bulletName)) continue;
                                    rounds.Add(new GroundAmmo { Container = bj.name, BulletName = rj.bulletName, Display = rj.display ?? "", Type = rj.kind ?? "", Mass = rj.mass, Speed = rj.speed, ExplosiveMass = rj.explosive, Caliber = rj.caliber, Penetration = rj.penetration });
                                }
                                bo.Rounds = rounds;
                            }
                            belts.Add(bo);
                        }
                        dst.BeltOptions = belts;
                    }
                    if (src.rackRounds != null)
                    {
                        foreach (KeyValuePair<string, int> r in src.rackRounds) dst.RackRounds[r.Key] = r.Value;
                    }
                    if (src.beltSizes != null)
                    {
                        foreach (KeyValuePair<string, int> b in src.beltSizes) dst.BeltSizes[b.Key] = b.Value;
                    }
                    dst.BeltTypeLimit = src.beltTypeLimit > 1 ? src.beltTypeLimit : 1;
                    result[kv.Key] = dst;
                }
            }
            catch { }
            return result;
        }

        internal GroundWeaponCacheData WorkspaceGetGroundWeaponCache(Aircraft target)
        {
            if (target == null || String.IsNullOrWhiteSpace(target.Id)) return null;
            GroundWeaponCacheData cache;
            if (groundWeaponCacheMap.TryGetValue(target.Id, out cache)) return cache;
            cache = new GroundWeaponCacheData();
            GroundWeaponCacheData prebuilt = null;
            if (prebuiltGroundWeapons != null && prebuiltGroundWeapons.TryGetValue(target.Id, out prebuilt) && prebuilt != null)
            {
                cache.Weapons = prebuilt.Weapons ?? new List<GroundWeaponInfo>();
                cache.Missiles = prebuilt.Missiles ?? new List<KeyValuePair<string, string>>();
                cache.BeltOptions = prebuilt.BeltOptions ?? new List<GroundWeaponBeltOption>();
                foreach (KeyValuePair<string, int> r in prebuilt.RackRounds) cache.RackRounds[r.Key] = r.Value;
                foreach (KeyValuePair<string, int> b in prebuilt.BeltSizes) cache.BeltSizes[b.Key] = b.Value;
                cache.BeltTypeLimit = prebuilt.BeltTypeLimit > 1 ? prebuilt.BeltTypeLimit : 1;
            }
            else
            {
                cache.Weapons = WorkspaceGroundWeaponsUncached(target);
                cache.Missiles = WorkspaceVehicleMissilesUncached(target);
                cache.BeltOptions = WorkspaceGunBeltOptionsUncached(target);
            }
            groundWeaponCacheMap[target.Id] = cache;
            return cache;
        }

        internal int WorkspaceRackRoundsCached(GroundWeaponCacheData cache, string blk)
        {
            if (cache == null) return WorkspaceRackRounds(blk);
            string key = blk ?? String.Empty;
            int rounds;
            if (cache.RackRounds.TryGetValue(key, out rounds)) return rounds;
            rounds = WorkspaceRackRounds(blk);
            cache.RackRounds[key] = rounds;
            return rounds;
        }

        internal IList<GroundWeaponInfo> WorkspaceGroundWeapons(Aircraft target)
        {
            GroundWeaponCacheData cache = WorkspaceGetGroundWeaponCache(target);
            return cache == null ? new List<GroundWeaponInfo>() : cache.Weapons;
        }

        private IList<GroundWeaponInfo> WorkspaceGroundWeaponsUncached(Aircraft target)
        {
            List<GroundWeaponInfo> result = new List<GroundWeaponInfo>();
            if (target == null) return result;
            try
            {
                string unitBlk = ExtractGameBlk(gameFolder.Text, "gamedata/units/tankmodels/" + target.Id.ToLowerInvariant() + ".blk");
                string native = File.ReadAllText(unitBlk, Encoding.UTF8);
                List<string> seen = new List<string>();
                string lastKey = null;
                foreach (BlockSpan w in BlkTools.Blocks(native, "Weapon"))
                {
                    string trigger = BlkTools.Field(w.Text, "trigger", "t");
                    if (String.IsNullOrWhiteSpace(trigger)) continue;
                    string blk = BlkTools.Field(w.Text, "blk", "t");
                    if (String.IsNullOrWhiteSpace(blk)) continue;
                    string key = (trigger + "|" + NormalizeGameResourcePath(blk)).ToLowerInvariant();
                    if (key.Equals(lastKey, StringComparison.OrdinalIgnoreCase) && result.Count > 0)
                    {
                        // Consecutive identical (trigger, blk) Weapon blocks are multi-mount
                        // weapons (e.g. quad M2 on M16): accumulate the native ammo.
                        int extraAmmo = 0;
                        Match m0 = Regex.Match(w.Text, @"(?m)^\s*bullets:i\s*=\s*(-?[0-9]+)");
                        if (m0.Success) Int32.TryParse(m0.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out extraAmmo);
                        result[result.Count - 1].NativeAmmo += Math.Max(0, extraAmmo);
                        continue;
                    }
                    lastKey = key;
                    if (seen.Any(x => x.Equals(key, StringComparison.OrdinalIgnoreCase))) continue;
                    seen.Add(key);
                    string file = String.IsNullOrWhiteSpace(blk) ? "" : blk.Substring(blk.LastIndexOf('/') + 1).Replace("_user_cannon", "").Replace("_user_machinegun", "").Replace(".blk", "").Replace('_', ' ');
                    int nativeAmmo = 0;
                    Match m = Regex.Match(w.Text, @"(?m)^\s*bullets:i\s*=\s*(-?[0-9]+)");
                    if (m.Success) Int32.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out nativeAmmo);
                    if (nativeAmmo <= 0 && !String.IsNullOrWhiteSpace(blk))
                    {
                        string cannonKey = NormalizeGameResourcePath(blk);
                        if (!groundCannonAmmoCache.TryGetValue(cannonKey, out nativeAmmo))
                        {
                            try
                            {
                                string cannonText = File.ReadAllText(ExtractGameBlk(gameFolder.Text, cannonKey), Encoding.UTF8);
                                Match cm = Regex.Match(cannonText, @"(?m)^\s*bullets:i\s*=\s*(-?[0-9]+)");
                                if (cm.Success) Int32.TryParse(cm.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out nativeAmmo);
                            }
                            catch { nativeAmmo = 0; }
                            groundCannonAmmoCache[cannonKey] = nativeAmmo;
                        }
                    }
                    result.Add(new GroundWeaponInfo { Trigger = trigger, Blk = blk, NativeAmmo = nativeAmmo, Display = (trigger.Equals("gunner0", StringComparison.OrdinalIgnoreCase) ? "PRIMARY" : trigger.ToUpperInvariant()) + " — " + file });
                }
            }
            catch { }
            return result;
        }
        public int WorkspaceRackRounds(string blk)
        {
            if (String.IsNullOrWhiteSpace(blk)) return 1;
            try
            {
                string launcherText = File.ReadAllText(ExtractGameBlk(gameFolder.Text, blk.Replace('\\', '/').TrimStart('/')), Encoding.UTF8);
                Match rackMatch = Regex.Match(launcherText, @"(?m)^\s*bullets:i\s*=\s*(\d+)\s*$");
                if (rackMatch.Success)
                {
                    int v = Int32.Parse(rackMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    if (v > 1) return v;
                }
            }
            catch { }
            return 1;
        }

public IList<GroundAmmo> WorkspaceResolveCannonAmmo(string cannonBlk)
        {
            List<GroundAmmo> result = new List<GroundAmmo>();
            if (String.IsNullOrWhiteSpace(cannonBlk)) return result;
            try
            {
                string cannonPath = NormalizeGameResourcePath(cannonBlk);
                string cannon = File.ReadAllText(ExtractGameBlk(gameFolder.Text, cannonPath), Encoding.UTF8);
                foreach (BlockSpan group in BlkTools.RootBlocks(cannon))
                {
                    string blockName = BlkTools.BlockName(group);
                    if (blockName.Equals("bullet", StringComparison.OrdinalIgnoreCase))
                    {
                        string bname = BlkTools.Field(group.Text, "bulletName", "t");
                        if (!String.IsNullOrWhiteSpace(bname))
                            result.Add(new GroundAmmo { SourceBlk = cannonBlk, BulletName = bname, Display = (bname.Replace('_', ' ')).Trim(), Type = "injected" });
                    }
                    else
                    {
                        List<BlockSpan> bullets = BlkTools.Blocks(group.Text, "bullet");
                        if (bullets.Count > 1)
                        {
                            result.Add(new GroundAmmo { SourceBlk = cannonBlk, BulletName = blockName, Display = (blockName.Replace('_', ' ')).Trim() + " (belt)", Type = "injected" });
                            foreach (BlockSpan pr in bullets)
                            {
                                string bname = BlkTools.Field(pr.Text, "bulletName", "t");
                                if (!String.IsNullOrWhiteSpace(bname))
                                    result.Add(new GroundAmmo { SourceBlk = cannonBlk, BulletName = bname, Display = (bname.Replace('_', ' ')).Trim(), Type = "injected" });
                            }
                        }
                        else if (bullets.Count == 1)
                        {
                            string bname = BlkTools.Field(bullets[0].Text, "bulletName", "t");
                            if (!String.IsNullOrWhiteSpace(bname))
                                result.Add(new GroundAmmo { SourceBlk = cannonBlk, BulletName = bname, Display = (bname.Replace('_', ' ')).Trim(), Type = "injected" });
                        }
                    }
                }
            }
            catch { }
            return result;
        }
        // Missile weapon presets of a ground vehicle (preset name + launcher blk),
        // extracted from the vehicle's weapon_presets tree. The workspace UI uses
        // this to list missiles by their native preset name - mission ammo slots
        // accept preset names (170mm_57e6_aam), not raw launcher bullet names.
        internal IList<KeyValuePair<string, string>> WorkspaceVehicleMissiles(Aircraft target)
        {
            GroundWeaponCacheData cache = WorkspaceGetGroundWeaponCache(target);
            return cache == null ? new List<KeyValuePair<string, string>>() : cache.Missiles;
        }

        private IList<KeyValuePair<string, string>> WorkspaceVehicleMissilesUncached(Aircraft target)
        {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
            if (target == null || String.IsNullOrWhiteSpace(target.Id)) return result;
            try
            {
                string native = File.ReadAllText(ExtractGameBlk(gameFolder.Text, "gamedata/units/tankModels/" + target.Id.ToLowerInvariant() + ".blk"), Encoding.UTF8);
                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (BlockSpan pylon in BlkTools.Blocks(native, "WeaponPilons"))
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
                                string key = NormalizeGameResourcePath(weaponBlk) + "|" + presetName;
                                if (seen.Add(key))
                                    result.Add(new KeyValuePair<string, string>(presetName, weaponBlk));
                            }
                        }
                    }
                }
            }
            catch { }
            return result;
        }

        // Belt (gun) ammunition options of a ground vehicle - the game exposes one
        // empty modification module per belt type (e.g. 30mm_2a38_HE, 30mm_2a42_AP
        // on Pantsir-SM-SV). Ask3lad lists these modification names as the vehicle's
        // ammo and the mission slots accept them directly (bullets0:t="30mm_2a42_AP").
        internal IList<GroundWeaponBeltOption> WorkspaceGunBeltOptions(Aircraft target)
        {
            GroundWeaponCacheData cache = WorkspaceGetGroundWeaponCache(target);
            return cache == null ? new List<GroundWeaponBeltOption>() : cache.BeltOptions;
        }

        private IList<GroundWeaponBeltOption> WorkspaceGunBeltOptionsUncached(Aircraft target)
        {
            List<GroundWeaponBeltOption> result = new List<GroundWeaponBeltOption>();
            if (target == null || String.IsNullOrWhiteSpace(target.Id)) return result;
            try
            {
                string native = File.ReadAllText(ExtractGameBlk(gameFolder.Text, "gamedata/units/tankModels/" + target.Id.ToLowerInvariant() + ".blk"), Encoding.UTF8);
                BlockSpan mods = BlkTools.FirstBlock(native, "modifications", 0);
                if (mods != null)
                    foreach (BlockSpan module in BlkTools.DirectChildBlocks(mods.Text))
                    {
                        string name = BlkTools.BlockName(module);
                        if (String.IsNullOrWhiteSpace(name)) continue;
                        if (!Regex.IsMatch(name, @"^\d+mm_", RegexOptions.IgnoreCase)) continue;
                        if (name.IndexOf("_ammo_pack", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                        if (BlkTools.DirectChildBlocks(module.Text).Count > 0) continue;
                        if (!result.Any(x => String.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
                            result.Add(new GroundWeaponBeltOption { Name = name });
                    }
            }
            catch { }
            return result;
        }

        internal IList<AircraftModification> WorkspaceModifications { get { return modifications; } }
        internal IList<GroundAmmo> WorkspaceGroundAmmo { get { return groundAmmo; } }
        internal IList<KeyValuePair<string, string>> WorkspaceNavalCannons { get { return navalCannons; } }
        internal IList<KeyValuePair<string, string>> WorkspaceAircraftCannons { get { return airOrdnance; } }
        internal IList<UnitWeapon> WorkspaceUnitWeapons { get { return unitWeapons; } }
        internal IList<CombinedMap> WorkspaceCombinedMaps { get { return combinedMaps; } }
        internal string WorkspaceGameFolder
        {
            get { return gameFolder.Text; }
            set
            {
                string selected = (value ?? "").Trim().Trim('"');
                gameFolder.Text = selected;
                groundWeaponCacheMap.Clear();
                if (!String.IsNullOrWhiteSpace(selected) && Directory.Exists(selected)) SettingsStore.SaveGameFolder(selected);
            }
        }
        internal Func<string, string, bool> WorkspaceConfirmation { get; set; }
        internal Aircraft WorkspaceSelectedAircraft { get { return SelectedAircraft; } }

        internal string WorkspaceBrowseFolder(string current, IntPtr ownerHandle)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the War Thunder root folder";
                string initial = (current ?? "").Trim().Trim('"');
                if (!Directory.Exists(initial)) initial = Directory.Exists(gameFolder.Text) ? gameFolder.Text : "";
                dialog.SelectedPath = initial;
                DialogResult answer = ownerHandle == IntPtr.Zero ? dialog.ShowDialog() : dialog.ShowDialog(new WindowHandleOwner(ownerHandle));
                if (answer != DialogResult.OK || String.IsNullOrWhiteSpace(dialog.SelectedPath)) return current ?? gameFolder.Text;
                WorkspaceGameFolder = Path.GetFullPath(dialog.SelectedPath);
                return gameFolder.Text;
            }
        }

        internal void WorkspaceSyncBase()
        {
            InstallBase(ValidGameRoot(), true);
            SetStatus("Base mission and clean test range installed.", false);
        }

        internal void WorkspaceOpenMissions()
        {
            string path = Path.Combine(ValidGameRoot(), MissionFolderRelative);
            Directory.CreateDirectory(path);
            Process.Start("explorer.exe", "\"" + path + "\"");
        }

        internal bool WorkspaceSelectAircraft(string id)
        {
            Aircraft target = aircraft.FirstOrDefault(x => x.Id.Equals(id ?? "", StringComparison.OrdinalIgnoreCase));
            if (target == null) return false;
            if (!aircraftList.Items.Cast<object>().OfType<Aircraft>().Any(x => x.Id.Equals(target.Id, StringComparison.OrdinalIgnoreCase)))
            {
                aircraftSearch.Text = "";
                nationFilter.SelectedIndex = 0;
                rankFilter.SelectedIndex = 0;
                vehicleFilter.SelectedIndex = 0;
                FilterAircraft();
            }
            aircraftList.SelectedItem = aircraftList.Items.Cast<object>().OfType<Aircraft>().FirstOrDefault(x => x.Id.Equals(target.Id, StringComparison.OrdinalIgnoreCase));
            return SelectedAircraft != null && SelectedAircraft.Id.Equals(target.Id, StringComparison.OrdinalIgnoreCase);
        }

        internal List<PylonSlot> WorkspacePylons(string aircraftId)
        {
            return pylons.Where(x => x.AircraftId.Equals(aircraftId ?? "", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Order).ThenBy(x => x.Slot).ToList();
        }

        internal List<DonorWeapon> WorkspaceWeapons(string aircraftId, int slot, bool injected, string search, string category, string nation, int sort)
        {
            IEnumerable<DonorWeapon> source = injected
                ? globalWeapons
                : nativeWeapons.Where(w => w.AircraftId.Equals(aircraftId ?? "", StringComparison.OrdinalIgnoreCase) && w.Slot == slot)
                    .GroupBy(w => w.Blk + "|" + w.Bullets).Select(g => g.First());
            if (!String.IsNullOrWhiteSpace(search))
                source = source.Where(w => w.Name.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 || w.Category.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 || w.Blk.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!String.IsNullOrWhiteSpace(category) && !category.Equals("All Weapon Types", StringComparison.OrdinalIgnoreCase))
                source = source.Where(w => w.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            if (!String.IsNullOrWhiteSpace(nation) && !nation.Equals("All Nations", StringComparison.OrdinalIgnoreCase))
                source = source.Where(w => (w.Nations ?? "").Split('|').Any(n => n.Equals(nation, StringComparison.OrdinalIgnoreCase)));
            if (sort == 1) source = source.OrderByDescending(w => w.TotalMass).ThenBy(w => w.Name);
            else if (sort == 2) source = source.OrderBy(w => w.Name).ThenBy(w => w.TotalMass);
            else source = source.OrderBy(w => w.TotalMass).ThenBy(w => w.Name);
            return source.ToList();
        }

        internal IEnumerable<string> WorkspaceWeaponCategories
        {
            get { return globalWeapons.Select(w => w.Category).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x); }
        }

        internal IEnumerable<string> WorkspaceNations
        {
            get { return aircraft.Select(a => a.Nation).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x); }
        }

        internal Dictionary<int, PylonAssignment> WorkspaceAssignments
        {
            get { return assignments.ToDictionary(x => x.Key, x => x.Value); }
        }

        internal bool WorkspaceAssignWeapon(int slot, DonorWeapon weapon, bool injected)
        {
            Aircraft selected = SelectedAircraft;
            PylonSlot pylon = selected == null ? null : pylons.FirstOrDefault(x => x.AircraftId.Equals(selected.Id, StringComparison.OrdinalIgnoreCase) && x.Slot == slot);
            if (pylon == null || weapon == null) return false;
            if (injected && (IsRiskyForPylon(pylon, weapon) || IsPresetStylePylon(pylon)))
            {
                bool legacyPresetStyle = IsPresetStylePylon(pylon);
                string warning = legacyPresetStyle
                    ? "This legacy aircraft has no native pylon tree, so the injected weapon will replace every store of the whole loadout scheme (the aircraft can carry only one scheme at a time). Display and firing behaviour depend on the model's store hooks and are experimental.\r\n\r\nInject it anyway?"
                    : "This injected weapon exceeds the known station mass or uses a mount that may be incompatible. War Thunder may reject the generated aircraft.\r\n\r\nMount it anyway?";
                bool accepted = WorkspaceConfirmation != null
                    ? WorkspaceConfirmation("Experimental Injection", warning)
                    : MessageBox.Show(this, warning, "Experimental injection", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
                if (!accepted) return false;
            }
            assignments[slot] = new PylonAssignment { Pylon = pylon, Weapon = weapon, Injected = injected };
            selectedPylon = pylon;
            RefreshPylons();
            return true;
        }

        internal static bool IsPresetStylePylon(PylonSlot pylon)
        {
            // Legacy aircraft have no WeaponSlot tree; their catalog rows are a
            // single scheme station (slot 0, no load limit) whose anchors are
            // whole preset names instead of pylon mounts.
            return pylon != null && pylon.Slot == 0 && pylon.MaxLoad == 0;
        }

        internal void WorkspaceClearStation(int slot)
        {
            assignments.Remove(slot);
            RefreshPylons();
        }

        internal void WorkspaceClearAll()
        {
            assignments.Clear();
            RefreshPylons();
        }

        internal AircraftSettings WorkspaceGetSettings(Aircraft item) { return GetAircraftSettings(item).Copy(); }

        internal void WorkspaceSetSettings(Aircraft item, AircraftSettings value)
        {
            if (item == null || value == null) return;
            aircraftSettings[item.Id] = value.Copy();
            PersistAircraftSettings();
            UpdateAircraftSettingsButton();
        }

        internal IList<CountermeasureLauncher> WorkspaceCountermeasureLaunchers(Aircraft item)
        {
            List<CountermeasureLauncher> result = new List<CountermeasureLauncher>();
            if (item == null) return result;
            try
            {
                string root = gameFolder.Text.Trim().Trim('"');
                if (!File.Exists(Path.Combine(root, "aces.vromfs.bin"))) throw new FileNotFoundException();
                string fm = File.ReadAllText(ExtractGameBlk(root, "gamedata/flightmodels/" + item.Id + ".blk"), Encoding.UTF8);
                // Aircraft that carry countermeasure upgrade modules (chaff launchers,
                // belt packs, ...) get a mixed flare/chaff loadout once fully upgraded,
                // while their stock launcher files are flare-only. Expose a chaff
                // slider whenever such modules exist so the flare/chaff ratio can be
                // configured even though the stock paths do not contain "with_chaff".
                bool countermeasureUpgrades = HasCountermeasureUpgradeModules(fm);
                int anonymous = 0;
                foreach (BlockSpan weapon in BlkTools.Blocks(fm, "Weapon"))
                {
                    if (!String.Equals(BlkTools.Field(weapon.Text, "trigger", "t"), "countermeasures", StringComparison.OrdinalIgnoreCase)) continue;
                    string path = BlkTools.Field(weapon.Text, "blk", "t") ?? "";
                    string emitter = BlkTools.Field(weapon.Text, "emitter", "t");
                    string key = String.IsNullOrWhiteSpace(emitter) ? "launcher-" + (++anonymous).ToString(CultureInfo.InvariantCulture) : emitter;
                    Match roundsMatch = Regex.Match(weapon.Text, @"(?m)^\s*bullets:i\s*=\s*(\d+)");
                    int rounds = roundsMatch.Success ? Int32.Parse(roundsMatch.Groups[1].Value, CultureInfo.InvariantCulture) : 30;
                    bool chaffOnly = path.IndexOf("chaff_only", StringComparison.OrdinalIgnoreCase) >= 0;
                    bool flareOnly = !chaffOnly && !countermeasureUpgrades && path.IndexOf("with_chaff", StringComparison.OrdinalIgnoreCase) < 0 &&
                        path.IndexOf("maw", StringComparison.OrdinalIgnoreCase) < 0 && path.IndexOf("bol", StringComparison.OrdinalIgnoreCase) < 0;
                    CountermeasureLauncher launcher = result.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
                    if (launcher == null)
                    {
                        string identity = path + " " + key;
                        string kind = identity.IndexOf("bol", StringComparison.OrdinalIgnoreCase) >= 0 ? "BOL COUNTERMEASURE DISPENSER" :
                            identity.IndexOf("bko", StringComparison.OrdinalIgnoreCase) >= 0 ? "BKO COUNTERMEASURE DISPENSER" :
                            identity.IndexOf("maw", StringComparison.OrdinalIgnoreCase) >= 0 ? "MAW COUNTERMEASURE DISPENSER" :
                            identity.IndexOf("large", StringComparison.OrdinalIgnoreCase) >= 0 ? "LARGE COUNTERMEASURE DISPENSER" : "INTERNAL COUNTERMEASURE DISPENSER";
                        launcher = new CountermeasureLauncher { Key = key, Display = kind, NativeRounds = Math.Max(1, rounds), AllowsFlares = !chaffOnly, AllowsChaff = !flareOnly };
                        result.Add(launcher);
                    }
                    else
                    {
                        launcher.NativeRounds = Math.Max(launcher.NativeRounds, rounds);
                        launcher.AllowsFlares |= !chaffOnly;
                        launcher.AllowsChaff |= !flareOnly;
                    }
                }
            }
            catch { }
            if (result.Count == 0)
            {
                AircraftSettings settings = GetAircraftSettings(item);
                foreach (CountermeasureLoadout saved in settings.CountermeasureLoadouts)
                    result.Add(new CountermeasureLauncher { Key = saved.Key, Display = "COUNTERMEASURE DISPENSER", NativeRounds = Math.Max(1, saved.Flares + saved.Chaff), AllowsFlares = true, AllowsChaff = true });
            }
            if (result.Count == 0)
                result.Add(new CountermeasureLauncher { Key = "default", Display = "INSTALLED COUNTERMEASURE DISPENSERS", NativeRounds = 90, AllowsFlares = true, AllowsChaff = true });
            foreach (IGrouping<string, CountermeasureLauncher> group in result.GroupBy(x => x.Display, StringComparer.OrdinalIgnoreCase))
            {
                if (group.Count() < 2) continue;
                int number = 1;
                foreach (CountermeasureLauncher launcher in group.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                    launcher.Display = group.Key + " " + (number++).ToString(CultureInfo.InvariantCulture);
            }
            return result.OrderBy(x => x.Display, StringComparer.OrdinalIgnoreCase).ToList();
        }

        internal static bool HasCountermeasureUpgradeModules(string flightmodel)
        {
            if (String.IsNullOrWhiteSpace(flightmodel)) return false;
            BlockSpan modifications = BlkTools.FirstBlock(flightmodel, "modifications", 0);
            if (modifications == null) return false;
            foreach (BlockSpan module in BlkTools.DirectChildBlocks(modifications.Text))
            {
                string name = BlkTools.BlockName(module);
                if (String.IsNullOrWhiteSpace(name)) continue;
                if (name.IndexOf("countermeasure", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    String.Equals(BlkTools.Field(module.Text, "group", "t"), "countermeasures", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        internal bool WorkspaceGenerateMission(string airTargetId, int airTargetCount, IList<string> groundTargetIds, bool hostile, string shipTargetId, int shipTargetCount, bool passiveShip)
        {
            return WorkspaceGenerateMission(airTargetId, airTargetCount, groundTargetIds, hostile, shipTargetId, shipTargetCount, passiveShip, null, null, "active", "all");
        }

        internal bool WorkspaceGenerateMission(string airTargetId, int airTargetCount, IList<string> groundTargetIds, bool hostile, string shipTargetId, int shipTargetCount, bool passiveShip, IList<FlyingTargetSlot> flyingTargets)
        {
            return WorkspaceGenerateMission(airTargetId, airTargetCount, groundTargetIds, hostile, shipTargetId, shipTargetCount, passiveShip, flyingTargets, null, "active", "all");
        }

        internal bool WorkspaceGenerateMission(string airTargetId, int airTargetCount, IList<string> groundTargetIds, bool hostile, string shipTargetId, int shipTargetCount, bool passiveShip, IList<FlyingTargetSlot> flyingTargets, CombinedScenarioSettings combinedScenario, string samSitesMode = "active", string samSitesSelection = "all")
        {
            SelectComboById(airTargetBox, airTargetId);
            string firstGround = groundTargetIds == null ? null : groundTargetIds.FirstOrDefault(x => !String.IsNullOrWhiteSpace(x));
            SelectComboById(groundTargetBox, firstGround);
            SelectComboById(shipTargetBox, shipTargetId);
            airCount.Value = Math.Max(0, Math.Min(20, airTargetCount));
            groundCount.Value = firstGround == null ? 0 : 1;
            shipCount.Value = Math.Max(0, Math.Min(20, shipTargetCount));
            hostileGround.Checked = hostile;
            samSites.Checked = samSitesMode != "disabled";
            pendingSamMode = samSitesMode;
            pendingSamSelection = samSitesSelection;
            workspaceGroundTargetOverrides = groundTargetIds == null ? null : groundTargetIds.Where(x => !String.IsNullOrWhiteSpace(x)).Take(7).ToList();
            workspaceFlyingTargets = flyingTargets == null ? null : flyingTargets.Where(x => x != null && !String.IsNullOrWhiteSpace(x.AircraftId)).ToList();
            workspacePassiveShip = passiveShip;
            workspaceCombinedScenario = combinedScenario == null ? null : combinedScenario.Copy();
            suppressSuccessDialog = true;
            lastGenerationSucceeded = false;
            workspaceOperation = true;
            workspaceLastError = null;
            try { ApplyClicked(); }
            finally
            {
                workspaceOperation = false;
                suppressSuccessDialog = false;
                workspaceGroundTargetOverrides = null;
                workspaceFlyingTargets = null;
                workspacePassiveShip = false;
                workspaceCombinedScenario = null;
            }
            if (workspaceLastError != null) throw workspaceLastError;
            return lastGenerationSucceeded;
        }

        internal int WorkspaceWeaponCount { get { return globalWeapons.Count; } }

        private static string[] Lines(string resource)
        {
            return Embedded.Text(resource).Replace("\r", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        // Deserialize an embedded catalog JSON resource into row DTOs. Mirrors the
        // legacy TSV parsers but reads the converted JSON catalogs instead.
        internal static List<T> JsonRows<T>(string resource)
        {
            try
            {
                string text = Embedded.Text(resource);
                if (String.IsNullOrWhiteSpace(text)) return new List<T>();
                System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                serializer.MaxJsonLength = int.MaxValue;
                List<T> rows = serializer.Deserialize<List<T>>(text);
                return rows ?? new List<T>();
            }
            catch { return new List<T>(); }
        }

        internal static double ParseNumber(string value)
        {
            double result;
            return Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ? result : 0;
        }

        private void LoadCatalogs()
        {
            prebuiltGroundWeapons = LoadPrebuiltGroundWeapons();
            foreach (AircraftRowJson r in MainForm.JsonRows<AircraftRowJson>("UTL.aircraft.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.id)) continue;
                aircraft.Add(new Aircraft { Id = r.id, Display = r.display, Type = r.type, DefaultPreset = r.defaultPreset, Nation = r.nation, Rank = r.rank, MaxLoad = r.maxLoad, Kind = String.IsNullOrWhiteSpace(r.kind) ? "Aircraft" : r.kind });
            }
            foreach (GroundRowJson r in MainForm.JsonRows<GroundRowJson>("UTL.ground.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.id)) continue;
                TargetUnit target = new TargetUnit
                {
                    Id = r.id, Display = r.display, DefaultPreset = r.defaultPreset, Nation = String.IsNullOrWhiteSpace(r.nation) ? "Other" : r.nation, Rank = r.rank,
                    Type = String.IsNullOrWhiteSpace(r.type) ? "Ground Vehicle" : r.type, MainWeaponBlk = r.mainWeaponBlk ?? "", MaxAmmo = r.maxAmmo,
                    NativeMass = r.mass, NativeEnginePower = r.enginePower,
                    NativeForwardSpeed = r.forwardSpeed, NativeReverseSpeed = r.reverseSpeed,
                    NativeReloadSeconds = r.reloadSeconds, NativeRecoil = r.recoil
                };
                groundTargets.Add(target);
                aircraft.Add(new Aircraft
                {
                    Id = target.Id, Display = target.Display, Type = target.Type, DefaultPreset = target.DefaultPreset, Nation = target.Nation,
                    Rank = target.Rank, Kind = "Ground Vehicle", MainWeaponBlk = target.MainWeaponBlk, MaxAmmo = target.MaxAmmo,
                    NativeMass = target.NativeMass, NativeEnginePower = target.NativeEnginePower, NativeForwardSpeed = target.NativeForwardSpeed,
                    NativeReverseSpeed = target.NativeReverseSpeed, NativeReloadSeconds = target.NativeReloadSeconds, NativeRecoil = target.NativeRecoil
                });
            }
            foreach (ShipRowJson r in MainForm.JsonRows<ShipRowJson>("UTL.ships.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.id)) continue;
                shipTargets.Add(new TargetUnit { Id = r.id, Display = r.display, DefaultPreset = r.defaultPreset, Nation = String.IsNullOrWhiteSpace(r.nation) ? "Other" : r.nation, Rank = r.rank, Type = String.IsNullOrWhiteSpace(r.type) ? "Ship" : r.type });
            }
            foreach (DonorWeaponRowJson r in MainForm.JsonRows<DonorWeaponRowJson>("UTL.donor_weapons.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.blk)) continue;
                nativeWeapons.Add(new DonorWeapon
                {
                    AircraftId = r.aircraftId, AircraftDisplay = r.aircraftDisplay, Slot = r.slot, Mount = r.mount, Trigger = r.trigger, Blk = r.blk,
                    Emitter = r.emitter, Bullets = r.bullets, Icon = r.icon, Name = r.name, Category = r.category, UnitMass = r.unitMass, TotalMass = r.totalMass
                });
            }
            foreach (DonorWeaponRowJson r in MainForm.JsonRows<DonorWeaponRowJson>("UTL.weapon_catalog.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.blk)) continue;
                globalWeapons.Add(new DonorWeapon { Trigger = r.trigger, Blk = r.blk, Bullets = r.bullets, Icon = r.icon, Name = r.name, Category = r.category, UnitMass = r.unitMass, TotalMass = r.totalMass });
            }
            navalCannons.Clear();
            foreach (NameValueRowJson r in MainForm.JsonRows<NameValueRowJson>("UTL.naval_cannons.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.key)) continue;
                navalCannons.Add(new KeyValuePair<string, string>(r.key.Trim(), (r.value ?? "").Trim()));
            }
            unitWeapons.Clear();
            foreach (UnitWeaponRowJson r in MainForm.JsonRows<UnitWeaponRowJson>("UTL.unit_weapons.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.unitId) || String.IsNullOrWhiteSpace(r.weaponBlk)) continue;
                unitWeapons.Add(new UnitWeapon { UnitId = r.unitId, Domain = r.domain, UnitDisplay = r.unitDisplay, WeaponBlk = r.weaponBlk, WeaponDisplay = r.weaponDisplay, Kind = r.kind });
            }
            airOrdnance.Clear();
            foreach (NameValueRowJson r in MainForm.JsonRows<NameValueRowJson>("UTL.air_ordnance.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.blk)) continue;
                airOrdnance.Add(new KeyValuePair<string, string>(r.blk.Trim(), (r.display ?? "").Trim()));
            }
            foreach (PylonSlotRowJson r in MainForm.JsonRows<PylonSlotRowJson>("UTL.aircraft_slots.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.aircraftId)) continue;
                pylons.Add(new PylonSlot { AircraftId = r.aircraftId, Slot = r.slot, Order = r.order, Tier = r.tier, MaxLoad = r.maxLoad, AnchorMount = r.anchorMount });
            }
            foreach (ModificationRowJson r in MainForm.JsonRows<ModificationRowJson>("UTL.modifications.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.aircraftId) || String.IsNullOrWhiteSpace(r.id)) continue;
                modifications.Add(new AircraftModification
                {
                    AircraftId = r.aircraftId, Id = r.id, Display = r.display, Tier = r.tier,
                    ModClass = r.modClass, Group = r.group, Requires = r.requires
                });
            }
            foreach (CombinedMapRowJson r in MainForm.JsonRows<CombinedMapRowJson>("UTL.combined_maps.json"))
            {
                if (r == null || String.IsNullOrWhiteSpace(r.id)) continue;
                CombinedMap map = combinedMaps.FirstOrDefault(x => x.Id.Equals(r.id, StringComparison.OrdinalIgnoreCase));
                if (map == null)
                {
                    map = new CombinedMap { Id = r.id, Display = r.display, Level = r.level };
                    combinedMaps.Add(map);
                }
                if (!String.IsNullOrWhiteSpace(r.kind) && r.kind.Equals("capture", StringComparison.OrdinalIgnoreCase))
                {
                    map.CapturePoints.Add(new CombinedCapturePoint { Id = r.detail, Label = r.label, Transform = r.transform });
                    continue;
                }
                map.Spawns.Add(new CombinedSpawn
                {
                    Kind = r.kind, Side = r.side, Option = r.detail, Label = r.label, Transform = r.transform, ObjectClass = r.objectClass
                });
            }

            combinedMaps.Sort(delegate(CombinedMap left, CombinedMap right) { return StringComparer.CurrentCultureIgnoreCase.Compare(left.Display, right.Display); });
            PopulateWeaponNations();
        }

        private void PopulateWeaponNations()
        {
            Dictionary<string, string> aircraftNations = aircraft.GroupBy(a => a.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Nation, StringComparer.OrdinalIgnoreCase);
            foreach (DonorWeapon weapon in nativeWeapons)
            {
                string nation;
                weapon.Nations = aircraftNations.TryGetValue(weapon.AircraftId, out nation) ? nation : "";
            }
            Dictionary<string, List<DonorWeapon>> sources = nativeWeapons
                .Where(w => w.AircraftId.IndexOf("killstreak", StringComparison.OrdinalIgnoreCase) < 0 && !w.AircraftId.StartsWith("nt_", StringComparison.OrdinalIgnoreCase))
                .GroupBy(WeaponKey, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            foreach (DonorWeapon weapon in globalWeapons)
            {
                List<DonorWeapon> donors;
                List<string> nations = sources.TryGetValue(WeaponKey(weapon), out donors)
                    ? donors.Select(w => w.Nations).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList()
                    : new List<string>();
                if (!String.IsNullOrWhiteSpace(weapon.Nations))
                    nations.AddRange(weapon.Nations.Split('|').Where(x => !String.IsNullOrWhiteSpace(x)));
                if (nations.Count == 0)
                {
                    string inferred = InferWeaponNation(weapon.Blk);
                    if (!String.IsNullOrEmpty(inferred)) nations.Add(inferred);
                }
                weapon.Nations = String.Join("|", nations.Distinct().OrderBy(x => x).ToArray());
            }
        }

        private static string WeaponKey(DonorWeapon weapon)
        {
            return (weapon.Trigger ?? "") + "|" + (weapon.Blk ?? "") + "|" + weapon.Bullets.ToString(CultureInfo.InvariantCulture);
        }

        private static string InferWeaponNation(string blk)
        {
            string file = Path.GetFileNameWithoutExtension(blk ?? "").ToLowerInvariant();
            if (file.StartsWith("us_") || file.StartsWith("aim_") || file.StartsWith("agm_") || file.StartsWith("gbu_")) return "USA";
            if (file.StartsWith("su_") || file.StartsWith("ussr_") || file.StartsWith("ru_") || file.StartsWith("r_") || file.StartsWith("kh_")) return "USSR";
            if (file.StartsWith("uk_") || file.StartsWith("gb_") || file.Contains("brimstone")) return "Britain";
            if (file.StartsWith("fr_") || file.Contains("magic") || file.Contains("mica")) return "France";
            if (file.StartsWith("de_") || file.StartsWith("ger_")) return "Germany";
            if (file.StartsWith("it_") || file.StartsWith("ita_")) return "Italy";
            if (file.StartsWith("jp_") || file.StartsWith("ja_")) return "Japan";
            if (file.StartsWith("cn_") || file.StartsWith("ch_")) return "China";
            if (file.StartsWith("il_") || file.StartsWith("isr_")) return "Israel";
            if (file.StartsWith("se_") || file.StartsWith("sw_")) return "Sweden";
            return "";
        }

        private void BuildUi()
        {
            GradientBackdrop backdrop = new GradientBackdrop { Dock = DockStyle.Fill };
            Controls.Add(backdrop);
            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Color.Transparent };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            backdrop.Controls.Add(root);
            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildWorkspace(), 0, 1);
            status = new Label { Dock = DockStyle.Fill, Text = "●  READY — choose a vehicle to begin", ForeColor = Theme.Good, BackColor = Color.FromArgb(210, 12, 15, 39), Padding = new Padding(20, 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft };
            root.Controls.Add(status, 0, 2);
        }

        private Control BuildHeader()
        {
            GlassPanel shell = new GlassPanel { Dock = DockStyle.Fill, HeaderStyle = true, Margin = new Padding(14, 10, 14, 2) };
            TableLayoutPanel bar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 7, BackColor = Color.Transparent, Padding = new Padding(16, 7, 12, 7) };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 255));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 102));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
            TableLayoutPanel brand = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.Transparent, Margin = new Padding(0) };
            brand.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            brand.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Label title = Theme.Label("U.T.L. by AstraSEP", true);
            title.Font = new Font("Segoe UI Semibold", 15.5f);
            title.ForeColor = Theme.Text;
            brand.Controls.Add(title, 0, 0);
            Label subtitle = Theme.Label("WAR THUNDER MISSION STUDIO", false);
            subtitle.ForeColor = Theme.AccentLight;
            subtitle.Font = new Font("Segoe UI Semibold", 7.7f);
            brand.Controls.Add(subtitle, 0, 1);
            bar.Controls.Add(brand, 0, 0);
            TableLayoutPanel folder = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.Transparent, Margin = new Padding(8, 0, 0, 0) };
            folder.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            folder.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Label folderLabel = Theme.Label("GAME DIRECTORY", false);
            folderLabel.Font = new Font("Segoe UI Semibold", 7.4f);
            folder.Controls.Add(folderLabel, 0, 0);
            gameFolder = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 2, 0, 3) };
            Theme.Input(gameFolder);
            folder.Controls.Add(gameFolder, 0, 1);
            bar.Controls.Add(folder, 1, 0);
            Button browse = new Button { Text = "BROWSE", Dock = DockStyle.Fill, Margin = new Padding(8, 8, 0, 8) };
            Theme.Button(browse, false);
            browse.Click += delegate { BrowseFolder(); };
            bar.Controls.Add(browse, 2, 0);
            Button install = new Button { Text = "SYNC BASE", Dock = DockStyle.Fill, Margin = new Padding(8, 8, 0, 8) };
            Theme.Button(install, false);
            install.Click += delegate { InstallClicked(); };
            bar.Controls.Add(install, 3, 0);
            Button open = new Button { Text = "MISSIONS", Dock = DockStyle.Fill, Margin = new Padding(8, 8, 0, 8) };
            Theme.Button(open, false);
            open.Click += delegate { OpenMissionFolder(); };
            bar.Controls.Add(open, 4, 0);
            Button presets = new Button { Text = "PRESETS", Dock = DockStyle.Fill, Margin = new Padding(8, 8, 0, 8) };
            Theme.Button(presets, false);
            presets.Click += delegate { ShowPresets(); };
            bar.Controls.Add(presets, 5, 0);
            Button about = new Button { Text = "SUPPORT", Dock = DockStyle.Fill, Margin = new Padding(8, 8, 0, 8) };
            Theme.Button(about, false);
            about.Click += delegate { ShowAbout(); };
            bar.Controls.Add(about, 6, 0);
            shell.Controls.Add(bar);
            return shell;
        }

        private Control BuildWorkspace()
        {
            TableLayoutPanel workspace = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(8, 4, 8, 8), BackColor = Color.Transparent };
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 316));
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 328));
            workspace.Controls.Add(BuildAircraftBrowser(), 0, 0);
            workspace.Controls.Add(BuildLoadoutBuilder(), 1, 0);
            workspace.Controls.Add(BuildMissionPanel(), 2, 0);
            return workspace;
        }

        private Control SurfacePanel()
        {
            return new GlassPanel { Dock = DockStyle.Fill, Margin = new Padding(6) };
        }

        private Control BuildAircraftBrowser()
        {
            Panel panel = (Panel)SurfacePanel();
            TableLayoutPanel grid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 8, ColumnCount = 1, Padding = new Padding(15), BackColor = Color.Transparent };
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            grid.Controls.Add(Theme.StepHeader("01", "CHOOSE VEHICLE", "Aircraft, helicopters and experimental airframes"), 0, 0);
            grid.Controls.Add(Theme.Label("SEARCH", false), 0, 1);
            aircraftSearch = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 2, 0, 5) };
            Theme.Input(aircraftSearch);
            aircraftSearch.TextChanged += delegate { FilterAircraft(); };
            grid.Controls.Add(aircraftSearch, 0, 2);
            grid.Controls.Add(Theme.Label("NATION  /  RANK  /  TYPE", false), 0, 3);
            TableLayoutPanel filters = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31));
            nationFilter = DarkCombo();
            rankFilter = DarkCombo();
            vehicleFilter = DarkCombo();
            nationFilter.SelectedIndexChanged += delegate { FilterAircraft(); };
            rankFilter.SelectedIndexChanged += delegate { FilterAircraft(); };
            vehicleFilter.SelectedIndexChanged += delegate { FilterAircraft(); };
            filters.Controls.Add(nationFilter, 0, 0);
            filters.Controls.Add(rankFilter, 1, 0);
            filters.Controls.Add(vehicleFilter, 2, 0);
            grid.Controls.Add(filters, 0, 4);
            grid.Controls.Add(Theme.Label("AVAILABLE VEHICLES", false), 0, 5);
            Label countHint = Theme.Label("Live catalog from your installed game files", false);
            countHint.Font = new Font("Segoe UI", 8.2f);
            countHint.ForeColor = Theme.AccentLight;
            grid.Controls.Add(countHint, 0, 6);
            aircraftList = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, BorderStyle = BorderStyle.None, BackColor = Theme.Field, ForeColor = Theme.Text, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 31 };
            aircraftList.DrawItem += DrawAircraftItem;
            aircraftList.SelectedIndexChanged += delegate { AircraftChanged(); };
            grid.Controls.Add(aircraftList, 0, 7);
            panel.Controls.Add(grid);
            return panel;
        }

        private Control BuildLoadoutBuilder()
        {
            Panel panel = (Panel)SurfacePanel();
            TableLayoutPanel grid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1, Padding = new Padding(15), BackColor = Color.Transparent };
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            TableLayoutPanel heading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
            heading.Controls.Add(Theme.StepHeader("02", "BUILD LOADOUT", "Select a station, then mount a weapon"), 0, 0);
            massLabel = Theme.Label("MASS: 0 kg", false);
            massLabel.TextAlign = ContentAlignment.MiddleRight;
            massLabel.ForeColor = Theme.AccentLight;
            massLabel.Font = new Font("Segoe UI Semibold", 8.5f);
            heading.Controls.Add(massLabel, 1, 0);
            grid.Controls.Add(heading, 0, 0);
            stationLabel = Theme.Label("Choose a vehicle to reveal its weapon stations.", false);
            grid.Controls.Add(stationLabel, 0, 1);
            pylonStrip = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight, BackColor = Theme.Field, Padding = new Padding(7), Margin = new Padding(0, 2, 0, 7) };
            Theme.Round(pylonStrip, 12);
            grid.Controls.Add(pylonStrip, 0, 2);
            grid.Controls.Add(BuildWeaponFilters(), 0, 3);
            weaponList = new ListView
            {
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false,
                BorderStyle = BorderStyle.None, BackColor = Theme.Field, ForeColor = Theme.Text
            };
            weaponList.Columns.Add("Weapon", 330);
            weaponList.Columns.Add("Type", 185);
            weaponList.Columns.Add("Ammo", 62, HorizontalAlignment.Center);
            weaponList.Columns.Add("Mass", 85, HorizontalAlignment.Right);
            weaponList.Columns.Add("Mode", 78, HorizontalAlignment.Center);
            weaponList.DoubleClick += delegate { AssignSelectedWeapon(); };
            grid.Controls.Add(weaponList, 0, 4);
            TableLayoutPanel actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, Padding = new Padding(0, 6, 0, 0) };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142));
            actions.Controls.Add(Theme.Label("Tip: double-click a weapon to mount it", false), 0, 0);
            aircraftSettingsButton = new Button { Text = "MODULES", Dock = DockStyle.Fill, Margin = new Padding(5, 0, 0, 0) };
            Theme.Button(aircraftSettingsButton, false);
            aircraftSettingsButton.Click += delegate { ShowAircraftSettings(); };
            actions.Controls.Add(aircraftSettingsButton, 1, 0);
            Button clear = new Button { Text = "CLEAR STATION", Dock = DockStyle.Fill, Margin = new Padding(5, 0, 0, 0) };
            Theme.Button(clear, false);
            clear.Click += delegate { ClearSelectedStation(); };
            actions.Controls.Add(clear, 2, 0);
            Button clearAll = new Button { Text = "CLEAR ALL", Dock = DockStyle.Fill, Margin = new Padding(5, 0, 0, 0) };
            Theme.Button(clearAll, false);
            clearAll.Click += delegate { assignments.Clear(); RefreshPylons(); };
            actions.Controls.Add(clearAll, 3, 0);
            Button add = new Button { Text = "MOUNT WEAPON", Dock = DockStyle.Fill, Margin = new Padding(7, 0, 0, 0) };
            Theme.Button(add, true);
            add.Click += delegate { AssignSelectedWeapon(); };
            actions.Controls.Add(add, 4, 0);
            grid.Controls.Add(actions, 0, 5);
            panel.Controls.Add(grid);
            return panel;
        }

        private Control BuildWeaponFilters()
        {
            TableLayoutPanel row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 2, Padding = new Padding(0, 3, 0, 3), BackColor = Color.Transparent };
            row.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            row.Controls.Add(Theme.Label("WEAPON SOURCE", false), 0, 0);
            row.Controls.Add(Theme.Label("SEARCH", false), 1, 0);
            row.Controls.Add(Theme.Label("WEAPON TYPE", false), 2, 0);
            row.Controls.Add(Theme.Label("NATION", false), 3, 0);
            row.Controls.Add(Theme.Label("SORT", false), 4, 0);
            injectionToggle = new CheckBox { Text = "INJECT ANY WEAPON", Dock = DockStyle.Fill, Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter };
            Theme.Toggle(injectionToggle);
            injectionToggle.ForeColor = Theme.AccentLight;
            injectionToggle.CheckedChanged += delegate { RefreshWeaponCatalog(); };
            row.Controls.Add(injectionToggle, 0, 1);
            weaponSearch = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(7, 3, 7, 3) };
            Theme.Input(weaponSearch);
            weaponSearch.Text = "";
            weaponSearch.TextChanged += delegate { RefreshWeaponCatalog(); };
            row.Controls.Add(weaponSearch, 1, 1);
            categoryFilter = DarkCombo();
            categoryFilter.SelectedIndexChanged += delegate { RefreshWeaponCatalog(); };
            row.Controls.Add(categoryFilter, 2, 1);
            weaponNationFilter = DarkCombo();
            weaponNationFilter.SelectedIndexChanged += delegate { RefreshWeaponCatalog(); };
            row.Controls.Add(weaponNationFilter, 3, 1);
            sortFilter = DarkCombo();
            sortFilter.Items.AddRange(new object[] { "Mass: low to high", "Mass: high to low", "Name: A to Z" });
            sortFilter.SelectedIndexChanged += delegate { RefreshWeaponCatalog(); };
            row.Controls.Add(sortFilter, 4, 1);
            return row;
        }

        private Control BuildMissionPanel()
        {
            Panel panel = (Panel)SurfacePanel();
            TableLayoutPanel grid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 14, ColumnCount = 1, Padding = new Padding(15), BackColor = Color.Transparent };
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 145));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            grid.Controls.Add(Theme.StepHeader("03", "CONFIGURE TEST", "Choose targets and generate the mission"), 0, 0);
            preview = new AircraftPreview { Dock = DockStyle.Fill };
            grid.Controls.Add(preview, 0, 1);
            grid.Controls.Add(Theme.Label("TARGETS", true), 0, 2);
            grid.Controls.Add(Theme.Label("AIR TARGET", false), 0, 3);
            airTargetBox = TargetRowCombo(aircraft.Cast<object>().ToList());
            airCount = CountBox(1);
            grid.Controls.Add(ComboAndCount(airTargetBox, airCount), 0, 4);
            grid.Controls.Add(Theme.Label("GROUND TARGET / ENEMY AIR DEFENCE", false), 0, 5);
            groundTargetBox = TargetRowCombo(groundTargets.Cast<object>().ToList());
            groundCount = CountBox(1);
            hostileGround = new CheckBox { Text = "HOSTILE", Dock = DockStyle.Fill, Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter };
            Theme.Toggle(hostileGround);
            hostileGround.ForeColor = Theme.Danger;
            grid.Controls.Add(ComboCountAndOption(groundTargetBox, groundCount, hostileGround), 0, 6);
            samSites = new CheckBox { Text = "SAM SITES: ACTIVE", Dock = DockStyle.Fill, Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter, Checked = true };
            Theme.Toggle(samSites);
            samSites.ForeColor = Theme.Danger;
            samSites.CheckedChanged += delegate { samSites.Text = samSites.Checked ? "SAM SITES: ACTIVE" : "SAM SITES: DISABLED"; };
            grid.Controls.Add(samSites, 0, 7);
            grid.Controls.Add(Theme.Label("NAVAL TARGET", false), 0, 8);
            shipTargetBox = TargetRowCombo(shipTargets.Cast<object>().ToList());
            shipCount = CountBox(1);
            grid.Controls.Add(ComboAndCount(shipTargetBox, shipCount), 0, 9);
            Label details = Theme.Label("FLIGHT PROFILE\r\nFull fuel • adaptive air start • no external tanks\r\nAmmo restoration: every 10 seconds\r\nHOSTILE: active enemy air defence\r\nSAM SITES: ACTIVE keeps the S-300/Patriot/Buk sites engaging you\r\nNuclear weapons: native detonation", false);
            details.Padding = new Padding(4, 12, 4, 4);
            grid.Controls.Add(details, 0, 10);
            Label hint = Theme.Label("Aircraft/helicopters: reopen User Missions. Ground vehicle changes: restart War Thunder once so the tank proxy is reloaded.", false);
            hint.ForeColor = Theme.Good;
            grid.Controls.Add(hint, 0, 11);
            Button apply = new Button { Text = "GENERATE TEST MISSION", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 11f), Margin = new Padding(0, 4, 0, 2) };
            Theme.Button(apply, true);
            apply.Click += delegate { ApplyClicked(); };
            grid.Controls.Add(apply, 0, 12);
            Label version = Theme.Label("HOT LOAD  •  NO GAME RESTART", false);
            version.TextAlign = ContentAlignment.MiddleCenter;
            version.ForeColor = Theme.AccentLight;
            grid.Controls.Add(version, 0, 13);
            panel.Controls.Add(grid);
            return panel;
        }

        private ComboBox DarkCombo()
        {
            ComboBox box = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Theme.Field, ForeColor = Theme.Text, Margin = new Padding(2, 2, 2, 5), DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 21 };
            box.DrawItem += DrawDarkComboItem;
            return box;
        }

        private void DrawDarkComboItem(object sender, DrawItemEventArgs e)
        {
            ComboBox box = sender as ComboBox;
            if (box == null) return;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (SolidBrush background = new SolidBrush(selected ? Theme.AccentDark : Theme.Field))
                e.Graphics.FillRectangle(background, e.Bounds);
            string value = e.Index >= 0 && e.Index < box.Items.Count ? box.Items[e.Index].ToString() : (box.SelectedItem == null ? "" : box.SelectedItem.ToString());
            TextRenderer.DrawText(e.Graphics, value, box.Font, new Rectangle(e.Bounds.X + 6, e.Bounds.Y, Math.Max(1, e.Bounds.Width - 8), e.Bounds.Height),
                Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private ComboBox TargetRowCombo(List<object> values)
        {
            ComboBox box = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown, FlatStyle = FlatStyle.Flat, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems, BackColor = Theme.Field, ForeColor = Theme.Text, Margin = new Padding(0, 2, 4, 4) };
            box.Items.AddRange(values.ToArray());
            return box;
        }

        private NumericUpDown CountBox(int value)
        {
            return new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 20, Value = value, BackColor = Theme.Field, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(2, 2, 0, 4) };
        }

        private Control ComboAndCount(ComboBox box, NumericUpDown count)
        {
            TableLayoutPanel row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62));
            row.Controls.Add(box, 0, 0);
            row.Controls.Add(count, 1, 0);
            return row;
        }

        private Control ComboCountAndOption(ComboBox box, NumericUpDown count, CheckBox option)
        {
            TableLayoutPanel row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = Color.Transparent };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
            row.Controls.Add(box, 0, 0);
            row.Controls.Add(count, 1, 0);
            row.Controls.Add(option, 2, 0);
            return row;
        }

        private void DrawAircraftItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= aircraftList.Items.Count) return;
            Aircraft item = aircraftList.Items[e.Index] as Aircraft;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Rectangle row = e.Bounds;
            using (SolidBrush background = new SolidBrush(selected ? Color.FromArgb(76, 62, 174) : Theme.Field))
                e.Graphics.FillRectangle(background, row);
            if (selected)
                using (SolidBrush accent = new SolidBrush(Theme.AccentLight)) e.Graphics.FillRectangle(accent, row.X, row.Y + 5, 3, Math.Max(1, row.Height - 10));
            string title = item == null ? aircraftList.Items[e.Index].ToString() : item.Display;
            string meta = item == null ? "" : item.Nation + "  •  RANK " + item.Rank.ToString(CultureInfo.InvariantCulture) + "  •  " + item.Kind;
            using (Font titleFont = new Font("Segoe UI Semibold", 8.8f))
            using (Font metaFont = new Font("Segoe UI", 7.2f))
            using (SolidBrush titleBrush = new SolidBrush(Theme.Text))
            using (SolidBrush metaBrush = new SolidBrush(selected ? Color.FromArgb(213, 220, 255) : Theme.Muted))
            {
                e.Graphics.DrawString(title, titleFont, titleBrush, new RectangleF(row.X + 10, row.Y + 2, row.Width - 14, 16));
                e.Graphics.DrawString(meta, metaFont, metaBrush, new RectangleF(row.X + 10, row.Y + 17, row.Width - 14, 13));
            }
            e.DrawFocusRectangle();
        }

        private void SelectDefaults()
        {
            nationFilter.Items.Add("All Nations");
            foreach (string nation in aircraft.Select(a => a.Nation).Distinct().OrderBy(x => x)) nationFilter.Items.Add(nation);
            rankFilter.Items.Add("Any");
            for (int i = 1; i <= Math.Max(9, aircraft.Max(a => a.Rank)); i++) rankFilter.Items.Add("Rank " + i.ToString(CultureInfo.InvariantCulture));
            vehicleFilter.Items.Add("All Types");
            foreach (string kind in aircraft.Select(a => a.Kind).Where(x => !String.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
                vehicleFilter.Items.Add(kind == "Aircraft" ? "Aircraft" : kind + "s");
            categoryFilter.Items.Add("All Weapon Types");
            foreach (string category in globalWeapons.Select(w => w.Category).Distinct().OrderBy(x => x)) categoryFilter.Items.Add(category);
            weaponNationFilter.Items.Add("All Nations");
            foreach (string nation in aircraft.Select(a => a.Nation).Distinct().OrderBy(x => x)) weaponNationFilter.Items.Add(nation);
            nationFilter.SelectedIndex = 0;
            rankFilter.SelectedIndex = 0;
            vehicleFilter.SelectedIndex = 0;
            categoryFilter.SelectedIndex = 0;
            weaponNationFilter.SelectedIndex = 0;
            sortFilter.SelectedIndex = 0;
            FilterAircraft();
            Aircraft defaultAircraft = aircraftList.Items.Cast<object>().OfType<Aircraft>().FirstOrDefault(a => a.Id == "ef_2000_typhoon_aesa");
            if (defaultAircraft != null) aircraftList.SelectedItem = defaultAircraft;
            SelectComboById(airTargetBox, "j_10c");
            SelectComboById(groundTargetBox, "ussr_bmpt");
            SelectComboById(shipTargetBox, "jp_battleship_yamato");
        }

        private static void SelectComboById(ComboBox combo, string id)
        {
            foreach (object item in combo.Items)
            {
                Aircraft a = item as Aircraft;
                TargetUnit t = item as TargetUnit;
                if ((a != null && a.Id == id) || (t != null && t.Id == id)) { combo.SelectedItem = item; return; }
            }
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private Aircraft SelectedAircraft { get { return aircraftList.SelectedItem as Aircraft; } }

        internal static bool IsGroundVehicle(Aircraft item)
        {
            return item != null && String.Equals(item.Kind, "Ground Vehicle", StringComparison.OrdinalIgnoreCase);
        }

        private void FilterAircraft()
        {
            if (aircraftList == null || nationFilter == null || rankFilter == null || vehicleFilter == null) return;
            string keep = SelectedAircraft == null ? null : SelectedAircraft.Id;
            string search = aircraftSearch.Text.Trim();
            string nation = nationFilter.SelectedIndex <= 0 ? null : nationFilter.SelectedItem as string;
            int rank = rankFilter.SelectedIndex <= 0 ? 0 : rankFilter.SelectedIndex;
            string vehicle = vehicleFilter.SelectedIndex <= 0 ? null : (vehicleFilter.SelectedItem as string ?? "").TrimEnd('s');
            IEnumerable<Aircraft> query = aircraft;
            if (!String.IsNullOrEmpty(search)) query = query.Where(a => a.Display.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 || a.Id.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!String.IsNullOrEmpty(nation)) query = query.Where(a => a.Nation == nation);
            if (rank > 0) query = query.Where(a => a.Rank == rank);
            if (!String.IsNullOrEmpty(vehicle)) query = query.Where(a => a.Kind.Equals(vehicle, StringComparison.OrdinalIgnoreCase));
            List<Aircraft> filtered = query.OrderByDescending(a => a.Rank).ThenBy(a => a.Display).ToList();
            aircraftList.BeginUpdate();
            aircraftList.Items.Clear();
            aircraftList.Items.AddRange(filtered.Cast<object>().ToArray());
            aircraftList.EndUpdate();
            Aircraft previous = filtered.FirstOrDefault(a => a.Id == keep);
            if (previous != null) aircraftList.SelectedItem = previous;
            else if (filtered.Count > 0) aircraftList.SelectedIndex = 0;
        }

        private void AircraftChanged()
        {
            Aircraft selected = SelectedAircraft;
            assignments.Clear();
            selectedPylon = null;
            preview.Aircraft = selected;
            preview.Invalidate();
            BuildPylonStrip();
            UpdateAircraftSettingsButton();
            if (selected != null)
                SetStatus("VEHICLE READY — " + selected.Display + "  •  " + pylonButtons.Count.ToString(CultureInfo.InvariantCulture) + " editable stations", false);
        }

        private static bool IsFpvDrone(Aircraft item)
        {
            return item != null && item.Id.Equals("uav_inf_fpv_strike_drone", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsHelicopter(Aircraft item, string unitBlk)
        {
            if (item != null && String.Equals(item.Kind, "Helicopter", StringComparison.OrdinalIgnoreCase)) return true;
            return Regex.IsMatch(unitBlk ?? "", @"(?i)hellicopters_metaparts|(?m)^\s*helicopter\s*\{");
        }

        internal static bool LooksLikeJetAircraft(string unitBlk)
        {
            string text = unitBlk ?? "";
            return Regex.IsMatch(text, @"(?i)(jet_(?:fighter|bomber)_metaparts|armor_jet_engine|(?:standard|afterburner|start)ExhaustFxType:t\s*=\s*""jet_)");
        }

        internal static int ResolveSpawnSpeed(Aircraft item, string unitBlk)
        {
            if (IsFpvDrone(item)) return 100;
            // Helicopters must spawn from a stationary hover. The fixed-wing mission
            // speed field is not interpreted as km/h for helicopter usermodels; using
            // 180 here launches an Apache at destructive overspeed.
            if (IsHelicopter(item, unitBlk)) return 0;
            if (LooksLikeJetAircraft(unitBlk))
                return item != null && item.Rank > 0 && item.Rank <= 5 ? 700 : 1100;
            return 450;
        }

        internal static int ResolveConfiguredSpawnSpeed(Aircraft item, string unitBlk, MissionSettings mission)
        {
            if (mission == null) return ResolveSpawnSpeed(item, unitBlk);
            // Airport takeoff must always start stationary on the runway.
            if (mission.SpawnMode != null && mission.SpawnMode.Equals("airport", StringComparison.OrdinalIgnoreCase)) return 0;
            if (!mission.SpawnSpeedAuto) return Math.Max(0, Math.Min(1100, mission.SpawnSpeedKmh));
            int speed = ResolveSpawnSpeed(item, unitBlk);
            if (speed <= 0) return speed;
            // Airframe structural limits are not exposed directly, so clamp the
            // spawn speed to the lowest published maxSpeed (m/s) in the flight
            // model. Overspeeding a fragile airframe at spawn tears it apart.
            double lowest = double.MaxValue;
            foreach (Match match in Regex.Matches(unitBlk ?? "", @"(?m)^\s*maxSpeed:r\s*=\s*([0-9.]+)"))
            {
                double value;
                if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value > 0)
                    lowest = Math.Min(lowest, value);
            }
            if (lowest < double.MaxValue)
                speed = Math.Min(speed, (int)Math.Round(lowest * 3.6));
            return speed;
        }

        private AircraftSettings GetAircraftSettings(Aircraft item)
        {
            if (item == null) return new AircraftSettings();
            AircraftSettings settings;
            if (!aircraftSettings.TryGetValue(item.Id, out settings))
            {
                settings = new AircraftSettings();
                aircraftSettings[item.Id] = settings;
            }
            return settings;
        }

        private static string AircraftSettingsStorePath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniversalTestLab", "aircraft_settings.txt"); }
        }

        private void LoadAircraftSettings()
        {
            try
            {
                Dictionary<string, object> all = ConfigStore.GetObject("aircraft_settings");
                if (all == null) return;
                foreach (KeyValuePair<string, object> pair in all)
                {
                    Dictionary<string, object> obj = pair.Value as Dictionary<string, object>;
                    if (obj == null) continue;
                    AircraftSettings settings = PresetStore.DeserializeSettingsJson(obj);
                    if (settings == null) continue;
                    aircraftSettings[pair.Key] = settings;
                }
            }
            catch { }
        }

        private void PersistAircraftSettings()
        {
            try
            {
                Dictionary<string, object> all = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, AircraftSettings> pair in aircraftSettings.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                    all[pair.Key] = PresetStore.SerializeSettingsJson(pair.Value);
                ConfigStore.SetObject("aircraft_settings", all);
                ConfigStore.Save();
            }
            catch { }
        }

        private void ShowAircraftSettings()
        {
            Aircraft selected = SelectedAircraft;
            if (selected == null)
            {
                MessageBox.Show(this, "Select an aircraft or helicopter first.", "Universal Test Lab", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            List<AircraftModification> available = modifications.Where(x => x.AircraftId.Equals(selected.Id, StringComparison.OrdinalIgnoreCase)).ToList();
            using (AircraftSettingsForm dialog = new AircraftSettingsForm(selected, available, GetAircraftSettings(selected), IsHelicopter(selected, null)))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result == null) return;
                aircraftSettings[selected.Id] = dialog.Result;
                PersistAircraftSettings();
            }
            UpdateAircraftSettingsButton();
            SetStatus("Modification and countermeasure settings updated for " + selected.Display + ".", false);
        }

        private void UpdateAircraftSettingsButton()
        {
            if (aircraftSettingsButton == null) return;
            Aircraft selected = SelectedAircraft;
            AircraftSettings settings;
            bool custom = selected != null && aircraftSettings.TryGetValue(selected.Id, out settings) &&
                (!settings.UseAllModifications || settings.OverrideCountermeasures || !settings.FullFuel || settings.GunBeltSelections.Count > 0);
            aircraftSettingsButton.Text = custom ? "MODULES  •" : "MODULES";
            aircraftSettingsButton.ForeColor = custom ? Theme.AccentLight : Theme.Text;
        }

        internal static string ApplyPlayerSpawnSpeed(string mission, int speedKmh)
        {
            if (String.IsNullOrEmpty(mission)) throw new ArgumentException("Mission text is required.", "mission");
            if (speedKmh < 0) throw new ArgumentOutOfRangeException("speedKmh");
            Regex marker = new Regex(@"(?m)^(\s*)speed:r=1100\s*$");
            if (!marker.IsMatch(mission)) throw new InvalidOperationException("Player spawn-speed markers are missing from the mission template.");
            return marker.Replace(mission, delegate(Match match)
            {
                return match.Groups[1].Value + "speed:r=" + speedKmh.ToString(CultureInfo.InvariantCulture);
            });
        }

        internal static string ApplyPlayerFuel(string mission, AircraftSettings settings)
        {
            if (String.IsNullOrEmpty(mission)) throw new ArgumentException("Mission text is required.", "mission");
            int percent = settings == null || settings.FullFuel ? 100 : Math.Max(8, Math.Min(100, (int)Math.Round(settings.FuelMinutes * 100.0 / 60.0)));
            Regex marker = new Regex(@"(?m)^(\s*)fuel:r=100\s*$");
            if (!marker.IsMatch(mission)) throw new InvalidOperationException("Player fuel markers are missing from the mission template.");
            return marker.Replace(mission, delegate(Match match)
            {
                return match.Groups[1].Value + "fuel:r=" + percent.ToString(CultureInfo.InvariantCulture);
            });
        }

        internal static string ApplyPlayerGunBelts(string mission, AircraftSettings settings)
        {
            if (String.IsNullOrEmpty(mission)) throw new ArgumentException("Mission text is required.", "mission");
            if (settings == null || settings.GunBeltSelections.Count == 0) return mission;
            BlockSpan player = BlkTools.UnitBlockByName(mission, "You");
            if (player == null) throw new InvalidOperationException("Player unit is missing from the mission template.");
            string block = player.Text;
            foreach (KeyValuePair<int, string> selection in settings.GunBeltSelections.OrderBy(x => x.Key))
            {
                if (selection.Key < 0 || selection.Key > 3 || String.IsNullOrWhiteSpace(selection.Value)) continue;
                string field = "bullets" + selection.Key.ToString(CultureInfo.InvariantCulture);
                Regex marker = new Regex("(?m)^(\\s*)" + Regex.Escape(field) + ":t\\s*=\\s*\"[^\"]*\"\\s*$");
                string safe = selection.Value.Replace("\"", "");
                if (marker.IsMatch(block))
                    block = marker.Replace(block, delegate(Match match) { return match.Groups[1].Value + field + ":t=\"" + safe + "\""; }, 1);
                else
                {
                    int open = block.IndexOf('{');
                    block = block.Insert(open + 1, Environment.NewLine + "    " + field + ":t=\"" + safe + "\"");
                }
            }
            return mission.Substring(0, player.Start) + block + mission.Substring(player.End + 1);
        }

        private void BuildPylonStrip()
        {
            pylonStrip.SuspendLayout();
            pylonStrip.Controls.Clear();
            pylonButtons.Clear();
            Aircraft selected = SelectedAircraft;
            if (selected != null)
            {
                foreach (PylonSlot pylon in pylons.Where(p => p.AircraftId == selected.Id).OrderBy(p => p.Order).ThenBy(p => p.Slot))
                {
                    Button button = new Button { Width = 103, Height = 78, Tag = pylon, Margin = new Padding(3), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 8.2f) };
                    Theme.Button(button, false);
                    button.Click += delegate(object sender, EventArgs e) { SelectPylon((PylonSlot)((Button)sender).Tag); };
                    pylonStrip.Controls.Add(button);
                    pylonButtons[pylon.Slot] = button;
                }
            }
            pylonStrip.ResumeLayout();
            if (pylonButtons.Count > 0) SelectPylon((PylonSlot)pylonButtons.Values.First().Tag);
            else
            {
                stationLabel.Text = IsFpvDrone(selected)
                    ? "FPV DRONE — no external pylons. Fly into the target to detonate the built-in HEAT warhead."
                    : (selected != null && selected.Id.StartsWith("nt_", StringComparison.OrdinalIgnoreCase)
                        ? "This Nuclear Escalation variant has a fixed event loadout. Choose the standard aircraft to edit its bomb-bay stations."
                        : "This vehicle has no editable weapon stations in the current catalog.");
                RefreshPylons();
                RefreshWeaponCatalog();
            }
        }

        private void SelectPylon(PylonSlot pylon)
        {
            selectedPylon = pylon;
            stationLabel.Text = "STATION " + pylon.Slot + " — choose a compatible weapon, or enable Injection for the full catalog.";
            RefreshPylons();
            RefreshWeaponCatalog();
        }

        private void RefreshPylons()
        {
            double total = 0;
            foreach (KeyValuePair<int, Button> pair in pylonButtons)
            {
                PylonAssignment assignment;
                bool has = assignments.TryGetValue(pair.Key, out assignment);
                string weapon = has ? ShortName(assignment.Weapon.Name, 18) : "EMPTY";
                pair.Value.Text = "STATION " + pair.Key + "\r\n" + weapon + (has && assignment.Injected ? "\r\nINJECTED" : "");
                pair.Value.BackColor = selectedPylon != null && selectedPylon.Slot == pair.Key ? Theme.AccentDark : (has ? Color.FromArgb(34, 92, 98) : Theme.Surface3);
                pair.Value.FlatAppearance.BorderColor = selectedPylon != null && selectedPylon.Slot == pair.Key ? Theme.AccentLight : Theme.Border;
                if (has) total += assignment.Weapon.TotalMass;
            }
            Aircraft selected = SelectedAircraft;
            string limit = selected != null && selected.MaxLoad > 0 ? " / " + selected.MaxLoad.ToString("0", CultureInfo.InvariantCulture) + " kg" : "";
            massLabel.Text = "MASS: " + total.ToString("0.0", CultureInfo.InvariantCulture) + " kg" + limit;
        }

        private static string ShortName(string value, int length)
        {
            if (String.IsNullOrEmpty(value)) return "WEAPON";
            string cleaned = Regex.Replace(value, @"\s+(air-to-air|air-to-ground|guided)?\s*(missile|missiles|bomb|bombs)$", "", RegexOptions.IgnoreCase).Trim();
            return cleaned.Length <= length ? cleaned.ToUpperInvariant() : cleaned.Substring(0, Math.Max(3, length - 1)).ToUpperInvariant() + "…";
        }

        private void RefreshWeaponCatalog()
        {
            if (weaponList == null) return;
            weaponList.BeginUpdate();
            weaponList.Items.Clear();
            weaponList.Groups.Clear();
            if (selectedPylon == null || SelectedAircraft == null) { weaponList.EndUpdate(); return; }
            IEnumerable<DonorWeapon> source = injectionToggle.Checked
                ? globalWeapons
                : nativeWeapons.Where(w => w.AircraftId == SelectedAircraft.Id && w.Slot == selectedPylon.Slot)
                    .GroupBy(w => w.Blk + "|" + w.Bullets).Select(g => g.First());
            string search = weaponSearch.Text.Trim();
            if (!String.IsNullOrEmpty(search)) source = source.Where(w => w.Name.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 || w.Category.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 || w.Blk.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            string category = categoryFilter.SelectedIndex <= 0 ? null : categoryFilter.SelectedItem as string;
            if (!String.IsNullOrEmpty(category)) source = source.Where(w => w.Category == category);
            string weaponNation = weaponNationFilter.SelectedIndex <= 0 ? null : weaponNationFilter.SelectedItem as string;
            if (!String.IsNullOrEmpty(weaponNation)) source = source.Where(w => (w.Nations ?? "").Split('|').Any(n => n.Equals(weaponNation, StringComparison.OrdinalIgnoreCase)));
            if (sortFilter.SelectedIndex == 1) source = source.OrderByDescending(w => w.TotalMass).ThenBy(w => w.Name);
            else if (sortFilter.SelectedIndex == 2) source = source.OrderBy(w => w.Name).ThenBy(w => w.TotalMass);
            else source = source.OrderBy(w => w.TotalMass).ThenBy(w => w.Name);
            Dictionary<string, ListViewGroup> groups = new Dictionary<string, ListViewGroup>();
            foreach (DonorWeapon weapon in source.Take(5000))
            {
                ListViewGroup group;
                if (!groups.TryGetValue(weapon.Category, out group))
                {
                    group = new ListViewGroup(weapon.Category, HorizontalAlignment.Left);
                    groups[weapon.Category] = group;
                    weaponList.Groups.Add(group);
                }
                ListViewItem item = new ListViewItem(weapon.Name, group);
                item.SubItems.Add(weapon.Category);
                item.SubItems.Add(weapon.Bullets.ToString(CultureInfo.InvariantCulture));
                item.SubItems.Add(weapon.TotalMass > 0 ? weapon.TotalMass.ToString("0.0", CultureInfo.InvariantCulture) + " kg" : "—");
                bool risky = injectionToggle.Checked && IsRiskyForSelectedPylon(weapon);
                item.SubItems.Add(risky ? "RISK" : (injectionToggle.Checked ? "INJECT" : "NATIVE"));
                item.Tag = weapon;
                if (weapon.Category == "Nuclear Weapons") item.ForeColor = Theme.Accent;
                else if (risky) item.ForeColor = Color.FromArgb(225, 142, 90);
                weaponList.Items.Add(item);
            }
            weaponList.EndUpdate();
        }

        private void AssignSelectedWeapon()
        {
            if (selectedPylon == null || weaponList.SelectedItems.Count == 0) return;
            DonorWeapon weapon = weaponList.SelectedItems[0].Tag as DonorWeapon;
            if (weapon == null) return;
            assignments[selectedPylon.Slot] = new PylonAssignment { Pylon = selectedPylon, Weapon = weapon, Injected = injectionToggle.Checked };
            RefreshPylons();
        }

        private bool IsRiskyForSelectedPylon(DonorWeapon weapon)
        {
            return selectedPylon != null && IsRiskyForPylon(selectedPylon, weapon);
        }

        private bool IsRiskyForPylon(PylonSlot pylon, DonorWeapon weapon)
        {
            if (pylon == null || weapon == null) return true;
            IEnumerable<DonorWeapon> native = nativeWeapons.Where(w => w.AircraftId == pylon.AircraftId && w.Slot == pylon.Slot);
            if (native.Any(w => String.Equals(w.Blk, weapon.Blk, StringComparison.OrdinalIgnoreCase) && w.Bullets == weapon.Bullets)) return false;
            if (!native.Any(w => String.Equals(w.Trigger, weapon.Trigger, StringComparison.OrdinalIgnoreCase))) return true;
            string path = (weapon.Blk ?? "").Replace('\\', '/').ToLowerInvariant();
            return path.Contains("/containers/") || path.Contains("/equipment/") || path.Contains("/payloadguns/");
        }

        private bool ConfirmRiskyLoadout()
        {
            Aircraft selected = SelectedAircraft;
            if (selected == null) return false;
            double total = assignments.Values.Sum(a => a.Weapon.TotalMass);
            if (selected.MaxLoad > 0 && total > selected.MaxLoad)
            {
                const string overload = "The configured weapon mass exceeds this vehicle's external load limit. Reduce the loadout before building the mission.";
                if (workspaceOperation) throw new InvalidOperationException(overload);
                MessageBox.Show(this, overload, "Loadout limit exceeded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            List<PylonAssignment> risky = assignments.Values.Where(a => a.Injected && IsRiskyForPylon(a.Pylon, a.Weapon)).OrderBy(a => a.Pylon.Order).ToList();
            if (risky.Count == 0) return true;
            string stations = String.Join(Environment.NewLine, risky.Take(8).Select(a => "• Station " + a.Pylon.Slot + " — " + a.Weapon.Name).ToArray());
            if (risky.Count > 8) stations += Environment.NewLine + "• …and " + (risky.Count - 8).ToString(CultureInfo.InvariantCulture) + " more";
            string message = "The game has no native mount of this weapon type on the selected stations:" + Environment.NewLine + Environment.NewLine + stations + Environment.NewLine + Environment.NewLine +
                "These Frankenstein mounts may work, but a structurally incompatible pylon can prevent the mission from loading. Build anyway?";
            if (WorkspaceConfirmation != null) return WorkspaceConfirmation("Injection Compatibility Warning", message);
            return MessageBox.Show(this, message, "Injection compatibility warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        private void ClearSelectedStation()
        {
            if (selectedPylon == null) return;
            assignments.Remove(selectedPylon.Slot);
            RefreshPylons();
        }

        private static string DetectGameFolder()
        {
            string applicationFolder = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string[] candidates =
            {
                SettingsStore.LoadGameFolder(),
                applicationFolder,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WarThunder"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Steam\steamapps\common\War Thunder"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"War Thunder")
            };
            foreach (string candidate in candidates)
                if (!String.IsNullOrWhiteSpace(candidate) && File.Exists(Path.Combine(candidate, "aces.vromfs.bin"))) return candidate;
            return applicationFolder;
        }

        private string ValidGameRoot()
        {
            string root = gameFolder.Text.Trim().Trim('"');
            if (!File.Exists(Path.Combine(root, "aces.vromfs.bin"))) throw new InvalidOperationException("The selected folder does not contain aces.vromfs.bin. Select the War Thunder root folder.");
            root = Path.GetFullPath(root);
            SettingsStore.SaveGameFolder(root);
            return root;
        }

        private void BrowseFolder()
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the War Thunder root folder";
                dialog.SelectedPath = Directory.Exists(gameFolder.Text) ? gameFolder.Text : "";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    gameFolder.Text = dialog.SelectedPath;
                    SettingsStore.SaveGameFolder(dialog.SelectedPath);
                }
            }
        }

        private static void WriteBytes(string path, byte[] data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".new";
            File.WriteAllBytes(temp, data);
            if (File.Exists(path))
            {
                string backup = path + ".bak";
                try { File.Replace(temp, path, backup, true); }
                catch { File.Copy(temp, path, true); File.Delete(temp); }
            }
            else File.Move(temp, path);
        }

        private void InstallBase(string root, bool overwrite)
        {
            string mission = Path.Combine(root, MissionFolderRelative, StarterMissionName);
            if (overwrite || !File.Exists(mission)) WriteBytes(mission, Embedded.Bytes("UTL.universal_test_lab.blk"));
            WriteEmbeddedWhenNeeded(Path.Combine(root, MissionFolderRelative, "usr.csv"), "UTL.usr.csv", overwrite);
            string obsoleteLocalization = Path.Combine(root, MissionFolderRelative, "usr_universal_test_lab.csv");
            if (File.Exists(obsoleteLocalization))
            {
                try { File.Delete(obsoleteLocalization); }
                catch { }
            }
            WriteEmbeddedWhenNeeded(Path.Combine(root, @"content\pkg_user\levels\Clean_Testdrive.bin"), "UTL.Clean_Testdrive.bin", overwrite);
            WriteEmbeddedWhenNeeded(Path.Combine(root, @"content\pkg_user\levels\Clean_Testdrive.blk"), "UTL.Clean_Testdrive.blk", overwrite);
            WriteEmbeddedWhenNeeded(Path.Combine(root, @"content\pkg_user\levels\Clean_Testdrive_map.png"), "UTL.Clean_Testdrive_map.png", overwrite);
            WriteEmbeddedWhenNeeded(Path.Combine(root, @"content\pkg_user\gameData\flightModels\utl_safe_player.blk"), "UTL.utl_safe_player.blk", overwrite);
            CleanLegacyMissionMenus(root);
        }

        private static void CleanLegacyMissionMenus(string root)
        {
            string userMissions = Path.Combine(root, "UserMissions");
            if (!Directory.Exists(userMissions)) return;
            foreach (string path in Directory.GetFiles(userMissions, "*.blk", SearchOption.AllDirectories))
            {
                string text;
                try { text = File.ReadAllText(path); }
                catch { continue; }
                if (text.IndexOf("UniversalTestLab", StringComparison.Ordinal) < 0 &&
                    text.IndexOf("CleanTestDrive", StringComparison.Ordinal) < 0 &&
                    text.IndexOf("chapter:t=\"TestDrive\"", StringComparison.Ordinal) < 0 &&
                    text.IndexOf("name:t=\"universal_test_lab\"", StringComparison.Ordinal) < 0) continue;
                string cleaned = BlkTools.CleanLegacyMenuKeys(text);
                if (!cleaned.Equals(text, StringComparison.Ordinal)) WriteBytes(path, new UTF8Encoding(false).GetBytes(cleaned));
            }
        }

        private static void WriteEmbeddedWhenNeeded(string path, string resource, bool overwrite)
        {
            if (overwrite || !File.Exists(path)) WriteBytes(path, Embedded.Bytes(resource));
        }

        private void InstallClicked()
        {
            try
            {
                InstallBase(ValidGameRoot(), true);
                SetStatus("Base mission and clean test range installed.", false);
                MessageBox.Show(this, "Base mission installed. Close the User Missions tab in War Thunder and open it again; no game restart is required.", "Universal Test Lab", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private static string ExtractResourceTool()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniversalTestLab", "tools");
            Directory.CreateDirectory(dir);
            string exe = Path.Combine(dir, "wt_ext_cli.exe");
            WriteBytes(exe, Embedded.Bytes("UTL.wt_ext_cli.exe"));
            WriteBytes(Path.Combine(dir, "WT_EXT_LICENSE.txt"), Embedded.Bytes("UTL.WT_EXT_LICENSE.txt"));
            return exe;
        }

        internal static string NormalizeGameResourcePath(string relative)
        {
            if (String.IsNullOrWhiteSpace(relative)) throw new ArgumentException("Game resource path cannot be empty.", "relative");
            return relative.Trim().Replace('\\', '/').TrimStart('/').ToLowerInvariant();
        }

        private static string ExtractGameBlk(string root, string relative)
        {
            string normalizedRelative = NormalizeGameResourcePath(relative);
            // Zero-extract fast path: a full pre-extracted game-data tree laid out as
            // <tree>/aces.vromfs.bin_u/gamedata/... (the project's universal_units_data /
            // universal_weapons_data extraction). Reading it avoids launching wt_ext_cli
            // per uncached resource - the per-weapon-swap stall.
            foreach (string tree in FindPreExtractedTrees())
            {
                string candidate = Path.Combine(tree, "aces.vromfs.bin_u", normalizedRelative.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate)) return candidate;
            }
            // Deterministic per-resource cache directory (stable across calls
            // and sessions) so selecting vehicles / building missions does not
            // re-launch wt_ext_cli for the same game resource every time.
            string cacheRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniversalTestLab", "cache");
            string cacheDir = Path.Combine(cacheRoot, "r" + GetStableHash(normalizedRelative).ToString("x8"));
            string resultPath = Path.Combine(cacheDir, "aces.vromfs.bin_u", normalizedRelative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(resultPath)) return resultPath;
            Directory.CreateDirectory(cacheDir);
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = ExtractResourceTool(),
                Arguments = "unpack_vromf --input_dir_or_file \"" + Path.Combine(root, "aces.vromfs.bin") + "\" --output_dir \"" + cacheDir + "\" --format BlkText --folder \"" + normalizedRelative + "\" --continue Quiet",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (Process process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0) throw new InvalidOperationException("Could not read game resource: " + normalizedRelative + Environment.NewLine + output + Environment.NewLine + error);
            }
            if (!File.Exists(resultPath))
                throw new FileNotFoundException("The War Thunder folder is valid, but this game resource was not found after extraction:" + Environment.NewLine + normalizedRelative, resultPath);
            return resultPath;
        }

        private static string[] preExtractedTrees = null;
        // Project-local full vromfs extractions (universal_units_data for gamedata/units,
        // universal_weapons_data for gamedata/weapons). Both may sit next to the exe's
        // parent (project root) or in the exe folder itself. Auto-detected once.
        private static string[] FindPreExtractedTrees()
        {
            if (preExtractedTrees != null) return preExtractedTrees;
            preExtractedTrees = new string[0];
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                List<string> found = new List<string>();
                string[] roots =
                {
                    Path.GetFullPath(Path.Combine(baseDir, "..")),
                    Path.GetFullPath(Path.Combine(baseDir, "..", "..")),
                    Path.GetFullPath(baseDir)
                };
                string[] names = { "universal_units_data", "universal_weapons_data" };
                foreach (string root in roots)
                {
                    foreach (string name in names)
                    {
                        string candidate = Path.Combine(root, name);
                        try
                        {
                            if (Directory.Exists(Path.Combine(candidate, "aces.vromfs.bin_u", "gamedata")) && !found.Contains(candidate))
                                found.Add(candidate);
                        }
                        catch { }
                    }
                }
                preExtractedTrees = found.ToArray();
            }
            catch { }
            return preExtractedTrees;
        }

        private static uint GetStableHash(string value)
        {
            if (value == null) return 0;
            uint hash = 2166136261;
            foreach (char c in value) { hash ^= c; hash *= 16777619; }
            return hash;
        }

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

            // Publish dependencies first. The game must never observe a playable unit
            // whose gun BLK is still absent or was deleted with the previous token.
            if (useCustomCannon) WriteBytes(cannonOut, new UTF8Encoding(false).GetBytes(cannon));
            WriteBytes(unitOut, new UTF8Encoding(false).GetBytes(unit));
            GeneratedAircraft generated = new GeneratedAircraft { ClassId = classId, PresetId = !String.IsNullOrWhiteSpace(stockPreset) ? stockPreset : target.DefaultPreset, ModelId = BlkTools.Field(nativeUnit, "model", "t") ?? target.Id, FlightModelPath = unitOut, PresetPath = useCustomCannon ? cannonOut : unitOut, SpawnSpeedKmh = 0, IsGround = true, UserSightFolder = generatedSightFolder };
            foreach (GroundAmmoLoadout loadout in missionAmmo) generated.GroundAmmoLoadouts.Add(loadout.Copy());
            if (useCustomCannon) generated.AuxiliaryPaths.Add(cannonOut);
            return generated;
        }

        // True when a commonWeapons Weapon is a camera-aiming dummy (dummy:b = true).
        // These vehicles (launcher/SAM trucks, e.g. Buk/Osa/Tor TELs) mount the real
        // weapon on a separate gunner1 Weapon - swapping the dummy would hang the
        // injected gun on the observation sight instead of the launcher.
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
                text = BlkTools.AccelerateRangeRecovery(text, combinedMap == null, MissionSettings.Current.TargetRespawnDelaySeconds, MissionSettings.Current.RearmSeconds);
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

    internal sealed class PresetManagerForm : Form
    {
        private readonly MainForm main;
        private readonly List<SavedPreset> presets;
        private readonly ListView list;
        private readonly TextBox presetName;

        
        public PresetManagerForm(MainForm owner)
        {
            main = owner;
            presets = PresetStore.Load();
            Text = "Custom Presets";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(720, 500);
            MinimumSize = new Size(620, 420);
            BackColor = Theme.Window;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.2f);

            GradientBackdrop backdrop = new GradientBackdrop { Dock = DockStyle.Fill };
            Controls.Add(backdrop);
            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, Padding = new Padding(18), BackColor = Color.Transparent };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            backdrop.Controls.Add(root);

            Label heading = Theme.Label("CUSTOM LOADOUT PRESETS", true);
            heading.Font = new Font("Segoe UI Semibold", 15f);
            heading.ForeColor = Theme.AccentLight;
            root.Controls.Add(heading, 0, 0);
            root.Controls.Add(Theme.Label("PRESET NAME", false), 0, 1);
            presetName = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Text = main.CurrentAircraftName };
            Theme.Input(presetName);
            root.Controls.Add(presetName, 0, 2);

            list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, BorderStyle = BorderStyle.None, BackColor = Theme.Field, ForeColor = Theme.Text };
            list.Columns.Add("Preset", 360);
            list.Columns.Add("Aircraft", 260);
            list.DoubleClick += delegate { LoadSelected(); };
            root.Controls.Add(list, 0, 3);

            TableLayoutPanel buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Padding = new Padding(0, 8, 0, 0) };
            for (int i = 0; i < 4; i++) buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            Button save = DialogButton("SAVE CURRENT", true); save.Click += delegate { SaveCurrent(); }; buttons.Controls.Add(save, 0, 0);
            Button load = DialogButton("LOAD SELECTED", false); load.Click += delegate { LoadSelected(); }; buttons.Controls.Add(load, 1, 0);
            Button delete = DialogButton("DELETE", false); delete.Click += delegate { DeleteSelected(); }; buttons.Controls.Add(delete, 2, 0);
            Button close = DialogButton("CLOSE", false); close.Click += delegate { Close(); }; buttons.Controls.Add(close, 3, 0);
            root.Controls.Add(buttons, 0, 4);
            RefreshList();
        }

        private Button DialogButton(string text, bool primary)
        {
            Button button = new Button { Text = text, Dock = DockStyle.Fill, Margin = new Padding(4, 0, 4, 0) };
            Theme.Button(button, primary);
            return button;
        }

        private SavedPreset Selected
        {
            get { return list.SelectedItems.Count == 0 ? null : list.SelectedItems[0].Tag as SavedPreset; }
        }

        private void RefreshList()
        {
            list.BeginUpdate();
            list.Items.Clear();
            foreach (SavedPreset preset in presets.OrderBy(x => x.Name))
            {
                ListViewItem row = new ListViewItem(preset.Name);
                row.SubItems.Add(main.AircraftName(preset.AircraftId));
                row.Tag = preset;
                list.Items.Add(row);
            }
            list.EndUpdate();
        }

        private void SaveCurrent()
        {
            try
            {
                string name = presetName.Text.Trim();
                if (String.IsNullOrEmpty(name)) throw new InvalidOperationException("Enter a preset name.");
                SavedPreset existing = presets.FirstOrDefault(x => x.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
                if (existing != null && MessageBox.Show(this, "Replace the existing preset named '" + existing.Name + "'?", "Replace preset", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                if (existing != null) presets.Remove(existing);
                presets.Add(main.CaptureCurrentPreset(name));
                PresetStore.Save(presets);
                RefreshList();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Custom Presets", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void LoadSelected()
        {
            try
            {
                SavedPreset selected = Selected;
                if (selected == null) throw new InvalidOperationException("Select a preset to load.");
                main.LoadSavedPreset(selected);
                Close();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Custom Presets", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void DeleteSelected()
        {
            SavedPreset selected = Selected;
            if (selected == null) return;
            if (MessageBox.Show(this, "Delete preset '" + selected.Name + "'?", "Delete preset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            presets.Remove(selected);
            PresetStore.Save(presets);
            RefreshList();
        }
    }

    internal sealed class AboutForm : Form
    {
        private const string SupportUrl = "https://buy.stripe.com/bJe00bbHB0GH0qI655fQI00";
        private const string ProjectUrl = "https://github.com/UKRAngler/Universal-Test-Lab";
        private const string AstraYoutubeUrl = "https://youtube.com/@astra-sep?si=TiMO8--EXG2zXapG";

        public AboutForm(int aircraftCount, int weaponCount)
        {
            Text = "Support Universal Test Lab";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(860, 640);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Window;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);

            GradientBackdrop backdrop = new GradientBackdrop { Dock = DockStyle.Fill };
            Controls.Add(backdrop);
            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, Padding = new Padding(24), BackColor = Color.Transparent };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            backdrop.Controls.Add(root);

            Label title = Theme.Label("UNIVERSAL TEST LAB", true);
            title.Font = new Font("Segoe UI Semibold", 20f);
            title.ForeColor = Theme.AccentLight;
            root.Controls.Add(title, 0, 0);
            Label version = Theme.Label("Public beta  •  community-inspired mission and vehicle test workspace for War Thunder", false);
            version.Font = new Font("Segoe UI", 10.5f);
            root.Controls.Add(version, 0, 1);
            TableLayoutPanel content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 8, 0, 8) };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            root.Controls.Add(content, 0, 2);

            TableLayoutPanel info = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(0, 0, 18, 0) };
            info.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            info.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            info.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            content.Controls.Add(info, 0, 0);

            Label body = Theme.Label(
                "PROJECT\r\nBuild experimental vehicles, modules, ammunition, loadouts and reusable test missions. " + aircraftCount.ToString(CultureInfo.InvariantCulture) + " vehicle entries  •  " + weaponCount.ToString(CultureInfo.InvariantCulture) + " air-weapon entries\r\n\r\n" +
                "COMMUNITY INSPIRATION\r\nIndependent work by AstraSEP, inspired by GUI and custom-mission concepts shared by community creators and YouTube channels, for example Ask3lad. They are not project contributors.\r\n\r\n" +
                "OPEN SOURCE\r\nSource and contribution information are on GitHub. The bundled wt_ext_cli component retains its Apache 2.0 license.", false);
            body.Font = new Font("Segoe UI", 10f);
            body.Padding = new Padding(8, 8, 8, 8);
            info.Controls.Add(body, 0, 0);

            Button channel = new Button { Text = "ASTRASEP ON YOUTUBE", Dock = DockStyle.Fill, Margin = new Padding(8, 3, 8, 3) };
            Theme.Button(channel, false);
            channel.Click += delegate { OpenUrl(AstraYoutubeUrl); };
            info.Controls.Add(channel, 0, 1);

            Button project = new Button { Text = "OPEN PROJECT ON GITHUB", Dock = DockStyle.Fill, Margin = new Padding(8, 3, 8, 3) };
            Theme.Button(project, false);
            project.Click += delegate { OpenUrl(ProjectUrl); };
            info.Controls.Add(project, 0, 2);

            GlassPanel supportCard = new GlassPanel { Dock = DockStyle.Fill, Margin = new Padding(6) };
            TableLayoutPanel support = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(16), BackColor = Color.Transparent };
            support.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            support.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            support.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            support.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            supportCard.Controls.Add(support);
            content.Controls.Add(supportCard, 1, 0);

            Label supportTitle = Theme.Label("SUPPORT THE PROJECT", true);
            supportTitle.TextAlign = ContentAlignment.MiddleCenter;
            supportTitle.ForeColor = Theme.AccentLight;
            support.Controls.Add(supportTitle, 0, 0);

            PictureBox qr = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(12), Cursor = Cursors.Hand };
            qr.Image = LoadEmbeddedImage("UTL.support-qr.png");
            qr.Click += delegate { OpenUrl(SupportUrl); };
            support.Controls.Add(qr, 0, 1);

            Label supportText = Theme.Label("Scan the QR code or open the secure Stripe payment page. Support is optional.", false);
            supportText.TextAlign = ContentAlignment.MiddleCenter;
            support.Controls.Add(supportText, 0, 2);

            Button stripe = new Button { Text = "SUPPORT VIA STRIPE", Dock = DockStyle.Fill, Margin = new Padding(8, 3, 8, 3) };
            Theme.Button(stripe, true);
            stripe.Click += delegate { OpenUrl(SupportUrl); };
            support.Controls.Add(stripe, 0, 3);

            Label privacy = Theme.Label("Presets stay on this PC. The application does not send loadouts or account data anywhere.", false);
            privacy.ForeColor = Theme.Good;
            root.Controls.Add(privacy, 0, 3);
            Button close = new Button { Text = "CLOSE", Dock = DockStyle.Right, Width = 140 };
            Theme.Button(close, true);
            close.Click += delegate { Close(); };
            root.Controls.Add(close, 0, 4);
        }

        private static Image LoadEmbeddedImage(string resourceName)
        {
            using (MemoryStream stream = new MemoryStream(Embedded.Bytes(resourceName)))
            using (Image source = Image.FromStream(stream))
                return new Bitmap(source);
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open the link.\r\n\r\n" + url + "\r\n\r\n" + ex.Message, "Universal Test Lab", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            // Diagnostic crash log for headless self-tests (writes next to the exe).
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                try
                {
                    string log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "selftest_crash.log");
                    File.WriteAllText(log, e.ExceptionObject == null ? "(null exception)" : e.ExceptionObject.ToString());
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
                groundMission = BlkTools.AccelerateRangeRecovery(groundMission);
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
                    
                    
                                                            groundMission.IndexOf("UTL Fast Rearm Policy", StringComparison.Ordinal) < 0 ||
                    groundMission.IndexOf("rearmTimeOnField:r=1", StringComparison.Ordinal) < 0 ||
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
