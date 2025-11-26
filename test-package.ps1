# Local Package Test Script
# Run this from the repository root to test the packaging process locally

$project = 'KittenRemoteControl'
Write-Host "=== Testing Release Package Creation ===" -ForegroundColor Cyan
Write-Host ""

# Check if build exists
$outputDir = "$project/bin/Release/net9.0"

if (-not (Test-Path $outputDir)) {
    Write-Host "Build output not found. Building project first..." -ForegroundColor Yellow
    dotnet build -c Release
    Write-Host ""
}

if (-not (Test-Path $outputDir)) {
    Write-Host "ERROR: Build failed or output directory not found: $outputDir" -ForegroundColor Red
    exit 1
}

Write-Host "Found build output: $outputDir" -ForegroundColor Green
Write-Host ""

# Create package directory
$contentDir = "$project`_Test"
if (Test-Path $contentDir) { Remove-Item -Recurse -Force $contentDir }
New-Item -ItemType Directory -Path $contentDir -Force | Out-Null

Write-Host "Packaging into: $contentDir" -ForegroundColor Cyan
Write-Host ""

# Define exclusions
$excludeDlls = @(
    'KSA.dll', 'Brutal.Core.Numerics.dll', 'Brutal.Glfw.dll', 
    'Brutal.ImGui.dll', 'Brutal.ImGui.Extensions.dll',
    'Brutal.Core.Collections.dll', 'Brutal.Core.Common.dll',
    'Brutal.Core.Logging.dll', 'Brutal.Core.Memory.dll',
    'Brutal.Core.Strings.dll', 'Brutal.Fmod.dll',
    'Brutal.Framework.dll', 'Brutal.Gli.dll',
    'Brutal.Gli.Texture.dll', 'Brutal.GltfApi.dll',
    'Brutal.ImGui.Abstractions.dll', 'Brutal.Ktx.dll',
    'Brutal.Ktx.Texture.dll', 'Brutal.Native.dll',
    'Brutal.ShaderCompiler.dll', 'Brutal.Stb.dll',
    'Brutal.Stb.Texture.dll', 'Brutal.Texture.dll',
    'Brutal.Vulkan.dll', 'Brutal.Vulkan.Abstractions.dll',
    'StarMap.API.dll', '0Harmony.dll'
)

# Copy DLLs
Write-Host "Copying DLLs from build output..." -ForegroundColor Yellow
$dllFiles = Get-ChildItem -Path $outputDir -Filter "*.dll" -File
Write-Host "Found $($dllFiles.Count) DLL files" -ForegroundColor Gray

$copiedCount = 0
$skippedCount = 0

$dllFiles | ForEach-Object {
    if ($excludeDlls -notcontains $_.Name) {
        Write-Host "  ✓ Copying: $($_.Name)" -ForegroundColor Green
        Copy-Item $_.FullName -Destination $contentDir -Force
        $copiedCount++
    } else {
        Write-Host "  ✗ Skipping: $($_.Name)" -ForegroundColor DarkGray
        $skippedCount++
    }
}

Write-Host ""
Write-Host "Copied: $copiedCount DLLs, Skipped: $skippedCount DLLs" -ForegroundColor Cyan
Write-Host ""

# Copy deps.json
$depsFile = Get-ChildItem -Path $outputDir -Filter "$project.deps.json" -File
if ($depsFile) {
    Write-Host "✓ Copying deps.json" -ForegroundColor Green
    Copy-Item $depsFile.FullName -Destination $contentDir -Force
}

# Copy other files
$filesToCopy = @(
    @{Path="$project/mod.toml"; Name="mod.toml"},
    @{Path="README.md"; Name="README.md"},
    @{Path="LICENSE"; Name="LICENSE"},
    @{Path="NOTICE.txt"; Name="NOTICE.txt"},
    @{Path="openapi.yaml"; Name="openapi.yaml"}
)

Write-Host "Copying documentation and config files..." -ForegroundColor Yellow
foreach ($file in $filesToCopy) {
    if (Test-Path $file.Path) {
        Write-Host "  ✓ Copying: $($file.Name)" -ForegroundColor Green
        Copy-Item $file.Path -Destination $contentDir -Force
    } else {
        Write-Host "  ✗ Not found: $($file.Name)" -ForegroundColor DarkYellow
    }
}

# Copy licenses directory
if (Test-Path "licenses") {
    Write-Host "  ✓ Copying: licenses/" -ForegroundColor Green
    Copy-Item "licenses" -Destination $contentDir -Recurse -Force
}

Write-Host ""

# Create ZIP
$zipName = "$project`_Test.zip"
if (Test-Path $zipName) { Remove-Item $zipName -Force }

Write-Host "Creating ZIP archive: $zipName" -ForegroundColor Cyan
Compress-Archive -Path $contentDir -DestinationPath $zipName -Force

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Package Contents:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$fileCount = 0
$dllCount = 0
$totalSize = 0

Get-ChildItem -Path $contentDir -Recurse | ForEach-Object {
    $relativePath = $_.FullName.Substring($contentDir.Length + 1)
    if ($_.PSIsContainer) {
        Write-Host "  [DIR]  $relativePath" -ForegroundColor Blue
    } else {
        $fileCount++
        $totalSize += $_.Length
        if ($_.Extension -eq ".dll") { 
            $dllCount++
            $color = "Green"
        } else {
            $color = "Gray"
        }
        $sizeKb = [math]::Round($_.Length / 1KB, 2)
        Write-Host "  [FILE] $relativePath ($sizeKb KB)" -ForegroundColor $color
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Total files: $fileCount" -ForegroundColor White
Write-Host "  DLL files: $dllCount" -ForegroundColor White
Write-Host "  Total size: $([math]::Round($totalSize / 1MB, 2)) MB" -ForegroundColor White
Write-Host "  ZIP file: $zipName ($([math]::Round((Get-Item $zipName).Length / 1MB, 2)) MB)" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test ZIP structure
Write-Host "Testing ZIP structure..." -ForegroundColor Yellow
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path $zipName))
Write-Host "ZIP contains $($zip.Entries.Count) entries:" -ForegroundColor Gray
$zip.Entries | Select-Object -First 10 | ForEach-Object {
    Write-Host "  $($_.FullName)" -ForegroundColor DarkGray
}
if ($zip.Entries.Count -gt 10) {
    Write-Host "  ... and $($zip.Entries.Count - 10) more" -ForegroundColor DarkGray
}
$zip.Dispose()

Write-Host ""
Write-Host "✓ Package created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Test the package:" -ForegroundColor Yellow
Write-Host "  1. Extract: Expand-Archive -Path $zipName -DestinationPath ./test_extract" -ForegroundColor Gray
Write-Host "  2. Check structure: ls ./test_extract" -ForegroundColor Gray
Write-Host ""
Write-Host "Clean up test files:" -ForegroundColor Yellow
Write-Host "  Remove-Item -Recurse -Force $contentDir, $zipName" -ForegroundColor Gray
Write-Host ""

