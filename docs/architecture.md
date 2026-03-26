# ZyntaSchoolBell — Architecture

## System Overview

```
┌─────────────────────────────────────────────────┐
│                   Program.cs                     │
│            (Entry point + Mutex)                  │
└──────────────────────┬──────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────┐
│                   MainForm                       │
│  ┌─────────┐ ┌──────────┐ ┌──────────────────┐  │
│  │ Profile  │ │  Alarm   │ │   Volume/Mute    │  │
│  │ Selector │ │   Grid   │ │    Controls      │  │
│  └─────────┘ └──────────┘ └──────────────────┘  │
└──────────────────────┬──────────────────────────┘
                       │ events
┌──────────────────────▼──────────────────────────┐
│                 Service Layer                    │
│  ┌──────────────┐ ┌────────────┐ ┌───────────┐  │
│  │ AlarmEngine   │ │AudioPlayer │ │ Profile   │  │
│  │ (Timer+Events)│ │ (NAudio)   │ │ Service   │  │
│  └──────┬───────┘ └──────┬─────┘ └─────┬─────┘  │
│         │  AlarmFired     │ Play()      │ JSON   │
│         │  event          │             │ I/O    │
│  ┌──────▼───────┐        │             │        │
│  │SleepWake     │        │             │        │
│  │Handler       │        │             │        │
│  └──────────────┘        │             │        │
└──────────────────────────┼─────────────┼────────┘
                           │             │
              ┌────────────▼─┐    ┌──────▼────────┐
              │ audio/*.mp3  │    │ %APPDATA%/    │
              │ (LOCALAPPDATA)│    │ profiles/     │
              └──────────────┘    │ settings.json │
                                  └───────────────┘
```

## Component Design

### AlarmEngine
- `System.Threading.Timer` fires every 1000ms
- Compares `DateTime.Now.ToString("HH:mm")` against enabled alarms
- Maintains `HashSet<string> firedTodaySet` (thread-safe with `lock`)
- Raises `AlarmFired` event — does NOT directly call AudioPlayer
- At midnight: clears firedTodaySet, raises MidnightReset event
- On startup: marks past alarms as already fired

### AudioPlayer
- Implements `IAudioPlayer` and `IDisposable`
- Uses `WaveOutEvent` for Windows 7+ compatibility
- Plays sequential chain: si.mp3 → en.mp3 → ta.mp3 (Sinhala first, then English, then Tamil)
- Chains via `PlaybackStopped` event (no Thread.Sleep)
- `CancelCurrent()` stops playback for overlapping alarms
- Missing files: logs error, skips language, continues chain

### ProfileService
- CRUD operations for Profile and AppSettings JSON files
- Atomic writes: write to .tmp, then File.Move
- UTF-8 without BOM for Sinhala/Tamil compatibility
- Auto-creates directories on first run

### SleepWakeHandler
- Hooks `SystemEvents.PowerModeChanged`
- On resume: logs wake, does NOT fire missed alarms
- Clears sleep-window alarms from firedTodaySet

### TrayManager
- NotifyIcon with bell icon
- Tooltip shows next alarm time or "Muted Today"
- Context menu: schedule info, mute toggle, open/exit

## Threading Model

- Timer callbacks run on ThreadPool threads
- UI updates marshalled via `Control.Invoke()`
- `firedTodaySet` protected by `lock` object
- NAudio playback on background thread (WaveOutEvent)

## Data Flow

1. Timer tick → check alarms → raise AlarmFired event
2. MainForm handles event → calls AudioPlayer.Play(audioKey)
3. AudioPlayer chains si→en→ta via PlaybackStopped
4. Profile changes → ProfileService.Save() → AlarmEngine.Reload()

## File Paths

| Path | Content |
|------|---------|
| `{exe dir}/audio/{key}/{lang}.mp3` | Audio files |
| `%APPDATA%/ZyntaSchoolBell/profiles/{id}.json` | Schedule profiles |
| `%APPDATA%/ZyntaSchoolBell/settings.json` | App settings |
| `%APPDATA%/ZyntaSchoolBell/logs/app.log` | Application log |
