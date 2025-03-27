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
    
    private void DrawTriangles(VertexPositionColor[] vertices)
    {
        basicEffect.CurrentTechnique.Passes[0].Apply();

        graphicsDevice.RasterizerState = new()
        {
            CullMode = CullMode.None
        };

        if (vertices.Length % 3 != 0)
            throw new ArgumentException("Vertices array must have a multiple of 3 elements", nameof(vertices));

        graphicsDevice.DrawUserPrimitives(
            PrimitiveType.TriangleList,
            vertices,
            0,
            vertices.Length / 3
        );
    }

    private void DrawQuads(VertexPositionColor[] vertices)
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

        VertexPositionColor[] hardVertices = new VertexPositionColor[occluders.Count() * 4];
        VertexPositionColor[] softVertices = new VertexPositionColor[occluders.Count() * 6];
        int hardVertexOffset = 0;
        int softVertexOffset = 0;

        foreach (var occluder in occluders)
        {
            AddShadowVertices(sceneInfo, occluder, hardVertices, ref hardVertexOffset, softVertices, ref softVertexOffset);
        }

        if (hardVertexOffset == 0) return;

        DrawQuads(hardVertices);
        DrawTriangles(softVertices);
    }

    private void AddShadowVertices(
        SceneInfo scene,
        ILightingOccluder occluder,
        VertexPositionColor[] hardVertices,
        ref int hardVertexOffset,
        VertexPositionColor[] softVertices,
        ref int softVertexOffset)
    {
        (float angle1, float angle2) =
            ComputeTangentAngles(scene.Light.LightPosition, occluder.LightPosition, occluder.Radius);

        Vector2 tanget1World = ComputeTangentPoint(occluder.LightPosition, occluder.Radius, angle1, 0);
        Vector2 tangent2World = ComputeTangentPoint(occluder.LightPosition, occluder.Radius, angle2, 0);

        Vector2 extend1World = ExtendShadowRay(scene.Light.LightPosition, tanget1World, ShadowLength);
        Vector2 extend2World = ExtendShadowRay(scene.Light.LightPosition, tangent2World, ShadowLength);

        Vector2 tangent1Screen = scene.Camera.TransformToClip(tanget1World, scene.ScreenBounds);
        Vector2 tangent2Screen = scene.Camera.TransformToClip(tangent2World, scene.ScreenBounds);
        Vector2 extend1Screen = scene.Camera.TransformToClip(extend1World, scene.ScreenBounds);
        Vector2 extend2Screen = scene.Camera.TransformToClip(extend2World, scene.ScreenBounds);

        // Add vertices for this shadow quad
        hardVertices[hardVertexOffset] = new(new(tangent1Screen, 0f), Color.Black);
        hardVertices[hardVertexOffset + 1] = new(new(tangent2Screen, 0f), Color.Black);
        hardVertices[hardVertexOffset + 2] = new(new(extend1Screen, 0f), Color.Black);
        hardVertices[hardVertexOffset + 3] = new(new(extend2Screen, 0f), Color.Black);

        hardVertexOffset += 4;
        
        Vector2 sourceTangent1World = ComputeTangentPoint(scene.Light.LightPosition, scene.Light.Radius, angle1, 0);
        Vector2 sourceTangent2World = ComputeTangentPoint(scene.Light.LightPosition, scene.Light.Radius, angle2, 0);
        
        Vector2 softExtend1World = ExtendShadowRay(sourceTangent2World, tanget1World, ShadowLength);
        Vector2 softExtend2World = ExtendShadowRay(sourceTangent1World, tangent2World, ShadowLength);
        
        Vector2 softExtend1Screen = scene.Camera.TransformToClip(softExtend1World, scene.ScreenBounds);
        Vector2 softExtend2Screen = scene.Camera.TransformToClip(softExtend2World, scene.ScreenBounds);
        
        softVertices[softVertexOffset] = new(new(softExtend1Screen, 0f), Color.Transparent);
        softVertices[softVertexOffset + 1] = new(new(extend1Screen, 0f), Color.Black);
        softVertices[softVertexOffset + 2] = new(new(tangent1Screen, 0f), Color.Black);
        
        softVertices[softVertexOffset + 3] = new(new(softExtend2Screen, 0f), Color.Transparent);
        softVertices[softVertexOffset + 4] = new(new(extend2Screen, 0f), Color.Black);
        softVertices[softVertexOffset + 5] = new(new(tangent2Screen, 0f), Color.Black);
        
        softVertexOffset += 6;
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