using Microsoft.Xna.Framework;
using Orbital2.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Engine
{
    public abstract class PhysicalGameObject : GameObject
    {
        public Body Body { get; }
        public Vector2 InterpolatedPosition => Body.InterpolatedPosition;
        public Vector2 PreviousPosition => Body.PreviousPosition;
        public Vector2 Position => Body.Position;

        public PhysicalGameObject(Body body)
        {
            Body = body;
        }

        public virtual void OnCollisionFound(PhysicalGameObject other, float t, Engine engine) { }
        public virtual void OnCollisionPassed(PhysicalGameObject other, float t, Engine engine) { }
    }
}
