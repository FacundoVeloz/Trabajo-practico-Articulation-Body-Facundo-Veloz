$content = [System.IO.File]::ReadAllText("Assets/Scenes/SampleScene.unity")
$lines = $content -split "`r?`n"
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match "m_Name:\s*BrazoMecanico") {
        for ($j = [Math]::Max(0, $i - 10); $j -le [Math]::Min($lines.Length - 1, $i + 35); $j++) {
            Write-Output "[$j] $($lines[$j])"
        }
    }
}
