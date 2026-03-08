using CP_Sight.Web.Services;
using CP_Sight.Core.Services;
using CP_Sight.ML.Services;
using MudBlazor.Services;

using CP_Sight.Web.Components;
using CP_Sight.Web;

using DotNetEnv;

// Load environment variables securely from .env file
Env.Load();
var builder = WebApplication.CreateBuilder(args);

// ========================================
// LOGGING CONFIGURATION
// ========================================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// ========================================
// SERVICES CONFIGURATION
// ========================================

// Add Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor UI components
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
});

// Add HttpClient for Cloudinary and external API calls
builder.Services.AddHttpClient();

// ========================================
// CLOUDINARY CONFIGURATION
// ========================================
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddScoped<CloudinaryService>();

// ========================================
// POSE ESTIMATION - Real ML Integration
// ========================================
var env = builder.Environment;
var modelPath = builder.Configuration["PoseEstimation:ModelPath"] 
    ?? Path.Combine(env.ContentRootPath, "Models", "movenet.onnx");

builder.Services.Configure<PoseServiceSettings>(options =>
{
    options.MoveNetModelPath = modelPath;
    options.UseMediaPipe = builder.Configuration.GetValue<bool>("PoseEstimation:UseMediaPipe");
});

// Register PoseService as singleton (loads model once)
builder.Services.AddSingleton<PoseService>();

// ========================================
// ML & ANALYSIS SERVICES
// ========================================
builder.Services.AddScoped<FeatureExtractor>();
builder.Services.AddScoped<MovementClassifier>();
builder.Services.AddScoped<RiskAssessmentService>();
builder.Services.AddScoped<ReportGenerator>();

// ========================================
// HEALTH CHECKS
// ========================================
builder.Services.AddHealthChecks();

var app = builder.Build();

// ========================================
// LOG STARTUP INFO
// ========================================
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("=== CP-sight Starting ===");
logger.LogInformation("Environment: {Env}", env.EnvironmentName);
logger.LogInformation("Model Path: {Path}", modelPath);
logger.LogInformation("Model Exists: {Exists}", File.Exists(modelPath));

// ========================================
// APPLICATION PIPELINE
// ========================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// ========================================
// API ENDPOINTS
// ========================================

// Video upload endpoint
app.MapPost("/api/upload", async (
    IFormFile video, 
    CloudinaryService cloudinary,
    ILogger<Program> log) =>
{
    try
    {
        if (video == null || video.Length == 0)
            return Results.BadRequest(new { error = "No video file provided" });

        var allowedTypes = new[] { "video/mp4", "video/quicktime", "video/x-msvideo", "video/webm" };
        if (!allowedTypes.Contains(video.ContentType.ToLower()))
            return Results.BadRequest(new { error = "Invalid video format. Supported: MP4, MOV, AVI, WebM" });

        if (video.Length > 100 * 1024 * 1024)
            return Results.BadRequest(new { error = "Video file too large. Maximum: 100MB" });

        log.LogInformation("Uploading video: {FileName} ({Size} bytes)", video.FileName, video.Length);

        using var stream = video.OpenReadStream();
        var publicId = $"cp-sight-{Guid.NewGuid():N}";
        var result = await cloudinary.UploadVideoAsync(stream, publicId);

        return Results.Ok(new
        {
            success = true,
            publicId = result.PublicId,
            url = result.SecureUrl,
            duration = result.Duration,
            width = result.Width,
            height = result.Height,
            format = result.Format
        });
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Video upload failed");
        return Results.Problem("Video upload failed: " + ex.Message);
    }
});

// Pose estimation status endpoint
app.MapGet("/api/pose/status", (PoseService poseService) =>
{
    var status = poseService.GetStatus();
    return Results.Ok(new
    {
        success = true,
        moveNetAvailable = status.MoveNetAvailable,
        mediaPipeAvailable = status.MediaPipeAvailable,
        usingSimulation = status.UsingSimulation,
        activeMethod = status.ActiveMethod
    });
});

// Pose estimation from image endpoint
app.MapPost("/api/pose/estimate", async (
    IFormFile image,
    PoseService poseService,
    ILogger<Program> log) =>
{
    try
    {
        using var ms = new MemoryStream();
        await image.CopyToAsync(ms);
        var imageBytes = ms.ToArray();

        var result = poseService.ExtractPose(imageBytes);

        return Results.Ok(new
        {
            success = true,
            method = result.Method,
            isRealML = result.IsRealML,
            processingTimeMs = result.ProcessingTimeMs,
            jointCount = result.Joints.Count,
            joints = result.Joints
        });
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Pose estimation failed");
        return Results.Problem("Pose estimation failed: " + ex.Message);
    }
});

// Full analysis endpoint
app.MapPost("/api/analyze", async (
    IFormFile video,
    int ageWeeks,
    bool isPreterm,
    int? correctedAgeWeeks,
    int? gestationalAge,
    string? riskFactors,
    CloudinaryService cloudinary,
    PoseService poseService,
    FeatureExtractor featureExtractor,
    MovementClassifier classifier,
    RiskAssessmentService riskService,
    ILogger<Program> log) =>
{
    try
    {
        log.LogInformation("Starting analysis for {Age} week old infant", ageWeeks);

        // 1. Upload video
        using var stream = video.OpenReadStream();
        var publicId = $"cp-sight-{Guid.NewGuid():N}";
        var videoResult = await cloudinary.UploadVideoAsync(stream, publicId);

        // 2. Extract frames from Cloudinary
        var frames = await cloudinary.ExtractFramesAsync(publicId, 30);
        log.LogInformation("Extracted {Count} frames", frames.Count);

        // 3. Generate pose data (using MoveNet/MediaPipe/Simulation)
        var poseData = new List<CP_Sight.Core.Models.PoseFrame>();
        var poseStatus = poseService.GetStatus();
        
        for (int i = 0; i < frames.Count; i++)
        {
            // In production, download each frame and run pose estimation
            // For now, use simulation
            var poseResult = poseService.ExtractPose(Array.Empty<byte>());
            var joints = poseResult.Joints;
            poseData.Add(new CP_Sight.Core.Models.PoseFrame
            {
                FrameNumber = i,
                Timestamp = i / 30.0,
                Joints = joints
            });
        }

        // 4. Extract features
        var features = featureExtractor.ExtractFeatures(poseData);

        // 5. ML classification
        var prediction = classifier.Predict(features);

        // 6. Risk assessment
        var infantInfo = new CP_Sight.Core.Models.InfantInfo
        {
            AgeWeeks = ageWeeks,
            IsPreterm = isPreterm,
            CorrectedAgeWeeks = correctedAgeWeeks,
            GestationalAgeAtBirth = gestationalAge,
            RiskFactors = riskFactors?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()
        };

        var assessment = riskService.Assess(features, prediction, infantInfo);

        log.LogInformation("Analysis complete: {Risk} risk ({Score}%)", 
            assessment.OverallRisk, assessment.RiskScore);

        return Results.Ok(new
        {
            success = true,
            analysisId = Guid.NewGuid().ToString("N"),
            timestamp = DateTime.UtcNow,
            poseMethod = poseStatus.ActiveMethod,
            videoInfo = new
            {
                publicId = videoResult.PublicId,
                url = videoResult.SecureUrl,
                duration = videoResult.Duration
            },
            features = features,
            prediction = prediction.Classification,
            assessment = assessment
        });
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Analysis failed");
        return Results.Problem("Analysis failed: " + ex.Message);
    }
});

// Extract frames endpoint
app.MapPost("/api/extract-frames", async (
    string publicId,
    int fps,
    CloudinaryService cloudinary) =>
{
    var frames = await cloudinary.ExtractFramesAsync(publicId, fps);
    return Results.Ok(new { publicId, frameCount = frames.Count, frames });
});

// Get video info endpoint
app.MapGet("/api/video/{publicId}", async (
    string publicId,
    CloudinaryService cloudinary) =>
{
    var info = await cloudinary.GetVideoInfoAsync(publicId);
    return Results.Ok(info);
});

// Delete video endpoint
app.MapDelete("/api/video/{publicId}", async (
    string publicId,
    CloudinaryService cloudinary) =>
{
    var deleted = await cloudinary.DeleteVideoAsync(publicId);
    return deleted ? Results.Ok(new { success = true }) : Results.NotFound();
});

// Health check
app.MapHealthChecks("/health");

// API info
app.MapGet("/api", () => Results.Ok(new
{
    name = "CP-sight API",
    version = "1.0.0",
    endpoints = new[]
    {
        "POST /api/upload - Upload video",
        "POST /api/analyze - Full analysis (video + infant info)",
        "POST /api/pose/estimate - Estimate pose from image",
        "GET /api/pose/status - Get pose estimation status",
        "POST /api/extract-frames - Extract frames from video",
        "GET /api/video/{publicId} - Get video info",
        "DELETE /api/video/{publicId} - Delete video",
        "GET /health - Health check"
    }
}));

// ========================================
// BLAZOR APP
// ========================================
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
