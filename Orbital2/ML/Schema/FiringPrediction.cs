using System.Collections.Generic;
using System.Linq;
using TorchSharp;

namespace Orbital2.ML.Schema;

public struct FiringPrediction
{
    public float ProjectileXNorm { get; set; }
        
    public FiringPrediction()
    {
    }
        
    public FiringPrediction(float projectileXNorm)
    {
        ProjectileXNorm = projectileXNorm;
    }
    
    public static torch.Tensor ToTensor(IEnumerable<FiringPrediction> predictions)
    {
        var data = predictions.ToArray();
        var array = new float[data.Length, 1];
        
        for (int i = 0; i < data.Length; i++)
        {
            array[i, 0] = data[i].ProjectileXNorm;
        }
        
        return torch.tensor(array);
    }
}