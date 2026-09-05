param(
  [string]$FlightModelsRoot = "..\universal_game_data\aces.vromfs.bin_u\gamedata\flightmodels",
  [string]$UnitsRoot = "..\universal_units_data\aces.vromfs.bin_u\gamedata\units",
  [string]$LangRoot = "..\universal_lang_data\lang.vromfs.bin_u\lang",
  [string]$WeaponsRoot = "..\universal_weapons_data\aces.vromfs.bin_u\gamedata\weapons",
  [string]$ShopPath = "..\universal_char_data\char.vromfs.bin_u\config\shop.blk",
  [string]$OutputRoot = ".\data",
  [string]$PhaseOnly = ""
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$FlightModelsRoot = [IO.Path]::GetFullPath((Join-Path $scriptRoot $FlightModelsRoot))
$UnitsRoot = [IO.Path]::GetFullPath((Join-Path $scriptRoot $UnitsRoot))
$LangRoot = [IO.Path]::GetFullPath((Join-Path $scriptRoot $LangRoot))
$WeaponsRoot = [IO.Path]::GetFullPath((Join-Path $scriptRoot $WeaponsRoot))
$ShopPath = [IO.Path]::GetFullPath((Join-Path $scriptRoot $ShopPath))
$OutputRoot = [IO.Path]::GetFullPath((Join-Path $scriptRoot $OutputRoot))
New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null

function Clean-Field([string]$value) {
  if ($null -eq $value) { return "" }
  return ($value -replace "[\t\r\n]", " " -replace "\s+", " ").Trim()
}

function Clean-DisplayName([string]$value) {
  return ((Clean-Field $value) -replace '^[^A-Za-z0-9]+', '').Trim()
}

function Get-VehicleDisplayName([string]$id) {
  if ($id -eq 'us_m1a2_sep3_abrams') { return 'M1A2 SEP V3' }
  foreach ($suffix in @('_shop', '_1', '_0')) {
    $key = $id + $suffix
    if ($unitNames.ContainsKey($key) -and -not [string]::IsNullOrWhiteSpace($unitNames[$key])) {
      return Clean-DisplayName $unitNames[$key]
    }
  }
  return Clean-DisplayName ($id -replace '_', ' ')
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

function Get-GroundModificationTier([string]$id, [string]$text) {
  $known = @{
    'new_tank_tracks' = 1; 'tank_tool_kit' = 1; 'new_tank_horizontal_aiming' = 1
    'new_tank_suspension' = 2; 'new_tank_brakes' = 2; 'manual_extinguisher' = 2; 'tank_new_gun' = 2
    'new_tank_filter' = 3; 'tank_medical_kit' = 3; 'new_tank_vertical_aiming' = 3; 'tank_engine_smoke_screen_system' = 3
    'new_tank_transmission' = 4; 'new_tank_engine' = 4; 'art_support' = 4; 'night_vision_system' = 4
  }
  if ($known.ContainsKey($id)) { return [int]$known[$id] }
  $tierMatch = [regex]::Match($text, '(?m)^\s*tier:i\s*=\s*(-?\d+)')
  if ($tierMatch.Success) { return [Math]::Max(1, [int]$tierMatch.Groups[1].Value + 1) }
  if ($id -match '(?i)ammo_pack$') { return 1 }
  if ($id -match '(?i)laser_rangefinder|rangefinder|lws') { return 4 }
  if ($id -match '(?i)protection|armor|armour') { return 3 }
  return 0
}

function Load-EnglishNames([string]$path) {
  $map = @{}
  foreach ($line in [IO.File]::ReadLines($path)) {
    $match = [regex]::Match($line, '^"((?:[^"]|"")*)";"((?:[^"]|"")*)"')
    if ($match.Success) {
      $key = ($match.Groups[1].Value -replace '""', '"')
      $value = ($match.Groups[2].Value -replace '""', '"') -replace '◄|​', ''
      if (-not $map.ContainsKey($key) -and $value) { $map[$key] = $value }
    }
  }
  return $map
}

function Get-NamedBlocks([string]$text, [string]$name) {
  $results = New-Object System.Collections.Generic.List[object]
  $matches = [regex]::Matches($text, "(?m)^\s*" + [regex]::Escape($name) + "\s*\{")
  foreach ($match in $matches) {
    $open = $text.IndexOf('{', $match.Index)
    if ($open -lt 0) { continue }
    $depth = 0
    $quoted = $false
    $escaped = $false
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
          $results.Add([pscustomobject]@{
            Start = $match.Index
            Open = $open
            End = $i
            Text = $text.Substring($match.Index, $i - $match.Index + 1)
          })
          break
        }
      }
    }
  }
  return $results
}

function Get-PresetPairs([string]$text, [string]$pathNeedle) {
  $escaped = [regex]::Escape($pathNeedle)
  # Some helicopter definitions use gameData/flightModels while aircraft use
  # gameData/FlightModels. BLK paths are case-insensitive in the game.
  $pattern = '(?is)preset\s*\{\s*name:t\s*=\s*"([^"]+)"\s*blk:t\s*=\s*"' + $escaped + '([^"]+)\.blk"'
  return [regex]::Matches($text, $pattern)
}

function Get-DirectChildBlocks([string]$containerText) {
  $results = New-Object System.Collections.Generic.List[object]
  $open = $containerText.IndexOf('{')
  $end = $containerText.LastIndexOf('}')
  if ($open -lt 0 -or $end -le $open) { return $results }
  $cursor = $open + 1
  while ($cursor -lt $end) {
    $match = [regex]::Match($containerText.Substring($cursor, $end - $cursor), '(?m)^\s*"?([A-Za-z0-9_\-]+)"?\s*\{')
    if (-not $match.Success) { break }
    $start = $cursor + $match.Index
    $childOpen = $containerText.IndexOf('{', $start)
    if ($childOpen -lt 0 -or $childOpen -ge $end) { break }
    $depth = 0
    $quoted = $false
    $escaped = $false
    $childEnd = -1
    for ($i = $childOpen; $i -lt $end; $i++) {
      $c = $containerText[$i]
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
        if ($depth -eq 0) { $childEnd = $i; break }
      }
    }
    if ($childEnd -lt 0) { break }
    $results.Add([pscustomobject]@{
      Name = $match.Groups[1].Value
      Start = $start
      Open = $childOpen
      End = $childEnd
      Text = $containerText.Substring($start, $childEnd - $start + 1)
    })
    $cursor = $childEnd + 1
  }
  return $results
}

function Get-PresetSummary([string]$presetPath) {
  if (-not (Test-Path -LiteralPath $presetPath)) { return "preset file unavailable" }
  $presetText = [IO.File]::ReadAllText($presetPath)
  $names = [regex]::Matches($presetText, 'preset:t\s*=\s*"([^"]+)"') | ForEach-Object { $_.Groups[1].Value }
  if (-not $names -or $names.Count -eq 0) { return "no suspended armament" }
  $parts = foreach ($group in ($names | Group-Object)) {
    $label = $group.Name -replace '_', ' '
    if ($group.Count -gt 1) { "$($group.Count)x $label" } else { $label }
  }
  return ($parts -join '; ')
}

function Get-ShopMetadata([string]$path) {
  $result = @{}
  $stack = New-Object System.Collections.Generic.List[object]
  $country = ''
  foreach ($line in [IO.File]::ReadLines($path)) {
    $open = [regex]::Match($line, '^\s*"?([A-Za-z0-9_\-]+)"?\s*\{')
    if ($open.Success) {
      $name = $open.Groups[1].Value
      if ($stack.Count -eq 0 -and $name -match '^country_') { $country = $name }
      $stack.Add([pscustomobject]@{ Name = $name; Country = $country })
    }
    $rank = [regex]::Match($line, '^\s*rank:i\s*=\s*(\d+)')
    if ($rank.Success -and $stack.Count -gt 0) {
      $item = $stack[$stack.Count - 1]
      if (-not $result.ContainsKey($item.Name)) {
        $result[$item.Name] = [pscustomobject]@{ Country = $item.Country; Rank = [int]$rank.Groups[1].Value }
      }
    }
    $closeCount = [regex]::Matches($line, '\}').Count
    for ($i = 0; $i -lt $closeCount -and $stack.Count -gt 0; $i++) {
      $stack.RemoveAt($stack.Count - 1)
      if ($stack.Count -eq 0) { $country = '' }
    }
  }
  return $result
}

function Nation-Name([string]$country) {
  $names = @{
    country_usa = 'USA'; country_germany = 'Germany'; country_ussr = 'USSR / Russia'
    country_britain = 'Great Britain'; country_japan = 'Japan'; country_china = 'China'
    country_italy = 'Italy'; country_france = 'France'; country_sweden = 'Sweden'; country_israel = 'Israel'
  }
  if ($names.ContainsKey($country)) { return $names[$country] }
  if ($country) { return (($country -replace '^country_', '') -replace '_', ' ') }
  return 'Other'
}

function Get-SamNation([string]$bulletName) {
  $id = $bulletName.ToLowerInvariant()
  if ($id -match 'aim_|amraam|mim|fim_92|sl_amraam') { return 'USA' }
  if ($id -match 'rb_70|bolide') { return 'Sweden' }
  if ($id -match 'rapier|starstreak|camm') { return 'Great Britain' }
  if ($id -match '9m33|9m331|9m317|57e6|95ya6|tkb_1055') { return 'USSR / Russia' }
  if ($id -match 'python|derby') { return 'Israel' }
  if ($id -match 'iris_t|roland|vt_1') { return 'Germany' }
  if ($id -match 'hn_6|hq17|fm_3000') { return 'China' }
  if ($id -match 'type_91|type_03') { return 'Japan' }
  if ($id -match 'mistral') { return 'France' }
  return 'International'
}

function Get-WeaponCategory([string]$trigger, [string]$icon, [string]$name, [string]$text, [string]$blk) {
  $haystack = ($trigger + ' ' + $icon + ' ' + $name + ' ' + $blk).ToLowerInvariant()
  if ($text -match '(?m)^\s*yield:r\s*=' -or $haystack -match 'nuclear|nuke|thermonuclear|rn_40|rds|b61|an52|an_52') { return 'Nuclear Weapons' }
  if ($trigger -eq 'targetingPod') { return 'Targeting & Sensor Pods' }
  if ($haystack -match 'anti.?radiation|\bharm\b|\barm\b') { return 'Anti-Radiation Missiles' }
  if ($haystack -match 'anti.?ship|harpoon|exocet|sea.?eagle|c-802|kh_35|x_35') { return 'Anti-Ship Missiles' }
  if ($trigger -eq 'aam') { return 'Air-to-Air Missiles' }
  if ($trigger -match 'atgm|agm|guided rockets') { return 'Air-to-Ground Missiles' }
  if ($trigger -match 'guided bombs' -or $icon -match 'guided|jdam|paveway|glide' -or ($trigger -match 'bomb' -and $text -match '(?m)^\s*guidance\s*\{')) { return 'Guided Bombs' }
  if ($trigger -match 'bomb') { return 'Bombs' }
  if ($trigger -match 'rocket') { return 'Rockets' }
  if ($trigger -match 'torpedo') { return 'Torpedoes' }
  if ($trigger -match 'mine') { return 'Mines' }
  return 'Other Weapons'
}

$weaponNames = Load-EnglishNames (Join-Path $LangRoot 'units_weaponry.csv')
$modificationNames = Load-EnglishNames (Join-Path $LangRoot 'units_modifications.csv')
$weaponMetaCache = @{}
function Get-WeaponMeta([string]$blk, [string]$trigger, [string]$icon, [int]$bullets) {
  $cacheKey = "$blk|$trigger|$icon|$bullets"
  if ($weaponMetaCache.ContainsKey($cacheKey)) { return $weaponMetaCache[$cacheKey] }
  $relative = $blk -replace '(?i)^gameData/Weapons/', '' -replace '/', [IO.Path]::DirectorySeparatorChar
  $path = Join-Path $WeaponsRoot $relative
  $base = [IO.Path]::GetFileNameWithoutExtension($relative)
  $text = if (Test-Path -LiteralPath $path) { [IO.File]::ReadAllText($path) } else { '' }
  $name = $null
  foreach ($key in @("weapons/$base", "weapons/$($base -replace '_default$','')")) {
    if ($weaponNames.ContainsKey($key)) { $name = Clean-Field $weaponNames[$key]; break }
  }
  if (-not $name) {
    $plain = Clean-Field ($base -replace '_', ' ')
    $name = [Globalization.CultureInfo]::InvariantCulture.TextInfo.ToTitleCase($plain)
  }
  $massMatch = [regex]::Match($text, '(?m)^\s*mass:r\s*=\s*([0-9.]+)')
  $mass = if ($massMatch.Success) { [double]::Parse($massMatch.Groups[1].Value, [Globalization.CultureInfo]::InvariantCulture) } else { 0.0 }
  # Cannon/gun-pod blk files carry the full fitted mass (gun + ammunition belt)
  # in mass:r, while their bullets:i field is the ammo count, not the store
  # count. Multiplying the mass by the ammo count reported absurd tonnages
  # (e.g. 100 kg x 250 rounds = 25 t for a single MG 151 pod), so cannons keep
  # TotalMass == Mass. Single-barrel pods mark cannon:b = true; multi-barrel
  # guns (M61/GAU-4/M134) and flexible guns (MG 81) expose multibarrel/machine
  # gun icon types instead.
  $isCannon = $text -match '(?m)^\s*cannon:b\s*=\s*true' -or $text -match '(?m)^\s*iconType:t\s*=\s*"(?:machine_gun|multibarrel_)' -or $text -match '(?m)^\s*weaponType:i\s*=\s*3'
  $category = Get-WeaponCategory $trigger $icon $name $text $blk
  if ($text -match '(?m)^\s*container:b\s*=\s*true') {
    $innerBlk = [regex]::Match($text, '(?m)^\s*blk:t\s*=\s*"([^"]+)"')
    $innerBullets = [regex]::Match($text, '(?m)^\s*bullets:i\s*=\s*(\d+)')
    if ($innerBlk.Success) {
      $innerCount = if ($innerBullets.Success) { [int]$innerBullets.Groups[1].Value } else { 1 }
      $inner = Get-WeaponMeta $innerBlk.Groups[1].Value $trigger $icon $innerCount
      $name = $inner.Name + $(if ($innerCount -gt 1) { " x$innerCount" } else { '' })
      $category = $inner.Category
      $mass += $inner.TotalMass
    }
  }
  # TotalMass must be computed after the container payload is folded into $mass.
  $totalMass = if ($isCannon) { $mass } else { $mass * [Math]::Max(1, $bullets) }
  $meta = [pscustomobject]@{ Name = $name; Category = $category; Mass = $mass; TotalMass = $totalMass }
  $weaponMetaCache[$cacheKey] = $meta
  return $meta
}

$unitNames = Load-EnglishNames (Join-Path $LangRoot 'units.csv')
$shopMetadata = Get-ShopMetadata $ShopPath
$aircraftRows = New-Object System.Collections.Generic.List[string]
$presetRows = New-Object System.Collections.Generic.List[string]
$slotRows = New-Object System.Collections.Generic.List[string]
$donorRows = New-Object System.Collections.Generic.List[string]
$aircraftSlotRows = New-Object System.Collections.Generic.List[string]
$weaponCatalogRows = New-Object System.Collections.Generic.List[string]
$modificationRows = New-Object System.Collections.Generic.List[string]
$groundAmmoRows = New-Object System.Collections.Generic.List[string]
$groundAmmoSeen = @{}
$weaponCatalogSeen = @{}
$playable = @{}

if ($PhaseOnly -ne '5') {
Write-Output "[catalog] $(Get-Date -Format HH:mm:ss) Phase 1/4: aircraft flight models"
$allFmFiles = @(Get-ChildItem -LiteralPath $FlightModelsRoot -File -Filter '*.blk' | Sort-Object Name)
$fmCount = 0
foreach ($file in $allFmFiles) {
  $id = [IO.Path]::GetFileNameWithoutExtension($file.Name)
  $nameKey = $id + '_0'
  if (-not $unitNames.ContainsKey($nameKey)) { continue }
  $text = [IO.File]::ReadAllText($file.FullName)
  $typeMatch = [regex]::Match($text, '(?m)^type:t\s*=\s*"([^"]+)"')
  if (-not $typeMatch.Success) { continue }
  $pairs = Get-PresetPairs $text 'gameData/FlightModels/weaponPresets/'
  if ($pairs.Count -eq 0) { continue }
  $display = Clean-DisplayName $unitNames[$nameKey]
  if ($id -match '^nt_') { $display += ' (Nuclear Escalation)' }
  $type = Clean-Field $typeMatch.Groups[1].Value
  $default = ($pairs | Where-Object { $_.Groups[1].Value -match 'default' } | Select-Object -First 1)
  if ($null -eq $default) { $default = $pairs[0] }
  $defaultName = $default.Groups[1].Value
  $shop = if ($shopMetadata.ContainsKey($id)) { $shopMetadata[$id] } else { $null }
  $nation = if ($null -ne $shop) { Nation-Name $shop.Country } else { 'Other' }
  $rank = if ($null -ne $shop) { $shop.Rank } else { 0 }
  $maxloadMatch = [regex]::Match($text, '(?m)^\s*maxloadMass:r\s*=\s*([0-9.]+)')
  $maxload = if ($maxloadMatch.Success) { $maxloadMatch.Groups[1].Value } else { '0' }
  $kind = if ($text -match '(?i)hellicopters_metaparts|(?m)^\s*helicopter\s*\{') { 'Helicopter' } else { 'Aircraft' }
  $aircraftRows.Add("$id`t$display`t$type`t$defaultName`t$nation`t$rank`t$maxload`t$kind")
  $playable[$id] = [pscustomobject]@{ Display = $display; Text = $text; Kind = $kind }
  $fmCount++
  if ($fmCount % 100 -eq 0) { Write-Output "[catalog] $(Get-Date -Format HH:mm:ss) aircraft $fmCount / $($allFmFiles.Count)" }

  $modifications = Get-NamedBlocks $text 'modifications' | Select-Object -First 1
  if ($null -ne $modifications) {
    foreach ($mod in (Get-DirectChildBlocks $modifications.Text)) {
      $modId = $mod.Name
      $displayKey = 'modification/' + $modId
      $uncheckedKey = $displayKey + '_unchecked'
      $modDisplay = if ($modificationNames.ContainsKey($displayKey)) {
        Clean-Field $modificationNames[$displayKey]
      } elseif ($modificationNames.ContainsKey($uncheckedKey)) {
        Clean-Field $modificationNames[$uncheckedKey]
      } else {
        Clean-Field ([Globalization.CultureInfo]::InvariantCulture.TextInfo.ToTitleCase(($modId -replace '_', ' ')))
      }
      $tierMatch = [regex]::Match($mod.Text, '(?m)^\s*tier:i\s*=\s*(-?\d+)')
      $classMatch = [regex]::Match($mod.Text, '(?m)^\s*modClass:t\s*=\s*"([^"]+)"')
      $groupMatch = [regex]::Match($mod.Text, '(?m)^\s*group:t\s*=\s*"([^"]+)"')
      $requireMatches = [regex]::Matches($mod.Text, '(?m)^\s*(?:reqModification|prevModification):t\s*=\s*"([^"]+)"')
      $tier = if ($tierMatch.Success) { $tierMatch.Groups[1].Value } else { '0' }
      $class = if ($classMatch.Success) { $classMatch.Groups[1].Value } else { '' }
      $group = if ($groupMatch.Success) { $groupMatch.Groups[1].Value } else { '' }
      $requires = Clean-Field (($requireMatches | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique) -join '|')
      $modificationRows.Add("$id`t$modId`t$modDisplay`t$tier`t$class`t$group`t$requires")
    }
  }

  foreach ($pair in $pairs) {
    $presetName = $pair.Groups[1].Value
    $presetFileName = $pair.Groups[2].Value + '.blk'
    $presetPath = Join-Path (Join-Path $FlightModelsRoot 'weaponpresets') $presetFileName
    $summary = Clean-Field (Get-PresetSummary $presetPath)
    $presetRows.Add("$id`t$presetName`t$summary")
    if (Test-Path -LiteralPath $presetPath) {
      $presetText = [IO.File]::ReadAllText($presetPath)
      foreach ($wm in [regex]::Matches($presetText, '(?s)Weapon\s*\{\s*slot:i\s*=\s*(\d+)\s*preset:t\s*=\s*"([^"]+)"')) {
        $slotRows.Add("$id`t$presetName`t$($wm.Groups[1].Value)`t$($wm.Groups[2].Value)")
      }
    }
  }
}

# Infantry FPV UAV is a complete player-controllable flight model, but its name is
# stored in inf.csv instead of units.csv, so the regular aircraft pass cannot see it.
$fpvId = 'uav_inf_fpv_strike_drone'
$fpvPath = Join-Path $FlightModelsRoot ($fpvId + '.blk')
if ((Test-Path -LiteralPath $fpvPath) -and -not $playable.ContainsKey($fpvId)) {
  $fpvText = [IO.File]::ReadAllText($fpvPath)
  $aircraftRows.Add("$fpvId`tFPV Strike Drone`ttypeFighter`tuav_inf_fpv_strike_drone_common`tInternational`t8`t0`tDrone")
  $presetRows.Add("$fpvId`tuav_inf_fpv_strike_drone_common`tBuilt-in 2.6 kg HEAT warhead")
  $playable[$fpvId] = [pscustomobject]@{ Display = 'FPV Strike Drone'; Text = $fpvText }
}

# Event flight model shipped by the game. It has no research-tree preset; the
# application writes an empty hot-load preset for it when the mission is built.
$v1Id = 'fau-1'
$v1Path = Join-Path $FlightModelsRoot ($v1Id + '.blk')
if ((Test-Path -LiteralPath $v1Path) -and -not $playable.ContainsKey($v1Id)) {
  $v1Text = [IO.File]::ReadAllText($v1Path)
  $aircraftRows.Add("$v1Id`tV-1 (Fi 103)`ttypeTransport`tfau-1_default`tEvent / Experimental`t0`t0`tDrone")
  $presetRows.Add("$v1Id`tfau-1_default`tBuilt-in event warhead")
  $playable[$v1Id] = [pscustomobject]@{ Display = 'V-1 (Fi 103)'; Text = $v1Text }
}

function Get-PresetWeaponSummary([string]$presetText, [string]$fallbackTrigger) {
  $groups = [regex]::Matches($presetText, 'blk:t\s*=\s*"([^"]+)"') | ForEach-Object { $_.Groups[1].Value } | Group-Object
  if (-not $groups -or $groups.Count -eq 0) { return 'no suspended armament' }
  $parts = foreach ($g in ($groups | Sort-Object Name)) {
    $meta = Get-WeaponMeta $g.Name $fallbackTrigger '' $g.Count
    $label = Clean-Field $meta.Name
    if ($g.Count -gt 1) { "$($g.Count)x $label" } else { $label }
  }
  return ($parts -join '; ')
}

Write-Output "[catalog] $(Get-Date -Format HH:mm:ss) Phase 2/4: pylon slots & weapon donors ($($playable.Count) aircraft)"
$slotCount = 0
foreach ($id in ($playable.Keys | Sort-Object)) {
  $slotCount++
  if ($slotCount % 100 -eq 0) { Write-Output "[catalog] $(Get-Date -Format HH:mm:ss) slots $slotCount / $($playable.Count)" }
  $display = $playable[$id].Display
  $text = $playable[$id].Text
  $legacySlotsFound = $false
  foreach ($slotBlock in (Get-NamedBlocks $text 'WeaponSlot')) {
    $slotMatch = [regex]::Match($slotBlock.Text, 'index:i\s*=\s*(\d+)')
    if (-not $slotMatch.Success) { continue }
    $slot = $slotMatch.Groups[1].Value
    if ([int]$slot -eq 0) { continue }
    $orderMatch = [regex]::Match($slotBlock.Text, 'order:i\s*=\s*(-?\d+)')
    $tierMatch = [regex]::Match($slotBlock.Text, 'tier:i\s*=\s*(-?\d+)')
    $maxloadMatch = [regex]::Match($slotBlock.Text, 'maxloadMass:r\s*=\s*([0-9.]+)')
    $order = if ($orderMatch.Success) { $orderMatch.Groups[1].Value } else { $slot }
    $tier = if ($tierMatch.Success) { $tierMatch.Groups[1].Value } else { '0' }
    $maxload = if ($maxloadMatch.Success) { $maxloadMatch.Groups[1].Value } else { '0' }
    $anchorMount = ''
    foreach ($presetBlock in (Get-NamedBlocks $slotBlock.Text 'WeaponPreset')) {
      $mountName = [regex]::Match($presetBlock.Text, 'name:t\s*=\s*"([^"]+)"')
      if (-not $mountName.Success) { continue }
      $weaponBlocks = Get-NamedBlocks $presetBlock.Text 'Weapon'
      if ($weaponBlocks.Count -eq 0) { continue }
      $weapon = $weaponBlocks[0].Text
      $trigger = [regex]::Match($weapon, 'trigger:t\s*=\s*"([^"]+)"')
      $blk = [regex]::Match($weapon, 'blk:t\s*=\s*"([^"]+)"')
      $emitter = [regex]::Match($weapon, 'emitter:t\s*=\s*"([^"]+)"')
      if (-not $trigger.Success -or -not $blk.Success -or -not $emitter.Success) { continue }
      if ($trigger.Groups[1].Value -match 'fuel tanks|countermeasures|cannon') { continue }
      # 弹数累加按"弹种基名"判定同型：同一挂点方案常把同一型弹拆成多个
      # blk 文件（如 us_aim_120a + us_aim_120a_default，混装成 4 发），若要求
      # blk 完全一致会把同型弹漏计（曾把 4 发 AMRAAM 算成 2 发）。
      # 去掉 _default 后缀后基名相同即视为同型累加；真混装异型弹（不同基名）
      # 仍按首个 weapon 计，保持原行为。
      $blkFamily = (([IO.Path]::GetFileName(($blk.Groups[1].Value -replace '/', '\\'))) -replace '_default\.blk$', '.blk')
      $bullets = 0
      foreach ($candidateWeapon in $weaponBlocks) {
        $candidateTrigger = [regex]::Match($candidateWeapon.Text, 'trigger:t\s*=\s*"([^"]+)"')
        $candidateBlk = [regex]::Match($candidateWeapon.Text, 'blk:t\s*=\s*"([^"]+)"')
        if (-not $candidateTrigger.Success -or -not $candidateBlk.Success) { continue }
        if ($candidateTrigger.Groups[1].Value -ne $trigger.Groups[1].Value) { continue }
        $candidateFamily = (([IO.Path]::GetFileName(($candidateBlk.Groups[1].Value -replace '/', '\\'))) -replace '_default\.blk$', '.blk')
        if ($candidateFamily -ne $blkFamily) { continue }
        $candidateBullets = [regex]::Match($candidateWeapon.Text, 'bullets:i\s*=\s*(\d+)')
        $bullets += if ($candidateBullets.Success) { [int]$candidateBullets.Groups[1].Value } else { 1 }
      }
      if ($bullets -le 0) { $bullets = 1 }
      $iconMatch = [regex]::Match($presetBlock.Text, 'iconType:t\s*=\s*"([^"]+)"')
      $icon = if ($iconMatch.Success) { $iconMatch.Groups[1].Value } else { '' }
      $weaponFile = [IO.Path]::GetFileNameWithoutExtension(($blk.Groups[1].Value -replace '/', '\'))
      $meta = Get-WeaponMeta $blk.Groups[1].Value $trigger.Groups[1].Value $icon $bullets
      $mass = $meta.Mass.ToString('0.###', [Globalization.CultureInfo]::InvariantCulture)
      $totalMass = $meta.TotalMass.ToString('0.###', [Globalization.CultureInfo]::InvariantCulture)
      $label = Clean-Field $meta.Name
      $donorRows.Add("$id`t$display`t$slot`t$($mountName.Groups[1].Value)`t$($trigger.Groups[1].Value)`t$($blk.Groups[1].Value)`t$($emitter.Groups[1].Value)`t$bullets`t$icon`t$label`t$($meta.Category)`t$mass`t$totalMass")
      if (-not $anchorMount) { $anchorMount = $mountName.Groups[1].Value }
      $catalogKey = "$($blk.Groups[1].Value)|$($trigger.Groups[1].Value)|$bullets"
      if (-not $weaponCatalogSeen.ContainsKey($catalogKey)) {
        $weaponCatalogSeen[$catalogKey] = $true
        $weaponCatalogRows.Add("$($trigger.Groups[1].Value)`t$($blk.Groups[1].Value)`t$bullets`t$icon`t$label`t$($meta.Category)`t$mass`t$totalMass")
      }
    }
    if ($anchorMount) { $aircraftSlotRows.Add("$id`t$slot`t$order`t$tier`t$maxload`t$anchorMount"); $legacySlotsFound = $true }
  }

  # Legacy aircraft (A-20G, A-26, A6M Zero, Hudson, ...) keep their external
  # stores in per-preset files as flat Weapon blocks instead of an in-model
  # WeaponSlot tree, so the pylon pass above finds nothing. Model every native
  # loadout preset as a selectable scheme on a single station (slot 0). The
  # application then writes weapons:t = <preset name> into the mission, which
  # is exactly how the game loads hangar loadouts natively.
  if (-not $legacySlotsFound) {
    $presetStylePairs = Get-PresetPairs $text 'gameData/FlightModels/weaponPresets/'
    $stationAdded = $false
    foreach ($pair in $presetStylePairs) {
      $presetName = $pair.Groups[1].Value
      $presetFileName = $pair.Groups[2].Value + '.blk'
      $presetPath = Join-Path (Join-Path $FlightModelsRoot 'weaponpresets') $presetFileName
      if (-not (Test-Path -LiteralPath $presetPath)) { continue }
      $presetText = [IO.File]::ReadAllText($presetPath)
      $weaponBlocks = Get-NamedBlocks $presetText 'Weapon'
      if ($weaponBlocks.Count -eq 0) { continue }
      $first = $weaponBlocks[0].Text
      $trigger = [regex]::Match($first, 'trigger:t\s*=\s*"([^"]+)"')
      $blk = [regex]::Match($first, 'blk:t\s*=\s*"([^"]+)"')
      $emitter = [regex]::Match($first, 'emitter:t\s*=\s*"([^"]+)"')
      if (-not $trigger.Success -or -not $blk.Success) { continue }
      $totalBullets = 0
      foreach ($wb in $weaponBlocks) {
        $b = [regex]::Match($wb.Text, 'bullets:i\s*=\s*(\d+)')
        $totalBullets += if ($b.Success) { [int]$b.Groups[1].Value } else { 1 }
      }
      $summary = Get-PresetWeaponSummary $presetText $trigger.Groups[1].Value
      $meta = Get-WeaponMeta $blk.Groups[1].Value $trigger.Groups[1].Value '' $totalBullets
      $mass = $meta.Mass.ToString('0.###', [Globalization.CultureInfo]::InvariantCulture)
      $totalMass = $meta.TotalMass.ToString('0.###', [Globalization.CultureInfo]::InvariantCulture)
      $emitterValue = if ($emitter.Success) { $emitter.Groups[1].Value } else { '' }
      $donorRows.Add("$id`t$display`t0`t$presetName`t$($trigger.Groups[1].Value)`t$($blk.Groups[1].Value)`t$emitterValue`t$totalBullets`t`t$summary`t$($meta.Category)`t$mass`t$totalMass")
      $catalogKey = "$($blk.Groups[1].Value)|$($trigger.Groups[1].Value)|$totalBullets"
      if (-not $weaponCatalogSeen.ContainsKey($catalogKey)) {
        $weaponCatalogSeen[$catalogKey] = $true
        $weaponCatalogRows.Add("$($trigger.Groups[1].Value)`t$($blk.Groups[1].Value)`t$totalBullets`t`t$summary`t$($meta.Category)`t$mass`t$totalMass")
      }
      if (-not $stationAdded) {
        $aircraftSlotRows.Add("$id`t0`t0`t0`t0`t$presetName")
        $stationAdded = $true
      }
    }
  }
}

# Strategic-bomber nuclear stores can live only in internal-bay/event presets, so
# they never appear in the external-pylon pass above. Add every native bomb gun
# with an explicit yield, including B28 (1.45 Mt), RDS-37 (1.6 Mt) and B83 (1.2 Mt).
$bombGunsRoot = Join-Path $WeaponsRoot 'bombguns'
foreach ($file in (Get-ChildItem -LiteralPath $bombGunsRoot -File -Filter '*.blk' | Sort-Object Name)) {
  $text = [IO.File]::ReadAllText($file.FullName)
  if ($text -notmatch '(?m)^\s*yield:r\s*=\s*[0-9.]+') { continue }
  $blk = 'gameData/Weapons/BombGuns/' + $file.Name
  $iconMatch = [regex]::Match($text, '(?m)^\s*iconType:t\s*=\s*"([^"]+)"')
  $icon = if ($iconMatch.Success) { $iconMatch.Groups[1].Value } else { 'bombs_heavy_nuke' }
  $meta = Get-WeaponMeta $blk 'bombs' $icon 1
  $mass = $meta.Mass.ToString('0.###', [Globalization.CultureInfo]::InvariantCulture)
  $catalogKey = "$blk|bombs|1"
  if (-not $weaponCatalogSeen.ContainsKey($catalogKey)) {
    $weaponCatalogSeen[$catalogKey] = $true
    $weaponCatalogRows.Add("bombs`t$blk`t1`t$icon`t$(Clean-Field $meta.Name)`tNuclear Weapons`t$mass`t$mass")
  }
}

# Ground-based SAM launchers store their missiles inside user_cannon files instead of
# aircraft rocketGun files. Add every unique SAM round as a virtual catalog entry;
# the GUI converts the selected round to an aircraft-compatible rocketGun at build time.
$samCandidates = New-Object System.Collections.Generic.List[object]
$groundWeaponsRoot = Join-Path $WeaponsRoot 'groundmodels_weapons'
foreach ($file in (Get-ChildItem -LiteralPath $groundWeaponsRoot -File -Filter '*user_cannon.blk' | Sort-Object Name)) {
  $text = [IO.File]::ReadAllText($file.FullName)
  if ($text -notmatch 'bulletType:t\s*=\s*"sam_tank"|isAam:b\s*=\s*true') { continue }
  foreach ($bulletBlock in (Get-NamedBlocks $text 'bullet')) {
    if ($bulletBlock.Text -notmatch 'bulletType:t\s*=\s*"sam_tank"|isAam:b\s*=\s*true') { continue }
    $bulletMatch = [regex]::Match($bulletBlock.Text, 'bulletName:t\s*=\s*"([^"]+)"')
    if (-not $bulletMatch.Success) { continue }
    $rocketBlock = Get-NamedBlocks $bulletBlock.Text 'rocket' | Select-Object -First 1
    if ($null -eq $rocketBlock) { continue }
    $massMatch = [regex]::Match($rocketBlock.Text, '(?m)^\s*mass:r\s*=\s*([0-9.]+)')
    $mass = if ($massMatch.Success) { [double]::Parse($massMatch.Groups[1].Value, [Globalization.CultureInfo]::InvariantCulture) } else { 0.0 }
    $samCandidates.Add([pscustomobject]@{ File = $file.Name; Bullet = $bulletMatch.Groups[1].Value; Mass = $mass })
  }
}

foreach ($group in ($samCandidates | Group-Object Bullet | Sort-Object Name)) {
  $sam = $group.Group | Sort-Object @{ Expression = { if ($_.File -match 'nasams') { 0 } else { 1 } } }, File | Select-Object -First 1
  $key = 'weapons/' + $sam.Bullet
  $plainKey = $sam.Bullet
  $name = if ($weaponNames.ContainsKey($key)) { Clean-Field $weaponNames[$key] } elseif ($weaponNames.ContainsKey($plainKey)) { Clean-Field $weaponNames[$plainKey] } else { Clean-Field (($sam.Bullet -replace '_', ' ').ToUpperInvariant()) }
  $mass = $sam.Mass.ToString('0.###', [Globalization.CultureInfo]::InvariantCulture)
  $descriptor = 'utl-sam:gamedata/weapons/groundmodels_weapons/' + $sam.File + '#' + $sam.Bullet
  $catalogKey = "$descriptor|aam|1"
  if (-not $weaponCatalogSeen.ContainsKey($catalogKey)) {
    $weaponCatalogSeen[$catalogKey] = $true
    $weaponCatalogRows.Add("aam`t$descriptor`t1`tmissile_type_b_air_to_air`t$name (Ground SAM)`tGround SAM Missiles`t$mass`t$mass`t$(Get-SamNation $sam.Bullet)")
  }
}

function Add-GroundModifications([string]$id, [string]$text) {
  $mods = Get-NamedBlocks $text 'modifications' | Select-Object -First 1
  if ($null -eq $mods) { return }
  foreach ($mod in (Get-DirectChildBlocks $mods.Text)) {
    if ($mod.Name -match '(?i)_expendable$') { continue }
    $tier = Get-GroundModificationTier $mod.Name $mod.Text
    # Empty projectile-selector blocks are weapon configuration, not research modules.
    if ($tier -le 0 -and $mod.Name -notmatch '(?i)ammo_pack$' -and $mod.Text -notmatch '(?m)^\s*(?:effects|disableModEffects)\s*\{') { continue }
    $displayKey = 'modification/' + $mod.Name
    $uncheckedKey = $displayKey + '_unchecked'
    $display = if ($modificationNames.ContainsKey($displayKey)) {
      Format-ProjectileName $modificationNames[$displayKey]
    } elseif ($modificationNames.ContainsKey($uncheckedKey)) {
      Format-ProjectileName $modificationNames[$uncheckedKey]
    } else {
      Format-ProjectileName ([Globalization.CultureInfo]::InvariantCulture.TextInfo.ToTitleCase(($mod.Name -replace '_', ' ')))
    }
    $classMatch = [regex]::Match($mod.Text, '(?m)^\s*modClass:t\s*=\s*"([^"]+)"')
    $groupMatch = [regex]::Match($mod.Text, '(?m)^\s*group:t\s*=\s*"([^"]+)"')
    $requireMatches = [regex]::Matches($mod.Text, '(?m)^\s*(?:reqModification|prevModification):t\s*=\s*"([^"]+)"')
    $class = if ($classMatch.Success) { $classMatch.Groups[1].Value } else { '' }
    $group = if ($groupMatch.Success) { $groupMatch.Groups[1].Value } else { '' }
    $requires = Clean-Field (($requireMatches | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique) -join '|')
    $modificationRows.Add("$id`t$($mod.Name)`t$display`t$tier`t$class`t$group`t$requires")
  }
}
}
function Get-BulletContainer([string]$text, [int]$bulletStart) {
  # bulletStart is the start of the matched line (before any indent).
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

function Build-GroundAmmoCatalog {
  Write-Output "[catalog] $(Get-Date -Format HH:mm:ss) Phase 4/4: ground ammunition catalog"
  # Scan every weapon blk in groundmodels_weapons: user cannon files, SAM/ATG
  # missile launchers (rocket_launcher / atgm launchers) and any other weapon
  # file that carries bullet definitions. Filtering only "*user_*.blk" skipped
  # missile launchers such as 170mm_57e6_rocket_launcher.blk, so SAM/ATGM
  # rounds never made it into the ground ammunition catalog.
  foreach ($file in (Get-ChildItem -LiteralPath (Join-Path $WeaponsRoot 'groundmodels_weapons') -File -Filter '*.blk' | Sort-Object Name)) {
    $source = 'gameData/Weapons/groundModels_weapons/' + $file.Name
    $text = [IO.File]::ReadAllText($file.FullName)
    foreach ($bullet in (Get-NamedBlocks $text 'bullet')) {
      $nameMatch = [regex]::Match($bullet.Text, '(?m)^\s*bulletName:t\s*=\s*"([^"]+)"')
      if (-not $nameMatch.Success) { continue }
      $bulletName = $nameMatch.Groups[1].Value
      $key = "$source|$bulletName"
      if ($groundAmmoSeen.ContainsKey($key)) { continue }
      $groundAmmoSeen[$key] = $true
      $displayKey = 'weapons/' + $bulletName
      $display = if ($weaponNames.ContainsKey($displayKey)) { Format-ProjectileName $weaponNames[$displayKey] } else { Format-ProjectileName ([Globalization.CultureInfo]::InvariantCulture.TextInfo.ToTitleCase(($bulletName -replace '_', ' '))) }
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
      $groundAmmoRows.Add("$source`t$container`t$bulletName`t$display`t$kind`t$mass`t$speed`t$explosive`t$caliber`t$penetration")
    }
  }
}

function Get-WeaponFile([string]$blk) {
  if ([string]::IsNullOrWhiteSpace($blk)) { return $null }
  $name = [IO.Path]::GetFileName(($blk -replace '/', '\'))
  $direct = @(
    (Join-Path (Join-Path $WeaponsRoot 'groundmodels_weapons') $name),
    (Join-Path (Join-Path $WeaponsRoot 'navalmodels_weapons') $name),
    (Join-Path $WeaponsRoot $name)
  )
  foreach ($c in $direct) { if (Test-Path -LiteralPath $c) { return Get-Item -LiteralPath $c } }
  try { return Get-ChildItem -LiteralPath $WeaponsRoot -Recurse -File -Filter $name | Select-Object -First 1 } catch { return $null }
}

function Escape-Json([string]$s) {
  if ($null -eq $s) { return '' }
  return $s.Replace('"', '\"')
}

function ConvertTo-JsonManual($map) {
  $sb = New-Object System.Text.StringBuilder
  [void]$sb.Append('{')
  $firstVehicle = $true
  foreach ($id in $map.Keys) {
    if (-not $firstVehicle) { [void]$sb.Append(',') }
    $firstVehicle = $false
    $e = $map[$id]
    [void]$sb.Append('"').Append((Escape-Json $id)).Append('":{')
    [void]$sb.Append('"weapons":[')
    for ($i = 0; $i -lt $e.weapons.Count; $i++) {
      if ($i -gt 0) { [void]$sb.Append(',') }
      $w = $e.weapons[$i]
      [void]$sb.Append('{"trigger":"').Append((Escape-Json $w.trigger)).Append('","blk":"').Append((Escape-Json $w.blk)).Append('","nativeAmmo":').Append([int]$w.nativeAmmo).Append('}')
    }
    [void]$sb.Append('],"missiles":[')
    for ($i = 0; $i -lt $e.missiles.Count; $i++) {
      if ($i -gt 0) { [void]$sb.Append(',') }
      $m = $e.missiles[$i]
      [void]$sb.Append('{"name":"').Append((Escape-Json $m.name)).Append('","blk":"').Append((Escape-Json $m.blk)).Append('"}')
    }
    [void]$sb.Append('],"beltOptions":[')
    for ($i = 0; $i -lt $e.beltOptions.Count; $i++) {
      if ($i -gt 0) { [void]$sb.Append(',') }
      $bo = $e.beltOptions[$i]
      [void]$sb.Append('{"name":"').Append((Escape-Json $bo.name)).Append('","calibre":').Append($bo.calibre).Append(',"rounds":[')
      for ($j = 0; $j -lt $bo.rounds.Count; $j++) {
        if ($j -gt 0) { [void]$sb.Append(',') }
        $r = $bo.rounds[$j]
        [void]$sb.Append('{"bulletName":"').Append((Escape-Json $r.bulletName)).Append('","display":"').Append((Escape-Json $r.display)).Append('","kind":"').Append((Escape-Json $r.kind)).Append('"')
        [void]$sb.Append(',"mass":').Append(([double]$r.mass).ToString([Globalization.CultureInfo]::InvariantCulture))
        [void]$sb.Append(',"speed":').Append(([double]$r.speed).ToString([Globalization.CultureInfo]::InvariantCulture))
        [void]$sb.Append(',"explosive":').Append(([double]$r.explosive).ToString([Globalization.CultureInfo]::InvariantCulture))
        [void]$sb.Append(',"caliber":').Append(([double]$r.caliber).ToString([Globalization.CultureInfo]::InvariantCulture))
        [void]$sb.Append(',"penetration":').Append(([double]$r.penetration).ToString([Globalization.CultureInfo]::InvariantCulture)).Append('}')
      }
      [void]$sb.Append(']}')
    }
    [void]$sb.Append('],"rackRounds":{')
    $rk = $e.rackRounds
    $rkFirst = $true
    foreach ($k in $rk.Keys) {
      if (-not $rkFirst) { [void]$sb.Append(',') }
      $rkFirst = $false
      [void]$sb.Append('"').Append((Escape-Json $k)).Append('":').Append([int]$rk[$k])
    }
    [void]$sb.Append('},"beltSizes":{')
    $bs = $e.beltSizes
    $bsFirst = $true
    foreach ($k in $bs.Keys) {
      if (-not $bsFirst) { [void]$sb.Append(',') }
      $bsFirst = $false
      [void]$sb.Append('"').Append((Escape-Json $k)).Append('":').Append([int]$bs[$k])
    }
    [void]$sb.Append('},"beltTypeLimit":').Append([int]$e.beltTypeLimit).Append('}')
  }
  [void]$sb.Append('}')
  return $sb.ToString()
}

function Format-Json([string]$raw) {
  $sb = New-Object System.Text.StringBuilder
  $indent = 0
  $inString = $false
  for ($i = 0; $i -lt $raw.Length; $i++) {
    $c = $raw[$i]
    if ($inString) {
      [void]$sb.Append($c)
      if ($c -eq '\' -and ($i + 1) -lt $raw.Length) { [void]$sb.Append($raw[$i + 1]); $i++ }
      elseif ($c -eq '"') { $inString = $false }
      continue
    }
    switch ($c) {
      '"' { $inString = $true; [void]$sb.Append($c) }
      '{' { [void]$sb.Append("`n"); [void]$sb.Append('  ' * $indent); [void]$sb.Append($c); $indent++ }
      '}' { $indent--; [void]$sb.Append("`n"); [void]$sb.Append('  ' * $indent); [void]$sb.Append($c) }
      '[' { [void]$sb.Append("`n"); [void]$sb.Append('  ' * $indent); [void]$sb.Append($c); $indent++ }
      ']' { $indent--; [void]$sb.Append("`n"); [void]$sb.Append('  ' * $indent); [void]$sb.Append($c) }
      ',' { [void]$sb.Append($c); [void]$sb.Append("`n"); [void]$sb.Append('  ' * $indent) }
      ':' { [void]$sb.Append($c); [void]$sb.Append(' ') }
      default { [void]$sb.Append($c) }
    }
  }
  return $sb.ToString()
}

function ConvertTo-GroundAmmoJson([string[]]$rows) {
  $sb = New-Object System.Text.StringBuilder
  [void]$sb.Append('[')
  for ($i = 0; $i -lt $rows.Count; $i++) {
    if ($i -gt 0) { [void]$sb.Append(',') }
    $p = $rows[$i] -split "`t"
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
  return $sb.ToString()
}

function Build-VehicleWeaponsJson {
  Write-Output "[catalog] $(Get-Date -Format HH:mm:ss) Phase 5/5: vehicle weapons json (prebuilt ammo data)"
  # Belt-type limits: ammunition types a vehicle may carry per belt weapon (game
  # facts; cross-checked against Ask3lad DB but authored independently).
  $beltLimitPath = Join-Path $OutputRoot 'belt_type_limits.tsv'
  $beltTypeLimits = @{}
  if (Test-Path -LiteralPath $beltLimitPath) {
    foreach ($line in [IO.File]::ReadAllLines($beltLimitPath)) {
      if ([string]::IsNullOrWhiteSpace($line)) { continue }
      $parts = $line -split "`t"
      if ($parts.Count -ge 2) {
        $v = 0
        if ([int]::TryParse($parts[1].Trim(), [ref]$v) -and $v -gt 1) { $beltTypeLimits[$parts[0].Trim()] = $v }
      }
    }
  }
  # Prebuilt ground-ammo catalog (source + container) for belt-option rounds.
  $gaJsonPath = Join-Path $OutputRoot 'ground_ammo.json'
  if (-not (Test-Path -LiteralPath $gaJsonPath)) {
    Build-GroundAmmoCatalog
    [IO.File]::WriteAllText($gaJsonPath, (ConvertTo-GroundAmmoJson ($groundAmmoRows | Sort-Object { ($_ -split "`t")[4] }, { ($_ -split "`t")[3] }, { ($_ -split "`t")[2] })), [Text.UTF8Encoding]::new($false))
  }
  $groundAmmoBySourceContainer = @{}
  $knownAmmoContainers = @{}
  $gaList = Get-Content -LiteralPath $gaJsonPath -Raw | ConvertFrom-Json
  foreach ($ga in $gaList) {
    if ($null -eq $ga -or [string]::IsNullOrWhiteSpace($ga.container)) { continue }
    $key = ([string]$ga.source).ToLowerInvariant() + '|' + [string]$ga.container
    if (-not $groundAmmoBySourceContainer.ContainsKey($key)) { $groundAmmoBySourceContainer[$key] = New-Object System.Collections.ArrayList }
    [void]$groundAmmoBySourceContainer[$key].Add($ga)
    $knownAmmoContainers[([string]$ga.container).ToLowerInvariant()] = $true
  }
  $map = @{}
  $tankDir = Join-Path $UnitsRoot 'tankmodels'
  $tankFiles = Get-ChildItem -LiteralPath $tankDir -File -Filter '*.blk' | Sort-Object Name
  $built = 0
  foreach ($file in $tankFiles) {
    $id = [IO.Path]::GetFileNameWithoutExtension($file.Name)
    $text = [IO.File]::ReadAllText($file.FullName)
    $entry = @{
      weapons = New-Object System.Collections.ArrayList
      missiles = New-Object System.Collections.ArrayList
      beltOptions = New-Object System.Collections.ArrayList
      rackRounds = @{}
      beltSizes = @{}
      beltTypeLimit = 1
    }
    $seenWeapons = @{}
    $lastWkey = ''
    $seenMissiles = @{}
    # weapons + rack rounds (mirror WorkspaceGroundWeapons / WorkspaceRackRounds)
    foreach ($w in (Get-NamedBlocks $text 'Weapon')) {
      $trigger = [regex]::Match($w.Text, '(?m)^\s*trigger:t\s*=\s*"([^"]+)"')
      if (-not $trigger.Success) { continue }
      $blkM = [regex]::Match($w.Text, '(?m)^\s*blk:t\s*=\s*"([^"]+)"')
      if (-not $blkM.Success) { continue }
      $blk = $blkM.Groups[1].Value
      $ammo = 0
      $ammoM = [regex]::Match($w.Text, '(?m)^\s*bullets:i\s*=\s*(-?[0-9]+)')
      if ($ammoM.Success) { $ammo = [int]$ammoM.Groups[1].Value }
      if ($ammo -le 0) {
        $wfile = Get-WeaponFile $blk
        if ($null -ne $wfile) {
          $wtxt = [IO.File]::ReadAllText($wfile.FullName)
          $cm = [regex]::Match($wtxt, '(?m)^\s*bullets:i\s*=\s*(-?[0-9]+)')
          if ($cm.Success) { $ammo = [int]$cm.Groups[1].Value }
        }
      }
      $wkey = ($trigger.Groups[1].Value + '|' + ($blk -replace '\\', '/')).ToLowerInvariant()
      if ($wkey -eq $lastWkey) {
        $wi = $entry.weapons.Count - 1
        if ($wi -ge 0) { $entry.weapons[$wi].nativeAmmo += $ammo }
        continue
      }
      $lastWkey = $wkey
      if ($seenWeapons.ContainsKey($wkey)) { continue }
      $seenWeapons[$wkey] = $true
      [void]$entry.weapons.Add(@{ trigger = $trigger.Groups[1].Value; blk = $blk; nativeAmmo = $ammo })
      $calM = [regex]::Match($blk, '(\d+)(?:_\d+)?mm', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
      if ($calM.Success) {
        $cal = [int]$calM.Groups[1].Value
        if ($cal -gt 0 -and $cal -le 40 -and -not $entry.beltSizes.ContainsKey($cal.ToString())) {
          $wfile3 = Get-WeaponFile $blk
          if ($null -ne $wfile3) {
            $wtxt3 = [IO.File]::ReadAllText($wfile3.FullName)
            $bcM = [regex]::Match($wtxt3, '(?m)^\s*bulletsCartridge:i\s*=\s*(\d+)')
            if ($bcM.Success -and [int]$bcM.Groups[1].Value -gt 0) {
              $entry.beltSizes[$cal.ToString()] = [int]$bcM.Groups[1].Value
            }
          }
        }
      }
      if ($blk -match '(?i)launcher|container') {
        $wfile2 = Get-WeaponFile $blk
        if ($null -ne $wfile2) {
          $wtxt2 = [IO.File]::ReadAllText($wfile2.FullName)
          $rm = [regex]::Match($wtxt2, '(?m)^\s*bullets:i\s*=\s*(\d+)\s*$')
          if ($rm.Success -and [int]$rm.Groups[1].Value -gt 1) { $entry.rackRounds[$blk] = [int]$rm.Groups[1].Value }
        }
      }
    }
    # missiles (mirror WorkspaceVehicleMissiles)
    foreach ($pylon in (Get-NamedBlocks $text 'WeaponPilons')) {
      foreach ($slot in (Get-NamedBlocks $pylon.Text 'WeaponSlot')) {
        foreach ($wp in (Get-NamedBlocks $slot.Text 'WeaponPreset')) {
          $pname = [regex]::Match($wp.Text, '(?m)^\s*name:t\s*=\s*"([^"]+)"')
          if (-not $pname.Success) { continue }
          foreach ($weapon in (Get-NamedBlocks $wp.Text 'Weapon')) {
            $wblkM = [regex]::Match($weapon.Text, '(?m)^\s*blk:t\s*=\s*"([^"]+)"')
            if (-not $wblkM.Success) { continue }
            $mkey = (($wblkM.Groups[1].Value -replace '\\', '/') -replace '(?i)^gameData/', 'gamedata/').ToLowerInvariant() + '|' + $pname.Groups[1].Value
            if ($seenMissiles.ContainsKey($mkey)) { continue }
            $seenMissiles[$mkey] = $true
            [void]$entry.missiles.Add(@{ name = $pname.Groups[1].Value; blk = $wblkM.Groups[1].Value })
          }
        }
      }
    }
    # belt options (mirror WorkspaceGunBeltOptions); each option carries the
    # rounds (bulletName + params) available inside its cannon container so the
    # UI can filter "container in beltOptions" per vehicle.
    $mods = Get-NamedBlocks $text 'modifications' | Select-Object -First 1
    if ($null -ne $mods) {
      foreach ($mod in (Get-DirectChildBlocks $mods.Text)) {
        $mname = $mod.Name
        # Ammo packs are named after their cannon container. Most carry a calibre
        # prefix (125mm_ussr_HE), but legacy packs keep a bare name (USSR_APDS_FS).
        # Accept either, as long as the name matches a known cannon container.
        if ($mname -notmatch '^\d+mm_' -and -not $knownAmmoContainers.ContainsKey($mname.ToLowerInvariant())) { continue }
        if ($mname -match '(?i)_ammo_pack$') { continue }
        if ((Get-DirectChildBlocks $mod.Text).Count -gt 0) { continue }
        $belt = @{ name = $mname; calibre = 0; rounds = (New-Object System.Collections.ArrayList) }
        if ($mname -match '^(\d+(?:_\d+)?)mm_') { $belt.calibre = [int]$Matches[1] }
        foreach ($w in $entry.weapons) {
          $blkLower = ([string]$w.blk).ToLowerInvariant()
          $prefix = $blkLower + '|'
          foreach ($knownKey in $groundAmmoBySourceContainer.Keys) {
            if (-not $knownKey.StartsWith($prefix)) { continue }
            $container = $knownKey.Substring($prefix.Length)
            $exact = ($container -eq $mname)
            $stripped = ''
            if (-not $exact) {
              $m2 = [regex]::Match($container, '^\d+(?:_\d+)?mm_(.+)$')
              if (-not $m2.Success) { continue }
              $stripped = $m2.Groups[1].Value
              if ($stripped -ne $mname) { continue }
            }
            if ($belt.calibre -le 0) {
              $m3 = [regex]::Match($container, '^(\d+(?:_\d+)?)mm_')
              if ($m3.Success) { $belt.calibre = [int]$m3.Groups[1].Value }
            }
            foreach ($ga in $groundAmmoBySourceContainer[$knownKey]) {
              [void]$belt.rounds.Add(@{ bulletName = [string]$ga.bulletName; display = [string]$ga.display; kind = [string]$ga.kind; mass = [double]$ga.mass; speed = [double]$ga.speed; explosive = [double]$ga.explosive; caliber = [double]$ga.caliber; penetration = [double]$ga.penetration })
            }
          }
        }
        [void]$entry.beltOptions.Add($belt)
      }
    }
    if ($beltTypeLimits.ContainsKey($id)) { $entry.beltTypeLimit = $beltTypeLimits[$id] }
    if ($entry.weapons.Count -gt 0 -or $entry.missiles.Count -gt 0 -or $entry.beltOptions.Count -gt 0 -or $entry.rackRounds.Count -gt 0) {
      $map[$id] = $entry
      $built++
    }
    if ($built % 100 -eq 0) { Write-Output "[catalog] $(Get-Date -Format HH:mm:ss) vehicle-weapons $built / $($tankFiles.Count)" }
  }
  $json = ConvertTo-JsonManual $map
  $json = Format-Json $json
  [IO.File]::WriteAllText((Join-Path $OutputRoot 'vehicle_weapons.json'), $json, [Text.UTF8Encoding]::new($false))
  Write-Output "VehicleWeapons=$built"
}

function Build-TargetCatalog([string]$directory, [string]$presetPathNeedle, [string]$outputName, [bool]$includeGroundDetails = $false) {
  Write-Output "[catalog] $(Get-Date -Format HH:mm:ss) Phase 3/4: $outputName"
  $rows = New-Object System.Collections.Generic.List[string]
  foreach ($file in (Get-ChildItem -LiteralPath $directory -File -Filter '*.blk' | Sort-Object Name)) {
    $id = [IO.Path]::GetFileNameWithoutExtension($file.Name)
    $key = $id + '_0'
    if (-not $unitNames.ContainsKey($key) -and -not $unitNames.ContainsKey($id + '_shop')) { continue }
    $text = [IO.File]::ReadAllText($file.FullName)
    $pairs = Get-PresetPairs $text $presetPathNeedle
    $preset = if ($pairs.Count -gt 0) { $pairs[0].Groups[1].Value } else { $id + '_default' }
    $shop = if ($shopMetadata.ContainsKey($id)) { $shopMetadata[$id] } else { $null }
    $isEventVehicle = $includeGroundDetails -and ($id -match '(?i)_event$|ladungstrager|goliath')
    $nation = if ($isEventVehicle) { 'Event / Experimental' } elseif ($null -ne $shop) { Nation-Name $shop.Country } elseif ($includeGroundDetails) { 'Event / Experimental' } else { 'Other' }
    $rank = if ($null -ne $shop) { $shop.Rank } else { 0 }
    $typeMatch = [regex]::Match($text, '(?m)^\s*type:t\s*=\s*"([^"]+)"')
    $type = if ($typeMatch.Success) { Clean-Field $typeMatch.Groups[1].Value } else { $(if ($includeGroundDetails) { 'Ground Vehicle' } else { 'Ship' }) }
    $mainCannon = ''
    $maxAmmo = 0; $nativeReload = 0; $nativeRecoil = 0
    if ($includeGroundDetails) {
      foreach ($weapon in (Get-NamedBlocks $text 'Weapon')) {
        if ($weapon.Text -notmatch '(?m)^\s*trigger:t\s*=\s*"gunner0"') { continue }
        $blkMatch = [regex]::Match($weapon.Text, '(?m)^\s*blk:t\s*=\s*"([^"]+)"')
        if ($blkMatch.Success) {
          $mainCannon = $blkMatch.Groups[1].Value
          $ammoMatch = [regex]::Match($weapon.Text, '(?m)^\s*bullets:i\s*=\s*(\d+)')
          $freqMatch = [regex]::Match($weapon.Text, '(?m)^\s*shotFreq:r\s*=\s*([0-9.eE+-]+)')
          $recoilMatch = [regex]::Match($weapon.Text, '(?m)^\s*recoilOffset:r\s*=\s*([0-9.eE+-]+)')
          if ($ammoMatch.Success) { $maxAmmo = [int]$ammoMatch.Groups[1].Value }
          if ($freqMatch.Success -and [double]$freqMatch.Groups[1].Value -gt 0) { $nativeReload = 1.0 / [double]$freqMatch.Groups[1].Value }
          if ($recoilMatch.Success) { $nativeRecoil = [double]$recoilMatch.Groups[1].Value }
          break
        }
      }
      Add-GroundModifications $id $text
    }
    $row = "$id`t$(Get-VehicleDisplayName $id)`t$preset`t$nation`t$rank`t$type"
    if ($includeGroundDetails) {
      $massMatch = [regex]::Match($text, '(?m)^\s*mass:r\s*=\s*([0-9.eE+-]+)')
      $forwardMatch = [regex]::Match($text, '(?m)^\s*maxFwdSpeed:r\s*=\s*([0-9.eE+-]+)')
      $reverseMatch = [regex]::Match($text, '(?m)^\s*maxRevSpeed:r\s*=\s*([0-9.eE+-]+)')
      $powerMatch = [regex]::Match($text, '(?m)^\s*horsePowers:r\s*=\s*([0-9.eE+-]+)')
      $nativeMass = if ($massMatch.Success) { $massMatch.Groups[1].Value } else { '0' }
      $nativeForward = if ($forwardMatch.Success) { $forwardMatch.Groups[1].Value } else { '0' }
      $nativeReverse = if ($reverseMatch.Success) { $reverseMatch.Groups[1].Value } else { '0' }
      $nativePower = if ($powerMatch.Success) { $powerMatch.Groups[1].Value } else { '0' }
      $row += "`t$mainCannon`t$maxAmmo`t$nativeMass`t$nativePower`t$nativeForward`t$nativeReverse`t$($nativeReload.ToString('0.######', [Globalization.CultureInfo]::InvariantCulture))`t$($nativeRecoil.ToString('0.######', [Globalization.CultureInfo]::InvariantCulture))"
    }
    $rows.Add($row)
  }
  [IO.File]::WriteAllLines((Join-Path $OutputRoot $outputName), $rows, [Text.UTF8Encoding]::new($false))
}

if ($PhaseOnly -ne '5') {
Write-Output "[catalog] $(Get-Date -Format HH:mm:ss) writing output files"
[IO.File]::WriteAllLines((Join-Path $OutputRoot 'aircraft.tsv'), $aircraftRows, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllLines((Join-Path $OutputRoot 'presets.tsv'), $presetRows, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllLines((Join-Path $OutputRoot 'preset_slots.tsv'), $slotRows, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllLines((Join-Path $OutputRoot 'donor_weapons.tsv'), $donorRows, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllLines((Join-Path $OutputRoot 'aircraft_slots.tsv'), $aircraftSlotRows, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllLines((Join-Path $OutputRoot 'weapon_catalog.tsv'), ($weaponCatalogRows | Sort-Object { ($_ -split "`t")[5] }, { [double](($_ -split "`t")[7]) }, { ($_ -split "`t")[4] }), [Text.UTF8Encoding]::new($false))

# Naval cannons (for cross-domain cannon injection into ground vehicles)
$navalCannonRows = New-Object System.Collections.Generic.List[string]
$navalRoot = Join-Path $WeaponsRoot 'navalmodels_weapons'
if (Test-Path -LiteralPath $navalRoot)
{
    foreach ($navalFile in (Get-ChildItem -LiteralPath $navalRoot -File -Filter '*user_cannon.blk' | Sort-Object Name))
    {
        $navalDisplay = (($navalFile.BaseName -replace '_naval_user_cannon$', '') -replace '_', ' ').Trim()
        $navalCannonRows.Add("gamedata/weapons/navalmodels_weapons/$($navalFile.Name)`t$navalDisplay")
    }
}
[IO.File]::WriteAllLines((Join-Path $OutputRoot 'naval_cannons.tsv'), $navalCannonRows, [Text.UTF8Encoding]::new($false))

# Air ordnance — guns and rocket/missile launchers (for cross-domain injection into ground vehicles)
$airOrdRows = New-Object System.Collections.Generic.List[string]
function Add-AirOrdnance([string]$subDir, [string]$kind) {
    $full = Join-Path $WeaponsRoot $subDir
    if (-not (Test-Path -LiteralPath $full)) { return }
    foreach ($airFile in (Get-ChildItem -LiteralPath $full -File -Filter '*.blk' | Sort-Object Name)) {
        $airDisplay = (($airFile.BaseName -replace '[_\-\.]', ' ') -replace '\s+', ' ').Trim()
        $airOrdRows.Add("gamedata/weapons/$subDir/$($airFile.Name)`t$airDisplay`t$kind")
    }
}
Add-AirOrdnance 'rocketguns' 'rocket'
foreach ($airFile in (Get-ChildItem -LiteralPath $WeaponsRoot -File -Filter '*.blk' | Where-Object { $_.Name -match '^(cannon|gun|machinegun)' } | Sort-Object Name)) {
    $airDisplay = (($airFile.BaseName -replace '[_\-\.]', ' ') -replace '\s+', ' ').Trim()
    $airOrdRows.Add("gamedata/weapons/$($airFile.Name)`t$airDisplay`tcannon")
}
[IO.File]::WriteAllLines((Join-Path $OutputRoot 'air_ordnance.tsv'), $airOrdRows, [Text.UTF8Encoding]::new($false))
Build-TargetCatalog (Join-Path $UnitsRoot 'tankmodels') 'gameData/units/tankModels/weaponPresets/' 'ground.tsv' $true
Build-TargetCatalog (Join-Path $UnitsRoot 'ships') 'gameData/units/ships/weaponPresets/' 'ships.tsv' $false
Build-GroundAmmoCatalog
[IO.File]::WriteAllLines((Join-Path $OutputRoot 'modifications.tsv'), ($modificationRows | Sort-Object { ($_ -split "`t")[0] }, { [int](($_ -split "`t")[3]) }, { ($_ -split "`t")[2] }), [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $OutputRoot 'ground_ammo.json'), (ConvertTo-GroundAmmoJson ($groundAmmoRows | Sort-Object { ($_ -split "`t")[4] }, { ($_ -split "`t")[3] }, { ($_ -split "`t")[2] })), [Text.UTF8Encoding]::new($false))
Write-Output "GroundAmmo=$($groundAmmoRows.Count)"

$nuclearRows = @(
  "nt_su_24m`tSu-24M — RN-40 (30 kt)`tnt_su_24m_rn_40",
  "f_111f_killstreak`tF-111F — B61`tf_111f_1xb61",
  "f_16a_block_15_adf_killstreak`tF-16A ADF — B61`tf_16a_block_15_adf_1xb61",
  "f_16d_block_40_barak_2_killstreak`tF-16D Barak II — B61`tf_16d_block_40_barak_2_1xb61",
  "jaguar_a_killstreak`tJaguar A — AN-52`tjaguar_a_1xan52",
  "su-7bkl_killstreak`tSu-7BKL — RN-24`tsu_7bkl_rn24",
  "b-29_killstreak`tB-29 — Mk 6`tb_29_1xmk6",
  "tu_4_killstreak`tTu-4 — RDS-4`ttu_4_1xrds4"
)
[IO.File]::WriteAllLines((Join-Path $OutputRoot 'nuclear.tsv'), $nuclearRows, [Text.UTF8Encoding]::new($false))

# Unit -> weapon mapping (ground / naval / air) for cross-domain cannon injection
$unitWeaponRows = New-Object System.Collections.Generic.List[string]
$unitWeaponSeen = @{}
function Add-UnitWeaponRow([string]$uid, [string]$domain, [string]$udisplay, [string]$wblk, [string]$wdisplay, [string]$kind) {
    $key = "$uid|$domain|$wblk"
    if ($unitWeaponSeen.ContainsKey($key)) { return }
    $unitWeaponSeen[$key] = $true
    $unitWeaponRows.Add("$uid`t$domain`t$udisplay`t$wblk`t$wdisplay`t$kind")
}
function Get-BlkDisplayName([string]$blkPath) {
    $name = [IO.Path]::GetFileNameWithoutExtension(($blkPath -replace '/', '\'))
    return (($name -replace '[_\\.]', ' ') -replace '\s+', ' ').Trim()
}
# GROUND: every ground vehicle's main cannon (from ground.tsv)
if (Test-Path -LiteralPath (Join-Path $OutputRoot 'ground.tsv')) {
    foreach ($g in [IO.File]::ReadAllLines((Join-Path $OutputRoot 'ground.tsv'))) {
        $p = $g -split "`t"
        if ($p.Count -lt 7 -or -not $p[6]) { continue }
        Add-UnitWeaponRow $p[0] 'ground' $p[1] $p[6] (Get-BlkDisplayName $p[6]) 'cannon'
    }
}
# NAVAL: every ship's guns (from ship unit files)
$shipsRoot = Join-Path $UnitsRoot 'ships'
if (Test-Path -LiteralPath $shipsRoot) {
    foreach ($sfile in (Get-ChildItem -LiteralPath $shipsRoot -File -Filter '*.blk' | Sort-Object Name)) {
        $sid = [IO.Path]::GetFileNameWithoutExtension($sfile.Name)
        if (-not $unitNames.ContainsKey($sid + '_0') -and -not $unitNames.ContainsKey($sid + '_shop')) { continue }
        $stext = [IO.File]::ReadAllText($sfile.FullName)
        $sdisplay = Get-VehicleDisplayName $sid
        foreach ($w in (Get-NamedBlocks $stext 'Weapon')) {
            $wblk = [regex]::Match($w.Text, '(?m)^\s*blk:t\s*=\s*"([^"]+)"')
            if (-not $wblk.Success) { continue }
            $bpath = $wblk.Groups[1].Value
            if ($bpath -notmatch '(?i)navalmodels_weapons') { continue }
            Add-UnitWeaponRow $sid 'naval' $sdisplay $bpath (Get-BlkDisplayName $bpath) 'cannon'
        }
    }
}
# AIR: fixed guns from flight models
foreach ($afile in (Get-ChildItem -LiteralPath $FlightModelsRoot -File -Filter '*.blk' | Sort-Object Name)) {
    $aid = [IO.Path]::GetFileNameWithoutExtension($afile.Name)
    if (-not $playable.ContainsKey($aid)) { continue }
    $atext = [IO.File]::ReadAllText($afile.FullName)
    $adisplay = $playable[$aid].Display
    foreach ($w in (Get-NamedBlocks $atext 'Weapon')) {
        $wblk = [regex]::Match($w.Text, '(?m)^\s*blk:t\s*=\s*"([^"]+)"')
        if (-not $wblk.Success) { continue }
        $bpath = $wblk.Groups[1].Value
        if ($bpath -notmatch '(?i)^gameData/Weapons/' -or $bpath -match '(?i)groundmodels|navalmodels|equipment|drop_tank') { continue }
        $airDomain = if ($playable.ContainsKey($aid) -and $playable[$aid].Kind -match '(?i)hel') { 'helicopter' } else { 'aircraft' }
        Add-UnitWeaponRow $aid $airDomain $adisplay $bpath (Get-BlkDisplayName $bpath) 'cannon'
    }
}
# AIR: external stores from donor weapons
foreach ($d in $donorRows) {
    $p = $d -split "`t"
    if ($p.Count -lt 6) { continue }
    $bpath = $p[5]
    if (-not $bpath -or $bpath -match '(?i)groundmodels|navalmodels') { continue }
    $label = if ($p.Count -gt 9 -and $p[9]) { $p[9] } else { Get-BlkDisplayName $bpath }
    $airDomain2 = if ($playable.ContainsKey($p[0]) -and $playable[$p[0]].Kind -match '(?i)hel') { 'helicopter' } else { 'aircraft' }
    Add-UnitWeaponRow $p[0] $airDomain2 $p[1] $bpath $label 'ordnance'
}
[IO.File]::WriteAllLines((Join-Path $OutputRoot 'unit_weapons.tsv'), $unitWeaponRows, [Text.UTF8Encoding]::new($false))
}

# Sensor (radar/IRST) catalog - every sensor blk next to the units tree, with the
# in-file display name, first transivers band, role (search vs track, derived from
# fsm mode names) and top-level rangeMax. Powers the radar-swap picker + detail cards.
$sensorRoot = Join-Path (Split-Path $UnitsRoot -Parent) 'sensors'
$sensorRows = New-Object System.Collections.Generic.List[string]
if (Test-Path -LiteralPath $sensorRoot) {
  foreach ($sfile in (Get-ChildItem -LiteralPath $sensorRoot -File -Filter '*.blk' | Sort-Object Name)) {
    try {
      $stext = [IO.File]::ReadAllText($sfile.FullName)
      $sname = [regex]::Match($stext, '(?m)^\s*name\s*:\s*t\s*=\s*"([^"]+)"')
      $sband = [regex]::Match($stext, '(?m)^\s*transivers\s*\{[^}]*?band\s*:\s*i\s*=\s*(-?\d+)')
      # role: fsm mode names are reliable - search/tws/scan => search-class (feeds active/TWS
      # missiles), lock/track/acquisition/illum => track-class (SARH illumination / SACLOS cmds)
      $fsmVals = [regex]::Matches($stext, '(?im)^\s*fsm\s*:\s*t\s*=\s*"([^"]+)"') | ForEach-Object { $_.Groups[1].Value }
      $role = ''
      if ($fsmVals -match '(?i)search|tws|scan|surveillance|acq') { $role = 'search' }
      elseif ($fsmVals -match '(?i)lock|track|illum|designat') { $role = 'track' }
      $srm = [regex]::Match($stext, '(?im)^\s*rangeMax\s*:\s*r\s*=\s*([\d.]+)')
      # detail-card fields: blk type (radar/irst/rwr), capability fsm set (minus parking noise),
      # weaponTargetsMax (missile data-link capacity) and presence of an IRST channel
      $stype = [regex]::Match($stext, '(?im)^\s*type\s*:\s*t\s*=\s*"([^"]+)"')
      $fset = ([regex]::Matches($stext, '(?im)^\s*fsm\s*:\s*t\s*=\s*"([^"]+)"') | ForEach-Object { $_.Groups[1].Value } | Where-Object { $_ -notmatch '(?i)parkAntenna|sleep|slewing' } | Sort-Object -Unique) -join ','
      $swm = [regex]::Match($stext, '(?im)^\s*weaponTargetsMax\s*:\s*i\s*=\s*(\d+)')
      $irst = [regex]::IsMatch($stext, '(?im)^\s*irst\w*\s*\{')
      $srow = $sfile.BaseName + "`t" + $(if ($sname.Success) { $sname.Groups[1].Value } else { $sfile.BaseName }) + "`t" + $(if ($sband.Success) { $sband.Groups[1].Value } else { '' }) + "`t" + $role + "`t" + $(if ($srm.Success) { [int][double]::Parse($srm.Groups[1].Value, [Globalization.CultureInfo]::InvariantCulture) } else { '' }) + "`t" + $(if ($stype.Success) { $stype.Groups[1].Value } else { '' }) + "`t" + $fset + "`t" + $(if ($swm.Success) { $swm.Groups[1].Value } else { '' }) + "`t" + $(if ($irst) { '1' } else { '' })
      $sensorRows.Add($srow)
    } catch { }
  }
}
[IO.File]::WriteAllLines((Join-Path $OutputRoot 'sensors.tsv'), $sensorRows, [Text.UTF8Encoding]::new($false))
Write-Output "Sensors=$($sensorRows.Count)"

Build-VehicleWeaponsJson

Write-Output "[catalog] $(Get-Date -Format HH:mm:ss) done"
Write-Output "Aircraft=$($aircraftRows.Count) Presets=$($presetRows.Count) Slots=$($slotRows.Count) Pylons=$($aircraftSlotRows.Count) DonorMounts=$($donorRows.Count) Weapons=$($weaponCatalogRows.Count) Modifications=$($modificationRows.Count)"
