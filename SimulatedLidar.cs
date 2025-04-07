using System;
using Godot;

public partial class SimulatedLidar : RayCast3D
{
    [Signal]
    public delegate void OnSurfaceDetectedEventHandler(float distance);

    [Export]
    public double PulseInterval { get; set; } = 0.2;

    [Export]
    public double NoiseVariance { get; set; } = 1;

    private double timeSinceLastPulse = 0;

    public override void _Process(double delta)
    {
        base._Process(delta);

        timeSinceLastPulse += delta;

        if (timeSinceLastPulse < PulseInterval)
            return;

        timeSinceLastPulse = 0;

        if (!IsColliding())
        {
            EmitSignal(SignalName.OnSurfaceDetected, 0);
            return;
        }
        var point = GetCollisionPoint();

        float distance = GlobalPosition.DistanceTo(point);

        EmitSignal(SignalName.OnSurfaceDetected, Math.Max(0, distance + Random.Shared.NextSingle() * NoiseVariance));
    }

}
