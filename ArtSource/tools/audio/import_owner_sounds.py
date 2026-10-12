# The owner's own sounds (2026-10-12: "I have made some new game sounds. Use them"): the recordings in
# ArtSource/audio/owner (the hit, the boss music and the three bow shots were tried and dropped, the owner: "Does not
# sound good", "remove all new bow sounds"), decoded
# with macOS afconvert, trimmed to where the sound is (a short lead-in kept, a short fade at
# the end), effects made mono, levelled to sit with the game's other sounds (never peaking above -1 dB), and written as
# 44.1 kHz WAVs where the game loads them: Resources/Audio/Sfx/<SoundId>.wav and Resources/Audio/Music/<track>.wav.
# make_sfx.py and make_music.py leave these names alone.
#   python3 ArtSource/tools/audio/import_owner_sounds.py
import array, math, os, subprocess, tempfile, wave

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
SOURCE = os.path.join(ROOT, "ArtSource", "audio", "owner")
SFX = os.path.join(ROOT, "Assets", "_Project", "Resources", "Audio", "Sfx")
MUSIC = os.path.join(ROOT, "Assets", "_Project", "Resources", "Audio", "Music")
RATE = 44100

# Source file, the game's name for it, where it goes, and the loudness (RMS of the sound, dB) it is levelled to:
# about what the sound it replaces or sits beside has (the bow and hit as the old shot and hit, the coin a little
# quieter, gold being picked up in bursts).
SOUNDS = [
    ("Coin pickup.mp3", "Gold", SFX, -27.0),
    ("Explosion.mp3", "Explosion", SFX, -14.0),
    ("Frost nova.mp3", "FrostNova", SFX, -15.0),
]
OWNER_NAMES = {name for _, name, _, _ in SOUNDS}


def decode(path):
    """The file as 16-bit samples at 44.1 kHz: (channels, interleaved samples)."""
    with tempfile.TemporaryDirectory() as tmp:
        out = os.path.join(tmp, "x.wav")
        subprocess.run(["afconvert", "-f", "WAVE", "-d", "LEI16@%d" % RATE, path, out], check=True)
        with wave.open(out, "rb") as w:
            return w.getnchannels(), array.array("h", w.readframes(w.getnframes()))


def to_mono(channels, data):
    if channels == 1:
        return list(data)
    return [(data[i] + data[i + 1]) / 2 for i in range(0, len(data), channels)]


# Cut shorter than the recording (the coin, 2026-10-12: "maybe shorter and lower volume overall").
MAX_SECONDS = {"Gold": 0.22}


def trim(samples, lead=0.01, tail=0.06, fade=0.04, longest=None):
    """From just before the sound starts to a short fade after it ends (the sound: above 2 percent of the peak), or
    to longest seconds, fading out over its last third, when that is shorter."""
    peak = max(abs(s) for s in samples) or 1
    threshold = peak * 0.02
    first = next(i for i, s in enumerate(samples) if abs(s) > threshold)
    last = len(samples) - next(i for i, s in enumerate(reversed(samples)) if abs(s) > threshold)
    start = max(0, first - int(lead * RATE))
    end = min(len(samples), last + int(tail * RATE))
    if longest is not None and end - start > int(longest * RATE):
        end = start + int(longest * RATE)
        fade = longest / 3
    out = samples[start:end]
    n = int(fade * RATE)
    for k in range(min(n, len(out))):
        out[len(out) - 1 - k] *= k / n
    return out


def level(samples, target_db):
    """Gain to the target loudness, held back so the peak stays at or under -1 dB."""
    rms = math.sqrt(sum(s * s for s in samples) / len(samples)) / 32768
    peak = max(abs(s) for s in samples) / 32768
    gain = 10 ** (target_db / 20) / max(rms, 1e-6)
    gain = min(gain, 10 ** (-1 / 20) / max(peak, 1e-6))
    return [s * gain for s in samples], 20 * math.log10(gain)


def write(path, channels, samples):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    data = array.array("h", (max(-32767, min(32767, int(round(s)))) for s in samples))
    with wave.open(path, "wb") as w:
        w.setnchannels(channels)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data.tobytes())


if __name__ == "__main__":
    for source, name, folder, target in SOUNDS:
        channels, data = decode(os.path.join(SOURCE, source))
        if folder == MUSIC:
            # Music keeps its stereo and its own level; only decoded to the format the other tracks have.
            write(os.path.join(folder, name + ".wav"), channels, data)
            print("REPORT %-14s %.2f s, stereo, as made" % (name, len(data) / channels / RATE))
            continue
        samples = trim(to_mono(channels, data), longest=MAX_SECONDS.get(name))
        samples, gain = level(samples, target)
        write(os.path.join(folder, name + ".wav"), 1, samples)
        print("REPORT %-14s %.2f s, %+.1f dB" % (name, len(samples) / RATE, gain))
