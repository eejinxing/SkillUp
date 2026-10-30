# Runs Skill UP so other devices on the same Wi-Fi can open it, for testing calls between two devices.
# Double-click Start-LAN.cmd (it asks for administrator rights once: Windows requires that to share a port on the network).
# Build the project in Visual Studio first (Ctrl+Shift+B), then close Visual Studio's own run of the site.
$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$http = 8080
$https = 44390
$siteName = 'SkillUpLan'

$iis = Join-Path ${env:ProgramFiles} 'IIS Express\iisexpress.exe'
if (-not (Test-Path $iis)) { $iis = Join-Path ${env:ProgramFiles(x86)} 'IIS Express\iisexpress.exe' }
if (-not (Test-Path $iis)) { throw 'IIS Express was not found. It is installed with Visual Studio.' }
if (-not (Test-Path (Join-Path $root 'bin\SKILL UP.dll'))) { throw 'The project has not been built yet. Build it in Visual Studio first (Ctrl+Shift+B).' }

# The address other devices type in: the first private IPv4 address on this PC.
# Virtual adapters (VirtualBox, VMware, Hyper-V, WSL) also have private addresses, but other devices cannot reach those, so
# the address comes from the adapter that carries the PC's internet route (the Wi-Fi or cable to the router).
$route = Get-NetRoute -DestinationPrefix '0.0.0.0/0' -ErrorAction SilentlyContinue |
    Sort-Object { $_.RouteMetric + (Get-NetIPInterface -InterfaceIndex $_.InterfaceIndex -AddressFamily IPv4).InterfaceMetric } |
    Select-Object -First 1
$ip = $null
if ($route) {
    $ip = Get-NetIPAddress -AddressFamily IPv4 -InterfaceIndex $route.InterfaceIndex -ErrorAction SilentlyContinue |
        Where-Object { $_.IPAddress -notlike '169.254.*' } | Select-Object -First 1 -ExpandProperty IPAddress
}
if (-not $ip) {
    $ip = Get-NetIPAddress -AddressFamily IPv4 |
        Where-Object { $_.IPAddress -match '^(192\.168\.|10\.|172\.(1[6-9]|2\d|3[01])\.)' -and $_.InterfaceAlias -notmatch 'VMware|VirtualBox|Virtual|vEthernet|WSL|Loopback|Host-Only' } |
        Select-Object -First 1 -ExpandProperty IPAddress
}
if (-not $ip) { throw 'No network address found. Connect this PC to the same Wi-Fi as the other device.' }

# A private copy of IIS Express's own settings with just this site, listening on every address.
$config = Join-Path $root 'obj\lan-applicationhost.config'
$template = Join-Path (Split-Path $iis) 'config\templates\PersonalWebServer\applicationhost.config'
[xml]$xml = Get-Content $template
$sites = $xml.configuration.'system.applicationHost'.sites
$sites.RemoveAll()
$sites.InnerXml = '<site name="' + $siteName + '" id="1"><application path="/" applicationPool="Clr4IntegratedAppPool">' +
    '<virtualDirectory path="/" physicalPath="' + $root + '" /></application><bindings>' +
    '<binding protocol="http" bindingInformation="*:' + $http + ':" />' +
    '<binding protocol="https" bindingInformation="*:' + $https + ':" /></bindings></site>'
$xml.Save($config)

# Windows Firewall: allow the two ports in, but only from devices on this PC's own network (same subnet). Home Wi-Fi is
# often marked "Public" by Windows, so the rule applies to every profile; the subnet limit is what keeps it narrow.
foreach ($port in $http, $https) {
    $rule = "Skill UP test $port"
    Get-NetFirewallRule -DisplayName $rule -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    New-NetFirewallRule -DisplayName $rule -Direction Inbound -Protocol TCP -LocalPort $port -Action Allow -Profile Any -RemoteAddress LocalSubnet | Out-Null
}

# HTTPS uses IIS Express's own development certificate (the browser shows a warning once; choose Advanced > Continue).
# The camera and microphone only work on a secure page, so the other device should use the https address.
$cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.FriendlyName -like 'IIS Express Development Certificate*' } | Select-Object -First 1
if ($cert) {
    netsh http delete sslcert ipport=0.0.0.0:$https 2>$null | Out-Null
    netsh http add sslcert ipport=0.0.0.0:$https certhash=$($cert.Thumbprint) appid='{214124cd-d05b-4309-9af9-9caa44b2b74a}' | Out-Null
}

Write-Host ''
Write-Host 'Skill UP is running. On the other device (same Wi-Fi) open:' -ForegroundColor Green
if ($cert) { Write-Host "    https://${ip}:$https/   (use this one for camera and microphone)" -ForegroundColor Cyan }
Write-Host "    http://${ip}:$http/    (works for chat and for hearing/seeing the other person only)" -ForegroundColor Cyan
Write-Host ''
Write-Host 'Log in as a different user on each device, open Messages, and press the call button. Press Ctrl+C here to stop.'
Write-Host 'Devices that connect are listed below, with the name each one gives the network when it shares one.'
Write-Host ''

# The name a device gives the network: the router's own name list first (it learns names when devices join), then the
# older Windows name lookup. Some phones keep their name private, so the address alone is shown for those.
function Get-DeviceName([string]$address) {
    try {
        $name = [System.Net.Dns]::GetHostEntry($address).HostName
        if ($name -and $name -ne $address) { return ($name -replace '\.(lan|local|home|fritz\.box|localdomain)$', '') }
    } catch { }
    try {
        $line = nbtstat -A $address 2>$null | Select-String '^\s*(\S+)\s+<00>\s+UNIQUE' | Select-Object -First 1
        if ($line) { return $line.Matches[0].Groups[1].Value }
    } catch { }
    return $null
}

$server = Start-Process -FilePath $iis -ArgumentList "/config:`"$config`"", "/site:$siteName" -NoNewWindow -PassThru
$known = @{}      # address -> @{ Name; Seen; Tries }
$mine = @('127.0.0.1', '::1', $ip)
try {
    while (-not $server.HasExited) {
        $now = Get-Date
        $live = Get-NetTCPConnection -State Established -LocalPort $http, $https -ErrorAction SilentlyContinue |
            Where-Object { $mine -notcontains $_.RemoteAddress } | Select-Object -ExpandProperty RemoteAddress -Unique
        foreach ($address in $live) {
            if (-not $known.ContainsKey($address)) {
                $known[$address] = @{ Name = $null; Seen = $now; Tries = 0; Shown = $false }
            }
            $device = $known[$address]
            $device.Seen = $now
            if (-not $device.Name -and $device.Tries -lt 3) {
                $device.Tries++
                $device.Name = Get-DeviceName $address
            }
            if (-not $device.Shown -or ($device.Name -and -not $device.NameShown)) {
                $label = if ($device.Name) { $device.Name } else { 'name not shared by this device' }
                Write-Host ("{0:HH:mm:ss}  Connected: {1}  ({2})" -f $now, $label, $address) -ForegroundColor Green
                $device.Shown = $true
                $device.NameShown = [bool]$device.Name
            }
        }
        foreach ($address in @($known.Keys)) {
            $device = $known[$address]
            if (($now - $device.Seen).TotalSeconds -gt 20) {
                $label = if ($device.Name) { $device.Name } else { 'unnamed device' }
                Write-Host ("{0:HH:mm:ss}  Left:      {1}  ({2})" -f $now, $label, $address) -ForegroundColor DarkGray
                $known.Remove($address)
            }
        }
        Start-Sleep -Seconds 2
    }
} finally {
    if (-not $server.HasExited) { Stop-Process -Id $server.Id -Force }
}
