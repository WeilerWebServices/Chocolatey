$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Get-Module Boxstarter.* | Remove-Module -ErrorAction  SilentlyContinue
Resolve-Path $here\..\..\Boxstarter.Common\*.ps1 |
    % { . $_.ProviderPath }
Resolve-Path $here\..\..\Boxstarter.Bootstrapper\*.ps1 |
    % { . $_.ProviderPath }
Resolve-Path $here\..\..\Boxstarter.Chocolatey\*.ps1 |
    ? { $_.Path -like "*-*" } |
    % { . $_.ProviderPath }
Resolve-Path $here\..\..\Boxstarter.TestRunner\*.ps1 |
    ? { $_.Path -like "*-*" } |
    % { . $_.ProviderPath }

Describe "Install-BoxstarterScripts" {
    $repo = Join-Path (Get-PSDrive TestDrive).Root "repo"
    $Boxstarter.SuppressLogging=$true

    Context "When the repository exists" {
        Mkdir $repo | Out-Null

        Install-BoxstarterScripts $repo

        It "should copy bootstrapper" {
            Join-Path $repo "BoxstarterScripts\bootstrap.ps1" | Should exist
        }
        It "should copy msbuild file" {
            Join-Path $repo "BoxstarterScripts\boxstarter.proj" | Should exist
        }
        It "should copy BoxstarterBuild" {
            Join-Path $repo "BoxstarterScripts\BoxstarterBuild.ps1" | Should exist
        }
        It "should write ignore file for secrets" {
            "$repo\BoxstarterScripts\.gitignore" | Should contain "-options.xml"
        }
        It "should write ignore file for API keys" {
            "$repo\BoxstarterScripts\.gitignore" | Should contain "FeedAPIKeys.xml"
        }
    }

    Context "When the repository does not exist" {
        try {
            Install-BoxstarterScripts $repo
        }
        catch{
            $err = $_
        }

        It "Should throw a validation error"{
            $err.CategoryInfo.Category | should be "InvalidData"
        }
    }
}
