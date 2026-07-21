param([string]$ScenePath)
$content = Get-Content $ScenePath -Raw
$parts = $content -split '--- !u!1001'
$grids = @(); $blocks = @()
for ($i = 1; $i -lt $parts.Count; $i++) {
  if ($parts[$i] -match 'value: (Grid[^\r\n]*)') {
    $name = $Matches[1]
    if ($parts[$i] -match 'propertyPath: m_LocalPosition\.x\r?\n\s+value: ([^\r\n]+)') { $x = [double]$Matches[1] }
    if ($parts[$i] -match 'propertyPath: m_LocalPosition\.z\r?\n\s+value: ([^\r\n]+)') { $z = [double]$Matches[1] }
    $grids += [pscustomobject]@{Name=$name; X=$x; Z=$z}
  }
  if ($parts[$i] -match 'value: (Block_[^\r\n]+)') {
    $name = $Matches[1]
    if ($parts[$i] -match 'propertyPath: m_LocalPosition\.x\r?\n\s+value: ([^\r\n]+)') { $x = [double]$Matches[1] }
    if ($parts[$i] -match 'propertyPath: m_LocalPosition\.z\r?\n\s+value: ([^\r\n]+)') { $z = [double]$Matches[1] }
    $blocks += [pscustomobject]@{Name=$name; X=$x; Z=$z}
  }
}
Write-Output "=== GRIDS ($($grids.Count)) ==="
$xs = ($grids.X | Sort-Object -Unique)
$zs = ($grids.Z | Sort-Object -Unique)
Write-Output "Unique X ($($xs.Count)): $($xs -join ', ')"
Write-Output "Unique Z ($($zs.Count)): $($zs -join ', ')"
Write-Output "=== BLOCKS ($($blocks.Count)) ==="
$blocks | Sort-Object Z, X | Format-Table -AutoSize
