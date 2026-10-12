# Double Bluetooth Speakers — correct left/right routing

This Windows 11 C# prototype captures the Windows default stereo playback mix and routes it to two separately selected playback devices.

- The physical LEFT speaker receives the source LEFT channel.
- The physical RIGHT speaker receives the source RIGHT channel.
- Each selected speaker's output stream contains that side's signal duplicated into both channels (`[L,L]` for left; `[R,R]` for right). This avoids relying on a mono Bluetooth speaker's downmix behavior.
- Center-panned content is present in both source channels, so it will naturally play from both speakers.
- Independent manual output delay controls are included (0–500 ms, 10 ms increments).

## Build locally (PowerShell)
```powershell
dotnet restore DoubleBluetoothSpeakers.csproj
dotnet build DoubleBluetoothSpeakers.csproj -c Release
dotnet run --project DoubleBluetoothSpeakers.csproj -c Release
```

## Controls
- `A` / `Z`: add/subtract 10 ms delay for LEFT speaker
- `K` / `M`: add/subtract 10 ms delay for RIGHT speaker
- `R`: reset delays
- `Q`: quit

## Important setup
The app captures the Windows default render mix through WASAPI loopback. For initial testing, keep the Windows default output on a separate third device, such as laptop speakers, and choose the two Bluetooth speakers as outputs. Otherwise feedback or echo may occur. Set Windows playback to stereo if possible.

Delay is manual, not automatic latency measurement. Bluetooth devices may still have different internal latency. This project has not been tested on the user's specific hardware.
