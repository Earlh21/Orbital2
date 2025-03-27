using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Orbital2.ML.Schema;
using Orbital2.ML.Training;
using Orbital2.Physics;
using Orbital2.Physics.Collision;
using TorchSharp;

namespace Orbital2.ML;

public static class TrainingSolver
{
    private static Random Random { get; } = new();

    public static TrainingData SolveSetup(TrainingSetup setup)
    {
        float bestAngle = 0;
        float minimumDistance = float.MaxValue;

        for (float angle = 0; angle < 2 * MathF.PI; angle += 0.001f + Random.NextSingle() * 0.0005f)
        {
            float distance = GetMinimumDistance(setup.World, setup.Source, setup.Target, angle);
            
            if (distance < minimumDistance)
            {
                minimumDistance = distance;
                bestAngle = angle;
            }

            if (minimumDistance < 0)
            {
                break;
            }
        }
        
        return new TrainingData(
            new FiringContext(setup.Target.Position.X - setup.Source.Position.X, setup.Target.Position.Y - setup.Source.Position.Y, setup.Target.Velocity.X - setup.Source.Velocity.X,
                setup.Target.Velocity.Y - setup.Source.Velocity.Y),
            new FiringPrediction(MathF.Cos(bestAngle), MathF.Sin(bestAngle))
        );
    }

    private static float GetMinimumDistance(World world, Body source, Body target, float angle)
    {
        var worldClone = world.Clone();
            
        var sourceClone = source.Clone();
        var targetClone = target.Clone();
            
        worldClone.AddBody(sourceClone);
        worldClone.AddBody(targetClone);

        Vector2 projectileDirection = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 projectilePosition = sourceClone.Position + projectileDirection * 1.1f * sourceClone.Radius;
        Body projectile = new(projectilePosition, new() { Hydrogen = 0.1f });

        projectile.Momentum = projectileDirection * projectile.Mass * 40;

        worldClone.AddBody(projectile);

        return GetMinimumDistance(worldClone, targetClone, projectile);
    }

    private static float GetMinimumDistance(World worldClone, Body target, Body projectile)
    {
        float minimumDistance = float.MaxValue;
        float previousDistance = 0;
        int stepsAway = 0;
        
        for (int i = 0; i < 100; i++)
        {
            worldClone.Step();
            float distance = (target.Position - projectile.Position).Length();

            if (worldClone.FindCollisions()
                .Any(collision => collision.Item2 == projectile && collision.Item3 == target))
            {
                return -1;
            }

            if (distance < minimumDistance)
            {
                minimumDistance = distance;
            }

            if (distance > previousDistance)
            {
                stepsAway++;
            }

            previousDistance = distance;

            if (stepsAway > 10)
            {
                return minimumDistance;
            }   
        }
        
        return minimumDistance;
    }

    public struct TrainingSetup
    {
        public World World { get; }
        public Body Source { get; }
        public Body Target { get; }

        public TrainingSetup(World world, Body source, Body target)
        {
            World = world;
            Source = source;
            Target = target;
        }
    }
}