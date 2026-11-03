# Start Remote Tunnel for Wand Enhancer Web Deck
# Allows access from ANY mobile network (cellular 4G/5G, foreign Wi-Fi, etc.)
param(
    [int]$Port = 3223
)

$ErrorActionPreference = "Stop"
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Wand Remote Deck - External Network Setup " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. Check local network status
$localIp = (Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias "Wi-Fi*", "Ethernet*" -ErrorAction SilentlyContinue | Where-Object { $_.IPAddress -notmatch '^169\.' } | Select-Object -First 1).IPAddress
if ($localIp) {
    Write-Host "[Local Wi-Fi]   http://${localIp}:${Port}/remote/" -ForegroundColor Green
}

# 2. Check Tailscale status
$tailscaleExe = "C:\Program Files\Tailscale\tailscale.exe"
if (Test-Path $tailscaleExe) {
    $tsIp = (& $tailscaleExe ip -4 2>$null)
    if ($tsIp) {
        Write-Host "[Tailscale VPN] http://${tsIp}:${Port}/remote/ (Worldwide access via Tailnet)" -ForegroundColor Magenta
    }
}

# 3. Cloudflare Quick Tunnel (Free, zero sign-up public HTTPS access)
$toolsDir = Join-Path $PSScriptRoot "tools"
if (-not (Test-Path $toolsDir)) {
    New-Item -ItemType Directory -Path $toolsDir | Out-Null
}
$cloudflaredPath = Join-Path $toolsDir "cloudflared.exe"

if (-not (Test-Path $cloudflaredPath)) {
    $installedCloudflared = Get-Command cloudflared -ErrorAction SilentlyContinue
    if ($installedCloudflared) {
        $cloudflaredPath = $installedCloudflared.Source
    }
}

if (-not (Test-Path $cloudflaredPath)) {
    Write-Host "`nDownloading Cloudflare Quick Tunnel (cloudflared.exe)..." -ForegroundColor Yellow
    $downloadUrl = "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe"
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
        Invoke-WebRequest -Uri $downloadUrl -OutFile $cloudflaredPath -UseBasicParsing
        Write-Host "Cloudflared downloaded successfully." -ForegroundColor Green
    } catch {
        Write-Host "Cloudflared download failed: $_" -ForegroundColor Red
        Write-Host "Use your Tailscale IP above or ensure internet connectivity." -ForegroundColor Yellow
        exit 1
    }
}

Write-Host "`nStarting secure public tunnel to http://127.0.0.1:${Port}..." -ForegroundColor Cyan
Write-Host "This provides a public HTTPS link accessible on ANY network (cellular / 4G / 5G / office).`n" -ForegroundColor Gray

# Run cloudflared and capture the public URL
$processInfo = New-Object System.Diagnostics.ProcessStartInfo
$processInfo.FileName = $cloudflaredPath
$processInfo.Arguments = "tunnel --url http://127.0.0.1:${Port}"
$processInfo.RedirectStandardError = $true
$processInfo.RedirectStandardOutput = $true
$processInfo.UseShellExecute = $false
$processInfo.CreateNoWindow = $true

$process = [System.Diagnostics.Process]::Start($processInfo)
$tunnelUrl = $null

while (-not $process.HasExited) {
    $line = $process.StandardError.ReadLine()
    if ($line -match 'https://[a-zA-Z0-9-]+\.trycloudflare\.com') {
        $tunnelUrl = $Matches[0] + "/remote/"
        break
    }
}

if ($tunnelUrl) {
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host " PUBLIC HTTPS REMOTE DECK LINK (ACCESSIBLE FROM ANYWHERE) " -ForegroundColor Green
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host "`n  $tunnelUrl`n" -ForegroundColor Yellow
    Write-Host "Open this link on your phone from any browser or network!" -ForegroundColor White
    Write-Host "Press Ctrl+C to stop the tunnel.`n" -ForegroundColor Gray
    
    # Wait for process
    $process.WaitForExit()
} else {
    Write-Host "Failed to establish tunnel. Please check your internet connection." -ForegroundColor Red
}
