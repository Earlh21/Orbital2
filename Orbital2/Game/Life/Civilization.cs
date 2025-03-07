namespace Orbital2.Game.Life;

public class Civilization(string name)
{
    public float TechLevel { get; set; }
    public string Name { get; set; } = name;
    public float GrowthRate { get; set; } = 1;
}