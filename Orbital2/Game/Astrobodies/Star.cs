using Orbital2.Engine;
using Orbital2.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orbital2.Game.Base;

namespace Orbital2.Game.Astrobodies;

public class Star : PhysicalGameObject
{
    public const float RadianceConstant = 20000f;

    public Star(Body body) : base(body)
    {
    }

    public override void PostPhysicsUpdate(float physicsTimestep, EventContext context)
    {
        foreach (var tempBody in context.World.FindObjectsByType<TemperatureObject>())
        {
            tempBody.ThermalEnergy += RadianceConstant * tempBody.Radius * Radius / (tempBody.Position - Position).Length();
        }
    }

    public override void OnCollisionPassed(PhysicalGameObject other, float t, EventContext context)
    {
        if (other is Star && other.Mass > Mass) return;

        Body.ApplyImpulse(other.Mass * other.Velocity);
        Body.Matter.Add(other.Body.Matter);
        context.World.RemoveObject(other);
    }
}