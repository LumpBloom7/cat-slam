
using System;
using System.Numerics;

public static class MathematicalUtils
{
    public static float ToMathematicalAngle(this float theta) => theta + MathF.PI / 2;
    public static float FromMathematicalAngle(this float theta) => theta - MathF.PI / 2;

    public static MathNet.Numerics.LinearAlgebra.Vector<float> ToMathVector(this Vector2 vector2)
    {
        return MathNet.Numerics.LinearAlgebra.Vector<float>.Build.Dense([vector2.X, -vector2.Y]);
    }
}
