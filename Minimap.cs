using Godot;

public partial class Minimap : TileMapLayer
{
    private Vector2I offset;
    public void OnMazeGenerated(Tile[][]? tiles)
    {
        if (tiles is null)
            return;
        int mapHeight = tiles.Length;
        int mapWidth = tiles[0].Length;

        offset = new Vector2I(mapHeight / 2, mapWidth / 2);

        for (int y = 0; y < tiles.Length; ++y)
        {
            for (int x = 0; x < tiles[y].Length; ++x)
            {
                SetCell(new Vector2I(x, y), 0, tiles[y][x] is Floor or Spawn ? new Vector2I(0, 0) : new Vector2I(1, 0));
            }
        }
    }


    private Vector2I lastCell = new Vector2I(-1, 0);
    private Vector2I lastCellUv = new Vector2I(-1, 0);

    public void OnPlayerMove(Vector3 position)
    {
        SetCell(lastCell, 0, lastCellUv);

        var pos2d = new Vector2I((int)(position.X - 0.5f), (int)(position.Z + -0.5f)) + offset;

        lastCell = pos2d;
        lastCellUv = GetCellAtlasCoords(pos2d);

        SetCell(pos2d, 0, new Vector2I(2, 0));
    }
}
