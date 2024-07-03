using Microsoft.Xna.Framework;
using Orbital2.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.GameObjects
{
    public class CollisionMarker(Vector2 position, float radius, float lifespan) : GameObject 
    {
        public Vector2 Position { get; } = position;
        public float Radius { get; } = radius;
        public float Lifespan { get; } = lifespan;

        private float time;

        public override void FrameUpdate(float timestep, Engine.Engine game)
        {
            time += timestep;

            if(time > Lifespan)
            {
                game.RemoveObject(this);
            }
        }
    }
}
