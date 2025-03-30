using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orbital2.Engine.Graphics.Shaders;

namespace Orbital2.Engine.Graphics;

public class DrawHelper
{
    private readonly GraphicsDevice graphicsDevice;
    
    public DrawHelper(GraphicsDevice graphicsDevice)
    {
        this.graphicsDevice = graphicsDevice;
    }

    private void DrawTrianglesHelper<T>(T[] vertices, BlendState blendMode, int? count) where T : struct, IVertexType
    {
        graphicsDevice.RasterizerState = RasterizerState.CullNone;
        graphicsDevice.BlendState = blendMode;
        
        if (vertices.Length < 3) return;
        if(vertices.Length % 3 != 0) throw new ArgumentException("Vertices length must be a multiple of 3");
        
        count ??= vertices.Length / 3;
        if(count > vertices.Length / 3) throw new ArgumentException("Count must be less than or equal to vertices length divided by 4");
        
        graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, count.Value);
    }
    
    public void DrawTriangles<T>(T[] vertices, Effect effect, BlendState blendMode, int? count = null) where T : struct, IVertexType
    {
        effect.CurrentTechnique.Passes[0].Apply();
        DrawTrianglesHelper(vertices, blendMode, count);
    }
    
    public void DrawTriangles<T>(T[] vertices, IEffect effect, BlendState blendMode, int? count = null) where T : struct, IVertexType
    {
        effect.Apply();
        DrawTrianglesHelper(vertices, blendMode, count);
    }

private readonly FixedList<short> indices16 = new();
private readonly FixedList<int> indices32 = new();

private void DrawQuadsHelper<T>(T[] vertices, BlendState blendMode, int? count) where T : struct, IVertexType
{
    graphicsDevice.RasterizerState = RasterizerState.CullNone;
    graphicsDevice.BlendState = blendMode;

    if (vertices.Length < 4) return;
    if (vertices.Length % 4 != 0) throw new ArgumentException("Vertices length must be a multiple of 4");

    count ??= vertices.Length / 4;
    if (count > vertices.Length / 4) throw new ArgumentException("Count must be less than or equal to vertices length divided by 4");

    int numVertices = vertices.Length;
    int numIndices = count.Value * 6;

    if (numVertices <= ushort.MaxValue)
    {
        indices16.Resize(numIndices);
        if (indices16.Used < indices16.Array.Length)
        {
            indices16.Reset();
            for (int i = 0; i < count; ++i)
            {
                short vOffset = (short)(i * 4);
                indices16.Add(vOffset);
                indices16.Add((short)(vOffset + 1));
                indices16.Add((short)(vOffset + 2));
                indices16.Add((short)(vOffset + 1));
                indices16.Add((short)(vOffset + 2));
                indices16.Add((short)(vOffset + 3));
            }
        }

        graphicsDevice.DrawUserIndexedPrimitives(
            PrimitiveType.TriangleList,
            vertices,
            0,
            vertices.Length,
            indices16.Array,
            0,
            count.Value * 2
        );
    }
    else
    {
        indices32.Resize(numIndices);
        if (indices32.Used < indices32.Array.Length)
        {
            indices32.Reset();
            for (int i = 0; i < count; ++i)
            {
                int vOffset = i * 4;
                indices32.Add(vOffset);
                indices32.Add(vOffset + 1);
                indices32.Add(vOffset + 2);
                indices32.Add(vOffset + 1);
                indices32.Add(vOffset + 2);
                indices32.Add(vOffset + 3);
            }
        }

        graphicsDevice.DrawUserIndexedPrimitives(
            PrimitiveType.TriangleList,
            vertices,
            0,
            vertices.Length,
            indices32.Array,
            0,
            count.Value * 2
        );
    }
}
    
    public void DrawQuads<T>(T[] vertices, Effect effect, BlendState blendMode, int? count = null) where T : struct, IVertexType
    {
        effect.CurrentTechnique.Passes[0].Apply();
        DrawQuadsHelper(vertices, blendMode, count);
    }
    
    public void DrawQuads<T>(T[] vertices, IEffect effect, BlendState blendMode, int? count = null) where T : struct, IVertexType
    {
        effect.Apply();
        DrawQuadsHelper(vertices, blendMode, count);
    }
    
    private void DrawScreenHelper(Camera camera, BlendState blendMode)
    {
        graphicsDevice.RasterizerState = RasterizerState.CullNone;
        graphicsDevice.BlendState = blendMode;
        
        Point topLeftScreen = new(graphicsDevice.Viewport.Bounds.Left, graphicsDevice.Viewport.Bounds.Top);
        Point topRightScreen = new(graphicsDevice.Viewport.Bounds.Right, graphicsDevice.Viewport.Bounds.Top);
        Point bottomLeftScreen = new(graphicsDevice.Viewport.Bounds.Left, graphicsDevice.Viewport.Bounds.Bottom);
        Point bottomRightScreen = new(graphicsDevice.Viewport.Bounds.Right, graphicsDevice.Viewport.Bounds.Bottom);

        var topLeftWorld = camera.TransformToWorld(topLeftScreen, graphicsDevice.Viewport.Bounds);
        var topRightWorld = camera.TransformToWorld(topRightScreen, graphicsDevice.Viewport.Bounds);
        var bottomLeftWorld = camera.TransformToWorld(bottomLeftScreen, graphicsDevice.Viewport.Bounds);
        var bottomRightWorld = camera.TransformToWorld(bottomRightScreen, graphicsDevice.Viewport.Bounds);
        
        VertexPosition[] vertices =
        [
            new(new(topLeftWorld, 0)),
            new(new(topRightWorld, 0)),
            new(new(bottomLeftWorld, 0)),
            new(new(bottomRightWorld, 0))
        ];
        
        DrawQuadsHelper(vertices, blendMode, null);
    }

    public void DrawScreen(Camera camera, Effect effect, BlendState blendMode)
    {
        effect.CurrentTechnique.Passes[0].Apply();
        DrawScreenHelper(camera, blendMode);
    }

    public void DrawScreen(Camera camera, IEffect effect, BlendState blendMode)
    {
        effect.Apply();
        DrawScreenHelper(camera, blendMode);
    }
}