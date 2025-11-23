# Release Package Contents

This document describes what files are included in the release package created by GitHub Actions.

## Package Structure

The ZIP file structure is:

```
KittenRemoteControl.zip
└── KittenRemoteControl/
    ├── KittenRemoteControl.dll          # Main mod DLL
    ├── KittenRemoteControl.deps.json    # .NET dependency information
    ├── mod.toml                          # Mod metadata for StarMap
    ├── Grapevine.dll                     # REST server framework
    ├── Grapeseed.dll                     # Grapevine dependency
    ├── [Microsoft.Extensions.*.dll]      # Microsoft dependencies (multiple files)
    ├── README.md                         # User documentation
    ├── LICENSE                           # MIT License
    ├── NOTICE.txt                        # Third-party notices
    ├── openapi.yaml                      # OpenAPI specification
    └── licenses/                         # License files directory
        ├── LICENSE-Harmony.txt
        ├── LICENSE-Grapevine.txt
        ├── LICENSE-Microsoft.txt
        ├── LICENSE-StarMap.txt
        ├── THIRD-PARTY-LICENSES.md
        └── README.md
```

**Note**: The ZIP contains exactly one folder level (KittenRemoteControl/) with all files inside.

## Included Files

### Core Mod Files
- **KittenRemoteControl.dll** - Main mod implementation
- **KittenRemoteControl.deps.json** - .NET dependency metadata
- **mod.toml** - StarMap mod loader configuration

### Dependencies (NuGet Packages)
The following required DLLs from NuGet packages are included:

- **Grapevine.dll** (v5.0.2) - REST server framework
- **Grapeseed.dll** - Grapevine core dependency
- **Microsoft.Extensions.Configuration.dll** - Configuration framework
- **Microsoft.Extensions.Configuration.FileExtensions.dll** - File-based configuration
- **Microsoft.Extensions.Configuration.Json.dll** - JSON configuration support
- **Microsoft.Extensions.Logging.dll** - Logging framework
- **Microsoft.Extensions.Logging.Abstractions.dll** - Logging interfaces
- **Microsoft.Extensions.Logging.Console.dll** - Console logging
- **Microsoft.Extensions.DependencyInjection.dll** - DI container
- **Microsoft.Extensions.DependencyInjection.Abstractions.dll** - DI interfaces
- **Microsoft.Extensions.Options.dll** - Options pattern
- **Microsoft.Extensions.FileProviders.Abstractions.dll** - File provider interfaces
- **Microsoft.Extensions.FileProviders.Physical.dll** - Physical file provider
- **Microsoft.Extensions.FileSystemGlobbing.dll** - File globbing support
- **Microsoft.Extensions.Primitives.dll** - Primitive types

**Not Included** (already provided by StarMap mod loader):
- ❌ **0Harmony.dll** (v2.4.1) - Provided by StarMap
- ❌ **StarMap.API.dll** - Provided by StarMap

### Documentation
- **README.md** - Complete API documentation with examples
- **openapi.yaml** - OpenAPI 3.0 specification for the REST API

### License Files
- **LICENSE** - MIT License for the main project
- **NOTICE.txt** - Third-party notices and attributions
- **licenses/** - Directory containing all dependency licenses
  - LICENSE-Harmony.txt
  - LICENSE-Grapevine.txt
  - LICENSE-Microsoft.txt
  - LICENSE-StarMap.txt
  - THIRD-PARTY-LICENSES.md (comprehensive overview)
  - README.md (licenses directory documentation)

## Excluded Files

### StarMap Mod Loader Dependencies (Already Provided)

The following libraries are **NOT** included because they are already provided by the StarMap mod loader:

- **0Harmony.dll** - Runtime patching library (included in StarMap)
- **StarMap.API.dll** - Mod loader API (included in StarMap)

### Kitten Space Agency DLLs (Proprietary)

The following game files are **NOT** included in the release package (users must own the game):
- KSA.dll
- Brutal.Core.Numerics.dll
- Brutal.Glfw.dll
- Brutal.ImGui.dll
- Brutal.ImGui.Extensions.dll
- Brutal.Core.Collections.dll
- Brutal.Core.Common.dll
- Brutal.Core.Logging.dll
- Brutal.Core.Memory.dll
- Brutal.Core.Strings.dll
- Brutal.Fmod.dll
- Brutal.Framework.dll
- Brutal.Gli.dll
- Brutal.Gli.Texture.dll
- Brutal.GltfApi.dll
- Brutal.ImGui.Abstractions.dll
- Brutal.Ktx.dll
- Brutal.Ktx.Texture.dll
- Brutal.Native.dll
- Brutal.ShaderCompiler.dll
- Brutal.Stb.dll
- Brutal.Stb.Texture.dll
- Brutal.Texture.dll
- Brutal.Vulkan.dll
- Brutal.Vulkan.Abstractions.dll

These files are copyrighted by Ahwoo Games and must be obtained by purchasing Kitten Space Agency.

## Installation

1. Download the latest release package (KittenRemoteControl.zip)
2. Extract the ZIP file - this will create a `KittenRemoteControl` folder
3. Move the extracted `KittenRemoteControl` folder to your game's mod directory:
   ```
   <Game Directory>/Content/KittenRemoteControl/
   ```
4. The StarMap mod loader will automatically load the mod on game start

**Example**:
```
# After extraction, you should have:
<Game Directory>/mods/KittenRemoteControl/KittenRemoteControl.dll
<Game Directory>/mods/KittenRemoteControl/mod.toml
# ... etc.
```

## Package Size

The complete package (without KSA DLLs and StarMap-provided DLLs) is approximately:
- **Release DLLs**: ~1-2 MB (Grapevine + Microsoft.Extensions.*)
- **Documentation**: ~100 KB
- **License files**: ~50 KB
- **Total**: ~1-2 MB compressed

## Verification

To verify the package contents, you can check the GitHub Actions build log which lists all included files with their sizes.

## Updates

When a new version is released, the GitHub Actions workflow automatically:
1. Builds the project in Release mode
2. Collects all required DLLs from the build output
3. Excludes proprietary game DLLs
4. Includes all documentation and license files
5. Creates a ZIP archive
6. Uploads the artifact
7. Creates a GitHub release (if tagged)

## License Compliance

All included dependencies are properly licensed and their licenses are included in the package:
- MIT License (Harmony, Microsoft libraries, StarMap.API)
- Apache 2.0 License (Grapevine)

See the licenses/ directory for full license texts.

---

For questions or issues, please open an issue on the GitHub repository.

