using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Orbital2.Engine.Graphics.Shaders;

public abstract class CustomEffect : IEffect
{
    protected readonly Effect effect;
    private readonly Camera camera;
    private readonly Func<Rectangle> getBounds;

    public CustomEffect(Effect effect, Camera camera, Func<Rectangle> getBounds)
    {
        this.effect = effect;
        this.camera = camera;
        this.getBounds = getBounds;
    }
    
    public virtual void Apply()
    {
        var bounds = getBounds();
        effect.Parameters["WorldViewProjection"].SetValue(camera.GetViewMatrix(bounds));
        effect.CurrentTechnique.Passes[0].Apply();
    }
}