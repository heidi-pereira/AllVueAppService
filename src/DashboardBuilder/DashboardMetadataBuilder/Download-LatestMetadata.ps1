# For the defaults to work, just run this script from the bin/Release directory so it's adjacent to the exe
Param(
$outputBaseDirectory = "LatestMetadata",
$nuget = ".\nuget.exe",
$packageId = "DashboardBuilder.Metadata",
$dashboardMetadataBuilder = ".\DashboardMetadataBuilder.exe",
$packageSource = 'https://teamcity.morar.co/httpAuth/app/nuget/v1/FeedService.svc/'
)
$ErrorActionPreference = "Stop"

Write-Output "Installing the last version of the metadata in $outputBaseDirectory"
New-Item -ItemType Directory -Path $outputBaseDirectory -Force
& $nuget @('install', $packageId, '-ExcludeVersion', '-Source', "$($env:USERPROFILE)\.nuget\packages\", '-FallbackSource', $packageSource, '-OutputDirectory', $outputBaseDirectory)

Exit $LASTEXITCODE