# Borea release package

Version: **0.1.0-borea.1** (experimental prerelease).

The archive has exactly one top-level folder:

```text
KittenRemoteControl/
  KittenRemoteControl.dll
  KittenRemoteControl.deps.json
  mod.toml
  build-info.json
  README.md
  CHANGELOG.md
  LICENSE
  NOTICE.txt
  openapi.yaml
  frontend/
    houston.html
    houston.css
    houston.js
    astronaut-cat.svg
    start-houston.cmd
    start-houston.ps1
  licenses/
```

No KSA, Brutal, Planet, StarMap, Harmony, Grapevine, or stub DLL is distributed. The game and loader provide runtime dependencies. Only the mod DLL is included. ZIP entry paths use forward slashes for Borea on different platforms; this is not a claim that the game integration was tested on those platforms.

## Build a release

Use a .NET 10 SDK, the actual game installation and the installed StarMap 0.4.8 loader:

```powershell
.\build-package.ps1 -GamePath 'C:\Program Files\Ahwoo\Games\Kitten Space Agency' -StarMapPath "$env:LOCALAPPDATA\Borea\Loaders\StarMap" -RefreshPrebuilt
```

The output is `artifacts/KittenRemoteControl-0.1.0-borea.1.zip` and its `.sha256` checksum. `-RefreshPrebuilt` updates the three tracked runtime files and `build-info.json` under `dist/KittenRemoteControl`. Commit these with their source changes. Source fingerprints normalize line endings so Windows and CI checkouts agree.

`package-release.ps1` verifies the prebuilt binary and source hashes and packages them without requiring game assemblies. The GitHub workflow runs that validation and produces an artifact; it does not compile against stubs or publish a release. `test-package.ps1 -ZipPath <archive>` checks paths, metadata, required frontend files and runtime hashes.

## Install into Borea

Close the game. Open the chosen instance's mods folder from Borea. Replace the previous `KittenRemoteControl` folder with the folder from the archive, preserving any local changes separately. Keep or add this entry in the instance's `manifest.toml` (do not duplicate it):

```toml
[[mods]]
id = "KittenRemoteControl"
enabled = true
```

Launch through Borea and start the simulation. The status window shows the active ports and connected clients. REST uses localhost:8080; Telemachus uses localhost:8085. The servers retain the branch's LAN binding.

For Houston, open the installed mod's `frontend` folder, run `start-houston.cmd`, and browse to http://localhost:8000/houston.html. Keep its server window open. Python 3 is needed for this helper, not for the mod. You can also run `python3 -m http.server 8000` from that folder, or open `houston.html` directly if your browser permits the requests. Houston uses host localhost and port 8085 by default.

## Publish later

See `release/RELEASING.md`, `release/RELEASE-NOTES.md`, the forum-post draft and `release/borea/KittenRemoteControl.toml.template`. A ZIP that installs manually is not automatically listed in Borea Discover. The catalogue needs a published GitHub release and a separate content-index listing.
