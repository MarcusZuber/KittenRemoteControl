# Third-Party Licenses

This project uses the following third-party libraries and dependencies. Each has its own license as detailed below.

## Direct Dependencies

### 1. Lib.Harmony (0Harmony) - v2.4.1
- **License**: MIT License
- **Copyright**: Andreas Pardeike
- **Repository**: https://github.com/pardeike/Harmony
- **NuGet**: https://www.nuget.org/packages/Lib.Harmony/
- **License File**: [LICENSE-Harmony.txt](LICENSE-Harmony.txt)

### 2. Grapevine - v5.0.2
- **License**: Apache License 2.0
- **Copyright**: Scott Offen
- **Repository**: https://github.com/scottoffen/grapevine
- **NuGet**: https://www.nuget.org/packages/Grapevine/
- **License File**: [LICENSE-Grapevine.txt](LICENSE-Grapevine.txt)

### 3. StarMap.API - v0.3.1
- **License**: See StarMap repository
- **Repository**: https://github.com/StarMapLoader
- **NuGet**: https://www.nuget.org/packages/starmap.api/
- **License File**: [LICENSE-StarMap.txt](LICENSE-StarMap.txt)

## Transitive Dependencies (via Grapevine)

The following Microsoft libraries are transitive dependencies brought in by Grapevine:

### Microsoft.Extensions.* Libraries
- **License**: MIT License
- **Copyright**: .NET Foundation and Contributors
- **Repository**: https://github.com/dotnet/runtime
- **License File**: [LICENSE-Microsoft.txt](LICENSE-Microsoft.txt)

Includes:
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Configuration.FileExtensions
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Logging.Abstractions
- Microsoft.Extensions.Logging.Console

## .NET Runtime Libraries

The following are part of the .NET 9.0 Runtime and are used by this project:

- **System.Text.Json** - JSON serialization/deserialization
- **System.Net.HttpListener** - HTTP server functionality
- **System.Net.Sockets** - TCP socket server (fallback)

All .NET runtime libraries are licensed under the MIT License by the .NET Foundation.

## Game-Specific Dependencies

### Kitten Space Agency (KSA) DLLs

The following DLLs from Kitten Space Agency are referenced but NOT distributed with this mod:

- Brutal.Core.Numerics.dll
- Brutal.Glfw.dll
- Brutal.ImGui.dll
- Brutal.ImGui.Extensions.dll
- KSA.dll

**Note**: These are proprietary game files and are NOT covered by this project's license. 
Users must own Kitten Space Agency to use this mod.

## License Compliance

All dependencies used in this project are compatible with the MIT License under which this project is distributed. 

### MIT License Compatible
- ✅ Lib.Harmony (MIT)
- ✅ Microsoft.Extensions.* (MIT)
- ✅ .NET Runtime libraries (MIT)

### Apache 2.0 License Compatible
- ✅ Grapevine (Apache 2.0)

Note: The Apache License 2.0 is compatible with the MIT License for distribution.

## Distribution

When distributing this mod, the following files should be included:

1. **This project's files**:
   - KittenRemoteControl.dll
   - LICENSE (main project license)
   - This THIRD-PARTY-LICENSES.md file

2. **Third-party dependencies** (NuGet packages):
   - 0Harmony.dll
   - Grapevine.dll (and its dependencies)

3. **License files**:
   - All LICENSE-*.txt files from the licenses/ directory

**Do NOT distribute**: Kitten Space Agency game files (KSA.dll, Brutal.*.dll)

## Acknowledgments

Special thanks to:
- Andreas Pardeike for Harmony
- Scott Offen for Grapevine
- The .NET Foundation for the runtime libraries
- The StarMap team for the mod loader API
- Ahwoo Games for Kitten Space Agency

## Questions?

If you have questions about licensing or need clarification, please open an issue at:
https://github.com/[your-repo]/KittenRemoteControl/issues

---

Last Updated: November 23, 2025

