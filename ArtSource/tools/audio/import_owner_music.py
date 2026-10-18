# The owner's own music, cut into seamless loops and written where the game loads its tracks
# (Resources/Audio/Music/<track>.wav):
# - dungeon (2026-10-18: "check the latest mp3 in the downloads folder, add is music in the dungeon. Try and make it loop
#   seamlessly"): "Liturgy of the Deep", a quiet build-up (to about 25 s), then a 64.0 s section played twice, ending in
#   a fade at about 113 s. The loop is one pass of that section, cut at 44.0 s where its two passes match best.
# - town (2026-10-19: "check the laste mp3 in downloads, make it the new town music. Make it loop seamlessly"): "Buried
#   Sanctum", phrases of 12.8 s, a quiet opening (to about 10 s) and a fade from about 113 s. The loop is the 89.6 s
#   (seven phrases) from 17.5 s, where the music 89.6 s later matches best.
# Each loop is a whole number of phrases (so the beat runs on through the seam), found by correlating the waveform one
# loop apart, and its first 1.5 s crossfaded with what follows the cut, so the end runs into the start with no jump.
# Levelled to the old generated loop's loudness, never above -1 dB. Needs numpy; decodes with macOS afconvert.
# make_music.py no longer writes these tracks.
#   python3 ArtSource/tools/audio/import_owner_music.py
import os, subprocess, tempfile, wave
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
MUSIC = os.path.join(ROOT, "Assets", "_Project", "Resources", "Audio", "Music")
RATE = 44100
CROSSFADE = 1.5    # seconds

# Source in ArtSource/audio/owner, the game's track, where the loop starts and how long it is (seconds), and its
# loudness (RMS dB, about the generated loop's it replaces).
TRACKS = [
    ("Liturgy of the Deep.mp3", "dungeon", 44.0, 64.0, -15.0),
    ("Buried Sanctum.mp3", "town", 17.5, 89.6, -15.0),
]


def decode(path):
    with tempfile.TemporaryDirectory() as tmp:
        out = os.path.join(tmp, "x.wav")
        subprocess.run(["afconvert", "-f", "WAVE", "-d", "LEI16@%d" % RATE, path, out], check=True)
        with wave.open(out, "rb") as w:
            data = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).astype(np.float64) / 32768
            return data.reshape(-1, w.getnchannels())


def make(source, track, start_seconds, length_seconds, loudness):
    x = decode(os.path.join(ROOT, "ArtSource", "audio", "owner", source))
    if x.shape[1] == 1:
        x = np.repeat(x, 2, axis=1)
    period, start, fade = int(round(length_seconds * RATE)), int(round(start_seconds * RATE)), int(CROSSFADE * RATE)
    loop = x[start:start + period].copy()
    # The loop's start, faded in under the same moment one loop later fading out (equal power): the sample after the
    # loop's last is then that later pass's own next sample.
    t = np.linspace(0.0, 1.0, fade)[:, None]
    loop[:fade] = loop[:fade] * np.sin(t * np.pi / 2) + x[start + period:start + period + fade] * np.cos(t * np.pi / 2)
    loop *= 10 ** (loudness / 20) / np.sqrt((loop ** 2).mean())
    peak = np.abs(loop).max()
    if peak > 10 ** (-1 / 20):
        loop *= 10 ** (-1 / 20) / peak
    out = os.path.join(MUSIC, track + ".wav")
    with wave.open(out, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((loop * 32767).round().astype("<i2").tobytes())
    jump = np.abs(loop[-1] - loop[0]).max()
    print("wrote %s: %.2f s, step across the seam %.4f, peak %.1f dB" % (out, len(loop) / RATE, jump, 20 * np.log10(np.abs(loop).max())))


for row in TRACKS:
    make(*row)
