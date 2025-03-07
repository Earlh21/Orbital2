using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Orbital2.Engine;
using Orbital2.Physics;
using Orbital2.Physics.Gravity;

namespace Orbital2.Game.Utility;

public class GravityObject(GravitySolver solver) : GameObject
{
    public GravitySolver GravitySolver { get; set; } = solver;

    private Task<Vector2[]>? accelsTask = null;
    private IReadOnlyList<Body> bodies = [];

    public override void PrePhysicsUpdate(float physicsTimestep, EventContext context)
    {
        if (accelsTask == null) return;

        accelsTask.Wait();
        var accels = accelsTask.Result;

        for (int i = 0; i < bodies.Count; i++)
        {
            bodies[i].ApplyForce(accels[i] * bodies[i].Mass);
        }
    }

    public override void PostPhysicsUpdate(float physicsTimestep, EventContext context)
    {
        bodies = context.World.PhysicalObjects.Select(o => o.Body).ToList();
        accelsTask = Task.Run(() => { return GravitySolver.ComputeAccelerations(bodies); });
    }
}