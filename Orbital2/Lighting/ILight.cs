using Microsoft.Xna.Framework;

namespace Orbital2.Lighting;

public interface ILight
{
    public Vector2 LightPosition { get; }
    public float Intensity { get; }
}