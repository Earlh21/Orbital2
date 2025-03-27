using System;
using System.Collections.Generic;
using System.Text;
using Orbital2.ML.Schema;

namespace Orbital2.ML.Training;

public struct TrainingData
{
    public FiringContext Context { get; }
    public FiringPrediction Prediction { get; }

    public TrainingData(FiringContext context, FiringPrediction prediction)
    {
        Context = context;
        Prediction = prediction;
    }

    public string ToCsvLine()
    {
        // Add ProjectileYNorm to CSV
        return $"{Context.TargetX},{Context.TargetY},{Context.TargetVelocityX},{Context.TargetVelocityY},{Prediction.ProjectileXNorm},{Prediction.ProjectileYNorm}";
    }

    public static TrainingData FromCsvLine(string csv)
    {
        var parts = csv.Split(',');
        if (parts.Length < 6) // Basic check for sufficient parts
        {
             // Handle error or return default/skip - depends on robustness needed
             // For now, let's throw an exception for clarity during debugging
             throw new ArgumentException($"CSV line has insufficient columns: '{csv}'");
        }
        return new TrainingData(
            new FiringContext(
                float.Parse(parts[0]),
                float.Parse(parts[1]),
                float.Parse(parts[2]),
                float.Parse(parts[3])
            ),
            // Parse both XNorm and YNorm
            new FiringPrediction(float.Parse(parts[4]), float.Parse(parts[5]))
        );
    }

    public static string ToCsv(IEnumerable<TrainingData> trainingData)
    {
        var sb = new StringBuilder();

        // Update header
        sb.AppendLine("TargetX,TargetY,TargetVelocityX,TargetVelocityY,ProjectileXNorm,ProjectileYNorm");

        foreach (var data in trainingData)
        {
            sb.AppendLine(data.ToCsvLine());
        }

        return sb.ToString();
    }

    public static IEnumerable<TrainingData> FromCsv(string csvText)
    {
        // Skip empty lines and handle potential final empty line from split
        var lines = csvText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Start from index 1 to skip header
        for (int i = 1; i < lines.Length; i++)
        {
             if (!string.IsNullOrWhiteSpace(lines[i])) // Ensure line isn't just whitespace
             {
                yield return FromCsvLine(lines[i].Trim()); // Trim whitespace
             }
        }
    }
}