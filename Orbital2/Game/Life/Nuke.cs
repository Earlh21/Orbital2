using Microsoft.Xna.Framework;
using MonoGame.Extended;
using Orbital2.Engine;
using Orbital2.Game.Astrobodies;
using Orbital2.Physics;

namespace Orbital2.Game.Life;

public class Nuke(Vector2 position, Vector2 velocity) : Ship(position, velocity) 
{
    public Civilization? Owner { get; set; }
    public Body? Target { get; set; }
    
    public int PopulationDamage { get; set; } = 100;

    public override void OnCollisionPassed(PhysicalGameObject other, float t, EventContext context)
    {
        if(other is Star)
        {
            context.World.RemoveObject(this);
            return;
        }

        if (other is Planet { Life: not null } planet && planet.Life.Civilization != Owner)
        {
            planet.Life.DealDamage(PopulationDamage);
            context.World.RemoveObject(this);
        }
    }

    public override void PrePhysicsUpdate(float physicsTimestep, EventContext context)
    {
        if (Target == null) return;
        if (Propulsion == null) return;

        //if ((Position - Target.Position).Length() > 40) return;

        Vector2 slowingDirection = (Velocity - Target.Velocity).NormalizedCopy();
        Vector2 targetDirection = (Target.Position - Position).NormalizedCopy();

        PropulseDirection(physicsTimestep, targetDirection);
    }
}