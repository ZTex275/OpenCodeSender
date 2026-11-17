param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    throw "vswhere.exe not found. Is Visual Studio installed?"
}

$msbuild = & $vswhere -latest -prerelease -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
if (-not $msbuild) {
    throw "MSBuild.exe not found."
}

Write-Host "MSBuild: $msbuild"

& $msbuild (Join-Path $root "PasteIntoAll.sln") -restore -p:Configuration=$Configuration -v:minimal -nologo
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}

$vsix = Join-Path $root "src\PasteIntoAll\bin\$Configuration\net472\PasteIntoAll.vsix"
Write-Host "VSIX: $vsix"
