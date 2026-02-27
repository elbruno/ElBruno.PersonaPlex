using ElBruno.PersonaPlex;
using ElBruno.PersonaPlex.Pipeline;

Console.WriteLine("=== PersonaPlex - Model Download & Custom Directory Demo ===");
Console.WriteLine();

// ──────────────────────────────────────────────────────────────
// 1. Show the default model cache location
// ──────────────────────────────────────────────────────────────
Console.WriteLine("📂 Default model cache location:");
Console.WriteLine($"   {ModelManager.DefaultModelDir}");
Console.WriteLine();

// Check if models are already cached
var cached = ModelManager.AreModelsPresent(ModelManager.DefaultModelDir);
Console.WriteLine($"   Models already cached: {(cached ? "✅ Yes" : "❌ No")}");
Console.WriteLine();

// ──────────────────────────────────────────────────────────────
// 2. Download models to default location with progress reporting
// ──────────────────────────────────────────────────────────────
Console.WriteLine("📥 Downloading models to default location...");
Console.WriteLine();

var downloadStart = DateTime.Now;
var defaultDir = await ModelManager.EnsureModelsAsync(
    progress: new Progress<DownloadProgress>(p =>
    {
        var barLength = 30;
        var filled = (int)(p.Percentage / 100.0 * barLength);
        var bar = new string('█', filled) + new string('░', barLength - filled);
        Console.Write($"\r   [{bar}] {p.Percentage,5:F1}% - {p.FileName} ({p.BytesDownloaded / (1024.0 * 1024.0):F1} MB)   ");
    }));

var elapsed = DateTime.Now - downloadStart;
Console.WriteLine();
Console.WriteLine($"   ✅ Models ready at: {defaultDir}");
Console.WriteLine($"   ⏱️  Time: {elapsed.TotalSeconds:F1}s");
Console.WriteLine();

// Show downloaded files
ShowModelFiles(defaultDir);

// ──────────────────────────────────────────────────────────────
// 3. Use a custom model directory
// ──────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("📂 Using a custom model directory...");

var customDir = Path.Combine(Directory.GetCurrentDirectory(), "my-custom-models");
Console.WriteLine($"   Custom path: {Path.GetFullPath(customDir)}");
Console.WriteLine();

Console.WriteLine("📥 Downloading models to custom location...");

downloadStart = DateTime.Now;
var customPath = await ModelManager.EnsureModelsAsync(
    modelDir: customDir,
    progress: new Progress<DownloadProgress>(p =>
    {
        var barLength = 30;
        var filled = (int)(p.Percentage / 100.0 * barLength);
        var bar = new string('█', filled) + new string('░', barLength - filled);
        Console.Write($"\r   [{bar}] {p.Percentage,5:F1}% - {p.FileName} ({p.BytesDownloaded / (1024.0 * 1024.0):F1} MB)   ");
    }));

elapsed = DateTime.Now - downloadStart;
Console.WriteLine();
Console.WriteLine($"   ✅ Models ready at: {customPath}");
Console.WriteLine($"   ⏱️  Time: {elapsed.TotalSeconds:F1}s");
Console.WriteLine();

ShowModelFiles(customPath);

// ──────────────────────────────────────────────────────────────
// 4. Create a pipeline using the custom model directory
// ──────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("🚀 Creating pipeline from custom model directory...");

using var pipeline = await PersonaPlexPipeline.CreateAsync(modelDir: customDir);

Console.WriteLine($"   Pipeline ready!");
Console.WriteLine($"   Encoder loaded: {pipeline.IsEncoderLoaded}");
Console.WriteLine($"   Decoder loaded: {pipeline.IsDecoderLoaded}");
Console.WriteLine($"   Model directory: {pipeline.ModelDirectory}");
Console.WriteLine();

// ──────────────────────────────────────────────────────────────
// 5. You can also configure it via PersonaPlexOptions
// ──────────────────────────────────────────────────────────────
Console.WriteLine("⚙️  Alternative: configure via PersonaPlexOptions");
Console.WriteLine();
Console.WriteLine("   var options = new PersonaPlexOptions");
Console.WriteLine("   {");
Console.WriteLine($"       ModelDirectory = \"{customDir}\",");
Console.WriteLine("       ExecutionProvider = ExecutionProvider.CPU,");
Console.WriteLine("       VoicePreset = VoicePreset.NATF2");
Console.WriteLine("   };");
Console.WriteLine("   using var p = await PersonaPlexPipeline.CreateAsync(options: options);");
Console.WriteLine();

Console.WriteLine("✅ Done! Models are cached and reused across runs.");

// ──────────────────────────────────────────────────────────────

static void ShowModelFiles(string dir)
{
    if (!Directory.Exists(dir)) return;

    var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
    Console.WriteLine($"   📁 Files in {dir}:");
    long totalSize = 0;
    foreach (var file in files)
    {
        var info = new FileInfo(file);
        totalSize += info.Length;
        Console.WriteLine($"      {info.Name,-30} {info.Length / (1024.0 * 1024.0),8:F1} MB");
    }
    Console.WriteLine($"      {"Total:",-30} {totalSize / (1024.0 * 1024.0),8:F1} MB");
}
