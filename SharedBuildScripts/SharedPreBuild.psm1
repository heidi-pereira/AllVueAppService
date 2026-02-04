
function TeamcitySharedPreBuild
{
  Param (
    [Parameter(Mandatory=$true)] $branch,
    $majorVersion,
    $minorVersion,
    $buildCounter,
    [Version] $buildNumberWithoutBranch = "${major}.${minor}.${buildCounter}"
  )

  Write-Host "TeamcitySharedPreBuild buildNumberWithoutBranch=$buildNumberWithoutBranch branch=$branch";

  $buildNumber = New-NuGetPackageVersion $buildNumberWithoutBranch $branch ($branch -eq "master")
  
  Write-Host "##teamcity[buildNumber '$buildNumber']";
  Write-Host "Build number set to $buildNumber"
}

function New-NuGetPackageVersion
{
    [CmdletBinding()]
    param(
        # A three or four digit version number of the form Major.Minor.Patch.Revision.
        [Parameter(Mandatory = $true)]
        [version] $Version,

        # The name of the current source control branch. e.g. 'master' or 'my-feature'. This is only used when IsDefaultBranch is false, in order to determine the pre-release version suffix. If the branch name is too long, this cmdlet will try to shorten it to satisfy the 20 character limit for the pre-release suffix. Nonetheless, you should try to avoid long branch names.
        [Parameter(Mandatory = $true)]
        [string] $BranchName,

        # Indicates whether or not BranchName represents the default branch for the source control system currently in use. Please note that this is not a switch parameter - you must specify this value explicitly.
        [Parameter(Mandatory = $true)]
        [bool] $IsDefaultBranch
    )

    # If this is the default branch, there's no pre-release suffix. Just return the version number.
    if ($IsDefaultBranch)
    {
        return [string]$Version
    }
    elseif (-not $BranchName)
    {
        throw 'BranchName must be specified when IsDefaultBranch is false'
    }

    # Otherwise establish the pre-release suffix from the branch name.
    $PreReleaseSuffix = $BranchName

    if (-not $PreReleaseSuffix.StartsWith('hotfix')) {
        # Remove anything preceding numeric id like "/ch00" or "/00" so more of the info is in the suffix
        $PreReleaseSuffix = $PreReleaseSuffix -replace '(?:.+\/(?=(?:ch|)[0-9]+\/)|)(.*)', '$1'
    }
    # Remove invalid characters from the suffix.
    $PreReleaseSuffix = $PreReleaseSuffix -replace '[/]', '-'
    $PreReleaseSuffix = $PreReleaseSuffix -replace '[^0-9A-Za-z-]', ''

    # Shorten the suffix if necessary, to satisfy NuGet's 20 character limit.
    if ($PreReleaseSuffix.Length -gt 20) {
        $PreReleaseSuffix = $PreReleaseSuffix.SubString(0, 20)
    }

    # And finally compose the full NuGet package version - this supports 3 part version numbers
    return "$Version-$PreReleaseSuffix"
}

Export-ModuleMember -Function TeamcitySharedPreBuild;
