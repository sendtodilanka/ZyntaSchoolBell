#!/usr/bin/env python3
"""
ZyntaSchoolBell Audio Generator v2.0

Generates multilingual audio announcements (Sinhala, Tamil, English)
for all school bell types using Google Text-to-Speech (gTTS).

Usage:
    pip install gtts
    python generate_audio.py
"""

import asyncio
import os
import sys

try:
    from gtts import gTTS
except ImportError:
    print("ERROR: gtts not installed. Run: pip install gtts")
    sys.exit(1)

# Output directory (relative to repo root)
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
AUDIO_DIR = os.path.join(SCRIPT_DIR, "..", "audio")

# Google TTS language codes
LANG_CODES = {
    "si": "si",
    "ta": "ta",
    "en": "en",
}

# Audio announcements: audioKey -> {lang: text}
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


def generate_audio(audio_key, lang, text, lang_code):
    """Generate a single audio file using Google TTS."""
    output_dir = os.path.join(AUDIO_DIR, audio_key)
    os.makedirs(output_dir, exist_ok=True)
    output_file = os.path.join(output_dir, f"{lang}.mp3")

    try:
        tts = gTTS(text=text, lang=lang_code, slow=False)
        tts.save(output_file)
        print(f"  [OK] {audio_key}/{lang}.mp3 (lang: {lang_code})")
        return True
    except Exception as e:
        print(f"  [ERROR] {audio_key}/{lang}: {e}")
        return False


async def main():
    print("ZyntaSchoolBell Audio Generator v2.0 (Google TTS)")
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
            lang_code = LANG_CODES[lang]
            result = generate_audio(audio_key, lang, text, lang_code)
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
