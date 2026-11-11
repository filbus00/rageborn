# The Wild Arrow from the owner's sheet (2026-11-11, the owner: "check downloads, new sheets and animations for the
# character"): wild_arrow_archer_16x16.png, a blonde archer in green with a red cape, 16 x 16 cells, a row for each of
# the 8 directions in the game's order (s, sw, w, nw, n, ne, e, se), 14 columns: idle 0-1, walk 2-5, shot 6-9, dive
# (crouch, two rolls, up) 10-13; wild_arrow_archer_preview.gif shows them playing. Written as the game's sheets for the
# drawn character "wild_arrow_drawn" (each pixel 2 game pixels, as the enemies), in place of wild_arrow_16.py's, which
# would overwrite them. The sheet has no hit or death: the hit is the crouch, the death the crouch and then her standing
# frame laid on its side. Every armour look gets the same pictures. (wild_arrow_arrow_16x16.png, eight 10-pixel sticks,
# is not used: her drawn arrows stay.)
# Run: Blender -b -P ArtSource/pixel/sixteen/archer_sheets/build_archer_sheets.py, then Tools > ARPG > Import Drawn Characters.
import bpy, os, json
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
OUT = os.path.join(P, "ArtSource", "pixel", "characters", "wild_arrow_drawn")
C = 16
SCALE = 2
LOOKS = ["bare", "ranger", "brigand", "scale", "plate", "knight", "stag_hide", "cinder_jerkin"]


def load(path):
    im = bpy.data.images.load(path)
    w, h = im.size
    a = np.array(im.pixels[:], np.float32).reshape(h, w, 4)[::-1].copy()   # top row first
    bpy.data.images.remove(im)
    return a


def save(img, path):
    h, w = img.shape[:2]
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    im.pixels = img[::-1].ravel()
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)


sheet = load(os.path.join(HERE, "wild_arrow_archer_16x16.png"))


def cell(row, col):
    return sheet[row * C:(row + 1) * C, col * C:(col + 1) * C].copy()


def lying(frame, row):
    """Her standing frame turned on its side, lying on the cell's bottom, her head toward where she faced."""
    k = 1 if row in (1, 2, 3) else -1
    turned = np.rot90(frame, k)
    ys, xs = np.where(turned[..., 3] > 0.5)
    part = turned[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    out = np.zeros_like(frame)
    h, w = part.shape[:2]
    left = (C - w) // 2
    out[C - h:, left:left + w] = part
    return out


# Each clip's frames by column; "lie" is the death's last frame.
WALK = [2, 3, 4, 5]
SHOT = [6, 7, 8, 9]
CLIPS = {
    "idle": [0, 1], "walk": WALK, "run": WALK, "run_back": WALK, "attack": SHOT, "overdraw": SHOT, "recoil": SHOT,
    "hit": [10, 0], "death": [10, "lie"], "dive": [10, 11, 12, 13],
    "walk_aim_forward": WALK, "walk_aim_back": WALK, "walk_aim_right": WALK, "walk_aim_left": WALK,
}
for name in ("attack", "overdraw", "recoil"):
    for suffix in ("", "_back", "_left", "_right"):
        CLIPS[name + "_move" + suffix] = SHOT

# Seconds and ground speeds. The loops at the sheet's own pace (idle 350 ms a frame, walk 130); a walk's speed is where
# that pace fits her feet, scaled from the old drawing's 0.8 s walk. The one-shots keep her old times (the game
# stretches them to the action).
PACE = 0.8 / 0.52
TIMING = {"idle": (0.7, 0), "run": (0.52, 3.4), "walk": (0.52, 1.1 * PACE), "run_back": (0.52, 2.7),
          "attack": (1.033, 0), "overdraw": (3.767, 0), "recoil": (0.7, 0), "hit": (0.5, 0), "death": (3.1, 0),
          "dive": (1.633, 0), "walk_aim_forward": (0.52, 1.1 * PACE), "walk_aim_back": (0.52, 0.9 * PACE),
          "walk_aim_right": (0.52, 1.27 * PACE), "walk_aim_left": (0.52, 1.41 * PACE)}
for name in ("attack", "overdraw", "recoil"):
    for suffix in ("", "_back", "_left", "_right"):
        TIMING[name + "_move" + suffix] = (0.7, 0)

# Clips the game shows from another's sheet (AppearanceRules.SheetStandIn): their timing only.
SHARED = {"run_back", "walk_aim_back", "walk_aim_left", "walk_aim_right", "attack_move_back", "attack_move_left",
          "attack_move_right", "recoil_move", "recoil_move_back", "recoil_move_left", "recoil_move_right",
          "overdraw_move_back", "overdraw_move_left", "overdraw_move_right"}


def strip(clip):
    rows = []
    for row in range(8):
        frames = [lying(cell(row, 0), row) if col == "lie" else cell(row, col) for col in CLIPS[clip]]
        rows.append(np.concatenate(frames, 1))
    return np.repeat(np.repeat(np.concatenate(rows, 0), SCALE, 0), SCALE, 1)


os.makedirs(OUT, exist_ok=True)
for old in os.listdir(OUT):
    if old.endswith(".png"):
        os.remove(os.path.join(OUT, old))
animations = []
strips = {}
for look in LOOKS:
    for clip, (seconds, speed) in TIMING.items():
        name = f"body_{look}_1h_{clip}"
        entry = {"name": name, "frames": len(CLIPS[clip]), "seconds": seconds, "speed": speed, "key": "1h_" + clip}
        if clip in SHARED:
            entry["shared"] = True
        else:
            if clip not in strips:
                strips[clip] = strip(clip)
            save(strips[clip], os.path.join(OUT, f"wild_arrow_drawn_{name}.png"))
        animations.append(entry)
with open(os.path.join(OUT, "sheet.json"), "w") as f:
    json.dump({"cellWidth": C * SCALE, "cellHeight": C * SCALE, "pivotX": C * SCALE // 2, "pivotY": SCALE // 2,
               "directions": 8, "animations": animations}, f)
print("wrote", len(animations), "animations,", len(strips), "pictures a look")
