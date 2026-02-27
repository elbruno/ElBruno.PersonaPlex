using Microsoft.ML.OnnxRuntime;

namespace ElBruno.PersonaPlex;

/// <summary>
/// Helper for creating ONNX Runtime session options with different execution providers.
/// </summary>
public static class SessionOptionsHelper
{
    /// <summary>
    /// Creates session options for CPU execution.
    /// </summary>
    public static SessionOptions CreateCpuOptions()
    {
        var options = new SessionOptions();
        options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        return options;
    }

    /// <summary>
    /// Creates session options for CUDA execution.
    /// Requires the Microsoft.ML.OnnxRuntime.Gpu NuGet package.
    /// </summary>
    public static SessionOptions CreateCudaOptions()
    {
        var options = new SessionOptions();
        options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        options.AppendExecutionProvider_CUDA(0);
        return options;
    }

    /// <summary>
    /// Creates session options for DirectML execution.
    /// Requires the Microsoft.ML.OnnxRuntime.DirectML NuGet package.
    /// </summary>
    public static SessionOptions CreateDirectMlOptions()
    {
        var options = new SessionOptions();
        options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        options.AppendExecutionProvider_DML(0);
        return options;
    }

    /// <summary>
    /// Creates session options for the specified execution provider.
    /// </summary>
    public static SessionOptions CreateOptions(ExecutionProvider provider) => provider switch
    {
        ExecutionProvider.CUDA => CreateCudaOptions(),
        ExecutionProvider.DirectML => CreateDirectMlOptions(),
        _ => CreateCpuOptions()
    };
}
