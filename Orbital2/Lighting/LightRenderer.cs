using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TorchSharp.Modules;

namespace Orbital2.Lighting;

public class LightRenderer : IDisposable
{
    public float ShadowLength { get; set; } = 100000f;

    private readonly GraphicsDevice graphicsDevice;
    private readonly SpriteBatch spriteBatch;
    private readonly BasicEffect basicEffect;
    private bool disposed;

    public LightRenderer(GraphicsDevice graphicsDevice)
    {
        this.graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        spriteBatch = new(graphicsDevice);

        basicEffect = new(graphicsDevice)
        {
            VertexColorEnabled = true,
            TextureEnabled = false,
            World = Matrix.Identity,
            View = Matrix.Identity,
            Projection = Matrix.Identity
        };
    }

    private (float angle1, float angle2) ComputeTangentAngles(Vector2 lightPos, Vector2 occluderPos,
        float occluderRadius)
    {
        Vector2 toOccluder = occluderPos - lightPos;
        float dist = toOccluder.Length();
        float offset = MathF.Acos(occluderRadius / dist);
        float centerAngle = MathF.Atan2(toOccluder.Y, toOccluder.X);
        float angle1 = centerAngle - offset;
        float angle2 = centerAngle + offset;
        return (angle1, angle2);
    }

    private Vector2 ComputeTangentPoint(Vector2 occluderPos, float occluderRadius, float referenceAngle,
        float rotationOffset)
    {
        float angle = referenceAngle + rotationOffset;
        return occluderPos + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * occluderRadius;
    }

    private Vector2 ExtendShadowRay(Vector2 lightPos, Vector2 tangentPoint, float shadowLength)
    {
        Vector2 dir = tangentPoint - lightPos;
        dir.Normalize();
        return lightPos + dir * shadowLength;
    }

    private void DrawQuads(VertexPositionColor[] vertices, Color color)
    {
        basicEffect.CurrentTechnique.Passes[0].Apply();
        
        graphicsDevice.RasterizerState = new()
        {
            CullMode = CullMode.None
        };

        if (vertices.Length % 4 != 0)
            throw new ArgumentException("Vertices array must have a multiple of 4 elements", nameof(vertices));

        var indices = new short[vertices.Length / 4 * 6];

        for (int i = 0; i < indices.Length; i += 6)
        {
            short vOffset = (short)(i / 6 * 4);
            indices[i] = vOffset;
            indices[i + 1] = (short)(vOffset + 1);
            indices[i + 2] = (short)(vOffset + 2);
            indices[i + 3] = (short)(vOffset + 1);
            indices[i + 4] = (short)(vOffset + 2);
            indices[i + 5] = (short)(vOffset + 3);
        }

        graphicsDevice.DrawUserIndexedPrimitives(
            PrimitiveType.TriangleList,
            vertices,
            0,
            vertices.Length,
            indices,
            0,
            vertices.Length / 2
        );
    }

    private record struct SceneInfo(
        ILight Light,
        Camera Camera,
        Rectangle ScreenBounds);

    public void DrawShadows(
        ILight light,
        IEnumerable<ILightingOccluder> occluders,
        Camera camera,
        Rectangle screenBounds)
    {
        var sceneInfo = new SceneInfo(light, camera, screenBounds);

        VertexPositionColor[] vertices = new VertexPositionColor[occluders.Count() * 4];
        int vertexOffset = 0;

        foreach (var occluder in occluders)
        {
            AddShadowVertices(sceneInfo, occluder, vertices, ref vertexOffset);
        }

        if (vertexOffset == 0) return;

        DrawQuads(vertices, Color.Black);
    }

    private void AddShadowVertices(
        SceneInfo scene,
        ILightingOccluder occluder,
        VertexPositionColor[] vertices,
        ref int vertexOffset)
    {
        (float angle1, float angle2) = ComputeTangentAngles(scene.Light.LightPosition, occluder.LightPosition, occluder.Radius);

        Vector2 t1World = ComputeTangentPoint(occluder.LightPosition, occluder.Radius, angle1, 0);
        Vector2 t2World = ComputeTangentPoint(occluder.LightPosition, occluder.Radius, angle2, 0);

        Vector2 p1World = ExtendShadowRay(scene.Light.LightPosition, t1World, ShadowLength);
        Vector2 p2World = ExtendShadowRay(scene.Light.LightPosition, t2World, ShadowLength);

        Vector2 t1Screen = scene.Camera.TransformToClip(t1World, scene.ScreenBounds);
        Vector2 t2Screen = scene.Camera.TransformToClip(t2World, scene.ScreenBounds);
        Vector2 p1Screen = scene.Camera.TransformToClip(p1World, scene.ScreenBounds);
        Vector2 p2Screen = scene.Camera.TransformToClip(p2World, scene.ScreenBounds);

        // Add vertices for this shadow quad
        vertices[vertexOffset] = new(new(t1Screen, 0f), Color.Black);
        vertices[vertexOffset + 1] = new(new(t2Screen, 0f), Color.Black);
        vertices[vertexOffset + 2] = new(new(p1Screen, 0f), Color.Black);
        vertices[vertexOffset + 3] = new(new(p2Screen, 0f), Color.Black);

        vertexOffset += 4;
    }

    // --- IDisposable Implementation ---

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                spriteBatch?.Dispose();
                basicEffect?.Dispose();
            }
            // Dispose unmanaged resources here if any

            disposed = true;
        }
    }

    ~LightRenderer()
    {
        Dispose(false);
    }
}