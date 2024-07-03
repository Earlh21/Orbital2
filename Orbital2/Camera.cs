using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Orbital2
{
    internal class Camera
    {
        public Vector2 Center { get; set; }
        public float Zoom { get; set; }

        public Camera(Vector2 center, float zoom)
        {
            Center = center;
            Zoom = zoom;
        }

        public Camera(float center_x, float center_y, float zoom) : this(new Vector2(center_x, center_y), zoom)
        {

        }

        public Vector2 TransformToScreen(Vector2 world_vector, Rectangle screen_bounds)
        {
            world_vector = new(world_vector.X, -world_vector.Y);
            world_vector -= Center;
            world_vector *= Zoom;
            world_vector += new Vector2(screen_bounds.Width / 2, screen_bounds.Height / 2);

            return new(world_vector.X, world_vector.Y);
        }


        public Vector2 TransformToWorld(Vector2 point_vector, Rectangle screen_bounds)
        {
            point_vector -= new Vector2(screen_bounds.Width / 2, screen_bounds.Height / 2);
            point_vector /= Zoom;
            point_vector += new Vector2(Center.X, Center.Y);

            return new Vector2(point_vector.X, -point_vector.Y);
        }
    }
}
