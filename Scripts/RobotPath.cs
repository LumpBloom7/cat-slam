using System;
using Godot;

public partial class RobotPath : Node3D
{
    private Vector3? lastPosition = null!;

    [Export]
    public Color LineColour { get; set; } = Color.Color8(255, 0, 0, 100);

    [Export]
    public bool dotted { get; set; } = false;

    public override void _Ready()
    {
        base._Ready();
        cylinderMesh = new()
        {
            TopRadius = 0.01f,
            BottomRadius = 0.01f,
            Material = new OrmMaterial3D()
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                AlbedoColor = LineColour
            },
            Height = 1
        };
    }

    private CylinderMesh cylinderMesh = null!;

    private int count = 0;
    private bool isDrawing = true;

    public void OnPositionChanged(Vector3 position)
    {
        if (lastPosition is null)
        {
            lastPosition = position;
            return;
        }

        if (lastPosition == position)
            return;

        if (dotted)
        {
            ++count;
            if (count >= 15)
            {
                isDrawing = !isDrawing;
                count = 0;
            }

            if (!isDrawing)
            {
                lastPosition = position;
                return;
            }
        }

        float length = (position with { Y = 0 }).DistanceTo(lastPosition!.Value with { Y = 0 });

        Vector3 inBetweenPos = position.Lerp(lastPosition.Value, 0.5f);
        var mesh = new LineMesh
        {
            Mesh = cylinderMesh,
        };
        mesh.Scale = mesh.Scale with { Y = length };
        AddChild(mesh);
        mesh.LookAtFromPosition(inBetweenPos, position);

        mesh.Rotation = mesh.Rotation with
        {
            X = Mathf.DegToRad(90)
        };

        lastPosition = position;
    }

    public partial class LineMesh : MeshInstance3D
    {
        public LineMesh()
        {
            CastShadow = ShadowCastingSetting.Off;
            Rotation = new Vector3(Mathf.DegToRad(90), 0, 0);
        }
    }
}
