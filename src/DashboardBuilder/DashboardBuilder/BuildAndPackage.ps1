param(
    [Parameter(Mandatory=$True)]
    [string]$DashboardBuilderVersion,# = $OctopusParameters['Octopus.Action[Deploy to server].Package.NuGetPackageVersion'],
    
    [Parameter(Mandatory=$True)]
    [string]$AppDirectoryPath,# = $OctopusParameters['Octopus.Action[Deploy to server].Output.Package.InstallationDirectoryPath'],
    
    [Parameter(Mandatory=$False)]
    [string]$DashboardBuilderCommands = 'all',
    
    [Parameter(Mandatory=$False)]
    [string]$EnvironmentPackageSuffix = '-dev'
)

Write-Host 'DashboardBuilderVersion = ' $DashboardBuilderVersion
Write-Host 'AppDirectoryPath = ' $AppDirectoryPath
Write-Host 'DashboardBuilderCommands = ' $DashboardBuilderCommands
Write-Host 'EnvironmentPackageSuffix = ' $EnvironmentPackageSuffix


$version = Get-Date -Format yyyy.MMdd.HHmm
$version += $EnvironmentPackageSuffix
$indexOfPrereleasePart = $DashboardBuilderVersion.IndexOf('-')

if ($indexOfPrereleasePart -gt -1) {
    $version += $DashboardBuilderVersion.Substring($indexOfPrereleasePart)
}

$exe = Join-Path $AppDirectoryPath 'DashboardBuilder.exe'
& $exe "build" $DashboardBuilderCommands.Split(',') "-PackageVersion=$version"