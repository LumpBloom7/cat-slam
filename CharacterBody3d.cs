using System;
using Godot;

public partial class CharacterBody3d : CharacterBody3D
{
    private double timeSinceLastRotation = 0;
    private const float acceleration_factor = 0.1f;
    private double timeUntilStop = 0.05;
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        MoveAndSlide();
        timeSinceLastRotation += delta;

        timeUntilStop -= delta;

        if (timeUntilStop <= 0)
            return;

        Velocity += Vector3.Forward.Rotated(new Vector3(0, 1, 0), Rotation.Y) * acceleration_factor * (float)delta;

        float speed = Math.Min(Velocity.Length(), 1);
        Vector3 normVelocity = Velocity.Normalized();
        Velocity = normVelocity * speed;

        if (timeSinceLastRotation >= 2)
        {
            Rotation = Rotation with { Y = (Random.Shared.NextSingle() * 2 - 1) * MathF.PI };
            timeSinceLastRotation = 0;
        }

    }



}
