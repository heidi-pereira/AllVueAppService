# Not deployed. This is a (likely out of date) copy of the script from the deployment process: https://savanta.octopus.app/app#/Spaces-1/library/steptemplates/ActionTemplates-21

$ErrorActionPreference = "Stop"

$featureBranchApps = (Get-WebApplication -Site $BranchCleanUp_RootTestSite) | 
	Where { $BranchCleanUp_OnlyDeleteAppsContaining.Length -eq 0 -or $_.path.Contains($BranchCleanUp_OnlyDeleteAppsContaining) }
$cleanUpBefore = (Get-Date).AddDays(-7)


if (-not $BranchCleanUp_RootTestSite.Contains('Test')) {
	throw "Cannot clean non test site '$BranchCleanUp_RootTestSite'" 
} else {
	$oldApps = $featureBranchApps | Where { -not (Test-Path $_.PhysicalPath) -or (Get-Item $_.PhysicalPath).LastWriteTime -lt $cleanUpBefore }
    $oldApps | % { 
      try {
        $toRemove = $_.path.TrimStart("/")
		Write-Host "Removing $toRemove"
		Remove-WebApplication -Site $BranchCleanUp_RootTestSite -Name $toRemove
		if ($AppPoolPerBranch) {
			$appPoolToRemove = $_.applicationPool
			Write-Host "Removing App Pool $appPoolToRemove"
			Remove-WebAppPool -Name $appPoolToRemove
		}
      } catch {
      	Write-Warning $_
      }
    }
}

$branches = $featureBranchApps |
    Sort-Object { (Get-Item $_.PhysicalPath).LastWriteTime } -Descending |
    % { $_.path }

if($ShouldAppendFile -eq $true) {
	Add-Content -Path $BranchCleanUp_FilePath -Value $branches
}
else {
	$branches > $BranchCleanUp_FilePath
}

