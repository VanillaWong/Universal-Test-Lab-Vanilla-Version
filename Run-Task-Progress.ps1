# Run-Task-Progress.ps1 - run UTL data tasks with a WPF progress bar window.
# Usage:
#   powershell -NoProfile -ExecutionPolicy Bypass -File .\Run-Task-Progress.ps1 -Task catalog
#   powershell -NoProfile -ExecutionPolicy Bypass -File .\Run-Task-Progress.ps1 -Task update
#   powershell -NoProfile -ExecutionPolicy Bypass -File .\Run-Task-Progress.ps1 -Task catalog -Extra '-SkipSomething'
param(
  [ValidateSet('catalog','update')][string]$Task = 'catalog',
  [string]$Extra = ''
)
$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase

$logPath = Join-Path $env:TEMP ("utl_task_" + [DateTime]::Now.ToString('HHmmss') + ".log")
$errPath = $logPath + ".err"

$taskArgs = @()
if ($Task -eq 'catalog') {
  $taskArgs = @('-NoProfile','-ExecutionPolicy','Bypass','-File', (Join-Path $scriptRoot 'Build-Catalog.ps1'),
    '-FlightModelsRoot','universal_game_data\aces.vromfs.bin_u\gamedata\flightmodels',
    '-UnitsRoot','universal_units_data\aces.vromfs.bin_u\gamedata\units',
    '-LangRoot','universal_lang_data\lang.vromfs.bin_u\lang',
    '-WeaponsRoot','universal_weapons_data\aces.vromfs.bin_u\gamedata\weapons',
    '-ShopPath','universal_char_data\char.vromfs.bin_u\config\shop.blk')
  if ($Extra) { $taskArgs += $Extra.Split(' ', [StringSplitOptions]::RemoveEmptyEntries) }
} else {
  $taskArgs = @('-NoProfile','-ExecutionPolicy','Bypass','-File', (Join-Path $scriptRoot 'Update-Data.ps1'))
  if ($Extra) { $taskArgs += $Extra.Split(' ', [StringSplitOptions]::RemoveEmptyEntries) }
}

$proc = Start-Process powershell -ArgumentList $taskArgs -WorkingDirectory $scriptRoot -RedirectStandardOutput $logPath -RedirectStandardError $errPath -WindowStyle Hidden -PassThru

[xml]$xaml = @"
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
 Title="UTL Task Progress" Height="320" Width="620" WindowStartupLocation="CenterScreen" Background="#1B1B24" ResizeMode="NoResize" FontFamily="Segoe UI" ShowInTaskbar="True">
 <Grid Margin="18">
  <Grid.RowDefinitions>
   <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="*"/><RowDefinition Height="Auto"/>
  </Grid.RowDefinitions>
  <TextBlock x:Name="Title" FontSize="17" FontWeight="SemiBold" Foreground="#4FD1FF" TextTrimming="CharacterEllipsis"/>
  <TextBlock x:Name="Status" Grid.Row="1" Margin="0,12,0,0" FontSize="13" Foreground="#C8C8D0" TextWrapping="Wrap"/>
  <ProgressBar x:Name="Bar" Grid.Row="2" Margin="0,12,0,0" Height="16" Minimum="0" Maximum="100" Value="0"/>
  <TextBox x:Name="Log" Grid.Row="3" Margin="0,12,0,0" IsReadOnly="True" Background="#12121A" Foreground="#9AA0AB" BorderThickness="0" FontFamily="Consolas" FontSize="12" VerticalScrollBarVisibility="Auto" TextWrapping="NoWrap"/>
  <Button x:Name="CloseBtn" Grid.Row="4" Content="CLOSE" Width="110" Height="32" Margin="0,14,0,0" HorizontalAlignment="Right" IsEnabled="False"/>
 </Grid>
</Window>
"@
$win = [Windows.Markup.XamlReader]::Load((New-Object System.Xml.XmlNodeReader $xaml))
$title = $win.FindName('Title'); $status = $win.FindName('Status'); $bar = $win.FindName('Bar'); $log = $win.FindName('Log'); $close = $win.FindName('CloseBtn')

$taskLabel = if ($Task -eq 'catalog') { 'Rebuilding vehicle / weapon catalogs' } else { 'Full data rebuild (unpack + catalog + compile)' }
$title.Text = "Universal Test Lab - $taskLabel"
$status.Text = 'Starting...'
$bar.IsIndeterminate = ($Task -eq 'update')
$close.Add_Click({ $win.Close() })
$win.Add_Closed({ $timer.Stop() })

$script:lastPos = 0
$script:finished = $false
$timer = New-Object System.Windows.Threading.DispatcherTimer
$timer.Interval = [TimeSpan]::FromMilliseconds(250)
$timer.Add_Tick({
  try {
    if (Test-Path $logPath) {
      $fs = [IO.File]::Open($logPath, 'Open', 'Read', 'ReadWrite')
      try {
        $fs.Seek($script:lastPos, 'Begin') | Out-Null
        $sr = New-Object IO.StreamReader($fs)
        $new = $sr.ReadToEnd()
        $script:lastPos = $fs.Position
        if ($new) {
          $log.AppendText($new)
          $log.ScrollToEnd()
          if (-not $bar.IsIndeterminate) {
            foreach ($line in ($new -split "`n")) {
              $t = $line.Trim()
              if ($t -match '\[catalog\] .*Phase (\d+)/(\d+): ([^ ]+)') {
                $phase = [int]$Matches[1]; $total = [int]$Matches[2]
                $bar.Value = [Math]::Min(95, (($phase - 1) / $total) * 100)
                $status.Text = "Phase $phase/$total : $($Matches[3])"
              } elseif ($t -match '(\d+) / (\d+)$') {
                $c = [double]$Matches[1]; $n = [double]$Matches[2]
                $base = [Math]::Floor($bar.Value / 25) * 25
                if ($n -gt 0) { $bar.Value = [Math]::Min(95, $base + ($c / $n) * 25) }
                $status.Text = "Processing... $c / $n"
              } elseif ($t -match 'writing output files') {
                $bar.Value = 96; $status.Text = 'Writing output files...'
              } elseif ($t -match '^\[catalog\] .*done|Aircraft=\d+') {
                $bar.Value = 100; $status.Text = 'Catalog rebuild complete.'
              }
            }
          }
        }
      } finally { $sr.Dispose(); $fs.Dispose() }
    }
    if (-not $script:finished -and $proc.HasExited) {
      $script:finished = $true
      $timer.Stop()
      try {
        if (Test-Path $errPath) {
          $errText = [IO.File]::ReadAllText($errPath)
          if ($errText) { $log.AppendText("`n--- ERROR OUTPUT ---`n" + $errText) }
        }
      } catch { }
      if ($proc.ExitCode -eq 0) {
        $bar.Value = 100
        $status.Text = 'DONE - task finished successfully.'
        $status.Foreground = [Windows.Media.BrushConverter]::new().ConvertFromString('#35C77A')
      } else {
        $status.Text = 'FAILED - check the log above. Exit code: ' + $proc.ExitCode
        $status.Foreground = [Windows.Media.BrushConverter]::new().ConvertFromString('#FF5E57')
      }
      $close.IsEnabled = $true
    }
  } catch { }
})
$timer.Start()
$null = $win.ShowDialog()
