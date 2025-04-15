using System;
using System.Collections.Generic;

public interface Tile { }

public enum Category
{
    normal,
    beacon
}

public struct Floor : Tile
{
    public float Gradient { get; }
    public Category Type { get; }

    public Floor(float gradient = 0, Category type = Category.normal)
    {
        Gradient = gradient;
        Type = type;
    }
}
public struct Spawn : Tile
{
    public Category Type { get; }
    public Spawn(Category type = Category.normal){
        Type = type;
    }
}
public struct Wall : Tile
{
    public Category Type { get; }

    public Wall(Category type = Category.normal)
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

