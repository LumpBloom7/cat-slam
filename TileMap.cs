using Godot;
using System;
using System.Collections.Generic;


public partial class TileMap : Godot.TileMap
{
    private int mapWidth;

    private int mapHeight;
    private int[,] bitmap;



    public TileMap() //Tiles is out matrix with tiles (has rows and columns, row is an array)
    {
        bitmap = new int[,] { { 1, 1, 1 }, { 1, 1, 1 } };
        mapHeight = bitmap.Length;
        mapWidth = bitmap.GetLength(1);
    }
    public override void _Ready()
    {
        GenerateMap();
    }

    private void GenerateMap()
    {
        //TileSet tileSet = TileSet;
        GD.Print(mapWidth, mapHeight);
        for (int x = 0; x < mapWidth - 1; x++)
        {
            for (int y = 0; y < mapHeight - 1; y++)
            {
                GD.Print(x, y);
                Vector2I tilePosition = new Vector2I(x, y);
                int sourceId = 0;
                Vector2I atlasCoords = new Vector2I(0, 0);  // Default value
                switch (bitmap[x, y])
                {
                    case 1:
                        sourceId = 0;
                        atlasCoords = new Vector2I(0, 0);
                        break;

                    case 0:
                        sourceId = 0;
                        atlasCoords = new Vector2I(1, 0);
                        break;
                }
                SetCell(0, tilePosition, sourceId, atlasCoords);
            }
        }
    }
}
