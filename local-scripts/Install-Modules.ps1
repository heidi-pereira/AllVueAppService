param ([string[]] $ModuleNames)
$ErrorActionPreference = "Stop"

function Test-Admin {
    $currentUser = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    $currentUser.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if ((Get-PackageProvider | Where-Object { $_.Name -eq 'NuGet' -and $_.Version -ge 2.8.5.201 }).Length -eq 0) {
    if (Test-Admin) {
        Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force
    } else {
        $CommandLine = "-File `"" + $MyInvocation.MyCommand.Path + "`" " + $MyInvocation.UnboundArguments
        Start-Process -FilePath PowerShell.exe -Verb Runas -ArgumentList $CommandLine
    }
}

foreach ($module in $ModuleNames) {
    if (!(Get-Module -ListAvailable -Name $module)) {
        Write-Host "Installing module: $module"
        if (Test-Admin) {
            Install-Module -Name $module -Force -AllowClobber -Scope AllUsers
        } else {
            Install-Module -Name $module -Force -AllowClobber -Scope CurrentUser
        }
    }
    Import-Module $module -Force
}