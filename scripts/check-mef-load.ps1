param(
    [Parameter(Mandatory=$true)]
    [string]$AppRoot
)
# SW2219 ITL FTS - MEF load diagnostic
# Replicates OCITestSystem loading paths and reports the exact LoaderExceptions
# (missing dependency assemblies) on THIS machine.
#
# Usage (run on the problem PC, in 32-bit like the app):
#   C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe -NoProfile -ExecutionPolicy Bypass -File check-mef-load.ps1 -AppRoot "C:\path\to\app"
# The script auto-relaunches itself in 32-bit if needed.

$ErrorActionPreference = 'Continue'

# ---- relaunch as 32-bit to match the x86 host process ----
if ([Environment]::Is64BitProcess -and (Test-Path "$env:windir\SysWOW64\WindowsPowerShell\v1.0\powershell.exe")) {
    & "$env:windir\SysWOW64\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -AppRoot $AppRoot
    exit $LASTEXITCODE
}

$module = Join-Path $AppRoot 'module'
if (-not (Test-Path $module)) { Write-Host ("module dir missing: " + $module); exit 1 }

Write-Host ("==== AppRoot: " + $AppRoot)
Write-Host ("==== Process: " + $(if ([Environment]::Is64BitProcess) {'64-bit (unexpected!)'} else {'32-bit (matches app)'}))

# ---- environment summary ----
$rel = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -ErrorAction SilentlyContinue
Write-Host ("[ENV] .NET4 Release=" + $rel.Release + " Version=" + $rel.Version + " (4.6.1+ required)")
foreach ($g in @('Ivi.Visa','NationalInstruments.Common','NationalInstruments.Visa')) {
    $p = "$env:windir\Microsoft.NET\assembly\GAC_MSIL\" + $g
    $p2 = "$env:windir\assembly\GAC_MSIL\" + $g
    $has = (Test-Path $p) -or (Test-Path $p2)
    Write-Host ("[ENV] GAC " + $g + " : " + $(if ($has) {'present'} else {'MISSING (NI-VISA / NI driver not installed?)'}))
}
$vc = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*','HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*' -ErrorAction SilentlyContinue |
    Where-Object { $_.DisplayName -like '*Visual C++ 2*Redistributable*x86*' -or $_.DisplayName -like '*Visual C++ 2*x86*' } |
    Select-Object -ExpandProperty DisplayName -Unique
if ($vc) { $vc | ForEach-Object { Write-Host ("[ENV] VC++ x86: " + $_) } }
else { Write-Host '[ENV] VC++ x86 redistributable: NOT FOUND' }

# ---- replicate host module cache sync ----
$cache = Join-Path $env:TEMP 'OCITestSystem\module_cache'
New-Item -ItemType Directory -Force $cache | Out-Null
Copy-Item -Path (Join-Path $module '*.dll') -Destination $cache -Force

$probeDirs = @($AppRoot, $cache, $module)
$script:handler = [System.ResolveEventHandler] {
    param($s, $e)
    $name = (New-Object System.Reflection.AssemblyName($e.Name)).Name
    foreach ($d in $script:probeDirs) {
        $p = Join-Path $d ($name + '.dll')
        if (Test-Path $p) { return [System.Reflection.Assembly]::Load([IO.File]::ReadAllBytes($p)) }
    }
    return $null
}
[System.AppDomain]::CurrentDomain.add_AssemblyResolve($script:handler)

function Report-Assembly([string]$dllPath, [string]$tag) {
    try {
        $asm = [System.Reflection.Assembly]::Load([IO.File]::ReadAllBytes($dllPath))
        try {
            $t = $asm.GetTypes()
            Write-Host ("[OK]   " + $tag)
        } catch [System.Reflection.ReflectionTypeLoadException] {
            Write-Host ("[FAIL] " + $tag)
            $uniq = @{}
            foreach ($le in $_.Exception.LoaderExceptions) {
                if (-not $uniq.ContainsKey($le.Message)) { $uniq[$le.Message] = 0 }
                $uniq[$le.Message]++
            }
            foreach ($k in $uniq.Keys) { Write-Host ("        x" + $uniq[$k] + "  " + $k) }
        }
    } catch [System.BadImageFormatException] {
        Write-Host ("[NAT]  " + $tag + "  (native/mixed or wrong bitness - skipped)")
    } catch {
        Write-Host ("[ERR]  " + $tag + " : " + $_.Exception.Message)
    }
}

# ---- Phase 1: host Compose() path (7 plugin DLLs of station ITL_FTS) ----
Write-Host ''
Write-Host '==== Phase 1: host Compose() plugin set (module_ITL_FTS.xml + core) ===='
$start = @('DeviceControl.dll','ConfigModel.dll','InterleaverAlgorithm.dll','UIOperateInterleaverFinalTest.dll','UICurve.dll','UIRealTimeStatus.dll','UIListInterleaver.dll')
foreach ($dll in $start) {
    $p = Join-Path $cache $dll
    if (-not (Test-Path $p)) { $p = Join-Path $module $dll }
    if (-not (Test-Path $p)) { Write-Host ("[MISSING FILE] " + $dll); continue }
    Report-Assembly $p $dll
}

# ---- Phase 2: plugin ctor DirectoryCatalog path (EVERY dll in module\) ----
Write-Host ''
Write-Host '==== Phase 2: full module scan (what plugin DirectoryCatalog enumerates) ===='
foreach ($f in (Get-ChildItem (Join-Path $module '*.dll') | Sort-Object Name)) {
    Report-Assembly $f.FullName ('module\' + $f.Name)
}

# ---- Phase 3: root vs module consistency ----
Write-Host ''
Write-Host '==== Phase 3: root vs module same-name DLL differences ===='
$md5 = [System.Security.Cryptography.MD5]::Create()
function Get-Md5([string]$p) {
    $fs = [IO.File]::OpenRead($p)
    try { [BitConverter]::ToString($md5.ComputeHash($fs)).Replace('-','') } finally { $fs.Close() }
}
foreach ($f in (Get-ChildItem (Join-Path $module '*.dll'))) {
    $rootCopy = Join-Path $AppRoot $f.Name
    if (Test-Path $rootCopy) {
        if ((Get-Md5 $f.FullName) -ne (Get-Md5 $rootCopy)) {
            Write-Host ("  DIFF " + $f.Name + "  (module vs root not identical - version mixing!)")
        }
    } else {
        Write-Host ("  module-only: " + $f.Name)
    }
}

Write-Host ''
Write-Host '==== done - send this output back for analysis ===='
