using Microsoft.Xna.Framework;
using Orbital2.Engine;

namespace Orbital2.Game.Utility;

public class CollisionMarker(Vector2 position, float radius, float lifespan) : GameObject 
{
    public Vector2 Position { get; } = position;
    public float Radius { get; } = radius;
    public float Lifespan { get; } = lifespan;

    private float time;

    public override void OnFrameUpdate(float timestep, EventContext context)
    {
        time += timestep;

        if(time > Lifespan)
        {
            context.World.RemoveObject(this);
        }
    }
}