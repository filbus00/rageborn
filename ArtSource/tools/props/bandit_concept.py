# A bandit designed in Blender for a clear silhouette (2026-11-03, the owner: "give a go designing the bandit in
# blender ... Focus on making the silhouette really clear. Be creative and do something different from the current
# bandit"): the Sack Thief, a hunched little robber under a loot sack on his back, a tall crimson hood whose point flops
# back, a wide cleaver held high. Three big shapes that read at 92 px: the sack's lump, the hood's spike, the cleaver.
#   Blender -b -P ArtSource/tools/props/bandit_concept.py -- <out folder> [rest]
# Writes the concept pictures (concept_kit.present), or with "rest" the A-pose figure for the rig in
# ArtSource/models/forest/sack_thief (its rig.json beside it).
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from concept_kit import *

# After the owner's reference (2026-11-03, ArtSource/pixel/ai_exchange/bandit_concept/reference/sack_thief_ref.jpeg:
# Gemini's painting of the first renders): a stitched burlap ragdoll of a robber, an iron skull mask with glowing eyes,
# a red cloth hood and a jagged jester's collar hung with brass bells, a rusty cleaver with a key ring, a leather belt
# with pouches and a chain, tall rust-brown boots with folded cuffs.
# Rich, saturated colours (the owner, 2026-11-03: "make the colors richer").
COLOURS.update({
    "burlap": (0.42, 0.21, 0.08), "hood": (0.58, 0.035, 0.02), "hood_dark": (0.3, 0.02, 0.015),
    "leather": (0.22, 0.075, 0.022), "boot": (0.4, 0.1, 0.02), "cloth": (0.62, 0.46, 0.2),
    "sack": (0.6, 0.44, 0.22), "rope": (0.42, 0.26, 0.08), "stitch": (0.1, 0.04, 0.015),
    "skull": (0.085, 0.085, 0.1), "teeth": (0.92, 0.8, 0.52), "steel": (0.52, 0.45, 0.38), "steel_dark": (0.2, 0.18, 0.17),
    "brass": (0.95, 0.58, 0.1), "gold": (1.0, 0.68, 0.08), "eye": (1.0, 0.85, 0.45), "void": (0.02, 0.01, 0.01),
    "wood": (0.36, 0.14, 0.035), "bone": (0.9, 0.8, 0.58),
})
FABRIC.update({"burlap", "hood", "hood_dark", "cloth", "sack", "rope", "leather", "boot"})
METAL.update({"steel", "steel_dark", "brass", "gold", "skull"})
# The soft parts (body, limbs, boots, hood, sack) are metaballs, so they flow together (the owner: "smooth out the shapes").
SOFT.update({"burlap", "hood", "sack", "boot"})


# The figure faces -Y; his right is -X. Hunched: his chest pushed forward and down, his head low before his shoulders.
# Legs: burlap thighs and shins, tall rust boots from below the knee with big folded cuffs, round toes.
for side in (-1, 1):
    hip = Vector((0.12 * side, 0.02, 0.78))
    knee = Vector((0.17 * side, -0.05, 0.44))
    ankle = Vector((0.16 * side, 0.0, 0.1))
    limb("thigh%d" % side, hip, knee, 0.1, 0.08, "burlap")
    limb("shin%d" % side, knee, knee.lerp(ankle, 0.35), 0.08, 0.075, "burlap")
    limb("boot_shaft%d" % side, knee.lerp(ankle, 0.3), ankle, 0.085, 0.08, "boot")
    ball("boot%d" % side, (0.16 * side, -0.08, 0.07), (0.1, 0.16, 0.075), "boot")
    box("boot_cuff%d" % side, (0.16 * side, -0.02, 0.2), (0.2, 0.2, 0.09), "leather", rot=(0.08, 0, 0))
    stitches(knee + Vector((0, -0.08, 0.0)), knee + Vector((0, -0.07, -0.08)), 3)
    stitches(hip.lerp(knee, 0.4) + Vector((0.06 * side, -0.08, 0)), hip.lerp(knee, 0.8) + Vector((0.07 * side, -0.07, 0)), 4)

# Body: a narrow burlap trunk, hunched, a seam stitched down the front of the chest.
ball("belly", (0, -0.04, 0.9), (0.17, 0.15, 0.16), "burlap")
ball("chest", (0, -0.08, 1.1), (0.23, 0.18, 0.2), "burlap", rot=(0.45, 0, 0))
stitches((0.04, -0.27, 1.13), (0.02, -0.2, 0.9), 8, size=0.014)
stitches((-0.15, -0.22, 1.18), (-0.21, -0.18, 1.05), 3)

# The belt: thick leather, a brass buckle, two pouches with flaps, a chain at his hip with a coin and a little bone.
box("belt", (0, -0.04, 0.82), (0.42, 0.36, 0.09), "leather")
box("buckle", (0.04, -0.225, 0.82), (0.08, 0.025, 0.07), "brass")
for side, x in ((-1, -0.2), (1, 0.12)):
    box("pouch%d" % side, (x, -0.2, 0.74), (0.11, 0.07, 0.13), "leather", rot=(0, 0, 0.3 * side))
    box("pouch_flap%d" % side, (x, -0.24, 0.79), (0.12, 0.03, 0.06), "leather", rot=(0.2, 0, 0.3 * side))
links = 6
for k in range(links):
    p = Vector((0.2, -0.17, 0.8)) + Vector((0.01 * k, -0.005 * k, -0.035 * k))
    ring("chain%d" % k, p, 0.016, 0.004, "steel_dark", rot=(0, math.pi / 2 * (k % 2), 0))
end = Vector((0.2, -0.17, 0.8)) + Vector((0.01 * links, -0.005 * links, -0.035 * links))
box("chain_bone", end + Vector((0, 0, -0.04)), (0.016, 0.016, 0.07), "bone")
for dz in (0.0, -0.08):
    ball("chain_bone_knob", end + Vector((0, 0, dz)), (0.016, 0.016, 0.012), "bone", seg=10)
ring("chain_coin", (0.23, -0.2, 0.8), 0.025, 0.008, "gold", rot=(math.pi / 2, 0, 0))
# The loincloth: a pale cloth panel before him, a red-striped one at his side.
box("loin", (0.0, -0.17, 0.66), (0.2, 0.02, 0.24), "cloth", rot=(0.12, 0, 0))
box("loin_side", (-0.12, -0.14, 0.67), (0.1, 0.02, 0.2), "hood_dark", rot=(0.12, 0, 0.5))

# The head, low and forward: an iron skull mask jutting from the hood, glowing eyes in deep sockets, a row of teeth.
head = Vector((0, -0.24, 1.3))
ball("hood_cowl", head + Vector((0, 0.06, 0.03)), (0.2, 0.2, 0.21), "hood")
ball("cranium", head + Vector((0, -0.11, 0.03)), (0.1, 0.09, 0.1), "skull")
ball("brow", head + Vector((0, -0.17, 0.045)), (0.095, 0.04, 0.03), "skull")
ball("muzzle", head + Vector((0, -0.16, -0.04)), (0.07, 0.065, 0.05), "skull")
for side in (-1, 1):
    ball("socket%d" % side, head + Vector((0.04 * side, -0.2, 0.012)), (0.038, 0.022, 0.034), "void", seg=12)
    ball("eye%d" % side, head + Vector((0.04 * side, -0.212, 0.012)), (0.022, 0.012, 0.019), "eye", seg=10)
    ball("cheek%d" % side, head + Vector((0.07 * side, -0.17, -0.03)), (0.028, 0.03, 0.022), "skull", seg=10)
ball("nose", head + Vector((0, -0.208, -0.022)), (0.012, 0.012, 0.016), "void", seg=8)
for k in range(-3, 4):
    box("tooth%d" % k, head + Vector((0.017 * k, -0.208 + 0.004 * abs(k), -0.06)), (0.014, 0.012, 0.026), "teeth")
# The hood's spike: tall, flopping back at the top.
hood_tail([head + Vector(p) for p in ((0, 0.07, 0.1), (0, 0.1, 0.3), (-0.02, 0.14, 0.5), (-0.06, 0.22, 0.64),
                                       (-0.14, 0.32, 0.68), (-0.22, 0.4, 0.6))], [0.17, 0.14, 0.11, 0.08, 0.05, 0.025], "hood")
stitches(head + Vector((0.12, 0.12, 0.25)), head + Vector((0.07, 0.17, 0.45)), 4)
# A jester's collar: red cloth points round his neck, a brass bell on each.
collar = Vector((0, -0.07, 1.2))
ball("collar", collar, (0.24, 0.2, 0.06), "hood")
for k in range(7):
    a = -math.pi / 2 + (k - 3) * 0.42
    at = collar + Vector((math.cos(a) * 0.22, math.sin(a) * 0.18, -0.01))
    out = Vector((math.cos(a) * 0.5, math.sin(a) * 0.5, -0.85))
    flap(at, out, 0.15, 0.06, "hood")
    ball("bell%d" % k, at + out.normalized() * 0.16, (0.026, 0.026, 0.026), "brass", seg=12)

if REST:
    # The A-pose: both arms down and out, fists empty; the key ring hangs from his belt instead.
    for side, tag in ((-1, "r"), (1, "l")):
        sh = Vector((0.27 * side, -0.05, 1.2))
        el = Vector((0.47 * side, -0.07, 1.0))
        wr = Vector((0.62 * side, -0.09, 0.82))
        limb("upper_" + tag, sh, el, 0.075, 0.065, "burlap")
        limb("fore_" + tag, el, wr, 0.065, 0.055, "burlap")
        ball("fist_" + tag, wr + (wr - el).normalized() * 0.045, (0.07, 0.07, 0.07), "burlap")
        box("bracer_" + tag, el.lerp(wr, 0.7), (0.12, 0.12, 0.08), "leather", rot=(0, -0.7 * side, 0))
        stitches(sh + Vector((0, -0.075, -0.02)), el + Vector((0, -0.07, 0)), 4)
        if tag == "l":
            wr_l = wr
    key_at = Vector((-0.2, -0.16, 0.74))
    ring("key_ring", key_at, 0.035, 0.006, "brass", rot=(math.pi / 2, 0, 0))
    box("key_shaft", key_at + Vector((0, -0.01, -0.09)), (0.012, 0.012, 0.13), "brass")
    box("key_bit", key_at + Vector((0.02, -0.01, -0.15)), (0.03, 0.01, 0.025), "brass")
else:
    # Right arm: raised high, the cleaver over his head.
    sh_r = Vector((-0.27, -0.05, 1.2))
    el_r = Vector((-0.44, -0.12, 1.4))
    wr_r = Vector((-0.38, -0.2, 1.66))
    limb("upper_r", sh_r, el_r, 0.075, 0.065, "burlap")
    limb("fore_r", el_r, wr_r, 0.065, 0.055, "burlap")
    ball("fist_r", wr_r, (0.07, 0.07, 0.07), "burlap")
    box("bracer_r", el_r.lerp(wr_r, 0.7), (0.13, 0.13, 0.08), "leather", rot=(0.35, 0.25, 0))
    stitches(sh_r + Vector((0, -0.07, 0.03)), el_r + Vector((0, -0.07, 0)), 4)
    # The cleaver, built in its own frame and turned into his fist: the handle along its x through the fist (the grip at
    # the origin), the broad rusty blade on the handle's far end, its spine up, a hole and a skull on it, brass rivets; an
    # iron ring of keys on the handle's near end.
    TILT = math.radians(-38)
    turn = Euler((0, TILT, 0)).to_matrix()


    def cl(p):
        return wr_r + turn @ Vector(p)


    box("handle", cl((0.09, 0, 0)), (0.44, 0.06, 0.07), "wood", rot=(0, TILT, 0))
    box("ferrule", cl((0.27, 0, 0)), (0.04, 0.075, 0.085), "steel_dark", rot=(0, TILT, 0))
    blade_rot = (0, TILT, 0)
    box("blade", cl((0.52, 0, 0.08)), (0.46, 0.03, 0.3), "steel", rot=blade_rot)
    box("blade_spine", cl((0.52, 0, 0.225)), (0.47, 0.04, 0.03), "steel_dark", rot=blade_rot)
    ball("blade_hole", cl((0.68, 0, 0.16)), (0.028, 0.04, 0.028), "void", seg=12)
    ball("blade_skull", cl((0.49, -0.018, 0.07)), (0.05, 0.008, 0.055), "steel_dark", seg=12)
    for x in (0.31, 0.36):
        ball("rivet", cl((x, -0.03, 0.0)), (0.012, 0.01, 0.012), "brass", seg=8)
    ring("key_ring", cl((-0.16, 0, -0.02)), 0.04, 0.006, "brass", rot=(math.pi / 2, 0, 0.3))
    key_top = cl((-0.18, 0, -0.06)) + Vector((0, 0, -0.03))
    ring("key_bow", key_top, 0.025, 0.006, "brass", rot=(math.pi / 2, 0, 0))
    box("key_shaft", key_top + Vector((0, 0, -0.1)), (0.012, 0.012, 0.15), "brass")
    box("key_bit", key_top + Vector((0.02, 0, -0.16)), (0.03, 0.01, 0.025), "brass")

    # Left arm: over his shoulder, gripping the sack's neck.
    sh_l = Vector((0.27, -0.05, 1.2))
    el_l = Vector((0.42, -0.22, 1.16))
    wr_l = Vector((0.25, -0.16, 1.44))
    limb("upper_l", sh_l, el_l, 0.075, 0.065, "burlap")
    limb("fore_l", el_l, wr_l, 0.065, 0.055, "burlap")
    box("bracer_l", el_l.lerp(wr_l, 0.7), (0.13, 0.13, 0.08), "leather", rot=(0.6, -0.5, 0))
    ball("fist_l", wr_l, (0.07, 0.07, 0.07), "burlap")

# The loot sack, olive burlap, tied at the neck, a patch and a seam.
S = 0.75
sack = Vector((0.1, 0.33, 1.22))
ball("sack", sack, (0.46 * S, 0.4 * S, 0.48 * S), "sack", rot=(0.2, 0.3, 0))
ball("sack_lump1", sack + Vector((0.22, 0.05, 0.25)) * S, (0.22 * S, 0.2 * S, 0.2 * S), "sack")
ball("sack_lump2", sack + Vector((-0.25, 0.1, -0.2)) * S, (0.2 * S, 0.2 * S, 0.18 * S), "sack")
box("sack_patch", sack + Vector((-0.2, -0.2, 0.18)) * S, (0.1, 0.02, 0.1), "cloth", rot=(0.3, 0.2, 0.6))
stitches(sack + Vector((0.3, -0.22, 0.2)) * S, sack + Vector((0.35, -0.12, -0.25)) * S, 6)
if REST:
    # Tied on his back: a short knotted neck on top, a rope strap over his left shoulder and across his chest.
    neck_top = sack + Vector((0.05, -0.05, 0.42)) * S
    limb("sack_neck", sack + Vector((0.03, -0.02, 0.3)) * S, neck_top, 0.08, 0.05, "sack")
    ring("sack_tie", neck_top + Vector((0, 0, -0.03)), 0.055, 0.022, "leather", rot=(0.2, 0, 0))
    limb("strap", Vector((0.2, 0.12, 1.33)), Vector((0.2, -0.22, 1.27)), 0.02, 0.02, "rope")
    limb("strap_front", Vector((0.2, -0.24, 1.27)), Vector((-0.17, -0.22, 0.95)), 0.02, 0.02, "rope")
else:
    limb("sack_neck", sack + Vector((0.1, -0.2, 0.35)) * S, wr_l + Vector((0, 0.03, 0.02)), 0.08, 0.05, "sack")
    ring("sack_tie", wr_l + Vector((0.0, 0.05, -0.02)), 0.05, 0.014, "rope", rot=(0.9, 0.3, 0))

present("sack_thief")
