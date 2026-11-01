// Minimal compile-time stub of the KSA types used by KittenRemoteControl.
//
// IMPORTANT: member KIND (field vs property vs method), SIGNATURES and enum VALUES here must
// match the real KSA.dll exactly. The mod binds to the real assembly at runtime by member
// reference, so e.g. declaring a real field as a property emits a get_X() call that the runtime
// cannot find ("MissingMethodException"). These definitions mirror KSA.dll (KSA 2026.7.x).
using System;

namespace KSA
{
    public static class Program
    {
        // Real KSA: a public static FIELD (not a property).
        public static Vehicle? ControlledVehicle;

        // Real KSA: a public static PROPERTY (get) — current controlled-vehicle altitude in km.
        public static double CurrentAltitudeKm { get; set; }
    }

    public class Vehicle
    {
        public Orbit? Orbit { get; set; }
        public FlightComputer FlightComputer { get; set; } = new FlightComputer();

        private NavBallData _navBallData;
        // Real KSA: a ref READONLY property over a NavBallData struct. The getter return carries an
        // InAttribute modreq; declaring plain `ref` here emits a getter signature the runtime can't
        // bind ("Method not found: KSA.NavBallData ByRef ... get_NavBallData()").
        public ref readonly NavBallData NavBallData => ref _navBallData;

        public double OrbitalSpeed { get; set; }
        public float PropellantMass { get; set; }
        public float TotalMass { get; set; }

        // Real KSA: public field. Root of the part/staging tree.
        public PartTree Parts = new PartTree();

        // Private field accessed by reflection in ManualControlHelper.
        private ManualControlInputs _manualControlInputs;

        public void SetNavBallFrame(VehicleReferenceFrame frame) { /* noop in stub */ }
        public void SetStabilization(bool on) { /* noop in stub */ }
    }

    public class Orbit
    {
        public double Apoapsis { get; set; }
        public double Periapsis { get; set; }
        public IParentBody? Parent { get; set; }
    }

    public interface IParentBody
    {
        double GetNearSurfaceRadius();
    }

    // Staging: real KSA fires the next stage via SequenceList.ActivateNextSequence(vehicle),
    // reached from the vehicle's part tree (Vehicle.Parts.SequenceList).
    public class PartTree
    {
        public SequenceList SequenceList = new SequenceList();
        public SequencePerformanceList PerformanceSequences = new SequencePerformanceList();
    }

    public class SequenceList
    {
        public void ActivateNextSequence(Vehicle vehicle) { /* noop in stub */ }
    }

    // Per-stage performance. Real KSA: a struct with many fields; we read propellant + delta-v.
    public struct SequencePerformance
    {
        public float DeltaV;   // stage delta-v (m/s)
        public float Thrust;   // stage thrust (N) — >0 means this stage is firing
    }

    public class SequencePerformanceList
    {
        // Real KSA: property returning ReadOnlySpan<SequencePerformance> by value.
        public System.ReadOnlySpan<SequencePerformance> PerformanceSequences => default;
        // Total delta-v across all stages (m/s).
        public float TotalDeltaV => 0;
    }

    public class FlightComputer
    {
        // Real KSA: a public FIELD (not a property).
        public FlightComputerAttitudeMode AttitudeMode;
        public void RateHold(VehicleReferenceFrame frame) { /* noop */ }
    }

    public enum FlightComputerAttitudeMode
    {
        Manual = 0,
        Auto = 1,
    }

    // Real KSA: a struct; Frame and Altitude are public fields.
    public struct NavBallData
    {
        public VehicleReferenceFrame Frame;
        public double Altitude;
    }

    public enum VehicleReferenceFrame
    {
        EclBody = 0,
        EnuBody = 1,
        Lvlh = 2,
        VlfBody = 3,
        BurnBody = 4,
        Dock = 5,
    }

    // Manual control inputs struct, accessed via reflection in ManualControlHelper.
    public struct ManualControlInputs
    {
        public bool EngineOn;
        public float EngineThrottle;
        public ThrusterMapFlags ThrusterCommandFlags;
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
