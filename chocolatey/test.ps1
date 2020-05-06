$scriptPath = (Split-Path -parent $MyInvocation.MyCommand.path)
$identity  = [System.Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object System.Security.Principal.WindowsPrincipal( $identity )
$isAdmin = $principal.IsInRole( [System.Security.Principal.WindowsBuiltInRole]::Administrator )

$chocoDir = $env:ChocolateyInstall
if(!$chocoDir){$chocoDir="$env:AllUsersProfile\chocolatey"}
if(!(Test-Path($chocoDir))){$chocoDir="$env:SystemDrive\chocolatey"}
$chocoDir = $chocoDir.Replace("\bin","")
$env:Path +=";$chocoDir\bin;"
Write-Host "Path is $($env:Path)"

$pesterVersion = '2.0.2'
cinst pester -version $pesterVersion
$pesterDir = "$chocoDir\lib\Pester.$pesterVersion"

if(-not $isAdmin){
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName="$env:windir\System32\WindowsPowerShell\v1.0\powershell.exe"
    $psi.Verb="runas"
    $psi.Arguments="-NoProfile -ExecutionPolicy unrestricted -Command `". { CD '$scriptPath';. '$pesterDir\tools\bin\pester.bat'; `$ec = `$?;if(!`$ec){Write-Host 'Press any key to continue ...';`$host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');exit 1} } `""
    $s = [System.Diagnostics.Process]::Start($psi)
    $s.WaitForExit()
    exit $s.ExitCode
} else {
    . "$pesterDir\tools\bin\pester.bat"
}
