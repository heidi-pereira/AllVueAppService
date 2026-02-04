param(
	[string]$baseUrl = "Paste_Your_Base_Url_Here",
	[string]$apiKey = "Paste_Your_Api_Key_Here"
)

function Get-SurveyClassResponses ([String]$surveyset, [String]$class, [System.DateTime]$date) {
    $dateString = $date.ToString("yyyy-MM-dd")
	Write-Host "Fetching answers for $dateString"
	$url = $baseUrl + "/api/surveysets/$surveyset/classes/$class/answers/$dateString"
	return Invoke-RestMethod $url -Headers @{Authorization = "Bearer $apiKey"} | ConvertFrom-Csv | foreach {
		 [pscustomobject] [ordered] @{
			ProfileId = $_.Profile_Id
			BrandId = $_.Brand_Id
			Consider = $_.Consider_General
			ConsumerSegment = $_.Consumer_Segment
			Familiarity = $_.Brand_Advantage
		}
	}
}

function Get-SurveyProfileRespondents ([String]$surveyset, [System.DateTime]$date) {
    $dateString = $date.ToString("yyyy-MM-dd")
	Write-Host "Fetching respondents for $dateString"
    $url = $baseUrl + "/api/surveysets/$surveyset/profile/answers/$dateString"
	return Invoke-RestMethod $url -Headers @{Authorization = "Bearer $apiKey"} | ConvertFrom-Csv | foreach {
		 [pscustomobject] [ordered] @{
			ProfileId = $_.Profile_Id
			COMPROUTE = $_.COMPROUTE
            StartTime = $_.Start_Date
            WeightingCellId = $_.Weighting_Cell_Id
		}
	}
}

function Get-Weightings ([String]$surveyset, [System.DateTime]$date) {
    $dateString = $date.ToString("yyyy-MM-dd")
	Write-Host "Fetching weightings for $dateString"
    $url = $baseUrl + "/api/surveysets/$surveyset/averages/Monthly/weights/$dateString"
	Write-Host $url
	return Invoke-RestMethod $url -Headers @{Authorization = "Bearer $apiKey"} | select -ExpandProperty value | foreach {
		 [pscustomobject] [ordered] @{
			WeightingCellId = $_.weightingCellId
 			Multiplier = $_.multiplier
		}
	}
}