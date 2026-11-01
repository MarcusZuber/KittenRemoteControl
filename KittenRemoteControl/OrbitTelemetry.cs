using System.Globalization;

namespace KittenRemoteControl;

internal static class OrbitTelemetry
{
    // IParentBody does not expose MeanRadius in current KSA. Use the concrete body's
    // sea-level radius for both APIs, rather than its terrain/near-surface radius.
    internal static double MeanRadius(object? body)
    {
        if (body == null) return 0.0;
        var property = body.GetType().GetProperty("MeanRadius")
            ?? throw new InvalidOperationException($"{body.GetType().Name} does not expose MeanRadius.");
        return Convert.ToDouble(property.GetValue(body), CultureInfo.InvariantCulture);
    }
}
