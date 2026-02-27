using ElBruno.PersonaPlex;
using ElBruno.PersonaPlex.Pipeline;

Console.WriteLine("=== PersonaPlex - Voice Selection Demo ===");
Console.WriteLine();

using var pipeline = await PersonaPlexPipeline.CreateAsync("models");

var inputPath = "input.wav";

if (!File.Exists(inputPath))
{
    Console.WriteLine($"⚠️  Please provide an input audio file at: {inputPath}");
    return;
}

// Demonstrate all voice categories
var voices = new[]
{
    (VoicePreset.NATF0, "Natural Female 0"),
    (VoicePreset.NATF2, "Natural Female 2"),
    (VoicePreset.NATM0, "Natural Male 0"),
    (VoicePreset.NATM2, "Natural Male 2"),
    (VoicePreset.VARF0, "Variety Female 0"),
    (VoicePreset.VARM0, "Variety Male 0"),
};

Console.WriteLine($"Generating speech with {voices.Length} different voices...");
Console.WriteLine();

foreach (var (preset, description) in voices)
{
    Console.Write($"  🎤 {description} ({preset})... ");

    var result = await pipeline.ProcessAsync(
        inputAudioPath: inputPath,
        voicePreset: preset,
        outputPath: $"output_{preset}.wav");

    Console.WriteLine($"✅ {result.InferenceTimeMs:F0}ms");
}

Console.WriteLine();
Console.WriteLine("Done! Listen to each output file to compare voices.");
Console.WriteLine();
Console.WriteLine("Available voice presets:");
Console.WriteLine("  Natural Female: NATF0, NATF1, NATF2, NATF3");
Console.WriteLine("  Natural Male:   NATM0, NATM1, NATM2, NATM3");
Console.WriteLine("  Variety Female: VARF0, VARF1, VARF2, VARF3, VARF4");
Console.WriteLine("  Variety Male:   VARM0, VARM1, VARM2, VARM3, VARM4");
