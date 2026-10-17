# Act 1's undead (2026-10-04), built in Blender from code on existing Mixamo rigs like the Cinder Warden (boss.py):
# the skeleton on the husk's rig (it moves with the husk's zombie idle, run, attack, hit and death) and the cultist on
# the bandit archer's (its draw is the cultist's cast). Each part is a solid weighted wholly to one bone. Writes
# Assets/_Project/Art/Models/Enemies/<name>/ with <name>.fbx, <name>_albedo.png and the donor's clips copied under the
# new name; EnemyBakeSetup.ClipAvatarFrom gives those clips the donor's avatar. Then Bake Enemies.
#   Blender -b -P ArtSource/tools/props/undead.py -- skeleton|cultist
import sys, os, math, shutil
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy, bmesh, mathutils
import numpy as np
from kit import STONE, WOOD, BLOOD, EMBER, BONE, COLD, MOSS, VIOLET, linear

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
ENEMIES = os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "Enemies")
# The rigs and clips everything here borrows (2026-10-13: the husk's, ghoul's and archer's Mixamo files moved out of the
# enemies, their bodies replaced by ours, below).
RIGS = os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "Rigs")
NAME = sys.argv[sys.argv.index("--") + 1]
DONORS = {"skeleton": "husk", "cultist": "bandit_archer", "cutthroat": "husk", "ember_acolyte": "bandit_archer",
          "pyre_keeper": "bandit_archer", "carrion_bloat": "ghoul",
          # The town's newcomers (2026-10-05): built here too, written to Art/Models/NPCs, idling as the merchant does.
          "stash_keeper": "bandit_archer", "healer": "bandit_archer", "gambler": "bandit_archer", "trainer": "bandit_archer",
          # The deep levels (2026-10-05) and their bosses (on the ghoul's rig, as the Warden).
          "drowned": "husk", "harpooner": "bandit_archer", "drowned_watchman": "ghoul", "skeleton_knight": "ghoul",
          "grave_priest": "bandit_archer", "hollowed": "husk", "void_wraith": "husk", "rift_caller": "bandit_archer",
          "tidewife": "ghoul", "saint_marrow": "ghoul", "first_watchman": "ghoul",
          # The burnt library's boss and the caves' (2026-10-11).
          "ash_archivist": "ghoul", "rift_mother": "ghoul",
          # The prison's bosses (2026-10-12).
          "gaoler": "ghoul", "hanged_magistrate": "ghoul",
          # Our own bodies for the first enemies and the two first townsfolk (2026-10-13, the owner: "remove all imported
          # meshy models"), on the rigs their Mixamo files had.
          "husk": "husk", "husk_champion": "husk", "husk_elite": "husk", "ghoul": "ghoul", "bandit_archer": "bandit_archer",
          "merchant": "bandit_archer", "pet_vendor": "bandit_archer",
          # The concept roster (2026-10-13, the owner's enemies concept art).
          "zombie": "husk", "zombie_champion": "husk", "zombie_elite": "husk", "skeleton_warrior": "husk", "fallen": "husk",
          "wight": "husk", "mummy": "husk", "imp": "husk",
          "bile_fiend": "ghoul", "flesh_golem": "ghoul", "ogre": "ghoul", "gargoyle": "ghoul", "stygian_demon": "ghoul",
          "giant": "ghoul", "balrog_lite": "ghoul",
          "dark_cultist": "bandit_archer", "bone_mage": "bandit_archer", "wraith": "bandit_archer", "lich": "bandit_archer",
          "succubus": "bandit_archer", "harpy": "bandit_archer",
          # The Retcher (2026-10-17, the owner: "Make a puking enemy that spews bile at the character"): on the archer's
          # rig, so its draw reads as the heave before it spews.
          "retcher": "bandit_archer",
          # The quest people (2026-10-08): Mother Aldis, and those she brings back to town.
          "aldis": "bandit_archer", "tobin": "bandit_archer", "pellam": "bandit_archer", "bram": "bandit_archer", "ivy": "bandit_archer"}
NPCS = {"merchant", "pet_vendor", "stash_keeper", "healer", "gambler", "trainer", "aldis", "tobin", "pellam", "bram", "ivy"}
DONOR = DONORS[NAME]
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "Models", "NPCs", NAME) if NAME in NPCS else os.path.join(ENEMIES, NAME)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(RIGS, DONOR, DONOR + ".fbx"))
arm = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"][0]
for o in [o for o in bpy.context.scene.objects if o.type == "MESH"]:
    bpy.data.objects.remove(o, do_unlink=True)
bones = arm.data.bones

COLORS = {
    "bone": BONE[1], "bone_dark": BONE[0], "rust": WOOD[2], "iron": STONE[2], "eye": COLD[4],
    "robe": BLOOD[1], "robe_dark": STONE[0], "trim": EMBER[3], "skin": (102, 68, 52), "fire": EMBER[4],
    "hood": STONE[1],
    # The rest of act 1's roster (2026-10-05).
    "leather": WOOD[2], "leather_dark": WOOD[1], "scarf": BLOOD[2], "steel": STONE[4], "ash": STONE[3],
    "ash_dark": STONE[2], "ember": EMBER[2], "flesh": (118, 112, 84), "flesh_dark": (78, 74, 54), "rot": MOSS[2],
    "pus": (150, 142, 88),
    # The newcomers.
    "beard": BONE[0], "apron": WOOD[3], "tunic": WOOD[1], "brass": EMBER[3], "linen": BONE[1], "sash": MOSS[2],
    "plum": VIOLET[1], "plum_dark": VIOLET[0], "gold": EMBER[3], "mail": STONE[3], "tabard": BLOOD[2], "hair": WOOD[0],
    # The deep levels.
    "drowned_skin": (120, 140, 128), "drowned_dark": (70, 88, 84), "kelp": MOSS[1], "oilskin": (44, 62, 66),
    "rust_plate": (112, 72, 46), "bone_white": BONE[2], "priest_green": MOSS[3], "ghost_green": (150, 220, 140),
    "ash_black": (34, 30, 36), "void": VIOLET[1], "void_dark": VIOLET[0], "void_glow": (190, 120, 240), "coral": (190, 96, 86),
    "black_iron": (40, 40, 46),
    # The Ash Archivist and the Rift Mother (2026-10-11).
    "char_robe": (46, 38, 34), "char_dark": (26, 22, 22), "parchment": (200, 186, 150), "ink": (24, 20, 30),
    "mother_skin": (92, 60, 104), "mother_dark": (52, 32, 62),
    # The Gaoler and the Hanged Magistrate (2026-10-12).
    "jailer_skin": (112, 100, 90), "jailer_dark": (78, 68, 62), "hood_black": (30, 26, 26), "apron": (70, 46, 30),
    "wig": (214, 208, 192), "robe_black": (30, 28, 34), "rope": (124, 94, 62), "pale": (176, 170, 156),
    # The husk (and its champion and elite colourings), the ghoul, the bandit archer, the merchant, the beast tamer.
    "husk_skin": (112, 116, 92), "husk_dark": (70, 74, 56), "rag": (66, 56, 44), "frost": (150, 186, 214),
    "frost_dark": (88, 120, 150), "ash_skin": (52, 48, 50), "ash_rag": (30, 26, 26), "ghoul_skin": (182, 176, 162),
    "ghoul_dark": (122, 116, 104), "claw": (200, 190, 160), "bandit_hood": (60, 50, 40), "bow_wood": (110, 74, 44),
    "coat": (92, 64, 40), "coat_dark": (62, 42, 28), "pack": (120, 92, 58), "cloth_red": (120, 40, 34),
    "hat": (50, 40, 32), "fur": (110, 92, 70), "fur_dark": (72, 60, 46),
    # The concept roster's palettes (from the sheet's swatches).
    "zombie": (96, 112, 64), "zombie_dark": (60, 72, 40), "rotten": (110, 70, 40), "demon": (150, 34, 26),
    "demon_dark": (90, 20, 18), "horn": (60, 50, 44), "fiend": (110, 140, 60), "fiend_dark": (66, 88, 36),
    # The Retcher (2026-10-17): grey-yellow sick skin, a dark mouth, bright bile.
    "sick": (150, 140, 96), "sick_dark": (92, 84, 56), "maw": (40, 16, 16), "bile": (150, 200, 40),
    "wraith_blue": (60, 100, 150), "wraith_glow": (120, 190, 240), "wight_green": (90, 200, 90), "gauze": (176, 160, 120),
    "gauze_dark": (124, 110, 80), "golem": (120, 116, 90), "stitch": (60, 40, 30), "ogre": (130, 98, 64),
    "ogre_dark": (88, 64, 40), "stone_grey": (96, 96, 100), "stone_dark": (60, 60, 64), "lich_violet": (90, 60, 130),
    "lich_dark": (46, 30, 70), "succubus": (150, 110, 140), "wing": (70, 46, 66), "harpy": (110, 86, 60), "feather_dark": (64, 50, 36),
    "giant": (140, 110, 80), "giant_dark": (96, 72, 50), "lava": (255, 140, 40), "imp": (170, 50, 30),
    # The NPC sheet's palettes (2026-10-13).
    "royal_red": (130, 30, 30), "gold_trim": (200, 160, 70), "elf_green": (70, 100, 50), "elf_dark": (44, 64, 34),
    "scholar_blue": (50, 66, 120), "scholar_dark": (34, 44, 80), "cream": (210, 196, 170), "apron_brown": (110, 76, 46),
    "teal": (40, 80, 90), "teal_dark": (26, 52, 60), "silver": (190, 196, 204), "ginger": (170, 80, 40),
    "priest_white": (220, 214, 196), "grey_hair": (170, 166, 160), "purple": (84, 50, 120), "purple_dark": (52, 30, 80),
    "goggles": (60, 120, 130),
}
color_list = list(COLORS)
bm = bmesh.new()
deform = bm.verts.layers.deform.verify()
face_color = []
groups = {}

def head(name):
    return mathutils.Vector(bones["mixamorig:" + name].head_local)


def tail(name):
    return mathutils.Vector(bones["mixamorig:" + name].tail_local)



def group(bone):
    if bone not in groups:
        groups[bone] = len(groups)
    return groups[bone]


def add(geom_verts, faces, bone, color):
    g = group("mixamorig:" + bone)
    vs = []
    for co in geom_verts:
        v = bm.verts.new(co)
        v[deform][g] = 1.0
        vs.append(v)
    for f in faces:
        try:
            bm.faces.new([vs[i] for i in f])
            face_color.append(color_list.index(color))
        except ValueError:
            pass


def frame(direction):
    d = direction.normalized()
    up = mathutils.Vector((0, 0, 1)) if abs(d.z) < 0.9 else mathutils.Vector((1, 0, 0))
    a = d.cross(up).normalized()
    b = d.cross(a).normalized()
    return a, b


def limb(p0, p1, r0, r1, bone, color, sides=6):
    """A tapered prism from p0 to p1."""
    a, b = frame(p1 - p0)
    verts = []
    for p, r in ((p0, r0), (p1, r1)):
        for k in range(sides):
            t = 2 * math.pi * k / sides
            verts.append(p + (a * math.cos(t) + b * math.sin(t)) * r)
    faces = [[k, (k + 1) % sides, sides + (k + 1) % sides, sides + k] for k in range(sides)]
    faces.append(list(reversed(range(sides))))
    faces.append(list(range(sides, 2 * sides)))
    add(verts, faces, bone, color)


def block(centre, size, bone, color, rot=None):
    """A box of size (x, y, z) about centre, turned by rot (a Matrix) if given."""
    sx, sy, sz = (s / 2 for s in size)
    corners = [mathutils.Vector((x, y, z)) for x in (-sx, sx) for y in (-sy, sy) for z in (-sz, sz)]
    if rot is not None:
        corners = [rot @ c for c in corners]
    verts = [centre + c for c in corners]
    faces = [[0, 1, 3, 2], [4, 6, 7, 5], [0, 4, 5, 1], [2, 3, 7, 6], [0, 2, 6, 4], [1, 5, 7, 3]]
    add(verts, faces, bone, color)


def blob(centre, radii, bone, color, segs=8):
    verts = []
    rings = segs // 2
    for i in range(rings + 1):
        phi = math.pi * i / rings
        for k in range(segs):
            t = 2 * math.pi * k / segs
            verts.append(centre + mathutils.Vector((math.sin(phi) * math.cos(t) * radii[0], math.cos(phi) * radii[1], math.sin(phi) * math.sin(t) * radii[2])))
    faces = []
    for i in range(rings):
        for k in range(segs):
            j = (k + 1) % segs
            faces.append([i * segs + k, i * segs + j, (i + 1) * segs + j, (i + 1) * segs + k])
    add(verts, faces, bone, color)


V = mathutils.Vector

V = mathutils.Vector
top = head("HeadTop_End")
toe = head("LeftToe_End")
U = (top.y - toe.y) / 1.8   # about a metre in this rig's units


def mid(a, b, f=0.5):
    return head(a) + (head(b) - head(a)) * f


def skeleton():
    # Bare bones: a skull with glowing eyes, a ribcage, thin limbs with knobbed joints, a rusted blade in the right hand
    # and a dented pauldron.
    b = 0.035 * U
    h = head("Head")
    blob(h + V((0, 0.1 * U, 0.01 * U)), (0.095 * U, 0.11 * U, 0.1 * U), "Head", "bone")
    block(h + V((0, 0.03 * U, 0.06 * U)), (0.1 * U, 0.05 * U, 0.06 * U), "Head", "bone_dark")
    for sx in (-1, 1):
        block(h + V((sx * 0.035 * U, 0.11 * U, 0.085 * U)), (0.03 * U, 0.025 * U, 0.02 * U), "Head", "eye")
    limb(head("Neck"), head("Head"), 0.025 * U, 0.025 * U, "Neck", "bone_dark")
    limb(head("Hips"), head("Spine2"), 0.03 * U, 0.03 * U, "Spine", "bone")
    s2 = head("Spine2")
    for k in range(4):
        y = s2.y - k * 0.06 * U + 0.06 * U
        block(V((0, y, 0.02 * U)), (0.26 * U - k * 0.02 * U, 0.025 * U, 0.17 * U), "Spine2" if k < 2 else "Spine1", "bone")
    block(V((0, head("Hips").y, 0)), (0.22 * U, 0.08 * U, 0.12 * U), "Hips", "bone_dark")
    for side in ("Left", "Right"):
        limb(head(side + "Arm"), head(side + "ForeArm"), b, b * 0.8, side + "Arm", "bone")
        limb(head(side + "ForeArm"), head(side + "Hand"), b * 0.8, b * 0.7, side + "ForeArm", "bone")
        blob(head(side + "ForeArm"), (b * 1.4, b * 1.4, b * 1.4), side + "Arm", "bone_dark")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.5, (b * 1.3, b * 1.0, b * 1.2), side + "Hand", "bone")
        limb(head(side + "UpLeg"), head(side + "Leg"), b * 1.1, b * 0.9, side + "UpLeg", "bone")
        limb(head(side + "Leg"), head(side + "Foot"), b * 0.9, b * 0.7, side + "Leg", "bone")
        blob(head(side + "Leg"), (b * 1.5, b * 1.5, b * 1.5), side + "Leg", "bone_dark")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.07 * U, 0.04 * U, 0.16 * U), side + "Foot", "bone")
    # A rusted blade in the right hand, pointing along the hand.
    hand = head("RightHand")
    d = (tail("RightHand") - hand).normalized()
    limb(hand, hand + d * 0.75 * U, 0.03 * U, 0.008 * U, "RightHand", "rust", 4)
    block(hand + d * 0.04 * U, (0.16 * U, 0.03 * U, 0.03 * U), "RightHand", "iron")
    blob(head("LeftArm") + V((0, 0.03 * U, 0)), (0.09 * U, 0.05 * U, 0.08 * U), "LeftArm", "iron")


def cultist():
    # A hooded robe to the ankles, a dark face in the hood with two embers for eyes, sleeves, a bone mask, a staff
    # topped with fire in the left hand (the hand that draws the bow).
    h = head("Head")
    blob(h + V((0, 0.1 * U, -0.01 * U)), (0.12 * U, 0.14 * U, 0.12 * U), "Head", "hood")
    blob(h + V((0, 0.08 * U, 0.04 * U)), (0.08 * U, 0.09 * U, 0.07 * U), "Head", "robe_dark")
    for sx in (-1, 1):
        block(h + V((sx * 0.03 * U, 0.1 * U, 0.11 * U)), (0.025 * U, 0.02 * U, 0.02 * U), "Head", "fire")
    block(h + V((0, 0.05 * U, 0.11 * U)), (0.07 * U, 0.04 * U, 0.02 * U), "Head", "bone")
    # The torso and the robe's upper part.
    s2 = head("Spine2")
    block(V((0, s2.y, 0)), (0.32 * U, 0.2 * U, 0.2 * U), "Spine2", "robe")
    block(V((0, head("Spine1").y, 0)), (0.28 * U, 0.12 * U, 0.19 * U), "Spine1", "robe")
    block(V((0, head("Spine").y - 0.02 * U, 0)), (0.27 * U, 0.12 * U, 0.19 * U), "Spine", "trim")
    # The skirt: wide panels down each thigh and shin, so the legs' swing shows under the robe.
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.11 * U, 0.12 * U, side + "UpLeg", "robe")
        limb(head(side + "Leg"), head(side + "Foot") + V((0, 0.03 * U, 0)), 0.12 * U, 0.14 * U, side + "Leg", "robe_dark")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.08 * U, 0.05 * U, 0.15 * U), side + "Foot", "robe_dark")
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.06 * U, 0.06 * U, side + "Arm", "robe")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.06 * U, 0.08 * U, side + "ForeArm", "robe")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.04 * U, 0.04 * U, 0.04 * U), side + "Hand", "skin")
    # The staff: upright through the left fist, a fire on top.
    hand = head("LeftHand") + (tail("LeftHand") - head("LeftHand")) * 0.4
    limb(hand - V((0, 0.8 * U, 0)), hand + V((0, 0.7 * U, 0)), 0.02 * U, 0.02 * U, "LeftHand", "rust", 5)
    blob(hand + V((0, 0.78 * U, 0)), (0.06 * U, 0.08 * U, 0.06 * U), "LeftHand", "fire")
    block(hand + V((0, 0.68 * U, 0)), (0.1 * U, 0.03 * U, 0.03 * U), "LeftHand", "bone_dark")


def cutthroat():
    # A lean bandit in dark leather, a red scarf over the face, a hood, and a long knife in each hand.
    h = head("Head")
    blob(h + V((0, 0.1 * U, -0.01 * U)), (0.105 * U, 0.12 * U, 0.105 * U), "Head", "leather_dark")
    block(h + V((0, 0.06 * U, 0.075 * U)), (0.13 * U, 0.06 * U, 0.05 * U), "Head", "scarf")
    for sx in (-1, 1):
        block(h + V((sx * 0.03 * U, 0.11 * U, 0.09 * U)), (0.025 * U, 0.015 * U, 0.02 * U), "Head", "skin")
    s2 = head("Spine2")
    block(V((0, s2.y, 0)), (0.28 * U, 0.18 * U, 0.17 * U), "Spine2", "leather")
    block(V((0, head("Spine1").y, 0)), (0.25 * U, 0.12 * U, 0.16 * U), "Spine1", "leather_dark")
    block(V((0, head("Spine").y - 0.02 * U, 0)), (0.26 * U, 0.06 * U, 0.17 * U), "Spine", "scarf")
    block(V((0, head("Hips").y, 0)), (0.26 * U, 0.1 * U, 0.16 * U), "Hips", "leather_dark")
    for side in ("Left", "Right"):
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.045 * U, 0.04 * U, side + "Arm", "leather")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.04 * U, 0.035 * U, side + "ForeArm", "leather_dark")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.035 * U, 0.035 * U, 0.035 * U), side + "Hand", "skin")
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.06 * U, 0.05 * U, side + "UpLeg", "leather_dark")
        limb(head(side + "Leg"), head(side + "Foot"), 0.05 * U, 0.04 * U, side + "Leg", "leather")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.07 * U, 0.05 * U, 0.15 * U), side + "Foot", "leather_dark")
        hand = head(side + "Hand")
        d = (tail(side + "Hand") - hand).normalized()
        limb(hand, hand + d * 0.38 * U, 0.022 * U, 0.004 * U, side + "Hand", "steel", 4)
        block(hand + d * 0.02 * U, (0.08 * U, 0.025 * U, 0.025 * U), side + "Hand", "rust")


def ember_acolyte():
    # An ash-grey robe with ember-orange trim, a bare head with a shaved scalp and glowing eyes, a smoking censer staff.
    h = head("Head")
    blob(h + V((0, 0.1 * U, 0)), (0.09 * U, 0.11 * U, 0.095 * U), "Head", "skin")
    for sx in (-1, 1):
        block(h + V((sx * 0.03 * U, 0.1 * U, 0.085 * U)), (0.025 * U, 0.018 * U, 0.02 * U), "Head", "fire")
    blob(h + V((0, 0.0, -0.02 * U)), (0.14 * U, 0.05 * U, 0.12 * U), "Neck", "ember")
    s2 = head("Spine2")
    block(V((0, s2.y, 0)), (0.3 * U, 0.2 * U, 0.19 * U), "Spine2", "ash")
    block(V((0, head("Spine1").y, 0)), (0.27 * U, 0.12 * U, 0.18 * U), "Spine1", "ash")
    block(V((0, head("Spine").y - 0.02 * U, 0)), (0.27 * U, 0.06 * U, 0.19 * U), "Spine", "ember")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.11 * U, 0.12 * U, side + "UpLeg", "ash")
        limb(head(side + "Leg"), head(side + "Foot") + V((0, 0.03 * U, 0)), 0.12 * U, 0.14 * U, side + "Leg", "ash_dark")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.08 * U, 0.05 * U, 0.15 * U), side + "Foot", "robe_dark")
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.06 * U, 0.06 * U, side + "Arm", "ash")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.06 * U, 0.085 * U, side + "ForeArm", "ember")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.04 * U, 0.04 * U, 0.04 * U), side + "Hand", "skin")
    hand = head("LeftHand") + (tail("LeftHand") - head("LeftHand")) * 0.4
    limb(hand - V((0, 0.7 * U, 0)), hand + V((0, 0.6 * U, 0)), 0.018 * U, 0.018 * U, "LeftHand", "iron", 5)
    blob(hand + V((0, 0.66 * U, 0)), (0.06 * U, 0.06 * U, 0.06 * U), "LeftHand", "iron")
    blob(hand + V((0, 0.7 * U, 0)), (0.04 * U, 0.05 * U, 0.04 * U), "LeftHand", "fire")


def pyre_keeper():
    # A tall, broad keeper in a heavy dark robe and a cowl, a tall pole topped with a burning iron brazier.
    h = head("Head")
    blob(h + V((0, 0.11 * U, -0.01 * U)), (0.13 * U, 0.15 * U, 0.13 * U), "Head", "robe_dark")
    blob(h + V((0, 0.08 * U, 0.05 * U)), (0.07 * U, 0.08 * U, 0.06 * U), "Head", "hood")
    block(h + V((0, 0.08 * U, 0.115 * U)), (0.08 * U, 0.06 * U, 0.02 * U), "Head", "iron")
    for sx in (-1, 1):
        block(h + V((sx * 0.025 * U, 0.09 * U, 0.127 * U)), (0.02 * U, 0.012 * U, 0.01 * U), "Head", "fire")
    s2 = head("Spine2")
    block(V((0, s2.y, 0)), (0.38 * U, 0.22 * U, 0.24 * U), "Spine2", "robe")
    blob(V((0, s2.y + 0.1 * U, 0)), (0.24 * U, 0.06 * U, 0.15 * U), "Spine2", "robe_dark")
    block(V((0, head("Spine1").y, 0)), (0.33 * U, 0.12 * U, 0.22 * U), "Spine1", "robe")
    block(V((0, head("Spine").y - 0.02 * U, 0)), (0.33 * U, 0.06 * U, 0.23 * U), "Spine", "iron")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.13 * U, 0.14 * U, side + "UpLeg", "robe")
        limb(head(side + "Leg"), head(side + "Foot") + V((0, 0.03 * U, 0)), 0.14 * U, 0.16 * U, side + "Leg", "robe_dark")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.09 * U, 0.05 * U, 0.16 * U), side + "Foot", "robe_dark")
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.075 * U, 0.075 * U, side + "Arm", "robe")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.075 * U, 0.09 * U, side + "ForeArm", "robe_dark")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.045 * U, 0.045 * U, 0.045 * U), side + "Hand", "skin")
    hand = head("LeftHand") + (tail("LeftHand") - head("LeftHand")) * 0.4
    limb(hand - V((0, 0.85 * U, 0)), hand + V((0, 0.9 * U, 0)), 0.025 * U, 0.025 * U, "LeftHand", "rust", 5)
    top = hand + V((0, 0.9 * U, 0))
    for k in range(4):
        a = k * math.pi / 2
        limb(top, top + V((math.cos(a) * 0.1 * U, 0.12 * U, math.sin(a) * 0.1 * U)), 0.012 * U, 0.012 * U, "LeftHand", "iron", 4)
    blob(top + V((0, 0.1 * U, 0)), (0.1 * U, 0.04 * U, 0.1 * U), "LeftHand", "iron")
    blob(top + V((0, 0.17 * U, 0)), (0.08 * U, 0.1 * U, 0.08 * U), "LeftHand", "fire")
    blob(top + V((0, 0.13 * U, 0)), (0.09 * U, 0.05 * U, 0.09 * U), "LeftHand", "ember")


def carrion_bloat():
    # A swollen corpse: a huge belly sagging over short legs, small arms, a lolling head, pale rotting skin with dark
    # patches and pus boils.
    h = head("Head")
    blob(h + V((0, 0.06 * U, 0.04 * U)), (0.1 * U, 0.1 * U, 0.1 * U), "Head", "flesh")
    block(h + V((0, 0.02 * U, 0.12 * U)), (0.07 * U, 0.03 * U, 0.02 * U), "Head", "robe_dark")
    for sx in (-1, 1):
        block(h + V((sx * 0.035 * U, 0.08 * U, 0.12 * U)), (0.02 * U, 0.02 * U, 0.02 * U), "Head", "pus")
    s1 = head("Spine1")
    blob(V((0, s1.y, 0.06 * U)), (0.34 * U, 0.36 * U, 0.34 * U), "Spine1", "flesh", 12)
    blob(V((0.12 * U, s1.y + 0.1 * U, 0.3 * U)), (0.12 * U, 0.1 * U, 0.08 * U), "Spine1", "flesh_dark")
    blob(V((-0.16 * U, s1.y - 0.12 * U, 0.26 * U)), (0.1 * U, 0.09 * U, 0.08 * U), "Spine1", "rot")
    blob(head("Spine2") + V((0, 0.05 * U, 0)), (0.3 * U, 0.16 * U, 0.26 * U), "Spine2", "flesh_dark")
    for (x, y, z, r) in ((0.2, 0.05, 0.28, 0.05), (-0.05, -0.15, 0.36, 0.045), (-0.25, 0.12, 0.18, 0.04), (0.28, -0.1, 0.1, 0.04)):
        blob(V((x * U, s1.y + y * U, z * U)), (r * U, r * U, r * U), "Spine1", "pus")
    block(V((0, head("Hips").y, 0)), (0.36 * U, 0.14 * U, 0.26 * U), "Hips", "flesh_dark")
    for side in ("Left", "Right"):
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.07 * U, 0.06 * U, side + "Arm", "flesh")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.06 * U, 0.05 * U, side + "ForeArm", "flesh_dark")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.06 * U, 0.05 * U, 0.06 * U), side + "Hand", "flesh")
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.12 * U, 0.1 * U, side + "UpLeg", "flesh")
        limb(head(side + "Leg"), head(side + "Foot"), 0.1 * U, 0.08 * U, side + "Leg", "flesh_dark")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.1 * U, 0.06 * U, 0.17 * U), side + "Foot", "flesh_dark")


def _townsfolk_body(top, top_dark, legs, boots, skin="skin", wide=1.0):
    """The shared figure for the townsfolk: torso, arms, legs and boots in the given colours."""
    s2 = head("Spine2")
    block(V((0, s2.y, 0)), (0.3 * U * wide, 0.2 * U, 0.19 * U * wide), "Spine2", top)
    block(V((0, head("Spine1").y, 0)), (0.27 * U * wide, 0.12 * U, 0.18 * U * wide), "Spine1", top)
    block(V((0, head("Spine").y - 0.02 * U, 0)), (0.27 * U * wide, 0.07 * U, 0.19 * U * wide), "Spine", top_dark)
    block(V((0, head("Hips").y, 0)), (0.26 * U * wide, 0.1 * U, 0.17 * U * wide), "Hips", legs)
    for side in ("Left", "Right"):
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.055 * U, 0.05 * U, side + "Arm", top)
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.05 * U, 0.045 * U, side + "ForeArm", top_dark)
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.04 * U, 0.04 * U, 0.04 * U), side + "Hand", skin)
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.065 * U, 0.055 * U, side + "UpLeg", legs)
        limb(head(side + "Leg"), head(side + "Foot"), 0.055 * U, 0.045 * U, side + "Leg", legs)
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.08 * U, 0.06 * U, 0.16 * U), side + "Foot", boots)
    limb(head("Neck"), head("Head"), 0.04 * U, 0.04 * U, "Neck", skin)


def stash_keeper():
    # A stout old man: bald, a grey beard, a brown tunic under a leather apron, a ring of brass keys at his belt.
    h = head("Head")
    _townsfolk_body("tunic", "leather_dark", "leather_dark", "robe_dark", wide=1.15)
    blob(h + V((0, 0.1 * U, 0)), (0.09 * U, 0.105 * U, 0.095 * U), "Head", "skin")
    blob(h + V((0, 0.02 * U, 0.06 * U)), (0.07 * U, 0.08 * U, 0.05 * U), "Head", "beard")
    block(V((0, head("Spine1").y, 0.1 * U)), (0.24 * U, 0.34 * U, 0.03 * U), "Spine1", "apron")
    blob(V((0.1 * U, head("Spine").y - 0.04 * U, 0.08 * U)), (0.04 * U, 0.05 * U, 0.02 * U), "Spine", "brass")


def healer():
    # A woman in a pale linen robe with a green sash, dark hair tied back, a satchel at her hip.
    h = head("Head")
    _townsfolk_body("linen", "sash", "linen", "leather_dark")
    blob(h + V((0, 0.1 * U, 0)), (0.085 * U, 0.1 * U, 0.09 * U), "Head", "skin")
    blob(h + V((0, 0.13 * U, -0.03 * U)), (0.095 * U, 0.09 * U, 0.08 * U), "Head", "hair")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.1 * U, 0.12 * U, side + "UpLeg", "linen")
        limb(head(side + "Leg"), head(side + "Foot") + V((0, 0.03 * U, 0)), 0.12 * U, 0.13 * U, side + "Leg", "linen")
    block(V((0.13 * U, head("Hips").y, 0.02 * U)), (0.06 * U, 0.1 * U, 0.12 * U), "Hips", "leather")


def gambler():
    # A thin man in a plum coat with gold trim and a wide-brimmed hat.
    h = head("Head")
    _townsfolk_body("plum", "gold", "plum_dark", "robe_dark", wide=0.95)
    blob(h + V((0, 0.1 * U, 0)), (0.085 * U, 0.1 * U, 0.09 * U), "Head", "skin")
    block(h + V((0, 0.19 * U, 0)), (0.3 * U, 0.02 * U, 0.3 * U), "Head", "plum_dark")
    blob(h + V((0, 0.23 * U, 0)), (0.1 * U, 0.06 * U, 0.1 * U), "Head", "plum_dark")
    block(h + V((0, 0.205 * U, 0)), (0.2 * U, 0.02 * U, 0.2 * U), "Head", "gold")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.09 * U, 0.1 * U, side + "UpLeg", "plum")


def trainer():
    # A veteran soldier: a mail shirt under a red tabard, short dark hair, a sword at his hip.
    h = head("Head")
    _townsfolk_body("mail", "tabard", "leather_dark", "robe_dark", wide=1.1)
    blob(h + V((0, 0.1 * U, 0)), (0.09 * U, 0.105 * U, 0.095 * U), "Head", "skin")
    blob(h + V((0, 0.15 * U, -0.01 * U)), (0.092 * U, 0.06 * U, 0.09 * U), "Head", "hair")
    block(V((0, head("Spine1").y, 0.1 * U)), (0.2 * U, 0.4 * U, 0.03 * U), "Spine1", "tabard")
    hip = V((-0.16 * U, head("Hips").y, 0.0))
    limb(hip + V((0, 0.05 * U, 0)), hip - V((0, 0.6 * U, 0)), 0.02 * U, 0.01 * U, "Hips", "steel", 4)
    block(hip + V((0, 0.06 * U, 0)), (0.12 * U, 0.025 * U, 0.03 * U), "Hips", "iron")


def aldis():
    # An old woman, the keeper of the dead Vigil fire: a long ash-grey robe, a dark red shawl, white hair in a knot.
    h = head("Head")
    _townsfolk_body("ash", "ash_dark", "ash", "robe_dark", wide=0.95)
    blob(h + V((0, 0.1 * U, 0)), (0.085 * U, 0.1 * U, 0.09 * U), "Head", "skin")
    blob(h + V((0, 0.14 * U, -0.03 * U)), (0.09 * U, 0.08 * U, 0.085 * U), "Head", "bone_white")
    blob(h + V((0, 0.2 * U, -0.07 * U)), (0.04 * U, 0.04 * U, 0.04 * U), "Head", "bone_white")
    block(V((0, head("Spine2").y + 0.04 * U, 0)), (0.36 * U, 0.12 * U, 0.24 * U), "Spine2", "robe")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.11 * U, 0.13 * U, side + "UpLeg", "ash")
        limb(head(side + "Leg"), head(side + "Foot") + V((0, 0.03 * U, 0)), 0.13 * U, 0.14 * U, side + "Leg", "ash")


def tobin():
    # A skinny boy: a patched brown tunic, a satchel across the chest, a mop of dark hair.
    h = head("Head")
    _townsfolk_body("tunic", "leather", "leather_dark", "robe_dark", wide=0.85)
    blob(h + V((0, 0.1 * U, 0)), (0.08 * U, 0.095 * U, 0.085 * U), "Head", "skin")
    blob(h + V((0, 0.15 * U, -0.01 * U)), (0.09 * U, 0.06 * U, 0.09 * U), "Head", "hair")
    block(V((0.12 * U, head("Hips").y + 0.05 * U, 0.06 * U)), (0.08 * U, 0.1 * U, 0.06 * U), "Hips", "leather")


def pellam():
    # The lampwright: a leather apron over a grey shirt, a brass lamp hanging at his belt, spectacles, a bald crown.
    h = head("Head")
    _townsfolk_body("ash_dark", "leather_dark", "leather_dark", "robe_dark", wide=1.05)
    blob(h + V((0, 0.1 * U, 0)), (0.09 * U, 0.1 * U, 0.095 * U), "Head", "skin")
    blob(h + V((0, 0.08 * U, -0.04 * U)), (0.095 * U, 0.06 * U, 0.08 * U), "Head", "beard")
    block(V((0, head("Spine1").y, 0.1 * U)), (0.24 * U, 0.34 * U, 0.03 * U), "Spine1", "apron")
    block(h + V((0, 0.1 * U, 0.09 * U)), (0.12 * U, 0.02 * U, 0.02 * U), "Head", "brass")
    lamp = V((0.14 * U, head("Hips").y - 0.06 * U, 0.06 * U))
    blob(lamp, (0.04 * U, 0.06 * U, 0.04 * U), "Hips", "fire")
    block(lamp + V((0, 0.06 * U, 0)), (0.06 * U, 0.02 * U, 0.06 * U), "Hips", "brass")


def bram():
    # The ferryman: a wide oilskin coat, a fisherman's cap, a big grey beard.
    h = head("Head")
    _townsfolk_body("oilskin", "leather_dark", "oilskin", "robe_dark", wide=1.2)
    blob(h + V((0, 0.1 * U, 0)), (0.095 * U, 0.105 * U, 0.1 * U), "Head", "skin")
    blob(h + V((0, 0.03 * U, 0.06 * U)), (0.085 * U, 0.09 * U, 0.06 * U), "Head", "beard")
    blob(h + V((0, 0.19 * U, 0)), (0.1 * U, 0.05 * U, 0.1 * U), "Head", "oilskin")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.1 * U, 0.11 * U, side + "UpLeg", "oilskin")


def ivy():
    # A young sister of the saint's order: a black habit, a pale wimple, a red cord at the waist.
    h = head("Head")
    _townsfolk_body("robe_dark", "robe_dark", "robe_dark", "robe_dark", wide=0.9)
    blob(h + V((0, 0.1 * U, 0)), (0.08 * U, 0.095 * U, 0.085 * U), "Head", "skin")
    blob(h + V((0, 0.12 * U, -0.02 * U)), (0.1 * U, 0.11 * U, 0.1 * U), "Head", "linen")
    block(V((0, head("Spine").y, 0)), (0.29 * U, 0.03 * U, 0.2 * U), "Spine", "scarf")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.1 * U, 0.12 * U, side + "UpLeg", "robe_dark")
        limb(head(side + "Leg"), head(side + "Foot") + V((0, 0.03 * U, 0)), 0.12 * U, 0.13 * U, side + "Leg", "robe_dark")


def drowned():
    # A drowned man: swollen pale blue-green skin, rags, kelp hanging from the shoulders.
    h = head("Head")
    _townsfolk_body("drowned_skin", "drowned_dark", "drowned_dark", "drowned_dark", skin="drowned_skin", wide=1.1)
    blob(h + V((0, 0.1 * U, 0)), (0.1 * U, 0.11 * U, 0.1 * U), "Head", "drowned_skin")
    for sx in (-1, 1):
        block(h + V((sx * 0.035 * U, 0.1 * U, 0.09 * U)), (0.025 * U, 0.02 * U, 0.02 * U), "Head", "eye")
        limb(head("Spine2") + V((sx * 0.13 * U, 0.08 * U, 0.05 * U)), head("Spine") + V((sx * 0.15 * U, -0.1 * U, 0.08 * U)), 0.025 * U, 0.015 * U, "Spine2", "kelp", 4)
    block(V((0, head("Hips").y - 0.05 * U, 0)), (0.3 * U, 0.16 * U, 0.2 * U), "Hips", "kelp")


def harpooner():
    # A whaler in a dark oilskin coat and hood, a long barbed harpoon in the left hand.
    h = head("Head")
    _townsfolk_body("oilskin", "leather_dark", "oilskin", "robe_dark")
    blob(h + V((0, 0.1 * U, -0.01 * U)), (0.11 * U, 0.125 * U, 0.11 * U), "Head", "oilskin")
    blob(h + V((0, 0.08 * U, 0.05 * U)), (0.07 * U, 0.08 * U, 0.05 * U), "Head", "drowned_skin")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.1 * U, 0.11 * U, side + "UpLeg", "oilskin")
    hand = head("LeftHand") + (tail("LeftHand") - head("LeftHand")) * 0.4
    limb(hand - V((0, 0.7 * U, 0)), hand + V((0, 0.8 * U, 0)), 0.018 * U, 0.018 * U, "LeftHand", "rust", 5)
    limb(hand + V((0, 0.8 * U, 0)), hand + V((0, 1.0 * U, 0)), 0.04 * U, 0.002 * U, "LeftHand", "steel", 4)
    block(hand + V((0, 0.85 * U, 0)), (0.1 * U, 0.02 * U, 0.02 * U), "LeftHand", "steel")


def drowned_watchman():
    # A drowned guard of the old watch: rusted plates over a swollen body, kelp, a heavy halberd.
    h = head("Head")
    _townsfolk_body("drowned_skin", "rust_plate", "drowned_dark", "rust_plate", skin="drowned_skin", wide=1.3)
    blob(h + V((0, 0.08 * U, 0.02 * U)), (0.11 * U, 0.12 * U, 0.11 * U), "Head", "rust_plate")
    block(h + V((0, 0.07 * U, 0.11 * U)), (0.12 * U, 0.025 * U, 0.02 * U), "Head", "ash_black")
    block(V((0, head("Spine2").y, 0.04 * U)), (0.42 * U, 0.26 * U, 0.26 * U), "Spine2", "rust_plate")
    for sx in (-1, 1):
        blob(head("LeftArm" if sx < 0 else "RightArm") + V((0, 0.04 * U, 0)), (0.11 * U, 0.07 * U, 0.1 * U), "LeftArm" if sx < 0 else "RightArm", "rust_plate")
    hand = head("RightHand") + (tail("RightHand") - head("RightHand")) * 0.4
    d = (tail("RightHand") - head("RightHand")).normalized()
    limb(hand - d * 0.5 * U, hand + d * 0.9 * U, 0.025 * U, 0.025 * U, "RightHand", "wood_dark" if "wood_dark" in COLORS else "rust", 5)
    block(hand + d * 0.85 * U, (0.24 * U, 0.04 * U, 0.12 * U), "RightHand", "steel")


def skeleton_knight():
    # An armoured skeleton: bones under an iron breastplate and helm, a round shield on the left arm, a sword in the right.
    h = head("Head")
    b = 0.04 * U
    blob(h + V((0, 0.1 * U, 0.01 * U)), (0.11 * U, 0.12 * U, 0.11 * U), "Head", "black_iron")
    block(h + V((0, 0.09 * U, 0.11 * U)), (0.12 * U, 0.025 * U, 0.02 * U), "Head", "eye")
    block(V((0, head("Spine2").y, 0.02 * U)), (0.38 * U, 0.28 * U, 0.24 * U), "Spine2", "black_iron")
    block(V((0, head("Spine1").y, 0)), (0.2 * U, 0.12 * U, 0.12 * U), "Spine1", "bone")
    block(V((0, head("Hips").y, 0)), (0.3 * U, 0.12 * U, 0.18 * U), "Hips", "black_iron")
    for side in ("Left", "Right"):
        limb(head(side + "Arm"), head(side + "ForeArm"), b, b * 0.8, side + "Arm", "bone")
        limb(head(side + "ForeArm"), head(side + "Hand"), b * 0.8, b * 0.7, side + "ForeArm", "bone")
        blob(head(side + "Arm") + V((0, 0.04 * U, 0)), (0.1 * U, 0.06 * U, 0.09 * U), side + "Arm", "black_iron")
        limb(head(side + "UpLeg"), head(side + "Leg"), b * 1.1, b * 0.9, side + "UpLeg", "bone")
        limb(head(side + "Leg"), head(side + "Foot"), b * 0.9, b * 0.7, side + "Leg", "black_iron")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.08 * U, 0.05 * U, 0.17 * U), side + "Foot", "black_iron")
    fore = head("LeftForeArm") + (head("LeftHand") - head("LeftForeArm")) * 0.5
    blob(fore + V((0.06 * U, 0, 0)), (0.03 * U, 0.2 * U, 0.2 * U), "LeftForeArm", "rust_plate")
    hand = head("RightHand")
    d = (tail("RightHand") - hand).normalized()
    limb(hand, hand + d * 0.8 * U, 0.035 * U, 0.008 * U, "RightHand", "steel", 4)
    block(hand + d * 0.04 * U, (0.18 * U, 0.03 * U, 0.03 * U), "RightHand", "iron")


def grave_priest():
    # A priest of the ossuary: a bone-white robe with green trim, a skull mask, a censer of green fire on a staff.
    h = head("Head")
    blob(h + V((0, 0.1 * U, -0.01 * U)), (0.12 * U, 0.14 * U, 0.12 * U), "Head", "bone_white")
    block(h + V((0, 0.08 * U, 0.1 * U)), (0.08 * U, 0.09 * U, 0.03 * U), "Head", "bone")
    for sx in (-1, 1):
        block(h + V((sx * 0.025 * U, 0.1 * U, 0.115 * U)), (0.02 * U, 0.015 * U, 0.01 * U), "Head", "ghost_green")
    s2 = head("Spine2")
    block(V((0, s2.y, 0)), (0.32 * U, 0.2 * U, 0.2 * U), "Spine2", "bone_white")
    block(V((0, head("Spine1").y, 0)), (0.28 * U, 0.12 * U, 0.19 * U), "Spine1", "bone_white")
    block(V((0, head("Spine").y - 0.02 * U, 0)), (0.27 * U, 0.06 * U, 0.19 * U), "Spine", "priest_green")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.11 * U, 0.12 * U, side + "UpLeg", "bone_white")
        limb(head(side + "Leg"), head(side + "Foot") + V((0, 0.03 * U, 0)), 0.12 * U, 0.14 * U, side + "Leg", "bone_white")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.08 * U, 0.05 * U, 0.15 * U), side + "Foot", "robe_dark")
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.06 * U, 0.06 * U, side + "Arm", "bone_white")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.06 * U, 0.08 * U, side + "ForeArm", "priest_green")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.04 * U, 0.04 * U, 0.04 * U), side + "Hand", "bone")
    hand = head("LeftHand") + (tail("LeftHand") - head("LeftHand")) * 0.4
    limb(hand - V((0, 0.7 * U, 0)), hand + V((0, 0.65 * U, 0)), 0.018 * U, 0.018 * U, "LeftHand", "bone", 5)
    blob(hand + V((0, 0.72 * U, 0)), (0.06 * U, 0.06 * U, 0.06 * U), "LeftHand", "iron")
    blob(hand + V((0, 0.76 * U, 0)), (0.04 * U, 0.05 * U, 0.04 * U), "LeftHand", "ghost_green")


def hollowed():
    # The Hollow's husk: ashen-black skin split by violet cracks, violet eyes.
    h = head("Head")
    _townsfolk_body("ash_black", "void_dark", "ash_black", "ash_black", skin="ash_black", wide=0.95)
    blob(h + V((0, 0.1 * U, 0)), (0.09 * U, 0.11 * U, 0.095 * U), "Head", "ash_black")
    for sx in (-1, 1):
        block(h + V((sx * 0.035 * U, 0.1 * U, 0.09 * U)), (0.03 * U, 0.02 * U, 0.02 * U), "Head", "void_glow")
        block(V((sx * 0.06 * U, head("Spine2").y, 0.1 * U)), (0.02 * U, 0.16 * U, 0.01 * U), "Spine2", "void_glow")


def void_wraith():
    # A wraith of the Hollow: a tattered violet-black cloak, a hood with a glowing face, long claws.
    h = head("Head")
    _townsfolk_body("void_dark", "void", "void_dark", "ash_black", skin="ash_black", wide=1.0)
    blob(h + V((0, 0.12 * U, -0.02 * U)), (0.12 * U, 0.15 * U, 0.12 * U), "Head", "void_dark")
    blob(h + V((0, 0.09 * U, 0.05 * U)), (0.06 * U, 0.07 * U, 0.04 * U), "Head", "void_glow")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.12 * U, 0.14 * U, side + "UpLeg", "void_dark")
        limb(head(side + "Leg"), head(side + "Foot") + V((0, 0.05 * U, 0)), 0.14 * U, 0.06 * U, side + "Leg", "void")
        hand = head(side + "Hand")
        d = (tail(side + "Hand") - hand).normalized()
        for k in (-1, 0, 1):
            limb(hand, hand + d * 0.22 * U + V((k * 0.03 * U, 0, 0)), 0.01 * U, 0.002 * U, side + "Hand", "void_glow", 3)


def rift_caller():
    # A caster of the Hollow: a violet robe with black, a deep hood, a staff with a violet orb.
    h = head("Head")
    blob(h + V((0, 0.1 * U, -0.01 * U)), (0.12 * U, 0.14 * U, 0.12 * U), "Head", "ash_black")
    for sx in (-1, 1):
        block(h + V((sx * 0.03 * U, 0.1 * U, 0.11 * U)), (0.025 * U, 0.02 * U, 0.02 * U), "Head", "void_glow")
    s2 = head("Spine2")
    block(V((0, s2.y, 0)), (0.32 * U, 0.2 * U, 0.2 * U), "Spine2", "void")
    block(V((0, head("Spine1").y, 0)), (0.28 * U, 0.12 * U, 0.19 * U), "Spine1", "void")
    block(V((0, head("Spine").y - 0.02 * U, 0)), (0.27 * U, 0.06 * U, 0.19 * U), "Spine", "ash_black")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.11 * U, 0.12 * U, side + "UpLeg", "void")
        limb(head(side + "Leg"), head(side + "Foot") + V((0, 0.03 * U, 0)), 0.12 * U, 0.14 * U, side + "Leg", "void_dark")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.08 * U, 0.05 * U, 0.15 * U), side + "Foot", "ash_black")
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.06 * U, 0.06 * U, side + "Arm", "void")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.06 * U, 0.08 * U, side + "ForeArm", "void_dark")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.04 * U, 0.04 * U, 0.04 * U), side + "Hand", "ash_black")
    hand = head("LeftHand") + (tail("LeftHand") - head("LeftHand")) * 0.4
    limb(hand - V((0, 0.75 * U, 0)), hand + V((0, 0.7 * U, 0)), 0.02 * U, 0.02 * U, "LeftHand", "black_iron", 5)
    blob(hand + V((0, 0.8 * U, 0)), (0.07 * U, 0.07 * U, 0.07 * U), "LeftHand", "void_glow")


def tidewife():
    # The Tidewife: a tall drowned queen in a long kelp-green gown, wet black hair to the waist, a coral crown, and
    # tentacles hanging from her arms and back.
    h = head("Head")
    _townsfolk_body("kelp", "drowned_dark", "kelp", "drowned_dark", skin="drowned_skin", wide=1.1)
    blob(h + V((0, 0.1 * U, 0)), (0.1 * U, 0.12 * U, 0.1 * U), "Head", "drowned_skin")
    blob(h + V((0, 0.05 * U, -0.07 * U)), (0.12 * U, 0.28 * U, 0.07 * U), "Head", "ash_black")
    for k in range(5):
        a = (k - 2) * 0.35
        limb(h + V((math.sin(a) * 0.08 * U, 0.2 * U, math.cos(a) * 0.04 * U)), h + V((math.sin(a) * 0.12 * U, 0.34 * U, math.cos(a) * 0.05 * U)), 0.02 * U, 0.005 * U, "Head", "coral", 4)
    for sx in (-1, 1):
        block(h + V((sx * 0.035 * U, 0.1 * U, 0.09 * U)), (0.03 * U, 0.02 * U, 0.02 * U), "Head", "eye")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.14 * U, 0.17 * U, side + "UpLeg", "kelp")
        limb(head(side + "Leg"), head(side + "Foot") + V((0, 0.03 * U, 0)), 0.17 * U, 0.2 * U, side + "Leg", "drowned_dark")
        for k in range(3):
            start = head(side + "ForeArm") + (head(side + "Hand") - head(side + "ForeArm")) * (0.2 + 0.3 * k)
            limb(start, start + V((0, -0.35 * U, 0.05 * U * (k - 1))), 0.035 * U, 0.008 * U, side + "ForeArm", "drowned_skin", 5)
    for k in range(4):
        x = (k - 1.5) * 0.09 * U
        limb(head("Spine2") + V((x, 0.05 * U, -0.12 * U)), head("Spine2") + V((x * 1.6, -0.5 * U, -0.32 * U)), 0.05 * U, 0.01 * U, "Spine2", "drowned_skin", 5)


def saint_marrow():
    # Saint Marrow: a tall saint in a white and gold robe, a skull for a face under a hood, a halo of bones behind the
    # head, a crozier of bone in the left hand.
    h = head("Head")
    blob(h + V((0, 0.11 * U, -0.01 * U)), (0.13 * U, 0.15 * U, 0.13 * U), "Head", "bone_white")
    blob(h + V((0, 0.08 * U, 0.05 * U)), (0.075 * U, 0.09 * U, 0.06 * U), "Head", "bone")
    for sx in (-1, 1):
        block(h + V((sx * 0.03 * U, 0.1 * U, 0.105 * U)), (0.025 * U, 0.02 * U, 0.015 * U), "Head", "ghost_green")
    for k in range(9):
        a = math.pi * (0.1 + 0.8 * k / 8)
        p = h + V((math.cos(a) * 0.24 * U, 0.12 * U + math.sin(a) * 0.24 * U, -0.12 * U))
        blob(p, (0.03 * U, 0.03 * U, 0.03 * U), "Head", "bone")
    s2 = head("Spine2")
    block(V((0, s2.y, 0)), (0.4 * U, 0.24 * U, 0.26 * U), "Spine2", "bone_white")
    block(V((0, s2.y + 0.02 * U, 0.12 * U)), (0.08 * U, 0.24 * U, 0.02 * U), "Spine2", "gold")
    block(V((0, head("Spine1").y, 0)), (0.36 * U, 0.14 * U, 0.24 * U), "Spine1", "bone_white")
    block(V((0, head("Spine").y - 0.02 * U, 0)), (0.36 * U, 0.07 * U, 0.25 * U), "Spine", "gold")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.15 * U, 0.17 * U, side + "UpLeg", "bone_white")
        limb(head(side + "Leg"), head(side + "Foot") + V((0, 0.03 * U, 0)), 0.17 * U, 0.2 * U, side + "Leg", "bone_white")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.09 * U, 0.05 * U, 0.16 * U), side + "Foot", "gold")
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.08 * U, 0.08 * U, side + "Arm", "bone_white")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.08 * U, 0.1 * U, side + "ForeArm", "gold")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.045 * U, 0.045 * U, 0.045 * U), side + "Hand", "bone")
    hand = head("LeftHand") + (tail("LeftHand") - head("LeftHand")) * 0.4
    limb(hand - V((0, 0.9 * U, 0)), hand + V((0, 0.9 * U, 0)), 0.025 * U, 0.025 * U, "LeftHand", "bone", 5)
    blob(hand + V((0, 0.98 * U, 0.05 * U)), (0.07 * U, 0.08 * U, 0.05 * U), "LeftHand", "bone")


def first_watchman():
    # The First Watchman: the knight whose failure began the fall, in blackened plate with a violet ember burning in his
    # chest, a tattered cloak and a great sword.
    h = head("Head")
    _townsfolk_body("black_iron", "black_iron", "black_iron", "black_iron", skin="black_iron", wide=1.3)
    blob(h + V((0, 0.09 * U, 0.01 * U)), (0.12 * U, 0.14 * U, 0.12 * U), "Head", "black_iron")
    block(h + V((0, 0.09 * U, 0.12 * U)), (0.13 * U, 0.025 * U, 0.02 * U), "Head", "void_glow")
    block(V((0, head("Spine2").y + 0.02 * U, 0.04 * U)), (0.46 * U, 0.3 * U, 0.3 * U), "Spine2", "black_iron")
    blob(V((0, head("Spine2").y + 0.02 * U, 0.2 * U)), (0.06 * U, 0.06 * U, 0.03 * U), "Spine2", "void_glow")
    for side in ("LeftArm", "RightArm"):
        blob(head(side) + V((0, 0.05 * U, 0)), (0.14 * U, 0.09 * U, 0.13 * U), side, "black_iron")
    block(V((0, head("Spine1").y - 0.2 * U, -0.18 * U)), (0.5 * U, 0.9 * U, 0.03 * U), "Spine2", "void_dark")
    hand = head("RightHand")
    d = (tail("RightHand") - hand).normalized()
    limb(hand - d * 0.15 * U, hand + d * 1.3 * U, 0.06 * U, 0.015 * U, "RightHand", "steel", 4)
    block(hand + d * 0.02 * U, (0.3 * U, 0.04 * U, 0.04 * U), "RightHand", "gold")


def ash_archivist():
    # The Ash Archivist: a tall hooded scholar in charred robes, his face a burnt skull with ember eyes, a book alight in
    # his left hand, loose pages round his shoulders, a long quill in his right.
    h = head("Head")
    _townsfolk_body("char_robe", "char_dark", "char_robe", "char_dark", skin="char_dark", wide=1.25)
    blob(h + V((0, 0.1 * U, -0.01 * U)), (0.14 * U, 0.17 * U, 0.14 * U), "Head", "char_dark")
    blob(h + V((0, 0.08 * U, 0.06 * U)), (0.07 * U, 0.085 * U, 0.06 * U), "Head", "ash_black")
    for sx in (-1, 1):
        block(h + V((sx * 0.03 * U, 0.1 * U, 0.11 * U)), (0.03 * U, 0.02 * U, 0.015 * U), "Head", "fire")
    # The robe's long skirt, wide at the hem, on the hips and legs.
    block(V((0, head("Hips").y - 0.25 * U, 0)), (0.42 * U, 0.5 * U, 0.34 * U), "Hips", "char_robe")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg") - V((0, 0.05 * U, 0)), 0.17 * U, 0.2 * U, side + "UpLeg", "char_robe")
        limb(head(side + "Leg"), head(side + "Foot") + V((0, 0.02 * U, 0)), 0.2 * U, 0.23 * U, side + "Leg", "char_dark")
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.07 * U, 0.08 * U, side + "Arm", "char_robe")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.08 * U, 0.11 * U, side + "ForeArm", "char_robe")
    # Pages round his shoulders.
    for k in range(7):
        a = math.pi * (0.1 + 0.8 * k / 6)
        p = head("Spine2") + V((math.cos(a) * 0.32 * U, 0.15 * U + math.sin(a) * 0.12 * U, -0.1 * U))
        block(p, (0.08 * U, 0.1 * U, 0.01 * U), "Spine2", "parchment")
    # The burning book in his left hand, the quill in his right.
    hand = head("LeftHand") + (tail("LeftHand") - head("LeftHand")) * 0.5
    block(hand + V((0, 0.03 * U, 0.06 * U)), (0.2 * U, 0.04 * U, 0.15 * U), "LeftHand", "char_dark")
    block(hand + V((0, 0.06 * U, 0.06 * U)), (0.18 * U, 0.01 * U, 0.14 * U), "LeftHand", "parchment")
    for k in range(3):
        blob(hand + V(((k - 1) * 0.05 * U, 0.12 * U + 0.03 * U * (k % 2), 0.06 * U)), (0.035 * U, 0.08 * U, 0.035 * U), "LeftHand", "fire")
    q = head("RightHand") + (tail("RightHand") - head("RightHand")) * 0.5
    limb(q - V((0, 0.05 * U, 0)), q + V((0, 0.45 * U, 0.05 * U)), 0.012 * U, 0.004 * U, "RightHand", "ink", 4)
    block(q + V((0, 0.35 * U, 0.05 * U)), (0.06 * U, 0.16 * U, 0.01 * U), "RightHand", "parchment")


def rift_mother():
    # The Rift Mother: a swollen void-beast, hunched, her belly a glowing rift, sacs along her back, long clawed arms.
    h = head("Head")
    s2 = head("Spine2")
    blob(V((0, head("Spine").y, 0.08 * U)), (0.42 * U, 0.38 * U, 0.42 * U), "Spine", "mother_skin")
    blob(V((0, head("Spine").y, 0.36 * U)), (0.2 * U, 0.24 * U, 0.08 * U), "Spine", "void_glow")
    blob(V((0, s2.y, 0)), (0.36 * U, 0.26 * U, 0.32 * U), "Spine2", "mother_dark")
    for k in range(4):
        x = (k - 1.5) * 0.16 * U
        blob(s2 + V((x, 0.18 * U, -0.22 * U)), (0.09 * U, 0.11 * U, 0.09 * U), "Spine2", "void")
    blob(h + V((0, 0.06 * U, 0.04 * U)), (0.13 * U, 0.12 * U, 0.15 * U), "Head", "mother_dark")
    for sx in (-1, 0, 1):
        block(h + V((sx * 0.045 * U, 0.08 * U, 0.17 * U)), (0.025 * U, 0.025 * U, 0.015 * U), "Head", "void_glow")
    for k in range(4):
        a = (k - 1.5) * 0.4
        limb(h + V((math.sin(a) * 0.1 * U, 0.15 * U, -0.02 * U)), h + V((math.sin(a) * 0.2 * U, 0.35 * U, -0.12 * U)), 0.03 * U, 0.005 * U, "Head", "void_dark", 4)
    block(V((0, head("Hips").y, 0)), (0.4 * U, 0.16 * U, 0.3 * U), "Hips", "mother_dark")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.13 * U, 0.1 * U, side + "UpLeg", "mother_skin")
        limb(head(side + "Leg"), head(side + "Foot"), 0.1 * U, 0.07 * U, side + "Leg", "mother_dark")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.12 * U, 0.06 * U, 0.2 * U), side + "Foot", "mother_dark")
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.1 * U, 0.08 * U, side + "Arm", "mother_skin")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.08 * U, 0.06 * U, side + "ForeArm", "mother_dark")
        hand = head(side + "Hand")
        d = (tail(side + "Hand") - hand).normalized()
        for k in range(3):
            limb(hand, hand + d * 0.28 * U + V(((k - 1) * 0.05 * U, 0, 0.04 * U)), 0.025 * U, 0.004 * U, side + "Hand", "bone_white", 4)


def gaoler():
    # The Gaoler: a huge jailer, grey-skinned, bare-armed, an executioner's hood with eye holes, a leather apron
    # stained dark, a great ring of keys at his belt, a heavy chain wound round his right fist and hanging to the floor.
    h = head("Head")
    s2 = head("Spine2")
    blob(V((0, s2.y, 0.02 * U)), (0.3 * U, 0.24 * U, 0.24 * U), "Spine2", "jailer_skin")
    blob(V((0, head("Spine1").y, 0.04 * U)), (0.28 * U, 0.18 * U, 0.26 * U), "Spine1", "jailer_skin")
    blob(V((0, head("Spine").y, 0.06 * U)), (0.3 * U, 0.16 * U, 0.28 * U), "Spine", "jailer_dark")
    block(V((0, head("Spine").y - 0.15 * U, 0.2 * U)), (0.4 * U, 0.6 * U, 0.03 * U), "Spine", "apron")
    block(V((0, head("Hips").y, 0)), (0.42 * U, 0.12 * U, 0.3 * U), "Hips", "leather_dark")
    blob(V((0, s2.y + 0.18 * U, 0.02 * U)), (0.2 * U, 0.1 * U, 0.16 * U), "Spine2", "hood_black")
    blob(h + V((0, 0.08 * U, 0.02 * U)), (0.13 * U, 0.15 * U, 0.13 * U), "Head", "hood_black")
    cone_tip = h + V((0, 0.3 * U, -0.02 * U))
    limb(h + V((0, 0.18 * U, 0)), cone_tip, 0.09 * U, 0.01 * U, "Head", "hood_black", 6)
    for sx in (-1, 1):
        block(h + V((sx * 0.045 * U, 0.1 * U, 0.12 * U)), (0.035 * U, 0.02 * U, 0.02 * U), "Head", "fire")
    for side in ("Left", "Right"):
        blob(head(side + "Arm") + V((0, 0.03 * U, 0)), (0.13 * U, 0.11 * U, 0.12 * U), side + "Arm", "jailer_skin")
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.1 * U, 0.09 * U, side + "Arm", "jailer_skin")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.09 * U, 0.08 * U, side + "ForeArm", "jailer_dark")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.07 * U, 0.07 * U, 0.07 * U), side + "Hand", "jailer_skin")
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.12 * U, 0.1 * U, side + "UpLeg", "leather_dark")
        limb(head(side + "Leg"), head(side + "Foot"), 0.1 * U, 0.08 * U, side + "Leg", "leather")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.12 * U, 0.08 * U, 0.2 * U), side + "Foot", "black_iron")
    # The ring of keys at his left hip.
    ring = V((0.24 * U, head("Hips").y - 0.04 * U, 0.1 * U))
    for k in range(10):
        a = 2 * math.pi * k / 10
        block(ring + V((0, math.sin(a) * 0.08 * U, math.cos(a) * 0.08 * U)), (0.02 * U, 0.025 * U, 0.025 * U), "Hips", "rust")
    for k in range(5):
        block(ring + V((0.01 * U, -0.1 * U - 0.01 * U * (k % 2), (k - 2) * 0.035 * U)), (0.012 * U, 0.09 * U, 0.02 * U), "Hips", "iron")
    # The chain: wound round his right fist, hanging down past it.
    hand = head("RightHand") + (tail("RightHand") - head("RightHand")) * 0.4
    for k in range(12):
        p = hand + V((0, -0.06 * U * k, 0.02 * U * math.sin(k)))
        block(p, (0.035 * U, 0.05 * U, 0.02 * U) if k % 2 else (0.02 * U, 0.05 * U, 0.035 * U), "RightHand", "iron")
    blob(hand + V((0, -0.75 * U, 0)), (0.08 * U, 0.06 * U, 0.08 * U), "RightHand", "black_iron")


def hanged_magistrate():
    # The Hanged Magistrate: a gaunt judge in a long black robe with a white collar, a powdered wig over a pale dead face,
    # a noose round his neck with its cut rope hanging, a gavel in his right hand and a scroll of sentences in his left.
    h = head("Head")
    _townsfolk_body("robe_black", "robe_black", "robe_black", "robe_black", skin="pale", wide=1.15)
    block(V((0, head("Hips").y - 0.3 * U, 0)), (0.42 * U, 0.62 * U, 0.34 * U), "Hips", "robe_black")
    block(V((0, head("Spine2").y + 0.08 * U, 0.1 * U)), (0.14 * U, 0.08 * U, 0.04 * U), "Spine2", "linen")
    blob(h + V((0, 0.09 * U, 0.03 * U)), (0.1 * U, 0.13 * U, 0.1 * U), "Head", "pale")
    for sx in (-1, 1):
        block(h + V((sx * 0.035 * U, 0.1 * U, 0.115 * U)), (0.025 * U, 0.018 * U, 0.012 * U), "Head", "ghost_green")
    # The wig: a crown and rolls of curls down each side.
    blob(h + V((0, 0.18 * U, -0.01 * U)), (0.13 * U, 0.08 * U, 0.13 * U), "Head", "wig")
    for sx in (-1, 1):
        for k in range(3):
            blob(h + V((sx * 0.12 * U, 0.12 * U - 0.07 * U * k, -0.02 * U)), (0.045 * U, 0.04 * U, 0.06 * U), "Head", "wig")
    # The noose and its rope, cut, hanging over his shoulder.
    n = head("Neck")
    for k in range(10):
        a = 2 * math.pi * k / 10
        block(n + V((math.cos(a) * 0.06 * U, 0.02 * U, math.sin(a) * 0.06 * U)), (0.03 * U, 0.03 * U, 0.03 * U), "Neck", "rope")
    limb(n + V((0.05 * U, 0.04 * U, -0.03 * U)), n + V((0.12 * U, 0.45 * U, -0.08 * U)), 0.018 * U, 0.015 * U, "Neck", "rope", 5)
    # The gavel and the scroll.
    hand = head("RightHand") + (tail("RightHand") - head("RightHand")) * 0.4
    d = (tail("RightHand") - head("RightHand")).normalized()
    limb(hand - d * 0.05 * U, hand + d * 0.3 * U, 0.018 * U, 0.018 * U, "RightHand", "leather_dark", 5)
    blob(hand + d * 0.32 * U, (0.07 * U, 0.05 * U, 0.05 * U), "RightHand", "leather_dark")
    q = head("LeftHand") + (tail("LeftHand") - head("LeftHand")) * 0.4
    limb(q - V((0.06 * U, 0, 0)), q + V((0.06 * U, 0, 0)), 0.035 * U, 0.035 * U, "LeftHand", "parchment", 6)
    block(q + V((0, -0.12 * U, 0.02 * U)), (0.1 * U, 0.22 * U, 0.008 * U), "LeftHand", "parchment")


def husk(skin="husk_skin", dark="husk_dark", rag="rag", eye="eye"):
    # A starved dead man: grey-green skin over a gaunt frame, ribs showing, a jaw hanging, rags round the hips, long
    # bony hands. The champion is frost-blue, the elite ash-black with ember eyes (colourings of the same body).
    h = head("Head")
    blob(h + V((0, 0.1 * U, 0.01 * U)), (0.085 * U, 0.105 * U, 0.095 * U), "Head", skin)
    block(h + V((0, 0.03 * U, 0.065 * U)), (0.08 * U, 0.05 * U, 0.05 * U), "Head", dark)
    for sx in (-1, 1):
        block(h + V((sx * 0.032 * U, 0.115 * U, 0.08 * U)), (0.026 * U, 0.02 * U, 0.02 * U), "Head", eye)
    limb(head("Neck"), head("Head"), 0.035 * U, 0.03 * U, "Neck", skin)
    s2 = head("Spine2")
    block(V((0, s2.y, 0.0)), (0.25 * U, 0.18 * U, 0.15 * U), "Spine2", skin)
    for k in range(4):
        block(V((0, s2.y + 0.05 * U - k * 0.045 * U, 0.075 * U)), (0.2 * U - k * 0.02 * U, 0.012 * U, 0.01 * U), "Spine2", dark)
    block(V((0, head("Spine1").y, 0)), (0.2 * U, 0.12 * U, 0.12 * U), "Spine1", skin)
    block(V((0, head("Spine").y - 0.02 * U, 0)), (0.2 * U, 0.08 * U, 0.12 * U), "Spine", dark)
    block(V((0, head("Hips").y - 0.03 * U, 0)), (0.25 * U, 0.15 * U, 0.16 * U), "Hips", rag)
    for side in ("Left", "Right"):
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.04 * U, 0.032 * U, side + "Arm", skin)
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.032 * U, 0.026 * U, side + "ForeArm", skin)
        blob(head(side + "ForeArm"), (0.04 * U, 0.04 * U, 0.04 * U), side + "Arm", dark)
        hand = head(side + "Hand")
        d = (tail(side + "Hand") - hand).normalized()
        block(hand + d * 0.05 * U, (0.1 * U, 0.025 * U, 0.06 * U) if abs(d.x) > 0.5 else (0.06 * U, 0.1 * U, 0.025 * U), side + "Hand", skin)
        for k in (-1, 0, 1):
            limb(hand + d * 0.09 * U + V((0, 0, k * 0.02 * U)), hand + d * 0.17 * U + V((0, -0.01 * U, k * 0.025 * U)), 0.009 * U, 0.004 * U, side + "Hand", dark, 4)
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.055 * U, 0.04 * U, side + "UpLeg", skin)
        limb(head(side + "Leg"), head(side + "Foot"), 0.04 * U, 0.032 * U, side + "Leg", skin)
        blob(head(side + "Leg"), (0.045 * U, 0.045 * U, 0.045 * U), side + "Leg", dark)
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.07 * U, 0.04 * U, 0.15 * U), side + "Foot", dark)
        # A rag hanging from each hip.
        block(mid(side + "UpLeg", side + "Leg", 0.25) + V((0, 0, 0.05 * U)), (0.1 * U, 0.16 * U, 0.02 * U), side + "UpLeg", rag)


def husk_champion():
    husk("frost", "frost_dark", "ash_rag", "eye")


def husk_elite():
    husk("ash_skin", "ash_rag", "ash_rag", "fire")


def ghoul():
    # A hulking eater of the dead: pale bloated flesh, a hunched back with bone spurs, a small head sunk between huge
    # shoulders, long arms ending in claws, torn trousers.
    h = head("Head")
    s2 = head("Spine2")
    blob(V((0, s2.y + 0.02 * U, -0.02 * U)), (0.3 * U, 0.24 * U, 0.24 * U), "Spine2", "ghoul_skin")
    blob(V((0, s2.y + 0.12 * U, -0.12 * U)), (0.2 * U, 0.12 * U, 0.12 * U), "Spine2", "ghoul_dark")
    for k in range(3):
        limb(V(((k - 1) * 0.08 * U, s2.y + 0.15 * U, -0.18 * U)), V(((k - 1) * 0.1 * U, s2.y + 0.3 * U, -0.28 * U)), 0.03 * U, 0.004 * U, "Spine2", "claw", 4)
    blob(V((0, head("Spine1").y, 0.02 * U)), (0.26 * U, 0.18 * U, 0.22 * U), "Spine1", "ghoul_skin")
    blob(V((0, head("Spine").y, 0.04 * U)), (0.25 * U, 0.16 * U, 0.23 * U), "Spine", "ghoul_dark")
    block(V((0, head("Hips").y, 0)), (0.36 * U, 0.14 * U, 0.24 * U), "Hips", "rag")
    limb(head("Neck") - V((0, 0.06 * U, 0)), head("Head") + V((0, 0.04 * U, 0.03 * U)), 0.11 * U, 0.09 * U, "Neck", "ghoul_dark", 8)
    blob(h + V((0, 0.06 * U, 0.05 * U)), (0.12 * U, 0.12 * U, 0.13 * U), "Head", "ghoul_skin")
    block(h + V((0, 0.0, 0.12 * U)), (0.12 * U, 0.05 * U, 0.06 * U), "Head", "ghoul_dark")
    for k in range(4):
        block(h + V(((k - 1.5) * 0.025 * U, 0.03 * U, 0.15 * U)), (0.012 * U, 0.025 * U, 0.01 * U), "Head", "claw")
    for sx in (-1, 1):
        block(h + V((sx * 0.035 * U, 0.09 * U, 0.14 * U)), (0.025 * U, 0.018 * U, 0.015 * U), "Head", "eye")
    for side in ("Left", "Right"):
        blob(head(side + "Arm"), (0.13 * U, 0.12 * U, 0.12 * U), side + "Arm", "ghoul_skin")
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.1 * U, 0.08 * U, side + "Arm", "ghoul_skin")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.09 * U, 0.07 * U, side + "ForeArm", "ghoul_dark")
        hand = head(side + "Hand")
        d = (tail(side + "Hand") - hand).normalized()
        blob(hand + d * 0.06 * U, (0.08 * U, 0.07 * U, 0.08 * U), side + "Hand", "ghoul_skin")
        for k in (-1, 0, 1):
            limb(hand + d * 0.1 * U + V((0, 0, k * 0.04 * U)), hand + d * 0.28 * U + V((0, -0.03 * U, k * 0.05 * U)), 0.02 * U, 0.004 * U, side + "Hand", "claw", 4)
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.11 * U, 0.08 * U, side + "UpLeg", "rag")
        limb(head(side + "Leg"), head(side + "Foot"), 0.08 * U, 0.06 * U, side + "Leg", "ghoul_skin")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.12 * U, 0.06 * U, 0.2 * U), side + "Foot", "ghoul_dark")


def bandit_archer():
    # A bandit with a bow: a dark hood and a scarf over the face, a leather jerkin over a red shirt, a quiver on the
    # back, a short bow in the left hand.
    h = head("Head")
    blob(h + V((0, 0.1 * U, -0.01 * U)), (0.11 * U, 0.125 * U, 0.11 * U), "Head", "bandit_hood")
    block(h + V((0, 0.07 * U, 0.08 * U)), (0.12 * U, 0.05 * U, 0.04 * U), "Head", "scarf")
    block(h + V((0, 0.115 * U, 0.088 * U)), (0.1 * U, 0.03 * U, 0.02 * U), "Head", "skin")
    for sx in (-1, 1):
        block(h + V((sx * 0.028 * U, 0.115 * U, 0.1 * U)), (0.02 * U, 0.012 * U, 0.01 * U), "Head", "robe_dark")
    s2 = head("Spine2")
    block(V((0, s2.y, 0)), (0.29 * U, 0.19 * U, 0.18 * U), "Spine2", "leather")
    block(V((0, s2.y + 0.02 * U, 0.08 * U)), (0.1 * U, 0.15 * U, 0.03 * U), "Spine2", "cloth_red")
    block(V((0, head("Spine1").y, 0)), (0.26 * U, 0.12 * U, 0.17 * U), "Spine1", "leather")
    block(V((0, head("Spine").y - 0.02 * U, 0)), (0.27 * U, 0.06 * U, 0.18 * U), "Spine", "leather_dark")
    block(V((0, head("Hips").y, 0)), (0.27 * U, 0.1 * U, 0.17 * U), "Hips", "leather_dark")
    # The quiver on the back, arrows showing.
    block(V((0.06 * U, s2.y, -0.13 * U)), (0.09 * U, 0.4 * U, 0.08 * U), "Spine2", "leather_dark")
    for k in range(4):
        limb(V((0.04 * U + k * 0.015 * U, s2.y + 0.2 * U, -0.13 * U)), V((0.04 * U + k * 0.015 * U, s2.y + 0.3 * U, -0.13 * U)), 0.01 * U, 0.01 * U, "Spine2", "linen", 4)
    for side in ("Left", "Right"):
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.048 * U, 0.042 * U, side + "Arm", "cloth_red")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.042 * U, 0.036 * U, side + "ForeArm", "leather")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.036 * U, 0.036 * U, 0.036 * U), side + "Hand", "skin")
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.065 * U, 0.052 * U, side + "UpLeg", "rag")
        limb(head(side + "Leg"), head(side + "Foot"), 0.052 * U, 0.042 * U, side + "Leg", "leather_dark")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.075 * U, 0.05 * U, 0.16 * U), side + "Foot", "leather_dark")
    # The bow, upright through the left fist.
    hand = head("LeftHand") + (tail("LeftHand") - head("LeftHand")) * 0.4
    for sy in (-1, 1):
        limb(hand, hand + V((0, sy * 0.5 * U, -0.08 * U)), 0.018 * U, 0.01 * U, "LeftHand", "bow_wood", 5)
    limb(hand + V((0, 0.5 * U, -0.08 * U)), hand + V((0, -0.5 * U, -0.08 * U)), 0.003 * U, 0.003 * U, "LeftHand", "linen", 3)


def merchant():
    # A pack bearer: a long brown coat with a red sash, a wide hat, a beard, and a great pack of goods on his back with
    # a pot and a bedroll tied on.
    h = head("Head")
    _townsfolk_body("coat", "coat_dark", "coat_dark", "leather_dark", wide=1.15)
    blob(h + V((0, 0.1 * U, 0.01 * U)), (0.09 * U, 0.105 * U, 0.095 * U), "Head", "skin")
    block(h + V((0, 0.04 * U, 0.07 * U)), (0.11 * U, 0.08 * U, 0.05 * U), "Head", "beard")
    for sx in (-1, 1):
        block(h + V((sx * 0.03 * U, 0.115 * U, 0.088 * U)), (0.02 * U, 0.012 * U, 0.01 * U), "Head", "robe_dark")
    block(h + V((0, 0.19 * U, 0)), (0.3 * U, 0.025 * U, 0.3 * U), "Head", "hat")
    blob(h + V((0, 0.22 * U, 0)), (0.09 * U, 0.06 * U, 0.09 * U), "Head", "hat")
    block(V((0, head("Spine").y, 0)), (0.3 * U, 0.06 * U, 0.21 * U), "Spine", "cloth_red")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.1 * U, 0.11 * U, side + "UpLeg", "coat")
    s2 = head("Spine2")
    block(V((0, s2.y + 0.05 * U, -0.22 * U)), (0.34 * U, 0.5 * U, 0.22 * U), "Spine2", "pack")
    block(V((0, s2.y + 0.33 * U, -0.22 * U)), (0.38 * U, 0.09 * U, 0.12 * U), "Spine2", "cloth_red")
    blob(V((0.12 * U, s2.y - 0.1 * U, -0.36 * U)), (0.07 * U, 0.06 * U, 0.07 * U), "Spine2", "iron")
    for sx in (-1, 1):
        limb(V((sx * 0.1 * U, s2.y + 0.1 * U, 0.1 * U)), V((sx * 0.1 * U, s2.y + 0.1 * U, -0.12 * U)), 0.015 * U, 0.015 * U, "Spine2", "leather_dark", 4)


def pet_vendor():
    # A beast tamer: a fur cloak over the shoulders, a leather vest on bare arms, a braid, a whip coiled at the hip.
    h = head("Head")
    _townsfolk_body("leather", "leather_dark", "leather_dark", "leather_dark", skin="skin", wide=1.1)
    blob(h + V((0, 0.1 * U, 0.01 * U)), (0.088 * U, 0.105 * U, 0.095 * U), "Head", "skin")
    blob(h + V((0, 0.14 * U, -0.02 * U)), (0.095 * U, 0.08 * U, 0.1 * U), "Head", "hair")
    limb(h + V((0, 0.1 * U, -0.1 * U)), h + V((0, -0.15 * U, -0.13 * U)), 0.03 * U, 0.015 * U, "Head", "hair", 6)
    for sx in (-1, 1):
        block(h + V((sx * 0.03 * U, 0.11 * U, 0.088 * U)), (0.02 * U, 0.012 * U, 0.01 * U), "Head", "robe_dark")
    for side in ("Left", "Right"):
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.05 * U, 0.045 * U, side + "Arm", "skin")
    s2 = head("Spine2")
    blob(V((0, s2.y + 0.1 * U, -0.03 * U)), (0.25 * U, 0.08 * U, 0.17 * U), "Spine2", "fur")
    block(V((0, s2.y - 0.1 * U, -0.13 * U)), (0.34 * U, 0.4 * U, 0.04 * U), "Spine2", "fur_dark")
    ring = V((-0.17 * U, head("Hips").y, 0.05 * U))
    for k in range(10):
        a = 2 * math.pi * k / 10
        block(ring + V((0, math.sin(a) * 0.07 * U, math.cos(a) * 0.07 * U)), (0.025 * U, 0.03 * U, 0.03 * U), "Hips", "leather_dark")



def _body(top, top_dark, legs, feet, skin, wide=1.0, bulk=1.0):
    """A plain figure in blocks (any rig): torso, arms, legs, feet; bulk thickens limbs."""
    s2 = head("Spine2")
    block(V((0, s2.y, 0)), (0.3 * U * wide, 0.2 * U, 0.19 * U * wide), "Spine2", top)
    block(V((0, head("Spine1").y, 0)), (0.27 * U * wide, 0.12 * U, 0.18 * U * wide), "Spine1", top)
    block(V((0, head("Spine").y - 0.02 * U, 0)), (0.27 * U * wide, 0.07 * U, 0.19 * U * wide), "Spine", top_dark)
    block(V((0, head("Hips").y, 0)), (0.26 * U * wide, 0.1 * U, 0.17 * U * wide), "Hips", legs)
    for side in ("Left", "Right"):
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.055 * U * bulk, 0.05 * U * bulk, side + "Arm", top)
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.05 * U * bulk, 0.045 * U * bulk, side + "ForeArm", top_dark)
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.04 * U * bulk, 0.04 * U * bulk, 0.04 * U * bulk), side + "Hand", skin)
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.065 * U * bulk, 0.055 * U * bulk, side + "UpLeg", legs)
        limb(head(side + "Leg"), head(side + "Foot"), 0.055 * U * bulk, 0.045 * U * bulk, side + "Leg", legs)
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.08 * U * bulk, 0.06 * U, 0.16 * U * bulk), side + "Foot", feet)
    limb(head("Neck"), head("Head"), 0.04 * U * bulk, 0.04 * U * bulk, "Neck", skin)


def _head(color, eye="eye", r=(0.09, 0.105, 0.095), y=0.1, eyes_y=0.11, eyes_z=0.085):
    h = head("Head")
    blob(h + V((0, y * U, 0.01 * U)), (r[0] * U, r[1] * U, r[2] * U), "Head", color)
    for sx in (-1, 1):
        block(h + V((sx * 0.03 * U, eyes_y * U, eyes_z * U)), (0.024 * U, 0.016 * U, 0.016 * U), "Head", eye)
    return h


def _horns(h, color, size=1.0, up=0.18):
    for sx in (-1, 1):
        limb(h + V((sx * 0.06 * U, 0.17 * U, 0)), h + V((sx * 0.13 * U * size, (0.17 + up) * U * size, -0.06 * U)), 0.03 * U * size, 0.004 * U, "Head", color, 5)


def _wings(color, span=0.6, bone="Spine2", up=0.3):
    s2 = head("Spine2")
    for sx in (-1, 1):
        root = V((sx * 0.08 * U, s2.y + 0.05 * U, -0.12 * U))
        tip = V((sx * span * U, s2.y + up * U, -0.3 * U))
        limb(root, tip, 0.025 * U, 0.01 * U, bone, color, 4)
        for k in range(3):
            f = 0.4 + 0.3 * k
            p = root + (tip - root) * f
            block(p + V((0, -0.14 * U, 0)), (0.02 * U, 0.3 * U * (1.1 - 0.25 * k), 0.12 * U), bone, color)


def _staff(color, top_color, hand_side="Left", length=0.75, glow=None):
    hand = head(hand_side + "Hand") + (tail(hand_side + "Hand") - head(hand_side + "Hand")) * 0.4
    limb(hand - V((0, 0.8 * U, 0)), hand + V((0, length * U, 0)), 0.02 * U, 0.02 * U, hand_side + "Hand", color, 5)
    blob(hand + V((0, (length + 0.06) * U, 0)), (0.06 * U, 0.07 * U, 0.06 * U), hand_side + "Hand", top_color)
    if glow:
        blob(hand + V((0, (length + 0.06) * U, 0)), (0.035 * U, 0.04 * U, 0.035 * U), hand_side + "Hand", glow)


def _weapon(hand_side, length, color, guard=None, width=0.03):
    hand = head(hand_side + "Hand")
    d = (tail(hand_side + "Hand") - hand).normalized()
    limb(hand, hand + d * length * U, width * U, 0.008 * U, hand_side + "Hand", color, 4)
    if guard:
        block(hand + d * 0.04 * U, (0.14 * U, 0.03 * U, 0.03 * U), hand_side + "Hand", guard)


def zombie(skin="zombie", dark="zombie_dark", rag="rotten", eye="eye"):
    # The zombie: green-grey rotting flesh, a hanging jaw, torn rags, a hunch.
    _body(rag, dark, rag, dark, skin, wide=1.05)
    h = _head(skin, eye)
    block(h + V((0, 0.03 * U, 0.07 * U)), (0.08 * U, 0.05 * U, 0.05 * U), "Head", dark)
    for side in ("Left", "Right"):
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.045 * U, 0.04 * U, side + "Arm", skin)
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.04 * U, 0.035 * U, side + "ForeArm", skin)
    block(V((0.05 * U, head("Spine1").y, 0.1 * U)), (0.08 * U, 0.1 * U, 0.02 * U), "Spine1", dark)


def zombie_champion():
    zombie("frost", "frost_dark", "ash_rag", "eye")


def zombie_elite():
    zombie("ash_skin", "ash_rag", "ash_rag", "fire")


def skeleton_warrior():
    # A skeleton in light armour: a helm, a breastplate, a round shield and a sword.
    skeleton()
    h = head("Head")
    blob(h + V((0, 0.15 * U, 0)), (0.1 * U, 0.06 * U, 0.1 * U), "Head", "iron")
    block(V((0, head("Spine2").y, 0.03 * U)), (0.26 * U, 0.2 * U, 0.16 * U), "Spine2", "iron")
    hand = head("LeftHand") + (tail("LeftHand") - head("LeftHand")) * 0.4
    blob(hand + V((0, 0, 0.06 * U)), (0.15 * U, 0.15 * U, 0.03 * U), "LeftHand", "rust")
    blob(hand + V((0, 0, 0.08 * U)), (0.04 * U, 0.04 * U, 0.02 * U), "LeftHand", "iron")


def fallen():
    # A small red demon: a hunched imp-like body, a mask of a face with a grin, a spear.
    _body("demon", "demon_dark", "demon", "demon_dark", "demon", wide=0.9, bulk=0.9)
    h = _head("demon", "fire", r=(0.1, 0.11, 0.1))
    _horns(h, "horn", 0.8, 0.1)
    block(h + V((0, 0.04 * U, 0.09 * U)), (0.09 * U, 0.02 * U, 0.02 * U), "Head", "bone")
    block(V((0, head("Hips").y - 0.05 * U, 0.08 * U)), (0.16 * U, 0.15 * U, 0.02 * U), "Hips", "rotten")
    hand = head("RightHand") + (tail("RightHand") - head("RightHand")) * 0.4
    limb(hand - V((0, 0.5 * U, 0)), hand + V((0, 0.7 * U, 0)), 0.015 * U, 0.015 * U, "RightHand", "bow_wood", 4)
    limb(hand + V((0, 0.7 * U, 0)), hand + V((0, 0.88 * U, 0)), 0.03 * U, 0.003 * U, "RightHand", "steel", 4)


def wight():
    # A gaunt undead knight in tattered black, sickly green light in its eyes and hands, a curved blade.
    _body("robe_black", "robe_dark", "robe_black", "black_iron", "pale", wide=1.0)
    h = _head("pale", "wight_green")
    blob(h + V((0, 0.13 * U, -0.01 * U)), (0.11 * U, 0.09 * U, 0.11 * U), "Head", "robe_black")
    for side in ("Left", "Right"):
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.05 * U, 0.05 * U, 0.05 * U), side + "Hand", "wight_green")
    _weapon("Right", 0.6, "steel", "black_iron")


def mummy():
    # Wrapped in yellowed bandages, strips hanging loose, a dark slit for the eyes.
    _body("gauze", "gauze_dark", "gauze", "gauze_dark", "gauze", wide=1.0)
    h = _head("gauze", "eye_dark" if "eye_dark" in COLORS else "robe_dark")
    block(h + V((0, 0.11 * U, 0.09 * U)), (0.1 * U, 0.02 * U, 0.01 * U), "Head", "robe_dark")
    for k in range(4):
        y = head("Spine2").y - k * 0.08 * U
        block(V((0, y, 0.1 * U)), (0.3 * U, 0.015 * U, 0.01 * U), "Spine2" if k < 2 else "Spine1", "gauze_dark")
    for side in ("Left", "Right"):
        block(head(side + "ForeArm") + V((0, -0.08 * U, 0)), (0.02 * U, 0.15 * U, 0.04 * U), side + "ForeArm", "gauze")


def imp():
    # A small fiery winged demon: red skin, horns, bat wings, a long tail.
    _body("imp", "demon_dark", "imp", "demon_dark", "imp", wide=0.85, bulk=0.85)
    h = _head("imp", "fire", r=(0.1, 0.1, 0.1))
    _horns(h, "horn", 0.9, 0.12)
    _wings("wing", 0.45, up=0.25)
    limb(V((0, head("Hips").y, -0.1 * U)), V((0, head("Hips").y - 0.3 * U, -0.4 * U)), 0.02 * U, 0.005 * U, "Hips", "imp", 4)


def bile_fiend():
    # A bloated green fiend: a huge round belly, a small head with a gaping maw, short thick limbs, pustules.
    s2 = head("Spine2")
    blob(V((0, head("Spine").y, 0.05 * U)), (0.38 * U, 0.36 * U, 0.38 * U), "Spine", "fiend")
    blob(V((0, s2.y, 0)), (0.3 * U, 0.2 * U, 0.28 * U), "Spine2", "fiend_dark")
    for k in range(6):
        a = k * 1.1
        blob(V((math.cos(a) * 0.3 * U, head("Spine").y + math.sin(a * 1.7) * 0.15 * U, 0.2 * U + math.sin(a) * 0.1 * U)), (0.05 * U, 0.05 * U, 0.05 * U), "Spine", "pus")
    h = _head("fiend", "eye", r=(0.12, 0.1, 0.12), y=0.04)
    block(h + V((0, 0.0, 0.12 * U)), (0.14 * U, 0.06 * U, 0.04 * U), "Head", "robe_dark")
    for side in ("Left", "Right"):
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.09 * U, 0.08 * U, side + "Arm", "fiend")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.08 * U, 0.07 * U, side + "ForeArm", "fiend_dark")
        blob(head(side + "Hand"), (0.07 * U, 0.06 * U, 0.07 * U), side + "Hand", "fiend")
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.11 * U, 0.09 * U, side + "UpLeg", "fiend")
        limb(head(side + "Leg"), head(side + "Foot"), 0.09 * U, 0.08 * U, side + "Leg", "fiend_dark")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.12 * U, 0.06 * U, 0.2 * U), side + "Foot", "fiend_dark")


def retcher():
    # A bloated, hunched thing (2026-10-17): a swollen sagging gut, a thin neck, a small bald head with a jaw hanging wide
    # open, strings of bile from the mouth down the chest, spindly arms, sores.
    s, s1, s2 = head("Spine"), head("Spine1"), head("Spine2")
    blob(V((0, s.y - 0.02 * U, 0.12 * U)), (0.3 * U, 0.3 * U, 0.32 * U), "Spine", "sick")
    blob(V((0, s1.y, 0.06 * U)), (0.26 * U, 0.2 * U, 0.26 * U), "Spine1", "sick")
    blob(V((0, s2.y, -0.04 * U)), (0.22 * U, 0.16 * U, 0.2 * U), "Spine2", "sick_dark")
    # A hump over the shoulders.
    blob(V((0, s2.y + 0.1 * U, -0.12 * U)), (0.16 * U, 0.12 * U, 0.12 * U), "Spine2", "sick_dark")
    for k in range(7):
        a = k * 0.9
        blob(V((math.cos(a) * 0.26 * U, s.y + math.sin(a * 1.3) * 0.16 * U, 0.26 * U + math.sin(a) * 0.08 * U)),
             (0.04 * U, 0.04 * U, 0.03 * U), "Spine", "pus")
    limb(head("Neck"), head("Head"), 0.05 * U, 0.05 * U, "Neck", "sick_dark")
    h = _head("sick", "eye", r=(0.09, 0.095, 0.1), y=0.05)
    # The jaw hanging open, the dark mouth, and bile pouring out of it down the front.
    block(h + V((0, -0.08 * U, 0.08 * U)), (0.13 * U, 0.04 * U, 0.1 * U), "Head", "sick_dark")
    block(h + V((0, -0.03 * U, 0.1 * U)), (0.1 * U, 0.05 * U, 0.04 * U), "Head", "maw")
    for k, (x, length) in enumerate(((-0.03, 0.22), (0.01, 0.3), (0.04, 0.18))):
        top = h + V((x * U, -0.06 * U, 0.12 * U))
        limb(top, top + V((0, -length * U, 0.05 * U)), 0.04 * U, 0.025 * U, "Head", "bile", 6)
    blob(V((0, s1.y + 0.02 * U, 0.3 * U)), (0.16 * U, 0.18 * U, 0.06 * U), "Spine1", "bile")
    blob(V((0.06 * U, s.y + 0.04 * U, 0.38 * U)), (0.1 * U, 0.1 * U, 0.05 * U), "Spine", "bile")
    for side in ("Left", "Right"):
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.05 * U, 0.04 * U, side + "Arm", "sick")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.04 * U, 0.035 * U, side + "ForeArm", "sick_dark")
        blob(head(side + "Hand"), (0.05 * U, 0.04 * U, 0.05 * U), side + "Hand", "sick")
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.09 * U, 0.07 * U, side + "UpLeg", "sick")
        limb(head(side + "Leg"), head(side + "Foot"), 0.07 * U, 0.06 * U, side + "Leg", "sick_dark")
        block((head(side + "Foot") + head(side + "ToeBase")) / 2, (0.1 * U, 0.05 * U, 0.17 * U), side + "Foot", "sick_dark")


def flesh_golem():
    # A hulk stitched from corpses: patchwork skin of different shades, heavy stitches, a small head, huge fists.
    _body("golem", "zombie_dark", "rotten", "stitch", "golem", wide=1.5, bulk=1.7)
    h = _head("golem", "eye", r=(0.08, 0.09, 0.09), y=0.05)
    s2 = head("Spine2")
    blob(V((0, s2.y + 0.05 * U, -0.05 * U)), (0.3 * U, 0.2 * U, 0.25 * U), "Spine2", "golem")
    for k in range(5):
        block(V(((k - 2) * 0.06 * U, s2.y + (k % 2) * 0.05 * U, 0.2 * U)), (0.012 * U, 0.2 * U, 0.01 * U), "Spine2", "stitch")
    blob(V((0.12 * U, head("Spine1").y, 0.12 * U)), (0.1 * U, 0.08 * U, 0.06 * U), "Spine1", "zombie")
    for side in ("Left", "Right"):
        blob(head(side + "Hand"), (0.1 * U, 0.09 * U, 0.1 * U), side + "Hand", "golem")


def ogre():
    # A brown-skinned ogre: a big gut, a loincloth, tusks, a spiked club.
    _body("ogre", "ogre_dark", "leather", "ogre_dark", "ogre", wide=1.5, bulk=1.6)
    h = _head("ogre", "eye", r=(0.11, 0.11, 0.11), y=0.06)
    for sx in (-1, 1):
        limb(h + V((sx * 0.04 * U, 0.0, 0.1 * U)), h + V((sx * 0.06 * U, 0.08 * U, 0.14 * U)), 0.015 * U, 0.004 * U, "Head", "bone", 4)
    blob(V((0, head("Spine").y, 0.12 * U)), (0.25 * U, 0.2 * U, 0.15 * U), "Spine", "ogre")
    hand = head("RightHand")
    d = (tail("RightHand") - hand).normalized()
    limb(hand, hand + d * 0.7 * U, 0.04 * U, 0.09 * U, "RightHand", "bow_wood", 6)
    for k in range(4):
        block(hand + d * (0.5 + 0.05 * k) * U + V((0, 0.06 * U, 0)), (0.02 * U, 0.06 * U, 0.02 * U), "RightHand", "iron")


def gargoyle():
    # A stone gargoyle: grey stone skin, a beaked face, horns, folded stone wings, clawed hands.
    _body("stone_grey", "stone_dark", "stone_grey", "stone_dark", "stone_grey", wide=1.2, bulk=1.3)
    h = _head("stone_grey", "fire", r=(0.1, 0.1, 0.11))
    block(h + V((0, 0.07 * U, 0.12 * U)), (0.05 * U, 0.05 * U, 0.08 * U), "Head", "stone_dark")
    _horns(h, "stone_dark", 1.0, 0.12)
    _wings("stone_dark", 0.55, up=0.4)


def stygian_demon():
    # A red horned demon with broad shoulders and a heavy jaw.
    _body("demon", "demon_dark", "demon_dark", "horn", "demon", wide=1.4, bulk=1.5)
    h = _head("demon", "lava", r=(0.11, 0.11, 0.11), y=0.06)
    _horns(h, "horn", 1.3, 0.2)
    for side in ("Left", "Right"):
        blob(head(side + "Arm"), (0.13 * U, 0.12 * U, 0.12 * U), side + "Arm", "demon")


def giant():
    # A towering giant: tan skin, a fur loincloth, a rope belt, a rock in one fist.
    _body("giant", "giant_dark", "fur", "giant_dark", "giant", wide=1.6, bulk=1.8)
    _head("giant", "eye", r=(0.11, 0.12, 0.11), y=0.07)
    blob(V((0, head("Spine").y, 0.1 * U)), (0.26 * U, 0.2 * U, 0.15 * U), "Spine", "giant")
    blob(head("RightHand") + V((0, 0, 0.05 * U)), (0.14 * U, 0.13 * U, 0.14 * U), "RightHand", "stone_grey")


def balrog_lite():
    # A fire demon: black-red skin cracked with lava, great horns, wings, a whip of fire.
    _body("demon_dark", "ash_black", "demon_dark", "ash_black", "demon_dark", wide=1.5, bulk=1.6)
    h = _head("demon_dark", "lava", r=(0.12, 0.12, 0.12), y=0.06)
    _horns(h, "horn", 1.6, 0.18)
    _wings("ash_black", 0.75, up=0.45)
    s2 = head("Spine2")
    for k in range(5):
        block(V(((k - 2) * 0.07 * U, s2.y - 0.05 * U + (k % 2) * 0.08 * U, 0.17 * U)), (0.03 * U, 0.12 * U, 0.01 * U), "Spine2", "lava")
    hand = head("RightHand")
    d = (tail("RightHand") - hand).normalized()
    p = hand
    for k in range(6):
        q = p + d * 0.12 * U + V((0, -0.06 * U, 0.02 * U * math.sin(k)))
        limb(p, q, 0.015 * U, 0.012 * U, "RightHand", "lava", 4)
        p = q


def dark_cultist():
    # A dark hooded cultist in brown robes with a fire in one hand and a twisted staff.
    cultist()
    hand = head("RightHand") + (tail("RightHand") - head("RightHand")) * 0.4
    blob(hand + V((0, 0.06 * U, 0)), (0.05 * U, 0.07 * U, 0.05 * U), "RightHand", "fire")


def bone_mage():
    # A skeleton in a hooded beige robe, a skull-topped staff.
    h = head("Head")
    blob(h + V((0, 0.1 * U, 0.01 * U)), (0.09 * U, 0.1 * U, 0.09 * U), "Head", "bone")
    blob(h + V((0, 0.12 * U, -0.01 * U)), (0.115 * U, 0.12 * U, 0.11 * U), "Head", "gauze_dark")
    for sx in (-1, 1):
        block(h + V((sx * 0.03 * U, 0.1 * U, 0.09 * U)), (0.024 * U, 0.02 * U, 0.016 * U), "Head", "robe_dark")
    s2 = head("Spine2")
    block(V((0, s2.y, 0)), (0.3 * U, 0.2 * U, 0.19 * U), "Spine2", "gauze")
    block(V((0, head("Spine1").y, 0)), (0.27 * U, 0.12 * U, 0.18 * U), "Spine1", "gauze")
    block(V((0, head("Spine").y - 0.02 * U, 0)), (0.27 * U, 0.06 * U, 0.19 * U), "Spine", "rust")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.1 * U, 0.12 * U, side + "UpLeg", "gauze")
        limb(head(side + "Leg"), head(side + "Foot") + V((0, 0.03 * U, 0)), 0.12 * U, 0.14 * U, side + "Leg", "gauze_dark")
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.06 * U, 0.06 * U, side + "Arm", "gauze")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.06 * U, 0.075 * U, side + "ForeArm", "gauze_dark")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.4, (0.035 * U, 0.035 * U, 0.035 * U), side + "Hand", "bone")
    _staff("bone_dark", "bone", "Left", glow="eye")


def wraith():
    # A blue spectral wraith: a hooded robe trailing to nothing below the knees, glowing hands and eyes.
    h = head("Head")
    blob(h + V((0, 0.11 * U, -0.01 * U)), (0.12 * U, 0.14 * U, 0.12 * U), "Head", "wraith_blue")
    blob(h + V((0, 0.09 * U, 0.04 * U)), (0.07 * U, 0.08 * U, 0.06 * U), "Head", "ash_black")
    for sx in (-1, 1):
        block(h + V((sx * 0.025 * U, 0.1 * U, 0.1 * U)), (0.02 * U, 0.015 * U, 0.015 * U), "Head", "wraith_glow")
    s2 = head("Spine2")
    block(V((0, s2.y, 0)), (0.3 * U, 0.2 * U, 0.2 * U), "Spine2", "wraith_blue")
    block(V((0, head("Spine1").y, 0)), (0.28 * U, 0.12 * U, 0.19 * U), "Spine1", "wraith_blue")
    for side in ("Left", "Right"):
        limb(head(side + "UpLeg"), head(side + "Leg"), 0.12 * U, 0.08 * U, side + "UpLeg", "wraith_blue")
        limb(head(side + "Arm"), head(side + "ForeArm"), 0.06 * U, 0.07 * U, side + "Arm", "wraith_blue")
        limb(head(side + "ForeArm"), head(side + "Hand"), 0.07 * U, 0.09 * U, side + "ForeArm", "wraith_blue")
        blob(head(side + "Hand") + (tail(side + "Hand") - head(side + "Hand")) * 0.5, (0.045 * U, 0.045 * U, 0.045 * U), side + "Hand", "wraith_glow")


def lich():
    # A lich: a crowned skull, purple robes with a high collar, a staff with a green orb.
    h = head("Head")
    blob(h + V((0, 0.1 * U, 0.01 * U)), (0.09 * U, 0.105 * U, 0.095 * U), "Head", "bone")
    for sx in (-1, 1):
        block(h + V((sx * 0.03 * U, 0.11 * U, 0.085 * U)), (0.024 * U, 0.016 * U, 0.016 * U), "Head", "wight_green")
    for k in range(5):
        a = math.pi * (0.15 + 0.7 * k / 4)
        limb(h + V((math.cos(a) * 0.08 * U, 0.18 * U, math.sin(a) * 0.03 * U)), h + V((math.cos(a) * 0.08 * U, 0.26 * U, math.sin(a) * 0.03 * U)), 0.012 * U, 0.004 * U, "Head", "gold", 4)
    _townsfolk_body("lich_violet", "lich_dark", "lich_violet", "lich_dark", skin="bone", wide=1.2)
    block(V((0, head("Hips").y - 0.3 * U, 0)), (0.4 * U, 0.6 * U, 0.32 * U), "Hips", "lich_violet")
    blob(V((0, head("Spine2").y + 0.12 * U, -0.05 * U)), (0.2 * U, 0.1 * U, 0.12 * U), "Spine2", "lich_dark")
    _staff("lich_dark", "lich_dark", "Left", glow="wight_green")


def succubus():
    # A winged temptress: pale lilac skin, dark hair, bat wings, horns, claws.
    _body("succubus", "wing", "succubus", "wing", "succubus", wide=0.9, bulk=0.85)
    h = _head("succubus", "fire")
    blob(h + V((0, 0.14 * U, -0.03 * U)), (0.1 * U, 0.08 * U, 0.1 * U), "Head", "ash_black")
    limb(h + V((0, 0.08 * U, -0.1 * U)), h + V((0, -0.15 * U, -0.13 * U)), 0.03 * U, 0.015 * U, "Head", "ash_black", 6)
    _horns(h, "horn", 0.9, 0.12)
    _wings("wing", 0.6, up=0.35)


def harpy():
    # A harpy: a feathered body, a beaked head, wide feathered wings for arms, taloned feet.
    _body("harpy", "feather_dark", "harpy", "horn", "harpy", wide=0.95, bulk=0.9)
    h = _head("harpy", "fire")
    block(h + V((0, 0.07 * U, 0.12 * U)), (0.035 * U, 0.04 * U, 0.08 * U), "Head", "gold")
    for side in ("Left", "Right"):
        a0, a1 = head(side + "Arm"), head(side + "Hand")
        for k in range(5):
            p = a0 + (a1 - a0) * (0.2 + 0.18 * k)
            block(p + V((0, -0.12 * U, -0.02 * U)), (0.06 * U, 0.26 * U, 0.02 * U), side + ("Arm" if k < 2 else "ForeArm"), "feather_dark" if k % 2 else "harpy")



# ---- The townsfolk after the owner's NPC sheet (2026-10-13), taking its names: each keeps its file name.

def _face(h, hair, beard=None, hair_back=0.0, ears=False):
    blob(h + V((0, 0.1 * U, 0.01 * U)), (0.088 * U, 0.105 * U, 0.095 * U), "Head", "skin")
    for sx in (-1, 1):
        block(h + V((sx * 0.03 * U, 0.11 * U, 0.088 * U)), (0.02 * U, 0.012 * U, 0.01 * U), "Head", "robe_dark")
    if hair:
        blob(h + V((0, 0.15 * U, -0.02 * U)), (0.095 * U, 0.075 * U, 0.1 * U), "Head", hair)
        if hair_back:
            block(h + V((0, 0.05 * U, -0.08 * U)), (0.15 * U, hair_back * U, 0.04 * U), "Head", hair)
    if beard:
        block(h + V((0, 0.03 * U, 0.075 * U)), (0.11 * U, 0.08 * U, 0.05 * U), "Head", beard)
    if ears:
        for sx in (-1, 1):
            limb(h + V((sx * 0.08 * U, 0.11 * U, 0)), h + V((sx * 0.15 * U, 0.17 * U, -0.02 * U)), 0.02 * U, 0.003 * U, "Head", "skin", 4)


def _robe(color, dark, wide=1.15, trim=None):
    block(V((0, head("Hips").y - 0.3 * U, 0)), (0.38 * U * wide, 0.62 * U, 0.3 * U * wide), "Hips", color)
    if trim:
        block(V((0, head("Hips").y - 0.6 * U, 0)), (0.4 * U * wide, 0.04 * U, 0.32 * U * wide), "Hips", trim)
        block(V((0, head("Spine2").y, 0.1 * U)), (0.05 * U, 0.2 * U, 0.02 * U), "Spine2", trim)


def merchant():
    # Garrett: a charismatic trader in rich red and gold robes, a coin pouch at the belt, a short beard.
    h = head("Head")
    _townsfolk_body("royal_red", "royal_red", "royal_red", "leather_dark", wide=1.15)
    _face(h, "hair", "hair")
    _robe("royal_red", "royal_red", 1.15, "gold_trim")
    blob(V((0, head("Spine2").y + 0.1 * U, 0)), (0.2 * U, 0.06 * U, 0.14 * U), "Spine2", "gold_trim")
    blob(V((0.15 * U, head("Hips").y, 0.08 * U)), (0.05 * U, 0.06 * U, 0.05 * U), "Hips", "gold")


def pet_vendor():
    # Lyriel: a graceful elven scout in green with leaf motifs, pointed ears, a bow.
    h = head("Head")
    _townsfolk_body("elf_green", "elf_dark", "elf_dark", "leather_dark", wide=0.95)
    _face(h, "gold_trim", hair_back=0.25, ears=True)
    block(V((0, head("Hips").y - 0.18 * U, 0.05 * U)), (0.25 * U, 0.3 * U, 0.04 * U), "Hips", "elf_green")
    hand = head("LeftHand") + (tail("LeftHand") - head("LeftHand")) * 0.4
    for sy in (-1, 1):
        limb(hand, hand + V((0, sy * 0.5 * U, -0.08 * U)), 0.015 * U, 0.008 * U, "LeftHand", "bow_wood", 5)


def stash_keeper():
    # Elara: a young scholar in simple blue robes with a book satchel.
    h = head("Head")
    _townsfolk_body("scholar_blue", "scholar_dark", "scholar_blue", "leather_dark", wide=1.0)
    _face(h, "hair", hair_back=0.12)
    _robe("scholar_blue", "scholar_dark", 1.05, "cream")
    block(V((-0.17 * U, head("Hips").y + 0.02 * U, 0.04 * U)), (0.06 * U, 0.16 * U, 0.14 * U), "Hips", "apron_brown")
    hand = head("RightHand") + (tail("RightHand") - head("RightHand")) * 0.4
    block(hand + V((0, 0, 0.04 * U)), (0.12 * U, 0.16 * U, 0.04 * U), "RightHand", "royal_red")


def healer():
    # Miranda: the innkeeper, a friendly face, a cream blouse, a brown corset and apron over a long skirt.
    h = head("Head")
    _townsfolk_body("cream", "apron_brown", "apron_brown", "leather_dark", wide=1.05)
    _face(h, "hair", hair_back=0.1)
    _robe("apron_brown", "apron_brown", 1.1)
    block(V((0, head("Hips").y - 0.25 * U, 0.12 * U)), (0.24 * U, 0.5 * U, 0.02 * U), "Hips", "cream")


def gambler():
    # Sylthara: an informant in a teal hooded cloak with silver accents.
    h = head("Head")
    _townsfolk_body("teal", "teal_dark", "teal_dark", "leather_dark", wide=1.0)
    _face(h, None)
    blob(h + V((0, 0.11 * U, -0.015 * U)), (0.115 * U, 0.13 * U, 0.115 * U), "Head", "teal")
    blob(h + V((0, 0.09 * U, 0.04 * U)), (0.07 * U, 0.08 * U, 0.06 * U), "Head", "skin")
    _robe("teal", "teal_dark", 1.15, "silver")
    block(V((0, head("Spine2").y - 0.05 * U, -0.13 * U)), (0.34 * U, 0.6 * U, 0.03 * U), "Spine2", "teal_dark")


def trainer():
    # Brunhilde: a guard captain, red braids, a heavy fur-trimmed cloak over armour, sturdy boots.
    h = head("Head")
    _townsfolk_body("mail", "steel", "leather_dark", "leather_dark", wide=1.2)
    _face(h, "ginger", hair_back=0.3)
    blob(V((0, head("Spine2").y + 0.1 * U, -0.02 * U)), (0.24 * U, 0.07 * U, 0.16 * U), "Spine2", "fur")
    block(V((0, head("Spine2").y - 0.12 * U, -0.13 * U)), (0.36 * U, 0.55 * U, 0.03 * U), "Spine2", "royal_red")
    for side in ("Left", "Right"):
        blob(head(side + "Arm"), (0.07 * U, 0.05 * U, 0.07 * U), side + "Arm", "steel")


def aldis():
    # Aldric (the main quest's giver, Mother Aldis until 2026-10-13): a wise elder priest in white and gold robes, grey
    # hair and beard, a simple staff.
    h = head("Head")
    _townsfolk_body("priest_white", "gold_trim", "priest_white", "leather_dark", wide=1.15)
    _face(h, "grey_hair", "grey_hair")
    _robe("priest_white", "priest_white", 1.2, "gold_trim")
    _staff("gold_trim", "gold_trim", "Left", 0.75)


def pellam():
    # Thorin (Pellam until 2026-10-13): a stout blacksmith, a thick beard, practical leather gear, a hammer.
    h = head("Head")
    _townsfolk_body("leather", "apron_brown", "leather_dark", "leather_dark", wide=1.3)
    _face(h, "hair", "hair")
    block(V((0, head("Spine1").y - 0.05 * U, 0.11 * U)), (0.26 * U, 0.4 * U, 0.03 * U), "Spine1", "apron_brown")
    hand = head("RightHand")
    d = (tail("RightHand") - hand).normalized()
    limb(hand, hand + d * 0.3 * U, 0.02 * U, 0.02 * U, "RightHand", "bow_wood", 4)
    block(hand + d * 0.32 * U, (0.1 * U, 0.06 * U, 0.06 * U), "RightHand", "iron")


def bram():
    # Drok (Bram until 2026-10-13): a grizzled miner, dusty leather and plates, goggles, a pickaxe.
    h = head("Head")
    _townsfolk_body("leather_dark", "rust", "leather_dark", "leather_dark", wide=1.2)
    _face(h, None, "beard")
    blob(h + V((0, 0.17 * U, 0)), (0.1 * U, 0.06 * U, 0.1 * U), "Head", "leather_dark")
    block(h + V((0, 0.13 * U, 0.09 * U)), (0.1 * U, 0.03 * U, 0.02 * U), "Head", "goggles")
    hand = head("RightHand")
    d = (tail("RightHand") - hand).normalized()
    limb(hand, hand + d * 0.5 * U, 0.018 * U, 0.018 * U, "RightHand", "bow_wood", 4)
    limb(hand + d * 0.5 * U + V((0, 0.12 * U, 0)), hand + d * 0.5 * U - V((0, 0.12 * U, 0)), 0.02 * U, 0.005 * U, "RightHand", "iron", 4)


def ivy():
    # Faelan (Sister Ivy until 2026-10-13): a slender elven mage in flowing purple robes with silver embroidery.
    h = head("Head")
    _townsfolk_body("purple", "purple_dark", "purple", "purple_dark", wide=0.95)
    _face(h, "ash_black", hair_back=0.3, ears=True)
    _robe("purple", "purple_dark", 1.1, "silver")
    blob(V((0, head("Spine2").y + 0.12 * U, -0.03 * U)), (0.18 * U, 0.08 * U, 0.12 * U), "Spine2", "purple_dark")


{"skeleton": skeleton, "cultist": cultist, "cutthroat": cutthroat, "ember_acolyte": ember_acolyte,
 "pyre_keeper": pyre_keeper, "carrion_bloat": carrion_bloat, "stash_keeper": stash_keeper, "healer": healer,
 "gambler": gambler, "trainer": trainer, "drowned": drowned, "harpooner": harpooner, "drowned_watchman": drowned_watchman,
 "skeleton_knight": skeleton_knight, "grave_priest": grave_priest, "hollowed": hollowed, "void_wraith": void_wraith,
 "rift_caller": rift_caller, "tidewife": tidewife, "saint_marrow": saint_marrow, "first_watchman": first_watchman,
 "ash_archivist": ash_archivist, "rift_mother": rift_mother, "gaoler": gaoler, "hanged_magistrate": hanged_magistrate,
 "husk": husk, "husk_champion": husk_champion, "husk_elite": husk_elite, "ghoul": ghoul, "bandit_archer": bandit_archer,
 "merchant": merchant, "pet_vendor": pet_vendor,
 "zombie": zombie, "zombie_champion": zombie_champion, "zombie_elite": zombie_elite, "skeleton_warrior": skeleton_warrior,
 "fallen": fallen, "wight": wight, "mummy": mummy, "imp": imp, "bile_fiend": bile_fiend, "flesh_golem": flesh_golem,
 "ogre": ogre, "gargoyle": gargoyle, "stygian_demon": stygian_demon, "giant": giant, "balrog_lite": balrog_lite,
 "retcher": retcher, "dark_cultist": dark_cultist, "bone_mage": bone_mage, "wraith": wraith, "lich": lich, "succubus": succubus, "harpy": harpy,
 "aldis": aldis, "tobin": tobin, "pellam": pellam, "bram": bram, "ivy": ivy}[NAME]()

# Every bone a humanoid needs keeps a vertex weighted to it: Unity strips bones nothing is weighted to, and the avatar
# then cannot be made ("Required human bone 'LeftHand' not found", the skeleton knight's shield hand, 2026-10-05).
for bone in ("Hips", "Spine", "Spine1", "Spine2", "Neck", "Head", "LeftArm", "LeftForeArm", "LeftHand", "RightArm",
             "RightForeArm", "RightHand", "LeftUpLeg", "LeftLeg", "LeftFoot", "RightUpLeg", "RightLeg", "RightFoot"):
    if ("mixamorig:" + bone) not in groups and ("mixamorig:" + bone) in bones:
        block(head(bone), (0.004 * U, 0.004 * U, 0.004 * U), bone, color_list[0])

mesh = bpy.data.meshes.new(NAME)
bm.normal_update()
bm.to_mesh(mesh)
bm.free()
obj = bpy.data.objects.new(NAME, mesh)
bpy.context.scene.collection.objects.link(obj)
for name, index in sorted(groups.items(), key=lambda p: p[1]):
    obj.vertex_groups.new(name=name)
obj.parent = arm
obj.matrix_parent_inverse = mathutils.Matrix.Identity(4)
mod = obj.modifiers.new("Armature", "ARMATURE")
mod.object = arm

# The atlas: one 8 px square a colour, every face mapped to its colour's middle.
cells = int(math.ceil(math.sqrt(len(color_list))))
size = cells * 8
pixels = np.zeros((size, size, 4), dtype=np.float32)
pixels[:, :, 3] = 1
for i, key in enumerate(color_list):
    cx, cy = i % cells, i // cells
    pixels[cy * 8:(cy + 1) * 8, cx * 8:(cx + 1) * 8, :3] = np.array(COLORS[key]) / 255.0
os.makedirs(OUT, exist_ok=True)
albedo = os.path.join(OUT, NAME + "_albedo.png")
image = bpy.data.images.new("atlas", size, size, alpha=True)
image.colorspace_settings.name = "Non-Color"
image.pixels = pixels.ravel()
image.filepath_raw = albedo
image.file_format = "PNG"
image.save()
uv = mesh.uv_layers.new(name="UV").data
for poly, c in zip(mesh.polygons, face_color):
    u, v = (c % cells + 0.5) / cells, (c // cells + 0.5) / cells
    for li in poly.loop_indices:
        uv[li].uv = (u, v)
material = bpy.data.materials.new(NAME)
material.use_nodes = True
tex = material.node_tree.nodes.new("ShaderNodeTexImage")
tex.image = bpy.data.images.load(albedo)
material.node_tree.links.new(tex.outputs["Color"], material.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
mesh.materials.append(material)

bpy.ops.object.select_all(action="DESELECT")
for o in bpy.context.scene.objects:
    o.select_set(True)
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, NAME + ".fbx"), use_selection=True, add_leaf_bones=False,
                         bake_anim=True, bake_anim_use_all_actions=False, path_mode="STRIP", embed_textures=False)
# The idle is the ghoul's own (EnemyBakeSetup takes it from ghoul.fbx): the body's exported animation does not come
# through as a clip the bake finds, and a copy of ghoul.fbx as a clip file drew T-pose frames.
for clip in (() if NAME in NPCS else ("run", "attack", "hit", "death")):
    source = os.path.join(RIGS, DONOR, "%s_%s.fbx" % (DONOR, clip))
    shutil.copyfile(source, os.path.join(OUT, "%s_%s.fbx" % (NAME, clip)))
print("REPORT wrote", OUT, len(mesh.polygons), "faces,", len(groups), "bones")
