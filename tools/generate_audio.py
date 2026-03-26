#!/usr/bin/env python3
"""
ZyntaSchoolBell Audio Generator

Generates multilingual audio announcements (Sinhala, Tamil, English)
for all school bell types using Microsoft Edge TTS.

Usage:
    pip install edge-tts
    python generate_audio.py
"""

import asyncio
import os
import sys

try:
    import edge_tts
except ImportError:
    print("ERROR: edge-tts not installed. Run: pip install edge-tts")
    sys.exit(1)

# Output directory (relative to repo root)
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
AUDIO_DIR = os.path.join(SCRIPT_DIR, "..", "audio")

# Voice configuration
VOICES = {
    "si": {
        "primary": "si-LK-SameeraNeural",
        "fallback": "si-LK-ThiliniNeural",
    },
    "ta": {
        "primary": "ta-LK-KumarNeural",
        "fallback": "ta-IN-PallaviNeural",
    },
    "en": {
        "primary": "en-US-AriaNeural",
        "fallback": "en-US-AriaNeural",
    },
}

# Audio announcements: audioKey -> {lang: text}
ANNOUNCEMENTS = {
    "opening_bell": {
        "si": "\u0db4\u0dcf\u0dc3\u0dbd \u0d86\u0dbb\u0db8\u0dca\u0db7 \u0dc0\u0dd9\u0dba\u0dd2. \u0dc3\u0dd2\u0dc3\u0dd4\u0db1\u0dca \u0dbb\u0dd0\u0dc3\u0dca\u0dc0\u0dd3\u0db8\u0dca \u0dc3\u0dca\u0dae\u0dcf\u0db1\u0dba\u0da7 \u0d91\u0db1\u0dca\u0db1.",
        "ta": "\u0baa\u0bb3\u0bcd\u0bb3\u0bbf \u0ba4\u0bca\u0b9f\u0b99\u0bcd\u0b95\u0bc1\u0b95\u0bbf\u0bb1\u0ba4\u0bc1. \u0bae\u0bbe\u0ba3\u0bb5\u0bb0\u0bcd\u0b95\u0bb3\u0bcd \u0b9a\u0bc7\u0bb0 \u0bb5\u0bc7\u0ba3\u0bcd\u0b9f\u0bc1\u0bae\u0bcd.",
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
        "si": "\u0dc0\u0dd2\u0dc0\u0dda\u0d9a \u0d9a\u0dcf\u0dbd\u0dba. \u0dc3\u0dd2\u0dc3\u0dd4\u0db1\u0dca\u0da7 \u0d9a\u0dd9\u0da7\u0dd2 \u0dc0\u0dd2\u0dc0\u0dda\u0d9a\u0dba\u0d9a\u0dca \u0d9c\u0dad \u0dc4\u0dd0\u0d9a.",
        "ta": "\u0b87\u0b9f\u0bc8\u0bb5\u0bc7\u0bb3\u0bc8 \u0ba8\u0bc7\u0bb0\u0bae\u0bcd. \u0bae\u0bbe\u0ba3\u0bb5\u0bb0\u0bcd\u0b95\u0bb3\u0bcd \u0b93\u0baf\u0bcd\u0bb5\u0bc1 \u0b8e\u0b9f\u0bc1\u0b95\u0bcd\u0b95\u0bb2\u0bbe\u0bae\u0bcd.",
        "en": "Interval time. Students may take a short break.",
    },
    "lunch_break": {
        "si": "\u0daf\u0dd2\u0dc0\u0dcf \u0d86\u0dc4\u0dcf\u0dbb \u0dc0\u0dd2\u0dc0\u0dda\u0d9a\u0dba. \u0dc3\u0dd2\u0dc3\u0dd4\u0db1\u0dca \u0d86\u0db4\u0db1\u0dc1\u0dcf\u0dbd\u0dcf\u0dc0\u0da7 \u0dba\u0db1\u0dca\u0db1.",
        "ta": "\u0bae\u0ba4\u0bbf\u0baf \u0b89\u0ba3\u0bb5\u0bc1 \u0b87\u0b9f\u0bc8\u0bb5\u0bc7\u0bb3\u0bc8. \u0bae\u0bbe\u0ba3\u0bb5\u0bb0\u0bcd\u0b95\u0bb3\u0bcd \u0b89\u0ba3\u0bb5\u0b95\u0ba4\u0bcd\u0ba4\u0bbf\u0bb1\u0bcd\u0b95\u0bc1 \u0b9a\u0bc6\u0bb2\u0bcd\u0bb2\u0bc1\u0b99\u0bcd\u0b95\u0bb3\u0bcd.",
        "en": "Lunch break. Students please proceed to the canteen.",
    },
    "afternoon_bell": {
        "si": "\u0daf\u0dc4\u0dc0\u0dbd\u0dca \u0dc3\u0dd0\u0dc3\u0dd2\u0dba \u0d86\u0dbb\u0db8\u0dca\u0db7 \u0dc0\u0dd9\u0dba\u0dd2.",
        "ta": "\u0baa\u0bbf\u0bb1\u0bcd\u0baa\u0b95\u0bb2\u0bcd \u0b85\u0bae\u0bb0\u0bcd\u0bb5\u0bc1 \u0ba4\u0bca\u0b9f\u0b99\u0bcd\u0b95\u0bc1\u0b95\u0bbf\u0bb1\u0ba4\u0bc1.",
        "en": "The afternoon session is beginning.",
    },
    "closing_bell": {
        "si": "\u0d85\u0daf \u0dc3\u0db3\u0dc4\u0dcf \u0db4\u0dcf\u0dc3\u0dbd \u0db1\u0dd2\u0db8\u0dcf \u0dc0\u0dd3 \u0d87\u0dad. \u0dc3\u0dd2\u0dc3\u0dd4\u0db1\u0dca \u0db4\u0dd2\u0dc5\u0dd2\u0dc0\u0dd9\u0dbd\u0da7 \u0db4\u0dd2\u0da7\u0dc0 \u0dba\u0db1\u0dca\u0db1.",
        "ta": "\u0b87\u0ba9\u0bcd\u0bb1\u0bc8\u0b95\u0bcd\u0b95\u0bc1 \u0baa\u0bb3\u0bcd\u0bb3\u0bbf \u0bae\u0bc1\u0b9f\u0bbf\u0ba8\u0bcd\u0ba4\u0ba4\u0bc1. \u0bae\u0bbe\u0ba3\u0bb5\u0bb0\u0bcd\u0b95\u0bb3\u0bcd \u0b92\u0bb4\u0bc1\u0b99\u0bcd\u0b95\u0bbe\u0b95 \u0bb5\u0bc6\u0bb3\u0bbf\u0baf\u0bc7\u0bb1\u0bc1\u0b99\u0bcd\u0b95\u0bb3\u0bcd.",
        "en": "School is over for today. Students please leave in an orderly manner.",
    },
    "warning_bell": {
        "si": "\u0d85\u0db1\u0dad\u0dd4\u0dbb\u0dd4 \u0d87\u0d9f\u0dc0\u0dd3\u0db8\u0dda \u0dc3\u0dd3\u0db1\u0dd4\u0dc0. \u0d9a\u0dbb\u0dd4\u0dab\u0dcf\u0d9a\u0dbb \u0d94\u0db6\u0dda \u0db4\u0db1\u0dca\u0dad\u0dd2 \u0d9a\u0dcf\u0db8\u0dbb\u0dc0\u0dbd\u0da7 \u0db1\u0dd0\u0dc0\u0dad \u0dba\u0db1\u0dca\u0db1.",
        "ta": "\u0b8e\u0b9a\u0bcd\u0b9a\u0bb0\u0bbf\u0b95\u0bcd\u0b95\u0bc8 \u0bae\u0ba3\u0bbf. \u0ba4\u0baf\u0bb5\u0bc1\u0b9a\u0bc6\u0baf\u0bcd\u0ba4\u0bc1 \u0bb5\u0b95\u0bc1\u0baa\u0bcd\u0baa\u0bb1\u0bc8\u0b95\u0bb3\u0bc1\u0b95\u0bcd\u0b95\u0bc1\u0ba4\u0bcd \u0ba4\u0bbf\u0bb0\u0bc1\u0bae\u0bcd\u0baa\u0bc1\u0b99\u0bcd\u0b95\u0bb3\u0bcd.",
        "en": "Warning bell. Please return to your classrooms.",
    },
    "assembly": {
        "si": "\u0dbb\u0dd0\u0dc3\u0dca\u0dc0\u0dd3\u0db8\u0dca \u0d9a\u0dcf\u0dbd\u0dba. \u0dc3\u0dd2\u0dc3\u0dd4\u0db1\u0dca \u0dbb\u0dd0\u0dc3\u0dca\u0dc0\u0dd3\u0db8\u0dca \u0db4\u0dd2\u0da7\u0dd2\u0dba\u0da7 \u0dba\u0db1\u0dca\u0db1.",
        "ta": "\u0b9a\u0b9f\u0bcd\u0b9f\u0b9a\u0baa\u0bc8 \u0ba8\u0bc7\u0bb0\u0bae\u0bcd. \u0bae\u0bbe\u0ba3\u0bb5\u0bb0\u0bcd\u0b95\u0bb3\u0bcd \u0b9a\u0b9f\u0bcd\u0b9f\u0b9a\u0baa\u0bc8 \u0bae\u0bc8\u0ba4\u0bbe\u0ba9\u0ba4\u0bcd\u0ba4\u0bbf\u0bb1\u0bcd\u0b95\u0bc1 \u0b9a\u0bc6\u0bb2\u0bcd\u0bb2\u0bc1\u0b99\u0bcd\u0b95\u0bb3\u0bcd.",
        "en": "Assembly time. All students please proceed to the assembly ground.",
    },
}


async def generate_audio(audio_key, lang, text, voice_config):
    """Generate a single audio file using edge-tts."""
    output_dir = os.path.join(AUDIO_DIR, audio_key)
    os.makedirs(output_dir, exist_ok=True)
    output_file = os.path.join(output_dir, f"{lang}.mp3")

    voice = voice_config["primary"]
    try:
        communicate = edge_tts.Communicate(text, voice)
        await communicate.save(output_file)
        print(f"  [OK] {audio_key}/{lang}.mp3 (voice: {voice})")
        return True
    except Exception as e:
        print(f"  [WARN] Primary voice failed for {audio_key}/{lang}: {e}")
        # Try fallback voice
        fallback = voice_config["fallback"]
        if fallback != voice:
            try:
                communicate = edge_tts.Communicate(text, fallback)
                await communicate.save(output_file)
                print(f"  [OK] {audio_key}/{lang}.mp3 (fallback voice: {fallback})")
                return True
            except Exception as e2:
                print(f"  [ERROR] Fallback also failed for {audio_key}/{lang}: {e2}")
                return False
        else:
            print(f"  [ERROR] No fallback available for {audio_key}/{lang}")
            return False


async def main():
    print("ZyntaSchoolBell Audio Generator")
    print("=" * 40)
    print(f"Output directory: {os.path.abspath(AUDIO_DIR)}")
    print()

    total = 0
    success = 0
    failed = 0

    for audio_key, texts in ANNOUNCEMENTS.items():
        print(f"\nGenerating: {audio_key}")
        for lang in ["si", "en", "ta"]:
            total += 1
            text = texts[lang]
            voice_config = VOICES[lang]
            result = await generate_audio(audio_key, lang, text, voice_config)
            if result:
                success += 1
            else:
                failed += 1

    print(f"\n{'=' * 40}")
    print(f"Complete: {success}/{total} files generated successfully")
    if failed > 0:
        print(f"Failed: {failed} files")
        sys.exit(1)


if __name__ == "__main__":
    asyncio.run(main())
