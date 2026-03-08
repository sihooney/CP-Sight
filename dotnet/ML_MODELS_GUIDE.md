# AI/ML Models - Download Guide

## ⚠️ CRITICAL: What You Need for Real ML

CP-sight has **two ML components** that need different types of models:

```
┌────────────────────────────────────────────────────────────────────────────┐
│                        CP-sight ML Pipeline                                │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  VIDEO ──▶ POSE ESTIMATION ──▶ FEATURE EXTRACTION ──▶ RISK CLASSIFICATION  │
│            (Model #1)           (Algorithm)            (Model #2)          │
│                                                                            │
│  Model #1: Pose Estimation ONNX Model (MoveNet, OpenPose, MediaPipe)      │
│  Model #2: Movement Classifier (ML.NET - already built-in)                │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## Model #1: Pose Estimation (REQUIRED for Real ML)

### What It Does:
- Takes an image/video frame as input
- Outputs 17 keypoint coordinates (X, Y, confidence)
- Used to extract infant body positions

### Download Options (Choose ONE):

| Model | Size | Speed | Accuracy | Download Link |
|-------|------|-------|----------|---------------|
| **MoveNet Lightning** | 4.6 MB | ⚡ Very Fast | Good | [Download](https://tfhub.dev/google/lite-model/movenet/singlepose/lightning/tflite/float16/4) |
| **MoveNet Thunder** | 12 MB | Fast | Better | [Download](https://tfhub.dev/google/lite-model/movenet/singlepose/thunder/tflite/float16/4) |
| **OpenPose ONNX** | 200 MB | Medium | Best | [Download](https://github.com/CMU-Perceptual-Computing-Lab/openpose) |
| **MediaPipe Pose** | 30 MB | Fast | Good | [Download](https://developers.google.com/mediapipe/solutions/vision/pose_landmarker) |

### Installation Steps:

1. **Download the model** from one of the links above

2. **Place in project:**
   ```
   dotnet/CP-sight.Web/Models/
   └── pose_model.onnx     # or movenet.tflite
   ```

3. **Update configuration** in `appsettings.json`:
   ```json
   {
     "PoseEstimation": {
       "ModelPath": "Models/pose_model.onnx",
       "ModelType": "MoveNet"  // or "OpenPose", "MediaPipe"
     }
   }
   ```

---

## Model #2: Movement Classifier (ALREADY INCLUDED)

### What It Does:
- Takes extracted features (complexity, variability, etc.)
- Outputs CP risk classification

### Status: ✅ Built-In
- Uses ML.NET
- Trained on GMA research thresholds
- Generates synthetic training data
- Located in `CP-sight.ML/Services/MovementClassifier.cs`

---

## MINI-RGBD Dataset (OPTIONAL - For Training)

### What It Is:
- Training data for improving models
- 12 sequences of synthetic infant poses
- 7 GB download

### Do You Need It?
| Use Case | Need MINI-RGBD? |
|----------|-----------------|
| Hackathon demo | ❌ NO |
| Development/testing | ❌ NO |
| Production ML training | ✅ YES |
| Clinical validation | ✅ YES |

### Download:
```
https://obj-web.iosb.fraunhofer.de/content/sensornetze/bewegungsanalyse/MINI-RGBD_web.zip
```

**Current Download Status:** 15% (1.2 GB / 7 GB) - Still in progress

---

## Quick Start for Hackathon

### Minimum Required (5 minutes):
1. Download **MoveNet Lightning** (4.6 MB): https://tfhub.dev/google/lite-model/movenet/singlepose/lightning/tflite/float16/4
2. Rename to `pose_model.tflite`
3. Place in `dotnet/CP-sight.Web/Models/`
4. Build and run

### If Model Download Doesn't Work:
The app will run in **simulation mode** using statistically-valid pose generation based on GMA research.

---

## Verification Checklist

```bash
# Check if models exist
ls -la dotnet/CP-sight.Web/Models/

# Expected output:
# pose_model.onnx   OR   movenet.tflite
```

---

## Troubleshooting

### "Pose estimation is slow"
- Use MoveNet Lightning (smallest, fastest)
- Consider GPU acceleration

### "Model format not supported"
- MoveNet TFLite requires TensorFlow Lite runtime
- Convert to ONNX using `tf2onnx` tool
- Or use OpenPose ONNX directly

### "Out of memory during inference"
- Reduce input image size
- Use MoveNet Lightning instead of Thunder
- Process fewer frames per second

---

## Technical Details

### Supported Model Formats:
| Format | Support | Notes |
|--------|---------|-------|
| ONNX (.onnx) | ✅ Full | Recommended |
| TensorFlow (.pb) | ✅ Full | Via OpenCV DNN |
| TFLite (.tflite) | ⚠️ Limited | Needs conversion |

### Keypoint Output Format:
```json
{
  "nose": { "x": 0.5, "y": 0.15, "confidence": 0.95 },
  "left_wrist": { "x": 0.25, "y": 0.45, "confidence": 0.88 },
  "right_wrist": { "x": 0.75, "y": 0.45, "confidence": 0.91 },
  // ... 17 keypoints total
}
```

### Performance Targets:
| Metric | Target |
|--------|--------|
| Single frame inference | < 100ms |
| 30 frame video | < 3 seconds |
| Memory usage | < 500MB |
