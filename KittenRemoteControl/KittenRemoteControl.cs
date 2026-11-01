using Brutal.ImGuiApi;
using StarMap.API;

namespace KittenRemoteControl
{
    [StarMapMod]
    public class RemoteControlMain
    {
        private const int Port = 8080;
        private const int TelemachusPort = 8085;
        private TcpHttpServer? _httpServer;
        private TcpHttpServer? _telemachusServer;
        private bool _guiDisabled;

        [StarMapAfterGui]
        public void OnAfterUi(double dt)
        {
            // Runs on the main/UI thread each frame — execute work queued by HTTP handler threads
            // (e.g. staging) here so game-state mutations don't race the simulation thread.
            MainThreadDispatcher.Drain();
        }

        [StarMapBeforeGui]
        public void OnBeforeUi(double dt)
        {
            if (_guiDisabled)
                return;

            // If the Brutal.ImGui binding can't resolve at runtime, disable the window rather than
            // throw every frame — the HTTP servers keep working regardless.
            try
            {
                DrawStatusWindow();
            }
            catch (Exception ex)
            {
                _guiDisabled = true;
                Console.WriteLine($"[KittenRemoteControl] Status window disabled (ImGui unavailable): {ex.Message}");
            }
        }

        private void DrawStatusWindow()
        {
            var open = ImGui.Begin("Kitten Remote Control", ImGuiWindowFlags.None);
            if (open)
            {
                ImGui.Text($"Control API : http://0.0.0.0:{Port}");
                ImGui.Text($"Telemachus  : http://0.0.0.0:{TelemachusPort}/telemachus/datalink");
                ImGui.Separator();

                var connections = ConnectionTracker.Snapshot();
                if (connections.Count == 0)
                {
                    ImGui.Text("No clients have connected yet.");
                }
                else
                {
                    var now = Environment.TickCount64;
                    ImGui.Text($"Clients ({connections.Count}):");
                    foreach (var c in connections)
                    {
                        var ageSec = (now - c.LastSeenTick) / 1000;
                        var live = now - c.LastSeenTick < 5000 ? "LIVE" : "idle";
                        ImGui.Text($"  [{live}] {c.Ip}:{c.Port}  reqs={c.Count}  {ageSec}s ago  {c.LastRequest}");
                    }
                }
            }

            ImGui.End();
        }

        [StarMapAllModsLoaded]
        public void OnFullyLoaded()
        {
            Patcher.Patch();

            // Initialize the built-in TCP HTTP server.
            // We deliberately avoid HttpListener (used by Grapevine) because it relies on the
            // Windows HTTP Server API (http.sys), which is not implemented under Wine/CrossOver.
            try
            {
                _httpServer = new TcpHttpServer(Port);

                var resource = new RemoteControlResource();
                resource.RegisterRoutes(_httpServer);

                _httpServer.Start();

                Console.WriteLine($"Remote Control REST Server started successfully on http://localhost:{Port}");
                Console.WriteLine($"Registered {_httpServer.RouteCount} routes. Available endpoints:");
                Console.WriteLine("  GET/PUT /control/throttle");
                Console.WriteLine("  GET/PUT /control/engineOn");
                Console.WriteLine("  GET/POST /control/thrusters");
                Console.WriteLine("  GET/PUT /control/referenceFrame");
                Console.WriteLine("  GET /control/referenceFrames");
                Console.WriteLine("  GET/PUT /control/flightComputer/attitudeMode");
                Console.WriteLine("  GET /control/flightComputer/attitudeModes");
                Console.WriteLine("  PUT /control/flightComputer/stabilization");
                Console.WriteLine("  GET /telemetry/apoapsis");
                Console.WriteLine("  GET /telemetry/periapsis");
                Console.WriteLine("  GET /telemetry/orbitingBody/meanRadius");
                Console.WriteLine("  GET /telemetry/apoapsis_elevation");
                Console.WriteLine("  GET /telemetry/periapsis_elevation");
                Console.WriteLine("  GET /telemetry/orbitalSpeed");
                Console.WriteLine("  GET /telemetry/propellantMass");
                Console.WriteLine("  GET /telemetry/totalMass");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start REST server: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            // Telemachus-compatible datalink server (separate port, bound to all interfaces).
            try
            {
                _telemachusServer = new TcpHttpServer(TelemachusPort);
                new TelemachusDatalink().RegisterRoutes(_telemachusServer);
                _telemachusServer.Start();
                Console.WriteLine($"Telemachus datalink started on http://0.0.0.0:{TelemachusPort}/telemachus/datalink");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start Telemachus datalink server: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        [StarMapImmediateLoad]
        public void OnImmediatLoad(KSA.Mod definingMod)
        {
        }

        [StarMapUnload]
        public void Unload()
        {
            try
            {
                _httpServer?.Dispose();
                _httpServer = null;
                _telemachusServer?.Dispose();
                _telemachusServer = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping REST server: {ex.Message}");
            }

            MainThreadDispatcher.Clear();
            TelemachusDatalink.ResetDeltaV();
            Patcher.Unload();
        }
    }
}
