#Requires -RunAsAdministrator
#Run this as admin if your inetpub needs admin rights, or if you need the website adding

Param($iisAppName = "Debug-WGSN", $directoryPath = "C:\inetpub\$iisAppName", $port = "3003",
 $solutionPath = "$PSScriptRoot\..\BrandVue.sln", $nupkgPath = "$PSScriptRoot\..\BrandVue.FrontEnd\bin\BrandVue.1.0.0.nupkg", [Switch] $StartBrowser = $false)

Add-Type -AssemblyName System.IO.Compression.FileSystem
function Unzip([string]$zipfile, [string]$outpath)
{
	$overwritableDirectoryMarker = "$outpath\.overwritewithnoconfirm"
	if (!(Test-Path $overwritableDirectoryMarker)) {
		Remove-Item $outpath
	} else {
		Remove-Item $outpath -Recurse
	}
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
	New-Item $overwritableDirectoryMarker -type file
}

function Set-VsContext() {
	# https://stackoverflow.com/a/2124759/1128762
	Push-Location "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\Tools"
	cmd /c "VsDevCmd.bat&set" |
	foreach {
	  if ($_ -match "=") {
		$v = $_.split("="); set-item -force -path "ENV:\$($v[0])"  -value "$($v[1])"
	  }
	}
	Pop-Location
	Write-Output "`nVisual Studio 2017 Command Prompt variables set."
}

function Update-WebConfigToGetDataFrom($webConfig, $basePath) {
	[xml]$xml = Get-Content $webConfig

	# Remove the auth related stuff that breaks my cut-down IIS install
	$handlersNode = $xml.SelectSingleNode('//configuration/system.webServer/handlers')
	$handlersNode.ParentNode.RemoveChild($handlersNode)
	
	(Select-Xml -xml $xml  -XPath '//configuration/appSettings/add').Node | where {$_.key.Contains('base')} | foreach-object { 
		$_.value = $_.value.Replace('..\..', "$basePath")
	}      

	$xml.save($webConfig)
}

Set-VsContext
& msbuild $solutionPath "/t:restore;build" "/m" "/property:Configuration=Debug;RunOctoPack=true;OctoPackEnforceAddingFiles=true"

Unzip $nupkgPath $directoryPath

Copy-Item "$PSScriptRoot\..\..\testdata" "$directoryPath\testdata" -recurse

Update-WebConfigToGetDataFrom (Join-Path $directoryPath "Web.config") "$directoryPath"

. $PSScriptRoot\Create-Website.ps1 $port $iisAppName $directoryPath

if ($StartBrowser) {
	Write-Output "Starting browser pointing at http://localhost:$port"
	Start-Process "http://localhost:$port"
} else {
	Write-Output "Visit http://localhost:$port, or run Start-Process http://localhost:$port"
}

