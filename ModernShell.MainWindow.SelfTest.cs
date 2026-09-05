// ModernShell.MainWindow.SelfTest.cs
// Screenshot and WPF self-test helpers (segment 5/5).
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
    internal sealed partial class ModernMainWindow
    {
        internal void RefreshFromController()
        {
            Aircraft current = controller.WorkspaceSelectedAircraft;
            if (current == null) return;
            aircraftSearch.Text = "";
            nationFilter.SelectedIndex = rankFilter.SelectedIndex = typeFilter.SelectedIndex = 0;
            FilterAircraft();
            aircraftList.SelectedItem = aircraftList.Items.Cast<AircraftView>().FirstOrDefault(x => x.Source.Id == current.Id);
            selectedAircraft = current;
            RefreshPylons();
            UpdateConfigurationSummary();
        }

        internal void SelectFirstFixedWingForSelfTest()
        {
            Aircraft pick = controller.WorkspaceAircraft.FirstOrDefault(x =>
                String.Equals(x.Kind, "Aircraft", StringComparison.OrdinalIgnoreCase) &&
                controller.WorkspacePylons(x.Id).Count > 0);
            if (pick == null) return;
            controller.WorkspaceSelectAircraft(pick.Id);
            RefreshFromController();
        }

        internal void ExerciseDropdownForSelfTest()
        {
            rankFilter.IsDropDownOpen = true;
            UpdateLayout();
            rankFilter.IsDropDownOpen = false;
        }

        internal void SelectVehicleKindForScreenshot(string kind)
        {
            AircraftView target = aircraftList.Items.Cast<AircraftView>().FirstOrDefault(x => String.Equals(x.Kind, kind, StringComparison.OrdinalIgnoreCase));
            if (target != null)
            {
                aircraftList.SelectedItem = target;
                aircraftList.ScrollIntoView(target);
            }
        }

        internal void EnableInjectionForScreenshot()
        {
            injectionToggle.IsChecked = true;
            UpdateLayout();
            UpdateWeaponColumns();
        }

        internal void ShowGroundPresetForScreenshot()
        {
            SelectVehicleKindForScreenshot("Ground Vehicle");
            ModernPresetWindow dialog = new ModernPresetWindow(controller, this) { Owner = this };
            ShowOverlay(dialog);
            dialog.SelectFirstCustomSightForScreenshot();
        }

        internal void ShowMessageForScreenshot(bool danger)
        {
            ModernMessageDialog dialog = new ModernMessageDialog(
                danger ? "Game Resource" : "Base Mission Installed",
                danger ? "Extracted game resource was not found." : "Base mission installed. Close the User Missions tab in War Thunder and open it again; no game restart is required.",
                "关闭", null, danger) { Owner = this };
            ShowOverlay(dialog);
        }

        internal bool ExerciseOverlayForSelfTest()
        {
            int windowCountBefore = System.Windows.Application.Current.Windows.Count;
            ModernMissionGeneratedWindow dialog = new ModernMissionGeneratedWindow { Owner = this };
            ShowOverlay(dialog);
            UpdateLayout();
            bool firstShown = overlayLayer.Visibility == Visibility.Visible &&
                overlayDialogs.Count == 1 &&
                overlayLayer.Children.Contains(dialog) &&
                dialog.OverlayChromeReadyForSelfTest() &&
                root.Effect is BlurEffect &&
                !root.IsHitTestVisible &&
                System.Windows.Application.Current.Windows.Count == windowCountBefore;

            ModernMessageDialog nested = new ModernMessageDialog("Overlay Test", "Nested confirmations stay inside the same application window.", "OK", "取消", false) { Owner = this };
            ShowOverlay(nested);
            UpdateLayout();
            bool nestedShown = overlayDialogs.Count == 2 &&
                overlayLayer.Children.Contains(nested) &&
                nested.OverlayChromeReadyForSelfTest() &&
                dialog.Effect is BlurEffect &&
                !dialog.IsHitTestVisible &&
                System.Windows.Application.Current.Windows.Count == windowCountBefore;
            nested.Close();
            UpdateLayout();
            bool nestedClosed = overlayDialogs.Count == 1 &&
                dialog.Effect == null && dialog.IsHitTestVisible && dialog.Opacity == 1;
            dialog.Close();
            UpdateLayout();
            bool closed = overlayLayer.Visibility == Visibility.Collapsed &&
                overlayDialogs.Count == 0 &&
                root.Effect == null &&
                root.IsHitTestVisible;
            return firstShown && nestedShown && nestedClosed && closed;
        }

        internal bool LayoutFixesReadyForSelfTest()
        {
            RectangleGeometry clip = previewClipContent.Clip as RectangleGeometry;
            Rect work = SystemParameters.WorkArea;
            bool insideWorkArea = Left >= work.Left - 1 && Top >= work.Top - 1 &&
                Left + ActualWidth <= work.Right + 1 && Top + ActualHeight <= work.Bottom + 1;
            ScrollBar testBar = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Style = (Style)root.Resources[typeof(ScrollBar)]
            };
            testBar.ApplyTemplate();
            Track track = testBar.Template.FindName("PART_Track", testBar) as Track;
            RectangleGeometry weaponClip = weaponTableClipContent.Clip as RectangleGeometry;
            injectionToggle.IsChecked = false;
            UpdateLayout();
            UpdateWeaponColumns();
            GridView normalView = weaponList.View as GridView;
            GridViewColumnHeader normalMode = normalView == null ? null : normalView.Columns[4].Header as GridViewColumnHeader;
            bool unroundedModeWithoutScroll = FindVisibleVerticalScrollBar(weaponList) == null &&
                normalMode != null && !Object.ReferenceEquals(normalMode.Style, root.Resources["LastGridHeader"]);
            if (normalMode != null) normalMode.ApplyTemplate();
            bool staticHeaderTemplate = normalMode != null && normalMode.Template != null &&
                normalMode.Template.FindName("HeaderBorder", normalMode) is Border &&
                normalMode.Template.Triggers.Count == 0;
            injectionToggle.IsChecked = true;
            UpdateLayout();
            UpdateWeaponColumns();
            GridView weaponView = weaponList.View as GridView;
            GridViewColumnHeader scrollingMode = weaponView == null ? null : weaponView.Columns[4].Header as GridViewColumnHeader;
            double columnWidth = weaponView == null ? 0 : weaponView.Columns.Sum(x => x.Width);
            ScrollBar weaponScroll = FindVisibleVerticalScrollBar(weaponList);
            double weaponGutter = weaponScroll == null ? 0 : Math.Max(8, weaponScroll.ActualWidth);
            double expectedColumnWidth = Math.Max(360, weaponList.ActualWidth - weaponGutter - 2);
            List<PylonSlot> stationSlots = pylonPanel.Children.OfType<Button>().Select(x => x.Tag as PylonSlot).Where(x => x != null).ToList();
            bool stationOrder = stationSlots.Count > 0 && stationSlots.Select(DisplayStation).SequenceEqual(stationSlots.Select(DisplayStation).OrderBy(x => x));
            bool stationFit = pylonPanel.Children.OfType<Button>().All(x => x.ActualWidth <= 100 && x.ActualHeight <= 64);
            SolidColorBrush rootBrush = root.Background as SolidColorBrush;
            Border titleBar = Find<Border>("TitleBar");
            SolidColorBrush titleBrush = titleBar.Background as SolidColorBrush;
            bool gameFolderVisible = gameFolder.ActualHeight >= 29 && gameFolder.Padding.Top <= 3 &&
                gameFolder.VerticalContentAlignment == VerticalAlignment.Center && !String.IsNullOrWhiteSpace(gameFolder.Text);
            UpdatePreviewKind("Aircraft");
            bool aircraftPreview = previewAircraftVisual.Visibility == Visibility.Visible && previewHelicopterVisual.Visibility == Visibility.Collapsed && previewDroneVisual.Visibility == Visibility.Collapsed && previewAircraftImage.Source != null;
            UpdatePreviewKind("Helicopter");
            bool helicopterPreview = previewAircraftVisual.Visibility == Visibility.Collapsed && previewHelicopterVisual.Visibility == Visibility.Visible && previewDroneVisual.Visibility == Visibility.Collapsed && previewHelicopterImage.Source != null;
            UpdatePreviewKind("Drone");
            bool dronePreview = previewAircraftVisual.Visibility == Visibility.Collapsed && previewHelicopterVisual.Visibility == Visibility.Collapsed && previewDroneVisual.Visibility == Visibility.Visible && Object.ReferenceEquals(previewHelicopterImage.Source, previewDroneImage.Source);
            UpdatePreviewKind(selectedAircraft == null ? "Aircraft" : selectedAircraft.Kind);
            return clip != null && clip.RadiusX == 14 && clip.RadiusY == 14 && previewCard.Clip == null &&
                previewClipContent.ActualWidth > 0 && previewClipContent.ActualHeight > 0 && insideWorkArea &&
                weaponClip != null && weaponClip.RadiusX == 12 && weaponTableFrame.Clip == null &&
                weaponScroll != null && unroundedModeWithoutScroll && staticHeaderTemplate && scrollingMode != null &&
                Object.ReferenceEquals(scrollingMode.Style, root.Resources["LastGridHeader"]) &&
                weaponList.ItemsSource != null &&
                Math.Abs(columnWidth - expectedColumnWidth) < 2 && stationOrder && stationFit &&
                rootBrush != null && rootBrush.Color.A < 255 && titleBrush != null && titleBrush.Color.A == 255 && gameFolderVisible &&
                aircraftPreview && helicopterPreview && dronePreview &&
                track != null && track.IsDirectionReversed &&
                !ModernXaml.Main.Contains("ChromeFill") &&
                ModernXaml.Main.Contains("x:Name=\"TitleBar\" Grid.Row=\"0\"") &&
                ModernXaml.Main.Contains("x:Name=\"TabVehicleContent\" Grid.Row=\"2\" Margin=\"12,10,12,10\"");
        }

        internal bool CombinedCatalogReadyForSelfTest()
        {
            return controller.WorkspaceCombinedMaps.Count >= 40 && controller.WorkspaceCombinedMaps.All(map =>
                !String.IsNullOrWhiteSpace(map.Level) && map.Spawns.Count == 12 && new[] { 1, 2 }.All(side =>
                    new[] { "ground_1", "ground_2", "airfield", "air", "heli_near", "heli_far" }.All(option =>
                        map.Spawns.Count(spawn => spawn.Side == side && spawn.Option.Equals(option, StringComparison.OrdinalIgnoreCase)) == 1)));
        }

        private void SetStatus(string message, bool error)
        {
            status.Text = error ? "●  ERROR — " + message : "●  " + message;
            status.Foreground = ModernPalette.Brush(error ? ModernPalette.Danger : ModernPalette.Good);
        }
    }
}
