using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class OccupancyMap : Node3D
{
    [Export]
    public Vector2 MapSize;

    [Export]
    public Vector2 CellSize;


    private CellContent[,] cellContents = null!;

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

        material = new StandardMaterial3D()
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor = Color.Color8(255, 0, 255, 128)
        };

        int Width = (int)Math.Ceiling(MapSize.X / CellSize.X);
        int Height = (int)Math.Ceiling(MapSize.Y / CellSize.Y);

        Position = -new Vector3(Width * CellSize.X, 0, Height * CellSize.Y) / 2;
        cellContents = new CellContent[Height, Width];

        for (int y = 0; y < cellContents.GetLength(0); ++y)
        {
            for (int x = 0; x < cellContents.GetLength(1); ++x)
            {
                var pos2d = CellSize * new Vector2(x, y);
                AddChild(cellContents[y, x] = new CellContent(material)
                {
                    Position = new Vector3(pos2d.X, 0, pos2d.Y),
                    Scale = new Vector3(CellSize.X, 1, CellSize.Y),
                });
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
        Vector2 halfCellSize = CellSize / 2;
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

            var cellContent = cellContents[cell.Y, cell.X];

            cellContent.OccupiedLikelihood = (float)Math.Clamp(cellContent.OccupiedLikelihood + (isFilled ? 1 : 0) - 0.5, 0, 1);
        }
    }

    public readonly record struct Cell
    {
        public int X { get; init; }
        public int Y { get; init; }
    }

    public partial class CellContent : MeshInstance3D
    {
        private StandardMaterial3D material;
        public CellContent(StandardMaterial3D material)
        {
            Mesh = new BoxMesh
            {
                Material = this.material = (StandardMaterial3D)material.Duplicate()
            };
        }
        private float occupiedL = 0.5f;

        public float OccupiedLikelihood
        {
            get => occupiedL;
            set
            {
                if (value == occupiedL)
                    return;

                occupiedL = value;
                material.AlbedoColor = material.AlbedoColor with { A = occupiedL };
                material.Transparency = occupiedL >= 1 ? BaseMaterial3D.TransparencyEnum.Disabled : BaseMaterial3D.TransparencyEnum.Alpha;
            }
        }

    }
}

