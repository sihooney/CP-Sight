using Microsoft.ML;
using Microsoft.ML.Data;
using CP_Sight.Core.Models;

namespace CP_Sight.ML.Services;

/// <summary>
/// ML.NET-based movement classifier for CP risk assessment
/// Trained on synthetic data based on GMA research thresholds
/// </summary>
public class MovementClassifier
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private PredictionEngine<MovementFeaturesInput, RiskPrediction>? _predictor;
    private readonly string _modelPath;

    public MovementClassifier(string? modelPath = null)
    {
        _mlContext = new MLContext(seed: 42);
        _modelPath = modelPath ?? "Models/movement_model.zip";
        
        if (File.Exists(_modelPath))
        {
            LoadModel();
        }
        else
        {
            TrainModel();
        }
    }

    public void TrainModel()
    {
        var trainingData = GenerateTrainingData();
        var trainingDataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = _mlContext.Transforms
            .Concatenate("Features", 
                "Complexity", "Variability", "Symmetry", "FidgetyScore")
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label"))
            .Append(_mlContext.MulticlassClassification.Trainers
                .SdcaMaximumEntropy("Label", "Features"))
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        _model = pipeline.Fit(trainingDataView);
        _predictor = _mlContext.Model
            .CreatePredictionEngine<MovementFeaturesInput, RiskPrediction>(_model);
        
        Directory.CreateDirectory(Path.GetDirectoryName(_modelPath)!);
        using var stream = File.Create(_modelPath);
        _mlContext.Model.Save(_model, trainingDataView.Schema, stream);
    }

    public void LoadModel()
    {
        using var stream = File.OpenRead(_modelPath);
        var model = _mlContext.Model.Load(stream, out var schema);
        _model = model;
        _predictor = _mlContext.Model
            .CreatePredictionEngine<MovementFeaturesInput, RiskPrediction>(model, schema);
    }

    public RiskPrediction Predict(MovementFeatures features)
    {
        _predictor ??= CreateDefaultPredictor();
        
        var input = new MovementFeaturesInput
        {
            Complexity = (float)features.MovementComplexity,
            Variability = (float)features.MovementVariability,
            Symmetry = (float)features.LeftRightSymmetry,
            FidgetyScore = (float)features.FidgetyScore
        };

        return _predictor.Predict(input);
    }

    private PredictionEngine<MovementFeaturesInput, RiskPrediction> CreateDefaultPredictor()
    {
        TrainModel();
        return _predictor!;
    }

    private List<MovementFeaturesInput> GenerateTrainingData()
    {
        var data = new List<MovementFeaturesInput>();
        var random = new Random(42);

        // Normal movements
        for (int i = 0; i < 200; i++)
        {
            data.Add(new MovementFeaturesInput
            {
                Complexity = (float)(0.6 + random.NextDouble() * 0.3),
                Variability = (float)(0.08 + random.NextDouble() * 0.07),
                Symmetry = (float)(0.75 + random.NextDouble() * 0.2),
                FidgetyScore = (float)(0.6 + random.NextDouble() * 0.3),
                Label = "normal"
            });
        }

        // Poor repertoire
        for (int i = 0; i < 150; i++)
        {
            data.Add(new MovementFeaturesInput
            {
                Complexity = (float)(0.2 + random.NextDouble() * 0.2),
                Variability = (float)(0.05 + random.NextDouble() * 0.05),
                Symmetry = (float)(0.5 + random.NextDouble() * 0.2),
                FidgetyScore = (float)(0.3 + random.NextDouble() * 0.2),
                Label = "poor_repertoire"
            });
        }

        // Cramped synchronized
        for (int i = 0; i < 150; i++)
        {
            data.Add(new MovementFeaturesInput
            {
                Complexity = (float)(0.1 + random.NextDouble() * 0.15),
                Variability = (float)(0.01 + random.NextDouble() * 0.03),
                Symmetry = (float)(0.85 + random.NextDouble() * 0.1),
                FidgetyScore = (float)(0.1 + random.NextDouble() * 0.15),
                Label = "cramped_synchronized"
            });
        }

        // Absent fidgety
        for (int i = 0; i < 150; i++)
        {
            data.Add(new MovementFeaturesInput
            {
                Complexity = (float)(0.1 + random.NextDouble() * 0.2),
                Variability = (float)(0.02 + random.NextDouble() * 0.04),
                Symmetry = (float)(0.4 + random.NextDouble() * 0.25),
                FidgetyScore = (float)(0.05 + random.NextDouble() * 0.15),
                Label = "absent_fidgety"
            });
        }

        return data;
    }
}

public class MovementFeaturesInput
{
    public float Complexity { get; set; }
    public float Variability { get; set; }
    public float Symmetry { get; set; }
    public float FidgetyScore { get; set; }
    public string Label { get; set; } = "normal";
}

public class RiskPrediction
{
    [ColumnName("PredictedLabel")]
    public string Classification { get; set; } = "";

    [ColumnName("Score")]
    public float[] Scores { get; set; } = Array.Empty<float>();
}
