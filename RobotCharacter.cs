using System;
using Godot;

public partial class RobotCharacter : CharacterBody3D
{

    [Export]
    public float AccelerationPerSecond { get; set; } = 1;
    [Export]
    public float DeccelerationPerSecond { get; set; } = 0.5f;
    [Export]
    public double TimeUntilStop { get; set; } = 5;

    [Export]
    public double RotationInterval { get; set; } = 2;

    [Export]
    public float RotationSpeedRadians { get; set; } = 1;

    [Export]
    public float MaxSpeed { get; set; } = 1;

    private double timeSinceLastRotation = 0;
    private float prevRotation = 0;
    private float newRotation = 0;
    private double rotationTimeAccumulator = 0;

    public override void _Ready()
    {
        base._Ready();
        prevRotation = newRotation = Rotation.Y;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        MoveAndSlide();
        timeSinceLastRotation += delta;
        rotationTimeAccumulator += delta;

        TimeUntilStop -= delta;

        if (TimeUntilStop >= 0)
            Velocity += Vector3.Forward.Rotated(new(0, 1, 0), Rotation.Y) * AccelerationPerSecond * (float)delta;

        var noGravityVelocity = Velocity with { Y = 0 };
        float newSpeed = Math.Min(noGravityVelocity.Length() - DeccelerationPerSecond * (float)delta, MaxSpeed);

        if (newSpeed < 0.0001f)
            newSpeed = 0;

        var norm = noGravityVelocity.Normalized();

        Velocity = norm * newSpeed + Vector3.Down;
        float rotProg = MathF.Min((float)rotationTimeAccumulator / (Math.Abs(newRotation - prevRotation) / RotationSpeedRadians), 1);

        Rotation = Rotation with { Y = prevRotation + (newRotation - prevRotation) * rotProg };

        if (timeSinceLastRotation >= RotationInterval)
        {
            prevRotation = Rotation.Y;
            newRotation = (Random.Shared.NextSingle() * 2 - 1) * MathF.PI;
            rotationTimeAccumulator = 0;
            timeSinceLastRotation = 0;
        }
    }
}
