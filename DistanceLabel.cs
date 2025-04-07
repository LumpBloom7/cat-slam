using System;
using Godot;

public partial class DistanceLabel : Label3D
{
    public override void _Ready()
    {
        base._Ready();
        FontSize = 16;
    }
    public void OnSignalReceived(float distance)
    {
        Text = distance.ToString("0.##");

    }
}
