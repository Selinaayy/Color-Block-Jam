# Apply valid non-overlapping layout to Level2 (same grid structure as Level1)
$scene = "C:\Users\selin\Desktop\colorblockjam\Assets\Scenes\Level2.unity"
$content = Get-Content $scene -Raw

$layout = @{
    "Block_Square2 (2)"    = @(-0.741, 1.10000235)
    "Block_ShortRect (11)" = @(0.009, 1.10000235)
    "Block_Square2 (3)"    = @(0.759, 1.10000235)
    "Block_ShortRect (6)"  = @(-0.991, 2.10000235)
    "Block_ShortRect (7)"  = @(-0.491, 2.10000235)
    "Block_ShortRect (8)"  = @(0.009, 2.10000235)
    "Block_ShortRect (9)"  = @(0.509, 2.10000235)
    "Block_ShortRect (10)" = @(1.009, 2.10000235)
    "Block_ShortRect (1)"  = @(-0.991, 3.10000235)
    "Block_ShortRect (2)"  = @(-0.491, 3.10000235)
    "Block_ShortRect (3)"  = @(0.009, 3.10000235)
    "Block_ShortRect (4)"  = @(0.509, 3.10000235)
    "Block_ShortRect (5)"  = @(1.009, 3.10000235)
    "Block_Square2"        = @(-0.741, 4.10000235)
    "Block_ShortRect"      = @(0.009, 4.10000235)
    "Block_Square2 (1)"    = @(0.759, 4.10000235)
}

$parts = $content -split '--- !u!1001'
$changed = 0

for ($i = 1; $i -lt $parts.Count; $i++) {
    if ($parts[$i] -notmatch 'value: (Block_[^\r\n]+)') { continue }
    $name = $Matches[1]
    if (-not $layout.ContainsKey($name)) { continue }

    $pos = $layout[$name]
    $parts[$i] = $parts[$i] -replace 'propertyPath: m_LocalPosition\.x\r?\n\s+value: [^\r\n]+', "propertyPath: m_LocalPosition.x`n      value: $($pos[0])"
    $parts[$i] = $parts[$i] -replace 'propertyPath: m_LocalPosition\.z\r?\n\s+value: [^\r\n]+', "propertyPath: m_LocalPosition.z`n      value: $($pos[1])"
    $changed++
}

($parts -join '--- !u!1001') | Set-Content $scene -NoNewline -Encoding utf8
Write-Output "Level2: $changed blok yerlestirildi"
