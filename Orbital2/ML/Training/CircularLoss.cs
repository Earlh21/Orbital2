using System;
using TorchSharp;

namespace Orbital2.ML.Training;

public class CircularLoss(torch.nn.Reduction reduction = torch.nn.Reduction.Mean) : 
    Loss<torch.Tensor, torch.Tensor, torch.Tensor>(reduction)
{
    private float min;
    private float max;

    public CircularLoss(float min, float max) : this()
    {
        this.min = min;
        this.max = max;
    }
    
    public override torch.Tensor forward(torch.Tensor input1, torch.Tensor input2)
    {
        throw new NotImplementedException();
    }
}