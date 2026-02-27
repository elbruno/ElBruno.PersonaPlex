using ElBruno.PersonaPlex;
using ElBruno.PersonaPlex.Pipeline;

Console.WriteLine("=== PersonaPlex - Simple Speech-to-Speech Demo ===");
Console.WriteLine();

// Models will download automatically on first run
Console.WriteLine("Initializing PersonaPlex pipeline...");
Console.WriteLine("(Models will be downloaded automatically on first run)");
Console.WriteLine();

using var pipeline = await PersonaPlexPipeline.CreateAsync("models");

Console.WriteLine($"Pipeline ready. Models loaded from: {pipeline.ModelDirectory}");
Console.WriteLine();

// Process input audio
var inputPath = "input.wav";
var outputPath = "output.wav";

if (!File.Exists(inputPath))
{
    Console.WriteLine($"⚠️  Please provide an input audio file at: {inputPath}");
    Console.WriteLine("   The file should be a 24kHz mono WAV recording.");
    return;
}

Console.WriteLine($"Processing: {inputPath}");
Console.WriteLine($"Voice: {VoicePreset.NATF2}");
Console.WriteLine();

var result = await pipeline.ProcessAsync(
    inputAudioPath: inputPath,
    voicePreset: VoicePreset.NATF2,
    outputPath: outputPath);

Console.WriteLine($"✅ Output saved to: {result.OutputAudioPath}");
Console.WriteLine($"   Inference time: {result.InferenceTimeMs:F0}ms");
Console.WriteLine($"   Voice: {result.VoicePreset}");
