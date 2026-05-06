using System;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework.Audio;

namespace MiniGolf.Systems;

/// <summary>
/// Loads a 16-bit PCM <c>.wav</c> file from disk and produces a
/// <see cref="SoundEffect"/> with the first <paramref name="offsetSeconds"/>
/// of audio data skipped. Used to bypass the MGCB content pipeline for sound
/// files that contain leading silence or a multi-shot recording, so the
/// audible part starts immediately when the effect is played.
/// </summary>
public static class WavLoader
{
    /// <summary>
    /// Parse <paramref name="filePath"/> as a 16-bit PCM WAV, drop the first
    /// <paramref name="offsetSeconds"/> of sample data, and build a SoundEffect.
    /// Returns null if the file is missing or not a supported WAV format.
    /// </summary>
    public static SoundEffect? LoadWithOffset(string filePath, float offsetSeconds = 0f)
    {
        if (!File.Exists(filePath)) return null;

        byte[] bytes;
        try { bytes = File.ReadAllBytes(filePath); }
        catch { return null; }

        if (bytes.Length < 44) return null;
        if (bytes[0] != 'R' || bytes[1] != 'I' || bytes[2] != 'F' || bytes[3] != 'F') return null;
        if (bytes[8] != 'W' || bytes[9] != 'A' || bytes[10] != 'V' || bytes[11] != 'E') return null;

        // Walk subchunks looking for "fmt " and "data".
        int pos = 12;
        int sampleRate = 0;
        short channels = 0;
        short bitsPerSample = 0;
        int dataStart = -1;
        int dataSize = 0;

        while (pos + 8 <= bytes.Length)
        {
            string chunkId = Encoding.ASCII.GetString(bytes, pos, 4);
            int chunkSize = BitConverter.ToInt32(bytes, pos + 4);

            if (chunkId == "fmt ")
            {
                channels = BitConverter.ToInt16(bytes, pos + 10);
                sampleRate = BitConverter.ToInt32(bytes, pos + 12);
                bitsPerSample = BitConverter.ToInt16(bytes, pos + 22);
            }
            else if (chunkId == "data")
            {
                dataStart = pos + 8;
                dataSize = chunkSize;
                break;
            }
            pos += 8 + chunkSize;
        }

        if (dataStart < 0 || sampleRate == 0 || channels == 0) return null;
        if (bitsPerSample != 16) return null; // SoundEffect ctor expects 16-bit PCM.

        int blockAlign = (bitsPerSample / 8) * channels;
        int skipBytes = (int)(offsetSeconds * sampleRate * blockAlign);
        skipBytes = (skipBytes / blockAlign) * blockAlign; // align to sample boundary

        int trimmedStart = dataStart + skipBytes;
        int trimmedSize = dataSize - skipBytes;
        if (trimmedSize <= 0) return null;
        if (trimmedStart + trimmedSize > bytes.Length) trimmedSize = bytes.Length - trimmedStart;

        byte[] trimmedData = new byte[trimmedSize];
        Array.Copy(bytes, trimmedStart, trimmedData, 0, trimmedSize);

        var audioChannels = channels == 2 ? AudioChannels.Stereo : AudioChannels.Mono;
        return new SoundEffect(trimmedData, sampleRate, audioChannels);
    }
}
