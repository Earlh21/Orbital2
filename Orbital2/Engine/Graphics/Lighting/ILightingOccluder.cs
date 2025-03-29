using Microsoft.Xna.Framework;

namespace Orbital2.Lighting;

public interface ILightingOccluder
{
    public Vector2 LightPosition { get; }
    public float Radius { get; }
}