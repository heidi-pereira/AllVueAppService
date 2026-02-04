Param([String] $storageAccountName = 'savantatechbackups', 
      [String] $containerName = 'database-backups',
      [String] $containerDirectory = "live/BrandVueMeta/", # 'testservers/sqltest$SQL2019/BrandVueMetaBeta/FULL/'
      [String] $targetDatabaseName = "BrandVueMeta", # 'BrandVueMetaBeta'
      [String] $databaseFileBase = 'BrandVueMeta'
      )
$ErrorActionPreference = "Stop"

. $PSScriptRoot\Install-Modules.ps1 -ModuleNames @('Az.Accounts', 'Az.Storage', 'SqlServer')

Update-AzConfig -DefaultSubscriptionForLogin 4e489b50-8ad1-4160-99d0-bc1b76e006b9

# Connect to Azure interactively
$context = Get-AzContext 
if (!$context) {
    Write-Host "The window to select account may appear behind the current window, try clicking the taskbar icon to minimize and unminimize if you can't see it"
    Connect-AzAccount  
}

# Get the storage account context
$storageAccount = Get-AzStorageAccount -ResourceGroupName (Get-AzStorageAccount | Where-Object StorageAccountName -eq $storageAccountName).ResourceGroupName -Name $storageAccountName
$storageContext = $storageAccount.Context

$sqlVersion = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL').PSObject.Properties | 
Sort-Object -Property Name -Descending | 
Select-Object -First 1 | 
Select-Object -ExpandProperty Name 

$sqlServerInstance = Get-ItemPropertyValue -Path 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL' -Name $sqlVersion

$sqlServerInstanceName = ("$($env:Computername)\" + $sqlVersion)

# Local path to save the backup file (without filename)
$localBackupFolder = Get-ItemPropertyValue -Path "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$sqlServerInstance\MSSQLServer" -Name 'BackupDirectory'

# Get the latest backup file from the container
$latestBackup = Get-AzStorageBlob -Container $containerName -Context $storageContext -Prefix $containerDirectory | 
    Where-Object { $_.Name -like "*.bak" } | 
    Sort-Object LastModified -Descending | 
    Select-Object -First 1

# Set the local backup path using the original filename
$localBackupPath = Join-Path $localBackupFolder $latestBackup.Name.Replace($containerDirectory,"")

# Check if the file already exists locally and compare its name with the remote file
$localFileExists = Test-Path $localBackupPath
$localFileUpToDate = $false

if ($localFileExists) {
    $localFile = Get-Item $localBackupPath
    $localFileUpToDate = ($localFile.Name -eq $latestBackup.Name.Replace($containerDirectory,""))
}

# Download the latest backup file only if it doesn't exist locally or is outdated
if (-not $localFileExists -or -not $localFileUpToDate) {
    Write-Host "Downloading the latest backup file..."
    Get-AzStorageBlobContent -Container $containerName -Blob $latestBackup.Name -Destination $localBackupPath -Context $storageContext
} else {
    Write-Host "The latest backup file already exists locally. Skipping download."
}

# Restore the database
$backupFolder = $(Get-ItemPropertyValue -Path "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$sqlServerInstance\MSSQLServer" -Name 'BackupDirectory')
$dataFolder = [IO.Path]::Combine($backupFolder,"..","Data")
$dataPath = Join-Path $dataFolder "$targetDatabaseName.mdf"
$logPath = Join-Path $dataFolder "$($targetDatabaseName)_log.ldf"
$query = @"
USE [master];

-- Set database to single user mode and close existing connections if it exists
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '$targetDatabaseName')
BEGIN
    ALTER DATABASE [$targetDatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
END


-- Restore the database
RESTORE DATABASE [$targetDatabaseName] 
FROM DISK = N'$localBackupPath' 
WITH FILE = 1,
    MOVE '$databaseFileBase' TO '$dataPath',
    MOVE '$($databaseFileBase)_log' TO '$logPath',
    REPLACE,
    STATS = 5;

-- Set database back to multi-user mode
ALTER DATABASE [$targetDatabaseName] SET MULTI_USER;
"@

try
{
    Invoke-Sqlcmd -ServerInstance $sqlServerInstanceName -Query $query -TrustServerCertificate
}
catch
{
    Write-Host "Database restore failed."
    if ($_ -match "Cannot find server certificate with thumbprint") {
        Write-Host
        Write-Host "Server certificate missing.  See this link for installing the certificate mentioned in the error below:"
        Write-Host "https://github.com/Savanta-Tech/TechWiki/wiki/SQL-Server-Backup-Encryption#to-restore-encrypted-databases"
    }
    Write-Host
    Write-Host "Error:"
    Write-Host "$_"
    Exit
}

Write-Host "Database restore completed successfully to $targetDatabaseName, ensure your connection string is pointing there"

# Optional: Remove the local backup file after restore
# Remove-Item $localBackupPath
