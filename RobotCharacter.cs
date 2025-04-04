using System;
using Godot;

public partial class RobotCharacter : CharacterBody3D
{
    private double timeSinceLastRotation = 0;
    private const float accelerationPerSecond = 1f;
    private const float decelerationPerSecond = 0.5f;
    private double timeUntilStop = 5;
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        MoveAndSlide();
        timeSinceLastRotation += delta;

        timeUntilStop -= delta;

        if (timeUntilStop >= 0)
            Velocity += Vector3.Forward.Rotated(new(0, 1, 0), Rotation.Y) * accelerationPerSecond * (float)delta;

        var noGravityVelocity = Velocity with { Y = 0 };
        float newSpeed = Math.Min(noGravityVelocity.Length() - decelerationPerSecond * (float)delta, 1);

        if (newSpeed < 0.0001f)
            newSpeed = 0;

        var norm = noGravityVelocity.Normalized();

        Velocity = norm * newSpeed + Vector3.Down;

        if (timeSinceLastRotation >= 2)
        {
            Rotation = Rotation with { Y = (Random.Shared.NextSingle() * 2 - 1) * MathF.PI };
            timeSinceLastRotation = 0;
        }
    }
}
