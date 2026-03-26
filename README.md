# ZyntaSchoolBell

A lightweight Windows desktop application that plays multilingual audio announcements (Sinhala, Tamil, English) at scheduled times for school periods.

## Features

- **Multilingual announcements**: Plays audio in Sinhala → Tamil → English sequence
- **Multiple schedule profiles**: Regular Day, Exam Day, Half Day, Saturday School, Special Day
- **System tray integration**: Runs quietly in the background
- **No admin required**: Per-user installation, no UAC prompts
- **Auto-start**: Launches automatically with Windows
- **Sleep/wake aware**: Handles system sleep gracefully without firing missed alarms

## System Requirements

- Windows 7 SP1 or later
- .NET Framework 4.8 ([download](https://dotnet.microsoft.com/download/dotnet-framework/net48))
- Audio output device

## Installation

Download the latest `ZyntaSchoolBell-Setup.exe` from [Releases](https://github.com/sendtodilanka/ZyntaSchoolBell/releases) and run it. No admin rights needed.

## Building from Source

### Prerequisites
- Visual Studio 2019+ with .NET desktop development workload
- Or MSBuild + NuGet CLI

### Build
```bash
nuget restore src/ZyntaSchoolBell/ZyntaSchoolBell.csproj -PackagesDirectory packages
msbuild src/ZyntaSchoolBell/ZyntaSchoolBell.csproj /p:Configuration=Release
```

### Build Installer
Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php):
```bash
iscc installer/ZyntaSchoolBell.iss
```

## Audio Generation

To regenerate audio files using edge-tts:
```bash
pip install edge-tts
python tools/generate_audio.py
```

## Project Structure

```
ZyntaSchoolBell/
├── src/ZyntaSchoolBell/     # C# WinForms application
│   ├── Models/              # Data models (AlarmEntry, Profile, AppSettings)
│   ├── Services/            # Core services (AlarmEngine, AudioPlayer, etc.)
│   └── UI/                  # WinForms UI (MainForm, dialogs, tray)
├── audio/                   # Multilingual MP3 announcements
├── installer/               # Inno Setup script + default profiles
├── tools/                   # Audio generation script
└── docs/                    # Architecture and planning docs
```

## License

All rights reserved.
