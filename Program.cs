using NAudio.CoreAudioApi;
using DoubleBluetoothSpeakers.Audio;

Console.Title = "Double Bluetooth Speakers - Stereo + Delay";
Console.WriteLine("Double Bluetooth Speakers");
Console.WriteLine("Mirrors the same full stereo mix to two selected playback devices.");
Console.WriteLine("Left/right channels remain in their original positions within each output stream.");
Console.WriteLine();

using var enumerator = new MMDeviceEnumerator();
var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();

if (devices.Count < 2)
{
    Console.WriteLine("Windows reports fewer than two active playback devices.");
    Console.WriteLine("Connect both speakers in Settings > Bluetooth & devices, then restart.");
    return;
}

var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
Console.WriteLine("Active playback devices:");
for (int i = 0; i < devices.Count; i++)
{
    string tag = devices[i].ID == defaultDevice.ID ? " (Windows default capture source)" : "";
    Console.WriteLine($"  [{i}] {devices[i].FriendlyName}{tag}");
}

Console.WriteLine();
Console.WriteLine("Select the two speakers you want to play the same audio.");
Console.WriteLine("For the first test, keep Windows' default output on a THIRD device (for example, laptop speakers).");
Console.WriteLine("This avoids routing the captured loopback audio back into its own capture source.");
Console.WriteLine();

int first = ReadIndex("Choose speaker A index: ", devices.Count);
int second = ReadIndex("Choose speaker B index: ", devices.Count);

if (first == second)
{
    Console.WriteLine("Select two different playback devices. Exiting.");
    return;
}

if (devices[first].ID == defaultDevice.ID || devices[second].ID == defaultDevice.ID)
{
    Console.WriteLine("WARNING: One selected speaker is also the loopback capture source.");
    Console.WriteLine("This can cause feedback or echo. Prefer a separate Windows default output.");
}

using var router = new DualOutputRouter(devices[first], devices[second]);
try
{
    router.Start();
}
catch (Exception ex)
{
    Console.WriteLine($"Could not start audio routing: {ex.Message}");
    Console.WriteLine("Check that both speakers are connected and listed as active playback devices.");
    return;
}

Console.WriteLine();
Console.WriteLine("Both outputs receive the full stereo stream.");
Console.WriteLine("Delay controls (manual adjustment, 0–500 ms):");
Console.WriteLine("  A / Z = speaker A delay +10 / -10 ms");
Console.WriteLine("  K / M = speaker B delay +10 / -10 ms");
Console.WriteLine("  R     = reset both delays to 0 ms");
Console.WriteLine("  Q     = quit");
Console.WriteLine("If one speaker is heard earlier, add delay to that speaker.");
ShowDelays();

while (true)
{
    var key = Console.ReadKey(intercept: true).Key;
    switch (key)
    {
        case ConsoleKey.Q:
            router.Stop();
            return;
        case ConsoleKey.A:
            router.FirstDelayMs = Math.Min(500, router.FirstDelayMs + 10);
            ShowDelays();
            break;
        case ConsoleKey.Z:
            router.FirstDelayMs = Math.Max(0, router.FirstDelayMs - 10);
            ShowDelays();
            break;
        case ConsoleKey.K:
            router.SecondDelayMs = Math.Min(500, router.SecondDelayMs + 10);
            ShowDelays();
            break;
        case ConsoleKey.M:
            router.SecondDelayMs = Math.Max(0, router.SecondDelayMs - 10);
            ShowDelays();
            break;
        case ConsoleKey.R:
            router.FirstDelayMs = 0;
            router.SecondDelayMs = 0;
            ShowDelays();
            break;
    }
}

void ShowDelays() =>
    Console.WriteLine($"Speaker A delay: {router.FirstDelayMs} ms | Speaker B delay: {router.SecondDelayMs} ms");

static int ReadIndex(string prompt, int count)
{
    while (true)
    {
        Console.Write(prompt);
        string? input = Console.ReadLine();
        if (int.TryParse(input, out int index) && index >= 0 && index < count)
            return index;
        Console.WriteLine($"Enter a number from 0 to {count - 1}.");
    }
}
