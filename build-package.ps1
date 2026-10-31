param(
    [Parameter(Mandatory=$true)][string]$GamePath,
    [Parameter(Mandatory=$true)][string]$StarMapPath,
    [string]$OutputPath = "$PSScriptRoot/artifacts",
    [switch]$RefreshPrebuilt
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/scripts/release-common.ps1"
$OutputPath = [IO.Path]::GetFullPath($OutputPath)
$GamePath = (Resolve-Path -LiteralPath $GamePath).Path
$StarMapPath = (Resolve-Path -LiteralPath $StarMapPath).Path
$gameVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $GamePath 'KSA.dll')).Version
$loaderVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $StarMapPath 'StarMap.API.dll')).Version
if ($gameVersion -lt [Version]'2026.10.25.5707' -or $loaderVersion -lt [Version]'0.4.8.0') {
    throw 'Release builds require the real KSA 2026.10.25.5707+ and StarMap 0.4.8+ assemblies; stub builds are unsupported.'
}
$build = Join-Path $OutputPath ('build-' + [Guid]::NewGuid().ToString('N'))
# Fresh output prevents stale dependencies from previous branches entering a release.
& dotnet build "$PSScriptRoot/KittenRemoteControl/KittenRemoteControl.csproj" -c Release -o $build "-p:KSA_DLLS_PATH=$GamePath" "-p:STARMAP_PATH=$StarMapPath" --nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
# Normalize text before hashing so checked-in runtime metadata survives CI checkout.
$utf8 = New-Object System.Text.UTF8Encoding($false)
foreach ($name in @('KittenRemoteControl.deps.json', 'mod.toml')) {
    $path = Join-Path $build $name
    [IO.File]::WriteAllText($path, [IO.File]::ReadAllText($path).Replace("`r`n", "`n"), $utf8)
}
$files = [ordered]@{}
foreach ($name in @('KittenRemoteControl.dll', 'KittenRemoteControl.deps.json', 'mod.toml')) {
    $files[$name] = (Get-FileHash (Join-Path $build $name) -Algorithm SHA256).Hash.ToLowerInvariant()
}
$info = [ordered]@{
    version = Get-ReleaseVersion
    targetFramework = 'net10.0'
    sourceSha256 = Get-SourceFingerprint
    gameAssemblyVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $GamePath 'KSA.dll')).Version.ToString()
    starMapApiVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $StarMapPath 'StarMap.API.dll')).Version.ToString()
    sdk = (& dotnet --version | Out-String).Trim()
    files = $files
}
$utf8 = New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $build 'build-info.json'), (($info | ConvertTo-Json -Depth 4).Replace("`r`n", "`n")) + "`n", $utf8)
if ($RefreshPrebuilt) {
    $prebuilt = Join-Path $PSScriptRoot 'dist/KittenRemoteControl'
    New-Item -ItemType Directory -Path $prebuilt -Force | Out-Null
    foreach ($name in @('KittenRemoteControl.dll', 'KittenRemoteControl.deps.json', 'mod.toml', 'build-info.json')) {
        Copy-Item -LiteralPath (Join-Path $build $name) -Destination $prebuilt -Force
    }
}
& "$PSScriptRoot/package-release.ps1" -RuntimePath $build -OutputPath $OutputPath
