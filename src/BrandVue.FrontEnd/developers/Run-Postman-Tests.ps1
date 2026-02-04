# Please find postmanApiKey in your (or any other member of the team workspace) Postman account
# Please find correct collection uid by calling this endpoint https://api.getpostman.com/collections
# Please find correct environment uid by calling this endpoint https://api.getpostman.com/environments

param(
	[Parameter(Mandatory)]
	[string]$postmanApiKey,
	[Parameter(Mandatory)]
	[string]$collectionUid,
	[Parameter(Mandatory)]
	[string]$environmentUid,
	[string]$baseUrlOverride
)

# This is the script to run Postman test collection for Survey Response API. It's trial technology for testing API. 
# !!!Important!!! If we agreed to use it as a main technology for testing we need to update https://github.com/MIG-Global/TechWiki/wiki/Systems-You-Need-Access-To
# You can find collections of tests in your Postman account https://identity.getpostman.com/login?addAccount=1 (be sure that your account is included to the Team workspace)
# Please use Postman API to be sure what collection (https://api.getpostman.com/collections) with what environment (https://api.getpostman.com/environments) you would like to test

$collectionUrl = "https://api.getpostman.com/collections/" + "$collectionUid"+'?' +"apikey=$postmanApiKey"
$environmentUrl = "https://api.getpostman.com/environments/" +"$environmentUid"+'?' +"apikey=$postmanApiKey"

# Make console output ignores warnings
$WarningPreference = 'SilentlyContinue'

Write-Host "***Installing choco***"
if (-not (Get-Command "choco" -ErrorAction Silent)) {
    # Get Chocolatey which is required for the rest of this script
    iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
}

Write-Host "***Installing npm***"
choco upgrade nodejs.install --version 12.8.0 -y

# Newman is a command line Collection Runner for Postman. It allows you to run and test a Postman Collection directly from the command line
Write-Host "***Installing newman***"
npm install newman -g

Write-Host "Collection Url is $collectionUrl"
Write-Host "Environment Url is $environmentUrl"

try {
	if ($baseUrlOverride -eq "") {
		return newman run $collectionUrl --environment $environmentUrl
	}
	##Override the baseUrl environment variable if it is provided as a parameter
	Write-Host "Url override is set to $baseUrlOverride"
	return newman run $collectionUrl --environment $environmentUrl --env-var "baseUrl=$baseUrlOverride"
} catch {
    Write-Warning $_.Exception | format-list -force
    throw $_
}

