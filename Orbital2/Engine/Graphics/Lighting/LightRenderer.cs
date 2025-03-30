using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orbital2.Engine;
using Orbital2.Engine.Graphics;
using Orbital2.Engine.Graphics.Shaders;
using TorchSharp.Modules;

namespace Orbital2.Lighting;

public class LightRenderer : IDisposable
{
    public float ShadowLength { get; set; } = 1000000f;

    private readonly GraphicsDevice graphicsDevice;
    private readonly DrawHelper drawHelper;
    private readonly SpriteBatch spriteBatch;
    private readonly BasicEffect basicEffect;
    private readonly PointDistanceEffect pointDistanceEffect;
    private readonly Effect occlusionShadowEffect;
    private bool disposed;

    public LightRenderer(GraphicsDevice graphicsDevice, PointDistanceEffect pointDistanceEffect, Effect occlusionShadowEffect)
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

        this.pointDistanceEffect = pointDistanceEffect;
        this.occlusionShadowEffect = occlusionShadowEffect;

        drawHelper = new(this.graphicsDevice);
    }

    public void DrawLight(ILight light, Camera camera)
    {
        pointDistanceEffect.Point = light.LightPosition;
        pointDistanceEffect.MaxDistance = light.LightIntensity;
        pointDistanceEffect.Color = light.LightColor.ToVector4();
        
        drawHelper.DrawScreen(camera, pointDistanceEffect, BlendState.Additive);
    }
    
    private readonly FixedList<VertexPositionColor> hardTriangles = new();
    private readonly FixedList<VertexPositionOccluder> softTriangles = new();
    private readonly FixedList<VertexPositionOccluder> quads = new();
    
    public void DrawShadows(
        ILight light,
        IEnumerable<ILightingOccluder> occluders,
        Camera camera)
    {
        basicEffect.View = camera.GetViewMatrix(graphicsDevice.Viewport.Bounds);
        
        hardTriangles.Resize(occluders.Count() * 3);
        hardTriangles.Reset();
        
        softTriangles.Resize(occluders.Count() * 3);
        softTriangles.Reset();
        
        quads.Resize(occluders.Count() * 4 * 4);
        quads.Reset();
        
        foreach (var occluder in occluders)
        {
            AddShadowVertices(light, occluder, hardTriangles, softTriangles, quads);
        }
        
        drawHelper.DrawTriangles(hardTriangles.Array, basicEffect, BlendState.AlphaBlend, occluders.Count());
        
        occlusionShadowEffect.Parameters["WorldViewProjection"].SetValue(camera.GetViewMatrix(graphicsDevice.Viewport.Bounds));
        occlusionShadowEffect.Parameters["lightPosition"].SetValue(light.LightPosition);
        occlusionShadowEffect.Parameters["lightRadius"].SetValue(light.LightRadius);
        
        drawHelper.DrawTriangles(softTriangles.Array, occlusionShadowEffect, BlendState.AlphaBlend, occluders.Count());
        drawHelper.DrawQuads(quads.Array, occlusionShadowEffect, BlendState.AlphaBlend, occluders.Count() * 4);
    }
    
    private void AddShadowVertices(
        ILight light,
        ILightingOccluder occluder,
        FixedList<VertexPositionColor> hardTriangles,
        FixedList<VertexPositionOccluder> softTriangles,
        FixedList<VertexPositionOccluder> quads)
    {
        Vector2 lightPos = light.LightPosition;
        Vector2 occPos = occluder.LightPosition;
        Vector2 dVec = occPos - lightPos;
        float d = dVec.Length();
        if (d < float.Epsilon) return;
        float theta = (float)Math.Atan2(dVec.Y, dVec.X);
        float r1 = light.LightRadius;
        float r2 = occluder.Radius;

        Vector2 TransformLocal(Vector2 local)
        {
            float cos = (float)Math.Cos(theta);
            float sin = (float)Math.Sin(theta);
            return new Vector2(local.X * cos - local.Y * sin, local.X * sin + local.Y * cos) + lightPos;
        }
        
        void AddSoftTriangle(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            softTriangles.Add(ConvertVector(p1, occluder.LightPosition, occluder.Radius));
            softTriangles.Add(ConvertVector(p2, occluder.LightPosition, occluder.Radius));
            softTriangles.Add(ConvertVector(p3, occluder.LightPosition, occluder.Radius));
        }
        
        void AddHardTriangle(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            hardTriangles.Add(ConvertVector(p1, Color.Black));
            hardTriangles.Add(ConvertVector(p2, Color.Black));
            hardTriangles.Add(ConvertVector(p3, Color.Black));
        }
        
        void AddQuad(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            quads.Add(ConvertVector(p1, occluder.LightPosition, occluder.Radius));
            quads.Add(ConvertVector(p2, occluder.LightPosition, occluder.Radius));
            quads.Add(ConvertVector(p3, occluder.LightPosition, occluder.Radius));
            quads.Add(ConvertVector(p4, occluder.LightPosition, occluder.Radius));
        }

        float kExt = r1 - r2;
        float sqrtExt = (float)Math.Sqrt(d * d - kExt * kExt);
        Vector2 localExTanLeftLi = new((r1 * kExt) / d, (r1 * sqrtExt) / d);
        Vector2 localExTanRightLi = new((r1 * kExt) / d, -(r1 * sqrtExt) / d);
        Vector2 localExTanLeftOc = new(d + (r2 * kExt) / d, (r2 * sqrtExt) / d);
        Vector2 localExTanRightOc = new(d + (r2 * kExt) / d, -(r2 * sqrtExt) / d);

        float kInt = r1 + r2;
        float sqrtInt = (float)Math.Sqrt(d * d - kInt * kInt);
        Vector2 localInTanLeftLi = new((r1 * kInt) / d, (r1 * sqrtInt) / d);
        Vector2 localInTanRightLi = new((r1 * kInt) / d, -(r1 * sqrtInt) / d);
        Vector2 localInTanRightOc = new(d - (r2 * kInt) / d, -(r2 * sqrtInt) / d);
        Vector2 localInTanLeftOc = new(d - (r2 * kInt) / d, (r2 * sqrtInt) / d);

        var exTanLeftLi = TransformLocal(localExTanLeftLi);
        var exTanRightLi = TransformLocal(localExTanRightLi);
        var exTanLeftOc = TransformLocal(localExTanLeftOc);
        var exTanRightOc = TransformLocal(localExTanRightOc);
        var inTanLeftLi = TransformLocal(localInTanLeftLi);
        var inTanRightLi = TransformLocal(localInTanRightLi);
        var inTanLeftOc = TransformLocal(localInTanLeftOc);
        var inTanRightOc = TransformLocal(localInTanRightOc);

        Vector2 exTanLeftEnd = ExtendShadowRay(exTanLeftLi, exTanLeftOc, ShadowLength);
        Vector2 exTanRightEnd = ExtendShadowRay(exTanRightLi, exTanRightOc, ShadowLength);
        
        Vector2 inTanLeftEnd = ExtendShadowRay(inTanRightLi, inTanLeftOc, ShadowLength);
        Vector2 inTanRightEnd = ExtendShadowRay(inTanLeftLi, inTanRightOc, ShadowLength);

        Vector2? umbraIntersect = GetIntersection(exTanLeftLi, exTanLeftEnd, exTanRightLi, exTanRightEnd);
        if (umbraIntersect == null) return;

        Vector2 rightSplitterEnd = ExtendShadowRay(inTanLeftLi, exTanRightOc, ShadowLength);
        Vector2 leftSplitterEnd = ExtendShadowRay(inTanRightLi, exTanLeftOc, ShadowLength);
        
        AddQuad(exTanLeftOc, umbraIntersect.Value, leftSplitterEnd, exTanRightEnd);
        AddQuad(umbraIntersect.Value, exTanRightOc, exTanLeftEnd, rightSplitterEnd);
        AddHardTriangle(exTanRightOc, exTanLeftOc, umbraIntersect.Value);
        AddSoftTriangle(umbraIntersect.Value, exTanLeftEnd, exTanRightEnd);
        AddQuad(inTanLeftOc, exTanLeftOc, inTanLeftEnd, leftSplitterEnd);
        AddQuad(exTanRightOc, inTanRightOc, rightSplitterEnd, inTanRightEnd);
    }

    private static Vector2 ExtendShadowRay(Vector2 rayStart, Vector2 rayEnd, float shadowLength)
    {
        Vector2 dir = rayEnd - rayStart;

        if (dir.LengthSquared() < float.Epsilon * float.Epsilon)
        {
            return rayEnd;
        }

        dir.Normalize();
        return rayStart + dir * shadowLength;
    }
    
    private static Vector2? GetIntersection(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
    {
        var r = p2 - p1;
        var s = q2 - q1;
        var rxs = r.X * s.Y - r.Y * s.X;
        var qp = q1 - p1;

        const float epsilon = 1e-5f;

        if (Math.Abs(rxs) < epsilon)
        {
            return null;
        }

        var t = (qp.X * s.Y - qp.Y * s.X) / rxs;
        var u = (qp.X * r.Y - qp.Y * r.X) / rxs;

        if (
            t is >= -epsilon and <= 1.0f + epsilon &&
            u is >= -epsilon and <= 1.0f + epsilon)
        {
            return p1 + t * r;
        }

        return null;
    }

    private VertexPositionOccluder ConvertVector(Vector2 v, Vector2 occluderPosition, float occluderRadius)
    {
        return new(new(v, 0), occluderPosition, occluderRadius);
    }
    
    private VertexPositionColor ConvertVector(Vector2 v, Color color)
    {
        return new(new(v, 0), color);
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
                spriteBatch.Dispose();
                basicEffect.Dispose();
            }

            disposed = true;
        }
    }

    ~LightRenderer()
    {
        Dispose(false);
    }
}