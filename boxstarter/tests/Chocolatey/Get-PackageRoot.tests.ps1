$here = Split-Path -Parent $MyInvocation.MyCommand.Path
if(Get-Module Boxstarter.Chocolatey){Remove-Module boxstarter.Chocolatey}
Import-Module "$here\..\..\Boxstarter.Chocolatey\Boxstarter.Chocolatey.psd1" -Force

Describe "Get-PackageRoot" {
    $testRoot=(Get-PSDrive TestDrive).Root
    $Boxstarter.SuppressLogging=$false

    Context "When Calling from ChocolateyInstall" {
        MkDir "$testRoot\tools" | Out-Null
        $chocolateyInstall = Join-Path $testRoot "tools\chocolateyInstall.ps1"
        New-Item $chocolateyInstall -type file -value "return Get-PackageRoot `$MyInvocation" | Out-Null

        $result = (& $chocolateyInstall)

        It "Should return path above tools"{
            $result | should be $testRoot
        }
    }

    Context "When Not Calling from ChocolateyInstall" {
        $install = Join-Path $testRoot "Install.ps1"
        New-Item $install -type file -value "return Get-PackageRoot `$MyInvocation" | Out-Null

        try {& $install -ErrorAction Stop} catch { $ex=$_ }

        It "Should throw"{
            $ex | should not be $null
        }
    }
}
