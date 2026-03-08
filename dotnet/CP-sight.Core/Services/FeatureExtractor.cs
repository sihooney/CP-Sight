using CP_Sight.Core.Models;

namespace CP_Sight.Core.Services;

/// <summary>
/// Extracts movement features from pose data for GMA-based CP risk assessment
/// Based on research by Prechtl et al. and automated GMA studies
/// </summary>
public class FeatureExtractor
{
    private const int TargetFps = 30;
    
    /// <summary>
    /// Extract movement features from pose sequence
    /// </summary>
    public MovementFeatures ExtractFeatures(List<PoseFrame> frames)
    {
        if (frames.Count < 2)
        {
            return GetDefaultFeatures();
        }

        var features = new MovementFeatures
        {
            MovementComplexity = CalculateComplexity(frames),
            MovementVariability = CalculateVariability(frames),
            LeftRightSymmetry = CalculateSymmetry(frames),
            FidgetyScore = CalculateFidgetyScore(frames),
            CrampedSynchronizedScore = CalculateCrampedSynchronized(frames),
            PoorRepertoireScore = CalculatePoorRepertoire(frames),
            AvgMovementSpeed = CalculateAvgSpeed(frames),
            PeakMovementSpeed = CalculatePeakSpeed(frames),
            AvgWristDistance = CalculateAvgJointDistance(frames, "left_wrist", "right_wrist"),
            AvgAnkleDistance = CalculateAvgJointDistance(frames, "left_ankle", "right_ankle"),
            WristMovementRange = CalculateMovementRange(frames, "left_wrist", "right_wrist"),
            AnkleMovementRange = CalculateMovementRange(frames, "left_ankle", "right_ankle"),
            DominantFrequency = CalculateDominantFrequency(frames)
        };

        return features;
    }

    private double CalculateComplexity(List<PoseFrame> frames)
    {
        // Direction changes in wrist movement
        var directionChanges = 0;
        var prevDirection = 0.0;
        
        for (int i = 2; i < frames.Count; i++)
        {
            var curr = GetJointPosition(frames[i], "right_wrist");
            var prev = GetJointPosition(frames[i - 1], "right_wrist");
            var prevPrev = GetJointPosition(frames[i - 2], "right_wrist");

            if (curr == null || prev == null || prevPrev == null) continue;

            var dx = curr.X - prev.X;
            var prevDx = prev.X - prevPrev.X;

            if (Math.Sign(dx) != Math.Sign(prevDx) && Math.Abs(dx) > 0.001)
            {
                directionChanges++;
                prevDirection = dx;
            }
        }

        return Math.Min(1.0, (double)directionChanges / (frames.Count / 10.0));
    }

    private double CalculateVariability(List<PoseFrame> frames)
    {
        var velocities = new List<double>();
        
        for (int i = 1; i < frames.Count; i++)
        {
            var joints = new[] { "left_wrist", "right_wrist", "left_ankle", "right_ankle" };
            
            foreach (var joint in joints)
            {
                var curr = GetJointPosition(frames[i], joint);
                var prev = GetJointPosition(frames[i - 1], joint);
                
                if (curr == null || prev == null) continue;

                var velocity = Math.Sqrt(
                    Math.Pow(curr.X - prev.X, 2) +
                    Math.Pow(curr.Y - prev.Y, 2)
                );
                velocities.Add(velocity);
            }
        }

        if (velocities.Count == 0) return 0.0;
        
        var mean = velocities.Average();
        var variance = velocities.Sum(v => Math.Pow(v - mean, 2)) / velocities.Count;
        
        return Math.Min(1.0, Math.Sqrt(variance) * 10);
    }

    private double CalculateSymmetry(List<PoseFrame> frames)
    {
        var symmetryScores = new List<double>();
        
        for (int i = 1; i < frames.Count; i++)
        {
            var leftWrist = GetJointPosition(frames[i], "left_wrist");
            var rightWrist = GetJointPosition(frames[i], "right_wrist");
            var prevLeftWrist = GetJointPosition(frames[i - 1], "left_wrist");
            var prevRightWrist = GetJointPosition(frames[i - 1], "right_wrist");
            
            if (leftWrist == null || rightWrist == null || 
                prevLeftWrist == null || prevRightWrist == null) continue;

            var leftMove = Math.Sqrt(
                Math.Pow(leftWrist.X - prevLeftWrist.X, 2) +
                Math.Pow(leftWrist.Y - prevLeftWrist.Y, 2)
            );
            var rightMove = Math.Sqrt(
                Math.Pow(rightWrist.X - prevRightWrist.X, 2) +
                Math.Pow(rightWrist.Y - prevRightWrist.Y, 2)
            );

            var symmetry = 1.0 - Math.Abs(leftMove - rightMove) / Math.Max(leftMove, Math.Max(rightMove, 0.001));
            symmetryScores.Add(symmetry);
        }

        return symmetryScores.Count > 0 ? symmetryScores.Average() : 0.5;
    }

    private double CalculateFidgetyScore(List<PoseFrame> frames)
    {
        // Fidgety movements: small amplitude, high frequency movements
        var complexity = CalculateComplexity(frames);
        var variability = CalculateVariability(frames);
        
        // Fidgety score combines complexity and variability
        return complexity * 0.6 + Math.Min(1.0, variability * 10) * 0.4;
    }

    private double CalculateCrampedSynchronized(List<PoseFrame> frames)
    {
        // Cramped-synchronized: low variability, simultaneous movements
        var variability = CalculateVariability(frames);
        var symmetry = CalculateSymmetry(frames);
        
        // High symmetry + low variability = cramped-synchronized
        return (1.0 - Math.Min(1.0, variability * 10)) * 0.5 + symmetry * 0.5;
    }

    private double CalculatePoorRepertoire(List<PoseFrame> frames)
    {
        // Poor repertoire: low complexity, monotonous movements
        var complexity = CalculateComplexity(frames);
        return 1.0 - complexity;
    }

    private double CalculateAvgSpeed(List<PoseFrame> frames)
    {
        var speeds = new List<double>();
        
        for (int i = 1; i < frames.Count; i++)
        {
            var joints = new[] { "left_wrist", "right_wrist", "left_ankle", "right_ankle" };
            
            foreach (var joint in joints)
            {
                var curr = GetJointPosition(frames[i], joint);
                var prev = GetJointPosition(frames[i - 1], joint);
                
                if (curr == null || prev == null) continue;

                var speed = Math.Sqrt(
                    Math.Pow(curr.X - prev.X, 2) +
                    Math.Pow(curr.Y - prev.Y, 2)
                );
                speeds.Add(speed);
            }
        }

        return speeds.Count > 0 ? speeds.Average() * TargetFps : 0.0;
    }

    private double CalculatePeakSpeed(List<PoseFrame> frames)
    {
        var speeds = new List<double>();
        
        for (int i = 1; i < frames.Count; i++)
        {
            var joints = new[] { "left_wrist", "right_wrist", "left_ankle", "right_ankle" };
            
            foreach (var joint in joints)
            {
                var curr = GetJointPosition(frames[i], joint);
                var prev = GetJointPosition(frames[i - 1], joint);
                
                if (curr == null || prev == null) continue;

                var speed = Math.Sqrt(
                    Math.Pow(curr.X - prev.X, 2) +
                    Math.Pow(curr.Y - prev.Y, 2)
                );
                speeds.Add(speed);
            }
        }

        return speeds.Count > 0 ? speeds.Max() * TargetFps * 1.5 : 0.0;
    }

    private double CalculateAvgJointDistance(List<PoseFrame> frames, string joint1, string joint2)
    {
        var distances = new List<double>();
        
        foreach (var frame in frames)
        {
            var pos1 = GetJointPosition(frame, joint1);
            var pos2 = GetJointPosition(frame, joint2);
            
            if (pos1 == null || pos2 == null) continue;

            var distance = Math.Sqrt(
                Math.Pow(pos1.X - pos2.X, 2) +
                Math.Pow(pos1.Y - pos2.Y, 2)
            );
            distances.Add(distance);
        }

        return distances.Count > 0 ? distances.Average() : 0.0;
    }

    private double CalculateMovementRange(List<PoseFrame> frames, string joint1, string joint2)
    {
        var distances = new List<double>();
        
        foreach (var frame in frames)
        {
            var pos1 = GetJointPosition(frame, joint1);
            var pos2 = GetJointPosition(frame, joint2);
            
            if (pos1 == null || pos2 == null) continue;

            var distance = Math.Sqrt(
                Math.Pow(pos1.X - pos2.X, 2) +
                Math.Pow(pos1.Y - pos2.Y, 2)
            );
            distances.Add(distance);
        }

        return distances.Count > 0 ? distances.Max() - distances.Min() : 0.0;
    }

    private double CalculateDominantFrequency(List<PoseFrame> frames)
    {
        // Simple frequency estimation using autocorrelation
        if (frames.Count < 10) return 2.0;

        var wristPositions = frames
            .Select(f => GetJointPosition(f, "right_wrist")?.X ?? 0.0)
            .ToList();

        if (wristPositions.Count < 10) return 1.0;

        var maxCorr = 0.0;
        var dominantLag = 1;

        for (int lag = 1; lag < Math.Min(frames.Count / 2, 30); lag++)
        {
            var corr = 0.0;
            for (int i = 0; i < frames.Count - lag; i++)
            {
                corr += wristPositions[i] * wristPositions[i + lag];
            }
            
            if (corr > maxCorr)
            {
                maxCorr = corr;
                dominantLag = lag;
            }
        }

        return TargetFps / dominantLag;
    }

    private JointPosition? GetJointPosition(PoseFrame frame, string jointName)
    {
        return frame.Joints.TryGetValue(jointName.ToLower(), out var pos) ? pos : null;
    }

    private MovementFeatures GetDefaultFeatures()
    {
        return new MovementFeatures
        {
            MovementComplexity = 0.0,
            MovementVariability = 1.0,
            LeftRightSymmetry = 1.0,
            FidgetyScore = 1.0,
            CrampedSynchronizedScore = 1.0,
            PoorRepertoireScore = 1.0,
            AvgMovementSpeed = 1.0,
            PeakMovementSpeed = 1.0,
            AvgWristDistance = 1.0,
            AvgAnkleDistance = 1.0,
            WristMovementRange = 1.0,
            AnkleMovementRange = 1.0,
            DominantFrequency = 1.0
        };
    }
}
