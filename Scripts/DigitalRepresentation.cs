using Godot;

public partial class DigitalRepresentation : Node3D
{
    private GhostRobot ghostRobot = null!;
    private OccupancyMap occupancyMap = null!;
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
        AddChild(ghostRobot = new GhostRobot(Colour));
        AddChild(occupancyMap = new OccupancyMap()
        {
            MapSize = MapSize,
            CellSize = CellSize
        });
        AddChild(path = new RobotPath()
        {
            LineColour = Colour,
            Dotted = true,
        });
    }

    public void UpdateGhostPosition(Vector3 vector3, Vector3 rotation)
    {
        ghostRobot.GlobalPosition = vector3;
        ghostRobot.GlobalRotation = rotation;

        path.OnPositionChanged(vector3);
    }

    public void UpdateOccupancyMap(float distance, float angle, bool isHit)
    {
        var origin = ghostRobot.GlobalPosition;
        var directionVector = (Vector3.Forward * distance).Rotated(new Vector3(0, 1, 0), angle + ghostRobot.GlobalRotation.Y);
        occupancyMap.ProcessRayCast(origin, origin + directionVector, isHit);
    }

    private partial class GhostRobot : Node3D
    {
        public GhostRobot(Color colour)
        {
            AddChild(new MeshInstance3D
            {
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
