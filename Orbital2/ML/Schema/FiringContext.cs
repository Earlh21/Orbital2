using System.Collections.Generic;
using System.Linq;
using TorchSharp;

namespace Orbital2.ML.Schema;

public struct FiringContext
{

    public float TargetX { get; set; }
    public float TargetY { get; set; }
    public float TargetVelocityX { get; set; }
    public float TargetVelocityY { get; set; }

    public FiringContext()
    {

    }

    public FiringContext(float targetX, float targetY, float targetVelocityX, float targetVelocityY)
    {
        TargetX = targetX;
        TargetY = targetY;
        TargetVelocityX = targetVelocityX;
        TargetVelocityY = targetVelocityY;
    }
    
    public static torch.Tensor ToTensor(IEnumerable<FiringContext> contexts)
    {
        var data = contexts.ToArray();
        var array = new float[data.Length, 4];
        
        for (int i = 0; i < data.Length; i++)
        {
            array[i, 0] = data[i].TargetX;
            array[i, 1] = data[i].TargetY;
            array[i, 2] = data[i].TargetVelocityX;
            array[i, 3] = data[i].TargetVelocityY;
        }
        
        return torch.tensor(array);
    }
}