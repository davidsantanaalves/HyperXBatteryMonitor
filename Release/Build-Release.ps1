param(
    [switch]$BuildStorePackage
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'HyperXBatteryMonitor.csproj'
$installer = Join-Path $root 'Installer\HyperXBatteryTray.iss'
$packageProject = Join-Path $root 'Packaging\HyperXBatteryMonitor.Package.wapproj'
$publish = Join-Path $root 'bin\Release\net10.0-windows10.0.17763.0\win-x64\publish'
$release = Join-Path $root 'Releases'

New-Item -ItemType Directory -Force -Path $release | Out-Null

Write-Host 'Publishing HyperX Battery Monitor 2.0.0...' -ForegroundColor Cyan
dotnet publish $project -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false

$exe = Join-Path $publish 'HyperX Battery Monitor.exe'
if (-not (Test-Path $exe)) {
    throw "Published executable was not found: $exe"
}

# Locate Inno Setup without dereferencing a null Get-Command result.
$isccCommand = Get-Command iscc.exe -ErrorAction SilentlyContinue
$isccCandidates = @()
if ($isccCommand) {
    $isccCandidates += $isccCommand.Source
}
$isccCandidates += @(
    "$env:ProgramFiles\Inno Setup 7\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
) | Where-Object { $_ -and (Test-Path $_) }
$isccCandidates = @($isccCandidates)

if ($isccCandidates.Count -eq 0) {
    throw 'Inno Setup (ISCC.exe) was not found. Expected Inno Setup 7 or 6.'
}

Write-Host 'Building installer...' -ForegroundColor Cyan
& $isccCandidates[0] $installer

if ($BuildStorePackage) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path $vswhere)) {
        $vswhere = Join-Path $env:ProgramFiles 'Microsoft Visual Studio\Installer\vswhere.exe'
    }

    if (Test-Path $vswhere) {
        $msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
    } else {
        $msbuild = $null
    }

    if (-not $msbuild -or -not (Test-Path $msbuild)) {
        $msbuildCommand = Get-Command msbuild.exe -ErrorAction SilentlyContinue
        if ($msbuildCommand) {
            $msbuild = $msbuildCommand.Source
        }
    }

    if (-not $msbuild -or -not (Test-Path $msbuild)) {
        throw 'MSBuild.exe was not found. Install Visual Studio with the Windows Application Packaging Project workload.'
    }

    Write-Host 'Building Microsoft Store MSIX package...' -ForegroundColor Cyan
    & $msbuild $packageProject /restore /p:Configuration=Release /p:Platform=x64 /p:SolutionDir="$root\"
}

Write-Host ''
Write-Host "Release artifacts are in: $release" -ForegroundColor Green
