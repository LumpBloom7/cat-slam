using System.Collections.Generic;

public static class BlockDefs
{
    private static Wall w = new();
    private static Floor f = new();
    public static readonly Block straight = new()
    {
        Name = ("straight", 1),
        TopAllow = [("cross", 1), ("straight", 1)],
        BotAllow = [("cross", 1), ("straight", 1)],
        RightAllow = [("straight", 1)],
        LeftAllow = [("straight", 1)],
        Tileset = [ [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                  ]
    };
    public static readonly Block cross = new()
    {
        Name = ("cross", 1),
        TopAllow = [("cross", 1), ("straight", 1)],
        BotAllow = [("cross", 1), ("straight", 1)],
        RightAllow = [("cross", 1), ("line", 1)],
        LeftAllow = [("cross", 1), ("line", 1)],
        Tileset = [ [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [f,f,f,f,f,f,f,f,f],
                    [f,f,f,f,f,f,f,f,f],
                    [f,f,f,f,f,f,f,f,f],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                  ]
    };
    public static readonly Block line = new()
    {
        Name = ("line", 1),
        TopAllow = [("line", 1)],
        BotAllow = [("line", 1)],
        RightAllow = [("cross", 1), ("line", 1)],
        LeftAllow = [("cross", 1), ("line", 1)],
        Tileset = [ [w,w,w,w,w,w,w,w,w],
                    [w,w,w,w,w,w,w,w,w],
                    [w,w,w,w,w,w,w,w,w],
                    [f,f,f,f,f,f,f,f,f],
                    [f,f,f,f,f,f,f,f,f],
                    [f,f,f,f,f,f,f,f,f],
                    [w,w,w,w,w,w,w,w,w],
                    [w,w,w,w,w,w,w,w,w],
                    [w,w,w,w,w,w,w,w,w],
                  ]
    };
    public static Dictionary<(string, int), Block> AllBlocks = new(){
        {("straight",1), straight},
        {("line",1), line},
        {("cross", 1), cross},
    };
}
