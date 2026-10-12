using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace DoubleHeadphones3D.Audio;

/// <summary>
/// Captures default Windows render audio with WASAPI loopback and mirrors the
/// original stereo signal to two selected playback endpoints. Each endpoint
/// has an independently adjustable delay to help align Bluetooth devices.
/// </summary>
public sealed class DualOutputRouter : IDisposable
{
    private readonly MMDevice _firstDevice;
    private readonly MMDevice _secondDevice;
    private WasapiLoopbackCapture? _capture;
    private WasapiOut? _firstOut;
    private WasapiOut? _secondOut;
    private BufferedWaveProvider? _firstBuffer;
    private BufferedWaveProvider? _secondBuffer;
    private DelayedWaveProvider? _firstDelay;
    private DelayedWaveProvider? _secondDelay;
    private readonly object _gate = new();
    private bool _started;
    private bool _disposed;

    public int FirstDelayMs
    {
        get => _firstDelay?.DelayMs ?? 0;
        set { if (_firstDelay is not null) _firstDelay.DelayMs = value; }
    }

    public int SecondDelayMs
    {
        get => _secondDelay?.DelayMs ?? 0;
        set { if (_secondDelay is not null) _secondDelay.DelayMs = value; }
    }

    public DualOutputRouter(MMDevice firstDevice, MMDevice secondDevice)
    {
        _firstDevice = firstDevice;
        _secondDevice = secondDevice;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started) return;

        _capture = new WasapiLoopbackCapture();
        var format = _capture.WaveFormat;

        _firstBuffer = CreateBuffer(format);
        _secondBuffer = CreateBuffer(format);
        _firstDelay = new DelayedWaveProvider(_firstBuffer, 0);
        _secondDelay = new DelayedWaveProvider(_secondBuffer, 0);

        _firstOut = new WasapiOut(_firstDevice, AudioClientShareMode.Shared, useEventSync: true, latency: 100);
        _secondOut = new WasapiOut(_secondDevice, AudioClientShareMode.Shared, useEventSync: true, latency: 100);
        _firstOut.Init(_firstDelay);
        _secondOut.Init(_secondDelay);

        _capture.DataAvailable += OnDataAvailable;
        _capture.RecordingStopped += OnRecordingStopped;

        _firstOut.Play();
        _secondOut.Play();
        _capture.StartRecording();
        _started = true;

        Console.WriteLine($"Capture format: {format}");
        Console.WriteLine("Stereo channels are passed through unchanged (left stays left, right stays right).");
    }

    private static BufferedWaveProvider CreateBuffer(WaveFormat format) => new(format)
    {
        DiscardOnBufferOverflow = true,
        BufferDuration = TimeSpan.FromMilliseconds(1000),
        ReadFully = true
    };

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_firstBuffer is null || _secondBuffer is null || e.BytesRecorded <= 0)
            return;

        // Do not swap, downmix, or widen channels: preserve left/right positioning.
        lock (_gate)
        {
            _firstBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
            _secondBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }
    }

    private static void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
            Console.WriteLine($"Capture stopped: {e.Exception.Message}");
    }

    public void Stop()
    {
        if (!_started) return;

        try { _capture?.StopRecording(); } catch { }
        try { _firstOut?.Stop(); } catch { }
        try { _secondOut?.Stop(); } catch { }
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

        _firstOut?.Dispose();
        _secondOut?.Dispose();
        _firstDevice.Dispose();
        _secondDevice.Dispose();
        _disposed = true;
    }
}
