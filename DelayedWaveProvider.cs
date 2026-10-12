using NAudio.Wave;

namespace DoubleBluetoothSpeakers.Audio;

/// <summary>
/// Manual stream delay for aligning two independent speaker outputs.
/// Delay is adjustable from 0 to 500 ms; no automatic latency measurement.
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
                _delayMs = Math.Clamp(value, 0, 500);
                int delayBytes = GetDelayBytes(_delayMs);
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
            int delayBytes = GetDelayBytes(_delayMs);
            if (delayBytes == 0) return read;

            int alignedRead = read - (read % _blockAlign);
            for (int i = 0; i < alignedRead; i++)
            {
                int readPosition = (_writePosition - delayBytes + _ring.Length) % _ring.Length;
                byte delayedByte = _ring[readPosition];
                _ring[_writePosition] = buffer[offset + i];
                buffer[offset + i] = delayedByte;
                _writePosition++;
                if (_writePosition >= _ring.Length) _writePosition = 0;
            }
            return read;
        }
    }

    private int GetDelayBytes(int milliseconds)
    {
        long raw = (long)_bytesPerSecond * milliseconds / 1000;
        return (int)(raw - raw % _blockAlign);
    }
}
