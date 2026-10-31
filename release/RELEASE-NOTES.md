# Kitten Remote Control 0.1.0-borea.1

Experimental compatibility prerelease for **KSA 2026.10.25.5707**, **StarMap 0.4.8** and **Borea**.

Provides HTTP spacecraft control and telemetry on port 8080, a Telemachus-compatible datalink on port 8085 for scripts and MicroBlocks, the in-game connection window, and Houston telemetry gauges.

## Installation

1. Download `KittenRemoteControl-0.1.0-borea.1.zip` and optionally verify its SHA-256 checksum.
2. Close KSA. Extract the single `KittenRemoteControl` folder into the chosen Borea instance's `mods` folder, replacing the older installation.
3. Enable `KittenRemoteControl` in the instance manifest, launch through Borea, and start the simulation.
4. For Houston, run `frontend/start-houston.cmd` (Python 3 required), keep that window open, and visit http://localhost:8000/houston.html. Datalink host: localhost; port: 8085.

The package does not include game or loader DLLs. No full Visual Studio installation is needed to use it.

## Validation and limitations

Windows/Borea launch and in-game window verified; all 15 REST GET routes and Telemachus GET/POST/OPTIONS passed. Throttle change/readback/restore passed. The user confirmed Houston works after starting its web server. Physical controllers, Wine/CrossOver, engine ignition, active thrusters and staging were not tested in this release session.

This remains a proof of concept. The servers bind to the LAN without authentication or TLS, as in the source branch. Use a trusted network.
