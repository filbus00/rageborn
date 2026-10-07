# Blender script: fit the owner's Meshy hood (ArtSource/models/hood/emerald_wraith.fbx, 2026-10-07, "Emerald Wraith",
# a hood with a shoulder mantle; the concept is emerald_wraith_concept.jpg beside it) onto the Wild Arrow's rig as the
# hood helm look: Assets/_Project/Art/Models/WildArrow/wild_arrow_helm_hood.fbx with its own texture beside it
# (wild_arrow_helm_hood_albedo.png; WildArrowBakeSetup uses a helm's own texture when it has one).
# The Meshy model stands along Blender's z with its face toward -y. It is decimated (the sprite is a few dozen pixels
# tall), scaled so the hood fits round her head, its top just over her crown, and weighted from her head through the
# neck to the upper chest, so it bends with her like cloth instead of sliding off her head in the idle's lean.
# Usage: Blender -b -P fit_hood.py [-- preview.png]
import os, sys, math
import bpy, mathutils
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import wild_arrow_gear as g

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
SOURCE = os.path.join(ROOT, "ArtSource", "models", "hood", "emerald_wraith.fbx")
ALBEDO = os.path.join(ROOT, "ArtSource", "models", "hood", "emerald_wraith_albedo.png")
TARGET = os.path.join(g.FOLDER, "wild_arrow_helm_hood.fbx")

SCALE = 0.34          # metres per Meshy unit: the hood (1.06 wide at the face) round her head (0.30 wide with hair)
TOP = 1.83            # the hood's crown, just over hers (1.75)
FORWARD = 0.0         # the hood's centre in depth against her head's (her head spans -0.145 to 0.143)
FACES = 24000         # after decimation


def smooth(a, b, y):
    t = min(1.0, max(0.0, (y - a) / (b - a)))
    return t * t * (3 - 2 * t)


def weights(p):
    head = smooth(1.5, 1.6, p.y)
    neck = (1 - head) * smooth(1.38, 1.47, p.y)
    return [("Head", head), ("Neck", neck), ("Spine2", 1 - head - neck)]


def main():
    arm, body = g.load()
    bpy.ops.import_scene.fbx(filepath=SOURCE)
    hood = [o for o in bpy.context.scene.objects if o.type == "MESH" and o != body][0]
    for o in list(bpy.context.scene.objects):
        if o.type not in ("MESH", "ARMATURE"):
            bpy.data.objects.remove(o, do_unlink=True)
    bpy.context.view_layer.objects.active = hood
    bpy.ops.object.select_all(action="DESELECT")
    hood.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    ratio = min(1.0, FACES / max(1, len(hood.data.polygons)))
    mod = hood.modifiers.new("Decimate", "DECIMATE")
    mod.ratio = ratio
    bpy.ops.object.modifier_apply(modifier=mod.name)

    # Meshy (x, y, z), z up and -y forward, into her armature's space (y up, z forward, x her left).
    top = max(v.co.z for v in hood.data.vertices)
    for v in hood.data.vertices:
        x, y, z = v.co
        v.co = mathutils.Vector((x * SCALE, TOP + (z - top) * SCALE, FORWARD - y * SCALE))
    hood.data.update()

    hood.vertex_groups.clear()
    groups = {}
    for v in hood.data.vertices:
        for name, w in weights(v.co):
            if w <= 0:
                continue
            if name not in groups:
                groups[name] = hood.vertex_groups.new(name="mixamorig:" + name)
            groups[name].add([v.index], w, "REPLACE")
    hood.parent = arm
    hood.matrix_parent_inverse = mathutils.Matrix.Identity(4)
    hood.matrix_world = arm.matrix_world.copy()
    hood.modifiers.new("Armature", "ARMATURE").object = arm

    material = bpy.data.materials.new("Hood")
    material.use_nodes = True
    nodes = material.node_tree.nodes
    image = nodes.new("ShaderNodeTexImage")
    image.image = bpy.data.images.load(ALBEDO)
    material.node_tree.links.new(image.outputs["Color"], nodes["Principled BSDF"].inputs["Base Color"])
    hood.data.materials.clear()
    hood.data.materials.append(material)
    hood.name = "Gear"

    ys = [v.co.y for v in hood.data.vertices]
    print(f"REPORT faces {len(hood.data.polygons)}, from {min(ys):.3f} to {max(ys):.3f} m")
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if args:
        preview(arm, body, hood, args[0])
        return
    bpy.data.objects.remove(body, do_unlink=True)
    g.export(arm, [hood], TARGET)
    import shutil
    shutil.copyfile(ALBEDO, TARGET.replace(".fbx", "_albedo.png"))


def preview(arm, body, hood, out):
    """Her body with the hood from the game's camera in 8 directions, close up."""
    import numpy as np
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.film_transparent = True
    scene.render.resolution_x = scene.render.resolution_y = 240
    scene.view_settings.view_transform = "Standard"
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 0.9
    scene.collection.objects.link(cam)
    scene.camera = cam
    for rot, energy in (((45, 0, -30), 3.5), ((-30, 0, 160), 1.0)):
        sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
        sun.data.energy = energy
        sun.rotation_euler = tuple(math.radians(a) for a in rot)
        scene.collection.objects.link(sun)
    world = bpy.data.worlds.new("W")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.6
    target = mathutils.Vector((0, 0, 1.5))
    tiles = []
    tile = out + ".tile.png"
    for k in range(8):
        cam.rotation_euler = (math.radians(60), 0, math.radians(-45 + 30 + k * 45))
        d = mathutils.Vector((0, 0, 1))
        d.rotate(cam.rotation_euler)
        cam.location = target + d * 6
        scene.render.filepath = tile
        bpy.ops.render.render(write_still=True)
        img = bpy.data.images.load(tile)
        a = np.array(img.pixels[:]).reshape(240, 240, 4)
        bpy.data.images.remove(img)
        al = a[:, :, 3:4]
        tiles.append(np.concatenate([a[:, :, :3] * al + 0.45 * (1 - al), np.ones((240, 240, 1))], 2))
    o = np.concatenate(tiles, 1)
    im = bpy.data.images.new("o", o.shape[1], o.shape[0], alpha=True)
    im.pixels = o.ravel()
    im.filepath_raw = out
    im.file_format = "PNG"
    im.save()
    print("REPORT preview", out)


if __name__ == "__main__":
    main()
