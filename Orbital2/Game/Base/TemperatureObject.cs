using Orbital2.Engine;
using Orbital2.Physics;

namespace Orbital2.Game.Base;

public abstract class TemperatureObject : PhysicalGameObject
{
    public const float EmissivityConstant = 0.01f;
    public float ThermalEnergy { get; set; } = 100;
    public float Emissivity { get; set; } = 0.9f;
    public float Temperature
    {
        get => ThermalEnergy / Body.Matter.HeatCapacity / Mass;
        set => ThermalEnergy = value * Body.Matter.HeatCapacity * Mass;
    }

    protected TemperatureObject(Body body, float temperature = 100) : base(body)
    {
        Temperature = temperature;
    }

    public override void PostPhysicsUpdate(float physicsTimestep, EventContext context)
    {
        ThermalEnergy -= EmissivityConstant * Emissivity * Radius * Temperature * Temperature * physicsTimestep;

        if (ThermalEnergy < 0)
        {
            ThermalEnergy = 0;
        }
    }
}