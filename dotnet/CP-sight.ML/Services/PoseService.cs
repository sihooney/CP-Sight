using CP_Sight.Core.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace CP_Sight.ML.Services;

/// <summary>
/// Simulation mode for generating different GMA movement patterns
/// </summary>
public enum SimulationMode
{
    /// <summary>Demo mode with seeded random for reproducible results</summary>
    Demo,
    /// <summary>Normal fidgety movements - variable, complex, fluent</summary>
    Normal,
    /// <summary>Reduced complexity/variability - monotonous movements</summary>
    PoorRepertoire,
    /// <summary>High symmetry, low variability - rigid, simultaneous</summary>
    CrampedSynchronized,
    /// <summary>No fidgety movements - absent small movements</summary>
    AbsentFidgety
}

/// <summary>
/// Unified pose estimation service
/// 
/// Priority order:
/// 1. MoveNet ONNX (fast, accurate, single-person)
/// 2. Simulation fallback (configurable GMA patterns for demos)
/// </summary>
public class PoseService : IDisposable
{
    private readonly MoveNetPoseEstimator? _moveNet;
    private readonly ILogger<PoseService> _logger;
    private readonly PoseServiceSettings _settings;
    private readonly Random _random;
    
    // State for temporal correlation between frames
    private Dictionary<string, (double x, double y)>? _previousPositions;
    private int _frameIndex;

    public PoseService(
        IOptions<PoseServiceSettings> settings,
        ILogger<PoseService> logger,
        ILogger<MoveNetPoseEstimator>? moveNetLogger = null)
    {
        _settings = settings.Value;
        _logger = logger;
        _random = _settings.Seed.HasValue ? new Random(_settings.Seed.Value) : new Random();
        _frameIndex = 0;
        
        // Initialize MoveNet if model exists
        var moveNetPath = _settings.MoveNetModelPath;
        if (!string.IsNullOrEmpty(moveNetPath) && File.Exists(moveNetPath))
        {
            _moveNet = new MoveNetPoseEstimator(moveNetPath, moveNetLogger);
            _logger.LogInformation("MoveNet initialized from {Path}", moveNetPath);
        }
        
        LogStatus();
    }

    private void LogStatus()
    {
        var status = GetStatus();
        _logger.LogInformation(
            "PoseService Status - MoveNet: {MoveNet}, Simulation: {Simulation}, Mode: {Mode}",
            status.MoveNetAvailable ? "Available" : "Not Available",
            status.UsingSimulation ? "Active" : "Inactive",
            _settings.SimulationMode);
    }

    /// <summary>
    /// Get the current status of pose estimation methods
    /// </summary>
    public PoseServiceStatus GetStatus()
    {
        return new PoseServiceStatus
        {
            MoveNetAvailable = _moveNet?.IsRealML ?? false,
            UsingSimulation = (_moveNet?.IsRealML ?? false) == false,
            ActiveMethod = GetActiveMethod()
        };
    }

    private string GetActiveMethod()
    {
        if (_moveNet?.IsRealML == true) return "MoveNet ONNX";
        return $"Simulation ({_settings.SimulationMode})";
    }

    /// <summary>
    /// Extract pose from image bytes (main entry point)
    /// </summary>
    public PoseEstimationResult ExtractPose(byte[] imageBytes)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Dictionary<string, JointPosition> joints;
        string method;

        if (_moveNet?.IsRealML == true)
        {
            joints = _moveNet.ExtractPose(imageBytes);
            method = "MoveNet ONNX";
        }
        else if (_moveNet != null)
        {
            joints = _moveNet.ExtractPose(imageBytes); // Will use simulation
            method = "Simulation";
        }
        else
        {
            // Configurable simulation fallback
            joints = GenerateSimulatedPose(_settings.SimulationMode);
            method = $"Simulation ({_settings.SimulationMode})";
        }

        sw.Stop();

        return new PoseEstimationResult
        {
            Joints = joints,
            Method = method,
            ProcessingTimeMs = sw.ElapsedMilliseconds,
            IsRealML = method.StartsWith("MoveNet")
        };
    }

    /// <summary>
    /// Extract poses from multiple frames with temporal correlation
    /// </summary>
    public List<PoseFrame> ExtractPosesFromFrames(List<byte[]> frames, double fps = 30.0)
    {
        var poseFrames = new List<PoseFrame>();
        ResetSimulationState();
        
        for (int i = 0; i < frames.Count; i++)
        {
            var result = ExtractPose(frames[i]);
            poseFrames.Add(new PoseFrame
            {
                FrameNumber = i,
                Timestamp = i / fps,
                Joints = result.Joints
            });
        }
        
        return poseFrames;
    }

    /// <summary>
    /// Generate a sequence of temporally-correlated simulated pose frames (no video input needed)
    /// </summary>
    public List<PoseFrame> GenerateSimulatedSequence(int frameCount, double fps = 30.0, SimulationMode? modeOverride = null)
    {
        var mode = modeOverride ?? _settings.SimulationMode;
        ResetSimulationState();
        var poseFrames = new List<PoseFrame>();

        for (int i = 0; i < frameCount; i++)
        {
            var joints = GenerateSimulatedPose(mode);
            poseFrames.Add(new PoseFrame
            {
                FrameNumber = i,
                Timestamp = i / fps,
                Joints = joints
            });
        }

        return poseFrames;
    }

    /// <summary>
    /// Reset temporal state for a new simulation sequence
    /// </summary>
    private void ResetSimulationState()
    {
        _previousPositions = null;
        _frameIndex = 0;
    }

    /// <summary>
    /// Generate simulated pose with configurable GMA movement patterns
    /// </summary>
    private Dictionary<string, JointPosition> GenerateSimulatedPose(SimulationMode mode)
    {
        var joints = new Dictionary<string, JointPosition>();
        var t = _frameIndex * (1.0 / 30.0); // Assume 30fps

        // Get movement parameters based on simulation mode
        var moveParams = GetMovementParameters(mode);

        foreach (var (jointName, (baseX, baseY)) in InfantBasePositions)
        {
            double offsetX, offsetY;

            if (_previousPositions != null && _previousPositions.TryGetValue(jointName, out var prev))
            {
                // Temporal correlation: smooth movement from previous position
                var targetX = baseX + ComputeMovementOffset(jointName, t, isX: true, moveParams);
                var targetY = baseY + ComputeMovementOffset(jointName, t, isX: false, moveParams);
                
                // Lerp towards target with smoothing factor
                offsetX = prev.x + (targetX - prev.x) * moveParams.Smoothing;
                offsetY = prev.y + (targetY - prev.y) * moveParams.Smoothing;
            }
            else
            {
                offsetX = baseX + ComputeMovementOffset(jointName, t, isX: true, moveParams);
                offsetY = baseY + ComputeMovementOffset(jointName, t, isX: false, moveParams);
            }

            // Add breathing simulation (subtle torso oscillation)
            if (jointName.Contains("shoulder") || jointName.Contains("hip"))
            {
                offsetY += Math.Sin(t * 2.5 * Math.PI) * 0.003; // ~0.8Hz breathing
            }

            var finalX = Math.Clamp(offsetX, 0.05, 0.95);
            var finalY = Math.Clamp(offsetY, 0.05, 0.95);

            joints[jointName] = new JointPosition
            {
                X = finalX,
                Y = finalY,
                Confidence = moveParams.BaseConfidence + _random.NextDouble() * 0.10
            };

            // Store for temporal correlation
            _previousPositions ??= new Dictionary<string, (double, double)>();
            _previousPositions[jointName] = (finalX, finalY);
        }

        _frameIndex++;
        return joints;
    }

    /// <summary>
    /// Compute movement offset for a specific joint at time t
    /// Applies different profiles based on joint type and simulation mode
    /// </summary>
    private double ComputeMovementOffset(string jointName, double t, bool isX, MovementParameters mp)
    {
        // Joint-specific amplitude scaling
        double jointScale = jointName switch
        {
            var n when n.Contains("wrist") => mp.LimbAmplitude * 1.5,
            var n when n.Contains("ankle") => mp.LimbAmplitude * 1.3,
            var n when n.Contains("elbow") || n.Contains("knee") => mp.LimbAmplitude * 1.0,
            var n when n.Contains("shoulder") || n.Contains("hip") => mp.LimbAmplitude * 0.4,
            var n when n.Contains("eye") || n.Contains("ear") => mp.LimbAmplitude * 0.2,
            _ => mp.LimbAmplitude * 0.3
        };

        // Compose multiple frequency components for realistic movement
        double offset = 0;
        
        // Primary oscillation (slow, large movements)
        var phaseShift = jointName.GetHashCode() * 0.1; // Different phase per joint
        offset += Math.Sin(t * mp.PrimaryFrequency * 2 * Math.PI + phaseShift) * jointScale;
        
        // Secondary oscillation (faster, smaller - fidgety component)
        if (mp.FidgetyAmplitude > 0)
        {
            offset += Math.Sin(t * mp.FidgetyFrequency * 2 * Math.PI + phaseShift * 1.7) 
                      * jointScale * mp.FidgetyAmplitude;
        }

        // Random noise component
        offset += (_random.NextDouble() - 0.5) * mp.NoiseScale * jointScale;

        return offset;
    }

    /// <summary>
    /// Get movement parameters for each simulation mode
    /// Tuned to produce features that the ML classifier will categorize correctly
    /// </summary>
    private static MovementParameters GetMovementParameters(SimulationMode mode)
    {
        return mode switch
        {
            SimulationMode.Demo => new MovementParameters
            {
                // Demo: Normal-looking movements, reproducible
                LimbAmplitude = 0.04,
                PrimaryFrequency = 0.5,         // 0.5 Hz - slow reaching
                FidgetyFrequency = 3.0,          // 3 Hz fidgety
                FidgetyAmplitude = 0.4,          // Present fidgety
                NoiseScale = 0.3,
                Smoothing = 0.3,
                SymmetryForce = 0.0,
                BaseConfidence = 0.85
            },
            SimulationMode.Normal => new MovementParameters
            {
                // Normal: Variable, complex, fluent movements with clear fidgety
                LimbAmplitude = 0.05,
                PrimaryFrequency = 0.4,
                FidgetyFrequency = 3.5,
                FidgetyAmplitude = 0.5,
                NoiseScale = 0.4,
                Smoothing = 0.25,
                SymmetryForce = 0.0,
                BaseConfidence = 0.88
            },
            SimulationMode.PoorRepertoire => new MovementParameters
            {
                // Poor Repertoire: Monotonous, low complexity, reduced variability
                LimbAmplitude = 0.02,
                PrimaryFrequency = 0.3,
                FidgetyFrequency = 1.5,
                FidgetyAmplitude = 0.15,
                NoiseScale = 0.1,
                Smoothing = 0.15,
                SymmetryForce = 0.0,
                BaseConfidence = 0.82
            },
            SimulationMode.CrampedSynchronized => new MovementParameters
            {
                // Cramped-Synchronized: High symmetry, low variability, rigid
                LimbAmplitude = 0.015,
                PrimaryFrequency = 0.2,
                FidgetyFrequency = 0.5,
                FidgetyAmplitude = 0.05,
                NoiseScale = 0.02,               // Very low noise = low variability
                Smoothing = 0.1,
                SymmetryForce = 0.9,
                BaseConfidence = 0.80
            },
            SimulationMode.AbsentFidgety => new MovementParameters
            {
                // Absent Fidgety: No small movements, only slow large ones
                LimbAmplitude = 0.025,
                PrimaryFrequency = 0.25,
                FidgetyFrequency = 0.3,
                FidgetyAmplitude = 0.0,           // No fidgety at all
                NoiseScale = 0.08,
                Smoothing = 0.12,
                SymmetryForce = 0.3,
                BaseConfidence = 0.78
            },
            _ => GetMovementParameters(SimulationMode.Demo)
        };
    }

    /// <summary>
    /// Base positions for infant lying supine (normalized coordinates)
    /// Based on typical infant body proportions from GMA research
    /// </summary>
    private static readonly Dictionary<string, (double x, double y)> InfantBasePositions = new()
    {
        { "nose", (0.50, 0.12) },
        { "left_eye", (0.47, 0.10) },
        { "right_eye", (0.53, 0.10) },
        { "left_ear", (0.44, 0.12) },
        { "right_ear", (0.56, 0.12) },
        { "left_shoulder", (0.38, 0.22) },
        { "right_shoulder", (0.62, 0.22) },
        { "left_elbow", (0.30, 0.32) },
        { "right_elbow", (0.70, 0.32) },
        { "left_wrist", (0.22, 0.42) },
        { "right_wrist", (0.78, 0.42) },
        { "left_hip", (0.42, 0.52) },
        { "right_hip", (0.58, 0.52) },
        { "left_knee", (0.40, 0.72) },
        { "right_knee", (0.60, 0.72) },
        { "left_ankle", (0.38, 0.90) },
        { "right_ankle", (0.62, 0.90) }
    };

    public void Dispose()
    {
        _moveNet?.Dispose();
    }
}

/// <summary>
/// Movement parameters for simulation profiles
/// </summary>
internal class MovementParameters
{
    public double LimbAmplitude { get; init; }
    public double PrimaryFrequency { get; init; }
    public double FidgetyFrequency { get; init; }
    public double FidgetyAmplitude { get; init; }
    public double NoiseScale { get; init; }
    public double Smoothing { get; init; }
    public double SymmetryForce { get; init; }
    public double BaseConfidence { get; init; }
}

/// <summary>
/// Settings for pose estimation service
/// </summary>
public class PoseServiceSettings
{
    public string MoveNetModelPath { get; set; } = "Models/model.onnx";
    public SimulationMode SimulationMode { get; set; } = SimulationMode.Demo;
    public int? Seed { get; set; } = 42;
}

/// <summary>
/// Status of the pose estimation service
/// </summary>
public class PoseServiceStatus
{
    public bool MoveNetAvailable { get; set; }
    public bool UsingSimulation { get; set; }
    public string ActiveMethod { get; set; } = "Unknown";
}

/// <summary>
/// Result of pose estimation
/// </summary>
public class PoseEstimationResult
{
    public Dictionary<string, JointPosition> Joints { get; set; } = new();
    public string Method { get; set; } = "Unknown";
    public long ProcessingTimeMs { get; set; }
    public bool IsRealML { get; set; }
}
