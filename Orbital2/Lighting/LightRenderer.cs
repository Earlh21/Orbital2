using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Orbital2.Lighting;

public class LightRenderer : IDisposable
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly SpriteBatch spriteBatch;
    private readonly BasicEffect basicEffect;
    private RenderTarget2D shadowMapRenderTarget;
    private bool disposed;

    // A very large number to project shadow edges far away
    private const float ShadowLength = 100000f;

    public LightRenderer(GraphicsDevice graphicsDevice)
    {
        this.graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        spriteBatch = new SpriteBatch(graphicsDevice);

        basicEffect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            TextureEnabled = false, // We are drawing solid colors
            // World, View, Projection will be set in Draw
        };

        // Initialize RenderTarget (size will be set later if needed, or on first draw)
        // For simplicity, let's create it with the current back buffer size.
        // It should ideally be resized if the screen size changes.
        var pp = graphicsDevice.PresentationParameters;
        shadowMapRenderTarget = new RenderTarget2D(
            graphicsDevice,
            pp.BackBufferWidth,
            pp.BackBufferHeight,
            false, // No mipmap
            graphicsDevice.PresentationParameters.BackBufferFormat, // Match back buffer format
            DepthFormat.None, // No depth needed for 2D shadows
            0, // No multisampling
            RenderTargetUsage.DiscardContents // Contents are redrawn each frame
        );
    }

    // Call this if the screen resolution changes to resize the render target
    public void ResizeRenderTarget(int width, int height)
    {
        shadowMapRenderTarget?.Dispose();
        shadowMapRenderTarget = new RenderTarget2D(
            graphicsDevice,
            width,
            height,
            false,
            graphicsDevice.PresentationParameters.BackBufferFormat,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents
        );
    }

    private bool ShouldSkipOccluder(Vector2 lightPos, Vector2 occluderPos, float occluderRadius)
    {
        Vector2 delta = occluderPos - lightPos;
        float distSq = delta.LengthSquared();
        float radiusSq = occluderRadius * occluderRadius;
        return distSq <= radiusSq;
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

    private void DrawScreenPolygon(
        Vector2 v1,
        Vector2 v2,
        Vector2 v3,
        Vector2 v4,
        Color color)
    {
        VertexPositionColor[] vertices = new VertexPositionColor[4];
        vertices[0] = new VertexPositionColor(new Vector3(v1, 0f), color);
        vertices[1] = new VertexPositionColor(new Vector3(v2, 0f), color);
        vertices[2] = new VertexPositionColor(new Vector3(v3, 0f), color);
        vertices[3] = new VertexPositionColor(new Vector3(v4, 0f), color);

        short[] indices = [0, 2, 1, 1, 2, 3];

        graphicsDevice.DrawUserIndexedPrimitives(
            PrimitiveType.TriangleList,
            vertices,
            0,
            4,
            indices,
            0,
            2
        );
    }

    private void DrawShadowForOccluder(
        Camera camera,
        Rectangle screenBounds,
        ILight light,
        ILightingOccluder occluder,
        float shadowLength,
        Color color)
    {
        //if (ShouldSkipOccluder(light.Position, occluder.Position, occluder.Radius))
            //return;

        (float angle1, float angle2) = ComputeTangentAngles(light.Position, occluder.Position, occluder.Radius);

        float angleCenterToT1 = angle1 + MathF.PI * 0.5f;
        float angleCenterToT2 = angle2 - MathF.PI * 0.5f;

        Vector2 t1World = ComputeTangentPoint(occluder.Position, occluder.Radius, angle1, angleCenterToT1);
        Vector2 t2World = ComputeTangentPoint(occluder.Position, occluder.Radius, angle2, angleCenterToT2);

        Vector2 p1World = ExtendShadowRay(light.Position, t1World, shadowLength);
        Vector2 p2World = ExtendShadowRay(light.Position, t2World, shadowLength);

        Vector2 t1Screen = camera.TransformToClip(t1World, screenBounds);
        Vector2 t2Screen = camera.TransformToClip(t2World, screenBounds);
        Vector2 p1Screen = camera.TransformToClip(p1World, screenBounds);
        Vector2 p2Screen = camera.TransformToClip(p2World, screenBounds);

        DrawScreenPolygon(t1Screen, t2Screen, p1Screen, p2Screen, color);
    }

    public void DrawShadows(
        ILight light,
        IEnumerable<ILightingOccluder> occluders,
        Camera camera,
        Rectangle screenBounds,
        float shadowLength = 10000.0f)
    {
        // Set up BasicEffect for rendering in screen space
        basicEffect.Projection = Matrix.Identity;
        basicEffect.View = Matrix.Identity;
        basicEffect.World = Matrix.Identity;

        // Apply the effect
        //basicEffect.CurrentTechnique.Passes[0].Apply();
        
        foreach (var occluder in occluders)
        {
            DrawShadowForOccluder(
                camera,
                screenBounds,
                light,
                occluder,
                shadowLength,
                Color.Black
            );
        }
    }


    // --- Optional: Draw the Light Source Itself ---
    // You might want a separate method or integrate this elsewhere

    // Example: Draw a simple representation of the light source
    // Requires a 1x1 white pixel texture. Create this in LoadContent.
    private Texture2D? pixelTexture;

    public void CreatePixelTexture() // Call this after GraphicsDevice is ready
    {
        pixelTexture?.Dispose();
        pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        pixelTexture.SetData(new[] { Color.White });
    }

    public void DrawLightSourceVisual(ILight light, Camera camera, Rectangle screenBounds)
    {
        if (pixelTexture == null) CreatePixelTexture(); // Ensure pixel exists

        Vector2 lightScreenPos = camera.TransformToClip(light.Position, screenBounds);
        float lightScreenRadius = 10; // Example fixed screen size for the light representation

        spriteBatch.Begin();
        spriteBatch.Draw(
            pixelTexture,
            new Rectangle(
                (int)(lightScreenPos.X - lightScreenRadius),
                (int)(lightScreenPos.Y - lightScreenRadius),
                (int)(lightScreenRadius * 2),
                (int)(lightScreenRadius * 2)
            ),
            Color.Yellow * light.Intensity // Use intensity to modulate brightness/color
        );
        spriteBatch.End();
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
                shadowMapRenderTarget?.Dispose();
                pixelTexture?.Dispose(); // Dispose the optional pixel texture
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