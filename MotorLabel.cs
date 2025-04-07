using System;
using Godot;

public partial class MotorLabel : Label3D
{
    public void OnSignalReceived(float velocity)
    {
        Text = velocity.ToString("0.##");
    }
}
