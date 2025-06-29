using Orbital2.Engine;
using Orbital2.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Orbital2.Game.Base;
using Orbital2.Lighting;

namespace Orbital2.Game.Astrobodies;

public class Star : PhysicalGameObject, ILight
{
    public const float RadianceConstant = 20000f;
    public float LightRadius => Radius;
    public Color LightColor => new (new Vector4(new Vector3(0.1f, 0.15f, 1) * 0.6f, 1));
    public float LightIntensity => Body.Circumference * 25;

    public Color BrightColor => Color.White;
    public Color DimColor => Color.Blue;

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