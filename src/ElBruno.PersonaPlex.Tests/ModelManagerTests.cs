using ElBruno.PersonaPlex;

namespace ElBruno.PersonaPlex.Tests;

public class ModelManagerTests
{
    [Fact]
    public void DefaultModelDir_IsNotEmpty()
    {
        Assert.NotEmpty(ModelManager.DefaultModelDir);
    }

    [Fact]
    public void DefaultModelDir_ContainsPersonaPlex()
    {
        Assert.Contains("PersonaPlex", ModelManager.DefaultModelDir);
    }

    [Fact]
    public void AreModelsPresent_ReturnsFalse_WhenDirectoryDoesNotExist()
    {
        Assert.False(ModelManager.AreModelsPresent("/nonexistent/path/should/not/exist"));
    }

    [Fact]
    public void AreModelsPresent_ReturnsFalse_WhenDirectoryIsEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"personaplex_test_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);
            Assert.False(ModelManager.AreModelsPresent(tempDir));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
