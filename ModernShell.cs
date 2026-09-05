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


    // One-click era presets for the Map & Scenario window, Ask3lad style.
    // GroundIds fill the seven range positions, AirIds fill the four flying
    // hostiles (Target_Air_01 / Target_Air_02 / Heli_Target / Heli_Target_02);
    // a null AirId disables that flying slot (count 0).






    // Keeps a Slider and a numeric TextBox in sync so users can type an exact
    // value instead of dragging. The slider remains the source of truth; the
    // box normalizes on focus loss and is clamped to the slider range.




    internal sealed partial class ModernMainWindow : Window
    {
        private readonly MainForm controller;
        private readonly Grid root;
        private readonly Grid windowHost;
        private readonly Grid overlayLayer;
        private readonly Border overlayBackdrop;
        private readonly Stack<ModernDialogWindow> overlayDialogs = new Stack<ModernDialogWindow>();
        private Border titleBar;
        private TextBox gameFolder;
        private TextBox aircraftSearch;
        private ComboBox nationFilter;
        private ComboBox rankFilter;
        private ComboBox typeFilter;
        private ListBox aircraftList;
        private TextBlock vehicleCount;
        private Border previewCard;
        private Grid previewClipContent;
        private TextBlock previewName;
        private TextBlock previewMeta;
        private Grid previewAircraftVisual;
        private Grid previewHelicopterVisual;
        private Grid previewDroneVisual;
        private Grid previewGroundVisual;
        private Image previewAircraftImage;
        private Image previewHelicopterImage;
        private Image previewDroneImage;
        private Image previewGroundImage;
        private TextBlock buildTitle;
        private TextBlock buildSubtitle;
        private Border pylonCard;
        private Grid weaponFilterPanel;
        private Grid groundWorkspacePanel;
        private ComboBox groundCannonBox;
        private List<ComboBox> groundSlotBoxes = new List<ComboBox>();
        private List<TextBox> groundSlotCounts = new List<TextBox>();
        private StackPanel groundGroupsPanel;
        private List<GroundAmmoSlotGroup> groundSlotGroups = new List<GroundAmmoSlotGroup>();
        private Dictionary<string, int> groundNativeTotals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, int> groundNativeByCalibre = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private TextBlock groundAmmoPoolText;
        private bool groundHasMainWeapon;

        private sealed class GroundAmmoEntry
        {
            public GroundAmmo Ammo;
            public int Native;
            public string Text;
            public override string ToString() { return Text; }
        }

        private sealed class GroundAmmoSlotGroup
        {
            public string WeaponBlk;
            public string Display;
            public int Calibre;
            public bool IsBelt;
            public int SlotCount;
            public int MaxTotal;
            public int FirstSlot;
            public List<GroundAmmoEntry> Options = new List<GroundAmmoEntry>();
            public TextBlock TotalText;
        }
        private ComboBox ammoPresetBox;
        private List<AmmoPreset> ammoPresets = new List<AmmoPreset>();
        private ToggleButton tabVehicleButton, tabTargetsButton, tabOptionsButton, tabGarageButton, tabExperimentalButton;
        private Grid tabVehicleContent, tabTargetsContent, tabOptionsContent, tabGarageContent, tabExperimentalContent;
        private bool suppressGarageTrack;
        private ListBox garageRecentlyBox;
        private ListBox garageFavBox;
        private ListBox garagePresetBox;
        private MapPanel targetsPanel;
        private bool experimentalBuilt;
        private object experimentalPanel;
        private bool groundUpdating;
        private string groundCannonBlk;
        private bool groundCannonNative = true;
        private Button systemsButton;
        private Button flightConfigureButton;
        private Button clearStationButton;
        private Button clearAllButton;
        private Button mountButton;

        
        private TextBlock stationText;
        private TextBlock massText;
        private UniformGrid pylonPanel;
        private ToggleButton injectionToggle;
        private System.Windows.Threading.DispatcherTimer weaponSearchTimer;
        private bool weaponColumnsPending;
        private TextBox weaponSearch;
        private ComboBox categoryFilter;
        private ComboBox weaponNationFilter;
        private ComboBox sortFilter;
        private Grid weaponTableFrame;
        private Grid weaponTableClipContent;
        private ListView weaponList;
        private ComboBox airTarget;
        private ComboBox groundTarget;
        private ComboBox shipTarget;
        private ComboBox airCount;
        private ComboBox groundCount;
        private ComboBox shipCount;
        private ToggleButton hostileToggle;
        private ToggleButton samSitesToggle;
        private TextBlock samSitesMode;
        private TextBlock samSitesSelection;
        private AircraftView airTarget01;
        private AircraftView heliTarget01;
        private AircraftView heliTarget02;
        private int airTarget01Count = 1;
        private int heliTarget01Count = 3;
        private int heliTarget02Count = 2;
        private TextBlock flightProfileText;
        private TextBlock targetSummaryText;
        private TextBlock status;
        private Aircraft selectedAircraft;
        private PylonSlot selectedPylon;
        private List<AircraftView> aircraftViews;
        private readonly List<TargetView> configuredGroundTargets = new List<TargetView>();
        private bool passiveShip;
        private CombinedScenarioSettings combinedScenario = new CombinedScenarioSettings();
        private bool updatingWeaponColumns;

        public ModernMainWindow()
        {
            controller = new MainForm();
            Title = ModernText.L("Universal Test Lab — Mission Studio", "Universal Test Lab — 任务工坊");
            ModernText.Chinese = ConfigStore.GetString("language") != "en";
        Width = 1500;
            Height = 920;
            MinWidth = 1200;
            MinHeight = 640;
            WindowStartupLocation = WindowStartupLocation.Manual;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;
            Background = Brushes.Transparent;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 38,
                ResizeBorderThickness = new Thickness(7),
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(0),
                UseAeroCaptionButtons = false
            });

            root = (Grid)ModernXaml.Parse(ModernXaml.Main);
            windowHost = new Grid { ClipToBounds = true };
            windowHost.Children.Add(root);
            overlayLayer = new Grid
            {
                Visibility = Visibility.Collapsed,
                Background = Brushes.Transparent,
                ClipToBounds = true
            };
            overlayBackdrop = new Border
            {
                Background = ModernPalette.Brush("#A60A142B"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            overlayLayer.Children.Add(overlayBackdrop);
            windowHost.Children.Add(overlayLayer);
            Content = windowHost;
            BindControls();
            ApplyChromeAccent();
            LoadPreviewImages();
        ApplyXamlLocalization();
            WireEvents();
            PopulateControls();
            controller.WorkspaceConfirmation = ConfirmWorkspaceAction;
            FitToWorkingArea();
            Loaded += delegate { ModernComboSizing.Attach(this); };
            SourceInitialized += delegate { DwmGlass.Apply(this); };
            Closed += delegate { SessionSave(); controller.Dispose(); };
        }

        
    private void ApplyXamlLocalization()
    {
        ApplyXamlLocalizationNode(this);
    }

    private static void ApplyXamlLocalizationNode(DependencyObject node)
    {
        if (node == null) return;
        TextBlock tb = node as TextBlock;
        if (tb != null && tb.Text != null)
        {
            string zh;
            if (ModernText.Chinese && ModernText.XamlMap.TryGetValue(tb.Text, out zh)) tb.Text = zh;
        }
        ContentControl cc = node as ContentControl;
        if (cc != null && cc.Content is string)
        {
            string zh;
            if (ModernText.Chinese && ModernText.XamlMap.TryGetValue((string)cc.Content, out zh)) cc.Content = zh;
        }
        ContentControl tt = node as ContentControl;
        if (tt != null && tt.ToolTip is string)
        {
            string zh;
            if (ModernText.Chinese && ModernText.XamlMap.TryGetValue((string)tt.ToolTip, out zh)) tt.ToolTip = zh;
        }
        int count = VisualTreeHelper.GetChildrenCount(node);
        for (int i = 0; i < count; i++) ApplyXamlLocalizationNode(VisualTreeHelper.GetChild(node, i));
    }

    internal void ShowOverlay(ModernDialogWindow dialog)
        {
            if (dialog == null) return;
            ModernDialogWindow previous = overlayDialogs.Count > 0 ? overlayDialogs.Peek() : null;
            if (previous != null)
            {
                previous.IsHitTestVisible = false;
                previous.Effect = new BlurEffect { Radius = 6, KernelType = KernelType.Gaussian, RenderingBias = RenderingBias.Quality };
                previous.Opacity = 0.52;
            }
            else
            {
                root.IsHitTestVisible = false;
                root.Effect = new BlurEffect { Radius = 12, KernelType = KernelType.Gaussian, RenderingBias = RenderingBias.Quality };
                overlayLayer.Visibility = Visibility.Visible;
            }

            dialog.AttachOverlay(this);
            dialog.HorizontalAlignment = HorizontalAlignment.Center;
            dialog.VerticalAlignment = VerticalAlignment.Center;
            dialog.Margin = new Thickness(24);
            dialog.MaxWidth = Math.Max(480, ActualWidth - 48);
            dialog.MaxHeight = Math.Max(420, ActualHeight - 48);
            dialog.MinWidth = 0;
            dialog.MinHeight = 0;
            overlayDialogs.Push(dialog);
            overlayLayer.Children.Add(dialog);
            dialog.Focus();
            Keyboard.Focus(dialog);
        }

        internal void CloseOverlay(ModernDialogWindow dialog)
        {
            if (dialog == null || !overlayDialogs.Contains(dialog)) return;
            if (!ReferenceEquals(overlayDialogs.Peek(), dialog))
            {
                overlayLayer.Children.Remove(dialog);
                return;
            }

            overlayDialogs.Pop();
            overlayLayer.Children.Remove(dialog);
            dialog.DetachOverlay();
            if (overlayDialogs.Count > 0)
            {
                ModernDialogWindow previous = overlayDialogs.Peek();
                previous.IsHitTestVisible = true;
                previous.Effect = null;
                previous.Opacity = 1;
                previous.Focus();
            }
            else
            {
                overlayLayer.Visibility = Visibility.Collapsed;
                root.Effect = null;
                root.IsHitTestVisible = true;
            }
        }

        private T Find<T>(string name) where T : DependencyObject { return (T)root.FindName(name); }

        private void BindControls()
        {
            titleBar = Find<Border>("TitleBar");
            gameFolder = Find<TextBox>("GameFolderBox");
            aircraftSearch = Find<TextBox>("AircraftSearch");
            nationFilter = Find<ComboBox>("NationFilter");
            rankFilter = Find<ComboBox>("RankFilter");
            typeFilter = Find<ComboBox>("TypeFilter");
            aircraftList = Find<ListBox>("AircraftList");
            vehicleCount = Find<TextBlock>("VehicleCountText");
            previewCard = Find<Border>("PreviewCard");
            previewClipContent = Find<Grid>("PreviewClipContent");
            previewName = Find<TextBlock>("PreviewName");
            previewMeta = Find<TextBlock>("PreviewMeta");
            previewAircraftVisual = Find<Grid>("PreviewAircraftVisual");
            previewHelicopterVisual = Find<Grid>("PreviewHelicopterVisual");
            previewDroneVisual = Find<Grid>("PreviewDroneVisual");
            previewAircraftImage = Find<Image>("PreviewAircraftImage");
            previewHelicopterImage = Find<Image>("PreviewHelicopterImage");
            previewDroneImage = Find<Image>("PreviewDroneImage");
            buildTitle = Find<TextBlock>("BuildTitle");
            buildSubtitle = Find<TextBlock>("BuildSubtitle");
            stationText = Find<TextBlock>("StationText");
            massText = Find<TextBlock>("MassText");
            pylonPanel = Find<UniformGrid>("PylonPanel");
            pylonCard = Find<Border>("PylonCard");
            weaponFilterPanel = Find<Grid>("WeaponFilterPanel");
            injectionToggle = Find<ToggleButton>("InjectionToggle");
            weaponSearchTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            weaponSearchTimer.Tick += delegate { weaponSearchTimer.Stop(); RefreshWeapons(); };
            weaponSearch = Find<TextBox>("WeaponSearch");
            categoryFilter = Find<ComboBox>("CategoryFilter");
            weaponNationFilter = Find<ComboBox>("WeaponNationFilter");
            sortFilter = Find<ComboBox>("SortFilter");
            weaponTableFrame = Find<Grid>("WeaponTableFrame");
            weaponTableClipContent = Find<Grid>("WeaponTableClipContent");
            weaponList = Find<ListView>("WeaponList");
            airTarget = Find<ComboBox>("AirTargetBox");
            groundTarget = Find<ComboBox>("GroundTargetBox");
            shipTarget = Find<ComboBox>("ShipTargetBox");
            airCount = Find<ComboBox>("AirCountBox");
            groundCount = Find<ComboBox>("GroundCountBox");
            shipCount = Find<ComboBox>("ShipCountBox");
            hostileToggle = Find<ToggleButton>("HostileToggle");
            samSitesToggle = Find<ToggleButton>("SamSitesToggle");
            samSitesMode = Find<TextBlock>("SamSitesMode");
            samSitesSelection = Find<TextBlock>("SamSitesSelection");
            flightProfileText = Find<TextBlock>("FlightProfileText");
            targetSummaryText = Find<TextBlock>("TargetSummaryText");
            status = Find<TextBlock>("StatusText");
            systemsButton = Find<Button>("SystemsButton");
            flightConfigureButton = Find<Button>("FlightConfigureButton");
            clearStationButton = Find<Button>("ClearStationButton");
            clearAllButton = Find<Button>("ClearAllButton");
            mountButton = Find<Button>("MountButton");
        }

        private void ApplyChromeAccent()
        {
            Color accent = SystemParameters.WindowGlassColor;
            if (accent.A < 64) accent = Color.FromRgb(210, 122, 242);
            accent.A = 255;
            SolidColorBrush brush = new SolidColorBrush(accent);
            brush.Freeze();
            titleBar.Background = brush;
        }
    }

    internal abstract class ModernDialogWindow : ContentControl
    {
        protected readonly Grid DialogRoot;
        protected readonly Border ContentCard;
        private ModernMainWindow overlayOwner;
        private Window standaloneHost;
        private System.Windows.Threading.DispatcherFrame dialogFrame;
        private bool isOpen;

        public string Title { get; set; }
        public Window Owner { get; set; }
        public ResizeMode ResizeMode { get; set; }
        public WindowStartupLocation WindowStartupLocation { get; set; }
        public WindowState WindowState { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public bool? DialogResult { get; set; }

        protected ModernDialogWindow(string title, double width, double height)
        {
            Title = title;
            Width = width;
            Height = height;
            MinWidth = Math.Min(width, 720);
            MinHeight = Math.Min(height, 520);
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResize;
            Background = Brushes.Transparent;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            Focusable = true;

            DialogRoot = new Grid { Background = Brushes.Transparent };
            Grid styleSource = (Grid)ModernXaml.Parse(ModernXaml.Main);
            foreach (object key in styleSource.Resources.Keys) DialogRoot.Resources[key] = styleSource.Resources[key];

            ContentCard = new Border
            {
                Margin = new Thickness(8),
                Padding = new Thickness(20),
                CornerRadius = new CornerRadius(18),
                Background = ModernPalette.Brush("#EE34415B"),
                BorderBrush = ModernPalette.Brush(ModernPalette.Border),
                BorderThickness = new Thickness(1),
                ClipToBounds = true
            };
            DialogRoot.Children.Add(ContentCard);

            Border closeCloud = new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(13),
                Background = ModernPalette.Brush(ModernPalette.Danger),
                BorderBrush = ModernPalette.Brush("#FFFFA2BC"),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 14, 14, 0),
                Cursor = Cursors.Hand,
                ToolTip = ModernText.L("Close", "关闭"),
                Tag = "OverlayCloseCloud"
            };
            closeCloud.Child = new TextBlock
            {
                Text = "×",
                Foreground = ModernPalette.Brush("#FFFFE8EF"),
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -2, 0, 0)
            };
            closeCloud.MouseLeftButtonUp += delegate { Close(); };
            Panel.SetZIndex(closeCloud, 20);
            DialogRoot.Children.Add(closeCloud);
            Content = DialogRoot;
            Loaded += delegate { ModernComboSizing.Attach(this); };
            PreviewKeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) { e.Handled = true; Close(); }
            };
        }

        internal bool OverlayChromeReadyForSelfTest()
        {
            Border closeCloud = DialogRoot.Children.OfType<Border>().FirstOrDefault(x => String.Equals(x.Tag as string, "OverlayCloseCloud", StringComparison.Ordinal));
            SolidColorBrush closeFill = closeCloud == null ? null : closeCloud.Background as SolidColorBrush;
            SolidColorBrush dangerFill = ModernPalette.Brush(ModernPalette.Danger) as SolidColorBrush;
            return DialogRoot.RowDefinitions.Count == 0 && ContentCard.CornerRadius.TopLeft >= 18 && ContentCard.BorderThickness.Left > 0 &&
                closeCloud != null && closeCloud.CornerRadius.TopLeft >= 12 && closeCloud.BorderBrush != null && closeFill != null && dangerFill != null &&
                closeFill.Color == dangerFill.Color && closeFill.Color.A == 255;
        }

        internal void AttachOverlay(ModernMainWindow owner)
        {
            overlayOwner = owner;
            Owner = owner;
            isOpen = true;
        }

        internal void DetachOverlay()
        {
            overlayOwner = null;
        }

        public bool? ShowDialog()
        {
            System.Windows.Application app = System.Windows.Application.Current;
            ModernMainWindow main = Owner as ModernMainWindow ?? (app != null ? app.MainWindow as ModernMainWindow : null);
            DialogResult = null;
            isOpen = true;
            if (main != null)
            {
                try
                {
                    main.ShowOverlay(this);
                    dialogFrame = new System.Windows.Threading.DispatcherFrame();
                    System.Windows.Threading.Dispatcher.PushFrame(dialogFrame);
                }
                catch
                {
                    // An exception thrown by a dialog interaction while the modal frame is
                    // active can leave the overlay stack half-open and hang the window.
                    // Detach cleanly before rethrowing to the global crash handler.
                    try { Close(); } catch { }
                    throw;
                }
                finally { dialogFrame = null; }
                return DialogResult;
            }

            Window host = CreateStandaloneHost();
            host.ShowDialog();
            return DialogResult;
        }

        public void Show()
        {
            if (isOpen) return;
            DialogResult = null;
            isOpen = true;
            CreateStandaloneHost().Show();
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            ModernMainWindow main = overlayOwner;
            if (main != null) main.CloseOverlay(this);
            Window host = standaloneHost;
            standaloneHost = null;
            if (host != null) host.Close();
            if (dialogFrame != null) dialogFrame.Continue = false;
        }

        public void DragMove()
        {
            if (standaloneHost != null)
            {
                try { standaloneHost.DragMove(); }
                catch (InvalidOperationException) { }
            }
        }

        private Window CreateStandaloneHost()
        {
            if (standaloneHost != null) return standaloneHost;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Margin = new Thickness(0);
            MaxWidth = Double.PositiveInfinity;
            MaxHeight = Double.PositiveInfinity;
            Window host = new Window
            {
                Title = Title,
                Width = Width,
                Height = Height,
                MinWidth = MinWidth,
                MinHeight = MinHeight,
                WindowStartupLocation = WindowStartupLocation,
                ResizeMode = ResizeMode,
                WindowStyle = WindowStyle.None,
                Background = Brushes.Transparent,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                Content = this
            };
            if (WindowStartupLocation == WindowStartupLocation.Manual)
            {
                host.Left = Left;
                host.Top = Top;
            }
            if (Owner != null && Owner != host) host.Owner = Owner;
            WindowChrome.SetWindowChrome(host, new WindowChrome
            {
                CaptionHeight = 0,
                ResizeBorderThickness = new Thickness(7),
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(0),
                UseAeroCaptionButtons = false
            });
            host.SourceInitialized += delegate { DwmGlass.Apply(host); };
            host.Closed += delegate
            {
                if (standaloneHost == host) standaloneHost = null;
                isOpen = false;
                if (dialogFrame != null) dialogFrame.Continue = false;
            };
            standaloneHost = host;
            return host;
        }

        protected Button DialogButton(string text, bool primary)
        {
            return new Button { Content = text, Style = (Style)DialogRoot.Resources[primary ? "PrimaryButton" : "ButtonStyle"], Margin = new Thickness(4, 0, 0, 0) };
        }

        protected TextBlock Heading(string text, double size)
        {
            return new TextBlock { Text = text, FontSize = size, FontWeight = FontWeights.SemiBold, Foreground = ModernPalette.Brush(ModernPalette.Text) };
        }

        protected TextBlock Caption(string text)
        {
            return new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ModernPalette.Brush(ModernPalette.Muted) };
        }
    }

    internal sealed class ModernMissionGeneratedWindow : ModernDialogWindow
    {
        public ModernMissionGeneratedWindow(bool ground = false) : base("Mission Generated", 590, 455)
        {
            ResizeMode = ResizeMode.NoResize;
            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(82) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });
            ContentCard.Child = layout;
            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            header.ColumnDefinitions.Add(new ColumnDefinition());
            Border badge = new Border { Width = 52, Height = 52, CornerRadius = new CornerRadius(16), Background = ModernPalette.Brush(ModernPalette.Good), VerticalAlignment = VerticalAlignment.Top };
            badge.Child = new TextBlock { Text = "✓", FontSize = 26, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            header.Children.Add(badge);
            StackPanel heading = new StackPanel { Margin = new Thickness(10, 3, 0, 0) };
            heading.Children.Add(Heading("MISSION GENERATED", 21));
            heading.Children.Add(new TextBlock { Text = ground ? "The ground proxy and mission are ready." : "The hot-load mission is ready in War Thunder.", Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 4, 0, 0) });
            Grid.SetColumn(heading, 1); header.Children.Add(heading); layout.Children.Add(header);
            Border instructions = new Border { CornerRadius = new CornerRadius(14), Background = ModernPalette.Brush(ModernPalette.Field), BorderBrush = ModernPalette.Brush(ModernPalette.Border), BorderThickness = new Thickness(1), Padding = new Thickness(18), Margin = new Thickness(0, 8, 0, 10) };
            StackPanel steps = new StackPanel();
            steps.Children.Add(Heading(ground ? "RELOAD THE GROUND PROXY" : "REFRESH USER MISSIONS", 14));
            steps.Children.Add(new TextBlock { Text = ground ? "1   Exit War Thunder completely.\n\n2   Start War Thunder again.\n\n3   Open User Missions and launch the current HOT UTL mission." : "1   Close the User Missions tab.\n\n2   Open User Missions again to refresh the list.\n\n3   Launch the current HOT UTL mission.", Foreground = ModernPalette.Brush(ModernPalette.Text), FontSize = 13, Margin = new Thickness(0, 14, 0, 0) });
            steps.Children.Add(new TextBlock { Text = ground ? "A restart is required only because War Thunder caches the reserve-tank proxy." : "No game restart is required.", Foreground = ModernPalette.Brush(ModernPalette.Good), Margin = new Thickness(0, 16, 0, 0), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            steps.Children.Add(new TextBlock { Text = "Custom ground sight selected? Press Alt + F9 once in the mission to reload UserSights.", Foreground = ModernPalette.Brush(ModernPalette.Cyan), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0), FontSize = 11 });
            instructions.Child = steps; Grid.SetRow(instructions, 1); layout.Children.Add(instructions);
            Button ok = DialogButton("GOT IT", true); ok.Width = 150; ok.HorizontalAlignment = HorizontalAlignment.Right; ok.Click += delegate { Close(); }; Grid.SetRow(ok, 2); layout.Children.Add(ok);
        }
    }

    internal sealed class ModernMessageDialog : ModernDialogWindow
    {
        public ModernMessageDialog(string title, string message, string primaryText, string secondaryText, bool danger)
            : base(title, 620, 390)
        {
            ResizeMode = ResizeMode.NoResize;
            bool confirmation = !String.IsNullOrEmpty(secondaryText);
            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(76) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
            ContentCard.Child = layout;

            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            header.ColumnDefinitions.Add(new ColumnDefinition());
            Border badge = new Border
            {
                Width = 50,
                Height = 50,
                CornerRadius = new CornerRadius(15),
                Background = ModernPalette.Brush(danger ? ModernPalette.Danger : confirmation ? ModernPalette.AccentDark : ModernPalette.Good),
                VerticalAlignment = VerticalAlignment.Top
            };
            badge.Child = new TextBlock
            {
                Text = danger ? "!" : confirmation ? "?" : "✓",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            header.Children.Add(badge);
            StackPanel heading = new StackPanel { Margin = new Thickness(10, 2, 0, 0) };
            heading.Children.Add(Heading(title.ToUpperInvariant(), 20));
            heading.Children.Add(new TextBlock
            {
                Text = danger ? "The requested action could not be completed." : confirmation ? "Please confirm this action." : "The action completed successfully.",
                Foreground = ModernPalette.Brush(danger ? ModernPalette.Danger : confirmation ? ModernPalette.Cyan : ModernPalette.Good),
                Margin = new Thickness(0, 4, 0, 0)
            });
            Grid.SetColumn(heading, 1);
            header.Children.Add(heading);
            layout.Children.Add(header);

            Border messageCard = new Border
            {
                CornerRadius = new CornerRadius(14),
                Background = ModernPalette.Brush(ModernPalette.Field),
                BorderBrush = ModernPalette.Brush(ModernPalette.Border),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(18),
                Margin = new Thickness(0, 6, 0, 10)
            };
            messageCard.Child = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = ModernPalette.Brush(ModernPalette.Text),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(messageCard, 1);
            layout.Children.Add(messageCard);

            Grid footer = new Grid { HorizontalAlignment = HorizontalAlignment.Right };
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            if (!String.IsNullOrEmpty(secondaryText)) footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            if (!String.IsNullOrEmpty(secondaryText))
            {
                Button secondary = DialogButton(secondaryText, false);
                secondary.Click += delegate { DialogResult = false; Close(); };
                footer.Children.Add(secondary);
            }
            Button primary = DialogButton(primaryText, true);
            primary.Click += delegate { DialogResult = true; Close(); };
            if (!String.IsNullOrEmpty(secondaryText)) Grid.SetColumn(primary, 1);
            footer.Children.Add(primary);
            Grid.SetRow(footer, 2);
            layout.Children.Add(footer);
        }
    }

    internal sealed class ModernInputWindow : ModernDialogWindow
    {
        public string Value { get; private set; }

        public ModernInputWindow(string title, string caption, string initialValue) : base(title, 460, 250)
        {
            ResizeMode = ResizeMode.NoResize;
            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
            ContentCard.Child = layout;
            StackPanel panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            panel.Children.Add(Heading(title, 18));
            panel.Children.Add(new TextBlock { Text = caption, Foreground = ModernPalette.Brush(ModernPalette.Cyan), Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap, FontSize = 12 });
            TextBox box = new TextBox { Text = initialValue ?? String.Empty, Margin = new Thickness(0, 12, 0, 0), Height = 36, Padding = new Thickness(10, 4, 10, 4), VerticalContentAlignment = VerticalAlignment.Center, FontSize = 14 };
            panel.Children.Add(box);
            Grid.SetRow(panel, 0); layout.Children.Add(panel);
            Grid footer = new Grid { HorizontalAlignment = HorizontalAlignment.Right };
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            Button cancel = DialogButton("取消", false); cancel.Click += delegate { DialogResult = false; Close(); }; footer.Children.Add(cancel);
            Button save = DialogButton("SAVE PRESET", true); Grid.SetColumn(save, 1); save.Click += delegate { Value = box.Text; DialogResult = true; Close(); }; footer.Children.Add(save);
            Grid.SetRow(footer, 1); layout.Children.Add(footer);
        }
    }

    // Ask3lad-style search picker: modal search box + instant-filter list.
    // Used for every long list choice (sensors, ammunition, maps, targets...).



    // Embedded in the main-window TARGETS tab; the standalone Map & Scenario
    // window keeps its own copy of this layout (keep both in sync when editing).




    // Embedded in the main-window EXPERIMENTAL tab; the standalone Ground
    // Configure window keeps its own copy (keep both in sync when editing).





    // Embedded in the main-window OPTIONS tab; the standalone Mission Options
    // window keeps its own copy of this layout (keep both in sync when editing).

    // Embedded in the main-window EXPERIMENTAL tab; the standalone Flight
    // Configure window keeps its own copy (keep both in sync when editing).






}