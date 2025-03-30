using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Orbital2.Engine.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexPositionCircle : IVertexType
{
    public Vector3 Position;
    public Vector2 CirclePosition;
    public float CircleRadius;

    private static readonly VertexDeclaration VertexDeclaration = new (
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(20, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1)
    );

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public VertexPositionCircle(Vector3 position, Vector2 circlePosition, float circleRadius)
    {
        Position = position;
        CirclePosition = circlePosition;
        CircleRadius = circleRadius;
    }
}