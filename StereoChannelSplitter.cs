using NAudio.Wave;

namespace DoubleBluetoothSpeakers.Audio;

/// <summary>
/// Produces two stereo buffers from one interleaved stereo input:
/// left output = [L, 0], right output = [0, R].
/// This means each physical speaker receives its own side's audio while
/// remaining a valid two-channel stream for Windows playback APIs.
/// </summary>
public static class StereoChannelSplitter
{
    public static bool IsSupported(WaveFormat format) =>
        format.Channels == 2 &&
        ((format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32) ||
         (format.Encoding == WaveFormatEncoding.Pcm && format.BitsPerSample == 16));

    public static void Split(byte[] source, int bytesRecorded, byte[] left, byte[] right, WaveFormat format)
    {
        if (!IsSupported(format))
            throw new NotSupportedException($"Unsupported audio format: {format}");

        int frameSize = format.BlockAlign;
        int usable = bytesRecorded - (bytesRecorded % frameSize);

        if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
        {
            for (int i = 0; i < usable; i += 8)
            {
                // Interleaved float32 stereo: L in bytes 0..3, R in bytes 4..7.
                Buffer.BlockCopy(source, i, left, i, 4);
                Array.Clear(left, i + 4, 4);
                Array.Clear(right, i, 4);
                Buffer.BlockCopy(source, i + 4, right, i + 4, 4);
            }
        }
        else // signed 16-bit PCM stereo
        {
            for (int i = 0; i < usable; i += 4)
            {
                Buffer.BlockCopy(source, i, left, i, 2);
                Array.Clear(left, i + 2, 2);
                Array.Clear(right, i, 2);
                Buffer.BlockCopy(source, i + 2, right, i + 2, 2);
            }
        }

        // Copy any incomplete trailing bytes unchanged only to avoid leaving buffers uninitialized.
        if (usable < bytesRecorded)
        {
            Buffer.BlockCopy(source, usable, left, usable, bytesRecorded - usable);
            Buffer.BlockCopy(source, usable, right, usable, bytesRecorded - usable);
        }
    }
}
