$scene = "C:\Users\selin\Desktop\colorblockjam\Assets\Scenes\Level1.unity"
$content = Get-Content $scene -Raw
$parts = $content -split '--- !u!1001'
$xs = @{}; $zs = @{}
foreach ($part in $parts) {
    if ($part -notmatch 'value: Grid \(') { continue }
    if ($part -match 'propertyPath: m_LocalPosition\.x\r?\n\s+value: ([^\r\n]+)') { $x = [double]$Matches[1]; $xs[$x] = $true }
    if ($part -match 'propertyPath: m_LocalPosition\.z\r?\n\s+value: ([^\r\n]+)') { $z = [double]$Matches[1]; $zs[$z] = $true }
}
Write-Output "Grid X ($($xs.Count)):"
($xs.Keys | Sort-Object) | ForEach-Object { Write-Output "  $_" }
Write-Output "Grid Z ($($zs.Count)):"
($zs.Keys | Sort-Object) | ForEach-Object { Write-Output "  $_" }

$gx = ($xs.Keys | Sort-Object)
$gz = ($zs.Keys | Sort-Object)
Write-Output "`nPair centers Z:"
for ($i = 0; $i -lt $gz.Count - 1; $i += 2) {
    $c = ($gz[$i] + $gz[$i+1]) / 2
    Write-Output "  rows $i-$($i+1): $c"
}
Write-Output "Pair centers X:"
for ($i = 0; $i -lt $gx.Count - 1; $i += 2) {
    $c = ($gx[$i] + $gx[$i+1]) / 2
    Write-Output "  cols $i-$($i+1): $c"
}
