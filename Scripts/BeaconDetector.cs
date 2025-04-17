using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class BeaconDetector : Area3D
{
    private Dictionary<Beacon, Line3D> trackedBeacons = [];

    private Beacon[] allBeacons = null!;

    public BeaconDetector(float radius = 5)
    {
        Name = "Omnidirectional sensor";
        Monitoring = true;
        AddChild(new CollisionShape3D() { Shape = new SphereShape3D { Radius = radius } });
    }

    public IEnumerable<(System.Numerics.Vector2 Position, float Distance)> GetTrackedBeacons() => trackedBeacons.Values.Select(b =>
        (
            new System.Numerics.Vector2(b.Target.X, b.Target.Z),
            (b.Target with { Y = 0 }).DistanceTo(GlobalPosition with { Y = 0 })
        )
    );

    public override void _Ready()
    {
        base._Ready();
        AreaEntered += OnBeaconEntered;
        AreaExited += OnBeaconLeave;

        var rootNode = GetNode("/root");
        allBeacons = [.. rootNode.GetDescendants<Beacon>(true)];
    }

    public IEnumerable<(System.Numerics.Vector2 Position, float Distance)> GetBeaconsInfoFrom(float x, float y)
    {
        // Do we care about line of sight?
        foreach (var b in allBeacons)
        {
            var beaconPos = b.GlobalPosition;
            yield return (new System.Numerics.Vector2(beaconPos.X, beaconPos.Z), (beaconPos with { Y = 0 }).DistanceTo(new Vector3(x, 0, y)));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        var currentPos = GlobalPosition;

        foreach (var beacon in trackedBeacons)
        {
            // We do a raycast to determine whether the beacon is actually in line-of-sight.
            var beaconPos = beacon.Key.GlobalPosition;
            var spaceState = GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(currentPos + new Vector3(0, 0.043f, 0), beaconPos);
            query.CollideWithAreas = true;

            var result = spaceState.IntersectRay(query);

            if (result.TryGetValue("collider", out var res) && res.As<Node?>() is not Beacon or null)
            {
                beacon.Value.Visible = false;
                continue;
            }

            beacon.Value.Visible = true;
            beacon.Value.Target = beaconPos;
        }
    }

    public void OnBeaconEntered(Area3D area3D)
    {
        if (area3D is not Beacon b)
            return;

        GD.Print("Beacon entered");
        AddChild(trackedBeacons[b] = new Line3D());
    }

    public void OnBeaconLeave(Area3D area3D)
    {
        if (area3D is not Beacon b)
            return;

        GD.Print("Beacon left");

        RemoveChild(trackedBeacons[b]);
        trackedBeacons.Remove(b);
    }
}
