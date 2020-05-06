function Enable-BoxstarterVHD {
<#
.SYNOPSIS
Enables WMI and LocalAccountTokenFilterPolicy in a VHD's Windows Registry

.DESCRIPTION
Prepares a VHD for Boxstarter Installation. Opening WMI ports and enabling
LocalAccountTokenFilterPolicy so that Boxstarter can later enable
PowerShell Remoting.

.PARAMETER VHDPath
The path to the VHD file

.PARAMETER IgnoreWMI
If specified, WMI ports will not be enabled

.PARAMETER IgnoreLocalAccountTokenFilterPolicy
If specified, IgnoreLocalAccountTokenFilterPolicy will not be enabled

.NOTES
The VHD must be accessible, writable and contain a system drive.

.OUTPUTS
The computer name stored in the VHD's Windows Registry

.EXAMPLE
$ComputerName = Enable-BoxstarterVHD $pathToVHD

Enables IgnoreLocalAccountTokenFilterPolicy and WMI ports in the Windows registry

.EXAMPLE
$ComputerName = Enable-BoxstarterVHD $pathToVHD -IgnoreWMI

Enables IgnoreLocalAccountTokenFilterPolicy in the Windows registry

.EXAMPLE
$ComputerName = Enable-BoxstarterVHD $pathToVHD -IgnoreLocalAccountTokenFilterPolicy

Enables WMI ports in the Windows registry

.LINK
https://boxstarter.org

#>
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=$true)]
        [ValidateScript({Test-Path $_})]
        [ValidatePattern("\.(a)?vhd(x)?$")]
        [string]$VHDPath,
        [switch]$IgnoreWMI,
        [switch]$IgnoreLocalAccountTokenFilterPolicy
    )
    $CurrentVerbosity=$global:VerbosePreference
    try {

        if($PSBoundParameters["Verbose"] -eq $true) {
            $global:VerbosePreference="Continue"
        }

        if(!(Get-Command -Name Get-VM -ErrorAction SilentlyContinue)){
            Write-Error "Boxstarter could not find the Hyper-V PowerShell Module installed. This is required for use with Boxstarter.HyperV. Run Install-windowsfeature -name hyper-v -IncludeManagementTools."
            return
        }

        if((Get-ItemProperty $VHDPath -Name IsReadOnly).IsReadOnly){
            throw New-Object -TypeName InvalidOperationException -ArgumentList "The VHD is Read-Only"
        }
        $before = (Get-Volume).DriveLetter | ? { $_ -ne $null }
        mount-vhd $VHDPath
        $after = (Get-Volume).DriveLetter | ? { $_ -ne $null }
        $winVolume = compare $before $after -Passthru
        Write-BoxstarterMessage "Drives added after mount are $($winVolume)" -Verbose
        $winVolume | % { new-PSDrive -Name $_ -PSProvider FileSystem -Root "$($_):\" -ErrorAction SilentlyContinue | Out-Null}
        try{
            $sysVolume = $winVolume | ? {Test-Path "$($_):\windows\System32\config"}
            if($sysVolume -eq $null){
                throw New-Object -TypeName InvalidOperationException -ArgumentList "The VHD does not contain system volume"
            }
            Write-BoxstarterMessage "Mounted $VHDPath with system volume to Drive $($sysVolume)"
            if(!$IgnoreLocalAccountTokenFilterPolicy) {
                reg load HKLM\VHDSOFTWARE "$($sysVolume):\windows\system32\config\software" | Out-Null
                $policyResult = reg add HKLM\VHDSOFTWARE\Microsoft\Windows\CurrentVersion\Policies\system /v LocalAccountTokenFilterPolicy /t REG_DWORD /d 1 /f
                Write-BoxstarterMessage "Enabled LocalAccountTokenFilterPolicy with result: $policyResult"
            }

            reg load HKLM\VHDSYS "$($sysVolume):\windows\system32\config\system" | Out-Null
            $current=Get-CurrentControlSet
            $computerName = (Get-ItemProperty "HKLM:\VHDSYS\ControlSet00$current\Control\ComputerName\ComputerName" -Name ComputerName).ComputerName

            if(!$IgnoreWMI){
                (Get-Item (Get-FireWallKey)).Property | ? { $_-like 'wmi-*' } | % { Enable-FireWallRule $_}
                Write-BoxstarterMessage "Enabled WMI Firewall Rules."
            }

            return "$computerName"
        }
        finally{
            [GC]::Collect() # The next line will fail without this since handles to the loaded hive have not yet been collected
            reg unload HKLM\VHDSOFTWARE 2>&1 | Out-Null
            reg unload HKLM\VHDSYS 2>&1 | Out-Null
            Write-BoxstarterMessage "VHD Registry Unloaded" -Verbose
            Dismount-VHD $VHDPath
            Write-BoxstarterMessage "VHD Dismounted"
        }
    }
    finally{
        $global:VerbosePreference=$CurrentVerbosity
    }
}

function Enable-FireWallRule($ruleName){
    $key=Get-FirewallKey
    $rules = Get-ItemProperty $key
    $rule=$rules.$ruleName
    $newVal = $rule.Replace("|Active=FALSE|","|Active=TRUE|")
    Set-ItemProperty $key -Name $ruleName -Value $newVal
    Write-BoxstarterMessage "Changed $ruleName firewall rule to: $newVal" -Verbose
}

function Disable-FireWallRule($ruleName){
    $key=Get-FirewallKey
    $rules = Get-ItemProperty $key
    $rule=$rules.$ruleName
    $newVal = $rule.Replace("|Active=TRUE|","|Active=FALSE|")
    Set-ItemProperty $key -Name $ruleName -Value $newVal
    Write-BoxstarterMessage "Changed $ruleName firewall rule to: $newVal" -Verbose
}

function Get-FireWallKey{
    $current = Get-CurrentControlSet
    return "HKLM:\VHDSYS\ControlSet00$current\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules"
}

function Get-CurrentControlSet {
    return (Get-ItemProperty "HKLM:\VHDSYS\Select" -Name Current).Current

}
