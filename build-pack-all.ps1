param (
    [Parameter(Mandatory=$true)][string]$tag,
    [Parameter()][string]$PlaynitePath = "C:\Playnite",
    [Parameter()][string]$MSBuildPath = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
)

$ToolboxPath = "$PlaynitePath\Toolbox.exe"

$releaseData = (./publish/Get-ReleaseData.ps1 $tag) -split '`n'
$projects = ConvertFrom-Json ($releaseData[3] -split '=',2)[1]

xcopy "$PlaynitePath\Emulation\*.yaml" ".\source\ExtraEmulatorProfiles\EmulationFiles\Original" /Y /I
xcopy "$PlaynitePath\Emulation\Emulators" ".\source\ExtraEmulatorProfiles\EmulationFiles\Original\Emulators" /Y /I /E

$bulkImportProjects = @('GiantBombMetadata', 'LaunchBoxMetadata', 'MobyGamesMetadata', 'PCGamingWikiMetadata', 'SteamTagsImporter', 'TvTropesMetadata', 'WikipediaCategoryImport')
foreach ($proj in $bulkImportProjects){
    xcopy ".\source\PlayniteExtensions.Metadata.Common\GamePropertyImportView.xaml*" ".\source\$proj\Common\Metadata" /Y /I
}

& "$MSBuildPath" ".\source\PlayniteExtensions.slnx" -p:Configuration=Release -restore -clp:ErrorsOnly

mkdir "PackingOutput" -Force
mkdir "Release-$tag" -Force

$playniteDlls = Get-ChildItem $PlaynitePath -Filter *.dll -Name

foreach ($p in $projects) {
    Write-Host "Packing project $p"
    $ver = $p.Version -replace '\.', '_'
    $pluginBuildOutputDir = ".\source\$($p.Name)\bin\Release\net462"

    Remove-Item "$pluginBuildOutputDir\*" -Include $playniteDlls

    & "$PlaynitePath\Toolbox.exe" pack $pluginBuildOutputDir ".\PackingOutput"
    Move-Item -Path ".\PackingOutput\*.pext" -Destination ".\Release-$tag\$($p.Name)_${ver}.pext"
}

Move-Item -Path ".\publish\release_notes.txt" -Destination ".\Release-$tag\" -Force
