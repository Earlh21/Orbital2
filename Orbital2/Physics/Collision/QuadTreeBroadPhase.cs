using Microsoft.Xna.Framework;
using QuikGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Orbital2.Physics.Gravity.QuadTree;

namespace Orbital2.Physics.Collision;

public class QuadTreeBroadPhase(float minCellSize) : BroadPhase
{
    public float MinCellSize { get; set; } = minCellSize;

    private FixedQuadTree? tree;
    private IReadOnlyList<Body>? bodies;

    public override IEnumerable<(Body, Body)> Collisions()
    {
        if (tree == null || bodies == null)
        {
            throw new InvalidOperationException("Must call UpdateBodies first.");
        }

        foreach(var body in bodies)
        {
            foreach(var other in tree.GetBodies(body))
            {
                if (body == other) continue;

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
        throw new NotImplementedException();
    }

    public override void UpdateBodies(IReadOnlyList<Body> bodies)
    {
        tree = new(MinCellSize, bodies);
        this.bodies = bodies;
    }

    private class FixedQuadTree
    {
        public float MinCellSize { get; }
        public Quad Root { get; }

        public FixedQuadTree(float minCellSize, IEnumerable<Body> bodies)
        {
            MinCellSize = minCellSize;

            float minX = bodies.Min(x => x.Position.X);
            float minY = bodies.Min(x => x.Position.Y);
            float maxX = bodies.Max(x => x.Position.X);
            float maxY = bodies.Max(y => y.Position.Y);

            float width = maxX - minX;
            float height = maxY - minY;

            minX -= width * 0.01f;
            minY -= height * 0.01f;

            Root = new StemQuad(new Bounds(minX, minY, maxX, maxY), null, MinCellSize);

            foreach(var body in bodies)
            {
                Root.Insert(body, body.GetAabb());
            }
        }

        public IEnumerable<Body> GetBodies(Body body)
        {
            List<Body> bodies = new();
            Root.GetBodies(bodies, body.GetAabb());

            return bodies;
        }

        public abstract class Quad
        {
            public StemQuad? Parent { get; }
            public int Level { get; }
            public Bounds Bounds { get; }

            public Quad(Bounds bounds, StemQuad? parent)
            {
                Bounds = bounds;
                Parent = parent;

                Level = parent == null ? 0 : parent.Level + 1;
            }

            public abstract void Insert(Body body, Bounds aabb);

            public abstract void GetBodies(List<Body> bodies, Bounds aabb);

            public bool ContainsPoint(Vector2 point)
            {
                return Bounds.ContainsPoint(point);
            }
        }

        public class StemQuad : Quad
        { 
            public Quad? BottomLeft { get; private set; }
            public Quad? BottomRight { get; private set; }
            public Quad? TopLeft { get; private set; }
            public Quad? TopRight { get; private set; }

            private float minCellSize;

            public StemQuad(Bounds bounds, StemQuad? parent, float minCellSize) : base(bounds, parent)
            {
                this.minCellSize = minCellSize;
            }

            public override void Insert(Body body, Bounds aabb)
            {
                if(BottomLeft == null)
                {
                    Expand();
                }

                if (BottomLeft.Bounds.Overlaps(aabb))
                {
                    BottomLeft.Insert(body, aabb);
                }
                    
                if (BottomRight.Bounds.Overlaps(aabb))
                {
                    BottomRight.Insert(body, aabb);
                }
                    
                if (TopLeft.Bounds.Overlaps(aabb))
                {
                    TopLeft.Insert(body, aabb);
                }
                    
                if (TopRight.Bounds.Overlaps(aabb))
                {
                    TopRight.Insert(body, aabb);
                }
            }

            public override void GetBodies(List<Body> bodies, Bounds aabb)
            {
                if (BottomLeft == null) return;

                if (BottomLeft.Bounds.Overlaps(aabb))
                {
                    BottomLeft.GetBodies(bodies, aabb);
                }
                    
                if (BottomRight.Bounds.Overlaps(aabb))
                {
                    BottomRight.GetBodies(bodies, aabb);
                }
                    
                if (TopLeft.Bounds.Overlaps(aabb))
                {
                    TopLeft.GetBodies(bodies, aabb);
                }
                    
                if (TopRight.Bounds.Overlaps(aabb))
                {
                    TopRight.GetBodies(bodies, aabb);
                }
            }

            private void Expand()
            {
                if (MathF.Min(Bounds.Width, Bounds.Height) / 2 < minCellSize)
                {
                    BottomLeft = new LeafQuad(new(Bounds.BottomLeft, Bounds.Center), this);
                    BottomRight = new LeafQuad(new(Bounds.Center.X, Bounds.Bottom, Bounds.Right, Bounds.CenterY), this);
                    TopLeft = new LeafQuad(new(Bounds.Left, Bounds.CenterY, Bounds.CenterX, Bounds.Top), this);
                    TopRight = new LeafQuad(new(Bounds.Center, Bounds.TopRight), this);
                }
                else
                {
                    BottomLeft = new StemQuad(new(Bounds.BottomLeft, Bounds.Center), this, minCellSize);
                    BottomRight = new StemQuad(new(Bounds.Center.X, Bounds.Bottom, Bounds.Right, Bounds.CenterY), this, minCellSize);
                    TopLeft = new StemQuad(new(Bounds.Left, Bounds.CenterY, Bounds.CenterX, Bounds.Top), this, minCellSize);
                    TopRight = new StemQuad(new(Bounds.Center, Bounds.TopRight), this, minCellSize);
                }
            }
        }

        public class LeafQuad : Quad
        {
            public IEnumerable<Body> Bodies => bodies == null ? new() : bodies;

            private List<Body>? bodies = null;

            public LeafQuad(Bounds bounds, StemQuad? parent) : base(bounds, parent)
            {
                    
            }

            public override void Insert(Body body, Bounds bounds)
            {
                if(bodies == null)
                {
                    bodies = [];
                }

                bodies.Add(body);
            }

            public override void GetBodies(List<Body> bodies, Bounds aabb)
            {
                if(this.bodies != null)
                {
                    bodies.AddRange(this.bodies);
                }
            }
        }
    }
}