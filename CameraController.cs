using System;
using Godot;

public partial class CameraController : Node
{
    private Camera3D topDownCamera = null!;
    private Camera3D behindCamera = null!;

    private bool isTopDown = true;

    public override void _Ready()
    {
        base._Ready();

        topDownCamera = GetNode<Camera3D>("TopDownCamera");
        behindCamera = GetNode<Camera3D>("BehindCamera");

        topDownCamera.MakeCurrent();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("SwitchCamera"))
        {
            if (isTopDown)
                behindCamera.MakeCurrent();
            else
                topDownCamera.MakeCurrent();

            isTopDown = !isTopDown;
            GetViewport().SetInputAsHandled();
        }

        base._Input(@event);
    }
}
