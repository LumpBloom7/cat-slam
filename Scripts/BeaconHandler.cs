using System;
using System.Collections.Generic;
using Godot;

public partial class BeaconDetector : Area3D
{
    private Dictionary<Beacon, Line3D> trackedBeacons = [];

    public BeaconDetector(float radius = 5)
    {
        Name = "Omnidirectional sensor";
        Monitoring = true;
        AddChild(new CollisionShape3D() { Shape = new SphereShape3D { Radius = radius } });
    }

    public override void _Ready()
    {
        base._Ready();
        AreaEntered += OnBeaconEntered;
        AreaExited += OnBeaconLeave;
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
            beacon.Value.Target = beacon.Key.GlobalPosition;
            // TODO: Report position to something
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
