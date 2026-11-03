# Shared tools for the enemies designed in Blender from code (2026-11-03: the Sack Thief, then the Bandit Bowman): soft
# parts as metaballs that flow together, hard parts bevelled and subdivided, fabric and metal materials, the concept
# renders (front, three-quarter, side, back and the game's camera, small, on the forest's green) and the rest-pose
# export for the rig (one mesh, its colours baked into one texture, a GLB beside its rig.json).
# A figure script imports this, fills COLOURS (and FABRIC, METAL, SOFT), builds its parts, then calls present().
import bpy, bmesh, sys, os, math
from mathutils import Vector, Euler, Matrix

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = os.path.abspath(args[0] if args else "concept")
# "rest": the figure for the rig, in an A-pose, empty-handed (held weapons are enemy_weapons.py's pieces).
REST = len(args) > 1 and args[1] == "rest"

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

COLOURS = {}
FABRIC = set()
METAL = set()
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
SOFT = set()
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




# Bevel widths by part name: the finest for stitches and small hardware, a middle one for flat panels; else 0.025.
FINE = ["stitch", "key", "chain", "flap"]
MIDDLE = ["blade", "buckle", "tooth", "loin", "patch", "pouch"]


def smooth_all():
    """Every part subdivided, boxes rounded first by a bevel, so the cubes and seams soften into one rounded figure."""
    for o in list(scene.objects):
        if o.type != "MESH":
            continue
        if not any(p.use_smooth for p in o.data.polygons):
            # The scale put into the mesh, so the bevel is in metres.
            o.data.transform(Matrix.Diagonal((*o.scale, 1)))
            o.scale = (1, 1, 1)
            bev = o.modifiers.new("bevel", "BEVEL")
            bev.width = 0.004 if o.name.startswith(tuple(FINE)) else 0.01 if any(k in o.name for k in MIDDLE) else 0.025
            bev.segments = 3
            bev.limit_method = "NONE"
            for p in o.data.polygons:
                p.use_smooth = True
        sub = o.modifiers.new("smooth", "SUBSURF")
        sub.levels = sub.render_levels = 2


def export_rest(name):
    """One textured mesh for the rig: everything made a mesh and joined, its colours baked into one texture (the weave
    and wear included, no light), decimated to a size the rig binds quickly, written as a GLB; and a front picture with
    the rig.json beside it."""
    folder = os.path.join(ROOT, "ArtSource", "models", "forest", name)
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
    body.name = name
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
    img = bpy.data.images.new(name + "_albedo", size, size, alpha=False)
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
    m = bpy.data.materials.new(name)
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



def present(name, target=Vector((0, 0.05, 0.95)), size=2.6):
    """Smooths the figure, sets the stage and renders the concept pictures, or with "rest" exports it for the rig."""
    smooth_all()
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

    def shoot(file, yaw, pitch, res, samples=48):
        cam_data.type = "ORTHO"
        cam_data.ortho_scale = size
        a, p = math.radians(yaw), math.radians(pitch)
        off = Vector((math.sin(a) * math.cos(p), -math.cos(a) * math.cos(p), math.sin(p))) * 6
        cam.location = target + off
        cam.rotation_mode = "QUATERNION"
        cam.rotation_quaternion = (-off).to_track_quat("-Z", "Y")
        scene.render.resolution_x, scene.render.resolution_y = res
        scene.cycles.samples = samples
        scene.render.filepath = os.path.join(OUT, file)
        bpy.ops.render.render(write_still=True)
        print("REPORT wrote", scene.render.filepath)

    if REST:
        export_rest(name)
        return
    shoot("front.png", 0, 5, (900, 1000))
    shoot("three_quarter.png", -40, 12, (900, 1000))
    shoot("side.png", 90, 5, (900, 1000))
    shoot("back.png", 200, 15, (900, 1000))
    # The game's view: isometric from above (30 degrees), small, as a 92 px figure is seen.
    shoot("game.png", -25, 30, (100, 110), 24)
    shoot("game_back.png", 160, 30, (100, 110), 24)
