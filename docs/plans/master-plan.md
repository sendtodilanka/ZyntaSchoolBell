# ZyntaSchoolBell — Master Implementation Plan

## Context

The user wants to build a lightweight Windows desktop school bell system that plays multilingual audio (Sinhala, Tamil, English) at scheduled times. The repo at `sendtodilanka/ZyntaSchoolBell` is empty. The user provided a detailed spec with a 4-phase plan. This master plan improves upon it by addressing architectural gaps, reordering phases for testability, adding CI/CD for automated setup.exe builds, and introducing risk mitigations.

## Key Improvements Over Original Plan

1. **Phase reordering**: Audio generation moves before UI (need audio to test playback)
2. **Phase 0 added**: Project scaffolding, solution setup, .gitignore, CI/CD pipeline
3. **CI/CD pipeline**: GitHub Actions workflow to build setup.exe on every push/tag
4. **Architecture**: Interface-based services, event-driven AlarmEngine, proper IDisposable
5. **Single instance**: Mutex-based enforcement to prevent duplicate launches
6. **Logging**: Structured file logging with rotation (no heavy frameworks)
7. **Git LFS**: For MP3 audio files to keep repo clean
8. **Graceful shutdown**: Proper disposal chain for NAudio, timers, tray icon
9. **JSON schema versioning**: Version field in settings/profiles for future migration

---

## Phase 0 — Project Scaffolding & CI/CD

### Deliverables
- Complete repository folder structure (all directories)
- `.gitignore` (Visual Studio standard + audio build artifacts)
- `.gitattributes` (Git LFS tracking for `*.mp3`)
- `.editorconfig` (consistent code style)
- `ZyntaSchoolBell.sln` solution file
- `src/ZyntaSchoolBell/ZyntaSchoolBell.csproj` targeting .NET Framework 4.8
- NuGet `packages.config` with NAudio 2.2.1 + Newtonsoft.Json 13.0.3
- `README.md` with project overview, build instructions, and architecture summary
- `docs/architecture.md` — system design document
- `.github/workflows/build.yml` — CI/CD pipeline
- Empty `Program.cs` with `Main()` entry point

### Repository Structure
```
ZyntaSchoolBell/
├── .github/
│   └── workflows/
│       └── build.yml
├── src/
│   └── ZyntaSchoolBell/
│       ├── ZyntaSchoolBell.csproj
│       ├── packages.config
│       ├── Properties/
│       │   └── AssemblyInfo.cs
│       ├── Program.cs
│       ├── Models/
│       ├── Services/
│       ├── UI/
│       └── Resources/
│           └── tray_icon.ico
├── audio/                          (Git LFS tracked)
│   ├── opening_bell/
│   ├── period_1/ through period_8/  (individual period announcements)
│   ├── interval/
│   ├── lunch_break/
│   ├── afternoon_bell/
│   ├── closing_bell/
│   ├── warning_bell/
│   └── assembly/
├── installer/
│   ├── ZyntaSchoolBell.iss
│   └── default_profiles/
├── tools/
│   └── generate_audio.py
├── docs/
│   ├── architecture.md
│   └── plans/
│       └── master-plan.md
├── .gitignore
├── .gitattributes
├── .editorconfig
└── README.md
```

### CI/CD Pipeline (`.github/workflows/build.yml`)

```yaml
name: Build & Package

on:
  push:
    branches: [main, develop]
    tags: ['v*']
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
        with:
          lfs: true

      - name: Setup MSBuild
        uses: microsoft/setup-msbuild@v2

      - name: Setup NuGet
        uses: NuGet/setup-nuget@v2

      - name: Restore NuGet packages
        run: nuget restore src/ZyntaSchoolBell/ZyntaSchoolBell.csproj -PackagesDirectory packages

      - name: Build (Release)
        run: msbuild src/ZyntaSchoolBell/ZyntaSchoolBell.csproj /p:Configuration=Release /p:OutputPath=../../build/Release

      - name: Install Inno Setup
        run: choco install innosetup --yes --no-progress

      - name: Compile Installer
        run: iscc installer/ZyntaSchoolBell.iss
        shell: cmd

      - name: Upload setup.exe artifact
        uses: actions/upload-artifact@v4
        with:
          name: ZyntaSchoolBell-Setup
          path: installer/Output/ZyntaSchoolBell-Setup.exe

  release:
    needs: build
    if: startsWith(github.ref, 'refs/tags/v')
    runs-on: windows-latest
    permissions:
      contents: write
    steps:
      - uses: actions/checkout@v4
        with:
          lfs: true

      - name: Setup MSBuild
        uses: microsoft/setup-msbuild@v2

      - name: Setup NuGet
        uses: NuGet/setup-nuget@v2

      - name: Restore NuGet packages
        run: nuget restore src/ZyntaSchoolBell/ZyntaSchoolBell.csproj -PackagesDirectory packages

      - name: Build (Release)
        run: msbuild src/ZyntaSchoolBell/ZyntaSchoolBell.csproj /p:Configuration=Release /p:OutputPath=../../build/Release

      - name: Install Inno Setup
        run: choco install innosetup --yes --no-progress

      - name: Compile Installer
        run: iscc installer/ZyntaSchoolBell.iss
        shell: cmd

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          files: installer/Output/ZyntaSchoolBell-Setup.exe
          generate_release_notes: true
```

**How it works:**
- Every push to `main`/`develop` or PR builds the app and uploads `setup.exe` as a downloadable artifact
- Pushing a tag like `v1.0.0` triggers the release job, which creates a GitHub Release with setup.exe attached
- Uses `windows-latest` runner (has MSBuild, NuGet, and can install Inno Setup via Chocolatey)
- Git LFS checkout ensures audio files are available for packaging

### Verification
- Push scaffolding commit → CI workflow runs → green build (even if just empty project compiles)
- Confirm NuGet restore + MSBuild succeed on `windows-latest`

---

## Phase 1 — Core Engine (No UI)

### Architecture Design

```
Program.cs
  └─ creates AppContext (IDisposable, manages lifecycle)
       ├─ ProfileService : IProfileService
       ├─ AudioPlayer : IAudioPlayer, IDisposable
       ├─ AlarmEngine : IDisposable
       │     ├─ raises AlarmFired event
       │     ├─ raises MidnightReset event
       │     └─ uses System.Threading.Timer (1s tick)
       └─ SleepWakeHandler : IDisposable
             └─ raises SystemResumed event
```

**Key design decisions:**
- **Event-driven**: `AlarmEngine` raises `AlarmFired(AlarmEntry)` event — it does NOT directly call `AudioPlayer`. The UI layer (Phase 3) wires them together. This keeps engine testable.
- **Interface-based services**: `IAudioPlayer`, `IProfileService` for testability and separation
- **Constructor injection**: Services receive dependencies via constructors (no DI container needed)
- **IDisposable chain**: `Program.cs` → disposes all services on shutdown
- **Thread safety**: `AlarmEngine` timer callback uses `lock` for `firedTodaySet`; any UI updates go through `Control.Invoke()`

### Files to Create

#### Models (`src/ZyntaSchoolBell/Models/`)

**AlarmEntry.cs**
```csharp
public class AlarmEntry
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("time")]
    public string Time { get; set; }  // "HH:mm" 24h

    [JsonProperty("label")]
    public string Label { get; set; }

    [JsonProperty("audioKey")]
    public string AudioKey { get; set; }

    [JsonProperty("enabled")]
    public bool Enabled { get; set; } = true;
}
```

**Profile.cs**
```csharp
public class Profile
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("version")]
    public int Version { get; set; } = 1;  // Schema version for migration

    [JsonProperty("alarms")]
    public List<AlarmEntry> Alarms { get; set; } = new List<AlarmEntry>();
}
```

**AppSettings.cs**
```csharp
public class AppSettings
{
    [JsonProperty("activeProfileId")]
    public string ActiveProfileId { get; set; } = "regular_day";

    [JsonProperty("volume")]
    public int Volume { get; set; } = 90;

    [JsonProperty("minimizeToTray")]
    public bool MinimizeToTray { get; set; } = true;

    [JsonProperty("mutedToday")]
    public bool MutedToday { get; set; }

    [JsonProperty("mutedDate")]
    public string MutedDate { get; set; }

    [JsonProperty("version")]
    public int Version { get; set; } = 1;  // Schema version
}
```

#### Services (`src/ZyntaSchoolBell/Services/`)

**ProfileService.cs** — JSON load/save for profiles + settings
- Paths: `%APPDATA%\ZyntaSchoolBell\profiles\` and `%APPDATA%\ZyntaSchoolBell\settings.json`
- Auto-create directories on first run
- UTF-8 encoding without BOM for Sinhala/Tamil compatibility
- Atomic writes: write to `.tmp` file, then `File.Move` to overwrite (prevents corruption on crash)
- `LoadAllProfiles()`, `SaveProfile(Profile)`, `DeleteProfile(string id)`, `LoadSettings()`, `SaveSettings(AppSettings)`

**AudioPlayer.cs** — NAudio sequential playback
- Implements `IAudioPlayer` and `IDisposable`
- Uses `WaveOutEvent` (compatible with Windows 7+, no UI thread dependency)
- Sequential chain: si.mp3 → en.mp3 → ta.mp3 via `PlaybackStopped` event (Sinhala first, then English, then Tamil)
- `CancelCurrent()` method to stop playback (for overlapping alarm edge case)
- `SetVolume(int percent)` maps 0-100 to 0.0-1.0 float
- Missing file: log error, skip to next language, continue chain
- All playback in try/catch — never crashes the app
- Audio path resolution: `Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio", audioKey, lang + ".mp3")`

**AlarmEngine.cs** — Timer + scheduling logic
- `System.Threading.Timer` at 1000ms interval
- `HashSet<string> firedTodaySet` (alarm IDs fired today) with `lock` for thread safety
- `event EventHandler<AlarmFiredEventArgs> AlarmFired`
- `event EventHandler MidnightReset`
- On tick: compare `DateTime.Now.ToString("HH:mm")` against enabled alarms
- Midnight detection: if stored date != today → clear `firedTodaySet`, raise `MidnightReset`
- On startup: mark all past alarms as already fired (don't replay)
- `IDisposable`: dispose timer

**SleepWakeHandler.cs** — Power state monitoring
- Hooks `SystemEvents.PowerModeChanged`
- On Resume: log wake event, do NOT fire missed alarms
- Clear alarms from firedTodaySet whose times fall within the sleep window (they were missed, don't re-fire)
- `IDisposable`: unhook event

### Single Instance Enforcement
In `Program.cs`, use a named `Mutex`:
```csharp
using (var mutex = new Mutex(true, "ZyntaSchoolBell_SingleInstance", out bool created))
{
    if (!created)
    {
        // Another instance running — bring it to front via WM_SHOWWINDOW or just exit
        return;
    }
    Application.Run(new MainForm());
}
```

### Logging
Simple file logger — no external dependencies:
- Write to `%APPDATA%\ZyntaSchoolBell\logs\app.log`
- Format: `[yyyy-MM-dd HH:mm:ss] [LEVEL] message`
- Rotate when file exceeds 1MB (rename to `app.log.1`, keep max 3 files)
- Levels: INFO, WARN, ERROR

### Verification
- Models serialize/deserialize correctly (write small console test in Program.cs)
- ProfileService reads/writes JSON files to correct paths
- AudioPlayer plays a test MP3 (create a tiny silent MP3 for testing)
- AlarmEngine fires events at correct times (accelerated test with short intervals)
- Commit: `"Phase 1: core engine — models, services, alarm engine, audio player"`

---

## Phase 2 — Audio Generation

> **Moved before UI** — we need real audio files to properly test the player and UI.

### `tools/generate_audio.py`

Python script using `gtts`:
- Generates audio for 15 audio keys x 3 languages (45 files total)
- Audio keys: `opening_bell`, `period_1` through `period_8`, `interval`, `lunch_break`, `afternoon_bell`, `closing_bell`, `warning_bell`, `assembly`
- Each period has unique ordinal announcements:
  - Sinhala: පළමු/දෙවන/තෙවන/සිව්වන/පස්වන/හයවන/හත්වන/අටවන කාලච්ඡේදය ආරම්භ වී ඇත.
  - English: The first/second/.../eighth period has begun.
  - Tamil: முதல்/இரண்டாம்/.../எட்டாம் பாடவேளை தொடங்கியது.
- Voice mapping:
  - Sinhala: `si-LK-SameeraNeural` (primary), `si-LK-ThiliniNeural` (fallback)
  - Tamil: `ta-LK-KumarNeural` (primary), `ta-IN-PallaviNeural` (fallback)
  - English: `en-US-AriaNeural`
- Output: `audio/{audioKey}/{lang}.mp3`
- Generation order per key: si → en → ta (matches playback order)
- Progress reporting per file
- Error handling per file (don't stop batch on single failure)

### Audio Files (Git LFS)
Configure `.gitattributes`:
```
audio/**/*.mp3 filter=lfs diff=lfs merge=lfs -text
```

### Verification
- Run script, verify 45 MP3 files generated (15 keys x 3 languages)
- Each file plays correctly and has audible speech
- Test AudioPlayer service plays the chain (si → en → ta) without gaps
- Commit: `"Phase 2: audio generation script and MP3 files"`

---

## Phase 3 — User Interface

### MainForm (`src/ZyntaSchoolBell/UI/MainForm.cs`)

**Layout (top to bottom):**
1. **Top toolbar**: Profile ComboBox + [New] [Rename] [Delete] buttons
2. **Status strip**: Green/red circle indicator + "ACTIVE"/"PAUSED" + "Next bell: HH:MM AM/PM"
3. **Center**: DataGridView with columns: Time | Bell Name | Audio Key | [Test] [Edit] [Delete]
4. **Bottom bar**: Volume TrackBar (0-100) + label + [Mute Today] CheckBox + [+ Add Bell] Button
5. **Close button behavior**: Minimize to tray, NOT exit

**Key behaviors:**
- Profile dropdown change → save current, load new, update AlarmEngine
- Status bar updates via timer (1s) showing next upcoming alarm
- DataGridView sorted by time ascending
- Volume slider → immediate `AudioPlayer.SetVolume()` + save settings

### AddEditAlarmDialog (`src/ZyntaSchoolBell/UI/AddEditAlarmDialog.cs`)

- Modal dialog, owner = MainForm
- Controls: DateTimePicker (time only, HH:mm), TextBox (label), ComboBox (audio key with friendly names), [Test Audio], [Save], [Cancel]
- On save: check duplicate time in profile → "Replace existing alarm at HH:MM?" confirmation
- Audio key dropdown items: Opening Bell, Period 1-8, Interval, Lunch Break, Afternoon Bell, Closing Bell, Warning Bell, Assembly

### TrayManager (`src/ZyntaSchoolBell/UI/TrayManager.cs`)

- `NotifyIcon` with bell icon
- Tooltip: "ZyntaSchoolBell — Next: HH:MM AM/PM" or "Muted Today"
- Context menu:
  - Active Schedule: {name} (disabled label)
  - Next Bell: {time} - {label} (disabled label)
  - Separator
  - Mute Today (checkable)
  - Open Window
  - Separator
  - Exit
- Double-click tray icon → show/restore MainForm
- "Exit" → proper disposal chain → `Application.Exit()`

### Edge Cases Handled
1. Duplicate alarm time → Replace warning dialog
2. Missing audio file → Tray balloon + log, skip language
3. Audio device unavailable → Catch, log, balloon notification
4. Sleep/wake → Skip missed alarms (SleepWakeHandler)
5. System clock change → Re-evaluate firedTodaySet
6. Delete active profile → Prompt, switch to first available or create empty
7. Zero alarms in profile → Status bar warning: "No alarms configured"
8. Overlapping audio → `AudioPlayer.CancelCurrent()` before starting new
9. MutedToday auto-reset → Check `mutedDate != today` on tick and startup
10. App starts after alarm time → Mark past alarms as fired on startup

### Additional UI Features
- **About dialog**: Version info from AssemblyInfo.cs, app name, link to repo
- **Keyboard shortcuts**: Ctrl+N (new alarm), Delete (remove selected), Ctrl+M (mute toggle)
- **Test All Audio** button in a diagnostic menu (helps verify installation)

### Verification
- All forms render correctly, no layout clipping
- Profile CRUD works (create, rename, switch, delete)
- Add/Edit alarm with duplicate detection
- Tray icon shows, context menu works, double-click restores
- Volume slider controls playback volume in real-time
- Mute Today toggles and persists
- Close button minimizes to tray, Exit properly shuts down
- Commit: `"Phase 3: complete UI — MainForm, dialogs, tray manager"`

---

## Phase 4 — Installer & Default Profiles

### Default Profile JSON Files (`installer/default_profiles/`)

5 files as specified: `regular_day.json`, `exam_day.json`, `half_day.json`, `saturday_school.json`, `special_day.json`

Each with the alarm schedules from the spec, properly formatted with UUIDs, time strings, labels, and audio keys.

### Inno Setup Script (`installer/ZyntaSchoolBell.iss`)

Key configuration:
```ini
[Setup]
AppName=ZyntaSchoolBell
AppVersion=1.0.0
PrivilegesRequired=lowest
DefaultDirName={localappdata}\ZyntaSchoolBell
DefaultGroupName=ZyntaSchoolBell
OutputDir=Output
OutputBaseFilename=ZyntaSchoolBell-Setup
```

**Installer behavior:**
- Per-user install to `{localappdata}\ZyntaSchoolBell\` (no UAC)
- Copy audio files to `{localappdata}\ZyntaSchoolBell\audio\`
- Copy default profiles to `{userappdata}\ZyntaSchoolBell\profiles\` — only if NOT already existing (preserves user edits on reinstall)
- HKCU Run key for auto-start: `"ZyntaSchoolBell" = "{app}\ZyntaSchoolBell.exe"`
- Desktop shortcut + Start Menu entry
- Uninstaller: removes app + audio + registry key, but preserves `%APPDATA%\ZyntaSchoolBell\` (user profiles & settings)

### Verification
- ISS script compiles without errors
- Installer runs on clean Windows without admin
- Default profiles land in correct location
- Auto-start registry key created
- Uninstall removes app but keeps user data
- CI/CD builds and uploads setup.exe artifact successfully
- Tag push creates GitHub Release with setup.exe
- Commit: `"Phase 4: installer, default profiles, CI/CD verified"`

---

## Phase 5 — Polish & Release

### Final Tasks
- Version bump in AssemblyInfo.cs and ISS script
- Test full flow: install → auto-start → alarms fire → audio plays → minimize to tray → uninstall
- Verify CI/CD produces valid setup.exe from clean checkout
- Tag `v1.0.0` → triggers release workflow → setup.exe on GitHub Releases
- Update README with download link and screenshots

---

## Risk Register

| Risk | Impact | Mitigation |
|------|--------|------------|
| NAudio on Windows 7 | Audio won't play | Use `WaveOutEvent` backend (most compatible); test on Win7 VM |
| .NET 4.8 not on Win7 | App won't launch | Document prerequisite; link to .NET 4.8 offline installer in README |
| Timer drift over hours | Missed alarms | 1s tick is sufficient — compare HH:mm strings, not exact timestamps |
| NAudio memory leak | App bloats over time | Strict `IDisposable`; dispose `WaveOutEvent` + `Mp3FileReader` after each playback |
| MP3 files bloat git | Slow clones | Git LFS for `audio/**/*.mp3` |
| gtts API down | Can't generate audio | Pre-generated audio committed to repo; script is for regeneration only |
| Sinhala/Tamil encoding | Corrupted text in JSON | UTF-8 without BOM; `JsonConvert` handles Unicode natively |
| Dual launch | Conflicting timers | Named Mutex single-instance check |
| Inno Setup not on CI | Build fails | Install via Chocolatey in GitHub Actions workflow |
| Profile JSON corruption | Lost schedules | Atomic writes (write .tmp, then rename); keep one backup copy |

---

## Critical Files Summary

| File | Purpose |
|------|--------|
| `.github/workflows/build.yml` | CI/CD: build + Inno Setup + release |
| `src/ZyntaSchoolBell/ZyntaSchoolBell.csproj` | Project targeting .NET Framework 4.8 |
| `src/ZyntaSchoolBell/Program.cs` | Entry point, single-instance, lifecycle |
| `src/ZyntaSchoolBell/Models/*.cs` | AlarmEntry, Profile, AppSettings |
| `src/ZyntaSchoolBell/Services/ProfileService.cs` | JSON CRUD for profiles & settings |
| `src/ZyntaSchoolBell/Services/AudioPlayer.cs` | NAudio sequential playback |
| `src/ZyntaSchoolBell/Services/AlarmEngine.cs` | Timer + scheduling + events |
| `src/ZyntaSchoolBell/Services/SleepWakeHandler.cs` | Power state monitoring |
| `src/ZyntaSchoolBell/UI/MainForm.cs` | Main window |
| `src/ZyntaSchoolBell/UI/AddEditAlarmDialog.cs` | Alarm editor dialog |
| `src/ZyntaSchoolBell/UI/TrayManager.cs` | System tray integration |
| `installer/ZyntaSchoolBell.iss` | Inno Setup script |
| `installer/default_profiles/*.json` | 5 default schedule profiles |
| `tools/generate_audio.py` | gtts audio generation |

---

## Commit Strategy

| Commit | Content |
|--------|---------|
| 1 | Phase 0: project scaffolding, CI/CD pipeline, solution setup |
| 2 | Phase 1: core engine — models, services, alarm engine, audio player |
| 3 | Phase 2: audio generation script and MP3 files |
| 4 | Phase 3: complete UI — MainForm, dialogs, tray manager |
| 5 | Phase 4: installer, default profiles |
| 6 | Phase 5: polish, README, tag v1.0.0 |
