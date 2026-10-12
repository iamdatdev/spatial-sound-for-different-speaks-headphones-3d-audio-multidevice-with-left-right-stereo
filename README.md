# Double Bluetooth Speakers — left/right split + manual delay

Windows 11 C# prototype using NAudio/WASAPI loopback.

## Intended behavior
- The selected LEFT speaker receives only the source's left channel.
- The selected RIGHT speaker receives only the source's right channel.
- Each endpoint is given a valid stereo stream: left speaker gets `[L, silence]`; right speaker gets `[silence, R]`.
- Each speaker has a manual 0–500 ms delay adjustment.
- This is channel splitting across two physical speakers, not full stereo mirrored to both speakers.

## Requirements
- Windows 11 x64
- .NET 8 SDK for local builds
- Two speakers paired and visible as active playback endpoints

## Build locally
```powershell
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

## Controls
- `A` / `Z`: add/subtract 10 ms for LEFT speaker
- `K` / `M`: add/subtract 10 ms for RIGHT speaker
- `R`: reset delays
- `Q`: quit

## Setup notes
- This app captures the Windows default playback mix using WASAPI loopback.
- For initial testing, leave the Windows default playback output on a third device (such as laptop speakers) and select the two Bluetooth speakers in the app. Otherwise, selecting the capture source as an output may create feedback.
- The implementation supports 32-bit float stereo and 16-bit PCM stereo loopback. It will show an error for other formats.
- Delay controls are manual; they do not measure Bluetooth latency automatically. Adjusting delay resets the delay line and can briefly interrupt audio.
- This source has not been tested on the user's specific PC.
