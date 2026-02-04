param(
    [Parameter(Mandatory)][string]$baseUrl, #Same as dashboard url e.g. "https://demo.all-vue.com/eatingout/"
    [Parameter(Mandatory)][string]$apiKey,
    [DateTime]$startDate='2019-10-01',
    [DateTime]$endDate=$startDate.AddMonths(1).AddDays(-1),
    [string]$surveyset = 'UK',
    [string]$fileSuffix = $startDate.ToString('yyyy-MM-dd') + '-' + $endDate.ToString('yyyy-MM-dd'),
    [string]$respondentsCsv = "./Respondents-$surveyset-$fileSuffix.csv",
    [string]$responsesCsv = "./Responses-$surveyset-$fileSuffix.csv"
)
$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Get-SurveyProfileRespondents ([String]$surveyset, [System.DateTime]$date) {
    $dateString = $date.ToString("yyyy-MM-dd")
    Write-Host "Fetching respondents for $dateString"
    $url = $baseUrl + "/api/surveysets/$surveyset/profile/answers/$dateString"
    return Invoke-RestMethod $url -Headers @{Authorization = "Bearer $apiKey"} | ConvertFrom-Csv
}

function Get-SurveyClassResponses ([String]$surveyset, [String]$class, [DateTime]$date) {
    $dateString = $date.ToString("yyyy-MM-dd")
    Write-Host "Fetching answers for $dateString"
    $url = $baseUrl + "/api/surveysets/$surveyset/classes/$class/answers/$dateString?includeText=true"
    return Invoke-RestMethod $url -Headers @{Authorization = "Bearer $apiKey"} | ConvertFrom-Csv
}

For ($date = $startDate; $date -le $endDate; $date = $date.AddDays(1)) {
    Get-SurveyProfileRespondents -surveyset $surveyset -date $date | Export-Csv -Path $respondentsCsv -Append -NoTypeInformation
    Get-SurveyClassResponses -surveyset $surveyset -class 'brand' -date $date | Export-Csv -Path $responsesCsv -Append -NoTypeInformation
}