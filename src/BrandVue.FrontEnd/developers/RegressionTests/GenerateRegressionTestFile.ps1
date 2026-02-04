param(
	[string]$urlPath="",
	[string]$name="newRegressionTest",
	[string]$baseUrl="http://localhost:8082/",
	[string]$urlFilePath=""
)

#This function should generate a Json test file from an api url
function GenerateFile {
	param(
		[string]$urlPath="",
		[string]$name="newRegressionTest",
		[string]$baseUrl="http://localhost:8082/"
	)

	$newTestModel = Invoke-RestMethod "$baseUrl$urlPath"

	$newTestModel | Add-Member -MemberType NoteProperty -TypeName "String" -Name "requestedUrl" -Value "$urlPath"

	$newTestModelJsonString = ConvertTo-Json $newTestModel -Depth 16

	$newTestModelJsonString > "$name.json"
}

if($urlPath -eq "")
{
	if($urlFilePath -eq "")
	{
		"urlPath or urlFilePath must be provided"
	}
	else
	{
		#load up ech line of the file with this format [{testName},{url}] and generate a json test file.
		foreach($line in Get-Content $urlFilePath) 
		{
			$fileName,$url = $line.split(',')
			GenerateFile -urlPath $url -name $fileName -baseUrl $baseUrl
		}
	}
}
else
{
	GenerateFile -urlPath $urlPath -name $name -baseUrl $baseUrl
}
