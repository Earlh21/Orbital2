using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Physics.Collision;

internal class HashGrid(float cellSize)
{
    public float CellSize { get; } = cellSize;

    public IEnumerable<KeyValuePair<Point, List<Body>>> GridCells => grid;

    private Dictionary<Point, List<Body>> grid = [];

    public IEnumerable<Body> GetBodies(Body body)
    {
        HashSet<Body> pairs = [];

        foreach (var other in GetBodies(body.GetAabb()))
        {
            if (pairs.Contains(other)) continue;
            if (body == other) continue;

            pairs.Add(other);

            yield return other;
        }
    }

    public IEnumerable<Body> GetBodies(Bounds aabb)
    {
        Point bottomLeft = GetGridPosition(aabb.BottomLeft);
        Point topRight = GetGridPosition(aabb.TopRight);

        return GetBodies(bottomLeft, topRight);
    }

    public IEnumerable<Body> GetBodies((Point, Point) gridBox)
    {
        return GetBodies(gridBox.Item1, gridBox.Item2);
    }

    public IEnumerable<Body> GetBodies(Point bottomLeft, Point topRight)
    {
        for (int x = bottomLeft.X; x <= topRight.X; x++)
        {
            for (int y = bottomLeft.Y; y <= topRight.Y; y++)
            {
                Point point = new(x, y);

                if (!grid.ContainsKey(point)) continue;

                foreach (var body in grid[point])
                {
                    yield return body;
                }
            }
        }
    }

    public void Insert(Body body)
    {
        Bounds aabb = body.GetAabb();

        Point bottomLeft = GetGridPosition(aabb.BottomLeft);
        Point topRight = GetGridPosition(aabb.TopRight);

        for (int x = bottomLeft.X; x <= topRight.X; x++)
        {
            for (int y = bottomLeft.Y; y <= topRight.Y; y++)
            {
                Insert(body, new Point(x, y));
            }
        }
    }

    public void Insert(Body body, Bounds aabb)
    {
        Point bottomLeft = GetGridPosition(aabb.BottomLeft);
        Point topRight = GetGridPosition(aabb.TopRight);

        for (int x = bottomLeft.X; x <= topRight.X; x++)
        {
            for (int y = bottomLeft.Y; y <= topRight.Y; y++)
            {
                Insert(body, new Point(x, y));
            }
        }
    }

    private void Insert(Body body, Point gridPosition)
    {
        if (!grid.ContainsKey(gridPosition))
        {
            grid[gridPosition] = [body];
        }
        else
        {
            grid[gridPosition].Add(body);
        }
    }

    public (Point, Point) GetGridBox(Bounds aabb)
    {
        return (
            GetGridPosition(aabb.BottomLeft),
            GetGridPosition(aabb.TopRight)
        );
    }

    public Vector2 GetWorldPosition(Point gridPosition)
    {
        return new(gridPosition.X * CellSize, gridPosition.Y * CellSize);
    }

    public Point GetGridPosition(Vector2 worldPosition)
    {
        return new((int)(worldPosition.X / CellSize), (int)(worldPosition.Y / CellSize));
    }
}