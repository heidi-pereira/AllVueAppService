param(
    $leftCsvPath,
    $rightCsvPath,
    $leftJoinField='Profile_Id',
    $rightJoinField=$leftJoinField,
    $outCsv='./joined.csv'
)

if ((Get-InstalledModule -Name 'Join-Object' -ErrorAction SilentlyContinue) -eq $null) {
    if (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) { Start-Process powershell.exe "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs; exit }
    Install-Module -Name Join-Object
}

$left = Import-Csv $leftCsvPath
$right =  Import-Csv $rightCsvPath
$joined = Join-Object -Left $left -Right $right -LeftJoinProperty $leftJoinField -RightJoinProperty $rightJoinField -Type AllInBoth -LeftMultiMode 'DuplicateLines' -RightMultiMode 'DuplicateLines'
$joined | Export-Csv -Path $outCsv -Append -NoTypeInformation
