using System;
using Godot;

public partial class SensorArray : Node3D
{
    [Export]
    public int NumberOfSensors { get; set; } = 12;

    [Export]
    public float SensorRange { get; set; } = 5;

    [Export]
    public float Noise { get; set; } = 1;

    [Signal]
    public delegate void OnRayCastedEventHandler(float distance, float angle, bool rayHit);

    public override void _Ready()
    {
        float rotStep = (2 * MathF.PI) / NumberOfSensors;
        for (int i = 0; i < NumberOfSensors; ++i)
        {
            AddChild(new SimulatedLidar()
            {
                Rotation = new Vector3(0, rotStep * i, 0),
                PulseInterval = 0.2,
                NoiseVariance = Noise,
                TargetPosition = new Vector3(0, 0, -SensorRange),
                OnRayCast = EmitSignalOnRayCasted
            });
        }
    }
}
