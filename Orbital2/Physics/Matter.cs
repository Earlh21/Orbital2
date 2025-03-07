using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbital2.Physics;

public class Matter
{
    public float Mass { get; private set; }
    public float Density { get; private set; }
    public float HeatCapacity { get; private set; }

    public IReadOnlyList<float> Composition => composition.AsReadOnly();

    private float[] composition = [0];

    public const float HydrogenDensity = 10f;
    public const float HydrogenHeatCapacity = 10;

    public float Hydrogen
    {
        get => this[0];
        set => this[0] = value;
    }

    public Matter() { }

    public Matter(IReadOnlyList<float> composition)
    {
        SetComposition(composition);
    }

    public float this[int index]
    {
        get => composition[index];
        set
        {
            Mass += value - composition[index];
            composition[index] = value;

            Density = ComputeDensity();
            HeatCapacity = ComputeHeatCapacity();
        }
    }

    public void SetComposition(IReadOnlyList<float> composition)
    {
        if (composition.Count != this.composition.Length)
        {
            throw new ArgumentException("Incorrect number of elements given.");
        }

        for (int i = 0; i < composition.Count; i++)
        {
            this.composition[i] = composition[i];
        }

        Mass = composition.Sum();

        Density = ComputeDensity();
        HeatCapacity = ComputeHeatCapacity();
    }

    public void Add(Matter matter)
    {
        for (int i = 0; i < Composition.Count; i++)
        {
            composition[i] += matter.Composition[i];
        }

        Mass += matter.Mass;

        Density = ComputeDensity();
        HeatCapacity = ComputeHeatCapacity();
    }

    public void Multiply(float value)
    { 
        for(int i = 0; i < Composition.Count; i++)
        {
            composition[i] *= value;
        }

        Mass = composition.Sum();

        Density = ComputeDensity();
        HeatCapacity = ComputeHeatCapacity();
    }

    public static Matter operator *(Matter a, float b)
    {
        var matter = new Matter(a.Composition);
        matter.Multiply(b);
        return matter;
    }

    public static Matter operator *(float a, Matter b)
    {
        return b * a;
    }

    public static Matter operator +(Matter a, Matter b)
    {
        var matter = new Matter(a.Composition);
        matter.Add(b);
        return matter;
    }

    private float ComputeDensity()
    {
        return Hydrogen * HydrogenDensity / Mass;
    }

    private float ComputeHeatCapacity()
    {
        return Hydrogen * HydrogenHeatCapacity / Mass;
    }
}