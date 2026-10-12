# Double Headphones 3D — stereo + latency prototype

Windows 11 C# prototype using NAudio/WASAPI loopback. It duplicates the captured stereo stream to two selected render devices while preserving left/right channel assignment, and provides independent delay controls (0–500 ms) for each output.

## Requirements
- Windows 11 x64
- .NET 8 SDK (only needed to build locally)
- Two Bluetooth headphones paired and connected in Windows

## Build locally
Open PowerShell in the project folder:
```powershell
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

## Controls
- `A` / `Z`: add / subtract 10 ms delay for headphone A
- `K` / `M`: add / subtract 10 ms delay for headphone B
- `R`: reset both delays
- `Q`: quit

Delay range is 0–500 ms. To align two Bluetooth headphones, start at 0 ms and add delay to the headphone that sounds earlier. This is manual delay adjustment, not automatic latency detection. Some Bluetooth devices already add substantial internal latency.

## Setup warning
This captures the Windows default playback endpoint using WASAPI loopback. For initial testing, leave Windows' default playback device on the laptop speakers and select the two Bluetooth headphones as outputs. Selecting the same endpoint as both capture source and output can create feedback. With this design, the laptop speakers may also play the original signal.

## Stereo behavior
The app copies the captured samples unchanged. Left-channel content remains on the left channel and right-channel content remains on the right channel. This is stereo pass-through, not a full HRTF/3D positional renderer.

## Limitations
- Console interface only.
- No automatic per-device latency measurement.
- No guarantee of sample-accurate synchronization across independent Bluetooth stacks/codecs.
- A delay change resets that output's delay line, so a short click/dropout can occur when adjusting it.
- Format conversion and endpoint behavior depend on Windows and the Bluetooth drivers.
- Prototype; not yet compile-tested on the user's computer.
