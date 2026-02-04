#
# pre-build.ps1
#

param(
        # The current build number without branch
        [Parameter(Mandatory = $true)]
        [String] $ThreePartBuildNumber,

        # The name of the current source control branch. e.g. 'master' or 'my-feature'. This is only used when IsDefaultBranch is false, in order to determine the pre-release version suffix. If the branch name is too long, this cmdlet will try to shorten it to satisfy the 20 character limit for the pre-release suffix. Nonetheless, you should try to avoid long branch names.
        [Parameter(Mandatory = $true)]
        [string] $BranchName,

        # Indicates whether or not BranchName represents the default branch for the source control system currently in use. Please note that this is not a switch parameter - you must specify this value explicitly.
        [Parameter(Mandatory = $true)]
        $IsDefaultBranch
    )
$IsDefaultBranch = $IsDefaultBranch -eq $true -Or $IsDefaultBranch -eq "true"; #Cater for bool or string
	
Import-Module $PSScriptRoot\New-NugetPackageVersion.psm1 -Verbose;

#From https://github.com/MIG-Global/Survey-Platform/blob/master/pre-build.ps1#L12
$clubhouseRegex = [regex]'[a-zA-Z.-]+\/([^\/]+)\/[a-zA-Z.-]+';
if ($clubhouseRegex.IsMatch($BranchName)) {
    $clubhouseId = $clubhouseRegex.Match($BranchName).Groups[1].Value;
    $BranchName = "clubhouse-$clubhouseId";
}

Write-Host "Base: $ThreePartBuildNumber";

$buildNumber = New-NugetPackageVersion $ThreePartBuildNumber $BranchName $IsDefaultBranch

Write-Host "##teamcity[buildNumber '$buildNumber']";

Write-Host "Updating AssemblyInfos"
Update-AllAssemblyInfoFiles $ThreePartBuildNumber;