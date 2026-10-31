Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:RepoRoot = Split-Path $PSScriptRoot -Parent
function Get-ReleaseVersion {
    [xml]$project = Get-Content -Raw (Join-Path $script:RepoRoot 'KittenRemoteControl/KittenRemoteControl.csproj')
    return [string]$project.Project.PropertyGroup.Version
}
function Get-SourceFingerprint {
    $projectRoot = Join-Path $script:RepoRoot 'KittenRemoteControl'
    $records = foreach ($file in Get-ChildItem $projectRoot -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | Where-Object { $_.Extension -in @('.cs', '.csproj') -or $_.Name -eq 'mod.toml' } | Sort-Object Name) {
        $normalized = [IO.File]::ReadAllText($file.FullName).Replace("`r`n", "`n")
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $hash = ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($normalized)))).Replace('-', '').ToLowerInvariant() }
        finally { $sha.Dispose() }
        $file.FullName.Substring($projectRoot.Length + 1).Replace('\', '/') + ':' + $hash
    }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($records -join "`n"))))).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}
function Test-ReleaseRuntime([string]$RuntimePath) {
    $info = Get-Content -Raw (Join-Path $RuntimePath 'build-info.json') | ConvertFrom-Json
    if ($info.version -ne (Get-ReleaseVersion)) { throw 'Prebuilt version does not match the project.' }
    if ($info.sourceSha256 -ne (Get-SourceFingerprint)) { throw 'Prebuilt DLL is stale. Run build-package.ps1 -RefreshPrebuilt against the installed game and loader.' }
    foreach ($file in @('KittenRemoteControl.dll', 'KittenRemoteControl.deps.json', 'mod.toml')) {
        $actual = (Get-FileHash (Join-Path $RuntimePath $file) -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -ne $info.files.$file) { throw "Prebuilt file hash mismatch: $file" }
    }
}
