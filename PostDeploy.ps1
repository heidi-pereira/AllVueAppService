Write-Host "Running PostDeploy.ps1 script"
$ErrorActionPreference = "Stop"

Import-Module WebAdministration

# These variables are set in Octopus Deploy
$ChannelName = $OctopusParameters["Octopus.Release.Channel.Name"]
$Product = $OctopusParameters["IISWebsiteDomain"]
$AppPoolName = $OctopusParameters["BrandVueWebsiteAndAppPoolName"]
$Domain = $OctopusParameters["Domain"]

Write-Host "ChannelName: $ChannelName"
Write-Host "Product: $Product"
Write-Host "AppPoolName: $AppPoolName"

if ($ChannelName -ne "Default") {
    Write-Host "We are on a feature branch deployment. No action required."
    return
}
if ([string]::IsNullOrWhitespace($Product)) {
    Write-Host "No Product specified. No action required."
    return
}

Write-Host "Setting IIS App Pool $AppPoolName properties:"
if ($OctopusEnvironmentName -ne "Live") {
    # BrandVue products are very memory hungry, we can't afford to keep them always running on Beta
    Write-Host "Env != Live, setting startMode to OnDemand..."
    Set-ItemProperty -Path IIS:\AppPools\$AppPoolName -Name startMode -Value 'OnDemand'
}
else {
    Write-Host "Env = Live, setting startMode to AlwaysRunning..."
    Set-ItemProperty -Path IIS:\AppPools\$AppPoolName -Name startMode -Value 'AlwaysRunning'
}

if ($Product -eq "survey") {
    Write-Host "Warming up Survey Vue is not supported."
    return;
}

Write-Host "Generating warm up URL..."
if ($OctopusEnvironmentName -ne "Live") {
    $Domain = "$OctopusEnvironmentName-$Domain"
}

if ($AppPoolName -eq "Live-WGSN-barometer") {
    $WarmUpUrl = "https://barometer.wgsn.com"
}
elseif ($AppPoolName -eq "Beta-WGSN-barometer") {
    $WarmUpUrl = "https://beta-barometer.morar.co"    
}
elseif ($AppPoolName -eq "Test-WGSN-barometer") {
    $WarmUpUrl = "https://test-barometer.morar.co"    
}
else {
    $WarmUpUrl = "https://demo.$Domain/$Product"
}

Write-Host "Warm up URL: $WarmUpUrl"

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# IIS will return a 503 response for up to around 30 s after deployment so we need to retry a few times
$MaxAttempts = 15
For ($i = 1; $i -le $MaxAttempts; $i++) {
    Write-Host "Calling warm up URL, attempt $i..."
    try {
        Invoke-WebRequest $WarmUpUrl -TimeoutSec 300 -UseBasicParsing
        Break
    }
    catch {
        if ($i -eq $MaxAttempts) {
            Throw
        }
        else {
            Start-Sleep -s 5
        }
    }
}
