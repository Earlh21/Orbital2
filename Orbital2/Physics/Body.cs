using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Physics
{
    public class Body
    {
        public Vector2 Force { get; set; }
        public Vector2 Velocity { get; set; }
        public Vector2 Position { get; internal set; }
        public float Mass { get; set; } = 1;
        public float Radius { get; set; } = 1;

        public Vector2 InterpolatedPosition { get; internal set; }
        public Vector2 PreviousPosition { get; internal set; }
        public Vector2 PreviousVelocity { get; internal set; }
        public Vector2 PreviousForce { get; internal set; }

        public Body(Vector2 position)
        {
            PreviousPosition = position;
            Position = position;
            InterpolatedPosition = position;
        }

        public void Translate(Vector2 displacement)
        {
            PreviousPosition += displacement;
            Position += displacement;
            InterpolatedPosition += displacement;
        }

        private float ClosestTOnLineToPoint(Vector2 line_start, Vector2 line_end, Vector2 p)
        {
            Vector2 v = line_end - line_start;
            Vector2 u = line_start - p;

            float t = -v.Dot(u) / v.Dot(v);

            if(t < 0)
            {
                return 0;
            }

            if (t > 1)
            {
                return 1;
            }

            return t;
        }

        private float ClosestTOnLineToOrigin(Vector2 line_start, Vector2 line_end)
        {
            Vector2 v = line_end - line_start;

            float t = -v.Dot(line_start) / v.Dot(v);

            if (t < 0)
            {
                return 0;
            }

            if (t > 1)
            {
                return 1;
            }

            return t;
        }

        public float? GetCollisionT(Body other)
        {
            //Switch to the other body's reference frame
            //This lets us treat the other body as stationary (and at the origin)
            //No need to store the shifted velocity, it's only used once
            Vector2 pos_start = PreviousPosition - other.PreviousPosition;
            Vector2 pos_end = Position - other.Position;

            float closest_t = ClosestTOnLineToOrigin(pos_start, pos_end);
            Vector2 closest_pos = pos_start * (1 - closest_t) + pos_end * closest_t;

            //Check for a collision and store these values for later
            float b_squared = closest_pos.LengthSquared();
            float c_squared = (Radius + other.Radius) * (Radius + other.Radius);

            if (b_squared > c_squared) return null;

            //Use Pythagorean theorem to determine the distance between their closest point and the first contact point
            float a = MathF.Sqrt(c_squared - b_squared);

            //Prevent div by 0 when they exactly collide at their closest point
            if (a == 0) return closest_t;

            //Transform back to t, no need to worry about reference frame since we're returning the time of collision
            float t_diff = a / (Velocity - other.Velocity).Length();
            return closest_t - t_diff;
        }

        public bool IsOverlapping(Body other)
        {
            return (Position - other.Position).LengthSquared() < (Radius + other.Radius) * (Radius + other.Radius);
        }
    }
}
