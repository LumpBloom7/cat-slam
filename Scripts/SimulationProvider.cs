using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Cell = OccupancyMap.Cell;
public partial class SimulationProvider : Node
{
    private RobotCharacter robotCharacter = null!;

    private DigitalRepresentation[] digitalRepresentations = null!;

    public override void _Ready()
    {
        base._Ready();

        var rootNode = GetNode("/root");

        digitalRepresentations = [.. rootNode.GetDescendants<DigitalRepresentation>(true)];
        robotCharacter = rootNode.GetDescendants<RobotCharacter>(true).First()!;
    }

    public DigitalRepresentation? activeDigitalRepresentation => digitalRepresentations.FirstOrDefault(d => d.Visible);

    public SimulationContext createSimulationContext()
    {
        var cDR = digitalRepresentations.Where(d => d.Name == "KalmanRepresentation").FirstOrDefault();

        return new SimulationContext(cDR!.OccupancyMap, cDR.Ghost.GlobalPosition, cDR.Ghost.GlobalRotation.Y, robotCharacter);
    }

    public class SimulationContext
    {
        public SimulationContext(OccupancyMap map, Vector3 originalPosition, float originalRotation, RobotCharacter robotCharacter)
        {
            occupancyMap = map;
            this.originalPosition = Position = originalPosition;
            this.originalRotation = Rotation = originalRotation;
            originalLeftVel = LeftVel = robotCharacter.LeftVel;
            originalRightVel = RightVel = robotCharacter.RightVel;

            AccelerationPerSecond = robotCharacter.AccelerationPerSecond;
            DeccelerationPerSecond = robotCharacter.DeccelerationPerSecond;
            MaxSpeed = robotCharacter.MaxSpeed;
            Radius = robotCharacter.Radius;


            SensorArray sensorArr = robotCharacter.GetDescendants<SensorArray>().First()!;
            originalDistances = [.. sensorArr.GetDistances()];
            SensorValues = [.. originalDistances];

            numSensor = sensorArr.NumberOfSensors;
            sensorRange = sensorArr.SensorRange;
        }
        public SimulationContext(SimulationContext other)
        {
            occupancyMap = other.occupancyMap;
            originalPosition = Position = other.originalPosition;
            originalRotation = Rotation = other.originalRotation;
            originalLeftVel = LeftVel = other.originalLeftVel;
            originalRightVel = RightVel = other.originalRightVel;

            AccelerationPerSecond = other.AccelerationPerSecond;
            DeccelerationPerSecond = other.DeccelerationPerSecond;
            MaxSpeed = other.MaxSpeed;
            Radius = other.Radius;

            originalDistances = other.originalDistances;
            SensorValues = [.. originalDistances];

            numSensor = originalDistances.Length;
            sensorRange = other.sensorRange;
        }

        public SimulationContext Copy() => new(this);

        private readonly int numSensor;
        private readonly OccupancyMap occupancyMap;
        private readonly Vector3 originalPosition;
        private readonly float originalRotation;
        private readonly float[] originalDistances;
        private readonly float AccelerationPerSecond;
        private readonly float DeccelerationPerSecond;
        private readonly float MaxSpeed;
        private readonly float Radius = 0.15f;
        private readonly float sensorRange;
        private readonly float originalLeftVel, originalRightVel;

        public Vector3 Position { get; private set; }
        public float Rotation { get; private set; }

        public float LeftVel { get; private set; }
        public float RightVel { get; private set; }

        public float[] SensorValues { get; private set; }

        public void Update(int leftInput, int rightInput, double dt)
        {
            float STEPSIZE = (float)dt * 10;
            // Compute acceleration amounts
            float leftAcc = 0;
            float rightAcc = 0;

            //Console.WriteLine(leftInput); Console.WriteLine(rightInput);

            leftAcc += AccelerationPerSecond * STEPSIZE * leftInput;
            rightAcc += AccelerationPerSecond * STEPSIZE * rightInput;

            // Apply speed changes
            if (leftAcc == 0)
                LeftVel = Math.Max(Math.Abs(LeftVel) - DeccelerationPerSecond * STEPSIZE, 0) * MathF.Sign(LeftVel);
            else
                LeftVel = Math.Clamp(leftAcc + LeftVel, -MaxSpeed, MaxSpeed);

            if (rightAcc == 0)
                RightVel = Math.Max(Math.Abs(RightVel) - DeccelerationPerSecond * STEPSIZE, 0) * MathF.Sign(RightVel);
            else
                RightVel = Math.Clamp(rightAcc + RightVel, -MaxSpeed, MaxSpeed);

            float rotAmount = (RightVel - LeftVel) / (Radius * 2);

            Rotation += rotAmount * STEPSIZE;

            float velocity = (LeftVel + RightVel) / 2;
            Vector3 movementVector = (Vector3.Forward * velocity).Rotated(new Vector3(0, 1, 0), Rotation);

            var newpos = Position + movementVector * STEPSIZE;

            // Aggressive prevention of movement
            float occMapHeight = occupancyMap.CellContents.GetLength(0);
            float occMapWidth = occupancyMap.CellContents.GetLength(1);

            float angleStep = 2 * MathF.PI / 4;

            Vector3 line = Vector3.Forward * Radius;

            for (int i = 0; i < 4; ++i)
            {
                float angleDiff = angleStep * i;
                var target = Position + line.Rotated(new(0, 1, 0), angleDiff + Rotation);

                var tmp = getIntersectingTiles(target, Position, 0.1f);

                foreach ((CellLite cell, float _) in tmp)
                {
                    bool OOB = cell.Y < 0 || cell.Y >= occMapHeight || cell.X < 0 || cell.X >= occMapWidth;

                    if (OOB)
                        return;

                    if (occupancyMap.CellContents[cell.Y, cell.X].OccupiedLikelihood >= 0.6)
                        return;
                }
            }

            Position = newpos;
        }

        public int ComputeReward()
        {
            float angleStep = 2 * MathF.PI / numSensor;
            Vector3 line = Vector3.Forward * sensorRange;

            float dist = originalPosition.DistanceTo(Position);

            int rewardSum = (int)(dist * 500000);

            for (int i = 0; i < numSensor; ++i)
            {
                float angleDiff = angleStep * i;
                var target = Position + line.Rotated(new(0, 1, 0), angleDiff + Rotation);

                int r = beamReward(Position, target, i);

                if (r < 0)
                {
                    rewardSum = -1000000;
                    break;
                }

                rewardSum += r;
            }

            return rewardSum;
        }

        public void Reset()
        {
            Position = originalPosition;
            Rotation = originalRotation;
            LeftVel = originalLeftVel;
            RightVel = originalRightVel;
            SensorValues = [.. originalDistances];
        }

        public IEnumerable<(CellLite cell, float prog)> getIntersectingTiles(Vector3 origin, Vector3 target, float precision = 0.01f)
        {
            Vector2 step = new Vector2(target.X - origin.X, target.Z - origin.Z) * precision;
            Vector2 halfMapSize = occupancyMap.MapSize / 2;

            Vector2 current = new Vector2(origin.X, origin.Z) + halfMapSize;
            CellLite? lastCell = null;
            float lastProg = 0;

            for (float i = 0f; i <= 1.1f; i += precision)
            {
                CellLite nextCell = new()
                {
                    X = (int)Math.Round(current.X / occupancyMap.CellSize.X),
                    Y = (int)Math.Round(current.Y / occupancyMap.CellSize.Y)
                };
                current += step;

                if (lastCell.HasValue && nextCell != lastCell)
                    yield return (lastCell.Value, lastProg);

                lastCell = nextCell;
                lastProg += precision;
            }

            if (lastCell.HasValue)
                yield return (lastCell.Value, lastProg);
        }

        private int beamReward(Vector3 origin, Vector3 target, int index)
        {
            var cells = getIntersectingTiles(origin, target);

            float length = origin.DistanceTo(target);

            int rewardCount = 0;

            float occMapHeight = occupancyMap.CellContents.GetLength(0);
            float occMapWidth = occupancyMap.CellContents.GetLength(1);

            float furthest = 1;

            foreach ((CellLite cell, float prog) in cells)
            {
                bool OOB = cell.Y < 0 || cell.Y >= occMapHeight || cell.X < 0 || cell.X >= occMapWidth;
                furthest = prog;

                // Don't incentivise OOB exploration
                if (OOB)
                {
                    if (length * prog < Radius * 1.5f)
                        rewardCount = -1000000;

                    break;
                }

                ref var actualCell = ref occupancyMap.CellContents[cell.Y, cell.X];

                // Let's not give more info
                if (actualCell.OccupiedLikelihood > 0.5f)
                {
                    if (length <= Radius * 1.5f)
                    {
                        rewardCount = -500000;
                        break;
                    }
                    break;
                }

                if (!actualCell.explored)
                    rewardCount += 1000;

                rewardCount += 10;
            }

            SensorValues[index] = Math.Clamp(length * furthest, 0, sensorRange);

            //rewardCount += (int)Math.Pow((int)SensorValues[index], 2) * 1000;

            return rewardCount;
        }

        public readonly record struct CellLite(int X, int Y);

    }

}
