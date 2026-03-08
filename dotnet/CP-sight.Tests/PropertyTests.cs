using CP_Sight.Core.Services;
using CP_Sight.Core.Models;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using System.Collections.Generic;

namespace CP_Sight.Tests;

public class PropertyTests
{
    private readonly FeatureExtractor _extractor = new FeatureExtractor();

    // FsCheck Property: Extracted features should never be null and their values should be within plausible ranges
    [Property]
    public Property FeatureExtraction_ShouldAlwaysReturnValidFeatures(List<PoseFrame> frames)
    {
        // Act
        var result = _extractor.ExtractFeatures(frames ?? new List<PoseFrame>());

        // Define our properties
        var isNotNull = result != null;
        var complexityIsNonNegative = result?.MovementComplexity >= 0;
        var variabilityIsNonNegative = result?.MovementVariability >= 0;
        var fidgetyIsNonNegative = result?.FidgetyMovements >= 0;
        var symmetryIsNonNegative = result?.LeftRightSymmetry >= 0;

        return (isNotNull && 
                complexityIsNonNegative && 
                variabilityIsNonNegative && 
                fidgetyIsNonNegative && 
                symmetryIsNonNegative).ToProperty();
    }

    [Property]
    public Property FeatureExtraction_WithPurelyRandomJoints_ShouldNotCrash(List<float> randomValues)
    {
        if (randomValues == null || randomValues.Count == 0) return true.ToProperty();

        // Generate synthetic random frames from FsCheck
        var frames = new List<PoseFrame>();
        for (int i = 0; i < randomValues.Count / 10; i++)
        {
            var joints = new Dictionary<string, JointPosition>
            {
                ["left_wrist"] = new JointPosition { X = randomValues[i], Y = randomValues[i], Confidence = 1.0 },
                ["right_wrist"] = new JointPosition { X = randomValues[i], Y = randomValues[i], Confidence = 1.0 },
                ["left_ankle"] = new JointPosition { X = randomValues[i], Y = randomValues[i], Confidence = 1.0 },
                ["right_ankle"] = new JointPosition { X = randomValues[i], Y = randomValues[i], Confidence = 1.0 }
            };
            frames.Add(new PoseFrame { FrameNumber = i, Timestamp = i * 0.033, Joints = joints });
        }

        var result = _extractor.ExtractFeatures(frames);

        return (result != null).ToProperty();
    }
}
