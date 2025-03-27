using Microsoft.Xna.Framework;

namespace Orbital2.Lighting;

public interface ILight
{
    public Vector2 Position { get; }
    public float Intensity { get; }
}