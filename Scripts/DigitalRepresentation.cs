using Godot;

public partial class DigitalRepresentation : Node3D
{
    public GhostRobot Ghost { get; private set; } = null!;
    public OccupancyMap OccupancyMap { get; private set; } = null!;

    private RobotPath path = null!;

    [Export]
    public Vector2 MapSize { get; set; } = new Vector2(30, 30);

    [Export]
    public Vector2 CellSize { get; set; } = new(0.5f, 0.5f);

    [Export]
    public Color Colour { get; set; } = Color.Color8(0, 255, 0);

    public override void _Ready()
    {
        base._Ready();
        AddChild(Ghost = new GhostRobot(Colour));
        AddChild(OccupancyMap = new OccupancyMap()
        {
            MapSize = MapSize,
            CellSize = CellSize,
            CellColour = Colour
        });
        AddChild(path = new RobotPath()
        {
            LineColour = Colour,
            Dotted = true,
        });
    }

    public void UpdateGhostPosition(Vector3 vector3, Vector3 rotation)
    {
        Ghost.GlobalPosition = vector3;
        Ghost.GlobalRotation = rotation;

        path.OnPositionChanged(vector3);
    }

    public void UpdateOccupancyMap(float distance, float angle, bool isHit)
    {
        var origin = Ghost.GlobalPosition;
        var directionVector = (Vector3.Forward * distance).Rotated(new Vector3(0, 1, 0), angle + Ghost.GlobalRotation.Y);
        OccupancyMap.ProcessRayCast(origin, origin + directionVector, isHit);
    }

    public partial class GhostRobot : Node3D
    {
        public GhostRobot(Color colour)
        {
            AddChild(new MeshInstance3D
            {
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Mesh = new CylinderMesh
                {
                    TopRadius = 0.15f,
                    BottomRadius = 0.15f,
                    Height = 0.07f,
                    Material = new StandardMaterial3D
                    {
                        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                        AlbedoColor = colour with { A = 0.5f },
                        NoDepthTest = true
                    }
                }
            });

            AddChild(new MeshInstance3D
            {
                Position = new Vector3(0, 0.05f, -0.15f),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Mesh = new BoxMesh
                {
                    Size = new(0.05f, 0.05f, 0.05f),
                    Material = new StandardMaterial3D
                    {
                        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                        AlbedoColor = colour with { A = 0.8f },
                        NoDepthTest = true
                    }
                }
            });
        }
    }
}
