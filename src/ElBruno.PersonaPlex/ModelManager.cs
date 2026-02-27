using ElBruno.HuggingFace;

namespace ElBruno.PersonaPlex;

/// <summary>
/// Manages downloading and caching of ONNX models from HuggingFace.
/// </summary>
public class ModelManager
{
    /// <summary>
    /// Known model files required for inference. Updated as ONNX export is finalized.
    /// </summary>
    internal static readonly string[] RequiredModelFiles =
    [
        "mimi_encoder.onnx",
        "lm_model.onnx",
        "mimi_decoder.onnx"
    ];

    private static readonly string DefaultCacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ElBruno", "PersonaPlex", "models");

    /// <summary>
    /// Gets the default model cache directory.
    /// </summary>
    public static string DefaultModelDir => DefaultCacheDir;

    /// <summary>
    /// Ensures all required model files are downloaded to the specified directory.
    /// </summary>
    /// <param name="modelDir">Target directory for model files. Uses default cache if null.</param>
    /// <param name="repoId">HuggingFace repository ID.</param>
    /// <param name="progress">Optional progress callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to the model directory.</returns>
    public static async Task<string> EnsureModelsAsync(
        string? modelDir = null,
        string repoId = "elbruno/personaplex-7b-v1-ONNX",
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var targetDir = modelDir ?? DefaultCacheDir;
        Directory.CreateDirectory(targetDir);

        // Check if models already exist
        if (AreModelsPresent(targetDir))
        {
            return targetDir;
        }

        // Download from HuggingFace
        using var downloader = new HuggingFaceDownloader();
        await downloader.DownloadFilesAsync(new DownloadRequest
        {
            RepoId = repoId,
            LocalDirectory = targetDir,
            RequiredFiles = RequiredModelFiles,
        }, cancellationToken);

        return targetDir;
    }

    /// <summary>
    /// Checks whether the required model files are present in the specified directory.
    /// </summary>
    public static bool AreModelsPresent(string modelDir)
    {
        if (!Directory.Exists(modelDir))
            return false;

        // Check for at least one ONNX file
        var onnxFiles = Directory.GetFiles(modelDir, "*.onnx", SearchOption.AllDirectories);
        return onnxFiles.Length > 0;
    }
}
