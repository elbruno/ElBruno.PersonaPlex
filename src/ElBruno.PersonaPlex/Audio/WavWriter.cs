namespace ElBruno.PersonaPlex.Audio;

/// <summary>
/// Writes PCM audio data to WAV files.
/// </summary>
public static class WavWriter
{
    /// <summary>
    /// Writes a float array of audio samples to a WAV file.
    /// </summary>
    /// <param name="path">Output file path.</param>
    /// <param name="samples">Audio samples (mono, normalized -1.0 to 1.0).</param>
    /// <param name="sampleRate">Sample rate in Hz (default: 24000).</param>
    public static void Write(string path, float[] samples, int sampleRate = 24000)
    {
        using var stream = File.Create(path);
        Write(stream, samples, sampleRate);
    }

    /// <summary>Alias for <see cref="Write(string, float[], int)"/>.</summary>
    public static void WriteWav(string path, float[] samples, int sampleRate = 24000)
        => Write(path, samples, sampleRate);

    /// <summary>
    /// Writes a float array of audio samples to a WAV stream.
    /// </summary>
    public static void Write(Stream stream, float[] samples, int sampleRate = 24000)
    {
        using var writer = new BinaryWriter(stream);
        int bitsPerSample = 16;
        int channels = 1;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        int blockAlign = channels * bitsPerSample / 8;
        int dataSize = samples.Length * blockAlign;

        // RIFF header
        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8);

        // fmt chunk
        writer.Write("fmt "u8);
        writer.Write(16); // chunk size
        writer.Write((short)1); // PCM format
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);

        // data chunk
        writer.Write("data"u8);
        writer.Write(dataSize);

        foreach (var sample in samples)
        {
            var clamped = Math.Clamp(sample, -1.0f, 1.0f);
            var pcm = (short)(clamped * short.MaxValue);
            writer.Write(pcm);
        }
    }
}
