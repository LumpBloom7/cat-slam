using Godot;

public partial class VisibilityCycler : Node3D
{
    [Export]
    public string InputMapAction { get; set; } = "";

    private int currentIndex = -1;

    public override void _Ready()
    {
        base._Ready();

        foreach (Node3D child in GetChildren())
            child.Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsActionPressed(InputMapAction, false))
        {
            var children = GetChildren();
            if (currentIndex != -1 && currentIndex < children.Count)
                ((Node3D)GetChild(currentIndex)).Visible = false;

            if (++currentIndex >= children.Count)
                currentIndex = -1;

            if (currentIndex != -1)
                ((Node3D)GetChild(currentIndex)).Visible = true;
        }
    }
}
