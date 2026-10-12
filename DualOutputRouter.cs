using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace DoubleBluetoothSpeakers.Audio;

/// <summary>
/// Captures the Windows default render mix using WASAPI loopback and sends
/// identical interleaved audio frames to two selected output endpoints.
/// No channel swap, split, downmix, or widening is applied.
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
        WaveFormat format = _capture.WaveFormat;
        Console.WriteLine($"Windows loopback format: {format}");
        if (format.Channels != 2)
            Console.WriteLine("WARNING: capture is not stereo; Windows may be configured for a non-stereo default output.");

        _firstBuffer = CreateBuffer(format);
        _secondBuffer = CreateBuffer(format);
        _firstDelay = new DelayedWaveProvider(_firstBuffer);
        _secondDelay = new DelayedWaveProvider(_secondBuffer);

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

        // Exact copy preserves the original channel ordering and sample values.
        _firstBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
        _secondBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
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
