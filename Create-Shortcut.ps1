<#
 Create-Shortcut.ps1 - put Universal Test Lab on the desktop / Start menu.

 Usage:
   .\Create-Shortcut.ps1                  # desktop shortcut
   .\Create-Shortcut.ps1 -StartMenu       # desktop + Start menu
   .\Create-Shortcut.ps1 -Name "UTL"      # custom name
   .\Create-Shortcut.ps1 -Uninstall       # remove them again

 The shortcut points at dist\UniversalTestLab.exe. UTL resolves its data trees
 from the executable's own folder (AppDomain.BaseDirectory), so the working
 directory does not matter - rebuilding the exe keeps the shortcut valid.
#>
param(
    [string]$Name = "Universal Test Lab",
    [string]$Target,
    [switch]$StartMenu,
    [switch]$Uninstall
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $Target) { $Target = Join-Path $root 'dist\UniversalTestLab.exe' }
if (-not (Test-Path -LiteralPath $Target)) { throw "Executable not found: $Target (run compile.bat first)" }
$Target = [IO.Path]::GetFullPath($Target)
$workDir = Split-Path -Parent $Target
$icon = Join-Path $root 'resources\utl.ico'
if (-not (Test-Path -LiteralPath $icon)) { $icon = $Target }

function New-Link([string]$path) {
    $dir = Split-Path -Parent $path
    if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    $shell = New-Object -ComObject WScript.Shell
    $lnk = $shell.CreateShortcut($path)
    $lnk.TargetPath = $Target
    $lnk.WorkingDirectory = $workDir
    $lnk.IconLocation = "$icon,0"
    $lnk.Description = 'Universal Test Lab - War Thunder vehicle test missions'
    $lnk.Save()
    Write-Output "Created $path"
}

$targets = @( (Join-Path ([Environment]::GetFolderPath('Desktop')) ($Name + '.lnk')) )
if ($StartMenu) { $targets += (Join-Path ([Environment]::GetFolderPath('Programs')) ($Name + '.lnk')) }
foreach ($p in $targets) {
    if ($Uninstall) {
        if (Test-Path -LiteralPath $p) { Remove-Item -LiteralPath $p -Force; Write-Output "Removed $p" }
    } else { New-Link $p }
}
