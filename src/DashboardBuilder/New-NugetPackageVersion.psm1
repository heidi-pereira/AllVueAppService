#requires -Version 2
# From https://github.com/red-gate/RedGate.Build/blob/b4673065292ec9dec3514ae01be54b8fdc64047b/Public/Nuget/New-NuGetPackageVersion.ps1
<#
.SYNOPSIS
  Obtains a NuGet package version based on the build version number and branch name.
.DESCRIPTION
  Obtains a NuGet package version based on a 3 or 4-digit build version number, the branch name and whether or not the branch is the default branch.
.OUTPUTS
  A NuGet version string based on the input parameters. The string is also suitable for use as an assembly's AssemblyInformationalVersion attribute value.
.EXAMPLE
  New-NuGetPackageVersion -Version '1.2.3.4' -BranchName 'master' -IsDefaultBranch $True

  Returns '1.2.3.4'. This shows how this cmdlet might be invoked on the default master branch with a four digit version number.
.EXAMPLE
  New-NuGetPackageVersion -Version '1.2.3.4' -BranchName 'SomeBranch' -IsDefaultBranch $False

  Returns '1.2.3.4-SomeBranch'. This shows how this cmdlet might be invoked on a feature branch, resulting in a pre-release version string.
.EXAMPLE
  New-NuGetPackageVersion -Version '1.2.3' -BranchName 'SomeBranch' -IsDefaultBranch $False

  Returns '1.2.3-SomeBranch. This shows how this cmdlet might be invoked on a semantically versioned feature branch, resulting in a pre-release version string.
#>
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

Export-ModuleMember -Function New-NuGetPackageVersion