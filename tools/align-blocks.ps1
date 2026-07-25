$scenes = @(
    "C:\Users\selin\Desktop\colorblockjam\Assets\Scenes\Level1.unity",
    "C:\Users\selin\Desktop\colorblockjam\Assets\Scenes\Level2.unity"
)

$CELL = 0.5
$originX = -0.991
$originZ = 0.85000235

function Get-CellCount([double]$worldSize) {
    return [Math]::Max(1, [int][Math]::Round($worldSize / $CELL))
}

function Snap-Axis([double]$position, [double]$origin, [int]$cellCount) {
    $relative = ($position - $origin) / $CELL
    if ($cellCount % 2 -eq 1) {
        return $origin + [Math]::Round($relative) * $CELL
    }
    $startCell = [Math]::Round($relative - $cellCount * 0.5 + 0.5)
    return $origin + ($startCell + $cellCount * 0.5 - 0.5) * $CELL
}

function Snap-Block([double]$x, [double]$z, [string]$type) {
    $cellsX = 1
    $cellsZ = 2
    if ($type -eq "square2") {
        $cellsX = 2
        $cellsZ = 2
    }
    return @{
        X = Snap-Axis $x $originX $cellsX
        Z = Snap-Axis $z $originZ $cellsZ
    }
}

foreach ($scenePath in $scenes) {
    if (-not (Test-Path $scenePath)) { continue }

    $content = Get-Content $scenePath -Raw
    $parts = $content -split '--- !u!1001'
    $changed = 0

    $xs = @()
    $zs = @()
    foreach ($part in $parts) {
        if ($part -notmatch 'value: Grid \(') { continue }
        if ($part -match 'propertyPath: m_LocalPosition\.x\r?\n\s+value: ([^\r\n]+)') { $xs += [double]$Matches[1] }
        if ($part -match 'propertyPath: m_LocalPosition\.z\r?\n\s+value: ([^\r\n]+)') { $zs += [double]$Matches[1] }
    }
    if ($xs.Count -gt 0) { $originX = ($xs | Measure-Object -Minimum).Minimum }
    if ($zs.Count -gt 0) { $originZ = ($zs | Measure-Object -Minimum).Minimum }

    for ($i = 1; $i -lt $parts.Count; $i++) {
        if ($parts[$i] -notmatch 'value: (Block_[^\r\n]+)') { continue }
        $name = $Matches[1]
        $type = $null
        if ($name -like '*Square2*') { $type = 'square2' }
        elseif ($name -like '*ShortRect*') { $type = 'shortRect' }
        else { continue }

        if ($parts[$i] -notmatch 'propertyPath: m_LocalPosition\.x\r?\n\s+value: ([^\r\n]+)') { continue }
        $oldX = [double]$Matches[1]
        if ($parts[$i] -notmatch 'propertyPath: m_LocalPosition\.z\r?\n\s+value: ([^\r\n]+)') { continue }
        $oldZ = [double]$Matches[1]

        $snapped = Snap-Block $oldX $oldZ $type
        if ([Math]::Abs($oldX - $snapped.X) -lt 0.0001 -and [Math]::Abs($oldZ - $snapped.Z) -lt 0.0001) { continue }

        $parts[$i] = $parts[$i] -replace 'propertyPath: m_LocalPosition\.x\r?\n\s+value: [^\r\n]+', "propertyPath: m_LocalPosition.x`n      value: $($snapped.X)"
        $parts[$i] = $parts[$i] -replace 'propertyPath: m_LocalPosition\.z\r?\n\s+value: [^\r\n]+', "propertyPath: m_LocalPosition.z`n      value: $($snapped.Z)"
        $changed++
        Write-Output "$(Split-Path $scenePath -Leaf) $name : ($oldX, $oldZ) -> ($($snapped.X), $($snapped.Z))"
    }

    if ($changed -gt 0) {
        ($parts -join '--- !u!1001') | Set-Content $scenePath -NoNewline -Encoding utf8
    }

    Write-Output "$(Split-Path $scenePath -Leaf): $changed blok guncellendi"
}
