using Microsoft.Xna.Framework;
using QuikGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Physics.Collision;

public abstract class BroadPhase
{
    public abstract void UpdateBodies(IReadOnlyList<Body> bodies);

    public abstract IEnumerable<ValueTuple<Body, Body>> Collisions();
    public abstract IEnumerable<Body> FixedRaycast(Vector2 start, Vector2 end);
    public abstract IEnumerable<Body> DynamicRaycast(Vector2 start, Vector2 end);

    public abstract IBidirectionalGraph<Body, Edge<Body>> CreateGraph(float distance, Func<Body, bool> selectorFunction);
    public abstract IEnumerable<Body> GetNearest(Body body, float distance);

    public IBidirectionalGraph<Body, Edge<Body>> CreateGraph(float distance)
    {
        return CreateGraph(distance, x => true);
    }
}