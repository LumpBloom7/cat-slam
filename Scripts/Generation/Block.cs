using System;
using System.Collections.Generic;

public interface Tile
{
    Category Type { get; }
}

[Flags]
public enum Category
{
    Normal = 0,
    Beacon = 1,
}

public readonly record struct Floor() : Tile
{
    public float Gradient { get; init; } = 0;
    public Category Type { get; init; } = Category.Normal;
}
public readonly record struct Spawn() : Tile
{
    public Category Type { get; init; } = Category.Normal;
}
public readonly record struct Wall() : Tile
{
    public Category Type { get; init; } = Category.Normal;
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

