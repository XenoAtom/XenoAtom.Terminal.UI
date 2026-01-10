using System.Diagnostics;

namespace XenoAtom.Terminal.UI.ControlsDemo;

public sealed class DemoRuntime
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private double _t;
    private long _lastMs;

    public State<long> Frame { get; } = new(0);

    public State<double> Pulse01 { get; } = new(0.0);

    public State<double> Progress01 { get; } = new(0.0);

    public bool Advance()
    {
        // Drive state updates ~60Hz.
        var nowMs = _stopwatch.ElapsedMilliseconds;
        if (nowMs - _lastMs < 16)
        {
            return true;
        }

        _lastMs = nowMs;
        Frame.Value++;

        _t += 0.05;
        Pulse01.Value = (Math.Sin(_t) + 1.0) / 2.0;

        var p = Progress01.Value + 0.01;
        if (p > 1.0) p = 0.0;
        Progress01.Value = p;

        return true;
    }
}

