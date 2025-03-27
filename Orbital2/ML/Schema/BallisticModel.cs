// --- START OF FILE BallisticModel.cs ---

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
    private static readonly int[] Layers = [40, 60, 60, 40];

    private readonly Module<Tensor, Tensor> sequential;

    public BallisticModel(string name) : base(name)
    {
        var modulesList = new List<Module<Tensor, Tensor>>();

        modulesList.Add(BatchNorm1d(4)); // Input features = 4

        modulesList.Add(Linear(4, Layers[0]));
        modulesList.Add(ReLU());

        for (int i = 0; i < Layers.Length - 1; i++)
        {
            modulesList.Add(Linear(Layers[i], Layers[i+1]));
            modulesList.Add(ReLU());
        }

        modulesList.Add(Linear(Layers.Last(), 2));

        sequential = Sequential(modulesList);

        RegisterComponents();
    }

    public override Tensor forward(Tensor input)
    {
        return sequential.forward(input);
    }

    // GetFiringAngle needs to interpret the (cos, sin) output
    public float GetFiringAngle(FiringContext context)
    {
        using var _ = torch.no_grad(); // Ensure no gradient tracking
        eval(); // Set model to evaluation mode

        // Get the [1, 2] tensor output for a single context
        using var outputTensor = forward(FiringContext.ToTensor(new[] { context }));

        // Extract cos and sin values
        float cosAngle = outputTensor[0, 0].ToSingle();
        float sinAngle = outputTensor[0, 1].ToSingle();

        // Use Atan2 to get the angle in radians (-pi to pi)
        return MathF.Atan2(sinAngle, cosAngle);
    }

    // GetAverageSquaredError needs to handle the [N, 2] tensors
    public string GetAverageSquaredError(IEnumerable<TrainingData> trainingData)
    {
        var data = trainingData.ToArray();
        if (!data.Any()) return "NaN (No data)";

        var contexts = FiringContext.ToTensor(data.Select(x => x.Context));
        // Predictions tensor is now [N, 2]
        var predictions = FiringPrediction.ToTensor(data.Select(x => x.Prediction));

        using var _ = torch.no_grad(); // Ensure no gradient tracking
        eval(); // Set model to evaluation mode

        using var output = forward(contexts); // Output is [N, 2]

        // MSELoss calculates element-wise squared error then averages
        // So it computes mean(((pred_cos - true_cos)^2 + (pred_sin - true_sin)^2) / 2)
        // which is appropriate.
        return MSELoss(reduction: Reduction.Mean).forward(output, predictions).ToSingle().ToString();
    }

    public void Train(IEnumerable<TrainingData> trainingData, IEnumerable<TrainingData> testData, int batchSize, int epochs)
    {
        var optimizer = optim.Adam(parameters());
        // MSELoss remains suitable for the (cos, sin) pair
        var loss = MSELoss(reduction: Reduction.Mean);

        var data = trainingData.ToArray();
        var testDataArray = testData.ToArray();
        if (!data.Any() || !testDataArray.Any())
        {
             Console.WriteLine("Error: Training or test data is empty.");
             return;
        }

        var contexts = FiringContext.ToTensor(data.Select(x => x.Context));
        // Predictions tensor is [N, 2]
        var predictions = FiringPrediction.ToTensor(data.Select(x => x.Prediction));

        // Dataset expects tensors of the same first dimension size
        var dataset = torch.utils.data.TensorDataset(contexts, predictions);
        var dataLoader = torch.utils.data.DataLoader(dataset, batchSize, shuffle: true, drop_last: true); // drop_last can help with BatchNorm stability

        Console.WriteLine($"Starting training for {epochs} epochs...");
        for (int i = 0; i < epochs; i++)
        {
            train(); // Set model to training mode
            double epochLoss = 0;
            long batchCount = 0;

            foreach (var batch in dataLoader)
            {
                var batchData = batch[0]; // Input contexts [batchSize, 4]
                var batchExpected = batch[1]; // Target (cos, sin) [batchSize, 2]

                optimizer.zero_grad(); // Clear previous gradients

                using var output = forward(batchData);
                var batchLoss = loss.forward(output, batchExpected);

                if (float.IsNaN(batchLoss.ToSingle()) || float.IsInfinity(batchLoss.ToSingle()))
                {
                    Console.WriteLine($"Warning: NaN or Infinity loss detected in epoch {i + 1}. Skipping batch.");
                    continue; // Skip backpropagation if loss is invalid
                }


                batchLoss.backward(); // Compute gradients
                optimizer.step(); // Update weights

                epochLoss += batchLoss.ToSingle();
                batchCount++;
            }

            if (batchCount > 0)
            {
                 epochLoss /= batchCount;
            }
            else
            {
                 Console.WriteLine($"Warning: No batches processed in epoch {i + 1}.");
            }


            // Evaluate on test data
            var testErrorStr = GetAverageSquaredError(testDataArray);

            Console.WriteLine($"Epoch {i + 1}/{epochs} - Avg Train Loss: {epochLoss:F6} - Test MSE: {testErrorStr}");

            // Dispose tensors from dataset/loader to free memory if needed, though usually managed okay
            // It might be beneficial to explicitly dispose contexts, predictions after use if memory is tight.
        }
         // Dispose tensors after training loop finishes
         contexts.Dispose();
         predictions.Dispose();
         dataset.Dispose();
         dataLoader.Dispose();
         loss.Dispose();
         optimizer.Dispose();
    }
}
// --- END OF FILE BallisticModel.cs ---