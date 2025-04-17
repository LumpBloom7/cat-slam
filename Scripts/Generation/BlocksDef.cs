using System.Collections.Generic;

public static class BlockDefs
{
    private static Wall w = new();
    private static Floor f = new();

    private static Wall wb = new Wall { Type = Category.Beacon };
    private static Floor fb = new Floor { Type = Category.Beacon };

    public static readonly Block straight = new()
    {
        Name = ("straight", 1),
        TopAllow = [("cross", 1), ("straight", 1), ("room", 1), ("solid", 1)],
        BotAllow = [("cross", 1), ("straight", 1), ("room", 1), ("solid", 1)],
        RightAllow = [("straight", 1), ("room", 1), ("solid", 1)],
        LeftAllow = [("straight", 1), ("room", 1), ("solid", 1)],
        Tileset = [ [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,wb,f,f,f,wb,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                  ]
    };

    public static readonly Block room = new()
    {
        Name = ("room", 1),
        TopAllow = [("cross", 1), ("straight", 1), ("line", 1), ("room", 1), ("solid", 1)],
        BotAllow = [("cross", 1), ("straight", 1), ("line", 1), ("room", 1), ("solid", 1)],
        RightAllow = [("cross", 1), ("straight", 1), ("line", 1), ("room", 1), ("solid", 1)],
        LeftAllow = [("cross", 1), ("straight", 1), ("line", 1), ("room", 1), ("solid", 1)],
        Tileset = [ [fb,f,f,f,f,f,f,f,fb],
                    [f,f,f,f,f,f,f,f,f],
                    [f,f,f,f,f,f,f,f,f],
                    [f,f,f,f,f,f,f,f,f],
                    [f,f,f,f,f,f,f,f,f],
                    [f,f,f,f,f,f,f,f,f],
                    [f,f,f,f,f,f,f,f,f],
                    [f,f,f,f,f,f,f,f,f],
                    [fb,f,f,f,f,f,f,f,fb],
                  ]
    };
    public static readonly Block solid = new()
    {
        Name = ("room", 1),
        TopAllow = [("cross", 1), ("straight", 1), ("line", 1), ("room", 1), ("solid", 1)],
        BotAllow = [("cross", 1), ("straight", 1), ("line", 1), ("room", 1), ("solid", 1)],
        RightAllow = [("cross", 1), ("straight", 1), ("line", 1), ("room", 1), ("solid", 1)],
        LeftAllow = [("cross", 1), ("straight", 1), ("line", 1), ("room", 1), ("solid", 1)],
        Tileset = [ [w,w,w,w,wb,w,w,w,w],
                    [w,w,w,w,w,w,w,w,w],
                    [w,w,w,w,w,w,w,w,w],
                    [w,w,w,w,w,w,w,w,w],
                    [wb,w,w,w,w,w,w,w,wb],
                    [w,w,w,w,w,w,w,w,w],
                    [w,w,w,w,w,w,w,w,w],
                    [w,w,w,w,w,w,w,w,w],
                    [w,w,w,w,wb,w,w,w,w],
                  ]
    };
    public static readonly Block cross = new()
    {
        Name = ("cross", 1),
        TopAllow = [("cross", 1), ("straight", 1), ("room", 1), ("solid", 1)],
        BotAllow = [("cross", 1), ("straight", 1), ("room", 1), ("solid", 1)],
        RightAllow = [("cross", 1), ("line", 1), ("room", 1), ("solid", 1)],
        LeftAllow = [("cross", 1), ("line", 1), ("room", 1), ("solid", 1)],
        Tileset = [ [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,wb,f,f,f,wb,w,w],
                    [f,f,f,f,f,f,f,f,f],
                    [f,f,f,f,f,f,f,f,f],
                    [f,f,f,f,f,f,f,f,f],
                    [w,w,wb,f,f,f,wb,w,w],
                    [w,w,w,f,f,f,w,w,w],
                    [w,w,w,f,f,f,w,w,w],
                  ]
    };
    public static readonly Block line = new()
    {
        Name = ("line", 1),
        TopAllow = [("line", 1), ("room", 1), ("solid", 1)],
        BotAllow = [("line", 1), ("room", 1), ("solid", 1)],
        RightAllow = [("cross", 1), ("line", 1), ("room", 1), ("solid", 1)],
        LeftAllow = [("cross", 1), ("line", 1), ("room", 1), ("solid", 1)],
        Tileset = [ [w,w,w,w,w,w,w,w,w],
                    [w,w,w,w,w,w,w,w,w],
                    [w,w,w,w,wb,w,w,w,w],
                    [f,f,f,f,f,f,f,f,f],
                    [f,f,f,f,f,f,f,f,f],
                    [f,f,f,f,f,f,f,f,f],
                    [w,w,w,w,wb,w,w,w,w],
                    [w,w,w,w,w,w,w,w,w],
                    [w,w,w,w,w,w,w,w,w],
                  ]
    };
    public static Dictionary<(string, int), Block> AllBlocks = new(){
        {("straight",1), straight},
        {("line",1), line},
        {("cross", 1), cross},
        {("room", 1), room},
        {("solid", 1), solid},
    };
}
