using KSA;
using StarMap.API;

namespace KittenRemoteControl
{
    public class RemoteControlMain : IStarMapMod, IStarMapOnUi
    {
        public bool ImmediateUnload => false;
        private SimpleSocketServer? _server;

        public void OnAfterUi(double dt)
        {
        }

        public void OnBeforeUi(double dt)
        {
        }

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

                // GET /telemetry/apoapasis - Apoapsis value
                _server.RegisterGet("/telemetry/apoapsis", () =>
                {
                    try
                    {
                        var v = Program.ControlledVehicle;
                        var orbit = v?.Orbit;
                        if (orbit == null) return "0";
                        var val = (object)orbit.Apoapsis;
                        if (val is IConvertible)
                        {
                            var d = Convert.ToDouble(val, System.Globalization.CultureInfo.InvariantCulture);
                            return d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                        }

                        return "0";
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
                        if (val is IConvertible)
                        {
                            var d = Convert.ToDouble(val, System.Globalization.CultureInfo.InvariantCulture);
                            return d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                        }

                        return "0";
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

        public void OnImmediatLoad()
        {
        }

        public void Unload()
        {
            _server?.Dispose();
            Patcher.Unload();
        }

        /// <summary>
        /// Helper to read a value from _manualControlInputs
        ///
        /// For some reason, they do not want us to directly set the value on the struct, so we have to do some reflection magic.
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
        ///
        /// For some reason, they do not want us to directly set the value on the struct, so we have to do some reflection magic.
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