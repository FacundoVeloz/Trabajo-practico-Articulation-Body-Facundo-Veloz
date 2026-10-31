$content = [System.IO.File]::ReadAllText("Assets/Scenes/SampleScene.unity")
$docs = $content -split "(?m)^--- !"

$objects = @{}
$articulations = @{}

foreach ($doc in $docs) {
    if ($doc -match "^u!(\d+) &(\d+)") {
        $typeId = $matches[1]
        $fileId = $matches[2]
        if ($typeId -eq "1") {
            $name = if ($doc -match "m_Name:\s*(.*)") { $matches[1].Trim() } else { "Unknown" }
            $objects[$fileId] = $name
        } elseif ($typeId -eq "171741748") {
            $goId = if ($doc -match "m_GameObject:\s*\{fileID:\s*(\d+)\}") { $matches[1] } else { "" }
            $articulations[$fileId] = @{ GameObjectId = $goId; Raw = $doc }
        }
    }
}

foreach ($abId in $articulations.Keys) {
    $ab = $articulations[$abId]
    $name = $objects[$ab.GameObjectId]
    $anchor = if ($ab.Raw -match "m_AnchorPosition:\s*\{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)\}") { "$($matches[1]), $($matches[2]), $($matches[3])" } else { "" }
    $pAnchor = if ($ab.Raw -match "m_ParentAnchorPosition:\s*\{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)\}") { "$($matches[1]), $($matches[2]), $($matches[3])" } else { "" }
    $rot = if ($ab.Raw -match "m_AnchorRotation:\s*\{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^,]+),\s*w:\s*([^}]+)\}") { "$($matches[1]), $($matches[2]), $($matches[3]), $($matches[4])" } else { "" }
    $pRot = if ($ab.Raw -match "m_ParentAnchorRotation:\s*\{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^,]+),\s*w:\s*([^}]+)\}") { "$($matches[1]), $($matches[2]), $($matches[3]), $($matches[4])" } else { "" }
    Write-Output "AB on '$name': Anchor=($anchor) ParentAnchor=($pAnchor) Rot=($rot) PRot=($pRot)"
}
