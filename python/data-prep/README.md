# CP-sight Data Preparation Package

This package contains all the tools needed to prepare training data for the CP-sight ML model.

## Installation

```bash
pip install -r requirements.txt
```

## Usage

### Prepare Training Data from MINI-RGBD

```bash
python scripts/prepare_training_data.py --input /path/to/mini-rgbd --output ./data/processed
```

### Generate Sample Training Data

```bash
python scripts/prepare_training_data.py --generate-samples 100 --output ./sample_data
```

## Dataset Download Links

| Dataset | Size | Link |
|---------|------|------|
| MINI-RGBD | 7 GB | https://www.iosb.fraunhofer.de/en/competences/image-exploitation/object-recognition/sensor-networks/motion-analysis.html |
| Pre-extracted Poses | ~50 MB | https://drive.google.com/file/d/1BH9IDINlwuqWjTiK5V |

## Directory Structure

```
data-prep/
├── scripts/
│   └── prepare_training_data.py
├── data/
│   └── (downloaded MINI-RGBD here)
├── sample_data/
│   ├── train/
│   │   ├── normal/
│   │   ├── poor_repertoire/
│   │   ├── cramped_synchronized/
│   │   └── absent_fidgety/
│   └── val/
│       └── (same structure)
├── requirements.txt
└── README.md
```
