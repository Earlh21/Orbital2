using Orbital2.Engine;
using Orbital2.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.GameObjects
{
    public class Planet : PhysicalGameObject
    {
        public Planet(Body body) : base(body) { }

        public override void OnCollisionFound(PhysicalGameObject other, float t, Engine.Engine engine)
        {
            var position = PreviousPosition * (1 - t) + Position * t;
            engine.AddObject(new CollisionMarker(position, Body.Radius, engine.PhysicsTimestep - t), "marker");
        }
    }
}
