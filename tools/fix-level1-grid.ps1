# Snap Level1 GameArea grid tiles and blocks to exact 0.5 cell spacing.
$scenePath = "C:\Users\selin\Desktop\colorblockjam\Assets\Scenes\Level1.unity"

$cellSize = 0.5
$originX = -0.991
$originZ = 0.85
$gridY = -0.05232147
$blockY = 0.067015596
$columns = 5
$rows = 8

function Get-AxisValues($origin, $count, $step) {
    $values = @()
    for ($i = 0; $i -lt $count; $i++) {
        $values += [math]::Round($origin + ($i * $step), 6)
    }
    return $values
}

$gridXs = Get-AxisValues $originX $columns $cellSize
$gridZs = Get-AxisValues $originZ $rows $cellSize

function Get-NearestIndex($value, $values) {
    $best = 0
    $bestDist = [double]::MaxValue
    for ($i = 0; $i -lt $values.Count; $i++) {
        $dist = [math]::Abs($value - $values[$i])
        if ($dist -lt $bestDist) {
            $bestDist = $dist
            $best = $i
        }
    }
    return $best
}

function Set-PositionValue($part, $axis, $value) {
    $pattern = "propertyPath: m_LocalPosition\.$axis\r?\n\s+value: [^\r\n]+"
    $replacement = "propertyPath: m_LocalPosition.$axis`n      value: $value"
    return ($part -replace $pattern, $replacement)
}

$x0 = $gridXs[0]; $x1 = $gridXs[1]; $x2 = $gridXs[2]; $x3 = $gridXs[3]; $x4 = $gridXs[4]
$x01 = [math]::Round(($gridXs[0] + $gridXs[1]) / 2, 6)
$x34 = [math]::Round(($gridXs[3] + $gridXs[4]) / 2, 6)
$z01 = [math]::Round(($gridZs[0] + $gridZs[1]) / 2, 6)
$z23 = [math]::Round(($gridZs[2] + $gridZs[3]) / 2, 6)
$z45 = [math]::Round(($gridZs[4] + $gridZs[5]) / 2, 6)
$z67 = [math]::Round(($gridZs[6] + $gridZs[7]) / 2, 6)

$blockLayout = @{
    "Block_Square2 (2)"    = @($x01, $z01)
    "Block_ShortRect (11)" = @($x2, $z01)
    "Block_Square2 (3)"    = @($x34, $z01)
    "Block_ShortRect (6)"  = @($x0, $z23)
    "Block_ShortRect (7)"  = @($x1, $z23)
    "Block_ShortRect (8)"  = @($x2, $z23)
    "Block_ShortRect (9)"  = @($x3, $z23)
    "Block_ShortRect (10)" = @($x4, $z23)
    "Block_ShortRect (1)"  = @($x0, $z45)
    "Block_ShortRect (2)"  = @($x1, $z45)
    "Block_ShortRect (3)"  = @($x2, $z45)
    "Block_ShortRect (4)"  = @($x3, $z45)
    "Block_ShortRect (5)"  = @($x4, $z45)
    "Block_Square2"        = @($x01, $z67)
    "Block_ShortRect"      = @($x2, $z67)
    "Block_Square2 (1)"    = @($x34, $z67)
}

$content = Get-Content $scenePath -Raw
$parts = $content -split '--- !u!1001'
$gridChanged = 0
$blockChanged = 0

for ($i = 1; $i -lt $parts.Count; $i++) {
    if ($parts[$i] -match 'value: Grid') {
        if ($parts[$i] -notmatch 'propertyPath: m_LocalPosition\.x\r?\n\s+value: ([^\r\n]+)') { continue }
        $oldX = [double]$Matches[1]
        if ($parts[$i] -notmatch 'propertyPath: m_LocalPosition\.z\r?\n\s+value: ([^\r\n]+)') { continue }
        $oldZ = [double]$Matches[1]

        $col = Get-NearestIndex $oldX $gridXs
        $row = Get-NearestIndex $oldZ $gridZs
        $newX = $gridXs[$col]
        $newZ = $gridZs[$row]

        $parts[$i] = Set-PositionValue $parts[$i] 'x' $newX
        $parts[$i] = Set-PositionValue $parts[$i] 'z' $newZ
        $parts[$i] = Set-PositionValue $parts[$i] 'y' $gridY
        $gridChanged++
        continue
    }

    if ($parts[$i] -match 'value: (Block_[^\r\n]+)') {
        $name = $Matches[1]
        if (-not $blockLayout.ContainsKey($name)) { continue }

        $pos = $blockLayout[$name]
        $parts[$i] = Set-PositionValue $parts[$i] 'x' $pos[0]
        $parts[$i] = Set-PositionValue $parts[$i] 'y' $blockY
        $parts[$i] = Set-PositionValue $parts[$i] 'z' $pos[1]
        $blockChanged++
    }
}

($parts -join '--- !u!1001') | Set-Content $scenePath -NoNewline -Encoding utf8
Write-Output "Grid tiles snapped: $gridChanged"
Write-Output "Blocks aligned: $blockChanged"
Write-Output "Grid X: $($gridXs -join ', ')"
Write-Output "Grid Z: $($gridZs -join ', ')"
