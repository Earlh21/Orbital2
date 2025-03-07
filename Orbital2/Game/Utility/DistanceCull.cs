using Orbital2.Engine;

namespace Orbital2.Game.Utility;

public class DistanceCull(float distance) : GameObject
{
    public override void PostPhysicsUpdate(float physicsTimestep, EventContext context)
    {
        foreach (var obj in context.World.PhysicalObjects)
        {
            if (obj.Position.Length() > distance)
            {
                context.World.RemoveObject(obj);
            }
        }
    }
}