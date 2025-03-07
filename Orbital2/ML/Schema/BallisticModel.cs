using System;
using System.Collections.Generic;
using System.Linq;
using Orbital2.ML.Training;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;
using static TorchSharp.torch;
using Sequential = TorchSharp.Modules.Sequential;

namespace Orbital2.ML.Schema;

public class BallisticModel : Module<Tensor, Tensor>
{
    private static readonly int[] Layers = [40, 40];
    
    private readonly Module<Tensor, Tensor> sequential;
    
    public BallisticModel(string name) : base(name)
    {
        var modulesList = new List<Module<Tensor, Tensor>>();
        
        modulesList.Add(BatchNorm1d(4));
        
        modulesList.Add(Linear(4, Layers[0]));
        modulesList.Add(ReLU());
        
        for (int i = 0; i < Layers.Length - 1; i++)
        {
            modulesList.Add(Linear(Layers[i], Layers[i+1]));
            modulesList.Add(ReLU());
        }
        
        modulesList.Add(Linear(Layers.Last(), 1));

        sequential = Sequential(modulesList);
        
        RegisterComponents();
    }

    public override Tensor forward(Tensor input)
    {
        return sequential.forward(input);
    }
    
    public float GetFiringAngle(FiringContext context)
    {
        return forward(context.ToTensor()).ToSingle();
    }
    
    public string GetAverageSquaredError(IEnumerable<TrainingData> trainingData)
    {
        var data = trainingData.ToArray();
        
        var contexts = FiringContext.ToTensor(data.Select(x => x.Context));
        var predictions = FiringPrediction.ToTensor(data.Select(x => x.Prediction));
        
        using var output = forward(contexts);

        return MSELoss().forward(output, predictions).ToSingle().ToString();
    }
    
    public void Train(IEnumerable<TrainingData> trainingData, IEnumerable<TrainingData> testData, int batchSize, int epochs)
    {
        var optimizer = optim.Adam(parameters());
        var loss = MSELoss();

        var data = trainingData.ToArray();
        var testDataArray = testData.ToArray();
        
        var contexts = FiringContext.ToTensor(data.Select(x => x.Context));
        var predictions = FiringPrediction.ToTensor(data.Select(x => x.Prediction));

        var dataset = torch.utils.data.TensorDataset(contexts, predictions);
        var dataLoader = torch.utils.data.DataLoader(dataset, batchSize, true);
            
        
        for (int i = 0; i < epochs; i++)
        {
            train();
                
            foreach (var batch in dataLoader)
            {
                var batchData = batch[0];
                var batchExpected = batch[1];

                using var eval = forward(batchData);
                using var output = loss.forward(forward(batchData), batchExpected);

                zero_grad();

                output.backward();

                optimizer.step();
            }
            
            eval();
            
            var testError = GetAverageSquaredError(testDataArray);
            
            Console.WriteLine($"Epoch {i + 1}/{epochs} - Test Error: {testError}");
        }
    }
}