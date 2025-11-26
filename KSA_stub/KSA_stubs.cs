// Minimal stub of KSA types used by KittenRemoteControl
using System;

namespace KSA
{
    // Program-like holder for the currently controlled vehicle
    public static class Program
    {
        public static Vehicle? ControlledVehicle { get; set; }
    }

    public class Vehicle
    {
        public FlightComputer FlightComputer { get; set; } = new FlightComputer();
        public NavBall NavBallData { get; set; } = new NavBall();

        // Telemetry properties
        public double? OrbitalSpeed { get; set; }
        public double? PropellantMass { get; set; }
        public double? TotalMass { get; set; }

        public Orbit? Orbit { get; set; }

        // simulate private field via public property for stubbing
        public ManualControlInputs _manualControlInputs;

        public void SetNavBallFrame(VehicleReferenceFrame frame) { /* noop in stub */ }
        public void SetStabilization(bool on) { /* noop in stub */ }
    }

    public class FlightComputer
    {
        public FlightComputerAttitudeMode AttitudeMode { get; set; } = FlightComputerAttitudeMode.Auto;
        public void RateHold(VehicleReferenceFrame frame) { /* noop */ }
    }

    public enum FlightComputerAttitudeMode
    {
        Auto = 0,
        Manual = 1,
        Hold = 2
    }

    public class NavBall
    {
        public VehicleReferenceFrame Frame { get; set; } = VehicleReferenceFrame.LVLH;
    }

    public enum VehicleReferenceFrame
    {
        LVLH = 0,
        Inertial = 1,
        Surface = 2
    }

    public class Orbit
    {
        public double Apoapsis { get; set; }
        public double Periapsis { get; set; }
        public ParentBody Parent { get; set; } = new ParentBody();
    }

    public class ParentBody
    {
        public double? MeanRadius { get; set; }
    }

    // Minimal manual control inputs struct used via reflection in ManualControlHelper
    public struct ManualControlInputs
    {
        public float EngineThrottle;
        public bool EngineOn;

        // The combined thruster flags
        public ThrusterMapFlags ThrusterCommandFlags;

        // keep a field to demonstrate other possible members
        public int Padding;
    }

    [Flags]
    public enum ThrusterMapFlags
    {
        None = 0,
        RollRight = 2,
        RollLeft = 4,
        PitchUp = 8,
        PitchDown = 16,
        YawRight = 32,
        YawLeft = 64,
        TranslateForward = 128,
        TranslateBackward = 256,
        TranslateRight = 512,
        TranslateLeft = 1024,
        TranslateDown = 2048,
        TranslateUp = 4096,
    }
}

