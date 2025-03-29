using Microsoft.Xna.Framework;

namespace Orbital2.Lighting;

public interface ILight
{
    public Vector2 LightPosition { get; }
    public float LightIntensity { get; }
    public float LightRadius { get; }
    public Color Lightcolor { get; }
}