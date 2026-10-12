# Double Bluetooth Speakers — stereo + manual delay

A Windows 11 C# prototype that captures the Windows default playback mix through WASAPI loopback and mirrors the same full audio stream to two selected playback devices.

## Behavior
- Both speakers receive the full stereo stream.
- Left-channel samples remain in the left channel and right-channel samples remain in the right channel for each output endpoint.
- Each speaker has a manually adjustable 0–500 ms delay.
- This does not split left audio onto one speaker and right audio onto the other.
- This does not automatically measure or synchronize Bluetooth latency.

## Requirements
- Windows 11 x64
- .NET 8 SDK (for local build)
- Two speakers paired and connected as separate active Windows playback devices

## Build locally
```powershell
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

## Controls
- `A` / `Z`: add / subtract 10 ms delay for speaker A
- `K` / `M`: add / subtract 10 ms delay for speaker B
- `R`: reset both delays
- `Q`: quit

Start at 0 ms. Add delay to the speaker that sounds earlier. Adjusting delay resets that output's delay line and may cause a short dropout.

## Avoid feedback
The app captures the Windows default output. For initial testing, keep the Windows default output on a third device (such as laptop speakers), then select the two target speakers in the app. If you choose the default capture endpoint as one of the outputs, feedback/echo may occur. With this loopback design, the default device can also play the original audio.

## GitHub Actions build
The workflow at `.github/workflows/build-windows.yml` builds a self-contained Windows x64 executable and uploads `DoubleBluetoothSpeakers-win-x64` as an artifact.
