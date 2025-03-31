using Microsoft.Xna.Framework;
using Orbital2.Engine;

namespace Orbital2.Game.Life;

public class Ship(Vector2 position, Vector2 velocity) : PhysicalGameObject(new(position, new() { Hydrogen = 1f }) { Momentum = velocity * 1f }) 
{
    public Propulsion? Propulsion { get; set; }
    public Resources? Resources { get; set; }
    
    protected void PropulseDirection(float timestep, Vector2 direction)
    {
        if (Propulsion == null) return;

        float force = Propulsion.MaxForce;
        float impulseExtracted = Propulsion.ExtractImpulse(Resources, force * timestep);

        force = impulseExtracted / timestep;

        Body.ApplyForce(force * direction);
    }
}