using NAudio.CoreAudioApi;
using DoubleBluetoothSpeakers;

Console.Title = "Double Bluetooth Speakers - Virtual Input";
Console.WriteLine("Double Bluetooth Speakers — virtual input + correct stereo routing");
Console.WriteLine("Select the audio INPUT first, then the physical LEFT and RIGHT speakers.");
Console.WriteLine("For VB-CABLE, the recording endpoint is usually named 'CABLE Output'.");
Console.WriteLine();

using var enumerator = new MMDeviceEnumerator();
var inputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
var outputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();

if (inputDevices.Count == 0)
{
    Console.WriteLine("Windows sees no active recording/input devices.");
    Console.WriteLine("Check Settings > System > Sound > All sound devices, and make sure VB-CABLE is enabled.");
    return;
}

if (outputDevices.Count < 2)
{
    Console.WriteLine("Windows sees fewer than two active playback devices.");
    Console.WriteLine("Connect both Bluetooth speakers in Settings > Bluetooth & devices, then restart.");
    return;
}

Console.WriteLine("ACTIVE INPUT DEVICES (recording endpoints):");
for (int i = 0; i < inputDevices.Count; i++)
    Console.WriteLine($"  [{i}] {inputDevices[i].FriendlyName}");

Console.WriteLine();
Console.WriteLine("ACTIVE OUTPUT DEVICES (playback endpoints):");
for (int i = 0; i < outputDevices.Count; i++)
    Console.WriteLine($"  [{i}] {outputDevices[i].FriendlyName}");

Console.WriteLine();
Console.WriteLine("Choose the VB-CABLE recording endpoint (normally 'CABLE Output').");
int inputIndex = ReadIndex("INPUT device index: ", inputDevices.Count);
Console.WriteLine("Now choose the physical speakers.");
int leftIndex = ReadIndex("LEFT speaker index: ", outputDevices.Count);
int rightIndex = ReadIndex("RIGHT speaker index: ", outputDevices.Count);

if (leftIndex == rightIndex)
{
    Console.WriteLine("Select two different output devices. Exiting.");
    return;
}

// Dispose unused endpoint handles to avoid holding unnecessary device references.
for (int i = 0; i < inputDevices.Count; i++)
    if (i != inputIndex) inputDevices[i].Dispose();
for (int i = 0; i < outputDevices.Count; i++)
    if (i != leftIndex && i != rightIndex) outputDevices[i].Dispose();

using var router = new DualOutputRouter(inputDevices[inputIndex], outputDevices[leftIndex], outputDevices[rightIndex]);
try
{
    router.Start();
}
catch (Exception ex)
{
    Console.WriteLine($"Could not start routing: {ex.Message}");
    Console.WriteLine("Check that the selected input is enabled and uses stereo 32-bit float or 16-bit PCM.");
    Console.WriteLine("For VB-CABLE, select 'CABLE Output' from the INPUT list.");
    return;
}

Console.WriteLine();
Console.WriteLine("Controls (manual delay 0–500 ms):");
Console.WriteLine("  A / Z = LEFT speaker delay +10 / -10 ms");
Console.WriteLine("  K / M = RIGHT speaker delay +10 / -10 ms");
Console.WriteLine("  R     = reset both delays");
Console.WriteLine("  Q     = quit");
Console.WriteLine("Set Windows' playback output to 'CABLE Input' so app audio enters the virtual cable.");
Console.WriteLine("This app captures the matching 'CABLE Output' recording endpoint.");
ShowDelays();

while (true)
{
    switch (Console.ReadKey(intercept: true).Key)
    {
        case ConsoleKey.A:
            router.LeftDelayMs = Math.Min(500, router.LeftDelayMs + 10);
            ShowDelays();
            break;
        case ConsoleKey.Z:
            router.LeftDelayMs = Math.Max(0, router.LeftDelayMs - 10);
            ShowDelays();
            break;
        case ConsoleKey.K:
            router.RightDelayMs = Math.Min(500, router.RightDelayMs + 10);
            ShowDelays();
            break;
        case ConsoleKey.M:
            router.RightDelayMs = Math.Max(0, router.RightDelayMs - 10);
            ShowDelays();
            break;
        case ConsoleKey.R:
            router.LeftDelayMs = 0;
            router.RightDelayMs = 0;
            ShowDelays();
            break;
        case ConsoleKey.Q:
            router.Stop();
            return;
    }
}

void ShowDelays() =>
    Console.WriteLine($"LEFT delay: {router.LeftDelayMs} ms | RIGHT delay: {router.RightDelayMs} ms");

static int ReadIndex(string prompt, int count)
{
    while (true)
    {
        Console.Write(prompt);
        if (int.TryParse(Console.ReadLine(), out int value) && value >= 0 && value < count)
            return value;
        Console.WriteLine($"Enter a number from 0 to {count - 1}.");
    }
}
