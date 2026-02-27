namespace ElBruno.PersonaPlex;

/// <summary>
/// Reports download progress for model files.
/// </summary>
public record DownloadProgress
{
    /// <summary>Index of the current file being downloaded (1-based).</summary>
    public int CurrentFile { get; init; }

    /// <summary>Total number of files to download.</summary>
    public int TotalFiles { get; init; }

    /// <summary>Name of the file being downloaded.</summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>Bytes downloaded so far for the current file.</summary>
    public long BytesDownloaded { get; init; }

    /// <summary>Total size of the current file in bytes (-1 if unknown).</summary>
    public long TotalBytes { get; init; }

    /// <summary>Download percentage for the current file (0-100).</summary>
    public double Percentage => TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes * 100.0 : 0;

    /// <summary>Overall progress across all files (0-100).</summary>
    public double OverallPercentage => TotalFiles > 0
        ? ((CurrentFile - 1) + (TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes : 0)) / TotalFiles * 100.0
        : 0;
}
