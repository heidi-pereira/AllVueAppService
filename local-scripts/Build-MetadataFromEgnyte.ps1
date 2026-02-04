
#Usage example:
# Get list of product names from https://savanta.octopus.app/app#/Spaces-1/library/builtinrepository
# Download just for drinks: .\Build-MetadataFromEgnyte.ps1 'drinks'
Param([String[]] $ProductNames = @('all'))

dotnet build $PSScriptRoot/../src/DashboardBuilder/DashboardBuilder/DashboardBuilder.csproj

foreach($product in $ProductNames) {
    try {
    	Push-Location $PSScriptRoot/../src/DashboardBuilder/DashboardBuilder/bin/Debug/
        ./DashboardBuilder build $product
    } catch {
        Write-Error "Could not update metadata for $product`r`n$_"
    } finally {
    	Pop-Location
    }
}
