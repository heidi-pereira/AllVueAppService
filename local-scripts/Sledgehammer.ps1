Write-Host @"
  ____                      _  __      __         
 |  _ \                    | | \ \    / /         
 | |_) |_ __ __ _ _ __   __| |  \ \  / /   _  ___ 
 |  _ <| '__/ _  | '_ \ / _  |   \ \/ / | | |/ _ \
 | |_) | | | (_| | | | | (_| |    \  /| |_| |  __/
 |____/|_|  \__,_|_| |_|\__,_|     \/  \__,_|\___|
                                                  
                                                  
"@ -ForegroundColor Green

$curDir = $pwd
$srcDir = "$PSScriptRoot\..\src"
$bvFrontEndDir = "$srcDir\BrandVue.FrontEnd"
$errors = 0
$LastExitCode = 0

try {
    # STEP 1: npm install
    Write-Host "STEP 1: npm install" -ForegroundColor Green
    cd "$bvFrontEndDir"
    npm install
    if ($LastExitCode) {
        throw
    }

    # STEP 2: dotnet build
    Write-Host "STEP 2: dotnet build" -ForegroundColor Green
    cd "$srcDir"
    dotnet build BrandVue.sln -p:Configuration=Debug -p:LocalScript=true
    if ($LastExitCode) {
        throw
    }

    # STEP 3: npm run build:dev
    Write-Host "STEP 3: npm run build:dev" -ForegroundColor Green
    cd "$bvFrontEndDir"
    npm run build:dev
    if ($LastExitCode) {
        throw
    }
}
catch {
    $errors = 1
    Write-Host @"
Something went wrong. Consider running 'git clean -xfd' before this script.
Save and commit all work defore before doing so.
"@ -ForegroundColor Yellow
}
finally {
    cd $curDir
}

if (!$errors) {
    Write-Host @"

      ____        _ __    __
     / __ )__  __(_) /___/ /
    / __  / / / / / / __  / 
   / /_/ / /_/ / / / /_/ /  
  /_____/\____/_/_/\____/   
                            
     ______                      __     __     
    / ____/___  ____ ___  ____  / /__  / /____ 
   / /   / __ \/ __  __ \/ __ \/ / _ \/ __/ _ \
  / /___/ /_/ / / / / / / /_/ / /  __/ /_/  __/
  \____/\____/_/ /_/ /_/ .___/_/\___/\__/\___/ 
                      /_/                      


"@ -ForegroundColor Green
    Write-Host "Now run $PSScriptRoot\hotreloading.ps1" -ForegroundColor Yellow
    Write-Host "And hit F5 in Visual Studio" -ForegroundColor Yellow
}

    Write-Host -NoNewLine 'Press any key to continue...' -ForegroundColor Green;
    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');
