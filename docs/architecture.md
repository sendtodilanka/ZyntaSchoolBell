# ZyntaSchoolBell — Architecture

## System Overview

ZyntaSchoolBell is a Windows desktop application that plays multilingual audio announcements (Sinhala, Tamil, English) at scheduled times for school periods. It runs as a system tray application on Windows 7 through Windows 11.

## Component Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Program.cs                            │
│              (Entry point, single-instance Mutex)        │
└──────────────────┬──────────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────────┐
│                    MainForm (UI)                         │
│  ┌──────────────┐ ┌────────────────┐ ┌───────────────┐  │
│  │ Profile       │ │ Alarm Grid     │ │ TrayManager   │  │
│  │ Selector      │ │ (DataGridView) │ │ (NotifyIcon)  │  │
│  └──────────────┘ └────────────────┘ └───────────────┘  │
│  ┌──────────────┐ ┌────────────────┐                    │
│  │ Volume Ctrl   │ │ Status Bar     │                    │
│  └──────────────┘ └────────────────┘                    │
└──────────────────┬──────────────────────────────────────┘
                   │ events
┌──────────────────▼──────────────────────────────────────┐
│                    Services Layer                        │
│  ┌──────────────┐ ┌────────────────┐ ┌───────────────┐  │
│  │ProfileService│ │  AlarmEngine   │ │  AudioPlayer  │  │
│  │ (JSON CRUD)  │ │ (Timer 1s tick)│ │ (NAudio chain)│  │
│  └──────────────┘ └────────────────┘ └───────────────┘  │
│  ┌──────────────┐ ┌────────────────┐                    │
│  │SleepWakeHndlr│ │    Logger      │                    │
│  └──────────────┘ └────────────────┘                    │
└─────────────────────────────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────────┐
│                    Data Layer                            │
│  %APPDATA%/ZyntaSchoolBell/                              │
│    ├── profiles/*.json                                   │
│    ├── settings.json                                     │
│    └── logs/app.log                                      │
│  %LOCALAPPDATA%/ZyntaSchoolBell/                         │
│    └── audio/{audioKey}/{si|ta|en}.mp3                   │
└─────────────────────────────────────────────────────────┘
```

## Key Design Decisions

1. **Event-driven AlarmEngine**: Raises `AlarmFired` events rather than directly calling AudioPlayer, enabling loose coupling and testability.
2. **WaveOutEvent backend**: NAudio's most compatible backend, works on Windows 7+ without UI thread dependency.
3. **Sequential audio chain**: si.mp3 → ta.mp3 → en.mp3 chained via `PlaybackStopped` events (no Thread.Sleep).
4. **Per-user installation**: All files in LOCALAPPDATA/APPDATA, no admin rights required.
5. **Atomic file writes**: Profile/settings saved to .tmp then moved, preventing corruption on crash.
6. **Single-instance Mutex**: Prevents duplicate launches that would cause conflicting timers.

## Technology Stack

| Component | Technology |
|-----------|------------|
| Language | C# (.NET Framework 4.8) |
| UI | WinForms |
| Audio | NAudio 2.2.1 |
| JSON | Newtonsoft.Json 13.x |
| Scheduling | System.Threading.Timer |
| Installer | Inno Setup 6 |
| Audio Generation | Python edge-tts |
| CI/CD | GitHub Actions (MSBuild + Inno Setup) |
