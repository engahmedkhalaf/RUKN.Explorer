param(
    [string]$SolutionDir,
    [string]$TargetDir
)

$ErrorActionPreference = "Stop"

$SolutionDir = $SolutionDir.Trim()
$TargetDir = $TargetDir.Trim()

# 1. Create target bundle directories
$bundleDir = Join-Path $SolutionDir "RUKN.Explorer.bundle"
$contentsDir = Join-Path $bundleDir "Contents\dlls\2024\RUKN.Explorer"
$enUsDir = Join-Path $contentsDir "en-US"
$imagesDir = Join-Path $contentsDir "Images"

if (!(Test-Path $enUsDir)) { New-Item -ItemType Directory -Path $enUsDir -Force | Out-Null }
if (!(Test-Path $imagesDir)) { New-Item -ItemType Directory -Path $imagesDir -Force | Out-Null }

# 2. Copy compiled dll and resources to bundle
Copy-Item -Path (Join-Path $TargetDir "*.dll") -Destination $contentsDir -Force
Copy-Item -Path (Join-Path $SolutionDir "RUKN.InsightPro.Common\Images\*") -Destination $imagesDir -Force
Copy-Item -Path (Join-Path $SolutionDir "RUKN.InsightPro.Common\en-US\PluginRibbon.xaml") -Destination $enUsDir -Force
Copy-Item -Path (Join-Path $SolutionDir "RUKN.InsightPro.Common\PackageContents.xml") -Destination $bundleDir -Force

# 3. Deploy to Autodesk ApplicationPlugins folder
$appDataPlugins = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\RUKN.Explorer.bundle"

if (Test-Path $appDataPlugins) {
    try {
        Remove-Item -Path $appDataPlugins -Recurse -Force -ErrorAction Stop
    } catch {
        # If folder is locked, delete unlocked files individually and proceed
        Get-ChildItem -Path $appDataPlugins -Recurse | ForEach-Object {
            try {
                Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
            } catch {}
        }
    }
}
New-Item -ItemType Directory -Path $appDataPlugins -Force | Out-Null
Copy-Item -Path (Join-Path $bundleDir "*") -Destination $appDataPlugins -Recurse -Force
