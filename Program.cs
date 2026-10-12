using NAudio.CoreAudioApi;
using NAudio.Wave;
using DoubleHeadphones3D.Audio;

Console.Title = "Double Headphones 3D - Stereo + Latency";
Console.WriteLine("Double Headphones 3D — stereo-preserving dual output");
Console.WriteLine("Left/right channels are preserved; delay can be adjusted per headphone.");
Console.WriteLine();

using var enumerator = new MMDeviceEnumerator();
var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();

if (devices.Count < 2)
{
    Console.WriteLine("Windows reports fewer than two active playback devices.");
    Console.WriteLine("Connect both Bluetooth headphones in Settings > Bluetooth & devices, then retry.");
    return;
}

Console.WriteLine("Active playback devices:");
for (var i = 0; i < devices.Count; i++)
{
    var isDefault = devices[i].ID == enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
    Console.WriteLine($"  [{i}] {devices[i].FriendlyName}{(isDefault ? "  (Windows default capture source)" : "")}");
}

Console.WriteLine();
Console.WriteLine("For the first test, leave Windows default output on laptop speakers.");
Console.WriteLine("Choose your two Bluetooth headphones as the outputs to avoid feedback.");
var first = ReadIndex("Choose headphone A index: ", devices.Count);
var second = ReadIndex("Choose headphone B index: ", devices.Count);
if (first == second)
{
    Console.WriteLine("Choose two different devices. Exiting.");
    return;
}

using var router = new DualOutputRouter(devices[first], devices[second]);
router.Start();

Console.WriteLine();
Console.WriteLine("Controls (each press changes delay by 10 ms):");
Console.WriteLine("  A / Z = headphone A delay +10 / -10 ms");
Console.WriteLine("  K / M = headphone B delay +10 / -10 ms");
Console.WriteLine("  R     = reset both delays to 0 ms");
Console.WriteLine("  Q     = quit");
Console.WriteLine("Start with 0 ms, then add delay to the headphone that sounds EARLIER.");
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
    Console.WriteLine($"Headphone A delay: {router.FirstDelayMs} ms | Headphone B delay: {router.SecondDelayMs} ms");

static int ReadIndex(string prompt, int count)
{
    while (true)
    {
        Console.Write(prompt);
        var input = Console.ReadLine();
        if (int.TryParse(input, out var index) && index >= 0 && index < count)
            return index;
        Console.WriteLine($"Enter a number from 0 to {count - 1}.");
    }
}
