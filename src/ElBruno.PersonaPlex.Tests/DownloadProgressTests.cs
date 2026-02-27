using ElBruno.PersonaPlex;

namespace ElBruno.PersonaPlex.Tests;

public class DownloadProgressTests
{
    [Fact]
    public void Percentage_IsCalculatedCorrectly()
    {
        var progress = new DownloadProgress
        {
            CurrentFile = 1,
            TotalFiles = 3,
            FileName = "model.onnx",
            BytesDownloaded = 500,
            TotalBytes = 1000
        };

        Assert.Equal(50.0, progress.Percentage);
    }

    [Fact]
    public void Percentage_IsZero_WhenTotalBytesIsZero()
    {
        var progress = new DownloadProgress
        {
            CurrentFile = 1,
            TotalFiles = 1,
            BytesDownloaded = 100,
            TotalBytes = 0
        };

        Assert.Equal(0, progress.Percentage);
    }

    [Fact]
    public void OverallPercentage_AccountsForMultipleFiles()
    {
        var progress = new DownloadProgress
        {
            CurrentFile = 2,
            TotalFiles = 4,
            BytesDownloaded = 500,
            TotalBytes = 1000
        };

        // (2-1 + 0.5) / 4 * 100 = 37.5%
        Assert.Equal(37.5, progress.OverallPercentage);
    }
}
