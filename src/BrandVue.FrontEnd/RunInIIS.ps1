Param($portNumber = 8082, $browserPath="Chrome.exe")
$PSScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
$timestamp = Get-Date -Format "HH:mm:ss"
Write-Host "At $timestamp, starting site in IIS express from $PSScriptRoot"
Start-Process "Chrome.exe" "http://localhost:$portNumber"
& 'C:\Program Files\IIS Express\iisexpress.exe'  @("/path:$PSScriptRoot", "/port:$portNumber")
