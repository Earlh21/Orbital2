using Microsoft.Xna.Framework;
using Orbital2.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orbital2.Lighting;

namespace Orbital2.Engine;

public abstract class PhysicalGameObject : GameObject, ILightingOccluder
{
    public Body Body { get; }
    public Vector2 InterpolatedPosition => Body.InterpolatedPosition;
    public Vector2 PreviousPosition => Body.PreviousPosition;
    public Vector2 Position => Body.Position;
    public Vector2 Momentum => Body.Momentum;
    public Vector2 Velocity => Body.Velocity;
    public float Mass => Body.Mass;
    public float Radius => Body.Radius;
    public Vector2 LightPosition => Body.InterpolatedPosition;

    public PhysicalGameObject(Body body)
    {
        Body = body;
    }

    public virtual void OnCollisionFound(PhysicalGameObject other, float t, EventContext context) { }
    public virtual void OnCollisionPassed(PhysicalGameObject other, float t, EventContext context) { }
}