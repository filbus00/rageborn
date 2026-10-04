# The game's sound effects, designed in code and written as WAV files (2026-10-04), replacing the startup synth of
# SoundSynth where a file exists: Assets/_Project/Resources/Audio/Sfx/<SoundId>.wav, which Sfx loads first. Each sound
# is layers of a few physical models: a plucked string (Karplus-Strong) for the bow, filtered noise bursts for
# whooshes, flesh and crunch, struck modes (sums of decaying partials) for metal, coins and bells, pitch-swept sines for
# thumps and booms, and a convolution reverb (decaying noise) for space. 44.1 kHz, 16-bit mono, peaks set per sound.
# Needs numpy, so run it with Blender's Python:
#   /Applications/Blender.app/Contents/MacOS/Blender -b --python-exit-code 1 -P ArtSource/tools/audio/make_sfx.py
import os, sys, wave
import numpy as np

RATE = 44100
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Resources", "Audio", "Sfx")
rng = np.random.default_rng(7)


def n(seconds):
    return int(seconds * RATE)


def t(seconds):
    return np.arange(n(seconds)) / RATE


def env(seconds, decay, attack=0.002):
    x = t(seconds)
    return np.minimum(1.0, x / max(attack, 1e-4)) * np.exp(-x / decay)


def noise(seconds):
    return rng.uniform(-1, 1, n(seconds))


def onepole(x, cutoff):
    """A one-pole low-pass; cutoff may be a number or an array per sample (Hz)."""
    cutoff = np.broadcast_to(np.asarray(cutoff, dtype=float), x.shape)
    a = 1 - np.exp(-2 * np.pi * cutoff / RATE)
    y = np.empty_like(x)
    s = 0.0
    for i in range(len(x)):
        s += a[i] * (x[i] - s)
        y[i] = s
    return y


def bandpass(x, centre, q=1.0):
    """A biquad band-pass (constant peak gain); centre a number (Hz)."""
    w = 2 * np.pi * centre / RATE
    alpha = np.sin(w) / (2 * q)
    b0, b1, b2 = alpha, 0.0, -alpha
    a0, a1, a2 = 1 + alpha, -2 * np.cos(w), 1 - alpha
    b0, b1, b2, a1, a2 = b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0
    y = np.zeros_like(x)
    x1 = x2 = y1 = y2 = 0.0
    for i in range(len(x)):
        v = b0 * x[i] + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
        x2, x1 = x1, x[i]
        y2, y1 = y1, v
        y[i] = v
    return y


def highpass(x, cutoff):
    return x - onepole(x, cutoff)


def sweep(seconds, f0, f1, decay, curve=2.0):
    """A sine gliding from f0 to f1 (exponentially), under a decay."""
    x = t(seconds)
    p = (x / seconds) ** (1 / curve)
    freq = f0 * (f1 / f0) ** p
    phase = 2 * np.pi * np.cumsum(freq) / RATE
    return np.sin(phase) * env(seconds, decay, 0.001)


def modes(seconds, partials, attack=0.0005):
    """Struck modes: (frequency, amplitude, decay) partials."""
    x = t(seconds)
    y = np.zeros_like(x)
    for f, a, d in partials:
        y += a * np.sin(2 * np.pi * f * x + rng.uniform(0, 6.28)) * np.exp(-x / d)
    return y * np.minimum(1, x / attack)


def pluck(seconds, freq, damping=0.996, brightness=0.5):
    """Karplus-Strong: a noise burst through a delay line with an averaging loss."""
    period = int(RATE / freq)
    buf = rng.uniform(-1, 1, period)
    buf = onepole(buf, 2000 + 8000 * brightness)
    out = np.zeros(n(seconds))
    for i in range(len(out)):
        j = i % period
        out[i] = buf[j]
        buf[j] = damping * 0.5 * (buf[j] + buf[(j + 1) % period])
    return out


def reverb(x, seconds=1.2, wet=0.25, tone=4000):
    ir = noise(seconds) * np.exp(-t(seconds) / (seconds / 5))
    ir = onepole(ir, tone)
    ir[0] = 0
    size = len(x) + len(ir)
    fft = np.fft.rfft(np.pad(x, (0, len(ir))), size) * np.fft.rfft(ir, size)
    tail = np.fft.irfft(fft, size)[:size]
    tail /= max(1e-9, np.max(np.abs(tail)))
    dry = np.pad(x, (0, len(ir)))
    return dry + tail * wet * np.max(np.abs(x))


def at(x, seconds, length=None):
    """x placed at a time offset in a buffer."""
    start = n(seconds)
    total = max(start + len(x), length or 0)
    y = np.zeros(total)
    y[start:start + len(x)] += x
    return y


def mix(*parts):
    length = max(len(p) for p in parts)
    y = np.zeros(length)
    for p in parts:
        y[:len(p)] += p
    return y


def fade_tail(x, seconds=0.02):
    k = min(len(x), n(seconds))
    x = x.copy()
    x[-k:] *= np.linspace(1, 0, k)
    return x


def finish(x, peak):
    """Normalised to a peak, cut where it has fallen below -60 dB (plus a short fade)."""
    m = np.max(np.abs(x))
    if m <= 0:
        return x
    x = x / m * peak
    loud = np.where(np.abs(x) > peak * 0.001)[0]
    if len(loud):
        x = x[:min(len(x), loud[-1] + n(0.05))]
    return fade_tail(x)


def note(name):
    names = {"C": -9, "D": -7, "E": -5, "F": -4, "G": -2, "A": 0, "B": 2}
    semis = names[name[0]] + (1 if "#" in name else 0) + 12 * (int(name[-1]) - 4)
    return 440.0 * 2 ** (semis / 12)


def bell(freq, seconds, decay):
    return modes(seconds, [(freq, 1.0, decay), (freq * 2.0, 0.35, decay * 0.6), (freq * 2.76, 0.25, decay * 0.4),
                           (freq * 5.4, 0.08, decay * 0.2)], 0.001)


# ---------------------------------------------------------------- the sounds

def arrow_shot():
    # A bow's release is not a note: the string slaps the limbs and stops dead (a dull, very short thump with a hint
    # of wood), and the arrow hisses away. No sustained pitch (a plucked string read as an electric guitar).
    thump = sweep(0.06, 210, 90, 0.018)
    slap = bandpass(noise(0.04), 420, 1.5) * env(0.04, 0.01)
    wood = modes(0.05, [(720, 0.6, 0.012), (1340, 0.3, 0.008)])
    hiss = highpass(noise(0.16), 2500)
    hiss = onepole(hiss, np.linspace(9000, 3000, len(hiss))) * env(0.16, 0.05, 0.004)
    return finish(mix(thump, slap * 0.8, wood * 0.35, at(hiss * 0.55, 0.008)), 0.7)


def heavy_shot():
    # A heavier draw: a deeper thump, a longer string slap and a broader whoosh.
    thump = sweep(0.09, 160, 60, 0.03)
    slap = bandpass(noise(0.06), 330, 1.3) * env(0.06, 0.015)
    wood = modes(0.07, [(560, 0.6, 0.016), (1080, 0.3, 0.01)])
    whoosh = bandpass(noise(0.26), 1600, 0.7) * np.sin(np.linspace(0, np.pi, n(0.26))) ** 2
    return finish(mix(thump, slap * 0.9, wood * 0.35, at(whoosh * 0.6, 0.01)), 0.8)


def hit():
    thud = sweep(0.12, 110, 45, 0.04)
    flesh = bandpass(noise(0.08), 1100, 1.2) * env(0.08, 0.02)
    crack = highpass(noise(0.015), 2500) * env(0.015, 0.004)
    return finish(mix(thud, flesh * 0.9, crack * 0.4), 0.6)


def crit():
    base = hit() / 0.6
    ping = modes(0.35, [(2350, 0.6, 0.09), (3720, 0.4, 0.06), (5510, 0.2, 0.04)])
    return finish(mix(base, at(ping * 0.5, 0.004)), 0.8)


def kill():
    thump = sweep(0.25, 90, 38, 0.08)
    grains = np.zeros(n(0.3))
    for k in range(6):
        g = bandpass(noise(0.03), rng.uniform(500, 1500), 1.5) * env(0.03, 0.008)
        grains = mix(grains, at(g, rng.uniform(0, 0.12)))
    return finish(reverb(mix(thump * 1.2, grains * 0.6), 0.6, 0.15, 1500), 0.8)


def swing():
    w = bandpass(noise(0.22), 1400, 0.6) * np.sin(np.linspace(0, np.pi, n(0.22))) ** 1.5
    return finish(w, 0.5)


def boom(depth=1.0, seconds=0.9):
    low = sweep(seconds, 85 * depth, 28 * depth, 0.28)
    rumble = onepole(noise(seconds), 260) * env(seconds, 0.3)
    debris = np.zeros(n(seconds))
    for k in range(10):
        g = bandpass(noise(0.04), rng.uniform(600, 1800), 2.0) * env(0.04, 0.01)
        debris = mix(debris, at(g, rng.uniform(0.02, 0.4)))
    return reverb(mix(low * 1.4, rumble * 1.4, debris * 0.15), 1.0, 0.2, 700)


def explosion():
    crack = highpass(noise(0.05), 1500) * env(0.05, 0.012)
    fire = onepole(noise(0.8), np.linspace(1600, 300, n(0.8))) * env(0.8, 0.25)
    return finish(mix(boom(1.1, 1.0), crack * 0.8, fire * 0.9), 0.95)


def hurt():
    thud = sweep(0.16, 130, 60, 0.05)
    x = t(0.24)
    saw = 2 * ((x * np.linspace(165, 115, len(x))) % 1) - 1
    grunt = bandpass(saw, 650, 2.0) * env(0.24, 0.08, 0.01)
    return finish(mix(thud, grunt * 0.6, bandpass(noise(0.06), 900, 1) * env(0.06, 0.02) * 0.5), 0.7)


def drop_common():
    clack = modes(0.15, [(620, 1.0, 0.03), (1130, 0.6, 0.02), (2410, 0.3, 0.012)])
    return finish(mix(clack, onepole(noise(0.05), 1200) * env(0.05, 0.01) * 0.6), 0.5)


def chime(notes, gap, decay, tail, wet):
    y = np.zeros(n(gap * len(notes) + decay * 4))
    for k, name in enumerate(notes):
        y = mix(y, at(bell(note(name), decay * 4, decay), gap * k))
    return reverb(y, tail, wet, 6000)


def drop_magic():
    return finish(chime(["E5", "B5"], 0.07, 0.25, 0.9, 0.3), 0.6)


def drop_rare():
    return finish(chime(["G5", "B5", "D6"], 0.06, 0.3, 1.2, 0.35), 0.7)


def drop_legendary():
    deep = bell(note("C3"), 2.5, 0.9) * 0.8
    chord = mix(*[at(bell(note(nm), 2.0, 0.6) * 0.5, 0.04 * k) for k, nm in enumerate(["C5", "E5", "G5", "C6"])])
    sparkle = np.zeros(n(1.5))
    for k in range(12):
        sparkle = mix(sparkle, at(bell(rng.uniform(2500, 5000), 0.3, 0.06) * 0.15, 0.25 + k * 0.07))
    return finish(reverb(mix(deep, chord, sparkle), 2.5, 0.45, 5000), 0.9)


def gold():
    """A coin purse jiggled (the owner, 2026-10-05: the old bright clinks were "horrible"; "the sound of jiggling a coin
    purse and make it somewhat muted"): two quick shakes, each a cluster of small dull coin knocks inside leather, under
    a soft leather rustle, all low-passed so it sits behind the fight."""
    length = 0.42
    y = np.zeros(n(length))
    for shake, start in enumerate((0.0, 0.15)):
        for k in range(rng.integers(7, 11)):
            f = rng.uniform(1700, 2600)
            knock = modes(0.07, [(f, 1.0, 0.018), (f * 1.47, 0.45, 0.012), (f * 2.13, 0.2, 0.008)])
            when = start + abs(rng.normal(0.03, 0.025))
            y = mix(y, at(knock * rng.uniform(0.25, 0.8) * (1.0 if shake == 0 else 0.75), when, len(y)))
        rustle = bandpass(noise(0.12), 900, 0.8) * np.sin(np.linspace(0, np.pi, n(0.12))) ** 2
        y = mix(y, at(rustle * 0.35 * (1.0 if shake == 0 else 0.7), start, len(y)))
    y = onepole(y, 2800)
    return finish(y, 0.32)


def pickup():
    rustle = bandpass(noise(0.12), 2500, 0.7) * np.sin(np.linspace(0, np.pi, n(0.12)))
    click = modes(0.06, [(900, 1, 0.012), (1800, 0.5, 0.008)])
    return finish(mix(rustle * 0.6, at(click, 0.05)), 0.45)


def level_up():
    notes = ["C4", "E4", "G4", "C5", "E5", "G5"]
    y = mix(*[at(bell(note(nm), 1.6, 0.5) * 0.55, 0.08 * k) for k, nm in enumerate(notes)])
    x = t(1.8)
    pad = np.zeros_like(x)
    for f in (note("C4"), note("G4"), note("C5")):
        for detune in (-0.4, 0.4):
            pad += 2 * ((x * (f + detune)) % 1) - 1
    pad = onepole(pad, 1500) * np.sin(np.linspace(0, np.pi, len(x))) * 0.08
    return finish(reverb(mix(y, at(pad, 0.3)), 2.0, 0.4, 5000), 0.85)


def potion():
    y = np.zeros(n(0.6))
    for k in range(5):
        glug = sweep(0.08, rng.uniform(240, 320), rng.uniform(600, 800), 0.03, 0.7)
        y = mix(y, at(glug * 0.8, 0.02 + k * 0.1))
    return finish(mix(y, onepole(noise(0.6), 500) * env(0.6, 0.2) * 0.15), 0.6)


def forge():
    anvil = modes(1.5, [(1180, 1.0, 0.5), (2870, 0.6, 0.35), (4520, 0.35, 0.2), (6600, 0.15, 0.1)])
    return finish(reverb(mix(anvil, highpass(noise(0.02), 3000) * env(0.02, 0.005)), 1.4, 0.3, 5000), 0.8)


def boss_phase():
    x = t(2.2)
    horn = np.zeros_like(x)
    for f in (55, 55.4, 82.5):
        horn += 2 * ((x * f) % 1) - 1
    horn = onepole(horn, np.linspace(300, 1200, len(x))) * np.sin(np.linspace(0, np.pi, len(x))) ** 0.6
    return finish(reverb(mix(horn * 0.5, boom(0.8, 1.2) * 0.7), 2.0, 0.35, 2500), 0.95)


def death():
    low = boom(0.7, 1.4)
    fall = sweep(1.6, 220, 55, 0.6) * 0.4
    return finish(reverb(mix(low, at(fall, 0.15)), 2.5, 0.4, 2000), 0.9)


def hint():
    return finish(chime(["A5", "E6"], 0.1, 0.2, 0.8, 0.3), 0.45)


def bull_rush():
    w = bandpass(noise(0.4), 700, 0.5) * np.sin(np.linspace(0, np.pi, n(0.4))) ** 1.2
    return finish(mix(w, sweep(0.4, 70, 45, 0.2) * 0.5), 0.75)


SOUNDS = {
    "ArrowShot": arrow_shot,
    "AxeThrow": heavy_shot,
    "Hit": hit,
    "Crit": crit,
    "Kill": kill,
    "Swing": swing,
    "Hew": swing,
    "GroundBreaker": lambda: finish(boom(1.0, 0.9), 0.9),
    "EnemySlam": lambda: finish(boom(0.85, 1.0), 0.9),
    "Explosion": explosion,
    "BullRush": bull_rush,
    "Hurt": hurt,
    "DropCommon": drop_common,
    "DropMagic": drop_magic,
    "DropRare": drop_rare,
    "DropLegendary": drop_legendary,
    "Gold": gold,
    "Pickup": pickup,
    "LevelUp": level_up,
    "Potion": potion,
    "Forge": forge,
    "BossPhase": boss_phase,
    "Death": death,
    "Hint": hint,
}


def write(name, samples):
    os.makedirs(OUT, exist_ok=True)
    data = (np.clip(samples, -1, 1) * 32767).astype(np.int16)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(RATE)
        f.writeframes(data.tobytes())
    print("REPORT %s %.2f s" % (name, len(samples) / RATE))


wanted = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
for name, make in SOUNDS.items():
    if not wanted or name in wanted:
        write(name, make())
