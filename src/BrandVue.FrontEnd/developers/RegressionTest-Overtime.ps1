# Ensure overtime UI api data match known ones
param(
	[string]$baseUrl="http://localhost:8082/",
	[string]$TestJsonFile="",
	[string]$TestJsonFolder="",
	[string]$TestJsonFolderFilter="*.json"
)

function TestOverJsonFile {
	param(
		[string]$baseUrl="",
		[string]$JsonFile=""
	)

	"Regression test for the file [$JsonFile] started"

	# the json file holds informations about the test that shuld run
	$testModel = Get-Content -Raw -Path $JsonFile | ConvertFrom-Json
	$requestedUrl = $testModel.requestedUrl

	#builds the full url to get the data from, the test model holds only the sub path of the url, the root should be passed as a paramenter in baseUrl
	$fullUrl = "$baseUrl$requestedUrl"

	"testing url: [$fullUrl]"

	#loads up the url json string and parse it into an object
	$liveModel = Invoke-RestMethod "$fullUrl"

	#it takes the inner object brandWeightedDailyResults (the only part of the data that we want to compare) from both the test model and the new downloaded set of data
	$testJsonString = ConvertTo-Json $testModel.brandWeightedDailyResults -Depth 15
	$liveJsonString = ConvertTo-Json $liveModel.brandWeightedDailyResults -Depth 15

	#the test model and the new downloaded set of data should be the same, if not he test is failed
	if($testJsonString -eq $liveJsonString)
	{
		"Regression test for the file [$JsonFile] succeeded"
	}
	else 
	{
		throw "Regression test [$JsonFile] failed, url [($fullUrl)] returned different data."
	}
}


if($TestJsonFile -eq "")
{
	if($TestJsonFolder -eq "")
	{
		throw "TestJsonFolder or TestJsonFile parameters must be provided"
	}
	else
	{
		#get each .json file in the target folder and run the test function over it.
		Get-ChildItem $TestJsonFolder -Filter $TestJsonFolderFilter | 
			Foreach-Object {
				$_.FullName
				TestOverJsonFile -JsonFile $_.FullName -baseUrl $baseUrl
			}
	}
}
else
{
	TestOverJsonFile -JsonFile $JsonFile -baseUrl $baseUrl
}