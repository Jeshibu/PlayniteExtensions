param(
    $ImportArgs
)

if (-not [System.IO.Directory]::Exists($ImportArgs.ScanDirectory))
{
    return
}

$games = Get-ChildItem -LiteralPath $ImportArgs.ScanDirectory -Recurse -Filter "eboot.bin" -File

foreach ($game in $games)
{
    $anyFunc = [Func[string,bool]]{ param($a) $a.Equals($game.FullName, 'OrdinalIgnoreCase') }
    if ([System.Linq.Enumerable]::Any($ImportArgs.ImportedFiles, $anyFunc))
    {
        continue
    }

    $scannedGame = New-Object "Playnite.Emulators.ScriptScannedGame"
    $scannedGame.Path = $game.FullName
    $scannedGame.Name = $game.Directory.Name
    
    # Suche nach param.json (sce_sys bevorzugt, sonst überall)
    $paramJsonPath = Join-Path $game.DirectoryName "sce_sys\param.json"
    if (-not (Test-Path -LiteralPath $paramJsonPath -PathType Leaf)) {
        $found = Get-ChildItem -Path $game.DirectoryName -Filter "param.json" -Recurse | Select-Object -First 1
        if ($found) { $paramJsonPath = $found.FullName }
    }

    if (Test-Path -LiteralPath $paramJsonPath -PathType Leaf)
    {
        try
        {
            $data = Get-Content -LiteralPath $paramJsonPath -Raw | ConvertFrom-Json
            
            # Vollständige Fallback-Kette
            $loc = $data.localizedParameters
            $title = $loc.'en-US'.titleName
            if (-not $title) { $title = $loc.'en-GB'.titleName }
            if (-not $title) { $title = $loc.'de-DE'.titleName }
            if (-not $title) { $title = $loc.'es-ES'.titleName }
            if (-not $title) { $title = $loc.'fr-FR'.titleName }
            if (-not $title) { $title = $loc.'ja-JP'.titleName }
            
            if ($title) {
                # Failsafe: Sonderzeichen entfernen, Steuerzeichen raus, Doppel-Leerzeichen glätten
                $cleanTitle = [regex]::Replace($title, '[<>:"/\\|?*]', '')
                $cleanTitle = [regex]::Replace($cleanTitle, '[\x00-\x1f\x7f]', '')
                $cleanTitle = [regex]::Replace($cleanTitle, '\s+', ' ').Trim()
                
                $scannedGame.Name = $cleanTitle
            }
            
            if ($data.titleId) {
                $scannedGame.Serial = $data.titleId
            }
        }
        catch { }
    }
    
    $scannedGame
}