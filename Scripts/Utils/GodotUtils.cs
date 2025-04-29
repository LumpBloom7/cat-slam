using System;
using System.Collections.Generic;
using Godot;

public static class GodotUtils
{
    public static IEnumerable<T> GetDescendants<T>(this Node node, bool recursive = false) where T : Node
    {
        var children = node.GetChildren();
        foreach (var child in children)
        {
            if (child is T t)
                yield return t;

            if (recursive)
            {
                foreach (var childOfChild in child.GetDescendants<T>(recursive))
                    yield return childOfChild;
            }
        }
    }

    public static Vector2 To2D(this Vector3 vector3) => new(vector3.X, vector3.Y);
}
