Set-Location "$PSScriptRoot"
$ErrorActionPreference = "Stop"

# Ensure a clean state by removing build/package folders
$Folders = @("$PSScriptRoot\publish", "$PSScriptRoot\bin", "$PSScriptRoot\releases")
foreach ($Folder in $Folders) {
    if (Test-Path $Folder) {
        Remove-Item -path "$Folder" -Recurse -Force
    }
}

Write-Host "Build Webber" -ForegroundColor Magenta
Set-Location "$PSScriptRoot\Server"
&dotnet remove reference "..\Client\Webber.Client.csproj"
&dotnet publish -p:PublishSingleFile=true --no-self-contained -c Release -r win-x64 -o "$PSScriptRoot\publish"
&dotnet add reference "..\Client\Webber.Client.csproj"

Write-Host "Build obs-express" -ForegroundColor Magenta
Set-Location "$PSScriptRoot\webber-react-ui"
&npm install
&npm run build
Copy-Item "build" -Destination "$PSScriptRoot\publish\wwwroot" -Recurse

Set-Content -Path "$PSScriptRoot\publish\run.bat" -Value '%0\..\Webber.exe --urls http://*:18902 -c "C:\Source\DesktopUtil\webber-settings.json"'
Write-Host "Done" -ForegroundColor Magenta