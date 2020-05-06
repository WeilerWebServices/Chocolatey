$here = Split-Path -Parent $MyInvocation.MyCommand.Definition
$common = Join-Path (Split-Path -Parent $here)  '_Common.ps1'
. $common

Setup -File 'userprofile\_crapworkaround.txt'
Describe "Get-ConfigValue" {
  Context "when retrieving a simple return value" {
    $oldProfile = $env:USERPROFILE
    $env:USERPROFILE = Join-Path 'TestDrive:' 'userProfile'
    Setup -File 'chocolatey\chocolateyInstall\chocolatey.config' @"
<?xml version="1.0"?>
<chocolatey>
    <useNuGetForSources>true</useNuGetForSources>
</chocolatey>
"@
    $result = Get-ConfigValue 'useNuGetForSources'
    $env:USERPROFILE = $oldProfile

    It "should not be null" {
      $true  | should Be $result -ne $null
    }

    It "should return the result of what we specified" {
      $result  | should Be 'true'
    }
  }

  Context "when retrieving a list" {
    $oldProfile = $env:USERPROFILE
    $env:USERPROFILE = Join-Path 'TestDrive:' 'userProfile'
    Setup -File 'chocolatey\chocolateyInstall\chocolatey.config' @"
<?xml version="1.0"?>
<chocolatey>
    <sources>
        <source id="chocolatey" value="https://chocolatey.org/api/v2/" />
        <source id="nuget" value="https://go.microsoft.com/fwlink/?LinkID=230477" />
    </sources>
</chocolatey>
"@
    $result = Get-ConfigValue 'sources'

    $env:USERPROFILE = $oldProfile
    It "should not be null" {
      $result | should Not BeNullOrEmpty
    }

    It "should return a type of what we specified" {
      $result.GetType() | should Be 'System.Xml.XmlElement'
    }

    It "should contain the same number of sources as specified" {
      $result.ChildNodes.Count | should Be 2
    }

    It "should contain source IDs that are not null or empty" {
       foreach ($source in $result.source) {
          $source.id | should Not BeNullOrEmpty
       }
    }

    It "should contain source values that are not null or empty" {
       foreach ($source in $result.source) {
          $source.value | should Not BeNullOrEmpty
       }
    }

    It "should contain a source for chocolatey" {
      $found = $false
      foreach ($source in $result.source) {
          if ($source.id -eq 'chocolatey') { $found = $true }
      }

      $found | should Be $true
    }
  }
}
