#Make the API calls for a specific brand
#Save CSV results in a database.
#Run a query to check that the weighting is applied correctly
param(
	[Parameter(Mandatory)]
	[string]$baseUrl,
	[Parameter(Mandatory)]
	[string]$apiKey,
	[Int32]$year = 2025,
	[Int32]$month = 1,
	[Parameter(Mandatory)]
	[string]$instanceName,
	[string]$sqlUser,
	[string]$sqlPassword,
	[string]$databaseNameToCreate = 'BrandVueDataFromApi'
)

#On error stop
$ErrorActionPreference = "Stop"

#Enforce TLS 1.2
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

#Load Api Shim
. "$PSScriptRoot/BrandVueApi-Shim.ps1" -baseUrl $baseUrl -apiKey $apiKey

#Load Sql Store
. "$PSScriptRoot/SqlServerDatabase.ps1" -instanceName $instanceName -sqlUser $sqlUser -sqlPassword $sqlPassword -databaseNameToCreate $databaseNameToCreate

#Constants
$surveyset = 'UK'
$date = Get-Date -Year $year -Month $month -Day 01

#Create DB Stucture to hold the data
Write-Host "Creating Database..."
Create-InitialTableStructure
Write-Host "Success"

#Csv paths
$weightingsCsv = "./Weightings.csv"
$respondentsCsv = "./Profiles.csv"
$responsesCsv = "./Responses.csv"

try {
	Get-Weightings -surveyset $surveyset -date $date | Export-Csv -Path $weightingsCsv -Append -NoTypeInformation

	#Get responses and respondents for each day of the month and pipe to csv file
	$daysInMonth = [DateTime]::DaysInMonth($date.year, $date.month)
	For ($i=0; $i -lt $daysInMonth; $i++) {
		$thisDate = $date.AddDays($i)
		Get-SurveyProfileRespondents -surveyset $surveyset -date $thisDate | Export-Csv -Path $respondentsCsv -Append -NoTypeInformation
		Get-SurveyClassResponses -surveyset $surveyset -class 'brand' -date $thisDate | Export-Csv -Path $responsesCsv -Append -NoTypeInformation
	}

	BulkInsertToDB -weightingsCsvPath $weightingsCsv -respondentsCsvPath $respondentsCsv -responsesCsvPath $responsesCsv
}
finally {
	#Clean up
	Write-Host "Cleaning temporary csv files"
	if (Test-Path $weightingsCsv) { Remove-Item $weightingsCsv }
	if (Test-Path $respondentsCsv) { Remove-Item $respondentsCsv }
	if (Test-Path $responsesCsv) { Remove-Item $responsesCsv }
}
