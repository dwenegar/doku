$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot

$TemplateRoot = "$PSScriptRoot/Doku.Templates/project"
$PdfTemplateRoot = "$PSScriptRoot/Doku.Templates/project-pdf"
$DestinationDir = "$PSScriptRoot/Doku.Lib/Templates"

if (!(Test-Path -Path $DestinationDir))
{
    New-Item -Path $DestinationDir -ItemType Directory
}

Set-Location "$TemplateRoot"
Compress-Archive -Force -Path * -DestinationPath $DestinationDir/project.zip
Set-Location "$PdfTemplateRoot"
Compress-Archive -Force -Path * -DestinationPath $DestinationDir/project-pdf.zip

Pop-Location
