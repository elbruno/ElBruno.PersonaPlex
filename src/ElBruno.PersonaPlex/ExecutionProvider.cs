namespace ElBruno.PersonaPlex;

/// <summary>
/// Supported ONNX Runtime execution providers.
/// </summary>
public enum ExecutionProvider
{
    /// <summary>CPU execution (default, works everywhere).</summary>
    CPU,

    /// <summary>NVIDIA CUDA GPU acceleration.</summary>
    CUDA,

    /// <summary>DirectML GPU acceleration (Windows, any GPU vendor).</summary>
    DirectML
}
