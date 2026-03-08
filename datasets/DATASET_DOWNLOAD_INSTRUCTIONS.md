# Dataset Download Instructions

## MINI-RGBD Dataset (Primary)

### Option 1: Fraunhofer IOSB (Official Source)

1. Visit: https://www.iosb.fraunhofer.de/en/competences/image-exploitation/object-recognition/sensor-networks/motion-analysis.html
2. Scroll to "Download MINI-RGBD" section
3. Click the download button (7 GB)
4. Extract to: `datasets/mini-rgbd/`

### Option 2: OpenDataLab Mirror

```bash
# Using wget
wget https://opendatalab.com/OpenDataLab/The_Moving_INfants_In_RGB-D_MINI-RGBD/download -O mini-rgbd.zip

# Or using curl
curl -L "https://opendatalab.com/OpenDataLab/The_Moving_INfants_In_RGB-D_MINI-RGBD/download" -o mini-rgbd.zip
```

### Option 3: Pre-extracted Poses (Quick Start)

If you just need pose data (no raw video/images):

```bash
# Google Drive link (about 50 MB)
wget --no-check-certificate "https://drive.google.com/uc?export=download&id=1BH9IDINlwuqWjTiK5V" -O mini-rgbd-poses.zip
```

---

## Dataset Contents

```
MINI-RGBD/
├── seq_01/                    # Easy sequence
│   ├── depth/                 # Depth images (PNG)
│   ├── rgb/                   # RGB images (PNG)
│   ├── ground_truth/          # 3D joint positions (TXT)
│   └── info.txt               # Metadata
├── seq_02/                    # Easy sequence
├── ...
├── seq_12/                    # Difficult sequence
└── README.txt
```

---

## Joint Definition (17 keypoints)

| Index | Joint Name |
|-------|------------|
| 0 | Head |
| 1 | Neck |
| 2 | Right Shoulder |
| 3 | Right Elbow |
| 4 | Right Wrist |
| 5 | Left Shoulder |
| 6 | Left Elbow |
| 7 | Left Wrist |
| 8 | Right Hip |
| 9 | Right Knee |
| 10 | Right Ankle |
| 11 | Left Hip |
| 12 | Left Knee |
| 13 | Left Ankle |
| 14 | Chest |
| 15 | Right Ear |
| 16 | Left Ear |

---

## Difficulty Levels

| Sequences | Difficulty |
|-----------|------------|
| 1-4 | Easy |
| 5-9 | Medium |
| 10-12 | Difficult |

---

## Data Processing

After downloading, process with:

```bash
cd python/data-prep
python scripts/prepare_training_data.py --input /path/to/MINI-RGBD --output ./processed
```

---

## Additional Datasets

### SMaL Dataset (Simultaneously-collected Multimodal Mannequin Lying)
- Contact research institutions for access
- Multimodal (RGB, depth, IR)
- Over 88k images from 53 premature infants

### RVI-38 Dataset
- 38 real infant videos
- Contact researchers directly for access

### Clinical GMA Videos
- Contact: General Movements Trust (https://general-movements-trust.info/)
- Requires research collaboration agreement
