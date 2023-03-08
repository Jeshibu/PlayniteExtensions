[CmdletBinding()]
param (
    [Parameter()]
    [string]$tag
)
Write-Host "Installing powershell-yaml"
Set-ExecutionPolicy -ExecutionPolicy Unrestricted
Install-Module powershell-yaml -Confirm -Verbose
Import-Module powershell-yaml
Write-Host "Installed powershell-yaml"

function Get-ReleaseData {
    param (
        [Parameter(Mandatory = $true, HelpMessage = "The tag name of this release")]
        [string]$tagName
    )
    $projects = Get-Projects $tagName
    $projectNames = @($projects | Select-Object -ExpandProperty Name)
    $testProjectNames = @(Get-TestProjectNames $projectNames)
    #Write-Host "projects=$($projects | ConvertTo-Json -Compress)"
    $manifestData = @($projectNames | % {$_ | Get-ManifestData})
    $releaseTitle = Get-ReleaseTitle $manifestData
    $releaseDescription = Get-ReleaseDescription $manifestData
    $output = @(
        "releasetitle=$releaseTitle"
        "releasedescription=$releaseDescription"
        "projects=$($projects | ConvertTo-Json -Compress)"
        "testprojects=$($testProjectNames | ConvertTo-Json -Compress)"
    )
    foreach ($o in $output){
        Write-Host $o
        echo $o >> $env:GITHUB_OUTPUT
    }
}

function Get-Projects {
    param (
        [Parameter(Mandatory = $true)]
        [string]$tag
    )
    $tagSplits = $tag -split '`+'
    $projects = @()
    foreach ($t in $tagSplits) {
        if (!($t -match '(?<Name>[a-z]+)(?<Version>([0-9]+\.){1,3}[0-9]+)')) {
            throw "No name + version match found for $t"
        }
        Write-Host "Name: $($Matches.Name), version: $($Matches.Version)"
        foreach ($n in Get-ProjectNames $t) {
            $projects += @{ Name = $n; Version = $Matches.Version }
        }
    }
    return $projects
}

function Get-ProjectNames {
    param (
        [string]$tagProjectName
    )
    switch ($Matches.Name) {
        "barnite" {
            return @("Barnite")
        }
        "gamersgate" {
            return @("GamersGateLibrary")
        }
        "gamessizecalculator" {
            return @("GamesSizeCalculator")
        }
        "giantbomb" {
            return @("GiantBombMetadata")
        }
        "groupees" {
            return @("GroupeesLibrary")
        }
        "ign" {
            return @("IgnMetadata")
        }
        "itchiobundletagger" {
            return @("itchIoBundleTagger")
        }
        "launchbox" {
            return @("LaunchBoxMetadata")
        }
        "legacygames" {
            return @("LegacyGamesLibrary")
        }
        "pathreplacer" {
            return @("PathReplacer")
        }
        "rawg" {
            return @("RawgLibrary", "RawgMetadata")
        }
        "steamactions" {
            return @("SteamActions")
        }
        "steamtagsimporter" {
            return @("SteamTagsImporter")
        }
        "viveport" {
            return @("ViveportLibrary")
        }
    }
}

function Get-TestProjectNames {
    param (
        [Array]$projects
    )

    $testprojects = @()
        
    foreach ($p in $projects) {
        if (Test-Path ".\source\$p.Tests" -PathType Container) {
            $testprojects += "$p.Tests"
        }
    }
    return $testprojects
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
        return @{ Name = $extensionYamlData.Name; Version = $latestRelease.Version; Changelog = $latestRelease.Changelog }
    }
    Write-Host $manifestContent
    throw "Could not find manifest for addon ID $($manifestContent.Id)"
}

function Get-ReleaseTitle {
    param (
        [Array]$manifests
    )
    $title = ""
    for ($i = 0; $i -lt $manifests.Length; $i++) {
        $m = $manifests[$i]
        if ($title.Length -ne 0) {
            $title += " + "
        }
        $title += "$($m.Name) $($m.Version)"
    }
    return $title
}

function Get-ReleaseDescription {
    param (
        [Array]$manifests
    )
    Write-Host "Get-ReleaseDescription manifests: $($manifests | ConvertTo-Json -Compress)"
    $description = ""
    foreach ($m in @($manifests)) {
        if ($manifests.Length -gt 1) {
            $description += "$($m.Name) $($m.Version)`r`n`r`n"
        }
        foreach ($c in $m.Changelog) {
            $description += "- $c`r`n"
        }
        $description += "`r`n`r`n"
    }
    $description = $description -replace '%', '%25' -replace '\n', '%0A' -replace '\r', '%0D'
    return $description
}

return Get-ReleaseData $tag