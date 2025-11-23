using System;
using System.Reflection;
using KSA;

namespace KittenRemoteControl
{
    /// <summary>
    /// Helper class to access private _manualControlInputs via reflection
    /// </summary>
    public static class ManualControlHelper
    {
        /// <summary>
        /// Helper to read a value from _manualControlInputs
        /// </summary>
        public static T? GetManualControlValue<T>(string fieldName)
        {
            var vehicle = Program.ControlledVehicle;

            if (vehicle == null) return default;

            var vehicleType = vehicle.GetType();
            var field = vehicleType.GetField("_manualControlInputs",
                BindingFlags.NonPublic |
                BindingFlags.Instance);

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
        public static void SetManualControlValue<T>(string fieldName, T value)
        {
            var vehicle = Program.ControlledVehicle;
            if (vehicle == null)
                throw new Exception("No vehicle controlled");

            var vehicleType = vehicle.GetType();
            var field = vehicleType.GetField("_manualControlInputs",
                BindingFlags.NonPublic |
                BindingFlags.Instance);

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
