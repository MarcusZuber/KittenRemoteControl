using KSA;
using StarMap.API;
using Grapevine;

namespace KittenRemoteControl
{
    [StarMapMod]
    public class RemoteControlMain
    {
        private IRestServer? _restServer;

        [StarMapAfterGui]
        public void OnAfterUi(double dt)
        {
        }

        [StarMapAllModsLoaded]
        public void OnFullyLoaded()
        {
            Patcher.Patch();

            // Initialize Grapevine REST server
            try
            {
                _restServer = RestServerBuilder.UseDefaults()
                    .Build();
                
                // Set the port via Prefixes
                _restServer.Prefixes.Add("http://localhost:8080/");

                _restServer.Start();
                Console.WriteLine("Remote Control REST Server started successfully on http://localhost:8080");
                Console.WriteLine("Available endpoints:");
                Console.WriteLine("  GET/PUT /control/throttle");
                Console.WriteLine("  GET/PUT /control/engineOn");
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
        }

        [StarMapImmediateLoad]
        public void OnImmediatLoad()
        {
        }

        [StarMapUnload]
        public void Unload()
        {
            try
            {
                _restServer?.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping REST server: {ex.Message}");
            }
            
            Patcher.Unload();
        }
    }
}

