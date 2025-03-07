using Orbital2.Physics;
using Orbital2.Physics.Collision;
using Orbital2.Physics.Gravity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Engine;

public class Engine
{
    public float PhysicsTimestep
    {
        get => GameWorld.PhysicsWorld.Timestep;
        set => GameWorld.PhysicsWorld.Timestep = value;
    }

    public GameWorld GameWorld { get; } = new();
    public Input Input { get; } = new();

    private List<Tuple<float, PhysicalGameObject, PhysicalGameObject>> collisions = [];

    private EventContext eventContext;

    private float accumulator = 0;

    private Stopwatch watch = new();
    private double totalTime = 0;
    private int runs = 0;

    public Engine(float physicsTimestep = 0.2f)
    {
        PhysicsTimestep = physicsTimestep;
        accumulator = 0;

        eventContext = new(GameWorld) { Input = Input };
    }

    public void Update(float timestep)
    {
        accumulator += timestep;

        TriggerWaitingCollisions(accumulator / PhysicsTimestep);

        while(accumulator >= PhysicsTimestep)
        {
            UpdatePhysics();
            accumulator -= PhysicsTimestep;
        }

        UpdateGameWorld();

        GameWorld.PhysicsWorld.InterpolateLinear(accumulator / PhysicsTimestep);

        foreach (var gameObject in GameWorld.GameObjects)
        {
            gameObject.OnFrameUpdate(timestep, eventContext);
        }

        UpdateGameWorld();
    }

    private void UpdateGameWorld()
    {
        var events = GameWorld.ProcessGameObjectQueue();

        foreach(var ev in events)
        {
            if(ev.Remove)
            {
                ev.GameObject.OnRemove(eventContext);
            }
            else
            {
                ev.GameObject.OnStart(eventContext);
            }
        }
    }

    private void UpdatePhysics()
    {
        foreach (var gameObject in GameWorld.GameObjects)
        {
            gameObject.PrePhysicsUpdate(PhysicsTimestep, eventContext);
        }

        GameWorld.PhysicsWorld.Step();

        foreach (var gameObject in GameWorld.GameObjects)
        {
            gameObject.PostPhysicsUpdate(PhysicsTimestep, eventContext);
        }

        watch.Start();
        FindAndTriggerCollisions();
        watch.Stop();

        totalTime += watch.Elapsed.TotalSeconds;
        runs++;

        watch.Reset();

        if(runs == 50)
        {
            Debug.WriteLine($"Average collision time: {totalTime / runs}");
        }
    }

    private void FindAndTriggerCollisions()
    {
        collisions.Clear();

        foreach (var collision in GameWorld.PhysicsWorld.FindCollisions())
        {
            var goa = GameWorld.GetGameObjectByBody(collision.Item2);
            var gob = GameWorld.GetGameObjectByBody(collision.Item3);

            if(goa == null || gob == null) continue;

            collisions.Add(new(collision.Item1, goa, gob));

            goa.OnCollisionFound(gob, collision.Item1, eventContext);
        }

        collisions.Sort((colA, colB) => MathF.Sign(colA.Item1 - colB.Item1));
    }

    private void TriggerWaitingCollisions(float t)
    {
        while (collisions.Count > 0)
        {
            if (t < collisions.First().Item1) break;

            collisions.First().Item2.OnCollisionPassed(collisions.First().Item3, t, eventContext);

            collisions.RemoveAt(0);
        }
    }
}