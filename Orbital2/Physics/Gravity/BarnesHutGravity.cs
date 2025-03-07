using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Physics.Gravity;

public class BarnesHutGravity : SelectiveGravitySolver
{
    public float Theta { get; set; } = 0.7f;

    public override Vector2[] ComputeAccelerations(IReadOnlyList<Body> bodies)
    {
        return ComputeAccelerationsSelective(bodies, bodies);
    }

    public override Vector2[] ComputeAccelerationsSelective(IReadOnlyList<Body> affectedBodies, IReadOnlyList<Body> affectorBodies)
    {
        if(affectorBodies.Count == 0)
        {
            return new Vector2[affectedBodies.Count];
        }

        var tree = new QuadTree(affectorBodies);
        Vector2[] accels = new Vector2[affectedBodies.Count];

        Parallel.ForEach(affectedBodies, (body, _, i) =>
        {
            accels[i] = ComputeAccelerationHelper(tree.Root, body);
        });

        return accels;
    }

    private Vector2 ComputeAccelerationHelper(QuadTree.Quad quad, Body body)
    {
        if (quad.IsLeaf)
        {
            if (quad.HasNode)
            {
                if(quad.Node == body)
                {
                    return new();
                }

                return ComputeAccelerationFromMass(quad.CenterOfMass, body);
            }
            else
            {
                return new();
            }
        }

        if(MathF.Max(quad.Bounds.Width, quad.Bounds.Height) / (body.Position - quad.CenterOfMass.Position).Length() < Theta)
        {
            return ComputeAccelerationFromMass(quad.CenterOfMass, body);
        }

        return ComputeAccelerationHelper(quad.BottomLeft, body)
               + ComputeAccelerationHelper(quad.BottomRight, body)
               + ComputeAccelerationHelper(quad.TopLeft, body)
               + ComputeAccelerationHelper(quad.TopRight, body);
    }

    private Vector2 ComputeAccelerationFromMass(QuadTree.CenterOfMass mass, Body body)
    {
        Vector2 disp = mass.Position - body.Position;
        return GravitationalConstant * mass.Mass * disp / disp.LengthSquared();
    }
}