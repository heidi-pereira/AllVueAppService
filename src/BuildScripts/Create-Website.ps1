#Requires -RunAsAdministrator
Param($port="3003", $iisAppName = "Debug-WGSN", $directoryPath = "C:\inetpub\$iisAppName")

Import-Module WebAdministration

#navigate to the sites root
Push-Location IIS:\Sites\
try {
	#check if the site exists
	if (!(Test-Path $iisAppName -pathType container))
	{
		New-Item $iisAppName -bindings @{protocol="http";bindingInformation=":"+$port+":localhost"} -physicalPath $directoryPath
	}
} finally {
    Pop-Location
}
