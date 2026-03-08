# Cloudinary Configuration Guide

## What is Cloudinary?

Cloudinary is a **cloud-based media management platform** that handles:
- **Video/Image Upload** - Secure storage in the cloud
- **Video Transformation** - Resize, transcode, enhance
- **Frame Extraction** - Extract specific frames for AI analysis
- **Content Delivery** - Fast CDN for media files

## What Cloudinary CAN and CANNOT Do

### ✅ CAN Do:
- Upload and store your infant movement videos
- Extract video frames for pose estimation
- Generate video thumbnails and previews
- Transcode videos to optimized formats
- Apply visual enhancements for better analysis

### ❌ CANNOT Do:
- Download external datasets (MINI-RGBD must be downloaded manually)
- Perform pose estimation (use ONNX Runtime for this)
- Make medical diagnoses

## Setting Up Your Cloudinary Account

### Step 1: Get Your Credentials

1. Go to https://cloudinary.com and log in
2. Navigate to **Dashboard**
3. Copy your credentials:
   - **Cloud Name**: Your unique cloud identifier
   - **API Key**: Public identifier (safe to commit)
   - **API Secret**: Secret key (NEVER commit this!)

### Step 2: Configure CP-sight

You have provided API Key: `887823313431246`

**You still need to provide:**
- Cloud Name
- API Secret

#### Option A: Environment Variables (Recommended for Production)

```bash
# Linux/Mac
export CLOUDINARY__CLOUDNAME="your-cloud-name"
export CLOUDINARY__APIKEY="887823313431246"
export CLOUDINARY__APISECRET="your-api-secret"

# Windows PowerShell
$env:CLOUDINARY__CLOUDNAME="your-cloud-name"
$env:CLOUDINARY__APIKEY="887823313431246"
$env:CLOUDINARY__APISECRET="your-api-secret"
```

#### Option B: User Secrets (Recommended for Development)

```bash
cd dotnet/CP-sight.Web
dotnet user-secrets init
dotnet user-secrets set "Cloudinary:CloudName" "your-cloud-name"
dotnet user-secrets set "Cloudinary:ApiKey" "887823313431246"
dotnet user-secrets set "Cloudinary:ApiSecret" "your-api-secret"
```

#### Option C: appsettings.Development.json (Only for local dev)

```json
{
  "Cloudinary": {
    "CloudName": "your-cloud-name",
    "ApiKey": "887823313431246",
    "ApiSecret": "your-api-secret"
  }
}
```

⚠️ **NEVER commit appsettings.Development.json with real secrets!**

## How Cloudinary Integrates with CP-sight

### Video Processing Pipeline

```
┌─────────────────┐
│  User uploads   │
│  infant video   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Cloudinary    │
│  - Stores video │
│  - Transcodes   │
│  - Optimizes    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Frame Extraction│
│  - Extract 30   │
│    frames/sec   │
│  - URLs for AI  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   ONNX Model    │
│  - Pose         │
│    estimation   │
│  - 17 joints    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   ML.NET        │
│  - Feature      │
│    extraction   │
│  - Risk scoring │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│    Results      │
│  - Risk level   │
│  - Report PDF   │
└─────────────────┘
```

### Key API Endpoints

| Endpoint | Purpose |
|----------|---------|
| `POST /api/upload` | Upload video to Cloudinary |
| `POST /api/extract-frames` | Get frame URLs for analysis |
| `GET /api/video/{id}` | Get video metadata |
| `DELETE /api/video/{id}` | Delete video (privacy) |

## Testing Cloudinary Integration

### Using curl:

```bash
# Upload a video
curl -X POST http://localhost:5000/api/upload \
  -F "video=@test_video.mp4"

# Expected response:
{
  "success": true,
  "publicId": "cp-sight-abc123",
  "url": "https://res.cloudinary.com/your-cloud/video/upload/...",
  "duration": 45.2
}
```

## Free Tier Limits

Cloudinary free tier includes:
- **25 GB** storage
- **25 GB** monthly bandwidth
- **25,000** transformations/month

This is sufficient for hackathon/demo purposes.

## Security Best Practices

1. **Never commit API Secret** to source control
2. **Use signed URLs** for video access
3. **Delete videos after analysis** for privacy
4. **Set up CORS** for browser uploads
5. **Use upload presets** for client-side uploads

## Cloudinary Dashboard Features

After uploading videos, you can:
- View all uploaded videos
- See storage/bandwidth usage
- Monitor transformation usage
- Set up webhooks for processing notifications
