using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Physics;

public class Body
{
    public Vector2 ProjectedNextPosition { get; private set; }
    public Vector2 Position { get; private set; }
    public Vector2 PreviousPosition { get; private set; }
    public Vector2 InterpolatedPosition { get; set; }

    public Vector2 Momentum { get; set; }
    public Vector2 PreviousMomentum { get; private set; }
    public Vector2 Velocity => Momentum / Mass;

    public Vector2 Force { get; private set; }
    public Vector2 PreviousForce { get; private set; }

    public Vector2 Impulse { get; set; }

    public Matter Matter { get; set; }
    public float Mass => Matter.Mass;
    public float Density => Matter.Density;
    public float Radius => MathF.Sqrt(Mass / (MathF.PI * Density));
    public float Circumference => Radius * MathF.PI * 2;

    public Body(Matter matter)
    {
        Matter = matter;
    }

    public Body(Vector2 position, Matter matter) : this(matter)
    {
        PreviousPosition = position;
        Position = position;
        InterpolatedPosition = position;
    }

    public Body Clone()
    {
        var body = new Body(Matter);

        body.ProjectedNextPosition = ProjectedNextPosition;
        body.Position = Position;
        body.PreviousPosition = PreviousPosition;
        body.InterpolatedPosition = InterpolatedPosition;
        body.Momentum = Momentum;
        body.PreviousMomentum = PreviousMomentum;
        body.Force = Force;
        body.PreviousForce = PreviousForce;
        body.Impulse = Impulse;

        return body;
    }

    public void Step(float timestep)
    {
        PreviousForce = Force;
        PreviousMomentum = Momentum;
        PreviousPosition = Position;

        Momentum += Force * timestep + Impulse;
        Position += Velocity * timestep;
        ProjectedNextPosition = Position + Velocity * timestep;

        Force = new();
        Impulse = new();
    }

    public void Translate(Vector2 displacement)
    {
        PreviousPosition += displacement;
        Position += displacement;
        InterpolatedPosition += displacement;
    }

    public void ApplyImpulse(Vector2 impulse)
    {
        Impulse += impulse;
    }

    public void ApplyForce(Vector2 force)
    {
        Force += force;
    }

    public bool IsOverlapping(Body other)
    {
        return (Position - other.Position).LengthSquared() < (Radius + other.Radius) * (Radius + other.Radius);
    }

    public Bounds GetAabb()
    {
        var minX = MathF.Min(Position.X, PreviousPosition.X) - Radius;
        var maxX = MathF.Max(Position.X, PreviousPosition.X) + Radius;
        var minY = MathF.Min(Position.Y, PreviousPosition.Y) - Radius;
        var maxY = MathF.Max(Position.Y, PreviousPosition.Y) + Radius;

        return new(minX, minY, maxX, maxY);
    }

    public Bounds GetProjectedAabb()
    {
        var minX = MathF.Min(ProjectedNextPosition.X, Position.X) - Radius;
        var maxX = MathF.Max(ProjectedNextPosition.X, Position.X) + Radius;
        var minY = MathF.Min(ProjectedNextPosition.Y, Position.Y) - Radius;
        var maxY = MathF.Max(ProjectedNextPosition.Y, Position.Y) + Radius;

        return new(minX, minY, maxX, maxY);
    }

    public float? GetCollisionT(Body other)
    {
        return GetCollisionT(PreviousPosition, Position, Radius, other.PreviousPosition, other.Position, other.Radius);
    }

    public static float? GetCollisionT(Vector2 previousPositionA, Vector2 positionA, float radiusA, Vector2 previousPositionB, Vector2 positionB, float radiusB)
    {
        float totalRadiusSquared = (radiusA + radiusB) * (radiusA + radiusB);
        Vector2 startDistance = previousPositionB - previousPositionA;

        if (startDistance.LengthSquared() < totalRadiusSquared)
        {
            return 0;
        }

        Vector2 deltaA = positionA - previousPositionA;
        Vector2 deltaB = positionB - previousPositionB;
        Vector2 relativeVelocity = deltaB - deltaA;

        // Quadratic equation coefficients
        float a = relativeVelocity.Dot(relativeVelocity);
        float b = 2 * relativeVelocity.Dot(startDistance);
        float c = startDistance.Dot(startDistance) - totalRadiusSquared;

        // Solve quadratic equation
        float discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
        {
            // No collision
            return null;
        }

        float t = (-b - MathF.Sqrt(discriminant)) / (2 * a);

        if (t >= 0 && t <= 1)
        {
            // Collision occurred between PreviousPosition and Position
            return t;
        }

        // No collision in the given time frame
        return null;
    }
}