using System.IO;
using UnityEngine;

namespace VHAssets
{
/// <summary>
/// Writes Unity <see cref="AudioClip"/> data to 16-bit PCM WAV files.
/// </summary>
public static class WavUtility
{
    const int BitsPerSample = 16;
    const int BytesPerSample = BitsPerSample / 8;
    const int HeaderSize = 44;
    const float Pcm16Scale = 32767f;

    /// <summary>
    /// Saves an <see cref="AudioClip"/> to disk as a PCM WAV file.
    /// </summary>
    /// <remarks>
    /// This method writes a standard RIFF/WAVE file containing 16-bit little-endian PCM sample data.
    /// It is intentionally narrow in scope because the current runtime use case only needs uncompressed
    /// PCM export as an intermediate step before invoking the external FLAC encoder used by Google ASR.
    ///
    /// The output format written here is:
    /// - container: RIFF/WAVE
    /// - format tag: PCM (1)
    /// - sample encoding: signed 16-bit integer
    /// - byte order: little-endian
    /// - channel count and sample rate: copied directly from the source <see cref="AudioClip"/>
    ///
    /// Unity audio samples are exposed as normalized floating-point values in the range [-1, 1]. This
    /// method rescales each sample to 16-bit PCM before writing it to disk.
    ///
    /// Reference material:
    /// - https://learn.microsoft.com/en-us/windows/win32/api/mmreg/ns-mmreg-waveformatex
    /// - https://en.wikipedia.org/wiki/WAV
    /// </remarks>
    /// <param name="filename">
    /// Output path relative to <see cref="Application.persistentDataPath"/>, or an explicit filename with
    /// extension. If no extension is supplied, <c>.wav</c> is appended automatically.
    /// </param>
    /// <param name="clip">The source clip whose interleaved sample data will be written.</param>
    /// <returns>
    /// <see langword="true"/> if the file path and clip were valid and the WAV file was written;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool Save(string filename, AudioClip clip)
    {
        if (string.IsNullOrWhiteSpace(filename) || clip == null)
            return false;

        string trimmedFilename = filename.Trim();
        if (string.IsNullOrEmpty(Path.GetExtension(trimmedFilename)))
            trimmedFilename += ".wav";

        string filepath = Path.Combine(Application.persistentDataPath, trimmedFilename);
        string directoryPath = Path.GetDirectoryName(filepath);
        if (!string.IsNullOrEmpty(directoryPath))
            Directory.CreateDirectory(directoryPath);

        int sampleCount = clip.samples * clip.channels;
        float[] samples = new float[sampleCount];
        clip.GetData(samples, 0);

        using FileStream stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None);
        using BinaryWriter writer = new BinaryWriter(stream);

        WriteHeader(writer, clip.channels, clip.frequency, sampleCount);

        for (int i = 0; i < samples.Length; i++)
        {
            short pcmSample = (short)(samples[i] * Pcm16Scale);
            writer.Write(pcmSample);
        }

        return true;
    }

    /// <summary>
    /// Writes the canonical PCM WAV header for the sample data that will follow in the stream.
    /// </summary>
    /// <remarks>
    /// This writes the fixed 44-byte header used by simple RIFF/WAVE PCM files:
    /// <c>RIFF</c>, file size, <c>WAVE</c>, <c>fmt </c> chunk, and <c>data</c> chunk header.
    ///
    /// The header values are derived from the caller-provided audio format:
    /// - <c>channels</c> becomes the WAV channel count
    /// - <c>sampleRate</c> becomes the sample frequency
    /// - <c>sampleCount</c> is treated as the total interleaved sample count across all channels
    ///
    /// Because this utility writes 16-bit PCM only, the following values are fixed:
    /// - format tag = 1 (PCM)
    /// - bits per sample = 16
    /// - bytes per sample = 2
    ///
    /// The byte rate and block alignment are computed using the standard PCM formulas documented by
    /// Microsoft's <c>WAVEFORMATEX</c> structure documentation.
    ///
    /// Reference material:
    /// - https://learn.microsoft.com/en-us/windows/win32/api/mmreg/ns-mmreg-waveformatex
    /// - https://en.wikipedia.org/wiki/WAV
    /// </remarks>
    /// <param name="writer">The binary writer positioned at the start of the output stream.</param>
    /// <param name="channels">Number of audio channels in the interleaved sample data.</param>
    /// <param name="sampleRate">Sample frequency, in hertz.</param>
    /// <param name="sampleCount">Total interleaved sample count that will be written after the header.</param>
    static void WriteHeader(BinaryWriter writer, int channels, int sampleRate, int sampleCount)
    {
        int dataSize = sampleCount * BytesPerSample;
        int byteRate = sampleRate * channels * BytesPerSample;
        short blockAlign = (short)(channels * BytesPerSample);

        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(HeaderSize - 8 + dataSize);
        writer.Write(new[] { 'W', 'A', 'V', 'E' });
        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write((short)BitsPerSample);
        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(dataSize);
    }
}
}
