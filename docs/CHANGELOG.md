# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- .NET 11.0 targeting for both NuGet packages, while retaining .NET 8.0 and .NET 10.0 support.
- .NET 11.0 CI coverage and sample builds.
- Updated `ElBruno.HuggingFace.Downloader` to 1.6.2, which provides a native .NET 11 asset.
- Updated Microsoft.Extensions.DependencyInjection.Abstractions to 10.0.5 to match the downloader dependency requirements.
- Both package versions set to 1.1.0 for the pending stable release.

## [0.6.1] - 2026-07-05

### Fixed

- Corrected the Twitter/X URL in the README About the Author section

### Added

- Initial project scaffold
- Core library project `ElBruno.PersonaPlex`
- Unit test project `ElBruno.PersonaPlex.Tests`
- Sample projects (scenario-01-simple, scenario-02-persona, scenario-03-voice-select)
- GitHub Actions workflow for NuGet publishing (OIDC Trusted Publishing)
- Python ONNX export scripts directory
- Documentation structure
