namespace Orbital2.Game.Life;

public abstract class Propulsion
{
    public abstract float MaxForce { get; }

    public abstract float GetImpulseLeft(Resources? resources);
    public abstract float ExtractImpulse(Resources? resources, float amount);
}