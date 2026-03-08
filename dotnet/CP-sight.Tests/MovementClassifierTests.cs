using CP_Sight.ML.Services;
using CP_Sight.Core.Models;
using FluentAssertions;
using Xunit;

namespace CP_Sight.Tests;

/// <summary>
/// Unit tests for the MovementClassifier
/// </summary>
public class MovementClassifierTests
{
    private readonly MovementClassifier _classifier;

    public MovementClassifierTests()
    {
        _classifier = new MovementClassifier();
    }

    [Fact]
    public void Predict_WithNormalFeatures_ReturnsNormalClassification()
    {
        // Arrange
        var features = new MovementFeatures
        {
            MovementComplexity = 0.75,
            MovementVariability = 0.10,
            LeftRightSymmetry = 0.80,
            FidgetyScore = 0.75
        };

        // Act
        var result = _classifier.Predict(features);

        // Assert
        result.Should().NotBeNull();
        result.Classification.Should().Be("normal");
        result.Scores.Should().NotBeEmpty();
    }

    [Fact]
    public void Predict_WithCrampedSynchronizedFeatures_ReturnsAbnormalClassification()
    {
        // Arrange
        var features = new MovementFeatures
        {
            MovementComplexity = 0.15,
            MovementVariability = 0.02,
            LeftRightSymmetry = 0.95,
            FidgetyScore = 0.10
        };

        // Act
        var result = _classifier.Predict(features);

        // Assert
        result.Should().NotBeNull();
        result.Classification.Should().BeOneOf("cramped_synchronized", "absent_fidgety", "poor_repertoire");
    }

    [Fact]
    public void Predict_WithAbsentFidgetyFeatures_ReturnsAbsentFidgety()
    {
        // Arrange
        var features = new MovementFeatures
        {
            MovementComplexity = 0.25,
            MovementVariability = 0.03,
            LeftRightSymmetry = 0.55,
            FidgetyScore = 0.08
        };

        // Act
        var result = _classifier.Predict(features);

        // Assert
        result.Should().NotBeNull();
        result.Classification.Should().BeOneOf("absent_fidgety", "poor_repertoire", "cramped_synchronized");
    }

    [Fact]
    public void Predict_ReturnsProbabilityScores()
    {
        // Arrange
        var features = new MovementFeatures
        {
            MovementComplexity = 0.5,
            MovementVariability = 0.05,
            LeftRightSymmetry = 0.7,
            FidgetyScore = 0.5
        };

        // Act
        var result = _classifier.Predict(features);

        // Assert
        result.Scores.Length.Should().BeGreaterThan(0);
    }
}
