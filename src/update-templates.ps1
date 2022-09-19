$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot

$TemplateRoot = "$PSScriptRoot/doku.templates/project"
$DestinationDir = "$PSScriptRoot/doku/Templates"

if (!(Test-Path -Path $DestinationDir))
{
    New-Item -Path $DestinationDir -ItemType Directory
}

Set-Location "$TemplateRoot"
Compress-Archive -Force -Path * -DestinationPath $DestinationDir/project.zip

Pop-Location
