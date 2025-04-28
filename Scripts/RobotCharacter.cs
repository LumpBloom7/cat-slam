using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

    [Export]
    public float OmnidirectionalSensorRange { get; set; } = 5f;

    private float leftVel = 0;
    private float rightVel = 0;

    [Signal]
    public delegate void LeftMotorValueChangedEventHandler(float velocity);

    [Signal]
    public delegate void RightMotorValueChangedEventHandler(float velocity);

    [Signal]
    public delegate void PositionChangedEventHandler(Vector3 position);

    [Signal]
    public delegate void GhostPositionChangedEventHandler(Vector3 position);

    private GridMap breadcrumbMap = null!;

    public BeaconDetector BeaconDetector { get; private set; } = null!;

    private KalmanFilter? kalmanFilter { get; set; } = null;

    private Node3D ghostNode = null!;

    public override void _Ready()
    {
        base._Ready();
        AddChild(BeaconDetector = new BeaconDetector(OmnidirectionalSensorRange));
        ghostNode = GetParent().GetNode<Node3D>("GhostRobot");
    }

    public void InitPosition(Vector2 position)
    {
        Position = new Vector3(position.X, 0, position.Y);

        // init initial state of KalmanFilter
        var mu = MathNet.Numerics.LinearAlgebra.Vector<float>.Build.Dense([position.X, -position.Y, 0f.ToMathematicalAngle()]);
        var sigma = MathNet.Numerics.LinearAlgebra.Matrix<float>.Build.DenseDiagonal(3, 0);
        kalmanFilter = new KalmanFilter(mu, sigma);
    }

    public Vector2 simulateMotion(float omega)
    {
        float velocity = (leftVel + rightVel) / 2;
        Vector3 movementVector = (Vector3.Forward * velocity).Rotated(new Vector3(0, 1, 0), omega);

        return new(movementVector.X, movementVector.Z);
    }

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

        Rotation = Rotation with { Y = Rotation.Y + rotAmount * (float)delta };

        float velocity = (leftVel + rightVel) / 2;

        Vector3 movementVector = (Vector3.Forward * velocity).Rotated(new Vector3(0, 1, 0), Rotation.Y);

        Velocity = movementVector;

        var origPos = Position;

        MoveAndSlide();

        if (origPos == Position && rotAmount == 0)
            leftVel = rightVel = 0;

        EmitSignal(SignalName.LeftMotorValueChanged, leftVel);
        EmitSignal(SignalName.RightMotorValueChanged, rightVel);
        EmitSignal(SignalName.PositionChanged, Position);

        // Update Kalman filter
        // Mathematical rotation is counterclockwise
        updateKalmanFilter(rotAmount, velocity, delta, Rotation.Y);

        //GD.Print(kalmanFilter?.Sigma);
    }

    private Vector2? triangulate(List<(System.Numerics.Vector2 Position, float Distance)> trackedBeacons)
    {
        var offset = trackedBeacons[0].Position;
        var p2 = trackedBeacons[1].Position - offset;
        p2.Y *= -1;
        var p3 = trackedBeacons[2].Position - offset;
        p3.Y *= -1;

        float theata = MathF.Acos(p2.X / p2.Length());
        (float sin, float cos) = MathF.SinCos(-theata);

        var np2 = new Vector2(p2.X * cos - p2.Y * sin, p2.X * sin + p2.Y * cos);
        var np3 = new Vector2(p3.X * cos - p3.Y * sin, p3.X * sin + p3.Y * cos);

        float r1 = trackedBeacons[0].Distance;
        float r2 = trackedBeacons[1].Distance;
        float r3 = trackedBeacons[2].Distance;

        float p4x = (r1 * r1 - r2 * r2 + np2.X * np2.X) / (2 * np2.X);
        float p4y = (r1 * r1 - r3 * r3 + np3.X * np3.X + np3.Y * np3.Y) / (2 * np3.Y) - (np3.X / np3.Y) * p4x;

        if (!float.IsFinite(p4y))
        {
            Console.WriteLine($"Triangulation Y is NaN, ignoring attempt");
            return null;
        }

        if (!float.IsFinite(p4x))
        {
            Console.WriteLine($"Triangulation X is NaN, ignoring attempt");
            return null;
        }

        (float cSin, float cCos) = MathF.SinCos(theata);

        Vector2 p4 = new(p4x * cCos - p4y * cSin, p4x * cSin + p4y * cCos);

        return new(p4.X + offset.X, -p4.Y + offset.Y);
    }

    // Iterative, using 'i' as bitmask to choose each combo members
    public static List<List<T>> GetAllCombos<T>(List<T> list)
    {
        int comboCount = (int)Math.Pow(2, list.Count) - 1;
        List<List<T>> result = new List<List<T>>();
        for (int i = 1; i < comboCount + 1; i++)
        {
            // make each combo here
            result.Add(new List<T>());
            for (int j = 0; j < list.Count; j++)
            {
                if ((i >> j) % 2 != 0)
                    result.Last().Add(list[j]);
            }
        }
        return result;
    }

    private void updateKalmanFilter(float omega, float vel, double dt, float theta)
    {
        var u = MathNet.Numerics.LinearAlgebra.Single.Vector.Build.Dense([vel, omega]);

        var combinations = GetAllCombos(BeaconDetector.GetTrackedBeacons().ToList()).Where(c => c.Count == 3);

        var items = combinations.Select(k => triangulate([.. k])).Where(i => i.HasValue).Select(i => i!.Value).ToArray();

        if (items.Length > 0)
        {
            Vector2 avgPos = new();
            foreach (var i in items)
                avgPos += i;

            avgPos /= items.Length;

            var z = MathNet.Numerics.LinearAlgebra.Single.Vector.Build.Dense([avgPos.X, -avgPos.Y, theta.ToMathematicalAngle()]);
            kalmanFilter?.Update(u, z, dt);
        }
        else
        {
            kalmanFilter?.Update(u, null, dt);
        }

        Debug.Assert(kalmanFilter is not null);
        ghostNode.Position = Position with { X = kalmanFilter.Mu[0], Z = -kalmanFilter.Mu[1] };
        ghostNode.Rotation = new Vector3(0, kalmanFilter.Mu[2].FromMathematicalAngle(), 0);

        EmitSignal(SignalName.GhostPositionChanged, ghostNode.Position);
    }
}
