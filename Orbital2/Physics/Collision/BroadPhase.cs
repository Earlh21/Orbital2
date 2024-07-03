using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Physics.Collision
{
    public abstract class BroadPhase
    {
        public abstract List<Tuple<Body, Body>> FindPotentialCollisions(IReadOnlyList<Body> bodies);
    }
}
