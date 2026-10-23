<#
.SYNOPSIS
    Builds the distributable package of the application.

.DESCRIPTION
    Publishes a self-contained, single-file Windows x64 build (the target machine does not need
    .NET installed) and zips it together with appsettings.json and the README:

      artifacts/Supermarket-v<version>-win-x64.zip

    appsettings.Local.json is never included, so local passwords cannot leak into a release.

.EXAMPLE
    ./publish.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$env:DOTNET_NOLOGO = 'true'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 'true'

$project = Join-Path $PSScriptRoot 'src/Supermarket.WinForms/Supermarket.WinForms.csproj'
$version = (Select-Xml -Path (Join-Path $PSScriptRoot 'Directory.Build.props') -XPath '//Version').Node.InnerText
$name = "Supermarket-v$version-win-x64"
$output = Join-Path $PSScriptRoot "artifacts/publish/$name"
$zip = Join-Path $PSScriptRoot "artifacts/$name.zip"

if (Test-Path $output) { Remove-Item $output -Recurse -Force }
if (Test-Path $zip) { Remove-Item $zip -Force }

Write-Host "==> Publishing $name" -ForegroundColor Cyan
dotnet publish $project --configuration Release --runtime win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none -p:PublishDocumentationFile=false -p:PublishReferencesDocumentationFiles=false `
    --output $output
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Defense in depth: fail instead of shipping developer settings.
if (Get-ChildItem $output -Filter 'appsettings.Local.json' -Recurse) {
    throw 'appsettings.Local.json must never be part of a release.'
}

Copy-Item (Join-Path $PSScriptRoot 'README.md') $output

Write-Host "==> Creating $zip" -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $output '*') -DestinationPath $zip

Get-ChildItem $output | Select-Object Name, @{ Name = 'MB'; Expression = { [math]::Round($_.Length / 1MB, 1) } } | Format-Table -AutoSize
Write-Host "Package ready: $zip" -ForegroundColor Green
