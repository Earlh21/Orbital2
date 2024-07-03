using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Physics.Gravity
{
    public abstract class SelectiveGravitySolver : GravitySolver
    {
        public abstract Vector2[] ComputeAccelerationsSelective(IReadOnlyList<Body> affected_bodies, IReadOnlyList<Body> affector_bodies);
    }
}
