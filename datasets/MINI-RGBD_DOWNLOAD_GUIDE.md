# MINI-RGBD Dataset - Complete Download Guide

## What is MINI-RGBD?

**MINI-RGBD** (The Moving INfants In RGB-D) is a **synthetic dataset** of infant poses created by Fraunhofer IOSB. It contains computer-generated (NOT real) infant models in various poses, designed for training pose estimation algorithms.

### What You're Downloading

| Component | Size | Description |
|-----------|------|-------------|
| **RGB Images** | ~4GB | Color images of synthetic infant poses |
| **Depth Images** | ~2GB | Corresponding depth maps |
| **Ground Truth Poses** | ~100MB | 3D joint positions (17 keypoints) |
| **Metadata** | ~10MB | Sequence info, camera parameters |

### Total Download Size
- **Full Dataset**: ~7 GB
- **Poses Only**: ~50-100 MB (if available separately)

---

## ⚠️ IMPORTANT: What This Dataset IS and IS NOT

| ✅ IT IS | ❌ IT IS NOT |
|----------|--------------|
| Synthetic (CGI) infant models | Real infant videos |
| For training pose estimation | Clinically validated data |
| 12 video sequences | Labeled for CP risk |
| Ground truth joint positions | Ready for clinical use |

**You still need clinical validation data** for real-world CP screening.

---

## Working Download Sources

### Source 1: Fraunhofer IOSB (Official - RECOMMENDED)

**URL:** https://argmax.ai/data/mini-rgbd/

**Steps:**
1. Visit the URL above
2. Scroll to find the download section
3. May require registration (free)
4. Download the full dataset (~7GB)

**Note:** This is the official source with the most reliable data.

---

### Source 2: OpenDataLab Mirror

**URL:** https://opendatalab.com/OpenDataLab/The_Moving_INfants_In_RGB-D_MINI-RGBD

**Steps:**
1. Create a free OpenDataLab account
2. Navigate to the dataset page
3. Click "Download" or use their CLI tool

**Using OpenDataLab CLI:**
```bash
pip install openxlab
openxlab login  # Enter your credentials
openxlab dataset download --dataset_path OpenDataLab/The_Moving_INfants_In_RGB-D_MINI-RGBD
```

---

### Source 3: Academic Request (Alternative)

If the above sources don't work, you can:

1. **Email the researchers directly:**
   - Fraunhofer IOSB: `motion-analysis@iosb.fraunhofer.de`
   - Reference the paper: "MINI-RGBD: A Synthetic Dataset for 3D Infant Pose Estimation"

2. **Check university networks:**
   - Some universities have mirrors of popular datasets

---

## What About Pre-Extracted Poses?

The pre-extracted poses Google Drive link is unfortunately dead. Here are your options:

### Option A: Download Full Dataset + Extract Yourself

```bash
# After downloading MINI-RGBD, run:
cd python/data-prep
pip install -r requirements.txt
python scripts/prepare_training_data.py --input /path/to/MINI-RGBD --output ./processed
```

### Option B: Use Our Included Sample Data

The archive already includes **50 sample pose sequences** for testing:

```
python/data-prep/sample_data/
├── train/
│   ├── normal/              (10 samples)
│   ├── poor_repertoire/     (10 samples)
│   ├── cramped_synchronized/(10 samples)
│   └── absent_fidgety/      (10 samples)
└── val/
    └── (10 samples total)
```

**This is enough for initial development!** You can add real MINI-RGBD data later.

### Option C: Generate Synthetic Training Data

The `MovementClassifier` already generates synthetic training data based on GMA research thresholds. This works for development.

---

## Dataset Structure After Download

```
MINI-RGBD/
├── seq_01/                    # Sequence 1 (Easy)
│   ├── depth/                 # Depth images (PNG, 640x480)
│   │   ├── 0000.png
│   │   ├── 0001.png
│   │   └── ...
│   ├── rgb/                   # RGB images (PNG, 640x480)
│   │   ├── 0000.png
│   │   ├── 0001.png
│   │   └── ...
│   ├── ground_truth/          # 3D joint positions
│   │   └── joints_3d.txt      # 17 joints per frame
│   └── info.txt               # Camera params, etc.
├── seq_02/                    # Sequence 2
├── ...
├── seq_12/                    # Sequence 12 (Difficult)
└── README.txt
```

---

## Joint Definition (17 Keypoints)

| Index | Joint Name | Use for CP Analysis |
|-------|------------|---------------------|
| 0 | Head | ❌ Less important |
| 1 | Neck | ❌ Less important |
| 2 | Right Shoulder | ⚠️ Moderate |
| 3 | Right Elbow | ⚠️ Moderate |
| 4 | Right Wrist | ✅ **Critical** |
| 5 | Left Shoulder | ⚠️ Moderate |
| 6 | Left Elbow | ⚠️ Moderate |
| 7 | Left Wrist | ✅ **Critical** |
| 8 | Right Hip | ⚠️ Moderate |
| 9 | Right Knee | ⚠️ Moderate |
| 10 | Right Ankle | ✅ **Critical** |
| 11 | Left Hip | ⚠️ Moderate |
| 12 | Left Knee | ⚠️ Moderate |
| 13 | Left Ankle | ✅ **Critical** |
| 14 | Chest | ❌ Less important |
| 15 | Right Ear | ❌ Less important |
| 16 | Left Ear | ❌ Less important |

**Wrists and Ankles are most important** for GMA analysis (fidgety movements).

---

## Do You NEED to Download MINI-RGBD?

### For Development: NO ❌
- 50 sample sequences included
- Synthetic training data built-in
- Simulated pose estimation works

### For Production: YES ✅
- Need real pose estimation model
- Training on more data improves accuracy
- Clinical validation requires real data

---

## Recommended Action

**For Hackathon/Demo:**
1. Use the included 50 sample sequences ✅
2. Project runs immediately ✅
3. Skip MINI-RGBD download for now

**For Production:**
1. Download MINI-RGBD from official source
2. Process with included Python scripts
3. Train real ML model
4. Validate on clinical data

---

## Quick Links Summary

| Source | URL | Size | Notes |
|--------|-----|------|-------|
| Official | https://argmax.ai/data/mini-rgbd/ | ~7GB | Best quality |
| OpenDataLab | https://opendatalab.com/OpenDataLab/The_Moving_INfants_In_RGB-D_MINI-RGBD | ~7GB | Free account needed |
| Sample Data | **Already in archive** | ~1MB | 50 sequences, ready to use |

---

## Contact for Issues

If download links don't work:
- Email: `argmax@iosb.fraunhofer.de`
- GitHub Issues: Check if there's a mirror repository
- ResearchGate: Some papers include dataset links
