<#
 Quick-GroundAmmo.ps1 - Fast rebuild of data/ground_ammo.tsv only.
 Scans every weapon blk in groundmodels_weapons (including SAM/ATGM missile
 launchers such as 170mm_57e6_rocket_launcher.blk) and regenerates the
 ground ammunition catalog. Skips the other 4 unpack phases of
 Update-Data.ps1 and the rest of Build-Catalog.ps1.

 Usage:
   .\Quick-GroundAmmo.ps1 -WeaponsRoot 'universal_weapons_data\aces.vromfs.bin_u\gamedata\weapons'
#>
param([string]$WeaponsRoot = 'universal_weapons_data\aces.vromfs.bin_u\gamedata\weapons')
$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$WeaponsRoot = [IO.Path]::GetFullPath((Join-Path $scriptRoot $WeaponsRoot))
$cannonDir = Join-Path $WeaponsRoot 'groundmodels_weapons'
if (-not (Test-Path -LiteralPath $cannonDir)) { throw "groundmodels_weapons not found: $cannonDir" }

function Clean-Field([string]$value) {
  if ($null -eq $value) { return '' }
  return ($value -replace "[\t\r\n]", ' ' -replace '\s+', ' ').Trim()
}
function Format-ProjectileName([string]$value) {
  $name = Clean-Field $value
  $name = [regex]::Replace($name, '(?i)\b(\d+(?:\.\d+)?)\s*mm\b', '$1 mm')
  $name = [regex]::Replace($name, '(?i)\b([A-Z]*\d+)A(\d+)\b', '$1A$2')
  $name = [regex]::Replace($name, '(?i)\bXM(\d+)\b', 'XM$1')
  $name = [regex]::Replace($name, '(?i)\bAPDS[ _-]*FS\b', 'APFSDS')
  $name = [regex]::Replace($name, '(?i)\bHEAT[ _-]*FS\b', 'HEAT-FS')
  $name = [regex]::Replace($name, '(?i)\bHE[ _-]*OR\b', 'HE-OR')
  return $name
}
function Friendly-ProjectileType([string]$value) {
  $kind = (Clean-Field $value).ToLowerInvariant()
  if ($kind -match 'apds[_-]?fs|apfsds') { return 'APFSDS' }
  if ($kind -match 'apds') { return 'APDS' }
  if ($kind -match 'apcbc') { return 'APCBC' }
  if ($kind -match 'aphe') { return 'APHE' }
  if ($kind -match 'heat.*(vt|mp)') { return 'HEAT-MP-T' }
  if ($kind -match 'heat') { return 'HEAT-FS' }
  if ($kind -match 'atgm|guided') { return 'ATGM' }
  if ($kind -match 'sam|missile|rocket') { return 'SAM' }
  if ($kind -match 'hesh') { return 'HESH' }
  if ($kind -match 'shrapnel') { return 'Shrapnel' }
  if ($kind -match 'smoke') { return 'Smoke' }
  if ($kind -match 'dist|proximity|radio') { return 'HE-FRAG (proximity fuse)' }
  if ($kind -match 'he_frag') { return 'HE-FRAG' }
  if ($kind -match 'he_or') { return 'HE-OR' }
  if ($kind -match 'he') { return 'HE' }
  if ($kind -match 'sap') { return 'SAP' }
  if ($kind -match 'ap') { return 'AP' }
  return [Globalization.CultureInfo]::InvariantCulture.TextInfo.ToTitleCase(($kind -replace '[_-]+', ' '))
}
function Get-NamedBlocks([string]$text, [string]$name) {
  $results = New-Object System.Collections.Generic.List[object]
  $matches = [regex]::Matches($text, "(?m)^\s*" + [regex]::Escape($name) + "\s*\{")
  foreach ($match in $matches) {
    $open = $text.IndexOf('{', $match.Index)
    if ($open -lt 0) { continue }
    $depth = 0; $quoted = $false; $escaped = $false
    for ($i = $open; $i -lt $text.Length; $i++) {
      $c = $text[$i]
      if ($quoted) {
        if ($escaped) { $escaped = $false; continue }
        if ($c -eq '\') { $escaped = $true; continue }
        if ($c -eq '"') { $quoted = $false }
        continue
      }
      if ($c -eq '"') { $quoted = $true; continue }
      if ($c -eq '{') { $depth++ }
      elseif ($c -eq '}') {
        $depth--
        if ($depth -eq 0) {
          $results.Add([pscustomobject]@{ Start = $match.Index; Open = $open; End = $i; Text = $text.Substring($match.Index, $i - $match.Index + 1) })
          break
        }
      }
    }
  }
  return $results
}

function Get-BulletContainer([string]$text, [int]$bulletStart) {
  $lineEnd = $text.IndexOf("`n", $bulletStart)
  if ($lineEnd -lt 0) { $lineEnd = $text.Length }
  $lineText = $text.Substring($bulletStart, $lineEnd - $bulletStart)
  if ($lineText -notmatch '^[ \t]*bullet\s*\{') { return '' }
  $indentMatch = [regex]::Match($lineText, '^[ \t]*')
  if ($indentMatch.Length -le 0) { return '' }
  $m = [regex]::Matches($text.Substring(0, $bulletStart), '(?m)^([A-Za-z0-9_./+]+)\s*\{')
  if ($m.Count -eq 0) { return '' }
  return $m[$m.Count - 1].Groups[1].Value
}
function Escape-Json([string]$s) {
  if ($null -eq $s) { return '' }
  return $s.Replace('"', '\"')
}

# Known display names from the previous catalog (SourceBlk|BulletName -> Display)
$knownDisplay = @{}
$oldPath = Join-Path $scriptRoot 'data\ground_ammo.tsv'
if (Test-Path -LiteralPath $oldPath) {
  $lines = [IO.File]::ReadAllLines($oldPath, [Text.Encoding]::UTF8)
  foreach ($line in $lines) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $p = $line.Split("`t")
    if ($p.Length -ge 3) { $knownDisplay["$($p[0])|$($p[1])"] = $p[2] }
  }
}

$rows = New-Object System.Collections.Generic.List[string]
$seen = @{}
Write-Host "Scanning $cannonDir ..."
foreach ($file in (Get-ChildItem -LiteralPath $cannonDir -File -Filter '*.blk' | Sort-Object Name)) {
  $source = 'gameData/Weapons/groundModels_weapons/' + $file.Name
  $text = [IO.File]::ReadAllText($file.FullName)
  foreach ($bullet in (Get-NamedBlocks $text 'bullet')) {
    $nameMatch = [regex]::Match($bullet.Text, '(?m)^\s*bulletName:t\s*=\s*"([^"]+)"')
    if (-not $nameMatch.Success) { continue }
    $bulletName = $nameMatch.Groups[1].Value
    $key = "$source|$bulletName"
    if ($seen.ContainsKey($key)) { continue }
    $seen[$key] = $true
    if ($knownDisplay.ContainsKey($key)) { $display = $knownDisplay[$key] }
    else { $display = Format-ProjectileName ([Globalization.CultureInfo]::InvariantCulture.TextInfo.ToTitleCase(($bulletName -replace '_', ' '))) }
    $massMatch = [regex]::Match($bullet.Text, '(?m)^\s*mass:r\s*=\s*([0-9.eE+-]+)')
    $speedMatch = [regex]::Match($bullet.Text, '(?m)^\s*speed:r\s*=\s*([0-9.eE+-]+)')
    $explosiveMatch = [regex]::Match($bullet.Text, '(?m)^\s*explosiveMass:r\s*=\s*([0-9.eE+-]+)')
    $caliberMatch = [regex]::Match($bullet.Text, '(?m)^\s*caliber:r\s*=\s*([0-9.eE+-]+)')
    $typeMatch = [regex]::Match($bullet.Text, '(?m)^\s*bulletType:t\s*=\s*"([^"]+)"')
    $penetrationMatch = [regex]::Match($bullet.Text, '(?mi)^\s*(?:armorPower:r|ArmorPower0m:p2)\s*=\s*([0-9.eE+-]+)')
    $mass = if ($massMatch.Success) { $massMatch.Groups[1].Value } else { '0' }
    $speed = if ($speedMatch.Success) { $speedMatch.Groups[1].Value } else { '0' }
    $explosive = if ($explosiveMatch.Success) { $explosiveMatch.Groups[1].Value } else { '0' }
    $caliber = if ($caliberMatch.Success) { $caliberMatch.Groups[1].Value } else { '0' }
    $kind = if ($typeMatch.Success) { Friendly-ProjectileType $typeMatch.Groups[1].Value } else { 'Projectile' }
    $penetration = if ($penetrationMatch.Success) { $penetrationMatch.Groups[1].Value } else { '0' }
    $container = Get-BulletContainer $text $bullet.Start
    $rows.Add("$source`t$container`t$bulletName`t$display`t$kind`t$mass`t$speed`t$explosive`t$caliber`t$penetration")
  }
}
$sorted = $rows | Sort-Object { ($_ -split "`t")[4] }, { ($_ -split "`t")[3] }, { ($_ -split "`t")[2] }
$sb = New-Object System.Text.StringBuilder
[void]$sb.Append('[')
for ($i = 0; $i -lt $sorted.Count; $i++) {
  if ($i -gt 0) { [void]$sb.Append(',') }
  $p = $sorted[$i] -split "`t"
  [void]$sb.Append('{"source":"').Append((Escape-Json $p[0]))
  [void]$sb.Append('","container":"').Append((Escape-Json $p[1]))
  [void]$sb.Append('","bulletName":"').Append((Escape-Json $p[2]))
  [void]$sb.Append('","display":"').Append((Escape-Json $p[3]))
  [void]$sb.Append('","kind":"').Append((Escape-Json $p[4]))
  [void]$sb.Append('","mass":').Append($p[5])
  [void]$sb.Append(',"speed":').Append($p[6])
  [void]$sb.Append(',"explosive":').Append($p[7])
  [void]$sb.Append(',"caliber":').Append($p[8])
  [void]$sb.Append(',"penetration":').Append($p[9]).Append('}')
}
[void]$sb.Append(']')
$outPath = Join-Path $scriptRoot 'data\ground_ammo.json'
[IO.File]::WriteAllText($outPath, $sb.ToString(), [Text.UTF8Encoding]::new($false))
Write-Host "ground_ammo.json updated: $($sorted.Count) rows -> $outPath"
