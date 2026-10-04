# The modelling and rendering kit for the props built in Blender from code (render_props.py). Models are made of simple
# shapes on plain, slightly mottled materials and rendered orthographically from the game's camera: 30 degrees up,
# turned so the tilemap's x axis runs right and up the screen and its y axis left and up (IsoMath). The scene's unit is
# the game's ground unit: one cell is a square 0.707 on a side, its diagonal (one tile, 40 game pixels) 1 unit across the
# screen. METRE converts real sizes: a person 1.77 m tall shows as tall as the Wild Arrow (2 world units).
# Renders are at RENDER_PPU, 4 times the game's 40 pixels a unit, for the importers to shrink, snap to the palette and
# outline.
import bpy, bmesh, math, mathutils, os, json
import numpy as np

METRE = 1.3
CELL = 0.7071
RENDER_PPU = 160
ELEVATION = 30.0

# The palette's ramps (PixelArt.cs), dark to light, as 0..255 sRGB.
STONE = [(30, 26, 26), (50, 46, 45), (74, 69, 66), (102, 96, 91), (134, 128, 120), (170, 164, 154), (214, 208, 196)]
WOOD = [(28, 18, 13), (48, 31, 21), (72, 47, 30), (100, 68, 43), (132, 94, 60), (168, 128, 86)]
BLOOD = [(38, 9, 9), (70, 15, 14), (108, 24, 20), (150, 38, 28), (192, 62, 40)]
SKIN = [(62, 40, 32), (102, 68, 52), (144, 102, 78), (186, 142, 110), (222, 186, 150)]
MOSS = [(18, 26, 16), (34, 48, 28), (54, 72, 40), (82, 100, 58), (118, 132, 82)]
COLD = [(20, 24, 32), (38, 46, 58), (62, 74, 90), (94, 108, 124), (136, 150, 164), (184, 196, 204)]
EMBER = [(88, 40, 12), (148, 70, 18), (204, 110, 30), (236, 160, 60), (252, 214, 120)]
BONE = [(150, 142, 118), (196, 188, 162), (232, 224, 198)]
VIOLET = [(60, 30, 70), (110, 60, 130)]
# Muted grey-olive earth for the ground (2026-10-04, after Diablo 2's first act).
EARTH = [(36, 31, 25), (52, 46, 36), (68, 61, 47), (86, 77, 59), (106, 96, 74), (130, 118, 92), (156, 144, 114)]


def linear(c):
    def one(v):
        v = v / 255.0
        return v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4
    return (one(c[0]), one(c[1]), one(c[2]), 1.0)


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.film_transparent = True
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.resolution_percentage = 100
    try:
        scene.eevee.taa_render_samples = 16
    except AttributeError:
        pass
    world = bpy.data.worlds.new("World")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.32, 0.3, 0.3, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 1.0
    # The key light from the upper left, toward the camera's side, as the character bake lights them.
    sun = bpy.data.lights.new("Key", "SUN")
    sun.energy = 3.2
    sun.angle = math.radians(8)
    key = bpy.data.objects.new("Key", sun)
    key.rotation_euler = (math.radians(50), 0, math.radians(-100))
    scene.collection.objects.link(key)
    fill = bpy.data.lights.new("Fill", "SUN")
    fill.energy = 0.8
    fill.use_shadow = False
    rim = bpy.data.objects.new("Fill", fill)
    rim.rotation_euler = (math.radians(60), 0, math.radians(120))
    scene.collection.objects.link(rim)


_materials = {}


def mat(color, mottle=0.25, dark=None, rough=0.9, emit=0.0, scale=6.0, metal=0.0):
    """A plain material of one palette colour, mottled toward a darker one by noise (none with mottle 0)."""
    key = (color, mottle, dark, rough, emit, scale, metal)
    if key in _materials:
        return _materials[key]
    m = bpy.data.materials.new("M%d" % len(_materials))
    m.use_nodes = True
    nodes = m.node_tree.nodes
    links = m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    base = linear(color)
    if mottle > 0:
        shade = linear(dark) if dark else tuple(v * 0.45 for v in base[:3]) + (1.0,)
        noise = nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = scale
        noise.inputs["Detail"].default_value = 3.0
        ramp = nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.interpolation = "CONSTANT"
        ramp.color_ramp.elements[0].position = 0.0
        ramp.color_ramp.elements[0].color = shade
        ramp.color_ramp.elements[1].position = mottle
        ramp.color_ramp.elements[1].color = base
        links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
        links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    else:
        bsdf.inputs["Base Color"].default_value = base
    if emit > 0:
        bsdf.inputs["Emission Color"].default_value = base
        bsdf.inputs["Emission Strength"].default_value = emit
    _materials[key] = m
    return m


def _finish(obj, material, smooth=False):
    if material is not None:
        obj.data.materials.append(material)
    for p in obj.data.polygons:
        p.use_smooth = smooth
    return obj


def _link(name, mesh):
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def box(size, loc, material, rot=(0, 0, 0), bevel=0.0):
    """A box of size (x, y, z), its base centred on loc (z is the bottom)."""
    mesh = bpy.data.meshes.new("box")
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=size, verts=bm.verts)
    bmesh.ops.translate(bm, vec=(0, 0, size[2] / 2), verts=bm.verts)
    if bevel > 0:
        bmesh.ops.bevel(bm, geom=bm.edges[:], offset=bevel, segments=1, affect="EDGES")
    bm.to_mesh(mesh)
    bm.free()
    obj = _link("box", mesh)
    obj.location = loc
    obj.rotation_euler = [math.radians(a) for a in rot]
    return _finish(obj, material)


def lathe(profile, loc, material, segments=16, rot=(0, 0, 0), smooth=True, cap=True):
    """A solid of revolution about z from a profile of (radius, height) pairs, bottom to top."""
    mesh = bpy.data.meshes.new("lathe")
    bm = bmesh.new()
    rings = []
    for r, z in profile:
        ring = []
        for i in range(segments):
            a = 2 * math.pi * i / segments
            ring.append(bm.verts.new((r * math.cos(a), r * math.sin(a), z)))
        rings.append(ring)
    for a, b in zip(rings, rings[1:]):
        for i in range(segments):
            j = (i + 1) % segments
            bm.faces.new((a[i], a[j], b[j], b[i]))
    if cap:
        if profile[0][0] > 0:
            bm.faces.new(list(reversed(rings[0])))
        if profile[-1][0] > 0:
            bm.faces.new(rings[-1])
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    obj = _link("lathe", mesh)
    obj.location = loc
    obj.rotation_euler = [math.radians(a) for a in rot]
    return _finish(obj, material, smooth)


def cyl(radius, height, loc, material, segments=12, rot=(0, 0, 0), smooth=True):
    return lathe([(radius, 0), (radius, height)], loc, material, segments, rot, smooth)


def cone(radius, height, loc, material, segments=12, rot=(0, 0, 0), smooth=False, top=0.0):
    return lathe([(radius, 0), (max(top, 0.0001), height)], loc, material, segments, rot, smooth)


def sphere(radius, loc, material, scale=(1, 1, 1), segments=12, smooth=True):
    mesh = bpy.data.meshes.new("sphere")
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segments, v_segments=max(6, segments // 2), radius=radius)
    bm.to_mesh(mesh)
    bm.free()
    obj = _link("sphere", mesh)
    obj.location = loc
    obj.scale = scale
    return _finish(obj, material, smooth)


def prism(points, height, loc, material, rot=(0, 0, 0)):
    """An extruded polygon: points (x, y) in order, raised height along z."""
    mesh = bpy.data.meshes.new("prism")
    bm = bmesh.new()
    low = [bm.verts.new((x, y, 0)) for x, y in points]
    high = [bm.verts.new((x, y, height)) for x, y in points]
    bm.faces.new(list(reversed(low)))
    bm.faces.new(high)
    n = len(points)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((low[i], low[j], high[j], high[i]))
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    obj = _link("prism", mesh)
    obj.location = loc
    obj.rotation_euler = [math.radians(a) for a in rot]
    return _finish(obj, material)


def poly(verts, faces, material, loc=(0, 0, 0), smooth=False):
    mesh = bpy.data.meshes.new("poly")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = _link("poly", mesh)
    obj.location = loc
    return _finish(obj, material, smooth)


def flat(points, material, z=0.002):
    """A flat shape lying on the ground (a decal): points (x, y)."""
    return poly([(x, y, z) for x, y in points], [list(range(len(points)))], material)


def blob(cx, cy, radius, material, points=10, wobble=0.35, rng=None, z=0.002, squash=1.0):
    import random
    rng = rng or random
    pts = []
    for i in range(points):
        a = 2 * math.pi * i / points
        r = radius * (1 - wobble / 2 + wobble * rng.random())
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a) * squash))
    return flat(pts, material, z)


def holdout(inner, outer, z=0.0):
    """A ground plane with a square hole (half sizes): hides whatever lies below ground outside the hole."""
    m = bpy.data.materials.new("Holdout")
    m.use_nodes = True
    nodes = m.node_tree.nodes
    for n in list(nodes):
        nodes.remove(n)
    out = nodes.new("ShaderNodeOutputMaterial")
    hold = nodes.new("ShaderNodeHoldout")
    m.node_tree.links.new(hold.outputs["Holdout"], out.inputs["Surface"])
    i, o = inner, outer
    verts = [(-o, -o, z), (o, -o, z), (o, o, z), (-o, o, z), (-i, -i, z), (i, -i, z), (i, i, z), (-i, i, z)]
    faces = [(0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return poly(verts, faces, m)


def light(kind, loc, energy, color, radius=0.1):
    data = bpy.data.lights.new("L", kind)
    data.energy = energy
    data.color = color
    try:
        data.shadow_soft_size = radius
    except AttributeError:
        pass
    obj = bpy.data.objects.new("L", data)
    obj.location = loc
    bpy.context.scene.collection.objects.link(obj)
    return obj


def _camera_axes():
    pitch = math.radians(90 - ELEVATION)
    yaw = math.radians(-45)
    rotation = mathutils.Euler((pitch, 0, yaw), "XYZ")
    m = rotation.to_matrix()
    right = m @ mathutils.Vector((1, 0, 0))
    up = m @ mathutils.Vector((0, 1, 0))
    back = m @ mathutils.Vector((0, 0, 1))
    return rotation, right, up, back


def render(path, width, height, origin_x, origin_y):
    """Renders the scene to a width x height PNG (render pixels) with the ground origin at pixel (origin_x, origin_y)
    counted from the bottom left."""
    scene = bpy.context.scene
    rotation, right, up, back = _camera_axes()
    data = bpy.data.cameras.new("Cam")
    data.type = "ORTHO"
    data.ortho_scale = max(width, height) / RENDER_PPU
    data.clip_start = 0.01
    data.clip_end = 200
    cam = bpy.data.objects.new("Cam", data)
    scene.collection.objects.link(cam)
    centre = (right * ((width / 2 - origin_x) / RENDER_PPU) + up * ((height / 2 - origin_y) / RENDER_PPU))
    cam.location = centre + back * 60
    cam.rotation_euler = rotation
    scene.camera = cam
    scene.render.resolution_x = width
    scene.render.resolution_y = height
    os.makedirs(os.path.dirname(path), exist_ok=True)
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam, do_unlink=True)


def render_cropped(path, width, height, origin_x, origin_y, manifest, name, step=4, margin=4):
    """Renders a large frame, crops it to what was drawn (edges on whole steps from the origin, so the importer's
    shrink by step keeps the origin on a pixel corner) and records the origin in the cropped image in manifest."""
    tmp = path + ".full.png"
    render(tmp, width, height, origin_x, origin_y)
    image = bpy.data.images.load(tmp)
    w, h = image.size
    px = np.array(image.pixels[:], dtype=np.float32).reshape(h, w, 4)
    alpha = px[:, :, 3] > 0.5
    rows = np.where(alpha.any(axis=1))[0]
    cols = np.where(alpha.any(axis=0))[0]
    if len(rows) == 0:
        raise RuntimeError("nothing drawn for " + name)
    x0, x1 = cols[0] - margin, cols[-1] + 1 + margin
    y0, y1 = rows[0] - margin, rows[-1] + 1 + margin
    # Snap the edges outward to whole steps from the origin, and keep the origin column in the middle so the sprite's
    # pivot is its centre line.
    half = max(origin_x - x0, x1 - origin_x)
    half = int(math.ceil(half / step) * step)
    x0, x1 = origin_x - half, origin_x + half
    y0 = origin_y - int(math.ceil((origin_y - y0) / step) * step)
    y1 = origin_y + int(math.ceil((y1 - origin_y) / step) * step)
    out = np.zeros((y1 - y0, x1 - x0, 4), dtype=np.float32)
    sx0, sx1, sy0, sy1 = max(x0, 0), min(x1, w), max(y0, 0), min(y1, h)
    out[sy0 - y0:sy1 - y0, sx0 - x0:sx1 - x0] = px[sy0:sy1, sx0:sx1]
    cropped = bpy.data.images.new(name, out.shape[1], out.shape[0], alpha=True)
    cropped.pixels = out.ravel()
    cropped.filepath_raw = path
    cropped.file_format = "PNG"
    cropped.save()
    os.remove(tmp)
    manifest[name] = {"width": int(out.shape[1]), "height": int(out.shape[0]),
                      "pivotX": int(origin_x - x0), "pivotY": int(origin_y - y0)}
    print("REPORT %s %d x %d pivot (%d, %d)" % (name, out.shape[1], out.shape[0], origin_x - x0, origin_y - y0))


def cell_offset(cx, cy):
    """The scene position of a tilemap cell's middle relative to cell (0, 0)."""
    return (cx * CELL, cy * CELL)
