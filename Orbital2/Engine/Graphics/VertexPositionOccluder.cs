using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Orbital2.Engine.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexPositionOccluder : IVertexType
{
    public Vector3 Position;
    public Vector2 OccluderPosition;

    public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),             // Offset 0, Size 12
        new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.Position, 1)
    );

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public VertexPositionOccluder(Vector3 position, Vector2 occluderPosition)
    {
        Position = position;
        OccluderPosition = occluderPosition;
    }
}