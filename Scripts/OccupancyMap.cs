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


    private Cell[,] cellContents = null!;

    private StandardMaterial3D material = null!;

    private bool lidarsConnected = false;

    private void connectLidars()
    {
        foreach (var lidar in GetParent().GetDescendants<SimulatedLidar>(true))
            lidar.RayCasted += ProcessRayCast;
    }
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
                    AlbedoColor = Color.Color8(255, 255, 255),
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
        cellContents = new Cell[Height, Width];

        for (int y = 0; y < cellContents.GetLength(0); ++y)
        {
            for (int x = 0; x < cellContents.GetLength(1); ++x)
            {
                int index = y * Height + x;
                var meshTransform = Transform3D.Identity;

                var cell = cellContents[y, x] = new Cell
                {
                    X = x,
                    Y = y,
                    Index = index,
                    OccupiedLikelihood = 0.5f
                };

                meshTransform = meshTransform.Scaled(new Vector3(CellSize.X, 0.5f, CellSize.Y)).TranslatedLocal(new Vector3(x, 0.25f, y));

                Multimesh.SetInstanceTransform(cell.Index, meshTransform);
                Multimesh.SetInstanceColor(cell.Index, Color.Color8((byte)Random.Shared.Next(0, 255), (byte)Random.Shared.Next(0, 255), (byte)Random.Shared.Next(0, 255), 255));
            }
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (lidarsConnected)
            return;

        connectLidars();
        lidarsConnected = true;
    }


    public IEnumerable<Cell> getIntersectingTiles(Vector3 origin, Vector3 target)
    {
        const float PRECISION = 0.01f;

        Vector2 step = new Vector2(target.X - origin.X, target.Z - origin.Z) * PRECISION;
        Vector2 halfMapSize = MapSize / 2;

        Vector2 current = new Vector2(origin.X, origin.Z) + halfMapSize;

        Cell cell = new Cell { X = -5000, Y = -5000 };
        for (float i = 0; i <= 1; i += PRECISION)
        {
            Cell nextCell = new()
            {
                X = (int)Math.Round(current.X / CellSize.X),
                Y = (int)Math.Round(current.Y / CellSize.Y)
            };
            current += step;

            if (nextCell != cell)
            {
                yield return nextCell;
            }

            cell = nextCell;
        }
    }

    public void ProcessRayCast(Vector3 origin, Vector3 target, bool isColliding)
    {
        var cells = getIntersectingTiles(origin, target).ToArray();

        for (int i = 0; i < cells.Length; ++i)
        {
            bool isFilled = i == cells.Length - 1 && isColliding;
            var cell = cells[i];

            if (cell.Y < 0 || cell.Y >= cellContents.GetLength(0))
                continue;

            if (cell.X < 0 || cell.X >= cellContents.GetLength(1))
                continue;

            ref var cellContent = ref cellContents[cell.Y, cell.X];

            float dist = new Vector2(cell.X * CellSize.X, cell.Y * CellSize.Y).DistanceTo(new Vector2(origin.X, origin.Z) + MapSize / 2);
            float prob = ((isFilled ? 1 : 0) - 0.5f) * (1 - Math.Clamp(dist, 0, 5) / 5);

            cellContent.OccupiedLikelihood = (float)Math.Clamp(cellContent.OccupiedLikelihood + prob, 0, 1);

            var newTransform = Transform3D.Identity;
            newTransform = newTransform.Scaled(new Vector3(CellSize.X, cellContent.OccupiedLikelihood, CellSize.Y)).TranslatedLocal(new Vector3(cell.X, cellContent.OccupiedLikelihood * 0.5f, cell.Y)).Translated(new Vector3(0, -0.1f, 0));

            Multimesh.SetInstanceTransform(cellContent.Index, newTransform);
        }
    }

    public record struct Cell
    {
        public int X { get; init; }
        public int Y { get; init; }

        public int Index { get; init; }

        public float OccupiedLikelihood { get; set; }
    }
}

