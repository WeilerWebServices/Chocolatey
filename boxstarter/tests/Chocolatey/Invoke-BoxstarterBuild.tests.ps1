﻿$here = Split-Path -Parent $MyInvocation.MyCommand.Path
if(Get-Module Boxstarter.Chocolatey){Remove-Module boxstarter.Chocolatey}
Resolve-Path $here\..\..\Boxstarter.Common\*.ps1 |
    % { . $_.ProviderPath }
Resolve-Path $here\..\..\Boxstarter.Bootstrapper\*.ps1 |
    % { . $_.ProviderPath }
Resolve-Path $here\..\..\Boxstarter.Chocolatey\*.ps1 |
    % { . $_.ProviderPath }

Describe "Invoke-BoxstarterBuild" {
    $Boxstarter.LocalRepo=Join-Path ((Get-PSDrive TestDrive).Root) "repo"
    $Boxstarter.SuppressLogging=$true
    $packageName="pkg"

    Context "When Building a single package" {
        Mock Write-Host -parameterFilter {$ForegroundColor -eq $null}
        New-BoxstarterPackage $packageName | Out-Null

        Invoke-BoxstarterBuild $packageName | Out-Null

        It "Will Create the nupkg" {
            Join-Path $Boxstarter.LocalRepo "$packageName.1.0.0.nupkg" | Should Exist
        }
    }

    Context "When Building all packages" {
        Mock Write-Host -parameterFilter {$ForegroundColor -eq $null}
        New-BoxstarterPackage "pkg1" | Out-Null
        New-BoxstarterPackage "pkg2" | Out-Null

        Invoke-BoxstarterBuild -all | Out-Null

        It "Will Create nupkg files for all packages" {
            Join-Path $Boxstarter.LocalRepo "pkg1.1.0.0.nupkg" | Should Exist
            Join-Path $Boxstarter.LocalRepo "pkg2.1.0.0.nupkg" | Should Exist
        }
    }


    Context "When LocalRepo is null" {
        Mock Write-Host -parameterFilter {$ForegroundColor -eq $null}
        New-BoxstarterPackage $packageName | Out-Null
        $boxstarter.LocalRepo = $null

        try {Invoke-BoxstarterBuild $packageName} catch { $ex=$_ }

        It "Will throw LocalRepo is null" {
            $ex | Should match "No Local Repository has been set*"
        }
        $Boxstarter.LocalRepo=Join-Path $boxstarter.BaseDir "repo"
    }

    Context "When No nuspec is in the named repo" {
        Mock Write-Host -parameterFilter {$ForegroundColor -eq $null}
        Mkdir $Boxstarter.LocalRepo -ErrorAction SilentlyContinue | Out-Null

        try {Invoke-BoxstarterBuild $packageName} catch { $ex=$_ }

        It "Will throw No Nuspec" {
            $ex | Should be "Cannot find $($Boxstarter.LocalRepo)\$packageName\$packageName.nuspec"
        }
    }
}
