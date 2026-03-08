using CP_Sight.Core.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace CP_Sight.ML.Services;

/// <summary>
/// Unified pose estimation service that automatically selects the best available method
/// 
/// Priority order:
/// 1. MoveNet ONNX (fast, accurate, single-person)
/// 2. MediaPipe (more features, requires Python bridge)
/// 3. Simulation (fallback, statistically valid for demos)
/// </summary>
public class PoseService : IDisposable
{
    private readonly MoveNetPoseEstimator? _moveNet;
    private readonly MediaPipePoseEstimator? _mediaPipe;
    private readonly ILogger<PoseService> _logger;
    private readonly PoseServiceSettings _settings;
    
    public PoseService(
        IOptions<PoseServiceSettings> settings,
        ILogger<PoseService> logger,
        ILogger<MoveNetPoseEstimator>? moveNetLogger = null,
        ILogger<MediaPipePoseEstimator>? mediaPipeLogger = null)
    {
        _settings = settings.Value;
        _logger = logger;
        
        // Initialize MoveNet if model exists
        var moveNetPath = _settings.MoveNetModelPath;
        if (!string.IsNullOrEmpty(moveNetPath) && File.Exists(moveNetPath))
        {
            _moveNet = new MoveNetPoseEstimator(moveNetPath, moveNetLogger);
            _logger.LogInformation("MoveNet initialized from {Path}", moveNetPath);
        }
        
        // Initialize MediaPipe if configured
        if (_settings.UseMediaPipe)
        {
            _mediaPipe = new MediaPipePoseEstimator(
                _settings.PythonPath,
                _settings.MediaPipeScriptPath,
                false,
                mediaPipeLogger);
            _logger.LogInformation("MediaPipe initialized (simulation mode)");
        }
        
        LogStatus();
    }

    private void LogStatus()
    {
        var status = GetStatus();
        _logger.LogInformation(
            "PoseService Status - MoveNet: {MoveNet}, MediaPipe: {MediaPipe}, Simulation: {Simulation}",
            status.MoveNetAvailable ? "Available" : "Not Available",
            status.MediaPipeAvailable ? "Available" : "Not Available",
            status.UsingSimulation ? "Active" : "Inactive");
    }

    /// <summary>
    /// Get the current status of pose estimation methods
    /// </summary>
    public PoseServiceStatus GetStatus()
    {
        return new PoseServiceStatus
        {
            MoveNetAvailable = _moveNet?.IsRealML ?? false,
            MediaPipeAvailable = _mediaPipe?.IsRealML ?? false,
            UsingSimulation = (_moveNet?.IsRealML ?? false) == false && 
                             (_mediaPipe?.IsRealML ?? false) == false,
            ActiveMethod = GetActiveMethod()
        };
    }

    private string GetActiveMethod()
    {
        if (_moveNet?.IsRealML == true) return "MoveNet ONNX";
        if (_mediaPipe?.IsRealML == true) return "MediaPipe";
        return "Simulation";
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
        else if (_mediaPipe?.IsRealML == true)
        {
            joints = _mediaPipe.ExtractPose(imageBytes);
            method = "MediaPipe";
        }
        else if (_moveNet != null)
        {
            joints = _moveNet.ExtractPose(imageBytes); // Will use simulation
            method = "Simulation";
        }
        else if (_mediaPipe != null)
        {
            joints = _mediaPipe.ExtractPose(imageBytes); // Will use simulation
            method = "Simulation";
        }
        else
        {
            // Fallback simulation
            joints = GenerateSimulatedPose();
            method = "Simulation";
        }

        sw.Stop();

        return new PoseEstimationResult
        {
            Joints = joints,
            Method = method,
            ProcessingTimeMs = sw.ElapsedMilliseconds,
            IsRealML = method != "Simulation"
        };
    }

    /// <summary>
    /// Extract poses from multiple frames
    /// </summary>
    public List<PoseFrame> ExtractPosesFromFrames(List<byte[]> frames, double fps = 30.0)
    {
        var poseFrames = new List<PoseFrame>();
        
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
    /// Generate simulated pose for fallback
    /// </summary>
    private Dictionary<string, JointPosition> GenerateSimulatedPose()
    {
        var random = new Random();
        var joints = new Dictionary<string, JointPosition>();
        
        var basePositions = new Dictionary<string, (double x, double y)>
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

        var movementScale = 0.03;
        
        foreach (var (jointName, (baseX, baseY)) in basePositions)
        {
            var offsetX = (random.NextDouble() - 0.5) * movementScale;
            var offsetY = (random.NextDouble() - 0.5) * movementScale;
            
            joints[jointName] = new JointPosition
            {
                X = Math.Clamp(baseX + offsetX, 0.05, 0.95),
                Y = Math.Clamp(baseY + offsetY, 0.05, 0.95),
                Confidence = 0.85 + random.NextDouble() * 0.10
            };
        }
        
        return joints;
    }

    public void Dispose()
    {
        _moveNet?.Dispose();
        _mediaPipe?.Dispose();
    }
}

/// <summary>
/// Settings for pose estimation service
/// </summary>
public class PoseServiceSettings
{
    public string MoveNetModelPath { get; set; } = "Models/movenet.onnx";
    public bool UseMediaPipe { get; set; } = false;
    public string PythonPath { get; set; } = "python3";
    public string MediaPipeScriptPath { get; set; } = "Scripts/mediapipe_bridge.py";
}

/// <summary>
/// Status of the pose estimation service
/// </summary>
public class PoseServiceStatus
{
    public bool MoveNetAvailable { get; set; }
    public bool MediaPipeAvailable { get; set; }
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
