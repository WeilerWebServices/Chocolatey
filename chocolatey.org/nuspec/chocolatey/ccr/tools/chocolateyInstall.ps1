$packageName   = $env:ChocolateyPackageName
$toolsDir      = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$webToolsDir   = Join-Path $toolsDir $packageName
$packageWebConfig  = Join-Path $webToolsDir 'Web.config'
$webInstallDir     = Join-Path (Get-ToolsLocation) $packageName
$existingWebConfig = Join-Path $webInstallDir 'Web.config'

# http://msdn.microsoft.com/en-us/library/windows/desktop/ms724832.aspx
$osVersion = [Environment]::OSVersion.Version
if ($osVersion -ge [Version]'6.2') #8/2012+
{

} else { #Windows 7/2008 and below
."$env:windir\microsoft.net\framework\v4.0.30319\aspnet_regiis.exe" -i
}

If (Test-Path -Path $existingWebConfig) {
  Write-Output "Copying existing web.config to package directory to allow proper updates"
  Copy-Item $existingWebConfig $packageWebConfig -Force -ErrorAction SilentlyContinue
  Write-Warning "Due to transforms happening AFTER this script completes, you will likely need to manually migrate '$packageWebConfig' back to '$existingWebConfig' once upgrade is complete. Also check the config file to make sure that it was not malformed by the XDT transform."
}

if (! (Test-Path -Path $webInstallDir)) {
  New-Item $webInstallDir -ItemType Directory -Force | Out-Null
  Copy-Item $webToolsDir\* $webInstallDir -Recurse -Force
} else {
  try {
    Write-Debug "Removing all but the App_Data folder in the existing '$webInstallDir'"
    Get-ChildItem -Path "$webInstallDir" -Recurse | % {
      if ($_.FullName -match 'App_Data' -or $_.FullName -match 'Web.config') {
        Write-Debug " - Skipping $($_.FullName)"
      } else {
        Write-Debug " - Removing $($_.FullName)"
        Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
      }
    }
  } catch {
    Write-Warning "Had an error deleting files from '$webInstallDir'. You will need to manually remove files. `n Error: $($_.Message)"
  }

  # Now copy all new except the App_Data folder
  Write-Debug "Copying files from '$webToolsDir' to '$webInstallDir'"
  Get-ChildItem -Path $webToolsDir -Recurse | % {
    if ($_.FullName -match 'App_Data') {
      # leave these items
      Write-Debug "- Skipping $($_.FullName)"
    } else {
      if (! ($_.PSIsContainer)) {
        $srcFile = $_.FullName
        $destinationFile = Join-Path $webInstallDir ($srcFile.Substring($webToolsDir.length))
        $destinationDir = $destinationFile.Replace($destinationFile.Split("\")[-1],"")
        #$destinationDir = Join-Path $webInstallDir ($_.Parent.FullName.Substring($webToolsDir.length))
        if (! (Test-Path -Path $destinationDir)) {
          Write-Debug " - Creating $destinationDir"
          New-Item $destinationDir -ItemType Directory -Force | Out-Null
        }
        try {
          Write-Debug " - Copying '$srcFile' to '$destinationFile'"
          Copy-Item $srcFile -Destination $destinationFile -Force -ErrorAction Stop
        } catch {
          Write-Warning "Unable to copy '$srcFile' to '$destinationFile'. `n Error: $_"
        }
      }
    }
  }
}