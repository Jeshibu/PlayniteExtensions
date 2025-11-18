[CmdletBinding()]
param (
    [Parameter()]
    [string]$tag
)

#Depends on powershell-yaml being installed!

function Get-ReleaseData {
    param(
        [Parameter(Mandatory = $true, HelpMessage = "The tag name of this release")]
        [string]$tagName
    )

    $newRelease = $true
    $releaseTag = $tagName

    if ($tagName -match '^(?<Date>[0-9]{4}(-[0-9]{2}){2})(-addto-(?<AddToTag>.+))?$') {
        $all = $true
        $tagName = $Matches.Date
        $projectNames = Get-ProjectNames "all"
    }
    elseif ($tagName -match '^(?<Name>[a-z]+)(?<Version>([0-9]+\.){1,3}[0-9]+)(-addto-(?<AddToTag>.+))?$') {
        $all = $false
        $projectNames = Get-ProjectNames $Matches.Name
    }
    else {
        throw "No name + version match found for $t"
    }

    if ($null -ne $Matches.AddToTag) {
        $newRelease = $false
        $releaseTag = $Matches.AddToTag
    }

    $manifests = @($projectNames | % { $_ | Get-ManifestData })

    if ($all) {
        $manifests = $manifests | Where-Object { $_.ReleaseDate -eq $tagName }
        $releaseTitle = $tagName
    }
    else {
        foreach ($m in $manifests) {
            if ($m.Version -ne $Matches.Version) {
                throw "Version mismatch: tag=$($Matches.Version) yaml=$($m.Version)"
            }
        }
        $releaseTitle = ($manifests | % { "$($_.DisplayName) $($_.Version)" }) -join " + "
    }

    $releaseDescription = Get-ReleaseDescription $manifests #side effect: writes to ./publish/release_notes.txt
    $filteredProjectNames = @($manifests | % { $_.Name })
    $testProjectNames = Get-TestProjectNames $filteredProjectNames

    $output = @(
        "newrelease=$newRelease"
        "releasetag=$releaseTag"
        "releasetitle=$releaseTitle"
        "projects=$($manifests | ConvertTo-Json -Compress)"
        "testprojects=$($testProjectNames | ConvertTo-Json -Compress)"
    )
    foreach ($o in $output) {
        Write-Host $o
        Write-Output $o
        Write-Output $o >> $env:GITHUB_OUTPUT
    }
}

function Get-ProjectNames {
    param (
        [string]$tagProjectName
    )
    $projectNames = @{
        "barnite"               = @("Barnite")
        "bigfish"               = @("BigFishLibrary", "BigFishMetadata")
        "bigfishlibrary"        = @("BigFishLibrary")
        "bigfishmetadata"       = @("BigFishMetadata")
        "ea"                    = @("EaLibrary")
        "extraemulatorprofiles" = @("ExtraEmulatorProfiles")
        "filtersearch"          = @("FilterSearch")
        "gamersgate"            = @("GamersGateLibrary")
        "gamessizecalculator"   = @("GamesSizeCalculator")
        "giantbomb"             = @("GiantBombMetadata")
        "gog"                   = @("GOGMetadata")
        "ign"                   = @("IgnMetadata")
        "itchiobundletagger"    = @("itchIoBundleTagger")
        "launchbox"             = @("LaunchBoxMetadata")
        "legacygames"           = @("LegacyGamesLibrary")
        "mobygames"             = @("MobyGamesMetadata")
        "mutualgames"           = @("MutualGames")
        "opencritic"            = @("OpenCriticMetadata")
        "pathreplacer"          = @("PathReplacer")
        "pcgw"                  = @("PCGamingWikiMetadata")
        "rawg"                  = @("RawgLibrary", "RawgMetadata")
        "steamtagsimporter"     = @("SteamTagsImporter")
        "tvtropes"              = @("TvTropesMetadata")
        "viveport"              = @("ViveportLibrary")
        "xbox"                  = @("XboxMetadata")
    }

    if ($tagProjectName -eq "all") {
        $o = @()
        foreach ($key in $projectNames.Keys) {
            $o += $projectNames[$key]
        }
        return $o | Sort-Object | Get-Unique
    }
    else {
        return $projectNames[$tagProjectName]
    }
}

function Get-TestProjectNames {
    param (
        [Array]$projects
    )

    $testProjects = @("PlayniteExtensions.Common.Tests")
        
    foreach ($p in $projects) {
        if (Test-Path ".\source\$p.Tests" -PathType Container) {
            Write-Host "Found test project for $p in .\source\$p.Tests"
            $testProjects += "$p.Tests"
        }
        else {
            Write-Host "No test project found at .\source\$p.Tests"
        }
    }
    return $testProjects
}

function Get-ManifestData {
    param (
        [Parameter(ValueFromPipeline = $true)]
        [string]$projectName
    )
    $extensionYamlData = Get-Content ".\source\$projectName\extension.yaml" | ConvertFrom-Yaml
    $manifestFiles = Get-ChildItem .\manifests\* -Include "*.yml", "*.yaml"
    foreach ($manifestFile in $manifestFiles) {
        $manifestContent = $manifestFile | Get-Content | ConvertFrom-Yaml
        if ($extensionYamlData.Id -ne $manifestContent.AddonId) { continue; }

        $latestRelease = $manifestContent.Packages[-1]
        if ($latestRelease.Version -ne $extensionYamlData.Version) {
            throw "Version mismatch: $projectName\extension.yaml: $($extensionYamlData.Version), $($manifestFile.Name): $($latestRelease.Version)"
        }
        return @{ Name = $projectName; DisplayName = $extensionYamlData.Name; Version = $latestRelease.Version; ReleaseDate = $latestRelease.ReleaseDate; Changelog = $latestRelease.Changelog | ForEach-Object{$_.Replace("'", "''")} }
    }
    throw "Could not find manifest for addon ID $($manifestContent.Id)"
}

function Get-ReleaseDescription {
    param (
        [Array]$manifests
    )
    $description = ""
    foreach ($m in @($manifests)) {
        if ($manifests.Length -gt 1) {
            $description += "## $($m.DisplayName) $($m.Version)`r`n"
        }
        foreach ($c in $m.Changelog) {
            $description += "- $c`r`n"
        }
        $description += "`r`n`r`n"
    }
    $description | Set-Content -Path "./publish/release_notes.txt"
    return $description
}

return Get-ReleaseData $tag