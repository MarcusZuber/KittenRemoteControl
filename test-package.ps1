param([Parameter(Mandatory=$true)][string]$ZipPath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $ZipPath).Path)
try {
    $names = @($zip.Entries | ForEach-Object FullName)
    if ($names.Count -ne @($names | Sort-Object -Unique).Count) { throw 'Duplicate ZIP entries.' }
    foreach ($name in $names) {
        if ($name -notlike 'KittenRemoteControl/*' -or $name.Contains('\') -or $name -match '(^|/)\.\.(/|$)') { throw "Invalid archive path: $name" }
        if ($name.EndsWith('.dll') -and $name -ne 'KittenRemoteControl/KittenRemoteControl.dll') { throw "Unexpected dependency DLL: $name" }
    }
    foreach ($name in @('KittenRemoteControl.dll','KittenRemoteControl.deps.json','mod.toml','build-info.json','LICENSE','README.md','frontend/houston.html','frontend/houston.css','frontend/houston.js','frontend/astronaut-cat.svg','frontend/start-houston.cmd','frontend/start-houston.ps1')) {
        if ($names -cnotcontains "KittenRemoteControl/$name") { throw "Missing release file: $name" }
    }
    $reader = New-Object IO.StreamReader($zip.GetEntry('KittenRemoteControl/mod.toml').Open())
    try { $metadata = $reader.ReadToEnd() } finally { $reader.Dispose() }
    if ($metadata -notmatch 'name\s*=\s*"KittenRemoteControl"' -or $metadata -notmatch 'EntryAssembly\s*=\s*"KittenRemoteControl"') { throw 'Invalid mod metadata.' }
    $reader = New-Object IO.StreamReader($zip.GetEntry('KittenRemoteControl/build-info.json').Open())
    try { $info = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
    foreach ($name in @('KittenRemoteControl.dll','KittenRemoteControl.deps.json','mod.toml')) {
        $stream = $zip.GetEntry("KittenRemoteControl/$name").Open()
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $hash = ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','').ToLowerInvariant() }
        finally { $sha.Dispose(); $stream.Dispose() }
        if ($hash -ne $info.files.$name) { throw "Archive file hash mismatch: $name" }
    }
    Write-Output "Package validated: $($names.Count) entries, version $($info.version)."
} finally { $zip.Dispose() }
