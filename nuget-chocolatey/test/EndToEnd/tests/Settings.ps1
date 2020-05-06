# Test that getting repository path from the VsSettings object returns
# the right value specified in $solutionDir\.nuget\nuget.config
function Test-GetRepositoryPathFromVsSettings {
    param($context)

	# Arrange
	$p1 = New-ClassLibrary

	$solutionFile = $dte.Solution.FullName
	$solutionDir = Split-Path $solutionFile -Parent
	$nugetDir = Join-Path $solutionDir ".nuget"
	$repoPath = Join-Path $solutionDir "my_repo"

	New-Item $nugetDir -type directory
	$settingFile = Join-Path $nugetDir "nuget.config"
	$settingFileContent =@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <config>
    <add key="repositoryPath" value="{0}" />
  </config>
</configuration>
"@

	$settingFileContent -f $repoPath | Out-File -Encoding "UTF8" $settingFile

	# Act
	# close & open the solution so that the settings are reloaded.
	$dte.Solution.SaveAs($solutionFile)
	Close-Solution
	Open-Solution $solutionFile
	$p2 = Get-Project
	$p2 | Install-Package elmah -Version 1.1

	$vsSetting = [NuGet.VisualStudio.SettingsHelper]::GetVsSettings()
	$v = $vsSetting.GetValue("config", "repositoryPath", $FALSE)

	# Assert
	Assert-AreEqual $repoPath $v
	Assert-True (Test-Path $repoPath)
	Assert-True (Test-Path (Join-Path $repoPath "elmah.1.1"))
}