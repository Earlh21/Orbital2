using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Engine;

public abstract class GameObject
{
    public virtual void OnStart(EventContext context) { }
    public virtual void OnRemove(EventContext context) { }
    public virtual void OnFrameUpdate(float timestep, EventContext context) { }
    public virtual void PostPhysicsUpdate(float physicsTimestep, EventContext context) { }
    public virtual void PrePhysicsUpdate(float physicsTimestep, EventContext context) { }
}