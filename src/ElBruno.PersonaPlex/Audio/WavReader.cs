namespace ElBruno.PersonaPlex.Audio;

/// <summary>
/// Reads WAV files into float arrays for ONNX model input.
/// Expects 24kHz mono PCM audio (resamples if different).
/// </summary>
public static class WavReader
{
    /// <summary>
    /// Reads a WAV file and returns mono float samples normalized to [-1, 1].
    /// </summary>
    /// <param name="path">Path to the WAV file.</param>
    /// <returns>Float array of audio samples at 24kHz mono.</returns>
    public static float[] ReadMono24kHz(string path)
    {
        using var stream = File.OpenRead(path);
        return ReadMono24kHz(stream);
    }

    /// <summary>
    /// Reads a WAV stream and returns mono float samples normalized to [-1, 1].
    /// </summary>
    public static float[] ReadMono24kHz(Stream stream)
    {
        using var reader = new BinaryReader(stream);

        // RIFF header
        var riff = reader.ReadBytes(4);
        if (riff[0] != 'R' || riff[1] != 'I' || riff[2] != 'F' || riff[3] != 'F')
            throw new InvalidDataException("Not a valid WAV file (missing RIFF header).");

        reader.ReadInt32(); // file size
        var wave = reader.ReadBytes(4);
        if (wave[0] != 'W' || wave[1] != 'A' || wave[2] != 'V' || wave[3] != 'E')
            throw new InvalidDataException("Not a valid WAV file (missing WAVE marker).");

        // Find fmt and data chunks
        int sampleRate = 0;
        short channels = 0;
        short bitsPerSample = 0;
        float[]? samples = null;

        while (stream.Position < stream.Length)
        {
            var chunkId = new string(reader.ReadChars(4));
            var chunkSize = reader.ReadInt32();

            if (chunkId == "fmt ")
            {
                var format = reader.ReadInt16(); // 1 = PCM
                channels = reader.ReadInt16();
                sampleRate = reader.ReadInt32();
                reader.ReadInt32(); // byte rate
                reader.ReadInt16(); // block align
                bitsPerSample = reader.ReadInt16();

                if (format != 1)
                    throw new NotSupportedException($"Only PCM WAV files are supported (got format {format}).");

                // Skip any extra fmt bytes
                if (chunkSize > 16)
                    reader.ReadBytes(chunkSize - 16);
            }
            else if (chunkId == "data")
            {
                if (bitsPerSample == 0)
                    throw new InvalidDataException("WAV file has 'data' chunk before 'fmt ' chunk or missing format info.");

                var bytesPerSample = bitsPerSample / 8;
                if (bytesPerSample == 0)
                    throw new InvalidDataException("WAV file has invalid bits per sample.");

                var totalSamples = chunkSize / bytesPerSample;
                var rawSamples = new float[totalSamples];

                for (int i = 0; i < totalSamples; i++)
                {
                    rawSamples[i] = bitsPerSample switch
                    {
                        16 => reader.ReadInt16() / (float)short.MaxValue,
                        32 => reader.ReadSingle(), // float32
                        _ => throw new NotSupportedException($"Unsupported bits per sample: {bitsPerSample}")
                    };
                }

                // Convert to mono if stereo
                if (channels == 2)
                {
                    var mono = new float[totalSamples / 2];
                    for (int i = 0; i < mono.Length; i++)
                        mono[i] = (rawSamples[i * 2] + rawSamples[i * 2 + 1]) / 2f;
                    samples = mono;
                }
                else
                {
                    samples = rawSamples;
                }

                break;
            }
            else
            {
                // Skip unknown chunks
                reader.ReadBytes(chunkSize);
            }
        }

        if (samples is null)
            throw new InvalidDataException("WAV file contains no audio data.");

        // Resample to 24kHz if needed
        if (sampleRate != 24000 && sampleRate > 0)
        {
            samples = Resample(samples, sampleRate, 24000);
        }

        return samples;
    }

    /// <summary>
    /// Simple linear interpolation resampling.
    /// </summary>
    internal static float[] Resample(float[] input, int srcRate, int dstRate)
    {
        if (srcRate == dstRate) return input;

        var ratio = (double)srcRate / dstRate;
        var outputLength = (int)(input.Length / ratio);
        var output = new float[outputLength];

        for (int i = 0; i < outputLength; i++)
        {
            var srcPos = i * ratio;
            var idx = (int)srcPos;
            var frac = (float)(srcPos - idx);

            if (idx + 1 < input.Length)
                output[i] = input[idx] * (1 - frac) + input[idx + 1] * frac;
            else
                output[i] = input[Math.Min(idx, input.Length - 1)];
        }

        return output;
    }
}
