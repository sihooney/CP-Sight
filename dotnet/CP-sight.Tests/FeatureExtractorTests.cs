using CP_Sight.Core.Services;
using CP_Sight.Core.Models;
using FluentAssertions;
using Xunit;

namespace CP_Sight.Tests;

/// <summary>
/// Unit tests for the FeatureExtractor service
/// </summary>
public class FeatureExtractorTests
{
    private readonly FeatureExtractor _extractor;

    public FeatureExtractorTests()
    {
        _extractor = new FeatureExtractor();
    }

    [Fact]
    public void ExtractFeatures_WithEmptyFrames_ReturnsDefaultFeatures()
    {
        // Arrange
        var frames = new List<PoseFrame>();

        // Act
        var result = _extractor.ExtractFeatures(frames);

        // Assert
        result.Should().NotBeNull();
        result.MovementComplexity.Should().Be(0.0);
    }

    [Fact]
    public void ExtractFeatures_WithSingleFrame_ReturnsDefaultFeatures()
    {
        // Arrange
        var frames = new List<PoseFrame>
        {
            CreateTestFrame(0)
        };

        // Act
        var result = _extractor.ExtractFeatures(frames);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ExtractFeatures_WithNormalMovement_ReturnsExpectedFeatures()
    {
        // Arrange
        var frames = CreateNormalMovementSequence(90);

        // Act
        var result = _extractor.ExtractFeatures(frames);

        // Assert
        result.Should().NotBeNull();
        result.MovementComplexity.Should().BeGreaterThan(0);
        result.MovementVariability.Should().BeGreaterThan(0);
        result.LeftRightSymmetry.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ExtractFeatures_WithCrampedSynchronized_ReturnsHighSymmetry()
    {
        // Arrange - Create highly synchronized movement
        var frames = CreateCrampedSynchronizedSequence(90);

        // Act
        var result = _extractor.ExtractFeatures(frames);

        // Assert
        result.LeftRightSymmetry.Should().BeGreaterThan(0.7);
        result.MovementVariability.Should().BeLessThan(0.1);
    }

    private PoseFrame CreateTestFrame(int frameNumber, double movementOffset = 0)
    {
        return new PoseFrame
        {
            FrameNumber = frameNumber,
            Timestamp = frameNumber / 30.0,
            Joints = new Dictionary<string, JointPosition>
            {
                ["nose"] = new() { X = 0.5, Y = 0.15, Confidence = 0.95 },
                ["left_shoulder"] = new() { X = 0.38, Y = 0.25, Confidence = 0.95 },
                ["right_shoulder"] = new() { X = 0.62, Y = 0.25, Confidence = 0.95 },
                ["left_elbow"] = new() { X = 0.32, Y = 0.35, Confidence = 0.90 },
                ["right_elbow"] = new() { X = 0.68, Y = 0.35, Confidence = 0.90 },
                ["left_wrist"] = new() { X = 0.25 + movementOffset, Y = 0.45 + movementOffset * 0.5, Confidence = 0.85 },
                ["right_wrist"] = new() { X = 0.75 - movementOffset, Y = 0.45 + movementOffset * 0.5, Confidence = 0.85 },
                ["left_hip"] = new() { X = 0.42, Y = 0.55, Confidence = 0.95 },
                ["right_hip"] = new() { X = 0.58, Y = 0.55, Confidence = 0.95 },
                ["left_knee"] = new() { X = 0.40, Y = 0.75, Confidence = 0.90 },
                ["right_knee"] = new() { X = 0.60, Y = 0.75, Confidence = 0.90 },
                ["left_ankle"] = new() { X = 0.38, Y = 0.92, Confidence = 0.85 },
                ["right_ankle"] = new() { X = 0.62, Y = 0.92, Confidence = 0.85 }
            }
        };
    }

    private List<PoseFrame> CreateNormalMovementSequence(int frameCount)
    {
        var frames = new List<PoseFrame>();
        var random = new Random(42);

        for (int i = 0; i < frameCount; i++)
        {
            var movementOffset = Math.Sin(i * 0.3) * 0.05 + random.NextDouble() * 0.02;
            frames.Add(CreateTestFrame(i, movementOffset));
        }

        return frames;
    }

    private List<PoseFrame> CreateCrampedSynchronizedSequence(int frameCount)
    {
        var frames = new List<PoseFrame>();

        for (int i = 0; i < frameCount; i++)
        {
            // Same movement on both sides (synchronized)
            var movementOffset = Math.Sin(i * 0.1) * 0.01;
            frames.Add(CreateTestFrame(i, movementOffset));
        }

        return frames;
    }
}
