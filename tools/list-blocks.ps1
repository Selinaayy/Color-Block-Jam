$scene = "C:\Users\selin\Desktop\colorblockjam\Assets\Scenes\Level1.unity"
$content = Get-Content $scene -Raw
$parts = $content -split '--- !u!1001'
foreach ($part in $parts) {
    if ($part -notmatch 'value: (Block_[^\r\n]+)') { continue }
    $name = $Matches[1]
    if ($name -notlike 'Block_*') { continue }
    $x=$null;$z=$null;$renk=$null
    if ($part -match 'propertyPath: m_LocalPosition\.x\r?\n\s+value: ([^\r\n]+)') { $x = $Matches[1] }
    if ($part -match 'propertyPath: m_LocalPosition\.z\r?\n\s+value: ([^\r\n]+)') { $z = $Matches[1] }
    if ($part -match 'propertyPath: renk\r?\n\s+value: (\d+)') { $renk = $Matches[1] }
    $type = if ($name -like '*Square2*') { 'SQ2' } else { 'SR ' }
    $color = switch($renk) { '0' {'R'} '1' {'B'} '2' {'Y'} default {'?'} }
    Write-Output "$type $color $name x=$x z=$z"
}

# Also get MonoBehaviour renk for added components
$mbParts = $content -split '--- !u!114'
foreach ($mb in $mbParts) {
    if ($mb -notmatch 'guid: ebbb7823e5e0f7d4fa606fefb6373506') { continue }
    if ($mb -notmatch 'm_GameObject: \{fileID: (\d+)\}') { continue }
    $go = $Matches[1]
    $renk = if ($mb -match 'renk: (\d+)') { $Matches[1] } else { '?' }
}
