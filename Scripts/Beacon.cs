using Godot;

public partial class Beacon : Area3D
{
    public Beacon()
    {
        Monitoring = false;
        AddChild(new CollisionShape3D() { Shape = new SphereShape3D { Radius = 0.001f } });
        AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh
            {
                Size = new Vector3(0.2f, 0.2f, 0.2f),
                Material = new OrmMaterial3D()
                {
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    AlbedoColor = Color.Color8(0, 255, 155)
                }
            }
        });
    }
}
