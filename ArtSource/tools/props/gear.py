# The Wild Arrow's bows and quivers, built in Blender from code and exported as the sprite bake expects them
# (WildArrowBakeSetup.Pieces): Assets/_Project/Art/Models/WildArrow/wild_arrow_weapon_<look>.fbx and
# wild_arrow_offhand_<look>.fbx, each with <file>_albedo.png beside it. Sizes are in metres (the body's units).
# A bow stands along Blender's z with the middle of its handle at the origin and its back (the side the handle stands
# out toward, away from the archer) toward -y, which the export (forward -Z, up Y) makes Unity's +Z, as
# prepare_bow.py does for a Meshy bow. A quiver stands along z, centred, 0.75 m with the arrows, as prepare_quiver.py.
# Each part's colour goes into a small atlas texture: every face of a part maps to its colour's square.
#   Blender -b -P ArtSource/tools/props/gear.py -- [looks...]
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy, bmesh
import numpy as np
import kit
from kit import *
from models import skull

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "WildArrow")
PREVIEW = os.path.join(ROOT, "ArtSource", "pixel", "gear_preview")


def sweep(points, width, depth, material, segments_round=False):
    """A limb along points (y, z) in the y-z plane, width across x and depth across the curve, each a (y, z, w, d)."""
    mesh = bpy.data.meshes.new("limb")
    bm = bmesh.new()
    rings = []
    for i, (y, z, w, d) in enumerate(points):
        a = points[max(i - 1, 0)]
        b = points[min(i + 1, len(points) - 1)]
        ty, tz = b[0] - a[0], b[1] - a[1]
        n = math.hypot(ty, tz) or 1.0
        ny, nz = -tz / n, ty / n
        ring = []
        for sx, sn in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            ring.append(bm.verts.new((sx * w * width / 2, y + sn * ny * d * depth / 2, z + sn * nz * d * depth / 2)))
        rings.append(ring)
    for r0, r1 in zip(rings, rings[1:]):
        for k in range(4):
            j = (k + 1) % 4
            bm.faces.new((r0[k], r0[j], r1[j], r1[k]))
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("limb", mesh)
    bpy.context.scene.collection.objects.link(obj)
    obj.data.materials.append(material)
    return obj


def bow(length, bend, recurve, wood, grip_color, tip_color, thick=1.0, bands=None, gem=None, string_color=BONE[2],
        glow=0.0, band_color=None, thorns=None, feathers=None):
    """A bow length long, its limbs bending bend toward +y (toward the archer) and curling recurve back at the tips."""
    half = length / 2
    pts = []
    steps = 24
    for i in range(steps + 1):
        t = -1 + 2 * i / steps
        z = t * half
        y = bend * t * t
        if abs(t) > 0.8 and recurve:
            y -= recurve * ((abs(t) - 0.8) / 0.2) ** 2
        taper = 1.0 - 0.55 * abs(t)
        pts.append((y, z, taper, taper))
    sweep(pts, 0.035 * thick, 0.03 * thick, mat(wood, 0.0, emit=glow))
    # The handle: a thicker wrap round the middle, standing out toward the back.
    box((0.045 * thick, 0.05 * thick, 0.16), (0, -0.008, -0.08), mat(grip_color, 0.0))
    # Tips and nocks.
    tip = mat(tip_color, 0.0)
    tips = []
    for sgn in (-1, 1):
        y, z = pts[0 if sgn < 0 else -1][:2]
        sphere(0.018 * thick, (0, y, z), tip, segments=8)
        tips.append((y, z))
    # The string, tip to tip, behind the limbs (toward the archer).
    (y0, z0), (y1, z1) = tips
    sy = max(y0, y1) + 0.005
    cyl(0.004, z1 - z0, (0, sy, z0), mat(string_color, 0.0), 6)
    if bands:
        for f in bands:
            i = int((f + 1) / 2 * steps)
            y, z = pts[i][:2]
            box((0.05 * thick, 0.045 * thick, 0.03), (0, y, z - 0.015), mat(band_color or STONE[3], 0.0, metal=0.5))
    if gem:
        sphere(0.022, (0, -0.04 * thick, 0.0), mat(gem, 0.0, emit=0.4), segments=8)
    # Legendaries' own touches (2026-10-05): thorns along the limbs, feathers hung from the tips.
    if thorns:
        for i in range(3, steps - 2, 3):
            if abs(i - steps / 2) < 3:
                continue
            y, z = pts[i][:2]
            cone(0.012, 0.05, (0, y - 0.02, z), mat(thorns, 0.0), 5, rot=(180, 0, 0))
    if feathers:
        for sgn in (-1, 1):
            y, z = pts[0 if sgn < 0 else -1][:2]
            for k in range(3):
                box((0.004, 0.03, 0.09), (0.0, y + 0.01 + k * 0.012, z - sgn * (0.06 + k * 0.025)), mat(feathers[k % len(feathers)], 0.0), rot=(sgn * 20, 0, 0))


def quiver(body, trim, fletch, studs=None, bone_plates=False, glow_trim=0.0, twigs=None, fur=None):
    h = 0.55
    r = 0.075
    lathe([(r * 0.9, -h / 2 - 0.1), (r, -h / 2 + 0.02), (r * 1.05, h / 2 - 0.12)], (0, 0, 0), mat(body, 0.0), 10, smooth=False)
    lathe([(r * 1.1, h / 2 - 0.16), (r * 1.1, h / 2 - 0.11)], (0, 0, 0), mat(trim, 0.0, emit=glow_trim), 10, smooth=False)
    lathe([(r * 1.0, -h / 2 - 0.1), (r * 1.0, -h / 2 - 0.06)], (0, 0, 0), mat(trim, 0.0), 10, smooth=False)
    shaft = mat(WOOD[3], 0.0)
    feather = mat(fletch, 0.0)
    for k in range(7):
        a = k * 2.4
        rr = 0.035 * (k % 3)
        x, y = rr * math.cos(a), rr * math.sin(a)
        top = h / 2 - 0.08 + 0.08 + (k % 2) * 0.03
        cyl(0.006, 0.12, (x, y, top - 0.12), shaft, 5)
        box((0.004, 0.035, 0.07), (x, y, top - 0.05), feather, rot=(0, 0, a * 57))
    if studs:
        stud = mat(studs, 0.0, metal=0.5, rough=0.4)
        for row in range(4):
            for k in range(6):
                a = k * math.pi / 3 + row * 0.5
                z = -h / 2 + 0.08 + row * 0.1
                sphere(0.012, (r * 1.04 * math.cos(a), r * 1.04 * math.sin(a), z), stud, segments=6)
    if twigs:
        # A magpie's nest round the mouth: twigs every way.
        for k in range(14):
            a = k * 0.9
            cyl(0.006, 0.16, (r * 1.15 * math.cos(a), r * 1.15 * math.sin(a), h / 2 - 0.14), mat(twigs, 0.0), 4, rot=(70, 0, math.degrees(a)))
    if fur:
        lathe([(r * 1.25, -0.02), (r * 1.3, 0.06), (r * 1.2, 0.14)], (0, 0, 0), mat(fur, 0.4, scale=40), 10, smooth=False)
    if bone_plates:
        bonec = mat(BONE[1], 0.0)
        for row in range(3):
            for k in range(4):
                a = k * math.pi / 2 + row * 0.6
                z = -h / 2 + 0.05 + row * 0.13
                box((0.05, 0.02, 0.09), (r * 1.06 * math.cos(a), r * 1.06 * math.sin(a), z), bonec, rot=(0, 0, math.degrees(a) + 90))
        skull((0, -r * 1.2, -0.02), 0.035, bonec, turn=-45)


LOOKS = {
    "weapon_recurve_bow": lambda: bow(1.15, 0.07, 0.07, WOOD[2], WOOD[0], BONE[1]),
    "weapon_horn_bow": lambda: bow(1.05, 0.08, 0.1, BONE[1], BLOOD[1], WOOD[1], thick=1.15),
    "weapon_yew_longbow": lambda: bow(1.65, 0.1, 0.0, WOOD[4], WOOD[1], BONE[1]),
    "weapon_war_bow": lambda: bow(1.7, 0.09, 0.03, WOOD[1], BLOOD[2], STONE[3], thick=1.3, bands=(-0.6, -0.3, 0.3, 0.6)),
    "weapon_great_bow": lambda: bow(1.8, 0.1, 0.06, STONE[0], EMBER[3], EMBER[3], thick=1.3, bands=(-0.5, 0.5), gem=BLOOD[4]),
    "offhand_studded_quiver": lambda: quiver(WOOD[1], STONE[3], BLOOD[3], studs=STONE[4]),
    "offhand_bone_quiver": lambda: quiver(STONE[1], BONE[1], STONE[0], bone_plates=True),
    # The named legendaries' own looks (2026-10-05, the owner: "legendaries that have unique looks").
    "weapon_splinterbough": lambda: bow(1.12, 0.08, 0.06, BONE[0], WOOD[1], MOSS[3], thorns=WOOD[3], gem=MOSS[3]),
    "weapon_ember_tongue": lambda: bow(1.1, 0.08, 0.08, EMBER[1], STONE[0], EMBER[4], glow=0.6, gem=EMBER[4], string_color=EMBER[3]),
    "weapon_hunters_promise": lambda: bow(1.12, 0.07, 0.06, MOSS[2], EMBER[2], EMBER[2], feathers=(BONE[2], BLOOD[2])),
    "weapon_galeheart": lambda: bow(1.15, 0.09, 0.12, COLD[4], COLD[2], BONE[2], gem=COLD[5], feathers=(BONE[2], COLD[4])),
    "weapon_widows_draw": lambda: bow(1.1, 0.08, 0.09, STONE[0], VIOLET[0], VIOLET[1], gem=VIOLET[1], string_color=BLOOD[3], thorns=STONE[2]),
    "weapon_gallowsreach": lambda: bow(1.85, 0.11, 0.02, WOOD[1], WOOD[3], STONE[2], thick=1.25, bands=(-0.7, -0.45, 0.45, 0.7), band_color=WOOD[4]),
    "weapon_stillwater_yew": lambda: bow(1.75, 0.1, 0.03, BONE[1], COLD[2], COLD[4], bands=(-0.5, 0.5), band_color=COLD[3], gem=COLD[5]),
    "weapon_the_long_silence": lambda: bow(1.95, 0.1, 0.05, STONE[0], EMBER[2], EMBER[2], thick=1.3, bands=(-0.75, -0.5, 0.5, 0.75), band_color=EMBER[2], gem=BONE[2]),
    "offhand_quiver_of_endless_splinters": lambda: quiver(BONE[0], WOOD[2], MOSS[3], studs=WOOD[3]),
    "offhand_ashfall_quiver": lambda: quiver(STONE[0], EMBER[3], EMBER[2], glow_trim=0.6, studs=EMBER[1]),
    "offhand_wind_sworn_quiver": lambda: quiver(MOSS[1], EMBER[2], BONE[2]),
    "offhand_magpies_nest": lambda: quiver(STONE[0], BONE[2], STONE[0], twigs=WOOD[2]),
    "offhand_quiver_of_the_hollow_hound": lambda: quiver(STONE[1], VIOLET[1], VIOLET[0], fur=EARTH[2], bone_plates=True),
}


def atlas_and_export(path):
    """Joins the scene's meshes into one, maps every face to its material colour's square of an atlas, writes the
    atlas beside the FBX as <file>_albedo.png and exports the FBX."""
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    colors = []
    for o in meshes:
        for m in o.data.materials:
            c = tuple(m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value[:3])
            if c not in colors:
                colors.append(c)
    cells = 4
    while cells * cells < len(colors):
        cells *= 2
    size = cells * 8
    pixels = np.zeros((size, size, 4), dtype=np.float32)
    pixels[:, :, 3] = 1
    for i, c in enumerate(colors):
        cx, cy = i % cells, i // cells
        pixels[cy * 8:(cy + 1) * 8, cx * 8:(cx + 1) * 8, :3] = c
    image = bpy.data.images.new("atlas", size, size, alpha=True)
    image.colorspace_settings.name = "Linear Rec.709"
    image.pixels = pixels.ravel()
    albedo = path.replace(".fbx", "_albedo.png")
    image.filepath_raw = albedo
    image.file_format = "PNG"
    image.save()
    # Saved from linear values into an sRGB PNG: write it again through numpy so the file holds sRGB.
    srgb = np.where(pixels[:, :, :3] <= 0.0031308, pixels[:, :, :3] * 12.92, 1.055 * np.power(pixels[:, :, :3], 1 / 2.4) - 0.055)
    out = bpy.data.images.new("atlas_srgb", size, size, alpha=True)
    out.colorspace_settings.name = "Non-Color"
    out.pixels = np.concatenate([srgb, pixels[:, :, 3:]], axis=2).ravel()
    out.filepath_raw = albedo
    out.file_format = "PNG"
    out.save()

    for o in meshes:
        if not o.data.uv_layers:
            o.data.uv_layers.new(name="UV")
        uv = o.data.uv_layers.active.data
        for poly in o.data.polygons:
            m = o.data.materials[poly.material_index]
            c = tuple(m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value[:3])
            i = colors.index(c)
            u, v = (i % cells + 0.5) / cells, (i // cells + 0.5) / cells
            for li in poly.loop_indices:
                uv[li].uv = (u, v)
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.convert(target="MESH")
    bpy.ops.object.join()
    joined = bpy.context.view_layer.objects.active
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    one = bpy.data.materials.new("Gear")
    one.use_nodes = True
    tex = one.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(albedo)
    one.node_tree.links.new(tex.outputs["Color"], one.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
    joined.data.materials.clear()
    joined.data.materials.append(one)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options="FBX_SCALE_ALL", path_mode="STRIP",
                             embed_textures=False, axis_forward="-Z", axis_up="Y", add_leaf_bones=False)
    print("REPORT wrote", path, len(colors), "colours")


argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
for look, build in LOOKS.items():
    if argv and look not in argv:
        continue
    kit.reset()
    kit._materials.clear()
    build()
    # A preview from the side, as the archer holds it, before the export joins the parts.
    for o in bpy.context.scene.objects:
        if o.type == "MESH":
            o.scale = (2.0, 2.0, 2.0)
            o.location = tuple(v * 2.0 for v in o.location)
    kit.render(os.path.join(PREVIEW, look + ".png"), 480, 640, 240, 320)
    for o in bpy.context.scene.objects:
        if o.type == "MESH":
            o.scale = (1.0, 1.0, 1.0)
            o.location = tuple(v / 2.0 for v in o.location)
    atlas_and_export(os.path.join(OUT, "wild_arrow_%s.fbx" % look))
