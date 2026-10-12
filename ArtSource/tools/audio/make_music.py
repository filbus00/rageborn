# The game's music (2026-10-04), composed and played in code and written as looping WAV files to
# Assets/_Project/Resources/Audio/Music/<track>.wav, which MusicDirector plays (MusicSynth stays the fallback):
#   town     a fingerpicked guitar in D minor, 6/8, after the feel of Tristram's and the Rogue camp's guitar (not their
#            notes), building over three passes: harp sparkles; then a hand drum, a cello and the flute's tune; then strings,
#            a second guitar and a fiddle's countermelody (remade 2026-10-12)
#   dungeon  dark: low drones, a distant "oo" choir and wind, with a slow heartbeat of deep drums, a cello's Phrygian
#            motif, tremolo string swells, chains clanking and sparse bells, in a long reverb (remade 2026-10-12)
#   boss     a driving piece at 132 a minute: taiko drums, a low string ostinato, choir chords and brass stabs
# Every track is one seamless loop: notes that run past the end wrap round to the start, and all filtering and the
# reverb are circular (done in the frequency domain over the whole loop), so the loop point cannot be heard.
# Instruments are physical-ish models in numpy: plucked strings as sums of decaying partials, saws as band-limited
# harmonic sums with a time-varying brightness, a formant-filtered choir, a breathy flute, inharmonic bells, pitch-swept
# drums. 44.1 kHz, 16-bit stereo. Needs numpy, so run it with Blender's Python:
#   /Applications/Blender.app/Contents/MacOS/Blender -b --python-exit-code 1 -P ArtSource/tools/audio/make_music.py -- [tracks]
import os, sys, wave
import numpy as np

RATE = 44100
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Resources", "Audio", "Music")


def hz(midi):
    return 440.0 * 2 ** ((midi - 69) / 12.0)


class Track:
    """A stereo loop of a fixed length; notes are added at a time and wrap round the end."""

    def __init__(self, seconds, seed):
        self.n = int(seconds * RATE)
        self.rng = np.random.default_rng(seed)
        self.buses = {}

    def bus(self, name):
        if name not in self.buses:
            self.buses[name] = np.zeros((2, self.n))
        return self.buses[name]

    def add(self, name, start, mono, pan=0.0, gain=1.0):
        """Adds a mono sound at a time (seconds), panned -1 (left) to 1 (right), wrapping round the loop's end."""
        b = self.bus(name)
        i = int(start * RATE) % self.n
        left = np.cos((pan + 1) * np.pi / 4) * gain
        right = np.sin((pan + 1) * np.pi / 4) * gain
        x = mono
        while len(x) > 0:
            take = min(len(x), self.n - i)
            b[0, i:i + take] += x[:take] * left
            b[1, i:i + take] += x[:take] * right
            x = x[take:]
            i = 0


def times(seconds):
    return np.arange(int(seconds * RATE)) / RATE


def adsr(seconds, attack, release, curve=1.0):
    t = times(seconds)
    a = np.minimum(1.0, t / max(attack, 1e-4))
    r = np.clip((seconds - t) / max(release, 1e-4), 0, 1)
    return (a * r) ** curve


# ------------------------------------------------------------------ instruments

def pluck(freq, seconds, rng, bright=1.0, decay=2.5, position=0.18):
    """A plucked nylon-ish string: harmonics with the pluck point's comb, each dying faster the higher it is, a touch
    of inharmonicity and a short noisy attack."""
    t = times(seconds)
    out = np.zeros_like(t)
    k = 1
    while freq * k < 7000 and k <= 40:
        f = freq * k * np.sqrt(1 + 0.00008 * k * k)
        amp = abs(np.sin(np.pi * k * position)) / k ** (1.4 - 0.3 * bright)
        tau = decay / (1 + 0.45 * (k - 1) / bright)
        out += amp * np.exp(-t / tau) * np.sin(2 * np.pi * f * t + rng.uniform(0, 6.28))
        k += 1
    attack = np.minimum(1, t / 0.002)
    click = rng.uniform(-1, 1, len(t)) * np.exp(-t / 0.004) * 0.15
    return (out * attack + click) * adsr(seconds, 0.001, 0.08)


def saw(freq, t, brightness, detune_cents=0.0, vibrato=0.0, rng=None):
    """A band-limited saw over times t; brightness (a number or an array over t) sets how fast the harmonics fall off
    (1 bright, 0.2 dark). Vibrato in cents at 5 Hz."""
    cents = detune_cents + vibrato * np.sin(2 * np.pi * 5.0 * t + (rng.uniform(0, 6.28) if rng is not None else 0))
    f = freq * 2 ** (cents / 1200.0)
    phase = 2 * np.pi * np.cumsum(f * np.ones_like(t)) / RATE
    phase += rng.uniform(0, 6.28) if rng is not None else 0
    out = np.zeros_like(t)
    k = 1
    b = np.maximum(np.asarray(brightness) * np.ones_like(t), 0.05)
    while freq * k < 9000 and k <= 60:
        out += np.sin(k * phase) / k * np.exp(-(k - 1) / (b * 12.0))
        k += 1
    return out


def pad(freqs, seconds, rng, attack=1.5, release=2.0, brightness=0.35, voices=3, vibrato=4.0):
    t = times(seconds)
    out = np.zeros_like(t)
    for f in freqs:
        for v in range(voices):
            out += saw(f, t, brightness, detune_cents=(v - (voices - 1) / 2) * 7, vibrato=vibrato, rng=rng)
    return out * adsr(seconds, attack, release) / (len(freqs) * voices)


def flute(freq, seconds, rng, vibrato=18.0):
    t = times(seconds)
    vib = vibrato * np.minimum(1, t / 0.4) * np.sin(2 * np.pi * 5.2 * t)
    f = freq * 2 ** (vib / 1200.0)
    phase = 2 * np.pi * np.cumsum(f) / RATE
    tone = np.sin(phase) + 0.18 * np.sin(2 * phase) + 0.06 * np.sin(3 * phase)
    breath = rng.uniform(-1, 1, len(t))
    breath = np.convolve(breath, np.ones(6) / 6, mode="same") * 0.05
    return (tone + breath) * adsr(seconds, 0.08, 0.25, 1.2)


BELL_RATIOS = [0.56, 0.92, 1.19, 1.71, 2.0, 2.74, 3.0, 3.76, 4.07]


def bell(freq, seconds, rng, decay=4.0):
    t = times(seconds)
    out = np.zeros_like(t)
    for i, r in enumerate(BELL_RATIOS):
        out += np.exp(-t / (decay / (1 + i * 0.35))) * np.sin(2 * np.pi * freq * r * t + rng.uniform(0, 6.28)) / (1 + i * 0.4)
    return out * np.minimum(1, t / 0.003)


def boom(seconds, rng, f0=55.0, f1=30.0, decay=1.2):
    """A deep drum: a sine falling in pitch, plus a low thud of noise."""
    t = times(seconds)
    f = f1 + (f0 - f1) * np.exp(-t / 0.08)
    body = np.sin(2 * np.pi * np.cumsum(f) / RATE) * np.exp(-t / decay)
    thud = lowpass_once(rng.uniform(-1, 1, len(t)), 200) * np.exp(-t / 0.05) * 0.8
    return body + thud


def taiko(rng, accent=1.0):
    t = times(1.2)
    f = 62 + 60 * np.exp(-t / 0.03)
    body = np.sin(2 * np.pi * np.cumsum(f) / RATE) * np.exp(-t / 0.35)
    skin = lowpass_once(rng.uniform(-1, 1, len(t)), 900) * np.exp(-t / 0.03) * 1.2
    return (body + skin) * accent


def frame_drum(rng):
    t = times(0.4)
    body = np.sin(2 * np.pi * 180 * t) * np.exp(-t / 0.06)
    rattle = bandpass_once(rng.uniform(-1, 1, len(t)), 1500, 5000) * np.exp(-t / 0.08)
    return body * 0.5 + rattle * 0.9


def cymbal_swell(seconds, rng):
    x = bandpass_once(rng.uniform(-1, 1, int(seconds * RATE)), 4000, 14000)
    t = times(seconds)
    return x * (t / seconds) ** 3


def choir(freq, seconds, rng, vowel="oo", attack=1.2, release=1.8):
    """Several detuned saws per note through a vowel's formants (filtered once per note in the frequency domain)."""
    t = times(seconds)
    raw = np.zeros_like(t)
    for v in range(5):
        raw += saw(freq, t, 0.9, detune_cents=(v - 2) * 6, vibrato=10.0, rng=rng)
    formants = {"oo": [(300, 90, 1.0), (870, 120, 0.3), (2240, 200, 0.08)],
                "ah": [(730, 110, 1.0), (1090, 130, 0.5), (2440, 200, 0.15)]}[vowel]
    spec = np.fft.rfft(raw)
    f = np.fft.rfftfreq(len(raw), 1 / RATE)
    shape = np.zeros_like(f)
    for centre, width, gain in formants:
        shape += gain * np.exp(-0.5 * ((f - centre) / width) ** 2)
    out = np.fft.irfft(spec * shape, len(raw))
    return out / (np.max(np.abs(out)) + 1e-9) * adsr(seconds, attack, release)


def brass(freq, seconds, rng):
    """A low brass stab: a saw whose brightness swells then falls."""
    t = times(seconds)
    bright = 0.15 + 0.7 * np.minimum(1, t / 0.06) * np.exp(-t / 0.5)
    out = saw(freq, t, bright, rng=rng) + saw(freq * 1.003, t, bright, rng=rng)
    return out * adsr(seconds, 0.03, 0.3) * 0.5


def strings_note(freq, seconds, rng, bright=0.3):
    t = times(seconds)
    out = saw(freq, t, bright, detune_cents=-4, vibrato=6, rng=rng) + saw(freq, t, bright, detune_cents=4, vibrato=6, rng=rng)
    return out * adsr(seconds, 0.015, 0.06) * 0.5


def bowed(freq, seconds, rng, bright=0.35, vibrato=12.0, attack=0.35, release=0.5, lo=120, hi=3200):
    """A bowed string (cello, fiddle): two detuned saws with a vibrato that comes in after the attack, through a body
    band, swelling in and dying away."""
    t = times(seconds)
    vib = vibrato * np.minimum(1, t / 0.5)
    out = np.zeros_like(t)
    for d in (-3, 3):
        cents = d + vib * np.sin(2 * np.pi * 5.3 * t + rng.uniform(0, 6.28))
        f = freq * 2 ** (cents / 1200.0)
        phase = 2 * np.pi * np.cumsum(f) / RATE + rng.uniform(0, 6.28)
        k = 1
        while freq * k < 9000 and k <= 40:
            out += np.sin(k * phase) / k * np.exp(-(k - 1) / (bright * 12.0))
            k += 1
    out = bandpass_once(out, lo, hi)
    return out * adsr(seconds, attack, release, 1.3) * 0.5


def tremolo(freqs, seconds, rng, rate=9.0, attack=3.0, release=1.5):
    """A cluster of strings bowed in tremolo, swelling."""
    t = times(seconds)
    out = np.zeros_like(t)
    for f in freqs:
        for d in (-6, 0, 6):
            out += saw(f, t, 0.45, detune_cents=d, vibrato=8, rng=rng)
    out *= 0.55 + 0.45 * np.sin(2 * np.pi * rate * t + rng.uniform(0, 6.28))
    out = bandpass_once(out, 150, 4000)
    return out * adsr(seconds, attack, release, 1.5) / (3 * len(freqs))


def hand_drum(rng, accent=1.0):
    """A bodhran-ish frame drum: a low thump with a little skin."""
    t = times(0.5)
    f = 78 + 50 * np.exp(-t / 0.02)
    body = np.sin(2 * np.pi * np.cumsum(f) / RATE) * np.exp(-t / 0.16)
    skin = bandpass_once(rng.uniform(-1, 1, len(t)), 300, 2500) * np.exp(-t / 0.03) * 0.5
    return (body + skin) * accent


def clank(rng):
    """A chain or an iron door somewhere: a short burst of hard metallic hits."""
    out = np.zeros(int(1.6 * RATE))
    at = 0.0
    for k in range(rng.integers(2, 5)):
        hit = bell(rng.uniform(900, 1800), 0.9, rng, decay=0.25) * rng.uniform(0.4, 1.0)
        i = int(at * RATE)
        n = min(len(hit), len(out) - i)
        out[i:i + n] += hit[:n]
        at += rng.uniform(0.06, 0.22)
    return out


def swell(seconds, rng, cutoff=900):
    """A low rush of noise rising into a section's start."""
    x = lowpass_once(rng.uniform(-1, 1, int(seconds * RATE)), cutoff)
    t = times(seconds)
    return x * (t / seconds) ** 2.5


# ------------------------------------------------------------------ filters (one-off on a note, and circular on a bus)

def lowpass_once(x, cutoff):
    spec = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1 / RATE)
    return np.fft.irfft(spec / np.sqrt(1 + (f / cutoff) ** 4), len(x))


def bandpass_once(x, lo, hi):
    spec = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1 / RATE)
    shape = 1 / np.sqrt(1 + (lo / np.maximum(f, 1)) ** 4) / np.sqrt(1 + (f / hi) ** 4)
    return np.fft.irfft(spec * shape, len(x))


def circular_filter(stereo, lo=None, hi=None):
    out = np.empty_like(stereo)
    f = np.fft.rfftfreq(stereo.shape[1], 1 / RATE)
    shape = np.ones_like(f)
    if lo:
        shape /= np.sqrt(1 + (lo / np.maximum(f, 1)) ** 4)
    if hi:
        shape /= np.sqrt(1 + (f / hi) ** 4)
    for c in range(2):
        out[c] = np.fft.irfft(np.fft.rfft(stereo[c]) * shape, stereo.shape[1])
    return out


def reverb(stereo, seconds, rng, damp=4000, predelay=0.02):
    """Circular convolution with a decaying stereo noise tail (darker as it decays), so the loop's tail wraps round."""
    n = stereo.shape[1]
    length = int(seconds * RATE)
    t = np.arange(length) / RATE
    out = np.empty_like(stereo)
    for c in range(2):
        ir = rng.uniform(-1, 1, length) * np.exp(-t * 6.9 / seconds)
        ir = lowpass_once(ir, damp)
        ir[: int(predelay * RATE)] = 0
        ir /= np.sqrt(np.sum(ir ** 2)) + 1e-9
        padded = np.zeros(n)
        padded[: min(length, n)] = ir[: min(length, n)]
        out[c] = np.fft.irfft(np.fft.rfft(stereo[c]) * np.fft.rfft(padded), n)
    return out


def mix(track, sends, wet_seconds, wet, damp=4000):
    """Sums the buses (gain each) and adds a shared reverb; sends maps bus name to (gain, reverb send)."""
    dry = np.zeros((2, track.n))
    send = np.zeros((2, track.n))
    for name, (gain, amount) in sends.items():
        if name in track.buses:
            dry += track.buses[name] * gain
            send += track.buses[name] * gain * amount
    out = dry + reverb(send, wet_seconds, track.rng, damp) * wet
    out = circular_filter(out, lo=28)
    # The same loudness for every track (RMS 0.2, about -14 dB), peaks rounded off softly and kept under -1 dB.
    out = np.tanh(out / (np.sqrt(np.mean(out ** 2)) + 1e-9) * 0.2 * 1.3) / 1.3
    return out * min(1.0, 0.89 / np.max(np.abs(out)))


def save(name, stereo):
    os.makedirs(OUT, exist_ok=True)
    data = (np.clip(stereo, -1, 1).T * 32767).astype("<i2")
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data.tobytes())
    print("REPORT %s %.1f s" % (name, stereo.shape[1] / RATE))


# ------------------------------------------------------------------ the tracks

D = 50  # D3


def town():
    # Remade 2026-10-12 (the owner: "punch up the background music"): the same 6/8 guitar in D minor, a little quicker,
    # built up over three passes of the 16-bar progression. A: the guitar alone with a drone and harp sparkles. B: a
    # hand drum, a cello on the roots and the flute's tune. C: soft strings, a second guitar, fuller drums and a fiddle's
    # countermelody; its last two bars thin out again into A.
    eighth = 60.0 / (60 * 3)       # 6/8, dotted quarter at 60
    bar = eighth * 6
    prog = ["Dm", "Dm", "C", "C", "Bb", "Bb", "A", "A", "Dm", "Dm", "F", "C", "Gm", "Gm", "A", "A"]
    chords = {"Dm": (38, 50, 53, 57), "C": (36, 48, 52, 55), "Bb": (34, 46, 50, 53), "A": (33, 45, 49, 52),
              "F": (41, 48, 53, 57), "Gm": (31, 43, 46, 50)}
    bars = len(prog) * 3
    tr = Track(bar * bars, 11)
    rng = tr.rng
    for b in range(bars):
        name = prog[b % len(prog)]
        bass, root, third, fifth = chords[name]
        start = b * bar
        section = b // len(prog)
        tail = section == 2 and b % len(prog) >= 14
        # Bass on the bar, then the arpeggio climbs and falls.
        tr.add("guitar", start, pluck(hz(bass + 12), 4.0, rng, bright=0.8, decay=3.2), pan=-0.15, gain=0.6)
        pattern = [fifth, root + 12, third + 12, root + 12, fifth]
        if b % 4 == 3:
            pattern = [fifth, third + 12, fifth + 12, third + 12, root + 12]
        for i, note in enumerate(pattern):
            swing = rng.normal(0, 0.008)
            tr.add("guitar", start + (i + 1) * eighth + swing, pluck(hz(note), 2.6, rng, bright=0.9, decay=1.8),
                   pan=0.1 + 0.05 * i, gain=0.45 + 0.08 * (i == 2))
            # The second guitar in C: the same pattern an octave up, strummed a hair later, on the other side.
            if section == 2 and not tail and i % 2 == 0:
                tr.add("guitar2", start + (i + 1) * eighth + 0.012, pluck(hz(note + 12), 1.6, rng, bright=1.0, decay=1.1), pan=-0.55, gain=0.3)
        if b % 4 == 0:
            tr.add("drone", start, pluck(hz(38), 8.0, rng, bright=0.5, decay=6.0), gain=0.3)
        # Harp sparkles in A: a rolled chord high up every other bar.
        if section == 0 and b % 2 == 1:
            for i, note in enumerate((root + 24, third + 24, fifth + 24)):
                tr.add("harp", start + 3 * eighth + i * 0.07, pluck(hz(note), 2.0, rng, bright=1.0, decay=1.4, position=0.5), pan=0.45, gain=0.22)
        # The hand drum from B: the bar's one strong, the fourth eighth softer; in C every other eighth taps too.
        if section >= 1 and not tail:
            hits = [(0, 1.0), (3, 0.6), (5, 0.35)]
            if section == 2:
                hits += [(2, 0.3), (4, 0.25)]
            for e, accent in hits:
                tr.add("drums", start + e * eighth + rng.normal(0, 0.005), hand_drum(rng, accent), pan=0.15)
        # The cello on the roots from B.
        if section >= 1:
            tr.add("cello", start + 0.02, bowed(hz(bass + 12), bar + 0.3, rng, bright=0.3, vibrato=10, attack=0.4, release=0.6, lo=60, hi=2200), pan=-0.35, gain=0.5)
        if section == 2:
            tr.add("strings", start - 0.3, pad([hz(root), hz(third), hz(fifth)], bar + 0.8, rng, attack=0.9, release=1.0, brightness=0.25), gain=0.35)
    # The flute's tune over B: (bar, eighth, MIDI, eighths long).
    tune = [(16, 0, 69, 3), (16, 3, 67, 3), (17, 0, 65, 4), (17, 4, 64, 2), (18, 0, 64, 3), (18, 3, 65, 3), (19, 0, 67, 6),
            (20, 0, 65, 3), (20, 3, 62, 3), (21, 0, 62, 6), (22, 0, 61, 3), (22, 3, 64, 3), (23, 0, 64, 6),
            (24, 0, 69, 3), (24, 3, 72, 3), (25, 0, 70, 4), (25, 4, 69, 2), (26, 0, 69, 3), (26, 3, 65, 3), (27, 0, 67, 6),
            (28, 0, 70, 3), (28, 3, 69, 3), (29, 0, 67, 6), (30, 0, 64, 3), (30, 3, 61, 3), (31, 0, 62, 6)]
    for b, e, note, length in tune:
        tr.add("flute", b * bar + e * eighth, flute(hz(note), length * eighth + 0.2, rng), pan=0.25, gain=0.32)
    # The fiddle's countermelody over C, higher, the flute answering it an octave down in its gaps.
    fiddle = [(32, 0, 74, 3), (32, 3, 77, 3), (33, 0, 76, 4), (33, 4, 74, 2), (34, 0, 72, 6), (35, 0, 67, 3), (35, 3, 72, 3),
              (36, 0, 74, 3), (36, 3, 70, 3), (37, 0, 69, 4), (37, 4, 67, 2), (38, 0, 69, 6), (39, 0, 73, 3), (39, 3, 76, 3),
              (40, 0, 77, 3), (40, 3, 74, 3), (41, 0, 74, 6), (42, 0, 72, 3), (42, 3, 77, 3), (43, 0, 76, 4), (43, 4, 72, 2),
              (44, 0, 74, 3), (44, 3, 70, 3), (45, 0, 67, 6), (46, 0, 69, 3), (46, 3, 73, 3), (47, 0, 69, 6)]
    for b, e, note, length in fiddle:
        tr.add("fiddle", b * bar + e * eighth, bowed(hz(note), length * eighth + 0.15, rng, bright=0.55, vibrato=18, attack=0.06, release=0.2, lo=350, hi=5000), pan=0.3, gain=0.42)
    for b, e, note, length in ((34, 3, 60, 3), (38, 3, 61, 3), (41, 3, 62, 3), (45, 3, 62, 3)):
        tr.add("flute", b * bar + e * eighth, flute(hz(note), length * eighth + 0.2, rng), pan=0.4, gain=0.26)
    out = mix(tr, {"guitar": (1.0, 0.35), "guitar2": (1.0, 0.4), "harp": (1.0, 0.7), "drone": (0.4, 0.5), "drums": (0.9, 0.25),
                   "cello": (1.0, 0.45), "strings": (0.8, 0.6), "flute": (1.3, 0.5), "fiddle": (1.2, 0.5)}, 2.6, 0.5, 3800)
    save("town", out)


def dungeon():
    # Remade 2026-10-12 (the owner: "punch up the background music"): the old drones, choir, wind and bells under four
    # 32-second sections with more going on: a slow heartbeat of deep drums in the second and fourth, a cello's
    # Phrygian motif (D, E flat, D, C) in the first and third, tremolo string clusters swelling at the ends of the
    # second and fourth, chains and iron clanking somewhere, and a rush of noise into each section.
    seconds = 128.0
    section = 32.0
    tr = Track(seconds, 23)
    rng = tr.rng
    t = times(seconds)
    # Drones on D1, A1 and D2 whose brightness breathes (whole cycles in the loop).
    for midi, period, gain in ((26, 32.0, 0.5), (33, 128.0 / 3, 0.3), (38, 64.0, 0.2)):
        bright = 0.12 + 0.2 * (0.5 + 0.5 * np.sin(2 * np.pi * t / period + rng.uniform(0, 6.28)))
        f = round(hz(midi) * seconds) / seconds
        tone = saw(f, t, bright, detune_cents=0) + saw(round(f * 1.004 * seconds) / seconds, t, bright)
        tr.add("drone", 0, tone, gain=gain)
    # A distant choir: four long chords, one a section, crossfading.
    chords = [(50, 53, 57), (46, 50, 53, 57), (43, 50, 55, 58), (45, 49, 52)]
    for i, chord in enumerate(chords):
        for note in chord:
            tr.add("choir", i * section - 4.0, choir(hz(note), section + 10.0, rng, "oo", attack=7.0, release=9.0), pan=rng.uniform(-0.6, 0.6), gain=0.2)
    for side in (-0.7, 0.7):
        wind = bandpass_once(rng.uniform(-1, 1, len(t)), 180, 900)
        swing = 0.4 + 0.6 * (0.5 + 0.5 * np.sin(2 * np.pi * t / 16.0 + rng.uniform(0, 6.28))) ** 2
        tr.add("wind", 0, wind * swing, pan=side, gain=0.25)
    # The heartbeat: a deep double thump every 1.6 s through the second and fourth sections, rising then fading.
    for s0 in (section, 3 * section):
        beats = int(section / 1.6)
        for k in range(beats):
            level = np.sin(np.pi * (k + 0.5) / beats) ** 0.7
            at = s0 + k * 1.6
            tr.add("pulse", at, boom(1.2, rng, f0=60, f1=34, decay=0.35), gain=0.9 * level)
            tr.add("pulse", at + 0.3, boom(1.0, rng, f0=52, f1=32, decay=0.3), gain=0.55 * level)
    # The cello's motif in the first and third sections: long notes, each swelling.
    motif = [(0.0, 38, 6.0), (6.5, 39, 4.0), (11.0, 38, 6.0), (18.0, 36, 5.0), (23.5, 38, 7.0)]
    for s0 in (0.0, 2 * section):
        for at, note, length in motif:
            tr.add("cello", s0 + 2.0 + at, bowed(hz(note), length, rng, bright=0.3, vibrato=14, attack=1.4, release=2.0, lo=50, hi=1800), pan=-0.3, gain=0.75)
    # Tremolo clusters swelling at the ends of the second and fourth sections, cut at the turn.
    for s0 in (section, 3 * section):
        tr.add("tremolo", s0 + section - 9.0, tremolo([hz(50), hz(51), hz(57)], 9.0, rng, attack=7.0, release=0.4), pan=0.2, gain=0.6)
    # Chains and iron somewhere in the dark, now and then.
    at = 5.0
    while at < seconds - 2:
        tr.add("clank", at, clank(rng), pan=rng.uniform(-0.9, 0.9), gain=0.12)
        at += rng.uniform(9.0, 17.0)
    # Sparse bells and far booms; a rush into each section.
    at = 3.0
    while at < seconds - 1:
        note = rng.choice([62, 65, 69, 61, 74, 63])
        tr.add("bells", at, bell(hz(note), 7.0, rng, decay=4.5), pan=rng.uniform(-0.8, 0.8), gain=0.13)
        at += rng.uniform(7.0, 13.0)
    for at in (0.0, 20.0, 44.0, 58.0, 76.0, 100.0, 116.0):
        tr.add("booms", at, boom(4.0, rng, decay=1.6), gain=0.7)
    for k in range(4):
        tr.add("swell", k * section - 3.0, swell(3.0, rng), gain=0.5)
    out = mix(tr, {"drone": (0.35, 0.4), "choir": (1.5, 0.9), "wind": (1.0, 0.5), "pulse": (0.8, 0.4), "cello": (1.0, 0.6),
                   "tremolo": (0.9, 0.6), "clank": (1.0, 1.0), "bells": (2.0, 1.0), "booms": (0.6, 0.8), "swell": (0.6, 0.7)}, 5.0, 0.7, 2500)
    save("dungeon", out)


def boss():
    beat = 60.0 / 132
    bar = beat * 4
    bars = 32
    tr = Track(bar * bars, 37)
    rng = tr.rng
    prog = [(38, (50, 53, 57)), (34, (46, 50, 53)), (31, (43, 46, 50)), (33, (45, 49, 52))]
    for b in range(bars):
        root, chord = prog[(b // 2) % 4]
        start = b * bar
        section = b // 8
        # Taiko: big on 1 and the and of 2, small fills; heavier in the second half.
        hits = [(0, 1.0), (1.5, 0.7), (2, 0.5), (3, 0.8)]
        if section >= 2:
            hits += [(2.5, 0.5), (3.5, 0.6), (3.75, 0.5)]
        for at, accent in hits:
            tr.add("drums", start + at * beat, taiko(rng, accent), pan=rng.uniform(-0.3, 0.3))
        for e in range(8):
            if e % 2 == 1:
                tr.add("drums", start + e * beat / 2, frame_drum(rng), pan=0.4, gain=0.25)
        # The low string ostinato in eighths.
        line = [0, 0, 12, 0, 3, 0, 2, 0] if b % 2 == 0 else [0, 0, 12, 0, 7, 5, 3, 2]
        for e, step in enumerate(line):
            tr.add("ostinato", start + e * beat / 2, strings_note(hz(root + 12 + step), beat / 2 * 0.9, rng, bright=0.45),
                   pan=-0.3, gain=0.5 + 0.15 * (e == 0))
        # Choir chords over two bars, and brass stabs on bar starts from the second section.
        if b % 2 == 0:
            for note in chord:
                tr.add("choir", start - 0.1, choir(hz(note + 12), bar * 2 + 0.6, rng, "ah", attack=0.4, release=0.6), pan=rng.uniform(-0.5, 0.5), gain=0.3)
            if section >= 1:
                for note in (root + 12, chord[1], chord[2]):
                    tr.add("brass", start, brass(hz(note), beat * 1.5, rng), pan=0.25, gain=0.5)
        if b % 8 == 7:
            tr.add("drums", start + bar - 2.0, cymbal_swell(2.0, rng), pan=0.0, gain=0.25)
    # A bell melody over the last section.
    tune = [74, 72, 70, 69, 70, 69, 67, 69]
    for i, note in enumerate(tune):
        tr.add("bells", (24 + i) * bar, bell(hz(note), 3.0, rng, decay=2.0), pan=0.3, gain=0.2)
    out = mix(tr, {"drums": (0.5, 0.25), "ostinato": (1.2, 0.3), "choir": (1.3, 0.7), "brass": (1.3, 0.4), "bells": (1.6, 0.8)}, 2.2, 0.5, 5000)
    save("boss", out)


argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
for name, make in (("town", town), ("dungeon", dungeon), ("boss", boss)):
    if not argv or name in argv:
        make()
