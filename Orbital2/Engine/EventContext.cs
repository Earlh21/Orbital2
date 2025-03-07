using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Engine;

public class EventContext(GameWorld world)
{
    public GameWorld World { get; set; } = world;
    public Input Input { get; set; } = new();
}