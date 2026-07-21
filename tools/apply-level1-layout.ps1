# Apply exact grid-aligned layout to Level1 and Level2
$scenes = @(
    "C:\Users\selin\Desktop\colorblockjam\Assets\Scenes\Level1.unity",
    "C:\Users\selin\Desktop\colorblockjam\Assets\Scenes\Level2.unity"
)

$x0 = -0.991
$x1 = -0.491
$x2 = 0.009
$x3 = 0.509
$x4 = 1.009
$x01 = -0.741
$x34 = 0.759

$z01 = 1.10
$z23 = 2.10
$z45 = 3.10
$z67 = 4.10
$blockY = 0.067015596

$layout = @{
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

foreach ($scenePath in $scenes) {
    if (-not (Test-Path $scenePath)) { continue }
    $content = Get-Content $scenePath -Raw
    $parts = $content -split '--- !u!1001'
    $changed = 0

    for ($i = 1; $i -lt $parts.Count; $i++) {
        if ($parts[$i] -notmatch 'value: (Block_[^\r\n]+)') { continue }
        $name = $Matches[1]
        if (-not $layout.ContainsKey($name)) { continue }

        $pos = $layout[$name]
        $parts[$i] = $parts[$i] -replace 'propertyPath: m_LocalPosition\.x\r?\n\s+value: [^\r\n]+', "propertyPath: m_LocalPosition.x`n      value: $($pos[0])"
        $parts[$i] = $parts[$i] -replace 'propertyPath: m_LocalPosition\.y\r?\n\s+value: [^\r\n]+', "propertyPath: m_LocalPosition.y`n      value: $blockY"
        $parts[$i] = $parts[$i] -replace 'propertyPath: m_LocalPosition\.z\r?\n\s+value: [^\r\n]+', "propertyPath: m_LocalPosition.z`n      value: $($pos[1])"
        $changed++
    }

    ($parts -join '--- !u!1001') | Set-Content $scenePath -NoNewline -Encoding utf8
    Write-Output "$(Split-Path $scenePath -Leaf): $changed blok"
}
