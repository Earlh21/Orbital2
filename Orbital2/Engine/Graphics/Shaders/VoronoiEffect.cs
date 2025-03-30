using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Orbital2.Engine.Graphics.Shaders;

public class VoronoiEffect : CustomEffect
{
    private readonly Clock clock;
    
    public Vector2 Position
    {
        get => effect.Parameters["position"].GetValueVector2();
        set => effect.Parameters["position"].SetValue(value);
    }
    
    public float Radius
    {
        get => effect.Parameters["radius"].GetValueSingle();
        set => effect.Parameters["radius"].SetValue(value);
    }
    
    public Vector4 Color0
    {
        get => effect.Parameters["color0"].GetValueVector4();
        set => effect.Parameters["color0"].SetValue(value);
    }
    
    public Vector4 Color1
    {
        get => effect.Parameters["color1"].GetValueVector4();
        set => effect.Parameters["color1"].SetValue(value);
    }
    
    public float WarpStrength
    {
        get => effect.Parameters["warpStrength"].GetValueSingle();
        set => effect.Parameters["warpStrength"].SetValue(value);
    }
    
    public float TimeScale
    {
        get => effect.Parameters["timeScale"].GetValueSingle();
        set => effect.Parameters["timeScale"].SetValue(value);
    }
    
    public float VoronoiScale
    {
        get => effect.Parameters["voronoiScale"].GetValueSingle();
        set => effect.Parameters["voronoiScale"].SetValue(value);
    }
    
    public float VoronoiJitter
    {
        get => effect.Parameters["jitter"].GetValueSingle();
        set => effect.Parameters["jitter"].SetValue(value);
    }

    public VoronoiEffect(
        Effect voronoiEffect,
        Camera camera,
        Func<Rectangle> getBounds,
        Clock clock) : base(voronoiEffect, camera, getBounds)
    {
        this.clock = clock;
    }

    public override void Apply()
    {
        effect.Parameters["time"].SetValue(clock.CurrentTime);

        base.Apply();
    }
}