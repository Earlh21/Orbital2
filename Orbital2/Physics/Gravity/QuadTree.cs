using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Physics.Gravity;

internal class QuadTree
{
    public float? MinCellSize { get; }
    public Quad Root { get; }

    public QuadTree(IEnumerable<Body> bodies, float? minCellSize = null)
    {
        float minX = bodies.Min(x => x.Position.X);
        float minY = bodies.Min(x => x.Position.Y);
        float maxX = bodies.Max(x => x.Position.X);
        float maxY = bodies.Max(y => y.Position.Y);

        float width = MathF.Max(maxX - minX, 0.01f);
        float height = MathF.Max(maxY - minY, 0.01f);

        minX -= width * 0.01f;
        minY -= height * 0.01f;

        maxX += width * 0.01f;
        maxY += height * 0.01f;

        Root = new(new(minX, minY, maxX, maxY));

        foreach (var body in bodies)
        {
            Root.Insert(body);
        }

        Root.ComputeCenterOfMass();
    }

    public void Insert(Body body)
    {
        Root.Insert(body);
    }

    internal struct CenterOfMass
    {
        public Vector2 Position { get; }
        public float Mass { get; }

        public CenterOfMass(Vector2 position, float mass)
        {
            Position = position;
            Mass = mass;
        }
    }

    internal class Quad
    {
        public Quad? BottomLeft { get; private set; }
        public Quad? BottomRight { get; private set; }
        public Quad? TopLeft { get; private set; }
        public Quad? TopRight { get; private set; }

        public Bounds Bounds { get; }

        public Body? Node { get; private set; }

        public CenterOfMass CenterOfMass { get; private set; }

        public bool IsLeaf => BottomLeft == null;
        public bool HasNode => Node != null;

        public Quad(Bounds bounds)
        {
            Bounds = bounds;
        }

        private void Expand()
        {
            if(!IsLeaf)
            {
                throw new InvalidOperationException("Quad has already been expanded.");
            }

            BottomLeft = new(new(Bounds.BottomLeft, Bounds.Center));
            BottomRight = new(new(Bounds.Center.X, Bounds.Bottom, Bounds.Right, Bounds.CenterY));
            TopLeft = new(new(Bounds.Left, Bounds.CenterY, Bounds.CenterX, Bounds.Top));
            TopRight = new(new(Bounds.Center, Bounds.TopRight));
        }

        public bool ContainsPoint(Vector2 point)
        {
            return Bounds.ContainsPoint(point);
        }

        internal void ComputeCenterOfMass()
        {
            if(!IsLeaf)
            {
                BottomLeft.ComputeCenterOfMass();
                BottomRight.ComputeCenterOfMass();
                TopLeft.ComputeCenterOfMass();
                TopRight.ComputeCenterOfMass();

                Vector2 pos = BottomLeft.CenterOfMass.Position * BottomLeft.CenterOfMass.Mass
                              + BottomRight.CenterOfMass.Position * BottomRight.CenterOfMass.Mass
                              + TopLeft.CenterOfMass.Position * TopLeft.CenterOfMass.Mass
                              + TopRight.CenterOfMass.Position * TopRight.CenterOfMass.Mass;

                float mass = BottomLeft.CenterOfMass.Mass
                             + BottomRight.CenterOfMass.Mass
                             + TopLeft.CenterOfMass.Mass
                             + TopRight.CenterOfMass.Mass;

                pos /= mass;

                CenterOfMass = new(pos, mass);
            }
            else
            {
                if(HasNode)
                {
                    CenterOfMass = new(Node.Position, Node.Mass);
                }
                else
                {
                    CenterOfMass = new(new(), 0);
                }
            }
        }

        public void Insert(Body body)
        {
            if(!IsLeaf)
            {
                if (BottomLeft.ContainsPoint(body.Position))
                {
                    BottomLeft.Insert(body);
                }
                else if (BottomRight.ContainsPoint(body.Position))
                {
                    BottomRight.Insert(body);
                }
                else if (TopLeft.ContainsPoint(body.Position))
                {
                    TopLeft.Insert(body);
                }
                else if (TopRight.ContainsPoint(body.Position))
                {
                    TopRight.Insert(body);
                }
                else
                {
                    throw new ArgumentException($"Quad at {Bounds} does not contain {body.Position}");
                }

                return;
            }

            if(!HasNode)
            {
                Node = body;
                return;
            }

            var temp = Node;
            Node = null;

            Expand();

            Insert(temp);
            Insert(body);
        }
    }
}