# Changelog

## 0.1.0-borea.2 — 2026-11-01

- Use the body's mean radius consistently for REST and Telemachus orbital elevations.
- Reset stage delta-v peak tracking when the craft/stage changes, no craft is available, or the mod unloads.
- Cancel and close accepted HTTP clients at shutdown and wait for executing handlers before returning.
- Time out incomplete requests after 15 seconds and discard queued main-thread commands on unload.
- Add regression checks for telemetry helpers, craft switching, fragmented HTTP, CORS, shutdown, timeouts and restart; run them in CI.

Build and regression checks passed. The previously running game was not restarted for this revision; these checks do not establish new in-game or Wine/CrossOver validation.

## 0.1.0-borea.1 — 2026-10-31

Experimental Borea compatibility prerelease, based on `fix/wine-httplistener-tcp-server`.

- Build against actual KSA 2026.10.25.5707 and StarMap API 0.4.8 assemblies on .NET 10.
- Correct the immediate-load callback signature and declare the mod entry assembly.
- Preserve the TCP REST server, Telemachus datalink, MicroBlocks protocol, in-game status window and Houston dashboard.
- Package the dashboard and a Windows startup helper with the mod.
- Ship a single mod folder, checksums and build provenance; exclude game/loader assemblies and obsolete dependencies.
- Validate releases against the source fingerprint and archive contents instead of building release binaries from stubs.

Verified on Windows through Borea: in-game status window, 15 REST GET endpoints, Telemachus GET/POST/OPTIONS and throttle change/readback/restore. The user confirmed Houston works after starting its HTTP server. Wine/CrossOver, physical controllers, active thruster firing, staging and engine ignition were not tested in this release session.
