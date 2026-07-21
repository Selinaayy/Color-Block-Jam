param(
    [Parameter(Mandatory = $true)]
    [string]$ScenePath
)

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

function Get-BlockFootprint($name) {
    if ($name -match 'Square2') { return @(2, 2) }
    if ($name -match 'ShortRect') { return @(1, 2) }
    return @(1, 1)
}

function Get-BestCenter($x, $z, $cellsX, $cellsZ, $gridXs, $gridZs) {
    $bestX = $x
    $bestZ = $z
    $bestDist = [double]::MaxValue

    for ($row = 0; $row -le ($gridZs.Count - $cellsZ); $row++) {
        for ($col = 0; $col -le ($gridXs.Count - $cellsX); $col++) {
            if ($cellsX -eq 1) {
                $cx = $gridXs[$col]
            }
            else {
                $cx = [math]::Round(($gridXs[$col] + $gridXs[$col + $cellsX - 1]) / 2, 6)
            }

            if ($cellsZ -eq 1) {
                $cz = $gridZs[$row]
            }
            else {
                $cz = [math]::Round(($gridZs[$row] + $gridZs[$row + $cellsZ - 1]) / 2, 6)
            }

            $dist = (($x - $cx) * ($x - $cx)) + (($z - $cz) * ($z - $cz))
            if ($dist -lt $bestDist) {
                $bestDist = $dist
                $bestX = $cx
                $bestZ = $cz
            }
        }
    }

    return @($bestX, $bestZ)
}

$gridXs = Get-AxisValues $originX $columns $cellSize
$gridZs = Get-AxisValues $originZ $rows $cellSize

$content = Get-Content $ScenePath -Raw
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

        $parts[$i] = Set-PositionValue $parts[$i] 'x' $gridXs[$col]
        $parts[$i] = Set-PositionValue $parts[$i] 'z' $gridZs[$row]
        $parts[$i] = Set-PositionValue $parts[$i] 'y' $gridY
        $gridChanged++
        continue
    }

    if ($parts[$i] -match 'value: (Block_[^\r\n]+)') {
        $name = $Matches[1]
        if ($parts[$i] -notmatch 'propertyPath: m_LocalPosition\.x\r?\n\s+value: ([^\r\n]+)') { continue }
        $oldX = [double]$Matches[1]
        if ($parts[$i] -notmatch 'propertyPath: m_LocalPosition\.z\r?\n\s+value: ([^\r\n]+)') { continue }
        $oldZ = [double]$Matches[1]

        $footprint = Get-BlockFootprint $name
        $pos = Get-BestCenter $oldX $oldZ $footprint[0] $footprint[1] $gridXs $gridZs

        $parts[$i] = Set-PositionValue $parts[$i] 'x' $pos[0]
        $parts[$i] = Set-PositionValue $parts[$i] 'y' $blockY
        $parts[$i] = Set-PositionValue $parts[$i] 'z' $pos[1]
        $blockChanged++
    }
}

($parts -join '--- !u!1001') | Set-Content $ScenePath -NoNewline -Encoding utf8
Write-Output "$(Split-Path $ScenePath -Leaf): grid=$gridChanged blocks=$blockChanged"
Write-Output "Grid X: $($gridXs -join ', ')"
Write-Output "Grid Z: $($gridZs -join ', ')"
