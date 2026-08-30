<#
 Update-Data.ps1 - Refresh the UTL data catalog after a War Thunder update.

 The data/ folder is a build-time snapshot of the game files. After the game
 updates, run this script once to:
   1. Unpack the game archives (flightmodels / units / weapons / lang / char)
   2. Rebuild the data catalog (Build-Catalog.ps1)
   3. Recompile UniversalTestLab.exe with the new embedded data

 Usage:
   .\Update-Data.ps1                          # full refresh (default game path)
   .\Update-Data.ps1 -GameRoot "D:\War Thunder"
   .\Update-Data.ps1 -SkipExtract             # reuse existing universal_*_data folders
   .\Update-Data.ps1 -SkipCatalog             # unpack + compile only
   .\Update-Data.ps1 -SkipCompile             # data only, no exe rebuild
#>
param(
    [string]$GameRoot = 'C:\Program Files (x86)\Steam\steamapps\common\War Thunder',
    [switch]$SkipExtract,
    [switch]$SkipCatalog,
    [switch]$SkipCompile
)
$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptRoot

if (-not (Test-Path -LiteralPath $GameRoot)) { throw "Game root not found: $GameRoot" }

# Locate wt_ext_cli (released by UTL at runtime, or bundled in resources)
$extractor = Join-Path $env:LOCALAPPDATA 'UniversalTestLab\tools\wt_ext_cli.exe'
if (-not (Test-Path -LiteralPath $extractor)) {
    $bundled = Join-Path $scriptRoot 'resources\wt_ext_cli.exe'
    if (Test-Path -LiteralPath $bundled) { $extractor = $bundled }
}
if (-not (Test-Path -LiteralPath $extractor)) { throw 'wt_ext_cli.exe not found. Run UTL once so it can release the tool, or restore resources\wt_ext_cli.exe.' }

# Build-Catalog.ps1 contains UTF-8 characters; ensure it has a BOM so
# Windows PowerShell 5.1 decodes it as UTF-8 instead of ANSI.
$bcPath = Join-Path $scriptRoot 'Build-Catalog.ps1'
if (Test-Path -LiteralPath $bcPath) {
    $bcBytes = [IO.File]::ReadAllBytes($bcPath)
    if (-not ($bcBytes.Length -ge 3 -and $bcBytes[0] -eq 0xEF -and $bcBytes[1] -eq 0xBB -and $bcBytes[2] -eq 0xBF)) {
        $withBom = New-Object byte[] ($bcBytes.Length + 3)
        $withBom[0] = 0xEF; $withBom[1] = 0xBB; $withBom[2] = 0xBF
        [Array]::Copy($bcBytes, 0, $withBom, 3, $bcBytes.Length)
        [IO.File]::WriteAllBytes($bcPath, $withBom)
        Write-Host 'Added UTF-8 BOM to Build-Catalog.ps1'
    }
}

function Unpack-From([string]$vromfs, [string]$folder, [string]$outDir) {
    $target = Join-Path $scriptRoot $outDir
    if (-not (Test-Path -LiteralPath $target)) { New-Item -ItemType Directory -Force -Path $target | Out-Null }
    Write-Host "Unpacking $vromfs -> $folder ..."
    & $extractor unpack_vromf --input_dir_or_file (Join-Path $GameRoot $vromfs) --output_dir $target --format BlkText --folder $folder --continue Quiet
    if ($LASTEXITCODE -ne 0) { throw "Extraction failed for $vromfs ($folder)" }
}

if (-not $SkipExtract) {
    Unpack-From 'aces.vromfs.bin' 'gamedata/flightmodels' 'universal_game_data'
    Unpack-From 'aces.vromfs.bin' 'gamedata/units' 'universal_units_data'
    Unpack-From 'aces.vromfs.bin' 'gamedata/weapons' 'universal_weapons_data'
    Unpack-From 'lang.vromfs.bin' 'lang' 'universal_lang_data'
    Unpack-From 'char.vromfs.bin' 'config' 'universal_char_data'
}

if (-not $SkipCatalog) {
    Write-Host 'Rebuilding data catalog ...'
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $scriptRoot 'Build-Catalog.ps1') `
        -FlightModelsRoot 'universal_game_data\aces.vromfs.bin_u\gamedata\flightmodels' `
        -UnitsRoot 'universal_units_data\aces.vromfs.bin_u\gamedata\units' `
        -LangRoot 'universal_lang_data\lang.vromfs.bin_u\lang' `
        -WeaponsRoot 'universal_weapons_data\aces.vromfs.bin_u\gamedata\weapons' `
        -ShopPath 'universal_char_data\char.vromfs.bin_u\config\shop.blk'
    if ($LASTEXITCODE -ne 0) { throw 'Build-Catalog.ps1 failed.' }
}

if (-not $SkipCompile) {
    Write-Host 'Compiling UniversalTestLab.exe ...'
    $csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (-not (Test-Path -LiteralPath $csc)) { $csc = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe' }
    & $csc '/lib:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF' '@build.rsp'
    if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
}

Write-Host ''
Write-Host 'Done. Data catalog and exe are up to date.'
Write-Host 'Close any running UniversalTestLab before starting the new dist\UniversalTestLab.exe.'
