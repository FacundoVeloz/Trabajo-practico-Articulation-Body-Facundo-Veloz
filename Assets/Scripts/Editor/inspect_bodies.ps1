$content = [System.IO.File]::ReadAllText("Assets/Scenes/SampleScene.unity")
$docs = $content -split "(?m)^--- !"

$objects = @{}
$transforms = @{}
$colliders = @{}
$articulations = @{}

foreach ($doc in $docs) {
    if ($doc -match "^u!(\d+) &(\d+)") {
        $typeId = $matches[1]
        $fileId = $matches[2]
        if ($typeId -eq "1") {
            $name = if ($doc -match "m_Name:\s*(.*)") { $matches[1].Trim() } else { "Unknown" }
            $objects[$fileId] = $name
        } elseif ($typeId -eq "4") {
            $goId = if ($doc -match "m_GameObject:\s*\{fileID:\s*(\d+)\}") { $matches[1] } else { "" }
            $pos = if ($doc -match "m_LocalPosition:\s*\{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)\}") { "$($matches[1]), $($matches[2]), $($matches[3])" } else { "" }
            $transforms[$fileId] = @{ GameObjectId = $goId; Pos = $pos }
        } elseif ($typeId -match "^(64|65|135|136)$") {
            $goId = if ($doc -match "m_GameObject:\s*\{fileID:\s*(\d+)\}") { $matches[1] } else { "" }
            $cType = switch ($typeId) { "64" {"MeshCol"} "65" {"BoxCol"} "135" {"SphereCol"} "136" {"CapsuleCol"} }
            $center = if ($doc -match "m_Center:\s*\{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)\}") { "$($matches[1]), $($matches[2]), $($matches[3])" } else { "0,0,0" }
            $size = if ($doc -match "m_Size:\s*\{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^}]+)\}") { "$($matches[1]), $($matches[2]), $($matches[3])" } else { "" }
            $radius = if ($doc -match "m_Radius:\s*([\d\.\-]+)") { $matches[1] } else { "" }
            $height = if ($doc -match "m_Height:\s*([\d\.\-]+)") { $matches[1] } else { "" }
            $isTrig = if ($doc -match "m_IsTrigger:\s*(\d+)") { $matches[1] } else { "0" }
            $colliders[$fileId] = @{ GameObjectId = $goId; Type = $cType; Center = $center; Size = $size; Radius = $radius; Height = $height; IsTrigger = $isTrig }
        } elseif ($typeId -eq "171741748") {
            $goId = if ($doc -match "m_GameObject:\s*\{fileID:\s*(\d+)\}") { $matches[1] } else { "" }
            $articulations[$fileId] = @{ GameObjectId = $goId; Raw = $doc }
        }
    }
}

Write-Output "=== ARTICULATION BODIES & COLLIDERS ==="
foreach ($abId in $articulations.Keys) {
    $ab = $articulations[$abId]
    $goName = $objects[$ab.GameObjectId]
    Write-Output "ArticulationBody on '$goName' (GO ID: $($ab.GameObjectId)):"
    
    # Find transform
    foreach ($tId in $transforms.Keys) {
        if ($transforms[$tId].GameObjectId -eq $ab.GameObjectId) {
            Write-Output "  LocalPos: $($transforms[$tId].Pos)"
        }
    }
    
    # Find colliders
    foreach ($cId in $colliders.Keys) {
        if ($colliders[$cId].GameObjectId -eq $ab.GameObjectId) {
            $c = $colliders[$cId]
            Write-Output "  Collider: $($c.Type), Center: ($($c.Center)), Size: ($($c.Size)), Radius: $($c.Radius), Height: $($c.Height), Trigger: $($c.IsTrigger)"
        }
    }
}
