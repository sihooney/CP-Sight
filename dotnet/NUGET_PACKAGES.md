# CP-sight NuGet Packages

This document lists all NuGet packages required for the CP-sight .NET 9 project.

## CP-sight.Core
No external packages required (only .NET 9 SDK).

## CP-sight.ML
| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.ML | 4.0.0 | Machine learning framework |
| Microsoft.ML.OnnxRuntime | 1.18.0 | ONNX model inference |

## CP-sight.Web
| Package | Version | Purpose |
|---------|---------|---------|
| MudBlazor | 7.8.0 | UI component library |
| Microsoft.ML | 4.0.0 | ML integration |
| Microsoft.ML.OnnxRuntime | 1.18.0 | Pose estimation |
| CloudinaryDotNet | 1.26.2 | Video upload/processing |
| QuestPDF | 2024.12.0 | PDF report generation |

## CP-sight.Tests
| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.NET.Test.Sdk | 17.12.0 | Test framework SDK |
| xunit | 2.9.2 | Testing framework |
| xunit.runner.visualstudio | 2.8.2 | VS test runner |
| coverlet.collector | 6.0.2 | Code coverage |
| FluentAssertions | 7.0.0 | Assertion library |
| Moq | 4.20.72 | Mocking framework |

## Restore Commands

```bash
# Restore all packages
cd dotnet
dotnet restore

# Or restore specific project
dotnet restore CP-sight.Web/CP-sight.Web.csproj
```

## Offline Package Installation

If you need offline packages:

```bash
# Download packages to local folder
nuget install Microsoft.ML -Version 4.0.0 -OutputDirectory packages

# Configure local package source
dotnet nuget add source ./packages -n local-packages
```
