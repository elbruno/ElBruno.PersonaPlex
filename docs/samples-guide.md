# Samples Guide

This guide explains each sample scenario, what it demonstrates, how to run it, and what test content to use.

## Prerequisites

- **.NET 10 SDK** (or .NET 8 SDK for the core library)
- A **WAV audio file** for scenarios 01–03 (24kHz mono recommended; other formats are auto-converted)

### Getting a Test Audio File

Record a short clip (5–30 seconds) of spoken English and save it as a WAV file. You can use:

- **Windows**: Voice Recorder app → export as WAV
- **macOS**: QuickTime Player → File → New Audio Recording → save, then convert to WAV with `ffmpeg -i recording.m4a -ar 24000 -ac 1 test.wav`
- **ffmpeg** (any platform): `ffmpeg -f lavfi -i "sine=frequency=440:duration=5" -ar 24000 -ac 1 test_tone.wav`

Place the WAV file in the sample folder or pass the path as an argument.

---

## Scenario 01 — Simple Speech-to-Speech

**Goal:** Minimal example showing the complete encode → decode round-trip through the Mimi audio codec.

**What it demonstrates:**
- Auto-downloading ONNX models from HuggingFace on first run
- Loading the `PersonaPlexPipeline`
- Processing an audio file (encode → decode)
- Output file size and duration reporting

**How to run:**

```bash
cd src/samples/scenario-01-simple

# Using default file name (sample_voice_orig_eng.wav in current directory)
dotnet run

# Providing a specific input file
dotnet run -- myrecording.wav

# Providing both input and output paths
dotnet run -- input.wav result.wav
```

**Test content:** Any WAV file with spoken English (5–30 seconds works well). The output WAV should be approximately the same duration as the input.

**Expected output:**

```
=== PersonaPlex - Simple Speech-to-Speech Demo ===

Initializing PersonaPlex pipeline...
Pipeline ready. Models loaded from: models

Processing: sample_voice_orig_eng.wav
Voice: NATF2

✅ Output saved to: output.wav
   Output size: 1250.0 KB (~26.0s)
   Inference time: 4200ms
   Voice: NATF2
```

---

## Scenario 02 — Custom Persona Prompts

**Goal:** Show how different text personas affect the model's processing, generating separate outputs for each persona.

**What it demonstrates:**
- Defining multiple persona prompts (teacher, customer service agent, casual conversationalist)
- Running the same input audio through different personas
- Producing multiple output files, one per persona

**How to run:**

```bash
cd src/samples/scenario-02-persona

# Using default file name
dotnet run

# Providing a specific input file
dotnet run -- myrecording.wav
```

**Test content:** A WAV file with a spoken question or greeting works best (e.g., *"Hello, can you help me?"* or *"What should I cook for dinner?"*). The scenario produces three output files: `output_assistant.wav`, `output_customer_service.wav`, and `output_casual.wav`.

**Personas used:**

| Persona | Prompt |
|---------|--------|
| `assistant` | *"You are a wise and friendly teacher..."* |
| `customer_service` | *"You work for AeroRentals Pro, a drone rental company..."* |
| `casual` | *"You enjoy having a good conversation..."* |

**Expected output:**

```
=== PersonaPlex - Persona Prompts Demo ===

--- Persona: assistant ---
Prompt: You are a wise and friendly teacher. Answer questions or provide advice i...
✅ Output: output_assistant.wav (4100ms)

--- Persona: customer_service ---
Prompt: You work for AeroRentals Pro which is a drone rental company and your nam...
✅ Output: output_customer_service.wav (4050ms)

--- Persona: casual ---
Prompt: You enjoy having a good conversation. Have a casual discussion about favo...
✅ Output: output_casual.wav (4080ms)

Done! Compare the outputs to hear how persona affects the response.
```

---

## Scenario 03 — Voice Selection

**Goal:** Demonstrate the variety of available voice presets, generating an output for each voice.

**What it demonstrates:**
- Iterating through multiple voice presets (Natural Female/Male, Variety Female/Male)
- Comparing voice characteristics across presets
- Listing all 18 available voice presets

**How to run:**

```bash
cd src/samples/scenario-03-voice-select

# Requires an input.wav file in the current directory
dotnet run
```

**Test content:** A WAV file named `input.wav` in the scenario folder. A short (5–10 second) spoken sentence is ideal since the sample generates 6 output files (`output_NATF0.wav`, `output_NATF2.wav`, etc.).

**Voices tested:**

| Preset | Description |
|--------|-------------|
| NATF0 | Natural Female 0 |
| NATF2 | Natural Female 2 |
| NATM0 | Natural Male 0 |
| NATM2 | Natural Male 2 |
| VARF0 | Variety Female 0 |
| VARM0 | Variety Male 0 |

**All available presets:**

| Category | Voices |
|----------|--------|
| Natural Female | NATF0, NATF1, NATF2, NATF3 |
| Natural Male | NATM0, NATM1, NATM2, NATM3 |
| Variety Female | VARF0, VARF1, VARF2, VARF3, VARF4 |
| Variety Male | VARM0, VARM1, VARM2, VARM3, VARM4 |

---

## Scenario 04 — Model Download & Custom Directory

**Goal:** Demonstrate the model download process, progress reporting, default vs. custom model directories, and how to verify cached models.

**What it demonstrates:**
- Displaying the default model cache location (`%LOCALAPPDATA%\ElBruno\PersonaPlex\models`)
- Download progress bar with file names and sizes
- Downloading to a custom directory (`my-custom-models/`)
- Listing all downloaded model files and sizes
- Creating a pipeline from a custom model directory
- Configuring via `PersonaPlexOptions`

**How to run:**

```bash
cd src/samples/scenario-04-model-download

# No input file needed — this scenario focuses on model management
dotnet run
```

**Test content:** No audio file required. This scenario downloads the ONNX models (~350 MB total) and demonstrates model management.

**Expected output:**

```
=== PersonaPlex - Model Download & Custom Directory Demo ===

📂 Default model cache location:
   C:\Users\you\AppData\Local\ElBruno\PersonaPlex\models

   Models already cached: ❌ No

📥 Downloading models to default location...

   [██████████████████████████████] 100.0% - mimi_decoder.onnx (169.8 MB)
   ✅ Models ready at: C:\Users\you\AppData\Local\ElBruno\PersonaPlex\models
   ⏱️  Time: 45.2s

   📁 Files:
      mimi_encoder.onnx                177.8 MB
      mimi_decoder.onnx                169.8 MB
      Total:                           347.6 MB

📂 Using a custom model directory...
   Custom path: C:\...\scenario-04-model-download\my-custom-models

📥 Downloading models to custom location...
   ...

🚀 Creating pipeline from custom model directory...
   Pipeline ready!
   Encoder loaded: True
   Decoder loaded: True

✅ Done! Models are cached and reused across runs.
```

---

## Summary

| Scenario | Audio Needed? | Docker Needed? | Goal |
|----------|:---:|:---:|------|
| **01 — Simple** | ✅ WAV file | ❌ | Basic encode → decode round-trip |
| **02 — Persona** | ✅ WAV file | ❌ | Multiple persona prompts comparison |
| **03 — Voice Select** | ✅ WAV file (`input.wav`) | ❌ | Voice preset comparison |
| **04 — Model Download** | ❌ | ❌ | Model management and custom directories |
