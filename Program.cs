using NAudio.CoreAudioApi;
using DoubleBluetoothSpeakers;

Console.Title = "Double Bluetooth Speakers - Correct Stereo";
Console.WriteLine("Double Bluetooth Speakers — correct left/right routing");
Console.WriteLine("LEFT speaker gets the source LEFT channel.");
Console.WriteLine("RIGHT speaker gets the source RIGHT channel.");
Console.WriteLine("Each side is duplicated to both channels of its own output stream so mono Bluetooth speakers work too.");
Console.WriteLine();

using var enumerator = new MMDeviceEnumerator();
using var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();

if (devices.Count < 2)
{
    Console.WriteLine("Windows sees fewer than two active playback devices.");
    Console.WriteLine("Connect both Bluetooth speakers in Settings > Bluetooth & devices, then restart.");
    return;
}

Console.WriteLine("Active playback devices:");
for (int i = 0; i < devices.Count; i++)
{
    string tag = devices[i].ID == defaultDevice.ID ? " (Windows default loopback source)" : "";
    Console.WriteLine($"  [{i}] {devices[i].FriendlyName}{tag}");
}

Console.WriteLine();
Console.WriteLine("Choose the physical LEFT speaker first, then the physical RIGHT speaker.");
Console.WriteLine("For initial testing, keep Windows' default output on a separate device to avoid feedback.");
int leftIndex = ReadIndex("LEFT speaker index: ", devices.Count);
int rightIndex = ReadIndex("RIGHT speaker index: ", devices.Count);

if (leftIndex == rightIndex)
{
    Console.WriteLine("Select two different devices. Exiting.");
    return;
}

if (devices[leftIndex].ID == defaultDevice.ID || devices[rightIndex].ID == defaultDevice.ID)
    Console.WriteLine("WARNING: An output is also the loopback source; feedback or echo may occur.");

using var router = new DualOutputRouter(devices[leftIndex], devices[rightIndex]);
try
{
    router.Start();
}
catch (Exception ex)
{
    Console.WriteLine($"Could not start routing: {ex.Message}");
    Console.WriteLine("Check that both speakers are connected and listed as active Windows playback devices.");
    return;
}

Console.WriteLine();
Console.WriteLine("Controls (manual delay 0–500 ms):");
Console.WriteLine("  A / Z = LEFT speaker delay +10 / -10 ms");
Console.WriteLine("  K / M = RIGHT speaker delay +10 / -10 ms");
Console.WriteLine("  R     = reset both delays");
Console.WriteLine("  Q     = quit");
Console.WriteLine("If one speaker is heard earlier, increase that speaker's delay.");
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
