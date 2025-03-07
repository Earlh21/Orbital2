using Microsoft.Xna.Framework;
using QuikGraph;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Orbital2.Physics.Collision;

internal class PredictiveSpatialHashing(float cellSize) : BroadPhase
{
    public float CellSize { get; set; } = cellSize;

    private HashGrid? currentGrid;
    private HashGrid? predictedGrid;
    private IReadOnlyList<Body>? bodiesClone;
    private Task<List<ValueTuple<Body, Body>>>? predictionTask;

    public override IEnumerable<(Body, Body)> Collisions()
    {
        if (currentGrid == null || bodiesClone == null)
        {
            throw new InvalidOperationException("Must call update_bodies first");
        }

        if (predictionTask == null)
        {
            foreach (var collision in GetCollisionsWithoutPrediction())
            {
                yield return collision;
            }
        }
        else
        {
            var taskResult = predictionTask.Result;

            foreach (var collision in taskResult)
            {
                yield return collision;
            }
        }

        predictionTask = PredictCollisions();
    }

    public override IBidirectionalGraph<Body, Edge<Body>> CreateGraph(float distance, Func<Body, bool> selectorFunction)
    {
        throw new NotImplementedException();
    }

    public override IEnumerable<Body> DynamicRaycast(Vector2 start, Vector2 end)
    {
        throw new NotImplementedException();
    }

    public override IEnumerable<Body> FixedRaycast(Vector2 start, Vector2 end)
    {
        throw new NotImplementedException();
    }

    public override IEnumerable<Body> GetNearest(Body body, float distance)
    {
        if (currentGrid == null)
        {
            return [];
        }

        float distanceSquared = distance * distance;

        var cellsInRange = currentGrid.GridCells.Where(cell =>
        {
            return (body.Position - currentGrid.GetWorldPosition(cell.Key)).LengthSquared() <= distance * distance;
        });

        return cellsInRange.SelectMany(cell => cell.Value);
    }

    public override void UpdateBodies(IReadOnlyList<Body> bodies)
    {
        bodiesClone = bodies.ToList();
        currentGrid = new(CellSize);

        foreach(var body in bodies)
        {
            currentGrid.Insert(body);
        }
    }

    private IEnumerable<ValueTuple<Body, Body>> GetCollisionsWithoutPrediction()
    {
        if (currentGrid == null || bodiesClone == null)
        {
            throw new InvalidOperationException("Must call update_bodies first");
        }

        HashSet<Body> pairs = [];

        foreach (var body in bodiesClone)
        {
            pairs.Clear();

            foreach (var other in currentGrid.GetBodies(body))
            {
                yield return new(body, other);
            }
        }
    }

    private Task<List<ValueTuple<Body, Body>>> PredictCollisions()
    {
        if (bodiesClone == null)
        {
            throw new InvalidOperationException("Must call update_bodies first");
        }

        ConcurrentBag<ValueTuple<Body, Body>> predictedCollisions = [];

        predictedGrid = new(CellSize);

        foreach (var body in bodiesClone)
        {
            predictedGrid.Insert(body);
        }

        return Task.Run(() =>
        {
            Parallel.ForEach(bodiesClone, body =>
            {
                var projectedAabb = body.GetProjectedAabb();

                foreach (var other in predictedGrid.GetBodies(projectedAabb))
                {
                    if (body == other) continue;
                    if (!GetPredictedCollision(body, other)) continue;

                    predictedCollisions.Add(new(body, other));
                    predictedCollisions.Add(new(other, body));
                }
            });

            return predictedCollisions.ToList();
        });
    }

    private bool GetPredictedCollision(Body a, Body b)
    {
        return Body.GetCollisionT(a.Position, a.ProjectedNextPosition, a.Radius, b.Position, b.ProjectedNextPosition, b.Radius) != null;
    }
}