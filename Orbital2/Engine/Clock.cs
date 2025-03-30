namespace Orbital2.Engine;

public class Clock
{
    public float TimeScale { get; set; } = 1;
    public float FixedTimeStep { get; set; }
    
    public float CurrentTime { get; private set; }
    
    public float DeltaTime { get; private set; }
    public float Accumulator { get; private set; }
    
    public float AccumulatorT => Accumulator / FixedTimeStep;
    
    public void Update(float deltaTime)
    {
        deltaTime *= TimeScale;
        
        DeltaTime = deltaTime;
        Accumulator += deltaTime;
        
        CurrentTime += deltaTime;
    }
    
    public bool DoFixedStep()
    {
        if (Accumulator >= FixedTimeStep)
        {
            Accumulator -= FixedTimeStep;
            return true;
        }
        
        return false;
    }
}