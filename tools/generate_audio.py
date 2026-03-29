#!/usr/bin/env python3
"""
ZyntaSchoolBell Audio Generator v2.0

Generates multilingual audio announcements (Sinhala, Tamil, English)
for all school bell types using one of three TTS engines:

  --engine edge     Microsoft Edge TTS (default, best quality, needs internet)
  --engine gtts     Google Text-to-Speech (good quality, needs internet)
  --engine espeak   eSpeak NG (offline/native, no internet required)

Usage:
    # Default (Edge TTS):
    pip install edge-tts
    python generate_audio.py

    # Google TTS:
    pip install gtts
    python generate_audio.py --engine gtts

    # Offline / Native (eSpeak NG):
    # Install: sudo apt install espeak-ng ffmpeg   (Linux)
    #          choco install espeak ffmpeg          (Windows via Chocolatey)
    python generate_audio.py --engine espeak

Custom Native Recordings
------------------------
To replace any auto-generated file with a human recording, simply put your
own MP3 file at the correct path:

    audio/<audio_key>/<lang>.mp3

Example: audio/opening_bell/si.mp3  ← replace with native Sinhala recording

Run with --skip-existing to keep already-recorded files intact:
    python generate_audio.py --skip-existing
"""

import argparse
import asyncio
import os
import subprocess
import sys
import tempfile

# ---------------------------------------------------------------------------
# Output directory (relative to repo root)
# ---------------------------------------------------------------------------
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
AUDIO_DIR = os.path.join(SCRIPT_DIR, "..", "audio")

# ---------------------------------------------------------------------------
# Voice configuration
# ---------------------------------------------------------------------------
VOICES = {
    "edge": {
        "si": {"primary": "si-LK-SameeraNeural", "fallback": "si-LK-ThiliniNeural"},
        "ta": {"primary": "ta-LK-KumarNeural",   "fallback": "ta-IN-PallaviNeural"},
        "en": {"primary": "en-US-AriaNeural",     "fallback": "en-US-AriaNeural"},
    },
    "espeak": {
        # eSpeak NG language codes
        "si": "si",
        "ta": "ta",
        "en": "en",
    },
    "gtts": {
        # Google TTS language codes
        "si": "si",
        "ta": "ta",
        "en": "en",
    },
}

# ---------------------------------------------------------------------------
# Announcements text
# ---------------------------------------------------------------------------
ANNOUNCEMENTS = {
    "opening_bell": {
        "si": "පාසල ආරම්භ වෙයි. සිසුන් රැස්වීම් ස්ථානයට එන්න.",
        "ta": "பள்ளி தொடங்குகிறது. மாணவர்கள் சேர வேண்டும்.",
        "en": "School is beginning. Students please assemble.",
    },
    "period_1": {
        "si": "පළමු කාලච්ඡේදය ආරම්භ වී ඇත.",
        "en": "The first period has begun.",
        "ta": "முதல் பாடவேளை தொடங்கியது.",
    },
    "period_2": {
        "si": "දෙවන කාලච්ඡේදය ආරම්භ වී ඇත.",
        "en": "The second period has begun.",
        "ta": "இரண்டாம் பாடவேளை தொடங்கியது.",
    },
    "period_3": {
        "si": "තෙවන කාලච්ඡේදය ආරම්භ වී ඇත.",
        "en": "The third period has begun.",
        "ta": "மூன்றாம் பாடவேளை தொடங்கியது.",
    },
    "period_4": {
        "si": "සිව්වන කාලච්ඡේදය ආරම්භ වී ඇත.",
        "en": "The fourth period has begun.",
        "ta": "நான்காம் பாடவேளை தொடங்கியது.",
    },
    "period_5": {
        "si": "පස්වන කාලච්ඡේදය ආරම්භ වී ඇත.",
        "en": "The fifth period has begun.",
        "ta": "ஐந்தாம் பாடவேளை தொடங்கியது.",
    },
    "period_6": {
        "si": "හයවන කාලච්ඡේදය ආරම්භ වී ඇත.",
        "en": "The sixth period has begun.",
        "ta": "ஆறாம் பாடவேளை தொடங்கியது.",
    },
    "period_7": {
        "si": "හත්වන කාලච්ඡේදය ආරම්භ වී ඇත.",
        "en": "The seventh period has begun.",
        "ta": "ஏழாம் பாடவேளை தொடங்கியது.",
    },
    "period_8": {
        "si": "අටවන කාලච්ඡේදය ආරම්භ වී ඇත.",
        "en": "The eighth period has begun.",
        "ta": "எட்டாம் பாடவேளை தொடங்கியது.",
    },
    "interval": {
        "si": "විවේක කාලය. සිසුන්ට කෙටි විවේකයක් ගත හැක.",
        "ta": "இடைவேளை நேரம். மாணவர்கள் ஓய்வு எடுக்கலாம்.",
        "en": "Interval time. Students may take a short break.",
    },
    "lunch_break": {
        "si": "දිවා ආහාර විවේකය. සිසුන් ආපනශාලාවට යන්න.",
        "ta": "மதிய உணவு இடைவேளை. மாணவர்கள் உணவகத்திற்கு செல்லுங்கள்.",
        "en": "Lunch break. Students please proceed to the canteen.",
    },
    "afternoon_bell": {
        "si": "දහවල් සැසිය ආරම්භ වෙයි.",
        "ta": "பிற்பகல் அமர்வு தொடங்குகிறது.",
        "en": "The afternoon session is beginning.",
    },
    "closing_bell": {
        "si": "අද සඳහා පාසල නිමා වී ඇත. සිසුන් පිළිවෙලට පිටව යන්න.",
        "ta": "இன்றைக்கு பள்ளி முடிந்தது. மாணவர்கள் ஒழுங்காக வெளியேறுங்கள்.",
        "en": "School is over for today. Students please leave in an orderly manner.",
    },
    "warning_bell": {
        "si": "අනතුරු ඇඟවීමේ සීනුව. කරුණාකර ඔබේ පන්ති කාමරවලට නැවත යන්න.",
        "ta": "எச்சரிக்கை மணி. தயவுசெய்து வகுப்பறைகளுக்குத் திரும்புங்கள்.",
        "en": "Warning bell. Please return to your classrooms.",
    },
    "assembly": {
        "si": "රැස්වීම් කාලය. සිසුන් රැස්වීම් පිටියට යන්න.",
        "ta": "சட்டசபை நேரம். மாணவர்கள் சட்டசபை மைதானத்திற்கு செல்லுங்கள்.",
        "en": "Assembly time. All students please proceed to the assembly ground.",
    },
}


# ---------------------------------------------------------------------------
# Engine: Edge TTS (async, best quality)
# ---------------------------------------------------------------------------
async def generate_edge(audio_key, lang, text, voice_config, output_file):
    try:
        import edge_tts
    except ImportError:
        print("ERROR: edge-tts not installed. Run: pip install edge-tts")
        sys.exit(1)

    voice = voice_config["primary"]
    try:
        communicate = edge_tts.Communicate(text, voice)
        await communicate.save(output_file)
        print(f"  [OK] {audio_key}/{lang}.mp3 (edge voice: {voice})")
        return True
    except Exception as e:
        print(f"  [WARN] Primary voice failed for {audio_key}/{lang}: {e}")
        fallback = voice_config["fallback"]
        if fallback != voice:
            try:
                communicate = edge_tts.Communicate(text, fallback)
                await communicate.save(output_file)
                print(f"  [OK] {audio_key}/{lang}.mp3 (edge fallback: {fallback})")
                return True
            except Exception as e2:
                print(f"  [ERROR] Fallback also failed for {audio_key}/{lang}: {e2}")
    return False


# ---------------------------------------------------------------------------
# Engine: Google TTS (gTTS)
# ---------------------------------------------------------------------------
def generate_gtts(audio_key, lang, text, lang_code, output_file):
    try:
        from gtts import gTTS
    except ImportError:
        print("ERROR: gtts not installed. Run: pip install gtts")
        sys.exit(1)

    try:
        tts = gTTS(text=text, lang=lang_code, slow=False)
        tts.save(output_file)
        print(f"  [OK] {audio_key}/{lang}.mp3 (gtts lang: {lang_code})")
        return True
    except Exception as e:
        print(f"  [ERROR] gTTS failed for {audio_key}/{lang}: {e}")
        return False


# ---------------------------------------------------------------------------
# Engine: eSpeak NG (offline/native)
# ---------------------------------------------------------------------------
def generate_espeak(audio_key, lang, text, lang_code, output_file):
    """Generate audio using eSpeak NG + ffmpeg (WAV → MP3)."""
    # Check dependencies
    for cmd in ("espeak-ng", "ffmpeg"):
        if subprocess.run(
            ["which", cmd] if os.name != "nt" else ["where", cmd],
            capture_output=True
        ).returncode != 0:
            print(f"ERROR: '{cmd}' not found.")
            if cmd == "espeak-ng":
                print("  Install: sudo apt install espeak-ng   (Debian/Ubuntu)")
                print("           choco install espeak          (Windows)")
            else:
                print("  Install: sudo apt install ffmpeg       (Debian/Ubuntu)")
                print("           choco install ffmpeg          (Windows)")
            return False

    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as tmp:
        wav_path = tmp.name

    try:
        # eSpeak NG → WAV
        result = subprocess.run(
            ["espeak-ng", "-v", lang_code, "-w", wav_path, text],
            capture_output=True, text=True
        )
        if result.returncode != 0:
            print(f"  [ERROR] espeak-ng failed for {audio_key}/{lang}: {result.stderr}")
            return False

        # ffmpeg: WAV → MP3
        result = subprocess.run(
            ["ffmpeg", "-y", "-i", wav_path, "-codec:a", "libmp3lame",
             "-qscale:a", "4", output_file],
            capture_output=True, text=True
        )
        if result.returncode != 0:
            print(f"  [ERROR] ffmpeg failed for {audio_key}/{lang}: {result.stderr}")
            return False

        print(f"  [OK] {audio_key}/{lang}.mp3 (espeak lang: {lang_code})")
        return True
    finally:
        if os.path.exists(wav_path):
            os.unlink(wav_path)


# ---------------------------------------------------------------------------
# Dispatcher
# ---------------------------------------------------------------------------
async def generate_one(engine, audio_key, lang, text, output_file):
    if engine == "edge":
        voice_config = VOICES["edge"][lang]
        return await generate_edge(audio_key, lang, text, voice_config, output_file)
    elif engine == "gtts":
        lang_code = VOICES["gtts"][lang]
        return generate_gtts(audio_key, lang, text, lang_code, output_file)
    elif engine == "espeak":
        lang_code = VOICES["espeak"][lang]
        return generate_espeak(audio_key, lang, text, lang_code, output_file)
    else:
        print(f"ERROR: Unknown engine '{engine}'")
        return False


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
async def main():
    parser = argparse.ArgumentParser(
        description="ZyntaSchoolBell Audio Generator v2.0"
    )
    parser.add_argument(
        "--engine",
        choices=["edge", "gtts", "espeak"],
        default="edge",
        help=(
            "TTS engine to use:\n"
            "  edge   - Microsoft Edge TTS (default, best quality, needs internet)\n"
            "  gtts   - Google Text-to-Speech (good quality, needs internet)\n"
            "  espeak - eSpeak NG (offline/native, no internet required)"
        ),
    )
    parser.add_argument(
        "--lang",
        choices=["si", "en", "ta", "all"],
        default="all",
        help="Generate only one language (default: all)",
    )
    parser.add_argument(
        "--skip-existing",
        action="store_true",
        help="Skip files that already exist (preserves custom/native recordings)",
    )
    args = parser.parse_args()

    langs = ["si", "en", "ta"] if args.lang == "all" else [args.lang]

    print("ZyntaSchoolBell Audio Generator v2.0")
    print("=" * 40)
    print(f"Engine : {args.engine}")
    print(f"Languages: {', '.join(langs)}")
    print(f"Output : {os.path.abspath(AUDIO_DIR)}")
    print()

    total = success = failed = skipped = 0

    for audio_key, texts in ANNOUNCEMENTS.items():
        print(f"\nGenerating: {audio_key}")
        for lang in langs:
            total += 1
            output_dir = os.path.join(AUDIO_DIR, audio_key)
            os.makedirs(output_dir, exist_ok=True)
            output_file = os.path.join(output_dir, f"{lang}.mp3")

            if args.skip_existing and os.path.exists(output_file):
                print(f"  [SKIP] {audio_key}/{lang}.mp3 (already exists)")
                skipped += 1
                success += 1
                continue

            text = texts[lang]
            ok = await generate_one(args.engine, audio_key, lang, text, output_file)
            if ok:
                success += 1
            else:
                failed += 1

    print(f"\n{'=' * 40}")
    print(f"Complete : {success}/{total} files generated successfully")
    if skipped:
        print(f"Skipped  : {skipped} files (existing, preserved)")
    if failed > 0:
        print(f"Failed   : {failed} files")
        sys.exit(1)


if __name__ == "__main__":
    asyncio.run(main())
