using System.Globalization;
using System.Linq;
using System.Text.Json;
using KSA;

namespace KittenRemoteControl
{
    /// <summary>
    /// REST API endpoints for remote control of the spacecraft.
    /// Registers its routes on a <see cref="TcpHttpServer"/> (no HttpListener dependency).
    /// </summary>
    public class RemoteControlResource
    {
        /// <summary>Registers every control and telemetry route on the given server.</summary>
        public void RegisterRoutes(TcpHttpServer server)
        {
            // ===== Control =====
            server.MapGet("/control/throttle", GetThrottle);
            server.MapPut("/control/throttle", SetThrottle);
            server.MapGet("/control/engineOn", GetEngineOn);
            server.MapPut("/control/engineOn", SetEngineOn);
            server.MapGet("/control/thrusters", GetThrusters);
            server.MapPost("/control/thrusters", SetThrusters);
            server.MapGet("/control/referenceFrame", GetReferenceFrame);
            server.MapPut("/control/referenceFrame", SetReferenceFrame);
            server.MapGet("/control/referenceFrames", GetReferenceFrames);

            // ===== Flight Computer =====
            server.MapGet("/control/flightComputer/attitudeMode", GetAttitudeMode);
            server.MapPut("/control/flightComputer/attitudeMode", SetAttitudeMode);
            server.MapGet("/control/flightComputer/attitudeModes", GetAttitudeModes);
            server.MapPut("/control/flightComputer/stabilization", SetStabilization);

            // ===== Telemetry =====
            server.MapGet("/telemetry/apoapsis", GetApoapsis);
            server.MapGet("/telemetry/periapsis", GetPeriapsis);
            server.MapGet("/telemetry/orbitingBody/meanRadius", GetMeanRadius);
            server.MapGet("/telemetry/apoapsis_elevation", GetApoapsisElevation);
            server.MapGet("/telemetry/periapsis_elevation", GetPeriapsisElevation);
            server.MapGet("/telemetry/orbitalSpeed", GetOrbitalSpeed);
            server.MapGet("/telemetry/propellantMass", GetPropellantMass);
            server.MapGet("/telemetry/totalMass", GetTotalMass);
        }

        // Helper to build a JSON response.
        private static HttpResponse Json(object data, int statusCode = 200)
            => new() { StatusCode = statusCode, Body = JsonSerializer.Serialize(data) };

        // ===== Control Endpoints =====

        private HttpResponse GetThrottle(HttpRequest request)
        {
            try
            {
                var throttle = ManualControlHelper.GetManualControlValue<float>("EngineThrottle");
                return Json(new { throttle });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 500);
            }
        }

        private HttpResponse SetThrottle(HttpRequest request)
        {
            try
            {
                var body = request.Body;
                float throttle;

                // Try to parse as JSON object with "throttle" property, or as plain number.
                if (body.Contains("throttle"))
                {
                    var json = JsonDocument.Parse(body);
                    throttle = json.RootElement.GetProperty("throttle").GetSingle();
                }
                else
                {
                    throttle = float.Parse(body, NumberStyles.Float, CultureInfo.InvariantCulture);
                }

                if (throttle is < 0.0f or > 1.0f)
                {
                    return Json(new { error = $"Throttle must be between 0.0 and 1.0, got {throttle}" }, 400);
                }

                ManualControlHelper.SetManualControlValue("EngineThrottle", throttle);
                return Json(new { success = true, throttle });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 400);
            }
        }

        private HttpResponse GetEngineOn(HttpRequest request)
        {
            try
            {
                var engineOn = ManualControlHelper.GetManualControlValue<bool>("EngineOn");
                return Json(new { engineOn });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 500);
            }
        }

        private HttpResponse SetEngineOn(HttpRequest request)
        {
            try
            {
                var body = request.Body;
                bool engineOn;

                // Try to parse as JSON object with "engineOn" property, or as plain boolean/number.
                if (body.Contains("engineOn"))
                {
                    var json = JsonDocument.Parse(body);
                    engineOn = json.RootElement.GetProperty("engineOn").GetBoolean();
                }
                else
                {
                    engineOn = body.Trim() is "1" or "true" or "True";
                }

                ManualControlHelper.SetManualControlValue("EngineOn", engineOn);
                return Json(new { success = true, engineOn });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 400);
            }
        }

        // ===== Thruster endpoints =====

        private HttpResponse GetThrusters(HttpRequest request)
        {
            try
            {
                // Read the combined ThrusterCommandFlags from the private inputs struct.
                var flagsObj = ManualControlHelper.GetManualControlValue<object>("ThrusterCommandFlags");
                var flags = flagsObj is ThrusterMapFlags f ? f : ThrusterMapFlags.None;

                var thrusters = Enum.GetValues<ThrusterMapFlags>()
                    .Cast<ThrusterMapFlags>()
                    .Where(x => x != ThrusterMapFlags.None)
                    .ToDictionary(x => x.ToString(), x => flags.HasFlag(x));

                return Json(new { thrusters });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 500);
            }
        }

        private HttpResponse SetThrusters(HttpRequest request)
        {
            try
            {
                var body = request.Body;
                JsonElement thrusterObj;

                // Accept either a top-level object with a "thrusters" property or the object itself.
                if (body.Contains("\"thrusters\""))
                {
                    var json = JsonDocument.Parse(body);
                    thrusterObj = json.RootElement.GetProperty("thrusters");
                }
                else
                {
                    var json = JsonDocument.Parse(body);
                    thrusterObj = json.RootElement;
                }

                if (thrusterObj.ValueKind != JsonValueKind.Object)
                {
                    return Json(new { error = "Request must be a JSON object mapping thruster names to boolean/number values" }, 400);
                }

                var currentObj = ManualControlHelper.GetManualControlValue<object>("ThrusterCommandFlags");
                var current = currentObj is ThrusterMapFlags cf ? cf : ThrusterMapFlags.None;
                var newFlags = current;
                var unknownKeys = new System.Collections.Generic.List<string>();

                foreach (var prop in thrusterObj.EnumerateObject())
                {
                    var name = prop.Name;
                    if (!Enum.TryParse<ThrusterMapFlags>(name, true, out var flag))
                    {
                        unknownKeys.Add(name);
                        continue;
                    }

                    var val = prop.Value;
                    bool on;

                    if (val.ValueKind == JsonValueKind.True || val.ValueKind == JsonValueKind.False)
                    {
                        on = val.GetBoolean();
                    }
                    else if (val.ValueKind == JsonValueKind.Number)
                    {
                        on = val.GetDouble() != 0.0;
                    }
                    else if (val.ValueKind == JsonValueKind.String)
                    {
                        var s = val.GetString();
                        on = s == "1" || s!.Equals("true", StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        unknownKeys.Add(name);
                        continue;
                    }

                    if (on) newFlags |= flag; else newFlags &= ~flag;
                }

                if (unknownKeys.Count > 0)
                {
                    return Json(new { error = "Unknown thruster keys", unknown = unknownKeys }, 400);
                }

                ManualControlHelper.SetManualControlValue("ThrusterCommandFlags", newFlags);

                var thrusters = Enum.GetValues<ThrusterMapFlags>()
                    .Cast<ThrusterMapFlags>()
                    .Where(x => x != ThrusterMapFlags.None)
                    .ToDictionary(x => x.ToString(), x => newFlags.HasFlag(x));

                return Json(new { success = true, thrusters });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 400);
            }
        }

        private HttpResponse GetReferenceFrame(HttpRequest request)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                if (vehicle == null)
                {
                    return Json(new { frame = "None", frameId = -1 });
                }

                var frame = vehicle.NavBallData.Frame;
                return Json(new { frame = frame.ToString(), frameId = (int)frame });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 500);
            }
        }

        private HttpResponse SetReferenceFrame(HttpRequest request)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                if (vehicle == null)
                {
                    return Json(new { error = "No vehicle controlled" }, 400);
                }

                var body = request.Body;
                VehicleReferenceFrame frame;

                // Try to parse as JSON object with "frame" property, or as plain string/number.
                if (body.Contains("frame"))
                {
                    var json = JsonDocument.Parse(body);
                    var frameValue = json.RootElement.GetProperty("frame");

                    if (frameValue.ValueKind == JsonValueKind.Number)
                    {
                        frame = (VehicleReferenceFrame)frameValue.GetInt32();
                    }
                    else
                    {
                        frame = Enum.Parse<VehicleReferenceFrame>(frameValue.GetString()!, true);
                    }
                }
                else if (int.TryParse(body.Trim(), CultureInfo.InvariantCulture, out var numeric))
                {
                    frame = (VehicleReferenceFrame)numeric;
                }
                else
                {
                    frame = Enum.Parse<VehicleReferenceFrame>(body.Trim(), true);
                }

                vehicle.SetNavBallFrame(frame);
                if (vehicle.FlightComputer.AttitudeMode == FlightComputerAttitudeMode.Auto)
                    vehicle.FlightComputer.RateHold(frame);

                return Json(new { success = true, frame = frame.ToString(), frameId = (int)frame });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 400);
            }
        }

        private HttpResponse GetReferenceFrames(HttpRequest request)
        {
            try
            {
                var names = Enum.GetNames<VehicleReferenceFrame>();
                var values = Enum.GetValues<VehicleReferenceFrame>();
                var frames = names.Select((name, i) => new { name, value = (int)values[i] }).ToArray();
                return Json(new { frames });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 500);
            }
        }

        // ===== Flight Computer Endpoints =====

        private HttpResponse GetAttitudeMode(HttpRequest request)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var mode = vehicle?.FlightComputer.AttitudeMode;
                return Json(new { attitudeMode = mode?.ToString(), modeId = (int?)mode });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 500);
            }
        }

        private HttpResponse SetAttitudeMode(HttpRequest request)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                if (vehicle == null)
                {
                    return Json(new { error = "No vehicle controlled" }, 400);
                }

                var body = request.Body;
                FlightComputerAttitudeMode mode;

                // Try to parse as JSON or plain value.
                if (body.Contains("mode"))
                {
                    var json = JsonDocument.Parse(body);
                    var modeValue = json.RootElement.GetProperty("mode");

                    if (modeValue.ValueKind == JsonValueKind.Number)
                    {
                        mode = (FlightComputerAttitudeMode)modeValue.GetInt32();
                    }
                    else
                    {
                        mode = Enum.Parse<FlightComputerAttitudeMode>(modeValue.GetString()!, true);
                    }
                }
                else if (int.TryParse(body.Trim(), CultureInfo.InvariantCulture, out var numeric))
                {
                    mode = (FlightComputerAttitudeMode)numeric;
                }
                else
                {
                    mode = Enum.Parse<FlightComputerAttitudeMode>(body.Trim(), true);
                }

                vehicle.FlightComputer.AttitudeMode = mode;
                return Json(new { success = true, attitudeMode = mode.ToString(), modeId = (int)mode });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 400);
            }
        }

        private HttpResponse GetAttitudeModes(HttpRequest request)
        {
            try
            {
                var names = Enum.GetNames<FlightComputerAttitudeMode>();
                var values = Enum.GetValues<FlightComputerAttitudeMode>();
                var modes = names.Select((name, i) => new { name, value = (int)values[i] }).ToArray();
                return Json(new { modes });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 500);
            }
        }

        private HttpResponse SetStabilization(HttpRequest request)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                if (vehicle == null)
                {
                    return Json(new { error = "No vehicle controlled" }, 400);
                }

                var body = request.Body;
                bool stabilization;

                // Try to parse as JSON or plain value.
                if (body.Contains("stabilization"))
                {
                    var json = JsonDocument.Parse(body);
                    stabilization = json.RootElement.GetProperty("stabilization").GetBoolean();
                }
                else
                {
                    stabilization = body.Trim() is "1" or "true" or "True";
                }

                vehicle.SetStabilization(stabilization);
                return Json(new { success = true, stabilization });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 400);
            }
        }

        // ===== Telemetry Endpoints =====

        private HttpResponse GetApoapsis(HttpRequest request)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var orbit = vehicle?.Orbit;
                if (orbit == null)
                {
                    return Json(new { apoapsis = 0.0 });
                }

                var val = (object)orbit.Apoapsis;
                var apoapsis = val is IConvertible ? Convert.ToDouble(val, CultureInfo.InvariantCulture) : 0.0;
                return Json(new { apoapsis });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 500);
            }
        }

        private HttpResponse GetPeriapsis(HttpRequest request)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var orbit = vehicle?.Orbit;
                if (orbit == null)
                {
                    return Json(new { periapsis = 0.0 });
                }

                var val = (object)orbit.Periapsis;
                var periapsis = val is IConvertible ? Convert.ToDouble(val, CultureInfo.InvariantCulture) : 0.0;
                return Json(new { periapsis });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 500);
            }
        }

        private HttpResponse GetMeanRadius(HttpRequest request)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var radius = OrbitTelemetry.MeanRadius(vehicle?.Orbit?.Parent);
                return Json(new { meanRadius = radius });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 500);
            }
        }

        private HttpResponse GetApoapsisElevation(HttpRequest request)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var orbit = vehicle?.Orbit;
                if (orbit == null)
                {
                    return Json(new { apoapsisElevation = 0.0 });
                }

                var apoVal = orbit.Apoapsis;
                var radiusVal = OrbitTelemetry.MeanRadius(orbit.Parent);
                var elevation = apoVal - radiusVal;

                return Json(new { apoapsisElevation = elevation });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 500);
            }
        }

        private HttpResponse GetPeriapsisElevation(HttpRequest request)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var orbit = vehicle?.Orbit;
                if (orbit == null)
                {
                    return Json(new { periapsisElevation = 0.0 });
                }

                var periVal = orbit.Periapsis;
                var radiusVal = OrbitTelemetry.MeanRadius(orbit.Parent);
                var elevation = periVal - radiusVal;

                return Json(new { periapsisElevation = elevation });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 500);
            }
        }

        private HttpResponse GetOrbitalSpeed(HttpRequest request)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var speed = vehicle?.OrbitalSpeed;
                var orbitalSpeed = speed.HasValue ? Convert.ToDouble(speed, CultureInfo.InvariantCulture) : 0.0;
                return Json(new { orbitalSpeed });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 500);
            }
        }

        private HttpResponse GetPropellantMass(HttpRequest request)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var mass = vehicle?.PropellantMass;
                var propellantMass = mass.HasValue ? Convert.ToDouble(mass, CultureInfo.InvariantCulture) : 0.0;
                return Json(new { propellantMass });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 500);
            }
        }

        private HttpResponse GetTotalMass(HttpRequest request)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var mass = vehicle?.TotalMass;
                var totalMass = mass.HasValue ? Convert.ToDouble(mass, CultureInfo.InvariantCulture) : 0.0;
                return Json(new { totalMass });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, 500);
            }
        }
    }
}
