using Microsoft.Xna.Framework;
using QuikGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Physics.Collision;

public class SpatialGrid : BroadPhase
{
    public float Spacing { get; set; }
    private Grid? grid;
    private IReadOnlyList<Body> bodies = [];

    public SpatialGrid(float spacing)
    {
        Spacing = spacing;
    }

    public override IEnumerable<ValueTuple<Body, Body>> Collisions()
    {
        if(grid == null)
        {
            throw new InvalidOperationException("Must call UpdateBodies first.");
        }

        foreach (var body in bodies)
        {
            foreach (var gridPosition in grid.GetPossibleGridPositions(body))
            {
                var cell = grid.GetBodiesInCell(gridPosition);

                if(cell != null)
                {
                    foreach(var other in cell)
                    {
                        if (body == other) continue;

                        yield return new(body, other);
                    }
                }
            }
        }
    }

    public override IEnumerable<Body> DynamicRaycast(Vector2 start, Vector2 end)
    {
        throw new NotImplementedException();
    }

    public override IEnumerable<Body> FixedRaycast(Vector2 start, Vector2 end)
    {
        throw new NotImplementedException();
    }

    public override void UpdateBodies(IReadOnlyList<Body> bodies)
    {
        this.bodies = bodies;
        grid = new(Spacing, bodies);
    }

    public override IBidirectionalGraph<Body, Edge<Body>> CreateGraph(float distance, Func<Body, bool> selectorFunction)
    {
        if (grid == null)
        {
            throw new InvalidOperationException("Must call update_bodies first.");
        }

        var graph = new BidirectionalGraph<Body, Edge<Body>>();

        // Add all bodies that satisfy the selector function as vertices
        foreach (var body in bodies.Where(selectorFunction))
        {
            graph.AddVertex(body);
        }

        // Create edges between bodies that are within the specified distance
        foreach (var body in graph.Vertices)
        {
            var nearestBodies = GetNearest(body, distance).Where(graph.ContainsVertex);

            foreach (var otherBody in nearestBodies)
            {
                if (otherBody != body)
                {
                    graph.AddEdge(new Edge<Body>(body, otherBody));
                }
            }
        }

        return graph;
    }

    public override IEnumerable<Body> GetNearest(Body body, float distance)
    {
        if (grid == null)
        {
            throw new InvalidOperationException("Must call UpdateBodies first.");
        }

        HashSet<Body> nearestBodies = new HashSet<Body>();
        float squaredDistance = distance * distance;

        foreach (var gridPosition in grid.GetGridPositionsInRadius(body.Position, distance))
        {
            var cellBodies = grid.GetBodiesInCell(gridPosition);
            if (cellBodies != null)
            {
                foreach (var other in cellBodies)
                {
                    if (other != body &&
                        Vector2.DistanceSquared(body.Position, other.Position) <= squaredDistance)
                    {
                        nearestBodies.Add(other);
                    }
                }
            }
        }

        return nearestBodies;
    }

    private class Grid
    {
        private List<Body>?[,] grid = new List<Body>[0, 0];
        private float spacing;
        private Vector2 gridLocation;

        public Grid(float spacing, IReadOnlyList<Body> bodies)
        {
            this.spacing = spacing;

            Vector2 min = new(
                bodies.Min(x => MathF.Min(x.Position.X, x.PreviousPosition.X) - x.Radius),
                bodies.Min(x => MathF.Min(x.Position.Y, x.PreviousPosition.Y) - x.Radius));

            Vector2 max = new(
                bodies.Max(x => MathF.Max(x.Position.X, x.PreviousPosition.X) + x.Radius),
                bodies.Max(x => MathF.Max(x.Position.Y, x.PreviousPosition.Y) + x.Radius));

            int cols = (int)Math.Ceiling((max.X - min.X) / spacing) + 2;
            int rows = (int)Math.Ceiling((max.Y - min.Y) / spacing) + 2;

            gridLocation = new Vector2(min.X - spacing, min.Y - spacing);

            grid = new List<Body>[cols, rows];

            foreach (var body in bodies)
            {
                InsertBody(body);
            }
        }

        private void InsertBody(Body body)
        {
            foreach(var gridPosition in GetPossibleGridPositions(body))
            {
                InsertBodyAtCell(gridPosition, body);
            }
        }

        private void InsertBodyAtCell(Point gridPosition, Body body)
        {
            if (grid[gridPosition.X, gridPosition.Y] == null)
            {
                grid[gridPosition.X, gridPosition.Y] = [body];
            }
            else
            {
                grid[gridPosition.X, gridPosition.Y].Add(body);
            }
        }

        public List<Body>? GetBodiesInCell(Point gridPosition)
        {
            if(gridPosition.X < 0 || gridPosition.Y < 0 || gridPosition.X >= grid.GetLength(0) || gridPosition.Y >= grid.GetLength(1))
            {
                return null;
            }

            return grid[gridPosition.X, gridPosition.Y];
        }

        public Point GetGridPosition(Vector2 worldPosition)
        {
            return new(
                (int)((worldPosition.X - gridLocation.X) / spacing),
                (int)((worldPosition.Y - gridLocation.Y) / spacing));
        }
        public IEnumerable<Point> GetGridPositionsInRadius(Vector2 center, float radius)
        {
            var minPos = GetGridPosition(new Vector2(center.X - radius, center.Y - radius));
            var maxPos = GetGridPosition(new Vector2(center.X + radius, center.Y + radius));

            for (int x = minPos.X; x <= maxPos.X; x++)
            {
                for (int y = minPos.Y; y <= maxPos.Y; y++)
                {
                    yield return new Point(x, y);
                }
            }
        }

        public IEnumerable<Point> GetPossibleGridPositions(Body body)
        {
            var minX = MathF.Min(body.Position.X, body.PreviousPosition.X) - body.Radius;
            var maxX = MathF.Max(body.Position.X, body.PreviousPosition.X) + body.Radius;
            var minY = MathF.Min(body.Position.Y, body.PreviousPosition.Y) - body.Radius;
            var maxY = MathF.Max(body.Position.Y, body.PreviousPosition.Y) + body.Radius;

            var topLeft = GetGridPosition(new Vector2(minX, minY));
            var bottomRight = GetGridPosition(new Vector2(maxX, maxY));

            for (int x = topLeft.X; x <= bottomRight.X; x++)
            {
                for (int y = topLeft.Y; y <= bottomRight.Y; y++)
                {
                    yield return new Point(x, y);
                }
            }
        }

        private IEnumerable<Point> BresenhamLine(int x0, int y0, int x1, int y1)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                yield return new(x0, y0);

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }
    }
}