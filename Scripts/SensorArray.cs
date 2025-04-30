using System;
using Godot;

public partial class SensorArray : Node3D
{
    [Export]
    public int NumberOfSensors { get; set; } = 12;

    [Export]
    public float SensorRange { get; set; } = 5;

    public override void _Ready()
    {
        float rotStep = (2 * MathF.PI) / NumberOfSensors;
        for (int i = 0; i < NumberOfSensors; ++i)
        {
            AddChild(new SimulatedLidar()
            {
                Rotation = new Vector3(0, rotStep * i, 0),
                PulseInterval = 0.2,
                NoiseVariance = 1.0,
                TargetPosition = new Vector3(0, 0, -SensorRange)
            });
        }
    }
}
