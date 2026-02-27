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

// Use file name from args, or default to sample_voice_orig_eng.wav
var inputPath = args.Length > 0 ? args[0] : "sample_voice_orig_eng.wav";
var outputPath = args.Length > 1 ? args[1] : "output.wav";

if (!File.Exists(inputPath))
{
    Console.WriteLine($"⚠️  Input file not found: {inputPath}");
    Console.WriteLine();
    Console.WriteLine("Usage: dotnet run [input.wav] [output.wav]");
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
