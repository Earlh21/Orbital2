using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Orbital2.Engine.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexPositionOccluder : IVertexType
{
    public Vector3 Position;
    public Vector2 OccluderPosition;
    public float OccluderRadius;

    public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),             // Offset 0, Size 12
        new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(20, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1)
    );

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public VertexPositionOccluder(Vector3 position, Vector2 occluderPosition, float occluderRadius)
    {
        Position = position;
        OccluderPosition = occluderPosition;
        OccluderRadius = occluderRadius;
    }
}