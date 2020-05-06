$here = Split-Path -Parent $MyInvocation.MyCommand.Path
if(Get-Module Boxstarter.Chocolatey){Remove-Module boxstarter.Chocolatey}
Resolve-Path $here\..\..\Boxstarter.Common\*.ps1 |
    % { . $_.ProviderPath }
Resolve-Path $here\..\..\Boxstarter.Bootstrapper\*.ps1 |
    % { . $_.ProviderPath }
Resolve-Path $here\..\..\Boxstarter.Chocolatey\*.ps1 |
    % { . $_.ProviderPath }

Describe "New-PackageFromScript" {
    $Boxstarter.LocalRepo=Join-Path ((Get-PSDrive TestDrive).Root) "repo"
    $Boxstarter.SuppressLogging=$true
    $unzipPath = Join-Path $Boxstarter.LocalRepo "unzipped"
    Mock Write-Host -parameterFilter {$ForegroundColor -eq $null}

    Context "When Building a script from a file" {
        MkDir $unzipPath
        New-Item TestDrive:\script.ps1 -type file -Value "return" | Out-Null

        ($result = New-PackageFromScript TestDrive:\script.ps1) | Out-Null

        It "Will Create the nupkg" {
            Join-Path $Boxstarter.LocalRepo "$result.1.0.0.nupkg" | Should Exist
        }
        It "Will contain the script" {
            Rename-Item "$($boxstarter.LocalRepo)\$result.1.0.0.nupkg" "$($boxstarter.LocalRepo)\$result.1.0.0.zip"
            $shell_app=new-object -com shell.application
            $filename = "$result.1.0.0.zip"
            $zip_file = $shell_app.namespace("$($boxstarter.LocalRepo)\$filename")
            $destination = $shell_app.namespace($unzipPath)
            $destination.Copyhere($zip_file.items())
            Get-content "$unzipPath\tools\ChocolateyInstall.ps1" | Should be "return"
        }
    }

    Context "When Building a script from a URL" {
        MkDir $unzipPath
        . "$env:chocolateyinstall\helpers\functions\Get-WebFile.ps1"
        Mock Get-HttpResource {return "return"}

        ($result = New-PackageFromScript "file://$($boxstarter.Basedir)/script.ps1") | Out-Null

        It "Will Create the nupkg" {
            Join-Path $Boxstarter.LocalRepo "$result.1.0.0.nupkg" | Should Exist
        }
        It "Will contain the script" {
            Rename-Item "$($boxstarter.LocalRepo)\$result.1.0.0.nupkg" "$($boxstarter.LocalRepo)\$result.1.0.0.zip"
            $shell_app=new-object -com shell.application
            $filename = "$result.1.0.0.zip"
            $zip_file = $shell_app.namespace("$($boxstarter.LocalRepo)\$filename")
            $destination = $shell_app.namespace($unzipPath)
            $destination.Copyhere($zip_file.items())
            Get-content "$unzipPath\tools\ChocolateyInstall.ps1" | Should be "return"
        }
    }

    Context "When http client throws an error" {
        . "$env:chocolateyinstall\helpers\functions\Get-WebFile.ps1"
        Mock Get-HttpResource {throw "blah"}
        Mock New-BoxstarterPackage

        try {($result = New-PackageFromScript "file://$($boxstarter.Basedir)/script.ps1") | Out-Null}catch{}

        It "Will not try to create package" {
            Assert-MockCalled New-BoxstarterPackage -Times 0
        }
    }

    Context "When script file is not found" {
        Mock New-BoxstarterPackage

        try {($result = New-PackageFromScript TestDrive:\script.ps1) | Out-Null}catch{}

        It "Will not try to create package" {
            Assert-MockCalled New-BoxstarterPackage -Times 0
        }
    }

    Context "When ReBuilding an existing package" {
        MkDir $unzipPath
        New-Item TestDrive:\script.ps1 -type file -Value "return" | Out-Null
        New-PackageFromScript TestDrive:\script.ps1 | Out-Null
        New-Item TestDrive:\script.ps1 -type file -Value "return 'again'" -force | Out-Null

        ($result = New-PackageFromScript TestDrive:\script.ps1) | Out-Null

        It "Will contain the new script" {
            Rename-Item "$($boxstarter.LocalRepo)\$result.1.0.0.nupkg" "$($boxstarter.LocalRepo)\$result.1.0.0.zip"
            $shell_app=new-object -com shell.application
            $filename = "$result.1.0.0.zip"
            $zip_file = $shell_app.namespace("$($boxstarter.LocalRepo)\$filename")
            $destination = $shell_app.namespace($unzipPath)
            $destination.Copyhere($zip_file.items())
            Get-content "$unzipPath\tools\ChocolateyInstall.ps1" | Should be "return 'again'"
        }
    }

}
