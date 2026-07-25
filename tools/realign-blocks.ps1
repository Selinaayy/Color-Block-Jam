# Restore original manual positions then re-snap with fixed formula
$scenes = @(
    "C:\Users\selin\Desktop\colorblockjam\Assets\Scenes\Level1.unity",
    "C:\Users\selin\Desktop\colorblockjam\Assets\Scenes\Level2.unity"
)

$originals = @{
    "Block_ShortRect (7)"  = @(-0.4979299, 2.1048553)
    "Block_Square2 (2)"    = @(-0.74782336, 1.1052829)
    "Block_Square2 (3)"    = @(0.7515353, 1.1052824)
    "Block_ShortRect (9)"  = @(0.5016428, 2.1048553)
    "Block_ShortRect (3)"  = @(0.0018564686, 3.1044278)
    "Block_ShortRect (2)"  = @(-0.4979299, 3.1044278)
    "Block_Square2"        = @(-0.74782336, 4.104)
    "Block_ShortRect (6)"  = @(-0.9977163, 2.1048553)
    "Block_ShortRect (1)"  = @(-0.9977163, 3.1044278)
    "Block_Square2 (1)"    = @(0.7515353, 4.1039996)
    "Block_ShortRect (10)" = @(1.01, 2.1047337)
    "Block_ShortRect (4)"  = @(0.5016428, 3.1044278)
    "Block_ShortRect (11)" = @(0.001855962, 1.105282)
    "Block_ShortRect"      = @(0.001855962, 4.103999)
    "Block_ShortRect (8)"  = @(0.0018564686, 2.1048553)
    "Block_ShortRect (5)"  = @(0.9995725, 3.1043062)
}

$CELL = 0.5
$originX = -0.991
$originZ = 0.85000235

function Snap-Axis([double]$position, [double]$origin, [int]$cellCount) {
    $relative = ($position - $origin) / $CELL
    if ($cellCount % 2 -eq 1) {
        return $origin + [Math]::Round($relative) * $CELL
    }
    $startCell = [Math]::Round($relative - $cellCount * 0.5 + 0.5)
    return $origin + ($startCell + $cellCount * 0.5 - 0.5) * $CELL
}

function Snap-Block([double]$x, [double]$z, [string]$type) {
    $cellsX = if ($type -eq 'square2') { 2 } else { 1 }
    $cellsZ = if ($type -eq 'square2') { 2 } else { 2 }
    return @{
        X = Snap-Axis $x $originX $cellsX
        Z = Snap-Axis $z $originZ $cellsZ
    }
}

foreach ($scenePath in $scenes) {
    if (-not (Test-Path $scenePath)) { continue }
    $content = Get-Content $scenePath -Raw

    # detect origin from grid
    $parts = $content -split '--- !u!1001'
    $xs = @(); $zs = @()
    foreach ($part in $parts) {
        if ($part -notmatch 'value: Grid \(') { continue }
        if ($part -match 'propertyPath: m_LocalPosition\.x\r?\n\s+value: ([^\r\n]+)') { $xs += [double]$Matches[1] }
        if ($part -match 'propertyPath: m_LocalPosition\.z\r?\n\s+value: ([^\r\n]+)') { $zs += [double]$Matches[1] }
    }
    if ($xs.Count -gt 0) { $script:originX = ($xs | Measure-Object -Minimum).Minimum }
    if ($zs.Count -gt 0) { $script:originZ = ($zs | Measure-Object -Minimum).Minimum }

    $changed = 0
    for ($i = 1; $i -lt $parts.Count; $i++) {
        if ($parts[$i] -notmatch 'value: (Block_[^\r\n]+)') { continue }
        $name = $Matches[1]
        $type = if ($name -like '*Square2*') { 'square2' } elseif ($name -like '*ShortRect*') { 'shortRect' } else { continue }

        # Use original position if Level1, else current
        $oldX = 0.0; $oldZ = 0.0
        if ($originals.ContainsKey($name) -and $scenePath -like '*Level1*') {
            $oldX = $originals[$name][0]; $oldZ = $originals[$name][1]
        } else {
            if ($parts[$i] -notmatch 'propertyPath: m_LocalPosition\.x\r?\n\s+value: ([^\r\n]+)') { continue }
            $oldX = [double]$Matches[1]
            if ($parts[$i] -notmatch 'propertyPath: m_LocalPosition\.z\r?\n\s+value: ([^\r\n]+)') { continue }
            $oldZ = [double]$Matches[1]
        }

        $snapped = Snap-Block $oldX $oldZ $type
        $parts[$i] = $parts[$i] -replace 'propertyPath: m_LocalPosition\.x\r?\n\s+value: [^\r\n]+', "propertyPath: m_LocalPosition.x`n      value: $($snapped.X)"
        $parts[$i] = $parts[$i] -replace 'propertyPath: m_LocalPosition\.z\r?\n\s+value: [^\r\n]+', "propertyPath: m_LocalPosition.z`n      value: $($snapped.Z)"
        $changed++
        Write-Output "$(Split-Path $scenePath -Leaf) $name : ($oldX, $oldZ) -> ($($snapped.X), $($snapped.Z))"
    }

    ($parts -join '--- !u!1001') | Set-Content $scenePath -NoNewline -Encoding utf8
    Write-Output "$(Split-Path $scenePath -Leaf): $changed blok (origin $originX, $originZ)"
}
