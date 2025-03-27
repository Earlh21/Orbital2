using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TorchSharp.Modules;

namespace Orbital2.Lighting;

public class LightRenderer : IDisposable
{
    public float ShadowLength { get; set; } = 1000f;

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

    private Vector2 ExtendShadowRay(Vector2 rayStart, Vector2 rayEnd, float shadowLength)
    {
        Vector2 dir = rayEnd - rayStart;

        if (dir.LengthSquared() < float.Epsilon * float.Epsilon)
        {
            return rayEnd;
        }

        dir.Normalize();
        return rayStart + dir * shadowLength;
    }

    private void DrawTriangles(VertexPositionColor[] vertices)
    {
        if (vertices.Length == 0) return;

        basicEffect.CurrentTechnique.Passes[0].Apply();

        graphicsDevice.RasterizerState = RasterizerState.CullNone;

        if (vertices.Length % 3 != 0)
            throw new ArgumentException("Vertices array must have a multiple of 3 elements", nameof(vertices));

        int triangleCount = vertices.Length / 3;
        if (triangleCount > 0)
        {
            graphicsDevice.DrawUserPrimitives(
                PrimitiveType.TriangleList,
                vertices,
                0,
                triangleCount
            );
        }
    }

    private void DrawQuads(VertexPositionColor[] vertices)
    {
        if (vertices == null || vertices.Length == 0) return;

        basicEffect.CurrentTechnique.Passes[0].Apply();

        graphicsDevice.RasterizerState = RasterizerState.CullNone;

        if (vertices.Length % 4 != 0)
            throw new ArgumentException("Vertices array must have a multiple of 4 elements", nameof(vertices));

        int quadCount = vertices.Length / 4;
        if (quadCount == 0) return;

        var indices = new short[quadCount * 6];
        for (int i = 0; i < quadCount; ++i)
        {
            short vOffset = (short)(i * 4);
            int indexOffset = i * 6;
            indices[indexOffset + 0] = vOffset;
            indices[indexOffset + 1] = (short)(vOffset + 1);
            indices[indexOffset + 2] = (short)(vOffset + 2);
            indices[indexOffset + 3] = (short)(vOffset + 1);
            indices[indexOffset + 4] = (short)(vOffset + 3);
            indices[indexOffset + 5] = (short)(vOffset + 2);
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

        List<VertexPositionColor> triangleVertices = [];
        List<VertexPositionColor> quadVertices = [];

        foreach (var occluder in occluders)
        {
            AddShadowVertices(sceneInfo, occluder, triangleVertices, quadVertices);
        }

        if (triangleVertices.Count > 0)
        {
            DrawTriangles(triangleVertices.ToArray());
        }

        if (quadVertices.Count > 0)
        {
            DrawQuads(quadVertices.ToArray());
        }
    }

    public Vector2? GetIntersection(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
    {
        var r = p2 - p1;
        var s = q2 - q1;
        var rxs = r.X * s.Y - r.Y * s.X;
        var qp = q1 - p1;
        var qpxr = qp.X * r.Y - qp.Y * r.X;

        const float Epsilon = 1e-5f;

        if (Math.Abs(rxs) < Epsilon)
        {
            return null;
        }

        var t = (qp.X * s.Y - qp.Y * s.X) / rxs;
        var u = (qp.X * r.Y - qp.Y * r.X) / rxs;

        if (t >= -Epsilon && t <= 1.0f + Epsilon && u >= -Epsilon && u <= 1.0f + Epsilon)
        {
            return p1 + t * r;
        }

        return null;
    }

    private void AddShadowVertices(
        SceneInfo scene,
        ILightingOccluder occluder,
        List<VertexPositionColor> triangles,
        List<VertexPositionColor> quads)
    {
        Vector2 exTanLeftLi;
        Vector2 exTanRightLi;
        Vector2 exTanLeftOc;
        Vector2 exTanRightOc;

        Vector2 inTanLeftLi;
        Vector2 inTanRightLi;
        Vector2 inTanLeftOc;
        Vector2 inTanRightOc;
        
        

        // Extend rays *from the light tangent points through the occluder tangent points*
        Vector2 exTanLeftEnd = ExtendShadowRay(exTanLeftLi, exTanLeftOc, ShadowLength);
        Vector2 exTanRightEnd = ExtendShadowRay(exTanRightLi, exTanRightOc, ShadowLength);

        // Find intersection of the umbra lines
        Vector2 umbraIntersect =
            GetIntersection(exTanLeftLi, exTanLeftEnd, exTanRightLi, exTanRightEnd)!.Value;

        Vector2 dVec = occluder.LightPosition - scene.Light.LightPosition;
        float d = dVec.Length();
        float radiusSum = scene.Light.Radius + occluder.Radius;

        float phi = MathF.Atan2(dVec.Y, dVec.X);
        float delta = MathF.Acos(radiusSum / d);

        Vector2 inTanLeftLi = scene.Light.LightPosition +
                              new Vector2(MathF.Cos(phi + delta), MathF.Sin(phi + delta)) * scene.Light.Radius;
        Vector2 inTanRightLi = scene.Light.LightPosition +
                               new Vector2(MathF.Cos(phi - delta), MathF.Sin(phi - delta)) * scene.Light.Radius;
        Vector2 inTanRightOc = occluder.LightPosition +
                               new Vector2(MathF.Cos(phi + delta + MathF.PI), MathF.Sin(phi + delta + MathF.PI)) *
                               occluder.Radius;
        Vector2 inTanLeftOc = occluder.LightPosition +
                              new Vector2(MathF.Cos(phi - delta + MathF.PI), MathF.Sin(phi - delta + MathF.PI)) *
                              occluder.Radius;

        Vector2 inTanLeftEnd = ExtendShadowRay(inTanLeftLi, inTanRightOc, ShadowLength);
        Vector2 inTanRightEnd = ExtendShadowRay(inTanRightLi, inTanLeftOc, ShadowLength);

        Vector2 rightSplitterEnd = ExtendShadowRay(inTanLeftLi, exTanRightOc, ShadowLength);
        Vector2 leftSplitterEnd = ExtendShadowRay(inTanRightLi, exTanLeftOc, ShadowLength);

        // Test external tangents
        triangles.Add(ConvertVector(exTanLeftOc, scene));
        triangles.Add(ConvertVector(exTanRightLi, scene));
        triangles.Add(ConvertVector(scene.Light.LightPosition, scene));
    }

    private VertexPositionColor ConvertVector(Vector2 v, SceneInfo scene, Color color = default)
    {
        if (color == default) color = Color.Black;

        return new VertexPositionColor(
            new Vector3(scene.Camera.TransformToClip(v, scene.ScreenBounds), 0),
            color
        );
    }

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
                spriteBatch?.Dispose();
                basicEffect?.Dispose();
            }

            disposed = true;
        }
    }

    ~LightRenderer()
    {
        Dispose(false);
    }
}