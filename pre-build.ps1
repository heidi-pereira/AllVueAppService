#
# pre-build.ps1
#

param(
        # The current build number
        [Parameter(Mandatory = $true)]
        [String] $BuildNumberWithoutBranch,

        # The name of the current source control branch. e.g. 'master', 'main' or 'my-feature'. This is only used when IsDefaultBranch is false, in order to determine the pre-release version suffix. If the branch name is too long, this cmdlet will try to shorten it to satisfy the 20 character limit for the pre-release suffix. Nonetheless, you should try to avoid long branch names.
        [Parameter(Mandatory = $true)]
        [string] $BranchName,

        # Indicates whether or not BranchName represents the default branch for the source control system currently in use. Please note that this is not a switch parameter - you must specify this value explicitly.
        [Parameter(Mandatory = $true)]
        $IsDefaultBranch
    )

Import-Module $PSScriptRoot\SharedBuildScripts\SharedPreBuild.psm1 -Verbose;

TeamcitySharedPreBuild -branch $BranchName -buildNumberWithoutBranch $BuildNumberWithoutBranch
