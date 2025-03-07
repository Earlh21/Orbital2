using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Physics;

public struct Bounds
{
    //Must use top/right instead of width/height or bounds checking becomes inconsistent and errors will occur
    public float Left { get; }
    public float Bottom { get; }
    public float Right { get; }
    public float Top { get; }

    public float Width => Right - Left;
    public float Height => Top - Bottom;

    public float CenterX => Left + Width / 2;
    public float CenterY => Bottom + Height / 2;
    public Vector2 Center => new(CenterX, CenterY);

    public Vector2 BottomLeft => new(Left, Bottom);
    public Vector2 BottomRight => new(Right, Bottom);
    public Vector2 TopLeft => new(Left, Top);
    public Vector2 TopRight => new(Right, Top);

    public Bounds(float left, float bottom, float right, float top)
    {
        Left = left;
        Bottom = bottom;
        Right = right;
        Top = top;
    }

    public Bounds(Vector2 bottomLeft, Vector2 topRight)
    {
        Left = bottomLeft.X;
        Bottom = bottomLeft.Y;
        Right = topRight.X;
        Top = topRight.Y;
    }

    public Bounds(float left, float bottom, Vector2 topRight)
    {
        Bottom = bottom;
        Left = left;
        Right = topRight.X;
        Top = topRight.Y;
    }

    public Bounds(Vector2 bottomLeft, float right, float top)
    {
        Left = bottomLeft.X;
        Bottom = bottomLeft.Y;
        Right = right;
        Top = top;
    }

    public bool Overlaps(Bounds other)
    {
        bool overlapsX = (Left <= other.Left && Right >= other.Left) || (Left > other.Left && Left <= other.Right);
        bool overlapsY = (Bottom <= other.Bottom && Top >= other.Bottom) || (Bottom > other.Bottom && Bottom <= other.Top);

        return overlapsX && overlapsY;
    }

    public bool ContainsPoint(Vector2 point)
    {
        return point.X >= Left && point.Y >= Bottom && point.X < Right && point.Y < Top;
    }

    public override string ToString()
    {
        return $"{{{Left}, {Bottom}, {Right}, {Top}}}";
    }
}