using Microsoft.Xna.Framework;
using MonoGame.Extended;
using Orbital2.Physics.Collision;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orbital2.Physics;

public class World
{
    public BroadPhase BroadPhase { get; set; } = new SpatialHashing(40);

    public float Timestep
    {
        get => timestep;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentException("Physics timestep must be greater than zero.");
            }

            timestep = value;
        }
    }

    private List<Body> bodies = [];
    private float timestep = 0.2f;

    public IReadOnlyList<Body> Bodies => bodies.AsReadOnly();

    public World()
    {
        
    }
    
    public World(BroadPhase broadPhase)
    {
        BroadPhase = broadPhase;
    }

    public World Clone()
    {
        return Clone(BroadPhase);
    }
    
    public World Clone(BroadPhase broadPhase)
    {
        var world = new World(broadPhase);

        foreach(var body in Bodies)
        {
            world.bodies.Add(body.Clone());
        }

        world.Timestep = timestep;

        return world;
    }

    public void AddBody(Body body)
    {
        bodies.Add(body);

        body.Step(Timestep);
    }

    public void RemoveBody(Body body)
    {
        bodies.Remove(body);
    }

    public void Clear()
    {
        bodies.Clear();
    }

    public void Step()
    {
        foreach (var body in bodies)
        {
            body.Step(Timestep);
        }

        BroadPhase.UpdateBodies(Bodies);
    }

    public IEnumerable<Body> FixedRaycast(Vector2 start, Vector2 end)
    {
        return BroadPhase.FixedRaycast(start, end);
    }

    //Predict collisions and invalid if state has changed
    public IEnumerable<ValueTuple<float, Body, Body>> FindCollisions()
    {
        var potentialCollisions = BroadPhase.Collisions();

        var collisions = potentialCollisions.Select<ValueTuple<Body, Body>, ValueTuple<float, Body, Body>?>(potentialCollision =>
        {
            float? collisionT = potentialCollision.Item1.GetCollisionT(potentialCollision.Item2);

            if (collisionT == null)
            {
                return null;
            }

            return new ValueTuple<float, Body, Body>(collisionT.Value, potentialCollision.Item1, potentialCollision.Item2);
        }).AsParallel();

        foreach (var collision in collisions)
        {
            if (collision == null) continue;

            yield return collision.Value;
        }
    }

    public void InterpolateLinear(float t)
    {
        if (t < 0)
        {
            t = 0;
        }

        if (t > 1)
        {
            t = 1;
        }

        foreach (var body in bodies)
        {
            body.InterpolatedPosition = body.PreviousPosition * (1 - t) + body.Position * t;
        }
    }
}