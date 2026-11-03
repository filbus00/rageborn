# A bandit designed in Blender for a clear silhouette (2026-11-03, the owner: "give a go designing the bandit in
# blender ... Focus on making the silhouette really clear. Be creative and do something different from the current
# bandit"). A concept only, no rig: the Sack Thief, a hunched little robber under a huge loot sack slung over his
# shoulder, a tall crimson hood whose point flops back into a long tail, a bone half-mask, and a wide cleaver held high.
# Three big shapes that read at 92 px: the sack's round lump, the hood's spike, the cleaver's slab.
#   Blender -b -P ArtSource/tools/props/bandit_concept.py -- <out folder>
# Writes front.png, three_quarter.png, side.png and game.png (from the game's camera, small, on the forest's green).
import bpy, bmesh, sys, os, math
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
from mathutils import Vector, Euler, Matrix

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = os.path.abspath(args[0] if args else "bandit_concept")
# "rest": the figure for the rig (the owner: "rig it and put it in game"): standing in an A-pose, empty-handed (the
# cleaver is a held piece, enemy_weapons.py), the sack tied on his back; written as a textured GLB with its rig.json.
REST = len(args) > 1 and args[1] == "rest"
os.makedirs(OUT, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

# After the owner's reference (2026-11-03, ArtSource/pixel/ai_exchange/bandit_concept/reference/sack_thief_ref.jpeg:
# Gemini's painting of the first renders): a stitched burlap ragdoll of a robber, an iron skull mask with glowing eyes,
# a red cloth hood and a jagged jester's collar hung with brass bells, a rusty cleaver with a key ring, a leather belt
# with pouches and a chain, tall rust-brown boots with folded cuffs.
# Rich, saturated colours (the owner, 2026-11-03: "make the colors richer").
COLOURS = {
    "burlap": (0.42, 0.21, 0.08), "hood": (0.58, 0.035, 0.02), "hood_dark": (0.3, 0.02, 0.015),
    "leather": (0.22, 0.075, 0.022), "boot": (0.4, 0.1, 0.02), "cloth": (0.62, 0.46, 0.2),
    "sack": (0.6, 0.44, 0.22), "rope": (0.42, 0.26, 0.08), "stitch": (0.1, 0.04, 0.015),
    "skull": (0.085, 0.085, 0.1), "teeth": (0.92, 0.8, 0.52), "steel": (0.52, 0.45, 0.38), "steel_dark": (0.2, 0.18, 0.17),
    "brass": (0.95, 0.58, 0.1), "gold": (1.0, 0.68, 0.08), "eye": (1.0, 0.85, 0.45), "void": (0.02, 0.01, 0.01),
    "wood": (0.36, 0.14, 0.035), "bone": (0.9, 0.8, 0.58),
}
FABRIC = {"burlap", "hood", "hood_dark", "cloth", "sack", "rope", "leather", "boot"}
METAL = {"steel", "steel_dark", "brass", "gold", "skull"}
materials = {}


def mat(name):
    """A material: fabrics with a fine weave bump and blotchy wear, metals with rust and wear in their roughness."""
    if name in materials:
        return materials[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    base = (*COLOURS[name], 1)
    bsdf.inputs["Roughness"].default_value = 0.85
    coord = nt.nodes.new("ShaderNodeTexCoord")
    if name in FABRIC or name in METAL:
        blotch = nt.nodes.new("ShaderNodeTexNoise")
        blotch.inputs["Scale"].default_value = 7.0
        blotch.inputs["Detail"].default_value = 4.0
        nt.links.new(coord.outputs["Object"], blotch.inputs["Vector"])
        mix = nt.nodes.new("ShaderNodeMix")
        mix.data_type = "RGBA"
        dark = [c * 0.72 for c in base[:3]] + [1]
        light = [min(1, c * 1.18) for c in base[:3]] + [1]
        if name == "steel":
            dark = (0.36, 0.17, 0.08, 1)  # rust
        mix.inputs[6].default_value = dark
        mix.inputs[7].default_value = light
        nt.links.new(blotch.outputs["Fac"], mix.inputs["Factor"])
        nt.links.new(mix.outputs[2], bsdf.inputs["Base Color"])
    else:
        bsdf.inputs["Base Color"].default_value = base
    if name in FABRIC:
        weave = nt.nodes.new("ShaderNodeTexNoise")
        weave.inputs["Scale"].default_value = 260.0 if name != "leather" else 90.0
        weave.inputs["Detail"].default_value = 2.0
        nt.links.new(coord.outputs["Object"], weave.inputs["Vector"])
        bump = nt.nodes.new("ShaderNodeBump")
        bump.inputs["Strength"].default_value = 0.35
        nt.links.new(weave.outputs["Fac"], bump.inputs["Height"])
        nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    if name in METAL:
        bsdf.inputs["Metallic"].default_value = 0.85 if name != "skull" else 0.6
        bsdf.inputs["Roughness"].default_value = 0.45
    if name == "teeth" or name == "bone":
        bsdf.inputs["Roughness"].default_value = 0.6
    if name == "eye":
        bsdf.inputs["Emission Color"].default_value = base
        bsdf.inputs["Emission Strength"].default_value = 12
    materials[name] = m
    return m


def finish(obj, colour, smooth=True):
    obj.data.materials.append(mat(colour))
    if smooth:
        for p in obj.data.polygons:
            p.use_smooth = True
    return obj


# The soft parts (body, limbs, boots, hood, sack) are metaballs, one object a colour, so the parts of a colour flow into
# each other with no seams or beads at the joints (the owner: "see if you can smooth out the shapes").
SOFT = {"burlap", "hood", "sack", "boot"}
blobs = {}
# A metaball's surface lies inside its radius; this brings a ball's surface out to the size asked.
GROW = 1.42


def blob(colour):
    if colour not in blobs:
        data = bpy.data.metaballs.new(colour)
        data.resolution = 0.012
        data.render_resolution = 0.006
        data.threshold = 0.6
        o = bpy.data.objects.new("soft_" + colour, data)
        scene.collection.objects.link(o)
        data.materials.append(mat(colour))
        blobs[colour] = data
    return blobs[colour]


def ball(name, at, size, colour, rot=(0, 0, 0), seg=24):
    if colour in SOFT:
        e = blob(colour).elements.new()
        e.type = "ELLIPSOID"
        e.co = at
        big = max(size)
        e.radius = big * GROW
        e.size_x, e.size_y, e.size_z = (c / big for c in size)
        e.rotation = Euler(rot).to_quaternion()
        e.stiffness = 2.0
        return e
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=seg // 2, location=at, rotation=rot)
    o = bpy.context.object
    o.name = name
    o.scale = size
    return finish(o, colour)


def limb(name, a, b, r0, r1, colour, seg=14):
    """A tapered tube from a to b: soft balls along it, close enough to flow into one smooth shape."""
    a, b = Vector(a), Vector(b)
    if colour not in SOFT:
        d = b - a
        bpy.ops.mesh.primitive_cylinder_add(vertices=seg, radius=r0, depth=d.length, location=(a + b) / 2)
        o = bpy.context.object
        o.rotation_mode = "QUATERNION"
        o.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(d.normalized())
        return finish(o, colour)
    n = max(2, int((b - a).length / (min(r0, r1) * 0.6)) + 1)
    for i in range(n):
        t = i / (n - 1)
        r = r0 + (r1 - r0) * t
        ball(name, a.lerp(b, t), (r, r, r), colour)


def box(name, at, size, colour, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=at, rotation=rot)
    o = bpy.context.object
    o.name = name
    o.scale = [s / 2 for s in size]
    return finish(o, colour, smooth=False)


def hood_tail(points, radii, colour):
    """A curved cone: rings along a path, each its radius, closed at the tip."""
    bm = bmesh.new()
    seg = 16
    rings = []
    for i, (p, r) in enumerate(zip(points, radii)):
        p = Vector(p)
        nxt = Vector(points[min(i + 1, len(points) - 1)])
        prv = Vector(points[max(i - 1, 0)])
        t = (nxt - prv).normalized()
        side = t.cross(Vector((1, 0, 0))).normalized()
        if side.length < 0.1:
            side = Vector((0, 1, 0))
        up = t.cross(side).normalized()
        ring = [bm.verts.new(p + (side * math.cos(2 * math.pi * k / seg) + up * math.sin(2 * math.pi * k / seg)) * r)
                for k in range(seg)]
        rings.append(ring)
    for a, b in zip(rings, rings[1:]):
        for k in range(seg):
            bm.faces.new([a[k], a[(k + 1) % seg], b[(k + 1) % seg], b[k]])
    bm.faces.new(list(reversed(rings[0])))
    tip = bm.verts.new(Vector(points[-1]) + (Vector(points[-1]) - Vector(points[-2])).normalized() * 0.02)
    for k in range(seg):
        bm.faces.new([rings[-1][k], rings[-1][(k + 1) % seg], tip])
    mesh = bpy.data.meshes.new("hood_tail")
    bm.to_mesh(mesh)
    o = bpy.data.objects.new("hood_tail", mesh)
    scene.collection.objects.link(o)
    return finish(o, colour)


def stitches(a, b, n, colour="stitch", size=0.012):
    """A row of little cross stitches from a to b (on a surface), the ragdoll's seams."""
    a, b = Vector(a), Vector(b)
    d = (b - a).normalized()
    for i in range(n):
        p = a.lerp(b, (i + 0.5) / n)
        for turn in (0.785, -0.785):
            box("stitch", p, (size * 0.35, size * 0.35, size * 2.2), colour, rot=(0, turn, math.atan2(d.y, d.x)))


def flap(at, direction, length, width, colour):
    """A triangular cloth point hanging from at toward direction."""
    direction = Vector(direction).normalized()
    bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=width, radius2=0, depth=length, location=Vector(at) + direction * length / 2)
    o = bpy.context.object
    o.rotation_mode = "QUATERNION"
    o.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(direction)
    o.name = "flap"
    o.scale = (1, 0.25, 1)
    return finish(o, colour, smooth=False)


def ring(name, at, radius, thick, colour, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(location=at, rotation=rot, major_radius=radius, minor_radius=thick,
                                     major_segments=20, minor_segments=8)
    o = bpy.context.object
    o.name = name
    return finish(o, colour)


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

# Smoothed (the owner: "see if you can smooth out the shapes"): every part subdivided, boxes rounded first by a bevel,
# so the block-out's cubes and seams soften into one rounded figure.
for o in list(scene.objects):
    if o.type != "MESH":
        continue
    if not any(p.use_smooth for p in o.data.polygons):
        # The scale put into the mesh, so the bevel is in metres.
        o.data.transform(Matrix.Diagonal((*o.scale, 1)))
        o.scale = (1, 1, 1)
        bev = o.modifiers.new("bevel", "BEVEL")
        bev.width = 0.004 if o.name.startswith(("stitch", "key", "chain", "flap")) else 0.01 if o.name.startswith(("blade", "buckle", "tooth", "loin", "sack_patch", "pouch")) else 0.025
        bev.segments = 3
        bev.limit_method = "NONE"
        for p in o.data.polygons:
            p.use_smooth = True
    sub = o.modifiers.new("smooth", "SUBSURF")
    sub.levels = sub.render_levels = 2

def export_rest():
    """One textured mesh for the rig: everything made a mesh and joined, its colours baked into one texture (the weave
    and wear included, no light), decimated to a size the rig binds quickly, written as a GLB; and a front picture with
    the rig.json beside it."""
    folder = os.path.join(ROOT, "ArtSource", "models", "forest", "sack_thief")
    os.makedirs(folder, exist_ok=True)
    parts = [o for o in scene.objects if o.type in ("MESH", "META") and o.name != "ground"]
    bpy.ops.object.select_all(action="DESELECT")
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.convert(target="MESH")
    meshes = [o for o in scene.objects if o.type == "MESH" and o.name != "ground"]
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.join()
    body = bpy.context.view_layer.objects.active
    body.name = "sack_thief"
    print("REPORT joined", len(body.data.polygons), "faces")
    dec = body.modifiers.new("dec", "DECIMATE")
    dec.ratio = min(1.0, 60000 / max(1, len(body.data.polygons)))
    bpy.ops.object.modifier_apply(modifier="dec")
    print("REPORT decimated", len(body.data.polygons), "faces")
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=1.15, island_margin=0.003)
    bpy.ops.object.mode_set(mode="OBJECT")
    size = 2048
    img = bpy.data.images.new("sack_thief_albedo", size, size, alpha=False)
    for slot in body.material_slots:
        nt = slot.material.node_tree
        node = nt.nodes.new("ShaderNodeTexImage")
        node.image = img
        nt.nodes.active = node
    scene.render.bake.use_pass_direct = False
    scene.render.bake.use_pass_indirect = False
    scene.render.bake.use_pass_color = True
    scene.render.bake.margin = 6
    scene.cycles.samples = 4
    bpy.ops.object.select_all(action="DESELECT")
    body.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.bake(type="DIFFUSE")
    img.filepath_raw = os.path.join(folder, "albedo.png")
    img.file_format = "PNG"
    img.save()
    # The baked texture on one plain material, written into the GLB.
    m = bpy.data.materials.new("sack_thief")
    m.use_nodes = True
    tex = m.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image = img
    m.node_tree.links.new(tex.outputs["Color"], m.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
    body.data.materials.clear()
    body.data.materials.append(m)
    bpy.ops.export_scene.gltf(filepath=os.path.join(folder, "shape_textured.glb"), use_selection=True, export_image_format="AUTO")
    co = [body.matrix_world @ v.co for v in body.data.vertices]
    print("REPORT height", max(c.z for c in co), "x", min(c.x for c in co), max(c.x for c in co))
    print("REPORT wrote", folder)


# Ground and lighting.
bpy.ops.mesh.primitive_plane_add(size=8, location=(0, 0, 0))
ground = bpy.context.object
ground.name = "ground"
gm = bpy.data.materials.new("ground")
gm.use_nodes = True
gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.05, 0.09, 0.04, 1)
gm.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0
gm.node_tree.nodes["Principled BSDF"].inputs["Specular IOR Level"].default_value = 0.0
ground.data.materials.append(gm)

world = bpy.data.worlds.new("w")
scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.08, 0.13, 0.07, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.35


def sun(name, rot, energy, colour=(1, 1, 1)):
    d = bpy.data.lights.new(name, "SUN")
    d.energy = energy
    d.color = colour
    o = bpy.data.objects.new(name, d)
    o.rotation_euler = Euler([math.radians(a) for a in rot])
    scene.collection.objects.link(o)


sun("key", (50, 0, -35), 3.4, (1.0, 0.93, 0.82))
# A warm rim light from behind, as in the reference.
sun("rim", (60, 0, 160), 2.4, (1.0, 0.75, 0.5))

scene.render.engine = "CYCLES"
scene.cycles.samples = 48
scene.cycles.device = "CPU"
scene.render.film_transparent = False
scene.view_settings.view_transform = "Standard"
scene.view_settings.exposure = -0.15

cam_data = bpy.data.cameras.new("cam")
cam = bpy.data.objects.new("cam", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam


def shoot(name, yaw, pitch, dist, size, res, ortho=True, target=Vector((0, 0.05, 0.95))):
    cam_data.type = "ORTHO" if ortho else "PERSP"
    cam_data.ortho_scale = size
    a, p = math.radians(yaw), math.radians(pitch)
    off = Vector((math.sin(a) * math.cos(p), -math.cos(a) * math.cos(p), math.sin(p))) * dist
    cam.location = target + off
    cam.rotation_mode = "QUATERNION"
    cam.rotation_quaternion = (-off).to_track_quat("-Z", "Y")
    scene.render.resolution_x, scene.render.resolution_y = res
    scene.render.filepath = os.path.join(OUT, name)
    bpy.ops.render.render(write_still=True)
    print("REPORT wrote", scene.render.filepath)


if REST:
    export_rest()
    sys.exit(0)
shoot("front.png", 0, 5, 6, 2.6, (900, 1000))
shoot("three_quarter.png", -40, 12, 6, 2.6, (900, 1000))
shoot("side.png", 90, 5, 6, 2.6, (900, 1000))
shoot("back.png", 200, 15, 6, 2.6, (900, 1000))
# The game's view: isometric from above (30 degrees), small, as a 92 px figure is seen.
scene.cycles.samples = 24
shoot("game.png", -25, 30, 6, 2.6, (100, 110))
shoot("game_back.png", 160, 30, 6, 2.6, (100, 110))
