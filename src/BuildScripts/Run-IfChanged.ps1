Param($filePath, $cacheFolder, $commandToRun, [Parameter(ValueFromRemainingArguments=$true)] [String[]]$commandArgs)
# ./Run-IfChanged package.json node_modules npm install --prefer-offline #Npm install example

$targetFilename = [System.IO.Path]::GetFileName($filePath)
$filePath = Join-Path "$PSScriptRoot\.." $filePath
$hashFilePath = (Join-Path "$PSScriptRoot\.." (Join-Path $cacheFolder $targetFilename)) + '.hash'

function Run-Command() {
	$null = & $commandToRun @commandArgs 2>&1 | Out-String
	Write-Host "`r`n"
	return $LASTEXITCODE
}

function Write-HashIfSuccessful($exitCode) {
	if ($exitCode -eq 0) {
		Write-Host "Exit code was $exitCode, caching for next time"
		$null = New-Item -Path $hashFilePath -Type file -Force
		$currentFileHash | Set-Content $hashFilePath
	} else {
		Write-Host "Exit code was $exitCode, not caching"
	}
}

if (Test-Path $filePath) {

	$currentFileHash = (Get-FileHash $filePath).Hash

	if (Test-Path $hashFilePath) {
		$previousFileHash = [IO.File]::ReadAllText($hashFilePath).Trim()
		if ($previousFileHash -ne $currentFileHash) {
			Write-Output "Running $commandToRun because $targetFilename has changed"
			Write-HashIfSuccessful (Run-Command)
		} else {
			Write-Output "Skipping $commandToRun because $targetFilename hasn't changed"
		}
	} else {
		Write-Output "Running $commandToRun because the hash of $filePath isn't stored at $hashFilePath"
		Write-HashIfSuccessful (Run-Command)
	}

} else {
	Write-Warning "Running $commandToRun because $filePath doesn't exist. The next call will not be optimized."
	Run-Command
}