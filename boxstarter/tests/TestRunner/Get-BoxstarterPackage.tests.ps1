$here = Split-Path -Parent $MyInvocation.MyCommand.Path
if(Get-Module Boxstarter.TestRunner){Remove-Module Boxstarter.TestRunner}
Resolve-Path $here\..\..\Boxstarter.Common\*.ps1 |
    % { . $_.ProviderPath }
Resolve-Path $here\..\..\Boxstarter.Bootstrapper\*.ps1 |
    % { . $_.ProviderPath }
Resolve-Path $here\..\..\Boxstarter.Chocolatey\*.ps1 |
    % { . $_.ProviderPath }
Resolve-Path $here\..\..\Boxstarter.TestRunner\*.ps1 |
    ? { $_.Path -like "*-*" } |
    % { . $_.ProviderPath }

Describe "Get-BoxstarterPackage" {
    $Boxstarter.LocalRepo=(Get-PSDrive TestDrive).Root
    $Boxstarter.SuppressLogging=$true

    Context "When Getting all packages in repo" {
        New-BoxstarterPackage -name package1
        New-BoxstarterPackage -name package2
        Mock Get-BoxstarterPackagePublishedVersion

        $result = Get-BoxstarterPackage

        it "should return the correct number of packages" {
            $result.Count | should be 2
        }
        it "should return package1" {
            $result[0].Id | should be "package1"
            $result[0].Version | should be "1.0.0"
        }
        it "should return package2" {
            $result[1].Id | should be "package2"
            $result[1].Version | should be "1.0.0"
        }
    }

    Context "When Getting published packages in repo" {
        New-BoxstarterPackage -name package1
        New-BoxstarterPackage -name package2
        Mock Get-BoxstarterPackagePublishedVersion {"5.5.5"} -parameterFilter {$PackageId -eq "package1"}
        Mock Get-BoxstarterPackagePublishedVersion {"6.6.6"} -parameterFilter {$PackageId -eq "package2"}

        $result = Get-BoxstarterPackage

        it "should return the correct version for package1" {
            $result[0].PublishedVersion | should be "5.5.5"
        }
        it "should return the correct version for package2" {
            $result[1].PublishedVersion | should be "6.6.6"
        }
    }

    Context "When a package feed is null or empty" {
        New-BoxstarterPackage -name package1
        Set-BoxstarterPackageNugetFeed -PackageName package1 -NugetFeed $null
        Mock Get-BoxstarterPackagePublishedVersion

        $result = Get-BoxstarterPackage

        it "should have a null published version" {
            $result[0].PublishedVersion | should be $null
        }
        it "should have no feed" {
            $result[0].Feed | should be $null
        }
    }

    Context "When Getting packages by name" {
        New-BoxstarterPackage -name package1
        New-BoxstarterPackage -name package2
        New-BoxstarterPackage -name package3
        Mock Get-BoxstarterPackagePublishedVersion

        $result = Get-BoxstarterPackage -PackageName "package1","package3"

        it "should return the correct number of packages" {
            $result.Count | should be 2
        }
        it "should return package1" {
            $result[0].Id | should be "package1"
            $result[0].Version | should be "1.0.0"
        }
        it "should return package3" {
            $result[1].Id | should be "package3"
            $result[1].Version | should be "1.0.0"
        }
    }

    Context "When Getting one package by name" {
        New-BoxstarterPackage -name package1
        New-BoxstarterPackage -name package2
        New-BoxstarterPackage -name package3
        Mock Get-BoxstarterPackagePublishedVersion

        $result = Get-BoxstarterPackage -PackageName "package2"

        it "should return the correct number of packages" {
            $result.Count | should be $null
        }
        it "should return package2" {
            $result[0].Id | should be "package2"
            $result[0].Version | should be "1.0.0"
        }
    }
}
