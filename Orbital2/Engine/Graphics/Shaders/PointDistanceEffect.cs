using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Orbital2.Engine.Graphics.Shaders;

public class PointDistanceEffect : CustomEffect
{
    public Vector2 Point
    {
        get => effect.Parameters["source"].GetValueVector2();
        set => effect.Parameters["source"].SetValue(value);
    }

    public Vector4 Color
    {
        get => effect.Parameters["sourceColor"].GetValueVector4();
        set => effect.Parameters["sourceColor"].SetValue(value);
    }
    
    public float MaxDistance
    {
        get => effect.Parameters["maxDistance"].GetValueSingle();
        set => effect.Parameters["maxDistance"].SetValue(value);
    }
    
    public PointDistanceEffect(Effect effect, Camera camera, Func<Rectangle> getbounds) : base(effect, camera , getbounds)
    {
        
    }
}