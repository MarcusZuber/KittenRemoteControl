param(
    [string]$RuntimePath = "$PSScriptRoot/dist/KittenRemoteControl",
    [string]$OutputPath = "$PSScriptRoot/artifacts"
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/scripts/release-common.ps1"
$OutputPath = [IO.Path]::GetFullPath($OutputPath)
Test-ReleaseRuntime $RuntimePath
$version = Get-ReleaseVersion
$stage = Join-Path $OutputPath ('package-' + [Guid]::NewGuid().ToString('N'))
$mod = Join-Path $stage 'KittenRemoteControl'
New-Item -ItemType Directory -Path $mod -Force | Out-Null
# Exact allowlist: no game, loader, Harmony, stub or obsolete Grapevine DLLs.
foreach ($name in @('KittenRemoteControl.dll', 'KittenRemoteControl.deps.json', 'mod.toml', 'build-info.json')) {
    Copy-Item -LiteralPath (Join-Path $RuntimePath $name) -Destination $mod
}
foreach ($name in @('README.md', 'LICENSE', 'NOTICE.txt', 'openapi.yaml', 'RELEASE-PACKAGE.md', 'CHANGELOG.md')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $mod
}
foreach ($name in @('frontend', 'licenses')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $mod -Recurse
}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipPath = [IO.Path]::GetFullPath((Join-Path $OutputPath "KittenRemoteControl-$version.zip"))
# ZipArchive with explicit '/' paths works for Borea on Windows, Linux and macOS.
$stream = [IO.File]::Open($zipPath, [IO.FileMode]::Create)
$zip = New-Object IO.Compression.ZipArchive($stream, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem $mod -Recurse -File | Sort-Object FullName) {
        $relative = $file.FullName.Substring($stage.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, $relative, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $zip.Dispose(); $stream.Dispose() }
& "$PSScriptRoot/test-package.ps1" -ZipPath $zipPath
$hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$zipPath.sha256", "$hash  $([IO.Path]::GetFileName($zipPath))`n", (New-Object Text.UTF8Encoding($false)))
Write-Output $zipPath
