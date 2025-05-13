using System;
using Godot;

public partial class RobotPath : MultiMeshInstance3D
{
    private Vector3? lastPosition = null!;

    [Export]

    public int Length { get; set; } = 20000;

    [Export]
    public Color LineColour { get; set; } = Color.Color8(255, 0, 0, 255);

    [Export]
    public bool Dotted { get; set; } = false;

    public override void _Ready()
    {
        base._Ready();
        // These lines are so small, casting a shadow is unlikely to change the environment
        // Let's disable it to reduce draw calls
        CastShadow = ShadowCastingSetting.Off;
        Multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = new BoxMesh()
            {
                Material = new StandardMaterial3D()
                {
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    AlbedoColor = LineColour
                },
                Size = new Vector3(0.02f, 1f, 0.02f)
            },
            InstanceCount = Length,
            // At first none of the instances are visible, let's not force the engine to draw them
            VisibleInstanceCount = 0
        };
    }

    private CylinderMesh cylinderMesh = null!;

    private int count = 0;
    private bool isDrawing = true;

    private int currentInstance = 0;

    public void OnPositionChanged(Vector3 position)
    {
        if (Length == 0)
            return;

        if (lastPosition is null)
        {
            lastPosition = position;
            return;
        }

        if (position == lastPosition)
            return;

        if (lastPosition.Value.IsEqualApprox(position))
            return;

        if (Dotted)
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

        if (inBetweenPos.IsEqualApprox(position))
            return;

        Transform3D transform = Transform3D.Identity
                            .TranslatedLocal(inBetweenPos)
                            .LookingAt(position)
                            .RotatedLocal(new Vector3(1, 0, 0), Mathf.DegToRad(90))
                            .ScaledLocal(new Vector3(1, length, 1));

        Multimesh.SetInstanceTransform(currentInstance, transform);

        currentInstance++;

        Multimesh.VisibleInstanceCount = Math.Max(Multimesh.VisibleInstanceCount, currentInstance);

        currentInstance %= Length;
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
