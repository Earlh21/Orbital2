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

    private void DrawTrianglesHelper<T>(T[] vertices, BlendState blendMode) where T : struct, IVertexType
    {
        graphicsDevice.RasterizerState = RasterizerState.CullNone;
        graphicsDevice.BlendState = blendMode;
        
        if (vertices.Length < 3) return;
        if(vertices.Length % 3 != 0) throw new ArgumentException("Vertices length must be a multiple of 3");
        
        graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length / 3);
    }
    
    public void DrawTriangles<T>(T[] vertices, Effect effect, BlendState blendMode) where T : struct, IVertexType
    {
        effect.CurrentTechnique.Passes[0].Apply();
        DrawTrianglesHelper(vertices, blendMode);
    }
    
    public void DrawTriangles<T>(T[] vertices, IEffect effect, BlendState blendMode) where T : struct, IVertexType
    {
        effect.Apply();
        DrawTrianglesHelper(vertices, blendMode);
    }
    
    private void DrawQuadsHelper<T>(T[] vertices, BlendState blendMode) where T : struct, IVertexType
    {
        graphicsDevice.RasterizerState = RasterizerState.CullNone;
        graphicsDevice.BlendState = blendMode;
        
        if (vertices.Length < 4) return;
        if(vertices.Length % 4 != 0) throw new ArgumentException("Vertices length must be a multiple of 4");
        
        int quadCount = vertices.Length / 4;
        var indices = new short[quadCount * 6];
        
        for (int i = 0; i < quadCount; ++i)
        {
            short vOffset = (short)(i * 4);
            int indexOffset = i * 6;
            indices[indexOffset + 0] = vOffset;
            indices[indexOffset + 1] = (short)(vOffset + 1);
            indices[indexOffset + 2] = (short)(vOffset + 2);
            indices[indexOffset + 3] = (short)(vOffset + 1);
            indices[indexOffset + 4] = (short)(vOffset + 2);
            indices[indexOffset + 5] = (short)(vOffset + 3);
        }
        
        graphicsDevice.DrawUserIndexedPrimitives(
            PrimitiveType.TriangleList,
            vertices,
            0,
            vertices.Length,
            indices,
            0,
            quadCount * 2
        );
    }
    
    public void DrawQuads<T>(T[] vertices, Effect effect, BlendState blendMode) where T : struct, IVertexType
    {
        effect.CurrentTechnique.Passes[0].Apply();
        DrawQuadsHelper(vertices, blendMode);
    }
    
    public void DrawQuads<T>(T[] vertices, IEffect effect, BlendState blendMode) where T : struct, IVertexType
    {
        effect.Apply();
        DrawQuadsHelper(vertices, blendMode);
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
        
        DrawQuadsHelper(vertices, blendMode);
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