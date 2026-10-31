$content = [System.IO.File]::ReadAllText("Assets/Scenes/SampleScene.unity")
$matches = [regex]::Matches($content, "(?s)--- !u!171741748 &(\d+).*?m_GameObject:\s*\{fileID:\s*(\d+)\}.*?m_MatchAnchors:\s*(\d+)")
foreach ($m in $matches) {
    Write-Output "AB $($m.Groups[1].Value) (GO $($m.Groups[2].Value)): MatchAnchors = $($m.Groups[3].Value)"
}
