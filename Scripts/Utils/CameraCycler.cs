using Godot;

public partial class CameraCycler : Node3D
{
    [Export]
    public string InputMapAction { get; set; } = "";

    private int currentIndex = 0;

    private Camera3D[] cameras = null!;

    public override void _Ready()
    {
        base._Ready();

        var root = GetNode("/root");

        cameras = [.. root.GetDescendants<Camera3D>(true)];
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsActionPressed(InputMapAction, false))
        {
            currentIndex = (currentIndex + 1) % cameras.Length;
            cameras[currentIndex].MakeCurrent();
        }
    }
}
