using System;
using Godot;

public partial class SimulatedLidar : RayCast3D
{

    [Export]
    public double PulseInterval { get; set; } = 0.2;

    [Export]
    public float NoiseVariance { get; set; } = 1;

    private double timeSinceLastPulse = 0;

    private Label3D distanceLabel = null!;

    public float Distance { get; private set; }

    public Action<float, float, bool> OnRayCast;

    public override void _Ready()
    {
        base._Ready();
        AddChild(distanceLabel = new Label3D()
        {
            Position = new Vector3(0, 0.055f, 0.3f),
            PixelSize = 0.001f,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
            Text = "0.00",
            FontSize = 64
        });
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
            OnRayCast?.Invoke(-TargetPosition.Z, Rotation.Y, false);
        }
        else
        {
            var point = GetCollisionPoint();

            Distance = GlobalPosition.DistanceTo(point) + Random.Shared.NextSingle() * NoiseVariance;
            updateLabel(Distance);
            OnRayCast?.Invoke(Distance, Rotation.Y, true);
        }
    }

    private void updateLabel(float distance)
    {
        distanceLabel.Text = distance.ToString("0.##");
    }

}
