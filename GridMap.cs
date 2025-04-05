using Godot;
using System;

public partial class GridMap : Godot.GridMap
{

    private int mapWidth;

    private int mapHeight;
    private int[,] bitmap;



    public GridMap() //Tiles is out matrix with tiles (has rows and columns, row is an array)
    {
        bitmap = new int[,] { { 1, 1, 1 }, { 0, 0, 0 } };
        mapHeight = bitmap.GetLength(0);
        mapWidth = bitmap.GetLength(1);
    }
    public override void _Ready()
    {
        //GenerateMap();
        int sourceId = MeshLibrary.FindItemByName("block-grass");
        GD.Print("Mesh ID:", sourceId);

        for (int x = -10; x <= 10; x++)
        {
            for (int z = -10; z <= 10; z++)
            {
                Vector3I pos = new Vector3I(x, -3, z);
                SetCellItem(pos, sourceId);
            }

        }
    }

    private void GenerateMap()
    {
        //TileSet tileSet = TileSet;
        GD.Print(mapWidth, mapHeight);
        for (int x = -10; x < 10; x++)  // Columns (Y-axis)
        {
            for (int z = -10; z < 10; z++) // Rows (X-axis)
            {
                //GD.Print(x, z);
                Vector3I tilePosition = new Vector3I(-1, -2, -1);
                int sourceId = MeshLibrary.FindItemByName("block-grass");
                GD.Print(sourceId);
                // switch (bitmap[x, z])
                // {
                // 	case 1:
                // 		//sourceId = 2;
                // 		//atlasCoords = new Vector2I(0, 0);
                // 		break;

                // 	case 0:
                // 		//sourceId = 2;
                // 		//atlasCoords = new Vector2I(8, 1);
                // 		break;
                // }
                SetCellItem(tilePosition, sourceId);
            }
        }
    }
}
