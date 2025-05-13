using System;
using Godot;
public partial class GridMapGenerator : GridMap
{

    public int Width { get; private set; }
    public int Height { get; private set; }

    private Node BeaconStorageNode = new Node()
    {
        Name = "Beacons"
    };

    public override void _Ready()
    {
        WaveFunctionCollapse wave = new WaveFunctionCollapse(3, 3);
        var bitmap = wave.Generate(); //z,x
        GenerateMap(bitmap);

        GetNode("/root").GetChild(0).CallDeferred(Node.MethodName.AddChild, BeaconStorageNode);
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsActionPressed("ToggleGeometry", false))
            Visible = !Visible;
    }


    private void GenerateMap(Tile[][]? bitmap)
    {
        if (bitmap is null)
        {
            Console.WriteLine("Failed to generate a map.");
            return;
        }

        Height = bitmap.Length;
        Width = bitmap[0].Length;

        GD.Print(Height, Width);
        var offset = new Vector3I(Height / 2, 0, Width / 2);
        for (int x = 0; x < Height; x++)
        {
            for (int z = 0; z < Width; z++)
            {
                Vector3I tilePosition = new Vector3I(x, -1, z);
                int sourceId = MeshLibrary.FindItemByName("block-grass");
                switch (bitmap[z][x])
                {
                    case Wall:
                        GD.Print("Wall");
                        sourceId = MeshLibrary.FindItemByName("block-snow-large");
                        tilePosition = new Vector3I(x, 0, z);

                        if (bitmap[z][x].Type.HasFlag(Category.Beacon))
                            BeaconStorageNode.AddChild(new Beacon { Position = tilePosition + new Vector3(0.5f, 1.5f, 0.5f) - offset });
                        break;
                    case Floor:
                    case Spawn:  // All spawns are also floors
                        GD.Print("Floor");
                        sourceId = MeshLibrary.FindItemByName("block-grass-large");
                        tilePosition = new Vector3I(x, -1, z);

                        if (bitmap[z][x].Type.HasFlag(Category.Beacon))
                            BeaconStorageNode.AddChild(new Beacon { Position = tilePosition + new Vector3(0.5f, 1.1f, 0.5f) - offset });

                        if (bitmap[z][x] is Spawn)
                        {
                            var player = GetParent().GetNode<RobotCharacter>("Robot");
                            player.InitPosition(new Vector2(tilePosition.X - offset.X + 0.5f, tilePosition.Z - offset.Z + 0.5f));
                        }
                        break;
                }
                GD.Print(tilePosition);
                GD.Print(sourceId);
                GD.Print("");
                SetCellItem(tilePosition - offset, sourceId);
            }
        }
    }
}
