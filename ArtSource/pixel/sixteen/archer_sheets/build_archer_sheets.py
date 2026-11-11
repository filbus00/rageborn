# The Wild Arrow from the owner's sheet (2026-11-11, the owner: "check downloads, new sheets and animations for the
# character"): a blonde archer in green with a red cape, first in 16 x 16 cells, then (the same day) wild_arrow_archer_24x24.png
# in 24 x 24 cells, a row for each of
# the 8 directions in the game's order (s, sw, w, nw, n, ne, e, se), 14 columns: idle 0-1, walk 2-5, shot 6-9, dive
# (crouch, two rolls, up) 10-13; wild_arrow_archer_preview.gif shows them playing. Written as the game's sheets for the
# drawn character "wild_arrow_drawn" (each pixel 2 game pixels, as the enemies), in place of wild_arrow_16.py's, which
# would overwrite them. The sheet has no hit or death: the hit is the crouch, the death the crouch and then her standing
# frame laid on its side. Every armour look gets the same pictures. (wild_arrow_arrow_16x16.png, eight 10-pixel sticks,
# and its 24 x 24 version are not used: her drawn arrows stay.)
# 21 pixels tall (2026-11-11, the owner: "rework the character to be 21px tall"): the sheet draws her 23 tall, so two
# rows are taken out of every frame (one out of the rolls), the rows that repeat a neighbour most: one in her tunic
# and one in her legs (never her head: a row taken from her hair flattened it), the same rows through a direction's
# standing frames, a row either way where a frame loses less. The frames as shortened are written to wild_arrow_archer_21.png beside the sheet.
# Run: Blender -b -P ArtSource/pixel/sixteen/archer_sheets/build_archer_sheets.py, then Tools > ARPG > Import Drawn Characters.
import bpy, os, json
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
P = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
OUT = os.path.join(P, "ArtSource", "pixel", "characters", "wild_arrow_drawn")
C = 24
SOURCE = "wild_arrow_archer_24x24.png"
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


sheet = load(os.path.join(HERE, SOURCE))


def raw(row, col):
    return sheet[row * C:(row + 1) * C, col * C:(col + 1) * C].copy()


def cell(row, col):
    return short[row][col].copy()


TALL = 21


def row_costs(frame):
    """For each row, the pixels it does not share with the nearer-matching of its neighbours: what taking it out loses."""
    drawn = np.concatenate([frame[..., :3] * frame[..., 3:], frame[..., 3:]], -1)
    diff = (np.abs(drawn[1:] - drawn[:-1]).sum(-1) > 0.05).sum(1).astype(float)     # row i against i + 1
    cost = np.full(C, 1e6)
    cost[1:-1] = np.minimum(diff[:-1], diff[1:])
    return cost


def without(frame, rows):
    keep = [y for y in range(C) if y not in rows]
    out = np.zeros_like(frame)
    out[len(rows):] = frame[keep]
    return out


def shorten(frames):
    """A direction's 14 frames, her standing ones (0 to 9) 21 tall."""
    costs = [row_costs(f) for f in frames]
    tops = [int(np.where(f[..., 3] > 0.5)[0].min()) for f in frames]
    top = min(tops[:10])
    total = sum(costs[:10])
    # Never her head (2026-11-11, the owner: "her head is really flat": the first try took a row of the hair over her
    # face and the dome went): one row of her tunic and one of her legs, at least three apart.
    body = top + 10 + int(np.argmin(total[top + 10:top + 16]))
    legs = top + 16 + int(np.argmin(total[top + 16:C - 2]))
    out = []
    for i, f in enumerate(frames):
        height = C - tops[i]
        if i < 10:
            rows = [min((body - 1, body, body + 1), key=lambda y: costs[i][y] + (0.5 if y != body else 0)),
                    min((legs - 1, legs, legs + 1), key=lambda y: costs[i][y] + (0.5 if y != legs else 0))]
            if abs(rows[0] - rows[1]) < 2:
                rows = [body, legs]
        else:
            # The dive: the crouches lose two rows, the rolls one, wherever it shows least.
            order = [int(y) for y in np.argsort(costs[i]) if tops[i] < y < C - 1]
            rows = [order[0]]
            if height > 16:
                rows.append(next(y for y in order[1:] if abs(y - order[0]) > 2))
        out.append(without(f, rows))
    return out


def lying(frame, row, body=None):
    """Her standing frame turned on its side, lying on the cell's bottom, her head toward where she faced. A helm's
    frame is turned and placed as her body's (body) is."""
    k = 1 if row in (1, 2, 3) else -1
    turned = np.rot90(frame, k)
    ys, xs = np.where(np.rot90(frame if body is None else body, k)[..., 3] > 0.5)
    part = turned[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    out = np.zeros_like(frame)
    h, w = part.shape[:2]
    left = (C - w) // 2
    out[C - h:, left:left + w] = part
    return out


short = [shorten([raw(row, col) for col in range(14)]) for row in range(8)]
save(np.concatenate([np.concatenate(frames, 1) for frames in short], 0), os.path.join(HERE, "wild_arrow_archer_21.png"))

# --- Helms (2026-11-11, the owner: "Start working on the sprites for the different helms") ------------------------------
# A helm is a layer over her body, a sheet for every clip as the body's (the game's helm layer: "helm_<look>_1h_<clip>").
# It is made from her own head in every frame, so it follows her in all eight directions with nothing drawn by hand
# twice: her head is found in the frame (its dome matched against the standing frame's), its pixels read as hair, skin
# or eyes, and the helm says what each becomes; her long hair hangs out under it. Horns, plumes and feathers are a few
# pixels put on top with an outline. In the two rolls of the dive, where her head is not found, the hair in the top of
# the ball takes the helm's colours. This is the first pass: one rule set for every direction.
def rgb(*c):
    return np.array(c, np.float32) / 255.0


INK = rgb(18, 14, 24)
HAIR = [rgb(240, 200, 104), rgb(230, 194, 92), rgb(190, 134, 52)]      # the first two are its light
SKIN = [rgb(236, 190, 154), rgb(226, 150, 132), rgb(192, 134, 106)]
EYE = rgb(36, 28, 44)


def is_colour(px, colours):
    return any(np.abs(px[:3] - c).max() < 0.02 for c in colours) and px[3] > 0.5


# light, mid, dark; dome: how many rows of the dome it covers from the top (4 all); sides: the hair beside her face;
# face: "open", "mask" (cloth over mouth and chin), "barbute" (a T left open), "visor" (shut but a dark slit);
# band: only a band over the hair (rows of the dome); extra: pixels put on top.
HELMS = {
    "hood": dict(ramp=[rgb(62, 132, 66), rgb(36, 84, 50), rgb(24, 56, 40)], dome=4, sides=True, face="open"),
    "mask": dict(ramp=[rgb(92, 100, 112), rgb(60, 66, 80), rgb(40, 44, 56)], dome=0, band=[3], sides=False, face="mask"),
    "barbute": dict(ramp=[rgb(176, 184, 196), rgb(120, 130, 148), rgb(72, 80, 100)], dome=4, sides=True, face="barbute"),
    "visored": dict(ramp=[rgb(210, 216, 226), rgb(150, 160, 178), rgb(90, 98, 120)], dome=4, sides=True, face="visor", extra="plume"),
    "horned": dict(ramp=[rgb(116, 116, 134), rgb(74, 74, 94), rgb(44, 44, 60)], dome=4, sides=True, face="visor", extra="horns"),
    "falconer": dict(ramp=[rgb(170, 118, 62), rgb(132, 86, 46), rgb(84, 52, 30)], dome=4, sides=True, face="open", extra="feather"),
    "unblinking_crown": dict(ramp=[rgb(232, 140, 36), rgb(176, 92, 28), rgb(110, 58, 24)], dome=0, band=[2, 3], sides=False, face="open", extra="crown"),
}
BONE = [rgb(242, 238, 222), rgb(200, 190, 160)]
PLUME = [rgb(192, 42, 46), rgb(122, 22, 38)]
GEM = rgb(110, 220, 232)
HEAD_ROWS = 8          # the dome's outline row, three rows of dome, four of face
HEAD_LEFT, HEAD_WIDE = 7, 10


def find_head(frame, stand, top):
    """Where her head's box (its top left in the standing frame: HEAD_LEFT, top) is in this frame, or None."""
    patch = stand[top:top + 4, HEAD_LEFT - 1:HEAD_LEFT + HEAD_WIDE + 1]
    solid = patch[..., 3] > 0.5
    best = None
    for dy in range(-4, 11):
        for dx in range(-7, 8):
            y, x = top + dy, HEAD_LEFT - 1 + dx
            if y < 0 or x < 0 or y + 4 > C or x + patch.shape[1] > C:
                continue
            here = frame[y:y + 4, x:x + patch.shape[1]]
            wrong = (np.abs(here - patch).max(-1) > 0.02) & solid
            score = wrong.sum() / max(1, solid.sum()) + 0.002 * (abs(dx) + abs(dy))
            if best is None or score < best[0]:
                best = (score, x + 1, y)
    return (best[1], best[2]) if best[0] < 0.3 else None


def put(helm, frame, x, y, colour, stamped):
    if 0 <= x < C and 0 <= y < C:
        helm[y, x, :3] = colour
        helm[y, x, 3] = 1
        if frame[y, x, 3] < 0.5:
            stamped.append((x, y))


def helm_frame(frame, spec, head, row):
    """The helm's pixels over this frame of her body."""
    helm = np.zeros_like(frame)
    light, mid, dark = spec["ramp"]
    if head is None:
        # A roll: the hair in the top of the ball.
        if spec["dome"]:
            top = int(np.where(frame[..., 3] > 0.5)[0].min())
            for y in range(top, min(C, top + 7)):
                for x in range(C):
                    if is_colour(frame[y, x], HAIR):
                        put(helm, frame, x, y, light if is_colour(frame[y, x], HAIR[:2]) else mid, [])
        return helm
    hx, hy = head
    stamped = []
    faces = [[x for x in range(hx - 1, hx + HEAD_WIDE + 1) if 0 <= x < C and is_colour(frame[hy + r, x], SKIN + [EYE])]
             if hy + r < C else [] for r in range(HEAD_ROWS)]
    seen = any(faces[r] for r in range(4, HEAD_ROWS))
    for r in range(HEAD_ROWS):
        y = hy + r
        if y >= C:
            break
        for x in range(max(0, hx - 1), min(C, hx + HEAD_WIDE + 1)):
            px = frame[y, x]
            hair = is_colour(px, HAIR)
            bright = is_colour(px, HAIR[:2])
            if hair and (r < spec["dome"] or (r >= 4 and spec["sides"]) or r in spec.get("band", ())):
                shade = light if bright else mid
                # The rim: the helm's last row, and the row over her face.
                if spec["sides"] and (r == HEAD_ROWS - 1 or (r == 3 and seen and faces[4] and min(faces[4]) <= x <= max(faces[4]))):
                    shade = dark if not bright else mid
                put(helm, frame, x, y, shade, stamped)
            elif r >= 4 and is_colour(px, SKIN + [EYE]) and faces[r]:
                middle = (min(faces[5] or faces[r]) + max(faces[5] or faces[r])) / 2.0
                face = spec["face"]
                if face == "mask" and r >= 6:
                    put(helm, frame, x, y, light if r == 6 else mid, stamped)
                elif face == "barbute" and (r == 4 or (r >= 6 and abs(x - middle) > 1)):
                    put(helm, frame, x, y, mid if r == 4 else light, stamped)
                elif face == "visor":
                    put(helm, frame, x, y, light if r == 4 else INK if r == 5 else mid if r == 6 else dark, stamped)
    # What stands on top. West-facing rows lean their pieces the other way.
    extra = spec.get("extra")
    east = row in (5, 6, 7)
    side = row in (2, 6)
    cx = hx + 4
    if extra == "horns":
        if side:
            for dx, dy in ((0, 0), (1, 0), (1, -1), (2, -2)):
                put(helm, frame, cx + (dx if not east else 1 - dx), hy + dy, BONE[0] if dy < 0 else BONE[1], stamped)
        else:
            for dx, dy, c in ((-1, 2, 1), (-2, 1, 0), (-2, 0, 0), (-3, -1, 0)):
                put(helm, frame, hx + dx, hy + dy, BONE[c], stamped)
                put(helm, frame, hx + HEAD_WIDE - 1 - dx, hy + dy, BONE[c], stamped)
    elif extra == "plume":
        for dx, dy, c in ((0, 0, 1), (1, 0, 1), (0, -1, 0), (1, -1, 0), (1, -2, 0)):
            put(helm, frame, cx + (dx if not east else 1 - dx), hy + dy, PLUME[c], stamped)
    elif extra == "feather":
        for dx, dy, c in ((7, 1, 1), (8, 0, 0), (8, -1, 0), (9, -2, 0)):
            put(helm, frame, hx + (dx if not east else HEAD_WIDE - 1 - dx), hy + dy, BONE[c] if dy < 1 else PLUME[0], stamped)
    elif extra == "crown":
        # Its points over the band, and the eye in the middle of it where her face shows.
        for x in range(hx + 1, hx + HEAD_WIDE - 1):
            if (x - hx) % 2 == 1 and hy + 1 < C and is_colour(frame[hy + 1, x], HAIR):
                put(helm, frame, x, hy + 1, rgb(255, 232, 140), stamped)
        if seen and faces[5] and max(faces[5]) - min(faces[5]) >= 3:
            gx = int(round((min(faces[5]) + max(faces[5])) / 2.0))
            for x in (gx, gx + (0 if (max(faces[5]) - min(faces[5])) % 2 == 0 else 1)):
                if is_colour(frame[hy + 3, x], HAIR) or helm[hy + 3, x, 3] > 0.5:
                    put(helm, frame, x, hy + 3, GEM, stamped)
    # An outline round what stands outside her.
    for x, y in stamped:
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < C and 0 <= ny < C and frame[ny, nx, 3] < 0.5 and helm[ny, nx, 3] < 0.5:
                helm[ny, nx, :3] = INK
                helm[ny, nx, 3] = 1
    return helm


heads = []
for row in range(8):
    stand = short[row][0]
    top = int(np.where(stand[..., 3] > 0.5)[0].min())
    heads.append([find_head(short[row][col], stand, top) for col in range(14)])
    print("heads", row, heads[row])
helm_cells = {look: [[helm_frame(short[row][col], spec, heads[row][col], row) for col in range(14)] for row in range(8)]
              for look, spec in HELMS.items()}
# Every helm on her, to look at: a helm a block, her standing frame, a walk, a shot and the dive's four in 8 directions.
SHOWN = [0, 3, 8, 10, 11, 12, 13]
sheet_of_helms = []
for look in HELMS:
    block = []
    for col in SHOWN:
        line = []
        for row in range(8):
            cellpx = short[row][col].copy()
            h = helm_cells[look][row][col]
            cellpx[h[..., 3] > 0.5] = h[h[..., 3] > 0.5]
            line.append(cellpx)
        block.append(np.concatenate(line, 1))
    sheet_of_helms.append(np.concatenate(block, 0))
save(np.concatenate(sheet_of_helms, 1), os.path.join(HERE, "wild_arrow_helms_preview.png"))

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


def strip(clip, cells=None):
    """A clip's sheet from her body's frames, or from a helm's (cells)."""
    rows = []
    for row in range(8):
        of = short[row] if cells is None else cells[row]
        frames = [lying(of[0], row, short[row][0]) if col == "lie" else of[col].copy() for col in CLIPS[clip]]
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
for look in HELMS:
    for clip, (seconds, speed) in TIMING.items():
        name = f"helm_{look}_1h_{clip}"
        entry = {"name": name, "frames": len(CLIPS[clip]), "seconds": seconds, "speed": speed, "key": "1h_" + clip}
        if clip in SHARED:
            entry["shared"] = True
        else:
            save(strip(clip, helm_cells[look]), os.path.join(OUT, f"wild_arrow_drawn_{name}.png"))
        animations.append(entry)
with open(os.path.join(OUT, "sheet.json"), "w") as f:
    json.dump({"cellWidth": C * SCALE, "cellHeight": C * SCALE, "pivotX": C * SCALE // 2, "pivotY": SCALE // 2,
               "directions": 8, "animations": animations}, f)
print("wrote", len(animations), "animations,", len(strips), "pictures a look")
