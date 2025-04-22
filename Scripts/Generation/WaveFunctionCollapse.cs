using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

public class WaveFunctionCollapse
{
    public int Height { get; }
    public int Width { get; }
    private (int, int) errorPos = (-1, -1);
    private Random random = new Random();
    private const int MAX_ATTEMPTS = 10; // Maximum backtracking attempts
    private const int BLOCK_LENGTH = 9;

    public WaveFunctionCollapse(int width, int height)
    {
        Height = height;
        Width = width;
    }

    private bool entropyLeft(HashSet<(string, int)>[][] mapToCheck)
    {
        for (int i = 0; i < mapToCheck.Length; i++)
        {
            for (int j = 0; j < mapToCheck[i].Length; j++)
            {
                if (mapToCheck[i][j].Count > 1)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private (int, int) findLeastEntropy(HashSet<(string, int)>[][] mapToCheck)
    {
        // Finds the  position with the minimal entropy
        int min = int.MaxValue;
        (int, int) pos = errorPos;

        for (int i = 0; i < mapToCheck.Length; i++)
        {
            for (int j = 0; j < mapToCheck[i].Length; j++)
            {
                int count = mapToCheck[i][j].Count;
                if (count > 1 && count < min)
                {
                    min = count;
                    pos = (i, j);
                }
            }
        }
        return pos;
    }

    private List<(int, int)> getNeighbours((int, int) pos)
    {
        // Finds the position of the neighbours
        List<(int, int)> toAddTo = new List<(int, int)>();

        // UP
        if (pos.Item1 - 1 >= 0)
            toAddTo.Add((pos.Item1 - 1, pos.Item2));

        // DOWN
        if (pos.Item1 + 1 < Height)
            toAddTo.Add((pos.Item1 + 1, pos.Item2));

        // LEFT
        if (pos.Item2 - 1 >= 0)
            toAddTo.Add((pos.Item1, pos.Item2 - 1));

        // RIGHT
        if (pos.Item2 + 1 < Width)
            toAddTo.Add((pos.Item1, pos.Item2 + 1));

        return toAddTo;
    }

    private HashSet<(string, int)> getConstraintsFromNeighbor(
        (int, int) current,
        (int, int) neighbor,
        HashSet<(string, int)>[][] map)
    {
        var blockDict = BlockDefs.AllBlocks;
        var neighborOptions = map[neighbor.Item1][neighbor.Item2];
        HashSet<(string, int)> allowed = new HashSet<(string, int)>();

        // Calculates the direction of the constraint
        if (neighbor.Item1 < current.Item1) // Neighbor is ABOVE
        {
            Console.WriteLine("UP constraint");
            foreach (var block in neighborOptions)
            {
                Block toCheck = blockDict[block];
                allowed.UnionWith(toCheck.BotAllow);
            }
        }
        else if (neighbor.Item1 > current.Item1) // Neighbor is BELOW
        {
            Console.WriteLine("DOWN constraint");
            foreach (var block in neighborOptions)
            {
                Block toCheck = blockDict[block];
                allowed.UnionWith(toCheck.TopAllow);
            }
        }
        else if (neighbor.Item2 < current.Item2) // Neighbor is LEFT
        {
            Console.WriteLine("LEFT constraint");
            foreach (var block in neighborOptions)
            {
                Block toCheck = blockDict[block];
                allowed.UnionWith(toCheck.RightAllow);
            }
        }
        else if (neighbor.Item2 > current.Item2) // Neighbor is RIGHT
        {
            Console.WriteLine("RIGHT constraint");
            foreach (var block in neighborOptions)
            {
                Block toCheck = blockDict[block];
                allowed.UnionWith(toCheck.LeftAllow);
            }
        }

        return allowed;
    }

    private bool updateCell((int, int) pos, HashSet<(string, int)>[][] map)
    {
        var neighbors = getNeighbours(pos);
        var currentOptions = new HashSet<(string, int)>(map[pos.Item1][pos.Item2]);

        foreach (var neighbor in neighbors)
        {
            var constraintsFromNeighbor = getConstraintsFromNeighbor(pos, neighbor, map);

            // Applies constraints by intersecting with current options
            currentOptions.IntersectWith(constraintsFromNeighbor);

            // Checks for contradiction (no options left)
            if (currentOptions.Count == 0)
            {
                Console.WriteLine($"Contradiction at ({pos.Item1}, {pos.Item2})");
                return false;
            }
        }

        // Checks if a change occured
        if (!currentOptions.SetEquals(map[pos.Item1][pos.Item2]))
        {
            map[pos.Item1][pos.Item2] = currentOptions;
            return true;
        }

        return false;
    }

    private bool propagateConstraints((int, int) startPos, HashSet<(string, int)>[][] map)
    {
        Queue<(int, int)> queue = new Queue<(int, int)>();
        HashSet<(int, int)> enqueued = new HashSet<(int, int)>();

        // Starts with neighbors of the collapsed cell
        foreach (var neighbor in getNeighbours(startPos))
        {
            queue.Enqueue(neighbor);
            enqueued.Add(neighbor);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            // Updates the current cell based on its neighbors
            bool cellUpdated = updateCell(current, map);

            // Signals contradiction
            if (map[current.Item1][current.Item2].Count == 0)
                return false;

            // If cell was updated, add neighbours to queue
            if (cellUpdated)
            {
                foreach (var neighbor in getNeighbours(current))
                {
                    if (!enqueued.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        enqueued.Add(neighbor);
                    }
                }
            }
        }

        return true; // No contradictions found
    }

    private HashSet<(string, int)>[][] cloneGrid(HashSet<(string, int)>[][] original)
    {
        // Clones the grid, for backtracking purpuses
        var clone = new HashSet<(string, int)>[Height][];

        for (int i = 0; i < Height; i++)
        {
            clone[i] = new HashSet<(string, int)>[Width];
            for (int j = 0; j < Width; j++)
            {
                clone[i][j] = new HashSet<(string, int)>(original[i][j]);
            }
        }

        return clone;
    }

    private bool collapseCell((int, int) pos, HashSet<(string, int)>[][] map, (string, int) chosenOption)
    {
        var options = map[pos.Item1][pos.Item2];

        // Collapses to the chosen option
        options.Clear();
        options.Add(chosenOption);

        // Propagates the effects of the collapse
        return propagateConstraints(pos, map);
    }
    public void wallOffMap(ref Tile[][] map)
    {
        int height = map.Length;
        int width = map[0].Length;
        for (int i = 0; i < height; i++)
        {
            map[i][0] = new Wall();
            map[i][width - 1] = new Wall();
        }
        for (int i = 0; i < width; i++)
        {
            map[0][i] = new Wall();
            map[width - 1][i] = new Wall();
        }
    }

    private Vector2I? chooseEmptyCell(ref Tile[][] map, Vector2I rangeBegin, Vector2I rangeEnd)
    {
        Vector2I? result = null;

        for (int y = rangeBegin.Y; y < rangeEnd.Y; ++y)
            for (int x = rangeBegin.X; x < rangeEnd.X; ++x)
                if (map[y][x] is Floor)
                    result = (Random.Shared.NextSingle() < 0.25f) ? new Vector2I(x, y) : (result ?? new Vector2I(x, y));


        return result;
    }

    public void setSpawn(ref Tile[][] map)
    {
        List<(int y, int x)> openPositions = new List<(int y, int x)>();
        for (int y = 0; y < map.Length; y++)
        {
            for (int x = 0; x < map[y].Length; x++)
            {
                if (map[y][x] is Floor)
                {
                    openPositions.Add((y, x));
                }
            }
        }
        int r = random.Next(openPositions.Count);
        var spawn = openPositions[r];

        map[spawn.y][spawn.x] = new Spawn();
    }

    private static Vector2I[] adjacencyOffsets = [new(-1, 0), new(0, -1), new(1, 0), new(0, 1)];

    public bool isConnected(Block source, Block other, Vector2I direction)
    {
        if (Math.Abs(direction.X) == 1)

            return source.Name.Item1 is "room" or "cross" or "line"
                && other.Name.Item1 is "room" or "cross" or "line";

        if (Math.Abs(direction.Y) == 1)
            return source.Name.Item1 is "room" or "cross" or "straight"
            && other.Name.Item1 is "room" or "cross" or "straight";

        return false;
    }

    public void connectIslands(Block[][] blockMap, ref Tile[][] map)
    {
        List<HashSet<Vector2I>> sets = [];
        List<Vector2I> queue = [];

        for (int y = 0; y < blockMap.Length; ++y)
            for (int x = 0; x < blockMap[y].Length; ++x)
                if (blockMap[y][x].Name.Item1 != "solid") queue.Add(new(x, y));

        while (queue.Count > 0)
        {
            HashSet<Vector2I> inGroup = [];
            var first = queue[0];

            checkConnectionRecursive(blockMap, first, first, [], inGroup);
            sets.Add(inGroup);

            foreach (var block in inGroup)
                queue.Remove(block);
        }

        while (sets.Count > 1)
        {
            var firstSet = sets[0];
            var secondSet = sets[1];

            Vector2I startBlock = firstSet.ToArray()[Random.Shared.Next(firstSet.Count)];
            Vector2I endBlock = secondSet.ToArray()[Random.Shared.Next(secondSet.Count)];

            var start = chooseEmptyCell(ref map, startBlock * 9, (startBlock + new Vector2I(1, 1)) * 9);
            var end = chooseEmptyCell(ref map, endBlock * 9, (endBlock + new Vector2I(1, 1)) * 9);

            Debug.Assert(start is not null);
            Debug.Assert(end is not null);

            performAstar(start.Value, end.Value, ref map);

            var removeTarget = (Random.Shared.NextSingle() < 0.5f) ? firstSet : secondSet;

            sets.Remove(removeTarget);
        }
    }

    private void performAstar(Vector2I origin, Vector2I dest, ref Tile[][] map)
    {
        PriorityQueue<Vector2I, int> priorityQueue = new();
        priorityQueue.Enqueue(origin, 0);

        Dictionary<Vector2I, Vector2I> cameFrom = [];

        Dictionary<Vector2I, int> gScore = [];
        gScore[origin] = 0;

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Dequeue();

            if (current == dest)
                break;

            var neighbours = adjacencyOffsets.Select(o => current + o);

            foreach (var n in neighbours)
            {
                int tentativeScore = gScore[current] + 1;

                if (tentativeScore >= gScore.GetValueOrDefault(n, int.MaxValue))
                    continue;

                cameFrom[n] = current;
                gScore[n] = tentativeScore;
                priorityQueue.Enqueue(n, dest.DistanceSquaredTo(n) + (map[n.Y][n.X] is Wall ? 100 : 0));
            }
        }

        var _current = dest;

        while (_current != origin)
        {
            var next = cameFrom[_current];
            map[next.Y][next.X] = new Floor();

            _current = next;
        }
    }



    public void checkConnectionRecursive(Block[][] blockMap, Vector2I first, Vector2I second, HashSet<Vector2I> visited, HashSet<Vector2I> inGroup)
    {
        if (second.Y < 0 || second.Y >= blockMap.Length)
            return;

        if (second.X < 0 || second.X >= blockMap[second.Y].Length)
            return;

        if (visited.Contains(second))
            return;

        visited.Add(second);
        Block firstBlock = blockMap[first.Y][first.X];
        Block secondBlock = blockMap[second.Y][second.X];

        if (first == second || isConnected(firstBlock, secondBlock, second - first))
        {
            inGroup.Add(second);

            foreach (var offset in adjacencyOffsets)
            {
                var newOffset = second + offset;
                checkConnectionRecursive(blockMap, second, newOffset, visited, inGroup);
            }
        }
    }

    public Tile[][] transformIntoTiles(Block[][] map)
    {
        int newHeight = Height * BLOCK_LENGTH;
        int newWidth = Width * BLOCK_LENGTH;
        Tile[][] finalTieset = new Tile[newHeight][];
        for (int i = 0; i < finalTieset.Length; i++)
        {
            finalTieset[i] = new Tile[newWidth];
        }

        for (int i = 0; i < map.Length; i++)
        {
            for (int j = 0; j < map[i].Length; j++)
            {
                Tile[][] currentTileset = map[i][j].Tileset;
                for (int n = 0; n < currentTileset.Length; n++)
                {
                    for (int k = 0; k < currentTileset[n].Length; k++)
                    {
                        int newRow = i * currentTileset.Length + n;
                        int newColumn = j * currentTileset[n].Length + k;
                        finalTieset[newRow][newColumn] = currentTileset[n][k];
                    }
                }
            }
        }
        // Post processing steps

        // Walling off the edges of the map
        wallOffMap(ref finalTieset);

        // Connecting islands
        connectIslands(map, ref finalTieset);

        // Setting the starting position
        setSpawn(ref finalTieset);

        return finalTieset;
    }

    public Tile[][]? Generate()
    {
        int attempts = 0;

        while (attempts < MAX_ATTEMPTS)
        {
            attempts++;
            Console.WriteLine($"Attempt {attempts}");

            // Initializes a new grid with all options
            HashSet<(string, int)>[][] map = new HashSet<(string, int)>[Height][];
            for (int i = 0; i < Height; i++)
            {
                map[i] = new HashSet<(string, int)>[Width];
                for (int j = 0; j < Width; j++)
                {
                    map[i][j] = new HashSet<(string, int)>(BlockDefs.AllBlocks.Keys);
                }
            }

            // Tries to solve the grid
            bool success = solveGrid(map);

            if (success)
                return transformIntoTiles([.. map.Select(row => row.Select(h => BlockDefs.AllBlocks[h.Single()]).ToArray())]);
        }

        Console.WriteLine($"Failed to generate a valid solution after {MAX_ATTEMPTS} attempts.");
        return null;

    }

    private bool solveGrid(HashSet<(string, int)>[][] map)
    {
        Stack<(HashSet<(string, int)>[][], (int, int), List<(string, int)>)> backtrackStack = new Stack<(HashSet<(string, int)>[][], (int, int), List<(string, int)>)>();

        try
        {
            while (entropyLeft(map))
            {
                Console.WriteLine("New iteration ----------------");

                // Finds cell with lowest entropy
                var pos = findLeastEntropy(map);
                if (pos == errorPos)
                    // No more cells with entropy > 1 (SHOULD NOT BE POSSIBLE, IF SO THEN RIP)
                    break;

                // Gets all options for this cell
                var options = map[pos.Item1][pos.Item2].ToList();

                // Saves the current state for backtracking
                backtrackStack.Push((cloneGrid(map), pos, options));

                // Gets random index to get a random option
                int randIndex = random.Next(options.Count);
                var option = options[randIndex];
                Console.WriteLine($"Trying option {option} at ({pos.Item1}, {pos.Item2})");

                //Error checking
                if (!collapseCell(pos, map, option))
                {
                    // Backtracking if collapse didn't work
                    if (!backtrack(ref map, backtrackStack))
                        // Backtracking failed
                        return false;
                }

                // Check for validity
                if (hasEmptyCells(map))
                {
                    // Backtracking if empty cells present
                    if (!backtrack(ref map, backtrackStack))
                        // Backtracking failed
                        return false;
                }
            }

            return !hasEmptyCells(map);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during solving: {ex.Message}");
            return false;
        }
    }

    private bool backtrack(ref HashSet<(string, int)>[][] map,
                          Stack<(HashSet<(string, int)>[][], (int, int), List<(string, int)>)> backtrackStack)
    {
        // MIGHT BE FOULTY, MADE JUST IN CASE
        Console.WriteLine("Backtracking...");

        if (backtrackStack.Count == 0)
            // Nothing to backtrack to (RIP)
            return false;

        var (savedMap, pos, options) = backtrackStack.Pop();

        // Remove option that lead to fail
        options.RemoveAt(0);

        if (options.Count == 0)
        {
            // Backtrack further if no oprions present
            return backtrack(ref map, backtrackStack);
        }

        // Restores the map state
        map = savedMap;

        // Try the next option
        var nextOption = options[0];
        Console.WriteLine($"Trying alternative option {nextOption} at ({pos.Item1}, {pos.Item2})");

        // Push updated options back to the stack
        backtrackStack.Push((cloneGrid(map), pos, options));

        // Try to collapse with the new option
        return collapseCell(pos, map, nextOption);
    }

    private bool hasEmptyCells(HashSet<(string, int)>[][] map)
    {
        for (int i = 0; i < Height; i++)
        {
            for (int j = 0; j < Width; j++)
            {
                if (map[i][j].Count == 0)
                    return true;
            }
        }
        return false;
    }

    public static void Main(string[] args)
    {
        WaveFunctionCollapse w = new WaveFunctionCollapse(3, 3);
        Tile[][]? tiles = w.Generate();

        // Print the result

        if (tiles is null)
        {
            Console.WriteLine("Failed to generate a solution.");
            return;
        }

        Console.WriteLine("Final solution:");
        // foreach (var row in blocks)
        // {
        //     foreach (var cell in row)
        //     {
        //         Console.Write($"[{cell.Count}] ");
        //         foreach (var option in cell)
        //         {
        //             Console.Write($"{option} ");
        //         }
        //         Console.Write(" | ");
        //     }
        //     Console.WriteLine();
        // }
        foreach (var row in tiles)
        {
            foreach (var cell in row)
            {
                if (cell is Wall)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("W");
                }
                else if (cell is Floor)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("F");
                }
                else if (cell is Spawn)
                {
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.Write("S");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("-");
                }
                Console.ForegroundColor = Console.BackgroundColor;
                Console.Write(" | ");
            }
            Console.WriteLine();
        }
    }
}
