# CP-sight

**AI-Powered Early Cerebral Palsy Screening Application**

---

## Overview

CP-sight is a .NET 9 application that implements automated General Movements Assessment (GMA) for early cerebral palsy screening. Based on Prechtl's validated methodology, it analyzes infant videos to detect abnormal movement patterns associated with CP risk.

---

## Tech Stack

| Component | Technology | Version |
|-----------|------------|---------|
| **Runtime** | .NET | 9.0 |
| **Web Framework** | ASP.NET Core Blazor Server | 9.0 |
| **UI Framework** | MudBlazor | 7.8.0 |
| **Machine Learning** | ML.NET | 4.0.0 |
| **Pose Estimation** | ONNX Runtime | 1.18.0 |
| **Image Processing** | SixLabors ImageSharp | 3.1.5 |
| **Video Processing** | Cloudinary | 1.26.2 |
| **PDF Generation** | QuestPDF | 2024.12.0 |
| **Testing** | xUnit + FluentAssertions + Moq | Latest |

---

## Features

### 🎥 Video Processing (Cloudinary)
- Upload infant movement videos (MP4, MOV, AVI, WebM)
- Automatic video transcoding and optimization
- Frame extraction at configurable FPS
- Thumbnail and preview generation
- Secure signed URLs with expiration
- Automatic video deletion for privacy compliance

### 🧠 Pose Estimation (MoveNet + MediaPipe)
- **MoveNet ONNX**: Fast, single-person pose estimation
- **MediaPipe**: Alternative with Python bridge support
- **Auto-fallback**: Simulation mode when model unavailable
- 17 keypoints in COCO format (wrists, ankles, shoulders, etc.)

### 📊 Movement Analysis (ML.NET)
- Movement Complexity (direction changes)
- Movement Variability (speed variance)
- Left-Right Symmetry (bilateral coordination)
- Fidgety Movement Score (GMA-specific)
- Cramped-Synchronized Detection
- Poor Repertoire Detection

### 📋 Risk Assessment (GMA Research-Based)
- Multi-class classification: Normal, Poor Repertoire, Cramped-Synchronized, Absent Fidgety
- Risk score (0-100) with confidence interval
- Age-adjusted assessment (corrected age for preterm)
- Clinical recommendations generation
- Follow-up guidance

### 📄 Report Generation (QuestPDF)
- Professional PDF reports
- Patient information section
- Risk assessment visualization
- Feature breakdown with status indicators
- Clinical recommendations
- Medical disclaimer

---

## Project Structure

```
CP-sight/
├── CP-sight.sln
├── README.md
├── spec/
│   ├── requirements.md      # Functional/non-functional requirements
│   ├── design.md            # Technical architecture
│   ├── tasks.md             # Development tasks
│   └── context.md           # Clinical background
├── dotnet/
│   ├── CP-sight.Core/       # Domain models, feature extraction
│   │   ├── Models/Models.cs
│   │   └── Services/FeatureExtractor.cs
│   ├── CP-sight.ML/         # ML services
│   │   └── Services/
│   │       ├── MoveNetPoseEstimator.cs
│   │       ├── MediaPipePoseEstimator.cs
│   │       ├── PoseService.cs
│   │       └── MovementClassifier.cs
│   ├── CP-sight.Web/        # Blazor Server app
│   │   ├── Services/
│   │   │   ├── CloudinaryService.cs
│   │   │   ├── RiskAssessmentService.cs
│   │   │   └── ReportGenerator.cs
│   │   ├── Components/Pages/
│   │   ├── Models/          # Place ONNX model here
│   │   └── appsettings.json
│   └── CP-sight.Tests/      # Unit tests
├── python/data-prep/        # Training data preparation
│   ├── scripts/prepare_training_data.py
│   └── sample_data/         # 50 sample sequences
└── datasets/
    ├── MINI-RGBD_DOWNLOAD_GUIDE.md
    └── MINI-RGBD-paper.pdf
```

---

## Quick Start

### Prerequisites
- **.NET 9 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Visual Studio 2022** (17.8+) or VS Code with C# extension
- **Cloudinary Account** (free tier)

### Setup

1. **Extract and navigate**
   ```bash
   tar -xzf CP-sight-NET9-Complete.tar.gz
   cd dotnet
   ```

2. **Add MoveNet ONNX Model**
   
   Download from: https://huggingface.co/Xenova/movenet-singlepose-lightning/blob/main/onnx/model.onnx
   
   Save as: `CP-sight.Web/Models/movenet.onnx`

3. **Build and run**
   ```bash
   dotnet restore
   dotnet build
   dotnet run --project CP-sight.Web
   ```

4. **Access application**
   ```
   https://localhost:5001
   ```

---

## Configuration

### Cloudinary (Video Processing)

Configured in `appsettings.json`:

```json
{
  "Cloudinary": {
    "CloudName": "dnvw0yyib",
    "ApiKey": "887823313431246",
    "ApiSecret": "3X6o4aSOwBLH0_cJkLEg4XtFp14"
  }
}
```

### Pose Estimation

```json
{
  "PoseEstimation": {
    "ModelPath": "Models/movenet.onnx",
    "UseMediaPipe": false
  }
}
```

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/upload` | Upload video to Cloudinary |
| POST | `/api/analyze` | Full analysis (video + infant info) |
| POST | `/api/pose/estimate` | Estimate pose from image |
| GET | `/api/pose/status` | Get pose estimation status |
| POST | `/api/extract-frames` | Extract frames from video |
| GET | `/api/video/{id}` | Get video metadata |
| DELETE | `/api/video/{id}` | Delete video |
| GET | `/health` | Health check |

---

## Pose Estimation Priority

The system automatically selects the best available method:

```
1. MoveNet ONNX (if model file exists)
      ↓ not available
2. MediaPipe (if enabled and Python bridge available)
      ↓ not available
3. Simulation (statistically valid for demos)
```

Check status via API:
```bash
curl https://localhost:5001/api/pose/status
# Returns: { "moveNetAvailable": true, "activeMethod": "MoveNet ONNX" }
```

---

## Clinical Background

### General Movements Assessment (GMA)

GMA evaluates spontaneous "general movements" - complex, whole-body movement patterns present from fetal life through early infancy.

**Critical Window:** 9-20 weeks (fidgety movements period)

| Classification | Description | CP Specificity |
|----------------|-------------|----------------|
| Normal | Variable, complex, fluent | < 5% |
| Poor Repertoire | Monotonous, simple | ~30% |
| Cramped-Synchronized | Rigid, simultaneous | ~95% |
| Absent Fidgety | No fidgety movements | >90% |

### Age Adjustment for Preterm

```
Corrected Age = Chronological Age - (40 - Gestational Age)
```

---

## Training Data

### Included Sample Data
- 50 pre-generated pose sequences
- 4 classifications (normal, poor_repertoire, cramped_synchronized, absent_fidgety)
- Train/validation split

### MINI-RGBD Dataset (Optional)
- Official source: https://argmax.ai/data/mini-rgbd/
- Size: ~7 GB
- Synthetic infant poses with ground truth
- See `datasets/MINI-RGBD_DOWNLOAD_GUIDE.md`

---

## Disclaimer

⚠️ **CP-sight is a research and screening tool only.**

It should not be used as the sole basis for clinical diagnosis. All assessments must be validated by qualified healthcare professionals trained in General Movements Assessment.

---

## References

1. Prechtl HFR et al. (1997). An early marker for neurological deficits after perinatal brain lesions. *Lancet*
2. Einspieler C et al. (2021). Prechtl's General Movements Assessment. *Clinics in Perinatology*
3. MINI-RGBD Dataset: https://argmax.ai/data/mini-rgbd/
4. MoveNet: https://blog.tensorflow.org/2021/05/next-generation-pose-detection-with-movenet-and-tensorflowjs.html
5. Cloudinary: https://cloudinary.com/documentation

---

**Built for Hackathon 2024** | AI-Powered Healthcare Innovation
