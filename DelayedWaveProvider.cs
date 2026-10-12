using NAudio.Wave;

namespace DoubleHeadphones3D.Audio;

/// <summary>
/// Adds an adjustable delay to a byte stream while preserving audio frame order.
/// Delay changes take effect immediately; changing it can cause a brief timing
/// transition. This is a practical alignment control, not automatic latency measurement.
/// </summary>
public sealed class DelayedWaveProvider : IWaveProvider
{
    private readonly IWaveProvider _source;
    private readonly int _bytesPerSecond;
    private readonly int _blockAlign;
    private readonly object _gate = new();
    private byte[] _ring = Array.Empty<byte>();
    private int _writePosition;
    private int _delayMs;

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int DelayMs
    {
        get { lock (_gate) return _delayMs; }
        set
        {
            lock (_gate)
            {
                int clamped = Math.Clamp(value, 0, 500);
                if (clamped == _delayMs) return;
                _delayMs = clamped;
                int delayBytes = GetAlignedDelayBytes(clamped);
                _ring = new byte[Math.Max(_blockAlign, delayBytes + _blockAlign)];
                _writePosition = 0;
            }
        }
    }

    public DelayedWaveProvider(IWaveProvider source, int initialDelayMs = 0)
    {
        _source = source;
        _bytesPerSecond = source.WaveFormat.AverageBytesPerSecond;
        _blockAlign = Math.Max(1, source.WaveFormat.BlockAlign);
        DelayMs = initialDelayMs;
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        int read = _source.Read(buffer, offset, count);
        if (read <= 0) return read;

        lock (_gate)
        {
            int delayBytes = GetAlignedDelayBytes(_delayMs);
            if (delayBytes == 0) return read;

            // Ensure delay buffer can hold the selected delay and current audio frame.
            int required = Math.Max(_blockAlign, delayBytes + _blockAlign);
            if (_ring.Length < required)
            {
                _ring = new byte[required];
                _writePosition = 0;
            }

            int alignedRead = read - (read % _blockAlign);
            for (int i = 0; i < alignedRead; i++)
            {
                int readPosition = (_writePosition - delayBytes + _ring.Length) % _ring.Length;
                byte delayed = _ring[readPosition];
                _ring[_writePosition] = buffer[offset + i];
                buffer[offset + i] = delayed;
                _writePosition++;
                if (_writePosition >= _ring.Length) _writePosition = 0;
            }

            // Preserve any trailing partial frame unchanged (normally none).
            return read;
        }
    }

    private int GetAlignedDelayBytes(int milliseconds)
    {
        long raw = (long)_bytesPerSecond * milliseconds / 1000;
        return (int)(raw - (raw % _blockAlign));
    }
}
