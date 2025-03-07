namespace Orbital2.Game.Life;

internal class HydrogenThruster : Propulsion
{
    public override float MaxForce => 10f;

    public override float ExtractImpulse(Resources? resources, float amount)
    {
        if (resources == null) return 0;

        float impulseLeft = GetImpulseLeft(resources);
        if (amount > impulseLeft) amount = impulseLeft;

        resources.Matter.Hydrogen -= amount;
        return amount;
    }

    public override float GetImpulseLeft(Resources? resources)
    {
        return resources?.Matter.Hydrogen ?? 0;
    }
}