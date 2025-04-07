using System;
using Godot;

public partial class RobotCharacter : CharacterBody3D
{

    [Export]
    public float AccelerationPerSecond { get; set; } = 1;
    [Export]
    public float DeccelerationPerSecond { get; set; } = 0.5f;

    [Export]
    public float MaxSpeed { get; set; } = 1;

    [Export]
    public float Radius { get; set; } = 0.15f;


    private float leftVel = 0;
    private float rightVel = 0;

    public override void _PhysicsProcess(double delta)
    {
        // Compute acceleration amounts
        float leftAcc = 0;
        float rightAcc = 0;
        if (Input.IsActionPressed("LeftMotorForwards"))
            leftAcc += AccelerationPerSecond * (float)delta;
        if (Input.IsActionPressed("LeftMotorBackwards"))
            leftAcc -= AccelerationPerSecond * (float)delta;

        if (Input.IsActionPressed("RightMotorForwards"))
            rightAcc += AccelerationPerSecond * (float)delta;
        if (Input.IsActionPressed("RightMotorBackwards"))
            rightAcc -= AccelerationPerSecond * (float)delta;

        // Apply speed changes
        if (leftAcc == 0)
            leftVel = Math.Max(Math.Abs(leftVel) - DeccelerationPerSecond * (float)delta, 0) * MathF.Sign(leftVel);
        else
            leftVel = Math.Clamp(leftAcc + leftVel, -MaxSpeed, MaxSpeed);

        if (rightAcc == 0)
            rightVel = Math.Max(Math.Abs(rightVel) - DeccelerationPerSecond * (float)delta, 0) * MathF.Sign(rightVel);
        else
            rightVel = Math.Clamp(rightAcc + rightVel, -MaxSpeed, MaxSpeed);

        float rotAmount = (rightVel - leftVel) / (Radius * 2);


        Console.WriteLine(AccelerationPerSecond);
        Console.WriteLine(leftAcc);
        Console.WriteLine(rightAcc);

        Rotation = Rotation with { Y = Rotation.Y + rotAmount * (float)delta };

        float velocity = (leftVel + rightVel) / 2;

        Vector3 movementVector = (Vector3.Forward * velocity).Rotated(new Vector3(0, 1, 0), Rotation.Y);

        Velocity = movementVector;

        MoveAndSlide();
    }
}
