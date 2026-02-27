using ElBruno.PersonaPlex;
using ElBruno.PersonaPlex.Pipeline;

Console.WriteLine("=== PersonaPlex - Persona Prompts Demo ===");
Console.WriteLine();

using var pipeline = await PersonaPlexPipeline.CreateAsync("models");

var inputPath = args.Length > 0 ? args[0] : "sample_voice_orig_eng.wav";
var personas = new Dictionary<string, string>
{
    ["assistant"] = "You are a wise and friendly teacher. Answer questions or provide advice in a clear and engaging way.",
    ["customer_service"] = "You work for AeroRentals Pro which is a drone rental company and your name is Alex. Information: AeroRentals Pro has the PhoenixDrone X ($65/4 hours) and the premium SpectraDrone 9 ($95/4 hours).",
    ["casual"] = "You enjoy having a good conversation. Have a casual discussion about favorite foods and cooking experiences."
};

if (!File.Exists(inputPath))
{
    Console.WriteLine($"⚠️  Input file not found: {inputPath}");
    Console.WriteLine();
    Console.WriteLine("Usage: dotnet run [input.wav]");
    Console.WriteLine("   The file should be a 24kHz mono WAV recording.");
    return;
}

foreach (var (name, prompt) in personas)
{
    Console.WriteLine($"--- Persona: {name} ---");
    Console.WriteLine($"Prompt: {prompt[..Math.Min(80, prompt.Length)]}...");

    var result = await pipeline.ProcessAsync(
        inputAudioPath: inputPath,
        voicePreset: VoicePreset.NATM1,
        textPrompt: prompt,
        outputPath: $"output_{name}.wav");

    Console.WriteLine($"✅ Output: {result.OutputAudioPath} ({result.InferenceTimeMs:F0}ms)");
    Console.WriteLine();
}

Console.WriteLine("Done! Compare the outputs to hear how persona affects the response.");
