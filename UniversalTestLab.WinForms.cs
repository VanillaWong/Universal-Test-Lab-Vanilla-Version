// UniversalTestLab.WinForms.cs
// Legacy WinForms/WPF visuals and classic dialogs kept for the controller.
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
        private const string ProjectUrl = "https://github.com/VanillaWong/Universal-Test-Lab-Vanilla-Version";

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

            Button project = new Button { Text = "OPEN PROJECT ON GITHUB", Dock = DockStyle.Fill, Margin = new Padding(8, 3, 8, 3) };
            Theme.Button(project, false);
            project.Click += delegate { OpenUrl(ProjectUrl); };
            info.Controls.Add(project, 0, 1);

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
}
