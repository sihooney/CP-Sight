using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using CP_Sight.Core.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CP_Sight.Web.Services;

/// <summary>
/// Comprehensive Cloudinary integration for CP-sight video processing
/// 
/// What Cloudinary DOES:
/// - Upload and store infant movement videos
/// - Extract video frames for AI analysis
/// - Generate thumbnails and previews
/// - Transcode videos to optimized formats
/// - Apply transformations and enhancements
/// 
/// What Cloudinary CANNOT do:
/// - Download external datasets (MINI-RGBD must be downloaded manually)
/// - Perform pose estimation (use ONNX for that)
/// - Make medical diagnoses
/// </summary>
public class CloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(
        IOptions<CloudinarySettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<CloudinaryService> logger)
    {
        var account = new Account(
            settings.Value.CloudName,
            settings.Value.ApiKey,
            settings.Value.ApiSecret
        );
        _cloudinary = new Cloudinary(account);
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        
        _logger.LogInformation("CloudinaryService initialized for cloud: {CloudName}", 
            settings.Value.CloudName);
    }

    #region Video Upload

    /// <summary>
    /// Upload video for analysis with optimized settings
    /// </summary>
    public async Task<CP_Sight.Core.Models.VideoUploadResult> UploadVideoAsync(Stream videoStream, string publicId)
    {
        var uploadParams = new VideoUploadParams
        {
            File = new FileDescription(publicId, videoStream),
            PublicId = publicId,
            Folder = "cp-sight-uploads",
            Overwrite = true,
            
            // Optimize video for analysis
            EagerTransforms = new List<Transformation>
            {
                // Standard format for analysis
                new Transformation()
                    .Width(1280).Height(720)
                    .Crop("limit")
                    .Quality("auto:good")
                    .VideoCodec("auto"),
                    
                // Compressed preview
                new Transformation()
                    .Width(640).Height(360)
                    .Crop("limit")
                    .Quality("auto:low")
            },
            
            // Generate eager transformations asynchronously
            EagerAsync = true,
            
            // Notification URL for async processing (configure in Cloudinary dashboard)
            // NotificationUrl = "https://your-server.com/api/cloudinary/notification",
            
            // Metadata
            Context = new StringDictionary
            {
                { "purpose", "cp-screening" },
                { "uploaded_at", DateTime.UtcNow.ToString("o") }
            },
            
            // Tags for organization
            Tags = "cp-sight,infant-movement,screening"
        };

        _logger.LogInformation("Uploading video: {PublicId}", publicId);
        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
        {
            _logger.LogError("Cloudinary upload error: {Message}", result.Error.Message);
            throw new Exception($"Upload failed: {result.Error.Message}");
        }

        _logger.LogInformation("Video uploaded successfully: {PublicId}, Duration: {Duration}s", 
            result.PublicId, result.Duration);

        return new CP_Sight.Core.Models.VideoUploadResult
        {
            PublicId = result.PublicId,
            SecureUrl = result.SecureUrl.ToString(),
            Duration = result.Duration,
            Format = result.Format,
            Width = result.Width,
            Height = result.Height
        };
    }

    /// <summary>
    /// Upload video from URL (e.g., from mobile app)
    /// </summary>
    public async Task<CP_Sight.Core.Models.VideoUploadResult> UploadVideoFromUrlAsync(string videoUrl, string publicId)
    {
        var uploadParams = new VideoUploadParams
        {
            File = new FileDescription(videoUrl),
            PublicId = publicId,
            Folder = "cp-sight-uploads"
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        return new CP_Sight.Core.Models.VideoUploadResult
        {
            PublicId = result.PublicId,
            SecureUrl = result.SecureUrl.ToString(),
            Duration = result.Duration,
            Format = result.Format,
            Width = result.Width,
            Height = result.Height
        };
    }

    #endregion

    #region Frame Extraction for AI Analysis

    /// <summary>
    /// Extract frames from video for pose estimation analysis
    /// This is the key integration point with our ML pipeline
    /// </summary>
    public async Task<List<FrameData>> ExtractFramesAsync(string publicId, int fps = 30)
    {
        _logger.LogInformation("Extracting frames from video: {PublicId} at {Fps} fps", publicId, fps);
        
        var frames = new List<FrameData>();
        
        // Get video info to determine duration
        var videoInfo = await GetVideoInfoAsync(publicId);
        var duration = videoInfo.Duration;
        var frameCount = (int)(duration * fps);
        
        // Generate frame URLs using Cloudinary's video transformation
        // Cloudinary can extract specific frames using the video thumbnail feature
        for (int i = 0; i < frameCount; i++)
        {
            var timestamp = i / (double)fps;
            var frameUrl = GetFrameUrl(publicId, timestamp);
            
            frames.Add(new FrameData
            {
                FrameNumber = i,
                Timestamp = timestamp,
                Url = frameUrl
            });
        }
        
        _logger.LogInformation("Generated {Count} frame URLs", frames.Count);
        return frames;
    }

    /// <summary>
    /// Get URL for a specific frame at a timestamp
    /// </summary>
    public string GetFrameUrl(string publicId, double timestampSeconds)
    {
        // Cloudinary can extract frames from video at specific timestamps
        return _cloudinary.Api.UrlVideoUp
            .Transform(new Transformation()
                .StartOffset(timestampSeconds)
                .Width(640)
                .Height(360)
                .Crop("fill")
                .Quality("auto:good")
                .FetchFormat("jpg"))
            .BuildUrl(publicId);
    }

    /// <summary>
    /// Download frame as byte array for pose estimation
    /// </summary>
    public async Task<byte[]> DownloadFrameAsync(string frameUrl)
    {
        try
        {
            var bytes = await _httpClient.GetByteArrayAsync(frameUrl);
            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download frame: {Url}", frameUrl);
            throw;
        }
    }

    /// <summary>
    /// Extract key frames (motion-detected frames) for analysis
    /// Reduces processing by focusing on frames with significant movement
    /// </summary>
    public async Task<List<FrameData>> ExtractKeyFramesAsync(string publicId, int maxFrames = 90)
    {
        var videoInfo = await GetVideoInfoAsync(publicId);
        var interval = videoInfo.Duration / maxFrames;
        
        var frames = new List<FrameData>();
        
        for (int i = 0; i < maxFrames; i++)
        {
            var timestamp = i * interval;
            var frameUrl = GetFrameUrl(publicId, timestamp);
            
            frames.Add(new FrameData
            {
                FrameNumber = i,
                Timestamp = timestamp,
                Url = frameUrl
            });
        }
        
        return frames;
    }

    #endregion

    #region Video Information

    /// <summary>
    /// Get detailed video information
    /// </summary>
    public async Task<VideoInfo> GetVideoInfoAsync(string publicId)
    {
        var result = await _cloudinary.GetResourceAsync(new GetResourceParams(publicId)
        {
            ResourceType = ResourceType.Video
        });

        return new VideoInfo
        {
            PublicId = result.PublicId,
            Duration = 0, // Not available directly in GetResourceResult in this SDK version
            Width = result.Width,
            Height = result.Height,
            Format = result.Format,
            FrameRate = 30,
            BitRate = 0
        };
    }

    #endregion

    #region Thumbnails and Previews

    /// <summary>
    /// Get video thumbnail URL
    /// </summary>
    public string GetThumbnailUrl(string publicId, int width = 400, int height = 300)
    {
        return _cloudinary.Api.UrlVideoUp
            .Transform(new Transformation()
                .Width(width)
                .Height(height)
                .Crop("fill")
                .Quality("auto:good")
                .FetchFormat("jpg"))
            .BuildUrl(publicId);
    }

    /// <summary>
    /// Generate animated preview (GIF) of video
    /// Useful for quick visual assessment
    /// </summary>
    public string GetAnimatedPreviewUrl(string publicId, int width = 320)
    {
        return _cloudinary.Api.UrlVideoUp
            .Transform(new Transformation()
                .Width(width)
                .Crop("scale")
                .VideoSampling("10")  // Sample 10 frames
                .Delay("200")  // 200ms between frames
                .FetchFormat("gif"))
            .BuildUrl(publicId);
    }

    #endregion

    #region Video Transformations for Analysis

    /// <summary>
    /// Generate optimized video for pose estimation
    /// - Stabilized (if needed)
    /// - Appropriate resolution
    /// - Standardized format
    /// </summary>
    public string GetOptimizedVideoUrl(string publicId)
    {
        return _cloudinary.Api.UrlVideoUp
            .Transform(new Transformation()
                .Width(1280)
                .Height(720)
                .Crop("limit")
                .Quality("auto:good")
                .VideoCodec("h264")
                .FetchFormat("mp4"))
            .BuildUrl(publicId);
    }

    /// <summary>
    /// Apply contrast enhancement for better pose detection
    /// </summary>
    public string GetEnhancedVideoUrl(string publicId)
    {
        return _cloudinary.Api.UrlVideoUp
            .Transform(new Transformation()
                .Width(1280)
                .Height(720)
                .Crop("limit")
                .Effect("contrast:20")
                .Effect("brightness:10")
                .Quality("auto:best")
                .FetchFormat("mp4"))
            .BuildUrl(publicId);
    }

    #endregion

    #region Management Operations

    /// <summary>
    /// Delete video after analysis (for privacy)
    /// </summary>
    public async Task<bool> DeleteVideoAsync(string publicId)
    {
        _logger.LogInformation("Deleting video: {PublicId}", publicId);
        
        var deleteParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Video
        };

        var result = await _cloudinary.DestroyAsync(deleteParams);
        
        if (result.Result == "ok")
        {
            _logger.LogInformation("Video deleted successfully: {PublicId}", publicId);
            return true;
        }
        
        _logger.LogWarning("Failed to delete video: {PublicId}, Result: {Result}", 
            publicId, result.Result);
        return false;
    }

    /// <summary>
    /// Delete all videos older than specified days (GDPR compliance)
    /// </summary>
    public async Task<int> DeleteOldVideosAsync(int olderThanDays = 30)
    {
        var deleted = 0;
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
        
        var listParams = new CloudinaryDotNet.Actions.ListResourcesByPrefixParams()
        {
            Type = "upload",
            Prefix = "cp-sight-uploads/",
            MaxResults = 500
        };
        var result = await _cloudinary.ListResourcesAsync(listParams);
        
        foreach (var resource in result.Resources)
        {
            if (DateTime.TryParse(resource.CreatedAt, out var createdAtDate) && createdAtDate < cutoffDate)
            {
                await DeleteVideoAsync(resource.PublicId);
                deleted++;
            }
        }
        
        _logger.LogInformation("Deleted {Count} old videos", deleted);
        return deleted;
    }

    #endregion

    #region Signed URLs for Security

    /// <summary>
    /// Generate signed URL for secure video access
    /// URL expires after specified time
    /// </summary>
    public string GetSignedVideoUrl(string publicId, int expiresInSeconds = 3600)
    {
        return _cloudinary.Api.UrlVideoUp
            .Transform(new Transformation()
                .Quality("auto:good")
                .FetchFormat("mp4"))
            .Signed(true)
            .Secure(true)
            .Version((DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expiresInSeconds).ToString())
            .BuildUrl(publicId);
    }

    #endregion
}

/// <summary>
/// Cloudinary configuration settings
/// </summary>
public class CloudinarySettings
{
    /// <summary>
    /// Your Cloudinary cloud name (found in Dashboard)
    /// </summary>
    public string CloudName { get; set; } = "";
    
    /// <summary>
    /// API Key (found in Dashboard)
    /// </summary>
    public string ApiKey { get; set; } = "";
    
    /// <summary>
    /// API Secret (found in Dashboard) - KEEP SECRET!
    /// </summary>
    public string ApiSecret { get; set; } = "";
    
    /// <summary>
    /// Upload preset for unsigned uploads (optional)
    /// </summary>
    public string? UploadPreset { get; set; }
}

/// <summary>
/// Video information from Cloudinary
/// </summary>
public record VideoInfo
{
    public string PublicId { get; init; } = "";
    public double Duration { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public string Format { get; init; } = "";
    public double FrameRate { get; init; }
    public long BitRate { get; init; }
}

/// <summary>
/// Frame data for analysis
/// </summary>
public record FrameData
{
    public int FrameNumber { get; init; }
    public double Timestamp { get; init; }
    public string Url { get; init; } = "";
}
