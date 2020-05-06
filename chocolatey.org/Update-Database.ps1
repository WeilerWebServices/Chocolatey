Write-Warning 'This will only work when run in the PowerShell console in Visual Studio'
#Write-Host 'Importing Entity Framework module'
#Import-Module 'C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\Extensions\Microsoft Corporation\NuGet Package Manager\2.8.50313.31\Modules\NuGet\nuget.psm1' -Force -Verbose
#Import-Module "$PSScriptRoot\packages\EntityFramework.4.3.1\tools\EntityFramework.psm1" -Verbose -Force
Write-Host 'Updating the database for what is currently found in the web.config file'
Write-Host "Start time is $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss.ffffffK')"
Update-Database -Verbose
Write-Host "Finish time is $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss.ffffffK')"
