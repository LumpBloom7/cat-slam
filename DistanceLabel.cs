using System;
using Godot;

public partial class DistanceLabel : Label3D
{
    public void OnSignalReceived(float distance)
    {
        Text = distance.ToString("0.##");
    }
}
