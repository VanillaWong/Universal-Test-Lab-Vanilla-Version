// ModernShell.Xaml.cs
// The main window XAML markup string (styles, layout, chrome).
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
    internal static class ModernXaml
    {
        public static object Parse(string xaml) { return XamlReader.Parse(xaml); }

        public const string Main = @"
<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
      xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
      x:Name=""Root"" Background=""#B329354D"">
  <Grid.Resources>
    <SolidColorBrush x:Key=""TextBrush"" Color=""#F3F6FF""/>
    <SolidColorBrush x:Key=""MutedBrush"" Color=""#9EACCE""/>
    <SolidColorBrush x:Key=""AccentBrush"" Color=""#6C63FF""/>
    <SolidColorBrush x:Key=""AccentDarkBrush"" Color=""#4A55CC""/>
    <SolidColorBrush x:Key=""CyanBrush"" Color=""#4BD5FF""/>
    <SolidColorBrush x:Key=""Good"" Color=""#48DEB3""/>
    <SolidColorBrush x:Key=""Danger"" Color=""#FF5B8B""/>
    <SolidColorBrush x:Key=""FieldBrush"" Color=""#B81B2740""/>
    <SolidColorBrush x:Key=""SurfaceBrush"" Color=""#80505B74""/>
    <SolidColorBrush x:Key=""BorderBrush"" Color=""#58759F""/>

    <Style TargetType=""TextBlock"">
      <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/>
      <Setter Property=""FontFamily"" Value=""Segoe UI""/>
    </Style>
    <Style x:Key=""Caption"" TargetType=""TextBlock"">
      <Setter Property=""Foreground"" Value=""{StaticResource MutedBrush}""/>
      <Setter Property=""FontSize"" Value=""11""/>
      <Setter Property=""FontWeight"" Value=""SemiBold""/>
    </Style>
    <Style x:Key=""GlassCard"" TargetType=""Border"">
      <Setter Property=""Background"" Value=""{StaticResource SurfaceBrush}""/>
      <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}""/>
      <Setter Property=""BorderThickness"" Value=""1""/>
      <Setter Property=""CornerRadius"" Value=""20""/>
      <Setter Property=""Padding"" Value=""16""/>
    </Style>
    <Style x:Key=""ButtonStyle"" TargetType=""Button"">
      <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/>
      <Setter Property=""Background"" Value=""#24365F""/>
      <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}""/>
      <Setter Property=""BorderThickness"" Value=""1""/>
      <Setter Property=""Padding"" Value=""14,8""/>
      <Setter Property=""FontWeight"" Value=""SemiBold""/>
      <Setter Property=""Cursor"" Value=""Hand""/>
      <Setter Property=""Template"">
        <Setter.Value>
          <ControlTemplate TargetType=""Button"">
            <Border x:Name=""bd"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"" CornerRadius=""10"">
              <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center"" Margin=""{TemplateBinding Padding}""/>
            </Border>
            <ControlTemplate.Triggers>
              <Trigger Property=""IsMouseOver"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#304A78""/></Trigger>
              <Trigger Property=""IsPressed"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#17294B""/></Trigger>
              <Trigger Property=""IsEnabled"" Value=""False""><Setter Property=""Opacity"" Value=""0.42""/></Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>
    <Style x:Key=""PrimaryButton"" TargetType=""Button"" BasedOn=""{StaticResource ButtonStyle}"">
      <Setter Property=""Background"" Value=""{StaticResource AccentDarkBrush}""/>
      <Setter Property=""BorderThickness"" Value=""0""/>
      <Setter Property=""FontSize"" Value=""13""/>
    </Style>
    <Style x:Key=""ChromeButton"" TargetType=""Button"" BasedOn=""{StaticResource ButtonStyle}"">
      <Setter Property=""Width"" Value=""46""/><Setter Property=""Height"" Value=""36""/>
      <Setter Property=""Padding"" Value=""0""/><Setter Property=""Background"" Value=""Transparent""/><Setter Property=""BorderThickness"" Value=""0""/>
      <Setter Property=""FontSize"" Value=""14""/>
    </Style>
    <Style TargetType=""TextBox"">
      <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/>
      <Setter Property=""Background"" Value=""{StaticResource FieldBrush}""/>
      <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}""/>
      <Setter Property=""BorderThickness"" Value=""1""/>
      <Setter Property=""Padding"" Value=""10,7""/>
      <Setter Property=""CaretBrush"" Value=""{StaticResource CyanBrush}""/>
      <Setter Property=""Template"">
        <Setter.Value>
          <ControlTemplate TargetType=""TextBox"">
            <Border x:Name=""bd"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"" CornerRadius=""8"">
              <ScrollViewer x:Name=""PART_ContentHost"" Margin=""{TemplateBinding Padding}""/>
            </Border>
            <ControlTemplate.Triggers><Trigger Property=""IsKeyboardFocused"" Value=""True""><Setter TargetName=""bd"" Property=""BorderBrush"" Value=""{StaticResource CyanBrush}""/></Trigger></ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>
    <Style x:Key=""ComboItemStyle"" TargetType=""ComboBoxItem"">
      <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/><Setter Property=""Background"" Value=""{StaticResource FieldBrush}""/>
      <Setter Property=""Padding"" Value=""10,8""/><Setter Property=""HorizontalContentAlignment"" Value=""Stretch""/>
      <Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""ComboBoxItem""><Border x:Name=""bd"" Background=""{TemplateBinding Background}"" Padding=""{TemplateBinding Padding}"" CornerRadius=""6""><ContentPresenter/></Border><ControlTemplate.Triggers><Trigger Property=""IsHighlighted"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#4A55CC""/></Trigger><Trigger Property=""IsSelected"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#2D4673""/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
    </Style>
    <Style TargetType=""ComboBox"">
      <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/><Setter Property=""Background"" Value=""{StaticResource FieldBrush}""/>
      <Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}""/><Setter Property=""BorderThickness"" Value=""1""/>
      <Setter Property=""Padding"" Value=""10,7""/><Setter Property=""ItemContainerStyle"" Value=""{StaticResource ComboItemStyle}""/>
      <Setter Property=""MaxDropDownHeight"" Value=""360""/>
      <Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""ComboBox""><Grid><ToggleButton x:Name=""toggle"" Focusable=""False"" IsChecked=""{Binding IsDropDownOpen, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}"" Background=""Transparent"" Foreground=""{TemplateBinding Foreground}"" BorderThickness=""0"" HorizontalContentAlignment=""Stretch"" VerticalContentAlignment=""Stretch""><ToggleButton.Template><ControlTemplate TargetType=""ToggleButton""><ContentPresenter HorizontalAlignment=""Stretch"" VerticalAlignment=""Stretch""/></ControlTemplate></ToggleButton.Template><Border x:Name=""bd"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"" CornerRadius=""8""><Grid><ContentPresenter Margin=""10,7,34,7"" VerticalAlignment=""Center"" HorizontalAlignment=""Left"" Content=""{TemplateBinding SelectionBoxItem}"" ContentTemplate=""{TemplateBinding SelectionBoxItemTemplate}"" TextElement.Foreground=""{TemplateBinding Foreground}""/><Path Data=""M 0 0 L 5 5 L 10 0 Z"" Fill=""#9EACCE"" HorizontalAlignment=""Right"" VerticalAlignment=""Center"" Margin=""0,0,10,0""/></Grid></Border></ToggleButton><Popup x:Name=""PART_Popup"" IsOpen=""{TemplateBinding IsDropDownOpen}"" Placement=""Bottom"" AllowsTransparency=""True"" Focusable=""False"" PopupAnimation=""Fade""><Border Background=""#0B1632"" BorderBrush=""#4D6D9F"" BorderThickness=""1"" CornerRadius=""10"" Padding=""5"" MinWidth=""{Binding ActualWidth, ElementName=toggle}"" MaxHeight=""{TemplateBinding MaxDropDownHeight}""><ScrollViewer VerticalScrollBarVisibility=""Auto"" CanContentScroll=""True""><ItemsPresenter/></ScrollViewer></Border></Popup></Grid><ControlTemplate.Triggers><Trigger Property=""IsKeyboardFocusWithin"" Value=""True""><Setter TargetName=""bd"" Property=""BorderBrush"" Value=""{StaticResource CyanBrush}""/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
    </Style>
    <Style TargetType=""ListBox"">
      <Setter Property=""Background"" Value=""{StaticResource FieldBrush}""/><Setter Property=""BorderThickness"" Value=""0""/>
      <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/><Setter Property=""ScrollViewer.HorizontalScrollBarVisibility"" Value=""Disabled""/>
    </Style>
    <Style TargetType=""ListBoxItem"">
      <Setter Property=""Padding"" Value=""10,7""/><Setter Property=""HorizontalContentAlignment"" Value=""Stretch""/><Setter Property=""Background"" Value=""Transparent""/>
      <Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""ListBoxItem""><Border x:Name=""bd"" Background=""{TemplateBinding Background}"" CornerRadius=""8"" Padding=""{TemplateBinding Padding}"" Margin=""3,2""><ContentPresenter/></Border><ControlTemplate.Triggers><Trigger Property=""IsMouseOver"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#24365F""/></Trigger><Trigger Property=""IsSelected"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#4A55CC""/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
    </Style>
    <Style TargetType=""ListViewItem"">
      <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/><Setter Property=""Background"" Value=""Transparent""/><Setter Property=""Padding"" Value=""6,7""/><Setter Property=""HorizontalContentAlignment"" Value=""Stretch""/>
      <Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""ListViewItem""><Border x:Name=""bd"" Background=""{TemplateBinding Background}"" CornerRadius=""6"" Padding=""{TemplateBinding Padding}""><GridViewRowPresenter Content=""{TemplateBinding Content}"" Columns=""{Binding View.Columns, RelativeSource={RelativeSource AncestorType=ListView}}""/></Border><ControlTemplate.Triggers><Trigger Property=""IsMouseOver"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#24365F""/></Trigger><Trigger Property=""IsSelected"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#4A55CC""/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
    </Style>
    <Style TargetType=""GridViewColumnHeader""><Setter Property=""Background"" Value=""#1D315C""/><Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/><Setter Property=""BorderBrush"" Value=""#A8C7ECFF""/><Setter Property=""Padding"" Value=""8,7""/><Setter Property=""FontWeight"" Value=""SemiBold""/><Setter Property=""HorizontalContentAlignment"" Value=""Center""/><Setter Property=""Focusable"" Value=""False""/><Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""GridViewColumnHeader""><Border x:Name=""HeaderBorder"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""0,0,1,1"" Padding=""{TemplateBinding Padding}""><ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/></Border></ControlTemplate></Setter.Value></Setter></Style>
    <Style x:Key=""LastGridHeader"" TargetType=""GridViewColumnHeader"" BasedOn=""{StaticResource {x:Type GridViewColumnHeader}}""><Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""GridViewColumnHeader""><Border Background=""{TemplateBinding Background}"" BorderBrush=""#A8C7ECFF"" BorderThickness=""0,0,1,1"" CornerRadius=""0,11,11,0"" Padding=""{TemplateBinding Padding}""><ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/></Border></ControlTemplate></Setter.Value></Setter></Style>
    <Style TargetType=""ScrollBar"">
      <Setter Property=""Background"" Value=""Transparent""/><Setter Property=""Width"" Value=""8""/><Setter Property=""Height"" Value=""8""/>
      <Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""ScrollBar""><Grid Background=""Transparent""><Track x:Name=""PART_Track"" Orientation=""{TemplateBinding Orientation}"" Minimum=""{TemplateBinding Minimum}"" Maximum=""{TemplateBinding Maximum}"" Value=""{TemplateBinding Value}"" ViewportSize=""{TemplateBinding ViewportSize}"" IsDirectionReversed=""False""><Track.DecreaseRepeatButton><RepeatButton x:Name=""dec"" Command=""{x:Static ScrollBar.PageUpCommand}"" Opacity=""0""/></Track.DecreaseRepeatButton><Track.Thumb><Thumb><Thumb.Template><ControlTemplate TargetType=""Thumb""><Border Background=""#4D6D9F"" CornerRadius=""4"" Margin=""1""/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb><Track.IncreaseRepeatButton><RepeatButton x:Name=""inc"" Command=""{x:Static ScrollBar.PageDownCommand}"" Opacity=""0""/></Track.IncreaseRepeatButton></Track></Grid><ControlTemplate.Triggers><Trigger Property=""Orientation"" Value=""Horizontal""><Setter Property=""Width"" Value=""Auto""/><Setter Property=""Height"" Value=""8""/><Setter TargetName=""PART_Track"" Property=""IsDirectionReversed"" Value=""False""/><Setter TargetName=""dec"" Property=""Command"" Value=""{x:Static ScrollBar.PageLeftCommand}""/><Setter TargetName=""inc"" Property=""Command"" Value=""{x:Static ScrollBar.PageRightCommand}""/></Trigger><Trigger Property=""Orientation"" Value=""Vertical""><Setter Property=""Width"" Value=""8""/><Setter Property=""Height"" Value=""Auto""/><Setter TargetName=""PART_Track"" Property=""IsDirectionReversed"" Value=""True""/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
    </Style>
    <Style x:Key=""ToggleStyle"" TargetType=""ToggleButton"">
      <Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/><Setter Property=""Background"" Value=""#24365F""/><Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}""/><Setter Property=""BorderThickness"" Value=""1""/><Setter Property=""Padding"" Value=""12,8""/><Setter Property=""FontWeight"" Value=""SemiBold""/><Setter Property=""Cursor"" Value=""Hand""/>
      <Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""ToggleButton""><Border x:Name=""bd"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"" CornerRadius=""10""><ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center"" Margin=""{TemplateBinding Padding}""/></Border><ControlTemplate.Triggers><Trigger Property=""IsMouseOver"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""#304A78""/></Trigger><Trigger Property=""IsChecked"" Value=""True""><Setter TargetName=""bd"" Property=""Background"" Value=""{StaticResource AccentDarkBrush}""/><Setter TargetName=""bd"" Property=""BorderBrush"" Value=""{StaticResource CyanBrush}""/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
    </Style>
    <Style x:Key=""StatusToggleStyle"" TargetType=""ToggleButton""><Setter Property=""Foreground"" Value=""{StaticResource TextBrush}""/><Setter Property=""Background"" Value=""#24365F""/><Setter Property=""BorderBrush"" Value=""{StaticResource BorderBrush}""/><Setter Property=""BorderThickness"" Value=""1""/><Setter Property=""Padding"" Value=""12,8""/><Setter Property=""FontWeight"" Value=""SemiBold""/><Setter Property=""Cursor"" Value=""Hand""/><Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""ToggleButton""><Border x:Name=""bd"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"" CornerRadius=""10""><ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center"" Margin=""{TemplateBinding Padding}""/></Border><ControlTemplate.Triggers><Trigger Property=""IsMouseOver"" Value=""True""><Setter TargetName=""bd"" Property=""Opacity"" Value=""0.86""/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
  </Grid.Resources>

  <Grid.RowDefinitions><RowDefinition Height=""38""/><RowDefinition Height=""*""/></Grid.RowDefinitions>
  <Border x:Name=""TitleBar"" Grid.Row=""0"" Background=""#FF35415E"" BorderBrush=""#664BD5FF"" BorderThickness=""0,0,0,1"">
    <Grid><Grid.ColumnDefinitions><ColumnDefinition Width=""*""/><ColumnDefinition Width=""Auto""/></Grid.ColumnDefinitions>
      <StackPanel Orientation=""Horizontal"" VerticalAlignment=""Center"" Margin=""14,0""><Border Width=""20"" Height=""20"" CornerRadius=""6"" Background=""#4A55CC"" Margin=""0,0,9,0""><TextBlock Text=""U"" FontWeight=""Bold"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" FontSize=""11""/></Border><TextBlock Text=""Universal Test Lab"" FontWeight=""SemiBold"" VerticalAlignment=""Center""/><TextBlock Text=""  /  Mission Studio"" Foreground=""#9EACCE"" VerticalAlignment=""Center""/></StackPanel>
      <StackPanel Grid.Column=""1"" Orientation=""Horizontal""><Button x:Name=""MinimizeButton"" Style=""{StaticResource ChromeButton}"" Content=""—""/><Button x:Name=""MaximizeButton"" Style=""{StaticResource ChromeButton}"" Content=""□""/><Button x:Name=""CloseButton"" Style=""{StaticResource ChromeButton}"" Content=""×""/></StackPanel>
    </Grid>
  </Border>

  <Grid Grid.Row=""1"" Margin=""0""><Grid.RowDefinitions><RowDefinition Height=""64""/><RowDefinition Height=""Auto""/><RowDefinition Height=""*""/><RowDefinition Height=""28""/></Grid.RowDefinitions>
    <Border Style=""{StaticResource GlassCard}"" Padding=""22,4"" Margin=""0"" CornerRadius=""0"" BorderThickness=""0,0,0,1""><Grid><Grid.ColumnDefinitions><ColumnDefinition Width=""270""/><ColumnDefinition Width=""*""/><ColumnDefinition Width=""Auto""/></Grid.ColumnDefinitions>
      <StackPanel VerticalAlignment=""Center""><TextBlock Text=""Universal Test Lab"" FontSize=""20"" FontWeight=""SemiBold""/><TextBlock Text=""AIR &amp; GROUND VEHICLE TEST WORKSPACE"" Foreground=""{StaticResource CyanBrush}"" FontSize=""10"" FontWeight=""SemiBold""/></StackPanel>
      <StackPanel Grid.Column=""1"" Margin=""10,0,12,0"" VerticalAlignment=""Center""><TextBlock Text=""GAME DIRECTORY"" Style=""{StaticResource Caption}"" Margin=""2,0,0,3""/><TextBox x:Name=""GameFolderBox"" Height=""30"" Padding=""10,3"" Margin=""0"" VerticalContentAlignment=""Center""/></StackPanel>
      <StackPanel Grid.Column=""2"" Orientation=""Horizontal"" VerticalAlignment=""Center""><Button x:Name=""BrowseButton"" Style=""{StaticResource ButtonStyle}"" Content=""BROWSE"" Margin=""4,0""/><Button x:Name=""SyncButton"" Style=""{StaticResource ButtonStyle}"" Content=""SYNC BASE"" Margin=""4,0""/><Button x:Name=""MissionsButton"" Style=""{StaticResource ButtonStyle}"" Content=""MISSIONS"" Margin=""4,0""/><Button x:Name=""PresetsButton"" Style=""{StaticResource ButtonStyle}"" Content=""PRESETS"" Margin=""4,0""/><Button x:Name=""AboutButton"" Style=""{StaticResource ButtonStyle}"" Content=""SUPPORT"" Margin=""4,0,0,0""/></StackPanel>
    </Grid></Border>

    <Border Grid.Row=""1"" Margin=""0,8,0,0"" Background=""Transparent""><StackPanel Orientation=""Horizontal""><ToggleButton x:Name=""TabVehicleButton"" Style=""{StaticResource ToggleStyle}"" Content=""VEHICLE"" IsChecked=""True"" Margin=""0,0,6,0""/><ToggleButton x:Name=""TabTargetsButton"" Style=""{StaticResource ToggleStyle}"" Content=""TARGETS"" Margin=""0,0,6,0""/><ToggleButton x:Name=""TabOptionsButton"" Style=""{StaticResource ToggleStyle}"" Content=""OPTIONS"" Margin=""0,0,6,0""/><ToggleButton x:Name=""TabGarageButton"" Style=""{StaticResource ToggleStyle}"" Content=""GARAGE"" Margin=""0,0,6,0""/><ToggleButton x:Name=""TabExperimentalButton"" Style=""{StaticResource ToggleStyle}"" Content=""EXPERIMENTAL""/></StackPanel></Border>

    <Grid x:Name=""TabVehicleContent"" Grid.Row=""2"" Margin=""12,10,12,10""><Grid.ColumnDefinitions><ColumnDefinition Width=""330""/><ColumnDefinition Width=""12""/><ColumnDefinition Width=""*"" MinWidth=""500""/><ColumnDefinition Width=""12""/><ColumnDefinition Width=""330""/></Grid.ColumnDefinitions>
      <Border Grid.Column=""0"" Style=""{StaticResource GlassCard}""><Grid><Grid.RowDefinitions><RowDefinition Height=""58""/><RowDefinition Height=""Auto""/><RowDefinition Height=""Auto""/><RowDefinition Height=""Auto""/><RowDefinition Height=""*""/></Grid.RowDefinitions>
        <Grid><Grid.ColumnDefinitions><ColumnDefinition Width=""48""/><ColumnDefinition Width=""*""/></Grid.ColumnDefinitions><Border Width=""44"" Height=""44"" CornerRadius=""13"" Background=""{StaticResource AccentDarkBrush}""><TextBlock Text=""01"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" FontWeight=""Bold""/></Border><StackPanel Grid.Column=""1"" Margin=""10,2,0,0""><TextBlock Text=""CHOOSE VEHICLE"" FontSize=""16"" FontWeight=""SemiBold""/><TextBlock Text=""Air and ground vehicles"" Foreground=""{StaticResource MutedBrush}"" FontSize=""11""/></StackPanel></Grid>
        <StackPanel Grid.Row=""1"" Margin=""0,8,0,10""><TextBlock Text=""SEARCH"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><TextBox x:Name=""AircraftSearch""/></StackPanel>
        <Grid Grid.Row=""2"" Margin=""0,0,0,10""><Grid.ColumnDefinitions><ColumnDefinition Width=""1.25*""/><ColumnDefinition Width=""1.2*""/><ColumnDefinition Width=""1*""/></Grid.ColumnDefinitions><StackPanel Margin=""0,0,5,0""><TextBlock Text=""NATION"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><ComboBox x:Name=""NationFilter""/></StackPanel><StackPanel Grid.Column=""1"" Margin=""5,0""><TextBlock Text=""RANK"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><ComboBox x:Name=""RankFilter""/></StackPanel><StackPanel Grid.Column=""2"" Margin=""5,0,0,0""><TextBlock Text=""TYPE"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><ComboBox x:Name=""TypeFilter""/></StackPanel></Grid>
        <StackPanel Grid.Row=""3"" Margin=""2,0,0,8""><TextBlock Text=""AVAILABLE VEHICLES"" Style=""{StaticResource Caption}""/><TextBlock x:Name=""VehicleCountText"" Foreground=""{StaticResource CyanBrush}"" FontSize=""11"" Margin=""0,4,0,0""/></StackPanel>
        <Border Grid.Row=""4"" Background=""{StaticResource FieldBrush}"" CornerRadius=""12"" Padding=""3""><ListBox x:Name=""AircraftList""><ListBox.ItemTemplate><DataTemplate><StackPanel><TextBlock Text=""{Binding Name}"" FontWeight=""SemiBold"" TextTrimming=""CharacterEllipsis""/><TextBlock Text=""{Binding Meta}"" Foreground=""#AEB9D8"" FontSize=""10"" Margin=""0,2,0,0"" TextTrimming=""CharacterEllipsis""/></StackPanel></DataTemplate></ListBox.ItemTemplate></ListBox></Border>
      </Grid></Border>

      <Border Grid.Column=""2"" Style=""{StaticResource GlassCard}""><Grid><Grid.RowDefinitions><RowDefinition Height=""58""/><RowDefinition Height=""32""/><RowDefinition Height=""Auto""/><RowDefinition Height=""Auto""/><RowDefinition Height=""*""/><RowDefinition Height=""Auto""/></Grid.RowDefinitions>
        <Grid><Grid.ColumnDefinitions><ColumnDefinition Width=""48""/><ColumnDefinition Width=""*""/><ColumnDefinition Width=""Auto""/></Grid.ColumnDefinitions><Border Width=""44"" Height=""44"" CornerRadius=""13"" Background=""{StaticResource AccentDarkBrush}""><TextBlock Text=""02"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" FontWeight=""Bold""/></Border><StackPanel Grid.Column=""1"" Margin=""10,2,0,0""><TextBlock x:Name=""BuildTitle"" Text=""BUILD LOADOUT"" FontSize=""16"" FontWeight=""SemiBold""/><TextBlock x:Name=""BuildSubtitle"" Text=""Select a station, then mount a weapon"" Foreground=""{StaticResource MutedBrush}"" FontSize=""11""/></StackPanel><TextBlock x:Name=""MassText"" Grid.Column=""2"" Foreground=""{StaticResource CyanBrush}"" FontWeight=""SemiBold"" VerticalAlignment=""Center""/></Grid>
        <TextBlock x:Name=""StationText"" Grid.Row=""1"" Foreground=""{StaticResource MutedBrush}"" VerticalAlignment=""Center"" TextTrimming=""CharacterEllipsis""/>
        <Border x:Name=""PylonCard"" Grid.Row=""2"" Background=""{StaticResource FieldBrush}"" CornerRadius=""12"" Padding=""5"" Margin=""0,2,0,8""><UniformGrid x:Name=""PylonPanel"" Rows=""1"" VerticalAlignment=""Center""/></Border>
        <Grid x:Name=""WeaponFilterPanel"" Grid.Row=""3""><Grid.ColumnDefinitions><ColumnDefinition Width=""175""/><ColumnDefinition Width=""*""/><ColumnDefinition Width=""155""/><ColumnDefinition Width=""125""/><ColumnDefinition Width=""145""/></Grid.ColumnDefinitions><StackPanel Margin=""0,0,5,0""><TextBlock Text=""WEAPON SOURCE"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><ToggleButton x:Name=""InjectionToggle"" Style=""{StaticResource ToggleStyle}"" Content=""INJECT ANY WEAPON""/></StackPanel><StackPanel Grid.Column=""1"" Margin=""5,0""><TextBlock Text=""SEARCH"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><TextBox x:Name=""WeaponSearch""/></StackPanel><StackPanel Grid.Column=""2"" Margin=""5,0""><TextBlock Text=""WEAPON TYPE"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><ComboBox x:Name=""CategoryFilter""/></StackPanel><StackPanel Grid.Column=""3"" Margin=""5,0""><TextBlock Text=""NATION"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><ComboBox x:Name=""WeaponNationFilter""/></StackPanel><StackPanel Grid.Column=""4"" Margin=""5,0,0,0""><TextBlock Text=""SORT"" Style=""{StaticResource Caption}"" Margin=""2,0,0,5""/><ComboBox x:Name=""SortFilter""/></StackPanel></Grid>
        <Grid x:Name=""WeaponTableFrame"" Grid.Row=""4"" Margin=""0,10,0,10""><Border Background=""{StaticResource FieldBrush}"" CornerRadius=""12""/><Grid x:Name=""WeaponTableClipContent""><ListView x:Name=""WeaponList"" Background=""Transparent"" BorderThickness=""0"" Foreground=""{StaticResource TextBrush}"" ScrollViewer.HorizontalScrollBarVisibility=""Disabled"" ScrollViewer.CanContentScroll=""True"" VirtualizingStackPanel.IsVirtualizing=""True"" VirtualizingStackPanel.VirtualizationMode=""Recycling""><ListView.Resources><Style TargetType=""ScrollBar"" BasedOn=""{StaticResource {x:Type ScrollBar}}""><Style.Triggers><Trigger Property=""Orientation"" Value=""Vertical""><Setter Property=""Margin"" Value=""0,32,0,1""/></Trigger></Style.Triggers></Style></ListView.Resources><ListView.GroupStyle><GroupStyle><GroupStyle.HeaderTemplate><DataTemplate><Border Background=""#D9152340"" BorderBrush=""#49698F"" BorderThickness=""0,1,0,1"" Padding=""10,6"" Margin=""0,4,0,2""><TextBlock Foreground=""{StaticResource CyanBrush}"" FontWeight=""SemiBold""><Run Text=""—  ""/><Run Text=""{Binding Name, Mode=OneWay}""/><Run Text=""  —""/></TextBlock></Border></DataTemplate></GroupStyle.HeaderTemplate></GroupStyle></ListView.GroupStyle><ListView.View><GridView><GridViewColumn Header=""Weapon"" Width=""330"" DisplayMemberBinding=""{Binding Name}""/><GridViewColumn Header=""Type"" Width=""185"" DisplayMemberBinding=""{Binding Category}""/><GridViewColumn Header=""Ammo"" Width=""70"" DisplayMemberBinding=""{Binding Ammo}""/><GridViewColumn Header=""Mass"" Width=""85"" DisplayMemberBinding=""{Binding Mass}""/><GridViewColumn Width=""82""><GridViewColumn.Header><GridViewColumnHeader Content=""Mode""/></GridViewColumn.Header><GridViewColumn.CellTemplate><DataTemplate><TextBlock Text=""{Binding Mode}"" HorizontalAlignment=""Center"" TextAlignment=""Center""/></DataTemplate></GridViewColumn.CellTemplate></GridViewColumn></GridView></ListView.View></ListView></Grid><Border BorderBrush=""#A8C7ECFF"" BorderThickness=""1"" CornerRadius=""12"" IsHitTestVisible=""False""/></Grid>
        <Grid Grid.Row=""5""><Grid.ColumnDefinitions><ColumnDefinition Width=""*""/><ColumnDefinition Width=""145""/><ColumnDefinition Width=""128""/><ColumnDefinition Width=""94""/><ColumnDefinition Width=""145""/></Grid.ColumnDefinitions><TextBlock Text=""Tip: double-click a weapon to mount it"" Foreground=""{StaticResource MutedBrush}"" VerticalAlignment=""Center""/><Button x:Name=""SystemsButton"" Grid.Column=""1"" Style=""{StaticResource ButtonStyle}"" Content=""模块"" Margin=""4,0""/><Button x:Name=""ClearStationButton"" Grid.Column=""2"" Style=""{StaticResource ButtonStyle}"" Content=""CLEAR STATION"" Margin=""4,0""/><Button x:Name=""ClearAllButton"" Grid.Column=""3"" Style=""{StaticResource ButtonStyle}"" Content=""全部清空"" Margin=""4,0""/><Button x:Name=""MountButton"" Grid.Column=""4"" Style=""{StaticResource PrimaryButton}"" Content=""MOUNT WEAPON"" Margin=""4,0,0,0""/></Grid>
      </Grid></Border>

      <Border Grid.Column=""4"" Style=""{StaticResource GlassCard}""><Grid><Grid.RowDefinitions><RowDefinition Height=""58""/><RowDefinition Height=""150""/><RowDefinition Height=""34""/><RowDefinition Height=""48""/><RowDefinition Height=""48""/><RowDefinition Height=""*""/><RowDefinition Height=""56""/><RowDefinition Height=""26""/></Grid.RowDefinitions>
        <Grid><Grid.ColumnDefinitions><ColumnDefinition Width=""48""/><ColumnDefinition Width=""*""/></Grid.ColumnDefinitions><Border Width=""44"" Height=""44"" CornerRadius=""13"" Background=""{StaticResource AccentDarkBrush}""><TextBlock Text=""03"" HorizontalAlignment=""Center"" VerticalAlignment=""Center"" FontWeight=""Bold""/></Border><StackPanel Grid.Column=""1"" Margin=""10,2,0,0""><TextBlock Text=""CONFIGURE TEST"" FontSize=""16"" FontWeight=""SemiBold""/><TextBlock Text=""Flight, targets and launch profile"" Foreground=""{StaticResource MutedBrush}"" FontSize=""11""/></StackPanel></Grid>
        <Border x:Name=""PreviewCard"" Grid.Row=""1"" CornerRadius=""15"" BorderBrush=""#78A7DFFF"" BorderThickness=""1"" Background=""#7A1D315C""><Grid x:Name=""PreviewClipContent""><Ellipse Width=""155"" Height=""105"" Fill=""#284BD5FF"" VerticalAlignment=""Top"" Margin=""0,12,0,0""/><Grid x:Name=""PreviewAircraftVisual""><Image x:Name=""PreviewAircraftImage"" Width=""220"" Height=""112"" Stretch=""Uniform"" Opacity=""0.92"" VerticalAlignment=""Top"" Margin=""0,4,0,0""/></Grid><Grid x:Name=""PreviewHelicopterVisual"" Visibility=""Collapsed""><Image x:Name=""PreviewHelicopterImage"" Width=""270"" Height=""108"" Stretch=""Uniform"" Opacity=""0.94"" VerticalAlignment=""Top"" Margin=""0,5,0,0""/></Grid><Grid x:Name=""PreviewDroneVisual"" Visibility=""Collapsed""><Image x:Name=""PreviewDroneImage"" Width=""270"" Height=""108"" Stretch=""Uniform"" Opacity=""0.94"" VerticalAlignment=""Top"" Margin=""0,5,0,0""/></Grid><Border VerticalAlignment=""Bottom"" Background=""#900A142E"" Padding=""12,10""><StackPanel><TextBlock x:Name=""PreviewName"" FontSize=""15"" FontWeight=""SemiBold"" TextTrimming=""CharacterEllipsis""/><TextBlock x:Name=""PreviewMeta"" Foreground=""{StaticResource MutedBrush}"" FontSize=""10"" Margin=""0,3,0,0"" TextTrimming=""CharacterEllipsis""/></StackPanel></Border></Grid></Border>
        <TextBlock Grid.Row=""2"" Text=""MISSION SETUP"" FontSize=""14"" FontWeight=""SemiBold"" VerticalAlignment=""Bottom""/>
        <Button x:Name=""FlightConfigureButton"" Grid.Row=""3"" Style=""{StaticResource ButtonStyle}"" Content=""飞行配置"" Margin=""0,7,0,0""/>
        <Button x:Name=""MapButton"" Grid.Row=""4"" Style=""{StaticResource ButtonStyle}"" Content=""MAP &amp; SCENARIO"" Margin=""0,7,0,0""/>
        <StackPanel Grid.Row=""5"" Margin=""2,16,2,8""><TextBlock Text=""FLIGHT PROFILE"" Style=""{StaticResource Caption}""/><TextBlock x:Name=""FlightProfileText"" Foreground=""{StaticResource MutedBrush}"" FontSize=""11"" TextWrapping=""Wrap"" Margin=""0,3,0,0""/><TextBlock Text=""MAP PROFILE"" Style=""{StaticResource Caption}"" Margin=""0,14,0,0""/><TextBlock x:Name=""TargetSummaryText"" Foreground=""{StaticResource MutedBrush}"" FontSize=""11"" TextWrapping=""Wrap"" Margin=""0,3,0,0""/><TextBlock Text=""Aircraft/helicopters: reopen User Missions. Ground vehicle changes: restart War Thunder once."" Foreground=""{StaticResource Good}"" FontSize=""11"" TextWrapping=""Wrap"" Margin=""0,14,0,0""/></StackPanel>
        <Grid Visibility=""Collapsed""><ComboBox x:Name=""AirTargetBox""/><ComboBox x:Name=""AirCountBox""/><ComboBox x:Name=""GroundTargetBox""/><ComboBox x:Name=""GroundCountBox""/><ToggleButton x:Name=""HostileToggle""/><ToggleButton x:Name=""SamSitesToggle""/><TextBlock x:Name=""SamSitesMode""/><TextBlock x:Name=""SamSitesSelection""/><ComboBox x:Name=""ShipTargetBox""/><ComboBox x:Name=""ShipCountBox""/></Grid>
        <Grid Grid.Row=""6"" Margin=""0,7,0,0""><Grid.ColumnDefinitions><ColumnDefinition Width=""132""/><ColumnDefinition Width=""*""/></Grid.ColumnDefinitions><Button x:Name=""MissionOptionsButton"" Style=""{StaticResource ButtonStyle}"" Content=""MISSION OPTIONS""/><Button x:Name=""GenerateButton"" Grid.Column=""1"" Margin=""6,0,0,0"" Style=""{StaticResource PrimaryButton}"" Content=""GENERATE TEST MISSION""/></Grid>
        <TextBlock Grid.Row=""7"" Text=""AIR HOT LOAD  •  GROUND PROXY RELOAD"" Foreground=""{StaticResource CyanBrush}"" FontSize=""10"" HorizontalAlignment=""Center"" VerticalAlignment=""Bottom""/>
      </Grid></Border>
    </Grid>
    <Grid x:Name=""TabTargetsContent"" Grid.Row=""2"" Visibility=""Collapsed"" IsHitTestVisible=""False""><StackPanel VerticalAlignment=""Center"" HorizontalAlignment=""Center""><TextBlock Text=""TARGETS — GROUND / AIR / NAVAL TARGETS"" FontSize=""18"" FontWeight=""SemiBold""/><TextBlock Text=""Stage1: migrating Map &amp; Scenario here"" Foreground=""{StaticResource MutedBrush}"" Margin=""0,8,0,0""/></StackPanel></Grid>
    <Grid x:Name=""TabOptionsContent"" Grid.Row=""2"" Visibility=""Collapsed"" IsHitTestVisible=""False""><StackPanel VerticalAlignment=""Center"" HorizontalAlignment=""Center""><TextBlock Text=""选项 — 任务设置"" FontSize=""18"" FontWeight=""SemiBold""/><TextBlock Text=""Stage1: migrating Mission Options here"" Foreground=""{StaticResource MutedBrush}"" Margin=""0,8,0,0""/></StackPanel></Grid>
    <Grid x:Name=""TabGarageContent"" Grid.Row=""2"" Visibility=""Collapsed"" IsHitTestVisible=""False""><StackPanel VerticalAlignment=""Center"" HorizontalAlignment=""Center""><TextBlock Text=""GARAGE — COLLECTION &amp; PRESETS"" FontSize=""18"" FontWeight=""SemiBold""/><TextBlock Text=""Stage2: recently used / favourites / presets"" Foreground=""{StaticResource MutedBrush}"" Margin=""0,8,0,0""/></StackPanel></Grid>
    <Grid x:Name=""TabExperimentalContent"" Grid.Row=""2"" Visibility=""Collapsed"" IsHitTestVisible=""False""><StackPanel VerticalAlignment=""Center"" HorizontalAlignment=""Center""><TextBlock Text=""EXPERIMENTAL — OVERRIDES &amp; INJECTION"" FontSize=""18"" FontWeight=""SemiBold""/><TextBlock Text=""Stage1: migrating Ground/Flight Configure here"" Foreground=""{StaticResource MutedBrush}"" Margin=""0,8,0,0""/></StackPanel></Grid>
    <Border Grid.Row=""3"" Background=""#D01A263D"" CornerRadius=""0"" Margin=""0"" Padding=""14,0"" BorderBrush=""#664BD5FF"" BorderThickness=""0,1,0,0""><TextBlock x:Name=""StatusText"" Text=""●  READY"" Foreground=""{StaticResource Good}"" VerticalAlignment=""Center""/></Border>
  </Grid>
</Grid>";
    }
}
