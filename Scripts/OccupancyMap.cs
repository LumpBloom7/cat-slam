using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class OccupancyMap : MultiMeshInstance3D
{
    [Export]
    public Vector2 MapSize;

    [Export]
    public Vector2 CellSize;

    [Export]
    public Color CellColour { get; set; } = Color.Color8(255, 0, 255);

    public Cell[,] CellContents { get; private set; } = null!;

    private StandardMaterial3D material = null!;

    private bool lidarsConnected = false;

    private int exploredTiles = 0;

    public override void _Ready()
    {
        base._Ready();

        Multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = new BoxMesh()
            {
                Material = new StandardMaterial3D
                {
                    AlbedoColor = Color.Color8(255, 255, 255, 255),
                    VertexColorUseAsAlbedo = true,
                    DiffuseMode = BaseMaterial3D.DiffuseModeEnum.Toon
                }
            },
        };

        int Width = (int)Math.Ceiling(MapSize.X / CellSize.X);
        int Height = (int)Math.Ceiling(MapSize.Y / CellSize.Y);

        Multimesh.InstanceCount = Width * Height;
        Multimesh.VisibleInstanceCount = Width * Height;

        Position = -new Vector3(Width * CellSize.X, 0, Height * CellSize.Y) / 2;
        CellContents = new Cell[Height, Width];

        for (int y = 0; y < CellContents.GetLength(0); ++y)
        {
            for (int x = 0; x < CellContents.GetLength(1); ++x)
            {
                int index = y * Height + x;
                var meshTransform = Transform3D.Identity;

                var cell = CellContents[y, x] = new Cell
                {
                    X = x,
                    Y = y,
                    Index = index,
                    OccupiedLikelihood = 0.5f
                };

                meshTransform = meshTransform.Scaled(new Vector3(CellSize.X, 0.5f, CellSize.Y)).TranslatedLocal(new Vector3(x, 0.25f, y));

                Multimesh.SetInstanceTransform(cell.Index, meshTransform);
                Multimesh.SetInstanceColor(cell.Index, CellColour * new Color(0.5f, 0.5f, 0.5f, 1));
            }
        }
    }

    public IEnumerable<(Cell, float prog)> getIntersectingTiles(Vector3 origin, Vector3 target)
    {
        const float PRECISION = 0.01f;

        Vector2 step = new Vector2(target.X - origin.X, target.Z - origin.Z) * PRECISION;
        Vector2 halfMapSize = MapSize / 2;

        Vector2 current = new Vector2(origin.X, origin.Z) + halfMapSize;

        for (float i = 0; i <= 1.1; i += PRECISION)
        {
            Cell nextCell = new()
            {
                X = (int)Math.Round(current.X / CellSize.X),
                Y = (int)Math.Round(current.Y / CellSize.Y)
            };
            current += step;
            yield return (nextCell, i);
        }
    }

    public void ProcessRayCast(Vector3 origin, Vector3 target, bool isColliding)
    {
        var cells = getIntersectingTiles(origin, target).GroupBy(x => x.Item1).Select(g => g.MaxBy(c => c.prog));

        foreach ((Cell cell, float prog) in cells)
        {
            bool isFilled = prog >= 0.9f && isColliding;

            if (cell.Y < 0 || cell.Y >= CellContents.GetLength(0))
                continue;

            if (cell.X < 0 || cell.X >= CellContents.GetLength(1))
                continue;

            ref var cellContent = ref CellContents[cell.Y, cell.X];

            float dist = new Vector2(cell.X * CellSize.X, cell.Y * CellSize.Y).DistanceTo(new Vector2(origin.X, origin.Z) + MapSize / 2);
            float prob = ((isFilled ? 1 : 0) - 0.5f) * (1 - Math.Clamp(dist, 0, 5) / 5);

            cellContent.OccupiedLikelihood = (float)Math.Clamp(cellContent.OccupiedLikelihood + prob, 0, 1);
            if (!cellContent.explored)
            {
                cellContent.explored = true;
                ++exploredTiles;
            }

            var newTransform = Transform3D.Identity;
            newTransform = newTransform.Scaled(new Vector3(CellSize.X, cellContent.OccupiedLikelihood, CellSize.Y)).TranslatedLocal(new Vector3(cell.X, cellContent.OccupiedLikelihood * 0.5f, cell.Y)).Translated(new Vector3(0, -0.1f, 0));

            Multimesh.SetInstanceTransform(cellContent.Index, newTransform);

            var colour = CellColour * new Color(cellContent.OccupiedLikelihood, cellContent.OccupiedLikelihood, cellContent.OccupiedLikelihood, 1);
            Multimesh.SetInstanceColor(cellContent.Index, colour);
        }
    }

    public record struct Cell : IEquatable<Cell>
    {
        public int X { get; init; }
        public int Y { get; init; }

        public int Index { get; init; }

        public float OccupiedLikelihood { get; set; }

        public bool explored { get; set; }
    }
}

