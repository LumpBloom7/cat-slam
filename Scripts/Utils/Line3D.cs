using System;
using Godot;

public partial class Line3D : Node3D
{
    private LineMesh lineMesh;

    public Line3D()
    {
        AddChild(lineMesh = new LineMesh());
    }

    private Vector3 target;

    public Vector3 Target
    {
        get => target;
        set
        {
            target = value;
            updateLine();
        }
    }

    private void updateLine()
    {
        float distance = target.DistanceTo(GlobalPosition);

        lineMesh.Height = distance;
        LookAt(target);
    }

    public partial class LineMesh : MeshInstance3D
    {
        private CylinderMesh mesh;

        public LineMesh()
        {
            CastShadow = ShadowCastingSetting.Off;
            Mesh = mesh = new()
            {
                TopRadius = 0.01f,
                BottomRadius = 0.01f,
                Material = new OrmMaterial3D()
                {
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    AlbedoColor = Color.Color8(255, 0, 100)
                }
            };
            Rotation = new Vector3(Mathf.DegToRad(90), 0, 0);
        }

        public float Height
        {
            get => mesh.Height;
            set
            {
                if (value == mesh.Height)
                    return;

                mesh.Height = value;
                Position = new Vector3(0, 0, -value * 0.5f);
            }
        }
    }
}
