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
		bitmap = new int[,] { { 1, 1, 1 }, { 0, 0, 0 } };
		mapHeight = bitmap.GetLength(0);
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
		for (int y = 0; y < mapWidth; y++)  // Columns (Y-axis)
		{
			for (int x = 0; x < mapHeight; x++) // Rows (X-axis)
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
						atlasCoords = new Vector2I(8, 1);
						break;
				}
				SetCell(0, tilePosition, sourceId, atlasCoords);
			}
		}
	}
}
