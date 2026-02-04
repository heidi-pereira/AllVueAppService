# For the defaults to work, just run this script from the bin/Release directory so it's adjacent to the exe
Param(
$commaSeparatedEgnyteBearerTokens,
$outputBaseDirectory = "LatestMetadata",
$nuget = ".\nuget.exe",
$packageId = "DashboardBuilder.Metadata",
$dashboardMetadataBuilder = ".\DashboardBuilder.exe",
$packageSource = 'https://teamcity.morar.co/httpAuth/app/nuget/v1/FeedService.svc/'
)
$ErrorActionPreference = "Stop"

$downloadLatestMetadataScriptPath = Join-Path $PSScriptRoot 'Download-LatestMetadata.ps1'
. $downloadLatestMetadataScriptPath $outputBaseDirectory $nuget $packageId $dashboardMetadataBuilder $packageSource
& $dashboardMetadataBuilder @('packagemapdirectories', $commaSeparatedEgnyteBearerTokens, $outputBaseDirectory, $packageId)

Exit $LASTEXITCODE