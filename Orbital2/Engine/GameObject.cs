using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Engine
{
    public abstract class GameObject
    {
        public virtual void FrameUpdate(float timestep, Engine game) { }
        public virtual void PostPhysicsUpdate(float physics_timestep, Engine game) { }
        public virtual void PrePhysicsUpdate(float physics_timestep, Engine game) { }
    }
}
