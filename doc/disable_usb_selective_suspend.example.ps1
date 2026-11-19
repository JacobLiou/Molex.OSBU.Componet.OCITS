#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Disable USB selective suspend on the active Windows power plan (station PC).
.NOTES
  Example for IT / field; do NOT run from the test program at startup.
  Also uncheck "Allow the computer to turn off this device to save power"
  on the USB-serial adapter and USB Root Hubs in Device Manager.
#>

$ErrorActionPreference = "Stop"

Write-Host "Active power scheme:"
powercfg /GETACTIVESCHEME

# USB selective suspend setting GUID (Windows)
$usbSubgroup = "2a737441-1930-4402-8d77-b2bebba308a3"
$usbSelectiveSuspend = "48e6b7a6-50f5-4782-a5d4-53bb8f07e226"

Write-Host "Disabling USB selective suspend (AC + DC) on active scheme..."
powercfg /SETACVALUEINDEX SCHEME_CURRENT $usbSubgroup $usbSelectiveSuspend 0
powercfg /SETDCVALUEINDEX SCHEME_CURRENT $usbSubgroup $usbSelectiveSuspend 0
powercfg /SETACTIVE SCHEME_CURRENT

Write-Host "Done. Verify under Power Options -> Advanced -> USB selective suspend = Disabled."
