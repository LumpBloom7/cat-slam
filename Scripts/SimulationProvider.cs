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
        var cDR = digitalRepresentations[0];

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

        public void Update(int leftInput, int rightInput)
        {
            const float STEPSIZE = 0.01f;
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

            Position += movementVector * STEPSIZE;
        }

        public int ComputeReward()
        {
            float angleStep = 2 * MathF.PI / numSensor;

            Vector3 line = Vector3.Forward * sensorRange;

            int rewardSum = 0;

            for (int i = 0; i < numSensor; ++i)
            {
                float angleDiff = angleStep * i;
                var target = Position + line.Rotated(new(0, 1, 0), angleDiff + Rotation);

                int r = beamReward(Position, target, i);

                if (r < 0)
                {
                    rewardSum = -10000000;
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

        private int beamReward(Vector3 origin, Vector3 target, int index)
        {
            const float PRECISION = 0.05f;

            Vector2 step = new Vector2(target.X - origin.X, target.Z - origin.Z) * PRECISION;

            float lengthStep = step.Length();

            Vector2 halfMapSize = occupancyMap.MapSize / 2;
            Vector2 current = new Vector2(origin.X, origin.Z) + halfMapSize;

            CellLite cell = new CellLite { X = -5000, Y = -5000 };
            int rewardCount = 0;

            float length = 0;

            float occMapHeight = occupancyMap.CellContents.GetLength(0);
            float occMapWidth = occupancyMap.CellContents.GetLength(1);

            for (float i = 0; i <= 1; i += PRECISION, length += lengthStep)
            {
                CellLite nextCell = new()
                {
                    X = (int)Math.Round(current.X / occupancyMap.CellSize.X),
                    Y = (int)Math.Round(current.Y / occupancyMap.CellSize.Y)
                };

                bool OOB = nextCell.Y < 0 || nextCell.Y >= occMapHeight || nextCell.X < 0 || nextCell.X >= occMapWidth;

                if (OOB)
                {
                    if (length < Radius * 1.5f)
                        rewardCount = -100000;

                    break;
                }
                ref var actualCell = ref occupancyMap.CellContents[nextCell.Y, nextCell.X];

                // Let's not give more info
                if (actualCell.OccupiedLikelihood > 0.5f)
                {
                    if (length <= Radius * 1.5f)
                    {
                        rewardCount = -50000;
                        break;
                    }
                }

                current += step;

                if (nextCell != cell)
                {
                    if (!actualCell.explored)
                        rewardCount += 1;
                }

                cell = nextCell;
            }

            SensorValues[index] = length;

            return rewardCount;
        }

        private readonly record struct CellLite(int X, int Y);

    }

}
