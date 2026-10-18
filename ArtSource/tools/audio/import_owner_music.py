# The owner's own music (2026-10-18: "check the latest mp3 in the downloads folder, add is music in the dungeon. Try and
# make it loop seamlessly"): "Liturgy of the Deep" in ArtSource/audio/owner becomes Resources/Audio/Music/dungeon.wav.
# The track is a quiet build-up (to about 25 s), then a 64.0 s section played twice, ending in a fade at about 113 s.
# The loop is one pass of that section, 64.0 s exactly (so the beat runs on through the seam), cut where its two passes
# match best (44.0 s; measured by correlating the waveform 64 s apart), and its first 1.5 s crossfaded with what follows
# the cut in the second pass, so the end runs into the start with no jump. Levelled to the old dungeon loop's loudness,
# never above -1 dB. Needs numpy; decodes with macOS afconvert. make_music.py no longer writes the dungeon track.
#   python3 ArtSource/tools/audio/import_owner_music.py
import os, subprocess, tempfile, wave
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
SOURCE = os.path.join(ROOT, "ArtSource", "audio", "owner", "Liturgy of the Deep.mp3")
OUT = os.path.join(ROOT, "Assets", "_Project", "Resources", "Audio", "Music", "dungeon.wav")
RATE = 44100
PERIOD = 64.0      # seconds between the section's two passes
START = 44.0       # where the loop is cut
CROSSFADE = 1.5    # seconds
LOUDNESS = -15.0   # RMS dB, about the generated dungeon loop's (-14.7)


def decode(path):
    with tempfile.TemporaryDirectory() as tmp:
        out = os.path.join(tmp, "x.wav")
        subprocess.run(["afconvert", "-f", "WAVE", "-d", "LEI16@%d" % RATE, path, out], check=True)
        with wave.open(out, "rb") as w:
            data = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).astype(np.float64) / 32768
            return data.reshape(-1, w.getnchannels())


def main():
    x = decode(SOURCE)
    if x.shape[1] == 1:
        x = np.repeat(x, 2, axis=1)
    period, start, fade = int(PERIOD * RATE), int(START * RATE), int(CROSSFADE * RATE)
    loop = x[start:start + period].copy()
    # The loop's start, faded in under the same moment of the second pass fading out (equal power): the sample after
    # the loop's last is then the second pass's own next sample.
    t = np.linspace(0.0, 1.0, fade)[:, None]
    loop[:fade] = loop[:fade] * np.sin(t * np.pi / 2) + x[start + period:start + period + fade] * np.cos(t * np.pi / 2)
    rms = np.sqrt((loop ** 2).mean())
    loop *= 10 ** (LOUDNESS / 20) / rms
    peak = np.abs(loop).max()
    if peak > 10 ** (-1 / 20):
        loop *= 10 ** (-1 / 20) / peak
    with wave.open(OUT, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((loop * 32767).round().astype("<i2").tobytes())
    jump = np.abs(loop[-1] - loop[0]).max()
    print("wrote %s: %.2f s, step across the seam %.4f, peak %.1f dB" % (OUT, len(loop) / RATE, jump, 20 * np.log10(np.abs(loop).max())))


main()
