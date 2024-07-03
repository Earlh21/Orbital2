using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Physics
{
    public class World
    {
        private List<Body> bodies = [];

        public IReadOnlyList<Body> Bodies => bodies.AsReadOnly();

        public World()
        {

        }

        public void AddBody(Body body)
        {
            bodies.Add(body);
        }

        public void RemoveBody(Body body)
        {
            bodies.Remove(body);
        }

        public void Clear()
        {
            bodies.Clear();
        }

        public void Step(float timestep)
        {
            foreach (var body in bodies)
            {
                body.PreviousVelocity = body.Velocity;
                body.Velocity += body.Force * timestep / body.Mass;

                body.PreviousPosition = body.Position;
                body.Position = body.PreviousPosition + body.Velocity * timestep;

                body.PreviousForce = body.Force;
                body.Force = new();
            }
        }

        public void InterpolateLinear(float t)
        {
            if (t < 0)
            {
                t = 0;
            }

            if (t > 1)
            {
                t = 1;
            }

            foreach (var body in bodies)
            {
                body.InterpolatedPosition = body.PreviousPosition * (1 - t) + body.Position * t;
            }
        }
    }
}
