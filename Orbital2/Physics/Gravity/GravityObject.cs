using Orbital2.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Physics.Gravity
{
    public class GravityObject(GravitySolver Solver) : GameObject
    {
        public override void PostPhysicsUpdate(float physics_timestep, Engine.Engine game)
        {
            var bodies = game.PhysicalObjects.Select(o => o.Body).ToList();
            var accels = Solver.ComputeAccelerations(bodies);
            
            for(int i = 0; i < bodies.Count; i++)
            {
                bodies[i].Force += accels[i] * bodies[i].Mass;
            }
        }
    }
}
