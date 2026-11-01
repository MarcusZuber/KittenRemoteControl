namespace KittenRemoteControl;

internal sealed class StageDeltaVTracker
{
    private readonly object _gate = new();
    private object? _vehicle;
    private int _stageIndex = -1;
    private double _peak = -1;

    internal (double current, double max) Update(object? vehicle, int stageIndex, double current)
    {
        lock (_gate)
        {
            if (vehicle == null || stageIndex < 0)
            {
                ResetCore();
                return (-1, -1);
            }
            if (!ReferenceEquals(vehicle, _vehicle) || stageIndex != _stageIndex)
            {
                _vehicle = vehicle;
                _stageIndex = stageIndex;
                _peak = current;
            }
            _peak = Math.Max(_peak, current);
            return (current, _peak);
        }
    }

    internal void Reset()
    {
        lock (_gate) ResetCore();
    }

    private void ResetCore()
    {
        _vehicle = null;
        _stageIndex = -1;
        _peak = -1;
    }
}
