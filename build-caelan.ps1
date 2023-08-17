Set-Location "$PSScriptRoot"
$ErrorActionPreference = "Stop"

# Ensure a clean state by removing build/package folders
Write-Host "Clean old build folders" -ForegroundColor Magenta
$Folders = @("$PSScriptRoot\publish", "$PSScriptRoot\bin", "$PSScriptRoot\releases", "$PSScriptRoot\Builds", "$PSScriptRoot\publish.zip")
foreach ($Folder in $Folders) {
    if (Test-Path $Folder) {
        Remove-Item -path "$Folder" -Recurse -Force
    }
}

Write-Host "Build Webber Server" -ForegroundColor Magenta
Set-Location "$PSScriptRoot\Server"
# &dotnet remove reference "..\Client\Webber.Client.csproj"
# &dotnet publish -p:PublishSingleFile=true --no-self-contained -c Release -r win-x64 -o "$PSScriptRoot\publish"

dotnet publish --no-self-contained -o "$PSScriptRoot\publish" -r linux-x64 -p:PublishSingleFile=true

# &dotnet add reference "..\Client\Webber.Client.csproj"

Write-Host "Build caelan-ui" -ForegroundColor Magenta
Set-Location "$PSScriptRoot\webber-caelan-ui"
&npm install --legacy-peer-deps
&npm run build
Copy-Item "build" -Destination "$PSScriptRoot\publish\wwwroot" -Recurse

# Set-Content -Path "$PSScriptRoot\publish\run.bat" -Value '%0\..\Webber.exe -config "C:\Source\DesktopUtil\webber-settings.json"'
# Set-Content -Path "$PSScriptRoot\publish\install.bat" -Value '%0\..\Webber.exe install -config "C:\Source\DesktopUtil\webber-settings.json" && %0\..\Webber.exe start'
# Set-Content -Path "$PSScriptRoot\publish\uninstall.bat" -Value '%0\..\Webber.exe stop && %0\..\Webber.exe uninstall'

Write-Host "Compressing" -ForegroundColor Magenta
Compress-Archive "$PSScriptRoot\publish\*" "$PSScriptRoot\publish.zip"

Write-Host "Done" -ForegroundColor Magenta