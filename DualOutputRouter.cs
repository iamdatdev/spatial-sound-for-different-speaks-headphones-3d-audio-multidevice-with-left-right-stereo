using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace DoubleBluetoothSpeakers.Audio;

/// <summary>
/// Captures the Windows default render mix, routes only the left channel to
/// the first selected speaker and only the right channel to the second speaker.
/// </summary>
public sealed class DualOutputRouter : IDisposable
{
    private readonly MMDevice _leftDevice;
    private readonly MMDevice _rightDevice;
    private WasapiLoopbackCapture? _capture;
    private WasapiOut? _leftOut;
    private WasapiOut? _rightOut;
    private BufferedWaveProvider? _leftBuffer;
    private BufferedWaveProvider? _rightBuffer;
    private DelayedWaveProvider? _leftDelay;
    private DelayedWaveProvider? _rightDelay;
    private bool _started;
    private bool _disposed;

    public int FirstDelayMs
    {
        get => _leftDelay?.DelayMs ?? 0;
        set { if (_leftDelay is not null) _leftDelay.DelayMs = value; }
    }

    public int SecondDelayMs
    {
        get => _rightDelay?.DelayMs ?? 0;
        set { if (_rightDelay is not null) _rightDelay.DelayMs = value; }
    }

    public DualOutputRouter(MMDevice leftDevice, MMDevice rightDevice)
    {
        _leftDevice = leftDevice;
        _rightDevice = rightDevice;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started) return;

        _capture = new WasapiLoopbackCapture();
        WaveFormat format = _capture.WaveFormat;
        Console.WriteLine($"Windows loopback format: {format}");

        if (format.Channels != 2)
            throw new NotSupportedException($"Expected stereo loopback capture, but Windows returned {format.Channels} channels. Set the Windows default output format to stereo and try again.");

        if (!StereoChannelSplitter.IsSupported(format))
            throw new NotSupportedException($"Unsupported capture format: {format}. This build supports 32-bit float or 16-bit PCM stereo.");

        _leftBuffer = CreateBuffer(format);
        _rightBuffer = CreateBuffer(format);
        _leftDelay = new DelayedWaveProvider(_leftBuffer);
        _rightDelay = new DelayedWaveProvider(_rightBuffer);

        _leftOut = new WasapiOut(_leftDevice, AudioClientShareMode.Shared, useEventSync: true, latency: 100);
        _rightOut = new WasapiOut(_rightDevice, AudioClientShareMode.Shared, useEventSync: true, latency: 100);
        _leftOut.Init(_leftDelay);
        _rightOut.Init(_rightDelay);

        _capture.DataAvailable += OnDataAvailable;
        _capture.RecordingStopped += OnRecordingStopped;
        _leftOut.Play();
        _rightOut.Play();
        _capture.StartRecording();
        _started = true;
    }

    private static BufferedWaveProvider CreateBuffer(WaveFormat format) => new(format)
    {
        DiscardOnBufferOverflow = true,
        BufferDuration = TimeSpan.FromMilliseconds(1000),
        ReadFully = true
    };

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_leftBuffer is null || _rightBuffer is null || e.BytesRecorded <= 0) return;

        byte[] left = new byte[e.BytesRecorded];
        byte[] right = new byte[e.BytesRecorded];
        StereoChannelSplitter.Split(e.Buffer, e.BytesRecorded, left, right, _capture!.WaveFormat);

        _leftBuffer.AddSamples(left, 0, e.BytesRecorded);
        _rightBuffer.AddSamples(right, 0, e.BytesRecorded);
    }

    private static void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
            Console.WriteLine($"Loopback capture stopped: {e.Exception.Message}");
    }

    public void Stop()
    {
        if (!_started) return;
        try { _capture?.StopRecording(); } catch { }
        try { _leftOut?.Stop(); } catch { }
        try { _rightOut?.Stop(); } catch { }
        _started = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        if (_capture is not null)
        {
            _capture.DataAvailable -= OnDataAvailable;
            _capture.RecordingStopped -= OnRecordingStopped;
            _capture.Dispose();
        }
        _leftOut?.Dispose();
        _rightOut?.Dispose();
        _leftDevice.Dispose();
        _rightDevice.Dispose();
        _disposed = true;
    }
}
