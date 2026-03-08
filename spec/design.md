# CP-sight Technical Design

## 1. System Architecture

### 1.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CP-sight System                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────┐     ┌──────────────┐     ┌──────────────────────────────┐ │
│  │   Browser    │────▶│  Blazor      │────▶│   ASP.NET Core 9 API        │ │
│  │   (Client)   │◀────│  Server UI   │◀────│   (Minimal APIs)             │ │
│  └──────────────┘     └──────────────┘     └──────────────────────────────┘ │
│                                    │                        │               │
│                                    ▼                        ▼               │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                         Application Services                          │  │
│  ├──────────────────┬──────────────────┬──────────────────┬─────────────┤  │
│  │ CloudinaryService│  PoseService     │ FeatureExtractor │ReportGenerator│
│  │ (Video Upload)   │  (MoveNet/MP)    │ (ML Features)    │ (QuestPDF)  │  │
│  └──────────────────┴──────────────────┴──────────────────┴─────────────┘  │
│                                    │                        │               │
│                                    ▼                        ▼               │
│  ┌──────────────────────┐  ┌──────────────────┐  ┌──────────────────────┐  │
│  │    Cloudinary Cloud  │  │   ONNX Runtime   │  │   ML.NET Context     │  │
│  │  (Video Storage)     │  │ (MoveNet Model)  │  │  (Classification)    │  │
│  └──────────────────────┘  └──────────────────┘  └──────────────────────┘  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 Project Structure

```
CP-sight/
├── CP-sight.sln                      # Visual Studio Solution
├── README.md                          # Project documentation
├── spec/                              # Specification documents
│   ├── requirements.md                # Functional/non-functional requirements
│   ├── design.md                      # This file
│   ├── tasks.md                       # Development tasks
│   └── context.md                     # Clinical context
├── dotnet/
│   ├── CP-sight.Core/                 # Domain Layer
│   │   ├── CP-sight.Core.csproj
│   │   ├── Models/
│   │   │   └── Models.cs              # Domain entities
│   │   └── Services/
│   │       └── FeatureExtractor.cs    # Movement feature extraction
│   │
│   ├── CP-sight.ML/                   # Machine Learning Layer
│   │   ├── CP-sight.ML.csproj
│   │   └── Services/
│   │       ├── MoveNetPoseEstimator.cs    # MoveNet ONNX inference
│   │       ├── MediaPipePoseEstimator.cs  # MediaPipe bridge
│   │       ├── PoseService.cs             # Unified pose service
│   │       └── MovementClassifier.cs      # ML.NET classification
│   │
│   ├── CP-sight.Web/                  # Presentation Layer
│   │   ├── CP-sight.Web.csproj
│   │   ├── Program.cs                 # App entry point & DI config
│   │   ├── appsettings.json           # Configuration
│   │   ├── _Imports.razor             # Global usings
│   │   ├── App.razor                  # Root component
│   │   ├── Models/                    # ONNX model folder
│   │   │   └── README.md
│   │   ├── Components/
│   │   │   ├── Layout/
│   │   │   │   └── MainLayout.razor
│   │   │   ├── Pages/
│   │   │   │   ├── Home.razor
│   │   │   │   ├── Upload.razor
│   │   │   │   └── About.razor
│   │   │   ├── ResultsDisplay.razor
│   │   │   └── Routes.razor
│   │   ├── Services/
│   │   │   ├── CloudinaryService.cs
│   │   │   ├── RiskAssessmentService.cs
│   │   │   └── ReportGenerator.cs
│   │   └── wwwroot/
│   │       ├── css/app.css
│   │       └── index.html
│   │
│   └── CP-sight.Tests/                # Test Project
│       ├── CP-sight.Tests.csproj
│       ├── FeatureExtractorTests.cs
│       └── MovementClassifierTests.cs
│
├── python/                            # Data Preprocessing
│   └── data-prep/
│       ├── requirements.txt
│       ├── README.md
│       ├── scripts/
│       │   └── prepare_training_data.py
│       └── sample_data/               # 50 sample sequences
│
└── datasets/                          # Dataset Documentation
    ├── DATASET_DOWNLOAD_INSTRUCTIONS.md
    ├── MINI-RGBD_DOWNLOAD_GUIDE.md
    ├── MINI-RGBD_INTEGRATION.md
    └── MINI-RGBD-paper.pdf
```

---

## 2. Domain Models

### 2.1 Core Entities (CP-sight.Core/Models/Models.cs)

```csharp
// Infant information for assessment context
public record InfantInfo
{
    public required int AgeWeeks { get; init; }           // Chronological age
    public int? CorrectedAgeWeeks { get; init; }          // For preterm infants
    public bool IsPreterm { get; init; }                  // Preterm birth flag
    public int? GestationalAgeAtBirth { get; init; }      // GA in weeks
    public required string[] RiskFactors { get; init; }   // Additional factors
}

// Single joint/keypoint position
public record JointPosition
{
    public required double X { get; init; }              // Normalized X (0-1)
    public required double Y { get; init; }              // Normalized Y (0-1)
    public double? Z { get; init; }                       // Depth (optional)
    public required double Confidence { get; init; }      // Detection confidence
}

// Single frame of pose data
public record PoseFrame
{
    public int FrameNumber { get; init; }                // Frame index
    public double Timestamp { get; init; }               // Time in seconds
    public required Dictionary<string, JointPosition> Joints { get; init; }
}

// Extracted movement features
public record MovementFeatures
{
    public double MovementComplexity { get; init; }      // Direction changes
    public double MovementVariability { get; init; }     // Speed variance
    public double LeftRightSymmetry { get; init; }       // Bilateral symmetry
    public double FidgetyScore { get; init; }            // GMA fidgety score
    public double CrampedSynchronizedScore { get; init; } // CS pattern score
    public double PoorRepertoireScore { get; init; }     // PR pattern score
    public double AvgMovementSpeed { get; init; }        // Average speed
    public double PeakMovementSpeed { get; init; }       // Maximum speed
    // ... additional features
}

// Risk assessment result
public record RiskAssessment
{
    public required string OverallRisk { get; init; }    // "low", "medium", "high"
    public required int RiskScore { get; init; }         // 0-100
    public required double Confidence { get; init; }     // 0-1
    public required FeatureBreakdown Breakdown { get; init; }
    public required string[] Recommendations { get; init; }
    public bool FollowUpRequired { get; init; }
}
```

### 2.2 17 Keypoint Skeleton (COCO Format)

```
Joint indices and names (MoveNet output order):
0: nose
1: left_eye
2: right_eye
3: left_ear
4: right_ear
5: left_shoulder
6: right_shoulder
7: left_elbow
8: right_elbow
9: left_wrist       ← Critical for GMA analysis
10: right_wrist     ← Critical for GMA analysis
11: left_hip
12: right_hip
13: left_knee
14: right_knee
15: left_ankle      ← Critical for GMA analysis
16: right_ankle     ← Critical for GMA analysis
```

---

## 3. Service Layer Design

### 3.1 PoseService (Unified Pose Estimation)

**Purpose:** Auto-select best available pose estimation method

**Priority Order:**
1. **MoveNet ONNX** (if model file exists) - Fast, accurate
2. **MediaPipe** (if Python bridge available) - More features
3. **Simulation** (fallback) - Statistically valid for demos

**Key Methods:**
```csharp
public class PoseService
{
    // Get current status
    public PoseServiceStatus GetStatus();
    
    // Extract pose from image
    public PoseEstimationResult ExtractPose(byte[] imageBytes);
    
    // Extract from multiple frames
    public List<PoseFrame> ExtractPosesFromFrames(List<byte[]> frames, double fps);
}
```

### 3.2 MoveNetPoseEstimator

**Purpose:** Real pose estimation using ONNX Runtime

**Model Details:**
- Input: 1×3×192×192 tensor (NCHW, normalized RGB)
- Output: 1×1×17×3 tensor (y, x, confidence per keypoint)
- Model Size: ~5MB ONNX

**Key Methods:**
```csharp
public class MoveNetPoseEstimator
{
    public bool IsRealML { get; }
    public Dictionary<string, JointPosition> ExtractPose(byte[] imageBytes);
    public List<PoseFrame> ExtractPosesFromFrames(List<byte[]> frames, double fps);
}
```

### 3.3 MediaPipePoseEstimator

**Purpose:** Alternative pose estimation via Python bridge

**MediaPipe Landmarks:** 33 keypoints (maps to 17 COCO)

**Key Methods:**
```csharp
public class MediaPipePoseEstimator
{
    public bool IsRealML { get; }
    public Dictionary<string, JointPosition> ExtractPose(byte[] imageBytes);
    public Dictionary<string, JointPosition> ExtractPoseFromLandmarks(
        IEnumerable<(float x, float y, float z, float visibility)> landmarks);
}
```

### 3.4 CloudinaryService

**Purpose:** Video upload, storage, and frame extraction

**Key Methods:**
```csharp
public class CloudinaryService
{
    // Video operations
    Task<VideoUploadResult> UploadVideoAsync(Stream videoStream, string publicId);
    Task<bool> DeleteVideoAsync(string publicId);
    Task<VideoInfo> GetVideoInfoAsync(string publicId);
    
    // Frame extraction
    Task<List<FrameData>> ExtractFramesAsync(string publicId, int fps);
    string GetFrameUrl(string publicId, double timestampSeconds);
    Task<byte[]> DownloadFrameAsync(string frameUrl);
    
    // URLs
    string GetThumbnailUrl(string publicId, int width, int height);
    string GetSignedVideoUrl(string publicId, int expiresInSeconds);
}
```

### 3.5 FeatureExtractor

**Purpose:** Extract movement features from pose sequence

**Algorithm Details:**

```
MovementComplexity:
  - Count direction changes in wrist movement
  - Normalize by frame count
  - Higher = more complex (normal)

MovementVariability:
  - Calculate velocity variance across all limb pairs
  - Standard deviation of movement speeds
  - Higher = more variable (normal)

LeftRightSymmetry:
  - Compare left vs right wrist movement distances
  - 1 - |left_move - right_move| / max(left_move, right_move)
  - Very high (>0.9) = suspicious (cramped-synchronized)

FidgetyScore:
  - Complexity * 0.6 + Variability * 0.4 (normalized)
  - Critical for 9-20 week age range
  - Low score = absent fidgety movements (high CP risk)
```

### 3.6 MovementClassifier

**Purpose:** Classify movement patterns using ML.NET

**Model Architecture:**
```
Input: [Complexity, Variability, Symmetry, FidgetyScore]
       ↓
Concatenate Features
       ↓
Normalize (MinMax)
       ↓
SDCA Maximum Entropy (Multiclass)
       ↓
Output: [normal, poor_repertoire, cramped_synchronized, absent_fidgety]
```

### 3.7 RiskAssessmentService

**Purpose:** Clinical risk assessment based on GMA research

**Risk Scoring Algorithm:**
```
Base Score from Classification:
  cramped_synchronized: 0.90
  absent_fidgety: 0.85
  poor_repertoire: 0.60
  normal: 0.15
  Weight: 40%

Fidgety Movement Adjustment (if age 9-20 weeks):
  Fidgety < 0.3: +25% (severely reduced)
  Fidgety < 0.5: +15% (reduced)

Movement Complexity:
  Complexity < 0.3: +15%

Movement Variability:
  Variability < 0.02: +10%

Symmetry Assessment:
  Symmetry > 0.9: +10% (cramped-synchronized pattern)

Final Score: min(1.0, sum of all factors)
Risk Level: < 0.35 = low, > 0.65 = high, else medium
```

---

## 4. Data Flow

### 4.1 Complete Analysis Pipeline

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         COMPLETE ANALYSIS PIPELINE                          │
└─────────────────────────────────────────────────────────────────────────────┘

Step 1: User Input
┌─────────────────┐
│ Browser uploads │────▶ IFormFile (video)
│ infant video +  │────▶ InfantInfo { AgeWeeks, IsPreterm, ... }
│ metadata        │
└─────────────────┘
         │
         ▼
Step 2: Video Processing (CloudinaryService)
┌─────────────────┐
│ Upload to       │────▶ VideoUploadResult { PublicId, SecureUrl, Duration }
│ Cloudinary      │
└─────────────────┘
         │
         ▼
Step 3: Frame Extraction (CloudinaryService)
┌─────────────────┐
│ Extract frames  │────▶ List<FrameData> [{ FrameNumber, Timestamp, Url }]
│ at 30 FPS       │
└─────────────────┘
         │
         ▼
Step 4: Pose Estimation (PoseService → MoveNet/MediaPipe/Simulation)
┌─────────────────┐
│ ONNX inference  │────▶ List<PoseFrame> [{ Joints: { X, Y, Confidence } }]
│ on each frame   │
└─────────────────┘
         │
         ▼
Step 5: Feature Extraction (FeatureExtractor)
┌─────────────────┐
│ Calculate       │────▶ MovementFeatures { Complexity, Variability, ... }
│ movement scores │
└─────────────────┘
         │
         ▼
Step 6: ML Classification (MovementClassifier)
┌─────────────────┐
│ ML.NET predict  │────▶ RiskPrediction { Classification, Scores[] }
│ from features   │
└─────────────────┘
         │
         ▼
Step 7: Risk Assessment (RiskAssessmentService)
┌─────────────────┐
│ Clinical risk   │────▶ RiskAssessment { OverallRisk, RiskScore, ... }
│ calculation     │
└─────────────────┘
         │
         ▼
Step 8: Report Generation (ReportGenerator - QuestPDF)
┌─────────────────┐
│ QuestPDF        │────▶ byte[] PDF file
│ generation      │
└─────────────────┘
         │
         ▼
Step 9: Result Delivery
┌─────────────────┐
│ Blazor UI       │────▶ ResultsDashboard component
│ displays result │────▶ Download PDF button
└─────────────────┘
```

---

## 5. API Design

### 5.1 REST Endpoints

```
POST /api/upload
  Request: IFormFile video (multipart/form-data)
  Response: { success, publicId, url, duration, width, height, format }
  
GET /api/pose/status
  Response: { moveNetAvailable, mediaPipeAvailable, usingSimulation, activeMethod }

POST /api/pose/estimate
  Request: IFormFile image
  Response: { success, method, isRealML, processingTimeMs, joints }
  
POST /api/analyze
  Request: IFormFile video, int ageWeeks, bool isPreterm, ...
  Response: Complete analysis result
  
POST /api/extract-frames
  Request: { publicId, fps }
  Response: { publicId, frameCount, frames: [{ frameNumber, timestamp, url }] }
  
GET /api/video/{publicId}
  Response: { publicId, duration, width, height, format, frameRate, bitRate }
  
DELETE /api/video/{publicId}
  Response: { success: true } | { error: "Not found" }
  
GET /health
  Response: { status: "Healthy" }
  
GET /api
  Response: { name, version, endpoints: [...] }
```

---

## 6. Configuration

### 6.1 appsettings.json Structure

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  
  "Cloudinary": {
    "CloudName": "dnvw0yyib",
    "ApiKey": "887823313431246",
    "ApiSecret": "3X6o4aSOwBLH0_cJkLEg4XtFp14"
  },
  
  "PoseEstimation": {
    "ModelPath": "Models/movenet.onnx",
    "UseMediaPipe": false
  },
  
  "Analysis": {
    "DefaultFps": 30,
    "MaxVideoSizeMB": 100
  }
}
```

### 6.2 Dependency Injection (Program.cs)

```csharp
// UI Framework
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();

// HTTP Client
builder.Services.AddHttpClient();

// Configuration
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("Cloudinary"));
builder.Services.Configure<PoseServiceSettings>(options => { ... });

// Services
builder.Services.AddSingleton<PoseService>();      // Singleton - loads model once
builder.Services.AddScoped<CloudinaryService>();
builder.Services.AddScoped<FeatureExtractor>();
builder.Services.AddScoped<MovementClassifier>();
builder.Services.AddScoped<RiskAssessmentService>();
builder.Services.AddScoped<ReportGenerator>();

// Health Checks
builder.Services.AddHealthChecks();
```

---

## 7. Cloudinary Transformations

### 7.1 Frame Extraction URL

```
https://res.cloudinary.com/dnvw0yyib/video/upload/
  so_5.0,           // Start offset: 5 seconds
  w_640,h_360,      // Resize: 640x360
  c_fill,           // Crop mode: fill
  q_auto:good,      // Quality: auto good
  f_jpg             // Format: JPEG
  {public_id}.jpg
```

### 7.2 Thumbnail URL

```
https://res.cloudinary.com/dnvw0yyib/video/upload/
  w_400,h_300,
  c_fill,
  q_auto,
  f_jpg
  {public_id}.jpg
```

---

## 8. Security Considerations

### 8.1 API Credentials

- Cloudinary credentials in `appsettings.json` for development
- For production: Use environment variables or Azure Key Vault
- API Secret should NEVER be in source control

### 8.2 Video Privacy

- Signed URLs with time-limited expiration
- DELETE endpoint for video removal after analysis
- GDPR compliance consideration

---

## 9. Future Enhancements

### 9.1 Short Term

1. Image preprocessing with SixLabors.ImageSharp
2. GPU acceleration for ONNX inference
3. Background job processing with Hangfire

### 9.2 Medium Term

1. Database persistence for analysis history
2. Batch video processing
3. Docker containerization

### 9.3 Long Term

1. Mobile app (Flutter/React Native)
2. HL7 FHIR integration
3. Multi-site clinical validation
