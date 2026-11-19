# Generic Wupstream Program

[Guía en español](README.md)

Connect a Wii U GamePad WUP-010 to a Windows PC over your local network. Stream your PC display and sound to the GamePad and use its controls as a virtual Xbox 360 controller.

Choose **Español / English** in the app before you connect.

## Connect

1. On the GamePad, open the **Homebrew Menu**, join Wi-Fi from **Settings**, and install **WUP Stream** from the **App Store**. If your GamePad still starts in Nintendo mode, follow the [Homebrew Menu setup guide](https://github.com/ravensclawx/wup-homebrew#get-it). The usual installation uses a Wii U with Aroma and GamePad firmware 7677.
2. Launch WUP Stream. Leave it on **Waiting for a Stream** and note the IPv4 address. The GamePad and PC must be on the same network. The PC can use Ethernet.
3. On Windows 10 or 11 x64 with .NET Framework 4.8 and [ViGEmBus](https://github.com/nefarius/ViGEmBus/releases/latest), run the setup from [Releases](https://github.com/JLiveOf/Generic-Wupstream-Program/releases) or extract the portable ZIP. Open **Generic Wupstream Program**, select your display, enter the GamePad IP, and click **Connect by IP**. Leave the address blank and click **Find GamePad** for UDP discovery.

The bilingual setup installs the app for the current user, creates a Start menu shortcut, and registers an uninstaller. Get ViGEmBus separately. Close the app before upgrading.

If the link drops, the app releases the controls, pauses video, and tries to recover for 20 seconds at the same IP address. Video resumes with a keyframe. The video bitrate cap starts at 3.2 Mb/s and adapts to retransmission requests. The app shows the cap and the request rate during streaming. Audio uses additional bandwidth.

## Controls and media

- **Stream screen** captures the selected monitor and preserves its aspect ratio on the 854 × 480 GamePad screen. Windowed and borderless games generally work with desktop capture. The Windows secure desktop, protected video, and some exclusive fullscreen games may appear black.
- **Stream PC audio** copies the sound from the default Windows playback device. You can still hear the audio on the PC. Use the GamePad volume slider to control its speakers or headphones.
- **Use touch as mouse** turns taps and drags on the streamed display into left mouse clicks and drags. It starts disabled and can be toggled during the session.
- **Test vibration** activates the motor for one third of a second. Games can also use the virtual controller's vibration. Use **Brightness** to adjust the screen during the session.
- **Enable microphone** receives audio from the GamePad microphone and shows its level. Choose **Record WAV…** before connecting to save 48 kHz mono PCM. The file closes when you disconnect.
- To use the microphone in Discord, OBS, or another Windows app, install [VB-CABLE](https://vb-audio.com/Cable/), select **Enable microphone** and **Send to VB-CABLE**, then choose **CABLE Output** as the input in the other app. Keep a physical playback device as the Windows default to keep streaming PC audio to the GamePad.
- **Test in Windows** opens the game controller control panel. Leave the sticks centered when you connect. A, B, X, and Y match the printed GamePad letters by default. Toggle **Xbox ABXY positions** to swap A/B and X/Y and match the physical positions of an Xbox controller. You can change it while connected. **+** maps to Start, **−** to Back, and **HOME** to Guide. Accelerometer and gyroscope values appear in the app as diagnostics.

The camera, NFC/Amiibo, and infrared need additional support in the GamePad software. WUP Stream does not transmit these peripherals. The local UDP connection is unencrypted.

## Build

Install Git for Windows, .NET Framework 4.8, and a Windows x64 MinGW-w64 toolchain with Make, such as [w64devkit](https://github.com/skeeto/w64devkit). Add [NASM](https://www.nasm.us/) to the PATH for x264 SIMD support. From the source directory, run in PowerShell:

    .\host\windows\Build.ps1 -ToolchainBin 'C:\path\to\w64devkit\bin'

The build outputs appear in `build/windows`. Windows builds require x264 SIMD support because missed encoding deadlines can stall the GamePad decoder. The build fetches a pinned x264 revision, applies the DRH patch, and verifies the Nefarius.ViGEm.Client package hash. Pass `-GitBash` if Git Bash is installed in a custom location.

After building the app, run `host/windows/BuildSetup.ps1` with [NSIS 3](https://nsis.sourceforge.io/Download) on the PATH or pass the compiler path as `-MakeNsis`.

The app and library changes are GPL-3.0-or-later. Based on [ravensclawx/wup-stream](https://github.com/ravensclawx/wup-stream/tree/790aef47a28146f01a535ab1afdc7990ddcb2dfb). The native engine contains [x264](https://github.com/mirror/x264/tree/b35605ace3ddf7c1a5d67a2eb553f034aef41d55) under the GPL. Nefarius.ViGEm.Client is MIT-licensed.
