using NAudio.Wave;

namespace DoubleHeadphones3D.Audio;

/// <summary>
/// A deliberately modest starter DSP: stereo width enhancement with a small
/// crossfeed component. This is NOT a full HRTF convolution engine and does
/// not create reliable object-based 3D positioning from arbitrary stereo audio.
/// Replace this class with a licensed/measured HRTF convolution implementation
/// for a stronger binaural spatial-audio effect.
/// </summary>
public static class StereoSpatializer
{
    // 1.0 = original width; 1.25 = slightly wider. Keep conservative to avoid clipping.
    private const float Width = 1.20f;
    private const float Crossfeed = 0.06f;

    public static void ProcessInPlace(byte[] buffer, int bytesRecorded, WaveFormat format)
    {
        if (format.Encoding != WaveFormatEncoding.IeeeFloat ||
            format.BitsPerSample != 32 ||
            format.Channels != 2)
            return;

        int usableBytes = bytesRecorded - (bytesRecorded % 8); // 2 float samples per stereo frame
        for (int offset = 0; offset < usableBytes; offset += 8)
        {
            float left = BitConverter.ToSingle(buffer, offset);
            float right = BitConverter.ToSingle(buffer, offset + 4);

            float mid = (left + right) * 0.5f;
            float side = (left - right) * 0.5f * Width;

            // Small crossfeed reduces extreme hard-panned separation.
            float outLeft = mid + side;
            float outRight = mid - side;
            outLeft = (outLeft * (1f - Crossfeed)) + (outRight * Crossfeed);
            outRight = (outRight * (1f - Crossfeed)) + (mid + side) * Crossfeed;

            // Soft safety clamp prevents invalid float values from escaping.
            outLeft = Math.Clamp(outLeft, -1f, 1f);
            outRight = Math.Clamp(outRight, -1f, 1f);

            Buffer.BlockCopy(BitConverter.GetBytes(outLeft), 0, buffer, offset, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(outRight), 0, buffer, offset + 4, 4);
        }
    }
}
