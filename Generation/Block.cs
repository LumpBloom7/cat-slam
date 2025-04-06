public interface Tile { }

public struct Floor : Tile
{
    public float Gradient { get; }
    public int Type { get; }

    public Floor(float gradient = 0, int type = 1)
    {
        Gradient = gradient;
        Type = type;
    }
}

public struct Wall : Tile
{
    public int Type { get; }

    public Wall(int type = 1)
    {
        Type = type;
    }
}

public class Block
{
    public required Tile[][] Tileset { get; init; }
    public HashSet<(string, int)> LeftAllow = [];
    public HashSet<(string, int)> RightAllow = [];
    public HashSet<(string, int)> TopAllow = [];
    public HashSet<(string, int)> BotAllow = [];

    public (string, int) Name;

}

