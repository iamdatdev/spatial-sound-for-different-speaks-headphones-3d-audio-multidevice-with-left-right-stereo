using NAudio.CoreAudioApi;
using DoubleBluetoothSpeakers.Audio;

Console.Title = "Double Bluetooth Speakers - Left/Right Split + Delay";
Console.WriteLine("Double Bluetooth Speakers — separate stereo channels across two speakers");
Console.WriteLine("Speaker A receives LEFT-channel audio only.");
Console.WriteLine("Speaker B receives RIGHT-channel audio only.");
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
Console.WriteLine("Choose the PHYSICAL LEFT speaker first, then the PHYSICAL RIGHT speaker.");
Console.WriteLine("The Windows default output is the capture source. For initial testing, keep it");
Console.WriteLine("on a third device (such as laptop speakers) to avoid feedback.");
Console.WriteLine();

int leftIndex = ReadIndex("Choose LEFT speaker index: ", devices.Count);
int rightIndex = ReadIndex("Choose RIGHT speaker index: ", devices.Count);
if (leftIndex == rightIndex)
{
    Console.WriteLine("Choose two different devices. Exiting.");
    return;
}

if (devices[leftIndex].ID == defaultDevice.ID || devices[rightIndex].ID == defaultDevice.ID)
    Console.WriteLine("WARNING: A selected speaker is the capture source; feedback/echo may occur.");

using var router = new DualOutputRouter(devices[leftIndex], devices[rightIndex]);
try
{
    router.Start();
}
catch (Exception ex)
{
    Console.WriteLine($"Could not start audio routing: {ex.Message}");
    Console.WriteLine("Check that both speakers are connected and active in Windows.");
    return;
}

Console.WriteLine();
Console.WriteLine("Channel routing:");
Console.WriteLine("  LEFT speaker: left channel only (right channel muted)");
Console.WriteLine("  RIGHT speaker: right channel only (left channel muted)");
Console.WriteLine();
Console.WriteLine("Delay controls (0–500 ms, manual adjustment):");
Console.WriteLine("  A / Z = left speaker delay +10 / -10 ms");
Console.WriteLine("  K / M = right speaker delay +10 / -10 ms");
Console.WriteLine("  R     = reset both delays to 0 ms");
Console.WriteLine("  Q     = quit");
Console.WriteLine("If a speaker sounds early, increase that speaker's delay.");
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
    Console.WriteLine($"Left speaker delay: {router.FirstDelayMs} ms | Right speaker delay: {router.SecondDelayMs} ms");

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
