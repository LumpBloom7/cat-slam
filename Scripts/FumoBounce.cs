using System;
using Godot;

public partial class FumoBounce : Node3D
{
    [Export]
    public float BounceAmount { get; set; } = 0.002f;

    private float originalYScale;

    public override void _Ready()
    {
        base._Ready();
        originalYScale = Scale.Y;
    }

    private double t = 0;

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        t += delta;

        Scale = Scale with { Y = originalYScale + BounceAmount * 0.25f * MathF.Sin((float)t * 25) };
    }
}
