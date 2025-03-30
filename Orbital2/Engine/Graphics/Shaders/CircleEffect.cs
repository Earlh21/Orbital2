using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Orbital2.Engine.Graphics.Shaders;

public class CircleEffect : CustomEffect
{
    public CircleEffect(Effect effect, Camera camera, Func<Rectangle> getBounds) : base(effect, camera, getBounds)
    {
    }
}