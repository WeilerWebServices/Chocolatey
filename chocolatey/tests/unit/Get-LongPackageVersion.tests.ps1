$here = Split-Path -Parent $MyInvocation.MyCommand.Definition
$common = Join-Path (Split-Path -Parent $here)  '_Common.ps1'
. $common

Describe "Get-LongPackageVersion" {
  Context "under normal circumstances" {
    $shortVersion = '0.1.3'
    $packageVersions = @($shortVersion)
    $returnValue = Get-LongPackageVersion $packageVersions
    $expectedValue = '000000000000.000000000001.000000000003'

    It "should return a long version string with 12 fields of padding back" {
      $returnValue  | should Be $expectedValue
    }
  }

  Context "when version has a date value" {
    $shortVersion = '2.0.1.20120225'
    $packageVersions = @($shortVersion)
    $returnValue = Get-LongPackageVersion $packageVersions
    $expectedValue = '000000000002.000000000000.000000000001.000020120225'

    It "should not error" {}

    It "should return a long version string that includes all of the date string" {
      $returnValue  | should Be $expectedValue
    }
  }

  Context "when there is a prerelease package version" {
    $shortVersion = '2.0.1.3-alpha1'
    $packageVersions = @($shortVersion)
    $returnValue = Get-LongPackageVersion $packageVersions
    $expectedValue = '000000000002.000000000000.000000000001.000000000003.alpha1'

    It "should not error" {}

    It "should return a long version string that includes the prerelease information as the last element" {
      $returnValue  | should Be $expectedValue
    }
  }

  Context "when there is a prerelease package version that contains multiple dashes" {
    $shortVersion = '2.0.1.3-alpha-1'
    $packageVersions = @($shortVersion)
    $returnValue = Get-LongPackageVersion $packageVersions
    $expectedValue = '000000000002.000000000000.000000000001.000000000003.alpha-1'

    It "should not error" {}

    It "should return a long version string that includes the prerelease information as the last element" {
      $returnValue  | should Be $expectedValue
    }
  }
}
