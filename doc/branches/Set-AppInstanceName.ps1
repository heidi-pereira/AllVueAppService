# Not deployed. This is a (likely out of date) copy of the script from the deployment process: https://savanta.octopus.app/app#/Spaces-1/projects/brandvue/deployments/process/steps?actionId=ead070df-53d3-407e-9ac3-67ea4ca40050&parentStepId=10b34636-b416-42cf-b42b-c583f49ab4f2


$version = $OctopusParameters["Octopus.Release.Number"]
$environment = $OctopusParameters["Octopus.Environment.Name"]
$channel = $OctopusParameters["Octopus.Release.Channel.Name"]
Write-Host "Package Version: $version"
Write-Host "Package Channel: $channel"
Write-Host "Environment file: $environment"


$appInstanceName = ""
$parentSite= ""
if ($channel -ne "Default") {
	$appInstanceName = "-"+$version
	$isPreRelease = $version -match "-heads-(?<name>.*$)"    
	if ($isPreRelease) {
        $appInstanceName = "-"+$matches['name']
	} 
    $isPR = $version -match "-pull-(?<name>.*)-merge$"
	if ($isPR) {
        $appInstanceName = "-"+$matches['name']
	} 
	else 
	{
		$isPR = $version -match "-pull-(?<name>.*)$"
		if ($isPR) {
			$appInstanceName = "-"+$matches['name']
		} else {
        	$version -match "-(?<name>.*)$"
        	$appInstanceName = "-"+$matches['name']
        }
	}
    $appInstanceName = $appInstanceName.Substring(0, [Math]::Min(40, $appInstanceName.Length))
}

Set-OctopusVariable -name "AppInstanceName" -value $appInstanceName
Write-Output "AppInstanceName: $appInstanceName"
