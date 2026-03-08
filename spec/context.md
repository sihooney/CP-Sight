# CP-sight Project Context

## Executive Summary

**CP-sight** is an AI-powered screening application for early detection of Cerebral Palsy (CP) in infants. It analyzes infant movement videos using computer vision and machine learning to identify abnormal movement patterns associated with CP risk.

**Development Origin:** 24-hour Hackathon Project → Production Enhancement

**Current Status:** Complete implementation with MoveNet ONNX and MediaPipe integration ready.

---

## 1. Problem Statement

### The Clinical Need

- **Cerebral Palsy** is the most common motor disability in childhood (2-3 per 1,000 live births)
- **Early detection is critical**: Intervention in the first years significantly improves outcomes
- **Traditional diagnosis** often occurs at age 2+, missing the optimal intervention window
- **GMA is validated**: Prechtl's General Movements Assessment has >90% specificity for CP
- **Access is limited**: Few clinicians are trained in GMA; access is limited in resource-poor settings

### The Solution

CP-sight democratizes GMA by:
1. Using smartphone videos (accessible anywhere)
2. Automating pose estimation (computer vision)
3. Applying ML classification (consistent assessment)
4. Providing instant results (no specialist needed on-site)

---

## 2. Technical Context

### Tech Stack Decision Rationale

| Technology | Why Chosen |
|------------|------------|
| **.NET 9** | User requirement (no VS2026 for .NET 10); enterprise-ready |
| **Blazor Server** | Server-side ML processing; no client-side model loading |
| **ML.NET** | Native .NET ML framework; easy integration |
| **ONNX Runtime** | Cross-platform pose estimation; model agnostic |
| **MoveNet** | Fast (4.6MB), accurate single-person pose estimation |
| **MediaPipe** | Alternative with Python bridge support |
| **Cloudinary** | Cloud-native video processing; hackathon-ready |
| **MudBlazor** | Professional UI components; fast development |
| **QuestPDF** | PDF generation without external dependencies |

### Architecture Decisions

| Decision | Rationale |
|----------|-----------|
| **Monolithic (not microservices)** | Simpler for hackathon; can split later |
| **Server-side rendering** | ML needs server resources; simpler deployment |
| **Cloudinary for video** | Avoid building video infrastructure |
| **PoseService pattern** | Auto-select best available method |

---

## 3. Clinical Context

### General Movements Assessment (GMA)

**What is GMA?**
GMA evaluates spontaneous "general movements" - complex, whole-body movement patterns present from fetal life through early infancy.

**Age Windows:**
| Age | Movement Type | Clinical Significance |
|-----|---------------|----------------------|
| 0-8 weeks | Writhing movements | Less predictive for CP |
| 9-20 weeks | Fidgety movements | **Critical window for CP prediction** |
| >20 weeks | Other patterns | GMA less predictive |

**Movement Classifications:**
| Classification | Description | CP Risk |
|----------------|-------------|---------|
| **Normal** | Variable, complex, fluent movements | < 5% |
| **Poor Repertoire** | Monotonous, simple movements | ~30% |
| **Cramped-Synchronized** | Rigid, simultaneous movements | ~95% |
| **Absent Fidgety** | No fidgety movements (9-20 weeks) | >90% |

### Preterm Infant Considerations

- **Corrected Age** must be used: `Chronological Age - (40 - Gestational Age)`
- Example: 32-week GA, now 16 weeks chronological = 8 weeks corrected
- Preterm infants have higher baseline CP risk

### Clinical Workflow (Intended)

```
1. Caregiver records 30-60 second video of infant (supine, active movement)
2. Video uploaded to CP-sight
3. Cloudinary extracts frames
4. MoveNet/MediaPipe extracts poses
5. FeatureExtractor calculates movement features
6. ML.NET classifies movement pattern
7. RiskAssessmentService generates risk assessment with recommendations
8. QuestPDF generates report
9. Video deleted for privacy (configurable)
```

---

## 4. Research Foundation

### Primary Reference

**Paper:** PMC12941449 - "An early marker for neurological deficits after perinatal brain lesions"
**Authors:** Prechtl HFR et al.
**Key Finding:** Absent fidgety movements have >90% specificity for CP prediction.

### Related Research

1. Einspieler C, et al. (2021). "Prechtl's General Movements Assessment"
2. Ferrari F, et al. (2002). "Cramped synchronized general movements in preterm infants"
3. Bosanquet M, et al. (2013). "A systematic review of tests for early motor development"

### Dataset Reference

**MINI-RGBD Dataset:**
- Source: Fraunhofer IOSB
- Content: Synthetic infant pose sequences (RGB + Depth)
- Size: ~7GB
- Purpose: Training pose estimation and movement classification models
- **Note:** Synthetic data - must be validated on real clinical data

---

## 5. Project Constraints

### Hard Constraints (Cannot Change)

| Constraint | Reason |
|------------|--------|
| .NET 9 only | User doesn't have VS2026 |
| Cloudinary for video | Already configured with credentials |
| Screening tool only | Not approved as medical device |

### Soft Constraints (Can Change with Justification)

| Constraint | Current State | Could Change To |
|------------|---------------|-----------------|
| Blazor Server | Current | Blazor WebAssembly (if client-side ML) |
| SQLite/No DB | Current | PostgreSQL/SQL Server |
| No authentication | Current | JWT/API Keys |

### Timeline Constraints

- Originally: 24-hour hackathon
- Now: Production enhancement phase
- No strict deadline, but prioritize core functionality

---

## 6. Security & Compliance Context

### Current Security Posture

| Aspect | Status | Notes |
|--------|--------|-------|
| API Authentication | ❌ Not implemented | API is currently open |
| Video Encryption | ⚠️ HTTPS only | Cloudinary handles storage |
| Data Retention | ⚠️ Videos deletable | Not automated |
| Audit Logging | ❌ Not implemented | Need for clinical use |
| HIPAA Compliance | ❌ Not compliant | Not for clinical deployment yet |

### Privacy Considerations

- Videos contain identifiable information (infants)
- GDPR applies (EU data subjects)
- Must offer video deletion capability
- Consider data residency requirements

### Credential Security

```
⚠️ CREDENTIALS IN CONFIG FILES - FOR DEVELOPMENT ONLY

Cloudinary credentials are in appsettings.json:
- Cloud Name: dnvw0yyib
- API Key: 887823313431246
- API Secret: 3X6o4aSOwBLH0_cJkLEg4XtFp14

For production: Use environment variables or secrets manager
```

---

## 7. Integration Context

### Cloudinary Integration

**What Cloudinary Does:**
- Video upload and storage
- Frame extraction via URL transformations
- Video transcoding and optimization
- CDN delivery

**What Cloudinary Does NOT Do:**
- Download external datasets (MINI-RGBD must be manual)
- Perform pose estimation (use ONNX)
- Make medical decisions

**Key Transformation URLs:**
```
Frame at 5 seconds:
https://res.cloudinary.com/dnvw0yyib/video/upload/so_5,w_640,h_360,c_fill,q_auto:good,f_jpg/{public_id}.jpg

Thumbnail:
https://res.cloudinary.com/dnvw0yyib/video/upload/w_400,h_300,c_fill,q_auto,f_jpg/{public_id}.jpg
```

### MoveNet ONNX Integration

**Model Details:**
- Source: https://huggingface.co/Xenova/movenet-singlepose-lightning
- Input: 1×3×192×192 tensor (NCHW format, normalized)
- Output: 1×1×17×3 (y, x, confidence for 17 keypoints)
- Size: ~5MB ONNX file

**Integration Status:**
- ✅ `MoveNetPoseEstimator.cs` implemented
- ✅ ONNX Runtime configured
- ⏳ Model file needs to be added by user

### MediaPipe Integration

**Status:**
- ✅ `MediaPipePoseEstimator.cs` implemented
- ✅ 33→17 keypoint mapping implemented
- ⏳ Python bridge optional (disabled by default)

---

## 8. Development Conventions

### Code Style

- **Language:** C# 12 with .NET 9 features
- **Nullable:** Enabled (`<Nullable>enable</Nullable>`)
- **Implicit Usings:** Enabled
- **Naming:** PascalCase for public, _camelCase for private fields
- **Records:** Use for immutable data models
- **Async:** All I/O operations should be async

### Namespace Structure

```
CP_Sight.Core          - Domain models, no dependencies
CP_Sight.Core.Services - Feature extraction
CP_Sight.ML.Services   - Pose estimation, ML classification
CP_Sight.Web           - ASP.NET Core application
CP_Sight.Web.Services  - Application services
CP_Sight.Web.Components - Blazor components
```

### Git Conventions

```
Branch naming: feature/TASK-XXX-description
Commit messages: TASK-XXX: Brief description

Example:
  feature/TASK-001-onnx-model-integration
  TASK-001: Add MoveNet ONNX model for pose estimation
```

---

## 9. Known Issues & Technical Debt

### Current Issues

| Issue | Impact | Workaround |
|-------|--------|------------|
| Image preprocessing incomplete | May affect pose accuracy | Model file not added yet |
| No authentication | API is open | Add before production |
| No persistence | Results lost after restart | Add database |
| Single-threaded processing | Long videos may timeout | Add background jobs |

### Technical Debt

| Item | Priority | Effort |
|------|----------|--------|
| Input validation | High | 2h |
| Error handling standardization | High | 2h |
| Logging infrastructure | Medium | 2h |
| Configuration validation | Medium | 1h |
| API versioning | Low | 2h |

---

## 10. Success Criteria

### For Hackathon Demonstration

- [x] Video upload works
- [x] Analysis completes
- [x] Results display correctly
- [x] PDF report generates
- [x] UI is professional
- [x] Cloudinary integrated
- [x] MoveNet integration ready
- [x] MediaPipe integration ready

### For Production Readiness

- [ ] MoveNet ONNX model added
- [ ] Image preprocessing complete
- [ ] Clinical validation completed
- [ ] Authentication implemented
- [ ] Database persistence added
- [ ] Background processing
- [ ] Comprehensive test coverage
- [ ] Documentation complete
- [ ] Security audit passed

### For Clinical Deployment

- [ ] IRB approval obtained
- [ ] Clinical validation study completed
- [ ] Regulatory pathway determined
- [ ] HIPAA/GDPR compliance verified
- [ ] Clinician training materials created

---

## 11. Contact & Resources

### Key Resources

| Resource | Location |
|----------|----------|
| Project Archive | `CP-sight-NET9-Complete.tar.gz` |
| Requirements | `spec/requirements.md` |
| Technical Design | `spec/design.md` |
| Development Tasks | `spec/tasks.md` |
| Cloudinary Config | `dotnet/CP-sight.Web/appsettings.json` |
| Dataset Integration | `datasets/MINI-RGBD_DOWNLOAD_GUIDE.md` |

### External Links

- Cloudinary Dashboard: https://cloudinary.com/console
- MINI-RGBD Dataset: https://argmax.ai/data/mini-rgbd/
- MoveNet Model: https://huggingface.co/Xenova/movenet-singlepose-lightning
- ML.NET Documentation: https://docs.microsoft.com/en-us/dotnet/machine-learning/
- ONNX Runtime: https://onnxruntime.ai/
- Prechtl GMA Training: https://www.general-movements-trust.info/

---

## 12. For AI Assistants

### When Making Changes

1. **Read all spec files first** to understand context
2. **Check existing patterns** in similar files
3. **Maintain namespace consistency** (`CP_Sight.*`)
4. **Update relevant documentation** after code changes
5. **Consider clinical implications** of any algorithm changes

### Key Assumptions to Validate

1. **Age calculations** - Always use corrected age for preterm
2. **Risk thresholds** - Based on GMA research, don't change without validation
3. **Movement features** - Specific to infant movement patterns
4. **Clinical recommendations** - Align with current GMA guidelines

### Common Pitfalls to Avoid

1. **Don't** change clinical thresholds without justification
2. **Don't** expose API secrets in logs or responses
3. **Don't** assume all videos are the same format/quality
4. **Don't** skip input validation for clinical data
5. **Don't** make medical claims - maintain "screening tool" language

### Questions to Ask Before Major Changes

1. Does this affect clinical accuracy?
2. Is this backwards compatible?
3. Are there security implications?
4. Does this require documentation updates?
5. Should this be configurable?

---

## Document Version

- **Version:** 2.0
- **Last Updated:** 2024
- **Status:** Complete with MoveNet and MediaPipe integration
