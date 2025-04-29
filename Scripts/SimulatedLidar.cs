using System;
using Godot;

public partial class SimulatedLidar : RayCast3D
{

    [Export]
    public double PulseInterval { get; set; } = 0.2;

    [Export]
    public double NoiseVariance { get; set; } = 1;

    private double timeSinceLastPulse = 0;

    private Label3D distanceLabel = null!;

    private Node3D ghostNode = null!;

    [Signal]
    public delegate void RayCastedEventHandler(Vector3 origin, Vector3 target, bool isColliding);

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

        ghostNode = GetNode("/root").GetChild(0).GetNode<Node3D>("GhostRobot");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        timeSinceLastPulse += delta;

        if (timeSinceLastPulse < PulseInterval)
            return;

        timeSinceLastPulse = 0;


        bool isColliding = IsColliding();

        if (!isColliding)
        {
            updateLabel(-TargetPosition.Z);
            EmitSignalRayCasted(ghostNode.GlobalPosition, GlobalPosition + TargetPosition.Rotated(new(0, 1, 0), GlobalRotation.Y), isColliding);
        }
        else
        {
            var point = GetCollisionPoint();

            float distance = GlobalPosition.DistanceTo(point);
            updateLabel((float)Math.Max(0, distance + Random.Shared.NextSingle() * NoiseVariance));
            EmitSignalRayCasted(ghostNode.GlobalPosition, point, isColliding);
        }
    }

    private void updateLabel(float distance)
    {
        distanceLabel.Text = distance.ToString("0.##");
    }

}
