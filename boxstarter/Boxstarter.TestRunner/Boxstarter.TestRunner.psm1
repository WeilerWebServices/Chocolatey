$unNormalized=(Get-Item "$PSScriptRoot\..\Boxstarter.Chocolatey\Boxstarter.Chocolatey.psd1")
Import-Module $unNormalized.FullName -global -DisableNameChecking -Force
$unNormalized=(Get-Item "$PSScriptRoot\..\Boxstarter.Azure\Boxstarter.Azure.psd1")
Import-Module $unNormalized.FullName -global -DisableNameChecking -Force -ErrorAction SilentlyContinue

Resolve-Path $PSScriptRoot\*-*.ps1 |
    % { . $_.ProviderPath }

Export-ModuleMember Get-BoxstarterDeployOptions, Set-BoxstarterDeployOptions, Test-BoxstarterPackage, Install-BoxstarterScripts, Get-BoxstarterPackage, Get-BoxstarterPackageNugetFeed, Set-BoxstarterPackageNugetFeed, Remove-BoxstarterPackageNugetFeed, Publish-BoxstarterPackages, Set-BoxstarterFeedAPIKey, Publish-BoxstarterPackage, Select-BoxstarterResultsToPublish, Get-BoxstarterFeedAPIKey
