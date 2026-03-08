# MINI-RGBD Dataset Integration Guide

This guide explains how to integrate the MINI-RGBD dataset you've downloaded with CP-sight.

## Dataset Sources

You've downloaded the MINI-RGBD dataset from three sources:

### 1. Fraunhofer IOSB (Primary Source)
- **Official Source**: https://argmax.ai/data/mini-rgbd/
- **Contents**: Original synthetic infant pose sequences
- **Size**: ~7GB

### 2. OpenDataLab Mirror
- **URL**: https://opendatalab.com/MINI-RGBD
- **Contents**: Same dataset with easier download interface
- **Requires**: Free account registration

### 3. Pre-extracted Poses
- **Contents**: Already processed pose data in JSON format
- **Use Case**: Direct use without pose estimation step

## Directory Structure Setup

After downloading, organize the datasets as follows:

```
/home/z/my-project/datasets/
├── MINI-RGBD/
│   ├── raw/                    # Original video/depth sequences
│   │   ├── train/
│   │   └── test/
│   ├── poses/                  # Pre-extracted pose files
│   │   ├── train/
│   │   └── test/
│   └── metadata/
│       └── labels.csv          # Classification labels
├── processed/
│   ├── train/
│   │   ├── normal/
│   │   ├── poor_repertoire/
│   │   ├── cramped_synchronized/
│   │   └── absent_fidgety/
│   └── val/
│       └── (same structure)
└── README.md
```

## Processing Steps

### Step 1: Extract Data from Archives

```bash
# Navigate to datasets folder
cd /home/z/my-project/datasets

# Extract downloaded archives
unzip MINI-RGBD_v1.0.zip -d MINI-RGBD/
tar -xzf mini-rgbd-poses.tar.gz -C MINI-RGBD/poses/
```

### Step 2: Run Python Preprocessing

Use the provided Python script to convert raw data to CP-sight format:

```bash
cd /home/z/my-project/python/data-prep
pip install -r requirements.txt
python scripts/prepare_training_data.py \
    --input /home/z/my-project/datasets/MINI-RGBD \
    --output /home/z/my-project/datasets/processed
```

### Step 3: Generate Training Features

```bash
# Generate features for ML.NET training
python scripts/generate_features.py \
    --poses /home/z/my-project/datasets/MINI-RGBD/poses \
    --output /home/z/my-project/dotnet/CP-sight.ML/Data/training_data.csv
```

## Integration with .NET Project

### Update CP-sight.ML Project

1. Copy processed poses to the ML project:

```bash
cp -r /home/z/my-project/datasets/processed/* /home/z/my-project/dotnet/CP-sight.ML/Data/
```

2. Update the `MovementClassifier` to load training data from CSV:

```csharp
// In MovementClassifier.cs
public void LoadTrainingDataFromCsv(string csvPath)
{
    var data = _mlContext.Data.LoadFromTextFile<MovementFeaturesInput>(
        csvPath, 
        hasHeader: true, 
        separatorChar: ',');
    
    // Train with real data
    var pipeline = CreatePipeline();
    _model = pipeline.Fit(data);
}
```

### Configure Data Path in appsettings.json

```json
{
  "ML": {
    "TrainingDataPath": "Data/training_data.csv",
    "ModelPath": "Models/movement_model.zip"
  }
}
```

## Training the Model

Once data is integrated:

```bash
cd /home/z/my-project/dotnet

# Build the project
dotnet build

# Run training (if you create a training console app)
dotnet run --project CP-sight.ML.Training
```

## Using Pre-extracted Poses for Testing

The pre-extracted poses can be used directly for testing:

```csharp
// Load a test pose sequence
var poseJson = File.ReadAllText("datasets/processed/val/normal/sample_0005.json");
var poseData = JsonSerializer.Deserialize<List<PoseFrame>>(poseJson);

// Run analysis
var features = featureExtractor.ExtractFeatures(poseData);
var prediction = classifier.Predict(features);
```

## Label Mapping

MINI-RGBD labels map to GMA classifications as follows:

| MINI-RGBD Label | GMA Classification | Description |
|-----------------|-------------------|-------------|
| `normal` | Normal | Typical fidgety movements |
| `abnormal_1` | Poor Repertoire | Low complexity, monotonic |
| `abnormal_2` | Cramped-Synchronized | Rigid, simultaneous |
| `abnormal_3` | Absent Fidgety | No fidgety movements |

## Notes

- **Synthetic Data**: MINI-RGBD uses synthetic infant models, not real infants
- **Validation**: Always validate model performance on real clinical data
- **Ethics**: Real infant data requires IRB approval and parental consent
- **Clinical Use**: This is for research/screening only, not diagnosis

## Troubleshooting

### Issue: Missing pose files
**Solution**: Ensure you downloaded the "Pre-extracted poses" package separately from the raw videos.

### Issue: Format mismatch
**Solution**: Run the preprocessing script to convert to CP-sight format.

### Issue: Training fails
**Solution**: Check that your CSV has proper headers: `Complexity,Variability,Symmetry,FidgetyScore,Label`
