using System;
using Godot;

public partial class SimulatedLidar : RayCast3D
{

    [Export]
    public double PulseInterval { get; set; } = 0.2;

    [Export]
    public double NoiseVariance { get; set; } = 1;

    private double timeSinceLastPulse = 0;

    private Label3D distanceLabel = null!;

    public override void _Ready()
    {
        base._Ready();
        distanceLabel = GetNode<Label3D>("Label3D");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        timeSinceLastPulse += delta;

        if (timeSinceLastPulse < PulseInterval)
            return;

        timeSinceLastPulse = 0;

        if (!IsColliding())
        {
            updateLabel(-TargetPosition.Z);
            return;
        }
        var point = GetCollisionPoint();

        float distance = GlobalPosition.DistanceTo(point);

        updateLabel((float)Math.Max(0, distance + Random.Shared.NextSingle() * NoiseVariance));
    }

    private void updateLabel(float distance)
    {
        distanceLabel.Text = distance.ToString("0.##");
    }

}
