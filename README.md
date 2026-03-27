# ZyntaSchoolBell

A lightweight Windows desktop application that plays pre-recorded multilingual audio announcements (Sinhala, Tamil, English) at scheduled times for school periods.

## Features

- **Multilingual announcements**: Plays Sinhala → English → Tamil audio sequences
- **Multiple schedule profiles**: Regular Day, Exam Day, Half Day, Saturday School, Special Day
- **System tray integration**: Runs quietly in the background
- **No admin rights required**: Per-user installation
- **Windows 7–11 compatible**: Built on .NET Framework 4.8
- **Auto-start**: Launches with Windows via HKCU registry

## Technical Stack

- **Language**: C# / WinForms (.NET Framework 4.8)
- **Audio**: NAudio 2.2.1
- **JSON**: Newtonsoft.Json 13.x
- **Installer**: Inno Setup 6 (per-user, no UAC)
- **Audio Generation**: Python edge-tts

## Building

### Prerequisites

- Visual Studio 2019+ with .NET Framework 4.8 targeting pack
- NuGet package manager
- (Optional) Inno Setup 6 for building the installer
- (Optional) Python 3.8+ with edge-tts for audio generation

### Build Steps

1. Clone the repository:
   ```
   git clone https://github.com/sendtodilanka/ZyntaSchoolBell.git
   cd ZyntaSchoolBell
   ```

2. Restore NuGet packages:
   ```
   nuget restore ZyntaSchoolBell.sln
   ```

3. Build with MSBuild:
   ```
   msbuild ZyntaSchoolBell.sln /p:Configuration=Release
   ```

4. (Optional) Build installer:
   ```
   iscc installer/ZyntaSchoolBell.iss
   ```

### Audio Generation

Generate all 45 MP3 files (15 audio keys × 3 languages):

```
pip install edge-tts
python tools/generate_audio.py
```

Audio keys: `opening_bell`, `period_1` through `period_8`, `interval`, `lunch_break`, `afternoon_bell`, `closing_bell`, `warning_bell`, `assembly`

Each alarm plays: **Sinhala → English → Tamil** (sequentially via NAudio).

## File Paths

| Path | Purpose |
|------|---------|
| `%LOCALAPPDATA%\ZyntaSchoolBell\` | Application + audio files |
| `%APPDATA%\ZyntaSchoolBell\profiles\` | Schedule profiles (JSON) |
| `%APPDATA%\ZyntaSchoolBell\settings.json` | App settings |
| `%APPDATA%\ZyntaSchoolBell\logs\` | Application logs |

## Project Structure

```
src/ZyntaSchoolBell/
├── Models/          Data models (AlarmEntry, Profile, AppSettings)
├── Services/        Core logic (ProfileService, AlarmEngine, AudioPlayer)
├── UI/              WinForms (MainForm, AddEditAlarmDialog, TrayManager)
└── Resources/       Icons and embedded resources

audio/               Multilingual MP3 announcements (15 categories × 3 languages)
installer/           Inno Setup script + default profiles
tools/               Audio generation script
docs/                Architecture docs and plans
```

## System Requirements

- Windows 7 SP1 or later
- .NET Framework 4.8 ([download](https://dotnet.microsoft.com/download/dotnet-framework/net48))
- Audio output device

## License

All rights reserved.
