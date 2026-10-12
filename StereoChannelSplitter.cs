using NAudio.Wave;

namespace DoubleBluetoothSpeakers;

/// <summary>
/// Converts interleaved stereo into two per-speaker streams.
/// LEFT stream: [L, L] — left content copied to both channels of the left
/// speaker's stream. RIGHT stream: [R, R] — right content copied to both
/// channels of the right speaker's stream. Duplicating each side is important
/// for mono Bluetooth speakers that downmix their stereo input.
/// </summary>
public static class StereoChannelSplitter
{
    public static bool IsSupported(WaveFormat format) =>
        format.Channels == 2 &&
        ((format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32) ||
         (format.Encoding == WaveFormatEncoding.Pcm && format.BitsPerSample == 16));

    public static void Split(byte[] source, int byteCount, byte[] left, byte[] right, WaveFormat format)
    {
        if (!IsSupported(format))
            throw new NotSupportedException($"Unsupported capture format: {format}");

        int frameSize = format.BlockAlign;
        int usable = byteCount - (byteCount % frameSize);

        if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
        {
            for (int i = 0; i < usable; i += 8)
            {
                // Input frame: [L float32][R float32]. Output frames: [L][L], [R][R].
                Buffer.BlockCopy(source, i, left, i, 4);
                Buffer.BlockCopy(source, i, left, i + 4, 4);
                Buffer.BlockCopy(source, i + 4, right, i, 4);
                Buffer.BlockCopy(source, i + 4, right, i + 4, 4);
            }
        }
        else
        {
            for (int i = 0; i < usable; i += 4)
            {
                // Input frame: [L int16][R int16]. Output frames: [L][L], [R][R].
                Buffer.BlockCopy(source, i, left, i, 2);
                Buffer.BlockCopy(source, i, left, i + 2, 2);
                Buffer.BlockCopy(source, i + 2, right, i, 2);
                Buffer.BlockCopy(source, i + 2, right, i + 2, 2);
            }
        }

        // A capture callback should contain whole frames; zero-fill any unexpected tail.
        if (usable < byteCount)
        {
            Array.Clear(left, usable, byteCount - usable);
            Array.Clear(right, usable, byteCount - usable);
        }
    }
}
