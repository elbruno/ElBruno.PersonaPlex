using ElBruno.PersonaPlex.Audio;

namespace ElBruno.PersonaPlex.Tests;

public class WavWriterTests
{
    [Fact]
    public void Write_CreatesValidWavFile()
    {
        var tempPath = Path.GetTempFileName();
        try
        {
            var samples = new float[] { 0.0f, 0.5f, -0.5f, 1.0f, -1.0f };
            WavWriter.Write(tempPath, samples, 24000);

            Assert.True(File.Exists(tempPath));
            var bytes = File.ReadAllBytes(tempPath);

            // RIFF header
            Assert.Equal((byte)'R', bytes[0]);
            Assert.Equal((byte)'I', bytes[1]);
            Assert.Equal((byte)'F', bytes[2]);
            Assert.Equal((byte)'F', bytes[3]);

            // WAVE format
            Assert.Equal((byte)'W', bytes[8]);
            Assert.Equal((byte)'A', bytes[9]);
            Assert.Equal((byte)'V', bytes[10]);
            Assert.Equal((byte)'E', bytes[11]);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void Write_EmptySamples_CreatesHeaderOnly()
    {
        var tempPath = Path.GetTempFileName();
        try
        {
            WavWriter.Write(tempPath, [], 24000);
            Assert.True(File.Exists(tempPath));

            // 44 bytes = RIFF header (12) + fmt chunk (24) + data header (8)
            var bytes = File.ReadAllBytes(tempPath);
            Assert.Equal(44, bytes.Length);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}
