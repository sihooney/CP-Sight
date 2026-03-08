using CP_Sight.Core.Models;
using CP_Sight.ML.Services;

namespace CP_Sight.Web.Services;

/// <summary>
/// Clinical risk assessment service based on GMA (General Movements Assessment) research
/// </summary>
public class RiskAssessmentService
{
    private const double LowRiskThreshold = 0.35;
    private const double HighRiskThreshold = 0.65;
    private const int MinAgeWeeksForFidgety = 9;
    private const int MaxAgeWeeksForFidgety = 20;

    /// <summary>
    /// Assess CP risk based on movement features, ML prediction, and infant info
    /// </summary>
    public RiskAssessment Assess(MovementFeatures features, RiskPrediction prediction, InfantInfo infantInfo)
    {
        var riskFactors = new List<string>();
        var recommendations = new List<string>();
        var riskScore = CalculateRiskScore(features, prediction, infantInfo, riskFactors);

        // Determine overall risk level
        var overallRisk = riskScore switch
        {
            < LowRiskThreshold -> "low",
            > HighRiskThreshold => "high",
            _ => "medium"
        };

        // Generate recommendations based on assessment
        GenerateRecommendations(overallRisk, prediction.Classification, infantInfo, recommendations);

        // Create feature breakdown
        var breakdown = CreateFeatureBreakdown(features, infantInfo);

        return new RiskAssessment
        {
            OverallRisk = overallRisk,
            RiskScore = (int)(riskScore * 100),
            Confidence = CalculateConfidence(features, prediction),
            Breakdown = breakdown,
            Recommendations = recommendations.ToArray(),
            FollowUpRequired = overallRisk != "low"
        };
    }

    private double CalculateRiskScore(MovementFeatures features, RiskPrediction prediction, 
        InfantInfo infantInfo, List<string> riskFactors)
    {
        double score = 0;

        // Classification-based risk (primary factor)
        var classificationRisk = prediction.Classification switch
        {
            "cramped_synchronized" => 0.9,
            "absent_fidgety" => 0.85,
            "poor_repertoire" => 0.6,
            _ => 0.15
        };
        score += classificationRisk * 0.4;

        // Fidgety movements assessment (critical for 9-20 week age range)
        if (IsInFidgetyAgeRange(infantInfo))
        {
            if (features.FidgetyScore < 0.3)
            {
                score += 0.25;
                riskFactors.Add("Absent or severely reduced fidgety movements");
            }
            else if (features.FidgetyScore < 0.5)
            {
                score += 0.15;
                riskFactors.Add("Reduced fidgety movements");
            }
        }

        // Movement complexity
        if (features.MovementComplexity < 0.3)
        {
            score += 0.15;
            riskFactors.Add("Low movement complexity");
        }

        // Movement variability
        if (features.MovementVariability < 0.02)
        {
            score += 0.1;
            riskFactors.Add("Reduced movement variability");
        }

        // Symmetry assessment
        if (features.LeftRightSymmetry > 0.9)
        {
            score += 0.1;
            riskFactors.Add("Excessive movement symmetry (possible cramped-synchronized pattern)");
        }

        // Preterm birth considerations
        if (infantInfo.IsPreterm && infantInfo.GestationalAgeAtBirth.HasValue && infantInfo.GestationalAgeAtBirth.Value < 32)
        {
            score += 0.1;
            riskFactors.Add("Very preterm birth (< 32 weeks)");
        }

        // Additional risk factors from history
        foreach (var factor in infantInfo.RiskFactors)
        {
            if (!string.IsNullOrEmpty(factor))
            {
                riskFactors.Add(factor);
            }
        }

        return Math.Min(1.0, score);
    }

    private bool IsInFidgetyAgeRange(InfantInfo infantInfo)
    {
        var age = infantInfo.CorrectedAgeWeeks ?? infantInfo.AgeWeeks;
        return age >= MinAgeWeeksForFidgety && age <= MaxAgeWeeksForFidgety;
    }

    private double CalculateConfidence(MovementFeatures features, RiskPrediction prediction)
    {
        // Higher confidence when model scores are more certain
        var maxScore = prediction.Scores.Max();
        var minScore = prediction.Scores.Min();
        var scoreRange = maxScore - minScore;

        // Base confidence from model certainty
        var confidence = 0.5 + (scoreRange * 0.3);

        // Adjust for feature quality
        if (features.MovementComplexity > 0 || features.MovementVariability > 0)
        {
            confidence += 0.1;
        }

        return Math.Min(0.95, Math.Max(0.5, confidence));
    }

    private FeatureBreakdown CreateFeatureBreakdown(MovementFeatures features, InfantInfo infantInfo)
    {
        return new FeatureBreakdown
        {
            MovementComplexity = new MovementFeatureStatus
            {
                Value = features.MovementComplexity,
                Status = GetFeatureStatus(features.MovementComplexity, 0.3, 0.5, true)
            },
            MovementVariability = new MovementFeatureStatus
            {
                Value = features.MovementVariability,
                Status = GetFeatureStatus(features.MovementVariability, 0.02, 0.05, true)
            },
            FidgetyMovements = new MovementFeatureStatus
            {
                Value = features.FidgetyScore,
                Status = IsInFidgetyAgeRange(infantInfo)
                    ? GetFeatureStatus(features.FidgetyScore, 0.3, 0.5, true)
                    : "not_assessed"
            },
            Symmetry = new MovementFeatureStatus
            {
                Value = features.LeftRightSymmetry,
                Status = GetSymmetryStatus(features.LeftRightSymmetry)
            }
        };
    }

    private string GetFeatureStatus(double value, double abnormalThreshold, double borderlineThreshold, bool higherIsBetter)
    {
        if (higherIsBetter)
        {
            return value switch
            {
                < abnormalThreshold => "abnormal",
                < borderlineThreshold => "borderline",
                _ => "normal"
            };
        }
        else
        {
            return value switch
            {
                > abnormalThreshold => "abnormal",
                > borderlineThreshold => "borderline",
                _ => "normal"
            };
        }
    }

    private string GetSymmetryStatus(double value)
    {
        // For symmetry, very high values indicate problem (cramped-synchronized)
        // Very low values also indicate problem
        return value switch
        {
            > 0.9 => "abnormal",  // Too symmetrical
            < 0.4 => "abnormal",  // Too asymmetrical
            > 0.8 => "borderline",
            _ => "normal"
        };
    }

    private void GenerateRecommendations(string overallRisk, string classification, 
        InfantInfo infantInfo, List<string> recommendations)
    {
        if (overallRisk == "high")
        {
            recommendations.Add("Urgent referral to pediatric neurologist recommended");
            recommendations.Add("Consider brain MRI if not already performed");
            
            if (classification == "cramped_synchronized")
            {
                recommendations.Add("High specificity for CP - early intervention critical");
            }
            else if (classification == "absent_fidgety")
            {
                recommendations.Add("Absent fidgety movements strongly associated with CP");
            }
        }
        else if (overallRisk == "medium")
        {
            recommendations.Add("Follow-up GMA assessment recommended in 2-4 weeks");
            recommendations.Add("Consider referral to developmental pediatrician");
        }
        else
        {
            recommendations.Add("Continue routine developmental monitoring");
        }

        // Age-specific recommendations
        var age = infantInfo.CorrectedAgeWeeks ?? infantInfo.AgeWeeks;
        if (age < 9)
        {
            recommendations.Add("Repeat assessment at 9-12 weeks (corrected age) for fidgety movement evaluation");
        }
        else if (age > 20)
        {
            recommendations.Add("Note: GMA most predictive at 9-20 weeks; consider additional assessments");
        }

        // Preterm-specific recommendations
        if (infantInfo.IsPreterm)
        {
            recommendations.Add($"Use corrected age ({infantInfo.CorrectedAgeWeeks ?? infantInfo.AgeWeeks} weeks) for assessment");
        }
    }
}
