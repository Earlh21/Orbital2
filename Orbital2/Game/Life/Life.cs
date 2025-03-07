using Orbital2.Physics;

namespace Orbital2.Game.Life;

public class Life(Civilization civilization)
{
    public Civilization Civilization { get; set; } = civilization;
    public Matter RawMaterials { get; set; } = new();
    public int TechLevel { get; set; }

    public long Population
    {
        get => (long)population;
        set => population = value;
    }

    private float population;

    public void Step(int carryingCapacity, float timestep)
    {
        if (carryingCapacity == 0)
        {
            population -= population * 0.9f * timestep;
        }
        else
        {
            population += Civilization.GrowthRate * Population * (1 - Population / carryingCapacity) * timestep;
        }
    }

    public void HarvestMatter(Matter sourceMatter, float circumferenceManned, float timestep)
    {
        RawMaterials += sourceMatter * circumferenceManned * timestep * (0.0001f + TechLevel * 0.00001f);
    }

    public void DealDamage(int damage)
    {
        population -= damage;

        if (population < 0)
        {
            population = 0;
        }
    }
}