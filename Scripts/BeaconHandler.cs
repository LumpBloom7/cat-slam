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

        foreach (var beacon in trackedBeacons)
        {
            beacon.Value.Target = beacon.Key.GlobalPosition;
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
