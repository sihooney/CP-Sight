namespace CP_Sight.Core.Models;

/// <summary>
/// Information about the infant being analyzed
/// </summary>
public record InfantInfo
{
    public required int AgeWeeks { get; init; }
    public int? CorrectedAgeWeeks { get; init; }
    public bool IsPreterm { get; init; }
    public int? GestationalAgeAtBirth { get; init; }
    public required string[] RiskFactors { get; init; }
}

/// <summary>
/// 2:1 scale normalized coordinates for a single joint
/// </summary>
public record JointPosition
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public double? Z { get; init; }
    public required double Confidence { get; init; }
}

/// <summary>
/// A single frame of pose data
/// </summary>
public record PoseFrame
{
    public int FrameNumber { get; init; }
    public double Timestamp { get; init; }
    public required Dictionary<string, JointPosition> Joints { get; init; }
}

/// <summary>
/// Movement features extracted from pose data
/// </summary>
public record MovementFeatures
{
    public double MovementComplexity { get; init; }
    public double MovementVariability { get; init; }
    public double AvgWristDistance { get; init; }
    public double AvgAnkleDistance { get; init; }
    public double WristMovementRange { get; init; }
    public double AnkleMovementRange { get; init; }
    public double AvgMovementSpeed { get; init; }
    public double PeakMovementSpeed { get; init; }
    public double LeftRightSymmetry { get; init; }
    public double DominantFrequency { get; init; }
    public double FidgetyScore { get; init; }
    public double CrampedSynchronizedScore { get; init; }
    public double PoorRepertoireScore { get; init; }
}

/// <summary>
/// Feature breakdown for risk assessment
/// </summary>
public record FeatureBreakdown
{
    public required MovementFeatureStatus MovementComplexity { get; init; }
    public required MovementFeatureStatus MovementVariability { get; init; }
    public required MovementFeatureStatus FidgetyMovements { get; init; }
    public required MovementFeatureStatus Symmetry { get; init; }
}

/// <summary>
/// Status of a single feature
/// </summary>
public record MovementFeatureStatus
{
    public required double Value { get; init; }
    public required string Status { get; init; } // "normal", "borderline", "abnormal"
}

/// <summary>
/// Risk assessment result
/// </summary>
public record RiskAssessment
{
    public required string OverallRisk { get; init; } // "low", "medium", "high"
    public required int RiskScore { get; init; }
    public required double Confidence { get; init; }
    public required FeatureBreakdown Breakdown { get; init; }
    public required string[] Recommendations { get; init; }
    public bool FollowUpRequired { get; init; }
}

/// <summary>
/// Video upload result
/// </summary>
public record VideoUploadResult
{
    public required string PublicId { get; init; }
    public required string SecureUrl { get; init; }
    public double Duration { get; init; }
    public required string Format { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}

/// <summary>
/// Complete analysis result
/// </summary>
public record AnalysisResult
{
    public required string Id { get; init; }
    public required DateTime Timestamp { get; init; }
    public required InfantInfo InfantInfo { get; init; }
    public required VideoUploadResult VideoInfo { get; init; }
    public required List<PoseFrame> PoseData { get; init; }
    public required MovementFeatures Features { get; init; }
    public required RiskAssessment Assessment { get; init; }
    public int ProcessingTimeMs { get; init; }
}

/// <summary>
/// Analysis request from client
/// </summary>
public record AnalysisRequest
{
    public required InfantInfo InfantInfo { get; init; }
    public required VideoUploadResult VideoInfo { get; init; }
}
