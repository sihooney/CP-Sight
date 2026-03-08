# CP-sight Project Requirements

## Project Overview

**Name:** CP-sight  
**Type:** AI-Powered Healthcare Screening Application  
**Purpose:** Early Cerebral Palsy (CP) Detection through Video Analysis  
**Target Users:** Healthcare professionals, researchers, caregivers  
**Development Context:** 24-hour Hackathon Project → Production Enhancement

---

## 1. Functional Requirements

### 1.1 Video Upload and Processing (Cloudinary)

| ID | Requirement | Priority | Status |
|----|-------------|----------|--------|
| FR-001 | Users must be able to upload infant movement videos (MP4, MOV, AVI, WebM) | Critical | ✅ Implemented |
| FR-002 | System must validate video file size (max 100MB) | High | ✅ Implemented |
| FR-003 | System must validate video format | High | ✅ Implemented |
| FR-004 | System must upload videos to Cloudinary for cloud storage | Critical | ✅ Implemented |
| FR-005 | System must extract video frames for analysis | Critical | ✅ Implemented |
| FR-006 | System must generate video thumbnails/previews | Medium | ✅ Implemented |
| FR-007 | System must delete videos after analysis for privacy compliance | High | ✅ Implemented |
| FR-008 | System must provide signed URLs for secure video access | High | ✅ Implemented |
| FR-009 | System must transcode videos to optimized formats | Medium | ✅ Implemented |

### 1.2 Pose Estimation (MoveNet ONNX + MediaPipe)

| ID | Requirement | Priority | Status |
|----|-------------|----------|--------|
| FR-010 | System must extract 17-keypoint skeleton from each frame | Critical | ✅ Implemented |
| FR-011 | Keypoints must include: nose, eyes, ears, shoulders, elbows, wrists, hips, knees, ankles | Critical | ✅ Implemented |
| FR-012 | Each keypoint must have X, Y coordinates and confidence score | Critical | ✅ Implemented |
| FR-013 | System must use ONNX Runtime for MoveNet inference | High | ✅ Implemented |
| FR-014 | System must handle frames with low confidence poses gracefully | High | ✅ Implemented |
| FR-015 | System must support MediaPipe as alternative pose estimator | Medium | ✅ Implemented |
| FR-016 | System must auto-fallback to simulation when model unavailable | High | ✅ Implemented |
| FR-017 | Pose estimation status must be queryable via API | Medium | ✅ Implemented |

### 1.3 Movement Feature Extraction

| ID | Requirement | Priority | Status |
|----|-------------|----------|--------|
| FR-020 | System must calculate Movement Complexity score | Critical | ✅ Implemented |
| FR-021 | System must calculate Movement Variability score | Critical | ✅ Implemented |
| FR-022 | System must calculate Left-Right Symmetry score | Critical | ✅ Implemented |
| FR-023 | System must calculate Fidgety Movement score (9-20 week age range) | Critical | ✅ Implemented |
| FR-024 | System must calculate Cramped-Synchronized score | High | ✅ Implemented |
| FR-025 | System must calculate Poor Repertoire score | High | ✅ Implemented |
| FR-026 | System must calculate average and peak movement speeds | Medium | ✅ Implemented |
| FR-027 | System must calculate dominant movement frequency | Medium | ✅ Implemented |

### 1.4 Risk Classification (ML.NET)

| ID | Requirement | Priority | Status |
|----|-------------|----------|--------|
| FR-030 | System must classify movements into 4 categories: Normal, Poor Repertoire, Cramped-Synchronized, Absent Fidgety | Critical | ✅ Implemented |
| FR-031 | System must calculate overall risk score (0-100) | Critical | ✅ Implemented |
| FR-032 | System must determine risk level: Low, Medium, High | Critical | ✅ Implemented |
| FR-033 | System must provide confidence score for classification | High | ✅ Implemented |
| FR-034 | System must consider infant age for assessment validity | High | ✅ Implemented |
| FR-035 | System must account for preterm birth (corrected age) | High | ✅ Implemented |
| FR-036 | System must generate feature breakdown for each assessment | High | ✅ Implemented |

### 1.5 Infant Information Collection

| ID | Requirement | Priority | Status |
|----|-------------|----------|--------|
| FR-040 | System must collect infant age in weeks | Critical | ✅ Implemented |
| FR-041 | System must collect corrected age for preterm infants | High | ✅ Implemented |
| FR-042 | System must identify preterm birth status | High | ✅ Implemented |
| FR-043 | System must collect gestational age at birth | Medium | ✅ Implemented |
| FR-044 | System must collect additional risk factors | Medium | ✅ Implemented |

### 1.6 Report Generation (QuestPDF)

| ID | Requirement | Priority | Status |
|----|-------------|----------|--------|
| FR-050 | System must generate PDF reports | High | ✅ Implemented |
| FR-051 | Reports must include patient information | Critical | ✅ Implemented |
| FR-052 | Reports must include risk assessment results | Critical | ✅ Implemented |
| FR-053 | Reports must include feature breakdown | High | ✅ Implemented |
| FR-054 | Reports must include clinical recommendations | High | ✅ Implemented |
| FR-055 | Reports must include medical disclaimer | Critical | ✅ Implemented |

### 1.7 User Interface (Blazor + MudBlazor)

| ID | Requirement | Priority | Status |
|----|-------------|----------|--------|
| FR-060 | System must provide web-based user interface | Critical | ✅ Implemented |
| FR-061 | UI must be responsive for mobile/tablet use | High | ✅ Implemented |
| FR-062 | UI must include landing page with project information | High | ✅ Implemented |
| FR-063 | UI must include upload form with infant information | Critical | ✅ Implemented |
| FR-064 | UI must show processing status with progress indication | High | ✅ Implemented |
| FR-065 | UI must display results with risk visualization | Critical | ✅ Implemented |
| FR-066 | UI must allow download of PDF reports | High | ✅ Implemented |

---

## 2. Non-Functional Requirements

### 2.1 Performance

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-001 | Video upload time | < 30 seconds for 100MB |
| NFR-002 | Frame extraction rate | 30 FPS via Cloudinary |
| NFR-003 | Pose estimation per frame | < 100ms (MoveNet) |
| NFR-004 | Complete analysis time | < 2 minutes for 60-second video |
| NFR-005 | Report generation | < 5 seconds |
| NFR-006 | Model loading time | < 2 seconds at startup |

### 2.2 Security

| ID | Requirement | Implementation |
|----|-------------|----------------|
| NFR-010 | API credentials in configuration | appsettings.json (dev), env vars (prod) |
| NFR-011 | Video URLs must be signed and time-limited | Cloudinary signed URLs |
| NFR-012 | Videos must be deletable after analysis | DELETE endpoint |
| NFR-013 | HTTPS must be enforced | ASP.NET Core HTTPS redirection |
| NFR-014 | Input validation on all user inputs | FluentValidation |

### 2.3 Reliability

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-020 | System availability | 99.5% |
| NFR-021 | Graceful error handling | All errors logged, user-friendly messages |
| NFR-022 | Fallback pose estimation | Simulation mode when model unavailable |

### 2.4 Compatibility

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-030 | .NET version | .NET 9 only (NOT .NET 10) |
| NFR-031 | Browser support | Chrome, Firefox, Safari, Edge (latest 2 versions) |
| NFR-032 | Platform | Windows, Linux, macOS |

---

## 3. Data Requirements

### 3.1 Input Data

| Data Type | Format | Validation | Required |
|-----------|--------|------------|----------|
| Video file | MP4, MOV, AVI, WebM | Max 100MB | Yes |
| Infant age | Integer (weeks) | 0-60 | Yes |
| Corrected age | Integer (weeks) | 0-60 | If preterm |
| Preterm status | Boolean | - | No |
| Gestational age | Integer (weeks) | 24-42 | If preterm |
| Risk factors | String array | - | No |

### 3.2 Output Data

| Data Type | Format | Description |
|-----------|--------|-------------|
| Risk classification | String enum | normal, poor_repertoire, cramped_synchronized, absent_fidgety |
| Risk score | Integer 0-100 | Composite risk percentage |
| Risk level | String | low, medium, high |
| Confidence | Float 0-1 | Model confidence score |
| Feature breakdown | Object | Individual feature scores with status |
| Recommendations | String array | Clinical guidance |
| Pose method | String | "MoveNet ONNX", "MediaPipe", or "Simulation" |

---

## 4. Integration Requirements

### 4.1 Cloudinary Integration

| ID | Requirement | Purpose |
|----|-------------|---------|
| IR-001 | Video upload to cloud storage | Secure, scalable video hosting |
| IR-002 | Frame extraction via transformation | Extract frames for analysis |
| IR-003 | Thumbnail generation | UI preview |
| IR-004 | Video deletion | Privacy compliance |
| IR-005 | Signed URLs | Secure access with expiration |

**Configured Credentials:**
- Cloud Name: `dnvw0yyib`
- API Key: `887823313431246`
- API Secret: `3X6o4aSOwBLH0_cJkLEg4XtFp14`

### 4.2 MoveNet ONNX Integration

| ID | Requirement | Purpose |
|----|-------------|---------|
| IR-010 | Load ONNX model at startup | Fast pose estimation |
| IR-011 | Preprocess images to 192x192 | Model input requirement |
| IR-012 | Parse 17-keypoint output | COCO format conversion |
| IR-013 | Report pose estimation status | API endpoint |

**Model Source:** https://huggingface.co/Xenova/movenet-singlepose-lightning

### 4.3 MediaPipe Integration

| ID | Requirement | Purpose |
|----|-------------|---------|
| IR-020 | Python bridge support | Alternative pose estimation |
| IR-021 | Map 33 landmarks to 17 keypoints | COCO format compatibility |
| IR-022 | JSON communication | stdin/stdout protocol |

### 4.4 Training Data Integration

| ID | Requirement | Purpose |
|----|-------------|---------|
| IR-030 | Sample training data included | 50 sequences for development |
| IR-031 | MINI-RGBD download guide | Training data for production |
| IR-032 | Python preprocessing scripts | Convert data to training format |

---

## 5. Constraints

### 5.1 Technical Constraints

| Constraint | Reason |
|------------|--------|
| .NET 9 only (NOT .NET 10) | User doesn't have VS2026 |
| Blazor Server (not WebAssembly) | Server-side ML processing |
| Cloudinary for video | Cloud-native, hackathon-ready |

### 5.2 Business Constraints

| Constraint | Reason |
|------------|--------|
| Hackathon project | Originally 24-hour timeline |
| Screening tool only | Not a diagnostic device |
| Research/educational use | Regulatory compliance not established |

### 5.3 Clinical Constraints

| Constraint | Reason |
|------------|--------|
| GMA 9-20 weeks optimal | Fidgety movement assessment window |
| Corrected age for preterm | Accurate assessment requires adjustment |
| Not for diagnosis | Regulatory and ethical reasons |

---

## 6. Assumptions

1. Users have basic understanding of infant motor development
2. Videos are recorded with infant lying supine
3. Videos have adequate lighting and visibility
4. Network connectivity is available for Cloudinary
5. Users accept the tool is for screening, not diagnosis

---

## 7. Dependencies

### 7.1 NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| MudBlazor | 7.8.0 | UI components |
| Microsoft.ML | 4.0.0 | Machine learning |
| Microsoft.ML.OnnxRuntime | 1.18.0 | Pose estimation |
| Microsoft.ML.OnnxRuntime.Extensions | 0.13.0 | ONNX utilities |
| SixLabors.ImageSharp | 3.1.5 | Image processing |
| CloudinaryDotNet | 1.26.2 | Video processing |
| QuestPDF | 2024.12.0 | PDF generation |

### 7.2 External Services

| Service | Purpose |
|---------|---------|
| Cloudinary | Video upload, storage, transformation |

### 7.3 Training Data

| Dataset | Source | Purpose |
|---------|--------|---------|
| Sample Data | Included (50 sequences) | Development/testing |
| MINI-RGBD | Fraunhofer IOSB | Production training |
