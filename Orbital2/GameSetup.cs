using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Orbital2.Game.Astrobodies;
using Orbital2.Physics;
using Orbital2.Physics.Gravity;

namespace Orbital2;

public static class GameSetup
{
    private static Random Random { get; } = new();
    
    public static (Body Source, Body Target) GenerateSimpleSetup(float minDistance, float maxDistance, float minMass, float maxMass, float minSpeed, float maxSpeed)
    {
        var source = GenerateBody(minDistance, maxDistance, minMass, maxMass, minSpeed, maxSpeed);
        var target = GenerateBody(minDistance, maxDistance, minMass, maxMass, minSpeed, maxSpeed);

        while (source.IsOverlapping(target))
        {
            source = GenerateBody(minDistance, maxDistance, minMass, maxMass, minSpeed, maxSpeed);
            target = GenerateBody(minDistance, maxDistance, minMass, maxMass, minSpeed, maxSpeed);
        }
        
        return (source, target);
    }
    
    public static (Body Star, List<Body> Planets) GenerateHalo(GravitySolver solver, int numPlanets, float maxDistance, float minDistance, float maxMass, float minMass,
        float starMass)
    {
        var starMatter = new Matter { Hydrogen = starMass };
        var starBody = new Body(new Vector2(0, 0), starMatter);

        var planets = Enumerable.Range(0, numPlanets)
            .Select(_ => GenerateBody(minDistance, maxDistance, minMass, maxMass, 0, 0))
            .ToList();
    
        var accelerations = solver.ComputeAccelerations(planets.Concat([starBody]).ToArray());

        for (int i = 0; i < planets.Count; i++)
        {
            var body = planets[i];
            var accel = accelerations[i];
            
            float distance = body.Position.Length();
            
            body.Momentum = GetOrbitVelocity(accel, distance) * body.Mass;
        }
        
        return (starBody, planets);
    }
    
    private static Body GenerateBody(float minDistance, float maxDistance, float minMass, float maxMass, float minSpeed, float maxSpeed)
    {
        float distance = minDistance + (float)Random.NextDouble() * (maxDistance - minDistance);
        double angle = Random.NextDouble() * Math.PI * 2;
        
        float x = distance * (float)Math.Cos(angle);
        float y = distance * (float)Math.Sin(angle);
        
        Vector2 position = new Vector2(x, y);
        
        float mass = minMass + (float)Random.NextDouble() * (maxMass - minMass);
        Matter matter = new Matter { Hydrogen = mass };
        
        Body body = new Body(position, matter);
        
        float speed = minSpeed + (float)Random.NextDouble() * (maxSpeed - minSpeed);
        double speedAngle = Random.NextDouble() * Math.PI * 2;
        
        Vector2 velocity = new Vector2((float)Math.Cos(speedAngle), (float)Math.Sin(speedAngle)) * speed;
        
        body.Momentum = velocity * body.Mass;
        
        return body;
    }
    
    public static Vector2 GetOrbitVelocity(Vector2 centripetalAcceleration, float distance)
    {
        float accelMag = centripetalAcceleration.Length();
        Vector2 accelDir = centripetalAcceleration / accelMag;

        Vector2 dir = new Vector2(-accelDir.Y, accelDir.X);
        float mag = MathF.Sqrt(accelMag * distance);

        return mag * dir; ;
    }
}