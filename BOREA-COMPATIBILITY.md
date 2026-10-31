# Borea compatibility verification

Release candidate: 0.1.0-borea.1, based on fix/wine-httplistener-tcp-server.
Tested on 2026-10-31 with KSA 2026.10.25.5707, StarMap.API 0.4.8.0 and .NET SDK 10.0.403 on Windows.

Build against the real installed assemblies; do not use KSA_stub, StarMap_stub or Brutal_stub for a release.
The current build has zero warnings and zero errors. The TCP server, Telemachus datalink, dispatch queue,
in-game status window and Houston frontend are retained. The immediate-load signature and metadata are corrected.

Verified: launch through Borea, visible in-game client list, all 15 REST GET routes, Telemachus GET/POST/OPTIONS,
and Telemachus throttle change to 0.25 with REST readback and restoration to 1. The user confirmed Houston after
starting its HTTP server from the frontend folder.

Not tested: physical controllers, engine ignition, active thrusters, staging, final shutdown and Wine/CrossOver.
Windows Firewall permissions are handled by the user. No original game installation files were changed.

See RELEASE-PACKAGE.md for installation and release/RELEASING.md for the local publication handoff.
