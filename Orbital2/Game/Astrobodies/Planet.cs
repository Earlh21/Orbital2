using Microsoft.Xna.Framework;
using MonoGame.Extended;
using Orbital2.Engine;
using Orbital2.ML;
using Orbital2.Physics;
using Orbital2.Physics.Gravity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Orbital2.Game.Base;
using Orbital2.Game.Life;
using Orbital2.ML.Schema;

namespace Orbital2.Game.Astrobodies;

public class Planet : TemperatureObject
{
    public const long PopulationDensity = 1000;

    public Life.Life? Life {  get; set; }

    public Planet(Body body) : base(body)
    {
        Temperature = 300;
    }

    public long MaxPopulation => (long)(Body.Circumference * PopulationDensity);
    public float CapacityReachedPercent => 1 - MathF.Min(300, MathF.Abs(300 - Temperature - 50)) / 300;
    public long CarryingCapacity => (long)(CapacityReachedPercent * MaxPopulation);
    public float CircumferenceManned => Life == null ? 0 : (float)Life.Population / PopulationDensity;

    private Random random = new();

    private float g;

    public override void OnStart(EventContext context)
    {
        //g = context.World.FindFirstObjectByType<GravityObject>().GravitySolver.GravitationalConstant;
    }

    public override void OnCollisionPassed(PhysicalGameObject other, float t, EventContext context)
    {
        if (other is Star) return;
        if (Body.Mass < other.Body.Mass) return;

        Body.ApplyImpulse(other.Mass * other.Velocity);
        Body.Matter.Add(other.Body.Matter);

        if (other is Planet otherPlanet)
        {
            ThermalEnergy += otherPlanet.ThermalEnergy;
        }

        context.World.RemoveObject(other);
    }

    public override void PrePhysicsUpdate(float physicsTimestep, EventContext context)
    {
        if (Mass > 10000)
        {
            context.World.RemoveObject(this);
            context.World.AddObject(new Star(Body.Clone()));
        }

        if(Life != null)
        {
            UpdateLife(physicsTimestep, context);
        }
    }

    private void UpdateLife(float timestep, EventContext context)
    {
        if (Life == null)
        {
            return;
        }

        //Life.Step(CarryingCapacity, timestep);
        Life.HarvestMatter(Body.Matter, CircumferenceManned, timestep);

        if (Life.Population < 100)
        {
            Life = null;
            return;
        }

        if(random.NextSingle() < 0.3f)
        {
            var targets = context.World.PhysicsWorld.BroadPhase.GetNearest(Body, 5000).Where(body =>
            {
                var obj = context.World.GetGameObjectByBody(body);

                if (obj == null) return false;
                if (obj == this) return false;

                if (obj is Planet planet)
                {
                    return planet.Life != null;
                }

                return false;
            }).ToList();

            if (targets.Count == 0) return;

            float nukeSpeed = 40;

            Body target = targets[random.Next(targets.Count)];
            Vector2? direction = GetFiringDirectionMl(target, nukeSpeed);

            if (direction == null) return;

            Vector2 nukePosition = Position + Radius * 1.1f * direction.Value;
            Nuke nuke = new(nukePosition, Velocity + direction.Value * nukeSpeed)
            {
                Target = target,
            };

            Life.RawMaterials.Hydrogen = 0;

            context.World.AddObject(nuke);
        }
    }

    private Vector2 GetFiringDirectionMl(Body target, float nukeSpeed)
    {
        Vector2 gravityAtSource = Body.PreviousForce;
        Vector2 gravityAtTarget = target.PreviousForce;
        Vector2 targetPosition = target.Position - Position;
        Vector2 targetVelocity = target.Velocity - Velocity;
        float sourceGravity = g * Body.Mass;
        float projectileSpeed = nukeSpeed;

        var firingContext = new FiringContext(targetPosition.X, targetPosition.X, targetVelocity.X, targetVelocity.Y);

        float angle = 0;//DeepBallisticOptimizer.GetFiringAngle(firing_context);

        return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }

    private Vector2? GetFiringDirection(Body target, float nukeSpeed)
    {
        Vector2 initialPosition = Position;
        Vector2 targetVelocity = target.Velocity - Velocity;
        Vector2 targetPosition = target.Position - Position;

        // Calculate coefficients for the quadratic equation
        float a = targetVelocity.LengthSquared() - nukeSpeed * nukeSpeed;
        float b = 2 * Vector2.Dot(targetPosition, targetVelocity);
        float c = targetPosition.LengthSquared();

        // Solve the quadratic equation
        float discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
        {
            // No solution exists (target is too fast or too far)
            return null; // or handle this case as appropriate
        }

        float t1 = (-b + MathF.Sqrt(discriminant)) / (2 * a);
        float t2 = (-b - MathF.Sqrt(discriminant)) / (2 * a);

        // Choose the smallest positive time
        float interceptTime = (t1 > 0 && (t2 <= 0 || t1 < t2)) ? t1 : t2;

        if (interceptTime <= 0)
        {
            // No valid solution (intercept would be in the past)
            return null; // or handle this case as appropriate
        }

        // Calculate the intercept point
        Vector2 interceptPoint = targetPosition + targetVelocity * interceptTime;

        // Calculate the firing direction
        return interceptPoint.NormalizedCopy();
    }
}