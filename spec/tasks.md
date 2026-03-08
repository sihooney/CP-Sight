# CP-sight Development Tasks

## Task Overview

This document contains prioritized tasks for continuing CP-sight development. Tasks are organized by category and include acceptance criteria.

---

## Priority Legend

| Priority | Meaning |
|----------|---------|
| 🔴 Critical | Must complete for core functionality |
| 🟡 High | Important for production readiness |
| 🟢 Medium | Enhancement / Nice to have |
| 🔵 Low | Future consideration |

---

## Current Status Summary

| Component | Status | Notes |
|-----------|--------|-------|
| Video Upload (Cloudinary) | ✅ Complete | All features working |
| Pose Estimation (MoveNet) | ✅ Complete | ONNX integration ready |
| Pose Estimation (MediaPipe) | ✅ Complete | Python bridge ready |
| Feature Extraction | ✅ Complete | All algorithms implemented |
| ML Classification | ✅ Complete | ML.NET trained |
| Risk Assessment | ✅ Complete | GMA-based scoring |
| PDF Reports | ✅ Complete | QuestPDF working |
| Blazor UI | ✅ Complete | All pages working |
| Unit Tests | ⚠️ Partial | Core tests exist |
| API Documentation | ⚠️ Partial | Endpoints documented |

---

## 1. ML Model Tasks

### TASK-001: Add Real MoveNet ONNX Model 🔴

**Description:** Add your downloaded MoveNet ONNX model to enable real pose estimation.

**Context:** The `MoveNetPoseEstimator` is ready but needs the model file.

**Steps:**
1. Download model from: https://huggingface.co/Xenova/movenet-singlepose-lightning/blob/main/onnx/model.onnx
2. Save as: `dotnet/CP-sight.Web/Models/movenet.onnx`
3. Run: `dotnet build && dotnet run --project CP-sight.Web`
4. Verify: `curl https://localhost:5001/api/pose/status` shows `"activeMethod": "MoveNet ONNX"`

**Acceptance Criteria:**
- [ ] Model file exists at correct path
- [ ] Application starts without errors
- [ ] `/api/pose/status` returns `"moveNetAvailable": true`
- [ ] Pose estimation works on test images

**Estimated Effort:** 5 minutes

---

### TASK-002: Implement Image Preprocessing for MoveNet 🟡

**Description:** Add proper image preprocessing using SixLabors.ImageSharp.

**Context:** Currently uses placeholder preprocessing. Need to:
1. Decode JPEG/PNG images
2. Resize to 192x192
3. Normalize pixel values
4. Convert to NCHW tensor format

**Files to Modify:**
- `dotnet/CP-sight.ML/Services/MoveNetPoseEstimator.cs`

**Acceptance Criteria:**
- [ ] Install SixLabors.ImageSharp
- [ ] Decode image from bytes
- [ ] Resize maintaining aspect ratio
- [ ] Normalize to model expected range
- [ ] Test with actual images

**Estimated Effort:** 2-3 hours

---

### TASK-003: Add GPU Acceleration for ONNX 🟢

**Description:** Enable CUDA GPU acceleration for faster inference.

**Acceptance Criteria:**
- [ ] Add CUDA NuGet package
- [ ] Configure session options
- [ ] Fallback to CPU if GPU unavailable
- [ ] Benchmark CPU vs GPU performance

**Estimated Effort:** 2-4 hours

---

## 2. API Tasks

### TASK-004: Add Input Validation 🔴

**Description:** Implement comprehensive input validation using FluentValidation.

**Files to Create:**
- `dotnet/CP-sight.Web/Validators/VideoUploadValidator.cs`
- `dotnet/CP-sight.Web/Validators/AnalysisRequestValidator.cs`

**Acceptance Criteria:**
- [ ] Install FluentValidation
- [ ] Validate video file type, size
- [ ] Validate age range (0-60 weeks)
- [ ] Return 400 with detailed errors

**Estimated Effort:** 2-3 hours

---

### TASK-005: Add API Authentication 🟡

**Description:** Secure API endpoints with JWT or API keys.

**Acceptance Criteria:**
- [ ] Configure JWT settings
- [ ] Add authentication middleware
- [ ] Protect endpoints with `[Authorize]`
- [ ] Document auth requirements

**Estimated Effort:** 3-4 hours

---

### TASK-006: Add Swagger/OpenAPI Documentation 🟡

**Description:** Add Swagger UI for API exploration.

**Acceptance Criteria:**
- [ ] Install Swashbuckle.AspNetCore
- [ ] Configure in Program.cs
- [ ] Add XML comments to all endpoints
- [ ] Test in Swagger UI

**Estimated Effort:** 1-2 hours

---

## 3. UI Tasks

### TASK-007: Add Real-Time Progress Updates 🟡

**Description:** Show real-time progress using SignalR.

**Acceptance Criteria:**
- [ ] Add SignalR to project
- [ ] Create `AnalysisHub`
- [ ] Show progress bar with current stage
- [ ] Handle connection errors

**Progress Stages:**
```
0%   - Starting
20%  - Uploading video
40%  - Extracting frames
60%  - Running pose estimation
80%  - Analyzing movements
100% - Complete
```

**Estimated Effort:** 3-4 hours

---

### TASK-008: Improve Results Visualization 🟢

**Description:** Add animated skeleton and detailed charts.

**Acceptance Criteria:**
- [ ] Create animated skeleton component
- [ ] Add movement timeline chart
- [ ] Add normal range comparison
- [ ] Add JSON/CSV export

**Estimated Effort:** 4-6 hours

---

## 4. Infrastructure Tasks

### TASK-009: Add Database Persistence 🟡

**Description:** Store analysis results in database.

**Database Options:** SQLite (simplest) or PostgreSQL

**Acceptance Criteria:**
- [ ] Add Entity Framework Core
- [ ] Create `AnalysisRecord` entity
- [ ] Add migration scripts
- [ ] Create repository pattern
- [ ] Add GET `/api/analysis/history` endpoint

**Estimated Effort:** 4-6 hours

---

### TASK-010: Add Docker Support 🟢

**Description:** Containerize application for deployment.

**Files to Create:**
- `Dockerfile`
- `docker-compose.yml`

**Acceptance Criteria:**
- [ ] Create multi-stage Dockerfile
- [ ] Configure environment variables
- [ ] Test container startup
- [ ] Document deployment steps

**Estimated Effort:** 2-3 hours

---

### TASK-011: Add Background Job Processing 🟡

**Description:** Move long analyses to background jobs.

**Implementation:** Use Hangfire or BackgroundService

**Acceptance Criteria:**
- [ ] Install Hangfire
- [ ] Configure job storage
- [ ] Create `ProcessVideoJob`
- [ ] Add job status endpoint
- [ ] Add job cancellation

**Estimated Effort:** 4-6 hours

---

## 5. Testing Tasks

### TASK-012: Add Integration Tests 🔴

**Description:** Create integration tests for all API endpoints.

**Acceptance Criteria:**
- [ ] Create test server fixture
- [ ] Test `/api/upload`
- [ ] Test `/api/pose/estimate`
- [ ] Test `/api/analyze`
- [ ] Test error handling

**Estimated Effort:** 3-5 hours

---

### TASK-013: Add End-to-End Tests 🟢

**Description:** Create E2E tests using Playwright.

**Acceptance Criteria:**
- [ ] Set up Playwright
- [ ] Test complete upload flow
- [ ] Test results display
- [ ] Add to CI pipeline

**Estimated Effort:** 4-6 hours

---

## 6. Clinical Validation Tasks

### TASK-014: Validate Against Clinical Data 🔴

**Description:** Compare algorithm results to expert GMA assessments.

**Prerequisites:**
- IRB approval
- Clinically validated dataset

**Acceptance Criteria:**
- [ ] Obtain validation dataset
- [ ] Run algorithm on dataset
- [ ] Calculate sensitivity/specificity
- [ ] Document discrepancies
- [ ] Adjust thresholds if needed

**Estimated Effort:** 8-16 hours

---

### TASK-015: Add Clinical Audit Logging 🟡

**Description:** Log all analyses for clinical audit trail.

**Acceptance Criteria:**
- [ ] Log all inputs and outputs
- [ ] Include timestamps and user info
- [ ] Secure log storage
- [ ] Add export functionality

**Estimated Effort:** 2-3 hours

---

## 7. Documentation Tasks

### TASK-016: Create Developer Setup Guide 🟡

**Description:** Comprehensive guide for new developers.

**Content:**
- Prerequisites checklist
- Step-by-step setup
- Troubleshooting guide
- Development workflow

**Estimated Effort:** 2-3 hours

---

### TASK-017: Create Clinical User Guide 🟡

**Description:** Guide for healthcare professionals.

**Content:**
- How to record videos
- Interpreting results
- When to refer
- Limitations

**Estimated Effort:** 2-3 hours

---

## Task Dependencies

```
TASK-001 (Add ONNX Model) ──┬──▶ TASK-002 (Image Preprocessing)
                            │
                            └──▶ TASK-014 (Clinical Validation)

TASK-004 (Validation) ──────▶ TASK-012 (Integration Tests)

TASK-009 (Database) ────────▶ TASK-011 (Background Jobs)
```

---

## Quick Start for New Developers

### Most Impactful First Tasks

1. **TASK-001** - Add MoveNet ONNX model (5 min)
2. **TASK-006** - Add Swagger documentation (1-2 hrs)
3. **TASK-004** - Add input validation (2-3 hrs)

### Low-Risk Starter Tasks

1. **TASK-016** - Create developer setup guide
2. **TASK-010** - Add Docker support
3. **TASK-015** - Add audit logging

---

## Notes for AI IDE Assistants

When working on these tasks:

1. **Namespace consistency:** All namespaces use `CP_Sight.*` pattern
2. **Follow existing patterns:** Check similar files before implementing
3. **Maintain ClinicalContext:** Risk calculations must align with GMA research
4. **Security first:** Never expose API secrets in logs or responses
5. **Test thoroughly:** Medical applications require high reliability
6. **Document assumptions:** Note any clinical assumptions made

### Key Files to Reference

- `spec/requirements.md` - All requirements
- `spec/design.md` - Architecture details
- `spec/context.md` - Clinical background
- `dotnet/ML_MODELS_GUIDE.md` - Model setup
- `datasets/MINI-RGBD_DOWNLOAD_GUIDE.md` - Training data
