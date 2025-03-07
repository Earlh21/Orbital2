using Microsoft.Xna.Framework;
using QuikGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Orbital2.Physics.Collision;

internal class SpatialHashing(float cellSize) : BroadPhase
{
    public float CellSize { get; set; } = cellSize;

    private HashGrid? grid;
    private IReadOnlyList<Body>? bodies;

    public override IEnumerable<(Body, Body)> Collisions()
    {
        if(grid == null || bodies == null)
        {
            throw new InvalidOperationException("Must call update_bodies first.");
        }

        HashSet<Body> pairs = [];

        foreach (var body in bodies)
        {
            pairs.Clear();

            foreach(var other in grid.GetBodies(body.GetAabb()))
            {
                if(pairs.Contains(other)) continue;
                if (body == other) continue;

                pairs.Add(other);

                yield return new(body, other);
            }
        }
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
        if (grid == null)
        {
            return [];
        }

        var cellsInRange = grid.GridCells.Where(cell =>
        {
            return (body.Position - grid.GetWorldPosition(cell.Key)).LengthSquared() <= distance * distance;
        });

        return cellsInRange.SelectMany(cell => cell.Value);
    }

    public override void UpdateBodies(IReadOnlyList<Body> bodies)
    {
        this.bodies = bodies;
        grid = new(CellSize);

        foreach(var body in bodies)
        {
            grid.Insert(body);
        }
    }
}