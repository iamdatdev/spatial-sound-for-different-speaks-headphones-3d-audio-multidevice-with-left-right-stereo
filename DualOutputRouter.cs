using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace DoubleBluetoothSpeakers;

/// <summary>
/// Captures a selected Windows recording endpoint (for example, VB-CABLE's
/// "CABLE Output") and routes left-channel content to the selected physical
/// left speaker and right-channel content to the selected physical right speaker.
/// </summary>
public sealed class DualOutputRouter : IDisposable
{
    private readonly MMDevice _leftDevice;
    private readonly MMDevice _rightDevice;
    private readonly MMDevice _inputDevice;
    private WasapiCapture? _capture;
    private WasapiOut? _leftOut;
    private WasapiOut? _rightOut;
    private BufferedWaveProvider? _leftBuffer;
    private BufferedWaveProvider? _rightBuffer;
    private DelayedWaveProvider? _leftDelay;
    private DelayedWaveProvider? _rightDelay;
    private bool _started;
    private bool _disposed;

    public int LeftDelayMs
    {
        get => _leftDelay?.DelayMs ?? 0;
        set { if (_leftDelay is not null) _leftDelay.DelayMs = value; }
    }

    public int RightDelayMs
    {
        get => _rightDelay?.DelayMs ?? 0;
        set { if (_rightDelay is not null) _rightDelay.DelayMs = value; }
    }

    public DualOutputRouter(MMDevice inputDevice, MMDevice leftDevice, MMDevice rightDevice)
    {
        _inputDevice = inputDevice;
        _leftDevice = leftDevice;
        _rightDevice = rightDevice;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started) return;

        // Capture a Windows recording endpoint directly. For VB-CABLE, select
        // "CABLE Output" (the recording/input endpoint), not "CABLE Input".
        _capture = new WasapiCapture(_inputDevice, useEventSync: true, audioBufferMilliseconds: 100);
        WaveFormat format = _capture.WaveFormat;
        Console.WriteLine($"Selected input format: {format}");

        if (!StereoChannelSplitter.IsSupported(format))
            throw new NotSupportedException(
                $"Expected 2-channel 32-bit float or 16-bit PCM audio; Windows provided {format}. " +
                "Set the Windows default playback format to stereo and try again.");

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
        if (_leftBuffer is null || _rightBuffer is null || _capture is null || e.BytesRecorded <= 0)
            return;

        byte[] left = new byte[e.BytesRecorded];
        byte[] right = new byte[e.BytesRecorded];
        StereoChannelSplitter.Split(e.Buffer, e.BytesRecorded, left, right, _capture.WaveFormat);

        _leftBuffer.AddSamples(left, 0, e.BytesRecorded);
        _rightBuffer.AddSamples(right, 0, e.BytesRecorded);
    }

    private static void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
            Console.WriteLine($"Input capture stopped: {e.Exception.Message}");
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
        _inputDevice.Dispose();
        _leftDevice.Dispose();
        _rightDevice.Dispose();
        _disposed = true;
    }
}
