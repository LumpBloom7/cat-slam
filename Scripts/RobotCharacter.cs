using System;
using System.Diagnostics;
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

    private GridMap breadcrumbMap = null!;

    public BeaconDetector BeaconDetector { get; private set; } = null!;

    private KalmanFilter? kalmanFilter { get; set; } = null;

    private Node3D ghostNode = null!;

    public override void _Ready()
    {
        base._Ready();
        breadcrumbMap = GetParent().GetNode<GridMap>("BreadcrumbMap");
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


        // Update breadcrumbs
        // The breadcrumb grid is already offseted, so we don't haee to do it locally.
        int x = (int)MathF.Round(Position.X / 0.3f);
        int z = (int)MathF.Round(Position.Z / 0.3f);
        breadcrumbMap.SetCellItem(new Vector3I(x, 0, z), 0);

        EmitSignal(SignalName.PositionChanged, Position);

        // Update Kalman filter
        // Mathematical rotation is counterclockwise
        updateKalmanFilter(rotAmount, velocity, delta, Rotation.Y);

        //GD.Print(kalmanFilter?.Sigma);
    }

    private void updateKalmanFilter(float omega, float vel, double dt, float theta)
    {
        var u = MathNet.Numerics.LinearAlgebra.Single.Vector.Build.Dense([vel, omega]);

        Vector2 sum = Vector2.Zero;
        int count = 0;
        foreach (var sensor in BeaconDetector.GetTrackedBeacons())
        {
            //GD.Print($"SensorX: {sensor.Position.X}");
            //GD.Print($"SensorY: {sensor.Position.Y}");

            // We flip the sign of Y
            float bearing = MathF.Atan2(-(sensor.Position.Y - Position.Z), sensor.Position.X - Position.X);

            (float sin, float cos) = MathF.SinCos(bearing);

            Vector2 estimatedPosition = new Vector2(-sensor.Distance * cos, sensor.Distance * sin) + new Vector2(sensor.Position.X, sensor.Position.Y);
            sum += estimatedPosition;
            ++count;
        }

        Vector2 avgPos = sum / Math.Max(1, count);

        //GD.Print($"Average position: {avgPos}");
        if (count == 0)
        {
            kalmanFilter?.Update(u, null, dt);
        }
        else
        {
            GD.Print("printing");
            var z = MathNet.Numerics.LinearAlgebra.Single.Vector.Build.Dense([avgPos.X, -avgPos.Y, theta.ToMathematicalAngle()]);
            kalmanFilter?.Update(u, z, dt);
        }
        Debug.Assert(kalmanFilter is not null);
        ghostNode.Position = Position with { X = kalmanFilter.Mu[0], Z = -kalmanFilter.Mu[1] };
        ghostNode.Rotation = new Vector3(0, kalmanFilter.Mu[2].FromMathematicalAngle(), 0);

        // Update breadcrumbs
        // The breadcrumb grid is already offseted, so we don't haee to do it locally.
        int xPos = (int)MathF.Round(ghostNode.Position.X / 0.3f);
        int zPos = (int)MathF.Round(ghostNode.Position.Z / 0.3f);
        breadcrumbMap.SetCellItem(new Vector3I(xPos, 1, zPos), 1);
    }
}
