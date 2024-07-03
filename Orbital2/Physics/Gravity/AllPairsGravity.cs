using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Physics.Gravity
{
    public class AllPairsGravity : SelectiveGravitySolver
    {
        public override Vector2[] ComputeAccelerations(IReadOnlyList<Body> bodies)
        {
            var accels = new Vector2[bodies.Count];

            Parallel.ForEach(bodies, (body, _, i) =>
            {
                foreach (Body b in bodies)
                {
                    if (b == body) continue;

                    Vector2 disp = b.Position - body.Position;
                    accels[i] += GravitationalConstant * b.Mass * disp / disp.LengthSquared();
                }
            });

            return accels;
        }

        public override Vector2[] ComputeAccelerationsSelective(IReadOnlyList<Body> affected_bodies, IReadOnlyList<Body> affector_bodies)
        {
            Vector2[] accels = [];

            Parallel.ForEach(affected_bodies, (body, _, i) =>
            {
                foreach (Body b in affector_bodies)
                {
                    if (b == body) continue;

                    Vector2 disp = b.Position - body.Position;
                    accels[i] += GravitationalConstant * b.Mass * disp / disp.LengthSquared();
                }
            });

            return accels;
        }
    }
}
