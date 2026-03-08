#!/usr/bin/env python3
"""
CP-sight Training Data Preparation Script

This script helps prepare training data from the MINI-RGBD dataset and other sources
for training AI models for early cerebral palsy detection.

Usage:
    python prepare_training_data.py --input /path/to/mini-rgbd --output ./training_data
"""

import os
import json
import numpy as np
import argparse
from pathlib import Path
from typing import Dict, List, Tuple, Any
import csv

# Joint definitions for MINI-RGBD (17 keypoints)
MINI_RGBD_JOINTS = [
    "head", "neck", 
    "right_shoulder", "right_elbow", "right_wrist",
    "left_shoulder", "left_elbow", "left_wrist",
    "right_hip", "right_knee", "right_ankle",
    "left_hip", "left_knee", "left_ankle",
    "chest", "right_ear", "left_ear"
]

# Movement thresholds for GMA classification
THRESHOLDS = {
    'complexity_normal': 0.5,
    'complexity_borderline': 0.3,
    'variability_normal': 0.08,
    'variability_borderline': 0.05,
    'symmetry_normal': 0.7,
    'symmetry_borderline': 0.5,
}

class PoseDataProcessor:
    """Process pose data from various datasets"""
    
    def __init__(self):
        self.joint_names = MINI_RGBD_JOINTS
    
    def load_mini_rgbd_sequence(self, seq_path: str) -> Dict[str, Any]:
        """Load a single MINI-RGBD sequence"""
        frames = []
        gt_path = os.path.join(seq_path, "ground_truth")
        
        if not os.path.exists(gt_path):
            return None
        
        gt_files = sorted([f for f in os.listdir(gt_path) if f.endswith('.txt')])
        
        for gt_file in gt_files:
            with open(os.path.join(gt_path, gt_file)) as f:
                joints_3d = np.loadtxt(f)
                
            frame_data = {
                'frame_number': len(frames),
                'timestamp': len(frames) / 30.0,  # Assuming 30 FPS
                'joints': {}
            }
            
            for i, joint_name in enumerate(self.joint_names):
                if i < len(joints_3d):
                    frame_data['joints'][joint_name] = {
                        'x': float(joints_3d[i][0]),
                        'y': float(joints_3d[i][1]),
                        'z': float(joints_3d[i][2]) if len(joints_3d[i]) > 2 else 0.0,
                        'confidence': 1.0  # Ground truth has perfect confidence
                    }
            
            frames.append(frame_data)
        
        return {
            'frames': frames,
            'total_frames': len(frames),
            'fps': 30
        }
    
    def compute_features(self, pose_sequence: Dict) -> Dict[str, float]:
        """Compute movement features from pose sequence"""
        frames = pose_sequence['frames']
        
        if len(frames) < 2:
            return self._default_features()
        
        features = {}
        
        # 1. Movement Complexity (direction changes)
        wrist_positions = [f['joints']['right_wrist'] for f in frames]
        direction_changes = 0
        for i in range(2, len(wrist_positions)):
            dx = wrist_positions[i]['x'] - wrist_positions[i-1]['x']
            prev_dx = wrist_positions[i-1]['x'] - wrist_positions[i-2]['x']
            if np.sign(dx) != np.sign(prev_dx) and abs(dx) > 0.001:
                direction_changes += 1
        
        features['movement_complexity'] = min(1.0, direction_changes / (len(frames) / 10))
        
        # 2. Movement Variability (velocity standard deviation)
        velocities = []
        for i in range(1, len(frames)):
            # Calculate velocity for all limbs
            for joint in ['right_wrist', 'left_wrist', 'right_ankle', 'left_ankle']:
                if joint in frames[i]['joints'] and joint in frames[i-1]['joints']:
                    v = np.sqrt(
                        (frames[i]['joints'][joint]['x'] - frames[i-1]['joints'][joint]['x'])**2 +
                        (frames[i]['joints'][joint]['y'] - frames[i-1]['joints'][joint]['y'])**2
                    )
                    velocities.append(v)
        
        features['movement_variability'] = np.std(velocities) if velocities else 0.0
        
        # 3. Symmetry (left-right correlation)
        left_wrist_x = [f['joints']['left_wrist']['x'] for f in frames]
        right_wrist_x = [f['joints']['right_wrist']['x'] for f in frames]
        
        if len(left_wrist_x) > 2 and np.std(left_wrist_x) > 0 and np.std(right_wrist_x) > 0:
            correlation = np.corrcoef(left_wrist_x, right_wrist_x)[0, 1]
            features['left_right_symmetry'] = abs(correlation) if not np.isnan(correlation) else 0.5
        else:
            features['left_right_symmetry'] = 0.5
        
        # 4. Average Movement Speed
        avg_speed = np.mean(velocities) if velocities else 0.0
        features['avg_movement_speed'] = avg_speed
        
        # 5. Peak Movement Speed
        features['peak_movement_speed'] = max(velocities) if velocities else 0.0
        
        # 6. Fidgety Score (combination of complexity and variability)
        features['fidgety_score'] = (
            features['movement_complexity'] * 0.6 +
            min(1.0, features['movement_variability'] * 10) * 0.4
        )
        
        # 7. Cramped-Synchronized Score (inverse of variability)
        features['cramped_synchronized_score'] = 1.0 - min(1.0, features['movement_variability'] * 10)
        
        # 8. Poor Repertoire Score (inverse of complexity)
        features['poor_repertoire_score'] = 1.0 - features['movement_complexity']
        
        return features
    
    def _default_features(self) -> Dict[str, float]:
        """Return default features when insufficient data"""
        return {
            'movement_complexity': 0.5,
            'movement_variability': 0.05,
            'left_right_symmetry': 0.5,
            'avg_movement_speed': 0.0,
            'peak_movement_speed': 0.0,
            'fidgety_score': 0.5,
            'cramped_synchronized_score': 0.5,
            'poor_repertoire_score': 0.5,
        }
    
    def classify_gma(self, features: Dict[str, float]) -> Tuple[str, int]:
        """Classify movement pattern based on features"""
        
        # Calculate risk score (0-100, higher is better/normal)
        complexity_score = (
            100 if features['movement_complexity'] >= THRESHOLDS['complexity_normal']
            else 50 if features['movement_complexity'] >= THRESHOLDS['complexity_borderline']
            else 0
        )
        
        variability_score = (
            100 if features['movement_variability'] >= THRESHOLDS['variability_normal']
            else 50 if features['movement_variability'] >= THRESHOLDS['variability_borderline']
            else 0
        )
        
        symmetry_score = (
            100 if features['left_right_symmetry'] >= THRESHOLDS['symmetry_normal']
            else 50 if features['left_right_symmetry'] >= THRESHOLDS['symmetry_borderline']
            else 0
        )
        
        risk_score = (
            complexity_score * 0.4 +
            variability_score * 0.35 +
            symmetry_score * 0.25
        )
        
        # Determine GMA classification
        if risk_score >= 70:
            return 'normal', risk_score
        elif risk_score >= 40:
            if features['cramped_synchronized_score'] > 0.6:
                return 'cramped_synchronized', risk_score
            else:
                return 'poor_repertoire', risk_score
        else:
            return 'absent_fidgety', risk_score


def process_mini_rgbd(input_dir: str, output_dir: str):
    """Process MINI-RGBD dataset"""
    processor = PoseDataProcessor()
    
    output_path = Path(output_dir)
    output_path.mkdir(parents=True, exist_ok=True)
    
    # Create subdirectories for each class
    for class_name in ['normal', 'poor_repertoire', 'cramped_synchronized', 'absent_fidgety']:
        (output_path / 'train' / class_name).mkdir(parents=True, exist_ok=True)
        (output_path / 'val' / class_name).mkdir(parents=True, exist_ok=True)
    
    all_data = []
    
    # Process each sequence
    input_path = Path(input_dir)
    seq_dirs = sorted([d for d in input_path.iterdir() if d.is_dir() and d.name.startswith('seq')])
    
    for seq_dir in seq_dirs:
        print(f"Processing {seq_dir.name}...")
        
        pose_data = processor.load_mini_rgbd_sequence(str(seq_dir))
        if pose_data is None:
            continue
        
        features = processor.compute_features(pose_data)
        classification, risk_score = processor.classify_gma(features)
        
        # Prepare output data
        output_data = {
            'sequence_id': seq_dir.name,
            'total_frames': pose_data['total_frames'],
            'features': features,
            'classification': classification,
            'risk_score': risk_score,
            'pose_data': pose_data['frames'][:30]  # First 30 frames
        }
        
        all_data.append(output_data)
        
        # Save to appropriate directory
        split = 'train' if hash(seq_dir.name) % 5 != 0 else 'val'  # 80/20 split
        output_file = output_path / split / classification / f"{seq_dir.name}.json"
        
        with open(output_file, 'w') as f:
            json.dump(output_data, f, indent=2)
        
        print(f"  -> {classification} (risk score: {risk_score:.1f})")
    
    # Save summary
    summary = {
        'total_sequences': len(all_data),
        'class_distribution': {},
        'feature_statistics': {}
    }
    
    for item in all_data:
        cls = item['classification']
        summary['class_distribution'][cls] = summary['class_distribution'].get(cls, 0) + 1
    
    for feature in all_data[0]['features'].keys():
        values = [d['features'][feature] for d in all_data]
        summary['feature_statistics'][feature] = {
            'mean': float(np.mean(values)),
            'std': float(np.std(values)),
            'min': float(np.min(values)),
            'max': float(np.max(values))
        }
    
    with open(output_path / 'summary.json', 'w') as f:
        json.dump(summary, f, indent=2)
    
    print(f"\nProcessing complete!")
    print(f"Total sequences: {len(all_data)}")
    print(f"Class distribution: {summary['class_distribution']}")
    
    return summary


def generate_sample_data(output_dir: str, num_samples: int = 100):
    """Generate sample training data for demonstration"""
    processor = PoseDataProcessor()
    
    output_path = Path(output_dir)
    output_path.mkdir(parents=True, exist_ok=True)
    
    # Create directories
    for class_name in ['normal', 'poor_repertoire', 'cramped_synchronized', 'absent_fidgety']:
        (output_path / 'train' / class_name).mkdir(parents=True, exist_ok=True)
        (output_path / 'val' / class_name).mkdir(parents=True, exist_ok=True)
    
    for i in range(num_samples):
        # Generate random features with class-specific distributions
        if i < num_samples // 4:
            # Normal movements
            features = {
                'movement_complexity': np.random.uniform(0.6, 0.9),
                'movement_variability': np.random.uniform(0.08, 0.15),
                'left_right_symmetry': np.random.uniform(0.75, 0.95),
                'avg_movement_speed': np.random.uniform(0.01, 0.03),
                'peak_movement_speed': np.random.uniform(0.03, 0.06),
                'fidgety_score': np.random.uniform(0.6, 0.9),
                'cramped_synchronized_score': np.random.uniform(0.1, 0.4),
                'poor_repertoire_score': np.random.uniform(0.1, 0.4),
            }
            classification = 'normal'
        elif i < num_samples // 2:
            # Poor repertoire
            features = {
                'movement_complexity': np.random.uniform(0.2, 0.4),
                'movement_variability': np.random.uniform(0.05, 0.1),
                'left_right_symmetry': np.random.uniform(0.5, 0.7),
                'avg_movement_speed': np.random.uniform(0.005, 0.015),
                'peak_movement_speed': np.random.uniform(0.015, 0.03),
                'fidgety_score': np.random.uniform(0.3, 0.5),
                'cramped_synchronized_score': np.random.uniform(0.4, 0.6),
                'poor_repertoire_score': np.random.uniform(0.6, 0.8),
            }
            classification = 'poor_repertoire'
        elif i < 3 * num_samples // 4:
            # Cramped synchronized
            features = {
                'movement_complexity': np.random.uniform(0.1, 0.3),
                'movement_variability': np.random.uniform(0.01, 0.05),
                'left_right_symmetry': np.random.uniform(0.8, 0.98),  # High but wrong
                'avg_movement_speed': np.random.uniform(0.002, 0.01),
                'peak_movement_speed': np.random.uniform(0.01, 0.02),
                'fidgety_score': np.random.uniform(0.1, 0.3),
                'cramped_synchronized_score': np.random.uniform(0.7, 0.95),
                'poor_repertoire_score': np.random.uniform(0.7, 0.9),
            }
            classification = 'cramped_synchronized'
        else:
            # Absent fidgety
            features = {
                'movement_complexity': np.random.uniform(0.1, 0.25),
                'movement_variability': np.random.uniform(0.02, 0.06),
                'left_right_symmetry': np.random.uniform(0.4, 0.6),
                'avg_movement_speed': np.random.uniform(0.003, 0.01),
                'peak_movement_speed': np.random.uniform(0.01, 0.025),
                'fidgety_score': np.random.uniform(0.1, 0.35),
                'cramped_synchronized_score': np.random.uniform(0.5, 0.7),
                'poor_repertoire_score': np.random.uniform(0.65, 0.9),
            }
            classification = 'absent_fidgety'
        
        # Generate synthetic pose data
        frames = []
        for frame_num in range(30):
            frame = {
                'frame_number': frame_num,
                'timestamp': frame_num / 30.0,
                'joints': {}
            }
            for joint in MINI_RGBD_JOINTS:
                base_x = 0.5 + np.random.uniform(-0.1, 0.1)
                base_y = 0.5 + np.random.uniform(-0.1, 0.1)
                
                # Add movement variation based on features
                movement = features['movement_variability'] * np.sin(frame_num * 0.5)
                
                frame['joints'][joint] = {
                    'x': base_x + movement * np.random.uniform(-1, 1),
                    'y': base_y + movement * np.random.uniform(-1, 1),
                    'z': 0.0,
                    'confidence': np.random.uniform(0.8, 1.0)
                }
            frames.append(frame)
        
        output_data = {
            'sequence_id': f'sample_{i:04d}',
            'total_frames': 30,
            'features': features,
            'classification': classification,
            'risk_score': int(features['fidgety_score'] * 100),
            'pose_data': frames
        }
        
        split = 'train' if i % 5 != 0 else 'val'
        output_file = output_path / split / classification / f"sample_{i:04d}.json"
        
        with open(output_file, 'w') as f:
            json.dump(output_data, f, indent=2)
    
    print(f"Generated {num_samples} sample training sequences")


def main():
    parser = argparse.ArgumentParser(description='Prepare CP-sight training data')
    parser.add_argument('--input', type=str, help='Input directory (MINI-RGBD dataset)')
    parser.add_argument('--output', type=str, default='./training_data', help='Output directory')
    parser.add_argument('--generate-samples', type=int, help='Generate N sample data points')
    
    args = parser.parse_args()
    
    if args.generate_samples:
        generate_sample_data(args.output, args.generate_samples)
    elif args.input:
        process_mini_rgbd(args.input, args.output)
    else:
        print("Please provide --input or --generate-samples")
        print("\nExample usage:")
        print("  python prepare_training_data.py --input /path/to/mini-rgbd --output ./training_data")
        print("  python prepare_training_data.py --generate-samples 100 --output ./sample_data")


if __name__ == '__main__':
    main()
