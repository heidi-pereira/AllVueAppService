# Ensure results match known ones
param(
	[string]$baseUrl='http://localhost:8082',
	[string]$apiKey='ThisIsTheDebugApiKey',
	[string]$instanceName='.\sql2022',
	[string]$sqlUser,
	[string]$sqlPassword,
	[string]$databaseNameToCreate = 'zTmpTestBrandVueSurveyApi',
	[Int32]$year = 2025,
	[Int32]$month = 1,
    $expectedResult = 82.9,
    $expectedSampleSize = 1205,
	$desiredBrandId = 22
)

#On error stop
$ErrorActionPreference = "Stop"

try {
	Write-Host "Running familiarity tests for month: $year/$month, brand: $desiredBrandId."
	Write-Host "Expecting to get average result: $expectedResult, sample size: $expectedSampleSize."

    $scriptRoot = "$PSScriptRoot/../wwwroot/developers"
	. "$scriptRoot/Create-BrandVueDatabaseFromApi.ps1" -baseUrl $baseUrl -apiKey $apiKey -instanceName $instanceName -sqlUser $sqlUser -sqlPassword $sqlPassword -databaseNameToCreate $databaseNameToCreate -year $year -month $month
	. "$scriptRoot/SqlServerGetFinalValues.ps1" -instanceName $instanceName -sqlUser $sqlUser -sqlPassword $sqlPassword -databaseNameToReadFrom $databaseNameToCreate

	Write-Host "Getting MetricResult and SampleSize for Familiarity Metric..."
	$actual =  Calculate-FamiliaritySampleSizeAndAverage $desiredBrandId

	# Print all properties of $actual
	Write-Host ($actual | Format-Table | Out-String)

	$sampleSize = $actual.SampleSize
	$metricResult = $actual.MetricResult
	$metricRound = [math]::Round($metricResult*100,1)

	$isResultCorrect = $metricRound -eq $expectedResult -and $sampleSize -eq $expectedSampleSize
	if(!$isResultCorrect)
	{
		throw "Expected ($expectedResult, $expectedSampleSize) does not match actual ($metricRound, $sampleSize)"
	}
} catch {
    Write-Warning $_.Exception | format-list -force
    throw $_
}

Write-Host "Succeeded at $(Get-Date)"