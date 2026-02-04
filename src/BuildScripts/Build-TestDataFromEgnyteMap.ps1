Push-Location ../DashboardBuilder

try {
	dotnet build DashboardBuilder.sln -c Release
	./DashboardBuilder/bin/Release/DashboardBuilder.exe build all
} finally {
	Pop-Location
}