using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Grapevine;
using KSA;

namespace KittenRemoteControl
{
    /// <summary>
    /// REST API endpoints for remote control of the spacecraft
    /// </summary>
    [RestResource]
    public class RemoteControlResource
    {
        // Helper method to send JSON response
        private async Task SendJsonAsync(IHttpContext context, object data, HttpStatusCode? statusCode = null)
        {
            context.Response.StatusCode = statusCode ?? HttpStatusCode.Ok;
            var json = JsonSerializer.Serialize(data);
            await context.Response.SendResponseAsync(json);
        }

        // Helper method to read request body
        private string GetRequestBody(IHttpContext context)
        {
            using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        // ===== Control Endpoints =====

        [RestRoute("Get", "/control/throttle")]
        public async Task<IHttpContext> GetThrottle(IHttpContext context)
        {
            try
            {
                var throttle = ManualControlHelper.GetManualControlValue<float>("EngineThrottle");
                await SendJsonAsync(context, new { throttle });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.InternalServerError);
            }
            return context;
        }

        [RestRoute("Put", "/control/throttle")]
        public async Task<IHttpContext> SetThrottle(IHttpContext context)
        {
            try
            {
                var body = GetRequestBody(context);
                float throttle;

                // Try to parse as JSON object with "throttle" property, or as plain number
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
                    await SendJsonAsync(context, new { error = $"Throttle must be between 0.0 and 1.0, got {throttle}" }, HttpStatusCode.BadRequest);
                    return context;
                }

                ManualControlHelper.SetManualControlValue("EngineThrottle", throttle);
                await SendJsonAsync(context, new { success = true, throttle });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.BadRequest);
            }
            return context;
        }

        [RestRoute("Get", "/control/engineOn")]
        public async Task<IHttpContext> GetEngineOn(IHttpContext context)
        {
            try
            {
                var engineOn = ManualControlHelper.GetManualControlValue<bool>("EngineOn");
                await SendJsonAsync(context, new { engineOn });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.InternalServerError);
            }
            return context;
        }

        [RestRoute("Put", "/control/engineOn")]
        public async Task<IHttpContext> SetEngineOn(IHttpContext context)
        {
            try
            {
                var body = GetRequestBody(context);
                bool engineOn;

                // Try to parse as JSON object with "engineOn" property, or as plain boolean/number
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
                await SendJsonAsync(context, new { success = true, engineOn });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.BadRequest);
            }
            return context;
        }

        [RestRoute("Get", "/control/referenceFrame")]
        public async Task<IHttpContext> GetReferenceFrame(IHttpContext context)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                if (vehicle == null)
                {
                    await SendJsonAsync(context, new { frame = "None", frameId = -1 });
                    return context;
                }

                var frame = vehicle.NavBallData.Frame;
                await SendJsonAsync(context, new { frame = frame.ToString(), frameId = (int)frame });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.InternalServerError);
            }
            return context;
        }

        [RestRoute("Put", "/control/referenceFrame")]
        public async Task<IHttpContext> SetReferenceFrame(IHttpContext context)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                if (vehicle == null)
                {
                    await SendJsonAsync(context, new { error = "No vehicle controlled" }, HttpStatusCode.BadRequest);
                    return context;
                }

                var body = GetRequestBody(context);
                VehicleReferenceFrame frame;

                // Try to parse as JSON object with "frame" property, or as plain string/number
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

                await SendJsonAsync(context, new { success = true, frame = frame.ToString(), frameId = (int)frame });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.BadRequest);
            }
            return context;
        }

        [RestRoute("Get", "/control/referenceFrames")]
        public async Task<IHttpContext> GetReferenceFrames(IHttpContext context)
        {
            try
            {
                var names = Enum.GetNames<VehicleReferenceFrame>();
                var values = Enum.GetValues<VehicleReferenceFrame>();
                var frames = names.Select((name, i) => new { name, value = (int)values[i] }).ToArray();
                await SendJsonAsync(context, new { frames });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.InternalServerError);
            }
            return context;
        }

        // ===== Flight Computer Endpoints =====

        [RestRoute("Get", "/control/flightComputer/attitudeMode")]
        public async Task<IHttpContext> GetAttitudeMode(IHttpContext context)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var mode = vehicle?.FlightComputer.AttitudeMode;
                await SendJsonAsync(context, new { attitudeMode = mode?.ToString(), modeId = (int?)mode });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.InternalServerError);
            }
            return context;
        }

        [RestRoute("Put", "/control/flightComputer/attitudeMode")]
        public async Task<IHttpContext> SetAttitudeMode(IHttpContext context)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                if (vehicle == null)
                {
                    await SendJsonAsync(context, new { error = "No vehicle controlled" }, HttpStatusCode.BadRequest);
                    return context;
                }

                var body = GetRequestBody(context);
                FlightComputerAttitudeMode mode;

                // Try to parse as JSON or plain value
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
                await SendJsonAsync(context, new { success = true, attitudeMode = mode.ToString(), modeId = (int)mode });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.BadRequest);
            }
            return context;
        }

        [RestRoute("Get", "/control/flightComputer/attitudeModes")]
        public async Task<IHttpContext> GetAttitudeModes(IHttpContext context)
        {
            try
            {
                var names = Enum.GetNames<FlightComputerAttitudeMode>();
                var values = Enum.GetValues<FlightComputerAttitudeMode>();
                var modes = names.Select((name, i) => new { name, value = (int)values[i] }).ToArray();
                await SendJsonAsync(context, new { modes });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.InternalServerError);
            }
            return context;
        }

        [RestRoute("Put", "/control/flightComputer/stabilization")]
        public async Task<IHttpContext> SetStabilization(IHttpContext context)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                if (vehicle == null)
                {
                    await SendJsonAsync(context, new { error = "No vehicle controlled" }, HttpStatusCode.BadRequest);
                    return context;
                }

                var body = GetRequestBody(context);
                bool stabilization;

                // Try to parse as JSON or plain value
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
                await SendJsonAsync(context, new { success = true, stabilization });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.BadRequest);
            }
            return context;
        }

        // ===== Telemetry Endpoints =====

        [RestRoute("Get", "/telemetry/apoapsis")]
        public async Task<IHttpContext> GetApoapsis(IHttpContext context)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var orbit = vehicle?.Orbit;
                if (orbit == null)
                {
                    await SendJsonAsync(context, new { apoapsis = 0.0 });
                    return context;
                }

                var val = (object)orbit.Apoapsis;
                var apoapsis = val is IConvertible ? Convert.ToDouble(val, CultureInfo.InvariantCulture) : 0.0;
                await SendJsonAsync(context, new { apoapsis });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.InternalServerError);
            }
            return context;
        }

        [RestRoute("Get", "/telemetry/periapsis")]
        public async Task<IHttpContext> GetPeriapsis(IHttpContext context)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var orbit = vehicle?.Orbit;
                if (orbit == null)
                {
                    await SendJsonAsync(context, new { periapsis = 0.0 });
                    return context;
                }

                var val = (object)orbit.Periapsis;
                var periapsis = val is IConvertible ? Convert.ToDouble(val, CultureInfo.InvariantCulture) : 0.0;
                await SendJsonAsync(context, new { periapsis });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.InternalServerError);
            }
            return context;
        }

        [RestRoute("Get", "/telemetry/orbitingBody/meanRadius")]
        public async Task<IHttpContext> GetMeanRadius(IHttpContext context)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var meanRadius = vehicle?.Orbit.Parent.MeanRadius;
                var radius = meanRadius.HasValue ? Convert.ToDouble(meanRadius, CultureInfo.InvariantCulture) : 0.0;
                await SendJsonAsync(context, new { meanRadius = radius });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.InternalServerError);
            }
            return context;
        }

        [RestRoute("Get", "/telemetry/apoapsis_elevation")]
        public async Task<IHttpContext> GetApoapsisElevation(IHttpContext context)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var orbit = vehicle?.Orbit;
                if (orbit == null)
                {
                    await SendJsonAsync(context, new { apoapsisElevation = 0.0 });
                    return context;
                }

                var apo = orbit.Apoapsis;
                var meanRadius = orbit.Parent.MeanRadius;

                var apoVal = Convert.ToDouble(apo, CultureInfo.InvariantCulture);
                var radiusVal = Convert.ToDouble(meanRadius, CultureInfo.InvariantCulture);
                var elevation = apoVal - radiusVal;
                
                await SendJsonAsync(context, new { apoapsisElevation = elevation });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.InternalServerError);
            }
            return context;
        }

        [RestRoute("Get", "/telemetry/periapsis_elevation")]
        public async Task<IHttpContext> GetPeriapsisElevation(IHttpContext context)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var orbit = vehicle?.Orbit;
                if (orbit == null)
                {
                    await SendJsonAsync(context, new { periapsisElevation = 0.0 });
                    return context;
                }

                var peri = orbit.Periapsis;
                var meanRadius = orbit.Parent.MeanRadius;

                var periVal = Convert.ToDouble(peri, CultureInfo.InvariantCulture);
                var radiusVal = Convert.ToDouble(meanRadius, CultureInfo.InvariantCulture);
                var elevation = periVal - radiusVal;
                
                await SendJsonAsync(context, new { periapsisElevation = elevation });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.InternalServerError);
            }
            return context;
        }

        [RestRoute("Get", "/telemetry/orbitalSpeed")]
        public async Task<IHttpContext> GetOrbitalSpeed(IHttpContext context)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var speed = vehicle?.OrbitalSpeed;
                var orbitalSpeed = speed.HasValue ? Convert.ToDouble(speed, CultureInfo.InvariantCulture) : 0.0;
                await SendJsonAsync(context, new { orbitalSpeed });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.InternalServerError);
            }
            return context;
        }

        [RestRoute("Get", "/telemetry/propellantMass")]
        public async Task<IHttpContext> GetPropellantMass(IHttpContext context)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var mass = vehicle?.PropellantMass;
                var propellantMass = mass.HasValue ? Convert.ToDouble(mass, CultureInfo.InvariantCulture) : 0.0;
                await SendJsonAsync(context, new { propellantMass });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.InternalServerError);
            }
            return context;
        }

        [RestRoute("Get", "/telemetry/totalMass")]
        public async Task<IHttpContext> GetTotalMass(IHttpContext context)
        {
            try
            {
                var vehicle = Program.ControlledVehicle;
                var mass = vehicle?.TotalMass;
                var totalMass = mass.HasValue ? Convert.ToDouble(mass, CultureInfo.InvariantCulture) : 0.0;
                await SendJsonAsync(context, new { totalMass });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(context, new { error = ex.Message }, HttpStatusCode.InternalServerError);
            }
            return context;
        }
    }
}

