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


def J(lo, hi):
    """A random number in a range (each variant of a bank is its own draw, 2026-11-18)."""
    return float(rng.uniform(lo, hi))


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

def arrow_shot(kind=None):
    # A bow's release is not a note: the string slaps the limbs and stops dead (a dull, very short thump with a hint
    # of wood), and the arrow hisses away. No sustained pitch (a plucked string read as an electric guitar).
    # Three characters (2026-11-18, the owner: the shot "gets a bit repetitive", "more dynamic and varied"): a sharp slap
    # of a light bow, a deep thrum of heavy limbs, a snappy click of wood; each draw of the parameters is its own sound.
    k = int(rng.integers(0, 3)) if kind is None else kind % 3
    f0 = J(190, 270) if k != 1 else J(125, 180)
    thump = sweep(J(0.05, 0.08), f0, f0 * J(0.4, 0.55), J(0.014, 0.024))
    slap = bandpass(noise(0.05), J(360, 560) if k != 1 else J(250, 380), J(1.2, 2.0)) * env(0.05, J(0.008, 0.014))
    # The wood of the limbs is a short band of noise, not tuned partials (they rang like a woodblock, 2026-11-18).
    body = bandpass(noise(0.04), J(900, 1500), 0.9) * env(0.04, J(0.008, 0.014))
    length = J(0.12, 0.22)
    hiss = highpass(noise(length), J(1500, 2200))
    hiss = onepole(hiss, np.linspace(J(3500, 5500), J(1500, 2500), len(hiss))) * env(length, J(0.035, 0.07), 0.004)
    parts = [thump * (1.0 if k != 2 else 0.7), slap * 0.8, body * (0.5 if k != 2 else 0.8), at(hiss * J(0.3, 0.5), J(0.004, 0.014))]
    return finish(mix(*parts), 0.7)


def heavy_shot():
    # A heavier draw: a deeper thump, a longer string slap and a broader whoosh.
    thump = sweep(0.09, 160, 60, 0.03)
    slap = bandpass(noise(0.06), 330, 1.3) * env(0.06, 0.015)
    wood = modes(0.07, [(560, 0.6, 0.016), (1080, 0.3, 0.01)])
    whoosh = bandpass(noise(0.26), 1600, 0.7) * np.sin(np.linspace(0, np.pi, n(0.26))) ** 2
    return finish(mix(thump, slap * 0.9, wood * 0.35, at(whoosh * 0.6, 0.01)), 0.8)


def soften(x, cutoff=2200):
    """The top taken off (the first rework's grains, clinks and clicks sounded like a box of pens)."""
    return onepole(x, cutoff)


def hit(kind=None):
    # An arrow into a target (2026-11-18, the owner: "make a thudding sound of an arrow hitting a target"): a thump with a
    # muffled body and a short dull thwack, nothing high and nothing tuned. Made for a phone's speaker, which gives
    # almost nothing below 150 Hz: the thump falls from about 250 to 90 Hz and the body is mostly 300 to 1200 Hz noise.
    # Five kinds of thud, each drawn a little differently every time: a body thud, a heavy one, a straw target's soft
    # thup, leather, and a light one.
    k = int(rng.integers(0, 5)) if kind is None else kind % 5
    # The thwack carries the thud on a phone's speaker: a dull band of noise at 450 to 750 Hz.
    thwack = bandpass(noise(0.035), J(450, 750), 0.8) * env(0.035, 0.012) * 1.8
    tip = bandpass(noise(0.006), J(900, 1400), 0.7) * env(0.006, 0.002)
    if k == 0:
        thud = sweep(0.12, J(210, 270), J(85, 110), J(0.035, 0.05))
        body = onepole(noise(0.06), J(700, 1100)) * env(0.06, 0.02)
        return finish(soften(mix(thud * 0.8, body * 0.9, thwack * 0.7, tip * 0.2)), 0.6)
    if k == 1:
        thud = sweep(0.14, J(200, 250), J(85, 105), J(0.04, 0.055))
        body = onepole(noise(0.10), J(600, 900)) * env(0.10, 0.035)
        return finish(soften(mix(thud * 0.6, body * 1.3, thwack * 0.6, tip * 0.15)), 0.62)
    if k == 2:
        body = onepole(noise(0.08), J(900, 1300)) * env(0.08, 0.025)
        thup = sweep(0.07, J(240, 300), J(105, 125), 0.03)
        return finish(soften(mix(body * 1.0, thup * 0.7, thwack * 0.6, tip * 0.15)), 0.55)
    if k == 3:
        thud = sweep(0.10, J(230, 280), J(100, 125), 0.04)
        whump = bandpass(noise(0.05), J(400, 600), 0.7) * env(0.05, 0.018)
        return finish(soften(mix(thud * 0.5, whump * 1.4, tip * 0.15)), 0.6)
    thud = sweep(0.09, J(240, 300), J(105, 130), 0.03)
    body = onepole(noise(0.05), 1100) * env(0.05, 0.015)
    return finish(soften(mix(thud * 0.6, body * 1.1, thwack * 0.6)), 0.55)


def crit(kind=None):
    # A heavier thud with a sharper crack, still low: a deeper drop under it and a short dull snap.
    base = hit((0, 1)[int(rng.integers(0, 2))] if kind is None else (0, 1)[kind % 2]) / 0.6
    boom = sweep(J(0.16, 0.22), J(280, 340), J(90, 110), J(0.05, 0.08))
    snap = bandpass(noise(0.04), J(800, 1200), 1.0) * env(0.04, J(0.01, 0.016))
    return finish(soften(mix(base, boom * J(0.5, 0.8), at(snap * J(0.6, 0.9), 0.002)), 3000), 0.8)


def kill(kind=None):
    # A last heavy thud and a low rolling out, with a second, softer one after it in some.
    thump = sweep(J(0.18, 0.26), J(220, 280), J(85, 105), J(0.06, 0.09))
    rumble = onepole(noise(0.3), J(500, 900)) * env(0.3, J(0.07, 0.11), 0.004)
    parts = [thump * J(0.6, 0.8), rumble * J(1.0, 1.4)]
    if kind is not None and kind % 2 == 1:
        parts.append(at(sweep(0.12, J(180, 220), 80, 0.04) * 0.5, J(0.09, 0.14)))
    return finish(soften(reverb(mix(*parts), J(0.3, 0.5), J(0.08, 0.14), 1500), 2500), 0.8)


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


def hurt(kind=None):
    # The player's grunt, a different voice each time (2026-11-18).
    thud = sweep(0.16, J(110, 150), J(50, 70), 0.05)
    x = t(0.24)
    saw = 2 * ((x * np.linspace(J(140, 190), J(95, 130), len(x))) % 1) - 1
    grunt = bandpass(saw, J(550, 800), J(1.6, 2.4)) * env(0.24, J(0.06, 0.1), 0.01)
    return finish(mix(thud, grunt * 0.6, bandpass(noise(0.06), J(750, 1050), 1) * env(0.06, 0.02) * 0.5), 0.7)


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
    """Coins settling in a leather purse (the owner, 2026-10-05: the clinks were "really bad and annoying"): no pitched
    tones at all, since pure partials read as beeps. Three or four coins, each a tiny burst of noise rung through two
    narrow resonances (a dull coin's knock), close together, over a soft low thump of the purse, all low-passed at
    2.4 kHz and short (about 0.2 s), so a stream of pickups stays in the background."""
    length = 0.24
    y = np.zeros(n(length))
    thump = sweep(0.06, 160, 90, 0.025) * 0.3
    y = mix(y, thump)
    when = 0.0
    for k in range(rng.integers(3, 5)):
        burst = noise(0.004) * np.linspace(1, 0, n(0.004))
        burst = np.concatenate([burst, np.zeros(n(0.05))])
        f = rng.uniform(1900, 2600)
        ring = bandpass(burst, f, 12) + bandpass(burst, f * 1.53, 10) * 0.6
        ring *= np.exp(-t(len(ring) / RATE) / rng.uniform(0.012, 0.02))
        ring /= np.max(np.abs(ring)) + 1e-9
        y = mix(y, at(ring * rng.uniform(0.5, 1.0) * (0.85 ** k), when, len(y)))
        when += rng.uniform(0.022, 0.045)
    y = onepole(onepole(y, 2400), 2400)
    return finish(y, 0.28)


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


# ---------------------------------------------------------------- the elemental arrows (2026-10-18, the owner: "make the
# various boons like lightning arrow, ice arrow, fire arrow etc have more distinct visual effects and sounds")

def crackles(seconds, count, start=0.0):
    """Sparse sharp clicks, as wood or embers crackling."""
    y = np.zeros(n(seconds))
    for _ in range(count):
        i = n(rng.uniform(start, seconds * 0.9))
        k = n(rng.uniform(0.002, 0.006))
        y[i:i + k] += rng.uniform(-1, 1, len(y[i:i + k])) * np.exp(-np.arange(len(y[i:i + k])) / max(1, k / 4)) * rng.uniform(0.4, 1.0)
    return highpass(y, 1500)


def fire_hit():
    # Catching fire: a soft rising whoosh of flame and a few crackles.
    whoosh = bandpass(noise(0.45), 900, 0.7) * env(0.45, 0.16, 0.03)
    body = onepole(noise(0.45), 500) * env(0.45, 0.12, 0.01) * 0.6
    return finish(reverb(mix(whoosh, body, crackles(0.45, 7, 0.03) * 0.5), 0.6, 0.15, 3500), 0.55)


def frost_hit():
    # Ice taking hold: a brittle crack and a short high tinkle of crystal.
    crack = highpass(noise(0.05), 3000) * env(0.05, 0.008)
    tinkle = modes(0.4, [(2850, 0.6, 0.09), (3910, 0.45, 0.07), (5230, 0.3, 0.05), (6900, 0.2, 0.035)])
    return finish(reverb(mix(crack, at(tinkle, 0.008)), 0.5, 0.2, 7000), 0.45)


def freeze():
    # Frozen solid: a sharp ice crack and crystals ringing down.
    crack = highpass(noise(0.08), 2200) * env(0.08, 0.015)
    thud = sweep(0.2, 260, 120, 0.05) * 0.4
    rings = mix(*[at(bell(f, 0.7, 0.18) * 0.35, d) for f, d in ((3100, 0.01), (2600, 0.05), (2200, 0.1), (1850, 0.15))])
    return finish(reverb(mix(crack, thud, rings), 0.9, 0.3, 6000), 0.6)


def zap():
    # An electric arc: a buzzing, jittering square wave over hiss, cut short.
    x = t(0.2)
    freq = 110 * (1 + 0.35 * rng.uniform(-1, 1, len(x)))
    phase = 2 * np.pi * np.cumsum(onepole(freq, 400)) / RATE
    buzz = np.sign(np.sin(phase)) * 0.5 + np.sign(np.sin(phase * 3.01)) * 0.25
    hiss = highpass(noise(0.2), 4000) * 0.6
    gate = (rng.uniform(0, 1, len(x)) > 0.25).astype(float)
    gate = onepole(gate, 900)
    return finish(highpass((buzz + hiss) * gate * env(0.2, 0.07, 0.002), 300), 0.42)


def thunder():
    # A thunderbolt striking close: a sharp splitting crack and a low rolling rumble.
    crack = highpass(noise(0.12), 1500) * env(0.12, 0.025)
    snap = zap() * 0.6
    rumble = onepole(noise(1.4), 180) * env(1.4, 0.45, 0.05) * 3.0
    return finish(reverb(mix(crack, snap, at(rumble, 0.03)), 1.4, 0.35, 2500), 0.8)


SOUNDS = {
    "FireHit": fire_hit,
    "FrostHit": frost_hit,
    "Freeze": freeze,
    "Zap": zap,
    "Thunder": thunder,
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


# Sounds that play often come in banks (2026-11-18): the base file and Name_1 ... Name_N, each its own draw of the
# generator (and its own kind where there are kinds); Sfx picks one at random, never the same twice running.
BANKS = {"ArrowShot": 7, "Hit": 10, "Crit": 5, "Kill": 5, "Hurt": 4}

wanted = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
# The owner's own recordings (import_owner_sounds.py, 2026-10-12) are only remade here when asked for by name.
OWNER = {"Gold", "Explosion", "FrostNova"}
for name, make in SOUNDS.items():
    if (not wanted and name not in OWNER) or name in wanted:
        write(name, make())
        for k in range(1, BANKS.get(name, 0) + 1):
            rng = np.random.default_rng(1000 + 31 * k + sum(map(ord, name)))
            write("%s_%d" % (name, k), make(k - 1))
