using System;
using Godot;
public partial class GridMap : Godot.GridMap
{

    private int mapWidth;

    private int mapHeight;
    private Tile[][]? bitmap;



    public GridMap() //Tiles is out matrix with tiles (has rows and columns, row is an array)
    {

    }
    public override void _Ready()
    {
        WaveFunctionCollapse wave = new WaveFunctionCollapse(3, 3);
        bitmap = wave.Generate(); //z,x
        if (bitmap is null)
        {
            Console.WriteLine("Failed to generate a solution.");
            return;
        }
        mapHeight = bitmap.Length;
        mapWidth = bitmap[0].Length;
        GenerateMap();
    }

    private void GenerateMap()
    {
        GD.Print(mapHeight, mapWidth);
        var offset = new Vector3I(mapHeight / 2, 0, mapWidth / 2);
        for (int x = 0; x < mapHeight; x++)
        {
            for (int z = 0; z < mapWidth; z++)
            {
                Vector3I tilePosition = new Vector3I(x, -1, z);
                int sourceId = MeshLibrary.FindItemByName("block-grass");
                switch (bitmap[z][x])
                {
                    case Wall:
                        GD.Print("Wall");
                        sourceId = MeshLibrary.FindItemByName("block-snow-large");
                        tilePosition = new Vector3I(x, 0, z);
                        break;
                    case Floor:
                        GD.Print("Floor");
                        sourceId = MeshLibrary.FindItemByName("block-grass-large");
                        tilePosition = new Vector3I(x, -1, z);
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
