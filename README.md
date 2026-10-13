# Double Bluetooth Speakers — Virtual Input build

This build lets you choose a Windows **recording/input endpoint** as the audio source, then routes the source's left channel to the selected left Bluetooth speaker and its right channel to the selected right Bluetooth speaker. Independent manual output delays are included.

## VB-CABLE setup

1. In Windows Sound settings, set the playback/output device for the audio you want to route to **CABLE Input (VB-Audio Virtual Cable)**. You can set it as the Windows default output, or assign individual apps to it under Volume mixer.
2. Run the app.
3. In the **INPUT** list, choose **CABLE Output**. This is the recording endpoint that receives audio sent into the virtual cable. Do not choose `CABLE Input` from the input list.
4. Choose the physical left and right Bluetooth speakers from the **OUTPUT** list.
5. Adjust latency using the on-screen keys: `A/Z` for left delay, `K/M` for right delay, `R` to reset, `Q` to quit.

If the cable is not listed, enable it under Windows Settings > System > Sound > All sound devices. The exact device names may vary by VB-CABLE version.

## Important

- The project targets Windows x64 and .NET 8.
- Source format currently supports stereo 32-bit float or stereo 16-bit PCM. If the selected input uses a different format, set its Windows format to stereo and try again.
- The program's output is the two selected Bluetooth devices; it does not automatically mirror audio to the laptop speakers.
- Bluetooth devices can have substantial and variable latency. The manual delay controls can delay either side, but cannot make a device play earlier.
- The source code has been updated but was not compiled or tested in this environment.
