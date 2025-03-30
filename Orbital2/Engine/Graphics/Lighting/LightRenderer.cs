using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

    private record struct SceneInfo(ILight Light, Camera Camera, Rectangle ScreenBounds);

    public void DrawLight(ILight light,
        Camera camera)
    {
        pointDistanceEffect.Point = light.LightPosition;
        pointDistanceEffect.MaxDistance = light.LightIntensity;
        pointDistanceEffect.Color = light.LightColor.ToVector4();
        
        drawHelper.DrawScreen(camera, pointDistanceEffect, BlendState.Additive);
    }
    
    public void DrawShadows(
        ILight light,
        IEnumerable<ILightingOccluder> occluders,
        Camera camera)
    {
        basicEffect.View = camera.GetViewMatrix(graphicsDevice.Viewport.Bounds);
        
        var sceneInfo = new SceneInfo(light, camera, graphicsDevice.Viewport.Bounds);
        
        foreach (var occluder in occluders)
        {
            DrawShadow(sceneInfo, occluder);
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

    private void DrawShadow(
        SceneInfo scene,
        ILightingOccluder occluder)
    {
        Vector2 exTanLeftLi;
        Vector2 exTanRightLi;
        Vector2 exTanLeftOc;
        Vector2 exTanRightOc;

        Vector2 inTanLeftLi;
        Vector2 inTanRightLi;
        Vector2 inTanLeftOc;
        Vector2 inTanRightOc;

        Vector2 lightPos = scene.Light.LightPosition;
        Vector2 occPos = occluder.LightPosition;
        Vector2 dVec = occPos - lightPos;
        float d = dVec.Length();
        if (d < float.Epsilon) return;
        float theta = (float)Math.Atan2(dVec.Y, dVec.X);
        float r1 = scene.Light.LightRadius;
        float r2 = occluder.Radius;

        Func<Vector2, Vector2> TransformLocal = local =>
        {
            float cos = (float)Math.Cos(theta);
            float sin = (float)Math.Sin(theta);
            return new Vector2(local.X * cos - local.Y * sin, local.X * sin + local.Y * cos) + lightPos;
        };

// External tangents (K = r1 - r2)
        float K_ext = r1 - r2;
        float sqrt_ext = (float)Math.Sqrt(d * d - K_ext * K_ext);
        Vector2 local_exTanLeftLi = new((r1 * K_ext) / d, (r1 * sqrt_ext) / d);
        Vector2 local_exTanRightLi = new((r1 * K_ext) / d, -(r1 * sqrt_ext) / d);
        Vector2 local_exTanLeftOc = new(d + (r2 * K_ext) / d, (r2 * sqrt_ext) / d);
        Vector2 local_exTanRightOc = new(d + (r2 * K_ext) / d, -(r2 * sqrt_ext) / d);

// Internal tangents (K = r1 + r2)
        float K_int = r1 + r2;
        float sqrt_int = (float)Math.Sqrt(d * d - K_int * K_int);
        Vector2 local_inTanLeftLi = new((r1 * K_int) / d, (r1 * sqrt_int) / d);
        Vector2 local_inTanRightLi = new((r1 * K_int) / d, -(r1 * sqrt_int) / d);
        Vector2 local_inTanRightOc = new(d - (r2 * K_int) / d, -(r2 * sqrt_int) / d);
        Vector2 local_inTanLeftOc = new(d - (r2 * K_int) / d, (r2 * sqrt_int) / d);

        exTanLeftLi = TransformLocal(local_exTanLeftLi);
        exTanRightLi = TransformLocal(local_exTanRightLi);
        exTanLeftOc = TransformLocal(local_exTanLeftOc);
        exTanRightOc = TransformLocal(local_exTanRightOc);
        inTanLeftLi = TransformLocal(local_inTanLeftLi);
        inTanRightLi = TransformLocal(local_inTanRightLi);
        inTanLeftOc = TransformLocal(local_inTanLeftOc);
        inTanRightOc = TransformLocal(local_inTanRightOc);

        Vector2 exTanLeftEnd = ExtendShadowRay(exTanLeftLi, exTanLeftOc, ShadowLength);
        Vector2 exTanRightEnd = ExtendShadowRay(exTanRightLi, exTanRightOc, ShadowLength);
        
        Vector2 inTanLeftEnd = ExtendShadowRay(inTanRightLi, inTanLeftOc, ShadowLength);
        Vector2 inTanRightEnd = ExtendShadowRay(inTanLeftLi, inTanRightOc, ShadowLength);

        Vector2? umbraIntersect = GetIntersection(exTanLeftLi, exTanLeftEnd, exTanRightLi, exTanRightEnd);
        if (umbraIntersect == null) return;

        Vector2 rightSplitterEnd = ExtendShadowRay(inTanLeftLi, exTanRightOc, ShadowLength);
        Vector2 leftSplitterEnd = ExtendShadowRay(inTanRightLi, exTanLeftOc, ShadowLength);

        List<VertexPosition> quads = [];
        List<VertexPositionColor> hardTriangles = [];
        List<VertexPosition> softTriangles = [];
        
        quads.Add(ConvertVector(exTanLeftOc));
        quads.Add(ConvertVector(umbraIntersect.Value));
        quads.Add(ConvertVector(leftSplitterEnd));
        quads.Add(ConvertVector(exTanRightEnd));

        quads.Add(ConvertVector(umbraIntersect.Value));
        quads.Add(ConvertVector(exTanRightOc));
        quads.Add(ConvertVector(exTanLeftEnd));
        quads.Add(ConvertVector(rightSplitterEnd));

        hardTriangles.Add(ConvertVector(exTanRightOc, Color.Black));
        hardTriangles.Add(ConvertVector(exTanLeftOc, Color.Black));
        hardTriangles.Add(ConvertVector(umbraIntersect.Value, Color.Black));

        softTriangles.Add(ConvertVector(umbraIntersect.Value));
        softTriangles.Add(ConvertVector(exTanLeftEnd));
        softTriangles.Add(ConvertVector(exTanRightEnd));

        quads.Add(ConvertVector(inTanLeftOc));
        quads.Add(ConvertVector(exTanLeftOc));
        quads.Add(ConvertVector(inTanLeftEnd));
        quads.Add(ConvertVector(leftSplitterEnd));

        quads.Add(ConvertVector(exTanRightOc));
        quads.Add(ConvertVector(inTanRightOc));
        quads.Add(ConvertVector(rightSplitterEnd));
        quads.Add(ConvertVector(inTanRightEnd));
        
        drawHelper.DrawTriangles(hardTriangles.ToArray(), basicEffect, BlendState.AlphaBlend);
        
        occlusionShadowEffect.Parameters["WorldViewProjection"].SetValue(scene.Camera.GetViewMatrix(scene.ScreenBounds));
        occlusionShadowEffect.Parameters["lightPosition"].SetValue(scene.Light.LightPosition);
        occlusionShadowEffect.Parameters["lightRadius"].SetValue(scene.Light.LightRadius);
        occlusionShadowEffect.Parameters["occluderPosition"].SetValue(occluder.LightPosition);
        occlusionShadowEffect.Parameters["occluderRadius"].SetValue(occluder.Radius);
        
        drawHelper.DrawTriangles(softTriangles.ToArray(), occlusionShadowEffect, BlendState.AlphaBlend);
        drawHelper.DrawQuads(quads.ToArray(), occlusionShadowEffect, BlendState.AlphaBlend);
    }

    private VertexPosition ConvertVector(Vector2 v)
    {
        return new(new(v, 0));
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