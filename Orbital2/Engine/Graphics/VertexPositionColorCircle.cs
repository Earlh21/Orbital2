using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Orbital2.Engine.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexPositionCircleColor : IVertexType
{
    public Vector3 Position;
    public Vector4 Color;
    public Vector2 CirclePosition;
    public float CircleRadius;

    private static readonly VertexDeclaration VertexDeclaration = new (
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
        new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(36, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1)
    );

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public VertexPositionCircleColor(Vector3 position, Vector4 color, Vector2 circlePosition, float circleRadius)
    {
        Position = position;
        Color = color;
        CirclePosition = circlePosition;
        CircleRadius = circleRadius;
    }
}