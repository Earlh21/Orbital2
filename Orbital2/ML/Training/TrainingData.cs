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
        return $"{Context.TargetX},{Context.TargetY},{Context.TargetVelocityX},{Context.TargetVelocityY},{Prediction.ProjectileXNorm}";
    }
    
    public static TrainingData FromCsvLine(string csv)
    {
        var parts = csv.Split(',');
        return new TrainingData(
            new FiringContext(
                float.Parse(parts[0]),
                float.Parse(parts[1]),
                float.Parse(parts[2]),
                float.Parse(parts[3])
            ),
            new FiringPrediction(float.Parse(parts[4]))
        );
    }

    public static string ToCsv(IEnumerable<TrainingData> trainingData)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("TargetX,TargetY,TargetVelocityX,TargetVelocityY,ProjectileXNorm,ProjectileYNorm");
        
        foreach (var data in trainingData)
        {
            sb.AppendLine(data.ToCsvLine());
        }
        
        return sb.ToString();
    }
    
    public static IEnumerable<TrainingData> FromCsv(string csvText)
    {
        var lines = csvText.Split('\n');
        
        for (int i = 1; i < lines.Length; i++)
        {
            yield return FromCsvLine(lines[i]);
        }
    }
}