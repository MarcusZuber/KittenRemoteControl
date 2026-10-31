# Kitten Remote Control — HTTP controls, MicroBlocks datalink and Houston telemetry

Kitten Remote Control is an experimental mod for Kitten Space Agency. It exposes spacecraft controls and telemetry over HTTP, supports a subset of the Telemachus datalink protocol for MicroBlocks and other clients, and includes an in-game connection window and the Houston browser dashboard.

**Compatibility:** tested on Windows with KSA 2026.10.25.5707, StarMap 0.4.8 and Borea. Other platforms have not been retested for this prerelease.

**Download:** https://github.com/attilastrba/KittenRemoteControl/releases/tag/v0.1.0-borea.1 (publish the release before posting).

Extract the ZIP's KittenRemoteControl folder into your Borea instance's mods folder and enable it. Start the simulation. REST is on port 8080 and the Telemachus datalink is on port 8085. Run frontend/start-houston.cmd and open http://localhost:8000/houston.html for the dashboard; keep its server running.

This is a proof of concept with no authentication or TLS; use a trusted network. Python 3 is only needed for the optional dashboard web server. Game and loader files are not distributed.

Source and issues: https://github.com/attilastrba/KittenRemoteControl

License: MIT. Hardware controller testing and further feedback are welcome.
