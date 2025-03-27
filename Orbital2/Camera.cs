using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Orbital2.Physics;

namespace Orbital2;

public class Camera
{
    public Vector2 Center { get; set; }
    public float Zoom { get; set; }

    public Camera(Vector2 center, float zoom)
    {
        Center = center;
        Zoom = zoom;
    }

    public Camera(float centerX, float centerY, float zoom) : this(new Vector2(centerX, centerY), zoom)
    {

    }

    public Bounds GetWorldBounds(Rectangle screenBounds)
    {
        float worldWidth = screenBounds.Width / Zoom;
        float worldHeight = MathF.Abs(screenBounds.Height / Zoom);

        return new Bounds(Center.X - worldWidth / 2, -Center.Y - worldHeight / 2, Center.X + worldWidth / 2, -Center.Y + worldHeight / 2);
    }

    public Vector2 TransformToScreen(Vector2 worldVector, Rectangle screenBounds)
    {
        worldVector = new(worldVector.X, -worldVector.Y);
        worldVector -= Center;
        worldVector *= Zoom;
        worldVector += new Vector2(screenBounds.Width / 2.0f, screenBounds.Height / 2.0f);

        return new(worldVector.X, worldVector.Y);
    }
    
    public Vector2 TransformToClip(Vector2 worldVector, Rectangle screenBounds)
    {
        var screenVector = TransformToScreen(worldVector, screenBounds);
        
        screenVector.X -= screenBounds.Width / 2.0f;
        screenVector.Y -= screenBounds.Height / 2.0f;
        
        screenVector.X /= screenBounds.Width / 2.0f;
        screenVector.Y /= -screenBounds.Height / 2.0f;

        return screenVector;
    }

    public Vector2 TransformToWorld(Point point, Rectangle screenBounds)
    {
        return TransformToWorld(new Vector2(point.X, point.Y), screenBounds);
    }

    public Vector2 TransformToWorld(Vector2 pointVector, Rectangle screenBounds)
    {
        pointVector -= new Vector2(screenBounds.Width / 2.0f, screenBounds.Height / 2.0f);
        pointVector /= Zoom;
        pointVector += new Vector2(Center.X, Center.Y);

        return new Vector2(pointVector.X, -pointVector.Y);
    }
}