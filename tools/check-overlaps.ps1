$scene = "C:\Users\selin\Desktop\colorblockjam\Assets\Scenes\Level1.unity"
$content = Get-Content $scene -Raw
$originX = -0.991
$originZ = 0.85000235
$CELL = 0.5

function Get-Cells($x, $z, $cellsX, $cellsZ) {
    $relX = ($x - $originX) / $CELL
    $relZ = ($z - $originZ) / $CELL
    if ($cellsX % 2 -eq 1) { $cx = [Math]::Round($relX) } else { $cx = [Math]::Round($relX - $cellsX * 0.5 + 0.5) }
    if ($cellsZ % 2 -eq 1) { $cz = [Math]::Round($relZ) } else { $cz = [Math]::Round($relZ - $cellsZ * 0.5 + 0.5) }
    $occupied = @()
    $startX = $cx - [Math]::Floor($cellsX / 2)
    $startZ = $cz - [Math]::Floor($cellsZ / 2)
    for ($i = 0; $i -lt $cellsX; $i++) {
        for ($j = 0; $j -lt $cellsZ; $j++) {
            $occupied += "$($startX + $i),$($startZ + $j)"
        }
    }
    return ,$occupied
}

$parts = $content -split '--- !u!1001'
$allCells = @{}
foreach ($part in $parts) {
    if ($part -notmatch 'value: (Block_[^\r\n]+)') { continue }
    $name = $Matches[1]
    if ($name -like '*Square2*') { $cx=2; $cz=2 } elseif ($name -like '*ShortRect*') { $cx=1; $cz=2 } else { continue }
    if ($part -notmatch 'propertyPath: m_LocalPosition\.x\r?\n\s+value: ([^\r\n]+)') { continue }
    $x = [double]$Matches[1]
    if ($part -notmatch 'propertyPath: m_LocalPosition\.z\r?\n\s+value: ([^\r\n]+)') { continue }
    $z = [double]$Matches[1]
    $cells = Get-Cells $x $z $cx $cz
    Write-Output "$name at ($x, $z) -> cells: $($cells -join ' | ')"
    foreach ($c in $cells) {
        if ($allCells.ContainsKey($c)) {
            Write-Output "  OVERLAP at cell $c with $($allCells[$c])"
        } else {
            $allCells[$c] = $name
        }
    }
}
