using Microsoft.Xna.Framework;

namespace Orbital2.Lighting;

public interface ILightingOccluder
{
    public Vector2 Position { get; }
    public float Radius { get; }
}