Param($machineName='.', $instanceName = 'SQL2017')

$db = @(gci data\*.bak)[0] | % {$_.BaseName}
$location = Get-Location

$backupLocation = $location.ToString() + "\data\" + $db + ".bak"
$dbWithEnvironment = $db

if ($OctopusParameters) {

    if ($OctopusParameters["DatabaseSuffix"]){
        $dbWithEnvironment = $db + "." + $OctopusParameters["DatabaseSuffix"]
    }

    $remoteBackupLocation = $OctopusParameters["RemoteBackupLocation"]

    if ($remoteBackupLocation){
        Write-Host "Copying $backupLocation to $remoteBackupLocation"
        Copy-Item $backupLocation -destination $remoteBackupLocation -force
        $backupLocation = $remoteBackupLocation + "\" + $db + ".bak"
    }
}

Write-Host "Restoring $backupLocation to $dbWithEnvironment"

$restoreSQL = @"

declare @databaseNameWithoutEnvironment nvarchar(50) = '$($db)'
declare @databaseName nvarchar(50) = '$($dbWithEnvironment)'
declare @backupLocation nvarchar(255) = '$($backupLocation)'

declare @dataPath nvarchar(255) = cast(serverproperty('InstanceDefaultDataPath') as nvarchar(255))
declare @logPath nvarchar(255) = cast(serverproperty('InstanceDefaultLogPath') as nvarchar(255))

declare @sql nvarchar(max) = '
IF (EXISTS (SELECT * 
     FROM sys.databases
     WHERE name = ''' + @databaseName + '''))
BEGIN
    ALTER DATABASE [' + @databaseName + '] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
END

RESTORE DATABASE [' + @databaseName + ']
FROM DISK=''' + @backupLocation + '''
WITH REPLACE, 
MOVE N''' + @databaseNameWithoutEnvironment + ''' TO N''' + @dataPath + @databaseName + '.mdf'',
MOVE N''' + @databaseNameWithoutEnvironment + '_Log'' TO N''' + @logPath + @databaseName + '_log.ldf''
'

EXEC (@sql)

"@

$restoreAirflowUserSQL = @"
USE [$($dbWithEnvironment)];
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'airflow_user')
BEGIN
    ALTER USER airflow_user WITH LOGIN = airflow_user;
END
"@

if ($OctopusParameters) {

    $instance = $OctopusParameters["SQLInstance"]
    $user = $OctopusParameters["SQLUser"]
    $password = $OctopusParameters["SQLPassword"]

} Else {
	If ($machineName -eq '.') {
		$availableInstances = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server' -ErrorAction SilentlyContinue).InstalledInstances
		If ($availableInstances.Contains($instanceName)) {
			Write-Host "Detected expected SQL Server instance."
		} ElseIf ($availableInstances.Count -eq 1) {
			$instanceName = ($availableInstances | Select -First 1)
			Write-Host "Detected single SQL Server instance."
		} Else {
			Write-Warning "Did not detect expected SQL Server instance $instanceName, nor was a single instance detected. Expect a failure shortly..."
		}
	}
	
	$instance =  $machineName + "\" + $instanceName -replace 'MSSQLSERVER',''
	Write-Host "Will restore to SQL Server instance '$instance'."
}

if ($instance) {
    if ($user) {
        SQLCMD -S $instance -Q $restoreSQL -U $user -P $password
        SQLCMD -S $instance -Q $restoreAirflowUserSQL -U $user -P $password
    }
    else {
        SQLCMD -S $instance -E -Q $restoreSQL
        SQLCMD -S $instance -E -Q $restoreAirflowUserSQL
    }
}
