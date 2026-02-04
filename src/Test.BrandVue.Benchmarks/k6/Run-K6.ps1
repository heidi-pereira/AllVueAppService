Param([Switch] $WriteResults = $false, [String] $Product = "retail")

$resultFilename = (Get-Date -uformat "%Y-%m-%d %H%-%M-%S")
$resultFilename += ' ' + (git branch --show-current) + ' ' + (git show -s --format="%H")

if ($null -eq (Get-Command 'k6')) {
    winget install k6 --source winget
}

mkdir ./results -ErrorAction SilentlyContinue

$k6Command = 'k6 run script.js --env RESULT_FILENAME=$resultFilename --env PRODUCT=$Product'
if ($WriteResults) {
    $k6Command += '--out csv=./results/$resultFilename.csv --out json=./results/$resultFilename.json --env MY_USER_AGENT="hello"'
}

Invoke-Expression $k6Command