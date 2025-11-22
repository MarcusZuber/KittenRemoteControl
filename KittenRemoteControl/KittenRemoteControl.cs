using KSA;
using StarMap.API;

namespace KittenRemoteControl
{
    [StarMapMod]
    public class RemoteControlMain
    {
        private SimpleSocketServer? _server;

        [StarMapAfterGui]
        public void OnAfterUi(double dt)
        {
        }


        [StarMapAllModsLoaded]
        public void OnFullyLoaded()
        {
            Patcher.Patch();

            // Initialize socket server
            try
            {
                _server = new SimpleSocketServer(8080);

                // GET /control/throttle - Get current throttle value
                _server.RegisterGet("/control/throttle", () =>
                {
                    var throttle = GetManualControlValue<float>("EngineThrottle");
                    return throttle.ToString(System.Globalization.CultureInfo.InvariantCulture);
                });

                // SET /control/throttle VALUE - Set throttle value (0.0-1.0)
                _server.RegisterSet("/control/throttle", (value) =>
                {
                    if (float.TryParse(value, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var throttle))
                    {
                        if (throttle is < 0.0f or > 1.0f)
                        {
                            throw new ArgumentException($"Throttle must be between 0.0 and 1.0, got {throttle}");
                        }

                        SetManualControlValue("EngineThrottle", throttle);
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"Invalid throttle value: '{value}'. Must be a number between 0.0 and 1.0");
                    }
                });

                // GET /control/engineOn - Get engine on/off status
                _server.RegisterGet("/control/engineOn", () =>
                {
                    var engineOn = GetManualControlValue<bool>("EngineOn");
                    return engineOn ? "1" : "0";
                });

                // SET /control/engineOn VALUE - Turn engine on/off (0 or 1)
                _server.RegisterSet("/control/engineOn", (value) =>
                {
                    if (int.TryParse(value, out var engineOnValue))
                    {
                        if (engineOnValue != 0 && engineOnValue != 1)
                        {
                            throw new ArgumentException($"EngineOn must be 0 or 1, got {engineOnValue}");
                        }

                        SetManualControlValue("EngineOn", engineOnValue == 1);
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid engineOn value: '{value}'. Must be 0 or 1");
                    }
                });

                _server.RegisterGet("/control/referenceFrame", () =>
                {
                    if (Program.ControlledVehicle == null) return "0";
                    var frame = Program.ControlledVehicle.NavBallData.Frame;
                    return frame.ToString();
                });

                _server.RegisterSet("/control/referenceFrame", (value) =>
                {
                    if (Program.ControlledVehicle == null)
                        throw new Exception("No vehicle controlled");

                    // Try to parse enum value
                    if (!Enum.TryParse<VehicleReferenceFrame>(value, true, out var frame))
                    {
                        // If parse failed access the numeric value directly
                        if (int.TryParse(value, out var numeric) &&
                            Enum.IsDefined(typeof(VehicleReferenceFrame), numeric))
                        {
                            frame = (VehicleReferenceFrame)numeric;
                        }
                        else
                        {
                            throw new ArgumentException($"Invalid reference frame: '{value}'");
                        }
                    }

                    // set the new frame
                    var v = Program.ControlledVehicle;
                    v.SetNavBallFrame(frame);
                    if (v.FlightComputer.AttitudeMode == FlightComputerAttitudeMode.Auto)
                        v.FlightComputer.RateHold(frame);
                });

                _server.RegisterGet("/control/referenceFrames", () =>
                {
                    var names = Enum.GetNames<VehicleReferenceFrame>();
                    return string.Join(",", names);
                });

                _server.RegisterGet("/control/FlightComputer/AttitudeMode", () =>
                {
                    var v = Program.ControlledVehicle;
                    return v?.FlightComputer.AttitudeMode.ToString();
                });

                _server.RegisterGet("/control/FlightComputer/AttitudeModes", () =>
                {
                    var names = Enum.GetNames<FlightComputerAttitudeMode>();
                    return string.Join(",", names);
                });

                _server.RegisterSet("/control/FlightComputer/AttitudeMode", (value) =>
                {
                    var v = Program.ControlledVehicle;
                    if (Enum.TryParse<FlightComputerAttitudeMode>(value, true, out var mode))
                    {
                        v?.FlightComputer.AttitudeMode = mode;
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid FlightComputer AttitudeMode: '{value}'");
                    }
                });

                _server.RegisterSet("/control/FlightComputer/stabilization", (value) =>
                {
                    var v = Program.ControlledVehicle;
                    v?.SetStabilization(value == "1");
                });

                // GET /telemetry/apoapasis - Apoapsis value
                _server.RegisterGet("/telemetry/apoapsis", () =>
                {
                    try
                    {
                        var v = Program.ControlledVehicle;
                        var orbit = v?.Orbit;
                        if (orbit == null) return "0";
                        var val = (object)orbit.Apoapsis;
                        if (val is not IConvertible) return "0";
                        var d = Convert.ToDouble(val, System.Globalization.CultureInfo.InvariantCulture);
                        return d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return "0";
                    }
                });


                // GET /telemetry/periapsis - Periapsis value
                _server.RegisterGet("/telemetry/periapsis", () =>
                {
                    try
                    {
                        var v = Program.ControlledVehicle;
                        var orbit = v?.Orbit;
                        if (orbit == null) return "0";
                        var val = (object)orbit.Periapsis;
                        if (val is not IConvertible) return "0";
                        var d = Convert.ToDouble(val, System.Globalization.CultureInfo.InvariantCulture);
                        return d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return "0";
                    }
                });

                // GET /telemetry/orbitingBody/meanRadius - Mean radius of the orbit parent body
                _server.RegisterGet("/telemetry/orbitingBody/meanRadius", () =>
                {
                    try
                    {
                        var v = Program.ControlledVehicle;
                        var mean = v?.Orbit.Parent.MeanRadius;
                        if (mean is null) return "0";
                        var d = Convert.ToDouble(mean, System.Globalization.CultureInfo.InvariantCulture);
                        return d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return "0";
                    }
                });

                // GET /telemetry/apoapsis_elevation - Apoapsis elevation above surface (apoapsis - meanRadius)
                _server.RegisterGet("/telemetry/apoapsis_elevation", () =>
                {
                    try
                    {
                        var v = Program.ControlledVehicle;
                        var orbit = v?.Orbit;
                        if (orbit == null) return "0";

                        var apo = orbit.Apoapsis;
                        double? meanRadius = orbit.Parent.MeanRadius;

                        var apoVal = Convert.ToDouble(apo, System.Globalization.CultureInfo.InvariantCulture);
                        var radiusVal = Convert.ToDouble(meanRadius, System.Globalization.CultureInfo.InvariantCulture);
                        var elevation = apoVal - radiusVal;
                        return elevation.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return "0";
                    }
                });

                // GET /telemetry/periapsis_elevation - Periapsis elevation above surface (periapsis - meanRadius)
                _server.RegisterGet("/telemetry/periapsis_elevation", () =>
                {
                    try
                    {
                        var v = Program.ControlledVehicle;
                        var orbit = v?.Orbit;
                        if (orbit == null) return "0";

                        var peri = orbit.Periapsis;
                        double? meanRadius = orbit.Parent.MeanRadius;

                        var periVal = Convert.ToDouble(peri, System.Globalization.CultureInfo.InvariantCulture);
                        var radiusVal = Convert.ToDouble(meanRadius, System.Globalization.CultureInfo.InvariantCulture);
                        var elevation = periVal - radiusVal;
                        return elevation.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return "0";
                    }
                });

                // GET /telemetry/orbitalSpeed - Current orbital speed
                _server.RegisterGet("/telemetry/orbitalSpeed", () =>
                {
                    try
                    {
                        var v = Program.ControlledVehicle;
                        var speed = v?.OrbitalSpeed;
                        if (speed == null) return "0";
                        var d = Convert.ToDouble(speed, System.Globalization.CultureInfo.InvariantCulture);
                        return d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return "0";
                    }
                });

                // GET /telemetry/propellantMass - Current propellant mass
                _server.RegisterGet("/telemetry/propellantMass", () =>
                {
                    try
                    {
                        var v = Program.ControlledVehicle;
                        var mass = v?.PropellantMass;
                        if (mass == null) return "0";
                        var d = Convert.ToDouble(mass, System.Globalization.CultureInfo.InvariantCulture);
                        return d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return "0";
                    }
                });

                // GET /telemetry/totalMass - Total vehicle mass
                _server.RegisterGet("/telemetry/totalMass", () =>
                {
                    try
                    {
                        var v = Program.ControlledVehicle;
                        var mass = v?.TotalMass;
                        if (mass == null) return "0";
                        var d = Convert.ToDouble(mass, System.Globalization.CultureInfo.InvariantCulture);
                        return d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return "0";
                    }
                });

                _server.Start();
                Console.WriteLine("Remote Control Socket Server started successfully on port 8080");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start Socket server: {ex.Message}");
            }
        }

        [StarMapImmediateLoad]
        public void OnImmediatLoad()
        {
        }

        [StarMapUnload]
        public void Unload()
        {
            _server?.Dispose();
            Patcher.Unload();
        }

        /// <summary>
        /// Helper to read a value from _manualControlInputs
        /// </summary>
        private static T? GetManualControlValue<T>(string fieldName)
        {
            var vehicle = Program.ControlledVehicle;

            if (vehicle == null) return default;

            var vehicleType = vehicle.GetType();
            var field = vehicleType.GetField("_manualControlInputs",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field == null) return default;

            var inputs = field.GetValue(vehicle);
            if (inputs == null) return default;

            var inputsType = inputs.GetType();
            var targetField = inputsType.GetField(fieldName);

            if (targetField == null) return default;

            return (T)targetField.GetValue(inputs)!;
        }

        /// <summary>
        /// Helper to set a value in _manualControlInputs
        /// </summary>
        private static void SetManualControlValue<T>(string fieldName, T value)
        {
            var vehicle = Program.ControlledVehicle;
            if (vehicle == null)
                throw new Exception("No vehicle controlled");

            var vehicleType = vehicle.GetType();
            var field = vehicleType.GetField("_manualControlInputs",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field == null)
                throw new Exception("_manualControlInputs field not found");

            var inputs = field.GetValue(vehicle);
            if (inputs == null)
                throw new Exception("_manualControlInputs is null");

            var inputsType = inputs.GetType();
            var targetField = inputsType.GetField(fieldName);

            if (targetField == null)
                throw new Exception($"{fieldName} field not found");

            // Set the value on the struct
            targetField.SetValue(inputs, value);

            // IMPORTANT: write the modified struct back
            field.SetValue(vehicle, inputs);
        }
    }
}